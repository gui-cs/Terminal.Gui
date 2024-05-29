using System.Diagnostics;

namespace Terminal.Gui;

public partial class View
{
    #region Content Area

    internal Size? _contentSize;

    /// <summary>
    ///     Sets the size of the View's content.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         By default, the content size is set to <see langword="null"/>.
    ///     </para>
    /// </remarks>
    /// <param name="contentSize">
    ///     <para>
    ///         If <see langword="null"/>, and the View has no visible subviews, <see cref="ContentSize"/> will track the size of <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         If <see langword="null"/>, and the View has visible subviews, <see cref="ContentSize"/> will track the maximum position plus size of any
    ///         visible Subviews
    ///         and <c>Viewport.Location</c>  will track the minimum position and size of any visible Subviews.
    ///     </para>
    ///     <para>
    ///         If not <see langword="null"/>, <see cref="ContentSize"/> is set to the passed value and <see cref="Viewport"/> describes the portion of the content currently visible
    ///         to the user. This enables virtual scrolling.
    ///     </para>
    ///     <para>
    ///         If not <see langword="null"/>, <see cref="ContentSize"/> is set to the passed value and the behavior of <see cref="DimAutoStyle.Content"/> will be to use the ContentSize
    ///         to determine the size of the view.
    ///     </para>
    ///     <para>
    ///         Negative sizes are not supported.
    ///     </para>
    /// </param>
    public void SetContentSize (Size? contentSize)
    {
        if (ContentSize.Width < 0 || ContentSize.Height < 0)
        {
            throw new ArgumentException (@"ContentSize cannot be negative.", nameof (contentSize));
        }

        if (contentSize == _contentSize)
        {
            return;
        }

        _contentSize = contentSize;
        OnContentSizeChanged (new (_contentSize));
    }

    /// <summary>
    ///     Gets the size of the View's content.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <see cref="SetContentSize"/> to change to change the content size.
    ///     </para>
    ///     <para>
    ///         If the content size has not been explicitly set with <see cref="SetContentSize"/>, the value tracks
    ///         <see cref="Viewport"/>.
    ///     </para>
    /// </remarks>
    public Size ContentSize => _contentSize ?? Viewport.Size;

    /// <summary>
    /// Called when <see cref="ContentSize"/> has changed.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected bool? OnContentSizeChanged (SizeChangedEventArgs e)
    {
        ContentSizeChanged?.Invoke (this, e);

        if (e.Cancel != true)
        {
            OnResizeNeeded ();
            //SetNeedsLayout ();
            //SetNeedsDisplay ();
        }

        return e.Cancel;
    }

    /// <summary>
    ///     Event raised when the <see cref="ContentSize"/> changes.
    /// </summary>
    public event EventHandler<SizeChangedEventArgs> ContentSizeChanged;

    /// <summary>
    ///     Converts a Content-relative location to a Screen-relative location.
    /// </summary>
    /// <param name="location">The Content-relative location.</param>
    /// <returns>The Screen-relative location.</returns>
    public Point ContentToScreen (in Point location)
    {
        // Subtract the ViewportOffsetFromFrame to get the Viewport-relative location.
        Point viewportOffset = GetViewportOffsetFromFrame ();
        Point contentRelativeToViewport = location;
        contentRelativeToViewport.Offset (-Viewport.X, -Viewport.Y);

        // Translate to Screen-Relative (our SuperView's Viewport-relative coordinates)
        return ViewportToScreen (contentRelativeToViewport);
    }

    /// <summary>Converts a Screen-relative coordinate to a Content-relative coordinate.</summary>
    /// <remarks>
    ///     Content-relative means relative to the top-left corner of the view's Content, which is
    ///     always at <c>0, 0</c>.
    /// </remarks>
    /// <param name="location">The Screen-relative location.</param>
    /// <returns>The coordinate relative to this view's Content.</returns>
    public Point ScreenToContent (in Point location)
    {
        Point viewportOffset = GetViewportOffsetFromFrame ();
        Point screen = ScreenToFrame (location);
        screen.Offset (Viewport.X - viewportOffset.X, Viewport.Y - viewportOffset.Y);

        return screen;
    }

