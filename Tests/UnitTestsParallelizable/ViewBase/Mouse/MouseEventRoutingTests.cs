using Terminal.Gui.App;
using Xunit.Abstractions;

namespace ApplicationTests;

/// <summary>
///     Parallelizable tests for mouse event routing and coordinate transformation.
///     These tests validate mouse event handling without Application.Begin or global state.
/// </summary>
[Trait ("Category", "Input")]
public class MouseEventRoutingTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Mouse Event Routing to Views

    [Theory]
    [InlineData (5, 5, 5, 5, true)]   // Click inside view
    [InlineData (0, 0, 0, 0, true)]   // Click at origin
    [InlineData (9, 9, 9, 9, true)]   // Click at far corner (view is 10x10)
    [InlineData (10, 10, -1, -1, false)] // Click outside view
    [InlineData (-1, -1, -1, -1, false)] // Click outside view
    public void View_NewMouseEvent_ReceivesCorrectCoordinates (int screenX, int screenY, int expectedViewX, int expectedViewY, bool shouldReceive)
    {
        // Arrange
        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };

        Point? receivedPosition = null;
        var eventReceived = false;

        view.MouseEvent += (sender, args) =>
        {
            eventReceived = true;
            receivedPosition = args.Position;
        };

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (screenX, screenY),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        view.NewMouseEvent (mouseEvent);

        // Assert
        if (shouldReceive)
        {
            Assert.True (eventReceived);
            Assert.NotNull (receivedPosition);
            Assert.Equal (expectedViewX, receivedPosition.Value.X);
            Assert.Equal (expectedViewY, receivedPosition.Value.Y);
        }

        view.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, 5, 5, 5, 5, true)]   // View at origin, click at (5,5) in view
    [InlineData (10, 10, 5, 5, 5, 5, true)] // View offset, but we still pass view-relative coords
    [InlineData (0, 0, 0, 0, 0, 0, true)]   // View at origin, click at origin
    [InlineData (5, 5, 9, 9, 9, 9, true)]   // View offset, click at far corner (view-relative)
    [InlineData (0, 0, 10, 10, -1, -1, false)] // Click outside view bounds
    [InlineData (0, 0, -1, -1, -1, -1, false)] // Click outside view bounds
    public void View_WithOffset_ReceivesCorrectCoordinates (
        int viewX,
        int viewY,
        int viewRelativeX,
        int viewRelativeY,
        int expectedViewX,
        int expectedViewY,
        bool shouldReceive)
    {
        // Arrange
        // Note: When testing View.NewMouseEvent directly (without Application routing),
        // coordinates are already view-relative. The view's X/Y position doesn't affect
        // the coordinate transformation at this level.
        View view = new ()
        {
            X = viewX,
            Y = viewY,
            Width = 10,
            Height = 10
        };

        Point? receivedPosition = null;
        var eventReceived = false;

        view.MouseEvent += (sender, args) =>
        {
            eventReceived = true;
            receivedPosition = args.Position;
        };

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (viewRelativeX, viewRelativeY),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        view.NewMouseEvent (mouseEvent);

        // Assert
        if (shouldReceive)
        {
            Assert.True (eventReceived, $"Event should be received at view-relative ({viewRelativeX},{viewRelativeY})");
            Assert.NotNull (receivedPosition);
            Assert.Equal (expectedViewX, receivedPosition.Value.X);
            Assert.Equal (expectedViewY, receivedPosition.Value.Y);
        }

        view.Dispose ();
    }

    #endregion

    #region View Hierarchy Mouse Event Routing

    [Fact]
    public void SubView_ReceivesMouseEvent_WithCorrectRelativeCoordinates ()
    {
        // Arrange
        View superView = new ()
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 20
        };

        View subView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10
        };

        superView.Add (subView);

        Point? subViewReceivedPosition = null;
        var subViewEventReceived = false;

        subView.MouseEvent += (sender, args) =>
        {
            subViewEventReceived = true;
            subViewReceivedPosition = args.Position;
        };

        // Click at position (2, 2) relative to subView (which is at 5,5 relative to superView)
        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (2, 2), // Relative to subView
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        subView.NewMouseEvent (mouseEvent);

        // Assert
        Assert.True (subViewEventReceived);
        Assert.NotNull (subViewReceivedPosition);
        Assert.Equal (2, subViewReceivedPosition.Value.X);
        Assert.Equal (2, subViewReceivedPosition.Value.Y);

        subView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void MouseClick_OnSubView_RaisesSelectingEvent ()
    {
        // Arrange
        View superView = new ()
        {
            Width = 20,
            Height = 20
        };

        View subView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10
        };

        superView.Add (subView);

        var selectingCount = 0;
        subView.Selecting += (sender, args) => selectingCount++;

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        subView.NewMouseEvent (mouseEvent);

        // Assert
        Assert.Equal (1, selectingCount);

        subView.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region Mouse Event Propagation

    [Fact]
    public void View_HandledEvent_StopsPropagation ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10 };
        var handlerCalled = false;
        var clickHandlerCalled = false;

        view.MouseEvent += (sender, args) =>
        {
            handlerCalled = true;
            args.Handled = true; // Mark as handled
        };

        view.MouseClick += (sender, args) => { clickHandlerCalled = true; };

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        bool? result = view.NewMouseEvent (mouseEvent);

        // Assert
        Assert.True (result.HasValue && result.Value); // Event was handled
        Assert.True (handlerCalled);
        Assert.False (clickHandlerCalled); // Click handler should not be called when event is handled

        view.Dispose ();
    }

    [Fact]
    public void View_UnhandledEvent_ContinuesProcessing ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10 };
        var eventHandlerCalled = false;
        var clickHandlerCalled = false;

        view.MouseEvent += (sender, args) =>
        {
            eventHandlerCalled = true;
            // Don't set Handled = true
        };

        view.MouseClick += (sender, args) => { clickHandlerCalled = true; };

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        view.NewMouseEvent (mouseEvent);

        // Assert
        Assert.True (eventHandlerCalled);
        Assert.True (clickHandlerCalled); // Click handler should be called when event is not handled

        view.Dispose ();
    }

    #endregion

    #region Mouse Button Events

    [Theory]
    [InlineData (MouseFlags.Button1Pressed, 1, 0, 0)]
    [InlineData (MouseFlags.Button1Released, 0, 1, 0)]
    [InlineData (MouseFlags.Button1Clicked, 0, 0, 1)]
    public void View_MouseButtonEvents_RaiseCorrectHandlers (MouseFlags flags, int expectedPressed, int expectedReleased, int expectedClicked)
    {
        // Arrange
        View view = new () { Width = 10, Height = 10 };
        var pressedCount = 0;
        var releasedCount = 0;
        var clickedCount = 0;

        view.MouseEvent += (sender, args) =>
        {
            if (args.Flags.HasFlag (MouseFlags.Button1Pressed))
            {
                pressedCount++;
            }

            if (args.Flags.HasFlag (MouseFlags.Button1Released))
            {
                releasedCount++;
            }
        };

        view.MouseClick += (sender, args) => { clickedCount++; };

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (5, 5),
            Flags = flags
        };

        // Act
        view.NewMouseEvent (mouseEvent);

        // Assert
        Assert.Equal (expectedPressed, pressedCount);
        Assert.Equal (expectedReleased, releasedCount);
        Assert.Equal (expectedClicked, clickedCount);

        view.Dispose ();
    }

    [Theory]
    [InlineData (MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Clicked)]
    [InlineData (MouseFlags.Button3Clicked)]
    [InlineData (MouseFlags.Button4Clicked)]
    public void View_AllMouseButtons_TriggerClickEvent (MouseFlags clickFlag)
    {
        // Arrange
        View view = new () { Width = 10, Height = 10 };
        var clickCount = 0;

        view.MouseClick += (sender, args) => clickCount++;

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (5, 5),
            Flags = clickFlag
        };

        // Act
        view.NewMouseEvent (mouseEvent);

        // Assert
        Assert.Equal (1, clickCount);

        view.Dispose ();
    }

    #endregion

    #region Disabled View Tests

    [Fact]
    public void View_Disabled_DoesNotRaiseMouseEvent ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
            Enabled = false
        };

        var eventCalled = false;
        view.MouseEvent += (sender, args) => { eventCalled = true; };

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        view.NewMouseEvent (mouseEvent);

        // Assert
        Assert.False (eventCalled);

        view.Dispose ();
    }

    [Fact]
    public void View_Disabled_DoesNotRaiseSelectingEvent ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
            Enabled = false
        };

        var selectingCalled = false;
        view.Selecting += (sender, args) => { selectingCalled = true; };

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        view.NewMouseEvent (mouseEvent);

        // Assert
        Assert.False (selectingCalled);

        view.Dispose ();
    }

    #endregion

    #region Focus and Selection Tests

    [Theory]
    [InlineData (true, true)]
    [InlineData (false, false)]
    public void MouseClick_SetsFocus_BasedOnCanFocus (bool canFocus, bool expectFocus)
    {
        // Arrange
        View superView = new () { CanFocus = true, Width = 20, Height = 20 };
        View subView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            CanFocus = canFocus
        };

        superView.Add (subView);
        superView.SetFocus (); // Give superView focus first

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (2, 2),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        subView.NewMouseEvent (mouseEvent);

        // Assert
        Assert.Equal (expectFocus, subView.HasFocus);

        subView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void MouseClick_RaisesSelecting_WhenCanFocus ()
    {
        // Arrange
        View superView = new () { CanFocus = true, Width = 20, Height = 20 };
        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            CanFocus = true
        };

        superView.Add (view);

        var selectingCount = 0;
        view.Selecting += (sender, args) => selectingCount++;

        MouseEventArgs mouseEvent = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        view.NewMouseEvent (mouseEvent);

        // Assert
        Assert.Equal (1, selectingCount);

        view.Dispose ();
        superView.Dispose ();
    }

    #endregion
}
