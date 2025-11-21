namespace Terminal.Gui.App;

/// <summary>
///     Non-generic base interface for runnable views. Provides common members without type parameter.
/// </summary>
/// <remarks>
///     <para>
///         This interface enables storing heterogeneous runnables in collections (e.g.,
///         <see cref="IApplication.RunnableSessionStack"/>)
///         while preserving type safety at usage sites via <see cref="IRunnable{TResult}"/>.
///     </para>
///     <para>
///         Most code should use <see cref="IRunnable{TResult}"/> directly. This base interface is primarily
///         for framework infrastructure (session management, stacking, etc.).
///     </para>
///     <para>
///         A runnable view executes as a self-contained blocking session with its own lifecycle,
///         event loop iteration, and focus management./>
///         blocks until
///         <see cref="IApplication.RequestStop()"/> is called.
///     </para>
///     <para>
///         This interface follows the Terminal.Gui Cancellable Work Pattern (CWP) for all lifecycle events.
///     </para>
/// </remarks>
/// <seealso cref="IRunnable{TResult}"/>
/// <seealso cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>
public interface IRunnable
{
    #region Running or not (added to/removed from RunnableSessionStack)

    /// <summary>
    ///     Gets whether this runnable session is currently running (i.e., on the
    ///     <see cref="IApplication.RunnableSessionStack"/>).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Read-only property derived from stack state. Returns <see langword="true"/> if this runnable
    ///         is currently on the <see cref="IApplication.RunnableSessionStack"/>, <see langword="false"/> otherwise.
    ///     </para>
    ///     <para>
    ///         Runnables are added to the stack during <see cref="IApplication.Begin(IRunnable)"/> and removed in
    ///         <see cref="IApplication.End(RunnableSessionToken)"/>.
    ///     </para>
    /// </remarks>
    bool IsRunning { get; }

    /// <summary>
    ///     Called by the framework to raise the <see cref="IsRunningChanging"/> event.
    /// </summary>
    /// <param name="oldIsRunning">The current value of <see cref="IsRunning"/>.</param>
    /// <param name="newIsRunning">The new value of <see cref="IsRunning"/> (true = starting, false = stopping).</param>
    /// <returns><see langword="true"/> if the change was canceled; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    ///     <para>
    ///         This method implements the Cancellable Work Pattern. It calls the protected virtual method first,
    ///         then raises the event if not canceled.
    ///     </para>
    ///     <para>
    ///         When <paramref name="newIsRunning"/> is <see langword="false"/> (stopping), this is the ideal place
    ///         for implementations to extract <c>Result</c> from views before the runnable is removed from the stack.
    ///     </para>
    /// </remarks>
    bool RaiseIsRunningChanging (bool oldIsRunning, bool newIsRunning);

    /// <summary>
    ///     Raised when <see cref="IsRunning"/> is changing (e.g., when <see cref="IApplication.Begin(IRunnable)"/> or
    ///     <see cref="IApplication.End(RunnableSessionToken)"/> is called).
    ///     Can be canceled by setting <see cref="CancelEventArgs{T}.Cancel"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Subscribe to this event to participate in the runnable lifecycle before state changes occur.
    ///         When <see cref="CancelEventArgs{T}.NewValue"/> is <see langword="false"/> (stopping),
    ///         this is the ideal place to extract <c>Result</c> before views are disposed and to optionally
    ///         cancel the stop operation (e.g., prompt to save changes).
    ///     </para>
    ///     <para>
    ///         This event follows the Terminal.Gui Cancellable Work Pattern (CWP).
    ///     </para>
    /// </remarks>
    event EventHandler<CancelEventArgs<bool>>? IsRunningChanging;

    /// <summary>
    ///     Called by the framework to raise the <see cref="IsRunningChanged"/> event.
    /// </summary>
    /// <param name="newIsRunning">The new value of <see cref="IsRunning"/> (true = started, false = stopped).</param>
    /// <remarks>
    ///     This method is called after the state change has occurred and cannot be canceled.
    /// </remarks>
    void RaiseIsRunningChangedEvent (bool newIsRunning);

    /// <summary>
    ///     Raised after <see cref="IsRunning"/> has changed (after the runnable has been added to or removed from the
    ///     <see cref="IApplication.RunnableSessionStack"/>).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Subscribe to this event to perform post-state-change logic. When <see cref="EventArgs{T}.Value"/> is
    ///         <see langword="true"/>,
    ///         the runnable has started and is on the stack. When <see langword="false"/>, the runnable has stopped and been
    ///         removed from the stack.
    ///     </para>
    ///     <para>
    ///         This event follows the Terminal.Gui Cancellable Work Pattern (CWP).
    ///     </para>
    /// </remarks>
    event EventHandler<EventArgs<bool>>? IsRunningChanged;

    #endregion Running or not (added to/removed from RunnableSessionStack)

    #region Modal or not (top of RunnableSessionStack or not)

