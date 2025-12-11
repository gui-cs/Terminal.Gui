#nullable enable
using UnitTests;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace UnitTests.ApplicationTests;

[Trait ("Category", "Input")]
public class ApplicationMouseTests
{
    private readonly ITestOutputHelper _output;

    public ApplicationMouseTests (ITestOutputHelper output)
    {
        _output = output;
#if DEBUG_IDISPOSABLE
        View.Instances.Clear ();
#endif
    }

    #region mouse coordinate tests

    // test Application.MouseEvent - ensure coordinates are screen relative
    [Theory]

    // inside tests
    [InlineData (0, 0, 0, 0, true)]
    [InlineData (1, 0, 1, 0, true)]
    [InlineData (0, 1, 0, 1, true)]
    [InlineData (9, 0, 9, 0, true)]
    [InlineData (0, 9, 0, 9, true)]

    // outside tests
    [InlineData (-1, -1, -1, -1, true)]
    [InlineData (0, -1, 0, -1, true)]
    [InlineData (-1, 0, -1, 0, true)]
    public void MouseEventCoordinatesAreScreenRelative (
        int clickX,
        int clickY,
        int expectedX,
        int expectedY,
        bool expectedClicked
    )
    {
        var mouse = new Mouse { ScreenPosition = new (clickX, clickY), Flags = MouseFlags.Button1Pressed };
        var clicked = false;

        void OnApplicationOnMouseEvent (object? s, Mouse e)
        {
            Assert.Equal (expectedX, e.ScreenPosition.X);
            Assert.Equal (expectedY, e.ScreenPosition.Y);
            clicked = true;
        }

        Application.MouseEvent += OnApplicationOnMouseEvent;

        Application.RaiseMouseEvent (mouse);
        Assert.Equal (expectedClicked, clicked);
        Application.MouseEvent -= OnApplicationOnMouseEvent;
    }

    /// <summary>
    ///     Tests that the mouse coordinates passed to the focused view are correct when the mouse is clicked. No adornments;
    ///     Frame == Viewport
    /// </summary>
    [Theory]
    [AutoInitShutdown]

    // click inside view tests
    [InlineData (0, 0, 0, 0, 0, true)]
    [InlineData (0, 1, 0, 1, 0, true)]
    [InlineData (0, 0, 1, 0, 1, true)]
    [InlineData (0, 9, 0, 9, 0, true)]
    [InlineData (0, 0, 9, 0, 9, true)]

    // view is offset from origin ; click is inside view 
    [InlineData (1, 1, 1, 0, 0, true)]
    [InlineData (1, 2, 1, 1, 0, true)]
    [InlineData (1, 1, 2, 0, 1, true)]
    [InlineData (1, 9, 1, 8, 0, true)]
    [InlineData (1, 1, 9, 0, 8, true)]

    // click outside view tests
    [InlineData (0, -1, -1, 0, 0, false)]
    [InlineData (0, 0, -1, 0, 0, false)]
    [InlineData (0, -1, 0, 0, 0, false)]
    [InlineData (0, 0, 10, 0, 0, false)]
    [InlineData (0, 10, 0, 0, 0, false)]
    [InlineData (0, 10, 10, 0, 0, false)]

    // view is offset from origin ; click is outside view 
    [InlineData (1, 0, 0, 0, 0, false)]
    [InlineData (1, 1, 0, 0, 0, false)]
    [InlineData (1, 0, 1, 0, 0, false)]
    [InlineData (1, 9, 0, 0, 0, false)]
    [InlineData (1, 0, 9, 0, 0, false)]
    public void MouseCoordinatesTest_NoAdornments (
        int offset,
        int clickX,
        int clickY,
        int expectedX,
        int expectedY,
        bool expectedClicked
    )
    {
        Size size = new (10, 10);
        Point pos = new (offset, offset);

        var clicked = false;

        var view = new View
        {
            X = pos.X,
            Y = pos.Y,
            Width = size.Width,
            Height = size.Height
        };

        var mouseEventToRaise = new Mouse { ScreenPosition = new (clickX, clickY), Flags = MouseFlags.Button1Clicked };

        view.MouseEvent += (s, mouse) =>
                           {
                               Assert.Equal (expectedX, mouse.Position!.Value.X);
                               Assert.Equal (expectedY, mouse.Position!.Value.Y);
                               clicked = true;
                           };

        var top = new Runnable ();
        top.Add (view);
        Application.Begin (top);

        Application.RaiseMouseEvent (mouseEventToRaise);
        Assert.Equal (expectedClicked, clicked);
        top.Dispose ();
    }


    #endregion mouse coordinate tests

    #region mouse grab tests

