#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Terminal.Gui.Drivers;

namespace Terminal.Gui.App;

/// <summary>
/// Implementation of core <see cref="Application"/> methods using the modern
/// main loop architecture with component factories for different platforms.
/// </summary>
public class ApplicationImpl : IApplication
{
    private readonly IComponentFactory? _componentFactory;
    private IMainLoopCoordinator? _coordinator;
    private string? _driverName;
    private readonly ITimedEvents _timedEvents = new TimedEvents ();
    private IConsoleDriver? _driver;
    private bool _initialized;
    private ApplicationPopover? _popover;
    private ApplicationNavigation? _navigation;
    private Toplevel? _top;
    private readonly ConcurrentStack<Toplevel> _topLevels = new ();
    private int _mainThreadId = -1;
    private bool _force16Colors;
    private string _forceDriver = string.Empty;
    private readonly List<SixelToRender> _sixel = new ();
    private readonly object _lockScreen = new ();
    private Rectangle? _screen;
    private bool _clearScreenNextIteration;

    // Private static readonly Lazy instance of Application
    private static Lazy<IApplication> _lazyInstance = new (() => new ApplicationImpl ());

    /// <summary>
    /// Gets the currently configured backend implementation of <see cref="Application"/> gateway methods.
    /// Change to your own implementation by using <see cref="ChangeInstance"/> (before init).
    /// </summary>
    public static IApplication Instance => _lazyInstance.Value;

    /// <inheritdoc/>
    public ITimedEvents? TimedEvents => _timedEvents;

    internal IMainLoopCoordinator? Coordinator => _coordinator;

    private IMouse? _mouse;

    /// <summary>
    /// Handles mouse event state and processing.
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

    private IKeyboard? _keyboard;

    /// <summary>
    /// Handles keyboard input and key bindings at the Application level
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
    public IConsoleDriver? Driver
    {
        get => _driver;
        set => _driver = value;
    }

    /// <inheritdoc/>
    public bool Initialized
    {
        get => _initialized;
        set => _initialized = value;
    }

    /// <inheritdoc/>
    public bool Force16Colors
    {
        get => _force16Colors;
        set => _force16Colors = value;
    }

    /// <inheritdoc/>
    public string ForceDriver
    {
        get => _forceDriver;
        set => _forceDriver = value;
    }

