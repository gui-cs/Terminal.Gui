using System.Reflection;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class AllViewsTests (ITestOutputHelper output)
{
    // TODO: Update all these tests to use AllViews like AllViews_Center_Properly does
    public static TheoryData<View, string> AllViews => TestHelpers.GetAllViewsTheoryData ();

    [Theory]
    [MemberData (nameof (AllViews))]
    public void AllViews_Center_Properly (View view, string viewName)
    {
        // See https://github.com/gui-cs/Terminal.Gui/issues/3156

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewName} - It's a Generic");
            Application.Shutdown ();

            return;
        }

        view.X = Pos.Center ();
        view.Y = Pos.Center ();

        // Turn off AutoSize
        view.AutoSize = false;

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

        if (view is ScrollBarView)
        {
            output.WriteLine ($"It's a {viewName} - It has its own Pos/Dim calculation");
            expectedX = frame.Frame.Width - view.Frame.Width;
            expectedY = frame.Frame.Bottom - view.Frame.Height;

            Assert.True (view.Frame.Left == expectedX);
            Assert.True (view.Frame.Top == expectedY);
        }
        else
        {
            Assert.True (
                         view.Frame.Left == expectedX,
                         $"{view} did not center horizontally. Expected: {expectedX}. Actual: {view.Frame.Left}"
                        );

            Assert.True (
                         view.Frame.Top == expectedY,
                         $"{view} did not center vertically. Expected: {expectedY}. Actual: {view.Frame.Top}"
                        );

        }
        Application.Shutdown ();
    }

    [Fact]
    public void AllViews_Enter_Leave_Events ()
    {
        foreach (Type type in TestHelpers.GetAllViewClasses ())
        {
            output.WriteLine ($"Testing {type.Name}");

            Application.Init (new FakeDriver ());

            Toplevel top = new ();
            View vType = TestHelpers.CreateViewFromType (type, type.GetConstructor (Array.Empty<Type> ()));

            if (vType == null)
            {
                output.WriteLine ($"Ignoring {type} - It's a Generic");
                top.Dispose ();
                Application.Shutdown ();

                continue;
            }

            vType.AutoSize = false;
            vType.X = 0;
            vType.Y = 0;
            vType.Width = 10;
            vType.Height = 1;

            var view = new View
            {
                X = 0,
                Y = 1,
                Width = 10,
                Height = 1,
                CanFocus = true
            };
            var vTypeEnter = 0;
            var vTypeLeave = 0;
            var viewEnter = 0;
            var viewLeave = 0;

            vType.Enter += (s, e) => vTypeEnter++;
            vType.Leave += (s, e) => vTypeLeave++;
            view.Enter += (s, e) => viewEnter++;
            view.Leave += (s, e) => viewLeave++;

            top.Add (vType, view);
            Application.Begin (top);

            if (!vType.CanFocus || (vType is Toplevel && ((Toplevel)vType).Modal))
            {
                top.Dispose ();
                Application.Shutdown ();

                continue;
            }

            if (vType is TextView)
            {
                top.NewKeyDownEvent (Key.Tab.WithCtrl);
            }
            else if (vType is DatePicker)
            {
                for (var i = 0; i < 4; i++)
                {
                    top.NewKeyDownEvent (Key.Tab.WithCtrl);
                }
            }
            else
            {
                top.NewKeyDownEvent (Key.Tab);
            }

            top.NewKeyDownEvent (Key.Tab);

            Assert.Equal (2, vTypeEnter);
            Assert.Equal (1, vTypeLeave);
            Assert.Equal (1, viewEnter);
            Assert.Equal (1, viewLeave);

            top.Dispose ();
            Application.Shutdown ();
        }
    }


    [Fact]
    public void AllViews_Tests_All_Constructors ()
    {
        Application.Init (new FakeDriver ());

        foreach (Type type in TestHelpers.GetAllViewClasses ())
        {
            Assert.True (Test_All_Constructors_Of_Type (type));
        }

        Application.Shutdown ();
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

    // BUGBUG: This is a hack. We should figure out how to dynamically
    // create the right type of argument for the constructor.
    private static void AddArguments (Type paramType, List<object> pTypes)
    {
        if (paramType == typeof (Rectangle))
        {
            pTypes.Add (Rectangle.Empty);
        }
        else if (paramType == typeof (string))
        {
            pTypes.Add (string.Empty);
        }
        else if (paramType == typeof (int))
        {
            pTypes.Add (0);
        }
        else if (paramType == typeof (bool))
        {
            pTypes.Add (true);
        }
        else if (paramType.Name == "IList")
        {
            pTypes.Add (new List<object> ());
        }
        else if (paramType.Name == "View")
        {
            var top = new Toplevel ();
            var view = new View ();
            top.Add (view);
            pTypes.Add (view);
        }
        else if (paramType.Name == "View[]")
        {
            pTypes.Add (new View [] { });
        }
        else if (paramType.Name == "Stream")
        {
            pTypes.Add (new MemoryStream ());
        }
        else if (paramType.Name == "String")
        {
            pTypes.Add (string.Empty);
        }
        else if (paramType.Name == "TreeView`1[T]")
        {
            pTypes.Add (string.Empty);
        }
        else
        {
            pTypes.Add (null);
        }
    }
}
