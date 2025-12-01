using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace DrawingTests;

/// <summary>
///     Pure unit tests for <see cref="LineCanvas"/> that don't require Application.Driver or View context.
///     These tests focus on properties and behavior that don't depend on glyph rendering.
///     Note: Tests that verify rendered output (ToString()) cannot be parallelized because LineCanvas
///     depends on Application.Driver for glyph resolution and configuration. Those tests remain in UnitTests.
/// </summary>
public class LineCanvasTests (ITestOutputHelper output) : FakeDriverBase
{
    #region Basic API Tests

    [Fact]
    public void Empty_Canvas_ToString_Returns_EmptyString ()
    {
        var canvas = new LineCanvas ();
        Assert.Equal (string.Empty, canvas.ToString ());
    }

    [Fact]
    public void Clear_Removes_All_Lines ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 0), 3, Orientation.Vertical, LineStyle.Single);

        canvas.Clear ();

        Assert.Empty (canvas.Lines);
        Assert.Equal (Rectangle.Empty, canvas.Bounds);
        Assert.Equal (string.Empty, canvas.ToString ());
    }

    [Fact]
    public void Lines_Property_Returns_ReadOnly_Collection ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);

        Assert.Single (canvas.Lines);
        Assert.IsAssignableFrom<IReadOnlyCollection<StraightLine>> (canvas.Lines);
    }

    [Fact]
    public void AddLine_Adds_Line_To_Collection ()
    {
        var canvas = new LineCanvas ();
        Assert.Empty (canvas.Lines);

        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        Assert.Single (canvas.Lines);

        canvas.AddLine (new (0, 0), 3, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (2, canvas.Lines.Count);
    }

    [Fact]
    public void Constructor_With_Lines_Creates_Canvas_With_Lines ()
    {
        StraightLine [] lines = new []
        {
            new StraightLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single),
            new StraightLine (new (0, 0), 3, Orientation.Vertical, LineStyle.Single)
        };

        var canvas = new LineCanvas (lines);

        Assert.Equal (2, canvas.Lines.Count);
    }

    #endregion

    #region Bounds Tests - Tests for Bounds property

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 1, 1)]
    [InlineData (0, 0, 1, 0, 0, 1, 1)]
    [InlineData (0, 0, 2, 0, 0, 2, 2)]
    [InlineData (0, 0, 3, 0, 0, 3, 3)]
    [InlineData (0, 0, -1, 0, 0, 1, 1)]
    [InlineData (0, 0, -2, -1, -1, 2, 2)]
    [InlineData (0, 0, -3, -2, -2, 3, 3)]
    public void Viewport_H_And_V_Lines_Both_Positive (
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
        canvas.AddLine (new (x, y), length, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (x, y), length, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new (expectedX, expectedY, expectedWidth, expectedHeight), canvas.Bounds);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 1, 1)]
    [InlineData (0, 0, 1, 0, 0, 1, 1)]
    [InlineData (0, 0, 2, 0, 0, 2, 1)]
    [InlineData (0, 0, 3, 0, 0, 3, 1)]
    [InlineData (0, 0, -1, 0, 0, 1, 1)]
    [InlineData (0, 0, -2, -1, 0, 2, 1)]
    [InlineData (0, 0, -3, -2, 0, 3, 1)]
    public void Viewport_H_Line (
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
        canvas.AddLine (new (x, y), length, Orientation.Horizontal, LineStyle.Single);

        Assert.Equal (new (expectedX, expectedY, expectedWidth, expectedHeight), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Specific_Coordinates ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (5, 5), 3, Orientation.Horizontal, LineStyle.Single);
        Assert.Equal (new (5, 5, 3, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Empty_Canvas_Returns_Empty_Rectangle ()
    {
        var canvas = new LineCanvas ();
        Assert.Equal (Rectangle.Empty, canvas.Bounds);
    }

    [Fact]
    public void Bounds_Single_Point_Zero_Length ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (5, 5), 0, Orientation.Horizontal, LineStyle.Single);

        Assert.Equal (new (5, 5, 1, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Horizontal_Line ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (2, 3), 5, Orientation.Horizontal, LineStyle.Single);

        Assert.Equal (new (2, 3, 5, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Vertical_Line ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (2, 3), 5, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new (2, 3, 1, 5), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Multiple_Lines_Returns_Union ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 0), 3, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new (0, 0, 5, 3), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Negative_Length_Line ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (5, 5), -3, Orientation.Horizontal, LineStyle.Single);

        // Line from (5,5) going left 3 positions: includes points 3, 4, 5 (width 3, X starts at 3)
        Assert.Equal (new (3, 5, 3, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Complex_Box ()
    {
        var canvas = new LineCanvas ();

        // top
        canvas.AddLine (new (0, 0), 3, Orientation.Horizontal, LineStyle.Single);

        // left
        canvas.AddLine (new (0, 0), 2, Orientation.Vertical, LineStyle.Single);

        // right
        canvas.AddLine (new (2, 0), 2, Orientation.Vertical, LineStyle.Single);

        // bottom
        canvas.AddLine (new (0, 2), 3, Orientation.Horizontal, LineStyle.Single);

        Assert.Equal (new (0, 0, 3, 3), canvas.Bounds);
    }

    #endregion

    #region Exclusion Tests

    [Fact]
    public void ClearExclusions_Clears_Exclusion_Region ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);

        var region = new Region (new (0, 0, 2, 1));
        canvas.Exclude (region);
        canvas.ClearExclusions ();

        // After clearing exclusions, GetMap should return all points
        Dictionary<Point, Rune> map = canvas.GetMap ();
        Assert.Equal (5, map.Count);
    }

    [Fact]
    public void Exclude_Removes_Points_From_Map ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);

        var region = new Region (new (0, 0, 2, 1));
        canvas.Exclude (region);

        Dictionary<Point, Rune> map = canvas.GetMap ();

        // Should have 5 - 2 = 3 points (excluding the first 2)
        Assert.Equal (3, map.Count);
    }

    #endregion

    #region Fill Property Tests

    [Fact]
    public void Fill_Property_Can_Be_Set ()
    {
        var foregroundFill = new SolidFill (new (255, 0));
        var backgroundFill = new SolidFill (new (0, 0));
        var fillPair = new FillPair (foregroundFill, backgroundFill);

        var canvas = new LineCanvas { Fill = fillPair };

        Assert.Equal (fillPair, canvas.Fill);
    }

    [Fact]
    public void Fill_Property_Defaults_To_Null ()
    {
        var canvas = new LineCanvas ();
        Assert.Null (canvas.Fill);
    }

    #endregion

    [Theory]

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
        IDriver driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas lc);
        v.Width = 10;
        v.Height = 10;
        v.Viewport = new (0, 0, 10, 10);

        lc.AddLine (new (x1, y1), len1, o1, s1);
        lc.AddLine (new (x2, y2), len2, o2, s2);

        OutputAssert.AssertEqual (output, expected, lc.ToString ());
        v.Dispose ();
    }


    [Fact]
    public void Viewport_Specific ()
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
        lc.AddLine (new (x, y), 2, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new (x, y, 2, 1), lc.Bounds);

        //LHS line down
        lc.AddLine (new (x, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new (x, y, 2, 2), lc.Bounds);

        //Vertical line before Title, results in a ╡
        lc.AddLine (new (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new (x, y, 2, 2), lc.Bounds);

        //Vertical line after Title, results in a ╞
        lc.AddLine (new (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new (x, y, 3, 2), lc.Bounds);

        // remainder of top line
        lc.AddLine (new (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new (x, y, 4, 2), lc.Bounds);

        //RHS line down
        lc.AddLine (new (x + width, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new (x, y, 4, 2), lc.Bounds);

        OutputAssert.AssertEqual (
                                  output,
                                  @"
╔╡╞╗
║  ║",
                                  $"{Environment.NewLine}{lc}"
                                 );
    }

    [Fact]
    public void Viewport_Specific_With_Ustring ()
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
        lc.AddLine (new (x, y), 2, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new (x, y, 2, 1), lc.Bounds);

        //LHS line down
        lc.AddLine (new (x, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new (x, y, 2, 2), lc.Bounds);

        //Vertical line before Title, results in a ╡
        lc.AddLine (new (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new (x, y, 2, 2), lc.Bounds);

        //Vertical line after Title, results in a ╞
        lc.AddLine (new (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new (x, y, 3, 2), lc.Bounds);

        // remainder of top line
        lc.AddLine (new (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new (x, y, 4, 2), lc.Bounds);

        //RHS line down
        lc.AddLine (new (x + width, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new (x, y, 4, 2), lc.Bounds);

        OutputAssert.AssertEqual (
                                  output,
                                  @"
╔╡╞╗
║  ║",
                                  $"{Environment.NewLine}{lc}"
                                 );
    }

    [Fact]
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
    public void Length_0_Is_1_Long (int x, int y, Orientation orientation, string expected)
    {
        var canvas = new LineCanvas ();

        // Add a line at 5, 5 that's has length of 1
        canvas.AddLine (new (x, y), 1, orientation, LineStyle.Single);
        OutputAssert.AssertEqual (output, $"{expected}", $"{canvas}");
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
    [Theory]    public void Length_n_Is_n_Long (int x, int y, int length, Orientation orientation, string expected)
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (x, y), length, orientation, LineStyle.Single);

        var result = canvas.ToString ();
        OutputAssert.AssertEqual (output, expected, result);
    }

    [Fact]
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
    public void Length_Zero_Alone_Is_Line (Orientation orientation, string expected)
    {
        var lc = new LineCanvas ();

        // Add a line at 0, 0 that's has length of 0
        lc.AddLine (Point.Empty, 0, orientation, LineStyle.Single);
        OutputAssert.AssertEqual (output, expected, $"{lc}");
    }

    [InlineData (Orientation.Horizontal, "┼")]
    [InlineData (Orientation.Vertical, "┼")]
    [Theory]
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
        OutputAssert.AssertEqual (output, expected, $"{lc}");
    }

    [InlineData (Orientation.Horizontal, "╥")]
    [InlineData (Orientation.Vertical, "╞")]
    [Theory]
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
        OutputAssert.AssertEqual (output, expected, $"{lc}");
    }

    [Fact]
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
        OutputAssert.AssertEqual (output, looksLike, $"{Environment.NewLine}{canvas}");
    }

    [Fact]
    public void TestLineCanvas_Window_Heavy ()
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Heavy);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Heavy);

        v.Draw ();

        var looksLike =
            @"    
┏━━━━┳━━━┓
┃    ┃   ┃
┣━━━━╋━━━┫
┃    ┃   ┃
┗━━━━┻━━━┛";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void TestLineCanvas_Window_HeavyTop_ThinSides (LineStyle thinStyle)
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, thinStyle);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Heavy);

        v.Draw ();

        var looksLike =
            @"    
