﻿using Xunit.Abstractions;

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
        superView.LayoutSubviews ();

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
}
