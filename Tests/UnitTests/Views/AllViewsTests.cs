using System.Reflection;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class AllViewsTests (ITestOutputHelper output) : TestsAllViews
{
    // TODO: Update all these tests to use AllViews like AllViews_Center_Properly does

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    [SetupFakeDriver] // Required for spinner view that wants to register timeouts
    public void AllViews_Center_Properly (Type viewType)
    {
        // Required for spinner view that wants to register timeouts
        Application.MainLoop = new (new FakeMainLoop (Application.Driver));

        var view = CreateInstanceIfNotGeneric (viewType);

        // See https://github.com/gui-cs/Terminal.Gui/issues/3156

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");
            Application.Shutdown ();

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        view.X = Pos.Center ();
        view.Y = Pos.Center ();

        // Ensure the view has positive dimensions
        view.Width = 10;
        view.Height = 10;

        var frame = new View { X = 0, Y = 0, Width = 50, Height = 50 };
        frame.Add (view);
        frame.BeginInit ();
        frame.EndInit ();
        frame.LayoutSubViews ();
        frame.Dispose ();
        Application.Shutdown ();

        // What's the natural width/height?
        int expectedX = (frame.Frame.Width - view.Frame.Width) / 2;
        int expectedY = (frame.Frame.Height - view.Frame.Height) / 2;

        Assert.True (
                     view.Frame.Left == expectedX,
                     $"{view} did not center horizontally. Expected: {expectedX}. Actual: {view.Frame.Left}"
                    );

        Assert.True (
                     view.Frame.Top == expectedY,
                     $"{view} did not center vertically. Expected: {expectedY}. Actual: {view.Frame.Top}"
                    );
    }

}
