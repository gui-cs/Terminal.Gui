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
    ///     <see cref="Application.Top"/> the position will be bound by  <see cref="Application.Screen"/>.
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
            maxDimension = Application.Screen.Width;
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
            maxDimension = statusVisible ? Application.Screen.Height - 1 : Application.Screen.Height;
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
    ///         cref="LayoutSubviews"/>) cause <see cref="Layout"/> and
    ///         <see cref="OnDrawContent(Rectangle)"/>
    ///         methods to be called.
    ///     </para>
    /// </remarks>
    public Rectangle Frame
    {
        get
        {
            if (_layoutNeeded)
            {
                //Debug.WriteLine("Frame_get with _layoutNeeded");
            }
            return _frame;
        }
        set
        {
            // This will set _frame, call SetsNeedsLayout, and raise OnViewportChanged/ViewportChanged
            if (SetFrame (value with { Width = Math.Max (value.Width, 0), Height = Math.Max (value.Height, 0) }))
            {
                // If Frame gets set, set all Pos/Dim to Absolute values.
                _x = _frame.X;
                _y = _frame.Y;
                _width = _frame.Width;
                _height = _frame.Height;

                // Implicit layout is ok here because we are setting the Frame directly.
                Layout ();
            }
        }
    }

    /// <summary>
    ///     INTERNAL API - Sets _frame, calls SetsNeedsLayout, and raises OnViewportChanged/ViewportChanged
    /// </summary>
    /// <param name="frame"></param>
    /// <returns><see langword="true"/> if the frame was changed.</returns>
    private bool SetFrame (in Rectangle frame)
    {
        if (_frame == frame)
        {
            return false;
        }

        var oldViewport = Rectangle.Empty;

        if (IsInitialized)
        {
            oldViewport = Viewport;
        }

        // This is the only place where _frame should be set directly. Use Frame = or SetFrame instead.
        _frame = frame;

        SetAdornmentFrames ();

        SetNeedsDisplay ();
        SetLayoutNeeded ();

        // BUGBUG: When SetFrame is called from Frame_set, this event gets raised BEFORE OnResizeNeeded. Is that OK?
        OnViewportChanged (new (IsInitialized ? Viewport : Rectangle.Empty, oldViewport));

        return true;
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
    ///         <see cref="Layout"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
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

            SetLayoutNeeded ();

            if (IsAbsoluteLayout())
            {
                // Implicit layout is ok here because all Pos/Dim are Absolute values.
                Layout ();
            }
        }
    }

    private bool IsAbsoluteLayout ()
    {
        return _x is PosAbsolute && _y is PosAbsolute && _width is DimAbsolute && _height is DimAbsolute;
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
    ///         <see cref="Layout"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
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

            SetLayoutNeeded ();

            if (IsAbsoluteLayout ())
            {
                // Implicit layout is ok here because all Pos/Dim are Absolute values.
                Layout ();
            }
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
    ///         <see cref="Layout"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
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

            SetLayoutNeeded ();

            if (IsAbsoluteLayout ())
            {
                // Implicit layout is ok here because all Pos/Dim are Absolute values.
                Layout ();
            }
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
    ///         <see cref="Layout"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
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

            SetLayoutNeeded ();

            if (IsAbsoluteLayout ())
            {
                // Implicit layout is ok here because all Pos/Dim are Absolute values.
                Layout ();
            }
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
    ///         other subviews, <see cref="Layout"/> on
    ///         will be called for that subview.
    ///     </para>
    ///     <para>
    ///         Some subviews may have SetRelativeLayout called on them as a side effect, particularly in DimAuto scenarios.
    ///     </para>
    /// </remarks>
    /// <param name="superviewContentSize">
    ///     The size of the SuperView's content (nominally the same as <c>this.SuperView.GetContentSize ()</c>).
    /// </param>
    /// <returns><see langword="true"/> if successful. <see langword="false"/> means a dependent View still needs layout.</returns>
    public bool SetRelativeLayout (Size superviewContentSize)
    {
        Debug.Assert (_x is { });
        Debug.Assert (_y is { });
        Debug.Assert (_width is { });
        Debug.Assert (_height is { });

        CheckDimAuto ();
        SetTextFormatterSize ();
        int newX, newW, newY, newH;

        try
        {
            // Calculate the new X, Y, Width, and Height
            // If the Width or Height is Dim.Auto, calculate the Width or Height first. Otherwise, calculate the X or Y first.
            if (_width.Has<DimAuto> (out _))
            {
                newW = _width.Calculate (0, superviewContentSize.Width, this, Dimension.Width);
                newX = _x.Calculate (superviewContentSize.Width, newW, this, Dimension.Width);
                if (newW != Frame.Width)
                {
                    // Pos.Calculate gave us a new position. We need to redo dimension
                    newW = _width.Calculate (newX, superviewContentSize.Width, this, Dimension.Width);
                }
            }
            else
            {
                newX = _x.Calculate (superviewContentSize.Width, _width, this, Dimension.Width);
                newW = _width.Calculate (newX, superviewContentSize.Width, this, Dimension.Width);
            }

            if (_height.Has<DimAuto> (out _))
            {
                newH = _height.Calculate (0, superviewContentSize.Height, this, Dimension.Height);
                newY = _y.Calculate (superviewContentSize.Height, newH, this, Dimension.Height);
                if (newH != Frame.Height)
                {
                    // Pos.Calculate gave us a new position. We need to redo dimension
                    newH = _height.Calculate (newY, superviewContentSize.Height, this, Dimension.Height);
                }
            }
            else
            {
                newY = _y.Calculate (superviewContentSize.Height, _height, this, Dimension.Height);
                newH = _height.Calculate (newY, superviewContentSize.Height, this, Dimension.Height);
            }

        }
        catch (Exception _)
        {
            // A Dim/PosFunc threw indicating it could not calculate (typically because a dependent View was not laid out).
            return false;
        }

        Rectangle newFrame = new (newX, newY, newW, newH);

        if (Frame != newFrame)
        {
            // Set the frame. Do NOT use `Frame` as it overwrites X, Y, Width, and Height
            // This will set _frame, call SetsNeedsLayout, and raise OnViewportChanged/ViewportChanged
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

            SuperView?.SetNeedsDisplay ();
        }

        if (TextFormatter.ConstrainToWidth is null)
        {
            TextFormatter.ConstrainToWidth = GetContentSize ().Width;
        }

        if (TextFormatter.ConstrainToHeight is null)
        {
            TextFormatter.ConstrainToHeight = GetContentSize ().Height;
        }

        return true;
    }

    /// <summary>
    ///    INTERNAL API - Causes the view's subviews and adornments to be laid out within the view's content areas. Assumes the view's relative layout has been set via <see cref="SetRelativeLayout"/>.
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
    internal void LayoutSubviews ()
    {
        if (!IsInitialized)
        {
            Debug.WriteLine ($"WARNING: LayoutSubviews called before view has been initialized. This is likely a bug in {this}");
        }

        if (!IsLayoutNeeded ())
        {
            return;
        }

        CheckDimAuto ();

        Size contentSize = GetContentSize ();
        OnLayoutStarted (new (contentSize));

        // The Adornments already have their Frame's set by SetRelativeLayout so we call LayoutSubViews vs. Layout here.
        Margin?.LayoutSubviews ();
        Border?.LayoutSubviews ();
        Padding?.LayoutSubviews ();

        // Sort out the dependencies of the X, Y, Width, Height properties
        HashSet<View> nodes = new ();
        HashSet<(View, View)> edges = new ();
        CollectAll (this, ref nodes, ref edges);
        List<View> ordered = TopologicalSort (SuperView!, nodes, edges);

        List<View> redo = new ();
        foreach (View v in ordered)
        {
            if (!v.Layout (contentSize))
            {
                redo.Add (v);
            }
        }

        bool layoutStillNeeded = false;
        foreach (View v in redo)
        {
            if (!v.Layout (contentSize))
            {
                layoutStillNeeded = true;
            }
        }

        // If the 'to' is rooted to 'from' it's a special-case.
        // Use Layout with the ContentSize of the 'from'.
        // See the Nested_SubViews_Ref_Topmost_SuperView unit test
        if (edges.Count > 0 && GetTopSuperView () is { })
        {
            foreach ((View from, View to) in edges)
            {
                // QUESTION: Do we test this with adornments well enough?
                to.Layout (from.GetContentSize ());
            }
        }

        _layoutNeeded = layoutStillNeeded;

        OnLayoutComplete (new (contentSize));
    }

    /// <summary>
    ///     Performs layout of the view and its subviews within the specified content size.
    /// </summary>
    /// <param name="contentSize"></param>
    /// <returns><see langword="false"/>If the view could not be laid out (typically because a dependencies was not ready). </returns>
    public bool Layout (Size contentSize)
    {
        // Note, SetRelativeLayout calls SetTextFormatterSize
        if (SetRelativeLayout (contentSize))
        {
            LayoutSubviews ();

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Performs layout of the view and its subviews using the content size of either the <see cref="SuperView"/> or <see cref="Application.Screen"/>.
    /// </summary>
    /// <returns><see langword="false"/>If the view could not be laid out (typically because dependency was not ready). </returns>
    public bool Layout ()
    {
        return Layout (GetBestGuessSuperViewContentSize ());
    }

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


    // We expose no setter for this to ensure that the ONLY place it's changed is in SetNeedsLayout
    private bool _layoutNeeded = false;

    /// <summary>
    ///     Indicates the View's Frame or the layout of the View's subviews (including Adornments) have
    ///     changed since the last time the View was laid out. 
    /// </summary>
    /// <remarks>
    /// <para>Used to prevent <see cref="Layout()"/> from needlessly computing
    ///     layout.
    /// </para>
    /// </remarks>
    /// <returns><see langword="true"/> if layout is needed.</returns>
    public bool IsLayoutNeeded () { return _layoutNeeded; }

    /// <summary>
    ///     Sets <see cref="IsLayoutNeeded"/> to return <see langword="true"/>, indicating this View and all of it's subviews (including adornments) need to be laid out in the next Application iteration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="MainLoop"/> will cause <see cref="Layout()"/> to be called on the next <see cref="Application.Iteration"/> so there is normally no reason to call see <see cref="Layout()"/>.
    ///     </para>
    /// </remarks>

    public void SetLayoutNeeded ()
    {
        if (IsLayoutNeeded ())
        {
            // Prevent infinite recursion
            return;
        }

        _layoutNeeded = true;

        Margin?.SetLayoutNeeded ();
        Border?.SetLayoutNeeded ();
        Padding?.SetLayoutNeeded ();

        // Use a stack to avoid recursion
        Stack<View> stack = new Stack<View> (Subviews);

        while (stack.Count > 0)
        {
            View current = stack.Pop ();
            if (!current.IsLayoutNeeded ())
            {
                current._layoutNeeded = true;
                current.Margin?.SetLayoutNeeded ();
                current.Border?.SetLayoutNeeded ();
                current.Padding?.SetLayoutNeeded ();

                foreach (View subview in current.Subviews)
                {
                    stack.Push (subview);
                }
            }
        }

        TextFormatter.NeedsFormat = true;

        SuperView?.SetLayoutNeeded ();

        if (this is Adornment adornment)
        {
            adornment.Parent?.SetLayoutNeeded ();
        }
    }

    /// <summary>
    ///    INTERNAL API FOR UNIT TESTS - Gets the size of the SuperView's content (nominally the same as
    ///    the SuperView's <see cref="GetContentSize ()"/>).
    /// </summary>
    /// <returns></returns>
    private Size GetBestGuessSuperViewContentSize ()
    {
        Size superViewContentSize = SuperView?.GetContentSize () ??
                                    (Application.Top is { } && Application.Top != this && Application.Top.IsInitialized
                                         ? Application.Top.GetContentSize ()
                                         : Application.Screen.Size);

        return superViewContentSize;
    }

    /// <summary>
    ///     INTERNAL API - Collects all views and their dependencies from a given starting view for layout purposes. Used by
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
    ///      INTERNAL API - Collects dimension (where Width or Height is `DimView`) dependencies for a given view.
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
        if (dim!.Has<DimView> (out DimView dv))
        {
            if (dv.Target != this)
            {
                nEdges.Add ((dv.Target!, from));
            }
        }

        if (dim!.Has<DimCombine> (out DimCombine dc))
        {
            CollectDim (dc.Left, from, ref nNodes, ref nEdges);
            CollectDim (dc.Right, from, ref nNodes, ref nEdges);
        }
    }

    /// <summary>
    ///     INTERNAL API - Collects position (where X or Y is `PosView`) dependencies for a given view.
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
        // TODO: Use Pos.Has<T> instead.
        switch (pos)
        {
            case PosView pv:
                Debug.Assert (pv.Target is { });
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
