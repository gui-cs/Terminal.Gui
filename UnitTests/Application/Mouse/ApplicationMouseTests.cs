using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ApplicationTests;

[Trait ("Category", "Input")]
public class ApplicationMouseTests
{
    private readonly ITestOutputHelper _output;

    public ApplicationMouseTests (ITestOutputHelper output)
    {
        _output = output;
#if DEBUG_IDISPOSABLE
        Responder.Instances.Clear ();
        RunState.Instances.Clear ();
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
        var mouseEvent = new MouseEventArgs { ScreenPosition = new (clickX, clickY), Flags = MouseFlags.Button1Pressed };
        var clicked = false;

        void OnApplicationOnMouseEvent (object s, MouseEventArgs e)
        {
            Assert.Equal (expectedX, e.ScreenPosition.X);
            Assert.Equal (expectedY, e.ScreenPosition.Y);
            clicked = true;
        }

        Application.MouseEvent += OnApplicationOnMouseEvent;

        Application.RaiseMouseEvent (mouseEvent);
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

        var mouseEvent = new MouseEventArgs { ScreenPosition = new (clickX, clickY), Flags = MouseFlags.Button1Clicked };

        view.MouseClick += (s, e) =>
                           {
                               Assert.Equal (expectedX, e.Position.X);
                               Assert.Equal (expectedY, e.Position.Y);
                               clicked = true;
                           };

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.RaiseMouseEvent (mouseEvent);
        Assert.Equal (expectedClicked, clicked);
        top.Dispose ();
    }

    /// <summary>
    ///     Tests that the mouse coordinates passed to the focused view are correct when the mouse is clicked. With
    ///     Frames; Frame != Viewport
    /// </summary>
    //[AutoInitShutdown]
    [Theory]

    // click on border
    [InlineData (0, 0, 0, 0, 0, false)]
    [InlineData (0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, false)]
    [InlineData (0, 9, 0, 0, 0, false)]
    [InlineData (0, 0, 9, 0, 0, false)]

    // outside border
    [InlineData (0, 10, 0, 0, 0, false)]
    [InlineData (0, 0, 10, 0, 0, false)]

    // view is offset from origin ; click is on border 
    [InlineData (1, 1, 1, 0, 0, false)]
    [InlineData (1, 2, 1, 0, 0, false)]
    [InlineData (1, 1, 2, 0, 0, false)]
    [InlineData (1, 10, 1, 0, 0, false)]
    [InlineData (1, 1, 10, 0, 0, false)]

    // outside border
    [InlineData (1, -1, 0, 0, 0, false)]
    [InlineData (1, 0, -1, 0, 0, false)]
    [InlineData (1, 10, 10, 0, 0, false)]
    [InlineData (1, 11, 11, 0, 0, false)]

    // view is at origin, click is inside border
    [InlineData (0, 1, 1, 0, 0, true)]
    [InlineData (0, 2, 1, 1, 0, true)]
    [InlineData (0, 1, 2, 0, 1, true)]
    [InlineData (0, 8, 1, 7, 0, true)]
    [InlineData (0, 1, 8, 0, 7, true)]
    [InlineData (0, 8, 8, 7, 7, true)]

    // view is offset from origin ; click inside border
    // our view is 10x10, but has a border, so it's bounds is 8x8
    [InlineData (1, 2, 2, 0, 0, true)]
    [InlineData (1, 3, 2, 1, 0, true)]
    [InlineData (1, 2, 3, 0, 1, true)]
    [InlineData (1, 9, 2, 7, 0, true)]
    [InlineData (1, 2, 9, 0, 7, true)]
    [InlineData (1, 9, 9, 7, 7, true)]
    [InlineData (1, 10, 10, 7, 7, false)]

    //01234567890123456789
    // |12345678|
    // |xxxxxxxx
    public void MouseCoordinatesTest_Border (
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

        Application.Top = new Toplevel ()
        {
            Id = "top",
        };
        Application.Top.X = 0;
        Application.Top.Y = 0;
        Application.Top.Width = size.Width * 2;
        Application.Top.Height = size.Height * 2;
        Application.Top.BorderStyle = LineStyle.None;

        var view = new View { Id = "view", X = pos.X, Y = pos.Y, Width = size.Width, Height = size.Height };

        // Give the view a border. With PR #2920, mouse clicks are only passed if they are inside the view's Viewport.
        view.BorderStyle = LineStyle.Single;
        view.CanFocus = true;

        Application.Top.Add (view);

        var mouseEvent = new MouseEventArgs { Position = new (clickX, clickY), ScreenPosition = new (clickX, clickY), Flags = MouseFlags.Button1Clicked };

        view.MouseClick += (s, e) =>
                           {
                               Assert.Equal (expectedX, e.Position.X);
                               Assert.Equal (expectedY, e.Position.Y);
                               clicked = true;
                           };

        Application.RaiseMouseEvent (mouseEvent);
        Assert.Equal (expectedClicked, clicked);
        Application.Top.Dispose ();
        Application.ResetState (ignoreDisposed: true);

    }

