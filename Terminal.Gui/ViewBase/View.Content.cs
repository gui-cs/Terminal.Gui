namespace Terminal.Gui.ViewBase;

public partial class View
{
    #region Content Area

    // nullable holder of developer specified Content Size. If null then the developer did not
    // explicitly set it and the content size will be calculated dynamically.
    private Size? _contentSize;

    /// <summary>
    ///     Sets the size of the View's content.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         Negative sizes are not supported.
    ///     </para>
    ///     <para>
    ///         If not explicitly set to a non-<see langword="null"/> value, and the View has Subviews,
    ///         <see cref="GetContentSize ()"/> will return
    ///         the size of the <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         If set to a non-<see langword="null"/> value, <see cref="Viewport"/> describes the portion of the content
    ///         currently visible to the user. This enables
    ///         virtual scrolling and the behavior of <see cref="DimAutoStyle.Content"/> will be to use
    ///         <see cref="GetContentSize ()"/> to determine the size
    ///         of the view.
    ///     </para>
    ///     <para>
    ///         This method follows the Cancellable Work Pattern (CWP). The <see cref="ContentSizeChanging"/> event
    ///         is raised before the change, and <see cref="ContentSizeChanged"/> is raised after.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ContentSizeChanging"/>
    /// <seealso cref="ContentSizeChanged"/>
    public void SetContentSize (Size? contentSize)
    {
        if (contentSize is { } && (contentSize.Value.Width < 0 || contentSize.Value.Height < 0))
        {
            throw new ArgumentException (@"ContentSize cannot be negative.", nameof (contentSize));
        }

        CWPPropertyHelper.ChangeProperty (this,
                                          ref _contentSize,
                                          contentSize,
                                          OnContentSizeChanging,
                                          ContentSizeChanging,
                                          _ => SetNeedsLayout (),
                                          OnContentSizeChanged,
                                          ContentSizeChanged,
                                          out Size? _);
    }

    /// <summary>
    ///     Gets the size of the View's content.
    /// </summary>
    /// <remarks>
    ///     a>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         If the content size was not explicitly set by <see cref="SetContentSize"/>, and the View has no visible
    ///         subviews, <see cref="GetContentSize ()"/> will return the
    ///         size of
    ///         <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         If the content size was not explicitly set by <see cref="SetContentSize"/>, this function will return the
    ///         Viewport size.
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
    ///     return the size of the <see cref="Viewport"/> and <see cref="ContentSizeTracksViewport"/> will be
    ///     <see langword="true"/>.
    /// </returns>
    public Size GetContentSize () => _contentSize ?? Viewport.Size;

    /// <summary>
    ///     Gets the minimum number of columns required for all the View's SubViews to fit in the content area.
    /// </summary>
    /// <returns></returns>
    public int GetWidthRequiredForSubViews () =>

        // DimAuto.Calculate adds the Adornments thickness, so we need to subtract it here since
        // we want the content size only.
        GetAutoWidth () - GetAdornmentsThickness ().Horizontal;

    /// <summary>
    ///     Gets the natural (auto-size) width of the view determined by Dim.Auto, which is the minimum of the size required to
    ///     fit all subviews and the container size.
    /// </summary>
    /// <returns></returns>
    public int GetAutoWidth () => Dim.Auto ().Calculate (0, GetContainerSize ().Width, this, Dimension.Width);

    /// <summary>
    ///     Gets the natural (auto-size) height of the view determined by Dim.Auto, which is the minimum of the size required
    ///     to fit all subviews and the container size.
    /// </summary>
    /// <returns></returns>
    public int GetAutoHeight () => Dim.Auto ().Calculate (0, GetContainerSize ().Height, this, Dimension.Height);

    /// <summary>
    ///     Gets the minimum number of rows required for all the View's SubViews to fit in the content area.
    /// </summary>
    /// <returns></returns>
    public int GetHeightRequiredForSubViews () =>

        // DimAuto.Calculate adds the Adornments thickness, so we need to subtract it here since
        // we want the content size only.
        GetAutoHeight () - GetAdornmentsThickness ().Vertical;

