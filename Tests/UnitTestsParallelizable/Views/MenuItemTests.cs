
namespace ViewsTests;

public class MenuItemTests
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var menuItem = new MenuItem ();
        Assert.Null (menuItem.TargetView);

        menuItem = new MenuItem (null, Command.NotBound, null, null);
        Assert.Null (menuItem.TargetView);
    }

    [Fact]
    public void Command_Accept_Executes_Action ()
    {
        MenuItem menuItem = new () { Title = "Test" };
        var actionFired = false;
        menuItem.Action = () => actionFired = true;

        // Accept executes Action
        menuItem.InvokeCommand (Command.Accept);

        Assert.True (actionFired);

        menuItem.Dispose ();
    }

    [Fact]
    public void Command_HotKey_Executes_Action ()
    {
        MenuItem menuItem = new () { Title = "_Test" };
        var actionFired = false;
        menuItem.Action = () => actionFired = true;

        // HotKey invokes Accept
        menuItem.InvokeCommand (Command.HotKey);

        Assert.True (actionFired);

        menuItem.Dispose ();
    }
}
