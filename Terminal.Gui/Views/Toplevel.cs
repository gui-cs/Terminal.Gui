#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Toplevel views are used for both an application's main view (filling the entire screen and for modal (pop-up)
///     views such as <see cref="Dialog"/>, <see cref="MessageBox"/>, and <see cref="Wizard"/>).
/// </summary>
/// <remarks>
///     <para>
///         Toplevel views can run as modal (popup) views, started by calling
///         <see cref="Application.Run(Toplevel, Func{Exception, bool})"/>. They return control to the caller when
///         <see cref="Application.RequestStop(Toplevel)"/> has been called (which sets the <see cref="Toplevel.Running"/>
///         property to <c>false</c>).
///     </para>
///     <para>
///         A Toplevel is created when an application initializes Terminal.Gui by calling <see cref="Application.Init"/>.
///         The application Toplevel can be accessed via <see cref="Application.Top"/>. Additional Toplevels can be created
///         and run (e.g. <see cref="Dialog"/>s). To run a Toplevel, create the <see cref="Toplevel"/> and call
///         <see cref="Application.Run(Toplevel, Func{Exception, bool})"/>.
///     </para>
/// </remarks>
public partial class Toplevel : View
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Toplevel"/> class,
    ///     defaulting to full screen. The <see cref="View.Width"/> and <see cref="View.Height"/> properties will be set to the
    ///     dimensions of the terminal using <see cref="Dim.Fill"/>.
    /// </summary>
    public Toplevel ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        Arrangement = ViewArrangement.Fixed;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        ColorScheme = Colors.ColorSchemes ["TopLevel"];
        MouseClick += Toplevel_MouseClick;
    }

    #region Keyboard & Mouse

    // TODO: IRunnable: Re-implement - Modal means IRunnable, ViewArrangement.Overlapped where modalView.Z > allOtherViews.Max (v = v.Z), and exclusive key/mouse input.
    /// <summary>
    ///     Determines whether the <see cref="Toplevel"/> is modal or not. If set to <c>false</c> (the default):
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="View.OnKeyDown"/> events will propagate keys upwards.</description>
    ///         </item>
    ///         <item>
    ///             <description>The Toplevel will act as an embedded view (not a modal/pop-up).</description>
    ///         </item>
    ///     </list>
    ///     If set to <c>true</c>:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="View.OnKeyDown"/> events will NOT propagate keys upwards.</description>
    ///         </item>
    ///         <item>
    ///             <description>The Toplevel will and look like a modal (pop-up) (e.g. see <see cref="Dialog"/>.</description>
    ///         </item>
    ///     </list>
    /// </summary>
    public bool Modal { get; set; }

    private void Toplevel_MouseClick (object? sender, MouseEventEventArgs e) { e.Handled = InvokeCommand (Command.HotKey) == true; }

    #endregion

    #region Subviews

    // TODO: Deprecate - Any view can host a menubar in v2
    /// <summary>Gets or sets the menu for this Toplevel.</summary>
    public MenuBar? MenuBar { get; set; }

    // TODO: Deprecate - Any view can host a statusbar in v2
    /// <summary>Gets or sets the status bar for this Toplevel.</summary>
    public StatusBar? StatusBar { get; set; }

    /// <inheritdoc/>
    public override View Add (View view)
    {
        CanFocus = true;
        AddMenuStatusBar (view);

        return base.Add (view);
    }

    /// <inheritdoc/>
    public override View Remove (View view)
    {
        if (this is Toplevel { MenuBar: { } })
        {
            RemoveMenuStatusBar (view);
        }

        return base.Remove (view);
    }

    /// <inheritdoc/>
    public override void RemoveAll ()
    {
        if (this == Application.Top)
        {
            MenuBar?.Dispose ();
            MenuBar = null;
            StatusBar?.Dispose ();
            StatusBar = null;
        }

        base.RemoveAll ();
    }

    internal void AddMenuStatusBar (View view)
    {
        if (view is MenuBar)
        {
            MenuBar = view as MenuBar;
        }

        if (view is StatusBar)
        {
            StatusBar = view as StatusBar;
        }
    }

    internal void RemoveMenuStatusBar (View view)
    {
        if (view is MenuBar)
        {
            MenuBar?.Dispose ();
            MenuBar = null;
        }

        if (view is StatusBar)
        {
            StatusBar?.Dispose ();
            StatusBar = null;
        }
    }

    // TODO: Overlapped - Rename to AllSubviewsClosed - Move to View?
    /// <summary>
    ///     Invoked when the last child of the Toplevel <see cref="RunState"/> is closed from by
    ///     <see cref="Application.End(RunState)"/>.
    /// </summary>
    public event EventHandler? AllChildClosed;

    // TODO: Overlapped - Rename to *Subviews* - Move to View?
    /// <summary>
    ///     Invoked when a child of the Toplevel <see cref="RunState"/> is closed by
    ///     <see cref="Application.End(RunState)"/>.
    /// </summary>
    public event EventHandler<ToplevelEventArgs>? ChildClosed;

    // TODO: Overlapped - Rename to *Subviews* - Move to View?
    /// <summary>Invoked when a child Toplevel's <see cref="RunState"/> has been loaded.</summary>
    public event EventHandler<ToplevelEventArgs>? ChildLoaded;

    // TODO: Overlapped - Rename to *Subviews* - Move to View?
    /// <summary>Invoked when a cjhild Toplevel's <see cref="RunState"/> has been unloaded.</summary>
    public event EventHandler<ToplevelEventArgs>? ChildUnloaded;

    #endregion

    #region Life Cycle

    // TODO: IRunnable: Re-implement as a property on IRunnable
    /// <summary>Gets or sets whether the main loop for this <see cref="Toplevel"/> is running or not.</summary>
    /// <remarks>Setting this property directly is discouraged. Use <see cref="Application.RequestStop"/> instead.</remarks>
    public bool Running { get; set; }

    // TODO: IRunnable: Re-implement in IRunnable
    /// <summary>
    ///     <see langword="true"/> if was already loaded by the <see cref="Application.Begin(Toplevel)"/>
    ///     <see langword="false"/>, otherwise.
    /// </summary>
    public bool IsLoaded { get; private set; }

    // TODO: IRunnable: Re-implement as an event on IRunnable; IRunnable.Activating/Activate
    /// <summary>Invoked when the Toplevel <see cref="RunState"/> becomes the <see cref="Application.Current"/> Toplevel.</summary>
    public event EventHandler<ToplevelEventArgs>? Activate;

    // TODO: IRunnable: Re-implement as an event on IRunnable; IRunnable.Deactivating/Deactivate?
    /// <summary>Invoked when the Toplevel<see cref="RunState"/> ceases to be the <see cref="Application.Current"/> Toplevel.</summary>
    public event EventHandler<ToplevelEventArgs>? Deactivate;

    /// <summary>Invoked when the Toplevel's <see cref="RunState"/> is closed by <see cref="Application.End(RunState)"/>.</summary>
    public event EventHandler<ToplevelEventArgs>? Closed;

    /// <summary>
    ///     Invoked when the Toplevel's <see cref="RunState"/> is being closed by
    ///     <see cref="Application.RequestStop(Toplevel)"/>.
    /// </summary>
    public event EventHandler<ToplevelClosingEventArgs>? Closing;

    /// <summary>
    ///     Invoked when the <see cref="Toplevel"/> <see cref="RunState"/> has begun to be loaded. A Loaded event handler
    ///     is a good place to finalize initialization before calling <see cref="Application.RunLoop(RunState)"/>.
    /// </summary>
    public event EventHandler? Loaded;

    /// <summary>
    ///     Called from <see cref="Application.Begin(Toplevel)"/> before the <see cref="Toplevel"/> redraws for the first
    ///     time.
    /// </summary>
    /// <remarks>
    ///     Overrides must call base.OnLoaded() to ensure any Toplevel subviews are initialized properly and the
    ///     <see cref="Loaded"/> event is raised.
    /// </remarks>
    public virtual void OnLoaded ()
    {
        IsLoaded = true;

        foreach (var view in Subviews.Where (v => v is Toplevel))
        {
            var tl = (Toplevel)view;
            tl.OnLoaded ();
        }

        Loaded?.Invoke (this, EventArgs.Empty);
    }

    /// <summary>
    ///     Invoked when the <see cref="Toplevel"/> main loop has started it's first iteration. Subscribe to this event to
    ///     perform tasks when the <see cref="Toplevel"/> has been laid out and focus has been set. changes.
    ///     <para>
    ///         A Ready event handler is a good place to finalize initialization after calling
    ///         <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> on this <see cref="Toplevel"/>.
    ///     </para>
    /// </summary>
    public event EventHandler? Ready;

    /// <summary>
    ///     Stops and closes this <see cref="Toplevel"/>. If this Toplevel is the top-most Toplevel,
    ///     <see cref="Application.RequestStop(Toplevel)"/> will be called, causing the application to exit.
    /// </summary>
    public virtual void RequestStop ()
    {
        if (IsOverlappedContainer
            && Running
            && (Application.Current == this
                || Application.Current?.Modal == false
                || (Application.Current?.Modal == true && Application.Current?.Running == false)))
        {
            foreach (Toplevel child in ApplicationOverlapped.OverlappedChildren!)
            {
                var ev = new ToplevelClosingEventArgs (this);

                if (child.OnClosing (ev))
                {
                    return;
                }

                child.Running = false;
                Application.RequestStop (child);
            }

            Running = false;
            Application.RequestStop (this);
        }
        else if (IsOverlappedContainer && Running && Application.Current?.Modal == true && Application.Current?.Running == true)
        {
            var ev = new ToplevelClosingEventArgs (Application.Current);

            if (OnClosing (ev))
            {
                return;
            }

            Application.RequestStop (Application.Current);
        }
        else if (!IsOverlappedContainer && Running && (!Modal || (Modal && Application.Current != this)))
        {
            var ev = new ToplevelClosingEventArgs (this);

            if (OnClosing (ev))
            {
                return;
            }

            Running = false;
            Application.RequestStop (this);
        }
        else
        {
            Application.RequestStop (Application.Current);
        }
    }

    /// <summary>
    ///     Invoked when the Toplevel <see cref="RunState"/> has been unloaded. A Unloaded event handler is a good place
    ///     to dispose objects after calling <see cref="Application.End(RunState)"/>.
    /// </summary>
    public event EventHandler? Unloaded;

    internal virtual void OnActivate (Toplevel deactivated) { Activate?.Invoke (this, new (deactivated)); }

    /// <summary>
    ///     Stops and closes the <see cref="Toplevel"/> specified by <paramref name="top"/>. If <paramref name="top"/> is
    ///     the top-most Toplevel, <see cref="Application.RequestStop(Toplevel)"/> will be called, causing the application to
    ///     exit.
    /// </summary>
    /// <param name="top">The Toplevel to request stop.</param>
    public virtual void RequestStop (Toplevel top) { top.RequestStop (); }

    internal virtual void OnAllChildClosed () { AllChildClosed?.Invoke (this, EventArgs.Empty); }

    internal virtual void OnChildClosed (Toplevel top)
    {
        if (IsOverlappedContainer)
        {
            SetSubViewNeedsDisplay ();
        }

        ChildClosed?.Invoke (this, new (top));
    }

    internal virtual void OnChildLoaded (Toplevel top) { ChildLoaded?.Invoke (this, new (top)); }
    internal virtual void OnChildUnloaded (Toplevel top) { ChildUnloaded?.Invoke (this, new (top)); }
    internal virtual void OnClosed (Toplevel top) { Closed?.Invoke (this, new (top)); }

    internal virtual bool OnClosing (ToplevelClosingEventArgs ev)
    {
        Closing?.Invoke (this, ev);

        return ev.Cancel;
    }

    internal virtual void OnDeactivate (Toplevel activated) { Deactivate?.Invoke (this, new (activated)); }

    /// <summary>
    ///     Called from <see cref="Application.RunLoop"/> after the <see cref="Toplevel"/> has entered the first iteration
    ///     of the loop.
    /// </summary>
    internal virtual void OnReady ()
    {
        foreach (var view in Subviews.Where (v => v is Toplevel))
        {
            var tl = (Toplevel)view;
            tl.OnReady ();
        }

        Ready?.Invoke (this, EventArgs.Empty);
    }

    /// <summary>Called from <see cref="Application.End(RunState)"/> before the <see cref="Toplevel"/> is disposed.</summary>
    internal virtual void OnUnloaded ()
    {
        foreach (var view in Subviews.Where (v => v is Toplevel))
        {
            var tl = (Toplevel)view;
            tl.OnUnloaded ();
        }

        Unloaded?.Invoke (this, EventArgs.Empty);
    }

    #endregion

    #region Draw

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        if (!Visible)
        {
            return;
        }

        if (NeedsDisplay || SubViewNeedsDisplay /*|| LayoutNeeded*/)
        {
            Clear ();

            //LayoutSubviews ();
            PositionToplevels ();

            if (this == ApplicationOverlapped.OverlappedTop)
            {
                // This enables correct draw behavior when switching between overlapped subviews
                foreach (Toplevel top in ApplicationOverlapped.OverlappedChildren!.AsEnumerable ().Reverse ())
                {
                    if (top.Frame.IntersectsWith (Viewport))
                    {
                        if (top != this && !top.IsCurrentTop && !OutsideTopFrame (top) && top.Visible)
                        {
                            top.SetNeedsLayout ();
                            top.SetNeedsDisplay (top.Viewport);
                            top.Draw ();
                            top.OnRenderLineCanvas ();
                        }
                    }
                }
            }

            // BUGBUG: This appears to be a hack to get ScrollBarViews to render correctly.
            foreach (View view in Subviews)
            {
                if (view.Frame.IntersectsWith (Viewport) && !OutsideTopFrame (this))
                {
                    //view.SetNeedsLayout ();
                    view.SetNeedsDisplay ();
                    view.SetSubViewNeedsDisplay ();
                }
            }

            base.OnDrawContent (viewport);
        }
    }

    #endregion

    #region Navigation

    /// <inheritdoc/>
    public override bool OnEnter (View view) { return MostFocused?.OnEnter (view) ?? base.OnEnter (view); }

    /// <inheritdoc/>
    public override bool OnLeave (View view) { return MostFocused?.OnLeave (view) ?? base.OnLeave (view); }

    #endregion

    #region Size / Position Management

    // TODO: Make cancelable?
    internal virtual void OnSizeChanging (SizeChangedEventArgs size) { SizeChanging?.Invoke (this, size); }

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        if (!IsOverlappedContainer)
        {
            if (Focused is null)
            {
                RestoreFocus ();
            }

            return null;
        }

        // This code path only happens when the Toplevel is an Overlapped container

        if (Focused is null)
        {
            // TODO: this is an Overlapped hack
            foreach (Toplevel top in ApplicationOverlapped.OverlappedChildren!)
            {
                if (top != this && top.Visible)
                {
                    top.SetFocus ();

                    return null;
                }
            }
        }

        Point? cursor2 = base.PositionCursor ();

        return null;
    }

    /// <summary>
    ///     Adjusts the location and size of <paramref name="top"/> within this Toplevel. Virtual method enabling
    ///     implementation of specific positions for inherited <see cref="Toplevel"/> views.
    /// </summary>
    /// <param name="top">The Toplevel to adjust.</param>
    public virtual void PositionToplevel (Toplevel? top)
    {
        if (top is null)
        {
            return;
        }

        View? superView = GetLocationEnsuringFullVisibility (
                                                            top,
                                                            top.Frame.X,
                                                            top.Frame.Y,
                                                            out int nx,
                                                            out int ny,
                                                            out StatusBar? sb
                                                           );

        if (superView is null)
        {
            return;
        }

        var layoutSubviews = false;
        var maxWidth = 0;

        if (superView.Margin is { } && superView == top.SuperView)
        {
            maxWidth -= superView.GetAdornmentsThickness ().Left + superView.GetAdornmentsThickness ().Right;
        }

        if ((superView != top || top?.SuperView is { } || (top != Application.Top && top!.Modal) || (top?.SuperView is null && ApplicationOverlapped.IsOverlapped (top)))
            && (top!.Frame.X + top.Frame.Width > maxWidth || ny > top.Frame.Y))
        {
            if (top?.X is null or PosAbsolute && top?.Frame.X != nx)
            {
                top!.X = nx;
                layoutSubviews = true;
            }

            if (top?.Y is null or PosAbsolute && top?.Frame.Y != ny)
            {
                top!.Y = ny;
                layoutSubviews = true;
            }
        }

        // TODO: v2 - This is a hack to get the StatusBar to be positioned correctly.
        if (sb != null
            && !top!.Subviews.Contains (sb)
            && ny + top.Frame.Height != superView.Frame.Height - (sb.Visible ? 1 : 0)
            && top.Height is DimFill
            && -top.Height.GetAnchor (0) < 1)
        {
            top.Height = Dim.Fill (sb.Visible ? 1 : 0);
            layoutSubviews = true;
        }

        if (superView.LayoutNeeded || layoutSubviews)
        {
            superView.LayoutSubviews ();
        }

        if (LayoutNeeded)
        {
            LayoutSubviews ();
        }
    }

    /// <summary>Invoked when the terminal has been resized. The new <see cref="Size"/> of the terminal is provided.</summary>
    public event EventHandler<SizeChangedEventArgs>? SizeChanging;

    private bool OutsideTopFrame (Toplevel top)
    {
        if (top.Frame.X > Driver.Cols || top.Frame.Y > Driver.Rows)
        {
            return true;
        }

        return false;
    }

    // TODO: v2 - Not sure this is needed anymore.
    internal void PositionToplevels ()
    {
        PositionToplevel (this);

        foreach (View top in Subviews)
        {
            if (top is Toplevel)
            {
                PositionToplevel ((Toplevel)top);
            }
        }
    }

    #endregion
}

