using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Terminal.Gui.App;

public partial class ApplicationImpl
{
    /// <inheritdoc />
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
        Screen = Driver!.Screen;
        Initialized = true;

        RaiseInitializedChanged (this, new (true));
        SubscribeDriverEvents ();

        SynchronizationContext.SetSynchronizationContext (new ());
        MainThreadId = Thread.CurrentThread.ManagedThreadId;

        return this;
    }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    public object? Shutdown ()
    {
        // Extract result from framework-owned runnable before disposal
        object? result = null;
        IRunnable? runnableToDispose = FrameworkOwnedRunnable;

        if (runnableToDispose is { })
        {
            // Extract the result using reflection to get the Result property value
            PropertyInfo? resultProperty = runnableToDispose.GetType ().GetProperty ("Result");
            result = resultProperty?.GetValue (runnableToDispose);
        }

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

        // Dispose the framework-owned runnable if it exists
        if (runnableToDispose is { })
        {
            if (runnableToDispose is IDisposable disposable)
            {
                disposable.Dispose ();
            }

            FrameworkOwnedRunnable = null;
        }

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

        return result;
    }

    /// <inheritdoc/>
    public void ResetState (bool ignoreDisposed = false)
    {
        // Shutdown is the bookend for Init. As such it needs to clean up all resources
        // Init created. Apps that do any threading will need to code defensively for this.
        // e.g. see Issue #537

        // === 0. Stop all timers ===
        TimedEvents?.StopAll ();

        // === 1. Stop all running toplevels ===
        foreach (SessionToken token in SessionStack!)
        {
            token.Runnable!.StopRequested = true;
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

        // === 3. Clean up toplevels ===
        SessionStack?.Clear ();

#if DEBUG_IDISPOSABLE

        // Don't dispose the TopRunnable. It's up to caller dispose it
        if (View.EnableDebugIDisposableAsserts && !ignoreDisposed && TopRunnable is { })
        {
            Debug.Assert (TopRunnable.WasDisposed, $"Title = {TopRunnable.Title}, Id = {TopRunnable.Id}");

            // If End wasn't called _CachedSessionTokenToplevel may be null
            if (CachedSessionTokenToplevel is { })
            {
                Debug.Assert (CachedSessionTokenToplevel.WasDisposed);
                Debug.Assert (CachedSessionTokenToplevel == TopRunnable);
            }
        }
#endif

        TopRunnable = null;
        CachedSessionTokenToplevel = null;

        // === 4. Clean up driver ===
        if (Driver is { })
        {
            UnsubscribeDriverEvents ();
            Driver?.End ();
            Driver = null;
        }

        // Reset screen
        ResetScreen ();
        _screen = null;

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

        //Navigation = null;

        // === 8. Reset initialization state ===
        Initialized = false;
        MainThreadId = null;

        // === 9. Clear graphics ===
        Sixel.Clear ();

        // === 10. Reset ForceDriver ===
        // Note: ForceDriver and Force16Colors are reset
        // If they need to persist across Init/Shutdown cycles
        // then the user of the library should manage that state
        Force16Colors = false;
        ForceDriver = string.Empty;

        // === 11. Reset synchronization context ===
        // IMPORTANT: Always reset sync context, even if not initialized
        // This ensures cleanup works correctly even if Shutdown is called without Init
        // Reset synchronization context to allow the user to run async/await,
        // as the main loop has been ended, the synchronization context from
        // gui.cs does no longer process any callbacks. See #1084 for more details:
        // (https://github.com/gui-cs/Terminal.Gui/issues/1084).
        SynchronizationContext.SetSynchronizationContext (null);

        // === 12. Unsubscribe from Application static property change events ===
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

    // Event handlers for Application static property changes
    private void OnForce16ColorsChanged (object? sender, ValueChangedEventArgs<bool> e) { Force16Colors = e.NewValue; }

    private void OnForceDriverChanged (object? sender, ValueChangedEventArgs<string> e) { ForceDriver = e.NewValue; }

    /// <summary>
    ///     Unsubscribes from Application static property change events.
    /// </summary>
    private void UnsubscribeApplicationEvents ()
    {
        Application.Force16ColorsChanged -= OnForce16ColorsChanged;
        Application.ForceDriverChanged -= OnForceDriverChanged;
    }
}
