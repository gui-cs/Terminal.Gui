namespace Terminal.Gui.ViewBase;

/// <summary>
/// Base implementation of <see cref="IRunnable{TResult}"/> for views that can be run as blocking sessions.
/// </summary>
/// <typeparam name="TResult">The type of result data returned when the session completes.</typeparam>
/// <remarks>
/// <para>
/// Views can derive from this class or implement <see cref="IRunnable{TResult}"/> directly.
/// </para>
/// <para>
/// This class provides default implementations of the <see cref="IRunnable{TResult}"/> interface
/// following the Terminal.Gui Cancellable Work Pattern (CWP).
/// </para>
/// </remarks>
public class Runnable<TResult> : View, IRunnable<TResult>
{
    /// <inheritdoc/>
    public TResult? Result { get; set; }

    #region IRunnable Implementation - IsRunning (from base interface)

    /// <inheritdoc/>
    public bool IsRunning => App?.RunnableSessionStack?.Any (token => token.Runnable == this) ?? false;

    /// <inheritdoc/>
    public bool RaiseIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        // Clear previous result when starting
        if (newIsRunning)
        {
            Result = default;
        }

        // CWP Phase 1: Virtual method (pre-notification)
        if (OnIsRunningChanging (oldIsRunning, newIsRunning))
        {
            return true; // Canceled
        }

        // CWP Phase 2: Event notification
        bool newValue = newIsRunning;
        CancelEventArgs<bool> args = new (in oldIsRunning, ref newValue);
        IsRunningChanging?.Invoke (this, args);

        return args.Cancel;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<bool>>? IsRunningChanging;

    /// <inheritdoc/>
    public void RaiseIsRunningChangedEvent (bool newIsRunning)
    {
        // CWP Phase 3: Post-notification (work already done by Application.Begin/End)
        OnIsRunningChanged (newIsRunning);

        EventArgs<bool> args = new (newIsRunning);
        IsRunningChanged?.Invoke (this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? IsRunningChanged;

    /// <summary>
    /// Called before <see cref="IsRunningChanging"/> event. Override to cancel state change or extract <see cref="Result"/>.
    /// </summary>
    /// <param name="oldIsRunning">The current value of <see cref="IsRunning"/>.</param>
    /// <param name="newIsRunning">The new value of <see cref="IsRunning"/> (true = starting, false = stopping).</param>
    /// <returns><see langword="true"/> to cancel; <see langword="false"/> to proceed.</returns>
    /// <remarks>
    /// <para>
    /// Default implementation returns <see langword="false"/> (allow change).
    /// </para>
    /// <para>
    /// <b>IMPORTANT</b>: When <paramref name="newIsRunning"/> is <see langword="false"/> (stopping), this is the ideal place
    /// to extract <see cref="Result"/> from views before the runnable is removed from the stack.
    /// At this point, all views are still alive and accessible, and subscribers can inspect the result
    /// and optionally cancel the stop.
    /// </para>
    /// <example>
    /// <code>
    /// protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    /// {
    ///     if (!newIsRunning)  // Stopping
    ///     {
    ///         // Extract result before removal from stack
    ///         Result = _textField.Text;
    ///
    ///         // Or check if user wants to save first
    ///         if (HasUnsavedChanges ())
    ///         {
    ///             int result = MessageBox.Query ("Save?", "Save changes?", "Yes", "No", "Cancel");
    ///             if (result == 2) return true;  // Cancel stopping
    ///             if (result == 0) Save ();
    ///         }
    ///     }
    ///
    ///     return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    protected virtual bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning) => false;

    /// <summary>
    /// Called after <see cref="IsRunning"/> has changed. Override for post-state-change logic.
    /// </summary>
    /// <param name="newIsRunning">The new value of <see cref="IsRunning"/> (true = started, false = stopped).</param>
    /// <remarks>
    /// Default implementation does nothing. Overrides should call base to ensure extensibility.
    /// </remarks>
    protected virtual void OnIsRunningChanged (bool newIsRunning)
    {
        // Default: no-op
    }

    #endregion

    #region IRunnable Implementation - IsModal (from base interface)

    /// <inheritdoc/>
    public bool IsModal => App?.TopRunnable == this as Toplevel || (App?.TopRunnable is IRunnable r && r == this);

    /// <inheritdoc/>
    public bool RaiseIsModalChanging (bool oldIsModal, bool newIsModal)
    {
        // CWP Phase 1: Virtual method (pre-notification)
        if (OnIsModalChanging (oldIsModal, newIsModal))
        {
            return true; // Canceled
        }

        // CWP Phase 2: Event notification
        bool newValue = newIsModal;
        CancelEventArgs<bool> args = new (in oldIsModal, ref newValue);
        IsModalChanging?.Invoke (this, args);

        return args.Cancel;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<bool>>? IsModalChanging;

    /// <inheritdoc/>
    public void RaiseIsModalChangedEvent (bool newIsModal)
    {
        // CWP Phase 3: Post-notification (work already done by Application)
        OnIsModalChanged (newIsModal);

        EventArgs<bool> args = new (newIsModal);
        IsModalChanged?.Invoke (this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? IsModalChanged;

    /// <summary>
    /// Called before <see cref="IsModalChanging"/> event. Override to cancel activation/deactivation.
    /// </summary>
    /// <param name="oldIsModal">The current value of <see cref="IsModal"/>.</param>
    /// <param name="newIsModal">The new value of <see cref="IsModal"/> (true = becoming modal/top, false = no longer modal).</param>
    /// <returns><see langword="true"/> to cancel; <see langword="false"/> to proceed.</returns>
    /// <remarks>
    /// Default implementation returns <see langword="false"/> (allow change).
    /// </remarks>
    protected virtual bool OnIsModalChanging (bool oldIsModal, bool newIsModal) => false;

    /// <summary>
    /// Called after <see cref="IsModal"/> has changed. Override for post-activation logic.
    /// </summary>
    /// <param name="newIsModal">The new value of <see cref="IsModal"/> (true = became modal, false = no longer modal).</param>
    /// <remarks>
    /// <para>
    /// Default implementation does nothing. Overrides should call base to ensure extensibility.
    /// </para>
    /// <para>
    /// Common uses: setting focus when becoming modal, updating UI state.
    /// </para>
    /// </remarks>
    protected virtual void OnIsModalChanged (bool newIsModal)
    {
        // Default: no-op
    }

    #endregion

    /// <summary>
    /// Requests that this runnable session stop.
    /// </summary>
    public virtual void RequestStop ()
    {
        // Use the IRunnable-specific RequestStop if the App supports it
        App?.RequestStop (this);
    }
}