    [Fact (Skip = "Rebuild to use ScrollBar")]
    [AutoInitShutdown]
    public void MouseGrabView_WithNullMouseEventView ()
    {
        //var tf = new TextField { Width = 10 };
        //var sv = new ScrollView { Width = Dim.Fill (), Height = Dim.Fill () };
        //sv.SetContentSize (new (100, 100));

        //sv.Add (tf);
        //var top = new Runnable ();
        //top.Add (sv);

        //int iterations = -1;

        //ApplicationImpl.Instance.Iteration += (s, a) =>
        //                         {
        //                             iterations++;

        //                             if (iterations == 0)
        //                             {
        //                                 Assert.True (tf.HasFocus);
        //                                 Assert.Null (Application.Mouse.MouseGrabView);

        //                                 Application.RaiseMouseEvent (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.ReportMousePosition });

        //                                 Assert.Equal (sv, Application.Mouse.MouseGrabView);

        //                                 MessageBox.Query (App, "Title", "Test", "Ok");

        //                                 Assert.Null (Application.Mouse.MouseGrabView);
        //                             }
        //                             else if (iterations == 1)
        //                             {
        //                                 // Application.Mouse.MouseGrabView is null because
        //                                 // another runnable (Dialog) was opened
        //                                 Assert.Null (Application.Mouse.MouseGrabView);

        //                                 Application.RaiseMouseEvent (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.ReportMousePosition });

        //                                 Assert.Null (Application.Mouse.MouseGrabView);

        //                                 Application.RaiseMouseEvent (new () { ScreenPosition = new (40, 12), Flags = MouseFlags.ReportMousePosition });

        //                                 Assert.Null (Application.Mouse.MouseGrabView);

        //                                 Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });

        //                                 Assert.Null (Application.Mouse.MouseGrabView);

        //                                 Application.RequestStop ();
        //                             }
        //                             else if (iterations == 2)
        //                             {
        //                                 Assert.Null (Application.Mouse.MouseGrabView);

        //                                 Application.RequestStop ();
        //                             }
        //                         };

        //Application.Run (top);
        //top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseGrabView_GrabbedMouse_UnGrabbedMouse ()
    {
        View? grabView = null;
        var count = 0;

        var view1 = new View { Id = "view1" };
        var view2 = new View { Id = "view2" };
        var view3 = new View { Id = "view3" };

        Application.Mouse.GrabbedMouse += Application_GrabbedMouse;
        Application.Mouse.UnGrabbedMouse += Application_UnGrabbedMouse;

        Application.Mouse.GrabMouse (view1);
        Assert.Equal (0, count);
        Assert.Equal (grabView, view1);
        Assert.Equal (view1, Application.Mouse.MouseGrabView);

        Application.Mouse.UngrabMouse ();
        Assert.Equal (1, count);
        Assert.Equal (grabView, view1);
        Assert.Null (Application.Mouse.MouseGrabView);

        Application.Mouse.GrabbedMouse += Application_GrabbedMouse;
        Application.Mouse.UnGrabbedMouse += Application_UnGrabbedMouse;

        Application.Mouse.GrabMouse (view2);
        Assert.Equal (1, count);
        Assert.Equal (grabView, view2);
        Assert.Equal (view2, Application.Mouse.MouseGrabView);

        Application.Mouse.UngrabMouse ();
        Assert.Equal (2, count);
        Assert.Equal (grabView, view2);
        Assert.Equal (view3, Application.Mouse.MouseGrabView);
        Application.Mouse.UngrabMouse ();
        Assert.Null (Application.Mouse.MouseGrabView);

        void Application_GrabbedMouse (object? sender, ViewEventArgs e)
        {
            if (count == 0)
            {
                Assert.Equal (view1, e.View);
                grabView = view1;
            }
            else
            {
                Assert.Equal (view2, e.View);
                grabView = view2;
            }

            Application.Mouse.GrabbedMouse -= Application_GrabbedMouse;
        }

        void Application_UnGrabbedMouse (object? sender, ViewEventArgs e)
        {
            if (count == 0)
            {
                Assert.Equal (view1, e.View);
                Assert.Equal (grabView, e.View);
            }
            else
            {
                Assert.Equal (view2, e.View);
                Assert.Equal (grabView, e.View);
            }

            count++;

            if (count > 1)
            {
                // It's possible to grab another view after the previous was ungrabbed
                Application.Mouse.GrabMouse (view3);
            }

            Application.Mouse.UnGrabbedMouse -= Application_UnGrabbedMouse;
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void View_Is_Responsible_For_Calling_UnGrabMouse_Before_Being_Disposed ()
    {
        var count = 0;
        var view = new View { Width = 1, Height = 1 };
        view.MouseEvent += (s, e) => count++;
        var top = new Runnable ();
        top.Add (view);
        Application.Begin (top);

        Assert.Null (Application.Mouse.MouseGrabView);
        Application.Mouse.GrabMouse (view);
        Assert.Equal (view, Application.Mouse.MouseGrabView);
        top.Remove (view);
        Application.Mouse.UngrabMouse ();
        view.Dispose ();
#if DEBUG_IDISPOSABLE
        Assert.True (view.WasDisposed);
#endif

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });
        Assert.Null (Application.Mouse.MouseGrabView);
        Assert.Equal (0, count);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseGrab_EventSentToGrabView_HasCorrectView ()
    {
        // BEFORE FIX: viewRelativeMouseEvent.View = deepestViewUnderMouse ?? MouseGrabView (potentially targetView).
        // AFTER FIX: viewRelativeMouseEvent.View = MouseGrabView (always the grab view).
        // Test fails before fix (receivedView == targetView), passes after fix (receivedView == grabView).

        var grabView = new View
        {
            Id = "grab",
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };

        var targetView = new View
        {
            Id = "target",
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };

        View? receivedView = null;
        grabView.MouseEvent += (_, e) => receivedView = e.View;

        var top = new Runnable { Width = 20, Height = 10 };
        top.Add (grabView);
        top.Add (targetView); // deepestViewUnderMouse = targetView
        Application.Begin (top);

        Application.Mouse.GrabMouse (grabView);
        Assert.Equal (grabView, Application.Mouse.MouseGrabView);

        Application.RaiseMouseEvent (new Mouse
        {
            ScreenPosition = new (2, 2), // Inside both views
            Flags = MouseFlags.Button1Clicked
        });

        // EXPECTED: Event sent to grab view has View == grabView.
        Assert.Equal (grabView, receivedView);

        Application.Mouse.UngrabMouse ();
        top.Dispose ();
    }

    #endregion
}
