namespace UnitTests_Parallelizable.ViewTests;

/// <summary>
///     Parallelizable tests for mouse drag functionality on movable and resizable views.
///     These tests validate mouse drag behavior without Application.Init or global state.
/// </summary>
[Trait ("Category", "Input")]
public class MouseDragTests
{
    #region Movable View Drag Tests

    [Fact]
    public void MovableView_MouseDrag_UpdatesPosition ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View movableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (movableView);

        // Simulate mouse press on border to start drag
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (0, 0), // Click on border
            ScreenPosition = new Point (10, 10), // Screen position matches view position
            Flags = MouseFlags.Button1Pressed
        };

        // Act - Start drag
        movableView.Border!.NewMouseEvent (pressEvent);

        // Simulate mouse drag
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (5, 5), // Moved 5,5 from start
            ScreenPosition = new Point (15, 15), // New screen position
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        movableView.Border!.NewMouseEvent (dragEvent);

        // Assert - View should have moved
        Assert.Equal (15, movableView.Frame.X);
        Assert.Equal (15, movableView.Frame.Y);
        Assert.Equal (10, movableView.Frame.Width);
        Assert.Equal (10, movableView.Frame.Height);

        movableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void MovableView_MouseDrag_WithSuperview_UsesCorrectCoordinates ()
    {
        // Arrange
        View superView = new ()
        {
            X = 5,
            Y = 5,
            Width = 50,
            Height = 50
        };

        View movableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (movableView);

        // Simulate mouse press on border
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (0, 0),
            ScreenPosition = new Point (15, 15), // 5+10 offset
            Flags = MouseFlags.Button1Pressed
        };

        movableView.Border!.NewMouseEvent (pressEvent);

        // Simulate mouse drag
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (3, 3),
            ScreenPosition = new Point (18, 18), // Moved 3,3
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        movableView.Border!.NewMouseEvent (dragEvent);

        // Assert - View should have moved relative to superview
        Assert.Equal (13, movableView.Frame.X); // 10 + 3
        Assert.Equal (13, movableView.Frame.Y); // 10 + 3

        movableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void MovableView_MouseRelease_StopsDrag ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View movableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (movableView);

        // Start drag
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (0, 0),
            ScreenPosition = new Point (10, 10),
            Flags = MouseFlags.Button1Pressed
        };

        movableView.Border!.NewMouseEvent (pressEvent);

        // Drag
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (5, 5),
            ScreenPosition = new Point (15, 15),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        movableView.Border!.NewMouseEvent (dragEvent);

        // Release
        MouseEventArgs releaseEvent = new ()
        {
            Position = new Point (5, 5),
            ScreenPosition = new Point (15, 15),
            Flags = MouseFlags.Button1Released
        };

        movableView.Border!.NewMouseEvent (releaseEvent);

        // Assert - Position should remain at dragged location
        Assert.Equal (15, movableView.Frame.X);
        Assert.Equal (15, movableView.Frame.Y);

        movableView.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region Resizable View Drag Tests

    [Fact]
    public void ResizableView_RightResize_Drag_IncreasesWidth ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on right border
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (9, 5), // Right edge of border (width 10 + border thickness)
            ScreenPosition = new Point (19, 15),
            Flags = MouseFlags.Button1Pressed
        };

        resizableView.Border!.NewMouseEvent (pressEvent);

        // Simulate mouse drag to the right
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (14, 5), // Drag 5 units right
            ScreenPosition = new Point (24, 15),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        resizableView.Border!.NewMouseEvent (dragEvent);

        // Assert - Width should have increased
        Assert.Equal (10, resizableView.Frame.X); // X unchanged
        Assert.Equal (10, resizableView.Frame.Y); // Y unchanged
        Assert.Equal (15, resizableView.Frame.Width); // Width increased by 5
        Assert.Equal (10, resizableView.Frame.Height); // Height unchanged

        resizableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_BottomResize_Drag_IncreasesHeight ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.BottomResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on bottom border
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (5, 9), // Bottom edge of border
            ScreenPosition = new Point (15, 19),
            Flags = MouseFlags.Button1Pressed
        };

        resizableView.Border!.NewMouseEvent (pressEvent);

        // Simulate mouse drag down
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (5, 14), // Drag 5 units down
            ScreenPosition = new Point (15, 24),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        resizableView.Border!.NewMouseEvent (dragEvent);

        // Assert - Height should have increased
        Assert.Equal (10, resizableView.Frame.X); // X unchanged
        Assert.Equal (10, resizableView.Frame.Y); // Y unchanged
        Assert.Equal (10, resizableView.Frame.Width); // Width unchanged
        Assert.Equal (15, resizableView.Frame.Height); // Height increased by 5

        resizableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_LeftResize_Drag_MovesAndResizes ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on left border
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (0, 5), // Left edge of border
            ScreenPosition = new Point (10, 15),
            Flags = MouseFlags.Button1Pressed
        };

        resizableView.Border!.NewMouseEvent (pressEvent);

        // Simulate mouse drag to the left
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (-3, 5), // Drag 3 units left
            ScreenPosition = new Point (7, 15),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        resizableView.Border!.NewMouseEvent (dragEvent);

        // Assert - Should move left and resize
        Assert.Equal (7, resizableView.Frame.X); // X moved left by 3
        Assert.Equal (10, resizableView.Frame.Y); // Y unchanged
        Assert.Equal (13, resizableView.Frame.Width); // Width increased by 3
        Assert.Equal (10, resizableView.Frame.Height); // Height unchanged

        resizableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_TopResize_Drag_MovesAndResizes ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on top border
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (5, 0), // Top edge of border
            ScreenPosition = new Point (15, 10),
            Flags = MouseFlags.Button1Pressed
        };

        resizableView.Border!.NewMouseEvent (pressEvent);

        // Simulate mouse drag up
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (5, -2), // Drag 2 units up
            ScreenPosition = new Point (15, 8),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        resizableView.Border!.NewMouseEvent (dragEvent);

        // Assert - Should move up and resize
        Assert.Equal (10, resizableView.Frame.X); // X unchanged
        Assert.Equal (8, resizableView.Frame.Y); // Y moved up by 2
        Assert.Equal (10, resizableView.Frame.Width); // Width unchanged
        Assert.Equal (12, resizableView.Frame.Height); // Height increased by 2

        resizableView.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region Corner Resize Tests

    [Fact]
    public void ResizableView_BottomRightCornerResize_Drag_ResizesBothDimensions ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.BottomResizable | ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on bottom-right corner
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (9, 9), // Bottom-right corner
            ScreenPosition = new Point (19, 19),
            Flags = MouseFlags.Button1Pressed
        };

        resizableView.Border!.NewMouseEvent (pressEvent);

        // Simulate mouse drag diagonally
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (14, 14), // Drag 5 units right and down
            ScreenPosition = new Point (24, 24),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        resizableView.Border!.NewMouseEvent (dragEvent);

        // Assert - Both dimensions should increase
        Assert.Equal (10, resizableView.Frame.X); // X unchanged
        Assert.Equal (10, resizableView.Frame.Y); // Y unchanged
        Assert.Equal (15, resizableView.Frame.Width); // Width increased by 5
        Assert.Equal (15, resizableView.Frame.Height); // Height increased by 5

        resizableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_TopLeftCornerResize_Drag_MovesAndResizes ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.TopResizable | ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on top-left corner
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (0, 0), // Top-left corner
            ScreenPosition = new Point (10, 10),
            Flags = MouseFlags.Button1Pressed
        };

        resizableView.Border!.NewMouseEvent (pressEvent);

        // Simulate mouse drag diagonally up and left
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (-3, -2), // Drag 3 left, 2 up
            ScreenPosition = new Point (7, 8),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        resizableView.Border!.NewMouseEvent (dragEvent);

        // Assert - Should move and resize
        Assert.Equal (7, resizableView.Frame.X); // X moved left by 3
        Assert.Equal (8, resizableView.Frame.Y); // Y moved up by 2
        Assert.Equal (13, resizableView.Frame.Width); // Width increased by 3
        Assert.Equal (12, resizableView.Frame.Height); // Height increased by 2

        resizableView.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region DetermineArrangeModeFromClick Tests

    [Fact]
    public void DetermineArrangeModeFromClick_MovableView_ReturnsMovable ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View movableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (movableView);

        // Simulate mouse press in center (not on edge)
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (5, 5), // Center of border
            ScreenPosition = new Point (15, 15),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        movableView.Border!.NewMouseEvent (pressEvent);

        // Assert - Should be in movable mode
        Assert.Equal (ViewArrangement.Movable, movableView.Border!.Arranging);

        movableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void DetermineArrangeModeFromClick_RightEdge_ReturnsRightResizable ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Resizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on right edge
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (9, 5), // Right edge
            ScreenPosition = new Point (19, 15),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        resizableView.Border!.NewMouseEvent (pressEvent);

        // Assert - Should be in right resizable mode
        Assert.Equal (ViewArrangement.RightResizable, resizableView.Border!.Arranging);

        resizableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void DetermineArrangeModeFromClick_BottomEdge_ReturnsBottomResizable ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Resizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on bottom edge
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (5, 9), // Bottom edge
            ScreenPosition = new Point (15, 19),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        resizableView.Border!.NewMouseEvent (pressEvent);

        // Assert - Should be in bottom resizable mode
        Assert.Equal (ViewArrangement.BottomResizable, resizableView.Border!.Arranging);

        resizableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void DetermineArrangeModeFromClick_BottomRightCorner_ReturnsCornerResize ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Resizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on bottom-right corner
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (9, 9), // Bottom-right corner
            ScreenPosition = new Point (19, 19),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        resizableView.Border!.NewMouseEvent (pressEvent);

        // Assert - Should be in corner resize mode
        Assert.Equal (ViewArrangement.BottomResizable | ViewArrangement.RightResizable, resizableView.Border!.Arranging);

        resizableView.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region Minimum Size Constraints

    [Fact]
    public void ResizableView_Drag_RespectsMinimumWidth ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on left border
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (0, 5),
            ScreenPosition = new Point (10, 15),
            Flags = MouseFlags.Button1Pressed
        };

        resizableView.Border!.NewMouseEvent (pressEvent);

        // Try to drag far to the right (making width very small)
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (8, 5), // Drag 8 units right, would make width 2
            ScreenPosition = new Point (18, 15),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        resizableView.Border!.NewMouseEvent (dragEvent);

        // Assert - Width should be constrained to minimum
        // Minimum width = border thickness + margin right
        int expectedMinWidth = resizableView.Border!.Thickness.Horizontal + resizableView.Margin!.Thickness.Right;
        Assert.True (resizableView.Frame.Width >= expectedMinWidth);

        resizableView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_Drag_RespectsMinimumHeight ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 50,
            Height = 50
        };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Simulate mouse press on top border
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (5, 0),
            ScreenPosition = new Point (15, 10),
            Flags = MouseFlags.Button1Pressed
        };

        resizableView.Border!.NewMouseEvent (pressEvent);

        // Try to drag far down (making height very small)
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (5, 8), // Drag 8 units down, would make height 2
            ScreenPosition = new Point (15, 18),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        resizableView.Border!.NewMouseEvent (dragEvent);

        // Assert - Height should be constrained to minimum
        int expectedMinHeight = resizableView.Border!.Thickness.Vertical + resizableView.Margin!.Thickness.Bottom;
        Assert.True (resizableView.Frame.Height >= expectedMinHeight);

        resizableView.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region Modal Window Drag Tests

    [Fact]
    public void ModalWindow_Drag_WorksLikeMovableView ()
    {
        // Arrange - Create a modal-like setup
        View modalContainer = new ()
        {
            Width = 30,
            Height = 20
        };

        Window modalWindow = new ()
        {
            X = 5,
            Y = 3,
            Width = 10,
            Height = 5,
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single
        };

        modalContainer.Add (modalWindow);

        // Simulate mouse press on border
        MouseEventArgs pressEvent = new ()
        {
            Position = new Point (0, 0),
            ScreenPosition = new Point (5, 3),
            Flags = MouseFlags.Button1Pressed
        };

        modalWindow.Border!.NewMouseEvent (pressEvent);

        // Simulate mouse drag
        MouseEventArgs dragEvent = new ()
        {
            Position = new Point (4, 2),
            ScreenPosition = new Point (9, 5),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        modalWindow.Border!.NewMouseEvent (dragEvent);

        // Assert - Window should have moved
        Assert.Equal (9, modalWindow.Frame.X);
        Assert.Equal (5, modalWindow.Frame.Y);
        Assert.Equal (10, modalWindow.Frame.Width);
        Assert.Equal (5, modalWindow.Frame.Height);

        modalWindow.Dispose ();
        modalContainer.Dispose ();
    }

    #endregion
}