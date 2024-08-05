using System.Collections;
using System.Reflection;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class AllViewsTests (ITestOutputHelper output) : TestsAllViews
{
    // TODO: Update all these tests to use AllViews like AllViews_Center_Properly does


    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Center_Properly (Type viewType)
    {
        var view = (View)CreateInstanceIfNotGeneric (viewType);
        // See https://github.com/gui-cs/Terminal.Gui/issues/3156

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");
            Application.Shutdown ();

            return;
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
        frame.LayoutSubviews ();

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
        Application.Shutdown ();

    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Tests_All_Constructors (Type viewType)
    {
        Assert.True (Test_All_Constructors_Of_Type (viewType));
    }

    //[Fact]
    //public void AllViews_HotKey_Works ()
    //{
    //	foreach (var type in GetAllViewClasses ()) {
    //		_output.WriteLine ($"Testing {type.Name}");
    //		var view = GetTypeInitializer (type, type.GetConstructor (Array.Empty<Type> ()));
    //		view.HotKeySpecifier = (Rune)'^';
    //		view.Text = "^text";
    //		Assert.Equal(Key.T, view.HotKey);
    //	}
    //}

    public bool Test_All_Constructors_Of_Type (Type type)
    {
        foreach (ConstructorInfo ctor in type.GetConstructors ())
        {
            View view = TestHelpers.CreateViewFromType (type, ctor);

            if (view != null)
            {
                Assert.True (type.FullName == view.GetType ().FullName);
            }
        }

        return true;
    }
}
