#nullable enable
using System.ComponentModel;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

/// <summary>
///     Adornments are a special form of <see cref="View"/> that appear outside the <see cref="View.Viewport"/>:
///     <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>. They are defined using the
///     <see cref="Thickness"/> class, which specifies the thickness of the sides of a rectangle.
/// </summary>
/// <remarsk>
///     <para>
///         Each of <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/> has slightly different
///         behavior relative to <see cref="ColorScheme"/>, <see cref="View.SetFocus()"/>, keyboard input, and
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
        // By default Adornments can't get focus; has to be enabled specifically.
        CanFocus = false;
        TabStop = TabBehavior.NoStop;
        Parent = parent;
    }

    /// <summary>The Parent of this Adornment (the View this Adornment surrounds).</summary>
    /// <remarks>
    ///     Adornments are distinguished from typical View classes in that they are not sub-views, but have a parent/child
    ///     relationship with their containing View.
    /// </remarks>
    public View? Parent { get; set; }

    #region Thickness

    private Thickness _thickness = Thickness.Empty;

    /// <summary>Defines the rectangle that the <see cref="Adornment"/> will use to draw its content.</summary>
    public Thickness Thickness
    {
        get => _thickness;
        set
        {
            Thickness current = _thickness;
            _thickness = value;

            if (current != _thickness)
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

                OnThicknessChanged ();
            }
        }
    }

    /// <summary>Fired whenever the <see cref="Thickness"/> property changes.</summary>
    [CanBeNull]
    public event EventHandler? ThicknessChanged;

    /// <summary>Called whenever the <see cref="Thickness"/> property changes.</summary>
    public void OnThicknessChanged ()
    {
        ThicknessChanged?.Invoke (this, EventArgs.Empty);
    }

    #endregion Thickness

    #region View Overrides

    /// <summary>
    ///     Adornments cannot be used as sub-views (see <see cref="Parent"/>); setting this property will throw
    ///     <see cref="InvalidOperationException"/>.
    /// </summary>
    public override View? SuperView
    {
        get => null!;
        set => throw new InvalidOperationException (@"Adornments can not be Subviews or have SuperViews. Use Parent instead.");
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
    /// <remarks>
    ///     The Viewport of an Adornment cannot be modified. Attempting to set this property will throw an
    ///     <see cref="InvalidOperationException"/>.
    /// </remarks>
    public override Rectangle Viewport
    {
        get => Frame with { Location = Point.Empty };
        set => throw new InvalidOperationException (@"The Viewport of an Adornment cannot be modified.");
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
        // in screen coords, ...
        Rectangle parentScreen = Parent.FrameToScreen ();

        // ...and add our Frame location to it.
        return new (new (parentScreen.X + Frame.X, parentScreen.Y + Frame.Y), Frame.Size);
    }

    /// <inheritdoc/>
    public override Point ScreenToFrame (in Point location)
    {
        return Parent!.ScreenToFrame (new (location.X - Frame.X, location.Y - Frame.Y));
    }

    /// <summary>Does nothing for Adornment</summary>
    /// <returns></returns>
    public override bool OnDrawAdornments () { return false; }

    /// <summary>Redraws the Adornments that comprise the <see cref="Adornment"/>.</summary>
    public override void OnDrawContent (Rectangle viewport)
    {
        if (Thickness == Thickness.Empty)
        {
            return;
        }

        Rectangle prevClip = SetClip ();

        Rectangle screen = ViewportToScreen (viewport);
        Attribute normalAttr = GetNormalColor ();
        Driver.SetAttribute (normalAttr);

        // This just draws/clears the thickness, not the insides.
        Thickness.Draw (screen, ToString ());

        if (!string.IsNullOrEmpty (TextFormatter.Text))
        {
            if (TextFormatter is { })
            {
                TextFormatter.ConstrainToSize = Frame.Size;
                TextFormatter.NeedsFormat = true;
            }
        }

        TextFormatter?.Draw (screen, normalAttr, normalAttr, Rectangle.Empty);

        if (Subviews.Count > 0)
        {
            base.OnDrawContent (viewport);
        }

        if (Driver is { })
        {
           Driver.Clip = prevClip;
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
        get => false; 
        set => throw new InvalidOperationException (@"Adornment can only render to their Parent or Parent's Superview.");
    }

    #endregion View Overrides

    #region Mouse Support


    /// <summary>
    /// Indicates whether the specified Parent's SuperView-relative coordinates are within the Adornment's Thickness.
    /// </summary>
    /// <remarks>
    ///     The <paramref name="location"/> is relative to the PARENT's SuperView.
    /// </remarks>
    /// <param name="location"></param>
    /// <returns><see langword="true"/> if the specified Parent's SuperView-relative coordinates are within the Adornment's Thickness. </returns>
    public override bool Contains (in Point location)
    {
        if (Parent is null)
        {
            return false;
        }

        Rectangle outside = Frame;
        outside.Offset (Parent.Frame.Location);

        return Thickness.Contains (outside, location);
    }

    ///// <inheritdoc/>
    //protected override bool OnMouseEnter (CancelEventArgs mouseEvent)
    //{
    //    // Invert Normal
    //    if (Diagnostics.HasFlag (ViewDiagnosticFlags.MouseEnter) && ColorScheme != null)
    //    {
    //        var cs = new ColorScheme (ColorScheme)
    //        {
    //            Normal = new (ColorScheme.Normal.Background, ColorScheme.Normal.Foreground)
    //        };
    //        ColorScheme = cs;
    //    }

    //    return false;
    //}

    ///// <inheritdoc/>   
    //protected override void OnMouseLeave ()
    //{
    //    // Invert Normal
    //    if (Diagnostics.FastHasFlags (ViewDiagnosticFlags.MouseEnter) && ColorScheme != null)
    //    {
    //        var cs = new ColorScheme (ColorScheme)
    //        {
    //            Normal = new (ColorScheme.Normal.Background, ColorScheme.Normal.Foreground)
    //        };
    //        ColorScheme = cs;
    //    }
    //}
    #endregion Mouse Support
}
