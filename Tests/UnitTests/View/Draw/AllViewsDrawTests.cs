using UnitTests;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

public class AllViewsDrawTests (ITestOutputHelper _output) : TestsAllViews
{
    [Theory]
    [SetupFakeDriver] // Required for spinner view that wants to register timeouts
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Draw_Does_Not_Layout (Type viewType)
    {
        Application.ResetState (true);
        // Required for spinner view that wants to register timeouts
        Application.MainLoop = new MainLoop (new FakeMainLoop (Application.Driver));

        var view = (View)CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        _output.WriteLine ($"Testing {viewType}");

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        var drawCompleteCount = 0;
        view.DrawComplete += (s, e) => drawCompleteCount++;

        var layoutStartedCount = 0;
        view.SubviewLayout += (s, e) => layoutStartedCount++;

        var layoutCompleteCount = 0;
        view.SubviewsLaidOut += (s, e) => layoutCompleteCount++;

        view.SetNeedsLayout ();
        view.Layout ();

        Assert.Equal (0, drawCompleteCount);
        Assert.Equal (1, layoutStartedCount);
        Assert.Equal (1, layoutCompleteCount);

        if (view.Visible)
        {
            view.SetNeedsDraw ();
            view.Draw ();

            Assert.Equal (1, drawCompleteCount);
            Assert.Equal (1, layoutStartedCount);
            Assert.Equal (1, layoutCompleteCount);
        }
    }
}
