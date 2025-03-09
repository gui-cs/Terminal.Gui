#nullable enable
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
    ///         See the View Layout Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         Negative sizes are not supported.
    ///     </para>
    ///     <para>
    ///         If not explicitly set, and the View has no visible subviews, <see cref="GetContentSize ()"/> will return the
    ///         size of
    ///         <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         If not explicitly set, and the View has visible subviews, <see cref="GetContentSize ()"/> will return the
    ///         maximum
    ///         position + dimension of the Subviews, supporting <see cref="Dim.Auto"/> with the
    ///         <see cref="DimAutoStyle.Content"/> flag set.
    ///     </para>
    ///     <para>
    ///         If set <see cref="Viewport"/> describes the portion of the content currently visible to the user. This enables
    ///         virtual scrolling.
    ///     </para>
    ///     <para>
    ///         If set the behavior of <see cref="DimAutoStyle.Content"/> will be to use the ContentSize to determine the size
    ///         of the view.
    ///     </para>
    /// </remarks>
    public void SetContentSize (Size? contentSize)
    {
        if (contentSize is { } && (contentSize.Value.Width < 0 || contentSize.Value.Height < 0))
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
    /// <remarks>a>
    ///     <para>
    ///         See the View Layout Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         If the content size was not explicitly set by <see cref="SetContentSize"/>, and the View has no visible subviews, <see cref="GetContentSize ()"/> will return the
    ///         size of
    ///         <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         If the content size was not explicitly set by <see cref="SetContentSize"/>, and the View has visible subviews, <see cref="GetContentSize ()"/> will return the
    ///         maximum
    ///         position + dimension of the Subviews, supporting <see cref="Dim.Auto"/> with the
    ///         <see cref="DimAutoStyle.Content"/> flag set.
    ///     </para>
    ///     <para>
    ///         If set <see cref="Viewport"/> describes the portion of the content currently visible to the user. This enables
    ///         virtual scrolling.
    ///     </para>
    ///     <para>
    ///         If set the behavior of <see cref="DimAutoStyle.Content"/> will be to use the ContentSize to determine the size
    ///         of the view.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     If the content size was not explicitly set by <see cref="SetContentSize"/>, <see cref="GetContentSize ()"/> will
    ///     return the size of the <see cref="Viewport"/> and <see cref="ContentSizeTracksViewport"/> will be <see langword="true"/>.
    /// </returns>
    public Size GetContentSize () { return _contentSize ?? Viewport.Size; }

    /// <summary>
    ///     Gets or sets a value indicating whether the view's content size tracks the <see cref="Viewport"/>'s
    ///     size or not.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <list type="bullet">
    ///         <listheader>
    ///             <term>Value</term> <description>Result</description>
    ///         </listheader>
    ///         <item>
    ///             <term>
    ///                 <see langword="true"/>
    ///             </term>
    ///             <description>
    ///                 <para>
    ///                     <see cref="GetContentSize ()"/> will return the <see cref="Viewport"/>'s size. Content scrolling
    ///                     will be
    ///                     disabled.
    ///                 </para>
    ///                 <para>
    ///                     The behavior of <see cref="DimAutoStyle.Content"/> will be to use position and size of the Subviews
    ///                     to
    ///                     determine the size of the view, ignoring <see cref="GetContentSize ()"/>.
    ///                 </para>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see langword="false"/>
    ///             </term>
    ///             <description>
    ///                 <para>
    ///                     The return value of <see cref="GetContentSize ()"/> is independent of <see cref="Viewport"/> and <see cref="Viewport"/>
    ///                     describes the portion of the content currently visible to the user enabling content scrolling.
    ///                 </para>
    ///                 <para>
    ///                     The behavior of <see cref="DimAutoStyle.Content"/> will be to use <see cref="GetContentSize ()"/>
    ///                     to
    ///                     determine the
    ///                     size of the view, ignoring the position and size of the Subviews.
    ///                 </para>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public bool ContentSizeTracksViewport
    {
        get => _contentSize is null;
        set => _contentSize = value ? null : _contentSize;
    }

    /// <summary>
    ///     Called when <see cref="GetContentSize ()"/> has changed.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    protected bool? OnContentSizeChanged (SizeChangedEventArgs e)
    {
        ContentSizeChanged?.Invoke (this, e);

        if (e.Cancel != true)
        {
            SetNeedsLayout ();
        }

        return e.Cancel;
    }

    /// <summary>
    ///     Event raised when the <see cref="GetContentSize ()"/> changes.
    /// </summary>
    public event EventHandler<SizeChangedEventArgs>? ContentSizeChanged;

    /// <summary>
    ///     Converts a Content-relative location to a Screen-relative location.
    /// </summary>
    /// <param name="location">The Content-relative location.</param>
    /// <returns>The Screen-relative location.</returns>
    public Point ContentToScreen (in Point location)
    {
        // Subtract the ViewportOffsetFromFrame to get the Viewport-relative location.
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
    ///     is <see cref="GetContentSize ()"/>.
    /// </summary>
    private Point _viewportLocation;

    /// <summary>
    ///     Gets or sets the rectangle describing the portion of the View's content that is visible to the user.
    ///     The viewport Location is relative to the top-left corner of the inner rectangle of <see cref="Padding"/>.
    ///     If the viewport Size is the same as <see cref="GetContentSize ()"/>, or <see cref="GetContentSize ()"/> is
    ///     <see langword="null"/> the Location will be <c>0, 0</c>.
    /// </summary>
    /// <value>
    ///     The rectangle describing the location and size of the viewport into the View's virtual content, described by
    ///     <see cref="GetContentSize ()"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         Positive values for the location indicate the visible area is offset into (down-and-right) the View's virtual
    ///         <see cref="GetContentSize ()"/>. This enables scrolling down and to the right (e.g. in a <see cref="ListView"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         Negative values for the location indicate the visible area is offset above (up-and-left) the View's virtual
    ///         <see cref="GetContentSize ()"/>. This enables scrolling up and to the left (e.g. in an image viewer that
    ///         supports
    ///         zoom
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
    ///         <see cref="Layout()"/> and <see cref="OnDrawingContent()"/> methods to be called.
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
                //SetNeedsDraw();
                //SetSubViewNeedsDraw();
            }

            RaiseViewportChangedEvent (oldViewport);

            return;
        }

        _viewportLocation = viewport.Location;

        // Update the Frame because we made it bigger or smaller which impacts subviews.
        Frame = Frame with
        {
            Size = newSize
        };

        // Note, setting the Frame will cause ViewportChanged to be raised.

        return;

        void ApplySettings (ref Rectangle newViewport)
        {
            if (!ViewportSettings.HasFlag (ViewportSettings.AllowXGreaterThanContentWidth))
            {
                if (newViewport.X >= GetContentSize ().Width)
                {
                    newViewport.X = GetContentSize ().Width - 1;
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

            if (!ViewportSettings.HasFlag (ViewportSettings.AllowNegativeXWhenWidthGreaterThanContentWidth))
            {
                if (Viewport.Width > GetContentSize ().Width)
                {
                    newViewport.X = 0;
                }
            }

            if (!ViewportSettings.HasFlag (ViewportSettings.AllowYGreaterThanContentHeight))
            {
                if (newViewport.Y >= GetContentSize ().Height)
                {
                    newViewport.Y = GetContentSize ().Height - 1;
                }
            }

            if (!ViewportSettings.HasFlag (ViewportSettings.AllowNegativeYWhenHeightGreaterThanContentHeight))
            {
                if (Viewport.Height > GetContentSize ().Height)
                {
                    newViewport.Y = 0;
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

    private void RaiseViewportChangedEvent (Rectangle oldViewport)
    {
        var args = new DrawEventArgs (IsInitialized ? Viewport : Rectangle.Empty, oldViewport, null);
        OnViewportChanged (args);
        ViewportChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Fired when the <see cref="Viewport"/> changes. This event is fired after the <see cref="Viewport"/> has been
    ///     updated.
    /// </summary>
    public event EventHandler<DrawEventArgs>? ViewportChanged;

    /// <summary>
    ///     Called when the <see cref="Viewport"/> changes. Invokes the <see cref="ViewportChanged"/> event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnViewportChanged (DrawEventArgs e) { }

    /// <summary>
    ///     Converts a <see cref="Viewport"/>-relative location and size to a screen-relative location and size.
    /// </summary>
    /// <remarks>
    ///     Viewport-relative means relative to the top-left corner of the inner rectangle of the <see cref="Padding"/>.
    /// </remarks>
    /// <param name="viewport">Viewport-relative location and size.</param>
    /// <returns>Screen-relative location and size.</returns>
    public Rectangle ViewportToScreen (in Rectangle viewport) { return viewport with { Location = ViewportToScreen (viewport.Location) }; }

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

    /// <summary>
    ///     Gets the Viewport rectangle with a screen-relative location.
    /// </summary>
    /// <returns>Screen-relative location and size.</returns>
    public Rectangle ViewportToScreen ()
    {
        // Translate bounds to Frame (our SuperView's Viewport-relative coordinates)
        Rectangle screen = FrameToScreen ();
        Point viewportOffset = GetViewportOffsetFromFrame ();
        screen.Offset (viewportOffset.X, viewportOffset.Y);

        return screen;
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
        if (GetContentSize () == Size.Empty || GetContentSize () == Viewport.Size)
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
        if (GetContentSize () == Size.Empty || GetContentSize () == Viewport.Size)
        {
            return false;
        }

        Viewport = Viewport with { X = Viewport.X + cols };

        return true;
    }

    #endregion Viewport
}
