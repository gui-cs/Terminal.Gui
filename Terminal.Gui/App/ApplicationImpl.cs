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

    /// <summary>
    /// Handles which <see cref="View"/> (if any) has captured the mouse
    /// </summary>
    public IMouseGrabHandler MouseGrabHandler { get; set; } = new MouseGrabHandler ();

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
        if (Application.Initialized)
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
            _driverName = Application.ForceDriver;
        }

        Debug.Assert(Application.Navigation is null);
        Application.Navigation = new ();

        Debug.Assert (Application.Popover is null);
        Application.Popover = new ();

        Application.AddKeyBindings ();

        CreateDriver (driverName ?? _driverName);

        Application.Initialized = true;

        Application.OnInitializedChanged (this, new (true));
        Application.SubscribeDriverEvents ();

        SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext ());
        Application.MainThreadId = Thread.CurrentThread.ManagedThreadId;
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
            _coordinator = CreateSubcomponents (() => new FakeComponentFactory ());
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
            _coordinator = CreateSubcomponents (() => new WindowsComponentFactory ());
        }
        else
        {
            _coordinator = CreateSubcomponents (() => new UnixComponentFactory ());
        }

        _coordinator.StartAsync ().Wait ();

        if (Application.Driver == null)
        {
            throw new ("Application.Driver was null even after booting MainLoopCoordinator");
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
        if (!Application.Initialized)
        {
            // Init() has NOT been called. Auto-initialize as per interface contract.
            Init (driver, null);
        }

        var top = new T ();
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

        if (!Application.Initialized)
        {
            throw new NotInitializedException (nameof (Run));
        }

        if (Application.Driver == null)
        {
            throw new  InvalidOperationException ("Driver was inexplicably null when trying to Run view");
        }

        Application.Top = view;

        RunState rs = Application.Begin (view);

        Application.Top.Running = true;

        while (Application.TopLevels.TryPeek (out Toplevel? found) && found == view && view.Running)
        {
            if (_coordinator is null)
            {
                throw new ($"{nameof (IMainLoopCoordinator)} inexplicably became null during Run");
            }

            _coordinator.RunIteration ();
        }

        Logging.Information ($"Run - Calling End");
        Application.End (rs);
    }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    public void Shutdown ()
    {
        _coordinator?.Stop ();
        
        bool wasInitialized = Application.Initialized;
        Application.ResetState ();
        ConfigurationManager.PrintJsonErrors ();

        if (wasInitialized)
        {
            bool init = Application.Initialized;
            Application.OnInitializedChanged (this, new (in init));
        }

        Application.Driver = null;
        _lazyInstance = new (() => new ApplicationImpl ());
    }

    /// <inheritdoc />
    public void RequestStop (Toplevel? top)
    {
        Logging.Logger.LogInformation ($"RequestStop '{(top is {} ? top : "null")}'");

        top ??= Application.Top;

        if (top == null)
        {
            return;
        }

        var ev = new ToplevelClosingEventArgs (top);
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
        if (Application.MainThreadId == Thread.CurrentThread.ManagedThreadId)
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
    public void LayoutAndDraw (bool forceDraw)
    {
        Application.Top?.SetNeedsDraw();
        Application.Top?.SetNeedsLayout ();
    }
}
