#nullable enable
using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class ClipTests (ITestOutputHelper _output)
{
    [Fact]
    [SetupFakeDriver]
    public void Move_Is_Not_Constrained_To_Viewport ()
    {
        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 3, Height = 3
        };
        view.Margin!.Thickness = new (1);

        view.Move (0, 0);
        Assert.Equal (new (2, 2), new Point (Application.Driver!.Col, Application.Driver!.Row));

        view.Move (-1, -1);
        Assert.Equal (new (1, 1), new Point (Application.Driver!.Col, Application.Driver!.Row));

        view.Move (1, 1);
        Assert.Equal (new (3, 3), new Point (Application.Driver!.Col, Application.Driver!.Row));
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
        view.Padding!.Thickness = new (1);
        view.Padding.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.BeginInit ();
        view.EndInit ();
        view.Draw ();

        // Only valid location w/in Viewport is 0, 0 (view) - 2, 2 (screen)
        Assert.Equal ((Rune)' ', Application.Driver?.Contents! [2, 2].Rune);

        // When we exit Draw, the view is excluded from the clip. So drawing at 0,0, is not valid and is clipped.
        view.AddRune (0, 0, Rune.ReplacementChar);
        Assert.Equal ((Rune)' ', Application.Driver?.Contents! [2, 2].Rune);

        view.AddRune (-1, -1, Rune.ReplacementChar);
        Assert.Equal ((Rune)'P', Application.Driver?.Contents! [1, 1].Rune);

        view.AddRune (1, 1, Rune.ReplacementChar);
        Assert.Equal ((Rune)'P', Application.Driver?.Contents! [3, 3].Rune);
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
        superView.LayoutSubViews ();

        superView.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       _output);

        Rectangle toFill = new (x, y, width, height);
        View.SetClipToScreen ();
        view.FillRect (toFill);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │ │
 └─┘",
                                                       _output);

        // Now try to clear beyond Viewport (invalid; clipping should prevent)
        superView.SetNeedsDraw ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       _output);
        toFill = new (-width, -height, width, height);
        view.FillRect (toFill);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       _output);

        // Now try to clear beyond Viewport (valid)
        superView.SetNeedsDraw ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       _output);
        toFill = new (-1, -1, width + 1, height + 1);

        View.SetClipToScreen ();
        view.FillRect (toFill);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │ │
 └─┘",
                                                       _output);

        // Now clear too much size
        superView.SetNeedsDraw ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       _output);
        toFill = new (0, 0, width * 2, height * 2);
        View.SetClipToScreen ();
        view.FillRect (toFill);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │ │
 └─┘",
                                                       _output);
    }

    // TODO: Simplify this test to just use AddRune directly
    [Fact]
    [SetupFakeDriver]
    [Trait ("Category", "Unicode")]
    public void Clipping_Wide_Runes ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 1);

        var top = new View
        {
            Id = "top",
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        var frameView = new View
        {
            Id = "frameView",
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = """
                   これは広いルーンラインです。
                   """
        };
        frameView.Border!.LineStyle = LineStyle.Single;
        frameView.Border.Thickness = new (1, 0, 0, 0);

        top.Add (frameView);
        View.SetClipToScreen ();
        top.Layout ();
        top.Draw ();

        var expectedOutput = """
                             │これは広いルーンラインです。
                             """;

        DriverAssert.AssertDriverContentsWithFrameAre (expectedOutput, _output);

        var view = new View
        {
            Text = "0123456789",

            //Text = "ワイドルー。",
            X = 2,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            BorderStyle = LineStyle.Single
        };
        view.Border!.Thickness = new (1, 0, 1, 0);

        top.Add (view);
        top.Layout ();
        View.SetClipToScreen ();
        top.Draw ();

        //                            012345678901234567890123456789012345678
        //                            012 34 56 78 90 12 34 56 78 90 12 34 56 78
        //                            │こ れ  は 広 い  ル ー ン  ラ イ ン で  す 。
        //                            01 2345678901234 56 78 90 12 34 56 
        //                            │� |0123456989│� ン  ラ イ ン で  す 。
        expectedOutput = """
                         │�│0123456789│�ンラインです。
                         """;

        DriverAssert.AssertDriverContentsWithFrameAre (expectedOutput, _output);
    }

    // TODO: Add more AddRune tests to cover all the cases where wide runes are clipped

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
        var view = new View
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ViewportSettings = ViewportSettingsFlags.ClipContentOnly
        };
        view.SetContentSize (new Size (10, 10));
        view.Border!.Thickness = new (1);
        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (view.Frame, View.GetClip ()!.GetBounds ());

        // Act
        view.AddViewportToClip ();

        // Assert
        Assert.Equal (expectedClip, View.GetClip ()!.GetBounds ());
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
        var view = new View
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        view.SetContentSize (new Size (10, 10));
        view.Border!.Thickness = new (1);
        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (view.Frame, View.GetClip ()!.GetBounds ());
        view.Viewport = view.Viewport with { X = 1, Y = 1 };

        // Act
        view.AddViewportToClip ();

        // Assert
        Assert.Equal (expectedClip, View.GetClip ()!.GetBounds ());
        view.Dispose ();
    }
}
