namespace Terminal.Gui.App;

/// <summary>
///     Non-generic base interface for runnable views. Provides common members without type parameter.
/// </summary>
/// <remarks>
///     <para>
///         This interface enables storing heterogeneous runnables in collections (e.g.,
///         <see cref="IApplication.SessionStack"/>)
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
    #region Result

    /// <summary>
    ///     Gets or sets the result data extracted when the session was accepted, or <see langword="null"/> if not accepted.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is the non-generic version of the result property. For type-safe access, cast to
    ///         <see cref="IRunnable{TResult}"/> or access the derived interface's <c>Result</c> property directly.
    ///     </para>
    ///     <para>
    ///         Implementations should set this in the <see cref="RaiseIsRunningChanging"/> method
    ///         (when stopping, i.e., <c>newIsRunning == false</c>) by extracting data from
    ///         views before they are disposed.
    ///     </para>
    ///     <para>
    ///         <see langword="null"/> indicates the session was stopped without accepting (ESC key, close without action).
    ///         Non-<see langword="null"/> contains the result data.
    ///     </para>
    /// </remarks>
    object? Result { get; set; }

    #endregion Result

    #region Running or not (added to/removed from RunnableSessionStack)

    /// <summary>
    ///     Sets the application context for this runnable. Called from <see cref="IApplication.Begin(IRunnable)"/>.
    /// </summary>
    /// <param name="app"></param>
    void SetApp (IApplication app);

    /// <summary>
    ///     Gets whether this runnable session is currently running (i.e., on the
    ///     <see cref="IApplication.SessionStack"/>).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property returns a cached value that is updated atomically when the runnable is added to or
    ///         removed from the session stack. The cached state ensures thread-safe access without race conditions.
    ///     </para>
    ///     <para>
    ///         Returns <see langword="true"/> if this runnable is currently on the <see cref="IApplication.SessionStack"/>,
    ///         <see langword="false"/> otherwise.
    ///     </para>
    ///     <para>
    ///         Runnables are added to the stack during <see cref="IApplication.Begin(IRunnable)"/> and removed in
    ///         <see cref="IApplication.End(SessionToken)"/>.
    ///     </para>
    /// </remarks>
    bool IsRunning { get; }

    /// <summary>
    ///     Sets the cached IsRunning state. Called by ApplicationImpl within the session stack lock.
    ///     This method is internal to the framework and should not be called by application code.
    /// </summary>
    /// <param name="value">The new IsRunning value.</param>
    void SetIsRunning (bool value);

    /// <summary>
    ///     Requests that this runnable session stop.
    /// </summary>
    public void RequestStop ();

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
    ///     <see cref="IApplication.End(SessionToken)"/> is called).
    ///     Can be canceled by setting `args.Cancel` to <see langword="true"/>.
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
    ///     <see cref="IApplication.SessionStack"/>).
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
    ///     Gets whether this runnable session is at the top of the <see cref="IApplication.SessionStack"/> and thus
    ///     exclusively receiving mouse and keyboard input.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property returns a cached value that is updated atomically when the runnable's modal state changes.
    ///         The cached state ensures thread-safe access without race conditions.
    ///     </para>
    ///     <para>
    ///         Returns <see langword="true"/> if this runnable is at the top of the stack (i.e., <c>this == app.TopRunnable</c>),
    ///         <see langword="false"/> otherwise.
    ///     </para>
    ///     <para>
    ///         The runnable at the top of the stack gets all mouse/keyboard input and thus is running "modally".
    ///     </para>
    /// </remarks>
    bool IsModal { get; }

    /// <summary>
    ///     Sets the cached IsModal state. Called by ApplicationImpl within the session stack lock.
    ///     This method is internal to the framework and should not be called by application code.
    /// </summary>
    /// <param name="value">The new IsModal value.</param>
    void SetIsModal (bool value);

    /// <summary>
    ///     Gets or sets whether a stop has been requested for this runnable session.
    /// </summary>
    bool StopRequested { get; set; }

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
    ///     Gets or sets the input data used to initialize the session state.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set this property before calling <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>
    ///         to initialize the runnable's state. This is useful for prompts and dialogs that need to show
    ///         a default value or pre-populate fields.
    ///     </para>
    ///     <para>
    ///         <see langword="null"/> indicates no input data was provided.
    ///         Non-<see langword="null"/> contains the type-safe initialization data.
    ///     </para>
    /// </remarks>
    new TResult? Input { get; set; }

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
    new TResult? Result { get; set; }
}
