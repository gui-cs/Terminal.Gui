using UnitTests;

namespace Terminal.Gui.ViewsTests;

public class ToplevelTests
{
    [Fact]
    public void Constructor_Default ()
    {
        var top = new Toplevel ();

        Assert.Equal ("Toplevel", top.SchemeName);
        Assert.Equal ("Fill(Absolute(0))", top.Width.ToString ());
        Assert.Equal ("Fill(Absolute(0))", top.Height.ToString ());
        Assert.False (top.Running);
        Assert.False (top.Modal);
        Assert.Null (top.MenuBar);

        //Assert.Null (top.StatusBar);
    }

    [Fact]
    public void Arrangement_Default_Is_Overlapped ()
    {
        var top = new Toplevel ();
        Assert.Equal (ViewArrangement.Overlapped, top.Arrangement);
    }

    [Fact]
    [AutoInitShutdown]
    public void Internal_Tests ()
    {
        var top = new Toplevel ();

        var eventInvoked = "";

        top.Loaded += (s, e) => eventInvoked = "Loaded";
        top.OnLoaded ();
        Assert.Equal ("Loaded", eventInvoked);
        top.Ready += (s, e) => eventInvoked = "Ready";
        top.OnReady ();
        Assert.Equal ("Ready", eventInvoked);
        top.Unloaded += (s, e) => eventInvoked = "Unloaded";
        top.OnUnloaded ();
        Assert.Equal ("Unloaded", eventInvoked);

        top.Add (new MenuBar ());
        Assert.NotNull (top.MenuBar);

        //top.Add (new StatusBar ());
        //Assert.NotNull (top.StatusBar);
        MenuBar menuBar = top.MenuBar;
        top.Remove (top.MenuBar);
        Assert.Null (top.MenuBar);
        Assert.NotNull (menuBar);

        //var statusBar = top.StatusBar;
        //top.Remove (top.StatusBar);
        //Assert.Null (top.StatusBar);
        //Assert.NotNull (statusBar);
#if DEBUG_IDISPOSABLE
        Assert.False (menuBar.WasDisposed);

        //Assert.False (statusBar.WasDisposed);
        menuBar.Dispose ();

        //statusBar.Dispose ();
        Assert.True (menuBar.WasDisposed);

        //Assert.True (statusBar.WasDisposed);
#endif

        Application.Begin (top);
        Assert.Equal (top, Application.Top);

        // Application.Top without menu and status bar.
        View supView = View.GetLocationEnsuringFullVisibility (top, 2, 2, out int nx, out int ny /*, out StatusBar sb*/);
        Assert.Equal (Application.Top, supView);
        Assert.Equal (0, nx);
        Assert.Equal (0, ny);

        //Assert.Null (sb);

        top.Add (new MenuBar ());
        Assert.NotNull (top.MenuBar);

        // Application.Top with a menu and without status bar.
        View.GetLocationEnsuringFullVisibility (top, 2, 2, out nx, out ny /*, out sb*/);
        Assert.Equal (0, nx);
        Assert.Equal (1, ny);

        //Assert.Null (sb);

        //top.Add (new StatusBar ());
        //Assert.NotNull (top.StatusBar);

        // Application.Top with a menu and status bar.
        View.GetLocationEnsuringFullVisibility (top, 2, 2, out nx, out ny /*, out sb*/);
        Assert.Equal (0, nx);

        // The available height is lower than the Application.Top height minus
        // the menu bar and status bar, then the top can go beyond the bottom
        //        Assert.Equal (2, ny);
        //Assert.NotNull (sb);

        menuBar = top.MenuBar;
        top.Remove (top.MenuBar);
        Assert.Null (top.MenuBar);
        Assert.NotNull (menuBar);

        // Application.Top without a menu and with a status bar.
        View.GetLocationEnsuringFullVisibility (top, 2, 2, out nx, out ny /*, out sb*/);
        Assert.Equal (0, nx);

        // The available height is lower than the Application.Top height minus
        // the status bar, then the top can go beyond the bottom
        //        Assert.Equal (2, ny);
        //Assert.NotNull (sb);

        //statusBar = top.StatusBar;
        //top.Remove (top.StatusBar);
        //Assert.Null (top.StatusBar);
        //Assert.NotNull (statusBar);
        Assert.Null (top.MenuBar);

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        top.Add (win);
        top.LayoutSubViews ();

        // The SuperView is always the same regardless of the caller.
        supView = View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny /*, out sb*/);
        Assert.Equal (Application.Top, supView);
        supView = View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny /*, out sb*/);
        Assert.Equal (Application.Top, supView);

        // Application.Top without menu and status bar.
        View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny /*, out sb*/);
        Assert.Equal (0, nx);
        Assert.Equal (0, ny);

        //Assert.Null (sb);

        top.Add (new MenuBar ());
        Assert.NotNull (top.MenuBar);

        // Application.Top with a menu and without status bar.
        View.GetLocationEnsuringFullVisibility (win, 2, 2, out nx, out ny /*, out sb*/);
        Assert.Equal (0, nx);
        Assert.Equal (1, ny);

        //Assert.Null (sb);

        top.Add (new StatusBar ());

        //Assert.NotNull (top.StatusBar);

        // Application.Top with a menu and status bar.
        View.GetLocationEnsuringFullVisibility (win, 30, 20, out nx, out ny /*, out sb*/);
        Assert.Equal (0, nx);

        // The available height is lower than the Application.Top height minus
        // the menu bar and status bar, then the top can go beyond the bottom
        //Assert.Equal (20, ny);
        //Assert.NotNull (sb);

        menuBar = top.MenuBar;

        //statusBar = top.StatusBar;
        top.Remove (top.MenuBar);
        Assert.Null (top.MenuBar);
        Assert.NotNull (menuBar);

        //top.Remove (top.StatusBar);
        //Assert.Null (top.StatusBar);
        //Assert.NotNull (statusBar);

        top.Remove (win);

        win = new () { Width = 60, Height = 15 };
        top.Add (win);

        // Application.Top without menu and status bar.
        View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny /*, out sb*/);
        Assert.Equal (0, nx);
        Assert.Equal (0, ny);

        //Assert.Null (sb);

        top.Add (new MenuBar ());
        Assert.NotNull (top.MenuBar);

        // Application.Top with a menu and without status bar.
        View.GetLocationEnsuringFullVisibility (win, 2, 2, out nx, out ny /*, out sb*/);
        Assert.Equal (2, nx);
        Assert.Equal (2, ny);

        //Assert.Null (sb);

        top.Add (new StatusBar ());

        //Assert.NotNull (top.StatusBar);

        // Application.Top with a menu and status bar.
        View.GetLocationEnsuringFullVisibility (win, 30, 20, out nx, out ny /*, out sb*/);
        Assert.Equal (20, nx); // 20+60=80

        //Assert.Equal (9, ny); // 9+15+1(mb)=25
        //Assert.NotNull (sb);

        //Assert.Null (Toplevel._dragPosition);
        win.NewMouseEvent (new () { Position = new (6, 0), Flags = MouseFlags.Button1Pressed });

        // Assert.Equal (new Point (6, 0), Toplevel._dragPosition);
        win.NewMouseEvent (new () { Position = new (6, 0), Flags = MouseFlags.Button1Released });

        //Assert.Null (Toplevel._dragPosition);
        win.CanFocus = false;
        win.NewMouseEvent (new () { Position = new (6, 0), Flags = MouseFlags.Button1Pressed });

        //Assert.Null (Toplevel._dragPosition);
