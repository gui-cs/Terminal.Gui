using Xunit.Abstractions;

//using static Terminal.Gui.ViewTests.MenuTests;

namespace UnitTests_Parallelizable.ViewsTests;

public class MenuBarItemTests ()
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var menuBarItem = new MenuBarItem ();
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);

        menuBarItem = new MenuBarItem (targetView: null, command: Command.NotBound, commandText: null, popoverMenu: null);
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);


    }
}
