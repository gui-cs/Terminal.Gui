#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

/// <summary>
///     Implementation of <see cref="IApplication"/> that boots the new 'v2'
///     main loop architecture.
/// </summary>
public class ApplicationV2 : ApplicationImpl
{
    private readonly Func<INetInput> _netInputFactory;
    private readonly Func<IConsoleOutput> _netOutputFactory;
    private readonly Func<IWindowsInput> _winInputFactory;
    private readonly Func<IConsoleOutput> _winOutputFactory;
    private IMainLoopCoordinator? _coordinator;
    private string? _driverName;

    private readonly ITimedEvents _timedEvents = new TimedEvents ();

    /// <summary>
    ///     Creates anew instance of the Application backend. The provided
    ///     factory methods will be used on Init calls to get things booted.
    /// </summary>
    public ApplicationV2 () : this (
                                    () => new NetInput (),
                                    () => new NetOutput (),
                                    () => new WindowsInput (),
                                    () => new WindowsOutput ()
                                   )
    { }

    internal ApplicationV2 (
        Func<INetInput> netInputFactory,
        Func<IConsoleOutput> netOutputFactory,
        Func<IWindowsInput> winInputFactory,
        Func<IConsoleOutput> winOutputFactory
    )
    {
        _netInputFactory = netInputFactory;
        _netOutputFactory = netOutputFactory;
        _winInputFactory = winInputFactory;
        _winOutputFactory = winOutputFactory;
        IsLegacy = false;
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public override void Init (IConsoleDriver? driver = null, string? driverName = null)
    {
        if (Application.Initialized)
        {
            Logging.Logger.LogError ("Init called multiple times without shutdown, ignoring.");

            return;
        }

        if (!string.IsNullOrWhiteSpace (driverName))
        {
            _driverName = driverName;
        }

        Application.Navigation = new ();

        Application.AddKeyBindings ();

        // This is consistent with Application.ForceDriver which magnetically picks up driverName
        // making it use custom driver in future shutdown/init calls where no driver is specified
        CreateDriver (driverName ?? _driverName);

        Application.InitializeConfigurationManagement ();

        Application.Initialized = true;

        Application.OnInitializedChanged (this, new (true));
        Application.SubscribeDriverEvents ();

        SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext ());
        Application.MainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    private void CreateDriver (string? driverName)
    {
        PlatformID p = Environment.OSVersion.Platform;

        bool definetlyWin = driverName?.Contains ("win") ?? false;
        bool definetlyNet = driverName?.Contains ("net") ?? false;

        if (definetlyWin)
        {
            _coordinator = CreateWindowsSubcomponents ();
        }
        else if (definetlyNet)
        {
            _coordinator = CreateNetSubcomponents ();
        }
        else if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            _coordinator = CreateWindowsSubcomponents ();
        }
        else
        {
            _coordinator = CreateNetSubcomponents ();
        }

        _coordinator.StartAsync ().Wait ();

        if (Application.Driver == null)
        {
            throw new ("Application.Driver was null even after booting MainLoopCoordinator");
        }
    }

    private IMainLoopCoordinator CreateWindowsSubcomponents ()
    {
        ConcurrentQueue<WindowsConsole.InputRecord> inputBuffer = new ();
        MainLoop<WindowsConsole.InputRecord> loop = new ();

        return new MainLoopCoordinator<WindowsConsole.InputRecord> (
                                                                    _timedEvents,
                                                                    _winInputFactory,
                                                                    inputBuffer,
                                                                    new WindowsInputProcessor (inputBuffer),
                                                                    _winOutputFactory,
                                                                    loop);
    }

    private IMainLoopCoordinator CreateNetSubcomponents ()
    {
        ConcurrentQueue<ConsoleKeyInfo> inputBuffer = new ();
        MainLoop<ConsoleKeyInfo> loop = new ();

        return new MainLoopCoordinator<ConsoleKeyInfo> (
                                                        _timedEvents,
                                                        _netInputFactory,
                                                        inputBuffer,
                                                        new NetInputProcessor (inputBuffer),
                                                        _netOutputFactory,
                                                        loop);
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public override T Run<T> (Func<Exception, bool>? errorHandler = null, IConsoleDriver? driver = null)
    {
        var top = new T ();

        Run (top, errorHandler);

        return top;
    }

    /// <inheritdoc/>
    public override void Run (Toplevel view, Func<Exception, bool>? errorHandler = null)
    {
        Logging.Logger.LogInformation ($"Run '{view}'");
        ArgumentNullException.ThrowIfNull (view);

        if (!Application.Initialized)
        {
            throw new NotInitializedException (nameof (Run));
        }

        Application.Top = view;

        Application.Begin (view);

        // TODO : how to know when we are done?
        while (Application.TopLevels.TryPeek (out Toplevel? found) && found == view)
        {
            if (_coordinator is null)
            {
                throw new ($"{nameof (IMainLoopCoordinator)}inexplicably became null during Run");
            }

            _coordinator.RunIteration ();
        }
    }

    /// <inheritdoc/>
    public override void Shutdown ()
    {
        _coordinator?.Stop ();
        base.Shutdown ();
        Application.Driver = null;
    }

    /// <inheritdoc/>
    public override void RequestStop (Toplevel? top)
    {
        Logging.Logger.LogInformation ($"RequestStop '{top}'");

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

        // TODO: This definition of stop seems sketchy
        Application.TopLevels.TryPop (out _);

        if (Application.TopLevels.Count > 0)
        {
            Application.Top = Application.TopLevels.Peek ();
        }
        else
        {
            Application.Top = null;
        }

        // Notify that it is closed
        top.OnClosed (top);
    }

    /// <inheritdoc/>
    public override void Invoke (Action action)
    {
        _timedEvents.AddIdle (
                              () =>
                              {
                                  action ();

                                  return false;
                              }
                             );
    }

    /// <inheritdoc/>
    public override void AddIdle (Func<bool> func) { _timedEvents.AddIdle (func); }

    /// <summary>
    ///     Removes an idle function added by <see cref="AddIdle"/>
    /// </summary>
    /// <param name="fnTrue">Function to remove</param>
    /// <returns>True if it was found and removed</returns>
    public bool RemoveIdle (Func<bool> fnTrue) { return _timedEvents.RemoveIdle (fnTrue); }

    /// <inheritdoc/>
    public override object AddTimeout (TimeSpan time, Func<bool> callback) { return _timedEvents.AddTimeout (time, callback); }

    /// <inheritdoc/>
    public override bool RemoveTimeout (object token) { return _timedEvents.RemoveTimeout (token); }

    /// <inheritdoc />
    public override void LayoutAndDraw (bool forceDraw)
    {
        // No more ad-hoc drawing, you must wait for iteration to do it
        Application.Top?.SetNeedsDraw();
        Application.Top?.SetNeedsLayout ();
    }
}
