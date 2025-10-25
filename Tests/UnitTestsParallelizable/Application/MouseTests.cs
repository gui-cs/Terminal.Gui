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
        Point? actualPosition = mouse.GetLastMousePosition ();

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
}
