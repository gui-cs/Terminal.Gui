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
/// </remarks>
public class Runnable<TResult> : View, IRunnable<TResult>
{
    // Cached state - eliminates race conditions from stack queries
    private bool _isRunning;
    private bool _isModal;

    /// <summary>
    ///     Constructs a new instance of the <see cref="Runnable{TResult}"/> class,
    /// </summary>
    public Runnable ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        Arrangement = ViewArrangement.Overlapped;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Runnable);
    }

    /// <inheritdoc/>
    public TResult? Result { get; set; }

    /// <summary>
    ///     Explicit implementation of the non-generic Result property from <see cref="IRunnable"/>.
    ///     This allows polymorphic access to results without knowing the concrete type.
    /// </summary>
    object? IRunnable.Result
    {
        get => Result;
        set => Result = value is TResult typedValue ? typedValue : default;
    }

    #region IRunnable Implementation - IsRunning (from base interface)

    /// <inheritdoc/>
    public bool IsRunning => _isRunning;

    /// <inheritdoc/>
    public void SetIsRunning (bool value) { _isRunning = value; }

    /// <inheritdoc />
    public virtual void RequestStop ()
    {
        // Use the IRunnable-specific RequestStop if the App supports it
        App?.RequestStop (this);
    }

    /// <inheritdoc/>
    public bool RaiseIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        // Clear previous result when starting
        if (newIsRunning)
        {
            Result = default (TResult);
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
    ///     Called before <see cref="IsRunningChanging"/> event. Override to cancel state change or extract
    ///     <see cref="Result"/>.
    /// </summary>
    /// <param name="oldIsRunning">The current value of <see cref="IsRunning"/>.</param>
    /// <param name="newIsRunning">The new value of <see cref="IsRunning"/> (true = starting, false = stopping).</param>
    /// <returns><see langword="true"/> to cancel; <see langword="false"/> to proceed.</returns>
    /// <remarks>
    ///     <para>
    ///         Default implementation returns <see langword="false"/> (allow change).
    ///     </para>
    ///     <para>
    ///         <b>IMPORTANT</b>: When <paramref name="newIsRunning"/> is <see langword="false"/> (stopping), this is the ideal
    ///         place
    ///         to extract <see cref="Result"/> from views before the runnable is removed from the stack.
    ///         At this point, all views are still alive and accessible, and subscribers can inspect the result
    ///         and optionally cancel the stop.
    ///     </para>
    ///     <example>
    ///         <code>
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
    ///             int result = MessageBox.Query (App, "Save?", "Save changes?", "Yes", "No", "Cancel");
    ///             if (result == 2) return true;  // Cancel stopping
    ///             if (result == 0) Save ();
    ///         }
    ///     }
    /// 
    ///     return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    /// }
    /// </code>
    ///     </example>
    /// </remarks>
    protected virtual bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning) => false;

    /// <summary>
    ///     Called after <see cref="IsRunning"/> has changed. Override for post-state-change logic.
    /// </summary>
    /// <param name="newIsRunning">The new value of <see cref="IsRunning"/> (true = started, false = stopped).</param>
    /// <remarks>
    ///     Default implementation does nothing. Overrides should call base to ensure extensibility.
    /// </remarks>
    protected virtual void OnIsRunningChanged (bool newIsRunning)
    {
        // Default: no-op
    }

    #endregion

    #region IRunnable Implementation - IsModal (from base interface)

    /// <inheritdoc/>
    public bool IsModal => _isModal;

    /// <inheritdoc/>
    public void SetIsModal (bool value) { _isModal = value; }

    /// <inheritdoc />
    public bool StopRequested { get; set; }

    /// <inheritdoc/>
    public void RaiseIsModalChangedEvent (bool newIsModal)
    {
        // CWP Phase 3: Post-notification (work already done by Application)
        OnIsModalChanged (newIsModal);

        EventArgs<bool> args = new (newIsModal);
        IsModalChanged?.Invoke (this, args);

        // Layout may need to change when modal state changes
        SetNeedsLayout ();
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? IsModalChanged;

    /// <summary>
    ///     Called after <see cref="IsModal"/> has changed. Override for post-activation logic.
    /// </summary>
    /// <param name="newIsModal">The new value of <see cref="IsModal"/> (true = became modal, false = no longer modal).</param>
    /// <remarks>
    ///     <para>
    ///         Default implementation does nothing. Overrides should call base to ensure extensibility.
    ///     </para>
    ///     <para>
    ///         Common uses: setting focus when becoming modal, updating UI state.
    ///     </para>
    /// </remarks>
    protected virtual void OnIsModalChanged (bool newIsModal)
    {
        // Default: no-op
    }

    #endregion
}
