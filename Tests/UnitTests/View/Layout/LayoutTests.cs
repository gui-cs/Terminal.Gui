using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

public class LayoutTests (ITestOutputHelper output) : TestsAllViews
{
    [Theory]
    [SetupFakeDriver] // Required for spinner view that wants to register timeouts
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Layout_Does_Not_Draw (Type viewType)
    {

        // Required for spinner view that wants to register timeouts
        Application.MainLoop = new MainLoop (new FakeMainLoop (Application.Driver));

        var view = (View)CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        var drawContentCount = 0;
        view.DrawingContent += (s, e) => drawContentCount++;

        var layoutStartedCount = 0;
        view.SubViewLayout += (s, e) => layoutStartedCount++;

        var layoutCompleteCount = 0;
        view.SubViewsLaidOut += (s, e) => layoutCompleteCount++;

        view.SetNeedsLayout ();
        view.SetNeedsDraw();
        view.Layout ();

        Assert.Equal (0, drawContentCount);
        Assert.Equal (1, layoutStartedCount);
        Assert.Equal (1, layoutCompleteCount);
    }
}
