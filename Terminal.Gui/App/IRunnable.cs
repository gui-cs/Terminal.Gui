namespace Terminal.Gui.App;

/// <summary>
/// Defines a view that can be run as an independent session with <see cref="IApplication.Run"/>.
/// </summary>
/// <remarks>
/// <para>
/// A runnable view can execute as a self-contained session with its own lifecycle,
/// event loop iteration, and focus management.
/// </para>
/// <para>
/// Implementing <see cref="IRunnable"/> does not require any specific view hierarchy
/// (e.g., deriving from Toplevel) or layout mode (e.g., Overlapped).
/// </para>
/// <para>
/// For exclusive input capture (modal behavior), implement <see cref="IModalRunnable{TResult}"/>.
/// </para>
/// <para>
/// This interface follows the Terminal.Gui Cancellable Work Pattern (CWP) for lifecycle events
/// where cancellation makes sense (Stopping, Activating, Deactivating).
/// </para>
/// </remarks>
public interface IRunnable
{
    /// <summary>
    /// Gets or sets whether this runnable session is currently running.
    /// </summary>
    /// <remarks>
    /// This property is set by the framework during session lifecycle. Setting this property
    /// directly is discouraged. Use <see cref="IApplication.RequestStop"/> instead.
    /// </remarks>
    bool Running { get; set; }

    #region Lifecycle Methods (Called by IApplication)

    /// <summary>
    /// Raises the <see cref="Stopping"/> and <see cref="Stopped"/> events.
    /// Called by <see cref="IApplication.RequestStop"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the Cancellable Work Pattern for stopping:
    /// 1. Calls virtual method (can cancel)
    /// 2. Raises <see cref="Stopping"/> event (can cancel)
    /// 3. If not canceled, sets <see cref="Running"/> = false
    /// 4. Calls post-notification virtual method
    /// 5. Raises <see cref="Stopped"/> event
    /// </para>
    /// <para>
    /// Implementations should follow this pattern. See <see cref="Runnable"/> for reference implementation.
    /// </para>
    /// </remarks>
    void RaiseStoppingEvent ();

    /// <summary>
    /// Raises the <see cref="Activating"/> event.
    /// Called by <see cref="IApplication.Begin"/> when this runnable is becoming the active session.
    /// </summary>
    /// <param name="deactivated">The previously active runnable being deactivated, or null if none.</param>
    /// <returns><see langword="true"/> if activation was canceled; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method implements the Cancellable Work Pattern for activation:
    /// 1. Calls virtual method (can cancel)
    /// 2. Raises <see cref="Activating"/> event (can cancel)
    /// 3. If canceled, returns true
    /// 4. If not canceled, calls <see cref="RaiseActivatedEvent"/> and returns false
    /// </para>
    /// </remarks>
    bool RaiseActivatingEvent (IRunnable? deactivated);

    /// <summary>
    /// Raises the <see cref="Activated"/> event.
    /// Called by <see cref="RaiseActivatingEvent"/> after activation succeeds.
    /// </summary>
    /// <param name="deactivated">The previously active runnable that was deactivated, or null if none.</param>
    /// <remarks>
    /// This is the post-notification phase of activation. Implementations should raise the
    /// <see cref="Activated"/> event.
    /// </remarks>
    void RaiseActivatedEvent (IRunnable? deactivated);

    /// <summary>
    /// Raises the <see cref="Deactivating"/> event.
    /// Called by <see cref="IApplication.Begin"/> when switching to a new runnable.
    /// </summary>
    /// <param name="activated">The newly activated runnable, or null if none.</param>
    /// <returns><see langword="true"/> if deactivation was canceled; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method implements the Cancellable Work Pattern for deactivation:
    /// 1. Calls virtual method (can cancel)
    /// 2. Raises <see cref="Deactivating"/> event (can cancel)
    /// 3. If canceled, returns true
    /// 4. If not canceled, calls <see cref="RaiseDeactivatedEvent"/> and returns false
    /// </para>
    /// </remarks>
    bool RaiseDeactivatingEvent (IRunnable? activated);

