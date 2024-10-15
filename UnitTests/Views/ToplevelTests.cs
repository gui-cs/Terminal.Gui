using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public partial class ToplevelTests (ITestOutputHelper output)
{
    [Fact]
    public void Constructor_Default ()
    {
        var top = new Toplevel ();

        Assert.Equal (Colors.ColorSchemes ["TopLevel"], top.ColorScheme);
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
        var menuBar = top.MenuBar;
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
        View supView = View.GetLocationEnsuringFullVisibility (top, 2, 2, out int nx, out int ny/*, out StatusBar sb*/);
        Assert.Equal (Application.Top, supView);
        Assert.Equal (0, nx);
        Assert.Equal (0, ny);
        //Assert.Null (sb);

        top.Add (new MenuBar ());
        Assert.NotNull (top.MenuBar);

        // Application.Top with a menu and without status bar.
        View.GetLocationEnsuringFullVisibility (top, 2, 2, out nx, out ny/*, out sb*/);
        Assert.Equal (0, nx);
        Assert.Equal (1, ny);
        //Assert.Null (sb);

        //top.Add (new StatusBar ());
        //Assert.NotNull (top.StatusBar);

        // Application.Top with a menu and status bar.
        View.GetLocationEnsuringFullVisibility (top, 2, 2, out nx, out ny/*, out sb*/);
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
        View.GetLocationEnsuringFullVisibility (top, 2, 2, out nx, out ny/*, out sb*/);
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
        top.LayoutSubviews ();

        // The SuperView is always the same regardless of the caller.
        supView = View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny/*, out sb*/);
        Assert.Equal (Application.Top, supView);
        supView = View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny/*, out sb*/);
        Assert.Equal (Application.Top, supView);

        // Application.Top without menu and status bar.
        View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny/*, out sb*/);
        Assert.Equal (0, nx);
        Assert.Equal (0, ny);
        //Assert.Null (sb);

        top.Add (new MenuBar ());
        Assert.NotNull (top.MenuBar);

        // Application.Top with a menu and without status bar.
        View.GetLocationEnsuringFullVisibility (win, 2, 2, out nx, out ny/*, out sb*/);
        Assert.Equal (0, nx);
        Assert.Equal (1, ny);
        //Assert.Null (sb);

        top.Add (new StatusBar ());
        //Assert.NotNull (top.StatusBar);

        // Application.Top with a menu and status bar.
        View.GetLocationEnsuringFullVisibility (win, 30, 20, out nx, out ny/*, out sb*/);
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
        View.GetLocationEnsuringFullVisibility (win, 0, 0, out nx, out ny/*, out sb*/);
        Assert.Equal (0, nx);
        Assert.Equal (0, ny);
        //Assert.Null (sb);

        top.Add (new MenuBar ());
        Assert.NotNull (top.MenuBar);

        // Application.Top with a menu and without status bar.
        View.GetLocationEnsuringFullVisibility (win, 2, 2, out nx, out ny/*, out sb*/);
        Assert.Equal (2, nx);
        Assert.Equal (2, ny);
        //Assert.Null (sb);

        top.Add (new StatusBar ());
        //Assert.NotNull (top.StatusBar);

        // Application.Top with a menu and status bar.
        View.GetLocationEnsuringFullVisibility (win, 30, 20, out nx, out ny/*, out sb*/);
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

    [Fact (Skip = "#2491 - Test is broken until #2491 is more mature.")]
    [AutoInitShutdown]
    public void KeyBindings_Command ()
    {
        var isRunning = false;

        var win1 = new Window { Id = "win1", Width = Dim.Percent (50), Height = Dim.Fill () };
        var lblTf1W1 = new Label { Id = "lblTf1W1", Text = "Enter text in TextField on Win1:" };

        var tf1W1 = new TextField
        {
            Id = "tf1W1", X = Pos.Right (lblTf1W1) + 1, Width = Dim.Fill (), Text = "Text1 on Win1"
        };

        var lblTvW1 = new Label
        {
            Id = "lblTvW1", Y = Pos.Bottom (lblTf1W1) + 1, Text = "Enter text in TextView on Win1:"
        };

        var tvW1 = new TextView
        {
            Id = "tvW1",
            X = Pos.Left (tf1W1),
            Width = Dim.Fill (),
            Height = 2,
            Text = "First line Win1\nSecond line Win1"
        };

        var lblTf2W1 = new Label
        {
            Id = "lblTf2W1", Y = Pos.Bottom (lblTvW1) + 1, Text = "Enter text in TextField on Win1:"
        };
        var tf2W1 = new TextField { Id = "tf2W1", X = Pos.Left (tf1W1), Width = Dim.Fill (), Text = "Text2 on Win1" };
        win1.Add (lblTf1W1, tf1W1, lblTvW1, tvW1, lblTf2W1, tf2W1);

        var win2 = new Window
        {
            Id = "win2", X = Pos.Right (win1) + 1, Width = Dim.Percent (50), Height = Dim.Fill ()
        };
        var lblTf1W2 = new Label { Id = "lblTf1W2", Text = "Enter text in TextField on Win2:" };

        var tf1W2 = new TextField
        {
            Id = "tf1W2", X = Pos.Right (lblTf1W2) + 1, Width = Dim.Fill (), Text = "Text1 on Win2"
        };

        var lblTvW2 = new Label
        {
            Id = "lblTvW2", Y = Pos.Bottom (lblTf1W2) + 1, Text = "Enter text in TextView on Win2:"
        };

        var tvW2 = new TextView
        {
            Id = "tvW2",
            X = Pos.Left (tf1W2),
            Width = Dim.Fill (),
            Height = 2,
            Text = "First line Win1\nSecond line Win2"
        };

        var lblTf2W2 = new Label
        {
            Id = "lblTf2W2", Y = Pos.Bottom (lblTvW2) + 1, Text = "Enter text in TextField on Win2:"
        };
        var tf2W2 = new TextField { Id = "tf2W2", X = Pos.Left (tf1W2), Width = Dim.Fill (), Text = "Text2 on Win2" };
        win2.Add (lblTf1W2, tf1W2, lblTvW2, tvW2, lblTf2W2, tf2W2);

        Toplevel top = new ();
        top.Add (win1, win2);
        top.Loaded += (s, e) => isRunning = true;
        top.Closing += (s, e) => isRunning = false;
        Application.Begin (top);
        top.Running = true;

        Assert.Equal (new (0, 0, 40, 25), win1.Frame);
        Assert.Equal (new (41, 0, 40, 25), win2.Frame);
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tf1W1, top.MostFocused);

        Assert.True (isRunning);
        Assert.True (Application.RaiseKeyDownEvent (Application.QuitKey));
        Assert.False (isRunning);
        Assert.True (Application.RaiseKeyDownEvent (Key.Z.WithCtrl));

        Assert.True (Application.RaiseKeyDownEvent (Key.F5)); // refresh

        Assert.True (Application.RaiseKeyDownEvent (Key.Tab));
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tvW1, top.MostFocused);
        Assert.True (Application.RaiseKeyDownEvent (Key.Tab));
        Assert.Equal ($"\tFirst line Win1{Environment.NewLine}Second line Win1", tvW1.Text);
        Assert.True (Application.RaiseKeyDownEvent (Key.Tab.WithShift));
        Assert.Equal ($"First line Win1{Environment.NewLine}Second line Win1", tvW1.Text);

        var prevMostFocusedSubview = top.MostFocused;

        Assert.True (Application.RaiseKeyDownEvent (Key.F6)); // move to next TabGroup (win2)
        Assert.Equal (win2, top.Focused);

        Assert.True (Application.RaiseKeyDownEvent (Key.F6.WithShift)); // move to prev TabGroup (win1)
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tf2W1, top.MostFocused);  // BUGBUG: Should be prevMostFocusedSubview - We need to cache the last focused view in the TabGroup somehow

        prevMostFocusedSubview.SetFocus ();

        Assert.Equal (tvW1, top.MostFocused);

        tf2W1.SetFocus ();
        Assert.True (Application.RaiseKeyDownEvent (Key.Tab)); // tf2W1 is last subview in win1 - tabbing should take us to first subview of win1
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tf1W1, top.MostFocused);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorRight)); // move char to right in tf1W1. We're at last char so nav to next view
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tvW1, top.MostFocused);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown)); // move down to next view (tvW1)
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tvW1, top.MostFocused);
#if UNIX_KEY_BINDINGS
        Assert.True (Application.OnKeyDown (new (Key.I.WithCtrl)));
        Assert.Equal (win1, top.GetFocused ());
        Assert.Equal (tf2W1, top.MostFocused);