┍━━━━┯━━━┑
│    │   │
┝━━━━┿━━━┥
│    │   │
┕━━━━┷━━━┙
";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void TestLineCanvas_Window_ThinTop_HeavySides (LineStyle thinStyle)
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Heavy);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, thinStyle);

        v.Draw ();

        var looksLike =
            @"    
┎────┰───┒
┃    ┃   ┃
┠────╂───┨
┃    ┃   ┃
┖────┸───┚

";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]
    public void Top_Left_From_TopRight_LeftUp ()
    {
        var canvas = new LineCanvas ();

        // Upper box
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 1), -2, Orientation.Vertical, LineStyle.Single);

        var looksLike =
            @"
┌─
│ ";
        OutputAssert.AssertEqual (output, looksLike, $"{Environment.NewLine}{canvas}");
    }

    [Fact]
    public void Top_With_1Down ()
    {
        var canvas = new LineCanvas ();

        // Top      ─  
        canvas.AddLine (Point.Empty, 1, Orientation.Horizontal, LineStyle.Single);

        // Bottom   ─
        canvas.AddLine (new (1, 1), -1, Orientation.Horizontal, LineStyle.Single);

        //// Right down
        //canvas.AddLine (new Point (9, 0), 3, Orientation.Vertical, LineStyle.Single);

        //// Bottom
        //canvas.AddLine (new Point (9, 3), -10, Orientation.Horizontal, LineStyle.Single);

        //// Left Up
        //canvas.AddLine (new Point (0, 3), -3, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new (0, 0, 2, 2), canvas.Bounds);

        Dictionary<Point, Rune> map = canvas.GetMap ();
        Assert.Equal (2, map.Count);

        OutputAssert.AssertEqual (
                                  output,
                                  @"
─ 
 ─",
                                  $"{Environment.NewLine}{canvas}"
                                 );
    }

    [Fact]
    public void ToString_Empty ()
    {
        var lc = new LineCanvas ();
        OutputAssert.AssertEqual (output, string.Empty, lc.ToString ());
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
    public void ToString_Positive_Horizontal_1Line_Offset (int x, int y, string expected)
    {
        var lc = new LineCanvas ();
        lc.AddLine (new (x, y), 3, Orientation.Horizontal, LineStyle.Double);
        OutputAssert.AssertEqual (output, expected, $"{lc}");
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
    public void ToString_Positive_Horizontal_2Line_Offset (int x1, int y1, int x2, int y2, string expected)
    {
        var lc = new LineCanvas ();
        lc.AddLine (new (x1, y1), 3, Orientation.Horizontal, LineStyle.Double);
        lc.AddLine (new (x2, y2), 3, Orientation.Horizontal, LineStyle.Double);

        OutputAssert.AssertEqual (output, expected, $"{lc}");
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
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas lc);
        v.Width = 10;
        v.Height = 10;
        v.Viewport = new (0, 0, 10, 10);

        lc.AddLine (new (x1, y1), length, o1, s1);

        v.Draw ();

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
        v.Dispose ();
    }

    /// <summary>This test demonstrates how to correctly trigger a corner.  By overlapping the lines in the same cell</summary>
    [Fact]
    public void View_Draws_Corner_Correct ()
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, LineStyle.Single);

        v.Draw ();

        var looksLike =
            @"    
