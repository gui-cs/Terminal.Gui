using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests;

public class LineCanvasTests
{
    private readonly ITestOutputHelper output;
    public LineCanvasTests (ITestOutputHelper output) { this.output = output; }

    [Theory]
    [AutoInitShutdown]

    // Horizontal lines with a vertical zero-length
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    "╞"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    "╡"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    "╟"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    "╢"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    "├"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    "┤"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    "╠"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    "╣"
                )]

    // Vertical lines with a horizontal zero-length
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "╥"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "╨"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╤"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╧"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "┬"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "┴"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╦"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╩"
                )]

    // Crosses (two zero-length)
    [InlineData (
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "╫"
                )]
    [InlineData (
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╪"
                )]
    [InlineData (
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "┼"
                )]
    [InlineData (
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╬"
                )]
    public void Add_2_Lines (
        int x1,
        int y1,
        int len1,
        Orientation o1,
        LineStyle s1,
        int x2,
        int y2,
        int len2,
        Orientation o2,
        LineStyle s2,
        string expected
    )
    {
        View v = GetCanvas (out LineCanvas lc);
        v.Width = 10;
        v.Height = 10;
        v.Bounds = new Rectangle (0, 0, 10, 10);

        lc.AddLine (new Point (x1, y1), len1, o1, s1);
        lc.AddLine (new Point (x2, y2), len2, o2, s2);

        TestHelpers.AssertEqual (output, expected, lc.ToString ());
    }

    [InlineData (
                    0,
                    0,
                    0,
                    0,
                    0,
                    1,
                    1
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    0,
                    0,
                    1,
                    1
                )]
    [InlineData (
                    0,
                    0,
                    2,
                    0,
                    0,
                    2,
                    2
                )]
    [InlineData (
                    0,
                    0,
                    3,
                    0,
                    0,
                    3,
                    3
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    0,
                    0,
                    1,
                    1
                )]
    [InlineData (
                    0,
                    0,
                    -2,
                    -1,
                    -1,
                    2,
                    2
                )]
    [InlineData (
                    0,
                    0,
                    -3,
                    -2,
                    -2,
                    3,
                    3
                )]
    [Theory]
    [SetupFakeDriver]
    public void Bounds_H_And_V_Lines_Both_Positive (
        int x,
        int y,
        int length,
        int expectedX,
        int expectedY,
        int expectedWidth,
        int expectedHeight
    )
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (x, y), length, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (x, y), length, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new Rectangle (expectedX, expectedY, expectedWidth, expectedHeight), canvas.Bounds);
    }

    [InlineData (
                    0,
                    0,
                    0,
                    0,
                    0,
                    1,
                    1
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    0,
                    0,
                    1,
                    1
                )]
    [InlineData (
                    0,
                    0,
                    2,
                    0,
                    0,
                    2,
                    1
                )]
    [InlineData (
                    0,
                    0,
                    3,
                    0,
                    0,
                    3,
                    1
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    0,
                    0,
                    1,
                    1
                )]
    [InlineData (
                    0,
                    0,
                    -2,
                    -1,
                    0,
                    2,
                    1
                )]
    [InlineData (
                    0,
                    0,
                    -3,
                    -2,
                    0,
                    3,
                    1
                )]
    [Theory]
    [SetupFakeDriver]
    public void Bounds_H_Line (
        int x,
        int y,
        int length,
        int expectedX,
        int expectedY,
        int expectedWidth,
        int expectedHeight
    )
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (x, y), length, Orientation.Horizontal, LineStyle.Single);

        Assert.Equal (new Rectangle (expectedX, expectedY, expectedWidth, expectedHeight), canvas.Bounds);
    }

    [Fact]
    [SetupFakeDriver]
    public void Bounds_Specific ()
    {
        // Draw at 1,1 within client area of View (i.e. leave a top and left margin of 1)
        // This proves we aren't drawing excess above
        var x = 1;
        var y = 2;
        var width = 3;
        var height = 2;

        var lc = new LineCanvas ();

        // 01230
        // ╔╡╞╗1
        // ║  ║2

        // Add a short horiz line for ╔╡
        lc.AddLine (new Point (x, y), 2, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new Rectangle (x, y, 2, 1), lc.Bounds);

        //LHS line down
        lc.AddLine (new Point (x, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new Rectangle (x, y, 2, 2), lc.Bounds);

        //Vertical line before Title, results in a ╡
        lc.AddLine (new Point (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new Rectangle (x, y, 2, 2), lc.Bounds);

        //Vertical line after Title, results in a ╞
        lc.AddLine (new Point (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new Rectangle (x, y, 3, 2), lc.Bounds);

        // remainder of top line
        lc.AddLine (new Point (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new Rectangle (x, y, 4, 2), lc.Bounds);

        //RHS line down
        lc.AddLine (new Point (x + width, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new Rectangle (x, y, 4, 2), lc.Bounds);

        TestHelpers.AssertEqual (
                                 output,
                                 @"
╔╡╞╗
║  ║",
                                 $"{Environment.NewLine}{lc}"
                                );
    }

    [Fact]
    [SetupFakeDriver]
    public void Bounds_Specific_With_Ustring ()
    {
        // Draw at 1,1 within client area of View (i.e. leave a top and left margin of 1)
        // This proves we aren't drawing excess above
        var x = 1;
        var y = 2;
        var width = 3;
        var height = 2;

        var lc = new LineCanvas ();

        // 01230
        // ╔╡╞╗1
        // ║  ║2

        // Add a short horiz line for ╔╡
        lc.AddLine (new Point (x, y), 2, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new Rectangle (x, y, 2, 1), lc.Bounds);

        //LHS line down
        lc.AddLine (new Point (x, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new Rectangle (x, y, 2, 2), lc.Bounds);

        //Vertical line before Title, results in a ╡
        lc.AddLine (new Point (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new Rectangle (x, y, 2, 2), lc.Bounds);

        //Vertical line after Title, results in a ╞
        lc.AddLine (new Point (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new Rectangle (x, y, 3, 2), lc.Bounds);

        // remainder of top line
        lc.AddLine (new Point (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new Rectangle (x, y, 4, 2), lc.Bounds);

        //RHS line down
        lc.AddLine (new Point (x + width, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new Rectangle (x, y, 4, 2), lc.Bounds);

        TestHelpers.AssertEqual (
                                 output,
                                 @"
╔╡╞╗
║  ║",
                                 $"{Environment.NewLine}{lc}"
                                );
    }

    [Fact]
    [SetupFakeDriver]
    public void Canvas_Updates_On_Changes ()
    {
        var lc = new LineCanvas ();

        Assert.Equal (Rectangle.Empty, lc.Bounds);

        lc.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Double);
        Assert.NotEqual (Rectangle.Empty, lc.Bounds);

        lc.Clear ();
        Assert.Equal (Rectangle.Empty, lc.Bounds);
    }

    [InlineData (0, 0, Orientation.Horizontal, "─")]
    [InlineData (1, 0, Orientation.Horizontal, "─")]
    [InlineData (0, 1, Orientation.Horizontal, "─")]
    [InlineData (-1, 0, Orientation.Horizontal, "─")]
    [InlineData (0, -1, Orientation.Horizontal, "─")]
    [InlineData (-1, -1, Orientation.Horizontal, "─")]
    [InlineData (0, 0, Orientation.Vertical, "│")]
    [InlineData (1, 0, Orientation.Vertical, "│")]
    [InlineData (0, 1, Orientation.Vertical, "│")]
    [InlineData (0, -1, Orientation.Vertical, "│")]
    [InlineData (-1, 0, Orientation.Vertical, "│")]
    [InlineData (-1, -1, Orientation.Vertical, "│")]
    [Theory]
    [SetupFakeDriver]
    public void Length_0_Is_1_Long (int x, int y, Orientation orientation, string expected)
    {
        var canvas = new LineCanvas ();

        // Add a line at 5, 5 that's has length of 1
        canvas.AddLine (new Point (x, y), 1, orientation, LineStyle.Single);
        TestHelpers.AssertEqual (output, $"{expected}", $"{canvas}");
    }

    // X is offset by 2
    [InlineData (0, 0, 1, Orientation.Horizontal, "─")]
    [InlineData (1, 0, 1, Orientation.Horizontal, "─")]
    [InlineData (0, 1, 1, Orientation.Horizontal, "─")]
    [InlineData (0, 0, 1, Orientation.Vertical, "│")]
    [InlineData (1, 0, 1, Orientation.Vertical, "│")]
    [InlineData (0, 1, 1, Orientation.Vertical, "│")]
    [InlineData (-1, 0, 1, Orientation.Horizontal, "─")]
    [InlineData (0, -1, 1, Orientation.Horizontal, "─")]
    [InlineData (-1, 0, 1, Orientation.Vertical, "│")]
    [InlineData (0, -1, 1, Orientation.Vertical, "│")]
    [InlineData (0, 0, -1, Orientation.Horizontal, "─")]
    [InlineData (1, 0, -1, Orientation.Horizontal, "─")]
    [InlineData (0, 1, -1, Orientation.Horizontal, "─")]
    [InlineData (0, 0, -1, Orientation.Vertical, "│")]
    [InlineData (1, 0, -1, Orientation.Vertical, "│")]
    [InlineData (0, 1, -1, Orientation.Vertical, "│")]
    [InlineData (-1, 0, -1, Orientation.Horizontal, "─")]
    [InlineData (0, -1, -1, Orientation.Horizontal, "─")]
    [InlineData (-1, 0, -1, Orientation.Vertical, "│")]
    [InlineData (0, -1, -1, Orientation.Vertical, "│")]
    [InlineData (0, 0, 2, Orientation.Horizontal, "──")]
    [InlineData (1, 0, 2, Orientation.Horizontal, "──")]
    [InlineData (0, 1, 2, Orientation.Horizontal, "──")]
    [InlineData (1, 1, 2, Orientation.Horizontal, "──")]
    [InlineData (0, 0, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (1, 0, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, 1, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (1, 1, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (-1, 0, 2, Orientation.Horizontal, "──")]
    [InlineData (0, -1, 2, Orientation.Horizontal, "──")]
    [InlineData (-1, 0, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, -1, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (-1, -1, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, 0, -2, Orientation.Horizontal, "──")]
    [InlineData (1, 0, -2, Orientation.Horizontal, "──")]
    [InlineData (0, 1, -2, Orientation.Horizontal, "──")]
    [InlineData (0, 0, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (1, 0, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, 1, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (1, 1, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (-1, 0, -2, Orientation.Horizontal, "──")]
    [InlineData (0, -1, -2, Orientation.Horizontal, "──")]
    [InlineData (-1, 0, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, -1, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (-1, -1, -2, Orientation.Vertical, "│\r\n│")]
    [Theory]
    [SetupFakeDriver]
    public void Length_n_Is_n_Long (int x, int y, int length, Orientation orientation, string expected)
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (x, y), length, orientation, LineStyle.Single);

        var result = canvas.ToString ();
        TestHelpers.AssertEqual (output, expected, result);
    }

    [Fact]
    [SetupFakeDriver]
    public void Length_Negative ()
    {
        var offset = new Point (5, 5);

        var canvas = new LineCanvas ();
        canvas.AddLine (offset, -3, Orientation.Horizontal, LineStyle.Single);

        var looksLike = "───";

        Assert.Equal (looksLike, $"{canvas}");
    }

    [InlineData (Orientation.Horizontal, "─")]
    [InlineData (Orientation.Vertical, "│")]
    [Theory]
    [SetupFakeDriver]
    public void Length_Zero_Alone_Is_Line (Orientation orientation, string expected)
    {
        var lc = new LineCanvas ();

        // Add a line at 0, 0 that's has length of 0
        lc.AddLine (Point.Empty, 0, orientation, LineStyle.Single);
        TestHelpers.AssertEqual (output, expected, $"{lc}");
    }

    [InlineData (Orientation.Horizontal, "┼")]
    [InlineData (Orientation.Vertical, "┼")]
    [Theory]
    [SetupFakeDriver]
    public void Length_Zero_Cross_Is_Cross (Orientation orientation, string expected)
    {
        var lc = new LineCanvas ();

        // Add point at opposite orientation
        lc.AddLine (
                    Point.Empty,
                    0,
                    orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal,
                    LineStyle.Single
                   );

        // Add a line at 0, 0 that's has length of 0
        lc.AddLine (Point.Empty, 0, orientation, LineStyle.Single);
        TestHelpers.AssertEqual (output, expected, $"{lc}");
    }

    [InlineData (Orientation.Horizontal, "╥")]
    [InlineData (Orientation.Vertical, "╞")]
    [Theory]
    [SetupFakeDriver]
    public void Length_Zero_NextTo_Opposite_Is_T (Orientation orientation, string expected)
    {
        var lc = new LineCanvas ();

        // Add line with length of 1 in opposite orientation starting at same location
        if (orientation == Orientation.Horizontal)
        {
            lc.AddLine (Point.Empty, 1, Orientation.Vertical, LineStyle.Double);
        }
        else
        {
            lc.AddLine (Point.Empty, 1, Orientation.Horizontal, LineStyle.Double);
        }

        // Add a line at 0, 0 that's has length of 0
        lc.AddLine (Point.Empty, 0, orientation, LineStyle.Single);
        TestHelpers.AssertEqual (output, expected, $"{lc}");
    }

    [Fact]
    [AutoInitShutdown]
    public void TestLineCanvas_LeaveMargin_Top1_Left1 ()
    {
        var canvas = new LineCanvas ();

        // Upper box
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, LineStyle.Single);

        var looksLike =
            @"
┌─
│ ";
        TestHelpers.AssertEqual (output, looksLike, $"{Environment.NewLine}{canvas}");
    }

    [Fact]
    [AutoInitShutdown]
    public void TestLineCanvas_Window_Heavy ()
    {
        View v = GetCanvas (out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new Point (9, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new Point (9, 4), -10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new Point (0, 4), -5, Orientation.Vertical, LineStyle.Heavy);

        canvas.AddLine (new Point (5, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new Point (0, 2), 10, Orientation.Horizontal, LineStyle.Heavy);

        v.Draw ();

        var looksLike =
            @"    
┏━━━━┳━━━┓
┃    ┃   ┃
┣━━━━╋━━━┫
┃    ┃   ┃
┗━━━━┻━━━┛";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void TestLineCanvas_Window_HeavyTop_ThinSides (LineStyle thinStyle)
    {
        View v = GetCanvas (out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new Point (9, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new Point (9, 4), -10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new Point (0, 4), -5, Orientation.Vertical, thinStyle);

        canvas.AddLine (new Point (5, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new Point (0, 2), 10, Orientation.Horizontal, LineStyle.Heavy);

        v.Draw ();

        var looksLike =
            @"    
┍━━━━┯━━━┑
│    │   │
┝━━━━┿━━━┥
│    │   │
┕━━━━┷━━━┙
";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void TestLineCanvas_Window_ThinTop_HeavySides (LineStyle thinStyle)
    {
        View v = GetCanvas (out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new Point (9, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new Point (9, 4), -10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new Point (0, 4), -5, Orientation.Vertical, LineStyle.Heavy);

        canvas.AddLine (new Point (5, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new Point (0, 2), 10, Orientation.Horizontal, thinStyle);

        v.Draw ();

        var looksLike =
            @"    
┎────┰───┒
┃    ┃   ┃
┠────╂───┨
┃    ┃   ┃
┖────┸───┚

";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Top_Left_From_TopRight_LeftUp ()
    {
        var canvas = new LineCanvas ();

        // Upper box
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 1), -2, Orientation.Vertical, LineStyle.Single);

        var looksLike =
            @"
┌─
│ ";
        TestHelpers.AssertEqual (output, looksLike, $"{Environment.NewLine}{canvas}");
    }

    [Fact]
    [SetupFakeDriver]
    public void Top_With_1Down ()
    {
        var canvas = new LineCanvas ();

        // Top      ─  
        canvas.AddLine (Point.Empty, 1, Orientation.Horizontal, LineStyle.Single);

        // Bottom   ─
        canvas.AddLine (new Point (1, 1), -1, Orientation.Horizontal, LineStyle.Single);

        //// Right down
        //canvas.AddLine (new Point (9, 0), 3, Orientation.Vertical, LineStyle.Single);

        //// Bottom
        //canvas.AddLine (new Point (9, 3), -10, Orientation.Horizontal, LineStyle.Single);

        //// Left Up
        //canvas.AddLine (new Point (0, 3), -3, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new Rectangle (0, 0, 2, 2), canvas.Bounds);

        Dictionary<Point, Rune> map = canvas.GetMap ();
        Assert.Equal (2, map.Count);

        TestHelpers.AssertEqual (
                                 output,
                                 @"
─ 
 ─",
                                 $"{Environment.NewLine}{canvas}"
                                );
    }

    [Fact]
    [SetupFakeDriver]
    public void ToString_Empty ()
    {
        var lc = new LineCanvas ();
        TestHelpers.AssertEqual (output, string.Empty, lc.ToString ());
    }

    //                  012
    [InlineData (0, 0, "═══")]
    [InlineData (1, 0, "═══")]
    [InlineData (0, 1, "═══")]
    [InlineData (1, 1, "═══")]
    [InlineData (2, 2, "═══")]
    [InlineData (-1, 0, "═══")]
    [InlineData (0, -1, "═══")]
    [InlineData (-1, -1, "═══")]
    [InlineData (-2, -2, "═══")]
    [Theory]
    [SetupFakeDriver]
    public void ToString_Positive_Horizontal_1Line_Offset (int x, int y, string expected)
    {
        var lc = new LineCanvas ();
        lc.AddLine (new Point (x, y), 3, Orientation.Horizontal, LineStyle.Double);
        TestHelpers.AssertEqual (output, expected, $"{lc}");
    }

    [InlineData (0, 0, 0, 0, "═══")]
    [InlineData (1, 0, 1, 0, "═══")]
    [InlineData (-1, 0, -1, 0, "═══")]
    [InlineData (0, 0, 1, 0, "════")]
    [InlineData (1, 0, 3, 0, "═════")]
    [InlineData (1, 0, 4, 0, "══════")]
    [InlineData (1, 0, 5, 0, "═══ ═══")]
    [InlineData (0, 0, 0, 1, "\u2550\u2550\u2550\r\n\u2550\u2550\u2550")]
    [InlineData (0, 0, 1, 1, "═══ \r\n ═══")]
    [InlineData (0, 0, 2, 1, "═══  \r\n  ═══")]
    [InlineData (1, 0, 0, 1, " ═══\r\n═══ ")]
    [InlineData (0, 1, 0, 1, "═══")]
    [InlineData (1, 1, 0, 1, "════")]
    [InlineData (2, 2, 0, 1, "═══  \r\n  ═══")]
    [Theory]
    [SetupFakeDriver]
    public void ToString_Positive_Horizontal_2Line_Offset (int x1, int y1, int x2, int y2, string expected)
    {
        var lc = new LineCanvas ();
        lc.AddLine (new Point (x1, y1), 3, Orientation.Horizontal, LineStyle.Double);
        lc.AddLine (new Point (x2, y2), 3, Orientation.Horizontal, LineStyle.Double);

        TestHelpers.AssertEqual (output, expected, $"{lc}");
    }

    //		[Fact, SetupFakeDriver]
    //		public void LeaveMargin_Top1_Left1 ()
    //		{
    //			var canvas = new LineCanvas ();

    //			// Upper box
    //			canvas.AddLine (Point.Empty, 9, Orientation.Horizontal, LineStyle.Single);
    //			canvas.AddLine (new Point (8, 0), 3, Orientation.Vertical, LineStyle.Single);
    //			canvas.AddLine (new Point (8, 3), -9, Orientation.Horizontal, LineStyle.Single);
    //			canvas.AddLine (new Point (0, 2), -3, Orientation.Vertical, LineStyle.Single);

    //			// Lower Box
    //			canvas.AddLine (new Point (5, 0), 2, Orientation.Vertical, LineStyle.Single);
    //			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, LineStyle.Single);

    //			string looksLike =
    //@"
    //┌────┬──┐
    //│    │  │
    //├────┼──┤
    //└────┴──┘
    //";
    //			Assert.Equal (looksLike, $"{Environment.NewLine}{canvas}");
    //		}

    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Double, "═")]
    [InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Double, "║")]
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Single, "─")]
    [InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Single, "│")]
    [InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Double, "═")]
    [InlineData (0, 0, 1, Orientation.Vertical, LineStyle.Double, "║")]
    [InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Single, "─")]
    [InlineData (0, 0, 1, Orientation.Vertical, LineStyle.Single, "│")]
    [InlineData (0, 0, 2, Orientation.Horizontal, LineStyle.Double, "══")]
    [InlineData (0, 0, 2, Orientation.Vertical, LineStyle.Double, "║\n║")]
    [InlineData (0, 0, 2, Orientation.Horizontal, LineStyle.Single, "──")]
    [InlineData (0, 0, 2, Orientation.Vertical, LineStyle.Single, "│\n│")]
    [AutoInitShutdown]
    [Theory]
    public void View_Draws_1LineTests (
        int x1,
        int y1,
        int length,
        Orientation o1,
        LineStyle s1,
        string expected
    )
    {
        View v = GetCanvas (out LineCanvas lc);
        v.Width = 10;
        v.Height = 10;
        v.Bounds = new Rectangle (0, 0, 10, 10);

        lc.AddLine (new Point (x1, y1), length, o1, s1);

        v.Draw ();

        TestHelpers.AssertDriverContentsAre (expected, output);
    }

    /// <summary>This test demonstrates how to correctly trigger a corner.  By overlapping the lines in the same cell</summary>
    [Fact]
    [AutoInitShutdown]
    public void View_Draws_Corner_Correct ()
    {
        View v = GetCanvas (out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, LineStyle.Single);

        v.Draw ();

        var looksLike =
            @"    
┌─
│";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    /// <summary>
    ///     This test demonstrates that corners are only drawn when lines overlap. Not when they terminate adjacent to one
    ///     another.
    /// </summary>
    [Fact]
    [AutoInitShutdown]
    public void View_Draws_Corner_NoOverlap ()
    {
        View v = GetCanvas (out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 1), 2, Orientation.Vertical, LineStyle.Single);

        v.Draw ();

        var looksLike =
            @"    
──
│
│";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    [Theory]
    [AutoInitShutdown]
    public void View_Draws_Horizontal (LineStyle style)
    {
        View v = GetCanvas (out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, style);

        v.Draw ();

        var looksLike =
            @"    
──";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void View_Draws_Horizontal_Double ()
    {
        View v = GetCanvas (out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @" 
══";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    [Theory]
    [AutoInitShutdown]
    public void View_Draws_Vertical (LineStyle style)
    {
        View v = GetCanvas (out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, style);

        v.Draw ();

        var looksLike =
            @"    
│
│";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void View_Draws_Vertical_Double ()
    {
        View v = GetCanvas (out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @"    
║
║";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void View_Draws_Window_Double ()
    {
        View v = GetCanvas (out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new Point (9, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new Point (9, 4), -10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new Point (0, 4), -5, Orientation.Vertical, LineStyle.Double);

        canvas.AddLine (new Point (5, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new Point (0, 2), 10, Orientation.Horizontal, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @"    
╔════╦═══╗
║    ║   ║
╠════╬═══╣
║    ║   ║
╚════╩═══╝";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void View_Draws_Window_DoubleTop_SingleSides (LineStyle thinStyle)
    {
        View v = GetCanvas (out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new Point (9, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new Point (9, 4), -10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new Point (0, 4), -5, Orientation.Vertical, thinStyle);

        canvas.AddLine (new Point (5, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new Point (0, 2), 10, Orientation.Horizontal, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @"    
╒════╤═══╕
│    │   │
╞════╪═══╡
│    │   │
╘════╧═══╛
";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    /// <summary>
    ///     Demonstrates when <see cref="LineStyle.Rounded"/> corners are used. Notice how not all lines declare rounded.
    ///     If there are 1+ lines intersecting and a corner is to be used then if any of them are rounded a rounded corner is
    ///     used.
    /// </summary>
    [Fact]
    [AutoInitShutdown]
    public void View_Draws_Window_Rounded ()
    {
        View v = GetCanvas (out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Rounded);

        // LineStyle.Single is ignored because corner overlaps with the above line which is Rounded
        // this results in a rounded corner being used.
        canvas.AddLine (new Point (9, 0), 5, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new Point (9, 4), -10, Orientation.Horizontal, LineStyle.Rounded);
        canvas.AddLine (new Point (0, 4), -5, Orientation.Vertical, LineStyle.Single);

        // These lines say rounded but they will result in the T sections which are never rounded.
        canvas.AddLine (new Point (5, 0), 5, Orientation.Vertical, LineStyle.Rounded);
        canvas.AddLine (new Point (0, 2), 10, Orientation.Horizontal, LineStyle.Rounded);

        v.Draw ();

        var looksLike =
            @"    
╭────┬───╮
│    │   │
├────┼───┤
│    │   │
╰────┴───╯";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void View_Draws_Window_SingleTop_DoubleSides (LineStyle thinStyle)
    {
        View v = GetCanvas (out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new Point (9, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new Point (9, 4), -10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new Point (0, 4), -5, Orientation.Vertical, LineStyle.Double);

        canvas.AddLine (new Point (5, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new Point (0, 2), 10, Orientation.Horizontal, thinStyle);

        v.Draw ();

        var looksLike =
            @"    
╓────╥───╖
║    ║   ║
╟────╫───╢
║    ║   ║
╙────╨───╜

";
        TestHelpers.AssertDriverContentsAre (looksLike, output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Window ()
    {
        var canvas = new LineCanvas ();

        // Frame
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (9, 0), 5, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new Point (9, 4), -10, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 4), -5, Orientation.Vertical, LineStyle.Single);

        // Cross
        canvas.AddLine (new Point (5, 0), 5, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new Point (0, 2), 10, Orientation.Horizontal, LineStyle.Single);

        var looksLike =
            @"
┌────┬───┐
│    │   │
├────┼───┤
│    │   │
└────┴───┘";
        TestHelpers.AssertEqual (output, looksLike, $"{Environment.NewLine}{canvas}");
    }

    [Fact]
    [SetupFakeDriver]
    public void Zero_Length_Intersections ()
    {
        // Draw at 1,2 within client area of View (i.e. leave a top and left margin of 1)
        // This proves we aren't drawing excess above
        var x = 1;
        var y = 2;
        var width = 5;
        var height = 2;

        var lc = new LineCanvas ();

        // ╔╡╞═════╗
        // Add a short horiz line for ╔╡
        lc.AddLine (new Point (x, y), 2, Orientation.Horizontal, LineStyle.Double);

        //LHS line down
        lc.AddLine (new Point (x, y), height, Orientation.Vertical, LineStyle.Double);

        //Vertical line before Title, results in a ╡
        lc.AddLine (new Point (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);

        //Vertical line after Title, results in a ╞
        lc.AddLine (new Point (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);

        // remainder of top line
        lc.AddLine (new Point (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);

        //RHS line down
        lc.AddLine (new Point (x + width, y), height, Orientation.Vertical, LineStyle.Double);

        var looksLike = @"
╔╡╞══╗
║    ║";
        TestHelpers.AssertEqual (output, looksLike, $"{Environment.NewLine}{lc}");
    }

    // TODO: Remove this and make all LineCanvas tests independent of View
    /// <summary>
    ///     Creates a new <see cref="View"/> into which a <see cref="LineCanvas"/> is rendered at
    ///     <see cref="View.DrawContentComplete"/> time.
    /// </summary>
    /// <param name="canvas">The <see cref="LineCanvas"/> you can draw into.</param>
    /// <param name="offsetX">How far to offset drawing in X</param>
    /// <param name="offsetY">How far to offset drawing in Y</param>
    /// <returns></returns>
    private View GetCanvas (out LineCanvas canvas, int offsetX = 0, int offsetY = 0)
    {
        var v = new View { Width = 10, Height = 5, Bounds = new Rectangle (0, 0, 10, 5) };
        var top = new Toplevel ();
        top.Add (v);
        Application.Begin (top);

        LineCanvas canvasCopy = canvas = new LineCanvas ();

        v.DrawContentComplete += (s, e) =>
                                 {
                                     v.Clear (v.Bounds);

                                     foreach (KeyValuePair<Point, Rune> p in canvasCopy.GetMap ())
                                     {
                                         v.AddRune (
                                                    offsetX + p.Key.X,
                                                    offsetY + p.Key.Y,
                                                    p.Value
                                                   );
                                     }

                                     canvasCopy.Clear ();
                                 };

        return v;
    }
}
