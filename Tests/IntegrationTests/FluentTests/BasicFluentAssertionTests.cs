using TerminalGuiFluentTesting;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class BasicFluentAssertionTests
{
    private readonly TextWriter _out;

    public BasicFluentAssertionTests (ITestOutputHelper outputHelper)
    {
        _out = new TestOutputWriter (outputHelper);
    }


    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void GuiTestContext_NewInstance_Runs (V2TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d);
        Assert.True (Application.Top!.Running);

        context.WriteOutLogs (_out);
        context.Stop ();
    }


    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void GuiTestContext_QuitKey_Stops (V2TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d);
        Assert.True (Application.Top!.Running);

        Toplevel top = Application.Top;
        context.RaiseKeyDownEvent (Application.QuitKey);
        Assert.False (top!.Running);

        Application.Top?.Dispose ();
        Application.Shutdown ();

        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void GuiTestContext_StartsAndStopsWithoutError (V2TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d);

        // No actual assertions are needed — if no exceptions are thrown, it's working
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void GuiTestContext_ForgotToStop (V2TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d);
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void TestWindowsResize (V2TestDriver d)
    {
        var lbl = new Label
        {
            Width = Dim.Fill ()
        };

        using GuiTestContext c = With.A<Window> (40, 10, d)
                                     .Add (lbl)
                                     .Then (() => Assert.Equal (38, lbl.Frame.Width)) // Window has 2 border
                                     .ResizeConsole (20, 20)
                                     .Then (() => Assert.Equal (18, lbl.Frame.Width))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void ContextMenu_CrashesOnRight (V2TestDriver d)
    {
        var clicked = false;

        MenuItemv2 [] menuItems = [new ("_New File", string.Empty, () => { clicked = true; })];

        using GuiTestContext c = With.A<Window> (40, 10, d)
                                     .WithContextMenu (new PopoverMenu (menuItems))
                                     .ScreenShot ("Before open menu", _out)

                                     // Click in main area inside border
                                     .RightClick (1, 1)
                                     .Then (
                                            () =>
                                            {
                                                // Test depends on menu having a border
                                                IPopover? popover = Application.Popover!.GetActivePopover ();
                                                Assert.NotNull (popover);
                                                var popoverMenu = popover as PopoverMenu;
                                                popoverMenu!.Root!.BorderStyle = LineStyle.Single;

                                            })
                                     .WaitIteration ()
                                     .ScreenShot ("After open menu", _out)
                                     .LeftClick (2, 2)
                                     .Stop ()
                                     .WriteOutLogs (_out);
        Assert.True (clicked);
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void ContextMenu_OpenSubmenu (V2TestDriver d)
    {
        var clicked = false;

        MenuItemv2 [] menuItems = [
                                      new ("One", "", null),
                                      new ("Two", "", null),
                                      new ("Three", "", null),
                                      new ("Four", "", new (
                                           [
                                               new ("SubMenu1", "", null),
                                               new ("SubMenu2", "", ()=>clicked=true),
                                               new ("SubMenu3", "", null),
                                               new ("SubMenu4", "", null),
                                               new ("SubMenu5", "", null),
                                               new ("SubMenu6", "", null),
                                               new ("SubMenu7", "", null)
                                           ])),
                                      new  ("Five", "", null),
                                      new  ("Six", "", null)
                                  ];

        using GuiTestContext c = With.A<Window> (40, 10, d)
                                     .WithContextMenu (new PopoverMenu (menuItems))
                                     .ScreenShot ("Before open menu", _out)

                                     // Click in main area inside border
                                     .RightClick (1, 1)
                                     .ScreenShot ("After open menu", _out)
                                     .Down ()
                                     .Down ()
                                     .Down ()
                                     .Right ()
                                     .ScreenShot ("After open submenu", _out)
                                     .Down ()
                                     .Enter ()
                                     .ScreenShot ("Menu should be closed after selecting", _out)
                                     .Stop ()
                                     .WriteOutLogs (_out);
        Assert.True (clicked);
    }
}