    #endregion mouse coordinate tests

    #region mouse grab tests

    [Fact]
    [AutoInitShutdown]
    public void MouseGrabView_WithNullMouseEventView ()
    {
        var tf = new TextField { Width = 10 };
        var sv = new ScrollView { Width = Dim.Fill (), Height = Dim.Fill () };
        sv.SetContentSize (new (100, 100));

        sv.Add (tf);
        var top = new Toplevel ();
        top.Add (sv);

        int iterations = -1;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         Assert.True (tf.HasFocus);
                                         Assert.Null (Application.MouseGrabView);

                                         Application.RaiseMouseEvent (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.ReportMousePosition });

                                         Assert.Equal (sv, Application.MouseGrabView);

                                         MessageBox.Query ("Title", "Test", "Ok");

                                         Assert.Null (Application.MouseGrabView);
                                     }
                                     else if (iterations == 1)
                                     {
                                         // Application.MouseGrabView is null because
                                         // another toplevel (Dialog) was opened
                                         Assert.Null (Application.MouseGrabView);

                                         Application.RaiseMouseEvent (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.ReportMousePosition });

                                         Assert.Null (Application.MouseGrabView);

                                         Application.RaiseMouseEvent (new () { ScreenPosition = new (40, 12), Flags = MouseFlags.ReportMousePosition });

                                         Assert.Null (Application.MouseGrabView);

                                         Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });

                                         Assert.Null (Application.MouseGrabView);

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 2)
                                     {
                                         Assert.Null (Application.MouseGrabView);

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseGrabView_GrabbedMouse_UnGrabbedMouse ()
    {
        View grabView = null;
        var count = 0;

        var view1 = new View { Id = "view1" };
        var view2 = new View { Id = "view2" };
        var view3 = new View { Id = "view3" };

        Application.GrabbedMouse += Application_GrabbedMouse;
        Application.UnGrabbedMouse += Application_UnGrabbedMouse;

        Application.GrabMouse (view1);
        Assert.Equal (0, count);
        Assert.Equal (grabView, view1);
        Assert.Equal (view1, Application.MouseGrabView);

        Application.UngrabMouse ();
        Assert.Equal (1, count);
        Assert.Equal (grabView, view1);
        Assert.Null (Application.MouseGrabView);

        Application.GrabbedMouse += Application_GrabbedMouse;
        Application.UnGrabbedMouse += Application_UnGrabbedMouse;

        Application.GrabMouse (view2);
        Assert.Equal (1, count);
        Assert.Equal (grabView, view2);
        Assert.Equal (view2, Application.MouseGrabView);

        Application.UngrabMouse ();
        Assert.Equal (2, count);
        Assert.Equal (grabView, view2);
        Assert.Equal (view3, Application.MouseGrabView);
        Application.UngrabMouse ();
        Assert.Null (Application.MouseGrabView);

        void Application_GrabbedMouse (object sender, ViewEventArgs e)
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

            Application.GrabbedMouse -= Application_GrabbedMouse;
        }

        void Application_UnGrabbedMouse (object sender, ViewEventArgs e)
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
                Application.GrabMouse (view3);
            }

            Application.UnGrabbedMouse -= Application_UnGrabbedMouse;
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void View_Is_Responsible_For_Calling_UnGrabMouse_Before_Being_Disposed ()
    {
        var count = 0;
        var view = new View { Width = 1, Height = 1 };
        view.MouseEvent += (s, e) => count++;
        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Assert.Null (Application.MouseGrabView);
        Application.GrabMouse (view);
        Assert.Equal (view, Application.MouseGrabView);
        top.Remove (view);
        Application.UngrabMouse ();
        view.Dispose ();
#if DEBUG_IDISPOSABLE
        Assert.True (view.WasDisposed);
#endif

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });
        Assert.Null (Application.MouseGrabView);
        Assert.Equal (0, count);
        top.Dispose ();
    }

    #endregion
}
