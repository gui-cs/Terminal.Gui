#nullable enable
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Terminal.Gui;

public partial class View // Layout APIs
{
    /// <summary>
    ///     Indicates whether the specified SuperView-relative coordinates are within the View's <see cref="Frame"/>.
    /// </summary>
    /// <param name="location">SuperView-relative coordinate</param>
    /// <returns><see langword="true"/> if the specified SuperView-relative coordinates are within the View.</returns>
    public virtual bool Contains (in Point location) { return Frame.Contains (location); }

    // BUGBUG: This method interferes with Dialog/MessageBox default min/max size.
    /// <summary>
    ///     Gets a new location of the <see cref="View"/> that is within the Viewport of the <paramref name="viewToMove"/>'s
    ///     <see cref="View.SuperView"/> (e.g. for dragging a Window). The `out` parameters are the new X and Y coordinates.
    /// </summary>
    /// <remarks>
    ///     If <paramref name="viewToMove"/> does not have a <see cref="View.SuperView"/> or it's SuperView is not
    ///     <see cref="Application.Top"/> the position will be bound by the <see cref="ConsoleDriver.Cols"/> and
    ///     <see cref="ConsoleDriver.Rows"/>.
    /// </remarks>
    /// <param name="viewToMove">The View that is to be moved.</param>
    /// <param name="targetX">The target x location.</param>
    /// <param name="targetY">The target y location.</param>
    /// <param name="nx">The new x location that will ensure <paramref name="viewToMove"/> will be fully visible.</param>
    /// <param name="ny">The new y location that will ensure <paramref name="viewToMove"/> will be fully visible.</param>
    /// <returns>
    ///     Either <see cref="Application.Top"/> (if <paramref name="viewToMove"/> does not have a Super View) or
    ///     <paramref name="viewToMove"/>'s SuperView. This can be used to ensure LayoutSubviews is called on the correct View.
    /// </returns>
    internal static View? GetLocationEnsuringFullVisibility (
        View viewToMove,
        int targetX,
        int targetY,
        out int nx,
        out int ny
       //,
       // out StatusBar? statusBar
    )
    {
        int maxDimension;
        View? superView;
        //statusBar = null!;

        if (viewToMove is not Toplevel || viewToMove?.SuperView is null || viewToMove == Application.Top || viewToMove?.SuperView == Application.Top)
        {
            maxDimension = Driver.Cols;
            superView = Application.Top;
        }
        else
        {
            // Use the SuperView's Viewport, not Frame
            maxDimension = viewToMove!.SuperView.Viewport.Width;
            superView = viewToMove.SuperView;
        }

        if (superView?.Margin is { } && superView == viewToMove!.SuperView)
        {
            maxDimension -= superView.GetAdornmentsThickness ().Left + superView.GetAdornmentsThickness ().Right;
        }

        if (viewToMove!.Frame.Width <= maxDimension)
        {
            nx = Math.Max (targetX, 0);
            nx = nx + viewToMove.Frame.Width > maxDimension ? Math.Max (maxDimension - viewToMove.Frame.Width, 0) : nx;

            if (nx > viewToMove.Frame.X + viewToMove.Frame.Width)
            {
                nx = Math.Max (viewToMove.Frame.Right, 0);
            }
        }
        else
        {
            nx = targetX;
        }

        //System.Diagnostics.Debug.WriteLine ($"nx:{nx}, rWidth:{rWidth}");
        var menuVisible = false;
        var statusVisible = false;

        if (viewToMove?.SuperView is null || viewToMove == Application.Top || viewToMove?.SuperView == Application.Top)
        {
            menuVisible = Application.Top?.MenuBar?.Visible == true;
        }
        else
        {
            View? t = viewToMove!.SuperView;

            while (t is { } and not Toplevel)
            {
                t = t.SuperView;
            }

            if (t is Toplevel topLevel)
            {
                menuVisible = topLevel.MenuBar?.Visible == true;
            }
        }

        if (viewToMove?.SuperView is null || viewToMove == Application.Top || viewToMove?.SuperView == Application.Top)
        {
            maxDimension = menuVisible ? 1 : 0;
        }
        else
        {
            maxDimension = 0;
        }

        ny = Math.Max (targetY, maxDimension);

        //if (viewToMove?.SuperView is null || viewToMove == Application.Top || viewToMove?.SuperView == Application.Top)
        //{
        //    statusVisible = Application.Top?.StatusBar?.Visible == true;
        //    statusBar = Application.Top?.StatusBar!;
        //}
        //else
        //{
        //    View? t = viewToMove!.SuperView;

        //    while (t is { } and not Toplevel)
        //    {
        //        t = t.SuperView;
        //    }

        //    if (t is Toplevel topLevel)
        //    {
        //        statusVisible = topLevel.StatusBar?.Visible == true;
        //        statusBar = topLevel.StatusBar!;
        //    }
        //}

        if (viewToMove?.SuperView is null || viewToMove == Application.Top || viewToMove?.SuperView == Application.Top)
        {
            maxDimension = statusVisible ? Driver.Rows - 1 : Driver.Rows;
        }
        else
        {
            maxDimension = statusVisible ? viewToMove!.SuperView.Viewport.Height - 1 : viewToMove!.SuperView.Viewport.Height;
        }

        if (superView?.Margin is { } && superView == viewToMove?.SuperView)
        {
            maxDimension -= superView.GetAdornmentsThickness ().Top + superView.GetAdornmentsThickness ().Bottom;
        }

        ny = Math.Min (ny, maxDimension);

        if (viewToMove?.Frame.Height <= maxDimension)
        {
            ny = ny + viewToMove.Frame.Height > maxDimension
                     ? Math.Max (maxDimension - viewToMove.Frame.Height, menuVisible ? 1 : 0)
                     : ny;

            if (ny > viewToMove.Frame.Y + viewToMove.Frame.Height)
            {
                ny = Math.Max (viewToMove.Frame.Bottom, 0);
            }
        }

        //System.Diagnostics.Debug.WriteLine ($"ny:{ny}, rHeight:{rHeight}");

        return superView!;
    }

