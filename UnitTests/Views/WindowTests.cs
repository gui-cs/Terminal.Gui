using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class WindowTests
{
    private readonly ITestOutputHelper _output;
    public WindowTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void Activating_MenuBar_By_Alt_Key_Does_Not_Throw ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem ("Child", new MenuItem [] { new ("_Create Child", "", null) })
            ]
        };
        var win = new Window ();
        win.Add (menu);
        Application.Top.Add (win);
        Application.Begin (Application.Top);

        Exception exception = Record.Exception (() => win.NewKeyDownEvent (KeyCode.AltMask));
        Assert.Null (exception);
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBar_And_StatusBar_Inside_Window ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem ("File", new MenuItem [] { new ("Open", "", null), new ("Quit", "", null) }),
                new MenuBarItem (
                                 "Edit",
                                 new MenuItem [] { new ("Copy", "", null) }
                                )
            ]
        };

        var sb = new StatusBar (
                                new StatusItem []
                                {
                                    new ((KeyCode)Key.Q.WithCtrl, "~^Q~ Quit", null),
                                    new ((KeyCode)Key.O.WithCtrl, "~^O~ Open", null),
                                    new ((KeyCode)Key.C.WithCtrl, "~^C~ Copy", null)
                                }
                               );

        var fv = new FrameView { Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1), Title = "Frame View" };
        var win = new Window ();
        win.Add (menu, sb, fv);
        Toplevel top = Application.Top;
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (20, 10);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│ File  Edit       │
│┌┤Frame View├────┐│
││                ││
││                ││
││                ││
││                ││
│└────────────────┘│
│ ^Q Quit │ ^O Open│
└──────────────────┘",
                                                      _output
                                                     );

        ((FakeDriver)Application.Driver).SetBufferSize (40, 20);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌┤Frame View├────────────────────────┐│
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
│└────────────────────────────────────┘│
│ ^Q Quit │ ^O Open │ ^C Copy          │
└──────────────────────────────────────┘",
                                                      _output
                                                     );

        ((FakeDriver)Application.Driver).SetBufferSize (20, 10);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│ File  Edit       │
│┌┤Frame View├────┐│
││                ││
││                ││
││                ││
││                ││
│└────────────────┘│
│ ^Q Quit │ ^O Open│
└──────────────────┘",
                                                      _output
                                                     );
    }

    [Fact]
    public void New_Initializes ()
    {
        // Parameterless
        var r = new Window ();
        Assert.NotNull (r);
        Assert.Equal (string.Empty, r.Title);

        // Toplevels have Width/Height set to Dim.Fill
        Assert.Equal (LayoutStyle.Computed, r.LayoutStyle);

        // If there's no SuperView, Top, or Driver, the default Fill width is int.MaxValue
        Assert.Equal ("Window()(0,0,2147483647,2147483647)", r.ToString ());
        Assert.True (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new Rect (0, 0, 2147483645, 2147483645), r.Bounds);
        Assert.Equal (new Rect (0, 0, 2147483647, 2147483647), r.Frame);
        Assert.Null (r.Focused);
        Assert.NotNull (r.ColorScheme);
        Assert.Equal (0, r.X);
        Assert.Equal (0, r.Y);
        Assert.Equal (Dim.Fill (), r.Width);
        Assert.Equal (Dim.Fill (), r.Height);
        Assert.False (r.IsCurrentTop);
        Assert.Empty (r.Id);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

        // Empty Rect
        r = new Window { Frame = Rect.Empty, Title = "title" };
        Assert.NotNull (r);
        Assert.Equal ("title", r.Title);
        Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
        Assert.Equal ("title", r.Title);
        Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
        Assert.Equal ("Window(title)(0,0,0,0)", r.ToString ());
        Assert.True (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
        Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
        Assert.Null (r.Focused);
        Assert.NotNull (r.ColorScheme);
        Assert.Equal (0, r.X);
        Assert.Equal (0, r.Y);
        Assert.Equal (0, r.Width);
        Assert.Equal (0, r.Height);
        Assert.False (r.IsCurrentTop);
        Assert.Equal (r.Title, r.Id);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

        // Rect with values
        r = new Window { Frame = new Rect (1, 2, 3, 4), Title = "title" };
        Assert.Equal ("title", r.Title);
        Assert.NotNull (r);
        Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
        Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
        Assert.Equal ("Window(title)(1,2,3,4)", r.ToString ());
        Assert.True (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new Rect (0, 0, 1, 2), r.Bounds);
        Assert.Equal (new Rect (1, 2, 3, 4), r.Frame);
        Assert.Null (r.Focused);
        Assert.NotNull (r.ColorScheme);
        Assert.Equal (1, r.X);
        Assert.Equal (2, r.Y);
        Assert.Equal (3, r.Width);
        Assert.Equal (4, r.Height);
        Assert.False (r.IsCurrentTop);
        Assert.Equal (r.Title, r.Id);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
        r.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void OnCanFocusChanged_Only_Must_ContentView_Forces_SetFocus_After_IsInitialized_Is_True ()
    {
        var win1 = new Window { Id = "win1", Width = 10, Height = 1 };
        var view1 = new View { Id = "view1", Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
        var win2 = new Window { Id = "win2", Y = 6, Width = 10, Height = 1 };
        var view2 = new View { Id = "view2", Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
        win2.Add (view2);
        win1.Add (view1, win2);

        Application.Begin (win1);

        Assert.True (win1.HasFocus);
        Assert.True (view1.HasFocus);
        Assert.False (win2.HasFocus);
        Assert.False (view2.HasFocus);
    }
}
