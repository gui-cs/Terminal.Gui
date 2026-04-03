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

    /// <summary>
    ///     Tests that when Menu handles Accepting, MenuBar does NOT receive the event.
    ///     Command.Accept already bubbles, so this tests the "handled stops propagation" behavior.
    /// </summary>
    [Fact]
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
        MouseBinding mouseBinding = new ([Command.Accept], MouseFlags.LeftButtonDoubleClicked) { Source = new WeakReference<View> (checkBox) };
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
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Enter, Source = new WeakReference<View> (subView) };
        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (subView), Binding = keyBinding };

        subView.InvokeCommand (Command.Activate, ctx);

        // Assert: SuperView should NOT receive the event (Activate doesn't bubble by default)
        // This documents the CURRENT behavior
        Assert.False (activatingReceived, "Command.Activate should NOT bubble by default (current behavior)");
    }

    #endregion
}