    #endregion Content Area

    #region Viewport

    private ViewportSettings _viewportSettings;

    /// <summary>
    ///     Gets or sets how scrolling the <see cref="View.Viewport"/> on the View's Content Area is handled.
    /// </summary>
    public ViewportSettings ViewportSettings
    {
        get => _viewportSettings;
        set
        {
            if (_viewportSettings == value)
            {
                return;
            }

            _viewportSettings = value;

            if (IsInitialized)
            {
                // Force set Viewport to cause settings to be applied as needed
                SetViewport (Viewport);
            }
        }
    }

    /// <summary>
    ///     The location of the viewport into the view's content (0,0) is the top-left corner of the content. The Content
    ///     area's size
    ///     is <see cref="ContentSize"/>.
    /// </summary>
    private Point _viewportLocation;

    /// <summary>
    ///     Gets or sets the rectangle describing the portion of the View's content that is visible to the user.
    ///     The viewport Location is relative to the top-left corner of the inner rectangle of <see cref="Padding"/>.
    ///     If the viewport Size is the same as <see cref="ContentSize"/>, or <see cref="ContentSize"/> is
    ///     <see langword="null"/> the Location will be <c>0, 0</c>.
    /// </summary>
    /// <value>
    ///     The rectangle describing the location and size of the viewport into the View's virtual content, described by
    ///     <see cref="ContentSize"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         Positive values for the location indicate the visible area is offset into (down-and-right) the View's virtual
    ///         <see cref="ContentSize"/>. This enables scrolling down and to the right (e.g. in a <see cref="ListView"/>.
    ///     </para>
    ///     <para>
    ///         Negative values for the location indicate the visible area is offset above (up-and-left) the View's virtual
    ///         <see cref="ContentSize"/>. This enables scrolling up and to the left (e.g. in an image viewer that supports zoom
    ///         where the image stays centered).
    ///     </para>
    ///     <para>
    ///         The <see cref="ViewportSettings"/> property controls how scrolling is handled. 
    ///     </para>
    ///     <para>
    ///         Updates to the Viewport Size updates <see cref="Frame"/>, and has the same impact as updating the
    ///         <see cref="Frame"/>.
    ///     </para>
    ///     <para>
    ///         Altering the Viewport Size will eventually (when the view is next laid out) cause the
    ///         <see cref="LayoutSubview(View, Size)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    /// </remarks>
    public virtual Rectangle Viewport
    {
        get
        {
            if (Margin is null || Border is null || Padding is null)
            {
                // CreateAdornments has not been called yet.
                return new (_viewportLocation, Frame.Size);
            }

            Thickness thickness = GetAdornmentsThickness ();
            return new (
                        _viewportLocation,
                        new (
                             Math.Max (0, Frame.Size.Width - thickness.Horizontal),
                             Math.Max (0, Frame.Size.Height - thickness.Vertical)
                            ));
        }
        set => SetViewport (value);
    }

