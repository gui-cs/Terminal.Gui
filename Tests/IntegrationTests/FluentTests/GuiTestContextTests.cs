using System.Drawing;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class GuiTestContextTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Constructor_Sets_Application_Screen (TestDriver d)
    {
        using var context = new GuiTestContext (d, _out, TimeSpan.FromSeconds (10));

        Assert.NotEqual (Rectangle.Empty, Application.Screen);

        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void With_New_A_Runs (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);
        Assert.True (Application.Top!.Running);
        Assert.NotEqual (Rectangle.Empty, Application.Screen);
        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_ViaApplication_Stops (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d);
        Assert.True (Application.Top!.Running);

        Toplevel top = Application.Top;
        Application.RaiseKeyDownEvent (Application.QuitKey);
        Assert.False (top!.Running);

        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_ViaDriver_Stops (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);
        Assert.True (Application.Top!.Running);

        Toplevel top = Application.Top;
        context.Send (Application.QuitKey);
        //Thread.Sleep (1000);
        Assert.False (top!.Running);

        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void With_Starts_Stops_Without_Error (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);

        // No actual assertions are needed — if no exceptions are thrown, it's working
        context.Stop ();
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
                                     .AssertEqual (18, lbl.Frame.Width)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

}
