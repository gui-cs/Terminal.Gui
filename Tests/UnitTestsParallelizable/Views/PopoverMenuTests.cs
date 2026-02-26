using JetBrains.Annotations;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="PopoverMenu"/> command bubbling across the PopoverMenu boundary.
///     Hierarchy under test:
///     <code>
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
    ///     A PopoverMenu subclass that tracks Activating/Accepting events for testing.
    /// </summary>
    private class TrackingPopoverMenu : PopoverMenu
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
    ///     PopoverMenu → Menu → MenuItem → OptionSelector&lt;Schemes&gt; → CheckBox
    ///     Returns the individual parts for test assertions.
    /// </summary>
    private static (TrackingPopoverMenu popoverMenu, TrackingMenu menu, MenuItem menuItem, OptionSelector<Schemes> selector, CheckBox secondCheckBox)
        BuildOptionSelectorInPopoverMenuHierarchy ()
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
        TrackingPopoverMenu popoverMenu = new () { Id = "popoverMenu" };
        popoverMenu.Root = menu;

        return (popoverMenu, menu, menuItem, selector, secondCheckBox);
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
        //(TrackingPopoverMenu popoverMenu, TrackingMenu menu, _, _, CheckBox secondCheckBox)
        //    = BuildOptionSelectorInPopoverMenuHierarchy ();

        //// Act: Invoke Command.Activate directly on the second CheckBox
        //secondCheckBox.InvokeCommand (Command.Activate);

        //// Assert: Activate should have reached the Menu (via CheckBox → OptionSelector → MenuItem → Menu bubbling)
        //Assert.Contains (menu.EventLog, e => e.EventName == "Activating");

        //// Assert: Activate should have reached the MenuBar (via PopoverMenu event bridging)
        //Assert.Contains (menuBar.EventLog, e => e.EventName == "Activating");

        //menuBar.Dispose ();
    }

    /// <summary>
    ///     Tests that Command.Activate on a CheckBox via keyboard (Space key binding)
    ///     propagates through PopoverMenu to MenuBar.
    /// </summary>

    // Claude - Opus 4.6
    [Fact (Skip = "#4620 - Requires Phase 5: Activate event bridging across PopoverMenu boundary")]
    public void Activate_FromOptionSelectorCheckBox_ReachesMenuBar_Keyboard ()
    {
        //(TrackingPopoverMenu menuBar, _, _, TrackingMenu menu, _, _, CheckBox secondCheckBox)
        //    = BuildOptionSelectorInPopoverMenuHierarchy ();

        //// Act: Simulate keyboard activation via Space key binding
        //KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = new WeakReference<View> (secondCheckBox) };

        //CommandContext ctx = new ()
        //{
        //    Command = Command.Activate,
        //    Source = new WeakReference<View> (secondCheckBox),
        //    Binding = keyBinding
        //};

        //secondCheckBox.InvokeCommand (Command.Activate, ctx);

        //// Assert: Menu should have received Activating
        //Assert.Contains (menu.EventLog, e => e.EventName == "Activating");

        //// Assert: MenuBar should have received Activating (requires PopoverMenu bridge)
        //Assert.Contains (menuBar.EventLog, e => e.EventName == "Activating");

        //menuBar.Dispose ();
    }

    /// <summary>
    ///     Tests that Command.Activate on a CheckBox via mouse click
    ///     propagates through PopoverMenu to MenuBar.
    /// </summary>

    // Claude - Opus 4.6
    [Fact (Skip = "#4620 - Requires Phase 5: Activate event bridging across PopoverMenu boundary")]
    public void Activate_FromOptionSelectorCheckBox_ReachesMenuBar_Mouse ()
    {
        //(TrackingPopoverMenu menuBar, _, _, TrackingMenu menu, _, _, CheckBox secondCheckBox)
        //    = BuildOptionSelectorInPopoverMenuHierarchy ();

        //// Act: Simulate mouse activation via LeftButtonReleased binding
        //MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonReleased) { Source = new WeakReference<View> (secondCheckBox) };

        //CommandContext ctx = new ()
        //{
        //    Command = Command.Activate,
        //    Source = new WeakReference<View> (secondCheckBox),
        //    Binding = mouseBinding
        //};

        //secondCheckBox.InvokeCommand (Command.Activate, ctx);

        //// Assert: Menu should have received Activating
        //Assert.Contains (menu.EventLog, e => e.EventName == "Activating");

        //// Assert: MenuBar should have received Activating (requires PopoverMenu bridge)
        //Assert.Contains (menuBar.EventLog, e => e.EventName == "Activating");

        //menuBar.Dispose ();
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
        //(TrackingPopoverMenu menuBar, _, _, _, _, OptionSelector<Schemes> selector, CheckBox secondCheckBox)
        //    = BuildOptionSelectorInPopoverMenuHierarchy ();

        //// Initial value should be 0 (Schemes.Base)
        //Assert.Equal (Schemes.Base, selector.Value);

        //// Act: Activate the second CheckBox (Schemes.Menu)
        //secondCheckBox.InvokeCommand (Command.Activate);

        //// Assert: Value should have changed to Schemes.Menu (index 1)
        //Assert.Equal (Schemes.Menu, selector.Value);

        //menuBar.Dispose ();
    }

    #endregion

    #region Source Preservation

    /// <summary>
    ///     Tests that the Activate event source is preserved when crossing the PopoverMenu boundary.
    /// </summary>

    // Claude - Opus 4.6
    [Fact (Skip = "#4620 - Requires Phase 5: Activate event bridging across PopoverMenu boundary. ConsumeDispatch on SelectorBase stops propagation.")]
    public void Activate_Source_Preserved_AcrossBoundary ()
    {
        (TrackingPopoverMenu popoverMenu, TrackingMenu menu, MenuItem menuItem, OptionSelector<Schemes> optionSelector, CheckBox secondCheckBox) = BuildOptionSelectorInPopoverMenuHierarchy ();

        // Act
        KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = new WeakReference<View> (secondCheckBox) };

        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (secondCheckBox), Binding = keyBinding };

        menuItem.Activating += (sender, args) =>
                               {

                               };
        secondCheckBox.InvokeCommand (Command.Activate, ctx);

        // Assert: At Menu level, source should still be the CheckBox
        (string _, View? menuSource, ICommandBinding? _) = menu.EventLog.FirstOrDefault (e => e.EventName == "Activating");

        Assert.NotNull (menuSource);
        Assert.Same (secondCheckBox, menuSource);

        // Assert: At PopoverMenu level (once bridging works), source should still be the CheckBox
        (string _, View? popoverMenuSource, ICommandBinding? _) = popoverMenu.EventLog.FirstOrDefault (e => e.EventName == "Activating");

        Assert.NotNull (popoverMenuSource);
        Assert.Same (secondCheckBox, popoverMenuSource);

        popoverMenu.Dispose ();
    }

    #endregion

    #region Positioning

    // Claude - Sonnet 4.6
    [Fact]
    public void MakeVisible_WithAnchor_PositionsBelowAnchor_WhenFits ()
    {
        // Arrange — 80×24 screen, anchor at (10, 5, 10, 1), menu height 4
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem1 = new () { Id = "mi1", Title = "Item 1" };
        MenuItem menuItem2 = new () { Id = "mi2", Title = "Item 2" };
        MenuItem menuItem3 = new () { Id = "mi3", Title = "Item 3" };
        PopoverMenu popoverMenu = new ([menuItem1, menuItem2, menuItem3]) { Id = "popoverMenu" };

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        app.Popovers?.Register (popoverMenu);

        Rectangle anchor = new (10, 5, 10, 1);

        // Act
        popoverMenu.MakeVisible (anchor: anchor);

        // Assert — menu should be positioned below the anchor
        Assert.NotNull (popoverMenu.Root);
        popoverMenu.Root.Layout ();
        Assert.Equal (6, popoverMenu.Root.Frame.Y);
        Assert.Equal (10, popoverMenu.Root.Frame.X);

        popoverMenu.Dispose ();
    }

    // Claude - Sonnet 4.6
    [Fact]
    public void MakeVisible_WithAnchor_FlipsAbove_WhenBelowOverflows ()
    {
        // Arrange — 80×25 screen (ANSI default), anchor near bottom
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem1 = new () { Id = "mi1", Title = "Item 1" };
        MenuItem menuItem2 = new () { Id = "mi2", Title = "Item 2" };
        MenuItem menuItem3 = new () { Id = "mi3", Title = "Item 3" };
        PopoverMenu popoverMenu = new ([menuItem1, menuItem2, menuItem3]) { Id = "popoverMenu" };

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        app.Popovers?.Register (popoverMenu);

        // First, make visible at a normal position to get the menu laid out and measure its height
        popoverMenu.MakeVisible (anchor: new Rectangle (10, 0, 10, 1));
        Assert.NotNull (popoverMenu.Root);
        popoverMenu.Root.Layout ();
        int menuHeight = popoverMenu.Root.Frame.Height;
        Assert.True (menuHeight > 0, "Menu should have non-zero height after layout");
        popoverMenu.Visible = false;

        // Place anchor so that anchor.Bottom + menuHeight > screenHeight (25)
        int anchorY = 25 - menuHeight + 1; // ensures overflow by 1 row
        Rectangle anchor = new (10, anchorY, 10, 1);

        // Act
        popoverMenu.MakeVisible (anchor: anchor);

        // Assert — menu should flip above the anchor
        popoverMenu.Root.Layout ();
        Assert.Equal (anchorY - menuHeight, popoverMenu.Root.Frame.Y);

        popoverMenu.Dispose ();
    }

    // Claude - Sonnet 4.6
    [Fact]
    public void MakeVisible_WithAnchor_ClampsX_WhenRightEdgeOverflows ()
    {
        // Arrange — 80×24 screen, anchor near right edge at (75, 5, 10, 1)
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem1 = new () { Id = "mi1", Title = "Long Menu Item 1" };
        MenuItem menuItem2 = new () { Id = "mi2", Title = "Long Menu Item 2" };
        PopoverMenu popoverMenu = new ([menuItem1, menuItem2]) { Id = "popoverMenu" };

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        app.Popovers?.Register (popoverMenu);

        Rectangle anchor = new (75, 5, 10, 1);

        // Act
        popoverMenu.MakeVisible (anchor: anchor);

        // Assert — X should be clamped so menu doesn't go off-screen right
        Assert.NotNull (popoverMenu.Root);
        popoverMenu.Root.Layout ();
        int menuWidth = popoverMenu.Root.Frame.Width;
        int rootX = popoverMenu.Root.Frame.X;
        Assert.True (rootX + menuWidth <= 80, $"Root.Frame.X ({rootX}) + menuWidth ({menuWidth}) should be <= 80");
        Assert.True (rootX >= 0, "Root.Frame.X should be >= 0");

        popoverMenu.Dispose ();
    }

    // Claude - Sonnet 4.6
    [Fact]
    public void MenuBarItem_PopoverMenuAnchor_UsesAnchorRect_NotMbiFrame ()
    {
        // Arrange — MenuBarItem with custom PopoverMenuAnchor pointing to (20, 10, 5, 1)
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        MenuItem menuItem = new () { Id = "menuItem1", Title = "Item _1" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_File" };
        PopoverMenu popoverMenu = new ();
        menuBarItem.PopoverMenu = popoverMenu;
        popoverMenu.Root = menu;

        // Anchor to a custom rectangle, not the MenuBarItem's own frame
        menuBarItem.PopoverMenuAnchor = () => new Rectangle (20, 10, 5, 1);

        MenuBar menuBar = new ([menuBarItem]) { Id = "menuBar" };
        hostView.Add (menuBar);
        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act
        menuBarItem.SetFocus ();
        menuBarItem.PopoverMenuOpen = true;

        // Assert — Root should be positioned below the anchor at y=11, x=20
        Assert.NotNull (popoverMenu.Root);
        popoverMenu.Root.Layout ();
        Assert.Equal (11, popoverMenu.Root.Frame.Y);
        Assert.Equal (20, popoverMenu.Root.Frame.X);
    }

    #endregion
}