    /// <inheritdoc/>
    public List<SixelToRender> Sixel => _sixel;

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
                throw new NotImplementedException ($"Screen locations other than 0, 0 are not yet supported");
            }

            lock (_lockScreen)
            {
                _screen = value;
            }
        }
    }

    /// <inheritdoc/>
    public bool ClearScreenNextIteration
    {
        get => _clearScreenNextIteration;
        set => _clearScreenNextIteration = value;
    }

    /// <inheritdoc/>
    public ApplicationPopover? Popover
    {
        get => _popover;
        set => _popover = value;
    }

    /// <inheritdoc/>
    public ApplicationNavigation? Navigation
    {
        get => _navigation;
        set => _navigation = value;
    }

    /// <inheritdoc/>
    public Toplevel? Top
    {
        get => _top;
        set => _top = value;
    }

    /// <inheritdoc/>
    public ConcurrentStack<Toplevel> TopLevels => _topLevels;

    // When `End ()` is called, it is possible `RunState.Toplevel` is a different object than `Top`.
    // This variable is set in `End` in this case so that `Begin` correctly sets `Top`.
    /// <inheritdoc />
    public Toplevel? CachedRunStateToplevel { get; set; }

    /// <summary>
    /// Gets or sets the main thread ID for the application.
    /// </summary>
    internal int MainThreadId
    {
        get => _mainThreadId;
        set => _mainThreadId = value;
    }

    /// <inheritdoc/>
    public void RequestStop () => RequestStop (null);

    /// <inheritdoc />
    public bool StopAfterFirstIteration { get; set; }

    /// <summary>
    /// Creates a new instance of the Application backend.
    /// </summary>
    public ApplicationImpl ()
    {
    }

    internal ApplicationImpl (IComponentFactory componentFactory)
    {
        _componentFactory = componentFactory;
    }

    /// <summary>
    /// Change the singleton implementation, should not be called except before application
    /// startup. This method lets you provide alternative implementations of core static gateway
    /// methods of <see cref="Application"/>.
    /// </summary>
    /// <param name="newApplication"></param>
    public static void ChangeInstance (IApplication newApplication)
    {
        _lazyInstance = new Lazy<IApplication> (newApplication);
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void Init (IConsoleDriver? driver = null, string? driverName = null)
    {
        if (_initialized)
        {
            Logging.Logger.LogError ("Init called multiple times without shutdown, aborting.");

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

        Debug.Assert (_navigation is null);
        _navigation = new ();

        Debug.Assert (_popover is null);
        _popover = new ();

        // Preserve existing keyboard settings if they exist
        bool hasExistingKeyboard = _keyboard is not null;
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
        _initialized = true;

        Application.OnInitializedChanged (this, new (true));
        Application.SubscribeDriverEvents ();

        SynchronizationContext.SetSynchronizationContext (new ());
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

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
        bool nameIsDotNet = (driverName?.Contains ("dotnet", StringComparison.OrdinalIgnoreCase) ?? false);
        bool nameIsUnix = driverName?.Contains ("unix", StringComparison.OrdinalIgnoreCase) ?? false;
        bool nameIsFake = driverName?.Contains ("fake", StringComparison.OrdinalIgnoreCase) ?? false;

        // Decide which driver to use - component factory type takes priority
        if (factoryIsFake || (!factoryIsWindows && !factoryIsDotNet && !factoryIsUnix && nameIsFake))
        {
            FakeOutput fakeOutput = new ();
            _coordinator = CreateSubcomponents (() => new FakeComponentFactory (null, fakeOutput));
        }
        else if (factoryIsWindows || (!factoryIsDotNet && !factoryIsUnix && nameIsWindows))
        {
            _coordinator = CreateSubcomponents (() => new WindowsComponentFactory ());
        }
        else if (factoryIsDotNet || (!factoryIsWindows && !factoryIsUnix && nameIsDotNet))
        {
            _coordinator = CreateSubcomponents (() => new NetComponentFactory ());
        }
        else if (factoryIsUnix || (!factoryIsWindows && !factoryIsDotNet && nameIsUnix))
        {
            _coordinator = CreateSubcomponents (() => new UnixComponentFactory ());
        }
        else if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            if (ConsoleDriver.RunningUnitTests)
            {
                FakeOutput fakeOutput = new ();
                _coordinator = CreateSubcomponents (() => new FakeComponentFactory (null, fakeOutput));
            }
            else
            {
                _coordinator = CreateSubcomponents (() => new WindowsComponentFactory ());
            }
        }
        else
        {
            if (ConsoleDriver.RunningUnitTests)
            {
                FakeOutput fakeOutput = new ();
                _coordinator = CreateSubcomponents (() => new FakeComponentFactory (null, fakeOutput));
            }
            else
            {
                _coordinator = CreateSubcomponents (() => new UnixComponentFactory ());
            }
        }

        _coordinator.StartAsync ().Wait ();


        if (_driver == null)
        {
            throw new ("Driver was null even after booting MainLoopCoordinator");
        }

        if (!ConsoleDriver.RunningUnitTests && _driver.Screen.IsEmpty)
        {
            throw new InvalidOperationException (
                                                 "Driver.Screen is empty after Init. The driver should set the screen size during Init.");
        }
    }

    private IMainLoopCoordinator CreateSubcomponents<T> (Func<IComponentFactory<T>> fallbackFactory)
    {
        ConcurrentQueue<T> inputBuffer = new ();
        ApplicationMainLoop<T> loop = new ();

        IComponentFactory<T> cf;

        if (_componentFactory is IComponentFactory<T> typedFactory)
        {
            cf = typedFactory;
        }
        else
        {
            cf = fallbackFactory ();
        }

        return new MainLoopCoordinator<T> (_timedEvents, inputBuffer, loop, cf);
    }

    /// <summary>
    ///     Runs the application by creating a <see cref="Toplevel"/> object and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool})"/>.
    /// </summary>
    /// <returns>The created <see cref="Toplevel"/> object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public Toplevel Run (Func<Exception, bool>? errorHandler = null, IConsoleDriver? driver = null) { return Run<Toplevel> (errorHandler, driver); }

    /// <summary>
    ///     Runs the application by creating a <see cref="Toplevel"/>-derived object of type <c>T</c> and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool})"/>.
    /// </summary>
    /// <param name="errorHandler"></param>
    /// <param name="driver">
    ///     The <see cref="IConsoleDriver"/> to use. If not specified the default driver for the platform will
    ///     be used. Must be <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>The created T object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public T Run<T> (Func<Exception, bool>? errorHandler = null, IConsoleDriver? driver = null)
        where T : Toplevel, new()
    {
        if (!_initialized)
        {
            // Init() has NOT been called. Auto-initialize as per interface contract.
            Init (driver, null);
        }

        T top = new ();
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

        if (!_initialized)
        {
            throw new NotInitializedException (nameof (Run));
        }

        if (_driver == null)
        {
            throw new InvalidOperationException ("Driver was inexplicably null when trying to Run view");
        }

        _top = view;

        RunState rs = Application.Begin (view);

        _top.Running = true;

        bool firstIteration = true;
        while (_topLevels.TryPeek (out Toplevel? found) && found == view && view.Running)
        {
            if (_coordinator is null)
            {
                throw new ($"{nameof (IMainLoopCoordinator)} inexplicably became null during Run");
            }

            _coordinator.RunIteration ();
            if (StopAfterFirstIteration && firstIteration)
            {
                Logging.Information ("Run - Stopping after first iteration as requested");
                view.RequestStop ();
            }
        }

        Logging.Information ($"Run - Calling End");
        Application.End (rs);
    }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    public void Shutdown ()
    {
        _coordinator?.Stop ();

        bool wasInitialized = _initialized;

        // Reset Screen before calling Application.ResetState to avoid circular reference
        ResetScreen ();

        // Call ResetState FIRST so it can properly dispose Popover and other resources
        // that are accessed via Application.* static properties that now delegate to instance fields
        Application.ResetState ();
        ConfigurationManager.PrintJsonErrors ();

        // Clear instance fields after ResetState has disposed everything
        _driver = null;
        _mouse = null;
        _keyboard = null;
        _initialized = false;
        _navigation = null;
        _popover = null;
        CachedRunStateToplevel = null;
        _top = null;
        _topLevels.Clear ();
        _mainThreadId = -1;
        _screen = null;
        _clearScreenNextIteration = false;
        _sixel.Clear ();
        // Don't reset ForceDriver and Force16Colors; they need to be set before Init is called

        if (wasInitialized)
        {
            bool init = _initialized; // Will be false after clearing fields above
            Application.OnInitializedChanged (this, new (in init));
        }

        _lazyInstance = new (() => new ApplicationImpl ());
    }

    /// <inheritdoc />
    public void RequestStop (Toplevel? top)
    {
        Logging.Logger.LogInformation ($"RequestStop '{(top is { } ? top : "null")}'");

        top ??= _top;

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

    /// <inheritdoc />
    public void Invoke (Action action)
    {
        // If we are already on the main UI thread
        if (Top is { Running: true } && _mainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            action ();
            return;
        }

        _timedEvents.Add (TimeSpan.Zero,
                              () =>
                              {
                                  action ();
                                  return false;
                              }
                             );
    }

    /// <inheritdoc />
    public bool IsLegacy => false;

    /// <inheritdoc />
    public object AddTimeout (TimeSpan time, Func<bool> callback) { return _timedEvents.Add (time, callback); }

    /// <inheritdoc />
    public bool RemoveTimeout (object token) { return _timedEvents.Remove (token); }

    /// <inheritdoc />
    public void LayoutAndDraw (bool forceRedraw = false)
    {
        List<View> tops = [.. _topLevels];

        if (_popover?.GetActivePopover () as View is { Visible: true } visiblePopover)
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
            _driver?.ClearContents ();
        }

        View.SetClipToScreen ();
        View.Draw (tops, neededLayout || forceRedraw);
        View.SetClipToScreen ();
        _driver?.Refresh ();
    }

    /// <summary>
    /// Resets the Screen field to null so it will be recalculated on next access.
    /// </summary>
    internal void ResetScreen ()
    {
        lock (_lockScreen)
        {
            _screen = null;
        }
    }
}
