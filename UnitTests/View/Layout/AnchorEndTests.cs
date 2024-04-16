using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AnchorEndTests (ITestOutputHelper output)
{
    [Fact]
    public void AnchorEnd_Equal ()
    {
        var n1 = 0;
        var n2 = 0;

        Pos pos1 = Pos.AnchorEnd (n1);
        Pos pos2 = Pos.AnchorEnd (n2);
        Assert.Equal (pos1, pos2);

        // Test inequality
        n2 = 5;
        pos2 = Pos.AnchorEnd (n2);
        Assert.NotEqual (pos1, pos2);
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [AutoInitShutdown]
    public void AnchorEnd_Equal_Inside_Window ()
    {
        var viewWidth = 10;
        var viewHeight = 1;

        var tv = new TextView
        {
            X = Pos.AnchorEnd (viewWidth), Y = Pos.AnchorEnd (viewHeight), Width = viewWidth, Height = viewHeight
        };

        var win = new Window ();

        win.Add (tv);

        Toplevel top = new ();
        top.Add (win);
        RunState rs = Application.Begin (top);

        Assert.Equal (new (0, 0, 80, 25), top.Frame);
        Assert.Equal (new (0, 0, 80, 25), win.Frame);
        Assert.Equal (new (68, 22, 10, 1), tv.Frame);
        Application.End (rs);
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [AutoInitShutdown]
    public void AnchorEnd_Equal_Inside_Window_With_MenuBar_And_StatusBar_On_Toplevel ()
    {
        var viewWidth = 10;
        var viewHeight = 1;

        var tv = new TextView
        {
            X = Pos.AnchorEnd (viewWidth), Y = Pos.AnchorEnd (viewHeight), Width = viewWidth, Height = viewHeight
        };

        var win = new Window ();

        win.Add (tv);

        var menu = new MenuBar ();
        var status = new StatusBar ();
        Toplevel top = new ();
        top.Add (win, menu, status);
        RunState rs = Application.Begin (top);

        Assert.Equal (new (0, 0, 80, 25), top.Frame);
        Assert.Equal (new (0, 0, 80, 1), menu.Frame);
        Assert.Equal (new (0, 24, 80, 1), status.Frame);
        Assert.Equal (new (0, 1, 80, 23), win.Frame);
        Assert.Equal (new (68, 20, 10, 1), tv.Frame);

        Application.End (rs);
    }

    [Fact]
    public void AnchorEnd_Negative_Throws ()
    {
        Pos pos;
        int n = -1;
        Assert.Throws<ArgumentException> (() => pos = Pos.AnchorEnd (n));
    }

    [Fact]
    public void AnchorEnd_SetsValue ()
    {
        var n = 0;
        Pos pos = Pos.AnchorEnd (0);
        Assert.Equal ($"AnchorEnd({n})", pos.ToString ());

        n = 5;
        pos = Pos.AnchorEnd (n);
        Assert.Equal ($"AnchorEnd({n})", pos.ToString ());
    }

    // This test used to be Dialog_In_Window_With_TextField_And_Button_AnchorEnd in DialogTests.
    [Fact]
    [SetupFakeDriver]
    public void AnchorEnd_View_And_Button ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (20, 5);

        var b = $"{CM.Glyphs.LeftBracket} Ok {CM.Glyphs.RightBracket}";

        var frame = new FrameView { Width = 18, Height = 3 };
        Assert.Equal (16, frame.Viewport.Width);

        Button btn = null;

        int Btn_Width () { return btn?.Viewport.Width ?? 0; }

        btn = new() { Text = "Ok", X = Pos.AnchorEnd (0) - Pos.Function (Btn_Width) };

        var view = new View
        {
            Text = "0123456789abcdefghij",

            // Dim.Fill (1) fills remaining space minus 1 (16 - 1 = 15)
            // Dim.Function (Btn_Width) is 6
            // Width should be 15 - 6 = 9
            Width = Dim.Fill (1) - Dim.Function (Btn_Width),
            Height = 1
        };

        frame.Add (btn, view);
        frame.BeginInit ();
        frame.EndInit ();
        frame.Draw ();

        Assert.Equal (6, btn.Viewport.Width);
        Assert.Equal (10, btn.Frame.X); // frame.Viewport.Width (16) - btn.Frame.Width (6) = 10
        Assert.Equal (0, btn.Frame.Y);
        Assert.Equal (6, btn.Frame.Width);
        Assert.Equal (1, btn.Frame.Height);

        Assert.Equal (9, view.Viewport.Width); // frame.Viewport.Width (16) - Dim.Fill (1) - Dim.Function (6) = 9
        Assert.Equal (0, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (9, view.Frame.Width);
        Assert.Equal (1, view.Frame.Height);

        var expected = $@"
┌────────────────┐
│012345678 {
    b
}│
└────────────────┘
";
        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }
}
