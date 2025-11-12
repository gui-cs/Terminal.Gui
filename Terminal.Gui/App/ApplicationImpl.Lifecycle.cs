#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public partial class ApplicationImpl
{
    /// <inheritdoc/>
    public bool Initialized { get; set; }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? InitializedChanged;

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void Init (IDriver? driver = null, string? driverName = null)
    {
        if (Initialized)
        {
            Logging.Error ("Init called multiple times without shutdown, aborting.");

            throw new InvalidOperationException ("Init called multiple times without Shutdown");
        }

        if (!string.IsNullOrWhiteSpace (driverName))
        {
            _driverName = driverName;
        }

        if (string.IsNullOrWhiteSpace (_driverName))
        {
            _driverName = ForceDriver;
        }

        Debug.Assert (Navigation is null);
        Navigation = new ();

        Debug.Assert (Popover is null);
        Popover = new ();

        // Preserve existing keyboard settings if they exist
        bool hasExistingKeyboard = _keyboard is { };
        Key existingQuitKey = _keyboard?.QuitKey ?? Key.Esc;
        Key existingArrangeKey = _keyboard?.ArrangeKey ?? Key.F5.WithCtrl;
        Key existingNextTabKey = _keyboard?.NextTabKey ?? Key.Tab;
        Key existingPrevTabKey = _keyboard?.PrevTabKey ?? Key.Tab.WithShift;
        Key existingNextTabGroupKey = _keyboard?.NextTabGroupKey ?? Key.F6;
        Key existingPrevTabGroupKey = _keyboard?.PrevTabGroupKey ?? Key.F6.WithShift;

        // Reset keyboard to ensure fresh state with default bindings
        _keyboard = new KeyboardImpl { Application = this };

        // Restore previously set keys if they existed and were different from defaults
        if (hasExistingKeyboard)
        {
            _keyboard.QuitKey = existingQuitKey;
            _keyboard.ArrangeKey = existingArrangeKey;
            _keyboard.NextTabKey = existingNextTabKey;
            _keyboard.PrevTabKey = existingPrevTabKey;
            _keyboard.NextTabGroupKey = existingNextTabGroupKey;
            _keyboard.PrevTabGroupKey = existingPrevTabGroupKey;
        }

        CreateDriver (driverName ?? _driverName);
        Screen = Driver!.Screen;
        Initialized = true;

        RaiseInitializedChanged (this, new (true));
        SubscribeDriverEvents ();

        SynchronizationContext.SetSynchronizationContext (new ());
        MainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    public void Shutdown ()
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

        // Create a new lazy instance for potential future Init
        _lazyInstance = new (() => new ApplicationImpl ());
    }

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

    /// <inheritdoc/>
    public void ResetState (bool ignoreDisposed = false)
    {
        // Shutdown is the bookend for Init. As such it needs to clean up all resources
        // Init created. Apps that do any threading will need to code defensively for this.
        // e.g. see Issue #537

        // === 1. Stop all running toplevels ===
        foreach (Toplevel? t in SessionStack)
        {
            t!.Running = false;
        }

        // === 2. Close and dispose popover ===
        if (Popover?.GetActivePopover () is View popover)
        {
            // This forcefully closes the popover; invoking Command.Quit would be more graceful
            // but since this is shutdown, doing this is ok.
            popover.Visible = false;
        }

        Popover?.Dispose ();
        Popover = null;

        // === 3. Clean up toplevels ===
        SessionStack.Clear ();

#if DEBUG_IDISPOSABLE

        // Don't dispose the Current. It's up to caller dispose it
        if (View.EnableDebugIDisposableAsserts && !ignoreDisposed && Current is { })
        {
            Debug.Assert (Current.WasDisposed, $"Title = {Current.Title}, Id = {Current.Id}");

            // If End wasn't called _CachedSessionTokenToplevel may be null
            if (CachedSessionTokenToplevel is { })
            {
                Debug.Assert (CachedSessionTokenToplevel.WasDisposed);
                Debug.Assert (CachedSessionTokenToplevel == Current);
            }
        }
#endif

        Current = null;
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
        // Mouse and Keyboard will be lazy-initialized on next access
        _mouse = null;
        _keyboard = null;
        Mouse.ResetState ();

        // === 7. Clear navigation and screen state ===
        ScreenChanged = null;
        Navigation = null;

        // === 8. Reset initialization state ===
        Initialized = false;
        MainThreadId = null;

        // === 9. Clear graphics ===
        Sixel.Clear ();

        // === 10. Reset synchronization context ===
        // IMPORTANT: Always reset sync context, even if not initialized
        // This ensures cleanup works correctly even if Shutdown is called without Init
        // Reset synchronization context to allow the user to run async/await,
        // as the main loop has been ended, the synchronization context from
        // gui.cs does no longer process any callbacks. See #1084 for more details:
        // (https://github.com/gui-cs/Terminal.Gui/issues/1084).
        SynchronizationContext.SetSynchronizationContext (null);

        // Note: ForceDriver and Force16Colors are NOT reset; 
        // they need to persist across Init/Shutdown cycles
    }

    /// <summary>
    ///     Raises the <see cref="InitializedChanged"/> event.
    /// </summary>
    internal void RaiseInitializedChanged (object sender, EventArgs<bool> e) { InitializedChanged?.Invoke (sender, e); }
}
