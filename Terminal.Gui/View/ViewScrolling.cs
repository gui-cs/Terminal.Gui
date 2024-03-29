using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     Controls the scrolling behavior of a view.
/// </summary>
[Flags]
public enum ScrollSettings
{
    /// <summary>
    ///     Default settings.
    /// </summary>
    Default = 0,

    /// <summary>
    ///     If set, does not restrict vertical scrolling to the content size.
    /// </summary>
    NoRestrictVertical = 1,

    /// <summary>
    ///     If set, does not restrict horizontal scrolling to the content size.
    /// </summary>
    NoRestrictHorizontal = 2,

    /// <summary>
    ///     If set, does not restrict either vertical or horizontal scrolling to the content size.
    /// </summary>
    NoRestrict = NoRestrictVertical | NoRestrictHorizontal
}

public partial class View
{
    #region Content Area

    private Size _contentSize;

    /// <summary>
    ///     Gets or sets the size of the View's content. If the value is <c>Size.Empty</c> the size of the content is
    ///     the same as the size of the <see cref="Viewport"/>, and <c>Viewport.Location</c> will always be <c>0, 0</c>.
    ///     If a positive size is provided, <see cref="Viewport"/> describes the portion of the content currently visible
    ///     to the view. This enables virtual scrolling.
    /// </summary>
    public Size ContentSize
    {
        get => _contentSize == Size.Empty ? Viewport.Size : _contentSize;
        set => _contentSize = value;
    }

    /// <summary>
    ///     Converts a content-relative location to a screen-relative location.
    /// </summary>
    /// <param name="location"></param>
    /// <returns>The screen-relative location.</returns>
    public Point ContentToScreen (in Point location)
    {
        // Translate to Viewport
        Point viewportOffset = GetViewportOffset ();
        Point contentRelativeToViewport = location;
        contentRelativeToViewport.Offset (-Viewport.X, -Viewport.Y);

        // Translate to Frame (our SuperView's Viewport-relative coordinates)
        Rectangle screen = ViewportToScreen (new (contentRelativeToViewport, Size.Empty));

        return screen.Location;
    }

    /// <summary>Converts a screen-relative coordinate to a Content-relative coordinate.</summary>
    /// <remarks>
    ///     Content-relative means relative to the top-left corner of the view's Content.
    /// </remarks>
    /// <param name="x">Column relative to the left side of the Content.</param>
    /// <param name="y">Row relative to the top of the Content</param>
    /// <returns>The coordinate relative to this view's Content.</returns>
    public Point ScreenToContent (int x, int y)
    {
        Point viewportOffset = GetViewportOffset ();
        Point screen = ScreenToFrame (x, y);
        screen.Offset (Viewport.X - viewportOffset.X, Viewport.Y - viewportOffset.Y);

        return screen;
    }

    #endregion Content Area

    #region Viewport

    /// <summary>
    ///     Gets or sets the scrolling behavior of the view.
    /// </summary>
    public ScrollSettings ScrollSettings { get; set; }

    private Point _viewportOffset;

