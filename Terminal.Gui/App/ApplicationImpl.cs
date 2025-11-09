#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.VisualBasic;

namespace Terminal.Gui.App;

/// <summary>
///     Implementation of core <see cref="Application"/> methods using the modern
///     main loop architecture with component factories for different platforms.
/// </summary>
public class ApplicationImpl : IApplication
{
    // Private static readonly Lazy instance of Application
    private static Lazy<IApplication> _lazyInstance = new (() => new ApplicationImpl ());

    /// <summary>
    ///     Creates a new instance of the Application backend.
    /// </summary>
    public ApplicationImpl () { }

    internal ApplicationImpl (IComponentFactory componentFactory) { _componentFactory = componentFactory; }

    private readonly IComponentFactory? _componentFactory;
    private readonly ITimedEvents _timedEvents = new TimedEvents ();
    private readonly object _lockScreen = new ();
    private string? _driverName;
    private Rectangle? _screen;

    private IMouse? _mouse;

    private IKeyboard? _keyboard;

    /// <inheritdoc/>
    public ITimedEvents? TimedEvents => _timedEvents;

    /// <summary>
    ///     Handles mouse event state and processing.
    /// </summary>
    public IMouse Mouse
    {
        get
        {
            if (_mouse is null)
            {
                _mouse = new MouseImpl { Application = this };
            }

            return _mouse;
        }
        set => _mouse = value ?? throw new ArgumentNullException (nameof (value));
    }

    /// <summary>
    ///     Handles keyboard input and key bindings at the Application level
    /// </summary>
    public IKeyboard Keyboard
    {
        get
        {
            if (_keyboard is null)
            {
                _keyboard = new KeyboardImpl { Application = this };
            }

            return _keyboard;
        }
        set => _keyboard = value ?? throw new ArgumentNullException (nameof (value));
    }

    /// <inheritdoc/>
    public IDriver? Driver { get; set; }

    /// <inheritdoc/>
    public bool Initialized { get; set; }

    /// <inheritdoc/>
    public bool Force16Colors { get; set; }

    /// <inheritdoc/>
    public string ForceDriver { get; set; } = string.Empty;

    /// <inheritdoc/>
    public List<SixelToRender> Sixel { get; } = new ();

    /// <inheritdoc/>
    public Rectangle Screen
    {
        get
        {
            lock (_lockScreen)
            {
                if (_screen == null)
                {
                    _screen = Driver?.Screen ?? new (new (0, 0), new (2048, 2048));
                }

                return _screen.Value;
            }
        }
        set
        {
            if (value is { } && (value.X != 0 || value.Y != 0))
            {
                throw new NotImplementedException ("Screen locations other than 0, 0 are not yet supported");
            }

            lock (_lockScreen)
            {
                _screen = value;
            }
        }
    }

    /// <inheritdoc/>
    public bool ClearScreenNextIteration { get; set; }