#endif
        Assert.True (Application.RaiseKeyDownEvent (Key.Tab.WithShift)); // Ignored. TextView eats shift-tab by default
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tvW1, top.MostFocused);
        tvW1.AllowsTab = false;
        Assert.True (Application.RaiseKeyDownEvent (Key.Tab.WithShift));
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tf1W1, top.MostFocused);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tf2W1, top.MostFocused);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tvW1, top.MostFocused);

        // nav to win2
        Assert.True (Application.RaiseKeyDownEvent (Key.F6));
        Assert.Equal (win2, top.Focused);
        Assert.Equal (tf1W2, top.MostFocused);
        Assert.True (Application.RaiseKeyDownEvent (Key.F6.WithShift));
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tf2W1, top.MostFocused);
        Assert.True (Application.RaiseKeyDownEvent (Application.NextTabGroupKey));
        Assert.Equal (win2, top.Focused);
        Assert.Equal (tf1W2, top.MostFocused);
        Assert.True (Application.RaiseKeyDownEvent (Application.PrevTabGroupKey));
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tf2W1, top.MostFocused);
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorUp));
        Assert.Equal (win1, top.Focused);
        Assert.Equal (tvW1, top.MostFocused);

        top.Dispose ();
    }

    [Fact]
    public void Added_Event_Should_Not_Be_Used_To_Initialize_Toplevel_Events ()
    {
        var wasAdded = false;

        var view = new View ();
        view.Added += View_Added;

        void View_Added (object sender, SuperViewChangedEventArgs e)
        {
            Assert.False (wasAdded);
            wasAdded = true;
            view.Added -= View_Added;
        }

        var win = new Window ();
        win.Add (view);
        Application.Init (new FakeDriver ());
        Toplevel top = new ();
        top.Add (win);

        Assert.True (wasAdded);

        Application.Shutdown ();
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
                                         Assert.Null (Application.MouseGrabView);

                                         // Grab the mouse
                                         Application.RaiseMouseEvent (new () { ScreenPosition = new (3, 2), Flags = MouseFlags.Button1Pressed });

                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabView);
                                         Assert.Equal (new (2, 2, 10, 3), Application.Top.Frame);
                                     }
                                     else if (iterations == 3)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabView);

                                         // Drag to left
                                         Application.RaiseMouseEvent (
                                                                   new ()
                                                                   {
                                                                       ScreenPosition = new (2, 2), Flags = MouseFlags.Button1Pressed
                                                                                                      | MouseFlags.ReportMousePosition
                                                                   });
                                         Application.Refresh ();

                                         Assert.Equal (Application.Top.Border, Application.MouseGrabView);
                                         Assert.Equal (new (1, 2, 10, 3), Application.Top.Frame);
                                     }
                                     else if (iterations == 4)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabView);
                                         Assert.Equal (new (1, 2), Application.Top.Frame.Location);

                                         Assert.Equal (Application.Top.Border, Application.MouseGrabView);
                                     }
                                     else if (iterations == 5)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabView);

                                         // Drag up
                                         Application.RaiseMouseEvent (
                                                                   new ()
                                                                   {
                                                                       ScreenPosition = new (2, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                                                   });
                                         Application.Refresh ();

                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabView);
                                         Assert.Equal (new (1, 1, 10, 3), Application.Top.Frame);
                                     }
                                     else if (iterations == 6)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabView);
                                         Assert.Equal (new (1, 1), Application.Top.Frame.Location);

                                         Assert.Equal (Application.Top.Border, Application.MouseGrabView);
                                         Assert.Equal (new (1, 1, 10, 3), Application.Top.Frame);
                                     }
                                     else if (iterations == 7)
                                     {
                                         Assert.Equal (Application.Top!.Border, Application.MouseGrabView);

                                         // Ungrab the mouse
                                         Application.RaiseMouseEvent (new () { ScreenPosition = new (2, 1), Flags = MouseFlags.Button1Released });
                                         Application.Refresh ();

                                         Assert.Null (Application.MouseGrabView);
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

                                         Assert.Null (Application.MouseGrabView);

                                         // Grab the mouse
                                         Application.RaiseMouseEvent (
                                                                   new ()
                                                                   {
                                                                       ScreenPosition = new (win.Frame.X, win.Frame.Y), Flags = MouseFlags.Button1Pressed
                                                                   });

                                         Assert.Equal (win.Border, Application.MouseGrabView);
                                     }
                                     else if (iterations == 2)
                                     {
                                         Assert.Equal (win.Border, Application.MouseGrabView);

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

                                         Assert.Equal (win.Border, Application.MouseGrabView);
                                     }
                                     else if (iterations == 3)
                                     {
                                         // we should have moved +1, +0
                                         Assert.Equal (win.Border, Application.MouseGrabView);
                                         Assert.Equal (win.Border, Application.MouseGrabView);
                                         location.Offset (movex, movey);
                                     }
                                     else if (iterations == 4)
                                     {
                                         Assert.Equal (win.Border, Application.MouseGrabView);

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

                                         Assert.Equal (win.Border, Application.MouseGrabView);
                                     }
                                     else if (iterations == 5)
                                     {
                                         // we should have moved +0, -1
                                         Assert.Equal (win.Border, Application.MouseGrabView);
                                         location.Offset (movex, movey);
                                         Assert.Equal (location, win.Frame);
                                     }
                                     else if (iterations == 6)
                                     {
                                         Assert.Equal (win.Border, Application.MouseGrabView);

                                         // Ungrab the mouse
                                         movex = 0;
                                         movey = 0;

                                         Application.RaiseMouseEvent (
                                                                   new ()
                                                                   {
                                                                       ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey),
                                                                       Flags = MouseFlags.Button1Released
                                                                   });

                                         Assert.Null (Application.MouseGrabView);
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

        view.LayoutStarted += ViewLayoutStarted;

        void ViewLayoutStarted (object sender, LayoutEventArgs e)
        {
            Assert.Equal (new (0, 0, 20, 10), view._needsDisplayRect);
            view.LayoutStarted -= ViewLayoutStarted;
        }

        Application.Begin (top);

        Assert.True (top.IsLoaded);
        Assert.True (subTop.IsLoaded);
        Assert.Equal (new (0, 0, 20, 10), view.Frame);

        view.Frame = new (1, 3, 10, 5);
        Assert.Equal (new (1, 3, 10, 5), view.Frame);
        Assert.Equal (new (0, 0, 10, 5), view._needsDisplayRect);

        view.OnDrawContent (view.Viewport);
        view.Frame = new (1, 3, 10, 5);
        Assert.Equal (new (1, 3, 10, 5), view.Frame);
        Assert.Equal (new (0, 0, 10, 5), view._needsDisplayRect);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Toplevel_Inside_ScrollView_MouseGrabView ()
    {
        var scrollView = new ScrollView
        {
            X = 3,
            Y = 3,
            Width = 40,
            Height = 16
        };
        scrollView.SetContentSize (new (200, 100));
        var win = new Window { X = 3, Y = 3, Width = Dim.Fill (3), Height = Dim.Fill (3), Arrangement = ViewArrangement.Movable };
        scrollView.Add (win);
        Toplevel top = new ();
        top.Add (scrollView);
        Application.Begin (top);

        Assert.Equal (new (0, 0, 80, 25), top.Frame);
        Assert.Equal (new (3, 3, 40, 16), scrollView.Frame);
        Assert.Equal (new (0, 0, 200, 100), scrollView.Subviews [0].Frame);
        Assert.Equal (new (3, 3, 194, 94), win.Frame);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (6, 6), Flags = MouseFlags.Button1Pressed });
        Assert.Equal (win.Border, Application.MouseGrabView);
        Assert.Equal (new (3, 3, 194, 94), win.Frame);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (9, 9), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition });
        Assert.Equal (win.Border, Application.MouseGrabView);
        top.SetNeedsLayout ();
        top.LayoutSubviews ();
        Assert.Equal (new (6, 6, 191, 91), win.Frame);
        Application.Refresh ();

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (5, 5), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                  });
        Assert.Equal (win.Border, Application.MouseGrabView);
        top.SetNeedsLayout ();
        top.LayoutSubviews ();
        Assert.Equal (new (2, 2, 195, 95), win.Frame);
        Application.Refresh ();

        Application.RaiseMouseEvent (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.Button1Released });

        // ScrollView always grab the mouse when the container's subview OnMouseEnter don't want grab the mouse
        Assert.Equal (scrollView, Application.MouseGrabView);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (4, 4), Flags = MouseFlags.ReportMousePosition });
        Assert.Equal (scrollView, Application.MouseGrabView);
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
        Application.Refresh ();
        Assert.Equal (new (0, 0, 40, 10), top.Frame);
        Assert.Equal (new (0, 0, 20, 3), window.Frame);

        Assert.Null (Application.MouseGrabView);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });

        Assert.Equal (window.Border, Application.MouseGrabView);

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (-11, -4), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                  });

        Application.Refresh ();
        Assert.Equal (new (0, 0, 40, 10), top.Frame);
        Assert.Equal (new (-11, -4, 20, 3), window.Frame);

        // Changes Top size to same size as Dialog more menu and scroll bar
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 3);

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (-1, -1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                  });

        Application.Refresh ();
        Assert.Equal (new (0, 0, 20, 3), top.Frame);
        Assert.Equal (new (-1, -1, 20, 3), window.Frame);

        // Changes Top size smaller than Dialog size
        ((FakeDriver)Application.Driver!).SetBufferSize (19, 2);

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (-1, -1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                  });

        Application.Refresh ();
        Assert.Equal (new (0, 0, 19, 2), top.Frame);
        Assert.Equal (new (-1, -1, 20, 3), window.Frame);

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (18, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                  });

        Application.Refresh ();
        Assert.Equal (new (0, 0, 19, 2), top.Frame);
        Assert.Equal (new (18, 1, 20, 3), window.Frame);

        // On a real app we can't go beyond the SuperView bounds
        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (19, 2), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                  });

        Application.Refresh ();
        Assert.Equal (new (0, 0, 19, 2), top.Frame);
        Assert.Equal (new (19, 2, 20, 3), window.Frame);
        //TestHelpers.AssertDriverContentsWithFrameAre (@"", output);

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

        Assert.Null (Application.MouseGrabView);
        Assert.Equal (new (0, 0, 10, 3), window.Frame);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });

        var firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);
        Assert.Equal (window.Border, Application.MouseGrabView);

        Assert.Equal (new (0, 0, 10, 3), window.Frame);

        Application.RaiseMouseEvent (
                                  new ()
                                  {
                                      ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                  });

        firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);
        Assert.Equal (window.Border, Application.MouseGrabView);
        Assert.Equal (new (1, 1, 10, 3), window.Frame);

        Application.End (rs);
        window.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Begin_With_Window_Sets_Size_Correctly ()
    {
        Toplevel top = new ();
        RunState rsTop = Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 20);

        var testWindow = new Window { X = 2, Y = 1, Width = 15, Height = 10 };
        Assert.Equal (new (2, 1, 15, 10), testWindow.Frame);

        RunState rsTestWindow = Application.Begin (testWindow);
        Assert.Equal (new (2, 1, 15, 10), testWindow.Frame);

        Application.End (rsTestWindow);
        Application.End (rsTop);
        top.Dispose ();
    }

    // Don't use Dialog as a Top, use a Window instead - dialog has complex layout behavior that is not needed here.
    [Fact]
    [AutoInitShutdown]
    public void Draw_A_Top_Subview_On_A_Window ()
    {
        // Override CM
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Toplevel top = new ();
        var win = new Window ();
        top.Add (win);
        RunState rsTop = Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 20);

        Assert.Equal (new (0, 0, 20, 20), win.Frame);

        var btnPopup = new Button { Text = "Popup" };
        var testWindow = new Window { X = 2, Y = 1, Width = 15, Height = 10 };
        testWindow.Add (btnPopup);

        btnPopup.Accepting += (s, e) =>
                           {
                               Rectangle viewToScreen = btnPopup.ViewportToScreen (top.Frame);

                               var viewAddedToTop = new View
                               {
                                   Text = "viewAddedToTop",
                                   X = 1,
                                   Y = viewToScreen.Y + 1,
                                   Width = 18,
                                   Height = 16,
                                   BorderStyle = LineStyle.Single
                               };
                               Assert.Equal (testWindow, Application.Top);
                               Application.Top!.DrawContentComplete += OnDrawContentComplete;
                               top.Add (viewAddedToTop);

                               void OnDrawContentComplete (object sender, DrawEventArgs e)
                               {
                                   Assert.Equal (new (1, 3, 18, 16), viewAddedToTop.Frame);

                                   viewAddedToTop.SetNeedsDisplay ();
                                   viewAddedToTop.Draw ();
                                   top.Move (2, 15);
                                   View.Driver.AddStr ("One");
                                   top.Move (2, 16);
                                   View.Driver.AddStr ("Two");
                                   top.Move (2, 17);
                                   View.Driver.AddStr ("Three");

                                   Application.Top!.DrawContentComplete -= OnDrawContentComplete;
                               }
                           };
        RunState rsTestWindow = Application.Begin (testWindow);

        Assert.Equal (new (2, 1, 15, 10), testWindow.Frame);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (5, 2), Flags = MouseFlags.Button1Clicked });

        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @$"
┌──────────────────┐
│ ┌─────────────┐  │
│ │{CM.Glyphs.LeftBracket} Popup {CM.Glyphs.RightBracket}    │  │
│┌────────────────┐│
││viewAddedToTop  ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││One             ││
││Two             ││
││Three           ││
│└────────────────┘│
└──────────────────┘",
                                                      output
                                                     );

        Application.End (rsTestWindow);
        Application.End (rsTop);
        top.Dispose ();
    }

    private void OnDrawContentComplete (object sender, DrawEventArgs e) { throw new NotImplementedException (); }

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
    [TestRespondersDisposed]
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
