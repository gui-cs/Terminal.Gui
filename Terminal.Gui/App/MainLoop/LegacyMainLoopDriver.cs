#nullable enable
//
// LegacyMainLoopDriver.cs: IMainLoopDriver and MainLoop for legacy v1 driver based applications
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System.Collections.ObjectModel;

namespace Terminal.Gui.App;

/// <summary>
///     The main event loop of legacy v1 driver based applications.
/// </summary>
/// <remarks>
///     <para>
///         This class is provided for backward compatibility with the legacy FakeDriver implementation.
///         New code should use the modern <see cref="ApplicationMainLoop{T}"/> architecture instead.
///     </para>
///     <para>
///         Monitoring of file descriptors is only available on Unix, there does not seem to be a way of supporting this
///         on Windows.
///     </para>
/// </remarks>
[Obsolete ("This class is for legacy FakeDriver compatibility only. Use ApplicationMainLoop<T> for new code.")]
public class MainLoop : IDisposable
{
    /// <summary>
    /// Gets the class responsible for handling timeouts
    /// </summary>
    public ITimedEvents TimedEvents { get; } = new TimedEvents();

    /// <summary>Creates a new MainLoop.</summary>
    /// <remarks>Use <see cref="Dispose"/> to release resources.</remarks>
    /// <param name="driver">
    ///     The <see cref="IConsoleDriver"/> instance (one of the implementations FakeMainLoop, UnixMainLoop,
    ///     NetMainLoop or WindowsMainLoop).
    /// </param>
    internal MainLoop (IMainLoopDriver driver)
    {
        MainLoopDriver = driver;
        driver.Setup (this);
    }


    /// <summary>The current <see cref="IMainLoopDriver"/> in use.</summary>
    /// <value>The main loop driver.</value>
    internal IMainLoopDriver? MainLoopDriver { get; private set; }

    /// <summary>Used for unit tests.</summary>
    internal bool Running { get; set; }


    /// <inheritdoc/>
    public void Dispose ()
    {
        GC.SuppressFinalize (this);
        Stop ();
        Running = false;
        MainLoopDriver?.TearDown ();
        MainLoopDriver = null;
    }


    /// <summary>Determines whether there are pending events to be processed.</summary>
    /// <remarks>
    ///     You can use this method if you want to probe if events are pending. Typically used if you need to flush the
    ///     input queue while still running some of your own code in your main thread.
    /// </remarks>
    internal bool EventsPending () { return MainLoopDriver!.EventsPending (); }


    /// <summary>Runs the <see cref="MainLoop"/>. Used only for unit tests.</summary>
    internal void Run ()
    {
        bool prev = Running;
        Running = true;

        while (Running)
        {
            EventsPending ();
            RunIteration ();
        }

        Running = prev;
    }

    /// <summary>Runs one iteration of timers and file watches</summary>
    /// <remarks>
    ///     Use this to process all pending events (timers handlers and file watches).
    ///     <code>
    ///     while (main.EventsPending ()) RunIteration ();
    ///   </code>
    /// </remarks>
    internal void RunIteration ()
    {
        RunAnsiScheduler ();

        MainLoopDriver?.Iteration ();

        TimedEvents.RunTimers ();
    }

    private void RunAnsiScheduler ()
    {
        Application.Driver?.GetRequestScheduler ().RunSchedule ();
    }

    /// <summary>Stops the main loop driver and calls <see cref="IMainLoopDriver.Wakeup"/>. Used only for unit tests.</summary>
    internal void Stop ()
    {
        Running = false;
        Wakeup ();
    }


    /// <summary>Wakes up the <see cref="MainLoop"/> that might be waiting on input.</summary>
    internal void Wakeup () { MainLoopDriver?.Wakeup (); }


}
