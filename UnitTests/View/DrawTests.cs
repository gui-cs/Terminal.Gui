#nullable enable
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait("Category","Output")]
public class DrawTests
{
    private readonly ITestOutputHelper _output;
    public DrawTests (ITestOutputHelper output) { _output = output; }

    [Theory]
    [InlineData (0, 0, 1, 1)]
    [InlineData (0, 0, 2, 2)]
    [InlineData (-1, -1, 2, 2)]
    [SetupFakeDriver]
    public void Clear_Bounds_Clears_Only_Bounds (int x, int y, int width, int height)
    {
        var superView = new View { Width = Dim.Fill (), Height = Dim.Fill () };

        var view = new View
        {
            Text = "X",
            X = 1, Y = 1,
            Width = 3, Height = 3,
            BorderStyle = LineStyle.Single
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubviews ();

        superView.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);

        Rectangle boundsToClear = new (x, y, width, height);
        view.Clear (boundsToClear);
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);

    }

    [Fact]
    [AutoInitShutdown]
    [Trait("Category","Unicode")]
    public void CJK_Compatibility_Ideographs_ConsoleWidth_ColumnWidth_Equal_Two ()
    {
        const string us = "\U0000f900";
        var r = (Rune)0xf900;

        Assert.Equal ("豈", us);
        Assert.Equal ("豈", r.ToString ());
        Assert.Equal (us, r.ToString ());

        Assert.Equal (2, us.GetColumns ());
        Assert.Equal (2, r.GetColumns ());

        var win = new Window { Title = us };
        var view = new View { Text = r.ToString (), Height = Dim.Fill (), Width = Dim.Fill () };
        var tf = new TextField { Text = us, Y = 1, Width = 3 };
        win.Add (view, tf);
        Toplevel top = new ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        const string expectedOutput = """

                                      ┌┤豈├────┐
                                      │豈      │
                                      │豈      │
                                      └────────┘
                                      """;
        TestHelpers.AssertDriverContentsWithFrameAre (expectedOutput, _output);

        TestHelpers.AssertDriverContentsAre (expectedOutput, _output);

        Attribute [] expectedColors =
        {
            // 0
            Colors.ColorSchemes ["Base"].Normal,

            // 1
            Colors.ColorSchemes ["Base"].Focus,

            // 2
            Colors.ColorSchemes ["Base"].HotNormal
        };

        TestHelpers.AssertDriverAttributesAre (
                                               """

                                               0011000000
                                               0000000000
                                               0111000000
                                               0000000000
                                               """,
                                               Application.Driver,
                                               expectedColors
                                              );
    }

    // TODO: Refactor this test to not depend on TextView etc... Make it as primitive as possible
    [Fact]
    [AutoInitShutdown]
    [Trait("Category","Unicode")]
    public void Clipping_AddRune_Left_Or_Right_Replace_Previous_Or_Next_Wide_Rune_With_Space ()
    {
        var tv = new TextView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = """
                   これは広いルーンラインです。
                   これは広いルーンラインです。
                   これは広いルーンラインです。
                   これは広いルーンラインです。
                   これは広いルーンラインです。
                   これは広いルーンラインです。
                   これは広いルーンラインです。
                   これは広いルーンラインです。
                   """
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (tv);
        var top = new Toplevel ();
        top.Add (win);

        // Don't use Label. It sets AutoSize = true which is not what we're testing here.
        var view = new View { Text = "ワイドルーン。", Height = Dim.Fill (), Width = Dim.Fill () };

        // Don't have unit tests use things that aren't absolutely critical for the test, like Dialog
        var dg = new Window { X = 2, Y = 2, Width = 14, Height = 3 };
        dg.Add (view);
        RunState rsTop = Application.Begin (top);
        RunState rsDiag = Application.Begin (dg);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 10);

        const string expectedOutput = """

                                      ┌────────────────────────────┐
                                      │これは広いルーンラインです。│
                                      │�┌────────────┐�ラインです。│
                                      │�│ワイドルーン│�ラインです。│
                                      │�└────────────┘�ラインです。│
                                      │これは広いルーンラインです。│
                                      │これは広いルーンラインです。│
                                      │これは広いルーンラインです。│
                                      │これは広いルーンラインです。│
                                      └────────────────────────────┘
                                      """;

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expectedOutput, _output);
        Assert.Equal (new Rectangle (0, 0, 30, 10), pos);

        Application.End (rsDiag);
        Application.End (rsTop);
    }

    [Fact]
    [AutoInitShutdown]
    [Trait("Category","Output")]
    public void Colors_On_TextAlignment_Right_And_Bottom ()
    {
        var viewRight = new View
        {
            Text = "Test",
            Width = 6,
            Height = 1,
            TextAlignment = TextAlignment.Right,
            ColorScheme = Colors.ColorSchemes ["Base"]
        };

        var viewBottom = new View
        {
            Text = "Test",
            TextDirection = TextDirection.TopBottom_LeftRight,
            Y = 1,
            Width = 1,
            Height = 6,
            VerticalTextAlignment = VerticalTextAlignment.Bottom,
            ColorScheme = Colors.ColorSchemes ["Base"]
        };
        Toplevel top = new ();
        top.Add (viewRight, viewBottom);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (7, 7);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                        Test
                                                            
                                                            
                                                      T     
                                                      e     
                                                      s     
                                                      t     
                                                      """,
                                                      _output
                                                     );

        TestHelpers.AssertDriverAttributesAre (
                                               """

                                               000000
                                               0
                                               0
                                               0
                                               0
                                               0
                                               0
                                               """,
                                               Application.Driver,
                                               Colors.ColorSchemes ["Base"].Normal
                                              );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Bounds ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Bounds);

        Assert.Equal (new (0,0,2,2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Bounds);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      ┌┐
                                                      └┘
                                                      """,
                                                      _output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Bounds_Without_Bottom ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (1, 1, 1, 0);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Bounds);

        Assert.Equal (new (0,0,2,1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Bounds);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (string.Empty, _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Bounds_Without_Left ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (0, 1, 1, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Bounds);

        Assert.Equal (new (0,0,1,2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Bounds);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      │
                                                      │
                                                      """,
                                                      _output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Bounds_Without_Right ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (1, 1, 0, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Bounds);

        Assert.Equal (new (0,0,1,2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Bounds);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      │
                                                      │
                                                      """,
                                                      _output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Bounds_Without_Top ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (1, 0, 1, 1);

        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Bounds);

        Assert.Equal (new (0,0,2,1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Bounds);

        view.Draw ();

        // BUGBUG: Wha? Is this right? Shouldn't it be "└┘"???
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      ┌┐
                                                      """,
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Bounds_Horizontal_With_New_Lines ()
    {
        var subView = new View
        {
            Id = "subView",
            X = 1,
            Width = 1,
            Height = 7,
            Text = """
            s
            u
            b
            V
            i
            e
            w
            """
        };

        var view = new View
        {
            Id = "view", Width = 2, Height = 20, Text = """
            0
            1
            2
            3
            4
            5
            6
            7
            8
            9
            0
            1
            2
            3
            4
            5
            6
            7
            8
            9
            """
        };
        view.Add (subView);
        var content = new View { Id = "content", Width = 20, Height = 20 };
        content.Add (view);

        var container = new View
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };
        container.Add (content);
        Toplevel top = new ();
        top.Add (container);
        Application.Driver.Clip = container.Frame;
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       0s
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                      """,
                                                      _output
                                                     );

        content.X = -1;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       s
                                                       u
                                                       b
                                                       V
                                                       i
                                                      """,
                                                      _output
                                                     );

        content.X = -2;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre (@"", _output);

        content.X = 0;
        content.Y = -1;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                       5e
                                                      """,
                                                      _output
                                                     );

        content.Y = -6;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       6w
                                                       7 
                                                       8 
                                                       9 
                                                       0 
                                                      """,
                                                      _output
                                                     );

        content.Y = -19;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       9
                                                      """,
                                                      _output
                                                     );

        content.Y = -20;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

        content.X = -2;
        content.Y = 0;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Bounds_Horizontal_Without_New_Lines ()
    {
        // BUGBUG: This previously assumed the default height of a View was 1.
        var subView = new View
        {
            Id = "subView",
            Y = 1,
            Width = 7,
            Height = 1,
            Text = "subView"
        };
        var view = new View { Id = "view", Width = 20, Height = 2, Text = "01234567890123456789" };
        view.Add (subView);
        var content = new View { Id = "content", Width = 20, Height = 20 };
        content.Add (view);

        var container = new View
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };
        container.Add (content);
        Toplevel top = new ();
        top.Add (container);

        // BUGBUG: v2 - it's bogus to reference .Frame before BeginInit. And why is the clip being set anyway???

        void Top_LayoutComplete (object sender, LayoutEventArgs e) { Application.Driver.Clip = container.Frame; }

        top.LayoutComplete += Top_LayoutComplete;
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       01234
                                                       subVi
                                                      """,
                                                      _output
                                                     );

        content.X = -1;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       12345
                                                       ubVie
                                                      """,
                                                      _output
                                                     );

        content.Y = -1;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       ubVie
                                                      """,
                                                      _output
                                                     );

        content.Y = -2;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

        content.X = -20;
        content.Y = 0;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Bounds_Vertical ()
    {
        var subView = new View
        {
            Id = "subView",
            X = 1,
            Width = 1,
            Height = 7,
            Text = "subView",
            TextDirection = TextDirection.TopBottom_LeftRight
        };

        var view = new View
        {
            Id = "view",
            Width = 2,
            Height = 20,
            Text = "01234567890123456789",
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        view.Add (subView);
        var content = new View { Id = "content", Width = 20, Height = 20 };
        content.Add (view);

        var container = new View
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };
        container.Add (content);
        Toplevel top = new ();
        top.Add (container);
        Application.Driver.Clip = container.Frame;
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       0s
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                      """,
                                                      _output
                                                     );

        content.X = -1;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       s
                                                       u
                                                       b
                                                       V
                                                       i
                                                      """,
                                                      _output
                                                     );

        content.X = -2;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre (@"", _output);

        content.X = 0;
        content.Y = -1;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                       5e
                                                      """,
                                                      _output
                                                     );

        content.Y = -6;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       6w
                                                       7 
                                                       8 
                                                       9 
                                                       0 
                                                      """,
                                                      _output
                                                     );

        content.Y = -19;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       9
                                                      """,
                                                      _output
                                                     );

        content.Y = -20;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

        content.X = -2;
        content.Y = 0;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);
    }

    [Theory]
    [SetupFakeDriver]
    [InlineData ("𝔽𝕆𝕆𝔹𝔸R")]
    [InlineData ("a𐐀b")]
    public void DrawHotString_NonBmp (string expected)
    {
        var view = new View { Width = 10, Height = 1 };
        view.DrawHotString (expected, Attribute.Default, Attribute.Default);

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    // TODO: The tests below that use Label should use View instead.
    [Fact]
    [AutoInitShutdown]
    public void Non_Bmp_ConsoleWidth_ColumnWidth_Equal_Two ()
    {
        var us = "\U0001d539";
        var r = (Rune)0x1d539;

        Assert.Equal ("𝔹", us);
        Assert.Equal ("𝔹", r.ToString ());
        Assert.Equal (us, r.ToString ());

        Assert.Equal (1, us.GetColumns ());
        Assert.Equal (1, r.GetColumns ());

        var win = new Window { Title = us };
        var view = new Label { Text = r.ToString () };
        var tf = new TextField { Text = us, Y = 1, Width = 3 };
        win.Add (view, tf);
        Toplevel top = new ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        var expected = """

            ┌┤𝔹├─────┐
            │𝔹       │
            │𝔹       │
            └────────┘
            """;
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        TestHelpers.AssertDriverContentsAre (expected, _output);

        Attribute [] expectedColors =
        {
            // 0
            Colors.ColorSchemes ["Base"].Normal,

            // 1
            Colors.ColorSchemes ["Base"].Focus,

            // 2
            Colors.ColorSchemes ["Base"].HotNormal
        };

        TestHelpers.AssertDriverAttributesAre (
                                               """

                                               0010000000
                                               0000000000
                                               0111000000
                                               0000000000
                                               """,
                                               Application.Driver,
                                               expectedColors
                                              );
    }
}
