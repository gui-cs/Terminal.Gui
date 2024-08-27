//
// MainLoop.cs: IMainLoopDriver and MainLoop for Terminal.Gui
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System.Collections.ObjectModel;

namespace Terminal.Gui;

/// <summary>Interface to create a platform specific <see cref="MainLoop"/> driver.</summary>
internal interface IMainLoopDriver
{
    /// <summary>Must report whether there are any events pending, or even block waiting for events.</summary>
    /// <returns><c>true</c>, if there were pending events, <c>false</c> otherwise.</returns>
    bool EventsPending ();

    /// <summary>The iteration function.</summary>
    void Iteration ();

    /// <summary>Initializes the <see cref="MainLoop"/>, gets the calling main loop for the initialization.</summary>
    /// <remarks>Call <see cref="TearDown"/> to release resources.</remarks>
    /// <param name="mainLoop">Main loop.</param>
    void Setup (MainLoop mainLoop);

    /// <summary>Tears down the <see cref="MainLoop"/> driver. Releases resources created in <see cref="Setup"/>.</summary>
    void TearDown ();

    /// <summary>Wakes up the <see cref="MainLoop"/> that might be waiting on input, must be thread safe.</summary>
    void Wakeup ();
}

/// <summary>The MainLoop monitors timers and idle handlers.</summary>
/// <remarks>
///     Monitoring of file descriptors is only available on Unix, there does not seem to be a way of supporting this
///     on Windows.
/// </remarks>
internal class MainLoop : IDisposable
{
    internal List<Func<bool>> _idleHandlers = new ();
    internal SortedList<long, Timeout> _timeouts = new ();

    /// <summary>The idle handlers and lock that must be held while manipulating them</summary>
    private readonly object _idleHandlersLock = new ();

    private readonly object _timeoutsLockToken = new ();

    /// <summary>Creates a new MainLoop.</summary>
    /// <remarks>Use <see cref="Dispose"/> to release resources.</remarks>
    /// <param name="driver">
    ///     The <see cref="ConsoleDriver"/> instance (one of the implementations FakeMainLoop, UnixMainLoop,
    ///     NetMainLoop or WindowsMainLoop).
    /// </param>
    internal MainLoop (IMainLoopDriver driver)
    {
        MainLoopDriver = driver;
        driver.Setup (this);
    }

    /// <summary>Gets a copy of the list of all idle handlers.</summary>
    internal ReadOnlyCollection<Func<bool>> IdleHandlers
    {
        get
        {
            lock (_idleHandlersLock)
            {
                return new List<Func<bool>> (_idleHandlers).AsReadOnly ();
            }
        }
    }

    /// <summary>The current <see cref="IMainLoopDriver"/> in use.</summary>
    /// <value>The main loop driver.</value>
    internal IMainLoopDriver MainLoopDriver { get; private set; }

    /// <summary>Used for unit tests.</summary>
    internal bool Running { get; set; }

    /// <summary>
    ///     Gets the list of all timeouts sorted by the <see cref="TimeSpan"/> time ticks. A shorter limit time can be
    ///     added at the end, but it will be called before an earlier addition that has a longer limit time.
    /// </summary>
    internal SortedList<long, Timeout> Timeouts => _timeouts;

    /// <inheritdoc/>
    public void Dispose ()
    {
        GC.SuppressFinalize (this);
        Stop ();
        Running = false;
        MainLoopDriver?.TearDown ();
        MainLoopDriver = null;
    }

