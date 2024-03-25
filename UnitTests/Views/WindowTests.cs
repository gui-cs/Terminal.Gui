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
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);

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
        Toplevel top = new ();
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
        using var defaultWindow = new Window ();
        Assert.NotNull (defaultWindow);
        Assert.Equal (string.Empty, defaultWindow.Title);

        // Toplevels have Width/Height set to Dim.Fill
        Assert.Equal (LayoutStyle.Computed, defaultWindow.LayoutStyle);

        // If there's no SuperView, Top, or Driver, the default Fill width is int.MaxValue
        Assert.Equal ($"Window(){defaultWindow.Frame}", defaultWindow.ToString ());
        Assert.True (defaultWindow.CanFocus);
        Assert.False (defaultWindow.HasFocus);
        Assert.Equal (new Rectangle (0, 0, 2147483645, 2147483645), defaultWindow.Bounds);
        Assert.Equal (new Rectangle (0, 0, 2147483647, 2147483647), defaultWindow.Frame);
        Assert.Null (defaultWindow.Focused);
        Assert.NotNull (defaultWindow.ColorScheme);
        Assert.Equal (0, defaultWindow.X);
        Assert.Equal (0, defaultWindow.Y);
        Assert.Equal (Dim.Fill (), defaultWindow.Width);
        Assert.Equal (Dim.Fill (), defaultWindow.Height);
        Assert.False (defaultWindow.IsCurrentTop);
        Assert.Empty (defaultWindow.Id);
        Assert.False (defaultWindow.WantContinuousButtonPressed);
        Assert.False (defaultWindow.WantMousePositionReports);
        Assert.Null (defaultWindow.SuperView);
        Assert.Null (defaultWindow.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, defaultWindow.TextDirection);

        // Empty Rect
        using var windowWithFrameRectEmpty = new Window { Frame = Rectangle.Empty, Title = "title" };
        Assert.NotNull (windowWithFrameRectEmpty);
        Assert.Equal ("title", windowWithFrameRectEmpty.Title);
        Assert.Equal (LayoutStyle.Absolute, windowWithFrameRectEmpty.LayoutStyle);
        Assert.Equal ("title", windowWithFrameRectEmpty.Title);
        Assert.Equal (LayoutStyle.Absolute, windowWithFrameRectEmpty.LayoutStyle);
        // TODO: Fix things so that this works in release and debug
        // BUG: This also looks like it might be unintended behavior.
        // Can actually also be removed, since the tests below make it redundant.
    #if DEBUG
        Assert.Equal ($"Window(title){windowWithFrameRectEmpty.Frame}", windowWithFrameRectEmpty.ToString ());
    #else
        Assert.Equal ($"Window(){windowWithFrameRectEmpty.Frame}", windowWithFrameRectEmpty.ToString ());
    #endif
        Assert.True (windowWithFrameRectEmpty.CanFocus);
        Assert.False (windowWithFrameRectEmpty.HasFocus);
        Assert.Equal (Rectangle.Empty, windowWithFrameRectEmpty.Bounds);
        Assert.Equal (Rectangle.Empty, windowWithFrameRectEmpty.Frame);
        Assert.Null (windowWithFrameRectEmpty.Focused);
        Assert.NotNull (windowWithFrameRectEmpty.ColorScheme);
        Assert.Equal (0, windowWithFrameRectEmpty.X);
        Assert.Equal (0, windowWithFrameRectEmpty.Y);
        Assert.Equal (0, windowWithFrameRectEmpty.Width);
        Assert.Equal (0, windowWithFrameRectEmpty.Height);
        Assert.False (windowWithFrameRectEmpty.IsCurrentTop);
    #if DEBUG
        Assert.Equal (windowWithFrameRectEmpty.Title, windowWithFrameRectEmpty.Id);
    #endif
        Assert.False (windowWithFrameRectEmpty.WantContinuousButtonPressed);
        Assert.False (windowWithFrameRectEmpty.WantMousePositionReports);
        Assert.Null (windowWithFrameRectEmpty.SuperView);
        Assert.Null (windowWithFrameRectEmpty.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, windowWithFrameRectEmpty.TextDirection);

        // Rect with values
        using var windowWithFrame1234 = new Window ( );
        windowWithFrame1234.Frame = new  (1, 2, 3, 4);
        windowWithFrame1234.Title = "title";
        Assert.Equal ("title", windowWithFrame1234.Title);
        Assert.NotNull (windowWithFrame1234);
        Assert.Equal (LayoutStyle.Absolute, windowWithFrame1234.LayoutStyle);
        Assert.Equal (LayoutStyle.Absolute, windowWithFrame1234.LayoutStyle);
    #if DEBUG
        Assert.Equal ($"Window(title){windowWithFrame1234.Frame}", windowWithFrame1234.ToString ());
    #else
        Assert.Equal ($"Window(){windowWithFrame1234.Frame}", windowWithFrame1234.ToString ());
    #endif
        Assert.True (windowWithFrame1234.CanFocus);
        Assert.False (windowWithFrame1234.HasFocus);
        Assert.Equal (new (0, 0, 1, 2), windowWithFrame1234.Bounds);
        Assert.Equal (new (1, 2, 3, 4), windowWithFrame1234.Frame);
        Assert.Null (windowWithFrame1234.Focused);
        Assert.NotNull (windowWithFrame1234.ColorScheme);
        Assert.Equal (1, windowWithFrame1234.X);
        Assert.Equal (2, windowWithFrame1234.Y);
        Assert.Equal (3, windowWithFrame1234.Width);
        Assert.Equal (4, windowWithFrame1234.Height);
        Assert.False (windowWithFrame1234.IsCurrentTop);
    #if DEBUG
        Assert.Equal (windowWithFrame1234.Title, windowWithFrame1234.Id);
    #endif
        Assert.False (windowWithFrame1234.WantContinuousButtonPressed);
        Assert.False (windowWithFrame1234.WantMousePositionReports);
        Assert.Null (windowWithFrame1234.SuperView);
        Assert.Null (windowWithFrame1234.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, windowWithFrame1234.TextDirection);
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
