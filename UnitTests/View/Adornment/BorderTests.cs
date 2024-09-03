using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class BorderTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeDriver]
    public void Border_Parent_HasFocus_Title_Uses_FocusAttribute ()
    {
        var superView = new View { Width = 10, Height = 10, CanFocus = true };
        var otherView = new View { Width = 0, Height = 0, CanFocus = true };
        superView.Add (otherView);

        var view = new View { Title = "A", Height = 2, Width = 5 };
        superView.Add (view);

        view.Border.Thickness = new (0, 1, 0, 0);
        view.Border.LineStyle = LineStyle.Single;

        view.ColorScheme = new ()
        {
            Normal = new (Color.Red, Color.Green),
            Focus = new (Color.Green, Color.Red)
        };
        Assert.NotEqual (view.ColorScheme.Normal.Foreground, view.ColorScheme.Focus.Foreground);
        Assert.Equal (ColorName.Red, view.Border.GetNormalColor ().Foreground.GetClosestNamedColor ());
        Assert.Equal (ColorName.Green, view.Border.GetFocusColor ().Foreground.GetClosestNamedColor ());
        Assert.Equal (view.GetFocusColor (), view.Border.GetFocusColor ());

        superView.BeginInit ();
        superView.EndInit ();
        superView.Draw ();

        var expected = @"─┤A├─";
        TestHelpers.AssertDriverContentsAre (expected, output);
        TestHelpers.AssertDriverAttributesAre ("00000", null, view.ColorScheme.Normal);

        view.CanFocus = true;
        view.SetFocus ();
        view.Draw ();
        Assert.Equal (view.GetFocusColor (), view.Border.GetFocusColor ());
        Assert.Equal (view.ColorScheme.Focus.Foreground, view.Border.GetFocusColor ().Foreground);
        Assert.Equal (view.ColorScheme.Normal.Foreground, view.Border.GetNormalColor ().Foreground);
        TestHelpers.AssertDriverAttributesAre ("00100", null, view.ColorScheme.Normal, view.GetFocusColor ());
    }

    [Fact]
    [SetupFakeDriver]
    public void Border_Uses_Parent_ColorScheme ()
    {
        var view = new View { Title = "A", Height = 2, Width = 5 };
        view.Border.Thickness = new (0, 1, 0, 0);
        view.Border.LineStyle = LineStyle.Single;

        view.ColorScheme = new ()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        };
        Assert.Equal (ColorName.Red, view.Border.GetNormalColor ().Foreground.GetClosestNamedColor ());
        Assert.Equal (ColorName.Green, view.Border.GetFocusColor ().Foreground.GetClosestNamedColor ());
        Assert.Equal (view.GetNormalColor (), view.Border.GetNormalColor ());
        Assert.Equal (view.GetFocusColor (), view.Border.GetFocusColor ());

        view.BeginInit ();
        view.EndInit ();
        view.Draw ();

        var expected = @"─┤A├─";
        TestHelpers.AssertDriverContentsAre (expected, output);
        TestHelpers.AssertDriverAttributesAre ("00000", null, view.ColorScheme.Normal);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void Border_With_Title_Border_Double_Thickness_Top_Four_Size_Width (int width)
    {
        var win = new Window
        {
            Title = "1234", Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Double
        };
        win.Border.Thickness = win.Border.Thickness with { Top = 4 };

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (width, 5);
        Application.RunIteration (ref rs, ref firstIteration);
        var expected = string.Empty;

        switch (width)
        {
            case 1:
                Assert.Equal (new (0, 0, 1, 5), win.Frame);

                expected = @"
║
║
║";

                break;
            case 2:
                Assert.Equal (new (0, 0, 2, 5), win.Frame);

                expected = @"
╔╗
║║
╚╝";

                break;
            case 3:
                Assert.Equal (new (0, 0, 3, 5), win.Frame);

                expected = @"
╔═╗
║ ║
╚═╝";

                break;
            case 4:
                Assert.Equal (new (0, 0, 4, 5), win.Frame);

                expected = @"
 ╒╕ 
╔╡╞╗
║╘╛║
╚══╝";

                break;
            case 5:
                Assert.Equal (new (0, 0, 5, 5), win.Frame);

                expected = @"
 ╒═╕ 
╔╡1╞╗
║╘═╛║
╚═══╝";

                break;
            case 6:
                Assert.Equal (new (0, 0, 6, 5), win.Frame);

                expected = @"
 ╒══╕ 
╔╡12╞╗
║╘══╛║
╚════╝";

                break;
            case 7:
                Assert.Equal (new (0, 0, 7, 5), win.Frame);

                expected = @"
 ╒═══╕ 
╔╡123╞╗
║╘═══╛║
╚═════╝";

                break;
            case 8:
                Assert.Equal (new (0, 0, 8, 5), win.Frame);

                expected = @"
 ╒════╕ 
╔╡1234╞╗
║╘════╛║
╚══════╝";

                break;
            case 9:
                Assert.Equal (new (0, 0, 9, 5), win.Frame);

                expected = @"
 ╒════╕  
╔╡1234╞═╗
║╘════╛ ║
╚═══════╝";

                break;
            case 10:
                Assert.Equal (new (0, 0, 10, 5), win.Frame);

                expected = @"
 ╒════╕   
╔╡1234╞══╗
║╘════╛  ║
╚════════╝";

                break;
        }

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        win.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void Border_With_Title_Border_Double_Thickness_Top_Three_Size_Width (int width)
    {
        var win = new Window
        {
            Title = "1234", Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Double
        };
        win.Border.Thickness = win.Border.Thickness with { Top = 3 };

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (width, 4);
        Application.RunIteration (ref rs, ref firstIteration);
        var expected = string.Empty;

        switch (width)
        {
            case 1:
                Assert.Equal (new (0, 0, 1, 4), win.Frame);

                expected = @"
║
║
║";

                break;
            case 2:
                Assert.Equal (new (0, 0, 2, 4), win.Frame);

                expected = @"
╔╗
║║
╚╝";

                break;
            case 3:
                Assert.Equal (new (0, 0, 3, 4), win.Frame);

                expected = @"
╔═╗
║ ║
╚═╝";

                break;
            case 4:
                Assert.Equal (new (0, 0, 4, 4), win.Frame);

                expected = @"
 ╒╕ 
╔╡╞╗
║╘╛║
╚══╝";

                break;
            case 5:
                Assert.Equal (new (0, 0, 5, 4), win.Frame);

                expected = @"
 ╒═╕ 
╔╡1╞╗
║╘═╛║
╚═══╝";

                break;
            case 6:
                Assert.Equal (new (0, 0, 6, 4), win.Frame);

                expected = @"
 ╒══╕ 
╔╡12╞╗
║╘══╛║
╚════╝";

                break;
            case 7:
                Assert.Equal (new (0, 0, 7, 4), win.Frame);

                expected = @"
 ╒═══╕ 
╔╡123╞╗
║╘═══╛║
╚═════╝";

                break;
            case 8:
                Assert.Equal (new (0, 0, 8, 4), win.Frame);

                expected = @"
 ╒════╕ 
╔╡1234╞╗
║╘════╛║
╚══════╝";

                break;
            case 9:
                Assert.Equal (new (0, 0, 9, 4), win.Frame);

                expected = @"
 ╒════╕  
╔╡1234╞═╗
║╘════╛ ║
╚═══════╝";

                break;
            case 10:
                Assert.Equal (new (0, 0, 10, 4), win.Frame);

                expected = @"
 ╒════╕   
╔╡1234╞══╗
║╘════╛  ║
╚════════╝";

                break;
        }

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        win.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void Border_With_Title_Border_Double_Thickness_Top_Two_Size_Width (int width)
    {
        var win = new Window
        {
            Title = "1234", Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Double
        };
        win.Border.Thickness = win.Border.Thickness with { Top = 2 };

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (width, 4);
        Application.RunIteration (ref rs, ref firstIteration);
        var expected = string.Empty;

        switch (width)
        {
            case 1:
                Assert.Equal (new (0, 0, 1, 4), win.Frame);

                expected = @"
║
║
║";

                break;
            case 2:
                Assert.Equal (new (0, 0, 2, 4), win.Frame);

                expected = @"
╔╗
║║
╚╝";

                break;
            case 3:
                Assert.Equal (new (0, 0, 3, 4), win.Frame);

                expected = @"
╔═╗
║ ║
╚═╝";

                break;
            case 4:
                Assert.Equal (new (0, 0, 4, 4), win.Frame);

                expected = @"
 ╒╕ 
╔╛╘╗
║  ║
╚══╝";

                break;
            case 5:
                Assert.Equal (new (0, 0, 5, 4), win.Frame);

                expected = @"
 ╒═╕ 
╔╛1╘╗
║   ║
╚═══╝";

                break;
            case 6:
                Assert.Equal (new (0, 0, 6, 4), win.Frame);

                expected = @"
 ╒══╕ 
╔╛12╘╗
║    ║
╚════╝";

                break;
            case 7:
                Assert.Equal (new (0, 0, 7, 4), win.Frame);

                expected = @"
 ╒═══╕ 
╔╛123╘╗
║     ║
╚═════╝";

                break;
            case 8:
                Assert.Equal (new (0, 0, 8, 4), win.Frame);

                expected = @"
 ╒════╕ 
╔╛1234╘╗
║      ║
╚══════╝";

                break;
            case 9:
                Assert.Equal (new (0, 0, 9, 4), win.Frame);

                expected = @"
 ╒════╕  
╔╛1234╘═╗
║       ║
╚═══════╝";

                break;
            case 10:
                Assert.Equal (new (0, 0, 10, 4), win.Frame);

                expected = @"
 ╒════╕   
╔╛1234╘══╗
║        ║
╚════════╝";

                break;
        }

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        win.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    public void Border_With_Title_Size_Height (int height)
    {
        var win = new Window { Title = "1234", Width = Dim.Fill (), Height = Dim.Fill () };

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (20, height);
        Application.RunIteration (ref rs, ref firstIteration);
        var expected = string.Empty;

        switch (height)
        {
            case 0:
                //Assert.Equal (new (0, 0, 17, 0), subview.Frame);
                expected = @"
";

                break;
            case 1:
                //Assert.Equal (new (0, 0, 17, 0), subview.Frame);
                expected = @"
─┤1234├─────────────";

                break;
            case 2:
                //Assert.Equal (new (0, 0, 17, 1), subview.Frame);
                expected = @"
┌┤1234├────────────┐
└──────────────────┘
";

                break;
            case 3:
                //Assert.Equal (new (0, 0, 17, 2), subview.Frame);
                expected = @"
┌┤1234├────────────┐
│                  │
└──────────────────┘
";

                break;
        }

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        win.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void Border_With_Title_Size_Width (int width)
    {
        var win = new Window { Title = "1234", Width = Dim.Fill (), Height = Dim.Fill () };

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (width, 3);
        Application.RunIteration (ref rs, ref firstIteration);
        var expected = string.Empty;

        switch (width)
        {
            case 1:
                //Assert.Equal (new (0, 0, 17, 0), subview.Frame);
                expected = @"
│
│
│";

                break;
            case 2:
                //Assert.Equal (new (0, 0, 17, 1), subview.Frame);
                expected = @"
┌┐
││
└┘";

                break;
            case 3:
                //Assert.Equal (new (0, 0, 17, 2), subview.Frame);
                expected = @"
┌─┐
│ │
└─┘
";

                break;
            case 4:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌┤├┐
│  │
└──┘";

                break;
            case 5:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌┤1├┐
│   │
└───┘";

                break;
            case 6:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌┤12├┐
│    │
└────┘";

                break;
            case 7:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌┤123├┐
│     │
└─────┘";

                break;
            case 8:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌┤1234├┐
│      │
└──────┘";

                break;
            case 9:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌┤1234├─┐
│       │
└───────┘";

                break;
            case 10:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌┤1234├──┐
│        │
└────────┘";

                break;
        }

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        win.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, 0, 2, 2)]
    [InlineData (1, 0, 0, 4, 4)]
    [InlineData (2, 0, 0, 6, 6)]
    [InlineData (1, 1, 0, 5, 4)]
    [InlineData (1, 0, 1, 4, 5)]
    [InlineData (1, 1, 1, 5, 5)]
    [InlineData (1, 10, 10, 14, 14)]
    public void FrameToScreen_NestedSuperView_WithBorder (
        int superOffset,
        int frameX,
        int frameY,
        int expectedScreenX,
        int expectedScreenY
    )
    {
        var superSuper = new View
        {
            X = superOffset,
            Y = superOffset,
            Width = 30,
            Height = 30,
            BorderStyle = LineStyle.Single
        };

        var super = new View
        {
            X = superOffset,
            Y = superOffset,
            Width = 20,
            Height = 20,
            BorderStyle = LineStyle.Single
        };
        superSuper.Add (super);

        var view = new View { X = frameX, Y = frameY, Width = 10, Height = 10 };
        super.Add (view);
        var expected = new Rectangle (expectedScreenX, expectedScreenY, 10, 10);
        Rectangle actual = view.FrameToScreen ();
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (0, 0, 0, 1, 1)]
    [InlineData (1, 0, 0, 2, 2)]
    [InlineData (2, 0, 0, 3, 3)]
    [InlineData (1, 1, 0, 3, 2)]
    [InlineData (1, 0, 1, 2, 3)]
    [InlineData (1, 1, 1, 3, 3)]
    [InlineData (1, 10, 10, 12, 12)]
    public void FrameToScreen_SuperView_WithBorder (
        int superOffset,
        int frameX,
        int frameY,
        int expectedScreenX,
        int expectedScreenY
    )
    {
        var super = new View
        {
            X = superOffset,
            Y = superOffset,
            Width = 20,
            Height = 20,
            BorderStyle = LineStyle.Single
        };

        var view = new View { X = frameX, Y = frameY, Width = 10, Height = 10 };
        super.Add (view);
        var expected = new Rectangle (expectedScreenX, expectedScreenY, 10, 10);
        Rectangle actual = view.FrameToScreen ();
        Assert.Equal (expected, actual);
    }

    [Fact]
    [AutoInitShutdown]
    public void HasSuperView ()
    {
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.Double;

        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

        top.Add (frame);
        RunState rs = Application.Begin (top);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (5, 5);
        Application.RunIteration (ref rs, ref firstIteration);

        var expected = @"
╔═══╗
║┌─┐║
║│ │║
║└─┘║
╚═══╝";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HasSuperView_Title ()
    {
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.Double;

        var frame = new FrameView { Title = "1234", Width = Dim.Fill (), Height = Dim.Fill () };

        top.Add (frame);
        RunState rs = Application.Begin (top);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (10, 4);
        Application.RunIteration (ref rs, ref firstIteration);

        var expected = @"
╔════════╗
║┌┤1234├┐║
║└──────┘║
╚════════╝";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void NoSuperView ()
    {
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (3, 3);
        Application.RunIteration (ref rs, ref firstIteration);

        var expected = @"
┌─┐
│ │
└─┘";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        win.Dispose ();
    }

    [Fact]
    public void View_BorderStyle_Defaults ()
    {
        var view = new View ();
        Assert.Equal (LineStyle.None, view.BorderStyle);
        Assert.Equal (Thickness.Empty, view.Border.Thickness);
        view.Dispose ();
    }

    [Fact]
    public void View_SetBorderStyle ()
    {
        var view = new View ();
        view.BorderStyle = LineStyle.Single;
        Assert.Equal (LineStyle.Single, view.BorderStyle);
        Assert.Equal (new (1), view.Border.Thickness);

        view.BorderStyle = LineStyle.Double;
        Assert.Equal (LineStyle.Double, view.BorderStyle);
        Assert.Equal (new (1), view.Border.Thickness);

        view.BorderStyle = LineStyle.None;
        Assert.Equal (LineStyle.None, view.BorderStyle);
        Assert.Equal (Thickness.Empty, view.Border.Thickness);
        view.Dispose ();
    }
}
