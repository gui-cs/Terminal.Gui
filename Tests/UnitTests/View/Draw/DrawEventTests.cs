#nullable enable
using UnitTests;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class DrawEventTests
{
    [Fact]
    [AutoInitShutdown]
    public void DrawContentComplete_Event_Is_Always_Called ()
    {
        var viewCalled = false;
        var tvCalled = false;

        var view = new View { Width = 10, Height = 10, Text = "View" };
        view.DrawComplete += (s, e) => viewCalled = true;
        var tv = new TextView { Y = 11, Width = 10, Height = 10 };
        tv.DrawComplete += (s, e) => tvCalled = true;

        var top = new Toplevel ();
        top.Add (view, tv);
        RunState runState = Application.Begin (top);
        Application.RunIteration (ref runState);

        Assert.True (viewCalled);
        Assert.True (tvCalled);
        top.Dispose ();
    }
}
