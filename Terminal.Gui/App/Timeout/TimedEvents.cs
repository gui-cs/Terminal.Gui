#nullable enable
namespace Terminal.Gui.App;

/// <summary>
///     Manages scheduled timeouts (timed callbacks) for the application.
///     <para>
///         Allows scheduling of callbacks to be invoked after a specified delay, with optional repetition.
///         Timeouts are stored in a sorted list by their scheduled execution time (UTC ticks).
///         Thread-safe for concurrent access.
///     </para>
///     <para>
///         Typical usage:
///         <list type="number">
///             <item>
///                 <description>Call <see cref="Add(TimeSpan, Func{bool})"/> to schedule a callback.</description>
///             </item>
///             <item>
///                 <description>
///                     Call <see cref="RunTimers"/> periodically (e.g., from the main loop) to execute due
///                     callbacks.
///                 </description>
///             </item>
///             <item>
///                 <description>Call <see cref="Remove"/> to cancel a scheduled timeout.</description>
///             </item>
///         </list>
///     </para>
/// </summary>
public class TimedEvents : ITimedEvents
{
    internal SortedList<long, Timeout> _timeouts = new ();
    private readonly object _timeoutsLockToken = new ();

    /// <summary>
    ///     Gets the list of all timeouts sorted by the <see cref="TimeSpan"/> time ticks. A shorter limit time can be
    ///     added at the end, but it will be called before an earlier addition that has a longer limit time.
    /// </summary>
    public SortedList<long, Timeout> Timeouts => _timeouts;

    /// <inheritdoc/>
    public event EventHandler<TimeoutEventArgs>? Added;

    /// <inheritdoc/>
    public void RunTimers ()
    {
        lock (_timeoutsLockToken)
        {
            if (_timeouts.Count > 0)
            {
                RunTimersImpl ();
            }
        }
    }

    /// <inheritdoc/>
    public bool Remove (object token)
    {
        lock (_timeoutsLockToken)
        {
            int idx = _timeouts.IndexOfValue ((token as Timeout)!);

            if (idx == -1)
            {
                return false;
            }

            _timeouts.RemoveAt (idx);
        }

        return true;
    }

    /// <inheritdoc/>
    public object Add (TimeSpan time, Func<bool> callback)
    {
        ArgumentNullException.ThrowIfNull (callback);

        var timeout = new Timeout { Span = time, Callback = callback };
        AddTimeout (time, timeout);

        return timeout;
    }

    /// <inheritdoc/>
    public object Add (Timeout timeout)
    {
        AddTimeout (timeout.Span, timeout);

        return timeout;
    }

    /// <inheritdoc/>
    public bool CheckTimers (out int waitTimeout)
    {
        long now = DateTime.UtcNow.Ticks;

        waitTimeout = 0;

        lock (_timeoutsLockToken)
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

        return false;
    }

    private void AddTimeout (TimeSpan time, Timeout timeout)
    {
        lock (_timeoutsLockToken)
        {
            long k = (DateTime.UtcNow + time).Ticks;

            // if user wants to run as soon as possible set timer such that it expires right away (no race conditions)
            if (time == TimeSpan.Zero)
            {
                k -= 100;
            }

            _timeouts.Add (NudgeToUniqueKey (k), timeout);
            Added?.Invoke (this, new (timeout, k));
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

    private void RunTimersImpl ()
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
            _timeouts = new ();
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
