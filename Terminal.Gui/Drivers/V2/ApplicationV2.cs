#nullable enable
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Implementation of <see cref="IApplication"/> that boots the new 'v2'
///     main loop architecture.
/// </summary>
public class ApplicationV2 : ApplicationImpl
{
    private readonly IComponentFactory? _componentFactory;
    private IMainLoopCoordinator? _coordinator;
    private string? _driverName;

    private readonly ITimedEvents _timedEvents = new TimedEvents ();

    /// <inheritdoc/>
    public override ITimedEvents TimedEvents => _timedEvents;

    internal IMainLoopCoordinator? Coordinator => _coordinator;

    /// <summary>
    ///     Creates anew instance of the Application backend. The provided
    ///     factory methods will be used on Init calls to get things booted.
    /// </summary>
    public ApplicationV2 ()
    {
        IsLegacy = false;
    }

    internal ApplicationV2 (IComponentFactory componentFactory)
    {
        _componentFactory = componentFactory;
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

        Debug.Assert(Application.Navigation is null);
        Application.Navigation = new ();

        Debug.Assert (Application.Popover is null);
        Application.Popover = new ();

        Application.AddKeyBindings ();

        // This is consistent with Application.ForceDriver which magnetically picks up driverName
        // making it use custom driver in future shutdown/init calls where no driver is specified
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

        bool definetlyWin = (driverName?.Contains ("win") ?? false) || _componentFactory is IComponentFactory<WindowsConsole.InputRecord>;
        bool definetlyNet = (driverName?.Contains ("net") ?? false) || _componentFactory is IComponentFactory<ConsoleKeyInfo>;
        bool definetlyUnix = (driverName?.Contains ("unix") ?? false) || _componentFactory is IComponentFactory<char>;

        if (definetlyWin)
        {
            _coordinator = CreateSubcomponents (() => new WindowsComponentFactory ());
        }
        else if (definetlyNet)
        {
            _coordinator = CreateSubcomponents (() => new NetComponentFactory ());
        }
        else if (definetlyUnix)
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
        MainLoop<T> loop = new ();

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
        Logging.Information ($"Run '{view}'");
        ArgumentNullException.ThrowIfNull (view);

        if (!Application.Initialized)
        {
            throw new NotInitializedException (nameof (Run));
        }

        if (Application.Driver == null)
        {
            // See Run_T_Init_Driver_Cleared_with_TestTopLevel_Throws
            throw new  InvalidOperationException ("Driver was inexplicably null when trying to Run view");
        }

        Application.Top = view;

        RunState rs = Application.Begin (view);

        Application.Top.Running = true;

        // QUESTION: how to know when we are done? - ANSWER: Running == false
        while (Application.TopLevels.TryPeek (out Toplevel? found) && found == view && view.Running)
        {
            if (_coordinator is null)
            {
                throw new ($"{nameof (IMainLoopCoordinator)}inexplicably became null during Run");
            }

            _coordinator.RunIteration ();
        }

        Logging.Information ($"Run - Calling End");
        Application.End (rs);
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

        // All RequestStop does is set the Running property to false - In the next iteration
        // this will be detected
        top.Running = false;
    }

    /// <inheritdoc/>
    public override void Invoke (Action action)
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

    /// <inheritdoc/>
    public override object AddTimeout (TimeSpan time, Func<bool> callback) { return _timedEvents.Add (time, callback); }

    /// <inheritdoc/>
    public override bool RemoveTimeout (object token) { return _timedEvents.Remove (token); }

    /// <inheritdoc />
    public override void LayoutAndDraw (bool forceDraw)
    {
        // No more ad-hoc drawing, you must wait for iteration to do it
        Application.Top?.SetNeedsDraw();
        Application.Top?.SetNeedsLayout ();
    }
}