using Xunit.Abstractions;

//using static Terminal.Gui.ViewTests.MenuTests;

namespace Terminal.Gui.ViewsTests;

#pragma warning disable CS0618 // Type or member is obsolete
public class Menuv1Tests
{
    private readonly ITestOutputHelper _output;
    public Menuv1Tests (ITestOutputHelper output) { _output = output; }

    // TODO: Create more low-level unit tests for Menu and MenuItem

    [Fact]
    public void Menu_Constructors_Defaults ()
    {
        Assert.Throws<ArgumentNullException> (() => new Menu { Host = null, BarItems = new MenuBarItem () });
        Assert.Throws<ArgumentNullException> (() => new Menu { Host = new MenuBar (), BarItems = null });

        var menu = new Menu { Host = new MenuBar (), X = 0, Y = 0, BarItems = new MenuBarItem () };
        Assert.Empty (menu.Title);
        Assert.Empty (menu.Text);
    }

    [Fact]
    public void MenuItem_Constructors_Defaults ()
    {
        var menuItem = new MenuItem ();
        Assert.Equal ("", menuItem.Title);
        Assert.Equal ("", menuItem.Help);
        Assert.Null (menuItem.Action);
        Assert.Null (menuItem.CanExecute);
        Assert.Null (menuItem.Parent);
        Assert.Equal (Key.Empty, menuItem.ShortcutKey);

        menuItem = new MenuItem ("Test", "Help", Run, () => { return true; }, new MenuItem (), KeyCode.F1);
        Assert.Equal ("Test", menuItem.Title);
        Assert.Equal ("Help", menuItem.Help);
        Assert.Equal (Run, menuItem.Action);
        Assert.NotNull (menuItem.CanExecute);
        Assert.NotNull (menuItem.Parent);
        Assert.Equal (KeyCode.F1, menuItem.ShortcutKey);

        void Run () { }
    }

    [Fact]
    public void MenuBarItem_SubMenu_Can_Return_Null ()
    {
        var menuItem = new MenuItem ();
        var menuBarItem = new MenuBarItem ();
        Assert.Null (menuBarItem.SubMenu (menuItem));
    }

    [Fact]
    public void MenuBarItem_Constructors_Defaults ()
    {
        var menuBarItem = new MenuBarItem ();
        Assert.Equal ("", menuBarItem.Title);
        Assert.Equal ("", menuBarItem.Help);
        Assert.Null (menuBarItem.Action);
        Assert.Null (menuBarItem.CanExecute);
        Assert.Null (menuBarItem.Parent);
        Assert.Equal (Key.Empty, menuBarItem.ShortcutKey);
        Assert.Equal ([], menuBarItem.Children);
        Assert.False (menuBarItem.IsTopLevel);

        menuBarItem = new MenuBarItem (null!, null!, Run, () => true, new ());
        Assert.Equal ("", menuBarItem.Title);
        Assert.Equal ("", menuBarItem.Help);
        Assert.Equal (Run, menuBarItem.Action);
        Assert.NotNull (menuBarItem.CanExecute);
        Assert.NotNull (menuBarItem.Parent);
        Assert.Equal (Key.Empty, menuBarItem.ShortcutKey);
        Assert.Null (menuBarItem.Children);
        Assert.False (menuBarItem.IsTopLevel);

        menuBarItem = new MenuBarItem (null!, Array.Empty<MenuItem> (), new ());
        Assert.Equal ("", menuBarItem.Title);
        Assert.Equal ("", menuBarItem.Help);
        Assert.Null (menuBarItem.Action);
        Assert.Null (menuBarItem.CanExecute);
        Assert.NotNull (menuBarItem.Parent);
        Assert.Equal (Key.Empty, menuBarItem.ShortcutKey);
        Assert.Equal ([], menuBarItem.Children);
        Assert.False (menuBarItem.IsTopLevel);

        menuBarItem = new MenuBarItem (null!, new List<MenuItem []> (), new ());
        Assert.Equal ("", menuBarItem.Title);
        Assert.Equal ("", menuBarItem.Help);
        Assert.Null (menuBarItem.Action);
        Assert.Null (menuBarItem.CanExecute);
        Assert.NotNull (menuBarItem.Parent);
        Assert.Equal (Key.Empty, menuBarItem.ShortcutKey);
        Assert.Equal ([], menuBarItem.Children);
        Assert.False (menuBarItem.IsTopLevel);

        menuBarItem = new MenuBarItem ([]);
        Assert.Equal ("", menuBarItem.Title);
        Assert.Equal ("", menuBarItem.Help);
        Assert.Null (menuBarItem.Action);
        Assert.Null (menuBarItem.CanExecute);
        Assert.Null (menuBarItem.Parent);
        Assert.Equal (Key.Empty, menuBarItem.ShortcutKey);
        Assert.Equal ([], menuBarItem.Children);
        Assert.False (menuBarItem.IsTopLevel);

        void Run () { }
    }
}
