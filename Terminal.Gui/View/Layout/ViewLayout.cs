using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     <para>Indicates the LayoutStyle for the <see cref="View"/>.</para>
///     <para>
///         If Absolute, the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and
///         <see cref="View.Height"/> objects are all absolute values and are not relative. The position and size of the
///         view is described by <see cref="View.Frame"/>.
///     </para>
///     <para>
///         If Computed, one or more of the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, or
///         <see cref="View.Height"/> objects are relative to the <see cref="View.SuperView"/> and are computed at layout
///         time.
///     </para>
/// </summary>
public enum LayoutStyle
{
    /// <summary>
    ///     Indicates the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and
    ///     <see cref="View.Height"/> objects are all absolute values and are not relative. The position and size of the view
    ///     is described by <see cref="View.Frame"/>.
    /// </summary>
    Absolute,

    /// <summary>
    ///     Indicates one or more of the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, or
    ///     <see cref="View.Height"/>
    ///     objects are relative to the <see cref="View.SuperView"/> and are computed at layout time.  The position and size of
    ///     the
    ///     view
    ///     will be computed based on these objects at layout time. <see cref="View.Frame"/> will provide the absolute computed
    ///     values.
    /// </summary>
    Computed
}

public partial class View
{
    #region Frame

    private Rectangle _frame;

    /// <summary>Gets or sets the absolute location and dimension of the view.</summary>
    /// <value>
    ///     The rectangle describing absolute location and dimension of the view, in coordinates relative to the
    ///     <see cref="SuperView"/>'s Content, which is bound by <see cref="ContentSize"/>.
    /// </value>
    /// <remarks>
    ///     <para>Frame is relative to the <see cref="SuperView"/>'s Content, which is bound by <see cref="ContentSize"/>.</para>
    ///     <para>
    ///         Setting Frame will set <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> to the
    ///         values of the corresponding properties of the <paramref name="value"/> parameter.
    ///         This causes <see cref="LayoutStyle"/> to be <see cref="LayoutStyle.Absolute"/>.
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

            // If Frame gets set, by definition, the View is now LayoutStyle.Absolute, so
            // set all Pos/Dim to Absolute values.
            _x = _frame.X;
            _y = _frame.Y;
            _width = _frame.Width;
            _height = _frame.Height;

            // TODO: Figure out if the below can be optimized.
            if (IsInitialized)
            {
                OnResizeNeeded ();
            }
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

