namespace Terminal.Gui.ViewBase;

/// <summary>
///     Base implementation of <see cref="IRunnable{TResult}"/> for views that can be run as blocking sessions.
/// </summary>
/// <typeparam name="TResult">The type of result data returned when the session completes.</typeparam>
/// <remarks>
///     <para>
///         Views can derive from this class or implement <see cref="IRunnable{TResult}"/> directly.
///     </para>
///     <para>
///         This class provides default implementations of the <see cref="IRunnable{TResult}"/> interface
///         following the Terminal.Gui Cancellable Work Pattern (CWP).
///     </para>
///     <para>
///         For views that don't need to return a result, use <see cref="Runnable"/> instead.
///     </para>
///     <para>
///         This class inherits from <see cref="Runnable"/> to avoid code duplication and ensure consistent behavior.
///     </para>
/// </remarks>
public class Runnable<TResult> : Runnable, IRunnable<TResult>
{
    /// <summary>
    ///     Constructs a new instance of the <see cref="Runnable{TResult}"/> class.
    /// </summary>
    public Runnable ()
    {
        // Base constructor handles common initialization
    }

    /// <inheritdoc/>
    public new TResult? Result
    {
        get => base.Result is TResult typedValue ? typedValue : default;
        set => base.Result = value;
    }

    /// <summary>
    ///     Override to clear typed result when starting.
    ///     Called by base <see cref="Runnable.RaiseIsRunningChanging"/> before events are raised.
    /// </summary>
    protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        // Clear previous typed result when starting
        if (newIsRunning)
        {
            Result = default;
        }

        // Call base implementation to allow further customization
        return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    }
}
