using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>Tests for view coordinate mapping (e.g. <see cref="View.ScreenToFrame"/> etc...).</summary>
public class ScreenToTests
{
    private readonly ITestOutputHelper _output;
    public ScreenToTests (ITestOutputHelper output) { _output = output; }

    /// <summary>
    ///     Tests that screen to bounds mapping works correctly when the view has no superview and there ARE Adornments on the
    ///     view.
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, -1, -1)]
    [InlineData (0, 0, 1, 1, 0, 0)]
    [InlineData (0, 0, 9, 9, 8, 8)]
    [InlineData (0, 0, 11, 11, 10, 10)]
    [InlineData (1, 1, 0, 0, -2, -2)]
    [InlineData (1, 1, 1, 1, -1, -1)]
    [InlineData (1, 1, 9, 9, 7, 7)]
    [InlineData (1, 1, 11, 11, 9, 9)]
    public void ScreenToBounds_NoSuper_HasAdornments (int viewX, int viewY, int x, int y, int expectedX, int expectedY)
    {
        var view = new View
        {
            X = viewX,
            Y = viewY,
            Width = 10,
            Height = 10,
            BorderStyle = LineStyle.Single
        };

        Point actual = view.ScreenToBounds (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    ///     Tests that screen to bounds mapping works correctly when the view has no superview and there are no Adornments on
    ///     the view.
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToBounds_NoSuper_NoAdornments (int viewX, int viewY, int x, int y, int expectedX, int expectedY)
    {
        var view = new View { X = viewX, Y = viewY, Width = 10, Height = 10 };

        Point actual = view.ScreenToBounds (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>Tests that screen to bounds mapping works correctly when the view has as superview it DOES have Adornments</summary>
    [Theory]
    [InlineData (0, 0, 0, 0, -1, -1)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (0, 0, 1, 1, 0, 0)]
    [InlineData (0, 0, 9, 9, 8, 8)]
    [InlineData (0, 0, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -2, -2)]
    [InlineData (1, 1, 1, 1, -1, -1)]
    [InlineData (1, 1, 9, 9, 7, 7)]
    [InlineData (1, 1, 11, 11, 9, 9)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToBounds_SuperHasAdornments (int viewX, int viewY, int x, int y, int expectedX, int expectedY)
    {
        var super = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            BorderStyle = LineStyle.Single
        };
        var view = new View { X = viewX, Y = viewY, Width = 5, Height = 5 };
        super.Add (view);

        Point actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>Tests that screen to bounds mapping works correctly when the view has as superview it does not have Adornments</summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToBounds_SuperHasNoAdornments (int viewX, int viewY, int x, int y, int expectedX, int expectedY)
    {
        var super = new View { X = 0, Y = 0, Width = 10, Height = 10 };
        var view = new View { X = viewX, Y = viewY, Width = 5, Height = 5 };
        super.Add (view);

        Point actual = view.ScreenToBounds (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    ///     Tests that screen to view mapping works correctly when the view has no superview and there ARE Adornments on the
    ///     view.
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToView_NoSuper_HasAdornments (int viewX, int viewY, int x, int y, int expectedX, int expectedY)
    {
        var view = new View
        {
            X = viewX,
            Y = viewY,
            Width = 10,
            Height = 10,
            BorderStyle = LineStyle.Single
        };

        Point actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>
    ///     Tests that screen to view mapping works correctly when the view has no superview and there are no Adornments on
    ///     the view.
    /// </summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToView_NoSuper_NoAdornments (int viewX, int viewY, int x, int y, int expectedX, int expectedY)
    {
        var view = new View { X = viewX, Y = viewY, Width = 10, Height = 10 };

        Point actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>Tests that screen to view mapping works correctly when the view has as superview it DOES have Adornments</summary>
    [Theory]
    [InlineData (0, 0, 0, 0, -1, -1)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (0, 0, 1, 1, 0, 0)]
    [InlineData (0, 0, 9, 9, 8, 8)]
    [InlineData (0, 0, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -2, -2)]
    [InlineData (1, 1, 1, 1, -1, -1)]
    [InlineData (1, 1, 9, 9, 7, 7)]
    [InlineData (1, 1, 11, 11, 9, 9)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToView_SuperHasAdornments (int viewX, int viewY, int x, int y, int expectedX, int expectedY)
    {
        var super = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            BorderStyle = LineStyle.Single
        };
        var view = new View { X = viewX, Y = viewY, Width = 5, Height = 5 };
        super.Add (view);

        Point actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }

    /// <summary>Tests that screen to view mapping works correctly when the view has as superview it does not have Adornments</summary>
    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1, 1, 1)]
    [InlineData (0, 0, 9, 9, 9, 9)]
    [InlineData (0, 0, 11, 11, 11, 11)] // it's ok for the view to return coordinates outside of its bounds
    [InlineData (1, 1, 0, 0, -1, -1)]
    [InlineData (1, 1, 1, 1, 0, 0)]
    [InlineData (1, 1, 9, 9, 8, 8)]
    [InlineData (1, 1, 11, 11, 10, 10)] // it's ok for the view to return coordinates outside of its bounds
    public void ScreenToView_SuperHasNoAdornments (int viewX, int viewY, int x, int y, int expectedX, int expectedY)
    {
        var super = new View { X = 0, Y = 0, Width = 10, Height = 10 };
        var view = new View { X = viewX, Y = viewY, Width = 5, Height = 5 };
        super.Add (view);

        Point actual = view.ScreenToFrame (x, y);
        Assert.Equal (expectedX, actual.X);
        Assert.Equal (expectedY, actual.Y);
    }
}
