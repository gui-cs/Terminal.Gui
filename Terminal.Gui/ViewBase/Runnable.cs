namespace Terminal.Gui.ViewBase;

/// <summary>
/// Base implementation of <see cref="IRunnable"/> for views that can be run as sessions.
/// </summary>
/// <remarks>
/// <para>
/// Views can derive from this class or implement <see cref="IRunnable"/> directly.
/// This base class provides a complete reference implementation of the <see cref="IRunnable"/>
/// interface following Terminal.Gui's Cancellable Work Pattern.
/// </para>
/// <para>
/// To customize lifecycle behavior, override the protected virtual methods:
/// <see cref="OnStarting"/>, <see cref="OnStarted"/>, <see cref="OnStopping"/>, <see cref="OnStopped"/>.
/// </para>
/// </remarks>
public class Runnable : View, IRunnable
{
    /// <inheritdoc/>
    public bool Running { get; set; }

    #region IRunnable Implementation (RaiseXxxEvent Methods)

    /// <inheritdoc/>
    public virtual bool RaiseStartingEvent ()
    {
        // CWP Phase 1: Pre-notification via virtual method (can cancel)
        if (OnStarting ())
        {
            return true; // Starting canceled
        }

        // CWP Phase 2: Event notification (can cancel)
        var args = new System.ComponentModel.CancelEventArgs ();
        Starting?.Invoke (this, args);

        if (args.Cancel)
        {
            return true; // Starting canceled
        }

        // CWP Phase 3: Perform the work (mark as running)
        Running = true;

        // CWP Phase 4: Post-notification via virtual method
        OnStarted ();

        // CWP Phase 5: Post-notification event
        Started?.Invoke (this, EventArgs.Empty);

        return false; // Starting succeeded
    }

    /// <inheritdoc/>
    public virtual void RaiseStartedEvent ()
    {
        Started?.Invoke (this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public virtual void RaiseStoppingEvent ()
    {
        // CWP Phase 1: Pre-notification via virtual method (can cancel)
        if (OnStopping ())
        {
            return; // Stopping canceled
        }

        // CWP Phase 2: Event notification (can cancel)
        var args = new System.ComponentModel.CancelEventArgs ();
        Stopping?.Invoke (this, args);

        if (args.Cancel)
        {
            return; // Stopping canceled
        }

        // CWP Phase 3: Perform the work (stop the session)
        Running = false;

        // CWP Phase 4: Post-notification via virtual method
        OnStopped ();

        // CWP Phase 5: Post-notification event
        Stopped?.Invoke (this, EventArgs.Empty);
    }

    #endregion

    #region Protected Virtual Methods (Override Pattern)

    /// <summary>
    /// Called before <see cref="Starting"/> event. Override to cancel starting.
    /// </summary>
    /// <returns><see langword="true"/> to cancel; <see langword="false"/> to proceed.</returns>
    /// <remarks>
    /// <para>
    /// This is the first phase of the Cancellable Work Pattern for starting.
    /// Default implementation returns <see langword="false"/> (allow starting).
    /// </para>
    /// <para>
    /// Override this method to provide custom logic for determining whether the runnable
    /// should start (e.g., validating preconditions).
    /// </para>
    /// </remarks>
    protected virtual bool OnStarting ()
    {
        return false; // Default: allow starting
    }

    /// <summary>
    /// Called after session has started. Override for post-start work.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the fourth phase of the Cancellable Work Pattern for starting.
    /// At this point, <see cref="Running"/> is <see langword="true"/>.
    /// Default implementation does nothing.
    /// </para>
    /// <para>
    /// Override this method to perform work that should occur after the session starts.
    /// </para>
    /// </remarks>
    protected virtual void OnStarted ()
    {
        // Default: do nothing
    }

    /// <summary>
    /// Called before <see cref="Stopping"/> event. Override to cancel stopping.
    /// </summary>
    /// <returns><see langword="true"/> to cancel; <see langword="false"/> to proceed.</returns>
    /// <remarks>
    /// <para>
    /// This is the first phase of the Cancellable Work Pattern for stopping.
    /// Default implementation returns <see langword="false"/> (allow stopping).
    /// </para>
    /// <para>
    /// Override this method to provide custom logic for determining whether the runnable
    /// should stop (e.g., prompting the user to save changes).
    /// </para>
    /// </remarks>
    protected virtual bool OnStopping ()
    {
        return false; // Default: allow stopping
    }

    /// <summary>
    /// Called after session has stopped. Override for post-stop cleanup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the fourth phase of the Cancellable Work Pattern for stopping.
    /// At this point, <see cref="Running"/> is <see langword="false"/>.
    /// Default implementation does nothing.
    /// </para>
    /// <para>
    /// Override this method to perform cleanup work that should occur after the session stops.
    /// </para>
    /// </remarks>
    protected virtual void OnStopped ()
    {
        // Default: do nothing
    }

    #endregion

    #region Events

    // Note: Initializing and Initialized events are inherited from View (ISupportInitialize pattern)

    /// <inheritdoc/>
    public event EventHandler<System.ComponentModel.CancelEventArgs>? Starting;

    /// <inheritdoc/>
    public event EventHandler? Started;

    /// <inheritdoc/>
    public event EventHandler<System.ComponentModel.CancelEventArgs>? Stopping;

    /// <inheritdoc/>
    public event EventHandler? Stopped;

    #endregion

    /// <summary>
    /// Stops and closes this runnable session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method calls <see cref="RaiseStoppingEvent"/> to initiate the stopping process.
    /// The Application infrastructure will update this once IApplication supports IRunnable directly.
    /// </para>
    /// </remarks>
    public virtual void RequestStop ()
    {
        // TODO: Phase 3 - Update Application.RequestStop to accept IRunnable
        // For now, directly call RaiseStoppingEvent which follows CWP
        RaiseStoppingEvent ();
    }
}
