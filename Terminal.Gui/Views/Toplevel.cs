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
    ///     dimensions of the terminal using <see cref="Dim.Fill(Dim)"/>.
    /// </summary>
    public Toplevel ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        Arrangement = ViewArrangement.Overlapped;
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

    private void Toplevel_MouseClick (object? sender, MouseEventArgs e) { e.Handled = InvokeCommand (Command.HotKey) == true; }

    #endregion

    #region Subviews

    // TODO: Deprecate - Any view can host a menubar in v2
    /// <summary>Gets the latest <see cref="MenuBar"/> added into this Toplevel.</summary>
    public MenuBar? MenuBar => (MenuBar?)Subviews?.LastOrDefault (s => s is MenuBar);

    //// TODO: Deprecate - Any view can host a statusbar in v2
    ///// <summary>Gets the latest <see cref="StatusBar"/> added into this Toplevel.</summary>
    //public StatusBar? StatusBar => (StatusBar?)Subviews?.LastOrDefault (s => s is StatusBar);

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
    /// <summary>Invoked when the Toplevel <see cref="RunState"/> active.</summary>
    public event EventHandler<ToplevelEventArgs>? Activate;

    // TODO: IRunnable: Re-implement as an event on IRunnable; IRunnable.Deactivating/Deactivate?
    /// <summary>Invoked when the Toplevel<see cref="RunState"/> ceases to be active.</summary>
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
        Application.RequestStop (Application.Top);
    }

    /// <summary>
    ///     Invoked when the Toplevel <see cref="RunState"/> has been unloaded. A Unloaded event handler is a good place
    ///     to dispose objects after calling <see cref="Application.End(RunState)"/>.
    /// </summary>
    public event EventHandler? Unloaded;

    internal virtual void OnActivate (Toplevel deactivated) { Activate?.Invoke (this, new (deactivated)); }

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
    
    #region Size / Position Management

    // TODO: Make cancelable?
    internal void OnSizeChanging (SizeChangedEventArgs size) { SizeChanging?.Invoke (this, size); }

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
                                                            out int ny
                                                           //,
                                                           // out StatusBar? sb
                                                           );

        if (superView is null)
        {
            return;
        }

        //var layoutSubviews = false;
        var maxWidth = 0;

        if (superView.Margin is { } && superView == top.SuperView)
        {
            maxWidth -= superView.GetAdornmentsThickness ().Left + superView.GetAdornmentsThickness ().Right;
        }

        // BUGBUG: The && true is a temp hack
        if ((superView != top || top?.SuperView is { } || (top != Application.Top && top!.Modal) || (top == Application.Top && top?.SuperView is null))
            && (top!.Frame.X + top.Frame.Width > maxWidth || ny > top.Frame.Y))

        {
            if (top?.X is null or PosAbsolute && top?.Frame.X != nx)
            {
                top!.X = nx;
                //layoutSubviews = true;
            }

            if (top?.Y is null or PosAbsolute && top?.Frame.Y != ny)
            {
                top!.Y = ny;
                //layoutSubviews = true;
            }
        }


        //if (superView.IsLayoutNeeded () || layoutSubviews)
        //{
        //    superView.LayoutSubviews ();
        //}

        //if (IsLayoutNeeded ())
        //{
        //    LayoutSubviews ();
        //}
    }

    /// <summary>Invoked when the terminal has been resized. The new <see cref="Size"/> of the terminal is provided.</summary>
    public event EventHandler<SizeChangedEventArgs>? SizeChanging;

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