    /// <summary>
    ///     Gets or sets the rectangle describing the portion of the View's content that is visible to the user.
    ///     The viewport Location is relative to the top-left corner of the inner rectangle of the <see cref="Adornment"/>s.
    ///     If the viewport Size is the sames as the <see cref="ContentSize"/> the Location will be <c>0, 0</c>.
    ///     Non-zero values for the location indicate the visible area is offset into the View's virtual
    ///     <see cref="ContentSize"/>.
    /// </summary>
    /// <value>
    ///     The rectangle describing the location and size of the viewport into the View's virtual content, described by
    ///     <see cref="ContentSize"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         If <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Computed"/> the value of Viewport is indeterminate until
    ///         the view has been initialized ( <see cref="IsInitialized"/> is true) and <see cref="LayoutSubviews"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Updates to the Viewport Size updates <see cref="Frame"/>, and has the same impact as updating the
    ///         <see cref="Frame"/>.
    ///     </para>
    ///     <para>
    ///         Altering the Viewport Size will eventually (when the view is next laid out) cause the
    ///         <see cref="LayoutSubview(View, Rectangle)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    /// </remarks>
    public virtual Rectangle Viewport
    {
        get
        {
#if DEBUG
            if (LayoutStyle == LayoutStyle.Computed && !IsInitialized)
            {
                Debug.WriteLine (
                                 $"WARNING: Viewport is being accessed before the View has been initialized. This is likely a bug in {this}"
                                );
            }
#endif // DEBUG

            if (Margin is null || Border is null || Padding is null)
            {
                // CreateAdornments has not been called yet.
                return new (_viewportOffset, Frame.Size);
            }

            Thickness totalThickness = GetAdornmentsThickness ();

            return new (
                        _viewportOffset,
                        new (
                             Math.Max (0, Frame.Size.Width - totalThickness.Horizontal),
                             Math.Max (0, Frame.Size.Height - totalThickness.Vertical)));
        }
        set
        {
            _viewportOffset = value.Location;

            Thickness totalThickness = GetAdornmentsThickness ();
            Size newSize = new (value.Size.Width + totalThickness.Horizontal,
                                value.Size.Height + totalThickness.Vertical);
            if (newSize == Frame.Size)
            {
                SetNeedsLayout ();
                return;
            }

            Frame = Frame with
            {
                Size = newSize
            };
        }
    }

    /// <summary>
    ///     Converts a <see cref="Viewport"/>-relative location to a screen-relative location.
    /// </summary>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    public Rectangle ViewportToScreen (in Rectangle location)
    {
        // Translate bounds to Frame (our SuperView's Viewport-relative coordinates)
        Rectangle screen = FrameToScreen ();
        Point viewportOffset = GetViewportOffset ();
        screen.Offset (viewportOffset.X + location.X, viewportOffset.Y + location.Y);

        return new (screen.Location, location.Size);
    }

    /// <summary>Converts a screen-relative coordinate to a Viewport-relative coordinate.</summary>
    /// <returns>The coordinate relative to this view's <see cref="Viewport"/>.</returns>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    /// <param name="x">Column relative to the left side of the Viewport.</param>
    /// <param name="y">Row relative to the top of the Viewport</param>
    public Point ScreenToViewport (int x, int y)
    {
        Point viewportOffset = GetViewportOffset ();
        Point screen = ScreenToFrame (x, y);
        screen.Offset (-viewportOffset.X, -viewportOffset.Y);

        return screen;
    }

    /// <summary>
    ///     Helper to get the X and Y offset of the Viewport from the Frame. This is the sum of the Left and Top properties
    ///     of <see cref="Margin"/>, <see cref="Border"/> and <see cref="Padding"/>.
    /// </summary>
    public Point GetViewportOffset () { return Padding is null ? Point.Empty : Padding.Thickness.GetInside (Padding.Frame).Location; }

    /// <summary>
    ///     Scrolls the view vertically by the specified number of rows.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     </para>
    /// </remarks>
    /// <param name="rows"></param>
    /// <returns><see langword="true"/> if the <see cref="Viewport"/> was changed.</returns>
    public bool? ScrollVertical (int rows)
    {
        if (ContentSize == Size.Empty || ContentSize == Viewport.Size)
        {
            return false;
        }

        if (!ScrollSettings.HasFlag (ScrollSettings.NoRestrictVertical)
            && (Viewport.Y + rows > ContentSize.Height - Viewport.Height || Viewport.Y + rows < 0))
        {
            return false;
        }

        Viewport = Viewport with { Y = Viewport.Y + rows };

        return true;
    }

    /// <summary>
    ///     Scrolls the view horizontally by the specified number of columns.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     </para>
    /// </remarks>
    /// <param name="cols"></param>
    /// <returns><see langword="true"/> if the <see cref="Viewport"/> was changed.</returns>
    public bool? ScrollHorizontal (int cols)
    {
        if (ContentSize == Size.Empty || ContentSize == Viewport.Size)
        {
            return false;
        }

        if (!ScrollSettings.HasFlag (ScrollSettings.NoRestrictHorizontal)
            && (Viewport.X + cols > ContentSize.Width - Viewport.Width || Viewport.X + cols < 0))
        {
            return false;
        }

        Viewport = Viewport with { X = Viewport.X + cols };

        return true;
    }

    #endregion Viewport
}