    /// <inheritdoc/>
    public bool PositionCursor ()
    {
        // TODO: Move this to IApplication/ApplicationImpl
        // Find the most focused view and position the cursor there.
        View? mostFocused = Navigation?.GetFocused ();

        // If the view is not visible or enabled, don't position the cursor
        if (mostFocused is null || !mostFocused.Visible || !mostFocused.Enabled)
        {
            var current = CursorVisibility.Invisible;
            Driver?.GetCursorVisibility (out current);

            if (current != CursorVisibility.Invisible)
            {
                Driver?.SetCursorVisibility (CursorVisibility.Invisible);
            }

            return false;
        }

        // If the view is not visible within it's superview, don't position the cursor
        Rectangle mostFocusedViewport = mostFocused.ViewportToScreen (mostFocused.Viewport with { Location = Point.Empty });

        Rectangle superViewViewport =
            mostFocused.SuperView?.ViewportToScreen (mostFocused.SuperView.Viewport with { Location = Point.Empty }) ?? Driver!.Screen;

        if (!superViewViewport.IntersectsWith (mostFocusedViewport))
        {
            return false;
        }

        Point? cursor = mostFocused.PositionCursor ();

        Driver!.GetCursorVisibility (out CursorVisibility currentCursorVisibility);

        if (cursor is { })
        {
            // Convert cursor to screen coords
            cursor = mostFocused.ViewportToScreen (mostFocused.Viewport with { Location = cursor.Value }).Location;

            // If the cursor is not in a visible location in the SuperView, hide it
            if (!superViewViewport.Contains (cursor.Value))
            {
                if (currentCursorVisibility != CursorVisibility.Invisible)
                {
                    Driver.SetCursorVisibility (CursorVisibility.Invisible);
                }

                return false;
            }

            // Show it
            if (currentCursorVisibility == CursorVisibility.Invisible)
            {
                Driver.SetCursorVisibility (mostFocused.CursorVisibility);
            }

            return true;
        }

        if (currentCursorVisibility != CursorVisibility.Invisible)
        {
            Driver.SetCursorVisibility (CursorVisibility.Invisible);
        }

        return false;
    }
    /// <inheritdoc/>
    public ApplicationPopover? Popover { get; set; }

    /// <inheritdoc/>
    public ApplicationNavigation? Navigation { get; set; }

    /// <inheritdoc/>
    public Toplevel? Top { get; set; }

    /// <inheritdoc/>
    public ConcurrentStack<Toplevel> TopLevels { get; } = new ();

    // When `End ()` is called, it is possible `RunState.Toplevel` is a different object than `Top`.
    // This variable is set in `End` in this case so that `Begin` correctly sets `Top`.
    /// <inheritdoc/>
    public Toplevel? CachedRunStateToplevel { get; set; }

    /// <inheritdoc/>
    public void RequestStop () { RequestStop (null); }