        if (!TextFormatter.AutoSize)
        {
            TextFormatter.Size = ContentSize.GetValueOrDefault ();
        }
    }

    /// <summary>Gets the <see cref="Frame"/> with a screen-relative location.</summary>
    /// <returns>The location and size of the view in screen-relative coordinates.</returns>
    public virtual Rectangle FrameToScreen ()
    {
        Rectangle screen = Frame;
        View current = SuperView;

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
        superViewViewportOffset.Offset(-SuperView.Viewport.X, -SuperView.Viewport.Y);

        Point frame = location;
        frame.Offset(-superViewViewportOffset.X, -superViewViewportOffset.Y);

        frame = SuperView.ScreenToFrame (frame);
        frame.Offset (-Frame.X, -Frame.Y);

        return frame;
    }

    private Pos _x = Pos.At (0);

    /// <summary>Gets or sets the X position for the view (the column).</summary>
    /// <value>The <see cref="Pos"/> object representing the X position.</value>
    /// <remarks>
    ///     <para>
    ///         The position is relative to the <see cref="SuperView"/>'s Content, which is bound by <see cref="ContentSize"/>.
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
    ///         Changing this property will cause <see cref="Frame"/> to be updated. If the new value is not of type
    ///         <see cref="Pos.PosAbsolute"/> the <see cref="LayoutStyle"/> will change to <see cref="LayoutStyle.Computed"/>.
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

    private Pos _y = Pos.At (0);

    /// <summary>Gets or sets the Y position for the view (the row).</summary>
    /// <value>The <see cref="Pos"/> object representing the Y position.</value>
    /// <remarks>
    ///     <para>
    ///         The position is relative to the <see cref="SuperView"/>'s Content, which is bound by <see cref="ContentSize"/>.
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
    ///         Changing this property will cause <see cref="Frame"/> to be updated. If the new value is not of type
    ///         <see cref="Pos.PosAbsolute"/> the <see cref="LayoutStyle"/> will change to <see cref="LayoutStyle.Computed"/>.
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

    private Dim _height = Dim.Sized (0);

    /// <summary>Gets or sets the height dimension of the view.</summary>
    /// <value>The <see cref="Dim"/> object representing the height of the view (the number of rows).</value>
    /// <remarks>
    ///     <para>
    ///         The dimension is relative to the <see cref="SuperView"/>'s Content, which is bound by <see cref="ContentSize"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Dim.Fill(int)"/>) the value is indeterminate until the view has
    ///         been initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Size)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated. If the new value is not of type
    ///         <see cref="Dim.DimAbsolute"/> the <see cref="LayoutStyle"/> will change to <see cref="LayoutStyle.Computed"/>.
    ///     </para>
    ///     <para>The default value is <c>Dim.Sized (0)</c>.</para>
    /// </remarks>
    public Dim Height
    {
        get => VerifyIsInitialized (_height, nameof (Height));
        set
        {
            if (Equals (_height, value))
            {
                return;
            }

            if (_height is Dim.DimAuto)
            {
                // Reset ContentSize to Viewport
                _contentSize = null;
            }

            _height = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (Height)} cannot be null");

            OnResizeNeeded ();
        }
    }

    private Dim _width = Dim.Sized (0);

    /// <summary>Gets or sets the width dimension of the view.</summary>
    /// <value>The <see cref="Dim"/> object representing the width of the view (the number of columns).</value>
    /// <remarks>
    ///     <para>
    ///         The dimension is relative to the <see cref="SuperView"/>'s Content, which is bound by <see cref="ContentSize"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Dim.Fill(int)"/>) the value is indeterminate until the view has
    ///         been initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Size)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated. If the new value is not of type
    ///         <see cref="Dim.DimAbsolute"/> the <see cref="LayoutStyle"/> will change to <see cref="LayoutStyle.Computed"/>.
    ///     </para>
    ///     <para>The default value is <c>Dim.Sized (0)</c>.</para>
    /// </remarks>
    public Dim Width
    {
        get => VerifyIsInitialized (_width, nameof (Width));
        set
        {
            if (Equals (_width, value))
            {
                return;
            }

            if (_width is Dim.DimAuto)
            {
                // Reset ContentSize to Viewport
                _contentSize = null;
            }

            _width = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (Width)} cannot be null");

            OnResizeNeeded ();
        }
    }

    #endregion Frame

    #region Layout Engine


    // @tig Notes on layout flow. Ignore for now.
    // BeginLayout
    //   If !LayoutNeeded return
    //   If !SizeNeeded return
    //   Call OnLayoutStarted
    //      Views and subviews can update things
    //   


    // EndLayout

    /// <summary>
    ///     Controls how the View's <see cref="Frame"/> is computed during <see cref="LayoutSubviews"/>. If the style is
    ///     set to <see cref="LayoutStyle.Absolute"/>, LayoutSubviews does not change the <see cref="Frame"/>. If the style is
    ///     <see cref="LayoutStyle.Computed"/> the <see cref="Frame"/> is updated using the <see cref="X"/>, <see cref="Y"/>,
    ///     <see cref="Width"/>, and <see cref="Height"/> properties.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property to <see cref="LayoutStyle.Absolute"/> will cause <see cref="Frame"/> to determine the
    ///         size and position of the view. <see cref="X"/> and <see cref="Y"/> will be set to <see cref="Dim.DimAbsolute"/>
    ///         using <see cref="Frame"/>.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="LayoutStyle.Computed"/> will cause the view to use the
    ///         <see cref="LayoutSubviews"/> method to size and position of the view. If either of the <see cref="X"/> and
    ///         <see cref="Y"/> properties are `null` they will be set to <see cref="Pos.PosAbsolute"/> using the current value
    ///         of <see cref="Frame"/>. If either of the <see cref="Width"/> and <see cref="Height"/> properties are `null`
    ///         they will be set to <see cref="Dim.DimAbsolute"/> using <see cref="Frame"/>.
    ///     </para>
    /// </remarks>
    /// <value>The layout style.</value>
    public LayoutStyle LayoutStyle
    {
        get
        {
            if (_x is Pos.PosAbsolute
                && _y is Pos.PosAbsolute
                && _width is Dim.DimAbsolute
                && _height is Dim.DimAbsolute)
            {
                return LayoutStyle.Absolute;
            }

            return LayoutStyle.Computed;
        }
    }

    #endregion Layout Engine

    /// <summary>
    ///     Indicates whether the specified SuperView-relative coordinates are within the View's <see cref="Frame"/>.
    /// </summary>
    /// <param name="x">SuperView-relative X coordinate.</param>
    /// <param name="y">SuperView-relative Y coordinate.</param>
    /// <returns><see langword="true"/> if the specified SuperView-relative coordinates are within the View.</returns>
    public virtual bool Contains (int x, int y) { return Frame.Contains (x, y); }

