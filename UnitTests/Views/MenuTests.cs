using Xunit.Abstractions;

//using static Terminal.Gui.ViewTests.MenuTests;

namespace Terminal.Gui.ViewsTests;

public class MenuTests
{
    public MenuTests (ITestOutputHelper output) { _output = output; }
    private readonly ITestOutputHelper _output;

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
        Assert.Equal (KeyCode.Null, menuItem.Shortcut);

        menuItem = new MenuItem ("Test", "Help", Run, () => { return true; }, new MenuItem (), KeyCode.F1);
        Assert.Equal ("Test", menuItem.Title);
        Assert.Equal ("Help", menuItem.Help);
        Assert.Equal (Run, menuItem.Action);
        Assert.NotNull (menuItem.CanExecute);
        Assert.NotNull (menuItem.Parent);
        Assert.Equal (KeyCode.F1, menuItem.Shortcut);

        void Run () { }
    }
}