    /// <inheritdoc/>
    public bool StopAfterFirstIteration { get; set; }

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void Init (IDriver? driver = null, string? driverName = null)
    {
        if (Initialized)
        {
            Logging.Error ("Init called multiple times without shutdown, aborting.");

            throw new InvalidOperationException ("Init called multiple times without Shutdown");
        }

        if (!string.IsNullOrWhiteSpace (driverName))
        {
            _driverName = driverName;
        }

        if (string.IsNullOrWhiteSpace (_driverName))
        {
            _driverName = ForceDriver;
        }

        Debug.Assert (Navigation is null);
        Navigation = new ();

        Debug.Assert (Popover is null);
        Popover = new ();

        // Preserve existing keyboard settings if they exist
        bool hasExistingKeyboard = _keyboard is { };
        Key existingQuitKey = _keyboard?.QuitKey ?? Key.Esc;
        Key existingArrangeKey = _keyboard?.ArrangeKey ?? Key.F5.WithCtrl;
        Key existingNextTabKey = _keyboard?.NextTabKey ?? Key.Tab;
        Key existingPrevTabKey = _keyboard?.PrevTabKey ?? Key.Tab.WithShift;
        Key existingNextTabGroupKey = _keyboard?.NextTabGroupKey ?? Key.F6;
        Key existingPrevTabGroupKey = _keyboard?.PrevTabGroupKey ?? Key.F6.WithShift;

        // Reset keyboard to ensure fresh state with default bindings
        _keyboard = new KeyboardImpl { Application = this };

        // Restore previously set keys if they existed and were different from defaults
        if (hasExistingKeyboard)
        {
            _keyboard.QuitKey = existingQuitKey;
            _keyboard.ArrangeKey = existingArrangeKey;
            _keyboard.NextTabKey = existingNextTabKey;
            _keyboard.PrevTabKey = existingPrevTabKey;
            _keyboard.NextTabGroupKey = existingNextTabGroupKey;
            _keyboard.PrevTabGroupKey = existingPrevTabGroupKey;
        }

        CreateDriver (driverName ?? _driverName);
        Screen = Driver!.Screen;
        Initialized = true;

        RaiseInitializedChanged (this, new (true));
        SubscribeDriverEvents ();

        SynchronizationContext.SetSynchronizationContext (new ());
        MainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    /// <inheritdoc />
    public RunState Begin (Toplevel toplevel)
    {
        // TODO: Move this to IApplication/ApplicationImpl
        ArgumentNullException.ThrowIfNull (toplevel);

        // Ensure the mouse is ungrabbed.
        if (Mouse.MouseGrabView is { })
        {
            Mouse.UngrabMouse ();
        }

        var rs = new RunState (toplevel);

#if DEBUG_IDISPOSABLE
        if (View.EnableDebugIDisposableAsserts && Top is { } && toplevel != Top && !TopLevels.Contains (Top))
        {
            // This assertion confirm if the Top was already disposed
            Debug.Assert (Top.WasDisposed);
            Debug.Assert (Top == CachedRunStateToplevel);
        }
#endif

        lock (TopLevels)
        {
            if (Top is { } && toplevel != Top && !TopLevels.Contains (Top))
            {
                // If Top was already disposed and isn't on the Toplevels Stack,
                // clean it up here if is the same as _cachedRunStateToplevel
                if (Top == CachedRunStateToplevel)
                {
                    Top = null;
                }
                else
                {
                    // Probably this will never hit
                    throw new ObjectDisposedException (Top.GetType ().FullName);
                }
            }

            // BUGBUG: We should not depend on `Id` internally.
            // BUGBUG: It is super unclear what this code does anyway.
            if (string.IsNullOrEmpty (toplevel.Id))
            {
                var count = 1;
                var id = (TopLevels.Count + count).ToString ();

                while (TopLevels.Count > 0 && TopLevels.FirstOrDefault (x => x.Id == id) is { })
                {
                    count++;
                    id = (TopLevels.Count + count).ToString ();
                }

                toplevel.Id = (TopLevels.Count + count).ToString ();

                TopLevels.Push (toplevel);
            }
            else
            {
                Toplevel? dup = TopLevels.FirstOrDefault (x => x.Id == toplevel.Id);

                if (dup is null)
                {
                    TopLevels.Push (toplevel);
                }
            }
        }

        if (Top is null)
        {
            Top = toplevel;
        }

        if ((Top?.Modal == false && toplevel.Modal)
            || (Top?.Modal == false && !toplevel.Modal)
            || (Top?.Modal == true && toplevel.Modal))
        {
            if (toplevel.Visible)
            {
                if (Top is { HasFocus: true })
                {
                    Top.HasFocus = false;
                }

                // Force leave events for any entered views in the old Top
                if (Mouse.GetLastMousePosition () is { })
                {
                   Mouse.RaiseMouseEnterLeaveEvents (Mouse.GetLastMousePosition ()!.Value, new ());
                }

                Top?.OnDeactivate (toplevel);
                Toplevel previousTop = Top!;

                Top = toplevel;
                Top.OnActivate (previousTop);
            }
        }

        // View implements ISupportInitializeNotification which is derived from ISupportInitialize
        if (!toplevel.IsInitialized)
        {
            toplevel.BeginInit ();
            toplevel.EndInit (); // Calls Layout
        }

        // Try to set initial focus to any TabStop
        if (!toplevel.HasFocus)
        {
            toplevel.SetFocus ();
        }

        toplevel.OnLoaded ();

        ApplicationImpl.Instance.LayoutAndDraw (true);

        if (PositionCursor ())
        {
            Driver?.UpdateCursor ();
        }

        NotifyNewRunState?.Invoke (toplevel, new (rs));

        return rs;
    }

    /// <summary>
    ///     Runs the application by creating a <see cref="Toplevel"/> object and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool})"/>.
    /// </summary>
    /// <returns>The created <see cref="Toplevel"/> object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public Toplevel Run (Func<Exception, bool>? errorHandler = null, string? driver = null)
    { return Run<Toplevel> (errorHandler, driver); }

    /// <summary>
    ///     Runs the application by creating a <see cref="Toplevel"/>-derived object of type <c>T</c> and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool})"/>.
    /// </summary>
    /// <param name="errorHandler"></param>
    /// <param name="driver">
    ///     The <see cref="IDriver"/> to use. If not specified the default driver for the platform will
    ///     be used. Must be <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>The created TView object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public TView Run<TView> (Func<Exception, bool>? errorHandler = null, string? driver = null)
        where TView : Toplevel, new()
    {
        if (!Initialized)
        {
            // Init() has NOT been called. Auto-initialize as per interface contract.
            Init (null, driver);
        }

        TView top = new ();
        Run (top, errorHandler);

        return top;
    }

    /// <summary>Runs the Application using the provided <see cref="Toplevel"/> view.</summary>
    /// <param name="view">The <see cref="Toplevel"/> to run as a modal.</param>
    /// <param name="errorHandler">Handler for any unhandled exceptions.</param>
    public void Run (Toplevel view, Func<Exception, bool>? errorHandler = null)
    {
        Logging.Information ($"Run '{view}'");
        ArgumentNullException.ThrowIfNull (view);

        if (!Initialized)
        {
            throw new NotInitializedException (nameof (Run));
        }

        if (Driver == null)
        {
            throw new InvalidOperationException ("Driver was inexplicably null when trying to Run view");
        }

        Top = view;

        RunState rs = Application.Begin (view);

        Top.Running = true;

        var firstIteration = true;

        while (TopLevels.TryPeek (out Toplevel? found) && found == view && view.Running)
        {
            if (Coordinator is null)
            {
                throw new ($"{nameof (IMainLoopCoordinator)} inexplicably became null during Run");
            }

            Coordinator.RunIteration ();

            if (StopAfterFirstIteration && firstIteration)
            {
                Logging.Information ("Run - Stopping after first iteration as requested");
                view.RequestStop ();
            }
        }

        Logging.Information ("Run - Calling End");
        Application.End (rs);
    }

    /// <inheritdoc />
    public void RaiseIteration ()
    {
        Iteration?.Invoke (null, new ());
    }

    /// <inheritdoc />
    public event EventHandler<IterationEventArgs>? Iteration;

    public void End (RunState runState)
    {
        // TODO: Move this to IApplication/ApplicationImpl
        ArgumentNullException.ThrowIfNull (runState);

        if (Popover?.GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            ApplicationPopover.HideWithQuitCommand (visiblePopover);
        }

        runState.Toplevel.OnUnloaded ();

        // End the RunState.Toplevel
        // First, take it off the Toplevel Stack
        if (TopLevels.TryPop (out Toplevel? topOfStack))
        {
            if (topOfStack != runState.Toplevel)
            {
                // If the top of the stack is not the RunState.Toplevel then
                // this call to End is not balanced with the call to Begin that started the RunState
                throw new ArgumentException ("End must be balanced with calls to Begin");
            }
        }

        // Notify that it is closing
        runState.Toplevel?.OnClosed (runState.Toplevel);

        if (TopLevels.TryPeek (out Toplevel? newTop))
        {
            Top = newTop;
            Top?.SetNeedsDraw ();
        }

        if (runState.Toplevel is { HasFocus: true })
        {
            runState.Toplevel.HasFocus = false;
        }

        if (Top is { HasFocus: false })
        {
            Top.SetFocus ();
        }

        CachedRunStateToplevel = runState.Toplevel;

        runState.Toplevel = null;
        runState.Dispose ();

        LayoutAndDraw (true);
    }

    /// <inheritdoc />
    public event EventHandler<RunStateEventArgs>? NotifyNewRunState;

    /// <inheritdoc />
    public event EventHandler<ToplevelEventArgs>? NotifyStopRunState;

    /// <summary>
    ///  Raises the <see cref="InitializedChanged"/> event.
    /// </summary>
    internal void RaiseInitializedChanged (object sender, EventArgs<bool> e)
    {
        InitializedChanged?.Invoke (sender, e);
    }

    /// <inheritdoc />
    public event EventHandler<EventArgs<bool>>? InitializedChanged;

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    public void Shutdown ()
    {
        Coordinator?.Stop ();

        bool wasInitialized = Initialized;

        // Reset Screen before calling Application.ResetState to avoid circular reference
        ResetScreen ();

        // Call ResetState FIRST so it can properly dispose Popover and other resources
        // that are accessed via Application.* static properties that now delegate to instance fields
        ResetState ();
        ConfigurationManager.PrintJsonErrors ();

        // Clear instance fields after ResetState has disposed everything
        Driver = null;
        _mouse = null;
        _keyboard = null;
        Initialized = false;
        Navigation = null;
        Popover = null;
        CachedRunStateToplevel = null;
        Top = null;
        TopLevels.Clear ();
        MainThreadId = null;
        _screen = null;
        ClearScreenNextIteration = false;
        Sixel.Clear ();

        // Don't reset ForceDriver and Force16Colors; they need to be set before Init is called

        if (wasInitialized)
        {
            bool init = Initialized; // Will be false after clearing fields above
            RaiseInitializedChanged (this, new (in init));
        }

        // TODO: Determine if we should be resetting this here. Initialized is bound to
        // TODO: Init/Shutdown, and the point of this event is to notify when that changes.
        InitializedChanged = null;

        _lazyInstance = new (() => new ApplicationImpl ());
    }

    /// <inheritdoc/>
    public void RequestStop (Toplevel? top)
    {
        Logging.Trace ($"Top: '{(top is { } ? top : "null")}'");

        top ??= Top;

        if (top == null)
        {
            return;
        }

        ToplevelClosingEventArgs ev = new (top);
        top.OnClosing (ev);

        if (ev.Cancel)
        {
            return;
        }

        top.Running = false;
    }

    /// <inheritdoc/>
    public void Invoke (Action action)
    {
        // If we are already on the main UI thread
        if (Top is { Running: true } && MainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            action ();

            return;
        }

        _timedEvents.Add (
                          TimeSpan.Zero,
                          () =>
                          {
                              action ();

                              return false;
                          }
                         );
    }

    /// <inheritdoc/>
    public bool IsLegacy => false;

    /// <inheritdoc/>
    public object AddTimeout (TimeSpan time, Func<bool> callback) { return _timedEvents.Add (time, callback); }

    /// <inheritdoc/>
    public bool RemoveTimeout (object token) { return _timedEvents.Remove (token); }

    /// <inheritdoc/>
    public void LayoutAndDraw (bool forceRedraw = false)
    {
        List<View> tops = [.. TopLevels];

        if (Popover?.GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            visiblePopover.SetNeedsDraw ();
            visiblePopover.SetNeedsLayout ();
            tops.Insert (0, visiblePopover);
        }

        bool neededLayout = View.Layout (tops.ToArray ().Reverse (), Screen.Size);

        if (ClearScreenNextIteration)
        {
            forceRedraw = true;
            ClearScreenNextIteration = false;
        }

        if (forceRedraw)
        {
            Driver?.ClearContents ();
        }

        View.SetClipToScreen ();
        View.Draw (tops, neededLayout || forceRedraw);
        View.SetClipToScreen ();
        Driver?.Refresh ();
    }

    /// <summary>
    ///     Change the singleton implementation, should not be called except before application
    ///     startup. This method lets you provide alternative implementations of core static gateway
    ///     methods of <see cref="Application"/>.
    /// </summary>
    /// <param name="newApplication"></param>
    public static void ChangeInstance (IApplication? newApplication) { _lazyInstance = new (newApplication!); }

    /// <summary>
    ///     Gets the currently configured backend implementation of <see cref="Application"/> gateway methods.
    ///     Change to your own implementation by using <see cref="ChangeInstance"/> (before init).
    /// </summary>
    public static IApplication Instance => _lazyInstance.Value;

    internal IMainLoopCoordinator? Coordinator { get; private set; }

    /// <summary>
    ///     Gets or sets the main thread ID for the application.
    /// </summary>
    internal int? MainThreadId { get; set; }

    /// <summary>
    ///     Resets the Screen field to null so it will be recalculated on next access.
    /// </summary>
    internal void ResetScreen ()
    {
        lock (_lockScreen)
        {
            _screen = null;
        }
    }

    /// <summary>
    ///     Creates the appropriate <see cref="IDriver"/> based on platform and driverName.
    /// </summary>
    /// <param name="driverName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    private void CreateDriver (string? driverName)
    {
        PlatformID p = Environment.OSVersion.Platform;

        // Check component factory type first - this takes precedence over driverName
        bool factoryIsWindows = _componentFactory is IComponentFactory<WindowsConsole.InputRecord>;
        bool factoryIsDotNet = _componentFactory is IComponentFactory<ConsoleKeyInfo>;
        bool factoryIsUnix = _componentFactory is IComponentFactory<char>;
        bool factoryIsFake = _componentFactory is IComponentFactory<ConsoleKeyInfo>;

        // Then check driverName
        bool nameIsWindows = driverName?.Contains ("win", StringComparison.OrdinalIgnoreCase) ?? false;
        bool nameIsDotNet = driverName?.Contains ("dotnet", StringComparison.OrdinalIgnoreCase) ?? false;
        bool nameIsUnix = driverName?.Contains ("unix", StringComparison.OrdinalIgnoreCase) ?? false;
        bool nameIsFake = driverName?.Contains ("fake", StringComparison.OrdinalIgnoreCase) ?? false;

        // Decide which driver to use - component factory type takes priority
        if (factoryIsFake || (!factoryIsWindows && !factoryIsDotNet && !factoryIsUnix && nameIsFake))
        {
            Coordinator = CreateSubcomponents (fallbackFactory: () => new FakeComponentFactory ());
        }
        else if (factoryIsWindows || (!factoryIsDotNet && !factoryIsUnix && nameIsWindows))
        {
            Coordinator = CreateSubcomponents (fallbackFactory: () => new WindowsComponentFactory ());
        }
        else if (factoryIsDotNet || (!factoryIsWindows && !factoryIsUnix && nameIsDotNet))
        {
            Coordinator = CreateSubcomponents (fallbackFactory: () => new NetComponentFactory ());
        }
        else if (factoryIsUnix || (!factoryIsWindows && !factoryIsDotNet && nameIsUnix))
        {
            Coordinator = CreateSubcomponents (fallbackFactory: () => new UnixComponentFactory ());
        }
        else if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            Coordinator = CreateSubcomponents (fallbackFactory: () => new WindowsComponentFactory ());
        }
        else
        {
            Coordinator = CreateSubcomponents (fallbackFactory: () => new UnixComponentFactory ());
        }

        Logging.Trace ($"Created Subcomponents: {Coordinator}");

        Coordinator.StartInputTaskAsync ().Wait ();

        if (Driver == null)
        {
            throw new ("Driver was null even after booting MainLoopCoordinator");
        }
    }

