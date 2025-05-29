using UnitTests;
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

        view.Border!.Thickness = new (0, 1, 0, 0);
        view.Border.LineStyle = LineStyle.Single;

        view.SetScheme (new ()
        {
            Normal = new (Color.Red, Color.Green),
            Focus = new (Color.Green, Color.Red)
        });
        Assert.NotEqual (view.GetScheme ().Normal.Foreground, view.GetScheme ().Focus.Foreground);
        Assert.Equal (ColorName16.Red, view.Border.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Green, view.Border.GetAttributeForRole (VisualRole.Focus).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (view.GetAttributeForRole (VisualRole.Focus), view.Border.GetAttributeForRole (VisualRole.Focus));

        superView.BeginInit ();
        superView.EndInit ();
        superView.Draw ();

        var expected = @"─┤A├─";
        DriverAssert.AssertDriverContentsAre (expected, output);
        DriverAssert.AssertDriverAttributesAre ("00000", output, null, view.GetScheme ().Normal);

        view.CanFocus = true;
        view.SetFocus ();
        View.SetClipToScreen ();
        view.Draw ();
        Assert.Equal (view.GetAttributeForRole (VisualRole.Focus), view.Border.GetAttributeForRole (VisualRole.Focus));
        Assert.Equal (view.GetScheme ().Focus.Foreground, view.Border.GetAttributeForRole (VisualRole.Focus).Foreground);
        Assert.Equal (view.GetScheme ().Normal.Foreground, view.Border.GetAttributeForRole (VisualRole.Normal).Foreground);
        DriverAssert.AssertDriverAttributesAre ("00100", output, null, view.GetScheme ().Normal, view.GetAttributeForRole (VisualRole.Focus));
    }

    [Fact]
    [SetupFakeDriver]
    public void Border_Uses_Parent_Scheme ()
    {
        var view = new View { Title = "A", Height = 2, Width = 5 };
        view.Border.Thickness = new (0, 1, 0, 0);
        view.Border.LineStyle = LineStyle.Single;

        view.SetScheme (new ()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        });
        Assert.Equal (ColorName16.Red, view.Border.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Green, view.Border.GetAttributeForRole (VisualRole.Focus).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (view.GetAttributeForRole (VisualRole.Normal), view.Border.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (view.GetAttributeForRole (VisualRole.Focus), view.Border.GetAttributeForRole (VisualRole.Focus));

        view.BeginInit ();
        view.EndInit ();
        view.Draw ();

        var expected = @"─┤A├─";
        DriverAssert.AssertDriverContentsAre (expected, output);
        DriverAssert.AssertDriverAttributesAre ("00000", output, null, view.GetScheme ().Normal);
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
        Application.RunIteration (ref rs, firstIteration);
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

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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

        ((FakeDriver)Application.Driver!).SetBufferSize (width, 4);
        Application.RunIteration (ref rs, false);
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

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        Application.RunIteration (ref rs, firstIteration);
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

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        Application.RunIteration (ref rs, firstIteration);
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

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        Application.RunIteration (ref rs, firstIteration);
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

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        superSuper.Layout ();

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
        super.Layout ();

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

        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single};

        top.Add (frame);
        RunState rs = Application.Begin (top);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (5, 5);
        Application.RunIteration (ref rs, firstIteration);

        var expected = @"
╔═══╗
║┌─┐║
║│ │║
║└─┘║
╚═══╝";

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HasSuperView_Title ()
    {
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.Double;

        var frame = new FrameView { Title = "1234", Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };

        top.Add (frame);
        RunState rs = Application.Begin (top);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (10, 4);
        Application.RunIteration (ref rs, firstIteration);

        var expected = @"
╔════════╗
║┌┤1234├┐║
║└──────┘║
╚════════╝";

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        Application.RunIteration (ref rs, firstIteration);

        var expected = @"
┌─┐
│ │
└─┘";

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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

    [Theory]
    [InlineData (false, @"
┌───┐
│ ║ │
│═┌┄│
│ ┊ │
└───┘")]
    [InlineData (true, @"
╔═╦─┐
║ ║ │
╠═╬┄┤
│ ┊ ┊
└─┴┄┘")]
    [SetupFakeDriver]
    public void SuperViewRendersLineCanvas_No_SubViews_AutoJoinsLines (bool superViewRendersLineCanvas, string expected)
    {
        View superView = new View ()
        {
            Id = "superView",
            Width = 5,
            Height = 5,
            BorderStyle = LineStyle.Single
        };

        View view1 = new View ()
        {
            Id = "view1",
            Width = 3,
            Height = 3,
            X = -1,
            Y = -1,
            BorderStyle = LineStyle.Double,
            SuperViewRendersLineCanvas = superViewRendersLineCanvas
        };

        View view2 = new View ()
        {
            Id = "view2",
            Width = 3,
            Height = 3,
            X = 1,
            Y = 1,
            BorderStyle = LineStyle.Dotted,
            SuperViewRendersLineCanvas = superViewRendersLineCanvas
        };

        superView.Add (view1, view2);

        superView.BeginInit ();
        superView.EndInit ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre (expected, output);
    }


    [Theory]
    [InlineData (false, @"
┌┤A├──────┐
│    ║    │
│    ║    │
│════┌┤C├┄│
│    ┊    │
│    ┊    │
└─────────┘")]
    [InlineData (true, @"
╔╡A╞═╦────┐
║    ║    │
║    ║    │
╠════╬┤C├┄┤
│    ┊    ┊
│    ┊    ┊
└────┴┄┄┄┄┘")]
    [SetupFakeDriver]
    public void SuperViewRendersLineCanvas_Title_AutoJoinsLines (bool superViewRendersLineCanvas, string expected)
    {
        View superView = new View ()
        {
            Id = "superView",
            Title = "A",
            Width = 11,
            Height = 7,
            CanFocus = true,
            BorderStyle = LineStyle.Single,
        };

        View view1 = new View ()
        {
            Id = "view1",
            Title = "B",
            Width = 6,
            Height = 4,
            X = -1,
            Y = -1,
            CanFocus = true,
            BorderStyle = LineStyle.Double,
            SuperViewRendersLineCanvas = superViewRendersLineCanvas
        };

        View view2 = new View ()
        {
            Id = "view2",
            Title = "C",
            Width = 6,
            Height = 4,
            X = 4,
            Y = 2,
            CanFocus = true,
            BorderStyle = LineStyle.Dotted,
            SuperViewRendersLineCanvas = superViewRendersLineCanvas
        };

        superView.Add (view1, view2);

        superView.BeginInit ();
        superView.EndInit ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre (expected, output);
    }
}
