#nullable enable
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class DrawTests (ITestOutputHelper _output)
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
        Assert.Equal (new Point (2, 2), new Point (Application.Driver!.Col, Application.Driver!.Row));

        view.Move (-1, -1);
        Assert.Equal (new Point (2, 2), new Point (Application.Driver!.Col, Application.Driver!.Row));

        view.Move (1, 1);
        Assert.Equal (new Point (2, 2), new Point (Application.Driver!.Col, Application.Driver!.Row));
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
        view.BeginInit ();
        view.EndInit ();
        view.Draw ();

        // Only valid location w/in Viewport is 0, 0 (view) - 2, 2 (screen)
        Assert.Equal ((Rune)' ', Application.Driver?.Contents! [2, 2].Rune);

        view.AddRune (0, 0, Rune.ReplacementChar);
        Assert.Equal (Rune.ReplacementChar, Application.Driver?.Contents! [2, 2].Rune);

        view.AddRune (-1, -1, Rune.ReplacementChar);
        Assert.Equal ((Rune)'M', Application.Driver?.Contents! [1, 1].Rune);

        view.AddRune (1, 1, Rune.ReplacementChar);
        Assert.Equal ((Rune)'M', Application.Driver?.Contents! [3, 3].Rune);

        View.Diagnostics = ViewDiagnosticFlags.Off;
    }

    [Theory]
    [InlineData (0, 0, 1, 1)]
    [InlineData (0, 0, 2, 2)]
    [InlineData (-1, -1, 2, 2)]
    [SetupFakeDriver]
    public void FillRect_Fills_HonorsClip (int x, int y, int width, int height)
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

        Rectangle toFill = new (x, y, width, height);
        view.FillRect (toFill);
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);

        // Now try to clear beyond Viewport (invalid; clipping should prevent)
        superView.SetNeedsDisplay ();
        superView.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);
        toFill = new (-width, -height, width, height);
        view.FillRect (toFill);
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);

        // Now try to clear beyond Viewport (valid)
        superView.SetNeedsDisplay ();
        superView.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);
        toFill = new (-1, -1, width + 1, height + 1);
        view.FillRect (toFill);
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);

        // Now clear too much size
        superView.SetNeedsDisplay ();
        superView.Draw ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │X│
 └─┘",
                                                      _output);
        toFill = new (0, 0, width * 2, height * 2);
        view.FillRect (toFill);
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Clear_ClearsEntireViewport ()
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

        view.Clear ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Clear_WithClearVisibleContentOnly_ClearsVisibleContentOnly ()
    {
        var superView = new View { Width = Dim.Fill (), Height = Dim.Fill () };

        var view = new View
        {
            Text = "X",
            X = 1, Y = 1,
            Width = 3, Height = 3,
            BorderStyle = LineStyle.Single,
            ViewportSettings = ViewportSettings.ClearContentOnly
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

        view.Clear ();
        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌─┐
 │ │
 └─┘",
                                                      _output);
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
        var view = new View { Text = r.ToString (), Height = Dim.Fill (), Width = Dim.Fill ()};
        var tf = new TextField { Text = us, Y = 1, Width = 3 };
        win.Add (view, tf);
        Toplevel top = new ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (10, 4);

        const string expectedOutput = """

                                      ┌┤豈├────┐
                                      │豈      │
                                      │豈      │
                                      └────────┘
                                      """;
        TestHelpers.AssertDriverContentsWithFrameAre (expectedOutput, _output);

        TestHelpers.AssertDriverContentsAre (expectedOutput, _output);

        // This test has nothing to do with color - removing as it is not relevant and fragile
        top.Dispose ();
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
        var top = new Toplevel ();
        top.Add (win);

        var view = new View { Text = "ワイドルーン。", Height = Dim.Fill (), Width = Dim.Fill () };

        // Don't have unit tests use things that aren't absolutely critical for the test, like Dialog
        var dg = new Window { X = 2, Y = 2, Width = 14, Height = 3 };
        dg.Add (view);
        RunState rsTop = Application.Begin (top);
        RunState rsDiag = Application.Begin (dg);
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 10);

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
        dg.Dispose ();
        Application.End (rsTop);
        top.Dispose ();
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
            TextAlignment = Alignment.End,
            ColorScheme = Colors.ColorSchemes ["Base"]
        };

        var viewBottom = new View
        {
            Text = "Test",
            TextDirection = TextDirection.TopBottom_LeftRight,
            Y = 1,
            Width = 1,
            Height = 6,
            VerticalTextAlignment = Alignment.End,
            ColorScheme = Colors.ColorSchemes ["Base"]
        };
        Toplevel top = new ();
        top.Add (viewRight, viewBottom);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (7, 7);

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
                                               Colors.ColorSchemes ["Base"]!.Normal
                                              );
        top.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 2, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

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
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Bottom ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (1, 1, 1, 0);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 2, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre ("──", _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Left ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (0, 1, 1, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 1, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

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
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Right ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (1, 1, 0, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 1, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

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
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Top ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border.Thickness = new Thickness (1, 0, 1, 1);

        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 2, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre ("││",
                                                      _output
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
        Toplevel top = new ();
        top.Add (container);
        Application.Driver!.Clip = container.Frame;
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
        top.Dispose ();
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
        Toplevel top = new ();
        top.Add (container);

        // BUGBUG: v2 - it's bogus to reference .Frame before BeginInit. And why is the clip being set anyway???

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
        top.Dispose ();

        return;

        void Top_LayoutComplete (object? sender, LayoutEventArgs e) { Application.Driver!.Clip = container.Frame; }
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
        Toplevel top = new ();
        top.Add (container);
        Application.Driver!.Clip = container.Frame;
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
        top.Dispose ();
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
        ((FakeDriver)Application.Driver!).SetBufferSize (10, 4);

        var expected = """

            ┌┤𝔹├─────┐
            │𝔹       │
            │𝔹       │
            └────────┘
            """;
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        TestHelpers.AssertDriverContentsAre (expected, _output);
        top.Dispose ();
        // This test has nothing to do with color - removing as it is not relevant and fragile
    }

    [Fact]
    [SetupFakeDriver]
    public void SetClip_ClipVisibleContentOnly_VisibleContentIsClipped ()
    {
        // Screen is 25x25
        // View is 25x25
        // Viewport is (0, 0, 23, 23)
        // ContentSize is (10, 10)
        // ViewportToScreen is (1, 1, 23, 23)
        // Visible content is (1, 1, 10, 10)
        // Expected clip is (1, 1, 10, 10) - same as visible content
        Rectangle expectedClip = new (1, 1, 10, 10);
        // Arrange
        var view = new View ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ViewportSettings = ViewportSettings.ClipContentOnly
        };
        view.SetContentSize (new Size (10, 10));
        view.Border.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (view.Frame, Application.Driver?.Clip);

        // Act
        view.SetClip ();

        // Assert
        Assert.Equal (expectedClip, Application.Driver?.Clip);
        view.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void SetClip_Default_ClipsToViewport ()
    {
        // Screen is 25x25
        // View is 25x25
        // Viewport is (0, 0, 23, 23)
        // ContentSize is (10, 10)
        // ViewportToScreen is (1, 1, 23, 23)
        // Visible content is (1, 1, 10, 10)
        // Expected clip is (1, 1, 23, 23) - same as Viewport
        Rectangle expectedClip = new (1, 1, 23, 23);
        // Arrange
        var view = new View ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
        };
        view.SetContentSize (new Size (10, 10));
        view.Border.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (view.Frame, Application.Driver?.Clip);
        view.Viewport = view.Viewport with { X = 1, Y = 1 };

        // Act
        view.SetClip ();

        // Assert
        Assert.Equal (expectedClip, Application.Driver?.Clip);
        view.Dispose ();
    }


    [Fact]
    [TestRespondersDisposed]
    public void Draw_Throws_IndexOutOfRangeException_With_Negative_Bounds ()
    {
        Application.Init (new FakeDriver ());

        Toplevel top = new ();

        var view = new View { X = -2, Text = "view" };
        top.Add (view);

        Application.Iteration += (s, a) =>
        {
            Assert.Equal (-2, view.X);

            Application.RequestStop ();
        };

        try
        {
            Application.Run (top);
        }
        catch (IndexOutOfRangeException ex)
        {
            // After the fix this exception will not be caught.
            Assert.IsType<IndexOutOfRangeException> (ex);
        }

        top.Dispose ();
        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

}