┌─
│";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    /// <summary>
    ///     This test demonstrates that corners are only drawn when lines overlap. Not when they terminate adjacent to one
    ///     another.
    /// </summary>
    [Fact]
    public void View_Draws_Corner_NoOverlap ()
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 1), 2, Orientation.Vertical, LineStyle.Single);

        v.Draw ();

        var looksLike =
            @"    
──
│
│";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    [Theory]
    public void View_Draws_Horizontal (LineStyle style)
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, style);

        v.Draw ();

        var looksLike =
            @"    
──";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]
    public void View_Draws_Horizontal_Double ()
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @" 
══";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    [Theory]
    public void View_Draws_Vertical (LineStyle style)
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, style);

        v.Draw ();

        var looksLike =
            @"    
│
│";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]

    public void View_Draws_Vertical_Double ()
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @"    
║
║";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]
    public void View_Draws_Window_Double ()
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Double);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @"    
╔════╦═══╗
║    ║   ║
╠════╬═══╣
║    ║   ║
╚════╩═══╝";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void View_Draws_Window_DoubleTop_SingleSides (LineStyle thinStyle)
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, thinStyle);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @"    
╒════╤═══╕
│    │   │
╞════╪═══╡
│    │   │
╘════╧═══╛
";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    /// <summary>
    ///     Demonstrates when <see cref="LineStyle.Rounded"/> corners are used. Notice how not all lines declare rounded.
    ///     If there are 1+ lines intersecting and a corner is to be used then if any of them are rounded a rounded corner is
    ///     used.
    /// </summary>
    [Fact]
    public void View_Draws_Window_Rounded ()
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Rounded);

        // LineStyle.Single is ignored because corner overlaps with the above line which is Rounded
        // this results in a rounded corner being used.
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Rounded);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Single);

        // These lines say rounded but they will result in the T sections which are never rounded.
        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Rounded);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Rounded);

        v.Draw ();

        var looksLike =
            @"    