    /// <summary>
    /// Raises the <see cref="Deactivated"/> event.
    /// Called by <see cref="RaiseDeactivatingEvent"/> after deactivation succeeds.
    /// </summary>
    /// <param name="activated">The newly activated runnable, or null if none.</param>
    /// <remarks>
    /// This is the post-notification phase of deactivation. Implementations should raise the
    /// <see cref="Deactivated"/> event.
    /// </remarks>
    void RaiseDeactivatedEvent (IRunnable? activated);

    #endregion

    #region Lifecycle Events

    /// <summary>
    /// Raised during <see cref="ISupportInitialize.EndInit"/> before initialization completes.
    /// Can be canceled by setting <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is from the <see cref="ISupportInitialize"/> pattern and is raised by <see cref="View.EndInit"/>.
    /// Subscribe to this event to perform pre-initialization work or to cancel initialization.
    /// </para>
    /// <para>
    /// This event follows the Terminal.Gui Cancellable Work Pattern.
    /// </para>
    /// </remarks>
    event EventHandler<System.ComponentModel.CancelEventArgs>? Initializing;

    /// <summary>
    /// Raised after the runnable has been initialized (via <see cref="ISupportInitialize.BeginInit"/>/<see cref="ISupportInitialize.EndInit"/>).
    /// The view is laid out and ready to be drawn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is from the <see cref="ISupportInitialize"/> pattern and is raised by <see cref="View.EndInit"/>.
    /// Subscribe to this event to perform initialization that requires the view to be fully laid out.
    /// </para>
    /// <para>
    /// This is the post-notification event in the Cancellable Work Pattern pair with <see cref="Initializing"/>.
    /// </para>
    /// </remarks>
    event EventHandler? Initialized;

    /// <summary>
    /// Raised when <see cref="IApplication.RequestStop"/> is called on this runnable.
    /// Can be canceled by setting <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Subscribe to this event to prevent the runnable from stopping (e.g., to prompt the user
    /// to save changes). If canceled, <see cref="Running"/> will remain <see langword="true"/>.
    /// </remarks>
    event EventHandler<System.ComponentModel.CancelEventArgs>? Stopping;

    /// <summary>
    /// Raised after the runnable session has stopped (<see cref="Running"/> = <see langword="false"/>).
    /// </summary>
    /// <remarks>
    /// This is the post-notification event in the Cancellable Work Pattern pair with <see cref="Stopping"/>.
    /// Subscribe to this event for cleanup work that should occur after the session stops.
    /// </remarks>
    event EventHandler? Stopped;

    /// <summary>
    /// Raised when the runnable session is about to become active (the current session).
    /// Can be canceled by setting <see cref="RunnableActivatingEventArgs.Cancel"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Subscribe to this event to prevent activation or to perform pre-activation work.
    /// </remarks>
    event EventHandler<RunnableActivatingEventArgs>? Activating;

    /// <summary>
    /// Raised when the runnable session has become active.
    /// </summary>
    /// <remarks>
    /// This is the post-notification event in the Cancellable Work Pattern pair with <see cref="Activating"/>.
    /// Subscribe to this event for post-activation logic (e.g., setting focus).
    /// </remarks>
    event EventHandler<RunnableEventArgs>? Activated;

    /// <summary>
    /// Raised when the runnable session is about to cease being active (another session is activating).
    /// Can be canceled by setting <see cref="RunnableDeactivatingEventArgs.Cancel"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Subscribe to this event to prevent deactivation or to perform pre-deactivation work.
    /// </remarks>
    event EventHandler<RunnableDeactivatingEventArgs>? Deactivating;

    /// <summary>
    /// Raised when the runnable session has ceased being active.
    /// </summary>
    /// <remarks>
    /// This is the post-notification event in the Cancellable Work Pattern pair with <see cref="Deactivating"/>.
    /// Subscribe to this event for cleanup or state preservation after deactivation.
    /// </remarks>
    event EventHandler<RunnableEventArgs>? Deactivated;

    #endregion
}
