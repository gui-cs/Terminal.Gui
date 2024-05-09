﻿using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Test the <see cref="View.Viewport"/>.
/// DOES NOT TEST Adornment.Viewport methods. Those are in ./Adornment/ViewportTests.cs
/// </summary>
/// <param name="output"></param>
public class ViewportTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (0, 10)]
    [InlineData (1, 10)]
    [InlineData (-1, 10)]
    [InlineData (11, 10)]
    public void Get_Viewport_NoSuperView_WithoutAdornments (int x, int expectedW)
    {
        // We test with only X because Y is equivalent. Height/Width are irrelevant.
        // Arrange
        var frame = new Rectangle (x, 0, 10, 10);

        var view = new View ();
        view.Frame = frame;
        view.BeginInit ();
        view.EndInit ();

        // Act
        var bounds = view.Viewport;

        // Assert
        Assert.Equal (expectedW, bounds.Width);
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

    public void Get_Viewport_NestedSuperView_WithAdornments (int frameX, int borderThickness, int expectedW)
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
        var bounds = view.Viewport;

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
    public void Get_Viewport_NestedSuperView_WithAdornments_WithBorder (int frameX, int borderThickness, int expectedW)
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
        var bounds = view.Viewport;

        // Assert
        Assert.Equal (expectedW, bounds.Width);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 0)]
    [InlineData (0, 1)]
    [InlineData (-1, -1)]
    public void Set_Viewport_Location_Preserves_Size_And_Frame (int xOffset, int yOffset)
    {
        View view = new ()
        {
            Width = 10,
            Height = 10,
            ViewportSettings = ViewportSettings.AllowNegativeLocation
        };

        Assert.Equal (new Rectangle (0, 0, 10, 10), view.Frame);

        Rectangle testRect = new Rectangle (0, 0, 1, 1);
        Assert.Equal (new Point (0, 0), view.ViewportToScreen (testRect).Location);
        view.Viewport = view.Viewport with { Location = new Point (xOffset, yOffset) };
        Assert.Equal (new Rectangle (xOffset, yOffset, 10, 10), view.Viewport);

        Assert.Equal (new Rectangle (0, 0, 10, 10), view.Frame);
    }

    [Fact]
    public void Set_Viewport_Changes_Frame ()
    {
        var frame = new Rectangle (1, 2, 3, 4);
        var newViewport = new Rectangle (0, 0, 30, 40);

        var v = new View { Frame = frame };
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);

        v.Viewport = newViewport;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newViewport, v.Viewport);
        Assert.Equal (new Rectangle (1, 2, newViewport.Width, newViewport.Height), v.Frame);
        Assert.Equal (new Rectangle (0, 0, newViewport.Width, newViewport.Height), v.Viewport);
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (30), v.Width);
        Assert.Equal (Dim.Sized (40), v.Height);

        newViewport = new Rectangle (0, 0, 3, 4);
        v.Viewport = newViewport;
        Assert.Equal (newViewport, v.Viewport);
        Assert.Equal (new Rectangle (1, 2, newViewport.Width, newViewport.Height), v.Frame);
        Assert.Equal (new Rectangle (0, 0, newViewport.Width, newViewport.Height), v.Viewport);
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (3), v.Width);
        Assert.Equal (Dim.Sized (4), v.Height);

        v.BorderStyle = LineStyle.Single;

        // Viewport should shrink
        Assert.Equal (new Rectangle (0, 0, 1, 2), v.Viewport);

        // Frame should not change
        Assert.Equal (new Rectangle (1, 2, 3, 4), v.Frame);
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (3), v.Width);
        Assert.Equal (Dim.Sized (4), v.Height);

        // Now set bounds bigger as before
        newViewport = new Rectangle (0, 0, 3, 4);
        v.Viewport = newViewport;
        Assert.Equal (newViewport, v.Viewport);

        // Frame grows because there's now a border
        Assert.Equal (new Rectangle (1, 2, 5, 6), v.Frame);
        Assert.Equal (new Rectangle (0, 0, newViewport.Width, newViewport.Height), v.Viewport);
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (5), v.Width);
        Assert.Equal (Dim.Sized (6), v.Height);
    }

    [Theory]
    [InlineData (0, 0, 10, 10, 0, 0)]
    [InlineData (10, 0, 10, 10, 9, 0)] // 9 because without AllowGreaterThanContentWidth, the location is clamped to size - 1
    [InlineData (0, 10, 10, 10, 0, 9)]
    [InlineData (10, 10, 10, 10, 9, 9)]
    public void Set_Viewport_ValidValue_UpdatesViewport (int viewWidth, int viewHeight, int viewportX, int viewportY, int expectedX, int expectedY)
    {
        // Arrange
        var view = new View ()
        {
            Width = viewWidth,
            Height = viewHeight,
        };
        var newViewport = new Rectangle (viewportX, viewportY, viewWidth, viewHeight);

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (new Rectangle (expectedX, expectedY, viewWidth, viewHeight), view.Viewport);
    }

    [Theory]
    [InlineData (0, 0, 10, 10, 10, 10)]
    [InlineData (10, 0, 10, 10, 10, 10)]
    [InlineData (0, 10, 10, 10, 10, 10)]
    [InlineData (10, 10, 10, 10, 10, 10)]
    public void Set_Viewport_ValidValue_UpdatesViewport_AllowLocationGreaterThanContentSize (int viewWidth, int viewHeight, int viewportX, int viewportY, int expectedX, int expectedY)
    {
        // Arrange
        var view = new View ()
        {
            Width = viewWidth,
            Height = viewHeight,
            ViewportSettings = ViewportSettings.AllowLocationGreaterThanContentSize
        };
        var newViewport = new Rectangle (viewportX, viewportY, viewWidth, viewHeight);

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (new Rectangle (expectedX, expectedY, viewWidth, viewHeight), view.Viewport);
    }

    [Fact]
    public void Set_Viewport_ValueGreaterThanContentSize_UpdatesViewportToContentSize ()
    {
        // Arrange
        var view = new View ();
        view.ContentSize = new Size (100, 100);
        var newViewport = new Rectangle (0, 0, 200, 200);
        view.ViewportSettings = ViewportSettings.AllowLocationGreaterThanContentSize;

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (newViewport, view.Viewport);
    }

    [Fact]
    public void Set_Viewport_NegativeValue_AllowedBySettings ()
    {
        // Arrange
        var view = new View ();
        var newViewport = new Rectangle (-10, -10, 100, 100);
        view.ViewportSettings = ViewportSettings.AllowNegativeLocation;

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (newViewport, view.Viewport);
    }

    [Fact]
    public void Set_Viewport_NegativeValue_NotAllowedBySettings ()
    {
        // Arrange
        var view = new View ();
        var newViewport = new Rectangle (-10, -10, 100, 100);
        view.ViewportSettings = ViewportSettings.None;

        // Act
        view.Viewport = newViewport;

        // Assert
        Assert.Equal (new Rectangle (0, 0, 100, 100), view.Viewport);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    public void GetViewportOffset_Returns_Offset_From_Frame (int adornmentThickness, int expectedOffset)
    {
        View view = new ()
        {
            X = 1,
            Y = 1,
            Width = 10,
            Height = 10
        };
        view.BeginInit ();
        view.EndInit ();
        view.Margin.Thickness = new (adornmentThickness);

        Assert.Equal (expectedOffset, view.GetViewportOffsetFromFrame ().X);
    }

    [Fact]
    public void ContentSize_Matches_ViewportSize_If_Not_Set ()
    {
        View view = new ()
        {
            Width = 1,
            Height = 1
        };
        Assert.Equal (view.Viewport.Size, view.ContentSize);
    }

    //[Theory]
    //[InlineData (0, 0, true)]
    //[InlineData (-1, 0, true)]
    //[InlineData (0, -1, true)]
    //[InlineData (-1, -1, true)]
    //[InlineData (-2, -2, true)]
    //[InlineData (-3, -3, true)]
    //[InlineData (-4, -4, true)]
    //[InlineData (-5, -4, false)]
    //[InlineData (-4, -5, false)]
    //[InlineData (-5, -5, false)]

    //[InlineData (1, 1, true)]
    //[InlineData (2, 2, true)]
    //[InlineData (3, 3, true)]
    //[InlineData (4, 4, true)]
    //[InlineData (5, 4, false)]
    //[InlineData (4, 5, false)]
    //[InlineData (5, 5, false)]
    //public void IsVisibleInSuperView_No_Driver_No_SuperView (int x, int y, bool expected)
    //{
    //    var view = new View { X = 1, Y = 1, Width = 5, Height = 5 };
    //    Assert.True (view.IsVisibleInSuperView (x, y) == expected);
    //}

    //[Theory]
    //[InlineData (0, 0, true)]
    //[InlineData (-1, 0, true)]
    //[InlineData (0, -1, true)]
    //[InlineData (-1, -1, true)]
    //[InlineData (-2, -2, true)]
    //[InlineData (-3, -3, true)]
    //[InlineData (-4, -4, true)]
    //[InlineData (-5, -4, true)]
    //[InlineData (-4, -5, true)]
    //[InlineData (-5, -5, true)]
    //[InlineData (-6, -5, false)]
    //[InlineData (-5, -6, false)]
    //[InlineData (-6, -6, false)]

    //[InlineData (1, 1, true)]
    //[InlineData (2, 2, true)]
    //[InlineData (3, 3, true)]
    //[InlineData (4, 4, true)]
    //[InlineData (5, 4, true)]
    //[InlineData (4, 5, true)]
    //[InlineData (5, 5, true)]
    //[InlineData (6, 5, true)]
    //[InlineData (6, 6, true)]
    //[InlineData (7, 7, true)]
    //[InlineData (8, 8, true)]
    //[InlineData (9, 8, false)]
    //[InlineData (8, 9, false)]
    //[InlineData (9, 9, false)]
    //public void IsVisibleInSuperView_No_Driver_With_SuperView (int x, int y, bool expected)
    //{
    //    var view = new View { X = 1, Y = 1, Width = 5, Height = 5 };
    //    var top = new Toplevel { Width = 10, Height = 10 };
    //    top.Add (view);

    //    Assert.True (view.IsVisibleInSuperView (x, y) == expected);
    //}

    //[SetupFakeDriver]
    //[Theory]
    //[InlineData (0, 0, true)]
    //[InlineData (-1, 0, false)]
    //[InlineData (0, -1, false)]
    //[InlineData (-1, -1, false)]

    //[InlineData (1, 0, true)]
    //[InlineData (0, 1, true)]
    //[InlineData (1, 1, true)]
    //[InlineData (2, 2, true)]
    //[InlineData (3, 3, true)]
    //[InlineData (4, 4, true)]
    //[InlineData (5, 4, false)]
    //[InlineData (4, 5, false)]
    //[InlineData (5, 5, false)]
    //public void IsVisibleInSuperView_With_Driver (int x, int y, bool expected)
    //{
    //    ((FakeDriver)Application.Driver).SetBufferSize (10, 10);

    //    var view = new View { X = 1, Y = 1, Width = 5, Height = 5 };
    //    var top = new Toplevel ();
    //    top.Add (view);
    //    Application.Begin (top);

    //    Assert.True (view.IsVisibleInSuperView (x, y) == expected);

    //    top.Dispose ();
    //    Application.Shutdown ();
    //}
}
