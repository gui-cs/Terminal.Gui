#nullable enable
namespace Terminal.Gui.App;

/// <summary>
///     Manages timers.
/// </summary>
public interface ITimedEvents
{
    /// <summary>
    ///     Adds a timeout to the application.
    /// </summary>
    /// <remarks>
    ///     When the specified time passes, the callback will be invoked. If the callback returns <see langword="true"/>, the
    ///     timeout will be
    ///     reset, repeating the invocation. If it returns <see langword="false"/>, the timeout will stop and be removed. The
    ///     returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="Remove"/>.
    /// </remarks>
    object Add (TimeSpan time, Func<bool> callback);

    /// <inheritdoc cref="Add(System.TimeSpan,System.Func{bool})"/>
    object Add (Timeout timeout);

    /// <summary>
    ///     Invoked when a new timeout is added. To be used in the case when
    ///     <see cref="Application.EndAfterFirstIteration"/> is <see langword="true"/>.
    /// </summary>
    event EventHandler<TimeoutEventArgs>? Added;

    /// <summary>
    ///     Called from <see cref="IMainLoopDriver.EventsPending"/> to check if there are any outstanding timer handlers.
    /// </summary>
    /// <param name="waitTimeout">
    ///     Returns the number of milliseconds remaining in the current timer (if any). Will be -1 if
    ///     there are no active timers.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if there is a timer active; otherwise, <see langword="false"/>.
    /// </returns>
    bool CheckTimers (out int waitTimeout);

    /// <summary>
    ///     Removes a previously scheduled timeout.
    /// </summary>
    /// <remarks>
    ///     The token parameter is the value returned by <see cref="Add(TimeSpan, Func{bool})"/> or <see cref="Add(Timeout)"/>.
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the timeout is successfully removed; otherwise, <see langword="false"/>.
    ///     This method also returns <see langword="false"/> if the timeout is not found.
    /// </returns>
    bool Remove (object token);

    /// <summary>
    ///     Runs all timeouts that are due.
    /// </summary>
    void RunTimers ();

    /// <summary>
    ///     Returns the next planned execution time (key - UTC ticks)
    ///     for each timeout that is not actively executing.
    /// </summary>
    SortedList<long, Timeout> Timeouts { get; }
}