#nullable enable
    /// <summary>Finds the first Subview of <paramref name="start"/> that is visible at the provided location.</summary>
    /// <remarks>
    ///     <para>
    ///         Used to determine what view the mouse is over.
    ///     </para>
    /// </remarks>
    /// <param name="start">The view to scope the search by.</param>
    /// <param name="x"><paramref name="start"/>.SuperView-relative X coordinate.</param>
    /// <param name="y"><paramref name="start"/>.SuperView-relative Y coordinate.</param>
    /// <returns>
    ///     The view that was found at the <paramref name="x"/> and <paramref name="y"/> coordinates.
    ///     <see langword="null"/> if no view was found.
    /// </returns>

    // CONCURRENCY: This method is not thread-safe. Undefined behavior and likely program crashes are exposed by unsynchronized access to InternalSubviews.
    internal static View? FindDeepestView (View? start, int x, int y)
    {
        while (start is { Visible: true } && start.Contains (x, y))
        {
            Adornment? found = null;

            if (start.Margin.Contains (x, y))
            {
                found = start.Margin;
            }
            else if (start.Border.Contains (x, y))
            {
                found = start.Border;
            }
            else if (start.Padding.Contains (x, y))
            {
                found = start.Padding;
            }

            Point viewportOffset = start.GetViewportOffsetFromFrame ();

            if (found is { })
            {
                start = found;
                viewportOffset = found.Parent.Frame.Location;
            }

            int startOffsetX = x - (start.Frame.X + viewportOffset.X);
            int startOffsetY = y - (start.Frame.Y + viewportOffset.Y);

            View? subview = null;

            for (int i = start.InternalSubviews.Count - 1; i >= 0; i--)
            {
                if (start.InternalSubviews [i].Visible
                    && start.InternalSubviews [i].Contains (startOffsetX + start.Viewport.X, startOffsetY + start.Viewport.Y))
                {
                    subview = start.InternalSubviews [i];
                    x = startOffsetX + start.Viewport.X;
                    y = startOffsetY + start.Viewport.Y;

                    // start is the deepest subview under the mouse; stop searching the subviews
                    break;
                }
            }

            if (subview is null)
            {
                // No subview was found that's under the mouse, so we're done
                return start;
            }

            // We found a subview of start that's under the mouse, continue...
            start = subview;
        }

        return null;
    }

