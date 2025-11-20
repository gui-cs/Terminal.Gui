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
/// <see cref="OnStopping"/>, <see cref="OnStopped"/>, <see cref="OnActivating"/>,
/// <see cref="OnActivated"/>, <see cref="OnDeactivating"/>, <see cref="OnDeactivated"/>.
/// </para>
/// </remarks>
public class Runnable : View, IRunnable
{
    /// <inheritdoc/>
    public bool Running { get; set; }

    #region IRunnable Implementation (RaiseXxxEvent Methods)

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

    /// <inheritdoc/>
    public virtual bool RaiseActivatingEvent (IRunnable? deactivated)
    {
        // CWP Phase 1: Pre-notification via virtual method (can cancel)
        if (OnActivating (deactivated))
        {
            return true; // Activation canceled
        }

        // CWP Phase 2: Event notification (can cancel)
        var args = new RunnableActivatingEventArgs (this, deactivated);
        Activating?.Invoke (this, args);

        if (args.Cancel)
        {
            return true; // Activation canceled
        }

        // CWP Phase 3: Work is done by Application (setting Current)
        // CWP Phase 4 & 5: Call post-notification methods
        OnActivated (deactivated);

        return false; // Activation succeeded
    }

    /// <inheritdoc/>
    public virtual void RaiseActivatedEvent (IRunnable? deactivated)
    {
        Activated?.Invoke (this, new RunnableEventArgs (this));
    }

    /// <inheritdoc/>
    public virtual bool RaiseDeactivatingEvent (IRunnable? activated)
    {
        // CWP Phase 1: Pre-notification via virtual method (can cancel)
        if (OnDeactivating (activated))
        {
            return true; // Deactivation canceled
        }

        // CWP Phase 2: Event notification (can cancel)
        var args = new RunnableDeactivatingEventArgs (this, activated);
        Deactivating?.Invoke (this, args);

        if (args.Cancel)
        {
            return true; // Deactivation canceled
        }

        // CWP Phase 3: Work is done by Application (changing Current)
        // CWP Phase 4 & 5: Call post-notification methods
        OnDeactivated (activated);

        return false; // Deactivation succeeded
    }

    /// <inheritdoc/>
    public virtual void RaiseDeactivatedEvent (IRunnable? activated)
    {
        Deactivated?.Invoke (this, new RunnableEventArgs (this));
    }

    #endregion

    #region Protected Virtual Methods (Override Pattern)

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

    /// <summary>
    /// Called before <see cref="Activating"/> event. Override to cancel activation.
    /// </summary>
    /// <param name="deactivated">The previously active runnable being deactivated, or null if none.</param>
    /// <returns><see langword="true"/> to cancel; <see langword="false"/> to proceed.</returns>
    /// <remarks>
    /// <para>
    /// This is the first phase of the Cancellable Work Pattern for activation.
    /// Default implementation returns <see langword="false"/> (allow activation).
    /// </para>
    /// <para>
    /// Override this method to provide custom logic for determining whether the runnable
    /// should become active.
    /// </para>
    /// </remarks>
    protected virtual bool OnActivating (IRunnable? deactivated)
    {
        return false; // Default: allow activation
    }

    /// <summary>
    /// Called after activation succeeds. Override for post-activation logic.
    /// </summary>
    /// <param name="deactivated">The previously active runnable that was deactivated, or null if none.</param>
    /// <remarks>
    /// <para>
    /// This is the fourth phase of the Cancellable Work Pattern for activation.
    /// Default implementation calls <see cref="RaiseActivatedEvent"/>.
    /// </para>
    /// <para>
    /// Override this method to perform work that should occur after activation
    /// (e.g., setting focus, updating UI). Overrides must call base to ensure the
    /// <see cref="Activated"/> event is raised.
    /// </para>
    /// </remarks>
    protected virtual void OnActivated (IRunnable? deactivated)
    {
        RaiseActivatedEvent (deactivated);
    }

    /// <summary>
    /// Called before <see cref="Deactivating"/> event. Override to cancel deactivation.
    /// </summary>
    /// <param name="activated">The newly activated runnable, or null if none.</param>
    /// <returns><see langword="true"/> to cancel; <see langword="false"/> to proceed.</returns>
    /// <remarks>
    /// <para>
    /// This is the first phase of the Cancellable Work Pattern for deactivation.
    /// Default implementation returns <see langword="false"/> (allow deactivation).
    /// </para>
    /// <para>
    /// Override this method to provide custom logic for determining whether the runnable
    /// should be deactivated (e.g., preventing switching away if unsaved changes exist).
    /// </para>
    /// </remarks>
    protected virtual bool OnDeactivating (IRunnable? activated)
    {
        return false; // Default: allow deactivation
    }

    /// <summary>
    /// Called after deactivation succeeds. Override for post-deactivation logic.
    /// </summary>
    /// <param name="activated">The newly activated runnable, or null if none.</param>
    /// <remarks>
    /// <para>
    /// This is the fourth phase of the Cancellable Work Pattern for deactivation.
    /// Default implementation calls <see cref="RaiseDeactivatedEvent"/>.
    /// </para>
    /// <para>
    /// Override this method to perform work that should occur after deactivation
    /// (e.g., saving state, releasing resources). Overrides must call base to ensure the
    /// <see cref="Deactivated"/> event is raised.
    /// </para>
    /// </remarks>
    protected virtual void OnDeactivated (IRunnable? activated)
    {
        RaiseDeactivatedEvent (activated);
    }

    #endregion

    #region Events

    // Note: Initializing and Initialized events are inherited from View (ISupportInitialize pattern)

    /// <inheritdoc/>
    public event EventHandler<System.ComponentModel.CancelEventArgs>? Stopping;

    /// <inheritdoc/>
    public event EventHandler? Stopped;

    /// <inheritdoc/>
    public event EventHandler<RunnableActivatingEventArgs>? Activating;

    /// <inheritdoc/>
    public event EventHandler<RunnableEventArgs>? Activated;

    /// <inheritdoc/>
    public event EventHandler<RunnableDeactivatingEventArgs>? Deactivating;

    /// <inheritdoc/>
    public event EventHandler<RunnableEventArgs>? Deactivated;

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