    private IMainLoopCoordinator CreateSubcomponents<TInputRecord> (Func<IComponentFactory<TInputRecord>> fallbackFactory) where TInputRecord : struct
    {
        ConcurrentQueue<TInputRecord> inputQueue = new ();
        ApplicationMainLoop<TInputRecord> loop = new ();

        IComponentFactory<TInputRecord> cf;

        if (_componentFactory is IComponentFactory<TInputRecord> typedFactory)
        {
            cf = typedFactory;
        }
        else
        {
            cf = fallbackFactory ();
        }

        return new MainLoopCoordinator<TInputRecord> (_timedEvents, inputQueue, loop, cf);
    }

    private void Driver_SizeChanged (object? sender, SizeChangedEventArgs e)
    {
        RaiseScreenChangedEvent (new Rectangle (new (0, 0), e.Size!.Value));
    }


    /// <summary>
    ///     Called when the application's size has changed. Sets the size of all <see cref="Toplevel"/>s and fires the
    ///     <see cref="ScreenChanged"/> event.
    /// </summary>
    /// <param name="screen">The new screen size and position.</param>
    public void RaiseScreenChangedEvent (Rectangle screen)
    {
        Screen = new (Point.Empty, screen.Size);

        ScreenChanged?.Invoke (this, new (screen));

        foreach (Toplevel t in TopLevels)
        {
            t.OnSizeChanging (new (screen.Size));
            t.SetNeedsLayout ();
        }

        LayoutAndDraw (true);
    }

