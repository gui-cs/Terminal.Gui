using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Test the <see cref="View.FrameToScreen"/> and <see cref="View.ViewportToScreen"/> methods.
/// DOES NOT TEST Adornment.xxxToScreen methods. Those are in ./Adornment/ToScreenTests.cs
/// </summary>
/// <param name="output"></param>
public class ToScreenTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


    // Test FrameToScreen
    [Theory]
    [InlineData (0, 0, 0, 0)]
    [InlineData (1, 0, 1, 0)]
    [InlineData (0, 1, 0, 1)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (10, 10, 10, 10)]
    public void FrameToScreen_NoSuperView (int frameX, int frameY, int expectedScreenX, int expectedScreenY)
    {
        var view = new View { X = frameX, Y = frameY, Width = 10, Height = 10 };
        var expected = new Rectangle (expectedScreenX, expectedScreenY, 10, 10);
        Rectangle actual = view.FrameToScreen ();
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0)]
    [InlineData (1, 0, 0, 1, 1)]
    [InlineData (2, 0, 0, 2, 2)]
    [InlineData (1, 1, 0, 2, 1)]
    [InlineData (1, 0, 1, 1, 2)]
    [InlineData (1, 1, 1, 2, 2)]
    [InlineData (1, 10, 10, 11, 11)]
    public void FrameToScreen_SuperView (
        int superOffset,
        int frameX,
        int frameY,
        int expectedScreenX,
        int expectedScreenY
    )
    {
        var super = new View { X = superOffset, Y = superOffset, Width = 20, Height = 20 };

        var view = new View { X = frameX, Y = frameY, Width = 10, Height = 10 };
        super.Add (view);
        var expected = new Rectangle (expectedScreenX, expectedScreenY, 10, 10);
        Rectangle actual = view.FrameToScreen ();
        Assert.Equal (expected, actual);
    }
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
        var screen = view.FrameToScreen();

        // Assert
        Assert.Equal(expectedX, screen.X);
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
        view.BorderStyle = LineStyle.Single;
        view.Frame = frame;

        // Act
        var screen = view.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, screen.X);
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
        var screen = view.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, screen.X);
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
        superView.BorderStyle = LineStyle.Single;

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var screen = view.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, screen.X);
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
        var screen = view.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, screen.X);
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
        superSuperView.BorderStyle = LineStyle.Single;

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        superSuperView.Add (superView);
        superView.BorderStyle = LineStyle.Single;

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var screen = view.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, screen.X);
    }

    // ViewportToScreen tests ----------------------

    [Fact]
    public void ViewportToScreen_With_Positive_Viewport_Location ()
    {
        View view = new ()
        {
            Width = 10,
            Height = 10
        };

        Rectangle testRect = new Rectangle (0, 0, 1, 1);
        Assert.Equal (new Point (0, 0), view.ViewportToScreen (testRect).Location);
        view.Viewport = view.Viewport with { Location = new Point (1, 1) };
        Assert.Equal (new Rectangle (1, 1, 10, 10), view.Viewport);
        Assert.Equal (new Point (1, 1), view.ViewportToScreen (testRect).Location);
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 0, 1)]
    [InlineData (-1, 0, -1)]
    [InlineData (11, 0, 11)]
    public void ViewportToScreen_NoSuperView_WithoutAdornments (int frameX, int viewportX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (frameX, 0, 10, 10);

        var view = new View ();
        view.Frame = frame;

        // Act
        var screen = view.ViewportToScreen (new (viewportX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, screen.X);
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
    public void ViewportToScreen_NoSuperView_WithAdornments (int frameX, int viewportX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (frameX, 0, 10, 10);

        var view = new View ();
        view.BorderStyle = LineStyle.Single;
        view.Frame = frame;

        // Act
        var screen = view.ViewportToScreen (new (viewportX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, screen.X);
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
    public void ViewportToScreen_SuperView_WithoutAdornments (int frameX, int viewportX, int expectedX)
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
        var screen = view.ViewportToScreen (new (viewportX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, screen.X);
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
    public void ViewportToScreen_SuperView_WithAdornments (int frameX, int viewportX, int expectedX)
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
        superView.BorderStyle = LineStyle.Single;

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var screen = view.ViewportToScreen (new (viewportX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, screen.X);
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
    public void ViewportToScreen_NestedSuperView_WithoutAdornments (int frameX, int viewportX, int expectedX)
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
        var screen = view.ViewportToScreen (new (viewportX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, screen.X);
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
    public void ViewportToScreen_NestedSuperView_WithAdornments (int frameX, int viewportX, int expectedX)
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
        superSuperView.BorderStyle = LineStyle.Single;

        var superView = new View ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Fill ()
        };

        superSuperView.Add (superView);
        superView.BorderStyle = LineStyle.Single;

        var view = new View ();
        view.Frame = frame;

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var screen = view.ViewportToScreen (new (viewportX, 0, 0, 0));

        // Assert
        Assert.Equal (expectedX, screen.X);
    }

}