    /// <summary>
    ///     Adds specified idle handler function to <see cref="MainLoop"/> processing. The handler function will be called
    ///     once per iteration of the main loop after other events have been handled.
    /// </summary>
    /// <remarks>
    ///     <para>Remove an idle handler by calling <see cref="RemoveIdle(Func{bool})"/> with the token this method returns.</para>
    ///     <para>
    ///         If the <paramref name="idleHandler"/> returns  <see langword="false"/> it will be removed and not called
    ///         subsequently.
    ///     </para>
    /// </remarks>
    /// <param name="idleHandler">Token that can be used to remove the idle handler with <see cref="RemoveIdle(Func{bool})"/> .</param>
    // QUESTION: Why are we re-inventing the event wheel here?
    // PERF: This is heavy.
    // CONCURRENCY: Race conditions exist here.
    // CONCURRENCY: null delegates will hose this.
    // 
    internal Func<bool> AddIdle (Func<bool> idleHandler)
    {
        lock (_idleHandlersLock)
        {
            _idleHandlers.Add (idleHandler);
        }

        MainLoopDriver.Wakeup ();

        return idleHandler;
    }

    /// <summary>Adds a timeout to the <see cref="MainLoop"/>.</summary>
    /// <remarks>
    ///     When time specified passes, the callback will be invoked. If the callback returns true, the timeout will be
    ///     reset, repeating the invocation. If it returns false, the timeout will stop and be removed. The returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="RemoveTimeout(object)"/>.
    /// </remarks>
    internal object AddTimeout (TimeSpan time, Func<bool> callback)
    {
        if (callback is null)
        {
            throw new ArgumentNullException (nameof (callback));
        }

        var timeout = new Timeout { Span = time, Callback = callback };
        AddTimeout (time, timeout);

        return timeout;
    }

