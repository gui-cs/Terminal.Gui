using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class MenuBarTests ()
{
    [Fact]
    [AutoInitShutdown]
    public void DefaultKey_Activates_And_Opens ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        Application.RaiseKeyDownEvent (MenuBarv2.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.HasFocus);
        Assert.True (menuBar.CanFocus);
        Assert.True (menuBarItem.PopoverMenu.Visible);
        Assert.True (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DefaultKey_Deactivates ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        Application.RaiseKeyDownEvent (MenuBarv2.DefaultKey);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu.Visible);

        Application.RaiseKeyDownEvent (MenuBarv2.DefaultKey);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu.Visible);
        Assert.False (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void QuitKey_Deactivates ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "Menu_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_MenuBarItem" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);

        Application.RaiseKeyDownEvent (MenuBarv2.DefaultKey);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu.Visible);

        // Act

        Application.RaiseKeyDownEvent (Application.QuitKey);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu.Visible);
        Assert.False (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBarItem_HotKey_Activates_And_Opens ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.HasFocus);
        Assert.True (menuBarItem.PopoverMenu.Visible);
        Assert.True (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBarItem_HotKey_Deactivates ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu.Visible);

        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu.Visible);
        Assert.False (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuItems_HotKey_RaisesAction ()
    {
        // Arrange
        int action = 0;
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "Menu_Item", Action = () => action++ };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_MenuBarItem" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        Application.RaiseKeyDownEvent (Key.M.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.CanFocus);
        Assert.True (menuBarItem.PopoverMenu.Visible);

        // Act
        Application.RaiseKeyDownEvent (Key.I);
        Assert.Equal (1, action);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuItems_HotKey_Deactivates ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "Menu_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_MenuBarItem" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        Application.RaiseKeyDownEvent (Key.M.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBarItem.PopoverMenu.Visible);

        // Act
        Application.RaiseKeyDownEvent (Key.I);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu.Visible);
        Assert.False (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_Makes_PopoverMenu_Visible_Only_Once ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        int visibleChangeCount = 0;
        menuBarItemPopover.VisibleChanged += (sender, args) =>
                                             {
                                                 if (menuBarItemPopover.Visible)
                                                 {
                                                     visibleChangeCount++;
                                                 }
                                             };

        // Act
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.Equal (1, visibleChangeCount);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void WhenOpen_Other_MenuBarItem_HotKey_Activates_And_Opens ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;

        var menuItem2 = new MenuItemv2 { Id = "menuItem2", Title = "_Copy" };
        var menu2 = new Menuv2 ([menuItem2]) { Id = "menu2" };
        var menuBarItem2 = new MenuBarItemv2 () { Id = "menuBarItem2", Title = "_Edit" };
        var menuBarItemPopover2 = new PopoverMenu () { Id = "menuBarItemPopover2" };
        menuBarItem2.PopoverMenu = menuBarItemPopover2;
        menuBarItemPopover2.Root = menu2;

        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        menuBar.Add (menuBarItem2);

        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());

        // Act
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu.Visible);

        Application.RaiseKeyDownEvent (Key.E.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem2.PopoverMenu.Visible);
        Assert.False (menuBarItem.PopoverMenu.Visible);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Mouse_Enter_Activates_But_Does_Not_Open ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        Application.RaiseMouseEvent (new ()
        {
            Flags = MouseFlags.ReportMousePosition
        });
        Assert.True (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.True (menuBar.HasFocus);
        Assert.True (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu.Visible);
        Assert.False (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Mouse_Click_Activates_And_Opens ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        Application.RaiseMouseEvent (new ()
        {
            Flags = MouseFlags.Button1Clicked
        });
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.HasFocus);
        Assert.True (menuBar.CanFocus);
        Assert.True (menuBarItem.PopoverMenu.Visible);
        Assert.True (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    // QUESTION: Windows' menus close the menu when you click on the menu bar item again.
    // QUESTION: What does Mac do?
    // QUESTION: How bad is it that this test is skipped?
    // QUESTION: Fixing this could be challenging. Should we fix it?
    [Fact (Skip = "Clicking outside Popover, passes mouse event to MenuBar, which activates the same item again.")]
    [AutoInitShutdown]
    public void Mouse_Click_Deactivates ()
    {
        // Arrange
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        Application.RaiseMouseEvent (new ()
        {
            Flags = MouseFlags.Button1Clicked
        });
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.HasFocus);
        Assert.True (menuBar.CanFocus);
        Assert.True (menuBarItem.PopoverMenu.Visible);
        Assert.True (menuBarItem.PopoverMenu.HasFocus);

        // Act
        Application.RaiseMouseEvent (new ()
        {
            Flags = MouseFlags.Button1Clicked
        });
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu.Visible);
        Assert.False (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Dynamic_Change_MenuItem_Title ()
    {
        // Arrange
        int action = 0;
        var menuItem = new MenuItemv2 { Title = "_Item", Action = () => action++ };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 ();
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);

        Assert.False (menuBar.Active);
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.Equal (0, action);

        Assert.Equal(Key.I, menuItem.HotKey);
        Application.RaiseKeyDownEvent (Key.I);
        Assert.Equal (1, action);
        Assert.False (menuBar.Active);

        menuItem.Title = "_Foo";
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.True (menuBar.Active);
        Application.RaiseKeyDownEvent (Key.I);
        Assert.Equal (1, action);
        Assert.True (menuBar.Active);

        Application.RaiseKeyDownEvent (Key.F);
        Assert.Equal (2, action);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Disabled_MenuBar_Is_Not_Activated ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        menuBar.Enabled = false;
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBarItem.PopoverMenu.Visible);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBarItem_Disabled_MenuBarItem_HotKey_No_Activate_Or_Open ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        menuBarItem.Enabled = false;
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBarItem.PopoverMenu.Visible);

        Application.End (rs);
        top.Dispose ();
    }


    [Fact]
    [AutoInitShutdown]
    public void MenuBarItem_Disabled_Popover_Is_Activated ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        menuBarItem.PopoverMenu.Enabled = false;
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.True (menuBar.Active);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBarItem.PopoverMenu.Visible);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact (Skip = "For v2, should the menu close on resize?")]
    [AutoInitShutdown]
    public void Resizing_Closes_Menus ()
    {

    }

    [Fact]
    [AutoInitShutdown]
    public void Update_MenuBarItem_HotKey_Works ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.HasFocus);
        Assert.True (menuBarItem.PopoverMenu.Visible);
        Assert.True (menuBarItem.PopoverMenu.HasFocus);

        // Act
        menuBarItem.HotKey = Key.E.WithAlt;

        // old key should do nothing
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.True (menuBar.IsOpen ());
        Assert.True (menuBar.HasFocus);
        Assert.True (menuBarItem.PopoverMenu.Visible);
        Assert.True (menuBarItem.PopoverMenu.HasFocus);

        // use new key
        Application.RaiseKeyDownEvent (Key.E.WithAlt);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu.Visible);
        Assert.False (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Visible_False_HotKey_Does_Not_Activate ()
    {
        // Arrange
        var menuItem = new MenuItemv2 { Id = "menuItem", Title = "_Item" };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        menuBar.Visible = false;
        Application.RaiseKeyDownEvent (Key.N.WithAlt);
        Assert.False (menuBar.Active);
        Assert.False (menuBar.IsOpen ());
        Assert.False (menuBar.HasFocus);
        Assert.False (menuBar.CanFocus);
        Assert.False (menuBarItem.PopoverMenu.Visible);
        Assert.False (menuBarItem.PopoverMenu.HasFocus);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Visible_False_MenuItem_Key_Does_Action ()
    {
        // Arrange
        int action = 0;
        var menuItem = new MenuItemv2 ()
        {
            Id = "menuItem",
            Title = "_Item",
            Key = Key.F1,
            Action = () => action++
        };
        var menu = new Menuv2 ([menuItem]) { Id = "menu" };
        var menuBarItem = new MenuBarItemv2 { Id = "menuBarItem", Title = "_New" };
        var menuBarItemPopover = new PopoverMenu ();
        menuBarItem.PopoverMenu = menuBarItemPopover;
        menuBarItemPopover.Root = menu;
        var menuBar = new MenuBarv2 () { Id = "menuBar" };
        menuBar.Add (menuBarItem);
        Assert.Single (menuBar.SubViews);
        Assert.Single (menuBarItem.SubViews);
        var top = new Toplevel ();
        top.Add (menuBar);
        RunState rs = Application.Begin (top);
        Assert.False (menuBar.Active);

        // Act
        menuBar.Visible = false;
        Application.RaiseKeyDownEvent (Key.F1);

        Assert.Equal (1, action);

        Application.End (rs);
        top.Dispose ();
    }
}