/// <summary>
///     Implements the <see cref="IEqualityComparer{T}"/> for comparing two <see cref="Toplevel"/>s used by
///     <see cref="StackExtensions"/>.
/// </summary>
public class ToplevelEqualityComparer : IEqualityComparer<Toplevel>
{
    /// <summary>Determines whether the specified objects are equal.</summary>
    /// <param name="x">The first object of type <see cref="Toplevel"/> to compare.</param>
    /// <param name="y">The second object of type <see cref="Toplevel"/> to compare.</param>
    /// <returns><see langword="true"/> if the specified objects are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals (Toplevel? x, Toplevel? y)
    {
        if (y is null && x is null)
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        if (x.Id == y.Id)
        {
            return true;
        }

        return false;
    }

    /// <summary>Returns a hash code for the specified object.</summary>
    /// <param name="obj">The <see cref="Toplevel"/> for which a hash code is to be returned.</param>
    /// <returns>A hash code for the specified object.</returns>
    /// <exception cref="ArgumentNullException">
    ///     The type of <paramref name="obj"/> is a reference type and
    ///     <paramref name="obj"/> is <see langword="null"/>.
    /// </exception>
    public int GetHashCode (Toplevel obj)
    {
        if (obj is null)
        {
            throw new ArgumentNullException ();
        }

        var hCode = 0;

        if (int.TryParse (obj.Id, out int result))
        {
            hCode = result;
        }

        return hCode.GetHashCode ();
    }
}

/// <summary>
///     Implements the <see cref="IComparer{T}"/> to sort the <see cref="Toplevel"/> from the
///     <see cref="ApplicationOverlapped.OverlappedChildren"/> if needed.
/// </summary>
public sealed class ToplevelComparer : IComparer<Toplevel>
{
    /// <summary>
    ///     Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the
    ///     other.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>
    ///     A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown
    ///     in the following table.Value Meaning Less than zero <paramref name="x"/> is less than <paramref name="y"/>.Zero
    ///     <paramref name="x"/> equals <paramref name="y"/> .Greater than zero <paramref name="x"/> is greater than
    ///     <paramref name="y"/>.
    /// </returns>
    public int Compare (Toplevel? x, Toplevel? y)
    {
        if (ReferenceEquals (x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        return string.CompareOrdinal (x.Id, y.Id);
    }
}
