namespace ViewsTests;

public class MenuTests
{
    // Claude - Opus 4.6
    [Fact]
    public void Constructors_Defaults ()
    {
        Menu menu = new ();

        Assert.Empty (menu.Title);
        Assert.Empty (menu.Text);
        Assert.Equal (Orientation.Vertical, menu.Orientation);
        Assert.IsType<DimAuto> (menu.Width);
        Assert.IsType<DimAuto> (menu.Height);
        Assert.Equal (Menu.DefaultBorderStyle, menu.BorderStyle);
        Assert.Contains (Command.Accept, menu.CommandsToBubbleUp);
        Assert.Contains (Command.Activate, menu.CommandsToBubbleUp);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Add_MenuItem_Sets_CanFocus ()
    {
        Menu menu = new ();
        MenuItem item = new () { Title = "Item1" };

        // Before adding, CanFocus is default (false for Shortcut-derived)
        menu.Add (item);

        Assert.True (item.CanFocus);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Add_Line_Grows_To_Fill ()
    {
        Menu menu = new ();
        Line line = new ();
        menu.Add (line);

        // After adding a Line, X and Width should be set for auto-join
        Assert.IsType<PosFunc> (line.X);
        Assert.NotNull (line.Width);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Accept_Bubbles_From_MenuItem ()
    {
        Menu menu = new ();
        MenuItem item = new () { Title = "Item1" };
        menu.Add (item);

        var menuAcceptingFired = false;

        menu.Accepting += (_, _) => { menuAcceptingFired = true; };

        // Accept on the MenuItem should bubble to the Menu via the Accepting handler in OnSubViewAdded
        item.InvokeCommand (Command.Accept);

        Assert.True (menuAcceptingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Activate_Bubbles_From_MenuItem ()
    {
        Menu menu = new ();
        MenuItem item = new () { Title = "Item1" };
        menu.Add (item);

        var menuActivatingFired = false;

        menu.Activating += (_, _) => { menuActivatingFired = true; };

        // Activate on the MenuItem should bubble via CommandsToBubbleUp
        item.InvokeCommand (Command.Activate);

        Assert.True (menuActivatingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_FocusChange_Updates_SelectedMenuItem ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        Menu menu = new ();
        MenuItem item1 = new () { Title = "Item1" };
        MenuItem item2 = new () { Title = "Item2" };
        menu.Add (item1);
        menu.Add (item2);

        IRunnable runnable = new Runnable ();
        (runnable as View)?.Add (menu);
        app.Begin (runnable);

        item2.SetFocus ();

        Assert.Equal (item2, menu.SelectedMenuItem);

        (runnable as View)?.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_FocusChange_Raises_SelectedMenuItemChanged ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        Menu menu = new ();
        MenuItem item1 = new () { Title = "Item1" };
        MenuItem item2 = new () { Title = "Item2" };
        menu.Add (item1);
        menu.Add (item2);

        IRunnable runnable = new Runnable ();
        (runnable as View)?.Add (menu);
        app.Begin (runnable);

        MenuItem? changedItem = null;

        menu.SelectedMenuItemChanged += (_, selected) => { changedItem = selected; };

        item2.SetFocus ();

        Assert.Equal (item2, changedItem);

        (runnable as View)?.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_OnVisibleChanged_Selects_First_Item ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        Menu menu = new ();
        MenuItem item1 = new () { Title = "Item1" };
        MenuItem item2 = new () { Title = "Item2" };
        menu.Add (item1);
        menu.Add (item2);

        IRunnable runnable = new Runnable ();
        (runnable as View)?.Add (menu);
        app.Begin (runnable);

        // Set Visible to false, then back to true
        menu.Visible = false;
        menu.Visible = true;

        // OnVisibleChanged should select the first MenuItem
        Assert.Equal (item1, menu.SelectedMenuItem);

        (runnable as View)?.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_SuperMenuItem_Set_Correctly ()
    {
        Menu subMenu = new ();
        MenuItem subItem = new () { Title = "SubItem" };
        subMenu.Add (subItem);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };

        Assert.Equal (parentItem, subMenu.SuperMenuItem);

        parentItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Multiple_MenuItems_Accept_Both_Work ()
    {
        Menu menu = new ();

        MenuItem item1 = new () { Title = "Item1" };
        MenuItem item2 = new () { Title = "Item2" };

        menu.Add (item1);
        menu.Add (item2);

        var acceptCount = 0;
        object? lastSender = null;

        // When a MenuItem is inside a Menu, Accept bubbles via the Accepting handler
        // in Menu.OnSubViewAdded which calls Menu.RaiseAccepted.
        // Subscribe to the Menu's Accepting event to verify both items trigger it independently.
        menu.Accepting += (sender, _) =>
                          {
                              acceptCount++;
                              lastSender = sender;
                          };

        // Accept item1
        item1.InvokeCommand (Command.Accept);
        Assert.Equal (1, acceptCount);

        // Accept item2
        item2.InvokeCommand (Command.Accept);
        Assert.Equal (2, acceptCount);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Has_CommandsToBubbleUp ()
    {
        Menu menu = new ();

        Assert.Contains (Command.Accept, menu.CommandsToBubbleUp);
        Assert.Contains (Command.Activate, menu.CommandsToBubbleUp);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Accept_Handled_Does_Not_Bubble_Further ()
    {
        // Menu inside a Bar — handled Accept on Menu should not reach Bar
        Bar bar = new ();
        Menu menu = new ();
        bar.Add (menu);

        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var barAcceptingFired = 0;

        menu.Accepting += (_, e) => { e.Handled = true; };
        bar.Accepting += (_, _) => { barAcceptingFired++; };

        menuItem.InvokeCommand (Command.Accept);

        Assert.Equal (0, barAcceptingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menu_Activate_Handled_Does_Not_Bubble_Further ()
    {
        Bar bar = new ();
        Menu menu = new ();
        bar.Add (menu);

        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var barActivatingFired = 0;

        menu.Activating += (_, e) => { e.Handled = true; };
        bar.Activating += (_, _) => { barActivatingFired++; };

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (0, barActivatingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Does_Not_Double_Fire_On_Menu ()
    {
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuActivatingCount = 0;

        menu.Activating += (_, _) => { menuActivatingCount++; };

        menuItem.InvokeCommand (Command.Activate);

        // Menu.Activating should fire exactly once
        Assert.Equal (1, menuActivatingCount);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Accept_Does_Not_Double_Fire_On_Menu ()
    {
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuAcceptingCount = 0;

        menu.Accepting += (_, _) => { menuAcceptingCount++; };

        menuItem.InvokeCommand (Command.Accept);

        // Menu.Accepting should fire exactly once
        Assert.Equal (1, menuAcceptingCount);

        menu.Dispose ();
    }

    [Fact (Skip = "Fix in #4620 - genericized deferred raising")]
    public void MenuItem_Action_Fires_On_Accept ()
    {
        Menu menu = new ();

        var actionFired = 0;
        MenuItem menuItem = new () { Title = "Test", Action = () => actionFired++ };
        menu.Add (menuItem);

        menuItem.InvokeCommand (Command.Accept);

        Assert.Equal (1, actionFired);

        menu.Dispose ();
    }

    [Fact]
    public void MenuItem_Action_Fires_On_Activate ()
    {
        Menu menu = new ();

        var actionFired = 0;
        MenuItem menuItem = new () { Title = "Test", Action = () => actionFired++ };
        menu.Add (menuItem);

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, actionFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Bubbles_Through_Menu_To_Bar ()
    {
        Bar bar = new ();
        Menu menu = new ();
        bar.Add (menu);

        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var barActivatingFired = 0;
        var menuActivatingFired = 0;

        menu.Activating += (_, _) => { menuActivatingFired++; };
        bar.Activating += (_, _) => { barActivatingFired++; };

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, menuActivatingFired);
        Assert.Equal (1, barActivatingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Accept_Bubbles_Through_Menu_To_Bar ()
    {
        Bar bar = new ();
        Menu menu = new ();
        bar.Add (menu);

        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var barAcceptingFired = 0;
        var menuAcceptingFired = 0;

        menu.Accepting += (_, _) => { menuAcceptingFired++; };
        bar.Accepting += (_, _) => { barAcceptingFired++; };

        menuItem.InvokeCommand (Command.Accept);

        Assert.Equal (1, menuAcceptingFired);
        Assert.Equal (1, barAcceptingFired);

        bar.Dispose ();
    }

    #region CommandView Activation Propagation to Menu and SuperView

    // ────────────────────────────────────────────────────────────────────
    //  Tests that various forms of activating a CommandView inside a MenuItem
    //  cause Activating to be raised on the Menu and on the Menu's SuperView.
    //
    //  Matrix:
    //    Activation methods:  Direct, Mouse, Space, Enter (Accept), HotKey
    //    CommandView types:   Default (View), CheckBox, OptionSelector, FlagSelector
    //
    //  OptionSelector and FlagSelector use ConsumeDispatch=true — when an inner
    //  CheckBox fires Activate with a binding, the selector consumes it. Direct
    //  invocations on the MenuItem itself still propagate normally.
    // ────────────────────────────────────────────────────────────────────

    #region Direct Activation (InvokeCommand on MenuItem)

    // Claude - Opus 4.6
    [Fact]
    public void Direct_Activate_DefaultCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Direct_Activate_CheckBoxCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        CheckBox checkBox = new () { Title = "_Toggle" };
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Direct_Activate_OptionSelectorCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        OptionSelector selector = new () { Labels = ["Opt1", "Opt2"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = selector };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Direct InvokeCommand on MenuItem (no binding → relay dispatch skipped)
        // MenuItem's own Activating fires and bubbles to Menu and SuperView
        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Direct_Activate_FlagSelectorCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        FlagSelector flagSelector = new () { Values = [1, 2, 4], Labels = ["F1", "F2", "F3"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = flagSelector };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Direct InvokeCommand on MenuItem (no binding → relay dispatch skipped)
        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    #endregion Direct Activation

    #region Mouse Activation (InjectMouse through Application pipeline)

    // These tests use app.InjectMouse / app.InjectSequence to simulate real mouse clicks
    // through the full Application mouse dispatch pipeline. This is more realistic than
    // crafting CommandContext objects manually.
    //
    // For OptionSelector/FlagSelector CommandViews, SelectorBase clears all mouse bindings
    // (the selector's inner CheckBoxes handle their own mouse). Clicking an inner CheckBox
    // causes the selector to consume the activation via ConsumeDispatch=true.

    // Claude - Opus 4.6
    [Fact]
    public void Mouse_Activate_DefaultCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate], Width = Dim.Fill (), Height = Dim.Fill () };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        ((View)runnable).Add (superView);
        app.Begin (runnable);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Click on the MenuItem via full Application mouse pipeline
        Point screenPos = menuItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (screenPos));

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        ((View)runnable).Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Mouse_Activate_CheckBoxCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate], Width = Dim.Fill (), Height = Dim.Fill () };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        CheckBox checkBox = new () { Title = "_Toggle" };
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        ((View)runnable).Add (superView);
        app.Begin (runnable);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        Point screenPos = menuItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (screenPos));

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        ((View)runnable).Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     OptionSelector uses ConsumeDispatch=true. When an inner CheckBox is clicked via mouse,
    ///     the selector consumes the activation. The consumption prevents Activating from bubbling
    ///     to Menu or SuperView. This documents the current ConsumeDispatch behavior.
    /// </summary>
    [Fact]
    public void Mouse_Activate_OptionSelectorCommandView_InnerCheckBox_Consumed ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate], Width = Dim.Fill (), Height = Dim.Fill () };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        OptionSelector selector = new () { Labels = ["Opt1", "Opt2"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = selector };
        menu.Add (menuItem);

        ((View)runnable).Add (superView);
        app.Begin (runnable);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Click on an inner CheckBox of the OptionSelector
        CheckBox innerCb = selector.SubViews.OfType<CheckBox> ().First ();
        Point screenPos = innerCb.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (screenPos));

        // OptionSelector consumes dispatch — Activating does NOT propagate to Menu/SuperView
        Assert.Equal (0, menuActivatingCount);
        Assert.Equal (0, superViewActivatingCount);

        ((View)runnable).Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     FlagSelector uses ConsumeDispatch=true. Same as OptionSelector: inner CheckBox
    ///     activation is consumed and does not bubble Activating to Menu or SuperView.
    /// </summary>
    [Fact]
    public void Mouse_Activate_FlagSelectorCommandView_InnerCheckBox_Consumed ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate], Width = Dim.Fill (), Height = Dim.Fill () };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        FlagSelector flagSelector = new () { Values = [1, 2, 4], Labels = ["F1", "F2", "F3"] };
        flagSelector.Value = null;
        MenuItem menuItem = new () { Title = "Test", CommandView = flagSelector };
        menu.Add (menuItem);

        ((View)runnable).Add (superView);
        app.Begin (runnable);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Click on an inner CheckBox of the FlagSelector
        CheckBox innerCb = flagSelector.SubViews.OfType<CheckBox> ().First ();
        Point screenPos = innerCb.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (screenPos));

        // FlagSelector consumes dispatch — Activating does NOT propagate to Menu/SuperView
        Assert.Equal (0, menuActivatingCount);
        Assert.Equal (0, superViewActivatingCount);

        ((View)runnable).Dispose ();
    }

    #endregion Mouse Activation

    #region Space Key Activation

    // Claude - Opus 4.6
    [Fact]
    public void Space_Activate_DefaultCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Space is bound to Command.Activate in View.SetupKeyboard
        menuItem.NewKeyDownEvent (Key.Space);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Space_Activate_CheckBoxCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        CheckBox checkBox = new () { Title = "_Toggle" };
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        menuItem.NewKeyDownEvent (Key.Space);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Space_Activate_OptionSelectorCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        OptionSelector selector = new () { Labels = ["Opt1", "Opt2"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = selector };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Space key on the MenuItem
        menuItem.NewKeyDownEvent (Key.Space);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Space_Activate_FlagSelectorCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        FlagSelector flagSelector = new () { Values = [1, 2, 4], Labels = ["F1", "F2", "F3"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = flagSelector };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        menuItem.NewKeyDownEvent (Key.Space);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    #endregion Space Key Activation

    #region Enter Key Activation (Accept path)

    // Enter is bound to Command.Accept, not Command.Activate. These tests verify that
    // the Accept path (Accepting event) bubbles from MenuItem through Menu to SuperView.

    // Claude - Opus 4.6
    [Fact]
    public void Enter_Accept_DefaultCommandView_Raises_Accepting_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Accept] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuAcceptingCount = 0;
        menu.Accepting += (_, _) => menuAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        menuItem.NewKeyDownEvent (Key.Enter);

        Assert.Equal (1, menuAcceptingCount);
        Assert.Equal (1, superViewAcceptingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Enter_Accept_CheckBoxCommandView_Raises_Accepting_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Accept] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        CheckBox checkBox = new () { Title = "_Toggle" };
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        var menuAcceptingCount = 0;
        menu.Accepting += (_, _) => menuAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        menuItem.NewKeyDownEvent (Key.Enter);

        Assert.Equal (1, menuAcceptingCount);
        Assert.Equal (1, superViewAcceptingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Enter_Accept_OptionSelectorCommandView_Raises_Accepting_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Accept] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        OptionSelector selector = new () { Labels = ["Opt1", "Opt2"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = selector };
        menu.Add (menuItem);

        var menuAcceptingCount = 0;
        menu.Accepting += (_, _) => menuAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        menuItem.NewKeyDownEvent (Key.Enter);

        Assert.Equal (1, menuAcceptingCount);
        Assert.Equal (1, superViewAcceptingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Enter_Accept_FlagSelectorCommandView_Raises_Accepting_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Accept] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        FlagSelector flagSelector = new () { Values = [1, 2, 4], Labels = ["F1", "F2", "F3"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = flagSelector };
        menu.Add (menuItem);

        var menuAcceptingCount = 0;
        menu.Accepting += (_, _) => menuAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        menuItem.NewKeyDownEvent (Key.Enter);

        Assert.Equal (1, menuAcceptingCount);
        Assert.Equal (1, superViewAcceptingCount);

        superView.Dispose ();
    }

    #endregion Enter Key Activation

    #region HotKey Activation

    // Claude - Opus 4.6
    [Fact]
    public void HotKey_Activate_DefaultCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // HotKey handler in View calls SetFocus then invokes Command.Activate
        menuItem.InvokeCommand (Command.HotKey);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void HotKey_Activate_CheckBoxCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        CheckBox checkBox = new () { Title = "_Toggle" };
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        menuItem.InvokeCommand (Command.HotKey);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void HotKey_Activate_OptionSelectorCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        OptionSelector selector = new () { Labels = ["Opt1", "Opt2"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = selector };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        menuItem.InvokeCommand (Command.HotKey);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void HotKey_Activate_FlagSelectorCommandView_Raises_Activating_On_Menu_And_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        FlagSelector flagSelector = new () { Values = [1, 2, 4], Labels = ["F1", "F2", "F3"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = flagSelector };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        menuItem.InvokeCommand (Command.HotKey);

        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    #endregion HotKey Activation

    #endregion CommandView Activation Propagation to Menu and SuperView

    #region CommandView Round-Trip: Dispatch Down + Bubble Up with Value Verification

    // ────────────────────────────────────────────────────────────────────
    //  These tests verify the "round-trip" flow:
    //
    //    SuperView → Menu → MenuItem (Shortcut) → CommandView
    //
    //  A key press on the MenuItem dispatches down to the CommandView,
    //  the CommandView changes state, and the activation/acceptance bubbles
    //  back up to Menu and SuperView. The SuperView's event handler can
    //  query the CommandView and see the updated value.
    //
    //  CheckBox uses relay dispatch (ConsumeDispatch=false) — activation
    //  bubbles normally after dispatch. Button raises Accept, which bubbles
    //  via CommandsToBubbleUp. FlagSelector uses ConsumeDispatch=true —
    //  inner CheckBox clicks are consumed and do NOT propagate to Menu.
    // ────────────────────────────────────────────────────────────────────

    // Claude - Opus 4.6
    /// <summary>
    ///     Space on a MenuItem with CheckBox CommandView dispatches to the CheckBox (relay),
    ///     toggles its state, and Activating bubbles to Menu and SuperView. The SuperView's
    ///     Activating handler sees the updated CheckBox value.
    /// </summary>
    [Fact]
    public void Space_Activate_CheckBoxCommandView_Updates_Value_And_Propagates_To_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        CheckBox checkBox = new () { Title = "_Toggle" };
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        CheckState? valueSeenBySuperView = null;

        superView.Activating += (_, _) =>
                                {
                                    // At this point, the CheckBox should already be toggled
                                    valueSeenBySuperView = checkBox.Value;
                                };

        // Space triggers Command.Activate with a binding → Shortcut dispatches to CheckBox
        menuItem.NewKeyDownEvent (Key.Space);

        // CheckBox state was toggled by OnActivated → AdvanceCheckState
        Assert.Equal (CheckState.Checked, checkBox.Value);

        // SuperView's Activating handler saw the updated value
        Assert.Equal (CheckState.Checked, valueSeenBySuperView);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Enter on a MenuItem with Button CommandView raises Accept, which bubbles to
    ///     Menu and SuperView via CommandsToBubbleUp=[Accept].
    /// </summary>
    [Fact]
    public void Enter_Accept_ButtonCommandView_Propagates_Accept_To_SuperView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Accept] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        Button button = new () { Title = "_Click", CanFocus = false };
        MenuItem menuItem = new () { Title = "Test", CommandView = button };
        menu.Add (menuItem);

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        // Enter triggers Command.Accept → bubbles through Menu to SuperView
        menuItem.NewKeyDownEvent (Key.Enter);

        Assert.Equal (1, superViewAcceptingCount);

        superView.Dispose ();
    }

    #endregion CommandView Round-Trip: Dispatch Down + Bubble Up with Value Verification

    #region SubMenu CommandView Propagation

    // ────────────────────────────────────────────────────────────────────
    //  Tests that non-default CommandViews (CheckBox, Button, FlagSelector)
    //  in SubMenu MenuItems properly propagate commands through the bridge.
    //
    //    superView (CommandsToBubbleUp=[Activate, Accept])
    //      └─ rootMenu (Menu)
    //           └─ parentMenuItem (MenuItem, SubMenu = subMenu)
    //                └─ subMenu (Menu)  [NOT a SubView of rootMenu]
    //                     └─ childMenuItem (MenuItem, CommandView = X)
    //
    //  The CommandBridge on parentMenuItem.SubMenu bridges completion
    //  events (Activated/Accepted) from subMenu → parentMenuItem via
    //  InvokeCommand. This re-enters the full command pipeline, so bridged
    //  commands bubble from parentMenuItem → rootMenu → superView.
    // ────────────────────────────────────────────────────────────────────

    // Claude - Opus 4.6
    /// <summary>
    ///     Space on a childMenuItem with CheckBox CommandView in a SubMenu dispatches to the
    ///     CheckBox, toggles its state, bubbles Activating to subMenu, and bridges Activated
    ///     to parentMenuItem. The parentMenuItem's Activated handler sees the updated CheckBox value.
    /// </summary>
    [Fact]
    public void SubMenu_Space_Activate_CheckBoxCommandView_Bridges_To_ParentMenuItem_With_Updated_Value ()
    {
        MenuItem childItem = new () { Title = "Child", CommandView = new CheckBox { Title = "_Toggle" } };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        CheckBox checkBox = (CheckBox)childItem.CommandView!;
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        CheckState? valueSeenByParent = null;

        parentItem.Activated += (_, _) => { valueSeenByParent = checkBox.Value; };

        // Space triggers Activate with binding → Shortcut dispatches to CheckBox
        childItem.NewKeyDownEvent (Key.Space);

        // CheckBox toggled
        Assert.Equal (CheckState.Checked, checkBox.Value);

        // Bridge relayed Activated to parentMenuItem with updated value visible
        Assert.Equal (CheckState.Checked, valueSeenByParent);

        rootMenu.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Enter on a childMenuItem with Button CommandView in a SubMenu raises Accept,
    ///     which bubbles to subMenu and bridges Accepted to parentMenuItem.
    /// </summary>
    [Fact]
    public void SubMenu_Enter_Accept_ButtonCommandView_Bridges_Accepted_To_ParentMenuItem ()
    {
        Button button = new () { Title = "_Click", CanFocus = false };
        MenuItem childItem = new () { Title = "Child", CommandView = button };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        var parentAcceptedCount = 0;
        parentItem.Accepted += (_, _) => parentAcceptedCount++;

        var subMenuAcceptingCount = 0;
        subMenu.Accepting += (_, _) => subMenuAcceptingCount++;

        // Enter triggers Command.Accept → bubbles to subMenu → bridges to parentMenuItem
        childItem.NewKeyDownEvent (Key.Enter);

        Assert.Equal (1, subMenuAcceptingCount);
        Assert.Equal (1, parentAcceptedCount);

        rootMenu.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Direct Activate on a childMenuItem with FlagSelector CommandView in a SubMenu
    ///     bubbles to subMenu and bridges Activated to parentMenuItem. Note: direct invocation
    ///     (no binding) does NOT dispatch to FlagSelector — the value does not change.
    ///     This verifies the command propagation path, not the FlagSelector state change.
    /// </summary>
    [Fact]
    public void SubMenu_Direct_Activate_FlagSelectorCommandView_Bridges_Activated_To_ParentMenuItem ()
    {
        FlagSelector flagSelector = new () { Values = [1, 2, 4], Labels = ["F1", "F2", "F3"] };
        MenuItem childItem = new () { Title = "Child", CommandView = flagSelector };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        Menu rootMenu = new ([parentItem]);

        var parentActivatedCount = 0;
        parentItem.Activated += (_, _) => parentActivatedCount++;

        var subMenuActivatingCount = 0;
        subMenu.Activating += (_, _) => subMenuActivatingCount++;

        // Direct InvokeCommand (no binding → dispatch to FlagSelector is skipped)
        childItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, subMenuActivatingCount);
        Assert.Equal (1, parentActivatedCount);

        rootMenu.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     The CommandBridge uses InvokeCommand on the parentMenuItem, re-entering the full
    ///     command pipeline. Bridged commands bubble from parentMenuItem to rootMenu to superView.
    /// </summary>
    [Fact]
    public void SubMenu_Bridge_Propagates_Through_ParentMenuItem_To_RootMenu_And_SuperView ()
    {
        CheckBox checkBox = new () { Title = "_Toggle" };
        MenuItem childItem = new () { Title = "Child", CommandView = checkBox };
        Menu subMenu = new ([childItem]);

        MenuItem parentItem = new () { Title = "Parent", SubMenu = subMenu };
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu rootMenu = new ([parentItem]);
        superView.Add (rootMenu);

        var parentActivatedCount = 0;
        parentItem.Activated += (_, _) => parentActivatedCount++;

        var rootMenuActivatingCount = 0;
        rootMenu.Activating += (_, _) => rootMenuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        childItem.InvokeCommand (Command.Activate);

        // Bridge fires InvokeCommand → Activated on parentMenuItem
        Assert.Equal (1, parentActivatedCount);

        // Bridge re-enters pipeline → TryBubbleUp → rootMenu and superView get Activating
        Assert.Equal (1, rootMenuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        superView.Dispose ();
    }

    #endregion SubMenu CommandView Propagation

    #region Dispatch Down and Round-Trip Value Verification

    // ────────────────────────────────────────────────────────────────────
    //  Tests that verify the complete round-trip:
    //    1. Command dispatches DOWN from MenuItem to CommandView
    //    2. CommandView changes state (value updated)
    //    3. Activation bubbles UP to Menu and SuperView
    //    4. SuperView's event handler queries CommandView and sees the new value
    //
    //  This works for CheckBox (relay dispatch, ConsumeDispatch=false):
    //    Space key → Shortcut dispatches to CheckBox → AdvanceCheckState →
    //    Activating bubbles to Menu → SuperView sees updated value.
    //
    //  FlagSelector (ConsumeDispatch=true) has a DispatchingDown guard that
    //  prevents multi-level dispatch. When Shortcut dispatches to FlagSelector,
    //  FlagSelector cannot further dispatch to its inner CheckBoxes. The value
    //  does NOT change through the Space key path. Inner CheckBox clicks DO
    //  change the value, but FlagSelector consumes the activation (it does not
    //  propagate to Menu or SuperView).
    //
    //  Menu.OnActivating dispatches to the focused MenuItem when focus is set.
    //  Without focus, menu.InvokeCommand(Activate) fires Menu's own Activating
    //  but does NOT dispatch to any MenuItem.
    // ────────────────────────────────────────────────────────────────────

    // Claude - Opus 4.6
    /// <summary>
    ///     Without focus, Menu.OnActivating has no Focused MenuItem to dispatch to.
    ///     Invoking Activate on the Menu fires Menu.Activating and SuperView.Activating,
    ///     but does NOT dispatch to any MenuItem or its CommandView.
    /// </summary>
    [Fact]
    public void Menu_InvokeActivate_Without_Focus_Does_Not_Dispatch_To_MenuItem_CommandView ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        FlagSelector flagSelector = new () { Values = [1, 2, 4], Labels = ["F1", "F2", "F3"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = flagSelector };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        var menuItemActivatingCount = 0;
        menuItem.Activating += (_, _) => menuItemActivatingCount++;

        // Without a running app and focus, Menu.Focused is null
        Assert.Null (menu.Focused);

        menu.InvokeCommand (Command.Activate);

        // Menu's Activating fires and bubbles to SuperView
        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        // MenuItem was NOT activated — no Focused MenuItem to dispatch to
        Assert.Equal (0, menuItemActivatingCount);

        // FlagSelector value unchanged
        Assert.Equal (1, flagSelector.Value);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Space key on a MenuItem with CheckBox CommandView: the full round-trip.
    ///     Dispatch down to CheckBox changes state, then Activating bubbles up to
    ///     Menu and SuperView. The SuperView's Activating handler can query the CheckBox
    ///     and see the correctly updated value.
    /// </summary>
    [Fact]
    public void RoundTrip_Space_CheckBox_Value_Visible_To_SuperView_Activating ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        CheckBox checkBox = new () { Title = "_Toggle" };
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        CheckState? valueSeenByMenu = null;
        CheckState? valueSeenBySuperView = null;

        menu.Activating += (_, _) => { valueSeenByMenu = checkBox.Value; };
        superView.Activating += (_, _) => { valueSeenBySuperView = checkBox.Value; };

        // Space triggers Activate with binding → Shortcut dispatches to CheckBox →
        // CheckBox.OnActivated toggles state → Activating bubbles to Menu → SuperView
        menuItem.NewKeyDownEvent (Key.Space);

        // Value toggled
        Assert.Equal (CheckState.Checked, checkBox.Value);

        // Both Menu and SuperView handlers saw the updated value
        Assert.Equal (CheckState.Checked, valueSeenByMenu);
        Assert.Equal (CheckState.Checked, valueSeenBySuperView);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Space key on a MenuItem with FlagSelector CommandView: Shortcut dispatches down
    ///     to FlagSelector, but the DispatchingDown guard prevents FlagSelector from further
    ///     dispatching to its inner CheckBoxes. FlagSelector.OnActivated checks source — since
    ///     the source is the MenuItem (not a CheckBox), it returns without toggling.
    ///     Activating DOES bubble to Menu and SuperView, but FlagSelector value is unchanged.
    /// </summary>
    [Fact]
    public void RoundTrip_Space_FlagSelector_Value_Does_Not_Change_Via_DispatchDown ()
    {
        View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        FlagSelector flagSelector = new () { Values = [1, 2, 4], Labels = ["F1", "F2", "F3"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = flagSelector };
        menu.Add (menuItem);

        var menuActivatingCount = 0;
        menu.Activating += (_, _) => menuActivatingCount++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        int? initialValue = flagSelector.Value; // Values setter auto-initializes to first entry (1)

        // Space triggers Activate with binding → Shortcut dispatches to FlagSelector (DispatchDown)
        // FlagSelector.TryDispatchToTarget is blocked (DispatchingDown guard) → no inner CheckBox toggle
        // FlagSelector.OnActivated: source is MenuItem, not CheckBox → returns without toggling
        menuItem.NewKeyDownEvent (Key.Space);

        // Activating still bubbles to Menu and SuperView (relay dispatch in Shortcut)
        Assert.Equal (1, menuActivatingCount);
        Assert.Equal (1, superViewActivatingCount);

        // FlagSelector value is unchanged — DispatchDown cannot reach inner CheckBoxes
        Assert.Equal (initialValue, flagSelector.Value);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     With focus set, menu.InvokeCommand(Activate) dispatches to the focused MenuItem
    ///     via Menu.OnActivating. The MenuItem dispatches to its CheckBox CommandView, which
    ///     toggles state. Activating bubbles back to Menu and SuperView, where the handler
    ///     can see the updated CheckBox value.
    /// </summary>
    [Fact]
    public void Menu_InvokeActivate_With_Focus_Dispatches_To_MenuItem_CheckBox_RoundTrip ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View superView = new ()
        {
            Id = "superView",
            CanFocus = true,
            CommandsToBubbleUp = [Command.Activate],
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        CheckBox checkBox = new () { Title = "_Toggle" };
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        ((View)runnable).Add (superView);
        app.Begin (runnable);

        // Explicitly set focus to the MenuItem
        menuItem.SetFocus ();
        Assert.Same (menuItem, menu.Focused);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        CheckState? valueSeenBySuperView = null;

        superView.Activating += (_, _) => { valueSeenBySuperView = checkBox.Value; };

        // Invoke Activate on the Menu — dispatches to focused MenuItem → CheckBox
        menu.InvokeCommand (Command.Activate);

        // CheckBox toggled
        Assert.Equal (CheckState.Checked, checkBox.Value);

        // SuperView saw the updated value during its Activating handler
        Assert.Equal (CheckState.Checked, valueSeenBySuperView);

        ((View)runnable).Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     With focus set, menu.InvokeCommand(Activate) dispatches to the focused MenuItem
    ///     which dispatches to its FlagSelector CommandView. The DispatchingDown guard prevents
    ///     FlagSelector from dispatching to inner CheckBoxes, but FlagSelector.OnActivated uses
    ///     the Focused CheckBox as a fallback target when DispatchingDown routing is detected.
    ///     The focused CheckBox is toggled, and Activating bubbles to Menu and SuperView.
    /// </summary>
    [Fact]
    public void Menu_InvokeActivate_With_Focus_FlagSelector_Toggles_Focused_CheckBox ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View superView = new ()
        {
            Id = "superView",
            CanFocus = true,
            CommandsToBubbleUp = [Command.Activate],
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        FlagSelector flagSelector = new () { Values = [1, 2, 4], Labels = ["F1", "F2", "F3"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = flagSelector };
        menu.Add (menuItem);

        ((View)runnable).Add (superView);
        app.Begin (runnable);

        // Explicitly set focus to the MenuItem
        menuItem.SetFocus ();
        Assert.Same (menuItem, menu.Focused);

        int? initialValue = flagSelector.Value; // Auto-initialized to first entry (1)

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        menu.InvokeCommand (Command.Activate);

        // Activating propagated to SuperView
        Assert.Equal (1, superViewActivatingCount);

        // FlagSelector value HAS changed — Focused fallback toggled the first CheckBox
        Assert.NotEqual (initialValue, flagSelector.Value);

        ((View)runnable).Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     With focus set, menu.InvokeCommand(Activate) dispatches to the focused MenuItem
    ///     which dispatches to its OptionSelector CommandView. The DispatchingDown guard prevents
    ///     OptionSelector from dispatching to inner CheckBoxes, but OptionSelector.ApplyActivation
    ///     uses the Focused CheckBox as a fallback target when DispatchingDown routing is detected.
    ///     The focused option is selected, and Activating bubbles to Menu and SuperView.
    /// </summary>
    [Fact]
    public void Menu_InvokeActivate_With_Focus_OptionSelector_Selects_Focused_Item ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View superView = new ()
        {
            Id = "superView",
            CanFocus = true,
            CommandsToBubbleUp = [Command.Activate],
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Menu menu = new () { Id = "menu" };
        superView.Add (menu);

        OptionSelector selector = new () { Labels = ["Opt1", "Opt2", "Opt3"] };
        MenuItem menuItem = new () { Title = "Test", CommandView = selector };
        menu.Add (menuItem);

        ((View)runnable).Add (superView);
        app.Begin (runnable);

        // Explicitly set focus to the MenuItem, then to the second CheckBox
        menuItem.SetFocus ();
        Assert.Same (menuItem, menu.Focused);

        // Value starts at 0 (first option selected). Focus the second CheckBox.
        Assert.Equal (0, selector.Value);
        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        menu.InvokeCommand (Command.Activate);

        // Activating propagated to SuperView
        Assert.Equal (1, superViewActivatingCount);

        // OptionSelector value changed — Focused fallback selected the second option
        Assert.Equal (1, selector.Value);

        ((View)runnable).Dispose ();
    }

    #endregion Dispatch Down and Round-Trip Value Verification
}
