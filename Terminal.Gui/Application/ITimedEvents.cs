#nullable enable
using System.Collections.ObjectModel;

namespace Terminal.Gui;

/// <summary>
/// Manages timers and idles
/// </summary>
public interface ITimedEvents
{
    /// <summary>
    ///     Adds specified idle handler function to main iteration processing. The handler function will be called
    ///     once per iteration of the main loop after other events have been handled.
    /// </summary>
    /// <param name="idleHandler"></param>
    void AddIdle (Func<bool> idleHandler);

    /// <summary>
    /// Runs all idle hooks
    /// </summary>
    void LockAndRunIdles ();

    /// <summary>
    /// Runs all timeouts that are due
    /// </summary>
    void LockAndRunTimers ();

    /// <summary>
    ///     Called from <see cref="IMainLoopDriver.EventsPending"/> to check if there are any outstanding timers or idle
    ///     handlers.
    /// </summary>
    /// <param name="waitTimeout">
    ///     Returns the number of milliseconds remaining in the current timer (if any). Will be -1 if
    ///     there are no active timers.
    /// </param>
    /// <returns><see langword="true"/> if there is a timer or idle handler active.</returns>
    bool CheckTimersAndIdleHandlers (out int waitTimeout);

    /// <summary>Adds a timeout to the application.</summary>
    /// <remarks>
    ///     When time specified passes, the callback will be invoked. If the callback returns true, the timeout will be
    ///     reset, repeating the invocation. If it returns false, the timeout will stop and be removed. The returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="RemoveTimeout(object)"/>.
    /// </remarks>
    object AddTimeout (TimeSpan time, Func<bool> callback);

    /// <summary>Removes a previously scheduled timeout</summary>
    /// <remarks>The token parameter is the value returned by AddTimeout.</remarks>
    /// <returns>
    /// Returns
    /// <see langword="true"/>
    /// if the timeout is successfully removed; otherwise,
    /// <see langword="false"/>
    /// .
    /// This method also returns
    /// <see langword="false"/>
    /// if the timeout is not found.
    /// </returns>
    bool RemoveTimeout (object token);

    /// <summary>
    /// Returns all currently registered idles. May not include
    /// actively executing idles.
    /// </summary>
    ReadOnlyCollection<Func<bool>> IdleHandlers { get;}

    /// <summary>
    /// Returns the next planned execution time (key - UTC ticks)
    /// for each timeout that is not actively executing.
    /// </summary>
    SortedList<long, Timeout> Timeouts { get; }


    /// <summary>Removes an idle handler added with <see cref="AddIdle(Func{bool})"/> from processing.</summary>
    /// <returns>
    /// <see langword="true"/>
    /// if the idle handler is successfully removed; otherwise,
    /// <see langword="false"/>
    /// .
    /// This method also returns
    /// <see langword="false"/>
    /// if the idle handler is not found.</returns>
    bool RemoveIdle (Func<bool> fnTrue);

    /// <summary>
    ///     Invoked when a new timeout is added. To be used in the case when
    ///     <see cref="Application.EndAfterFirstIteration"/> is <see langword="true"/>.
    /// </summary>
    event EventHandler<TimeoutEventArgs>? TimeoutAdded;
}
