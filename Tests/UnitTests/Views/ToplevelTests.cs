namespace UnitTests.ViewsTests;

public class ToplevelTests
{
    [Fact]
    public void Constructor_Default ()
    {
        var top = new Toplevel ();

        Assert.Equal ("Toplevel", top.SchemeName);
        Assert.Equal ("Fill(Absolute(0))", top.Width.ToString ());
        Assert.Equal ("Fill(Absolute(0))", top.Height.ToString ());
        Assert.False (top.IsRunning);
        Assert.False (top.Modal);

        //Assert.Null (top.StatusBar);
    }

    [Fact]
    public void Arrangement_Default_Is_Overlapped ()
    {
        var top = new Toplevel ();
        Assert.Equal (ViewArrangement.Overlapped, top.Arrangement);
    }

    //[Fact]
    //[AutoInitShutdown]
    //public void Internal_Tests ()
    //{
    //    var top = new Toplevel ();

    //    var eventInvoked = "";

    //    top.Loaded += (s, e) => eventInvoked = "Loaded";
    //    top.OnLoaded ();
    //    Assert.Equal ("Loaded", eventInvoked);
    //    top.Ready += (s, e) => eventInvoked = "Ready";
    //    top.OnReady ();
    //    Assert.Equal ("Ready", eventInvoked);
    //    top.SessionEnded += (s, e) => eventInvoked = "Unloaded";
    //    top.OnUnloaded ();
    //    Assert.Equal ("Unloaded", eventInvoked);

    //    Application.Begin (top);
    //    Assert.Equal (top, Application.TopRunnable);

    //    // Application.TopRunnable without menu and status bar.
    //    View supView = View.GetLocationEnsuringFullVisibility (top, 2, 2, out int nx, out int ny /*, out StatusBar sb*/);
    //    Assert.Equal (Application.TopRunnable, supView);
    //    Assert.Equal (0, nx);
    //    Assert.Equal (0, ny);
    //  // Application.Current with a menu and without status bar.
    //    View.GetLocationEnsuringFullVisibility (top, 2, 2, out nx, out ny /*, out sb*/);
    //    Assert.Equal (0, nx);
    //    Assert.Equal (0, ny);
    //    // Application.TopRunnable with a menu and status bar.
    //    View.GetLocationEnsuringFullVisibility (top, 2, 2, out nx, out ny /*, out sb*/);
    //    Assert.Equal (0, nx);

    // // Application.TopRunnable without a menu and with a status bar.
    //    View.GetLocationEnsuringFullVisibility (top, 2, 2, out nx, out ny /*, out sb*/);
    //    Assert.Equal (0, nx);


    //    var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
    //    top.Add (win);
    //    top.LayoutSubViews ();

    //    // The SuperView is always the same regardless of the caller.
    //    supView = View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny /*, out sb*/);
    //    Assert.Equal (Application.TopRunnable, supView);
    //    supView = View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny /*, out sb*/);
    //    Assert.Equal (Application.TopRunnable, supView);

    //    // Application.TopRunnable without menu and status bar.
    //    View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny /*, out sb*/);
    //    Assert.Equal (0, nx);
    //    Assert.Equal (0, ny);
    //    top.Remove (win);

    //    win = new () { Width = 60, Height = 15 };
    //    top.Add (win);
    //}

