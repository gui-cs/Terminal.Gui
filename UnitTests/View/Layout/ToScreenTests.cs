using Xunit.Abstractions;
using static System.Net.Mime.MediaTypeNames;

namespace Terminal.Gui.LayoutTests;

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
        view.Layout ();

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
        super.Layout ();

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
        var screen = view.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, screen.X);
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
    [InlineData (0, 1)]
    [InlineData (1, 2)]
    [InlineData (-1, 0)]
    [InlineData (11, 12)]
    public void FrameToScreen_NoSuperView_WithAdornment_WithSubview (int x, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var view = new View ();
        view.BorderStyle = LineStyle.Single;
        view.Frame = frame;

        var subviewOfBorder = new View ()
        {
            X = 1, // screen should be 1
            Y = 0,
            Width = 1,
            Height = 1
        };

        view.Border.Add (subviewOfBorder);
        view.BeginInit ();
        view.EndInit ();

        // Act
        var screen = subviewOfBorder.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, screen.X);
    }

    [Theory]
    [InlineData (0, 3)]
    [InlineData (1, 4)]
    [InlineData (-1, 2)]
    [InlineData (11, 14)]
    public void FrameToScreen_Adornment_WithSubview_WithSubview (int topX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var adornmentFrame = new Rectangle (topX, 0, 10, 10);

        var adornment = new Adornment ();
        adornment.Frame = adornmentFrame;
        adornment.Thickness = new (1);

        var subviewOfAdornment = new View ()
        {
            Id = "subviewOfAdornment",
            X = 1, // screen should be 1
            Y = 0,
            Width = 1,
            Height = 1,
        };

        var subviewOfSubview = new View ()
        {
            Id = "subviewOfSubview",
            X = 2, // screen should be 3 (the subviewOfAdornment location is 1)
            Y = 0,
            Width = 1,
            Height = 1
        };
        subviewOfAdornment.Add (subviewOfSubview);

        adornment.Add (subviewOfAdornment);
        adornment.BeginInit ();
        adornment.EndInit ();

        // Act
        var screen = subviewOfSubview.FrameToScreen ();

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
        superSuperView.Layout ();

        // Act
        var screen = view.FrameToScreen ();

        // Assert
        Assert.Equal (expectedX, screen.X);
    }

    // ContentToScreen tests ----------------------

    [Fact]
    public void ContentToScreen_With_Positive_Content_Location ()
    {
        View view = new ()
        {
            X = 1,
            Y = 1,
            Width = 10,
            Height = 10
        };
        view.Layout ();
        view.SetContentSize (new (20, 20));

        Point testPoint = new (0, 0);
        Assert.Equal (new Point (1, 1), view.ContentToScreen (testPoint));
    }

    [Theory]
    [InlineData (0, 0, 1)]
    [InlineData (1, 0, 2)]
    [InlineData (-1, 0, 0)]

    [InlineData (0, 1, 2)]
    [InlineData (1, 1, 3)]
    [InlineData (-1, 1, 1)]

    [InlineData (0, -1, 0)]
    [InlineData (1, -1, 1)]
    [InlineData (-1, -1, -1)]
    public void ContentToScreen_NoSuperView_WithAdornments (int frameX, int contentX, int expectedX)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (frameX, 0, 10, 10);

        var view = new View ();
        view.Frame = frame;
        view.SetContentSize (new (20, 20));
        view.BorderStyle = LineStyle.Single;

        // Act
        var screen = view.ContentToScreen (new (contentX, 0));

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
    public void ContentToScreen_SuperView_WithoutAdornments (int frameX, int contentX, int expectedX)
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
        view.SetContentSize (new (20, 20));

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var screen = view.ContentToScreen (new (contentX, 0));

        // Assert
        Assert.Equal (expectedX, screen.X);
    }

    //[Theory]
    //[InlineData (0, 0, 1)]
    //[InlineData (1, 0, 2)]
    //[InlineData (-1, 0, 0)]
    //[InlineData (11, 0, 12)]

    //[InlineData (0, 1, 2)]
    //[InlineData (1, 1, 3)]
    //[InlineData (-1, 1, 1)]
    //[InlineData (11, 1, 13)]

    //[InlineData (0, -1, 0)]
    //[InlineData (1, -1, 1)]
    //[InlineData (-1, -1, -1)]
    //[InlineData (11, -1, 11)]
    //public void ContentToScreen_SuperView_WithAdornments (int frameX, int ContentX, int expectedX)
    //{
    //    // We test with only X because Y is equivalent. Height/Width are irrelevant.
    //    // Arrange
    //    var frame = new Rectangle (frameX, 0, 10, 10);

    //    var superView = new View ()
    //    {
    //        X = 0,
    //        Y = 0,
    //        Height = Dim.Fill (),
    //        Width = Dim.Fill ()
    //    };
    //    superView.BorderStyle = LineStyle.Single;

    //    var view = new View ();
    //    view.Frame = frame;

    //    superView.Add (view);
    //    superView.LayoutSubviews ();

    //    // Act
    //    var screen = view.ContentToScreen (new (ContentX, 0, 0, 0));

    //    // Assert
    //    Assert.Equal (expectedX, screen.X);
    //}

    //[Theory]
    //[InlineData (0, 0, 0)]
    //[InlineData (1, 0, 1)]
    //[InlineData (-1, 0, -1)]
    //[InlineData (11, 0, 11)]

    //[InlineData (0, 1, 1)]
    //[InlineData (1, 1, 2)]
    //[InlineData (-1, 1, 0)]
    //[InlineData (11, 1, 12)]

    //[InlineData (0, -1, -1)]
    //[InlineData (1, -1, 0)]
    //[InlineData (-1, -1, -2)]
    //[InlineData (11, -1, 10)]
    //public void ContentToScreen_NestedSuperView_WithoutAdornments (int frameX, int ContentX, int expectedX)
    //{
    //    // We test with only X because Y is equivalent. Height/Width are irrelevant.
    //    // Arrange
    //    var frame = new Rectangle (frameX, 0, 10, 10);

    //    var superSuperView = new View ()
    //    {
    //        X = 0,
    //        Y = 0,
    //        Height = Dim.Fill (),
    //        Width = Dim.Fill ()
    //    };

    //    var superView = new View ()
    //    {
    //        X = 0,
    //        Y = 0,
    //        Height = Dim.Fill (),
    //        Width = Dim.Fill ()
    //    };

    //    superSuperView.Add (superView);

    //    var view = new View ();
    //    view.Frame = frame;

    //    superView.Add (view);
    //    superView.LayoutSubviews ();

    //    // Act
    //    var screen = view.ContentToScreen (new (ContentX, 0, 0, 0));

    //    // Assert
    //    Assert.Equal (expectedX, screen.X);
    //}

    //[Theory]
    //[InlineData (0, 0, 2)]
    //[InlineData (1, 0, 3)]
    //[InlineData (-1, 0, 1)]
    //[InlineData (11, 0, 13)]

    //[InlineData (0, 1, 3)]
    //[InlineData (1, 1, 4)]
    //[InlineData (-1, 1, 2)]
    //[InlineData (11, 1, 14)]

    //[InlineData (0, -1, 1)]
    //[InlineData (1, -1, 2)]
    //[InlineData (-1, -1, 0)]
    //[InlineData (11, -1, 12)]
    //public void ContentToScreen_NestedSuperView_WithAdornments (int frameX, int ContentX, int expectedX)
    //{
    //    // We test with only X because Y is equivalent. Height/Width are irrelevant.
    //    // Arrange
    //    var frame = new Rectangle (frameX, 0, 10, 10);

    //    var superSuperView = new View ()
    //    {
    //        X = 0,
    //        Y = 0,
    //        Height = Dim.Fill (),
    //        Width = Dim.Fill ()
    //    };
    //    superSuperView.BorderStyle = LineStyle.Single;

    //    var superView = new View ()
    //    {
    //        X = 0,
    //        Y = 0,
    //        Height = Dim.Fill (),
    //        Width = Dim.Fill ()
    //    };

    //    superSuperView.Add (superView);
    //    superView.BorderStyle = LineStyle.Single;

    //    var view = new View ();
    //    view.Frame = frame;

    //    superView.Add (view);
    //    superView.LayoutSubviews ();

    //    // Act
    //    var screen = view.ContentToScreen (new (ContentX, 0, 0, 0));

    //    // Assert
    //    Assert.Equal (expectedX, screen.X);
    //}


    //[Theory]
    //[InlineData (0, 0, 3)]
    //[InlineData (1, 0, 4)]
    //[InlineData (-1, 0, 2)]
    //[InlineData (11, 0, 14)]

    //[InlineData (0, 1, 4)]
    //[InlineData (1, 1, 5)]
    //[InlineData (-1, 1, 3)]
    //[InlineData (11, 1, 15)]

    //[InlineData (0, -1, 2)]
    //[InlineData (1, -1, 3)]
    //[InlineData (-1, -1, 1)]
    //[InlineData (11, -1, 13)]
    //public void ContentToScreen_Positive_NestedSuperView_WithAdornments (int frameX, int testX, int expectedX)
    //{
    //    // We test with only X because Y is equivalent. Height/Width are irrelevant.
    //    // Arrange
    //    var frame = new Rectangle (frameX, 0, 10, 10);

    //    var superSuperView = new View ()
    //    {
    //        X = 0,
    //        Y = 0,
    //        Height = Dim.Fill (),
    //        Width = Dim.Fill ()
    //    };
    //    superSuperView.BorderStyle = LineStyle.Single;

    //    var superView = new View ()
    //    {
    //        X = 0,
    //        Y = 0,
    //        Height = Dim.Fill (),
    //        Width = Dim.Fill ()
    //    };

    //    superSuperView.Add (superView);
    //    superView.BorderStyle = LineStyle.Single;

    //    var view = new View ();
    //    view.Frame = frame;
    //    view.SetContentSize (new (11, 11));
    //    view.Content = view.Content with { Location = new (1, 1) };

    //    superView.Add (view);
    //    superView.LayoutSubviews ();

    //    // Act
    //    var screen = view.ContentToScreen (new (testX, 0, 0, 0));

    //    // Assert
    //    Assert.Equal (expectedX, screen.X);
    //}

    // ViewportToScreen tests ----------------------

    [Fact]
    public void ViewportToScreen_With_Positive_Viewport_Location ()
    {
        View view = new ()
        {
            Width = 10,
            Height = 10,
            ViewportSettings = ViewportSettings.AllowNegativeLocation
        };
        view.Layout ();

        Rectangle testRect = new Rectangle (0, 0, 1, 1);
        Assert.Equal (new Point (0, 0), view.ViewportToScreen (testRect).Location);
        view.Viewport = view.Viewport with { Location = new Point (1, 1) };

        Assert.Equal (new Rectangle (1, 1, 10, 10), view.Viewport);
        Assert.Equal (new Point (0, 0), view.ViewportToScreen (testRect).Location);
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
        view.Layout ();

        // Act
        var screen = view.ViewportToScreen (new Point (viewportX, 0));

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
        var screen = view.ViewportToScreen (new Point (viewportX, 0));

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
        var screen = view.ViewportToScreen (new Point (viewportX, 0));

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
        var screen = view.ViewportToScreen (new Point (viewportX, 0));

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
        var screen = view.ViewportToScreen (new Point (viewportX, 0));

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
        superView.Layout ();

        // Act
        var screen = view.ViewportToScreen (new Point (viewportX, 0));

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
    public void ViewportToScreen_Positive_NestedSuperView_WithAdornments (int frameX, int testX, int expectedX)
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
        view.SetContentSize (new (11, 11));
        view.Viewport = view.Viewport with { Location = new (1, 1) };

        superView.Add (view);
        superView.LayoutSubviews ();

        // Act
        var screen = view.ViewportToScreen (new Point (testX, 0));

        // Assert
        Assert.Equal (expectedX, screen.X);
    }


    [Fact]
    [AutoInitShutdown]
    public void ScreenToView_ViewToScreen_GetViewsUnderMouse_Full_Top ()
    {
        Application.Top = new ();
        Application.Top.BorderStyle = LineStyle.Single;

        var view = new View
        {
            X = 3,
            Y = 2,
            Width = 10,
            Height = 1,
            Text = "0123456789"
        };
        Application.Top.Add (view);

        var rs = Application.Begin (Application.Top);

        Assert.Equal (new (0, 0, 80, 25), new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows));
        Assert.Equal (new (0, 0, View.Driver.Cols, View.Driver.Rows), Application.Top.Frame);
        Assert.Equal (new (0, 0, 80, 25), Application.Top.Frame);

        ((FakeDriver)Application.Driver!).SetBufferSize (20, 10);
        Assert.Equal (new (0, 0, View.Driver.Cols, View.Driver.Rows), Application.Top.Frame);
        Assert.Equal (new (0, 0, 20, 10), Application.Top.Frame);


        _ = TestHelpers.AssertDriverContentsWithFrameAre (
                                                          @"
┌──────────────────┐
│                  │
│                  │
│   0123456789     │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘"
        ,
                                                          _output
                                                         );

        // top
        Assert.Equal (Point.Empty, Application.Top.ScreenToFrame (new (0, 0)));
        Point screen = Application.Top.Margin.ViewportToScreen (new Point (0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = Application.Top.Border.ViewportToScreen (new Point (0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = Application.Top.Padding.ViewportToScreen (new Point (0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = Application.Top.ViewportToScreen (new Point (0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = Application.Top.ViewportToScreen (new Point (-1, -1));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        var found = View.GetViewsUnderMouse (new Point(0, 0)).LastOrDefault ();
        Assert.Equal (Application.Top.Border, found);

        Assert.Equal (0, found.Frame.X);
        Assert.Equal (0, found.Frame.Y);
        Assert.Equal (new (3, 2), Application.Top.ScreenToFrame (new (3, 2)));
        screen = Application.Top.ViewportToScreen (new Point (3, 2));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.GetViewsUnderMouse (new Point(screen.X, screen.Y)).LastOrDefault ();
        Assert.Equal (view, found);

        //Assert.Equal (0, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        found = View.GetViewsUnderMouse (new Point(3, 2)).LastOrDefault ();
        Assert.Equal (Application.Top, found);

        //Assert.Equal (3, found.FrameToScreen ().X);
        //Assert.Equal (2, found.FrameToScreen ().Y);
        Assert.Equal (new (13, 2), Application.Top.ScreenToFrame (new (13, 2)));
        screen = Application.Top.ViewportToScreen (new Point (12, 2));
        Assert.Equal (13, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.GetViewsUnderMouse (new Point(screen.X, screen.Y)).LastOrDefault ();
        Assert.Equal (view, found);

        //Assert.Equal (9, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        screen = Application.Top.ViewportToScreen (new Point (13, 2));
        Assert.Equal (14, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.GetViewsUnderMouse (new Point(13, 2)).LastOrDefault ();
        Assert.Equal (Application.Top, found);

        //Assert.Equal (13, found.FrameToScreen ().X);
        //Assert.Equal (2, found.FrameToScreen ().Y);
        Assert.Equal (new (14, 3), Application.Top.ScreenToFrame (new (14, 3)));
        screen = Application.Top.ViewportToScreen (new Point (14, 3));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        found = View.GetViewsUnderMouse (new Point(14, 3)).LastOrDefault ();
        Assert.Equal (Application.Top, found);

        //Assert.Equal (14, found.FrameToScreen ().X);
        //Assert.Equal (3, found.FrameToScreen ().Y);

        // view
        Assert.Equal (new (-4, -3), view.ScreenToFrame (new (0, 0)));
        screen = view.Margin.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Border.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Padding.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.ViewportToScreen (new Point (-4, -3));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        found = View.GetViewsUnderMouse (new Point(0, 0)).LastOrDefault ();
        Assert.Equal (Application.Top.Border, found);

        Assert.Equal (new (-1, -1), view.ScreenToFrame (new (3, 2)));
        screen = view.ViewportToScreen (new Point (0, 0));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.GetViewsUnderMouse (new Point(4, 3)).LastOrDefault ();
        Assert.Equal (view, found);

        Assert.Equal (new (9, -1), view.ScreenToFrame (new (13, 2)));
        screen = view.ViewportToScreen (new Point (10, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.GetViewsUnderMouse (new Point(14, 3)).LastOrDefault ();
        Assert.Equal (Application.Top, found);

        Assert.Equal (new (10, 0), view.ScreenToFrame (new (14, 3)));
        screen = view.ViewportToScreen (new Point (11, 1));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        found = View.GetViewsUnderMouse (new Point(15, 4)).LastOrDefault ();
        Assert.Equal (Application.Top, found);

        Application.Top.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }

    [Fact]
    [AutoInitShutdown]
    public void ScreenToView_ViewToScreen_GetViewsUnderMouse_Smaller_Top ()
    {
        Application.Top = new ()
        {
            X = 3,
            Y = 2,
            Width = 20,
            Height = 10,
            BorderStyle = LineStyle.Single
        };

        var view = new View
        {
            X = 3,
            Y = 2,
            Width = 10,
            Height = 1,
            Text = "0123456789"
        };
        Application.Top.Add (view);

        Application.Begin (Application.Top);

        Assert.Equal (new (0, 0, 80, 25), new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows));
        Assert.NotEqual (new (0, 0, View.Driver.Cols, View.Driver.Rows), Application.Top.Frame);
        Assert.Equal (new (3, 2, 20, 10), Application.Top.Frame);

        ((FakeDriver)Application.Driver!).SetBufferSize (30, 20);
        Assert.Equal (new (0, 0, 30, 20), new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows));
        Assert.NotEqual (new (0, 0, View.Driver.Cols, View.Driver.Rows), Application.Top.Frame);
        Assert.Equal (new (3, 2, 20, 10), Application.Top.Frame);

        Rectangle frame = TestHelpers.AssertDriverContentsWithFrameAre (
                                                                        @"
   ┌──────────────────┐
   │                  │
   │                  │
   │   0123456789     │
   │                  │
   │                  │
   │                  │
   │                  │
   │                  │
   └──────────────────┘"
        ,
                                                                        _output
                                                                       );

        // mean the output started at col 3 and line 2
        // which result with a width of 23 and a height of 10 on the output
        Assert.Equal (new (3, 2, 23, 10), frame);

        // top
        Assert.Equal (new (-3, -2), Application.Top.ScreenToFrame (new (0, 0)));
        Point screen = Application.Top.Margin.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = Application.Top.Border.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = Application.Top.Padding.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = Application.Top.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = Application.Top.ViewportToScreen (new Point (-4, -3));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        var found = View.GetViewsUnderMouse (new Point(-4, -3)).LastOrDefault ();
        Assert.Null (found);
        Assert.Equal (Point.Empty, Application.Top.ScreenToFrame (new (3, 2)));
        screen = Application.Top.ViewportToScreen (new Point (0, 0));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        Assert.Equal (Application.Top.Border, View.GetViewsUnderMouse (new Point(3, 2)).LastOrDefault ());

        //Assert.Equal (0, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        Assert.Equal (new (10, 0), Application.Top.ScreenToFrame (new (13, 2)));
        screen = Application.Top.ViewportToScreen (new Point (10, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (3, screen.Y);
        Assert.Equal (Application.Top.Border, View.GetViewsUnderMouse (new Point(13, 2)).LastOrDefault ());

        //Assert.Equal (10, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        Assert.Equal (new (11, 1), Application.Top.ScreenToFrame (new (14, 3)));
        screen = Application.Top.ViewportToScreen (new Point (11, 1));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        Assert.Equal (Application.Top, View.GetViewsUnderMouse (new Point(14, 3)).LastOrDefault ());

        // view
        Assert.Equal (new (-7, -5), view.ScreenToFrame (new (0, 0)));
        screen = view.Margin.ViewportToScreen (new Point (-6, -4));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Border.ViewportToScreen (new Point (-6, -4));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Padding.ViewportToScreen (new Point (-6, -4));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.ViewportToScreen (new Point (-6, -4));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        Assert.Null (View.GetViewsUnderMouse (new Point(1, 1)).LastOrDefault ());
        Assert.Equal (new (-4, -3), view.ScreenToFrame (new (3, 2)));
        screen = view.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        Assert.Equal (Application.Top, View.GetViewsUnderMouse (new Point(4, 3)).LastOrDefault ());
        Assert.Equal (new (-1, -1), view.ScreenToFrame (new (6, 4)));
        screen = view.ViewportToScreen (new Point (0, 0));
        Assert.Equal (7, screen.X);
        Assert.Equal (5, screen.Y);
        Assert.Equal (view, View.GetViewsUnderMouse (new Point(7, 5)).LastOrDefault ());
        Assert.Equal (new (6, -1), view.ScreenToFrame (new (13, 4)));
        screen = view.ViewportToScreen (new Point (7, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (5, screen.Y);
        Assert.Equal (view, View.GetViewsUnderMouse (new Point(14, 5)).LastOrDefault ());
        Assert.Equal (new (7, -2), view.ScreenToFrame (new (14, 3)));
        screen = view.ViewportToScreen (new Point (8, -1));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        Assert.Equal (Application.Top, View.GetViewsUnderMouse (new Point(15, 4)).LastOrDefault ());
        Assert.Equal (new (16, -2), view.ScreenToFrame (new (23, 3)));
        screen = view.ViewportToScreen (new Point (17, -1));
        Assert.Equal (24, screen.X);
        Assert.Equal (4, screen.Y);
        Assert.Null (View.GetViewsUnderMouse (new Point(24, 4)).LastOrDefault ());
        Application.Top.Dispose ();
    }
}
