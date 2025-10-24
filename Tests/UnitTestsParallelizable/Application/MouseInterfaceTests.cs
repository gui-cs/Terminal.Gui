using Terminal.Gui.App;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.ApplicationTests;

/// <summary>
///     Parallelizable tests for IMouse interface.
///     Tests the decoupled mouse handling without Application.Init or global state.
/// </summary>
[Trait ("Category", "Input")]
public class MouseInterfaceTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region IMouse Basic Properties

    [Fact]
    public void Mouse_LastMousePosition_InitiallyNull ()
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);

        // Act & Assert
        Assert.Null (mouse.LastMousePosition);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (10, 20)]
    [InlineData (-5, -10)]
    [InlineData (100, 200)]
    public void Mouse_LastMousePosition_CanBeSetAndRetrieved (int x, int y)
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);
        var testPosition = new Point (x, y);

        // Act
        mouse.LastMousePosition = testPosition;

        // Assert
        Assert.Equal (testPosition, mouse.LastMousePosition);
        Assert.Equal (testPosition, mouse.GetLastMousePosition ());
    }

    [Fact]
    public void Mouse_IsMouseDisabled_DefaultsFalse ()
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);

        // Act & Assert
        Assert.False (mouse.IsMouseDisabled);
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Mouse_IsMouseDisabled_CanBeSetAndRetrieved (bool disabled)
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);

        // Act
        mouse.IsMouseDisabled = disabled;

        // Assert
        Assert.Equal (disabled, mouse.IsMouseDisabled);
    }

    [Fact]
    public void Mouse_CachedViewsUnderMouse_InitiallyEmpty ()
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);

        // Act & Assert
        Assert.NotNull (mouse.CachedViewsUnderMouse);
        Assert.Empty (mouse.CachedViewsUnderMouse);
    }

    #endregion

    #region IMouse Event Handling

    [Fact]
    public void Mouse_MouseEvent_CanSubscribeAndFire ()
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);
        var eventFired = false;
        MouseEventArgs capturedArgs = null;

        mouse.MouseEvent += (sender, args) =>
        {
            eventFired = true;
            capturedArgs = args;
        };

        var testEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (5, 10),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        mouse.RaiseMouseEvent (testEvent);

        // Assert
        Assert.True (eventFired);
        Assert.NotNull (capturedArgs);
        Assert.Equal (testEvent.ScreenPosition, capturedArgs.ScreenPosition);
        Assert.Equal (testEvent.Flags, capturedArgs.Flags);
    }

    [Fact]
    public void Mouse_MouseEvent_CanUnsubscribe ()
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);
        var eventCount = 0;

        void Handler (object sender, MouseEventArgs args) => eventCount++;

        mouse.MouseEvent += Handler;

        var testEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (0, 0),
            Flags = MouseFlags.Button1Pressed
        };

        // Act - Fire once
        mouse.RaiseMouseEvent (testEvent);
        Assert.Equal (1, eventCount);

        // Unsubscribe
        mouse.MouseEvent -= Handler;

        // Fire again
        mouse.RaiseMouseEvent (testEvent);

        // Assert - Count should not increase
        Assert.Equal (1, eventCount);
    }

    [Fact]
    public void Mouse_RaiseMouseEvent_WithDisabledMouse_DoesNotFireEvent ()
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);
        var eventFired = false;

        mouse.MouseEvent += (sender, args) => { eventFired = true; };
        mouse.IsMouseDisabled = true;

        var testEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (0, 0),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        mouse.RaiseMouseEvent (testEvent);

        // Assert
        Assert.False (eventFired);
    }

    [Theory]
    [InlineData (MouseFlags.Button1Pressed)]
    [InlineData (MouseFlags.Button1Released)]
    [InlineData (MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Pressed)]
    [InlineData (MouseFlags.WheeledUp)]
    [InlineData (MouseFlags.ReportMousePosition)]
    public void Mouse_RaiseMouseEvent_CorrectlyPassesFlags (MouseFlags flags)
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);
        MouseFlags? capturedFlags = null;

        mouse.MouseEvent += (sender, args) => { capturedFlags = args.Flags; };

        var testEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (5, 5),
            Flags = flags
        };

        // Act
        mouse.RaiseMouseEvent (testEvent);

        // Assert
        Assert.NotNull (capturedFlags);
        Assert.Equal (flags, capturedFlags.Value);
    }

    #endregion

    #region IMouse ResetState

    [Fact]
    public void Mouse_ResetState_ClearsCachedViews ()
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);
        var testView = new View { Width = 10, Height = 10 };

        mouse.CachedViewsUnderMouse.Add (testView);
        Assert.Single (mouse.CachedViewsUnderMouse);

        // Act
        mouse.ResetState ();

        // Assert
        Assert.Empty (mouse.CachedViewsUnderMouse);

        testView.Dispose ();
    }

    [Fact]
    public void Mouse_ResetState_ClearsEventHandlers ()
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);
        var eventCount = 0;

        mouse.MouseEvent += (sender, args) => eventCount++;

        var testEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (0, 0),
            Flags = MouseFlags.Button1Pressed
        };

        // Verify event fires before reset
        mouse.RaiseMouseEvent (testEvent);
        Assert.Equal (1, eventCount);

        // Act
        mouse.ResetState ();

        // Raise event again
        mouse.RaiseMouseEvent (testEvent);

        // Assert - Event count should not increase after reset
        Assert.Equal (1, eventCount);
    }

    [Fact]
    public void Mouse_ResetState_DoesNotClearLastMousePosition ()
    {
        // Arrange
        var mouseGrabHandler = new MouseGrabHandler ();
        var mouse = new Mouse (mouseGrabHandler);
        var testPosition = new Point (42, 84);

        mouse.LastMousePosition = testPosition;

        // Act
        mouse.ResetState ();

        // Assert - LastMousePosition should NOT be cleared (per design)
        Assert.Equal (testPosition, mouse.LastMousePosition);
    }

    #endregion

    #region IMouse Isolation

    [Fact]
    public void Mouse_Instances_AreIndependent ()
    {
        // Arrange
        var grabHandler1 = new MouseGrabHandler ();
        var mouse1 = new Mouse (grabHandler1);

        var grabHandler2 = new MouseGrabHandler ();
        var mouse2 = new Mouse (grabHandler2);

        // Act
        mouse1.IsMouseDisabled = true;
        mouse1.LastMousePosition = new Point (10, 10);

        // Assert - mouse2 should be unaffected
        Assert.False (mouse2.IsMouseDisabled);
        Assert.Null (mouse2.LastMousePosition);
    }

    [Fact]
    public void Mouse_Events_AreIndependent ()
    {
        // Arrange
        var grabHandler1 = new MouseGrabHandler ();
        var mouse1 = new Mouse (grabHandler1);
        var mouse1EventCount = 0;

        var grabHandler2 = new MouseGrabHandler ();
        var mouse2 = new Mouse (grabHandler2);
        var mouse2EventCount = 0;

        mouse1.MouseEvent += (sender, args) => mouse1EventCount++;
        mouse2.MouseEvent += (sender, args) => mouse2EventCount++;

        var testEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (0, 0),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        mouse1.RaiseMouseEvent (testEvent);

        // Assert
        Assert.Equal (1, mouse1EventCount);
        Assert.Equal (0, mouse2EventCount);
    }

    [Fact]
    public void Mouse_CachedViews_AreIndependent ()
    {
        // Arrange
        var grabHandler1 = new MouseGrabHandler ();
        var mouse1 = new Mouse (grabHandler1);

        var grabHandler2 = new MouseGrabHandler ();
        var mouse2 = new Mouse (grabHandler2);

        var view1 = new View ();
        var view2 = new View ();

        // Act
        mouse1.CachedViewsUnderMouse.Add (view1);
        mouse2.CachedViewsUnderMouse.Add (view2);

        // Assert
        Assert.Single (mouse1.CachedViewsUnderMouse);
        Assert.Single (mouse2.CachedViewsUnderMouse);
        Assert.Contains (view1, mouse1.CachedViewsUnderMouse);
        Assert.Contains (view2, mouse2.CachedViewsUnderMouse);
        Assert.DoesNotContain (view2, mouse1.CachedViewsUnderMouse);
        Assert.DoesNotContain (view1, mouse2.CachedViewsUnderMouse);

        view1.Dispose ();
        view2.Dispose ();
    }

    #endregion
}