    /// <inheritdoc />
    public  event EventHandler<EventArgs<Rectangle>>? ScreenChanged;

    private void Driver_KeyDown (object? sender, Key e) { Keyboard?.RaiseKeyDownEvent (e); }
    private void Driver_KeyUp (object? sender, Key e) { Keyboard?.RaiseKeyUpEvent (e); }
    private void Driver_MouseEvent (object? sender, MouseEventArgs e) { Mouse?.RaiseMouseEvent (e); }

    internal void SubscribeDriverEvents ()
    {
        ArgumentNullException.ThrowIfNull (Driver);

        Driver.SizeChanged += Driver_SizeChanged;
        Driver.KeyDown += Driver_KeyDown;
        Driver.KeyUp += Driver_KeyUp;
        Driver.MouseEvent += Driver_MouseEvent;
    }

    internal void UnsubscribeDriverEvents ()
    {
        ArgumentNullException.ThrowIfNull (Driver);

        Driver.SizeChanged -= Driver_SizeChanged;
        Driver.KeyDown -= Driver_KeyDown;
        Driver.KeyUp -= Driver_KeyUp;
        Driver.MouseEvent -= Driver_MouseEvent;
    }

    /// <inheritdoc />
    public void ResetState (bool ignoreDisposed = false)
    {
       // CheckReset ();

        // Shutdown is the bookend for Init. As such it needs to clean up all resources
        // Init created. Apps that do any threading will need to code defensively for this.
        // e.g. see Issue #537
        foreach (Toplevel? t in TopLevels)
        {
            t!.Running = false;
        }

        if (Popover?.GetActivePopover () is View popover)
        {
            // This forcefully closes the popover; invoking Command.Quit would be more graceful
            // but since this is shutdown, doing this is ok.
            popover.Visible = false;
        }

        Popover?.Dispose ();
        Popover = null;

        TopLevels.Clear ();
#if DEBUG_IDISPOSABLE

        // Don't dispose the Top. It's up to caller dispose it
        if (View.EnableDebugIDisposableAsserts && !ignoreDisposed && Top is { })
        {
            Debug.Assert (Top.WasDisposed, $"Title = {Top.Title}, Id = {Top.Id}");

            // If End wasn't called _cachedRunStateToplevel may be null
            if (CachedRunStateToplevel is { })
            {
                Debug.Assert (CachedRunStateToplevel.WasDisposed);
                Debug.Assert (CachedRunStateToplevel == Top);
            }
        }
#endif
        //RunningUnitTests = false;
        Top = null;
        CachedRunStateToplevel = null;

        MainThreadId = null;
        Iteration = null;
        StopAfterFirstIteration = false;
        ClearScreenNextIteration = false;

        // Driver stuff
        if (Driver is { })
        {
            UnsubscribeDriverEvents ();
            Driver?.End ();
            Driver = null;
        }

        // Reset Screen to null so it will be recalculated on next access
        // Note: ApplicationImpl.Shutdown() also calls ResetScreen() before calling this method
        // to avoid potential circular reference issues. Calling it twice is harmless.
        if (ApplicationImpl.Instance is ApplicationImpl impl)
        {
            impl.ResetScreen ();
        }

        // Run State stuff
        NotifyNewRunState = null;
        NotifyStopRunState = null;
        // Mouse and Keyboard will be lazy-initialized in ApplicationImpl on next access

        Initialized = false;

        // Mouse
        Mouse.ResetState ();

        // Keyboard events and bindings are now managed by the Keyboard instance

        ScreenChanged = null;

        Navigation = null;

        // Reset synchronization context to allow the user to run async/await,
        // as the main loop has been ended, the synchronization context from
        // gui.cs does no longer process any callbacks. See #1084 for more details:
        // (https://github.com/gui-cs/Terminal.Gui/issues/1084).
        SynchronizationContext.SetSynchronizationContext (null);
    }

