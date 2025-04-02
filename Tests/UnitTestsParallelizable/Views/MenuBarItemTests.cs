using Xunit.Abstractions;

//using static Terminal.Gui.ViewTests.MenuTests;

namespace Terminal.Gui.ViewsTests;

public class MenuBarItemTests ()
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var menuBarItem = new MenuBarItemv2 ();
        Assert.Empty (menuBarItem.SubViews);
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);

        menuBarItem = new MenuBarItemv2 (targetView: null, command: Command.NotBound, commandText: null, popoverMenu: null);
        Assert.Empty (menuBarItem.SubViews);
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);


    }

    [Fact]
    public void Add_With_No_Root_Throws ()
    {
        var menuBarItem = new MenuBarItemv2 ();
        Assert.Throws<InvalidOperationException> (() => menuBarItem.Add (new MenuItemv2 ()));
    }
}
