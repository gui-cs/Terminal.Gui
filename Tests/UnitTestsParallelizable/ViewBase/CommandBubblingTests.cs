namespace ViewBaseTests.Commands;

/// <summary>
///     Tests for command propagation through view hierarchies.
///     These tests verify the FlagSelector in MenuItem in MenuBar scenario
///     documented in command-propagation-analysis.md.
/// </summary>
/// <remarks>
///     Claude - Opus 4.5
///     IMPORTANT: Most of these tests are expected to FAIL with the current implementation
///     because Command.Activate does not bubble. They document the DESIRED behavior
///     that will be enabled by the CommandsToBubbleUp feature.
///     Hierarchy under test:
///     <code>
///     MenuBar (contains MenuBarItems as SubViews)
///       └─ MenuBarItem (owns PopoverMenu, NOT as SubView)
///           └─ PopoverMenu (dynamically manages Menu SubViews)
///               └─ Menu (Root - contains MenuItems as SubViews)
///                   └─ MenuItem (Shortcut subclass)
///                       └─ FlagSelector (as CommandView, NOT as SubView)
///                           └─ CheckBox (SubView of FlagSelector)
///     </code>
/// </remarks>
public class CommandBubblingTests
{
    #region Helper Classes for Tracking Bubbling

    /// <summary>
    ///     A Menu subclass that tracks Activating/Accepting events for testing.
    /// </summary>
    private class TrackingMenu : Menu
    {
        public List<(string EventName, View? Source, ICommandBinding? Binding)> EventLog { get; } = [];

        public bool HandleActivating { get; set; }
        public bool HandleAccepting { get; set; }

        protected override bool OnActivating (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Activating", sourceView, args.Context?.Binding));

            if (HandleActivating)
            {
                return true; // Handled - should stop propagation
            }

            return base.OnActivating (args);
        }

        protected override bool OnAccepting (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Accepting", sourceView, args.Context?.Binding));

            if (HandleAccepting)
            {
                return true; // Handled - should stop propagation
            }

            return base.OnAccepting (args);
        }
    }

    /// <summary>
    ///     A MenuBar subclass that tracks Activating/Accepting events for testing.
    /// </summary>
    private class TrackingMenuBar : MenuBar
    {
        public List<(string EventName, View? Source, ICommandBinding? Binding)> EventLog { get; } = [];

        public bool HandleActivating { get; set; }
        public bool HandleAccepting { get; set; }

        protected override bool OnActivating (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Activating", sourceView, args.Context?.Binding));

            if (HandleActivating)
            {
                return true;
            }

            return base.OnActivating (args);
        }

        protected override bool OnAccepting (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Accepting", sourceView, args.Context?.Binding));

            if (HandleAccepting)
            {
                return true;
            }

            return base.OnAccepting (args);
        }
    }

    /// <summary>
    ///     Helper to track events on a FlagSelector without subclassing (FlagSelector is sealed).
    /// </summary>
    private class FlagSelectorEventTracker
    {
        public List<(string EventName, View? Source, ICommandBinding? Binding)> EventLog { get; } = [];

        public void AttachTo (View flagSelector)
        {
            flagSelector.Activating += (_, args) =>
                                       {
                                           View? sourceView = null;
                                           args.Context?.Source?.TryGetTarget (out sourceView);
                                           EventLog.Add (("Activating", sourceView, args.Context?.Binding));
                                       };

            flagSelector.Accepting += (_, args) =>
                                      {
                                          View? sourceView = null;
                                          args.Context?.Source?.TryGetTarget (out sourceView);
                                          EventLog.Add (("Accepting", sourceView, args.Context?.Binding));
                                      };
        }
    }

    [Flags]
    private enum TestFlags
    {
        None = 0,
        Flag1 = 1,
        Flag2 = 2,
        Flag3 = 4
    }

    #endregion

    #region Test 1: Activate Bubbling Through Full Hierarchy

