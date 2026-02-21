using Terminal.Gui.Drawing;
using Terminal.Gui.Testing;

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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        MenuBar menuBar = new () { Id = "menuBar" };
        menuBar.EnableForDesign (ref hostView);
        hostView.Add (menuBar);

        ((View)runnable).Add (hostView);
        app.Begin (runnable);

        MenuBarItem? menuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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

        MenuBarItem? menuBarItem = menuBar.SubViews.OfType<MenuBarItem> ().First ();
        PopoverMenu? popoverMenu = menuBarItem.PopoverMenu;
        Menu? menu = popoverMenu?.Root;
        MenuItem? menuItem = menu?.SubViews.OfType<MenuItem> ().First ();

        MenuItem? focused = app.Navigation?.GetFocused () as MenuItem;
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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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

        MenuItem? prefsItem = rootMenu!.SubViews.OfType<MenuItem> ()
                                       .FirstOrDefault (mi => mi.Title == "_Preferences");
        Assert.NotNull (prefsItem);
        Assert.NotNull (prefsItem!.SubMenu);

        // Make Preferences SubMenu visible by navigating to it
        prefsItem.SetFocus ();

        // Find the MenuItem with the OptionSelector CommandView
        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ()
                                            .FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        Assert.NotNull (optionMenuItem);

        OptionSelector<Schemes>? optionSelector = optionMenuItem!.CommandView as OptionSelector<Schemes>;
        Assert.NotNull (optionSelector);
        Assert.Equal (Schemes.Base, optionSelector!.Value);

        // Find the "Error" checkbox (index 4)
        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ()
                                                .FirstOrDefault (cb => (int)cb.Data! == (int)Schemes.Error);
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
        errorCheckBox!.SetFocus ();
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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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
        MenuItem? prefsItem = rootMenu!.SubViews.OfType<MenuItem> ()
                                       .FirstOrDefault (mi => mi.Title == "_Preferences");
        prefsItem!.SetFocus ();

        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ()
                                            .FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        OptionSelector<Schemes>? optionSelector = optionMenuItem!.CommandView as OptionSelector<Schemes>;
        Assert.Equal (Schemes.Base, optionSelector!.Value);

        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ()
                                                .FirstOrDefault (cb => (int)cb.Data! == (int)Schemes.Error);

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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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
        MenuItem? prefsItem = rootMenu!.SubViews.OfType<MenuItem> ()
                                       .FirstOrDefault (mi => mi.Title == "_Preferences");
        prefsItem!.SetFocus ();

        MenuItem? optionMenuItem = prefsItem.SubMenu!.SubViews.OfType<MenuItem> ()
                                            .FirstOrDefault (mi => mi.Id == "mutuallyExclusiveOptions");
        OptionSelector<Schemes>? optionSelector = optionMenuItem!.CommandView as OptionSelector<Schemes>;
        Assert.Equal (Schemes.Base, optionSelector!.Value);

        CheckBox? errorCheckBox = optionSelector.SubViews.OfType<CheckBox> ()
                                                .FirstOrDefault (cb => (int)cb.Data! == (int)Schemes.Error);

        Schemes? newValue = null;
        var valueChangedCount = 0;

        optionSelector.ValueChanged += (_, args) =>
                                       {
                                           newValue = args.Value;
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

        View hostView = new ()
        {
            Id = "host",
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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
}
