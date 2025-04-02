using Xunit.Abstractions;

//using static Terminal.Gui.ViewTests.MenuTests;

namespace Terminal.Gui.ViewsTests;

public class MenuBarItemTests ()
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var menuBarItem = new MenuBarItemv2 ();
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);

        menuBarItem = new MenuBarItemv2 (targetView: null, command: Command.NotBound, commandText: null, popoverMenu: null);
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);


    }
}