    #region Frame

    private Rectangle _frame;

    /// <summary>Gets or sets the absolute location and dimension of the view.</summary>
    /// <value>
    ///     The rectangle describing absolute location and dimension of the view, in coordinates relative to the
    ///     <see cref="SuperView"/>'s Content, which is bound by <see cref="GetContentSize ()"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         Frame is relative to the <see cref="SuperView"/>'s Content, which is bound by <see cref="GetContentSize ()"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         Setting Frame will set <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> to the
    ///         values of the corresponding properties of the <paramref name="value"/> parameter.
    ///     </para>
    ///     <para>
    ///         Altering the Frame will eventually (when the view hierarchy is next laid out via  see
    ///         cref="LayoutSubviews"/>) cause <see cref="LayoutSubview(View, Size)"/> and
    ///         <see cref="OnDrawContent(Rectangle)"/>
    ///         methods to be called.
    ///     </para>
    /// </remarks>
    public Rectangle Frame
    {
        get => _frame;
        set
        {
            if (_frame == value)
            {
                return;
            }

            SetFrame (value with { Width = Math.Max (value.Width, 0), Height = Math.Max (value.Height, 0) });

            // If Frame gets set, set all Pos/Dim to Absolute values.
            _x = _frame.X;
            _y = _frame.Y;
            _width = _frame.Width;
            _height = _frame.Height;

            if (IsInitialized)
            {
                OnResizeNeeded ();
            }

            SetNeedsDisplay ();
        }
    }

    private void SetFrame (in Rectangle frame)
    {
        var oldViewport = Rectangle.Empty;

        if (IsInitialized)
        {
            oldViewport = Viewport;
        }

        // This is the only place where _frame should be set directly. Use Frame = or SetFrame instead.
        _frame = frame;

        OnViewportChanged (new (IsInitialized ? Viewport : Rectangle.Empty, oldViewport));
    }

