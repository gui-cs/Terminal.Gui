using System.Text;
using Xunit.Abstractions;

//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.DrawingTests;

public class ThicknessTests
{
    private readonly ITestOutputHelper _output;
    public ThicknessTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void Constructor_Defaults ()
    {
        var t = new Thickness ();
        Assert.Equal (0, t.Left);
        Assert.Equal (0, t.Top);
        Assert.Equal (0, t.Right);
        Assert.Equal (0, t.Bottom);
    }

    [Fact]
    public void Constructor_params ()
    {
        var t = new Thickness (1, 2, 3, 4);
        Assert.Equal (1, t.Left);
        Assert.Equal (2, t.Top);
        Assert.Equal (3, t.Right);
        Assert.Equal (4, t.Bottom);

        t = new Thickness (0, 0, 0, 0);
        Assert.Equal (0, t.Left);
        Assert.Equal (0, t.Top);
        Assert.Equal (0, t.Right);
        Assert.Equal (0, t.Bottom);

        t = new Thickness (-1, 0, 0, 0);
        Assert.Equal (-1, t.Left);
        Assert.Equal (0, t.Top);
        Assert.Equal (0, t.Right);
        Assert.Equal (0, t.Bottom);
    }

    [Fact]
    public void Constructor_Width ()
    {
        var t = new Thickness (1);
        Assert.Equal (1, t.Left);
        Assert.Equal (1, t.Top);
        Assert.Equal (1, t.Right);
        Assert.Equal (1, t.Bottom);
    }

    [Fact]
    [AutoInitShutdown]
    public void DrawTests ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (60, 60);
        var t = new Thickness (0, 0, 0, 0);
        var r = new Rectangle (5, 5, 40, 15);
        View.Diagnostics |= ViewDiagnosticFlags.Padding;

