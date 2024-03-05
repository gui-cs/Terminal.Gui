namespace Terminal.Gui;

// TODO: Missing 3D effect - 3D effects will be drawn by a mechanism separate from Adornments
// TODO: If a Adornment has focus, navigation keys (e.g Command.NextView) should cycle through SubViews of the Adornments
// QUESTION: How does a user navigate out of an Adornment to another Adornment, or back into the Parent's SubViews?

/// <summary>
///     Adornments are a special form of <see cref="View"/> that appear outside the <see cref="View.Bounds"/>:
///     <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>. They are defined using the
///     <see cref="Thickness"/> class, which specifies the thickness of the sides of a rectangle.
/// </summary>
/// <remarsk>
///     <para>
///         There is no prevision for creating additional subclasses of Adornment. It is not abstract to enable unit
///         testing.
///     </para>
///     <para>Each of <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/> can be customized.</para>
/// </remarsk>
public class Adornment : View
{
    private Point? _dragPosition;

    private Point _startGrabPoint;
    private Thickness _thickness = Thickness.Empty;

    /// <inheritdoc/>
    public Adornment ()
    {
        /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <summary>Constructs a new adornment for the view specified by <paramref name="parent"/>.</summary>
    /// <param name="parent"></param>
    public Adornment (View parent)
    {
        Application.GrabbingMouse += Application_GrabbingMouse;
        Application.UnGrabbingMouse += Application_UnGrabbingMouse;
        CanFocus = true;
        Parent = parent;
    }

    /// <summary>Gets the rectangle that describes the inner area of the Adornment. The Location is always (0,0).</summary>
    public override Rectangle Bounds
    {
        get => new (Point.Empty, Thickness?.GetInside (new (Point.Empty, Frame.Size)).Size ?? Frame.Size);

        // QUESTION: So why even have a setter then?
        set => throw new InvalidOperationException ("It makes no sense to set Bounds of a Thickness.");
    }

    /// <summary>The Parent of this Adornment (the View this Adornment surrounds).</summary>
    /// <remarks>
    ///     Adornments are distinguished from typical View classes in that they are not sub-views, but have a parent/child
    ///     relationship with their containing View.
    /// </remarks>
    public View Parent { get; set; }

    /// <summary>
    ///     Adornments cannot be used as sub-views (see <see cref="Parent"/>); this method always throws an
    ///     <see cref="InvalidOperationException"/>. TODO: Are we sure?
    /// </summary>
    public override View SuperView
    {
        get => null;
        set => throw new NotImplementedException ();
    }

    /// <summary>
    ///     Adornments only render to their <see cref="Parent"/>'s or Parent's SuperView's LineCanvas, so setting this
    ///     property throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    public override bool SuperViewRendersLineCanvas
    {
        get => false; // throw new NotImplementedException ();
        set => throw new NotImplementedException ();
    }

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
                Parent?.LayoutAdornments ();
                OnThicknessChanged (prev);
            }
        }
    }

    /// <inheritdoc/>
    public override void BoundsToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true)
    {
        // Adornments are *Children* of a View, not SubViews. Thus View.BoundsToScreen will not work.
        // To get the screen-relative coordinates of a Adornment, we need to know who
        // the Parent is
        Rectangle parentFrame = Parent?.Frame ?? Frame;
        rrow = row + parentFrame.Y;
        rcol = col + parentFrame.X;

        // We now have rcol/rrow in coordinates relative to our View's SuperView. If our View's SuperView has
        // a SuperView, keep going...
        Parent?.SuperView?.BoundsToScreen (rcol, rrow, out rcol, out rrow, clipped);
    }

    /// <inheritdoc/>
    public override Rectangle FrameToScreen ()
    {
        if (Parent is null)
        {
            return Frame;
        }

        // Adornments are *Children* of a View, not SubViews. Thus View.FrameToScreen will not work.
        // To get the screen-relative coordinates of a Adornment, we need to know who
        // the Parent is
        Rectangle parent = Parent.FrameToScreen ();

        // We now have coordinates relative to our View. If our View's SuperView has
        // a SuperView, keep going...
        return new (new (parent.X + Frame.X, parent.Y + Frame.Y), Frame.Size);
    }

    /// <inheritdoc/>
    public override Point ScreenToFrame (int x, int y)
    {
            return Parent.ScreenToFrame (x - Frame.X, y - Frame.Y);
    }

    ///// <inheritdoc/>
    //public override void SetNeedsDisplay (Rectangle region)
    //{
    //    SetSubViewNeedsDisplay ();
    //    foreach (View subView in Subviews)
    //    {
    //        subView.SetNeedsDisplay ();
    //    }
    //}

    /// <inheritdoc/>
    //protected override void ClearNeedsDisplay ()
    //{
    //    base.ClearNeedsDisplay ();
    //    foreach (View subView in Subviews)
    //    {
    //        subView.NeedsDisplay = false;
    //    }
    //}

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

        Rectangle screenBounds = BoundsToScreen (Frame);

        Attribute normalAttr = GetNormalColor ();

        // This just draws/clears the thickness, not the insides.
        Driver.SetAttribute (normalAttr);
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

    /// <summary>Called whenever the <see cref="Thickness"/> property changes.</summary>
    public virtual void OnThicknessChanged (Thickness previousThickness)
    {
        ThicknessChanged?.Invoke (
                                  this,
                                  new () { Thickness = Thickness, PreviousThickness = previousThickness }
                                 );
    }

    /// <summary>Fired whenever the <see cref="Thickness"/> property changes.</summary>
    public event EventHandler<ThicknessEventArgs> ThicknessChanged;

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

        // TODO: Checking for Toplevel is a hack until #2537 is fixed
        if (!Parent.CanFocus || !Parent.Arrangement.HasFlag(ViewArrangement.Movable))
        {
            return true;
        }

        int nx, ny;

        // BUGBUG: This is true even when the mouse started dragging outside of the Adornment, which is not correct.
        if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            Parent.SetFocus ();
            Application.BringOverlappedTopToFront ();

            // Only start grabbing if the user clicks in the Thickness area
            if (Thickness.Contains (Frame, mouseEvent.X, mouseEvent.Y) && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
            {
                _startGrabPoint = new (mouseEvent.X, mouseEvent.Y);
                _dragPosition = new (mouseEvent.X, mouseEvent.Y);
                Application.GrabMouse (this);
            }

            return true;
        }

        if (mouseEvent.Flags is (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            if (Application.MouseGrabView == this && _dragPosition.HasValue)
            {
                if (Parent.SuperView is null)
                {
                    // Redraw the entire app window.
                    Application.Top.SetNeedsDisplay ();
                }
                else
                {
                    Parent.SuperView.SetNeedsDisplay ();
                }

                _dragPosition = new Point (mouseEvent.X, mouseEvent.Y);

                Point parentLoc = Parent.SuperView?.ScreenToBounds (mouseEvent.ScreenPosition.X, mouseEvent.ScreenPosition.Y) ?? mouseEvent.ScreenPosition;

                GetLocationThatFits (
                                     Parent,
                                     parentLoc.X - _startGrabPoint.X,
                                     parentLoc.Y - _startGrabPoint.Y,
                                     out nx,
                                     out ny,
                                     out _,
                                     out _
                                    );

                Parent.X = nx;
                Parent.Y = ny;

                return true;
            }
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && _dragPosition.HasValue)
        {
            _dragPosition = null;
            Application.UngrabMouse ();
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Application.GrabbingMouse -= Application_GrabbingMouse;
        Application.UnGrabbingMouse -= Application_UnGrabbingMouse;

        _dragPosition = null;
        base.Dispose (disposing);
    }

    internal override Adornment CreateAdornment (Type adornmentType)
    {
        /* Do nothing - Adornments do not have Adornments */
        return null;
    }

    internal override void LayoutAdornments ()
    {
        /* Do nothing - Adornments do not have Adornments */
    }

    private void Application_GrabbingMouse (object sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }

    private void Application_UnGrabbingMouse (object sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }
}
