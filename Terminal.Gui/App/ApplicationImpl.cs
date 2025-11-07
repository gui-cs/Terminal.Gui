#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

        Application.OnInitializedChanged (this, new (true));
        Application.SubscribeDriverEvents ();

        SynchronizationContext.SetSynchronizationContext (new ());
        MainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    /// <summary>
    ///     Runs the application by creating a <see cref="Toplevel"/> object and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool})"/>.
    /// </summary>
    /// <returns>The created <see cref="Toplevel"/> object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public Toplevel Run (Func<Exception, bool>? errorHandler = null, string? driver = null) { return Run<Toplevel> (errorHandler, driver); }

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
        where TView : Toplevel, new ()
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

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    public void Shutdown ()
    {
        Coordinator?.Stop ();

        bool wasInitialized = Initialized;

        // Reset Screen before calling Application.ResetState to avoid circular reference
        ResetScreen ();

        // Call ResetState FIRST so it can properly dispose Popover and other resources
        // that are accessed via Application.* static properties that now delegate to instance fields
        Application.ResetState ();
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
            Application.OnInitializedChanged (this, new (in init));
        }

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
            Application.RunningUnitTests = true;
            Coordinator = CreateSubcomponents (() => new FakeComponentFactory ());
        }
        else if (factoryIsWindows || (!factoryIsDotNet && !factoryIsUnix && nameIsWindows))
        {
            Coordinator = CreateSubcomponents (() => new WindowsComponentFactory ());
        }
        else if (factoryIsDotNet || (!factoryIsWindows && !factoryIsUnix && nameIsDotNet))
        {
            Coordinator = CreateSubcomponents (() => new NetComponentFactory ());
        }
        else if (factoryIsUnix || (!factoryIsWindows && !factoryIsDotNet && nameIsUnix))
        {
            Coordinator = CreateSubcomponents (() => new UnixComponentFactory ());
        }
        else if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            Coordinator = CreateSubcomponents (() => new WindowsComponentFactory ());
        }
        else
        {
            Coordinator = CreateSubcomponents (() => new UnixComponentFactory ());
        }

        Logging.Trace ($"Created Subcomponents: {Coordinator}");

        Coordinator.StartAsync ().Wait ();

        if (Driver == null)
        {
            throw new ("Driver was null even after booting MainLoopCoordinator");
        }

        if (!Application.RunningUnitTests && !Driver.Screen.IsEmpty)
        {
            //throw new InvalidOperationException ("Driver.Screen is empty after Init. The driver should set the screen size during Init.");
        }
    }

    private IMainLoopCoordinator CreateSubcomponents<TInputRecord> (Func<IComponentFactory<TInputRecord>> fallbackFactory) where TInputRecord : struct
    {
        ConcurrentQueue<TInputRecord> inputBuffer = new ();
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

        return new MainLoopCoordinator<TInputRecord> (_timedEvents, inputBuffer, loop, cf);
    }
}
