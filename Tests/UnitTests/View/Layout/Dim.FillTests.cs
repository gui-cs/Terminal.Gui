using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

public class DimFillTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    [AutoInitShutdown]
    public void DimFill_SizedCorrectly ()
    {
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };
        var top = new Toplevel ();
        top.Add (view);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (32, 5);

        top.Layout ();

        //view.SetRelativeLayout (new (0, 0, 32, 5));
        Assert.Equal (32, view.Frame.Width);
        Assert.Equal (5, view.Frame.Height);
        top.Dispose ();
    }

}