    /// <summary>
    ///     Gets or sets a value indicating whether the view's content size tracks the <see cref="Viewport"/>'s
    ///     size or not.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/layout.html"/>
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
    ///                     The behavior of <see cref="DimAutoStyle.Content"/> will be to use position and size of the SubViews
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
    ///                     The return value of <see cref="GetContentSize ()"/> is independent of <see cref="Viewport"/> and
    ///                     <see cref="Viewport"/>
    ///                     describes the portion of the content currently visible to the user enabling content scrolling.
    ///                 </para>
    ///                 <para>
    ///                     The behavior of <see cref="DimAutoStyle.Content"/> will be to use <see cref="GetContentSize ()"/>
    ///                     to
    ///                     determine the
    ///                     size of the view, ignoring the position and size of the SubViews.
    ///                 </para>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public bool ContentSizeTracksViewport { get => _contentSize is null; set => _contentSize = value ? null : _contentSize; }

    /// <summary>
    ///     Called before the content size changes, allowing subclasses to cancel or modify the change.
    /// </summary>
    /// <param name="args">The event arguments containing the current and proposed new content size.</param>
    /// <returns>True to cancel the change, false to proceed.</returns>
    protected virtual bool OnContentSizeChanging (ValueChangingEventArgs<Size?> args) => false;

    /// <summary>
    ///     Called after the content size has changed.
    /// </summary>
    /// <param name="args">The event arguments containing the old and new content size.</param>
    protected virtual void OnContentSizeChanged (ValueChangedEventArgs<Size?> args) { }

    /// <summary>
    ///     Raised before the content size changes, allowing handlers to modify or cancel the change.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set <see cref="ValueChangingEventArgs{T}.Handled"/> to true to cancel the change or modify
    ///         <see cref="ValueChangingEventArgs{T}.NewValue"/> to adjust the proposed value.
    ///     </para>
    ///     <para>
    ///         This event follows the Cancellable Work Pattern (CWP). See the
    ///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/cancellable-work-pattern.html">CWP Deep Dive</see>
    ///         for more information.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.ContentSizeChanging += (sender, args) =>
    ///         {
    ///             if (args.NewValue?.Width > 1000)
    ///             {
    ///                 args.Handled = true;
    ///                 Console.WriteLine("Content size too large, change cancelled.");
    ///             }
    ///         };
    ///     </code>
    /// </example>
    public event EventHandler<ValueChangingEventArgs<Size?>>? ContentSizeChanging;

    /// <summary>
    ///     Raised after the content size has changed, notifying handlers of the completed change.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides the old and new content size via <see cref="ValueChangedEventArgs{T}.OldValue"/> and
    ///         <see cref="ValueChangedEventArgs{T}.NewValue"/>, which may be null.
    ///     </para>
    ///     <para>
    ///         This event follows the Cancellable Work Pattern (CWP). See the
    ///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/cancellable-work-pattern.html">CWP Deep Dive</see>
    ///         for more information.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.ContentSizeChanged += (sender, args) =>
    ///         {
    ///             Console.WriteLine($"Content size changed from {args.OldValue} to {args.NewValue}.");
    ///         };
    ///     </code>
    /// </example>
    public event EventHandler<ValueChangedEventArgs<Size?>>? ContentSizeChanged;

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

    #endregion Content Area

    #region Viewport

    /// <summary>
    ///     Gets or sets how scrolling the <see cref="View.Viewport"/> on the View's Content Area is handled.
    /// </summary>
    public ViewportSettingsFlags ViewportSettings
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            ViewportSettingsFlags oldFlags = field;
            field = value;

            SyncScrollBarsToSettings (oldFlags, value);

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
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/layout.html"/>
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
    ///         <see cref="Layout()"/> and <see cref="OnDrawingContent(DrawContext)"/> methods to be called.
    ///     </para>
    /// </remarks>
    public virtual Rectangle Viewport
    {
        get
        {
            Thickness thickness = GetAdornmentsThickness ();

            return new Rectangle (_viewportLocation,
                                  new Size (Math.Max (0, Frame.Size.Width - thickness.Horizontal), Math.Max (0, Frame.Size.Height - thickness.Vertical)));
        }
        set => SetViewport (value);
    }

