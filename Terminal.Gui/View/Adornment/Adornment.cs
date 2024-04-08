namespace Terminal.Gui;

/// <summary>
///     Adornments are a special form of <see cref="View"/> that appear outside the <see cref="View.Bounds"/>:
///     <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>. They are defined using the
///     <see cref="Thickness"/> class, which specifies the thickness of the sides of a rectangle.
/// </summary>
/// <remarsk>
///     <para>
///         Each of <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/> has slightly different
///         behavior relative to <see cref="ColorScheme"/>, <see cref="View.SetFocus"/>, keyboard input, and
///         mouse input. Each can be customized by manipulating their Subviews.
///     </para>
/// </remarsk>
public class Adornment : View
{
    /// <inheritdoc/>
    public Adornment ()
    {
        /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <summary>Constructs a new adornment for the view specified by <paramref name="parent"/>.</summary>
    /// <param name="parent"></param>
    public Adornment (View parent)
    {
        CanFocus = true;
        Parent = parent;
    }

    /// <summary>The Parent of this Adornment (the View this Adornment surrounds).</summary>
    /// <remarks>
    ///     Adornments are distinguished from typical View classes in that they are not sub-views, but have a parent/child
    ///     relationship with their containing View.
    /// </remarks>
    public View Parent { get; set; }

    #region Thickness

    private Thickness _thickness = Thickness.Empty;

    /// <summary>Defines the rectangle that the <see cref="Adornment"/> will use to draw its content.</summary>
    public Thickness Thickness
    {
        get => _thickness;
        set
        {
            Thickness prev = _thickness;
            _thickness = value;

            if (prev != _thickness)
            {
                if (Parent?.IsInitialized == false)
                {
                    // When initialized Parent.LayoutSubViews will cause a LayoutAdornments
                    Parent?.LayoutAdornments ();
                }
                else
                {
                    Parent?.SetNeedsLayout ();
                    Parent?.LayoutSubviews ();
                }

                OnThicknessChanged (prev);
            }
        }
    }

    /// <summary>Fired whenever the <see cref="Thickness"/> property changes.</summary>
    public event EventHandler<ThicknessEventArgs> ThicknessChanged;

    /// <summary>Called whenever the <see cref="Thickness"/> property changes.</summary>
    public void OnThicknessChanged (Thickness previousThickness)
    {
        ThicknessChanged?.Invoke (
                                  this,
                                  new () { Thickness = Thickness, PreviousThickness = previousThickness }
                                 );
    }

    #endregion Thickness

    #region View Overrides

    /// <summary>
    ///     Adornments cannot be used as sub-views (see <see cref="Parent"/>); setting this property will throw
    ///     <see cref="InvalidOperationException"/>.
    /// </summary>
    public override View SuperView
    {
        get => null;
        set => throw new NotImplementedException ();
    }

    //internal override Adornment CreateAdornment (Type adornmentType)
    //{
    //    /* Do nothing - Adornments do not have Adornments */
    //    return null;
    //}

    internal override void LayoutAdornments ()
    {
        /* Do nothing - Adornments do not have Adornments */
    }

    /// <summary>
    ///     Gets the rectangle that describes the area of the Adornment. The Location is always (0,0).
    ///     The size is the size of the <see cref="View.Frame"/>.
    /// </summary>
    public override Rectangle Bounds
    {
        get => Frame with { Location = Point.Empty };
        set => throw new InvalidOperationException ("It makes no sense to set Bounds of a Thickness.");
    }

    /// <inheritdoc/>
    public override Rectangle FrameToScreen ()
    {
        if (Parent is null)
        {
            return Frame;
        }

        // Adornments are *Children* of a View, not SubViews. Thus View.FrameToScreen will not work.
        // To get the screen-relative coordinates of an Adornment, we need get the parent's Frame
        // in screen coords, and add our Frame location to it.
        Rectangle parent = Parent.FrameToScreen ();

        return new (new (parent.X + Frame.X, parent.Y + Frame.Y), Frame.Size);
    }

    /// <inheritdoc/>
    public override Point ScreenToFrame (int x, int y) { return Parent.ScreenToFrame (x - Frame.X, y - Frame.Y); }

    /// <summary>Does nothing for Adornment</summary>
    /// <returns></returns>
    public override bool OnDrawAdornments () { return false; }

    /// <summary>Redraws the Adornments that comprise the <see cref="Adornment"/>.</summary>
    public override void OnDrawContent (Rectangle contentArea)
    {
        if (Thickness == Thickness.Empty)
        {
            return;
        }

        Rectangle screenBounds = BoundsToScreen (contentArea);
        Attribute normalAttr = GetNormalColor ();
        Driver.SetAttribute (normalAttr);

        // This just draws/clears the thickness, not the insides.
        Thickness.Draw (screenBounds, ToString ());

        if (!string.IsNullOrEmpty (TextFormatter.Text))
        {
            if (TextFormatter is { })
            {
                TextFormatter.Size = Frame.Size;
                TextFormatter.NeedsFormat = true;
            }
        }

        TextFormatter?.Draw (screenBounds, normalAttr, normalAttr, Rectangle.Empty);

        if (Subviews.Count > 0)
        {
            base.OnDrawContent (contentArea);
        }

        ClearLayoutNeeded ();
        ClearNeedsDisplay ();
    }

    /// <summary>Does nothing for Adornment</summary>
    /// <returns></returns>
    public override bool OnRenderLineCanvas () { return false; }

    /// <summary>
    ///     Adornments only render to their <see cref="Parent"/>'s or Parent's SuperView's LineCanvas, so setting this
    ///     property throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    public override bool SuperViewRendersLineCanvas
    {
        get => false; // throw new NotImplementedException ();
        set => throw new NotImplementedException ();
    }

    #endregion View Overrides

    #region Mouse Support


    /// <summary>
    /// Indicates whether the specified Parent's SuperView-relative coordinates are within the Adornment's Thickness.
    /// </summary>
    /// <remarks>
    ///     The <paramref name="x"/> and <paramref name="x"/> are relative to the PARENT's SuperView.
    /// </remarks>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns><see langword="true"/> if the specified Parent's SuperView-relative coordinates are within the Adornment's Thickness. </returns>
    public override bool Contains (int x, int y)
    {
        if (Parent is null)
        {
            return false;
        }
        Rectangle frame = Frame;
        frame.Offset (Parent.Frame.Location);

        return Thickness.Contains (frame, x, y);
    }

    /// <inheritdoc/>
    protected internal override bool? OnMouseEnter (MouseEvent mouseEvent)
    {
        // Invert Normal
        if (Diagnostics.HasFlag (ViewDiagnosticFlags.MouseEnter) && ColorScheme != null)
        {
            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (ColorScheme.Normal.Background, ColorScheme.Normal.Foreground)
            };
            ColorScheme = cs;
        }

        return base.OnMouseEnter (mouseEvent);
    }

    /// <summary>Called when a mouse event occurs within the Adornment.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Bounds"/>.
    ///     </para>
    ///     <para>
    ///         A mouse click on the Adornment will cause the Parent to focus.
    ///     </para>
    ///     <para>
    ///         A mouse drag on the Adornment will cause the Parent to move.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        if (Parent is null)
        {
            return false;
        }

        var args = new MouseEventEventArgs (mouseEvent);

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
        {
            if (Parent.CanFocus && !Parent.HasFocus)
            {
                Parent.SetFocus ();
                Parent.SetNeedsDisplay ();
            }

            return OnMouseClick (args);
        }

        return false;
    }

    /// <inheritdoc/>
    protected internal override bool OnMouseLeave (MouseEvent mouseEvent)
    {
        // Invert Normal
        if (Diagnostics.HasFlag (ViewDiagnosticFlags.MouseEnter) && ColorScheme != null)
        {
            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (ColorScheme.Normal.Background, ColorScheme.Normal.Foreground)
            };
            ColorScheme = cs;
        }

        return base.OnMouseLeave (mouseEvent);
    }

    #endregion Mouse Support
}
