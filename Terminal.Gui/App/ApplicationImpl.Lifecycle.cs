using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    /// <inheritdoc/>
    public int? MainThreadId { get; set; }

    /// <inheritdoc/>
    public bool Initialized { get; set; }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? InitializedChanged;

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public IApplication Init (string? driverName = null)
    {
        if (Initialized)
        {
            Logging.Error ("Init called multiple times without shutdown, aborting.");

            throw new InvalidOperationException ("Init called multiple times without Shutdown");
        }

        // Thread-safe fence check: Ensure we're not mixing application models
        // Use lock to make check-and-set atomic
        lock (_modelUsageLock)
        {
            // If this is a legacy static instance and instance-based model was used, throw
            if (this == _instance && ModelUsage == ApplicationModelUsage.InstanceBased)
            {
                throw new InvalidOperationException (ERROR_LEGACY_AFTER_MODERN);
            }

            // If this is an instance-based instance and legacy static model was used, throw
            if (this != _instance && ModelUsage == ApplicationModelUsage.LegacyStatic)
            {
                throw new InvalidOperationException (ERROR_MODERN_AFTER_LEGACY);
            }

            // If no model has been set yet, set it now based on which instance this is
            if (ModelUsage == ApplicationModelUsage.None)
            {
                ModelUsage = this == _instance ? ApplicationModelUsage.LegacyStatic : ApplicationModelUsage.InstanceBased;
            }
        }

        if (!string.IsNullOrWhiteSpace (driverName))
        {
            _driverName = driverName;
        }

        if (string.IsNullOrWhiteSpace (_driverName))
        {
            _driverName = ForceDriver;
        }

        // Debug.Assert (Navigation is null);
        // Navigation = new ();

        //Debug.Assert (Popover is null);
        //Popover = new ();

        // Preserve existing keyboard settings if they exist
        bool hasExistingKeyboard = _keyboard is { };
        Key existingQuitKey = _keyboard?.QuitKey ?? Application.QuitKey;
        Key existingArrangeKey = _keyboard?.ArrangeKey ?? Application.ArrangeKey;
        Key existingNextTabKey = _keyboard?.NextTabKey ?? Application.NextTabKey;
        Key existingPrevTabKey = _keyboard?.PrevTabKey ?? Application.PrevTabKey;
        Key existingNextTabGroupKey = _keyboard?.NextTabGroupKey ?? Application.NextTabGroupKey;
        Key existingPrevTabGroupKey = _keyboard?.PrevTabGroupKey ?? Application.PrevTabGroupKey;

        // Reset keyboard to ensure fresh state with default bindings
        _keyboard = new KeyboardImpl { App = this };

        // Sync keys from Application static properties (or existing keyboard if it had custom values)
        // This ensures we respect any Application.QuitKey etc changes made before Init()
        _keyboard.QuitKey = existingQuitKey;
        _keyboard.ArrangeKey = existingArrangeKey;
        _keyboard.NextTabKey = existingNextTabKey;
        _keyboard.PrevTabKey = existingPrevTabKey;
        _keyboard.NextTabGroupKey = existingNextTabGroupKey;
        _keyboard.PrevTabGroupKey = existingPrevTabGroupKey;

        CreateDriver (_driverName);

        Initialized = true;

        RaiseInitializedChanged (this, new (true));
        SubscribeDriverEvents ();

        SynchronizationContext.SetSynchronizationContext (new ());
        MainThreadId = Thread.CurrentThread.ManagedThreadId;

        _result = null;

        return this;
    }

    #region IDisposable Implementation

    private bool _disposed;

    /// <summary>
    ///     Disposes the application instance and releases all resources.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method implements the <see cref="IDisposable"/> pattern and performs the same cleanup
    ///         as <see cref="IDisposable.Dispose"/>, but without returning a result.
    ///     </para>
    ///     <para>
    ///         After calling <see cref="Dispose()"/>, use <see cref="GetResult"/> or <see cref="IApplication.GetResult{T}"/>
    ///         to retrieve the result from the last run session.
    ///     </para>
    /// </remarks>
    public void Dispose ()
    {
        Dispose (true);
        GC.SuppressFinalize (this);
    }

    /// <summary>
    ///     Disposes the application instance and releases all resources.
    /// </summary>
    /// <param name="disposing">
    ///     <see langword="true"/> if called from <see cref="Dispose()"/>;
    ///     <see langword="false"/> if called from finalizer.
    /// </param>
    protected virtual void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Dispose managed resources
            DisposeCore ();
        }

        // For the singleton instance (legacy Application.Init/Shutdown pattern),
        // we need to allow re-initialization after disposal. This enables:
        // Application.Init() -> Application.Shutdown() -> Application.Init()
        // For modern instance-based usage, this doesn't matter as new instances are created.
        if (this == _instance)
        {
            // Reset disposed flag to allow re-initialization
            _disposed = false;
        }
        else
        {
            // For instance-based usage, mark as disposed
            _disposed = true;
        }
    }

    /// <summary>
    ///     Core disposal logic - same as Shutdown() but without returning result.
    /// </summary>
    private void DisposeCore ()
    {
        // Stop the coordinator if running
        Coordinator?.Stop ();

        // Capture state before cleanup
        bool wasInitialized = Initialized;

#if DEBUG

        // Check that all Application events have no remaining subscribers BEFORE clearing them
        // Only check if we were actually initialized
        if (wasInitialized)
        {
            AssertNoEventSubscribers (nameof (Iteration), Iteration);
            AssertNoEventSubscribers (nameof (SessionBegun), SessionBegun);
            AssertNoEventSubscribers (nameof (SessionEnded), SessionEnded);
            AssertNoEventSubscribers (nameof (ScreenChanged), ScreenChanged);

            //AssertNoEventSubscribers (nameof (InitializedChanged), InitializedChanged);
        }
#endif

        // Clean up all application state (including sync context)
        // ResetState handles the case where Initialized is false
        ResetState ();

        // Configuration manager diagnostics
        ConfigurationManager.PrintJsonErrors ();

        // Raise the initialized changed event to notify shutdown
        if (wasInitialized)
        {
            bool init = Initialized; // Will be false after ResetState
            RaiseInitializedChanged (this, new (in init));
        }

        // Clear the event to prevent memory leaks
        InitializedChanged = null;
    }

    #endregion IDisposable Implementation

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    [Obsolete ("Use Dispose() or a using statement instead. This method will be removed in a future version.")]
    public object? Shutdown ()
    {
        // Shutdown is now just a wrapper around Dispose that returns the result
        object? result = GetResult ();
        Dispose ();

        return result;
    }

    private object? _result;

    /// <inheritdoc/>
    public object? GetResult () => _result;

    /// <inheritdoc/>
    public void ResetState (bool ignoreDisposed = false)
    {
        // Shutdown is the bookend for Init. As such it needs to clean up all resources
        // Init created. Apps that do any threading will need to code defensively for this.
        // e.g. see Issue #537

        // === 0. Stop all timers ===
        TimedEvents?.StopAll ();

        // === 1. Stop all running runnables ===
        foreach (SessionToken token in SessionStack!.Reverse ())
        {
            if (token.Runnable is { })
            {
                End (token);
            }
        }

        // === 2. Close and dispose popover ===
        if (Popover?.GetActivePopover () is View popover)
        {
            // This forcefully closes the popover; invoking Command.Quit would be more graceful
            // but since this is shutdown, doing this is ok.
            popover.Visible = false;
        }

        // Any popovers added to Popover have their lifetime controlled by Popover
        Popover?.Dispose ();
        Popover = null;

        // === 3. Clean up runnables ===
        SessionStack?.Clear ();

#if DEBUG_IDISPOSABLE

        // Don't dispose the TopRunnable. It's up to caller dispose it
        if (View.EnableDebugIDisposableAsserts && !ignoreDisposed && TopRunnableView is { })
        {
            Debug.Assert (TopRunnableView.WasDisposed, $"Title = {TopRunnableView.Title}, Id = {TopRunnableView.Id}");
        }
#endif

        // === 4. Clean up driver ===
        if (Driver is { })
        {
            UnsubscribeDriverEvents ();
            Driver?.Dispose ();
            Driver = null;
        }

        // === 5. Clear run state ===
        Iteration = null;
        SessionBegun = null;
        SessionEnded = null;
        StopAfterFirstIteration = false;
        ClearScreenNextIteration = false;

        // === 6. Reset input systems ===
        // Dispose keyboard and mouse to unsubscribe from events
        if (_keyboard is IDisposable keyboardDisposable)
        {
            keyboardDisposable.Dispose ();
        }

        if (_mouse is IDisposable mouseDisposable)
        {
            mouseDisposable.Dispose ();
        }

        // Mouse and Keyboard will be lazy-initialized on next access
        _mouse = null;
        _keyboard = null;
        Mouse.ResetState ();

        // === 7. Clear navigation and screen state ===
        ScreenChanged = null;

        // === 8. Reset initialization state ===
        Initialized = false;
        MainThreadId = null;

        // === 9. Reset synchronization context ===
        // IMPORTANT: Always reset sync context, even if not initialized
        // This ensures cleanup works correctly even if Shutdown is called without Init
        // Reset synchronization context to allow the user to run async/await,
        // as the main loop has been ended, the synchronization context from
        // gui.cs does no longer process any callbacks. See #1084 for more details:
        // (https://github.com/gui-cs/Terminal.Gui/issues/1084).
        SynchronizationContext.SetSynchronizationContext (null);

        // === 10. Unsubscribe from Application static property change events ===
        UnsubscribeApplicationEvents ();
    }

    /// <summary>
    ///     Raises the <see cref="InitializedChanged"/> event.
    /// </summary>
    internal void RaiseInitializedChanged (object sender, EventArgs<bool> e) { InitializedChanged?.Invoke (sender, e); }

