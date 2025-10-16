namespace Terminal.Gui.DrawingTests;

/// <summary>
/// Tests for <see cref="LineCanvas"/>. All tests are parallelizable as they test LineCanvas directly
/// without requiring Application.Driver or global state.
/// </summary>
public class LineCanvasTests : UnitTests.Parallelizable.ParallelizableBase
{
    #region Add_2_Lines Tests - Line Intersection Tests

    [Theory]
    // Horizontal lines with a vertical zero-length
    [InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Double, 0, 0, 0, Orientation.Vertical, LineStyle.Single, "╞")]
    [InlineData (0, 0, -1, Orientation.Horizontal, LineStyle.Double, 0, 0, 0, Orientation.Vertical, LineStyle.Single, "╡")]
    [InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Single, 0, 0, 0, Orientation.Vertical, LineStyle.Double, "╟")]
    [InlineData (0, 0, -1, Orientation.Horizontal, LineStyle.Single, 0, 0, 0, Orientation.Vertical, LineStyle.Double, "╢")]
    [InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Single, 0, 0, 0, Orientation.Vertical, LineStyle.Single, "├")]
    [InlineData (0, 0, -1, Orientation.Horizontal, LineStyle.Single, 0, 0, 0, Orientation.Vertical, LineStyle.Single, "┤")]
    [InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Double, 0, 0, 0, Orientation.Vertical, LineStyle.Double, "╠")]
    [InlineData (0, 0, -1, Orientation.Horizontal, LineStyle.Double, 0, 0, 0, Orientation.Vertical, LineStyle.Double, "╣")]
    // Vertical lines with a horizontal zero-length
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Double, 0, 0, 1, Orientation.Vertical, LineStyle.Single, "╥")]
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Double, 0, 0, -1, Orientation.Vertical, LineStyle.Single, "╨")]
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Single, 0, 0, 1, Orientation.Vertical, LineStyle.Double, "╤")]
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Single, 0, 0, -1, Orientation.Vertical, LineStyle.Double, "╧")]
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Single, 0, 0, 1, Orientation.Vertical, LineStyle.Single, "┬")]
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Single, 0, 0, -1, Orientation.Vertical, LineStyle.Single, "┴")]
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Double, 0, 0, 1, Orientation.Vertical, LineStyle.Double, "╦")]
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Double, 0, 0, -1, Orientation.Vertical, LineStyle.Double, "╩")]
    // Both zero-length (cross)
    [InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Single, 0, 0, 0, Orientation.Horizontal, LineStyle.Single, "┼")]
    [InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Double, 0, 0, 0, Orientation.Horizontal, LineStyle.Single, "╫")]
    [InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Single, 0, 0, 0, Orientation.Horizontal, LineStyle.Double, "╪")]
    [InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Double, 0, 0, 0, Orientation.Horizontal, LineStyle.Double, "╬")]
    public void Add_2_Lines_Creates_Correct_Intersection (
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
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (x1, y1), len1, o1, s1);
        canvas.AddLine (new Point (x2, y2), len2, o2, s2);

        string actual = canvas.ToString ();
        Assert.Equal (expected, actual);
    }

    #endregion

    #region Length Tests

    [Theory]
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
    public void Length_0_Creates_Single_Character_Line (int x, int y, Orientation orientation, string expected)
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (x, y), 0, orientation, LineStyle.Single);
        
        string actual = canvas.ToString ();
        Assert.Equal (expected, actual);
    }

    [Theory]
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
    [InlineData (0, 0, 2, Orientation.Horizontal, "──")]
    [InlineData (0, 0, 3, Orientation.Horizontal, "───")]
    [InlineData (0, 0, 2, Orientation.Vertical, "│\n│")]
    [InlineData (0, 0, 3, Orientation.Vertical, "│\n│\n│")]
    public void Length_n_Creates_Line_Of_Length_n (int x, int y, int length, Orientation orientation, string expected)
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (x, y), length, orientation, LineStyle.Single);
        
        string actual = canvas.ToString ();
        Assert.Equal (expected, actual);
    }

    [Fact]
    public void Length_Negative_Creates_Line_In_Opposite_Direction ()
    {
        var canvas = new LineCanvas ();
        
        // Add a horizontal line from (2, 0) going left (negative length)
        canvas.AddLine (new Point (2, 0), -3, Orientation.Horizontal, LineStyle.Single);
        
        string actual = canvas.ToString ();
        Assert.Equal ("───", actual);
    }

    [Theory]
    [InlineData (Orientation.Horizontal, "─")]
    [InlineData (Orientation.Vertical, "│")]
    public void Length_Zero_Alone_Creates_Single_Line (Orientation orientation, string expected)
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 0, orientation, LineStyle.Single);
        
        string actual = canvas.ToString ();
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (Orientation.Horizontal, "┼")]
    [InlineData (Orientation.Vertical, "┼")]
    public void Length_Zero_Cross_Creates_Intersection (Orientation orientation, string expected)
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 0, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 0), 0, orientation, LineStyle.Single);
        
        string actual = canvas.ToString ();
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (Orientation.Horizontal, "┬")]
    [InlineData (Orientation.Vertical, "├")]
    public void Length_Zero_NextTo_Opposite_Creates_T (Orientation orientation, string expected)
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 0), 0, orientation, LineStyle.Single);
        
        string actual = canvas.ToString ();
        Assert.Equal (expected, actual);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_Empty_Canvas_Returns_Empty_String ()
    {
        var canvas = new LineCanvas ();
        Assert.Equal (string.Empty, canvas.ToString ());
    }

    [Theory]
    [InlineData (0, 0, "─")]
    [InlineData (1, 0, " ─")]
    [InlineData (0, 1, "\n─")]
    [InlineData (1, 1, "\n ─")]
    [InlineData (2, 0, "  ─")]
    [InlineData (2, 1, "\n  ─")]
    [InlineData (0, 2, "\n\n─")]
    [InlineData (2, 2, "\n\n  ─")]
    public void ToString_Positive_Horizontal_1Line_With_Offset (int x, int y, string expected)
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (x, y), 1, Orientation.Horizontal, LineStyle.Single);
        
        string actual = canvas.ToString ();
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (0, 0, "│")]
    [InlineData (1, 0, " │")]
    [InlineData (0, 1, "\n│")]
    [InlineData (1, 1, "\n │")]
    [InlineData (2, 0, "  │")]
    [InlineData (2, 1, "\n  │")]
    [InlineData (0, 2, "\n\n│")]
    [InlineData (2, 2, "\n\n  │")]
    public void ToString_Positive_Vertical_1Line_With_Offset (int x, int y, string expected)
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (x, y), 1, Orientation.Vertical, LineStyle.Single);
        
        string actual = canvas.ToString ();
        Assert.Equal (expected, actual);
    }

    #endregion

    #region Bounds Tests

    [Fact]
    public void Bounds_Empty_Canvas_Returns_Empty_Rectangle ()
    {
        var canvas = new LineCanvas ();
        Assert.Equal (Rectangle.Empty, canvas.Bounds);
    }

    [Fact]
    public void Bounds_Single_Point_Returns_1x1_Rectangle ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (5, 5), 0, Orientation.Horizontal, LineStyle.Single);
        
        Assert.Equal (new Rectangle (5, 5, 1, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Horizontal_Line_Returns_Correct_Rectangle ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (2, 3), 5, Orientation.Horizontal, LineStyle.Single);
        
        Assert.Equal (new Rectangle (2, 3, 5, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Vertical_Line_Returns_Correct_Rectangle ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (2, 3), 5, Orientation.Vertical, LineStyle.Single);
        
        Assert.Equal (new Rectangle (2, 3, 1, 5), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Multiple_Lines_Returns_Union ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 0), 3, Orientation.Vertical, LineStyle.Single);
        
        Assert.Equal (new Rectangle (0, 0, 5, 3), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Negative_Length_Included ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (5, 5), -3, Orientation.Horizontal, LineStyle.Single);
        
        Assert.Equal (new Rectangle (2, 5, 3, 1), canvas.Bounds);
    }

    #endregion

    #region Lines Property Tests

    [Fact]
    public void Lines_Empty_Canvas_Returns_Empty_Collection ()
    {
        var canvas = new LineCanvas ();
        Assert.Empty (canvas.Lines);
    }

    [Fact]
    public void Lines_Returns_Added_Lines ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 0), 3, Orientation.Vertical, LineStyle.Double);
        
        Assert.Equal (2, canvas.Lines.Count);
    }

    [Fact]
    public void Lines_Is_ReadOnly ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        
        Assert.IsAssignableFrom<IReadOnlyCollection<StraightLine>> (canvas.Lines);
    }

    #endregion

    #region GetMap Tests

    [Fact]
    public void GetMap_Empty_Canvas_Returns_Empty_Dictionary ()
    {
        var canvas = new LineCanvas ();
        var map = canvas.GetMap ();
        
        Assert.Empty (map);
    }

    [Fact]
    public void GetMap_Single_Line_Returns_Correct_Points ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 3, Orientation.Horizontal, LineStyle.Single);
        
        var map = canvas.GetMap ();
        
        Assert.Equal (3, map.Count);
        Assert.True (map.ContainsKey (new Point (0, 0)));
        Assert.True (map.ContainsKey (new Point (1, 0)));
        Assert.True (map.ContainsKey (new Point (2, 0)));
    }

    [Fact]
    public void GetMap_Returns_Correct_Runes ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 3, Orientation.Horizontal, LineStyle.Single);
        
        var map = canvas.GetMap ();
        
        Assert.Equal (new System.Text.Rune ('─'), map [new Point (0, 0)]);
        Assert.Equal (new System.Text.Rune ('─'), map [new Point (1, 0)]);
        Assert.Equal (new System.Text.Rune ('─'), map [new Point (2, 0)]);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_Removes_All_Lines ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 0), 3, Orientation.Vertical, LineStyle.Single);
        
        canvas.Clear ();
        
        Assert.Empty (canvas.Lines);
        Assert.Equal (Rectangle.Empty, canvas.Bounds);
        Assert.Empty (canvas.GetMap ());
    }

    #endregion

    #region AddLine Tests

    [Fact]
    public void AddLine_Adds_Line_To_Canvas ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        
        Assert.Single (canvas.Lines);
    }

    [Fact]
    public void AddLine_Multiple_Times_Adds_All_Lines ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new Point (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 1), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 2), 5, Orientation.Horizontal, LineStyle.Single);
        
        Assert.Equal (3, canvas.Lines.Count);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_Creates_Empty_Canvas ()
    {
        var canvas = new LineCanvas ();
        
        Assert.Empty (canvas.Lines);
        Assert.Equal (Rectangle.Empty, canvas.Bounds);
    }

    [Fact]
    public void Constructor_With_Lines_Creates_Canvas_With_Lines ()
    {
        var lines = new[]
        {
            new StraightLine (new Point (0, 0), 5, Orientation.Horizontal, LineStyle.Single),
            new StraightLine (new Point (0, 0), 3, Orientation.Vertical, LineStyle.Single)
        };
        
        var canvas = new LineCanvas (lines);
        
        Assert.Equal (2, canvas.Lines.Count);
    }

    #endregion
}
