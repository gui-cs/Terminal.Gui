#nullable enable
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class DrawTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeDriver]
    public void Move_Is_Constrained_To_Viewport ()
    {
        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 3, Height = 3
        };
        view.Margin.Thickness = new Thickness (1);

        // Only valid location w/in Viewport is 0, 0 (view) - 2, 2 (screen)

        view.Move (0, 0);
        Assert.Equal(new Point(2, 2), new Point (Application.Driver.Col, Application.Driver.Row));

        view.Move (-1, -1);
        Assert.Equal (new Point (2, 2), new Point (Application.Driver.Col, Application.Driver.Row));

        view.Move (1, 1);
        Assert.Equal (new Point (2, 2), new Point (Application.Driver.Col, Application.Driver.Row));
    }

    [Fact]
    [SetupFakeDriver]
    public void AddRune_Is_Constrained_To_Viewport ()
    {
        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 3, Height = 3
        };
        view.Margin.Thickness = new Thickness (1);
        View.Diagnostics = ViewDiagnosticFlags.Padding;
        view.BeginInit();
        view.EndInit();
        view.Draw();

        // Only valid location w/in Viewport is 0, 0 (view) - 2, 2 (screen)
        Assert.Equal ((Rune)' ', Application.Driver.Contents [2, 2].Rune);

        view.AddRune(0, 0, Rune.ReplacementChar);
        Assert.Equal (Rune.ReplacementChar, Application.Driver.Contents [2, 2].Rune);

        view.AddRune (-1, -1, Rune.ReplacementChar);
        Assert.Equal ((Rune)'M', Application.Driver.Contents [1, 1].Rune);

        view.AddRune (1, 1, Rune.ReplacementChar);
        Assert.Equal ((Rune)'M', Application.Driver.Contents [3, 3].Rune);

        View.Diagnostics = ViewDiagnosticFlags.Off;
    }

    [Theory]
    [InlineData (0, 0, 1, 1)]
    [InlineData (0, 0, 2, 2)]
    [InlineData (-1, -1, 2, 2)]
    [SetupFakeDriver]
    public void Clear_Clears_Only_Viewport (int x, int y, int width, int height)
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
                                                      output);

        Rectangle toClear = new (x, y, width, height);
        view.Clear (toClear);
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      output);
        // Now try to clear beyond Viewport (invalid)
        superView.SetNeedsDisplay ();
        superView.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      output);
        toClear = new (-width, -height, width, height);
        view.Clear (toClear);
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      output);

        // Now try to clear beyond Viewport (valid)
        superView.SetNeedsDisplay ();
        superView.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      output);
        toClear = new (-1, -1, width + 1, height + 1);
        view.Clear (toClear);
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      output);

        // Now clear too much size
        superView.SetNeedsDisplay ();
        superView.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      output);
        toClear = new (0, 0, width * 2, height * 2);
        view.Clear (toClear);
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      output);
    }


    [Fact]
    [AutoInitShutdown]
    [Trait ("Category", "Unicode")]
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
        Toplevel top = Application.Top;
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        const string expectedOutput = """

                                      ┌┤豈├────┐
                                      │豈      │
                                      │豈      │
                                      └────────┘
                                      """;
        TestHelpers.AssertDriverContentsWithFrameAre (expectedOutput, output);

        TestHelpers.AssertDriverContentsAre (expectedOutput, output);

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
    [Trait ("Category", "Unicode")]
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
        Application.Top.Add (win);

        // Don't use Label. It sets AutoSize = true which is not what we're testing here.
        var view = new View { Text = "ワイドルーン。", Height = Dim.Fill (), Width = Dim.Fill () };

        // Don't have unit tests use things that aren't absolutely critical for the test, like Dialog
        var dg = new Window { X = 2, Y = 2, Width = 14, Height = 3 };
        dg.Add (view);
        Application.Begin (Application.Top);
        Application.Begin (dg);
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

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expectedOutput, output);
        Assert.Equal (new Rectangle (0, 0, 30, 10), pos);
    }

    [Fact]
    [AutoInitShutdown]
    [Trait ("Category", "Output")]
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
        Toplevel top = Application.Top;
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
                                                      output
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
    public void Draw_Minimum_Full_Border_With_Empty_Viewport ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Viewport);

        Assert.Equal (new (0, 0, 2, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      ┌┐
                                                      └┘
                                                      """,
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Bottom ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (1, 1, 1, 0);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Viewport);

        Assert.Equal (new (0, 0, 2, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (string.Empty, output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Left ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (0, 1, 1, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Viewport);

        Assert.Equal (new (0, 0, 1, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      │
                                                      │
                                                      """,
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Right ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (1, 1, 0, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Viewport);

        Assert.Equal (new (0, 0, 1, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      │
                                                      │
                                                      """,
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Top ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (1, 0, 1, 1);

        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Driver.Viewport);

        Assert.Equal (new (0, 0, 2, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        // BUGBUG: Wha? Is this right? Shouldn't it be "└┘"???
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                      ┌┐
                                                      """,
                                                      output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Viewport_Horizontal_With_New_Lines ()
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
        Toplevel top = Application.Top;
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
                                                      output
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
                                                      output
                                                     );

        content.X = -2;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre (@"", output);

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
                                                      output
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
                                                      output
                                                     );

        content.Y = -19;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       9
                                                      """,
                                                      output
                                                     );

        content.Y = -20;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", output);

        content.X = -2;
        content.Y = 0;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Viewport_Horizontal_Without_New_Lines ()
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
        Toplevel top = Application.Top;
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
                                                      output
                                                     );

        content.X = -1;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       12345
                                                       ubVie
                                                      """,
                                                      output
                                                     );

        content.Y = -1;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       ubVie
                                                      """,
                                                      output
                                                     );

        content.Y = -2;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", output);

        content.X = -20;
        content.Y = 0;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Viewport_Vertical ()
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
        Toplevel top = Application.Top;
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
                                                      output
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
                                                      output
                                                     );

        content.X = -2;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre (@"", output);

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
                                                      output
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
                                                      output
                                                     );

        content.Y = -19;
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      """

                                                       9
                                                      """,
                                                      output
                                                     );

        content.Y = -20;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", output);

        content.X = -2;
        content.Y = 0;
        Application.Refresh ();
        TestHelpers.AssertDriverContentsWithFrameAre ("", output);
    }

    [Theory]
    [SetupFakeDriver]
    [InlineData ("𝔽𝕆𝕆𝔹𝔸R")]
    [InlineData ("a𐐀b")]
    public void DrawHotString_NonBmp (string expected)
    {
        var view = new View { Width = 10, Height = 1 };
        view.DrawHotString (expected, Attribute.Default, Attribute.Default);

        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
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
        Toplevel top = Application.Top;
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        var expected = """

            ┌┤𝔹├─────┐
            │𝔹       │
            │𝔹       │
            └────────┘
            """;
        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        TestHelpers.AssertDriverContentsAre (expected, output);

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