#if DEBUG_IDISPOSABLE

        Assert.False (top.MenuBar.WasDisposed);

        //Assert.False (top.StatusBar.WasDisposed);
#endif
        menuBar = top.MenuBar;

        //statusBar = top.StatusBar;
        top.Dispose ();
        Assert.Null (top.MenuBar);

        //Assert.Null (top.StatusBar);
        Assert.NotNull (menuBar);

        //Assert.NotNull (statusBar);
#if DEBUG_IDISPOSABLE
        Assert.True (menuBar.WasDisposed);

        //Assert.True (statusBar.WasDisposed);
#endif
    }

    [Fact]
    public void SuperViewChanged_Should_Not_Be_Used_To_Initialize_Toplevel_Events ()
    {
        var wasAdded = false;

        var view = new View ();
        view.SuperViewChanged += SuperViewChanged;

        var win = new Window ();
        win.Add (view);
        Application.Init (new FakeDriver ());
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

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         ((FakeDriver)Application.Driver!).SetBufferSize (15, 7);

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
                                         Assert.Equal (new (2, 2), Application.Top!.Frame.Location);
                                     }
                                     else if (iterations == 2)
                                     {
                                         Assert.Null (Application.MouseGrabHandler.MouseGrabView);

                                         // Grab the mouse
                                         Application.RaiseMouseEvent (new () { ScreenPosition = new (3, 2), Flags = MouseFlags.Button1Pressed });

                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabHandler.MouseGrabView);
                                         Assert.Equal (new (2, 2, 10, 3), Application.Top.Frame);
                                     }
                                     else if (iterations == 3)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabHandler.MouseGrabView);

                                         // Drag to left
                                         Application.RaiseMouseEvent (
                                                                      new ()
                                                                      {
                                                                          ScreenPosition = new (2, 2), Flags = MouseFlags.Button1Pressed
                                                                              | MouseFlags.ReportMousePosition
                                                                      });
                                         Application.LayoutAndDraw ();

                                         Assert.Equal (Application.Top.Border, Application.MouseGrabHandler.MouseGrabView);
                                         Assert.Equal (new (1, 2, 10, 3), Application.Top.Frame);
                                     }
                                     else if (iterations == 4)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabHandler.MouseGrabView);
                                         Assert.Equal (new (1, 2), Application.Top.Frame.Location);

                                         Assert.Equal (Application.Top.Border, Application.MouseGrabHandler.MouseGrabView);
                                     }
                                     else if (iterations == 5)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabHandler.MouseGrabView);

                                         // Drag up
                                         Application.RaiseMouseEvent (
                                                                      new ()
                                                                      {
                                                                          ScreenPosition = new (2, 1),
                                                                          Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                                                      });
                                         Application.LayoutAndDraw ();

                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabHandler.MouseGrabView);
                                         Assert.Equal (new (1, 1, 10, 3), Application.Top.Frame);
                                     }
                                     else if (iterations == 6)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabHandler.MouseGrabView);
                                         Assert.Equal (new (1, 1), Application.Top.Frame.Location);

                                         Assert.Equal (Application.Top.Border, Application.MouseGrabHandler.MouseGrabView);
                                         Assert.Equal (new (1, 1, 10, 3), Application.Top.Frame);
                                     }
                                     else if (iterations == 7)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabHandler.MouseGrabView);

                                         // Ungrab the mouse
                                         Application.RaiseMouseEvent (new () { ScreenPosition = new (2, 1), Flags = MouseFlags.Button1Released });
                                         Application.LayoutAndDraw ();

                                         Assert.Null (Application.MouseGrabHandler.MouseGrabView);
                                     }
                                     else if (iterations == 8)
                                     {
                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 9)
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
        top.Dispose ();
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

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         ((FakeDriver)Application.Driver!).SetBufferSize (30, 10);
                                     }
                                     else if (iterations == 1)
                                     {
                                         location = win.Frame;

                                         Assert.Null (Application.MouseGrabHandler.MouseGrabView);

                                         // Grab the mouse
                                         Application.RaiseMouseEvent (
                                                                      new ()
                                                                      {
                                                                          ScreenPosition = new (win.Frame.X, win.Frame.Y), Flags = MouseFlags.Button1Pressed
                                                                      });

                                         Assert.Equal (win.Border, Application.MouseGrabHandler.MouseGrabView);
                                     }
                                     else if (iterations == 2)
                                     {
                                         Assert.Equal (win.Border, Application.MouseGrabHandler.MouseGrabView);

                                         // Drag to left
                                         movex = 1;
                                         movey = 0;

                                         Application.RaiseMouseEvent (
                                                                      new ()
                                                                      {
                                                                          ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey), Flags =
                                                                              MouseFlags.Button1Pressed
                                                                              | MouseFlags.ReportMousePosition
                                                                      });

                                         Assert.Equal (win.Border, Application.MouseGrabHandler.MouseGrabView);
                                     }
                                     else if (iterations == 3)
                                     {
                                         // we should have moved +1, +0
                                         Assert.Equal (win.Border, Application.MouseGrabHandler.MouseGrabView);
                                         Assert.Equal (win.Border, Application.MouseGrabHandler.MouseGrabView);
                                         location.Offset (movex, movey);
                                     }
                                     else if (iterations == 4)
                                     {
                                         Assert.Equal (win.Border, Application.MouseGrabHandler.MouseGrabView);

                                         // Drag up
                                         movex = 0;
                                         movey = -1;

                                         Application.RaiseMouseEvent (
                                                                      new ()
                                                                      {
                                                                          ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey), Flags =
                                                                              MouseFlags.Button1Pressed
                                                                              | MouseFlags.ReportMousePosition
                                                                      });

                                         Assert.Equal (win.Border, Application.MouseGrabHandler.MouseGrabView);
                                     }
                                     else if (iterations == 5)
                                     {
                                         // we should have moved +0, -1
                                         Assert.Equal (win.Border, Application.MouseGrabHandler.MouseGrabView);
                                         location.Offset (movex, movey);
                                         Assert.Equal (location, win.Frame);
                                     }
                                     else if (iterations == 6)
                                     {
                                         Assert.Equal (win.Border, Application.MouseGrabHandler.MouseGrabView);

                                         // Ungrab the mouse
                                         movex = 0;
                                         movey = 0;

                                         Application.RaiseMouseEvent (
                                                                      new ()
                                                                      {
                                                                          ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey),
                                                                          Flags = MouseFlags.Button1Released
                                                                      });

                                         Assert.Null (Application.MouseGrabHandler.MouseGrabView);
                                     }
                                     else if (iterations == 7)
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void GetLocationThatFits_With_Border_Null_Not_Throws ()
    {
        var top = new Toplevel ();
        top.BeginInit ();
        top.EndInit ();

        Exception exception = Record.Exception (() => ((FakeDriver)Application.Driver!).SetBufferSize (0, 10));
        Assert.Null (exception);

        exception = Record.Exception (() => ((FakeDriver)Application.Driver!).SetBufferSize (10, 0));
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

    [Fact]
    [AutoInitShutdown]
    public void IsLoaded_Application_Begin ()
    {
        Toplevel top = new ();
        Assert.False (top.IsLoaded);

        Application.Begin (top);
        Assert.True (top.IsLoaded);
        top.Dispose ();
    }

    [Fact]
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
        RunState rsTop = Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (40, 10);
        RunState rsWindow = Application.Begin (window);
        Application.LayoutAndDraw ();
        Assert.Equal (new (0, 0, 40, 10), top.Frame);
        Assert.Equal (new (0, 0, 20, 3), window.Frame);

        Assert.Null (Application.MouseGrabHandler.MouseGrabView);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });

        Assert.Equal (window.Border, Application.MouseGrabHandler.MouseGrabView);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (-11, -4), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        Application.LayoutAndDraw ();
        Assert.Equal (new (0, 0, 40, 10), top.Frame);
        Assert.Equal (new (-11, -4, 20, 3), window.Frame);

        // Changes Top size to same size as Dialog more menu and scroll bar
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 3);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (-1, -1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        Application.LayoutAndDraw ();
        Assert.Equal (new (0, 0, 20, 3), top.Frame);
        Assert.Equal (new (-1, -1, 20, 3), window.Frame);

        // Changes Top size smaller than Dialog size
        ((FakeDriver)Application.Driver!).SetBufferSize (19, 2);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (-1, -1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        Application.LayoutAndDraw ();
        Assert.Equal (new (0, 0, 19, 2), top.Frame);
        Assert.Equal (new (-1, -1, 20, 3), window.Frame);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (18, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        Application.LayoutAndDraw ();
        Assert.Equal (new (0, 0, 19, 2), top.Frame);
        Assert.Equal (new (18, 1, 20, 3), window.Frame);

        // On a real app we can't go beyond the SuperView bounds
        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (19, 2), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        Application.LayoutAndDraw ();
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

        RunState rs = Application.Begin (window);

        Assert.Null (Application.MouseGrabHandler.MouseGrabView);
        Assert.Equal (new (0, 0, 10, 3), window.Frame);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });

        var firstIteration = false;
        Application.RunIteration (ref rs, firstIteration);
        Assert.Equal (window.Border, Application.MouseGrabHandler.MouseGrabView);

        Assert.Equal (new (0, 0, 10, 3), window.Frame);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        firstIteration = false;
        Application.RunIteration (ref rs, firstIteration);
        Assert.Equal (window.Border, Application.MouseGrabHandler.MouseGrabView);
        Assert.Equal (new (1, 1, 10, 3), window.Frame);

        Application.End (rs);
        window.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Activating_MenuBar_By_Alt_Key_Does_Not_Throw ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new ("Child", new MenuItem [] { new ("_Create Child", "", null) })
            ]
        };
        var topChild = new Toplevel ();
        topChild.Add (menu);
        var top = new Toplevel ();
        top.Add (topChild);
        Application.Begin (top);

        Exception exception = Record.Exception (() => topChild.NewKeyDownEvent (KeyCode.AltMask));
        Assert.Null (exception);
        top.Dispose ();
    }

    [Fact]
    public void Multi_Thread_Toplevels ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();
        var w = new Window ();
        t.Add (w);

        int count = 0, count1 = 0, count2 = 0;
        bool log = false, log1 = false, log2 = false;
        var fromTopStillKnowFirstIsRunning = false;
        var fromTopStillKnowSecondIsRunning = false;
        var fromFirstStillKnowSecondIsRunning = false;

        Application.AddTimeout (
                                TimeSpan.FromMilliseconds (100),
                                () =>
                                {
                                    count++;

                                    if (count1 == 5)
                                    {
                                        log1 = true;
                                    }

                                    if (count1 == 14 && count2 == 10 && count == 15)
                                    {
                                        // count2 is already stopped
                                        fromTopStillKnowFirstIsRunning = true;
                                    }

                                    if (count1 == 7 && count2 == 7 && count == 8)
                                    {
                                        fromTopStillKnowSecondIsRunning = true;
                                    }

                                    if (count == 30)
                                    {
                                        Assert.Equal (30, count);
                                        Assert.Equal (20, count1);
                                        Assert.Equal (10, count2);

                                        Assert.True (log);
                                        Assert.True (log1);
                                        Assert.True (log2);

                                        Assert.True (fromTopStillKnowFirstIsRunning);
                                        Assert.True (fromTopStillKnowSecondIsRunning);
                                        Assert.True (fromFirstStillKnowSecondIsRunning);

                                        Application.RequestStop ();

                                        return false;
                                    }

                                    return true;
                                }
                               );

        t.Ready += FirstWindow;

        void FirstWindow (object sender, EventArgs args)
        {
            var firstWindow = new Window ();
            firstWindow.Ready += SecondWindow;

            Application.AddTimeout (
                                    TimeSpan.FromMilliseconds (100),
                                    () =>
                                    {
                                        count1++;

                                        if (count2 == 5)
                                        {
                                            log2 = true;
                                        }

                                        if (count2 == 4 && count1 == 5 && count == 5)
                                        {
                                            fromFirstStillKnowSecondIsRunning = true;
                                        }

                                        if (count1 == 20)
                                        {
                                            Assert.Equal (20, count1);
                                            Application.RequestStop ();

                                            return false;
                                        }

                                        return true;
                                    }
                                   );

            Application.Run (firstWindow);
            firstWindow.Dispose ();
        }

        void SecondWindow (object sender, EventArgs args)
        {
            var testWindow = new Window ();

            Application.AddTimeout (
                                    TimeSpan.FromMilliseconds (100),
                                    () =>
                                    {
                                        count2++;

                                        if (count < 30)
                                        {
                                            log = true;
                                        }

                                        if (count2 == 10)
                                        {
                                            Assert.Equal (10, count2);
                                            Application.RequestStop ();

                                            return false;
                                        }

                                        return true;
                                    }
                                   );

            Application.Run (testWindow);
            testWindow.Dispose ();
        }

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    public void Remove_Do_Not_Dispose_MenuBar_Or_StatusBar ()
    {
        var mb = new MenuBar ();
        var sb = new StatusBar ();
        var tl = new Toplevel ();

#if DEBUG
        Assert.False (mb.WasDisposed);
        Assert.False (sb.WasDisposed);
#endif
        tl.Add (mb, sb);
        Assert.NotNull (tl.MenuBar);

        //Assert.NotNull (tl.StatusBar);
#if DEBUG
        Assert.False (mb.WasDisposed);
        Assert.False (sb.WasDisposed);
#endif
        tl.RemoveAll ();
        Assert.Null (tl.MenuBar);

        //Assert.Null (tl.StatusBar);
#if DEBUG
        Assert.False (mb.WasDisposed);
        Assert.False (sb.WasDisposed);
#endif
    }
}
