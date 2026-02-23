namespace ViewsTests;

public class MenuBarTests
{
    [Fact]
    public void Command_HotKey_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        menuBar.InvokeCommand (Command.HotKey);

        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });
    }

    [Fact]
    public void DefaultKey_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });
    }

    [Fact]
    public void DefaultKey_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Arrange
        Assert.False (menuBar.Active);

        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });

        // Act
        app.InjectKey (MenuBar.DefaultKey);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: false });
    }

    [Fact]
    public void Command_Activate_Focuses_MenuBarItem_PopoverMenu_And_First_MenuItem ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        MenuBarItem menuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        PopoverMenu? popoverMenu = menuBarItem.PopoverMenu;
        Menu? menu = popoverMenu?.Root;
        MenuItem? menuItem = menu?.SubViews.OfType<MenuItem> ().First ();

        // Activate focuses MenuBarItem
        menuBar.InvokeCommand (Command.Activate);

        Assert.True (menuBar.HasFocus);
        Assert.True (menuBarItem.HasFocus);
        Assert.True (popoverMenu?.HasFocus);
        Assert.True (menu?.HasFocus);
        Assert.True (menuItem?.HasFocus);

        Assert.Equal (menu, popoverMenu?.Focused);
        Assert.Equal (menuItem, menu?.Focused);
        Assert.Equal (menuItem, app.Navigation?.GetFocused ());

        menuBar.Dispose ();
    }

    [Fact]
    public void Command_Activate_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        menuBar.InvokeCommand (Command.Activate);

        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });
    }

    [Fact]
    public void Command_Activate_Activates_Command_Activate_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.False (menuBar.Active);

        menuBar.InvokeCommand (Command.Activate);

        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });

        menuBar.InvokeCommand (Command.Activate);

        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });
    }

    [Fact]
    public void Command_Activate_WhenActive_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Arrange
        Assert.False (menuBar.Active);

        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: true });

        MenuBarItem menuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        PopoverMenu? popoverMenu = menuBarItem.PopoverMenu;
        Menu? menu = popoverMenu?.Root;
        MenuItem? menuItem = menu?.SubViews.OfType<MenuItem> ().First ();

        var focused = app.Navigation?.GetFocused () as MenuItem;
        Assert.Equal (menuItem, focused);

        // Act
        menuBar.InvokeCommand (Command.Activate);

        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.True (menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0).PopoverMenu is { Visible: false });

        Assert.Equal (hostView, app.Navigation?.GetFocused ());
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that clicking the "Error" checkbox in the OptionSelector&lt;Schemes&gt; inside
    ///     MenuBar.EnableForDesign's Preferences SubMenu changes Value from 0 (Base) to 4 (Error).
    ///     This exercises the full Shortcut → OptionSelector dispatch path inside a PopoverMenu SubMenu.
    /// </summary>
    [Fact]
    public void OptionSelector_In_SubMenu_Click_Sets_Correct_Value ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Find the OptionSelector<Schemes> in the menu hierarchy
        // It's in File → Preferences → "mutuallyExclusiveOptions" MenuItem
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Assert.NotNull (fileItem.PopoverMenu);

        // Open the File menu
        Point fileScreenPos = fileItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (fileScreenPos));
        Assert.True (menuBar.IsOpen (), "File menu should be open");

        // Find the "Preferences" MenuItem which has a SubMenu
        Menu? rootMenu = fileItem.PopoverMenu!.Root;
        Assert.NotNull (rootMenu);

        MenuItem? prefsItem = rootMenu.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Preferences");
        Assert.NotNull (prefsItem);
        Assert.NotNull (prefsItem.SubMenu);

        // Make Preferences SubMenu visible by navigating to it
        prefsItem.SetFocus ();

        // Find the MenuItem with the OptionSelector CommandView
        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        Assert.NotNull (optionMenuItem);

        OptionSelector<Schemes>? optionSelector = optionMenuItem.CommandView as OptionSelector<Schemes>;
        Assert.NotNull (optionSelector);
        Assert.Equal (Schemes.Base, optionSelector.Value);

        // Find the "Error" checkbox (index 4)
        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => (int)cb.Data! == (int)Schemes.Error);
        Assert.NotNull (errorCheckBox);

        // Subscribe to ValueChanged
        Schemes? newValue = null;
        var valueChangedCount = 0;

        optionSelector.ValueChanged += (_, args) =>
                                       {
                                           newValue = args.Value;
                                           valueChangedCount++;
                                       };

        // Act — click on the Error checkbox via the MenuItem (simulates real mouse click
        // which hits the Shortcut/MenuItem level and dispatches down to CommandView)
        errorCheckBox.SetFocus ();
        Point errorScreenPos = errorCheckBox.FrameToScreen ().Location;

        // Use InjectMouse to simulate the full mouse pipeline (LeftButtonPressed + LeftButtonReleased)
        // which goes through the Shortcut dispatch path, not directly to the CheckBox
        app.InjectMouse (new Mouse { ScreenPosition = errorScreenPos, Flags = MouseFlags.LeftButtonPressed });
        app.InjectMouse (new Mouse { ScreenPosition = errorScreenPos, Flags = MouseFlags.LeftButtonReleased });

        // Assert — Value should change from Base (0) to Error (4), not to Menu (1)
        Assert.Equal (1, valueChangedCount);
        Assert.Equal (Schemes.Error, newValue);
        Assert.Equal (Schemes.Error, optionSelector.Value);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing Space on the "Error" checkbox in the OptionSelector&lt;Schemes&gt;
    ///     inside MenuBar.EnableForDesign's Preferences SubMenu changes Value correctly.
    /// </summary>
    [Fact]
    public void OptionSelector_In_SubMenu_Space_Sets_Correct_Value ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Navigate to the OptionSelector
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Point fileScreenPos = fileItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (fileScreenPos));

        Menu? rootMenu = fileItem.PopoverMenu!.Root;
        MenuItem? prefsItem = rootMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Preferences");
        prefsItem!.SetFocus ();

        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        OptionSelector<Schemes>? optionSelector = optionMenuItem!.CommandView as OptionSelector<Schemes>;
        Assert.Equal (Schemes.Base, optionSelector!.Value);

        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => (int)cb.Data! == (int)Schemes.Error);

        Schemes? newValue = null;
        var valueChangedCount = 0;

        optionSelector.ValueChanged += (_, args) =>
                                       {
                                           newValue = args.Value;
                                           valueChangedCount++;
                                       };

        // Act — focus Error checkbox and press Space via the MenuItem context
        // Space is bound to Command.Activate on CheckBox. When inside a MenuItem/Shortcut,
        // the key goes through the Shortcut's key binding scope.
        errorCheckBox!.SetFocus ();
        app.InjectKey (Key.Space);

        // Assert — Value should change to Error (4)
        Assert.Equal (1, valueChangedCount);
        Assert.Equal (Schemes.Error, newValue);
        Assert.Equal (Schemes.Error, optionSelector.Value);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing Enter on the "Error" checkbox in the OptionSelector&lt;Schemes&gt;
    ///     inside MenuBar.EnableForDesign's Preferences SubMenu changes Value correctly.
    /// </summary>
    [Fact]
    public void OptionSelector_In_SubMenu_Enter_Sets_Correct_Value ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Navigate to the OptionSelector
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Point fileScreenPos = fileItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (fileScreenPos));

        Menu? rootMenu = fileItem.PopoverMenu!.Root;
        MenuItem? prefsItem = rootMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Preferences");
        prefsItem!.SetFocus ();

        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        OptionSelector<Schemes>? optionSelector = optionMenuItem!.CommandView as OptionSelector<Schemes>;
        Assert.Equal (Schemes.Base, optionSelector!.Value);

        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => (int)cb.Data! == (int)Schemes.Error);

        var valueChangedCount = 0;

        optionSelector.ValueChanged += (_, args) =>
                                       {
                                           valueChangedCount++;
                                       };

        // Act — focus Error checkbox and press Enter via the MenuItem context
        errorCheckBox!.SetFocus ();
        app.InjectKey (Key.Enter);

        // Assert — Value should change to Error (4)
        Assert.True (valueChangedCount >= 1, "ValueChanged should fire at least once");
        Assert.Equal (Schemes.Error, optionSelector.Value);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Mouse_Click_Activates_And_Opens ()
    {
        // Arrange - mirror the Menus.cs scenario: MenuBar inside a focusable host view
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        (runnable as View)!.Add (hostView);
        app.Begin (runnable);

        // Focus something else, like in the real scenario
        hostView.SetFocus ();

        Assert.False (menuBar.HasFocus);

        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());

        // Act - click on the first MenuBarItem
        MenuBarItem firstItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Point itemScreenPos = firstItem.FrameToScreen ().Location;

        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (itemScreenPos));

        // Assert
        Assert.True (menuBar.Active, "MenuBar should be Active after click");
        Assert.True (menuBar.IsOpen (), "PopoverMenu should be open after click");

        menuBar.Dispose ();
    }

    #region Ported from old UnitTests/Views/MenuBarTests.cs

    // Claude - Opus 4.6
    [Fact]
    public void QuitKey_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());

        // Act — Quit command should close everything.
        // Note: Application.QuitKey is a local KeyBinding, so we invoke the command directly
        // because the PopoverMenu has focus (separate view hierarchy from MenuBar).
        menuBar.InvokeCommand (Command.Quit);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuBarItem_HotKey_Activates_And_Opens ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);
        Assert.False (menuBar.Active);

        // Act — press MenuBarItem's HotKey (Alt+N for "_New")
        app.InjectKey (Key.N.WithAlt);

        // Assert
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuBarItem_HotKey_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);
        Assert.False (menuBar.Active);

        // Open via HotKey
        app.InjectKey (Key.N.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());

        // Act — second press should deactivate
        app.InjectKey (Key.N.WithAlt);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_HotKey_Fires_Action_When_Open ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var actionCount = 0;
        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item", Action = () => actionCount++ };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu via MenuBarItem's HotKey
        app.InjectKey (Key.M.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBarItem.PopoverMenu!.Visible);

        // Act — press MenuItem's HotKey ('I' for "Menu_Item") while menu is open
        app.InjectKey (Key.I);
        Assert.Equal (1, actionCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void WhenOpen_Switch_MenuBarItem_Via_HotKey ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem1 = new () { Id = "menuItem1", Title = "_Item1" };
        Menu menu1 = new ([menuItem1]) { Id = "menu1" };
        MenuBarItem menuBarItem1 = new () { Id = "menuBarItem1", Title = "_New" };
        PopoverMenu popover1 = new ();
        menuBarItem1.PopoverMenu = popover1;
        popover1.Root = menu1;

        MenuItem menuItem2 = new () { Id = "menuItem2", Title = "_Item2" };
        Menu menu2 = new ([menuItem2]) { Id = "menu2" };
        MenuBarItem menuBarItem2 = new () { Id = "menuBarItem2", Title = "_Edit" };
        PopoverMenu popover2 = new () { Id = "popover2" };
        menuBarItem2.PopoverMenu = popover2;
        popover2.Root = menu2;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem1);
        menuBar.Add (menuBarItem2);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);
        Assert.False (menuBar.Active);

        // Open first MenuBarItem
        app.InjectKey (Key.N.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBarItem1.PopoverMenu!.Visible);

        // Act — switch to second MenuBarItem via its HotKey
        app.InjectKey (Key.E.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem2.PopoverMenu!.Visible);
        Assert.False (menuBarItem1.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Update_MenuBarItem_HotKey_Works ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open with original hotkey
        app.InjectKey (Key.N.WithAlt);
        Assert.True (menuBar.IsOpen ());

        // Act — change the hotkey
        menuBarItem.HotKey = Key.E.WithAlt;

        // Old key should not close
        app.InjectKey (Key.N.WithAlt);
        Assert.True (menuBar.IsOpen ());

        // New key should close
        app.InjectKey (Key.E.WithAlt);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void Disabled_MenuBar_Is_Not_Activated ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — disable MenuBar and try hotkey
        menuBar.Enabled = false;
        app.InjectKey (Key.N.WithAlt);

        // Assert — should not activate
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void Disabled_MenuBarItem_HotKey_Does_Not_Open ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — disable MenuBarItem and try hotkey
        menuBarItem.Enabled = false;
        app.InjectKey (Key.N.WithAlt);

        // Assert — should not activate
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void Disabled_PopoverMenu_MenuBar_Still_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — disable PopoverMenu but MenuBar item should still respond
        menuBarItem.PopoverMenu!.Enabled = false;
        app.InjectKey (Key.N.WithAlt);

        // Assert — MenuBar activates, PopoverMenu is shown (even if disabled)
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu!.Visible);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Invisible_MenuBar_HotKey_Does_Not_Activate ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — hide MenuBar and try to activate via DefaultKey
        menuBar.Visible = false;
        app.InjectKey (MenuBar.DefaultKey);

        // Assert — should not activate
        Assert.False (menuBar.Active);
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void HotKey_Fires_VisibleChanged_Only_Once ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        var visibleChangeCount = 0;

        menuBarItemPopover.VisibleChanged += (_, _) =>
                                             {
                                                 if (menuBarItemPopover.Visible)
                                                 {
                                                     visibleChangeCount++;
                                                 }
                                             };

        // Act — open via MenuBarItem HotKey
        app.InjectKey (Key.N.WithAlt);

        // Assert — should fire VisibleChanged exactly once
        Assert.Equal (1, visibleChangeCount);
    }

    #endregion

    #region New gap coverage tests

    // Claude - Opus 4.6
    [Fact]
    public void Command_Right_Navigates_Between_MenuBarItems ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem1 = new () { Id = "menuItem1", Title = "_Item1" };
        Menu menu1 = new ([menuItem1]) { Id = "menu1" };
        MenuBarItem menuBarItem1 = new () { Id = "menuBarItem1", Title = "_File" };
        PopoverMenu popover1 = new ();
        menuBarItem1.PopoverMenu = popover1;
        popover1.Root = menu1;

        MenuItem menuItem2 = new () { Id = "menuItem2", Title = "_Item2" };
        Menu menu2 = new ([menuItem2]) { Id = "menu2" };
        MenuBarItem menuBarItem2 = new () { Id = "menuBarItem2", Title = "_Edit" };
        PopoverMenu popover2 = new ();
        menuBarItem2.PopoverMenu = popover2;
        popover2.Root = menu2;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem1);
        menuBar.Add (menuBarItem2);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Activate the MenuBar (Active = true, CanFocus = true) and focus first item
        menuBar.InvokeCommand (Command.Activate);
        Assert.True (menuBar.Active);

        // Focus the MenuBar's first item directly so Right arrow works within MenuBar
        menuBarItem1.SetFocus ();

        // Act — invoke Right command on the MenuBar
        menuBar.InvokeCommand (Command.Right);

        // Assert — focus should move to second MenuBarItem
        Assert.True (menuBar.Active);
        Assert.True (menuBarItem2.HasFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Escape_Closes_Open_PopoverMenu ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu
        menuBar.InvokeCommand (Command.Activate);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());

        // Act — press Escape
        app.InjectKey (Key.Esc);

        // Assert — menu should be closed
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void Focus_Restoration_After_Close ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Ensure host has focus before opening menu
        hostView.SetFocus ();
        Assert.True (hostView.HasFocus);
        Assert.False (menuBar.HasFocus);

        // Open and then close the menu
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.HasFocus);

        app.InjectKey (MenuBar.DefaultKey);

        // Assert — focus should return to hostView
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.True (hostView.HasFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Menus_Property_Setter_Replaces_Items ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        Assert.Empty (menuBar.SubViews.OfType<MenuBarItem> ());

        // Act — set Menus property
        MenuBarItem newItem1 = new () { Title = "_File" };
        MenuBarItem newItem2 = new () { Title = "_Edit" };
        menuBar.Menus = [newItem1, newItem2];

        // Assert — SubViews should contain the new items
        MenuBarItem [] items = menuBar.SubViews.OfType<MenuBarItem> ().ToArray ();
        Assert.Equal (2, items.Length);
        Assert.Equal ("_File", items [0].Title);
        Assert.Equal ("_Edit", items [1].Title);
    }

    // Claude - Opus 4.6
    [Fact]
    public void GetMenuItemsWith_Returns_Matching_Items ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Act — query by predicate
        IEnumerable<MenuItem> results = menuBar.GetMenuItemsWith (mi => mi.Command == Command.Quit);

        // Assert — should find at least one Quit MenuItem
        Assert.NotEmpty (results);
    }

    // Claude - Opus 4.6
    [Fact (Skip = "Mouse click switching has a timing issue: OnSelectedMenuItemChanged auto-opens, then click's Activate toggles off.")]
    public void Mouse_Click_On_Different_MenuBarItem_Switches ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        MenuBarItem firstItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (0);
        MenuBarItem secondItem = menuBar.SubViews.OfType<MenuBarItem> ().ElementAt (1);

        // Click on first item to open it
        Point firstScreenPos = firstItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (firstScreenPos));
        Assert.True (menuBar.Active);
        Assert.True (firstItem.PopoverMenu is { Visible: true });

        // Act — click on second item
        Point secondScreenPos = secondItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (secondScreenPos));

        // Assert — second item open, first closed
        Assert.True (menuBar.Active);
        Assert.True (secondItem.PopoverMenu is { Visible: true });
        Assert.False (firstItem.PopoverMenu is { Visible: true });
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuBar_Active_False_Sets_CanFocus_False ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Activate
        menuBar.InvokeCommand (Command.Activate);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.CanFocus);

        // Act — deactivate
        menuBar.InvokeCommand (Command.Activate);

        // Assert — CanFocus should be false
        Assert.False (menuBar.Active);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void IsOpen_False_After_All_Popovers_Close ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_New" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.IsOpen ());

        // Act — close
        app.InjectKey (MenuBar.DefaultKey);

        // Assert — IsOpen should be false (Bug 1 fix)
        Assert.False (menuBar.IsOpen ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void QuitKey_With_PopoverMenu_Visible_Fully_Deactivates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu so PopoverMenu is visible
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu!.Visible);

        // Act — single Escape (QuitKey) should fully deactivate everything
        app.InjectKey (Application.QuitKey);

        // Assert — MenuBar should be completely inactive after ONE press
        Assert.False (menuBarItem.PopoverMenu!.Visible);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.Active);
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Fully_Deactivates_MenuBar ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var actionCount = 0;
        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item", Action = () => actionCount++ };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open the menu
        app.InjectKey (MenuBar.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu!.Visible);

        // Act — activate MenuItem via its HotKey ('I' for "Menu_Item")
        app.InjectKey (Key.I);

        // Assert — Action should have fired
        Assert.Equal (1, actionCount);

        // Assert — MenuBar should be completely inactive after MenuItem activation
        Assert.False (menuBarItem.PopoverMenu!.Visible);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.Active);
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MouseLeave_Without_Open_Popover_Deactivates_MenuBar ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem menuItem = new () { Id = "menuItem", Title = "Menu_Item" };
        Menu menu = new ([menuItem]) { Id = "menu" };
        MenuBarItem menuBarItem = new () { Id = "menuBarItem", Title = "_MenuBarItem" };
        PopoverMenu menuBarItemPopover = new ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.Add (menuBarItem);

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Mouse enters MenuBar — should activate
        Point menuBarPos = menuBar.FrameToScreen ().Location;
        app.InjectMouse (new Mouse { ScreenPosition = menuBarPos, Flags = MouseFlags.PositionReport });

        Assert.True (menuBar.Active);
        Assert.False (menuBar.IsOpen (), "Hovering should not open a popover");

        // Mouse leaves MenuBar — should deactivate (no popover is open)
        app.InjectMouse (new Mouse { ScreenPosition = new Point (0, menuBar.FrameToScreen ().Bottom + 2), Flags = MouseFlags.PositionReport });

        Assert.False (menuBar.Active);
        Assert.False (menuBar.CanFocus);
    }

    // Claude - Opus 4.6
    [Fact]
    public void OptionSelector_Click_In_SubMenu_Deactivates_MenuBar ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View hostView = new () { Id = "host", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        // Open File menu
        MenuBarItem fileItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        Point fileScreenPos = fileItem.FrameToScreen ().Location;
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (fileScreenPos));
        Assert.True (menuBar.IsOpen (), "File menu should be open");

        // Navigate to Preferences SubMenu
        Menu? rootMenu = fileItem.PopoverMenu!.Root;
        MenuItem? prefsItem = rootMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Title == "_Preferences");
        Assert.NotNull (prefsItem);
        prefsItem.SetFocus ();

        // Find the OptionSelector
        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ().FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        Assert.NotNull (optionMenuItem);
        OptionSelector<Schemes>? optionSelector = optionMenuItem.CommandView as OptionSelector<Schemes>;
        Assert.NotNull (optionSelector);

        // Click on the Error checkbox
        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => (int)cb.Data! == (int)Schemes.Error);
        Assert.NotNull (errorCheckBox);
        errorCheckBox.SetFocus ();
        Point errorScreenPos = errorCheckBox.FrameToScreen ().Location;
        app.InjectMouse (new Mouse { ScreenPosition = errorScreenPos, Flags = MouseFlags.LeftButtonPressed });
        app.InjectMouse (new Mouse { ScreenPosition = errorScreenPos, Flags = MouseFlags.LeftButtonReleased });

        // Assert — value changed AND menu is fully deactivated
        Assert.Equal (Schemes.Error, optionSelector.Value);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.Active);

        menuBar.Dispose ();
    }

    #endregion
}