╭────┬───╮
│    │   │
├────┼───┤
│    │   │
╰────┴───╯";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void View_Draws_Window_SingleTop_DoubleSides (LineStyle thinStyle)
    {
        var driver = CreateFakeDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Double);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, thinStyle);

        v.Draw ();

        var looksLike =
            @"    
╓────╥───╖
║    ║   ║
╟────╫───╢
║    ║   ║
╙────╨───╜

";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]
    public void Window ()
    {
        var canvas = new LineCanvas ();

        // Frame
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Single);

        // Cross
        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Single);

        var looksLike =
            @"
┌────┬───┐
│    │   │
├────┼───┤
│    │   │
└────┴───┘";
        OutputAssert.AssertEqual (output, looksLike, $"{Environment.NewLine}{canvas}");
    }

    [Fact]
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
        lc.AddLine (new (x, y), 2, Orientation.Horizontal, LineStyle.Double);

        //LHS line down
        lc.AddLine (new (x, y), height, Orientation.Vertical, LineStyle.Double);

        //Vertical line before Title, results in a ╡
        lc.AddLine (new (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);

        //Vertical line after Title, results in a ╞
        lc.AddLine (new (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);

        // remainder of top line
        lc.AddLine (new (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);

        //RHS line down
        lc.AddLine (new (x + width, y), height, Orientation.Vertical, LineStyle.Double);

        var looksLike = @"
╔╡╞══╗
║    ║";
        OutputAssert.AssertEqual (output, looksLike, $"{Environment.NewLine}{lc}");
    }

    [Fact]
    public void LineCanvas_UsesFillCorrectly ()
    {
        // Arrange
        var foregroundColor = new Color (255, 0); // Red
        var backgroundColor = new Color (0, 0); // Black
        var foregroundFill = new SolidFill (foregroundColor);
        var backgroundFill = new SolidFill (backgroundColor);
        var fillPair = new FillPair (foregroundFill, backgroundFill);

        var lineCanvas = new LineCanvas
        {
            Fill = fillPair
        };

        // Act
        lineCanvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        Dictionary<Point, Cell?> cellMap = lineCanvas.GetCellMap ();

        // Assert
        foreach (Cell? cell in cellMap.Values)
        {
            Assert.NotNull (cell);
            Assert.Equal (foregroundColor, cell.Value.Attribute!.Value.Foreground);
            Assert.Equal (backgroundColor, cell.Value.Attribute.Value.Background);
        }
    }

    [Fact]
    public void LineCanvas_LineColorIgnoredBecauseOfFill ()
    {
        // Arrange
        var foregroundColor = new Color (255, 0); // Red
        var backgroundColor = new Color (0, 0); // Black
        var lineColor = new Attribute (new Color (0, 255), new Color (255, 255, 255)); // Green on White
        var foregroundFill = new SolidFill (foregroundColor);
        var backgroundFill = new SolidFill (backgroundColor);
        var fillPair = new FillPair (foregroundFill, backgroundFill);

        var lineCanvas = new LineCanvas
        {
            Fill = fillPair
        };

        // Act
        lineCanvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single, lineColor);
        Dictionary<Point, Cell?> cellMap = lineCanvas.GetCellMap ();

        // Assert
        foreach (Cell? cell in cellMap.Values)
        {
            Assert.NotNull (cell);
            Assert.Equal (foregroundColor, cell.Value.Attribute!.Value.Foreground);
            Assert.Equal (backgroundColor, cell.Value.Attribute.Value.Background);
        }
    }

    [Fact]
    public void LineCanvas_IntersectingLinesUseFillCorrectly ()
    {
        // Arrange
        var foregroundColor = new Color (255, 0); // Red
        var backgroundColor = new Color (0, 0); // Black
        var foregroundFill = new SolidFill (foregroundColor);
        var backgroundFill = new SolidFill (backgroundColor);
        var fillPair = new FillPair (foregroundFill, backgroundFill);

        var lineCanvas = new LineCanvas
        {
            Fill = fillPair
        };

        // Act
        lineCanvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        lineCanvas.AddLine (new (2, -2), 5, Orientation.Vertical, LineStyle.Single);
        Dictionary<Point, Cell?> cellMap = lineCanvas.GetCellMap ();

        // Assert
        foreach (Cell? cell in cellMap.Values)
        {
            Assert.NotNull (cell);
            Assert.Equal (foregroundColor, cell.Value.Attribute!.Value.Foreground);
            Assert.Equal (backgroundColor, cell.Value.Attribute.Value.Background);
        }
    }

    // TODO: Remove this and make all LineCanvas tests independent of View
    /// <summary>
    ///     Creates a new <see cref="View"/> into which a <see cref="LineCanvas"/> is rendered at
    ///     <see cref="View.DrawComplete"/> time.
    /// </summary>
    /// <param name="canvas">The <see cref="LineCanvas"/> you can draw into.</param>
    /// <param name="offsetX">How far to offset drawing in X</param>
    /// <param name="offsetY">How far to offset drawing in Y</param>
    /// <returns></returns>
    private View GetCanvas (IDriver driver, out LineCanvas canvas, int offsetX = 0, int offsetY = 0)
    {
        var v = new View { Width = 10, Height = 5, Viewport = new (0, 0, 10, 5) };
        v.Driver = driver;

        LineCanvas canvasCopy = canvas = new ();

        v.DrawComplete += (s, e) =>
                          {
                              v.FillRect (v.Viewport);

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