    //internal static void CheckReset ()
    //{
    //    // Check that all fields and properties are set to their default values

    //    // Public Properties
    //    //Debug.Assert(null == Application.Top);
    //    //Debug.Assert(null == Application.Mouse.MouseGrabView);

    //    //// Don't check Application.ForceDriver
    //    //// Assert.Empty (Application.ForceDriver);
    //    //// Don't check Application.Force16Colors
    //    ////Debug.Assert(false == Application.Force16Colors);
    //    //Debug.Assert(null == Application.Driver);
    //    //Debug.Assert(false == Application.StopAfterFirstIteration);

    //    //// Commented out because if CM changed the defaults, those changes should
    //    //// persist across Inits.
    //    ////Assert.Equal (Key.Tab.WithShift, Application.PrevTabKey);
    //    ////Assert.Equal (Key.Tab, Application.NextTabKey);
    //    ////Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);
    //    ////Assert.Equal (Key.F6, Application.NextTabGroupKey);
    //    ////Assert.Equal (Key.Esc, Application.QuitKey);

    //    //// Internal properties
    //    //Debug.Assert(false == Application.Initialized);
    //    ////Assert.Equal (Application.GetSupportedCultures (), Application.SupportedCultures);
    //    ////Assert.Equal (Application.GetAvailableCulturesFromEmbeddedResources (), Application.SupportedCultures);
    //    //Debug.Assert(false == Application._forceFakeConsole);
    //    //Debug.Assert(null == Application.MainThreadId);
    //    ////Assert.Empty (Application.TopLevels);
    //    ////Assert.Empty (Application.CachedViewsUnderMouse);

