using Terminal.Gui.App;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.ApplicationTests;

/// <summary>
///     Tests for the <see cref="IMouse"/> interface and <see cref="MouseImpl"/> implementation.
///     These tests demonstrate the decoupled mouse handling that enables parallel test execution.
/// </summary>
public class MouseTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Mouse_Instance_CreatedSuccessfully ()
    {
        // Arrange & Act
        MouseImpl mouse = new ();

        // Assert
        Assert.NotNull (mouse);
        Assert.False (mouse.IsMouseDisabled);
        Assert.Null (mouse.LastMousePosition);
    }

    [Fact]
    public void Mouse_LastMousePosition_CanBeSetAndRetrieved ()
    {
        // Arrange
        MouseImpl mouse = new ();
        Point expectedPosition = new (10, 20);

        // Act
        mouse.LastMousePosition = expectedPosition;
        Point? actualPosition = mouse.LastMousePosition;

        // Assert
        Assert.Equal (expectedPosition, actualPosition);
    }

    [Fact]
    public void Mouse_IsMouseDisabled_CanBeSetAndRetrieved ()
    {
        // Arrange
        MouseImpl mouse = new ();

        // Act
        mouse.IsMouseDisabled = true;

        // Assert
        Assert.True (mouse.IsMouseDisabled);
    }

    [Fact]
    public void Mouse_CachedViewsUnderMouse_InitializedEmpty ()
    {
        // Arrange
        MouseImpl mouse = new ();

        // Assert
        Assert.NotNull (mouse.CachedViewsUnderMouse);
        Assert.Empty (mouse.CachedViewsUnderMouse);
    }

    [Fact]
    public void Mouse_ResetState_ClearsEventAndCachedViews ()
    {
        // Arrange
        MouseImpl mouse = new ();
        var eventFired = false;
        mouse.MouseEvent += (sender, args) => eventFired = true;
        mouse.CachedViewsUnderMouse.Add (new View ());

        // Act
        mouse.ResetState ();

        // Assert - CachedViewsUnderMouse should be cleared
        Assert.Empty (mouse.CachedViewsUnderMouse);
        
        // Event handlers should be cleared
        MouseEventArgs mouseEvent = new () { ScreenPosition = new Point (0, 0), Flags = MouseFlags.Button1Pressed };
        mouse.RaiseMouseEvent (mouseEvent);
        Assert.False (eventFired, "Event should not fire after ResetState");
    }

    [Fact]
    public void Mouse_RaiseMouseEvent_DoesNotUpdateLastPositionWhenNotInitialized ()
    {
        // Arrange
        MouseImpl mouse = new ();
        MouseEventArgs mouseEvent = new () { ScreenPosition = new Point (5, 10), Flags = MouseFlags.Button1Pressed };

        // Act - Application is not initialized, so LastMousePosition should not be set
        mouse.RaiseMouseEvent (mouseEvent);

        // Assert
        // Since Application.Initialized is false, LastMousePosition should remain null
        // This behavior matches the original implementation
        Assert.Null (mouse.LastMousePosition);
    }

    [Fact]
    public void Mouse_MouseEvent_CanBeSubscribedAndUnsubscribed ()
    {
        // Arrange
        MouseImpl mouse = new ();
        var eventCount = 0;
        EventHandler<MouseEventArgs> handler = (sender, args) => eventCount++;

        // Act - Subscribe
        mouse.MouseEvent += handler;
        MouseEventArgs mouseEvent = new () { ScreenPosition = new Point (0, 0), Flags = MouseFlags.Button1Pressed };
        mouse.RaiseMouseEvent (mouseEvent);

        // Assert - Event fired once
        Assert.Equal (1, eventCount);

        // Act - Unsubscribe
        mouse.MouseEvent -= handler;
        mouse.RaiseMouseEvent (mouseEvent);

        // Assert - Event count unchanged
        Assert.Equal (1, eventCount);
    }


    /// <summary>
    ///     Tests that the mouse coordinates passed to the focused view are correct when the mouse is clicked. With
    ///     Frames; Frame != Viewport
    /// </summary>
    [Theory]

    // click on border
    [InlineData (0, 0, 0, 0, 0, false)]
    [InlineData (0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, false)]
    [InlineData (0, 9, 0, 0, 0, false)]
    [InlineData (0, 0, 9, 0, 0, false)]

    // outside border
    [InlineData (0, 10, 0, 0, 0, false)]
    [InlineData (0, 0, 10, 0, 0, false)]

    // view is offset from origin ; click is on border 
    [InlineData (1, 1, 1, 0, 0, false)]
    [InlineData (1, 2, 1, 0, 0, false)]
    [InlineData (1, 1, 2, 0, 0, false)]
    [InlineData (1, 10, 1, 0, 0, false)]
    [InlineData (1, 1, 10, 0, 0, false)]

    // outside border
    [InlineData (1, -1, 0, 0, 0, false)]
    [InlineData (1, 0, -1, 0, 0, false)]
    [InlineData (1, 10, 10, 0, 0, false)]
    [InlineData (1, 11, 11, 0, 0, false)]

    // view is at origin, click is inside border
    [InlineData (0, 1, 1, 0, 0, true)]
    [InlineData (0, 2, 1, 1, 0, true)]
    [InlineData (0, 1, 2, 0, 1, true)]
    [InlineData (0, 8, 1, 7, 0, true)]
    [InlineData (0, 1, 8, 0, 7, true)]
    [InlineData (0, 8, 8, 7, 7, true)]

    // view is offset from origin ; click inside border
    // our view is 10x10, but has a border, so it's bounds is 8x8
    [InlineData (1, 2, 2, 0, 0, true)]
    [InlineData (1, 3, 2, 1, 0, true)]
    [InlineData (1, 2, 3, 0, 1, true)]
    [InlineData (1, 9, 2, 7, 0, true)]
    [InlineData (1, 2, 9, 0, 7, true)]
    [InlineData (1, 9, 9, 7, 7, true)]
    [InlineData (1, 10, 10, 7, 7, false)]

    //01234567890123456789
    // |12345678|
    // |xxxxxxxx
    public void MouseCoordinatesTest_Border (
        int offset,
        int clickX,
        int clickY,
        int expectedX,
        int expectedY,
        bool expectedClicked
    )
    {
        Size size = new (10, 10);
        Point pos = new (offset, offset);

        var clicked = false;

        using IApplication? application = Application.Create ();

        application.Begin (new Window ()
        {
            Id = "top",
        });
        application.TopRunnableView!.X = 0;
        application.TopRunnableView.Y = 0;
        application.TopRunnableView.Width = size.Width * 2;
        application.TopRunnableView.Height = size.Height * 2;
        application.TopRunnableView.BorderStyle = LineStyle.None;

        var view = new View { Id = "view", X = pos.X, Y = pos.Y, Width = size.Width, Height = size.Height };

        // Give the view a border. With PR #2920, mouse clicks are only passed if they are inside the view's Viewport.
        view.BorderStyle = LineStyle.Single;
        view.CanFocus = true;

        application.TopRunnableView.Add (view);

        var mouseEvent = new MouseEventArgs { Position = new (clickX, clickY), ScreenPosition = new (clickX, clickY), Flags = MouseFlags.Button1Clicked };

        view.MouseClick += (s, e) =>
        {
            Assert.Equal (expectedX, e.Position.X);
            Assert.Equal (expectedY, e.Position.Y);
            clicked = true;
        };

        application.Mouse.RaiseMouseEvent (mouseEvent);
        Assert.Equal (expectedClicked, clicked);
    }
}
