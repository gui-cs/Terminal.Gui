using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Test the <see cref="Adornment.FrameToScreen"/> and <see cref="Adornment.BoundsToScreen"/> methods.
/// DOES NOT TEST View.xxxToScreen methods. Those are in ./View/Layout/ToScreenTests.cs
/// </summary>
/// <param name="output"></param>
public class AdornmentToScreenTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (-1, -1)]
    [InlineData (11, 11)]
    public void FrameToScreen_NoSuperView_WithoutAdornments (int x, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var view = new View ();
        view.Frame = frame;

        // Act
        var marginScreen = view.Margin.FrameToScreen();
        var borderScreen = view.Border.FrameToScreen ();
        var paddingScreen = view.Padding.FrameToScreen ();

        // Assert
        Assert.Equal(expectedX, marginScreen.X);
        Assert.Equal (expectedX, borderScreen.X);
        Assert.Equal (expectedX, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (-1, -1)]
    [InlineData (11, 11)]
    public void FrameToScreen_NoSuperView_WithAdornments (int x, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var view = new View ();
        view.Border.Thickness = new (1);
        view.Frame = frame;

        // Act
        var marginScreen = view.Margin.FrameToScreen ();
        var borderScreen = view.Border.FrameToScreen ();
        var paddingScreen = view.Padding.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (-1, -1)]
    [InlineData (11, 11)]
    public void FrameToScreen_SuperView_WithoutAdornments (int x, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var marginScreen = view.Margin.FrameToScreen ();
        var borderScreen = view.Border.FrameToScreen ();
        var paddingScreen = view.Padding.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 1)]
    [InlineData (1, 2)]
    [InlineData (-1, 0)]
    [InlineData (11, 12)]
    public void FrameToScreen_SuperView_WithAdornments (int x, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };
        superView.Border.Thickness = new (1);

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var marginScreen = view.Margin.FrameToScreen ();
        var borderScreen = view.Border.FrameToScreen ();
        var paddingScreen = view.Padding.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (-1, -1)]
    [InlineData (11, 11)]
    public void FrameToScreen_NestedSuperView_WithoutAdornments (int x, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var superSuperView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        superSuperView.Add (superView);

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var marginScreen = view.Margin.FrameToScreen ();
        var borderScreen = view.Border.FrameToScreen ();
        var paddingScreen = view.Padding.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 2)]
    [InlineData (1, 3)]
    [InlineData (-1, 1)]
    [InlineData (11, 13)]
    public void FrameToScreen_NestedSuperView_WithAdornments (int x, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var superSuperView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };
        superSuperView.Border.Thickness = new (1);

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        superSuperView.Add (superView);
        superView.Border.Thickness = new (1);

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var marginScreen = view.Margin.FrameToScreen ();
        var borderScreen = view.Border.FrameToScreen ();
        var paddingScreen = view.Padding.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }



    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 0, 1)]
    [InlineData (-1, 0, -1)]
    [InlineData (11, 0, 11)]
    public void BoundsToScreen_NoSuperView_WithoutAdornments (int frameX, int boundsX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (frameX, 0, 10, 10);

        var view = new View ();
        view.Frame = frame;

        // Act
        var marginScreen = view.Margin.BoundsToScreen (new (boundsX, 0, 0, 0));
        var borderScreen = view.Border.BoundsToScreen (new (boundsX, 0, 0, 0));
        var paddingScreen = view.Padding.BoundsToScreen (new (boundsX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 0, 1)]
    [InlineData (-1, 0, -1)]
    [InlineData (11, 0, 11)]

    [InlineData (0, 1, 1)]
    [InlineData (1, 1, 2)]
    [InlineData (-1, 1, 0)]
    [InlineData (11, 1, 12)]

    [InlineData (0, -1, -1)]
    [InlineData (1, -1, 0)]
    [InlineData (-1, -1, -2)]
    [InlineData (11, -1, 10)]
    public void BoundsToScreen_NoSuperView_WithAdornments (int frameX, int boundsX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (frameX, 0, 10, 10);

        var view = new View ();
        view.Border.Thickness = new (1);
        view.Frame = frame;

        // Act
        var marginScreen = view.Margin.BoundsToScreen (new (boundsX, 0, 0, 0));
        var borderScreen = view.Border.BoundsToScreen (new (boundsX, 0, 0, 0));
        var paddingScreen = view.Padding.BoundsToScreen (new (boundsX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX, borderScreen.X);
        Assert.Equal (expectedX, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 0, 1)]
    [InlineData (-1, 0, -1)]
    [InlineData (11, 0, 11)]

    [InlineData (0, 1, 1)]
    [InlineData (1, 1, 2)]
    [InlineData (-1, 1, 0)]
    [InlineData (11, 1, 12)]

    [InlineData (0, -1, -1)]
    [InlineData (1, -1, 0)]
    [InlineData (-1, -1, -2)]
    [InlineData (11, -1, 10)]
    public void BoundsToScreen_SuperView_WithoutAdornments (int frameX, int boundsX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (frameX, 0, 10, 10);

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var marginScreen = view.Margin.BoundsToScreen (new (boundsX, 0, 0, 0));
        var borderScreen = view.Border.BoundsToScreen (new (boundsX, 0, 0, 0));
        var paddingScreen = view.Padding.BoundsToScreen (new (boundsX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 0, 1)]
    [InlineData (1, 0, 2)]
    [InlineData (-1, 0, 0)]
    [InlineData (11, 0, 12)]

    [InlineData (0, 1, 2)]
    [InlineData (1, 1, 3)]
    [InlineData (-1, 1, 1)]
    [InlineData (11, 1, 13)]

    [InlineData (0, -1, 0)]
    [InlineData (1, -1, 1)]
    [InlineData (-1, -1, -1)]
    [InlineData (11, -1, 11)]
    public void BoundsToScreen_SuperView_WithAdornments (int frameX, int boundsX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (frameX, 0, 10, 10);

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };
        superView.Border.Thickness = new (1);

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var marginScreen = view.Margin.BoundsToScreen (new (boundsX, 0, 0, 0));
        var borderScreen = view.Border.BoundsToScreen (new (boundsX, 0, 0, 0));
        var paddingScreen = view.Padding.BoundsToScreen (new (boundsX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 0, 1)]
    [InlineData (-1, 0, -1)]
    [InlineData (11, 0, 11)]

    [InlineData (0, 1, 1)]
    [InlineData (1, 1, 2)]
    [InlineData (-1, 1, 0)]
    [InlineData (11, 1, 12)]

    [InlineData (0, -1, -1)]
    [InlineData (1, -1, 0)]
    [InlineData (-1, -1, -2)]
    [InlineData (11, -1, 10)]
    public void BoundsToScreen_NestedSuperView_WithoutAdornments (int frameX, int boundsX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (frameX, 0, 10, 10);

        var superSuperView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        superSuperView.Add (superView);

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var marginScreen = view.Margin.BoundsToScreen (new (boundsX, 0, 0, 0));
        var borderScreen = view.Border.BoundsToScreen (new (boundsX, 0, 0, 0));
        var paddingScreen = view.Padding.BoundsToScreen (new (boundsX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }

    [Theory]
    [InlineData (0, 0, 2)]
    [InlineData (1, 0, 3)]
    [InlineData (-1, 0, 1)]
    [InlineData (11, 0, 13)]

    [InlineData (0, 1, 3)]
    [InlineData (1, 1, 4)]
    [InlineData (-1, 1, 2)]
    [InlineData (11, 1, 14)]

    [InlineData (0, -1, 1)]
    [InlineData (1, -1, 2)]
    [InlineData (-1, -1, 0)]
    [InlineData (11, -1, 12)]
    public void BoundsToScreen_NestedSuperView_WithAdornments (int frameX, int boundsX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (frameX, 0, 10, 10);

        var superSuperView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };
        superSuperView.Border.Thickness = new (1);

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        superSuperView.Add (superView);
        superView.Border.Thickness = new (1);

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var marginScreen = view.Margin.BoundsToScreen (new (boundsX, 0, 0, 0));
        var borderScreen = view.Border.BoundsToScreen (new (boundsX, 0, 0, 0));
        var paddingScreen = view.Padding.BoundsToScreen (new (boundsX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, marginScreen.X);
        Assert.Equal (expectedX + view.Margin.Thickness.Left, borderScreen.X);
        Assert.Equal (expectedX + view.Border.Thickness.Left, paddingScreen.X);
    }

}
