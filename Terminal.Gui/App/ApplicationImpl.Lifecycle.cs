#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public partial class ApplicationImpl
{

    /// <inheritdoc/>
    public bool Initialized { get; set; }

    #region Lifecycle Events

    /// <inheritdoc />
    public event EventHandler<EventArgs<bool>>? InitializedChanged;

    #endregion Lifecycle Events

    #region Lifecycle Methods

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
#if DEBUG
        // Check that all Application events have no remaining subscribers
        AssertNoEventSubscribers (nameof (Iteration), Iteration);
        AssertNoEventSubscribers (nameof (NotifyNewRunState), NotifyNewRunState);
        AssertNoEventSubscribers (nameof (NotifyStopRunState), NotifyStopRunState);
        AssertNoEventSubscribers (nameof (ScreenChanged), ScreenChanged);
#endif

        Coordinator?.Stop ();

        bool wasInitialized = Initialized;

        // Reset Screen before calling Application.ResetState to avoid circular reference
        ResetScreen ();

        // Call ResetState FIRST so it can properly dispose Popover and other resources
        // that are accessed via Application.* static properties that now delegate to instance fields
        ResetState ();
        ConfigurationManager.PrintJsonErrors ();

        // Clear instance fields after ResetState has disposed everything
        Driver = null;
        _mouse = null;
        _keyboard = null;
        Initialized = false;
        Navigation = null;
        Popover = null;
        CachedRunStateToplevel = null;
        Top = null;
        TopLevels.Clear ();
        MainThreadId = null;
        _screen = null;
        ClearScreenNextIteration = false;
        Sixel.Clear ();

        // Don't reset ForceDriver and Force16Colors; they need to be set before Init is called

        if (wasInitialized)
        {
            bool init = Initialized; // Will be false after clearing fields above
            RaiseInitializedChanged (this, new (in init));
        }

        // TODO: Determine if we should be resetting this here. Initialized is bound to
        // TODO: Init/Shutdown, and the point of this event is to notify when that changes.
        InitializedChanged = null;

#if DEBUG
        // Check that all Application events have no remaining subscribers
        AssertNoEventSubscribers (nameof (InitializedChanged), InitializedChanged);
#endif

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
                                                 subscribers.Select (
                                                                     d => $"{d.Method.DeclaringType?.Name}.{d.Method.Name}"
                                                                    )
                                                );

            Debug.Fail (
                        $"Application.{eventName} has {subscribers.Length} remaining subscriber(s) after Shutdown: {subscriberInfo}"
                       );
        }
    }
#endif

    /// <inheritdoc />
    public void ResetState (bool ignoreDisposed = false)
    {
        // Shutdown is the bookend for Init. As such it needs to clean up all resources
        // Init created. Apps that do any threading will need to code defensively for this.
        // e.g. see Issue #537
        foreach (Toplevel? t in TopLevels)
        {
            t!.Running = false;
        }

        if (Popover?.GetActivePopover () is View popover)
        {
            // This forcefully closes the popover; invoking Command.Quit would be more graceful
            // but since this is shutdown, doing this is ok.
            popover.Visible = false;
        }

        Popover?.Dispose ();
        Popover = null;

        TopLevels.Clear ();
#if DEBUG_IDISPOSABLE

        // Don't dispose the Top. It's up to caller dispose it
        if (View.EnableDebugIDisposableAsserts && !ignoreDisposed && Top is { })
        {
            Debug.Assert (Top.WasDisposed, $"Title = {Top.Title}, Id = {Top.Id}");

            // If End wasn't called _cachedRunStateToplevel may be null
            if (CachedRunStateToplevel is { })
            {
                Debug.Assert (CachedRunStateToplevel.WasDisposed);
                Debug.Assert (CachedRunStateToplevel == Top);
            }
        }
#endif
        Top = null;
        CachedRunStateToplevel = null;

        MainThreadId = null;
        Iteration = null;
        StopAfterFirstIteration = false;
        ClearScreenNextIteration = false;

        // Driver stuff
        if (Driver is { })
        {
            UnsubscribeDriverEvents ();
            Driver?.End ();
            Driver = null;
        }

        // Reset Screen to null so it will be recalculated on next access
        // Note: ApplicationImpl.Shutdown() also calls ResetScreen() before calling this method
        // to avoid potential circular reference issues. Calling it twice is harmless.
        if (ApplicationImpl.Instance is ApplicationImpl impl)
        {
            impl.ResetScreen ();
        }

        // Run State stuff
        NotifyNewRunState = null;
        NotifyStopRunState = null;
        // Mouse and Keyboard will be lazy-initialized in ApplicationImpl on next access

        Initialized = false;

        // Mouse
        Mouse.ResetState ();

        // Keyboard events and bindings are now managed by the Keyboard instance

        ScreenChanged = null;

        Navigation = null;

        // Reset synchronization context to allow the user to run async/await,
        // as the main loop has been ended, the synchronization context from
        // gui.cs does no longer process any callbacks. See #1084 for more details:
        // (https://github.com/gui-cs/Terminal.Gui/issues/1084).
        SynchronizationContext.SetSynchronizationContext (null);
    }

    /// <summary>
    ///     Raises the <see cref="InitializedChanged"/> event.
    /// </summary>
    internal void RaiseInitializedChanged (object sender, EventArgs<bool> e)
    {
        InitializedChanged?.Invoke (sender, e);
    }

    #endregion Lifecycle Methods
}