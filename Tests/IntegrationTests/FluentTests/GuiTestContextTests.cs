using System.Drawing;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

/// <summary>
///     Basic tests for GuiTestContext functionality including constructor, lifecycle, and resize operations.
/// </summary>
public class GuiTestContextTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Constructor_Sets_Application_Screen (TestDriver d)
    {
        using var context = new GuiTestContext (d, _out, TimeSpan.FromSeconds (10));

        Assert.NotEqual (Rectangle.Empty, context.App?.Screen);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void ResizeConsole_Resizes (TestDriver d)
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
                                     .AssertEqual (18, lbl.Frame.Width);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void With_New_A_Runs (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);
        Assert.True (context.App!.TopRunnable!.Running);
        Assert.NotEqual (Rectangle.Empty, context.App!.Screen);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void AnsiScreenShot_Renders_Ansi_Stream (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (10, 3, d, _out)
                                           .Then ((app) =>
                                                  {
                                                      app.TopRunnable!.BorderStyle = LineStyle.None;
                                                      app.TopRunnable!.Border!.Thickness = Thickness.Empty;
                                                      app.TopRunnable.Text = "hello";
                                                  })
                                           .ScreenShot ("ScreenShot", _out)
                                           .AnsiScreenShot ("AnsiScreenShot", _out)
;
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void With_Starts_Stops_Without_Error (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);
        // No actual assertions are needed — if no exceptions are thrown, it's working
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void With_Without_Stop_Still_Cleans_Up (TestDriver d)
    {
        GuiTestContext? context;

        using (context = With.A<Window> (40, 10, d, _out))
        {
            Assert.False (context.Finished);
        }

        Assert.True (context.Finished);
    }
}