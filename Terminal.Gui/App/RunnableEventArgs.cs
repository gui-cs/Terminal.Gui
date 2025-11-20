namespace Terminal.Gui.App;

/// <summary>
/// Event args for <see cref="IRunnable"/> lifecycle events that provide information about the runnable.
/// </summary>
/// <remarks>
/// Used for post-notification events that cannot be canceled:
/// <see cref="IRunnable.Activated"/> and <see cref="IRunnable.Deactivated"/>.
/// </remarks>
public class RunnableEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="RunnableEventArgs"/>.
    /// </summary>
    /// <param name="runnable">The runnable involved in the event.</param>
    public RunnableEventArgs (IRunnable runnable)
    {
        Runnable = runnable;
    }

    /// <summary>
    /// Gets the runnable involved in the event.
    /// </summary>
    public IRunnable Runnable { get; }
}

/// <summary>
/// Event args for <see cref="IRunnable.Activating"/> event. Allows cancellation.
/// </summary>
/// <remarks>
/// This event is raised when a runnable session is about to become active. It can be canceled
/// by setting <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/>.
/// </remarks>
public class RunnableActivatingEventArgs : System.ComponentModel.CancelEventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="RunnableActivatingEventArgs"/>.
    /// </summary>
    /// <param name="activating">The runnable that is being activated.</param>
    /// <param name="deactivated">The runnable that is being deactivated, or null if none.</param>
    public RunnableActivatingEventArgs (IRunnable activating, IRunnable? deactivated)
    {
        Activating = activating;
        Deactivated = deactivated;
    }

    /// <summary>
    /// Gets the runnable that is being activated.
    /// </summary>
    public IRunnable Activating { get; }

    /// <summary>
    /// Gets the runnable that is being deactivated, or null if none.
    /// </summary>
    public IRunnable? Deactivated { get; }
}

/// <summary>
/// Event args for <see cref="IRunnable.Deactivating"/> event. Allows cancellation.
/// </summary>
/// <remarks>
/// This event is raised when a runnable session is about to cease being active. It can be canceled
/// by setting <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/>.
/// </remarks>
public class RunnableDeactivatingEventArgs : System.ComponentModel.CancelEventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="RunnableDeactivatingEventArgs"/>.
    /// </summary>
    /// <param name="deactivating">The runnable that is being deactivated.</param>
    /// <param name="activated">The runnable that is being activated, or null if none.</param>
    public RunnableDeactivatingEventArgs (IRunnable deactivating, IRunnable? activated)
    {
        Deactivating = deactivating;
        Activated = activated;
    }

    /// <summary>
    /// Gets the runnable that is being deactivated.
    /// </summary>
    public IRunnable Deactivating { get; }

    /// <summary>
    /// Gets the runnable that is being activated, or null if none.
    /// </summary>
    public IRunnable? Activated { get; }
}