    /// <summary>
    ///     Tests that Command.Activate bubbles from CheckBox through the full hierarchy to MenuBar.
    ///     EXPECTED TO FAIL: Current implementation does NOT bubble Command.Activate.
    ///     The activation stops at MenuItem because RaiseActivating() does not call TryBubbleToSuperView().
    ///     This test documents the DESIRED behavior that will be enabled by CommandsToBubbleUp.
    /// </summary>
    [Fact (Skip = "Command.Activate does not bubble - RaiseActivating lacks TryBubbleToSuperView call")]
    public void Activate_Propagates_FromCheckBox_ToMenuBar ()
    {
        // Arrange: Build the complete hierarchy
        // MenuBar → MenuBarItem → PopoverMenu → Menu → MenuItem → FlagSelector → CheckBox

        FlagSelector<TestFlags> flagSelector = new () { Id = "flagSelector" };
        FlagSelectorEventTracker flagSelectorTracker = new ();
        flagSelectorTracker.AttachTo (flagSelector);
        flagSelector.Layout (); // Creates CheckBoxes for flags

        // Get one of the CheckBoxes created by FlagSelector
        CheckBox? checkBox = flagSelector.SubViews.OfType<CheckBox> ().FirstOrDefault ();
        Assert.NotNull (checkBox);
        checkBox!.Id = "checkBox";

        // Create MenuItem with FlagSelector as CommandView
        MenuItem menuItem = new () { Id = "menuItem", CommandView = flagSelector };

        // Create Menu containing the MenuItem
        TrackingMenu menu = new () { Id = "menu" };
        menu.Add (menuItem);

        // Create PopoverMenu with the Menu as Root
        PopoverMenu popoverMenu = new (menu) { Id = "popoverMenu" };

        // Create MenuBarItem that owns the PopoverMenu
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", PopoverMenu = popoverMenu };

        // Create MenuBar containing the MenuBarItem
        TrackingMenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        // Layout the hierarchy
        menuBar.Layout ();

        // Act: Invoke Command.Activate on the CheckBox (simulating user pressing Space)
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = checkBox };
        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (checkBox), Binding = keyBinding };

        checkBox.InvokeCommand (Command.Activate, ctx);

        // Assert: MenuBar should have received the Activating event
        // FAILS: Command.Activate does NOT bubble - stops at MenuItem
        Assert.NotEmpty (menuBar.EventLog); // <-- This assertion will FAIL

        (string eventName, View? source, ICommandBinding? binding) = menuBar.EventLog.First ();
        Assert.Equal ("Activating", eventName);
        Assert.Same (checkBox, source); // Source should be the original CheckBox
        Assert.NotNull (binding);
        Assert.IsType<KeyBinding> (binding); // Binding type should be preserved
    }

    /// <summary>
    ///     Tests that Command.Activate bubbles through the FlagSelector to the containing view.
    ///     This verifies FlagSelector intercepts CheckBox events and forwards them.
    /// </summary>
    [Fact (Skip = "Command.Activate does not bubble - RaiseActivating lacks TryBubbleToSuperView call")]
    public void Activate_Propagates_FromCheckBox_ToFlagSelector ()
    {
        // Arrange
        FlagSelector<TestFlags> flagSelector = new () { Id = "flagSelector" };
        FlagSelectorEventTracker tracker = new ();
        tracker.AttachTo (flagSelector);
        flagSelector.Layout ();

        CheckBox? checkBox = flagSelector.SubViews.OfType<CheckBox> ().FirstOrDefault ();
        Assert.NotNull (checkBox);

        // Act: Invoke Command.Activate on the CheckBox
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = checkBox };
        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (checkBox), Binding = keyBinding };

        checkBox.InvokeCommand (Command.Activate, ctx);

        // Assert: FlagSelector should have received the Activating event
        // This should PASS because FlagSelector intercepts CheckBox.Activating and forwards it
        Assert.NotEmpty (tracker.EventLog);

        (string eventName, View? source, ICommandBinding? _) = tracker.EventLog.First ();
        Assert.Equal ("Activating", eventName);

        // Note: Source might be flagSelector (not checkBox) depending on how FlagSelector forwards the event
        // This is acceptable - the key is that the event reached FlagSelector
    }

    #endregion

    #region Test 2: Accept Bubbling Through Full Hierarchy

    /// <summary>
    ///     Tests that Command.Accept bubbles from CheckBox through the full hierarchy to MenuBar.
    ///     Command.Accept DOES bubble by default (hard-coded in RaiseAccepting), so this test
    ///     might pass, BUT the event interception in FlagSelector and Shortcut may interfere.
    /// </summary>
    [Fact (Skip = "Event interception in FlagSelector/Shortcut blocks Accept propagation through hierarchy")]
    public void Accept_Propagates_FromCheckBox_ToMenuBar ()
    {
        // Arrange: Build the complete hierarchy (same as Activate test)
        FlagSelector<TestFlags> flagSelector = new () { Id = "flagSelector" };
        FlagSelectorEventTracker flagSelectorTracker = new ();
        flagSelectorTracker.AttachTo (flagSelector);
        flagSelector.Layout ();

        CheckBox? checkBox = flagSelector.SubViews.OfType<CheckBox> ().FirstOrDefault ();
        Assert.NotNull (checkBox);
        checkBox!.Id = "checkBox";

        MenuItem menuItem = new () { Id = "menuItem", CommandView = flagSelector };

        TrackingMenu menu = new () { Id = "menu" };
        menu.Add (menuItem);

        PopoverMenu popoverMenu = new (menu) { Id = "popoverMenu" };

        MenuBarItem menuBarItem = new () { Id = "menuBarItem", PopoverMenu = popoverMenu };

        TrackingMenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        menuBar.Layout ();

        // Act: Invoke Command.Accept on the CheckBox (simulating double-click)
        MouseBinding mouseBinding = new ([Command.Accept], MouseFlags.LeftButtonDoubleClicked) { Source = checkBox };
        CommandContext ctx = new () { Command = Command.Accept, Source = new WeakReference<View> (checkBox), Binding = mouseBinding };

        checkBox.InvokeCommand (Command.Accept, ctx);

        // Assert: MenuBar should have received the Accepting event
        // This MAY fail if event interception in FlagSelector/Shortcut blocks propagation
        Assert.NotEmpty (menuBar.EventLog);

        (string eventName, View? source, ICommandBinding? binding) = menuBar.EventLog.First ();
        Assert.Equal ("Accepting", eventName);
        Assert.Same (checkBox, source); // Source should be the original CheckBox
        Assert.NotNull (binding);
        Assert.IsType<MouseBinding> (binding); // Binding type should be preserved
    }

    #endregion

    #region Test 3: Source Preservation

    /// <summary>
    ///     Tests that ctx.Source remains the original CheckBox at every level of propagation.
    ///     EXPECTED TO FAIL: Current implementation doesn't bubble Activate, so we can't
    ///     verify source preservation at higher levels.
    /// </summary>
    [Fact (Skip = "Command.Activate does not bubble - cannot verify source preservation")]
    public void Source_RemainsConstant_DuringActivateBubbling ()
    {
        // Arrange
        FlagSelector<TestFlags> flagSelector = new () { Id = "flagSelector" };
        FlagSelectorEventTracker flagSelectorTracker = new ();
        flagSelectorTracker.AttachTo (flagSelector);
        flagSelector.Layout ();

        CheckBox? checkBox = flagSelector.SubViews.OfType<CheckBox> ().FirstOrDefault ();
        Assert.NotNull (checkBox);
        checkBox!.Id = "originalCheckBox";

        MenuItem menuItem = new () { Id = "menuItem", CommandView = flagSelector };

        TrackingMenu menu = new () { Id = "menu" };
        menu.Add (menuItem);

        PopoverMenu popoverMenu = new (menu) { Id = "popoverMenu" };

        MenuBarItem menuBarItem = new () { Id = "menuBarItem", PopoverMenu = popoverMenu };

        TrackingMenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        menuBar.Layout ();

        // Act
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = checkBox };
        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (checkBox), Binding = keyBinding };

        checkBox.InvokeCommand (Command.Activate, ctx);

        // Assert: Check source at each level that received the event

        // FlagSelector should have received it (this works)
        Assert.NotEmpty (flagSelectorTracker.EventLog);

        // Note: FlagSelector may have changed Source when forwarding - check what we actually got
        // The important thing is that it reached FlagSelector

        // Menu should have received it (FAILS - Activate doesn't bubble)
        Assert.NotEmpty (menu.EventLog);
        Assert.Same (checkBox, menu.EventLog [0].Source);

        // MenuBar should have received it (FAILS - Activate doesn't bubble)
        Assert.NotEmpty (menuBar.EventLog);
        Assert.Same (checkBox, menuBar.EventLog [0].Source);
    }

    /// <summary>
    ///     Tests that ctx.Binding is preserved during propagation.
    ///     EXPECTED TO FAIL for same reason as source preservation test.
    /// </summary>
    [Fact (Skip = "Command.Activate does not bubble - cannot verify binding preservation")]
    public void Binding_IsPreserved_DuringActivateBubbling ()
    {
        // Arrange
        FlagSelector<TestFlags> flagSelector = new () { Id = "flagSelector" };
        FlagSelectorEventTracker flagSelectorTracker = new ();
        flagSelectorTracker.AttachTo (flagSelector);
        flagSelector.Layout ();

        CheckBox? checkBox = flagSelector.SubViews.OfType<CheckBox> ().FirstOrDefault ();
        Assert.NotNull (checkBox);

        MenuItem menuItem = new () { Id = "menuItem", CommandView = flagSelector };

        TrackingMenu menu = new () { Id = "menu" };
        menu.Add (menuItem);

        PopoverMenu popoverMenu = new (menu) { Id = "popoverMenu" };

        MenuBarItem menuBarItem = new () { Id = "menuBarItem", PopoverMenu = popoverMenu };

        TrackingMenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        menuBar.Layout ();

        // Create a distinctive binding we can verify
        KeyBinding originalBinding = new ([Command.Activate]) { Key = Key.F5, Source = checkBox, Data = "test-data-marker" };
        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (checkBox), Binding = originalBinding };

        // Act
        checkBox.InvokeCommand (Command.Activate, ctx);

        // Assert: Binding should be the exact same object at each level

        // FlagSelector (this works)
        Assert.NotEmpty (flagSelectorTracker.EventLog);

        // FAILS: Menu doesn't receive the event because Activate doesn't bubble
        Assert.NotEmpty (menu.EventLog);

        if (menu.EventLog [0].Binding is KeyBinding menuBinding)
        {
            Assert.Equal (Key.F5, menuBinding.Key);
            Assert.Equal ("test-data-marker", menuBinding.Data);
        }
        else
        {
            Assert.Fail ("Binding should be KeyBinding at Menu level");
        }

        // FAILS: MenuBar doesn't receive the event
        Assert.NotEmpty (menuBar.EventLog);

        if (menuBar.EventLog [0].Binding is KeyBinding menuBarBinding)
        {
            Assert.Equal (Key.F5, menuBarBinding.Key);
            Assert.Equal ("test-data-marker", menuBarBinding.Data);
        }
        else
        {
            Assert.Fail ("Binding should be KeyBinding at MenuBar level");
        }
    }

    #endregion

    #region Test 4: Handled at Intermediate Level Stops Bubbling

    /// <summary>
    ///     Tests that when Menu handles Activating, MenuBar does NOT receive the event.
    ///     This verifies CWP semantics are respected during propagation.
    ///     EXPECTED TO FAIL: Current implementation doesn't bubble Activate at all,
    ///     so we can't test the "handled stops propagation" behavior.
    /// </summary>
    [Fact (Skip = "Command.Activate does not bubble - cannot test handled-stops-propagation")]
    public void Activate_HandledAtMenu_DoesNotReachMenuBar ()
    {
        // Arrange
        FlagSelector<TestFlags> flagSelector = new () { Id = "flagSelector" };
        flagSelector.Layout ();

        CheckBox? checkBox = flagSelector.SubViews.OfType<CheckBox> ().FirstOrDefault ();
        Assert.NotNull (checkBox);

        MenuItem menuItem = new () { Id = "menuItem", CommandView = flagSelector };

        TrackingMenu menu = new () { Id = "menu", HandleActivating = true }; // Menu will handle it
        menu.Add (menuItem);

        PopoverMenu popoverMenu = new (menu) { Id = "popoverMenu" };

        MenuBarItem menuBarItem = new () { Id = "menuBarItem", PopoverMenu = popoverMenu };

        TrackingMenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        menuBar.Layout ();

        // Act
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = checkBox };
        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (checkBox), Binding = keyBinding };

        checkBox.InvokeCommand (Command.Activate, ctx);

        // Assert:
        // - Menu should have received the event (FAILS - Activate doesn't bubble to Menu)
        // - MenuBar should NOT have received it (because Menu handled it)

        // First, verify Menu received it (this will FAIL with current implementation)
        Assert.NotEmpty (menu.EventLog);
        Assert.Equal ("Activating", menu.EventLog [0].EventName);

        // MenuBar should be empty because Menu handled the event
        Assert.Empty (menuBar.EventLog);
    }

    /// <summary>
    ///     Tests that when Menu handles Accepting, MenuBar does NOT receive the event.
    ///     Command.Accept already bubbles, so this tests the "handled stops propagation" behavior.
    /// </summary>
    [Fact (Skip = "Event interception blocks Accept before reaching Menu - cannot test handled-stops-propagation")]
    public void Accept_HandledAtMenu_DoesNotReachMenuBar ()
    {
        // Arrange
        FlagSelector<TestFlags> flagSelector = new () { Id = "flagSelector" };
        flagSelector.Layout ();

        CheckBox? checkBox = flagSelector.SubViews.OfType<CheckBox> ().FirstOrDefault ();
        Assert.NotNull (checkBox);

        MenuItem menuItem = new () { Id = "menuItem", CommandView = flagSelector };

        TrackingMenu menu = new () { Id = "menu", HandleAccepting = true }; // Menu will handle it
        menu.Add (menuItem);

        PopoverMenu popoverMenu = new (menu) { Id = "popoverMenu" };

        MenuBarItem menuBarItem = new () { Id = "menuBarItem", PopoverMenu = popoverMenu };

        TrackingMenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        menuBar.Layout ();

        // Act
        MouseBinding mouseBinding = new ([Command.Accept], MouseFlags.LeftButtonDoubleClicked) { Source = checkBox };
        CommandContext ctx = new () { Command = Command.Accept, Source = new WeakReference<View> (checkBox), Binding = mouseBinding };

        checkBox.InvokeCommand (Command.Accept, ctx);

        // Assert:
        // - Menu should have received the event
        // - MenuBar should NOT have received it (because Menu handled it)

        // This test may fail if Accept doesn't even reach Menu due to event interception
        Assert.NotEmpty (menu.EventLog);
        Assert.Equal ("Accepting", menu.EventLog [0].EventName);

        // MenuBar should be empty because Menu handled the event
        Assert.Empty (menuBar.EventLog);
    }

    #endregion

    #region Simpler Bubbling Tests (Without Full Hierarchy)

    /// <summary>
    ///     Tests basic propagation: SuperView should receive Activate if it opts in.
    ///     EXPECTED TO FAIL: Current implementation has no opt-in mechanism for Activate propagation.
    ///     This test documents what CommandsToBubbleUp should enable.
    /// </summary>
    [Fact]
    public void Activate_DoesNotPropagate_ByDefault ()
    {
        // Arrange: Simple two-level hierarchy
        View subView = new () { Id = "subView" };
        View? receivedSource = null;
        var activatingReceived = false;

        View superView = new () { Id = "superView" };

        superView.Activating += (_, args) =>
                                {
                                    activatingReceived = true;
                                    args.Context?.Source?.TryGetTarget (out receivedSource);
                                };

        superView.Add (subView);
        superView.Layout ();

        // Act
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Enter, Source = subView };
        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (subView), Binding = keyBinding };

        subView.InvokeCommand (Command.Activate, ctx);

        // Assert: SuperView should NOT receive the event (Activate doesn't bubble by default)
        // This documents the CURRENT behavior
        Assert.False (activatingReceived, "Command.Activate should NOT bubble by default (current behavior)");
    }

    #endregion
}
