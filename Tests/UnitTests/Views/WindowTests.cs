using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class WindowTests (ITestOutputHelper output)
{
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
        top.Dispose ();
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

        var sb = new StatusBar ();

        var fv = new FrameView { Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1), Title = "Frame View", BorderStyle = LineStyle.Single };
        var win = new Window ();
        win.Add (menu, sb, fv);
        Toplevel top = new ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 10);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│ File  Edit       │
│┌┤Frame View├────┐│
││                ││
││                ││
││                ││
││                ││
│└────────────────┘│
│                  │
└──────────────────┘",
                                                      output
                                                     );

        ((FakeDriver)Application.Driver!).SetBufferSize (40, 20);

        DriverAssert.AssertDriverContentsWithFrameAre (
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
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        ((FakeDriver)Application.Driver!).SetBufferSize (20, 10);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│ File  Edit       │
│┌┤Frame View├────┐│
││                ││
││                ││
││                ││
││                ││
│└────────────────┘│
│                  │
└──────────────────┘",
                                                      output
                                                     );
        top.Dispose ();
    }

    [Fact]
    public void New_Initializes ()
    {
        // Parameterless
        using var defaultWindow = new Window ();
        defaultWindow.Layout ();
        Assert.NotNull (defaultWindow);
        Assert.Equal (string.Empty, defaultWindow.Title);

        // Toplevels have Width/Height set to Dim.Fill

        // If there's no SuperView, Top, or Driver, the default Fill width is int.MaxValue
        Assert.Equal ($"Window(){defaultWindow.Frame}", defaultWindow.ToString ());
        Assert.True (defaultWindow.CanFocus);
        Assert.False (defaultWindow.HasFocus);
        Assert.Equal (new Rectangle (0, 0, Application.Screen.Width - 2, Application.Screen.Height - 2), defaultWindow.Viewport);
        Assert.Equal (new Rectangle (0, 0, Application.Screen.Width, Application.Screen.Height), defaultWindow.Frame);
        Assert.Null (defaultWindow.Focused);
        Assert.NotNull (defaultWindow.GetScheme ());
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

        Assert.Equal (ViewArrangement.Overlapped, defaultWindow.Arrangement);

        // Empty Rect
        using var windowWithFrameRectEmpty = new Window { Frame = Rectangle.Empty, Title = "title" };
        windowWithFrameRectEmpty.Layout ();
        Assert.NotNull (windowWithFrameRectEmpty);
        Assert.Equal ("title", windowWithFrameRectEmpty.Title);
        Assert.True (windowWithFrameRectEmpty.CanFocus);
        Assert.False (windowWithFrameRectEmpty.HasFocus);
        Assert.Null (windowWithFrameRectEmpty.Focused);
        Assert.NotNull (windowWithFrameRectEmpty.GetScheme ());
        Assert.Equal (0, windowWithFrameRectEmpty.X);
        Assert.Equal (0, windowWithFrameRectEmpty.Y);
        Assert.Equal (0, windowWithFrameRectEmpty.Width);
        Assert.Equal (0, windowWithFrameRectEmpty.Height);
        Assert.False (windowWithFrameRectEmpty.IsCurrentTop);
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
        Assert.Equal ($"Window(){windowWithFrame1234.Frame}", windowWithFrame1234.ToString ());
        Assert.True (windowWithFrame1234.CanFocus);
        Assert.False (windowWithFrame1234.HasFocus);
        Assert.Equal (new (0, 0, 1, 2), windowWithFrame1234.Viewport);
        Assert.Equal (new (1, 2, 3, 4), windowWithFrame1234.Frame);
        Assert.Null (windowWithFrame1234.Focused);
        Assert.NotNull (windowWithFrame1234.GetScheme ());
        Assert.Equal (1, windowWithFrame1234.X);
        Assert.Equal (2, windowWithFrame1234.Y);
        Assert.Equal (3, windowWithFrame1234.Width);
        Assert.Equal (4, windowWithFrame1234.Height);
        Assert.False (windowWithFrame1234.IsCurrentTop);
        Assert.False (windowWithFrame1234.WantContinuousButtonPressed);
        Assert.False (windowWithFrame1234.WantMousePositionReports);
        Assert.Null (windowWithFrame1234.SuperView);
        Assert.Null (windowWithFrame1234.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, windowWithFrame1234.TextDirection);
    }
}
