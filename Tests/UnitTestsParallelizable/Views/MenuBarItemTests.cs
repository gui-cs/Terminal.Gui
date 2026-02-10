
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
    public void Command_Accept_Executes_Action ()
    {
        MenuBarItem menuBarItem = new () { Title = "Test" };
        var actionFired = false;
        menuBarItem.Action = () => actionFired = true;

        // Accept executes Action
        menuBarItem.InvokeCommand (Command.Accept);

        Assert.True (actionFired);

        menuBarItem.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Command_HotKey_Executes_Action ()
    {
        MenuBarItem menuBarItem = new () { Title = "_Test" };
        var actionFired = false;
        menuBarItem.Action = () => actionFired = true;

        // HotKey invokes Accept
        menuBarItem.InvokeCommand (Command.HotKey);

        Assert.True (actionFired);

        menuBarItem.Dispose ();
    }
}