    /// <summary>Gets the <see cref="Frame"/> with a screen-relative location.</summary>
    /// <returns>The location and size of the view in screen-relative coordinates.</returns>
    public virtual Rectangle FrameToScreen ()
    {
        Rectangle screen = Frame;
        View? current = SuperView;

        while (current is { })
        {
            if (current is Adornment adornment)
            {
                // Adornments don't have SuperViews; use Adornment.FrameToScreen override
                // which will give us the screen coordinates of the parent

                Rectangle parentScreen = adornment.FrameToScreen ();

                // Now add our Frame location
                parentScreen.Offset (screen.X, screen.Y);

                return parentScreen;
            }

            Point viewportOffset = current.GetViewportOffsetFromFrame ();
            viewportOffset.Offset (current.Frame.X - current.Viewport.X, current.Frame.Y - current.Viewport.Y);
            screen.X += viewportOffset.X;
            screen.Y += viewportOffset.Y;
            current = current.SuperView;
        }

        return screen;
    }

    /// <summary>
    ///     Converts a screen-relative coordinate to a Frame-relative coordinate. Frame-relative means relative to the
    ///     View's <see cref="SuperView"/>'s <see cref="Viewport"/>.
    /// </summary>
    /// <returns>The coordinate relative to the <see cref="SuperView"/>'s <see cref="Viewport"/>.</returns>
    /// <param name="location">Screen-relative coordinate.</param>
    public virtual Point ScreenToFrame (in Point location)
    {
        if (SuperView is null)
        {
            return new (location.X - Frame.X, location.Y - Frame.Y);
        }

        Point superViewViewportOffset = SuperView.GetViewportOffsetFromFrame ();
        superViewViewportOffset.Offset (-SuperView.Viewport.X, -SuperView.Viewport.Y);

        Point frame = location;
        frame.Offset (-superViewViewportOffset.X, -superViewViewportOffset.Y);

        frame = SuperView.ScreenToFrame (frame);
        frame.Offset (-Frame.X, -Frame.Y);

        return frame;
    }

    private Pos _x = Pos.Absolute (0);

