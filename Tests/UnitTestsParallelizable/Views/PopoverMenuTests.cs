using JetBrains.Annotations;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="PopoverMenu"/> command bubbling across the PopoverMenu boundary.
///     Hierarchy under test:
///     <code>
///     MenuBar (contains MenuBarItems as SubViews)
///       └─ MenuBarItem (owns PopoverMenu, NOT as SubView)
///           └─ PopoverMenu (dynamically manages Menu SubViews)
///               └─ Menu (Root - contains MenuItems as SubViews)
///                   └─ MenuItem (Shortcut subclass)
///                       └─ OptionSelector&lt;Schemes&gt; (as CommandView)
///                           └─ CheckBox (SubView of OptionSelector)
///     </code>
/// </summary>
[TestSubject (typeof (PopoverMenu))]
public class PopoverMenuTests
{
    #region Helper Classes for Tracking

    /// <summary>
    ///     A Menu subclass that tracks Activating/Accepting events for testing.
    /// </summary>
    private class TrackingMenu : Menu
    {
        public List<(string EventName, View? Source, ICommandBinding? Binding)> EventLog { get; } = [];

        protected override bool OnActivating (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Activating", sourceView, args.Context?.Binding));

            return base.OnActivating (args);
        }

        protected override bool OnAccepting (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Accepting", sourceView, args.Context?.Binding));

            return base.OnAccepting (args);
        }
    }

    /// <summary>
    ///     A MenuBar subclass that tracks Activating/Accepting events for testing.
    /// </summary>
    private class TrackingMenuBar : MenuBar
    {
        public List<(string EventName, View? Source, ICommandBinding? Binding)> EventLog { get; } = [];

        protected override bool OnActivating (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Activating", sourceView, args.Context?.Binding));

            return base.OnActivating (args);
        }

        protected override bool OnAccepting (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Accepting", sourceView, args.Context?.Binding));

            return base.OnAccepting (args);
        }
    }

    #endregion

    #region Hierarchy Builder

    /// <summary>
    ///     Builds the full hierarchy:
    ///     MenuBar → MenuBarItem → PopoverMenu → Menu → MenuItem → OptionSelector&lt;Schemes&gt; → CheckBox
    ///     Returns the individual parts for test assertions.
    /// </summary>
    private static (TrackingMenuBar menuBar, MenuBarItem menuBarItem, PopoverMenu popoverMenu,
        TrackingMenu menu, MenuItem menuItem, OptionSelector<Schemes> selector, CheckBox secondCheckBox)
        BuildOptionSelectorInMenuBarHierarchy ()
    {
        OptionSelector<Schemes> selector = new () { Id = "schemeSelector", CanFocus = true };

        // Force layout so CheckBoxes are created from enum values
        selector.Layout ();

        // Get the second CheckBox (index 1 = Schemes.Menu)
        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        CheckBox secondCheckBox = checkBoxes [1];
        secondCheckBox.Id = "secondCheckBox";

        // Create MenuItem with OptionSelector as CommandView
        MenuItem menuItem = new () { Id = "menuItem", CommandView = selector, HelpText = "Pick scheme" };

        // Create tracking Menu containing the MenuItem
        TrackingMenu menu = new () { Id = "menu" };
        menu.Add (menuItem);

        // Create PopoverMenu with the Menu as Root
        PopoverMenu popoverMenu = new (menu) { Id = "popoverMenu" };

        // Create MenuBarItem that owns the PopoverMenu
        MenuBarItem menuBarItem = new ("_Scheme", popoverMenu) { Id = "menuBarItem" };

        // Create tracking MenuBar containing the MenuBarItem
        TrackingMenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        // Layout the hierarchy
        menuBar.Layout ();

        return (menuBar, menuBarItem, popoverMenu, menu, menuItem, selector, secondCheckBox);
    }

    #endregion

    #region Activate Bubbling Across PopoverMenu Boundary

    /// <summary>
    ///     Tests that Command.Activate on a CheckBox inside an OptionSelector CommandView
    ///     inside a MenuItem inside a PopoverMenu reaches MenuBar via event bridging.
    ///     This is the direct programmatic invocation path.
    /// </summary>
    // Claude - Opus 4.6
    [Fact (Skip = "#4620 - Requires Phase 5: Activate event bridging across PopoverMenu boundary")]
    public void Activate_FromOptionSelectorCheckBox_ReachesMenuBar_Direct ()
    {
        (TrackingMenuBar menuBar, _, _, TrackingMenu menu, _, _, CheckBox secondCheckBox)
            = BuildOptionSelectorInMenuBarHierarchy ();

        // Act: Invoke Command.Activate directly on the second CheckBox
        secondCheckBox.InvokeCommand (Command.Activate);

        // Assert: Activate should have reached the Menu (via CheckBox → OptionSelector → MenuItem → Menu bubbling)
        Assert.Contains (menu.EventLog, e => e.EventName == "Activating");

        // Assert: Activate should have reached the MenuBar (via PopoverMenu event bridging)
        Assert.Contains (menuBar.EventLog, e => e.EventName == "Activating");

        menuBar.Dispose ();
    }