    private void SetViewport (Rectangle viewport)
    {
        Rectangle oldViewport = viewport;
        ApplySettings (ref viewport);

        Thickness thickness = GetAdornmentsThickness ();

        Size newSize = new (
                            viewport.Size.Width + thickness.Horizontal,
                            viewport.Size.Height + thickness.Vertical);

        if (newSize == Frame.Size)
        {
            // The change is not changing the Frame, so we don't need to update it.
            // Just call SetNeedsLayout to update the layout.
            if (_viewportLocation != viewport.Location)
            {
                _viewportLocation = viewport.Location;
                SetNeedsLayout ();
            }

            OnViewportChanged (new (IsInitialized ? Viewport : Rectangle.Empty, oldViewport));
            return;
        }

        _viewportLocation = viewport.Location;

        // Update the Frame because we made it bigger or smaller which impacts subviews.
        Frame = Frame with
        {
            Size = newSize
        };

        void ApplySettings (ref Rectangle newViewport)
        {
            if (!ViewportSettings.HasFlag (ViewportSettings.AllowXGreaterThanContentWidth))
            {
                if (newViewport.X >= ContentSize.Width)
                {
                    newViewport.X = ContentSize.Width - 1;
                }
            }

            // IMPORTANT: Check for negative location AFTER checking for location greater than content width
            if (!ViewportSettings.HasFlag (ViewportSettings.AllowNegativeX))
            {
                if (newViewport.X < 0)
                {
                    newViewport.X = 0;
                }
            }

            if (!ViewportSettings.HasFlag (ViewportSettings.AllowYGreaterThanContentHeight))
            {
                if (newViewport.Y >= ContentSize.Height)
                {
                    newViewport.Y = ContentSize.Height - 1;
                }
            }

            // IMPORTANT: Check for negative location AFTER checking for location greater than content width
            if (!ViewportSettings.HasFlag (ViewportSettings.AllowNegativeY))
            {
                if (newViewport.Y < 0)
                {
                    newViewport.Y = 0;
                }
            }
        }
    }

    /// <summary>
    ///     Fired when the <see cref="Viewport"/> changes. This event is fired after the <see cref="Viewport"/> has been updated.
    /// </summary>
    [CanBeNull]
    public event EventHandler<DrawEventArgs> ViewportChanged;

    /// <summary>
    ///     Called when the <see cref="Viewport"/> changes. Invokes the <see cref="ViewportChanged"/> event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnViewportChanged (DrawEventArgs e)
    {
        ViewportChanged?.Invoke (this, e);
    }

    /// <summary>
    ///     Converts a <see cref="Viewport"/>-relative location and size to a screen-relative location and size.
    /// </summary>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    /// <param name="viewport">Viewport-relative location and size.</param>
    /// <returns>Screen-relative location and size.</returns>
    public Rectangle ViewportToScreen (in Rectangle viewport)
    {
        return viewport with { Location = ViewportToScreen (viewport.Location) };
    }

    /// <summary>
    ///     Converts a <see cref="Viewport"/>-relative location to a screen-relative location.
    /// </summary>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    /// <param name="viewportLocation">Viewport-relative location.</param>
    /// <returns>Screen-relative location.</returns>
    public Point ViewportToScreen (in Point viewportLocation)
    {
        // Translate bounds to Frame (our SuperView's Viewport-relative coordinates)
        Rectangle screen = FrameToScreen ();
        Point viewportOffset = GetViewportOffsetFromFrame ();
        screen.Offset (viewportOffset.X + viewportLocation.X, viewportOffset.Y + viewportLocation.Y);

        return screen.Location;
    }

    /// <summary>Converts a screen-relative coordinate to a Viewport-relative coordinate.</summary>
    /// <returns>The coordinate relative to this view's <see cref="Viewport"/>.</returns>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    /// <param name="location">Screen-Relative Coordinate.</param>
    /// <returns>Viewport-relative location.</returns>
    public Point ScreenToViewport (in Point location)
    {
        Point viewportOffset = GetViewportOffsetFromFrame ();
        Point frame = ScreenToFrame (location);
        frame.Offset (-viewportOffset.X, -viewportOffset.Y);

        return frame;
    }

    /// <summary>
    ///     Helper to get the X and Y offset of the Viewport from the Frame. This is the sum of the Left and Top properties
    ///     of <see cref="Margin"/>, <see cref="Border"/> and <see cref="Padding"/>.
    /// </summary>
    public Point GetViewportOffsetFromFrame () { return Padding is null ? Point.Empty : Padding.Thickness.GetInside (Padding.Frame).Location; }

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

        Viewport = Viewport with { X = Viewport.X + cols };

        return true;
    }

    #endregion Viewport
}