    /// <summary>Gets or sets the X position for the view (the column).</summary>
    /// <value>The <see cref="Pos"/> object representing the X position.</value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         The position is relative to the <see cref="SuperView"/>'s Content, which is bound by
    ///         <see cref="GetContentSize ()"/>.
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Pos.Center"/>) the value is indeterminate until the view has been
    ///         initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Size)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated.
    ///     </para>
    ///     <para>The default value is <c>Pos.At (0)</c>.</para>
    /// </remarks>
    public Pos X
    {
        get => VerifyIsInitialized (_x, nameof (X));
        set
        {
            if (Equals (_x, value))
            {
                return;
            }

            _x = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (X)} cannot be null");

            OnResizeNeeded ();
        }
    }

    private Pos _y = Pos.Absolute (0);

    /// <summary>Gets or sets the Y position for the view (the row).</summary>
    /// <value>The <see cref="Pos"/> object representing the Y position.</value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         The position is relative to the <see cref="SuperView"/>'s Content, which is bound by
    ///         <see cref="GetContentSize ()"/>.
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Pos.Center"/>) the value is indeterminate until the view has been
    ///         initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Size)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated.
    ///     </para>
    ///     <para>The default value is <c>Pos.At (0)</c>.</para>
    /// </remarks>
    public Pos Y
    {
        get => VerifyIsInitialized (_y, nameof (Y));
        set
        {
            if (Equals (_y, value))
            {
                return;
            }

            _y = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (Y)} cannot be null");
            OnResizeNeeded ();
        }
    }

    private Dim? _height = Dim.Absolute (0);

    /// <summary>Gets or sets the height dimension of the view.</summary>
    /// <value>The <see cref="Dim"/> object representing the height of the view (the number of rows).</value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         The dimension is relative to the <see cref="SuperView"/>'s Content, which is bound by
    ///         <see cref="GetContentSize ()"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Dim.Fill(Dim)"/>) the value is indeterminate until the view has
    ///         been initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Size)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated.
    ///     </para>
    ///     <para>The default value is <c>Dim.Sized (0)</c>.</para>
    /// </remarks>
    public Dim? Height
    {
        get => VerifyIsInitialized (_height, nameof (Height));
        set
        {
            if (Equals (_height, value))
            {
                return;
            }

            if (_height is { } && _height.Has<DimAuto> (out _))
            {
                // Reset ContentSize to Viewport
                _contentSize = null;
            }

            _height = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (Height)} cannot be null");

            // Reset TextFormatter - Will be recalculated in SetTextFormatterSize
            TextFormatter.ConstrainToHeight = null;

            OnResizeNeeded ();
        }
    }

    private Dim? _width = Dim.Absolute (0);

    /// <summary>Gets or sets the width dimension of the view.</summary>
    /// <value>The <see cref="Dim"/> object representing the width of the view (the number of columns).</value>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         The dimension is relative to the <see cref="SuperView"/>'s Content, which is bound by
    ///         <see cref="GetContentSize ()"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Dim.Fill(Dim)"/>) the value is indeterminate until the view has
    ///         been initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Size)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated.
    ///     </para>
    ///     <para>The default value is <c>Dim.Sized (0)</c>.</para>
    /// </remarks>
    public Dim? Width
    {
        get => VerifyIsInitialized (_width, nameof (Width));
        set
        {
            if (Equals (_width, value))
            {
                return;
            }

            if (_width is { } && _width.Has<DimAuto> (out _))
            {
                // Reset ContentSize to Viewport
                _contentSize = null;
            }

            _width = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (Width)} cannot be null");

            // Reset TextFormatter - Will be recalculated in SetTextFormatterSize
            TextFormatter.ConstrainToWidth = null;

            OnResizeNeeded ();
        }
    }

    #endregion Frame

    #region Layout Engine

    /// <summary>Fired after the View's <see cref="LayoutSubviews"/> method has completed.</summary>
    /// <remarks>
    ///     Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has
    ///     otherwise changed.
    /// </remarks>
    public event EventHandler<LayoutEventArgs>? LayoutComplete;

    /// <summary>Fired after the View's <see cref="LayoutSubviews"/> method has completed.</summary>
    /// <remarks>
    ///     Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has
    ///     otherwise changed.
    /// </remarks>
    public event EventHandler<LayoutEventArgs>? LayoutStarted;

    /// <summary>
    ///     Adjusts <see cref="Frame"/> given the SuperView's ContentSize (nominally the same as
    ///     <c>this.SuperView.GetContentSize ()</c>)
    ///     and the position (<see cref="X"/>, <see cref="Y"/>) and dimension (<see cref="Width"/>, and
    ///     <see cref="Height"/>).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, or <see cref="Height"/> are
    ///         absolute, they will be updated to reflect the new size and position of the view. Otherwise, they
    ///         are left unchanged.
    ///     </para>
    ///     <para>
    ///         If any of the view's subviews have a position or dimension dependent on either <see cref="GetContentSize"/> or
    ///         other subviews, <see cref="LayoutSubview"/> on
    ///         will be called for that subview.
    ///     </para>
    /// </remarks>
    /// <param name="superviewContentSize">
    ///     The size of the SuperView's content (nominally the same as <c>this.SuperView.GetContentSize ()</c>).
    /// </param>
    internal void SetRelativeLayout (Size superviewContentSize)
    {
        Debug.Assert (_x is { });
        Debug.Assert (_y is { });
        Debug.Assert (_width is { });
        Debug.Assert (_height is { });

        CheckDimAuto ();
        SetTextFormatterSize ();

        int newX, newW, newY, newH;

        // Calculate the new X, Y, Width, and Height
        // If the Width or Height is Dim.Auto, calculate the Width or Height first. Otherwise, calculate the X or Y first.
        if (_width is DimAuto)
        {
            newW = _width.Calculate (0, superviewContentSize.Width, this, Dimension.Width);
            newX = _x.Calculate (superviewContentSize.Width, newW, this, Dimension.Width);
        }
        else
        {
            newX = _x.Calculate (superviewContentSize.Width, _width, this, Dimension.Width);
            newW = _width.Calculate (newX, superviewContentSize.Width, this, Dimension.Width);
        }

        if (_height is DimAuto)
        {
            newH = _height.Calculate (0, superviewContentSize.Height, this, Dimension.Height);
            newY = _y.Calculate (superviewContentSize.Height, newH, this, Dimension.Height);
        }
        else
        {
            newY = _y.Calculate (superviewContentSize.Height, _height, this, Dimension.Height);
            newH = _height.Calculate (newY, superviewContentSize.Height, this, Dimension.Height);
        }

        Rectangle newFrame = new (newX, newY, newW, newH);

        if (Frame != newFrame)
        {
            // Set the frame. Do NOT use `Frame` as it overwrites X, Y, Width, and Height
            SetFrame (newFrame);

            if (_x is PosAbsolute)
            {
                _x = Frame.X;
            }

            if (_y is PosAbsolute)
            {
                _y = Frame.Y;
            }

            if (_width is DimAbsolute)
            {
                _width = Frame.Width;
            }

            if (_height is DimAbsolute)
            {
                _height = Frame.Height;
            }

            if (!string.IsNullOrEmpty (Title))
            {
                SetTitleTextFormatterSize ();
            }

            SetNeedsLayout ();
            SetNeedsDisplay ();
        }

        if (TextFormatter.ConstrainToWidth is null)
        {
            TextFormatter.ConstrainToWidth = GetContentSize ().Width;
        }

        if (TextFormatter.ConstrainToHeight is null)
        {
            TextFormatter.ConstrainToHeight = GetContentSize ().Height;
        }
    }

    /// <summary>
    ///     Invoked when the dimensions of the view have changed, for example in response to the container view or terminal
    ///     resizing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See the View Layout Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
    ///     </para>
    ///     <para>
    ///         The position and dimensions of the view are indeterminate until the view has been initialized. Therefore, the
    ///         behavior of this method is indeterminate if <see cref="IsInitialized"/> is <see langword="false"/>.
    ///     </para>
    ///     <para>Raises the <see cref="LayoutComplete"/> event before it returns.</para>
    /// </remarks>
    public virtual void LayoutSubviews ()
    {
        if (!IsInitialized)
        {
            Debug.WriteLine ($"WARNING: LayoutSubviews called before view has been initialized. This is likely a bug in {this}");
        }

        if (!LayoutNeeded)
        {
            return;
        }

        CheckDimAuto ();

        Size contentSize = GetContentSize ();
        OnLayoutStarted (new (contentSize));

        LayoutAdornments ();

        // Sort out the dependencies of the X, Y, Width, Height properties
        HashSet<View> nodes = new ();
        HashSet<(View, View)> edges = new ();
        CollectAll (this, ref nodes, ref edges);
        List<View> ordered = TopologicalSort (SuperView!, nodes, edges);

        foreach (View v in ordered)
        {
            LayoutSubview (v, contentSize);
        }

        // If the 'to' is rooted to 'from' it's a special-case.
        // Use LayoutSubview with the Frame of the 'from'.
        if (SuperView is { } && GetTopSuperView () is { } && LayoutNeeded && edges.Count > 0)
        {
            foreach ((View from, View to) in edges)
            {
                LayoutSubview (to, from.GetContentSize ());
            }
        }

        LayoutNeeded = false;

        OnLayoutComplete (new (contentSize));
    }

    private void LayoutSubview (View v, Size contentSize)
    {
        // Note, SetRelativeLayout calls SetTextFormatterSize
        v.SetRelativeLayout (contentSize);
        v.LayoutSubviews ();
        v.LayoutNeeded = false;
    }

    /// <summary>Indicates that the view does not need to be laid out.</summary>
    protected void ClearLayoutNeeded () { LayoutNeeded = false; }

    /// <summary>
    ///     Raises the <see cref="LayoutComplete"/> event. Called from  <see cref="LayoutSubviews"/> before all sub-views
    ///     have been laid out.
    /// </summary>
    internal virtual void OnLayoutComplete (LayoutEventArgs args) { LayoutComplete?.Invoke (this, args); }

    /// <summary>
    ///     Raises the <see cref="LayoutStarted"/> event. Called from  <see cref="LayoutSubviews"/> before any subviews
    ///     have been laid out.
    /// </summary>
    internal virtual void OnLayoutStarted (LayoutEventArgs args) { LayoutStarted?.Invoke (this, args); }

    /// <summary>
    ///     Called whenever the view needs to be resized. This is called whenever <see cref="Frame"/>,
    ///     <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, or <see cref="View.Height"/> changes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Determines the relative bounds of the <see cref="View"/> and its <see cref="Frame"/>s, and then calls
    ///         <see cref="SetRelativeLayout"/> to update the view.
    ///     </para>
    /// </remarks>
    internal void OnResizeNeeded ()
    {
        // TODO: Identify a real-world use-case where this API should be virtual. 
        // TODO: Until then leave it `internal` and non-virtual

        // Determine our container's ContentSize -
        //  First try SuperView.Viewport, then Application.Top, then Driver.Viewport.
        //  Finally, if none of those are valid, use 2048 (for Unit tests).
        Size superViewContentSize = SuperView is { IsInitialized: true } ? SuperView.GetContentSize () :
                                    Application.Top is { } && Application.Top != this && Application.Top.IsInitialized ? Application.Top.GetContentSize () :
                                    Application.Screen.Size;

        SetRelativeLayout (superViewContentSize);

        if (IsInitialized)
        {
            LayoutAdornments ();
        }

        SetNeedsLayout ();

        // TODO: This ensures overlapped views are drawn correctly. However, this is inefficient.
        // TODO: The correct fix is to implement non-rectangular clip regions: https://github.com/gui-cs/Terminal.Gui/issues/3413
        if (Arrangement.HasFlag (ViewArrangement.Overlapped))
        {
            foreach (Toplevel v in Application.TopLevels)
            {
                if (v.Visible && v != this)
                {
                    v.SetNeedsDisplay ();
                }
            }
        }
    }

    internal bool LayoutNeeded { get; private set; } = true;

    /// <summary>
    ///     Sets <see cref="LayoutNeeded"/> for this View and all of it's subviews and it's SuperView.
    ///     The main loop will call SetRelativeLayout and LayoutSubviews for any view with <see cref="LayoutNeeded"/> set.
    /// </summary>
    internal void SetNeedsLayout ()
    {
        if (LayoutNeeded)
        {
            return;
        }

        LayoutNeeded = true;

        foreach (View view in Subviews)
        {
            view.SetNeedsLayout ();
        }

        TextFormatter.NeedsFormat = true;
        SuperView?.SetNeedsLayout ();
    }

    /// <summary>
    ///     Collects all views and their dependencies from a given starting view for layout purposes. Used by
    ///     <see cref="TopologicalSort"/> to create an ordered list of views to layout.
    /// </summary>
    /// <param name="from">The starting view from which to collect dependencies.</param>
    /// <param name="nNodes">A reference to a set of views representing nodes in the layout graph.</param>
    /// <param name="nEdges">
    ///     A reference to a set of tuples representing edges in the layout graph, where each tuple consists of a pair of views
    ///     indicating a dependency.
    /// </param>
    internal void CollectAll (View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        foreach (View? v in from.InternalSubviews)
        {
            nNodes.Add (v);
            CollectPos (v.X, v, ref nNodes, ref nEdges);
            CollectPos (v.Y, v, ref nNodes, ref nEdges);
            CollectDim (v.Width, v, ref nNodes, ref nEdges);
            CollectDim (v.Height, v, ref nNodes, ref nEdges);
        }
    }

    /// <summary>
    ///     Collects dimension (where Width or Height is `DimView`) dependencies for a given view.
    /// </summary>
    /// <param name="dim">The dimension (width or height) to collect dependencies for.</param>
    /// <param name="from">The view for which to collect dimension dependencies.</param>
    /// <param name="nNodes">A reference to a set of views representing nodes in the layout graph.</param>
    /// <param name="nEdges">
    ///     A reference to a set of tuples representing edges in the layout graph, where each tuple consists of a pair of views
    ///     indicating a dependency.
    /// </param>
    internal void CollectDim (Dim? dim, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        switch (dim)
        {
            case DimView dv:
                // See #2461
                //if (!from.InternalSubviews.Contains (dv.Target)) {
                //	throw new InvalidOperationException ($"View {dv.Target} is not a subview of {from}");
                //}
                if (dv.Target != this)
                {
                    nEdges.Add ((dv.Target!, from));
                }

                return;
            case DimCombine dc:
                CollectDim (dc.Left, from, ref nNodes, ref nEdges);
                CollectDim (dc.Right, from, ref nNodes, ref nEdges);

                break;
        }
    }

    /// <summary>
    ///     Collects position (where X or Y is `PosView`) dependencies for a given view.
    /// </summary>
    /// <param name="pos">The position (X or Y) to collect dependencies for.</param>
    /// <param name="from">The view for which to collect position dependencies.</param>
    /// <param name="nNodes">A reference to a set of views representing nodes in the layout graph.</param>
    /// <param name="nEdges">
    ///     A reference to a set of tuples representing edges in the layout graph, where each tuple consists of a pair of views
    ///     indicating a dependency.
    /// </param>
    internal void CollectPos (Pos pos, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        switch (pos)
        {
            case PosView pv:
                // See #2461
                //if (!from.InternalSubviews.Contains (pv.Target)) {
                //	throw new InvalidOperationException ($"View {pv.Target} is not a subview of {from}");
                //}
                if (pv.Target != this)
                {
                    nEdges.Add ((pv.Target!, from));
                }

                return;
            case PosCombine pc:
                CollectPos (pc.Left, from, ref nNodes, ref nEdges);
                CollectPos (pc.Right, from, ref nNodes, ref nEdges);

                break;
        }
    }

    // https://en.wikipedia.org/wiki/Topological_sorting
    internal static List<View> TopologicalSort (
        View superView,
        IEnumerable<View> nodes,
        ICollection<(View From, View To)> edges
    )
    {
        List<View> result = new ();

        // Set of all nodes with no incoming edges
        HashSet<View> noEdgeNodes = new (nodes.Where (n => edges.All (e => !e.To.Equals (n))));

        while (noEdgeNodes.Any ())
        {
            //  remove a node n from S
            View n = noEdgeNodes.First ();
            noEdgeNodes.Remove (n);

            // add n to tail of L
            if (n != superView)
            {
                result.Add (n);
            }

            // for each node m with an edge e from n to m do
            foreach ((View From, View To) e in edges.Where (e => e.From.Equals (n)).ToArray ())
            {
                View m = e.To;

                // remove edge e from the graph
                edges.Remove (e);

                // if m has no other incoming edges then
                if (edges.All (me => !me.To.Equals (m)) && m != superView)
                {
                    // insert m into S
                    noEdgeNodes.Add (m);
                }
            }
        }

        if (!edges.Any ())
        {
            return result;
        }

        foreach ((View from, View to) in edges)
        {
            if (from == to)
            {
                // if not yet added to the result, add it and remove from edge
                if (result.Find (v => v == from) is null)
                {
                    result.Add (from);
                }

                edges.Remove ((from, to));
            }
            else if (from.SuperView == to.SuperView)
            {
                // if 'from' is not yet added to the result, add it
                if (result.Find (v => v == from) is null)
                {
                    result.Add (from);
                }

                // if 'to' is not yet added to the result, add it
                if (result.Find (v => v == to) is null)
                {
                    result.Add (to);
                }

                // remove from edge
                edges.Remove ((from, to));
            }
            else if (from != superView?.GetTopSuperView (to, from) && !ReferenceEquals (from, to))
            {
                if (ReferenceEquals (from.SuperView, to))
                {
                    throw new InvalidOperationException (
                                                         $"ComputedLayout for \"{superView}\": \"{to}\" "
                                                         + $"references a SubView (\"{from}\")."
                                                        );
                }

                throw new InvalidOperationException (
                                                     $"ComputedLayout for \"{superView}\": \"{from}\" "
                                                     + $"linked with \"{to}\" was not found. Did you forget to add it to {superView}?"
                                                    );
            }
        }

        // return L (a topologically sorted order)
        return result;
    } // TopologicalSort

    // Diagnostics to highlight when X or Y is read before the view has been initialized
    private Pos VerifyIsInitialized (Pos pos, string member)
    {
        //#if DEBUG
        //        if (pos.ReferencesOtherViews () && !IsInitialized)
        //        {
        //            Debug.WriteLine (
        //                             $"WARNING: {member} = {pos} of {this} is dependent on other views and {member} "
        //                             + $"is being accessed before the View has been initialized. This is likely a bug."
        //                            );
        //        }
        //#endif // DEBUG
        return pos;
    }

    // Diagnostics to highlight when Width or Height is read before the view has been initialized
    private Dim? VerifyIsInitialized (Dim? dim, string member)
    {
        //#if DEBUG
        //        if (dim.ReferencesOtherViews () && !IsInitialized)
        //        {
        //            Debug.WriteLine (
        //                             $"WARNING: {member} = {dim} of {this} is dependent on other views and {member} "
        //                             + $"is being accessed before the View has been initialized. This is likely a bug."
        //                            );
        //        }
        //#endif // DEBUG
        return dim;
    }

    /// <summary>Gets or sets whether validation of <see cref="Pos"/> and <see cref="Dim"/> occurs.</summary>
    /// <remarks>
    ///     Setting this to <see langword="true"/> will enable validation of <see cref="X"/>, <see cref="Y"/>,
    ///     <see cref="Width"/>, and <see cref="Height"/> during set operations and in <see cref="LayoutSubviews"/>. If invalid
    ///     settings are discovered exceptions will be thrown indicating the error. This will impose a performance penalty and
    ///     thus should only be used for debugging.
    /// </remarks>
    public bool ValidatePosDim { get; set; }

    // TODO: Move this logic into the Pos/Dim classes
    /// <summary>
    ///     Throws an <see cref="InvalidOperationException"/> if any SubViews are using Dim objects that depend on this
    ///     Views dimensions.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void CheckDimAuto ()
    {
        if (!ValidatePosDim || !IsInitialized)
        {
            return;
        }

        var widthAuto = Width as DimAuto;
        var heightAuto = Height as DimAuto;

        // Verify none of the subviews are using Dim objects that depend on the SuperView's dimensions.
        foreach (View view in Subviews)
        {
            if (widthAuto is { } && widthAuto.Style.FastHasFlags (DimAutoStyle.Content) && ContentSizeTracksViewport)
            {
                ThrowInvalid (view, view.Width, nameof (view.Width));
                ThrowInvalid (view, view.X, nameof (view.X));
            }

            if (heightAuto is { } && heightAuto.Style.FastHasFlags (DimAutoStyle.Content) && ContentSizeTracksViewport)
            {
                ThrowInvalid (view, view.Height, nameof (view.Height));
                ThrowInvalid (view, view.Y, nameof (view.Y));
            }
        }

        return;

        void ThrowInvalid (View view, object? checkPosDim, string name)
        {
            object? bad = null;

            switch (checkPosDim)
            {
                case Pos pos and PosAnchorEnd:
                    break;

                case Pos pos and not PosAbsolute and not PosView and not PosCombine:
                    bad = pos;

                    break;

                case Pos pos and PosCombine:
                    // Recursively check for not Absolute or not View
                    ThrowInvalid (view, (pos as PosCombine)?.Left, name);
                    ThrowInvalid (view, (pos as PosCombine)?.Right, name);

                    break;

                case Dim dim and DimAuto:
                    break;

                case Dim dim and DimFill:
                    break;

                case Dim dim and not DimAbsolute and not DimView and not DimCombine:
                    bad = dim;

                    break;

                case Dim dim and DimCombine:
                    // Recursively check for not Absolute or not View
                    ThrowInvalid (view, (dim as DimCombine)?.Left, name);
                    ThrowInvalid (view, (dim as DimCombine)?.Right, name);

                    break;
            }

            if (bad != null)
            {
                throw new InvalidOperationException (
                                                     $"{view.GetType ().Name}.{name} = {bad.GetType ().Name} "
                                                     + $"which depends on the SuperView's dimensions and the SuperView uses Dim.Auto."
                                                    );
            }
        }
    }

    #endregion Layout Engine
}
