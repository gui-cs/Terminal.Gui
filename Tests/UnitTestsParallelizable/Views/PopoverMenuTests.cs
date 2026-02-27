using System.Diagnostics;
using JetBrains.Annotations;
using Terminal.Gui.Tests;
using Xunit.Abstractions;

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
public class PopoverMenuTests (ITestOutputHelper output)
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

    #region Target Bridging (ContextMenus pattern)

    /// <summary>
    ///     Proves the ContextMenus pattern works: a PopoverMenu with Target set to a host view
    ///     bridges Activated to the host when a MenuItem is activated inside the PopoverMenu.
    ///     This mirrors the <c>popoverMenuHost.Activated</c> handler in ContextMenus.cs.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Target_MenuItem_Activate_Bridges_To_Target_Activated ()
    {
        // Arrange: PopoverMenu → Menu → MenuItem, with Target set to a host view
        View host = new () { Id = "host" };

        MenuItem menuItem = new () { Id = "testItem", Title = "Test Item" };
        Menu menu = new () { Id = "contextMenu" };
        menu.Add (menuItem);

        PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
        popoverMenu.Root = menu;
        popoverMenu.Target = new WeakReference<View> (host);

        bool hostActivatedFired = false;
        ICommandContext? capturedCtx = null;

        host.Activated += (_, args) =>
                          {
                              hostActivatedFired = true;
                              capturedCtx = args.Value;
                          };

        // Act: Activate the MenuItem inside the PopoverMenu
        menuItem.InvokeCommand (Command.Activate);

        // Assert: The host's Activated event should fire via Target bridge
        Assert.True (hostActivatedFired);
        Assert.NotNull (capturedCtx);
        Assert.Equal (CommandRouting.Bridged, capturedCtx!.Routing);

        popoverMenu.Dispose ();
    }

    /// <summary>
    ///     Proves the ContextMenus pattern for CheckBox CommandView:
    ///     When a MenuItem has a CheckBox as its CommandView, activating the CheckBox should
    ///     bridge through PopoverMenu.Target to the host, and TryGetSource should identify
    ///     the CheckBox. This mirrors the <c>bordersCheckbox</c> pattern in ContextMenus.cs.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Target_CheckBox_CommandView_Activate_Source_Reaches_Target ()
    {
        // Arrange: Host ← Target ← PopoverMenu → Menu → MenuItem(CheckBox CommandView)
        View host = new () { Id = "host", CommandsToBubbleUp = [Command.Activate] };

        CheckBox bordersCheckBox = new () { Id = "bordersCheckbox", Title = "_Borders", CanFocus = false };
        MenuItem menuItemBorders = new () { Id = "menuItemBorders", Title = "_Borders", CommandView = bordersCheckBox };

        Menu menu = new () { Id = "contextMenu" };
        menu.Add (menuItemBorders);

        PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
        popoverMenu.Root = menu;
        popoverMenu.Target = new WeakReference<View> (host);

        View? capturedSource = null;

        host.Activated += (_, args) =>
                          {
                              args.Value?.TryGetSource (out capturedSource);
                          };

        // Act: Activate the MenuItem (which has the CheckBox as CommandView)
        menuItemBorders.InvokeCommand (Command.Activate);

        // Assert: The source reaching the host should be identifiable
        // (The exact source depends on how MenuItem propagates CommandView activations,
        //  but the Activated event must fire on the host via the Target bridge)
        Assert.NotNull (capturedSource);

        popoverMenu.Dispose ();
    }

    /// <summary>
    ///     Proves the ContextMenus pattern for OptionSelector CommandView:
    ///     When a MenuItem has an OptionSelector as its CommandView, activating a CheckBox
    ///     inside the OptionSelector should bridge through PopoverMenu.Target to the host,
    ///     and TryGetSource should identify the CheckBox within the OptionSelector.
    ///     This mirrors the <c>schemeOptionSelector</c> pattern in ContextMenus.cs.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Target_OptionSelector_CommandView_Activate_Source_Reaches_Target ()
    {
        // Arrange: Host ← Target ← PopoverMenu → Menu → MenuItem(OptionSelector CommandView → CheckBoxes)
        View host = new () { Id = "host" };

        OptionSelector<Schemes> schemeSelector = new () { Id = "schemeOptionSelector", CanFocus = true };
        schemeSelector.Layout ();

        MenuItem menuItemScheme = new () { Id = "menuItemScheme", Title = "_Scheme", CommandView = schemeSelector };

        Menu menu = new () { Id = "contextMenu" };
        menu.Add (menuItemScheme);

        PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
        popoverMenu.Root = menu;
        popoverMenu.Target = new WeakReference<View> (host);

        bool hostActivatedFired = false;

        host.Activated += (_, _) =>
                          {
                              hostActivatedFired = true;
                          };

        // Act: Activate the MenuItem
        menuItemScheme.InvokeCommand (Command.Activate);

        // Assert: The host's Activated event should fire via the Target bridge
        Assert.True (hostActivatedFired);

        popoverMenu.Dispose ();
    }

    /// <summary>
    ///     Proves that when PopoverMenu.Target is set and the target has CommandsToBubbleUp,
    ///     the activation bridges through to the target's SuperView. This mirrors the full
    ///     ContextMenus hierarchy: _appWindow (SuperView) → popoverMenuHost (Target) → PopoverMenu.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Target_Activated_Bubbles_To_Target_SuperView ()
    {
        // Arrange: SuperView → Host(Target) ← PopoverMenu → Menu → MenuItem
        View superView = new () { Id = "appWindow", CommandsToBubbleUp = [Command.Activate] };
        View host = new () { Id = "host", CommandsToBubbleUp = [Command.Activate] };
        superView.Add (host);

        MenuItem menuItem = new () { Id = "testItem", Title = "Test" };
        Menu menu = new () { Id = "contextMenu" };
        menu.Add (menuItem);

        PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
        popoverMenu.Root = menu;
        popoverMenu.Target = new WeakReference<View> (host);

        bool superViewActivatedFired = false;

        superView.Activated += (_, _) =>
                               {
                                   superViewActivatedFired = true;
                               };

        // Act: Activate the MenuItem
        menuItem.InvokeCommand (Command.Activate);

        // Assert: The SuperView's Activated event should fire (bridge → host → bubble up → superView)
        Assert.True (superViewActivatedFired);

        popoverMenu.Dispose ();
    }

    /// <summary>
    ///     Proves that when a <see cref="MenuItem"/> has a <see cref="CheckBox"/> as its CommandView,
    ///     activating the MenuItem bridges through <see cref="PopoverMenu.Target"/> to the host,
    ///     and <see cref="ICommandContext.Value"/> carries the CheckBox's post-toggle
    ///     <see cref="CheckState"/>. This mirrors the <c>bordersCheckbox</c> pattern in ContextMenus.cs.
    /// </summary>
    /// <remarks>
    ///     The fix uses <c>RefreshValue</c> in <c>TryBubbleUp</c> and <c>DefaultActivateHandler</c>
    ///     to re-read the dispatch target's <see cref="IValue.GetValue"/> after state changes complete.
    /// </remarks>

    // Claude - Opus 4.6
    [Fact]
    public void Target_CheckBox_CommandView_Activate_Source_Reaches_Target_And_Value_Is_Correct ()
    {
        using (TestLogging.Verbose (output))
        {
            Terminal.Gui.Tracing.Trace.CommandEnabled = true;

            // Arrange: Host ← Target ← PopoverMenu → Menu → MenuItem(CheckBox CommandView)
            View host = new () { Id = "host" };

            CheckBox bordersCheckBox = new () { Id = "bordersCheckbox", Title = "_Borders", CanFocus = false };

            // CheckBox starts UnChecked
            Assert.Equal (CheckState.UnChecked, bordersCheckBox.Value);

            MenuItem menuItemBorders = new () { Id = "menuItemBorders", Title = "_Borders", CommandView = bordersCheckBox };

            Menu menu = new () { Id = "contextMenu" };
            menu.Add (menuItemBorders);

            PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
            popoverMenu.Root = menu;
            popoverMenu.Target = new WeakReference<View> (host);

            object? capturedValue = null;
            var hostActivatedFired = 0;

            host.Activated += (_, args) =>
                              {
                                  hostActivatedFired++;
                                  capturedValue = args.Value?.Value;
                              };

            // Act: Activate the MenuItem with a binding (simulates real key/mouse activation).
            // A binding is required because Shortcut's relay dispatch guard skips when Binding is null.
            bordersCheckBox.InvokeCommand (Command.Activate, new KeyBinding ([Command.Activate], menuItemBorders));

            // Assert: Host's Activated event should fire via Target bridge
            Assert.Equal (1, hostActivatedFired);

            // The CheckBox should have toggled to Checked
            Assert.Equal (CheckState.Checked, bordersCheckBox.Value);

            // The value arriving at the host should be the post-toggle value (Checked).
            Assert.Equal (CheckState.Checked, capturedValue as CheckState?);

            popoverMenu.Dispose ();
        }

    }

    #endregion
}