#nullable restore

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
    /// <param name="statusBar">The new top most statusBar</param>
    /// <returns>
    ///     Either <see cref="Application.Top"/> (if <paramref name="viewToMove"/> does not have a Super View) or
    ///     <paramref name="viewToMove"/>'s SuperView. This can be used to ensure LayoutSubviews is called on the correct View.
    /// </returns>
    internal static View GetLocationEnsuringFullVisibility (
        View viewToMove,
        int targetX,
        int targetY,
        out int nx,
        out int ny,
        out StatusBar statusBar
    )
    {
        int maxDimension;
        View superView;
        statusBar = null;

        if (viewToMove?.SuperView is null || viewToMove == Application.Top || viewToMove?.SuperView == Application.Top)
        {
            maxDimension = Driver.Cols;
            superView = Application.Top;
        }
        else
        {
            // Use the SuperView's Viewport, not Frame
            maxDimension = viewToMove.SuperView.Viewport.Width;
            superView = viewToMove.SuperView;
        }

        if (superView?.Margin is { } && superView == viewToMove.SuperView)
        {
            maxDimension -= superView.GetAdornmentsThickness ().Left + superView.GetAdornmentsThickness ().Right;
        }

        if (viewToMove.Frame.Width <= maxDimension)
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
            View t = viewToMove.SuperView;

            while (t is { } and not Toplevel)
            {
                t = t.SuperView;
            }

            if (t is Toplevel toplevel)
            {
                menuVisible = toplevel.MenuBar?.Visible == true;
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

        if (viewToMove?.SuperView is null || viewToMove == Application.Top || viewToMove?.SuperView == Application.Top)
        {
            statusVisible = Application.Top?.StatusBar?.Visible == true;
            statusBar = Application.Top?.StatusBar;
        }
        else
        {
            View t = viewToMove.SuperView;

            while (t is { } and not Toplevel)
            {
                t = t.SuperView;
            }

            if (t is Toplevel toplevel)
            {
                statusVisible = toplevel.StatusBar?.Visible == true;
                statusBar = toplevel.StatusBar;
            }
        }

        if (viewToMove?.SuperView is null || viewToMove == Application.Top || viewToMove?.SuperView == Application.Top)
        {
            maxDimension = statusVisible ? Driver.Rows - 1 : Driver.Rows;
        }
        else
        {
            maxDimension = statusVisible ? viewToMove.SuperView.Viewport.Height - 1 : viewToMove.SuperView.Viewport.Height;
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

        return superView;
    }

    /// <summary>Fired after the View's <see cref="LayoutSubviews"/> method has completed.</summary>
    /// <remarks>
    ///     Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has
    ///     otherwise changed.
    /// </remarks>
    public event EventHandler<LayoutEventArgs> LayoutComplete;

    /// <summary>Fired after the View's <see cref="LayoutSubviews"/> method has completed.</summary>
    /// <remarks>
    ///     Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has
    ///     otherwise changed.
    /// </remarks>
    public event EventHandler<LayoutEventArgs> LayoutStarted;

    /// <summary>
    ///     Invoked when a view starts executing or when the dimensions of the view have changed, for example in response to
    ///     the container view or terminal resizing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The position and dimensions of the view are indeterminate until the view has been initialized. Therefore, the
    ///         behavior of this method is indeterminate if <see cref="IsInitialized"/> is <see langword="false"/>.
    ///     </para>
    ///     <para>Raises the <see cref="LayoutComplete"/> event) before it returns.</para>
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

        var contentSize = ContentSize.GetValueOrDefault ();
        OnLayoutStarted (new (contentSize));

        LayoutAdornments ();

        SetTextFormatterSize ();

        // Sort out the dependencies of the X, Y, Width, Height properties
        HashSet<View> nodes = new ();
        HashSet<(View, View)> edges = new ();
        CollectAll (this, ref nodes, ref edges);
        List<View> ordered = TopologicalSort (SuperView, nodes, edges);

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
                LayoutSubview (to, from.ContentSize.GetValueOrDefault ());
            }
        }

        LayoutNeeded = false;

        OnLayoutComplete (new (contentSize));
    }

    private void LayoutSubview (View v, Size contentSize)
    {
        // BUGBUG: Calling SetRelativeLayout before LayoutSubviews is problematic. Need to resolve.
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

    // BUGBUG: We need an API/event that is called from SetRelativeLayout instead of/in addition to 
    // BUGBUG: OnLayoutStarted which is called from LayoutSubviews.

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

        // First try SuperView.Viewport, then Application.Top, then Driver.Viewport.
        // Finally, if none of those are valid, use int.MaxValue (for Unit tests).
        Size? contentSize = SuperView is { IsInitialized: true } ? SuperView.ContentSize :
                           Application.Top is { } && Application.Top != this && Application.Top.IsInitialized ? Application.Top.ContentSize :
                           Application.Driver?.Screen.Size ?? new (int.MaxValue, int.MaxValue);

        SetTextFormatterSize ();

        SetRelativeLayout (contentSize.GetValueOrDefault ());

        if (IsInitialized)
        {
            LayoutAdornments ();
        }

        SetNeedsDisplay ();
        SetNeedsLayout ();
    }

    internal bool LayoutNeeded { get; private set; } = true;

    /// <summary>
    ///     Sets the internal <see cref="LayoutNeeded"/> flag for this View and all of it's subviews and it's SuperView.
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
    ///     Adjusts <see cref="Frame"/> given the SuperView's ContentSize (nominally the same as
    ///     <c>this.SuperView.ContentSize</c>)
    ///     and the position (<see cref="X"/>, <see cref="Y"/>) and dimension (<see cref="Width"/>, and
    ///     <see cref="Height"/>).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, or <see cref="Height"/> are
    ///         absolute, they will be updated to reflect the new size and position of the view. Otherwise, they
    ///         are left unchanged.
    ///     </para>
    /// </remarks>
    /// <param name="superviewContentSize">
    ///     The size of the SuperView's content (nominally the same as <c>this.SuperView.ContentSize</c>).
    /// </param>
    internal void SetRelativeLayout (Size? superviewContentSize)
    {
        Debug.Assert (_x is { });
        Debug.Assert (_y is { });
        Debug.Assert (_width is { });
        Debug.Assert (_height is { });

        if (superviewContentSize is null)
        {
            return;
        }

        CheckDimAuto ();
        int newX = _x.Calculate (superviewContentSize.Value.Width, _width, this, Dim.Dimension.Width);
        int newW = _width.Calculate (newX, superviewContentSize.Value.Width, this, Dim.Dimension.Width);
        int newY = _y.Calculate (superviewContentSize.Value.Height, _height, this, Dim.Dimension.Height);
        int newH = _height.Calculate (newY, superviewContentSize.Value.Height, this, Dim.Dimension.Height);

        Rectangle newFrame = new (newX, newY, newW, newH);

        if (Frame != newFrame)
        {
            // Set the frame. Do NOT use `Frame` as it overwrites X, Y, Width, and Height, making
            // the view LayoutStyle.Absolute.
            SetFrame (newFrame);

            if (_x is Pos.PosAbsolute)
            {
                _x = Frame.X;
            }

            if (_y is Pos.PosAbsolute)
            {
                _y = Frame.Y;
            }

            if (_width is Dim.DimAbsolute)
            {
                _width = Frame.Width;
            }

            if (_height is Dim.DimAbsolute)
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
    }

    internal void CollectAll (View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        // BUGBUG: This should really only work on initialized subviews
        foreach (View v in from.InternalSubviews /*.Where(v => v.IsInitialized)*/)
        {
            nNodes.Add (v);

            if (v.LayoutStyle != LayoutStyle.Computed)
            {
                continue;
            }

            CollectPos (v.X, v, ref nNodes, ref nEdges);
            CollectPos (v.Y, v, ref nNodes, ref nEdges);
            CollectDim (v.Width, v, ref nNodes, ref nEdges);
            CollectDim (v.Height, v, ref nNodes, ref nEdges);
        }
    }

    internal void CollectDim (Dim dim, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        switch (dim)
        {
            case Dim.DimView dv:
                // See #2461
                //if (!from.InternalSubviews.Contains (dv.Target)) {
                //	throw new InvalidOperationException ($"View {dv.Target} is not a subview of {from}");
                //}
                if (dv.Target != this)
                {
                    nEdges.Add ((dv.Target, from));
                }

                return;
            case Dim.DimCombine dc:
                CollectDim (dc._left, from, ref nNodes, ref nEdges);
                CollectDim (dc._right, from, ref nNodes, ref nEdges);

                break;
        }
    }

    internal void CollectPos (Pos pos, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        switch (pos)
        {
            case Pos.PosView pv:
                // See #2461
                //if (!from.InternalSubviews.Contains (pv.Target)) {
                //	throw new InvalidOperationException ($"View {pv.Target} is not a subview of {from}");
                //}
                if (pv.Target != this)
                {
                    nEdges.Add ((pv.Target, from));
                }

                return;
            case Pos.PosCombine pc:
                CollectPos (pc._left, from, ref nNodes, ref nEdges);
                CollectPos (pc._right, from, ref nNodes, ref nEdges);

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
#if DEBUG
        if ((pos.ReferencesOtherViews () || pos.ReferencesOtherViews ()) && !IsInitialized)
        {
            Debug.WriteLine (
                             $"WARNING: The {pos} of {this} is dependent on other views and {member} "
                             + $"is being accessed before the View has been initialized. This is likely a bug."
                            );
        }
#endif // DEBUG
        return pos;
    }

    // Diagnostics to highlight when Width or Height is read before the view has been initialized
    private Dim VerifyIsInitialized (Dim dim, string member)
    {
#if DEBUG
        if ((dim.ReferencesOtherViews () || dim.ReferencesOtherViews ()) && !IsInitialized)
        {
            Debug.WriteLine (
                             $"WARNING: The {member} of {this} is dependent on other views and is "
                             + $"is being accessed before the View has been initialized. This is likely a bug. "
                             + $"{member} is {dim}"
                            );
        }
#endif // DEBUG
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
        if (!ValidatePosDim || !IsInitialized || (Width is not Dim.DimAuto && Height is not Dim.DimAuto))
        {
            return;
        }

        // Verify none of the subviews are using Dim objects that depend on the SuperView's dimensions.
        foreach (View view in Subviews)
        {
            if (Width is Dim.DimAuto { _min: null })
            {
                ThrowInvalid (view, view.Width, nameof (view.Width));
                ThrowInvalid (view, view.X, nameof (view.X));
            }

            if (Height is Dim.DimAuto { _min: null })
            {
                ThrowInvalid (view, view.Height, nameof (view.Height));
                ThrowInvalid (view, view.Y, nameof (view.Y));
            }
        }

        return;

        void ThrowInvalid (View view, object checkPosDim, string name)
        {
            object bad = null;

            switch (checkPosDim)
            {
                case Pos pos and not Pos.PosAbsolute and not Pos.PosView and not Pos.PosCombine:
                    bad = pos;

                    break;

                case Pos pos and Pos.PosCombine:
                    // Recursively check for not Absolute or not View
                    ThrowInvalid (view, (pos as Pos.PosCombine)._left, name);
                    ThrowInvalid (view, (pos as Pos.PosCombine)._right, name);

                    break;

                case Dim dim and not Dim.DimAbsolute and not Dim.DimView and not Dim.DimCombine:
                    bad = dim;

                    break;

                case Dim dim and Dim.DimCombine:
                    // Recursively check for not Absolute or not View
                    ThrowInvalid (view, (dim as Dim.DimCombine)._left, name);
                    ThrowInvalid (view, (dim as Dim.DimCombine)._right, name);

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
}