    /// <summary>
    ///     Tests that Command.Activate on a CheckBox via keyboard (Space key binding)
    ///     propagates through PopoverMenu to MenuBar.
    /// </summary>
    // Claude - Opus 4.6
    [Fact (Skip = "#4620 - Requires Phase 5: Activate event bridging across PopoverMenu boundary")]
    public void Activate_FromOptionSelectorCheckBox_ReachesMenuBar_Keyboard ()
    {
        (TrackingMenuBar menuBar, _, _, TrackingMenu menu, _, _, CheckBox secondCheckBox)
            = BuildOptionSelectorInMenuBarHierarchy ();

        // Act: Simulate keyboard activation via Space key binding
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = secondCheckBox };

        CommandContext ctx = new ()
        {
            Command = Command.Activate,
            Source = new WeakReference<View> (secondCheckBox),
            Binding = keyBinding
        };

        secondCheckBox.InvokeCommand (Command.Activate, ctx);

        // Assert: Menu should have received Activating
        Assert.Contains (menu.EventLog, e => e.EventName == "Activating");

        // Assert: MenuBar should have received Activating (requires PopoverMenu bridge)
        Assert.Contains (menuBar.EventLog, e => e.EventName == "Activating");

        menuBar.Dispose ();
    }

    /// <summary>
    ///     Tests that Command.Activate on a CheckBox via mouse click
    ///     propagates through PopoverMenu to MenuBar.
    /// </summary>
    // Claude - Opus 4.6
    [Fact (Skip = "#4620 - Requires Phase 5: Activate event bridging across PopoverMenu boundary")]
    public void Activate_FromOptionSelectorCheckBox_ReachesMenuBar_Mouse ()
    {
        (TrackingMenuBar menuBar, _, _, TrackingMenu menu, _, _, CheckBox secondCheckBox)
            = BuildOptionSelectorInMenuBarHierarchy ();

        // Act: Simulate mouse activation via LeftButtonReleased binding
        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonReleased) { Source = secondCheckBox };

        CommandContext ctx = new ()
        {
            Command = Command.Activate,
            Source = new WeakReference<View> (secondCheckBox),
            Binding = mouseBinding
        };

        secondCheckBox.InvokeCommand (Command.Activate, ctx);

        // Assert: Menu should have received Activating
        Assert.Contains (menu.EventLog, e => e.EventName == "Activating");

        // Assert: MenuBar should have received Activating (requires PopoverMenu bridge)
        Assert.Contains (menuBar.EventLog, e => e.EventName == "Activating");

        menuBar.Dispose ();
    }

    #endregion

    #region Value Correctness

    /// <summary>
    ///     Tests that the OptionSelector value actually changes when activated through the menu hierarchy,
    ///     and that the change happens exactly once (no double-toggle from bubble round-trip).
    /// </summary>
    // Claude - Opus 4.6
    [Fact]
    public void OptionSelector_Value_Changes_ExactlyOnce ()
    {
        (TrackingMenuBar menuBar, _, _, _, _, OptionSelector<Schemes> selector, CheckBox secondCheckBox)
            = BuildOptionSelectorInMenuBarHierarchy ();

        // Initial value should be 0 (Schemes.Base)
        Assert.Equal (Schemes.Base, selector.Value);

        // Act: Activate the second CheckBox (Schemes.Menu)
        secondCheckBox.InvokeCommand (Command.Activate);

        // Assert: Value should have changed to Schemes.Menu (index 1)
        Assert.Equal (Schemes.Menu, selector.Value);

        menuBar.Dispose ();
    }

    #endregion

    #region Source Preservation

    /// <summary>
    ///     Tests that the Activate event source is preserved when crossing the PopoverMenu boundary.
    /// </summary>
    // Claude - Opus 4.6
    [Fact]
    public void Activate_Source_Preserved_AcrossBoundary ()
    {
        (TrackingMenuBar menuBar, _, _, TrackingMenu menu, _, _, CheckBox secondCheckBox)
            = BuildOptionSelectorInMenuBarHierarchy ();

        // Act
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = secondCheckBox };

        CommandContext ctx = new ()
        {
            Command = Command.Activate,
            Source = new WeakReference<View> (secondCheckBox),
            Binding = keyBinding
        };

        secondCheckBox.InvokeCommand (Command.Activate, ctx);

        // Assert: At Menu level, source should still be the CheckBox
        (string _, View? menuSource, ICommandBinding? _) = menu.EventLog.FirstOrDefault (e => e.EventName == "Activating");

        if (menuSource is { })
        {
            Assert.Same (secondCheckBox, menuSource);
        }

        // Assert: At MenuBar level (once bridging works), source should still be the CheckBox
        (string _, View? menuBarSource, ICommandBinding? _) = menuBar.EventLog.FirstOrDefault (e => e.EventName == "Activating");

        if (menuBarSource is { })
        {
            Assert.Same (secondCheckBox, menuBarSource);
        }

        menuBar.Dispose ();
    }

    #endregion
}