    /// <summary>
    ///     Gets whether this runnable session is at the top of the <see cref="IApplication.RunnableSessionStack"/> and thus
    ///     exclusively receiving mouse and keyboard input.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Read-only property derived from stack state. Returns <see langword="true"/> if this runnable
    ///         is at the top of the stack (i.e., <c>this == app.TopRunnable</c>), <see langword="false"/> otherwise.
    ///     </para>
    ///     <para>
    ///         The runnable at the top of the stack gets all mouse/keyboard input and thus is running "modally".
    ///     </para>
    /// </remarks>
    bool IsModal { get; }

    /// <summary>
    ///     Called by the framework to raise the <see cref="IsModalChanging"/> event.
    /// </summary>
    /// <param name="oldIsModal">The current value of <see cref="IsModal"/>.</param>
    /// <param name="newIsModal">The new value of <see cref="IsModal"/> (true = becoming modal/top, false = no longer modal).</param>
    /// <returns><see langword="true"/> if the change was canceled; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    ///     This method implements the Cancellable Work Pattern. It calls the protected virtual method first,
    ///     then raises the event if not canceled.
    /// </remarks>
    bool RaiseIsModalChanging (bool oldIsModal, bool newIsModal);

    /// <summary>
    ///     Raised when this runnable is about to become modal (top of stack) or cease being modal.
    ///     Can be canceled by setting <see cref="CancelEventArgs{T}.Cancel"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Subscribe to this event to participate in modal state transitions before they occur.
    ///         When <see cref="CancelEventArgs{T}.NewValue"/> is <see langword="true"/>, the runnable is becoming modal (top
    ///         of stack).
    ///         When <see langword="false"/>, another runnable is becoming modal and this one will no longer receive input.
    ///     </para>
    ///     <para>
    ///         This event follows the Terminal.Gui Cancellable Work Pattern (CWP).
    ///     </para>
    /// </remarks>
    event EventHandler<CancelEventArgs<bool>>? IsModalChanging;

    /// <summary>
    ///     Called by the framework to raise the <see cref="IsModalChanged"/> event.
    /// </summary>
    /// <param name="newIsModal">The new value of <see cref="IsModal"/> (true = became modal/top, false = no longer modal).</param>
    /// <remarks>
    ///     This method is called after the modal state change has occurred and cannot be canceled.
    /// </remarks>
    void RaiseIsModalChangedEvent (bool newIsModal);

    /// <summary>
    ///     Raised after this runnable has become modal (top of stack) or ceased being modal.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Subscribe to this event to perform post-activation logic (e.g., setting focus, updating UI state).
    ///         When <see cref="EventArgs{T}.Value"/> is <see langword="true"/>, the runnable became modal (top of
    ///         stack).
    ///         When <see langword="false"/>, the runnable is no longer modal (another runnable is on top).
    ///     </para>
    ///     <para>
    ///         This event follows the Terminal.Gui Cancellable Work Pattern (CWP).
    ///     </para>
    /// </remarks>
    event EventHandler<EventArgs<bool>>? IsModalChanged;

    #endregion Modal or not (top of RunnableSessionStack or not)
}

/// <summary>
///     Defines a view that can be run as an independent blocking session with <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>,
///     returning a typed result.
/// </summary>
/// <typeparam name="TResult">
///     The type of result data returned when the session completes.
///     Common types: <see cref="int"/> for button indices, <see cref="string"/> for file paths,
///     custom types for complex form data.
/// </typeparam>
/// <remarks>
///     <para>
///         A runnable view executes as a self-contained blocking session with its own lifecycle,
///         event loop iteration, and focus management. <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/> blocks until
///         <see cref="IApplication.RequestStop()"/> is called.
///     </para>
///     <para>
///         When <see cref="Result"/> is <see langword="null"/>, the session was stopped without being accepted
///         (e.g., ESC key pressed, window closed). When non-<see langword="null"/>, it contains the result data
///         extracted in <see cref="IRunnable.RaiseIsRunningChanging"/> (when stopping) before views are disposed.
///     </para>
///     <para>
///         Implementing <see cref="IRunnable{TResult}"/> does not require deriving from any specific
///         base class or using <see cref="ViewArrangement.Overlapped"/>. These are orthogonal concerns.
///     </para>
///     <para>
///         This interface follows the Terminal.Gui Cancellable Work Pattern (CWP) for all lifecycle events.
///     </para>
/// </remarks>
/// <seealso cref="IRunnable"/>
/// <seealso cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>
public interface IRunnable<TResult> : IRunnable
{
    /// <summary>
    ///     Gets or sets the result data extracted when the session was accepted, or <see langword="null"/> if not accepted.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Implementations should set this in the <see cref="IRunnable.RaiseIsRunningChanging"/> method
    ///         (when stopping, i.e., <c>newIsRunning == false</c>) by extracting data from
    ///         views before they are disposed.
    ///     </para>
    ///     <para>
    ///         <see langword="null"/> indicates the session was stopped without accepting (ESC key, close without action).
    ///         Non-<see langword="null"/> contains the type-safe result data.
    ///     </para>
    /// </remarks>
    TResult? Result { get; set; }
}
