namespace Terminal.Gui.DrawingTests;

/// <summary>
/// Pure unit tests for <see cref="LineCanvas"/> that don't require Application.Driver or View context.
/// These tests focus on properties and behavior that don't depend on glyph rendering.
/// 
/// Note: Tests that verify rendered output (ToString()) cannot be parallelized because LineCanvas
/// depends on Application.Driver for glyph resolution and configuration. Those tests remain in UnitTests.
/// </summary>
public class LineCanvasTests : UnitTests.Parallelizable.ParallelizableBase
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
        var lines = new[]
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

        var region = new Region (new Rectangle (0, 0, 2, 1));
        canvas.Exclude (region);
        canvas.ClearExclusions ();

        // After clearing exclusions, GetMap should return all points
        var map = canvas.GetMap ();
        Assert.Equal (5, map.Count);
    }

    [Fact]
    public void Exclude_Removes_Points_From_Map ()
    {
        var canvas = new LineCanvas ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);

        var region = new Region (new Rectangle (0, 0, 2, 1));
        canvas.Exclude (region);

        var map = canvas.GetMap ();
        // Should have 5 - 2 = 3 points (excluding the first 2)
        Assert.Equal (3, map.Count);
    }

    #endregion

    #region Fill Property Tests

    [Fact]
    public void Fill_Property_Can_Be_Set ()
    {
        var foregroundFill = new SolidFill (new Color (255, 0));
        var backgroundFill = new SolidFill (new Color (0, 0));
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
}
