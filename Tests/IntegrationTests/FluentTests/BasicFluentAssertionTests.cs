using System.Text;
using Terminal.Gui;
using TerminalGuiFluentTesting;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class BasicFluentAssertionTests
{
    private readonly TextWriter _out;

    public class TestOutputWriter : TextWriter
    {
        private readonly ITestOutputHelper _output;

        public TestOutputWriter (ITestOutputHelper output) { _output = output; }

        public override void WriteLine (string? value) { _output.WriteLine (value ?? string.Empty); }

        public override Encoding Encoding => Encoding.UTF8;
    }

    public BasicFluentAssertionTests (ITestOutputHelper outputHelper) { _out = new TestOutputWriter (outputHelper); }

    [Fact]
    public void GuiTestContext_StartsAndStopsWithoutError ()
    {
        using GuiTestContext context = With.A<Window> (40, 10);

        // No actual assertions are needed — if no exceptions are thrown, it's working
        context.Stop ();
    }

    [Fact]
    public void GuiTestContext_ForgotToStop ()
    {
        using GuiTestContext context = With.A<Window> (40, 10);
    }

    [Fact]
    public void TestWindowsResize ()
    {
        var lbl = new Label
        {
            Width = Dim.Fill ()
        };

        using GuiTestContext c = With.A<Window> (40, 10)
                                     .Add (lbl)
                                     .Then (() => Assert.Equal (lbl.Frame.Width, 38)) // Window has 2 border
                                     .ResizeConsole (20, 20)
                                     .Then (() => Assert.Equal (lbl.Frame.Width, 18))
                                     .Stop ();
    }

    [Fact]
    public void ContextMenu_CrashesOnRight ()
    {
        var clicked = false;

        var ctx = new ContextMenu ();

        var menuItems = new MenuBarItem (
                                         [
                                             new ("_New File", string.Empty, () => { clicked = true; })
                                         ]
                                        );

        using GuiTestContext c = With.A<Window> (40, 10)
                                     .WithContextMenu (ctx, menuItems)
                                     .ScreenShot ("Before open menu", _out)

                                     // Click in main area inside border
                                     .RightClick (1, 1)
                                     .ScreenShot ("After open menu", _out)
                                     .LeftClick (3, 3)
                                     .Stop ()
                                     .WriteOutLogs (_out);
        Assert.True (clicked);
    }

    [Theory]
    [InlineData(V2TestDriver.V2Win)]
    [InlineData (V2TestDriver.V2Net)]
    public void ContextMenu_OpenSubmenu (V2TestDriver v2TestDriver)
    {
        var clicked = false;

        var ctx = new ContextMenu ();



        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null),
                                             new MenuItem ("Three", "", null),
                                             new MenuBarItem (
                                                              "Four",
                                                              [
                                                                  new MenuItem ("SubMenu1", "", null),
                                                                  new MenuItem ("SubMenu2", "", ()=>clicked=true),
                                                                  new MenuItem ("SubMenu3", "", null),
                                                                  new MenuItem ("SubMenu4", "", null),
                                                                  new MenuItem ("SubMenu5", "", null),
                                                                  new MenuItem ("SubMenu6", "", null),
                                                                  new MenuItem ("SubMenu7", "", null)
                                                              ]
                                                             ),
                                             new MenuItem ("Five", "", null),
                                             new MenuItem ("Six", "", null)
                                         ]
                                        );

        using GuiTestContext c = With.A<Window> (40, 10,v2TestDriver)
                                     .WithContextMenu (ctx, menuItems)
                                     .ScreenShot ("Before open menu", _out)

                                     // Click in main area inside border
                                     .RightClick (1, 1)
                                     .ScreenShot ("After open menu", _out)
                                     .Down ()
                                     .Down ()
                                     .Down ()
                                     .Right()
                                     .ScreenShot ("After open submenu", _out)
                                     .Down ()
                                     .Enter ()
                                     .ScreenShot ("Menu should be closed after selecting", _out)
                                     .Stop ()
                                     .WriteOutLogs (_out);
        Assert.True (clicked);
    }
}