    //    //// Mouse
    //    //// Do not reset _lastMousePosition
    //    ////Debug.Assert(null == Application._lastMousePosition);

    //    //// Navigation
    //    //Debug.Assert(null == Application.Navigation);

    //    //// Popover
    //    //Debug.Assert(null == Application.Popover);

    //    // Events - Can't check
    //    Debug.Assert (null == GetEventSubscribers (typeof (Application), "InitializedChanged"));
    //    Debug.Assert (null == GetEventSubscribers (typeof (ApplicationImpl), "NotifyNewRunState"));
    //    Debug.Assert (null == GetEventSubscribers (typeof (Application), "Iteration"));
    //    Debug.Assert (null == GetEventSubscribers (typeof (ApplicationImpl), "ScreenChanged"));
    //    //Debug.Assert(null == GetEventSubscribers (typeof (Application.Mouse), "MouseEvent"));
    //    //Debug.Assert(null == GetEventSubscribers (typeof (Application.Keyboard), "KeyDown"));
    //    //Debug.Assert(null == GetEventSubscribers (typeof (Application.Keyboard), "KeyUp"));
    //}


    ///// <summary>
    ///// Gets the delegate backing an event to check if it has subscribers.
    ///// Returns null if there are no subscribers.
    ///// </summary>
    //private static Delegate? GetEventSubscribers (Type type, string eventName)
    //{
    //    // Events are backed by private fields with the same name (or sometimes EventName + "Event")
    //    var field = type.GetField (eventName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

    //    if (field == null)
    //    {
    //        // Try alternative naming convention
    //        field = type.GetField (eventName + "Event", BindingFlags.NonPublic | BindingFlags.Instance);
    //    }

    //    if (field == null)
    //    {
    //        throw new ArgumentException ($"Event field '{eventName}' not found on type {type.Name}");
    //    }

    //    return (Delegate?)field.GetValue (null); // null for static fields
    //}
}