        Application.Driver.FillRect (
                                     new Rectangle (0, 0, Application.Driver.Cols, Application.Driver.Rows),
                                     (Rune)' '
                                    );
        t.Draw (r, "Test");
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
       Test (Left=0,Top=0,Right=0,Bottom=0)",
                                                      _output
                                                     );

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (5, 5, 40, 15);
        View.Diagnostics |= ViewDiagnosticFlags.Padding;

        Application.Driver.FillRect (
                                     new Rectangle (0, 0, Application.Driver.Cols, Application.Driver.Rows),
                                     (Rune)' '
                                    );
        t.Draw (r, "Test");
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     TTTest (Left=1,Top=1,Right=1,Bottom=1)TT",
                                                      _output
                                                     );

        t = new Thickness (1, 2, 3, 4);
        r = new Rectangle (5, 5, 40, 15);
        View.Diagnostics |= ViewDiagnosticFlags.Padding;

        Application.Driver.FillRect (
                                     new Rectangle (0, 0, Application.Driver.Cols, Application.Driver.Rows),
                                     (Rune)' '
                                    );
        t.Draw (r, "Test");
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTest (Left=1,Top=2,Right=3,Bottom=4)TT",
                                                      _output
                                                     );

        t = new Thickness (-1, 1, 1, 1);
        r = new Rectangle (5, 5, 40, 15);
        View.Diagnostics |= ViewDiagnosticFlags.Padding;

        Application.Driver.FillRect (
                                     new Rectangle (0, 0, Application.Driver.Cols, Application.Driver.Rows),
                                     (Rune)' '
                                    );
        t.Draw (r, "Test");
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
     TTest (Left=-1,Top=1,Right=1,Bottom=1)TT",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void DrawTests_Ruler ()
    {
        // Add a frame so we can see the ruler
        var f = new FrameView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        var top = new Toplevel ();
        top.Add (f);
        Application.Begin (top);

        ((FakeDriver)Application.Driver).SetBufferSize (45, 20);
        var t = new Thickness (0, 0, 0, 0);
        var r = new Rectangle (2, 2, 40, 15);
        Application.Refresh ();
        View.Diagnostics |= ViewDiagnosticFlags.Ruler;
        t.Draw (r, "Test");
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsAre (
                                             @"
┌───────────────────────────────────────────┐
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
└───────────────────────────────────────────┘",
                                             _output
                                            );

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (1, 1, 40, 15);
        Application.Refresh ();
        View.Diagnostics |= ViewDiagnosticFlags.Ruler;
        t.Draw (r, "Test");
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsAre (
                                             @"
┌───────────────────────────────────────────┐
│|123456789|123456789|123456789|123456789   │
│1                                      1   │
│2                                      2   │
│3                                      3   │
│4                                      4   │
│5                                      5   │
│6                                      6   │
│7                                      7   │
│8                                      8   │
│9                                      9   │
│-                                      -   │
│1                                      1   │
│2                                      2   │
│3                                      3   │
│|123456789|123456789|123456789|123456789   │
│                                           │
│                                           │
│                                           │
└───────────────────────────────────────────┘",
                                             _output
                                            );

        t = new Thickness (1, 2, 3, 4);
        r = new Rectangle (2, 2, 40, 15);
        Application.Refresh ();
        View.Diagnostics |= ViewDiagnosticFlags.Ruler;
        t.Draw (r, "Test");
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌───────────────────────────────────────────┐
│                                           │
│ |123456789|123456789|123456789|123456789  │
│ 1                                      1  │
│ 2                                      2  │
│ 3                                      3  │
│ 4                                      4  │
│ 5                                      5  │
│ 6                                      6  │
│ 7                                      7  │
│ 8                                      8  │
│ 9                                      9  │
│ -                                      -  │
│ 1                                      1  │
│ 2                                      2  │
│ 3                                      3  │
│ |123456789|123456789|123456789|123456789  │
│                                           │
│                                           │
└───────────────────────────────────────────┘",
                                                      _output
                                                     );

        t = new Thickness (-1, 1, 1, 1);
        r = new Rectangle (5, 5, 40, 15);
        Application.Refresh ();
        View.Diagnostics |= ViewDiagnosticFlags.Ruler;
        t.Draw (r, "Test");
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌───────────────────────────────────────────┐
│                                           │
│                                           │
│                                           │
│                                           │
│    |123456789|123456789|123456789|123456789
│                                           1
│                                           2
│                                           3
│                                           4
│                                           5
│                                           6
│                                           7
│                                           8
│                                           9
│                                           -
│                                           1
│                                           2
│                                           3
└────|123456789|123456789|123456789|123456789",
                                                      _output
                                                     );
    }

    [Fact]
    public void Empty_Is_empty ()
    {
        var t = Thickness.Empty;
        Assert.Equal (0, t.Left);
        Assert.Equal (0, t.Top);
        Assert.Equal (0, t.Right);
        Assert.Equal (0, t.Bottom);
    }

    // TODO: Add more test cases:
    // - Negative thickness values

    [Fact]
    public void EqualsTest ()
    {
        var t = new Thickness (1, 2, 3, 4);
        var t2 = new Thickness (1, 2, 3, 4);
        Assert.True (t.Equals (t2));
        Assert.True (t == t2);
        Assert.False (t != t2);
    }

    [Fact]
    public void GetHashCodeTest ()
    {
        var t = new Thickness (1, 2, 3, 4);
        Assert.Equal (t.GetHashCode (), t.GetHashCode ());
    }

    // Test Thickness.GetInside(Rectangle)
    [Theory]
    [InlineData (0, 0, 10, 10, 1, 1, 8, 8)]
    [InlineData (1, 0, 10, 10, 2, 1, 8, 8)]
    [InlineData (0, 1, 10, 10, 1, 2, 8, 8)]
    public void GetInside_Uniform (int x, int y, int width, int height, int expectedX, int expectedY, int expectedWidth, int expectedHeight)
    {
        var t = new Thickness (1, 1, 1, 1); // Uniform thickness for simplicity
        var r = new Rectangle (x, y, width, height);
        Rectangle inside = t.GetInside (r);
        Assert.Equal (expectedX, inside.X);
        Assert.Equal (expectedY, inside.Y);
        Assert.Equal (expectedWidth, inside.Width);
        Assert.Equal (expectedHeight, inside.Height);
    }

    [Fact]
    public void GetInsideTests_Mixed_Pos_Neg_Thickness_Non_Empty_Size ()
    {
        var t = new Thickness (-1, 1, -1, 1);
        var r = new Rectangle (0, 0, 3, 3);
        Rectangle inside = t.GetInside (r);
        Assert.Equal (-1, inside.X);
        Assert.Equal (1, inside.Y);
        Assert.Equal (5, inside.Width);
        Assert.Equal (1, inside.Height);

        t = new Thickness (-1, 1, -1, 1);
        r = new Rectangle (-1, -1, 3, 3);
        inside = t.GetInside (r);
        Assert.Equal (-2, inside.X);
        Assert.Equal (0, inside.Y);
        Assert.Equal (5, inside.Width);
        Assert.Equal (1, inside.Height);

        t = new Thickness (-1, 1, -1, 1);
        r = new Rectangle (1, 1, 3, 3);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (2, inside.Y);
        Assert.Equal (5, inside.Width);
        Assert.Equal (1, inside.Height);

        t = new Thickness (-2, -1, 0, 1);
        r = new Rectangle (-1, 0, 50, 60);
        inside = t.GetInside (r);
        Assert.Equal (-3, inside.X);
        Assert.Equal (-1, inside.Y);
        Assert.Equal (52, inside.Width);
        Assert.Equal (60, inside.Height);
    }

    [Fact]
    public void GetInsideTests_Negative_Thickness_Non_Empty_Size ()
    {
        var t = new Thickness (-1, -1, -1, -1);
        var r = new Rectangle (0, 0, 3, 3);
        Rectangle inside = t.GetInside (r);
        Assert.Equal (-1, inside.X);
        Assert.Equal (-1, inside.Y);
        Assert.Equal (5, inside.Width);
        Assert.Equal (5, inside.Height);

        t = new Thickness (-1, -1, -1, -1);
        r = new Rectangle (-1, -1, 3, 3);
        inside = t.GetInside (r);
        Assert.Equal (-2, inside.X);
        Assert.Equal (-2, inside.Y);
        Assert.Equal (5, inside.Width);
        Assert.Equal (5, inside.Height);

        t = new Thickness (-1, -1, -1, -1);
        r = new Rectangle (1, 1, 3, 3);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (0, inside.Y);
        Assert.Equal (5, inside.Width);
        Assert.Equal (5, inside.Height);

        t = new Thickness (-1, -2, -3, -4);
        r = new Rectangle (-1, 0, 50, 60);
        inside = t.GetInside (r);
        Assert.Equal (-2, inside.X);
        Assert.Equal (-2, inside.Y);
        Assert.Equal (54, inside.Width);
        Assert.Equal (66, inside.Height);
    }

    [Fact]
    public void GetInsideTests_Positive_Thickness_Non_Empty_Size ()
    {
        var t = new Thickness (1, 1, 1, 1);
        var r = new Rectangle (0, 0, 3, 3);
        Rectangle inside = t.GetInside (r);
        Assert.Equal (1, inside.X);
        Assert.Equal (1, inside.Y);
        Assert.Equal (1, inside.Width);
        Assert.Equal (1, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (-1, -1, 3, 3);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (0, inside.Y);
        Assert.Equal (1, inside.Width);
        Assert.Equal (1, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (1, 1, 3, 3);
        inside = t.GetInside (r);
        Assert.Equal (2, inside.X);
        Assert.Equal (2, inside.Y);
        Assert.Equal (1, inside.Width);
        Assert.Equal (1, inside.Height);

        t = new Thickness (1, 2, 3, 4);
        r = new Rectangle (-1, 0, 50, 60);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (2, inside.Y);
        Assert.Equal (46, inside.Width);
        Assert.Equal (54, inside.Height);
    }

    [Fact]
    public void GetInsideTests_Positive_Thickness_Too_Small_Rect_Means_Empty_Size ()
    {
        var t = new Thickness (1, 1, 1, 1);
        var r = Rectangle.Empty;
        Rectangle inside = t.GetInside (r);
        Assert.Equal (1, inside.X);
        Assert.Equal (1, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (0, 0, 1, 1);
        inside = t.GetInside (r);
        Assert.Equal (1, inside.X);
        Assert.Equal (1, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (1, 1, 1, 1);
        inside = t.GetInside (r);
        Assert.Equal (2, inside.X);
        Assert.Equal (2, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (0, 0, 1, 0);
        inside = t.GetInside (r);
        Assert.Equal (1, inside.X);
        Assert.Equal (1, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (0, 0, 0, 1);
        inside = t.GetInside (r);
        Assert.Equal (1, inside.X);
        Assert.Equal (1, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (-1, -1, 0, 1);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (0, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (0, 0, 2, 2);
        inside = t.GetInside (r);
        Assert.Equal (1, inside.X);
        Assert.Equal (1, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (-1, -1, 2, 2);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (0, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (1, 1, 1, 1);
        r = new Rectangle (1, 1, 2, 2);
        inside = t.GetInside (r);
        Assert.Equal (2, inside.X);
        Assert.Equal (2, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (1, 2, 3, 4);
        r = new Rectangle (-1, 0, 4, 6);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (2, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);
    }

    [Fact]
    public void GetInsideTests_Zero_Thickness ()
    {
        var t = new Thickness (0, 0, 0, 0);
        var r = Rectangle.Empty;
        Rectangle inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (0, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (0, 0, 0, 0);
        r = new Rectangle (0, 0, 1, 1);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (0, inside.Y);
        Assert.Equal (1, inside.Width);
        Assert.Equal (1, inside.Height);

        t = new Thickness (0, 0, 0, 0);
        r = new Rectangle (1, 1, 1, 1);
        inside = t.GetInside (r);
        Assert.Equal (1, inside.X);
        Assert.Equal (1, inside.Y);
        Assert.Equal (1, inside.Width);
        Assert.Equal (1, inside.Height);

        t = new Thickness (0, 0, 0, 0);
        r = new Rectangle (0, 0, 1, 0);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (0, inside.Y);
        Assert.Equal (1, inside.Width);
        Assert.Equal (0, inside.Height);

        t = new Thickness (0, 0, 0, 0);
        r = new Rectangle (0, 0, 0, 1);
        inside = t.GetInside (r);
        Assert.Equal (0, inside.X);
        Assert.Equal (0, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (1, inside.Height);

        t = new Thickness (0, 0, 0, 0);
        r = new Rectangle (-1, -1, 0, 1);
        inside = t.GetInside (r);
        Assert.Equal (-1, inside.X);
        Assert.Equal (-1, inside.Y);
        Assert.Equal (0, inside.Width);
        Assert.Equal (1, inside.Height);
    }

    [Fact]
    public void Horizontal_get ()
    {
        var t = new Thickness (1, 2, 3, 4);
        Assert.Equal (4, t.Horizontal);

        t = new Thickness (0);
        Assert.Equal (0, t.Horizontal);
    }

    [Fact]
    public void Horizontal_set ()
    {
        var t = new Thickness ();
        t.Horizontal = 10;
        Assert.Equal (10, t.Horizontal);
        Assert.Equal (5, t.Left);
        Assert.Equal (0, t.Top);
        Assert.Equal (5, t.Right);
        Assert.Equal (0, t.Bottom);
        Assert.Equal (0, t.Vertical);

        t.Horizontal = 11;
        Assert.Equal (10, t.Horizontal);
        Assert.Equal (5, t.Left);
        Assert.Equal (0, t.Top);
        Assert.Equal (5, t.Right);
        Assert.Equal (0, t.Bottom);
        Assert.Equal (0, t.Vertical);

        t.Horizontal = 1;
        Assert.Equal (0, t.Horizontal);
        Assert.Equal (0, t.Left);
        Assert.Equal (0, t.Top);
        Assert.Equal (0, t.Right);
        Assert.Equal (0, t.Bottom);
        Assert.Equal (0, t.Vertical);
    }

    [Theory]
    [InlineData (0, 0, 10, 10, 3, 3, false)] // Inside the inner rectangle
    [InlineData (0, 0, 10, 10, 0, 0, true)] // On corner, in thickness
    [InlineData (0, 0, 10, 10, 9, 9, true)] // On opposite corner, in thickness
    [InlineData (0, 0, 10, 10, 5, 5, false)] // Inside the inner rectangle
    [InlineData (0, 0, 10, 10, -1, -1, false)] // Outside the outer rectangle
    [InlineData (0, 0, 0, 0, 3, 3, false)] // Inside the inner rectangle
    [InlineData (0, 0, 0, 0, 0, 0, false)] // On corner, in thickness
    [InlineData (0, 0, 0, 0, 9, 9, false)] // On opposite corner, in thickness
    [InlineData (0, 0, 0, 0, 5, 5, false)] // Inside the inner rectangle
    [InlineData (0, 0, 0, 0, -1, -1, false)] // Outside the outer rectangle
    [InlineData (1, 1, 10, 10, 1, 1, true)] // On corner, in thickness
    [InlineData (1, 1, 10, 10, 10, 10, true)] // On opposite corner, in thickness
    [InlineData (1, 1, 10, 10, 6, 6, false)] // Inside the inner rectangle
    [InlineData (1, 1, 10, 10, 0, 0, false)] // Outside the outer rectangle
    [InlineData (-1, -1, 10, 10, -1, -1, true)] // On corner, in thickness
    [InlineData (-1, -1, 10, 10, 8, 8, true)] // On opposite corner, in thickness
    [InlineData (-1, -1, 10, 10, 4, 4, false)] // Inside the inner rectangle
    [InlineData (-1, -1, 10, 10, -2, -2, false)] // Outside the outer rectangle
    public void TestContains_Uniform1 (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        var rect = new Rectangle (x, y, width, height);
        var thickness = new Thickness (1, 1, 1, 1); // Uniform thickness for simplicity
        bool result = thickness.Contains (rect, pointX, pointY);
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData (0, 0, 10, 10, 3, 3, false)] // Inside the inner rectangle
    [InlineData (0, 0, 10, 10, 0, 0, true)] // On corner, in thickness
    [InlineData (0, 0, 10, 10, 1, 1, true)] // On corner, in thickness
    [InlineData (0, 0, 10, 10, 9, 9, true)] // On opposite corner, in thickness
    [InlineData (0, 0, 10, 10, 5, 5, false)] // Inside the inner rectangle
    [InlineData (0, 0, 10, 10, 8, 8, true)] // On opposite corner, in thickness
    [InlineData (0, 0, 10, 10, -1, -1, false)] // Outside the outer rectangle

    // Test with a rectangle that is same size as the thickness (inner is empty)
    [InlineData (0, 0, 4, 4, 0, 0, true)] // in thickness
    [InlineData (0, 0, 4, 4, 1, 1, true)] // in thickness
    [InlineData (0, 0, 4, 4, 2, 2, true)] // in thickness
    [InlineData (0, 0, 4, 4, 3, 3, true)] // in thickness
    [InlineData (0, 0, 4, 4, 5, 5, false)] // outside outer rect
    [InlineData (0, 0, 4, 4, 4, 4, false)] // outside outer rect
    [InlineData (1, 1, 10, 10, 4, 4, false)] // Inside the inner rectangle
    [InlineData (1, 1, 10, 10, 1, 1, true)] // On corner, in thickness
    [InlineData (1, 1, 10, 10, 2, 2, true)] // On corner, in thickness
    [InlineData (1, 1, 10, 10, 10, 10, true)] // On opposite corner, in thickness
    [InlineData (1, 1, 10, 10, 6, 6, false)] // Inside the inner rectangle
    [InlineData (1, 1, 10, 10, 9, 9, true)] // On opposite corner, in thickness
    [InlineData (1, 1, 10, 10, 0, 0, false)] // Outside the outer rectangle

    // Test with a rectangle that is same size as the thickness (inner is empty)
    [InlineData (-1, -1, 4, 4, -1, -1, true)] // in thickness
    [InlineData (-1, -1, 4, 4, 0, 0, true)] // in thickness
    [InlineData (-1, -1, 4, 4, 1, 1, true)] // in thickness
    [InlineData (-1, -1, 4, 4, 2, 2, true)] // in thickness
    [InlineData (-1, -1, 4, 4, 4, 4, false)] // outside outer rect
    [InlineData (-1, -1, 4, 4, 3, 3, false)] // outside outer rect
    public void TestContains_Uniform2 (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        var rect = new Rectangle (x, y, width, height);
        var thickness = new Thickness (2, 2, 2, 2); // Uniform thickness for simplicity
        bool result = thickness.Contains (rect, pointX, pointY);
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData (0, 0, 10, 10, 3, 3, false)] // Inside the inner rectangle
    [InlineData (0, 0, 10, 10, 0, 0, false)] // On corner, in thickness
    [InlineData (0, 0, 10, 10, 9, 9, false)] // On opposite corner, in thickness
    [InlineData (0, 0, 10, 10, 5, 5, false)] // Inside the inner rectangle
    [InlineData (0, 0, 10, 10, -1, -1, false)] // Outside the outer rectangle
    [InlineData (0, 0, 0, 0, 3, 3, false)] // Inside the inner rectangle
    [InlineData (0, 0, 0, 0, 0, 0, false)] // On corner, in thickness
    [InlineData (0, 0, 0, 0, 9, 9, false)] // On opposite corner, in thickness
    [InlineData (0, 0, 0, 0, 5, 5, false)] // Inside the inner rectangle
    [InlineData (0, 0, 0, 0, -1, -1, false)] // Outside the outer rectangle
    [InlineData (1, 1, 10, 10, 1, 1, false)] // On corner, in thickness
    [InlineData (1, 1, 10, 10, 10, 10, false)] // On opposite corner, in thickness
    [InlineData (1, 1, 10, 10, 6, 6, false)] // Inside the inner rectangle
    [InlineData (1, 1, 10, 10, 0, 0, false)] // Outside the outer rectangle
    [InlineData (-1, -1, 10, 10, -1, -1, false)] // On corner, in thickness
    [InlineData (-1, -1, 10, 10, 8, 8, false)] // On opposite corner, in thickness
    [InlineData (-1, -1, 10, 10, 4, 4, false)] // Inside the inner rectangle
    [InlineData (-1, -1, 10, 10, -2, -2, false)] // Outside the outer rectangle
    public void TestContains_ZeroThickness (
        int x,
        int y,
        int width,
        int height,
        int pointX,
        int pointY,
        bool expected
    )
    {
        var rect = new Rectangle (x, y, width, height);
        var thickness = new Thickness (0, 0, 0, 0); // Uniform thickness for simplicity
        bool result = thickness.Contains (rect, pointX, pointY);
        Assert.Equal (expected, result);
    }

    [Fact]
    public void ToStringTest ()
    {
        var t = new Thickness (1, 2, 3, 4);
        Assert.Equal ("(Left=1,Top=2,Right=3,Bottom=4)", t.ToString ());
    }

    [Fact]
    public void Vertical_get ()
    {
        var t = new Thickness (1, 2, 3, 4);
        Assert.Equal (6, t.Vertical);

        t = new Thickness (0);
        Assert.Equal (0, t.Vertical);
    }

    [Fact]
    public void Vertical_set ()
    {
        var t = new Thickness ();
        t.Vertical = 10;
        Assert.Equal (10, t.Vertical);
        Assert.Equal (0, t.Left);
        Assert.Equal (5, t.Top);
        Assert.Equal (0, t.Right);
        Assert.Equal (5, t.Bottom);
        Assert.Equal (0, t.Horizontal);

        t.Vertical = 11;
        Assert.Equal (10, t.Vertical);
        Assert.Equal (0, t.Left);
        Assert.Equal (5, t.Top);
        Assert.Equal (0, t.Right);
        Assert.Equal (5, t.Bottom);
        Assert.Equal (0, t.Horizontal);

        t.Vertical = 1;
        Assert.Equal (0, t.Vertical);
        Assert.Equal (0, t.Left);
        Assert.Equal (0, t.Top);
        Assert.Equal (0, t.Right);
        Assert.Equal (0, t.Bottom);
        Assert.Equal (0, t.Horizontal);
    }

    // Test Thickness.Add
    [Theory]
    [InlineData (
                    1,
                    2,
                    3,
                    4,
                    1,
                    2,
                    3,
                    4,
                    2,
                    4,
                    6,
                    8)]
    [InlineData (
                    1,
                    2,
                    3,
                    4,
                    0,
                    0,
                    0,
                    0,
                    1,
                    2,
                    3,
                    4)]
    [InlineData (
                    1,
                    2,
                    3,
                    4,
                    -1,
                    -2,
                    -3,
                    -4,
                    0,
                    0,
                    0,
                    0)]
    [InlineData (
                    1,
                    2,
                    3,
                    4,
                    1,
                    1,
                    1,
                    1,
                    2,
                    3,
                    4,
                    5)]
    [InlineData (
                    1,
                    2,
                    3,
                    4,
                    1,
                    1,
                    1,
                    1,
                    2,
                    3,
                    4,
                    5)]
    public void AddTest (
        int left,
        int top,
        int right,
        int bottom,
        int left2,
        int top2,
        int right2,
        int bottom2,
        int expectedLeft,
        int expectedTop,
        int expectedRight,
        int expectedBottom
    )
    {
        var t = new Thickness (left, top, right, bottom);
        var t2 = new Thickness (left2, top2, right2, bottom2);
        var result = t.Add (t2);
        Assert.Equal (expectedLeft, result.Left);
        Assert.Equal (expectedTop, result.Top);
        Assert.Equal (expectedRight, result.Right);
        Assert.Equal (expectedBottom, result.Bottom);
    }
}