    private void SetViewport (Rectangle viewport)
    {
        Rectangle oldViewport = new (_viewportLocation, Viewport.Size);
        ApplySettings (ref viewport);

        Thickness thickness = GetAdornmentsThickness ();

        Size newSize = new (viewport.Size.Width + thickness.Horizontal, viewport.Size.Height + thickness.Vertical);

        if (newSize == Frame.Size)
        {
            // The change is not changing the Frame, so we don't need to update it.
            // Just call SetNeedsLayout to update the layout.
            if (_viewportLocation != viewport.Location)
            {
                _viewportLocation = viewport.Location;
                SetNeedsLayout ();
            }

            // QUESTION: Shouldn't this be inside the if statement above?
            RaiseViewportChangedEvent (oldViewport);

            return;
        }

        _viewportLocation = viewport.Location;

        // Update the Frame because we made it bigger or smaller which impacts subviews.
        Frame = Frame with { Size = newSize };

        // Note, setting the Frame will cause ViewportChanged to be raised.

        return;

        void ApplySettings (ref Rectangle newViewport)
        {
            if (!ViewportSettings.HasFlag (ViewportSettingsFlags.AllowXGreaterThanContentWidth))
            {
                if (newViewport.X >= GetContentSize ().Width)
                {
                    newViewport.X = GetContentSize ().Width - 1;
                }
            }

            if (!ViewportSettings.HasFlag (ViewportSettingsFlags.AllowXPlusWidthGreaterThanContentWidth))
            {
                if (newViewport.X + newViewport.Width > GetContentSize ().Width)
                {
                    newViewport.X = GetContentSize ().Width - newViewport.Width;
                }
            }

            // IMPORTANT: Check for negative location AFTER checking for location greater than content size
            if (!ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeX))
            {
                if (newViewport.X < 0)
                {
                    newViewport.X = 0;
                }
            }

            if (!ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeXWhenWidthGreaterThanContentWidth))
            {
                if (Viewport.Width > GetContentSize ().Width)
                {
                    newViewport.X = 0;
                }
            }

            if (!ViewportSettings.HasFlag (ViewportSettingsFlags.AllowYGreaterThanContentHeight))
            {
                if (newViewport.Y >= GetContentSize ().Height)
                {
                    newViewport.Y = GetContentSize ().Height - 1;
                }
            }

            if (!ViewportSettings.HasFlag (ViewportSettingsFlags.AllowYPlusHeightGreaterThanContentHeight))
            {
                if (newViewport.Y + newViewport.Height > GetContentSize ().Height)
                {
                    newViewport.Y = GetContentSize ().Height - newViewport.Height;
                }
            }

            // IMPORTANT: Check for negative location AFTER checking for location greater than content size
            if (!ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeY))
            {
                if (newViewport.Y < 0)
                {
                    newViewport.Y = 0;
                }
            }

            if (!ViewportSettings.HasFlag (ViewportSettingsFlags.AllowNegativeYWhenHeightGreaterThanContentHeight))
            {
                if (Viewport.Height > GetContentSize ().Height)
                {
                    newViewport.Y = 0;
                }
            }
        }
    }

    private void RaiseViewportChangedEvent (Rectangle oldViewport)
    {
        if (Cursor.IsVisible)
        {
            // Adjust the cursor if visible
            int deltaX = oldViewport.X - Viewport.X;
            int deltaY = oldViewport.Y - Viewport.Y;

            Point currentCursorPos = ScreenToViewport (Cursor.Position!.Value);

            SetCursor (Cursor with { Position = ViewportToScreen (new Point (currentCursorPos.X + deltaX, currentCursorPos.Y + deltaY)) });
        }

        DrawEventArgs args = new (IsInitialized ? Viewport : Rectangle.Empty, oldViewport, null);
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
    public Rectangle ViewportToScreen (in Rectangle viewport) => viewport with { Location = ViewportToScreen (viewport.Location) };

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

        return screen with { Size = Viewport.Size };
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
    public Point GetViewportOffsetFromFrame () => Padding.Thickness.GetInside (Padding.GetFrame ()).Location;

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
        if (GetContentSize () == Size.Empty || GetContentSize ().Width == Viewport.Size.Width)
        {
            return false;
        }

        Viewport = Viewport with { X = Viewport.X + cols };

        return true;
    }

    #endregion Viewport
}
