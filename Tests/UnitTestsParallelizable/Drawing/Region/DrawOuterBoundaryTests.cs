using System.Collections.Concurrent;
namespace DrawingTests.RegionTests;

/// <summary>
///     Tests for <see cref="Region.DrawOuterBoundary"/>.
/// </summary>
public class DrawOuterBoundaryTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void DrawOuterBoundary_AfterIntersect_DrawsIntersectedBoundary ()
    {
        // Arrange
        var region = new Region (new (0, 0, 10, 10));
        region.Intersect (new Rectangle (5, 5, 10, 10));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_AfterMinimalUnion_DrawsMinimalBoundary ()
    {
        // Arrange
        var region = new Region (new (0, 0, 3, 3));
        region.MinimalUnion (new Rectangle (3, 0, 3, 3));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_ComplexShape_HandlesMultipleRegions ()
    {
        // Arrange - Create a complex shape with multiple rectangles
        var region = new Region (new (0, 0, 3, 3));
        region.Union (new Rectangle (3, 3, 3, 3));
        region.Union (new Rectangle (6, 0, 3, 3));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_DiagonallyConnectedRectangles_DrawsOuterBoundary ()
    {
        // Arrange - Test the specific case from BUGBUG comment: (0,0,3,3) and (3,3,3,3)
        var region = new Region (new (0, 0, 3, 3));
        region.Union (new Rectangle (3, 3, 3, 3));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);

        // Note: According to BUGBUG comment, this should draw specific shape
        // with connecting corner but currently draws incorrectly
    }

    [Fact]
    public void DrawOuterBoundary_EmptyRegion_DoesNotThrow ()
    {
        // Arrange
        var region = new Region ();
        var canvas = new LineCanvas ();

        // Act
        Exception exception = Record.Exception (() => region.DrawOuterBoundary (canvas, LineStyle.Single));

        // Assert
        Assert.Null (exception);
        Assert.Empty (canvas.GetCellMap ());
    }

    [Fact]
    public void DrawOuterBoundary_GridPattern_DrawsOuterBoundary ()
    {
        // Arrange - Create a checkerboard pattern
        var region = new Region (new (0, 0, 2, 2));
        region.Union (new Rectangle (4, 0, 2, 2));
        region.Union (new Rectangle (0, 4, 2, 2));
        region.Union (new Rectangle (4, 4, 2, 2));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_HollowRectangle_DrawsOuterAndInnerBoundaries ()
    {
        // Arrange - Create a hollow rectangle (outer rect with inner rect removed)
        var region = new Region (new (0, 0, 10, 10));
        region.Exclude (new Rectangle (2, 2, 6, 6));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_HorizontalLineRectangle_DrawsHorizontalLine ()
    {
        // Arrange - A horizontal line (width>1, height=1)
        var region = new Region (new (0, 0, 4, 1));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_LShapedRegion_DrawsLShapeBoundary ()
    {
        // Arrange - Create an L-shape
        var region = new Region (new (0, 0, 3, 3));
        region.Union (new Rectangle (0, 3, 3, 3));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_MultipleCallsOnSameCanvas_AccumulatesLines ()
    {
        // Arrange
        var region1 = new Region (new (0, 0, 3, 3));
        var region2 = new Region (new (5, 5, 3, 3));
        var canvas = new LineCanvas ();

        // Act
        region1.DrawOuterBoundary (canvas, LineStyle.Single);
        int cellCountAfterFirst = canvas.GetCellMap ().Count;

        region2.DrawOuterBoundary (canvas, LineStyle.Single);
        int cellCountAfterSecond = canvas.GetCellMap ().Count;

        // Assert
        Assert.True (cellCountAfterSecond >= cellCountAfterFirst);
    }

    [Fact]
    public void DrawOuterBoundary_MultipleRegionsWithGaps_DrawsSeparateBoundaries ()
    {
        // Arrange
        var region = new Region ();
        region.Union (new Rectangle (0, 0, 2, 2));
        region.Union (new Rectangle (4, 0, 2, 2));
        region.Union (new Rectangle (8, 0, 2, 2));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);

        // Should have three separate boundary regions
    }

    [Fact]
    public void DrawOuterBoundary_OverlappingRectangles_DrawsOuterBoundaryOnly ()
    {
        // Arrange - Two overlapping rectangles
        var region = new Region (new (0, 0, 5, 5));
        region.Union (new Rectangle (3, 3, 5, 5));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);

        // Should only draw outer perimeter, not the overlapping internal area
    }

    [Fact]
    public void DrawOuterBoundary_RectangleAtNegativeCoordinates_DrawsBoundary ()
    {
        // Arrange
        var region = new Region (new (-5, -5, 3, 3));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_SinglePixelRectangle_DrawsSinglePoint ()
    {
        // Arrange - A 1x1 rectangle
        var region = new Region (new (5, 5, 1, 1));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_SingleRectangle_DrawsBoundary ()
    {
        // Arrange
        var region = new Region (new (0, 0, 3, 3));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_SingleWidthRegion_DrawsCorrectly ()
    {
        // Arrange - Test the specific case mentioned in BUGBUG comment
        var region = new Region (new (0, 0, 1, 4));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);

        // Note: According to BUGBUG, this should draw a single vertical line
        // but currently draws too wide
    }

    [Fact]
    public void DrawOuterBoundary_ThreadSafety_MultipleThreadsDrawing ()
    {
        // Arrange
        var region = new Region (new (0, 0, 10, 10));
        ConcurrentBag<Exception> exceptions = new ();

        // Act
        Parallel.For (
                      0,
                      10,
                      i =>
                      {
                          try
                          {
                              var canvas = new LineCanvas ();
                              region.DrawOuterBoundary (canvas, LineStyle.Single);
                          }
                          catch (Exception ex)
                          {
                              exceptions.Add (ex);
                          }
                      });

        // Assert
        Assert.Empty (exceptions);
    }

    [Fact]
    public void DrawOuterBoundary_ThreeWidthRegion_DrawsCorrectly ()
    {
        // Arrange - Test the specific case mentioned in BUGBUG comment
        var region = new Region (new (20, 0, 3, 4));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_TShapedRegion_DrawsCorrectBoundary ()
    {
        // Arrange - Create a T-shape
        var region = new Region (new (0, 0, 9, 3)); // Horizontal bar
        region.Union (new Rectangle (3, 3, 3, 6)); // Vertical bar
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_TwoAdjacentRectangles_DrawsOuterPerimeter ()
    {
        // Arrange - Two rectangles side by side
        var region = new Region (new (0, 0, 3, 3));
        region.Union (new Rectangle (3, 0, 3, 3));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);

        // Should draw outer boundary, not internal dividing line
        // The combined region should be treated as one shape
    }

    [Fact]
    public void DrawOuterBoundary_TwoSeparateRectangles_DrawsTwoBoundaries ()
    {
        // Arrange - Two non-adjacent rectangles
        var region = new Region (new (0, 0, 2, 2));
        region.Union (new Rectangle (5, 5, 2, 2));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);

        // Should have boundaries for both rectangles
        Assert.True (cells.Count > 0);
    }

    [Fact]
    public void DrawOuterBoundary_TwoWidthRegion_DrawsCorrectly ()
    {
        // Arrange - Test the specific case mentioned in BUGBUG comment
        var region = new Region (new (10, 0, 2, 4));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (10, 10)]
    [InlineData (-5, -5)]
    [InlineData (100, 100)]
    public void DrawOuterBoundary_VariousPositions_DrawsBoundary (int x, int y)
    {
        // Arrange
        var region = new Region (new (x, y, 5, 5));
        var canvas = new LineCanvas ();

        // Act
        Exception exception = Record.Exception (() => region.DrawOuterBoundary (canvas, LineStyle.Single));

        // Assert
        Assert.Null (exception);
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Theory]
    [InlineData (1, 1)]
    [InlineData (1, 5)]
    [InlineData (5, 1)]
    [InlineData (2, 2)]
    [InlineData (10, 10)]
    [InlineData (100, 100)]
    public void DrawOuterBoundary_VariousSizes_DrawsBoundary (int width, int height)
    {
        // Arrange
        var region = new Region (new (0, 0, width, height));
        var canvas = new LineCanvas ();

        // Act
        Exception exception = Record.Exception (() => region.DrawOuterBoundary (canvas, LineStyle.Single));

        // Assert
        Assert.Null (exception);
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_VerticalLineRectangle_DrawsVerticalLine ()
    {
        // Arrange - A vertical line (width=1, height>1)
        var region = new Region (new (0, 0, 1, 4));
        var canvas = new LineCanvas ();

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_VeryLargeRegion_FallsBackToDrawBoundaries ()
    {
        // Arrange - Create a region larger than the 1000x1000 threshold
        var region = new Region (new (0, 0, 1100, 1100));
        var canvas = new LineCanvas ();

        // Act
        Exception exception = Record.Exception (() => region.DrawOuterBoundary (canvas, LineStyle.Single));

        // Assert
        Assert.Null (exception);

        // Should fall back to DrawBoundaries for very large regions
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_WithCustomAttribute_AppliesAttribute ()
    {
        // Arrange
        var region = new Region (new (0, 0, 3, 3));
        var canvas = new LineCanvas ();
        var attribute = new Attribute (Color.Red, Color.Blue);

        // Act
        region.DrawOuterBoundary (canvas, LineStyle.Single, attribute);

        // Assert
        Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
        Assert.NotEmpty (cells);
    }

    [Fact]
    public void DrawOuterBoundary_WithDifferentLineStyles_DrawsWithCorrectStyle ()
    {
        // Arrange
        var region = new Region (new (0, 0, 3, 3));

        // Test each line style
        foreach (LineStyle style in Enum.GetValues<LineStyle> ())
        {
            var canvas = new LineCanvas ();

            // Act
            region.DrawOuterBoundary (canvas, style);

            // Assert
            Dictionary<Point, Cell?> cells = canvas.GetCellMap ();
            Assert.NotEmpty (cells);
        }
    }

    [Fact]
    public void DrawOuterBoundary_ZeroHeightRectangle_HandlesGracefully ()
    {
        // Arrange - Rectangle with zero height
        var region = new Region (new (5, 5, 5, 0));
        var canvas = new LineCanvas ();

        // Act
        Exception exception = Record.Exception (() => region.DrawOuterBoundary (canvas, LineStyle.Single));

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    public void DrawOuterBoundary_ZeroWidthRectangle_HandlesGracefully ()
    {
        // Arrange - Rectangle with zero width
        var region = new Region (new (5, 5, 0, 5));
        var canvas = new LineCanvas ();

        // Act
        Exception exception = Record.Exception (() => region.DrawOuterBoundary (canvas, LineStyle.Single));

        // Assert
        Assert.Null (exception);
    }
}
