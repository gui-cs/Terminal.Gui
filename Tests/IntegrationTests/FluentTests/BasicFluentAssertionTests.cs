using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class BasicFluentAssertionTests
{
    private readonly TextWriter _out;

    public BasicFluentAssertionTests (ITestOutputHelper outputHelper) { _out = new TestOutputWriter (outputHelper); }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void GuiTestContext_NewInstance_Runs (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);
        Assert.True (Application.Top!.Running);

        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void GuiTestContext_QuitKey_Stops (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d);
        Assert.True (Application.Top!.Running);

        Toplevel top = Application.Top;
        context.RaiseKeyDownEvent (Application.QuitKey);
        Assert.False (top!.Running);

        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void GuiTestContext_StartsAndStopsWithoutError (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d);

        // No actual assertions are needed — if no exceptions are thrown, it's working
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void GuiTestContext_ForgotToStop (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void TestWindowsResize (TestDriver d)
    {
        var lbl = new Label
        {
            Width = Dim.Fill ()
        };

        using GuiTestContext c = With.A<Window> (40, 10, d)
                                     .Add (lbl)
                                     .AssertEqual (38, lbl.Frame.Width) // Window has 2 border
                                     .ResizeConsole (20, 20)
                                     .WaitIteration ()
                                     .AssertEqual (18, lbl.Frame.Width)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void ContextMenu_CrashesOnRight (TestDriver d)
    {
        var clicked = false;

        MenuItemv2 [] menuItems = [new ("_New File", string.Empty, () => { clicked = true; })];

        using GuiTestContext c = With.A<Window> (40, 10, d)
                                     .WithContextMenu (new (menuItems))
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
    [ClassData (typeof (TestDrivers))]
    public void ContextMenu_OpenSubmenu (TestDriver d)
    {
        var clicked = false;

        MenuItemv2 [] menuItems =
        [
            new ("One", "", null),
            new ("Two", "", null),
            new ("Three", "", null),
            new (
                 "Four",
                 "",
                 new (
                      [
                          new ("SubMenu1", "", null),
                          new ("SubMenu2", "", () => clicked = true),
                          new ("SubMenu3", "", null),
                          new ("SubMenu4", "", null),
                          new ("SubMenu5", "", null),
                          new ("SubMenu6", "", null),
                          new ("SubMenu7", "", null)
                      ])),
            new ("Five", "", null),
            new ("Six", "", null)
        ];

        using GuiTestContext c = With.A<Window> (40, 10, d)
                                     .WithContextMenu (new (menuItems))
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

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Toplevel_TabGroup_Forward_Backward (TestDriver d)
    {
        var v1 = new View { Id = "v1", CanFocus = true };
        var v2 = new View { Id = "v2", CanFocus = true };
        var v3 = new View { Id = "v3", CanFocus = true };
        var v4 = new View { Id = "v4", CanFocus = true };
        var v5 = new View { Id = "v5", CanFocus = true };
        var v6 = new View { Id = "v6", CanFocus = true };

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                var w1 = new Window { Id = "w1" };
                                                w1.Add (v1, v2);
                                                var w2 = new Window { Id = "w2" };
                                                w2.Add (v3, v4);
                                                var w3 = new Window { Id = "w3" };
                                                w3.Add (v5, v6);
                                                Toplevel top = Application.Top!;
                                                Application.Top!.Add (w1, w2, w3);
                                            })
                                     .WaitIteration ()
                                     .AssertTrue (v5.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6)
                                     .AssertTrue (v1.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6)
                                     .AssertTrue (v3.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6.WithShift)
                                     .AssertTrue (v1.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6.WithShift)
                                     .AssertTrue (v5.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6.WithShift)
                                     .AssertTrue (v3.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6)
                                     .AssertTrue (v5.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6)
                                     .AssertTrue (v1.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6)
                                     .AssertTrue (v3.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6.WithShift)
                                     .AssertTrue (v1.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6.WithShift)
                                     .AssertTrue (v5.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6.WithShift)
                                     .AssertTrue (v3.HasFocus)
                                     .RaiseKeyDownEvent (Key.Tab)
                                     .AssertTrue (v4.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6)
                                     .AssertTrue (v5.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6)
                                     .AssertTrue (v1.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6.WithShift)
                                     .AssertTrue (v5.HasFocus)
                                     .RaiseKeyDownEvent (Key.Tab)
                                     .AssertTrue (v6.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6.WithShift)
                                     .AssertTrue (v4.HasFocus)
                                     .RaiseKeyDownEvent (Key.F6)
                                     .AssertTrue (v6.HasFocus)
                                     .WriteOutLogs (_out)
                                     .Stop ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        Assert.False (v4.HasFocus);
        Assert.False (v5.HasFocus);
    }
}
