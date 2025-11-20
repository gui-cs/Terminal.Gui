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
/// where cancellation makes sense (Stopping, Starting).
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
    /// Raises the <see cref="Starting"/> event.
    /// Called by <see cref="IApplication.Begin"/> or <see cref="IApplication.Run"/> when this runnable session is starting.
    /// </summary>
    /// <returns><see langword="true"/> if starting was canceled; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method implements the Cancellable Work Pattern for starting:
    /// 1. Calls virtual method (can cancel)
    /// 2. Raises <see cref="Starting"/> event (can cancel)
    /// 3. If canceled, returns true
    /// 4. If not canceled, calls <see cref="RaiseStartedEvent"/> and returns false
    /// </para>
    /// </remarks>
    bool RaiseStartingEvent ();

    /// <summary>
    /// Raises the <see cref="Started"/> event.
    /// Called by <see cref="RaiseStartingEvent"/> after starting succeeds.
    /// </summary>
    /// <remarks>
    /// This is the post-notification phase of starting. Implementations should raise the
    /// <see cref="Started"/> event.
    /// </remarks>
    void RaiseStartedEvent ();

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
    /// Raised when the runnable session is about to start running.
    /// Can be canceled by setting <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Subscribe to this event to prevent starting or to perform pre-start work.
    /// This aligns with <see cref="Running"/> and <see cref="IApplication.Run"/>.
    /// </remarks>
    event EventHandler<System.ComponentModel.CancelEventArgs>? Starting;

    /// <summary>
    /// Raised when the runnable session has started running.
    /// </summary>
    /// <remarks>
    /// This is the post-notification event in the Cancellable Work Pattern pair with <see cref="Starting"/>.
    /// Subscribe to this event for post-start logic (e.g., setting focus).
    /// This aligns with <see cref="Running"/> and <see cref="IApplication.Run"/>.
    /// </remarks>
    event EventHandler? Started;

    #endregion
}
