using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Test the <see cref="View.Bounds"/>.
/// DOES NOT TEST Adornment.Bounds methods. Those are in ./Adornment/BoundsTests.cs
/// </summary>
/// <param name="output"></param>
public class BoundsTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (0, 10)]
    [InlineData (1, 10)]
    [InlineData (-1, 10)]
    [InlineData (11, 10)]
    public void Get_Bounds_NoSuperView_WithoutAdornments (int x, int expectedW)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var view = new View ();
        view.Frame = frame;
        view.BeginInit();
        view.EndInit();

        // Act
        var bounds = view.Bounds;

        // Assert
        Assert.Equal(expectedW, bounds.Width);
    }
    
    [Theory]
    [InlineData (0, 0, 10)]
    [InlineData (1, 0, 9)]
    [InlineData (-1, 0, 11)]
    [InlineData (10, 0, 0)]
    [InlineData (11, 0, 0)]

    [InlineData (0, 1, 6)]
    [InlineData (1, 1, 5)]
    [InlineData (-1, 1, 7)]
    [InlineData (10, 1, 0)]
    [InlineData (11, 1, 0)]

    public void Get_Bounds_NestedSuperView_WithAdornments (int frameX, int borderThickness, int expectedW)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var superSuperView = new View ()
        {
            X = 0,
            Y = 0,
            Height = 10,
            Width = 10,
        };
        superSuperView.Border.Thickness = new Thickness (borderThickness);

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };
        superView.Border.Thickness = new Thickness (borderThickness);

        superSuperView.Add (superView);

        var view = new View ()
        {
            X = frameX,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        superView.Add (view);
        superSuperView.BeginInit ();
        superSuperView.EndInit ();
        superSuperView.LayoutSubviews ();

        // Act
        var bounds = view.Bounds;

        // Assert
        Assert.Equal (expectedW, bounds.Width);
    }



    [Theory]
    [InlineData (0, 0, 10)]
    [InlineData (1, 0, 9)]
    [InlineData (-1, 0, 11)]
    [InlineData (10, 0, 0)]
    [InlineData (11, 0, 0)]

    [InlineData (0, 1, 4)]
    [InlineData (1, 1, 3)]
    [InlineData (-1, 1, 5)]
    [InlineData (10, 1, 0)]
    [InlineData (11, 1, 0)]
    public void Get_Bounds_NestedSuperView_WithAdornments_WithBorder (int frameX, int borderThickness, int expectedW)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var superSuperView = new View ()
        {
            X = 0,
            Y = 0,
            Height = 10,
            Width = 10,
        };
        superSuperView.Border.Thickness = new Thickness (borderThickness);

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };
        superView.Border.Thickness = new Thickness (borderThickness);

        superSuperView.Add (superView);

        var view = new View ()
        {
            X = frameX,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };
        view.Border.Thickness = new Thickness (borderThickness);

        superView.Add (view);
        superSuperView.BeginInit ();
        superSuperView.EndInit ();
        superSuperView.LayoutSubviews ();

        // Act
        var bounds = view.Bounds;

        // Assert
        Assert.Equal (expectedW, bounds.Width);
    }
}