#if DEBUG
    /// <summary>
    ///     DEBUG ONLY: Asserts that an event has no remaining subscribers.
    /// </summary>
    /// <param name="eventName">The name of the event for diagnostic purposes.</param>
    /// <param name="eventDelegate">The event delegate to check.</param>
    private static void AssertNoEventSubscribers (string eventName, Delegate? eventDelegate)
    {
        if (eventDelegate is null)
        {
            return;
        }

        Delegate [] subscribers = eventDelegate.GetInvocationList ();

        if (subscribers.Length > 0)
        {
            string subscriberInfo = string.Join (
                                                 ", ",
                                                 subscribers.Select (d => $"{d.Method.DeclaringType?.Name}.{d.Method.Name}"
                                                                    )
                                                );

            Debug.Fail (
                        $"Application.{eventName} has {subscribers.Length} remaining subscriber(s) after Shutdown: {subscriberInfo}"
                       );
        }
    }
#endif

    private void OnForceDriverChanged (object? sender, ValueChangedEventArgs<string> e) { ForceDriver = e.NewValue; }

    /// <summary>
    ///     Unsubscribes from Application static property change events.
    /// </summary>
    private void UnsubscribeApplicationEvents ()
    {
        Application.ForceDriverChanged -= OnForceDriverChanged;
    }
}