    /// <summary>
    ///     Called from <see cref="IMainLoopDriver.EventsPending"/> to check if there are any outstanding timers or idle
    ///     handlers.
    /// </summary>
    /// <param name="waitTimeout">
    ///     Returns the number of milliseconds remaining in the current timer (if any). Will be -1 if
    ///     there are no active timers.
    /// </param>
    /// <returns><see langword="true"/> if there is a timer or idle handler active.</returns>
    internal bool CheckTimersAndIdleHandlers (out int waitTimeout)
    {
        long now = DateTime.UtcNow.Ticks;

        waitTimeout = 0;

        lock (_timeouts)
        {
            if (_timeouts.Count > 0)
            {
                waitTimeout = (int)((_timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);

                if (waitTimeout < 0)
                {
                    // This avoids 'poll' waiting infinitely if 'waitTimeout < 0' until some action is detected
                    // This can occur after IMainLoopDriver.Wakeup is executed where the pollTimeout is less than 0
                    // and no event occurred in elapsed time when the 'poll' is start running again.
                    waitTimeout = 0;
                }

                return true;
            }

            // ManualResetEventSlim.Wait, which is called by IMainLoopDriver.EventsPending, will wait indefinitely if
            // the timeout is -1.
            waitTimeout = -1;
        }

        // There are no timers set, check if there are any idle handlers

        lock (_idleHandlers)
        {
            return _idleHandlers.Count > 0;
        }
    }

    /// <summary>Determines whether there are pending events to be processed.</summary>
    /// <remarks>
    ///     You can use this method if you want to probe if events are pending. Typically used if you need to flush the
    ///     input queue while still running some of your own code in your main thread.
    /// </remarks>
    internal bool EventsPending () { return MainLoopDriver.EventsPending (); }

    /// <summary>Removes an idle handler added with <see cref="AddIdle(Func{bool})"/> from processing.</summary>
    /// <param name="token">A token returned by <see cref="AddIdle(Func{bool})"/></param>
    /// Returns
    /// <c>true</c>
    /// if the idle handler is successfully removed; otherwise,
    /// <c>false</c>
    /// .
    /// This method also returns
    /// <c>false</c>
    /// if the idle handler is not found.
    internal bool RemoveIdle (Func<bool> token)
    {
        lock (_idleHandlersLock)
        {
            return _idleHandlers.Remove (token);
        }
    }

    /// <summary>Removes a previously scheduled timeout</summary>
    /// <remarks>The token parameter is the value returned by AddTimeout.</remarks>
    /// Returns
    /// <c>true</c>
    /// if the timeout is successfully removed; otherwise,
    /// <c>false</c>
    /// .
    /// This method also returns
    /// <c>false</c>
    /// if the timeout is not found.
    internal bool RemoveTimeout (object token)
    {
        lock (_timeoutsLockToken)
        {
            int idx = _timeouts.IndexOfValue (token as Timeout);

            if (idx == -1)
            {
                return false;
            }

            _timeouts.RemoveAt (idx);
        }

        return true;
    }

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
    ///     Use this to process all pending events (timers, idle handlers and file watches).
    ///     <code>
    ///     while (main.EventsPending ()) RunIteration ();
    ///   </code>
    /// </remarks>
    internal void RunIteration ()
    {
        lock (_timeouts)
        {
            if (_timeouts.Count > 0)
            {
                RunTimers ();
            }
        }

        MainLoopDriver.Iteration ();

        var runIdle = false;

        lock (_idleHandlersLock)
        {
            runIdle = _idleHandlers.Count > 0;
        }

        if (runIdle)
        {
            RunIdle ();
        }
    }

    /// <summary>Stops the main loop driver and calls <see cref="IMainLoopDriver.Wakeup"/>. Used only for unit tests.</summary>
    internal void Stop ()
    {
        Running = false;
        Wakeup ();
    }

    /// <summary>
    ///     Invoked when a new timeout is added. To be used in the case when
    ///     <see cref="Application.EndAfterFirstIteration"/> is <see langword="true"/>.
    /// </summary>
    [CanBeNull]
    internal event EventHandler<TimeoutEventArgs> TimeoutAdded;

    /// <summary>Wakes up the <see cref="MainLoop"/> that might be waiting on input.</summary>
    internal void Wakeup () { MainLoopDriver?.Wakeup (); }

    private void AddTimeout (TimeSpan time, Timeout timeout)
    {
        lock (_timeoutsLockToken)
        {
            long k = (DateTime.UtcNow + time).Ticks;
            _timeouts.Add (NudgeToUniqueKey (k), timeout);
            TimeoutAdded?.Invoke (this, new TimeoutEventArgs (timeout, k));
        }
    }

    /// <summary>
    ///     Finds the closest number to <paramref name="k"/> that is not present in <see cref="_timeouts"/>
    ///     (incrementally).
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    private long NudgeToUniqueKey (long k)
    {
        lock (_timeoutsLockToken)
        {
            while (_timeouts.ContainsKey (k))
            {
                k++;
            }
        }

        return k;
    }

    // PERF: This is heavier than it looks.
    // CONCURRENCY: Potential deadlock city here.
    // CONCURRENCY: Multiple concurrency pitfalls on the delegates themselves.
    // INTENT: It looks like the general architecture here is trying to be a form of publisher/consumer pattern.
    private void RunIdle ()
    {
        List<Func<bool>> iterate;

        lock (_idleHandlersLock)
        {
            iterate = _idleHandlers;
            _idleHandlers = new List<Func<bool>> ();
        }

        foreach (Func<bool> idle in iterate)
        {
            if (idle ())
            {
                lock (_idleHandlersLock)
                {
                    _idleHandlers.Add (idle);
                }
            }
        }
    }

    private void RunTimers ()
    {
        long now = DateTime.UtcNow.Ticks;
        SortedList<long, Timeout> copy;

        // lock prevents new timeouts being added
        // after we have taken the copy but before
        // we have allocated a new list (which would
        // result in lost timeouts or errors during enumeration)
        lock (_timeoutsLockToken)
        {
            copy = _timeouts;
            _timeouts = new SortedList<long, Timeout> ();
        }

        foreach ((long k, Timeout timeout) in copy)
        {
            if (k < now)
            {
                if (timeout.Callback ())
                {
                    AddTimeout (timeout.Span, timeout);
                }
            }
            else
            {
                lock (_timeoutsLockToken)
                {
                    _timeouts.Add (NudgeToUniqueKey (k), timeout);
                }
            }
        }
    }
}