    [Fact]
    public void SuperViewChanged_Should_Not_Be_Used_To_Initialize_Toplevel_Events ()
    {
        var wasAdded = false;

        var view = new View ();
        view.SuperViewChanged += SuperViewChanged;

        var win = new Window ();
        win.Add (view);
        Application.Init ("fake");
        Toplevel top = new ();
        top.Add (win);

        Assert.True (wasAdded);

        Application.Shutdown ();

        return;

        void SuperViewChanged (object sender, SuperViewChangedEventArgs _)
        {
            Assert.False (wasAdded);
            wasAdded = true;
            view.SuperViewChanged -= SuperViewChanged;
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Mouse_Drag_On_Top_With_Superview_Null ()
    {
        var win = new Window ();
        Toplevel top = new ();
        top.Add (win);
        int iterations = -1;
        Window testWindow;

        Application.Iteration += OnApplicationOnIteration;

        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;
        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication> a)
        {
            iterations++;

            if (iterations == 0)
            {
                Application.Driver?.SetScreenSize (15, 7);

                // Don't use MessageBox here; it's too complicated for this unit test; just use Window
                testWindow = new ()
                {
                    Text = "Hello",
                    X = 2,
                    Y = 2,
                    Width = 10,
                    Height = 3,
                    Arrangement = ViewArrangement.Movable
                };
                Application.Run (testWindow);
            }
            else if (iterations == 1)
            {
                Assert.Equal (new (2, 2), Application.TopRunnableView!.Frame.Location);
            }
            else if (iterations == 2)
            {
                Assert.Null (Application.Mouse.MouseGrabView);

                // Grab the mouse
                Application.RaiseMouseEvent (new () { ScreenPosition = new (3, 2), Flags = MouseFlags.Button1Pressed });

                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (2, 2, 10, 3), Application.TopRunnableView.Frame);
            }
            else if (iterations == 3)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);

                // Drag to left
                Application.RaiseMouseEvent (
                                             new ()
                                             {
                                                 ScreenPosition = new (2, 2),
                                                 Flags = MouseFlags.Button1Pressed
                                                         | MouseFlags.ReportMousePosition
                                             });
                AutoInitShutdownAttribute.RunIteration ();

                Assert.Equal (Application.TopRunnableView.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 2, 10, 3), Application.TopRunnableView.Frame);
            }
            else if (iterations == 4)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 2), Application.TopRunnableView.Frame.Location);

                Assert.Equal (Application.TopRunnableView.Border, Application.Mouse.MouseGrabView);
            }
            else if (iterations == 5)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);

                // Drag up
                Application.RaiseMouseEvent (new () { ScreenPosition = new (2, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition });
                AutoInitShutdownAttribute.RunIteration ();

                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 1, 10, 3), Application.TopRunnableView.Frame);
            }
            else if (iterations == 6)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 1), Application.TopRunnableView.Frame.Location);

                Assert.Equal (Application.TopRunnableView.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 1, 10, 3), Application.TopRunnableView.Frame);
            }
            else if (iterations == 7)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);

                // Ungrab the mouse
                Application.RaiseMouseEvent (new () { ScreenPosition = new (2, 1), Flags = MouseFlags.Button1Released });
                AutoInitShutdownAttribute.RunIteration ();

                Assert.Null (Application.Mouse.MouseGrabView);
            }
            else if (iterations == 8)
            {
                Application.RequestStop ();
            }
            else if (iterations == 9)
            {
                Application.RequestStop ();
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Mouse_Drag_On_Top_With_Superview_Not_Null ()
    {
        var win = new Window { X = 3, Y = 2, Width = 10, Height = 5, Arrangement = ViewArrangement.Movable };
        Toplevel top = new ();
        top.Add (win);

        int iterations = -1;

        var movex = 0;
        var movey = 0;

        var location = new Rectangle (win.Frame.X, win.Frame.Y, 7, 3);

        Application.Iteration += OnApplicationOnIteration;

        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;
        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication> a)
        {
            iterations++;

            if (iterations == 0)
            {
                Application.Driver?.SetScreenSize (30, 10);
            }
            else if (iterations == 1)
            {
                location = win.Frame;

                Assert.Null (Application.Mouse.MouseGrabView);

                // Grab the mouse
                Application.RaiseMouseEvent (new () { ScreenPosition = new (win.Frame.X, win.Frame.Y), Flags = MouseFlags.Button1Pressed });

                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
            }
            else if (iterations == 2)
            {
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);

                // Drag to left
                movex = 1;
                movey = 0;

                Application.RaiseMouseEvent (
                                             new ()
                                             {
                                                 ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey),
                                                 Flags = MouseFlags.Button1Pressed
                                                         | MouseFlags.ReportMousePosition
                                             });

                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
            }
            else if (iterations == 3)
            {
                // we should have moved +1, +0
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
                location.Offset (movex, movey);
            }
            else if (iterations == 4)
            {
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);

                // Drag up
                movex = 0;
                movey = -1;

                Application.RaiseMouseEvent (
                                             new ()
                                             {
                                                 ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey),
                                                 Flags = MouseFlags.Button1Pressed
                                                         | MouseFlags.ReportMousePosition
                                             });

                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
            }
            else if (iterations == 5)
            {
                // we should have moved +0, -1
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
                location.Offset (movex, movey);
                Assert.Equal (location, win.Frame);
            }
            else if (iterations == 6)
            {
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);

                // Ungrab the mouse
                movex = 0;
                movey = 0;

                Application.RaiseMouseEvent (new () { ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey), Flags = MouseFlags.Button1Released });

                Assert.Null (Application.Mouse.MouseGrabView);
            }
            else if (iterations == 7)
            {
                Application.RequestStop ();
            }
        }
    }

    [Fact]
    [SetupFakeApplication]
    public void GetLocationThatFits_With_Border_Null_Not_Throws ()
    {
        var top = new Toplevel ();
        top.BeginInit ();
        top.EndInit ();

        Exception exception = Record.Exception (() => Application.Driver!.SetScreenSize (0, 10));
        Assert.Null (exception);

        exception = Record.Exception (() => Application.Driver!.SetScreenSize (10, 0));
        Assert.Null (exception);
    }

    [Fact]
    [AutoInitShutdown]
    public void PositionCursor_SetCursorVisibility_To_Invisible_If_Focused_Is_Null ()
    {
        var tf = new TextField { Width = 5, Text = "test" };
        var view = new View { Width = 10, Height = 10, CanFocus = true };
        view.Add (tf);
        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Assert.True (tf.HasFocus);
        Application.PositionCursor ();
        Application.Driver!.GetCursorVisibility (out CursorVisibility cursor);
        Assert.Equal (CursorVisibility.Default, cursor);

        view.Enabled = false;
        Assert.False (tf.HasFocus);
        Application.PositionCursor ();
        Application.Driver!.GetCursorVisibility (out cursor);
        Assert.Equal (CursorVisibility.Invisible, cursor);
        top.Dispose ();
    }

    [Fact (Skip = "Toplevel is going away")]
    [AutoInitShutdown]
    public void IsLoaded_Application_Begin ()
    {
        Toplevel top = new ();
        Assert.False (top.IsLoaded);

        Application.Begin (top);
        Assert.True (top.IsLoaded);
        top.Dispose ();
    }

    [Fact (Skip = "Toplevel is going away")]
    [AutoInitShutdown]
    public void IsLoaded_With_Sub_Toplevel_Application_Begin_NeedDisplay ()
    {
        Toplevel top = new ();
        var subTop = new Toplevel ();
        var view = new View { Frame = new (0, 0, 20, 10) };
        subTop.Add (view);
        top.Add (subTop);

        Assert.False (top.IsLoaded);
        Assert.False (subTop.IsLoaded);
        Assert.Equal (new (0, 0, 20, 10), view.Frame);

        view.SubViewLayout += ViewLayoutStarted;

        void ViewLayoutStarted (object sender, LayoutEventArgs e)
        {
            Assert.Equal (new (0, 0, 20, 10), view.NeedsDrawRect);
            view.SubViewLayout -= ViewLayoutStarted;
        }

        Application.Begin (top);

        Assert.True (top.IsLoaded);
        Assert.True (subTop.IsLoaded);
        Assert.Equal (new (0, 0, 20, 10), view.Frame);

        view.Frame = new (1, 3, 10, 5);
        Assert.Equal (new (1, 3, 10, 5), view.Frame);
        Assert.Equal (new (0, 0, 10, 5), view.NeedsDrawRect);

        view.Frame = new (1, 3, 10, 5);
        top.Layout ();
        Assert.Equal (new (1, 3, 10, 5), view.Frame);
        Assert.Equal (new (0, 0, 10, 5), view.NeedsDrawRect);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Window_Viewport_Bigger_Than_Driver_Cols_And_Rows_Allow_Drag_Beyond_Left_Right_And_Bottom ()
    {
        Toplevel top = new ();
        var window = new Window { Width = 20, Height = 3, Arrangement = ViewArrangement.Movable };
        SessionToken rsTop = Application.Begin (top);
        Application.Driver?.SetScreenSize (40, 10);

        SessionToken rsWindow = Application.Begin (window);
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (new (0, 0, 40, 10), top.Frame);
        Assert.Equal (new (0, 0, 20, 3), window.Frame);

        Assert.Null (Application.Mouse.MouseGrabView);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });

        Assert.Equal (window.Border, Application.Mouse.MouseGrabView);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (-11, -4), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (new (0, 0, 40, 10), top.Frame);
        Assert.Equal (new (-11, -4, 20, 3), window.Frame);

        // Changes Top size to same size as Dialog more menu and scroll bar
        Application.Driver?.SetScreenSize (20, 3);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (-1, -1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (new (0, 0, 20, 3), top.Frame);
        Assert.Equal (new (-1, -1, 20, 3), window.Frame);

        // Changes Top size smaller than Dialog size
        Application.Driver?.SetScreenSize (19, 2);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (-1, -1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (new (0, 0, 19, 2), top.Frame);
        Assert.Equal (new (-1, -1, 20, 3), window.Frame);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (18, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (new (0, 0, 19, 2), top.Frame);
        Assert.Equal (new (18, 1, 20, 3), window.Frame);

        // On a real app we can't go beyond the SuperView bounds
        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (19, 2), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (new (0, 0, 19, 2), top.Frame);
        Assert.Equal (new (19, 2, 20, 3), window.Frame);

        //DriverAsserts.AssertDriverContentsWithFrameAre (@"", output);

        Application.End (rsWindow);
        Application.End (rsTop);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Modal_As_Top_Will_Drag_Cleanly ()
    {
        // Don't use Dialog as a Top, use a Window instead - dialog has complex layout behavior that is not needed here.
        var window = new Window { Width = 10, Height = 3, Arrangement = ViewArrangement.Movable };

        window.Add (
                    new Label
                    {
                        X = Pos.Center (),
                        Y = Pos.Center (),
                        Width = Dim.Fill (),
                        Height = Dim.Fill (),
                        TextAlignment = Alignment.Center,
                        VerticalTextAlignment = Alignment.Center,
                        Text = "Test"
                    }
                   );

        SessionToken rs = Application.Begin (window);

        Assert.Null (Application.Mouse.MouseGrabView);
        Assert.Equal (new (0, 0, 10, 3), window.Frame);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });

        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (window.Border, Application.Mouse.MouseGrabView);

        Assert.Equal (new (0, 0, 10, 3), window.Frame);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (window.Border, Application.Mouse.MouseGrabView);
        Assert.Equal (new (1, 1, 10, 3), window.Frame);

        Application.End (rs);
        window.Dispose ();
    }
}
