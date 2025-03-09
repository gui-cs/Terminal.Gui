using System.Collections;
using System.Reflection;
using System.Text;
using UnitTests;
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
        Application.MainLoop = new MainLoop (new FakeMainLoop (Application.Driver));

        var view = (View)CreateInstanceIfNotGeneric (viewType);
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
    [SetupFakeDriver] // Required for spinner view that wants to register timeouts
    public void AllViews_Tests_All_Constructors (Type viewType)
    {
        Assert.True (Test_All_Constructors_Of_Type (viewType));
    }


    public bool Test_All_Constructors_Of_Type (Type type)
    {
        foreach (ConstructorInfo ctor in type.GetConstructors ())
        {
            View view = ViewTestHelpers.CreateViewFromType (type, ctor);

            if (view != null)
            {
                Assert.True (type.FullName == view.GetType ().FullName);
            }
        }

        return true;
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

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    [SetupFakeDriver] // Required for spinner view that wants to register timeouts
    public void AllViews_Command_Select_Raises_Selecting (Type viewType)
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

        var selectingCount = 0;
        view.Selecting += (s, e) => selectingCount++;

        var acceptedCount = 0;
        view.Accepting += (s, e) =>
                         {
                             acceptedCount++;
                         };


        if (view.InvokeCommand(Command.Select) == true)
        {
            Assert.Equal(1, selectingCount);
            Assert.Equal (0, acceptedCount);
        }
    }

    [Theory]
    [SetupFakeDriver] // Required for spinner view that wants to register timeouts
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Command_Accept_Raises_Accepted (Type viewType)
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

        var selectingCount = 0;
        view.Selecting += (s, e) => selectingCount++;

        var acceptedCount = 0;
        view.Accepting += (s, e) =>
                         {
                             acceptedCount++;
                         };


        if (view.InvokeCommand (Command.Accept) == true)
        {
            Assert.Equal (0, selectingCount);
            Assert.Equal (1, acceptedCount);
        }
    }


    [Theory]
    [MemberData (nameof (AllViewTypes))]
    [SetupFakeDriver] // Required for spinner view that wants to register timeouts
    public void AllViews_Command_HotKey_Raises_HandlingHotKey (Type viewType)
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
        else
        {
            view.HotKey = Key.T;
        }

        var acceptedCount = 0;
        view.Accepting += (s, e) =>
                         {
                             acceptedCount++;
                         };

        var handlingHotKeyCount = 0;
        view.HandlingHotKey += (s, e) =>
                         {
                             handlingHotKeyCount++;
                         };

        if (view.InvokeCommand (Command.HotKey) == true)
        {
            Assert.Equal (1, handlingHotKeyCount);
            Assert.Equal (0, acceptedCount);
        }
    }
}
