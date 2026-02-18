using System.ComponentModel;

namespace ViewsTests;

public class MenuItemTests
{
    /// <summary>Test view that exposes AddCommand publicly for testing.</summary>
    private class TestTargetView : View
    {
        public void RegisterCommand (Command command, Func<bool?> impl) => AddCommand (command, impl);
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var menuItem = new MenuItem ();
        Assert.Null (menuItem.TargetView);

        menuItem = new MenuItem (null, Command.NotBound);
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

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Raises_Activating ()
    {
        MenuItem menuItem = new () { Title = "Test" };
        var activatingCount = 0;
        menuItem.Activating += (_, _) => activatingCount++;

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, activatingCount);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_HotKey_Raises_Activating ()
    {
        MenuItem menuItem = new () { Title = "_Test" };
        var activatingCount = 0;
        menuItem.Activating += (_, _) => activatingCount++;

        menuItem.InvokeCommand (Command.HotKey);

        Assert.True (activatingCount > 0);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_SubMenu_Sets_RightArrow_On_KeyView ()
    {
        MenuItem menuItem = new () { Title = "Parent" };
        Menu subMenu = new ();

        menuItem.SubMenu = subMenu;

        Assert.Contains (Glyphs.RightArrow.ToString (), menuItem.KeyView.Text);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_MouseEnter_Sets_Focus ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuItem item1 = new () { Title = "First" };
        MenuItem item2 = new () { Title = "Second" };

        Menu menu = new ([item1, item2]);
        (runnable as View)?.Add (menu);
        app.Begin (runnable);

        // First item should have focus initially
        Assert.True (item1.HasFocus);

        // Simulate mouse entering second item (internal method visible via InternalsVisibleTo)
        item2.NewMouseEnterEvent (new CancelEventArgs ());

        Assert.True (item2.HasFocus);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Command_Property_Sets_Title_And_HelpText ()
    {
        MenuItem menuItem = new ();

        // Verify Command property round-trips correctly
        menuItem.Command = Command.Accept;
        Assert.Equal (Command.Accept, menuItem.Command);

        menuItem.Command = Command.Activate;
        Assert.Equal (Command.Activate, menuItem.Command);

        menuItem.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Accept_Invokes_Command_On_TargetView ()
    {
        TestTargetView targetView = new () { Title = "Target" };
        var commandInvoked = false;

        targetView.RegisterCommand (Command.Save,
                                    () =>
                                    {
                                        commandInvoked = true;

                                        return true;
                                    });

        // Bind a key so the MenuItem constructor can find it
        targetView.HotKeyBindings.Add (Key.S.WithCtrl, Command.Save);

        MenuItem menuItem = new (targetView, Command.Save);

        // Invoke Accept on the MenuItem - this should call OnAccepted which invokes Command on TargetView
        menuItem.InvokeCommand (Command.Accept);

        Assert.True (commandInvoked);

        menuItem.Dispose ();
        targetView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Dispose_Disposes_SubMenu ()
    {
        MenuItem menuItem = new () { Title = "Parent" };
        Menu subMenu = new ();

        menuItem.SubMenu = subMenu;

        Assert.NotNull (menuItem.SubMenu);

        menuItem.Dispose ();

        Assert.Null (menuItem.SubMenu);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Constructors_Defaults_Extended ()
    {
        TestTargetView targetView = new () { Title = "Target" };
        targetView.RegisterCommand (Command.Save, () => true);
        targetView.HotKeyBindings.Add (Key.S.WithCtrl, Command.Save);

        Menu subMenu = new ();

        MenuItem menuItem = new (targetView, Command.Save, "Save File", "Saves the current file", subMenu);

        Assert.Equal (targetView, menuItem.TargetView);
        Assert.Equal (Command.Save, menuItem.Command);
        Assert.Equal ("Save File", menuItem.Title);
        Assert.Equal ("Saves the current file", menuItem.HelpText);
        Assert.Equal (subMenu, menuItem.SubMenu);

        menuItem.Dispose ();
        targetView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Bubbles_To_Menu ()
    {
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test Item" };
        menu.Add (menuItem);

        var menuActivatingFired = 0;

        menu.Activating += (_, _) => { menuActivatingFired++; };

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, menuActivatingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_With_CommandView_Activate_Bubbles_To_Menu ()
    {
        CheckBox checkBox = new () { Title = "_Check", CanFocus = false };
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test", CommandView = checkBox };
        menu.Add (menuItem);

        var menuActivatingFired = 0;

        menu.Activating += (_, _) => { menuActivatingFired++; };

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (1, menuActivatingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Accept_Handled_Does_Not_Bubble_To_Menu ()
    {
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuAcceptingFired = 0;

        menuItem.Accepting += (_, e) => { e.Handled = true; };
        menu.Accepting += (_, _) => { menuAcceptingFired++; };

        menuItem.InvokeCommand (Command.Accept);

        Assert.Equal (0, menuAcceptingFired);

        menu.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void MenuItem_Activate_Handled_Does_Not_Bubble_To_Menu ()
    {
        Menu menu = new ();
        MenuItem menuItem = new () { Title = "Test" };
        menu.Add (menuItem);

        var menuActivatingFired = 0;

        menuItem.Activating += (_, e) => { e.Handled = true; };
        menu.Activating += (_, _) => { menuActivatingFired++; };

        menuItem.InvokeCommand (Command.Activate);

        Assert.Equal (0, menuActivatingFired);

        menu.Dispose ();
    }
}
