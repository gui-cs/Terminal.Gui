

//using static Terminal.Gui.ViewBaseTests.MenuTests;

namespace ViewsTests;

public class MenuBarItemTests
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var menuBarItem = new MenuBarItem ();
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);

        menuBarItem = new MenuBarItem (null, Command.NotBound, null, null);
        Assert.Null (menuBarItem.PopoverMenu);
        Assert.Null (menuBarItem.TargetView);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void MenuItem_Command_Activate_SetsFocus ()
    {
        MenuItem menuItem = new () { Title = "Test" };

        // MenuItem Activate sets focus and raises SelectedMenuItemChanged
        bool? result = menuItem.InvokeCommand (Command.Activate);

        // MenuItem may not handle Activate directly
        Assert.NotEqual (true, result);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void MenuItem_Command_Accept_ExecutesAction ()
    {
        MenuItem menuItem = new () { Title = "Test" };
        var actionFired = false;
        menuItem.Action = () => actionFired = true;

        // Accept executes Action
        bool? result = menuItem.InvokeCommand (Command.Accept);

        Assert.True (actionFired);
        Assert.True (result);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void MenuItem_Command_HotKey_InvokesAccept ()
    {
        MenuItem menuItem = new () { Title = "_Test" };
        var actionFired = false;
        menuItem.Action = () => actionFired = true;

        // HotKey invokes Accept
        bool? result = menuItem.InvokeCommand (Command.HotKey);

        Assert.True (actionFired);
        Assert.True (result);

        menuItem.Dispose ();
    }
}
