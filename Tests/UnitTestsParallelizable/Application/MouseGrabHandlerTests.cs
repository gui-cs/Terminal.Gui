using Terminal.Gui.App;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.ApplicationTests;

/// <summary>
///     Parallelizable tests for IMouseGrabHandler interface.
///     Tests the decoupled mouse grab handling without Application.Init or global state.
/// </summary>
[Trait ("Category", "Input")]
public class MouseGrabHandlerTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Basic Grab/Ungrab

    [Fact]
    public void MouseGrabHandler_MouseGrabView_InitiallyNull ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();

        // Act & Assert
        Assert.Null (handler.MouseGrabView);
    }

    [Fact]
    public void MouseGrabHandler_GrabMouse_SetsMouseGrabView ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View { Id = "testView" };

        // Act
        handler.GrabMouse (view);

        // Assert
        Assert.NotNull (handler.MouseGrabView);
        Assert.Equal (view, handler.MouseGrabView);

        view.Dispose ();
    }

    [Fact]
    public void MouseGrabHandler_UngrabMouse_ClearsMouseGrabView ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View { Id = "testView" };
        handler.GrabMouse (view);
        Assert.NotNull (handler.MouseGrabView);

        // Act
        handler.UngrabMouse ();

        // Assert
        Assert.Null (handler.MouseGrabView);

        view.Dispose ();
    }

    [Fact]
    public void MouseGrabHandler_UngrabMouse_WhenNoGrab_DoesNothing ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        Assert.Null (handler.MouseGrabView);

        // Act & Assert - Should not throw
        handler.UngrabMouse ();
        Assert.Null (handler.MouseGrabView);
    }

    [Fact]
    public void MouseGrabHandler_GrabMouse_WithNull_DoesNothing ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();

        // Act
        handler.GrabMouse (null);

        // Assert
        Assert.Null (handler.MouseGrabView);
    }

    #endregion

    #region GrabbingMouse Event

    [Fact]
    public void MouseGrabHandler_GrabbingMouse_FiredBeforeGrab ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View { Id = "testView" };
        View capturedView = null;
        var eventFired = false;

        handler.GrabbingMouse += (sender, args) =>
        {
            eventFired = true;
            capturedView = args.View;
            // At this point, MouseGrabView should still be null
            Assert.Null (handler.MouseGrabView);
        };

        // Act
        handler.GrabMouse (view);

        // Assert
        Assert.True (eventFired);
        Assert.Equal (view, capturedView);
        Assert.NotNull (handler.MouseGrabView);

        view.Dispose ();
    }

    [Fact]
    public void MouseGrabHandler_GrabbingMouse_CanCancelGrab ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View { Id = "testView" };

        handler.GrabbingMouse += (sender, args) => { args.Cancel = true; };

        // Act
        handler.GrabMouse (view);

        // Assert
        Assert.Null (handler.MouseGrabView);

        view.Dispose ();
    }

    #endregion

    #region GrabbedMouse Event

    [Fact]
    public void MouseGrabHandler_GrabbedMouse_FiredDuringGrab ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View { Id = "testView" };
        View capturedView = null;
        var eventFired = false;

        handler.GrabbedMouse += (sender, args) =>
        {
            eventFired = true;
            capturedView = args.View;
            // GrabbedMouse is fired BEFORE MouseGrabView is actually set
            // This allows handlers to know about the grab before it completes
        };

        // Act
        handler.GrabMouse (view);

        // Assert
        Assert.True (eventFired);
        Assert.Equal (view, capturedView);
        // After GrabMouse completes, MouseGrabView should be set
        Assert.NotNull (handler.MouseGrabView);
        Assert.Equal (view, handler.MouseGrabView);

        view.Dispose ();
    }

    #endregion

    #region UnGrabbingMouse Event

    [Fact]
    public void MouseGrabHandler_UnGrabbingMouse_FiredBeforeUngrab ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View { Id = "testView" };
        handler.GrabMouse (view);

        View capturedView = null;
        var eventFired = false;

        handler.UnGrabbingMouse += (sender, args) =>
        {
            eventFired = true;
            capturedView = args.View;
            // At this point, MouseGrabView should still be set
            Assert.NotNull (handler.MouseGrabView);
        };

        // Act
        handler.UngrabMouse ();

        // Assert
        Assert.True (eventFired);
        Assert.Equal (view, capturedView);
        Assert.Null (handler.MouseGrabView);

        view.Dispose ();
    }

    [Fact]
    public void MouseGrabHandler_UnGrabbingMouse_CanCancelUngrab ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View { Id = "testView" };
        handler.GrabMouse (view);

        handler.UnGrabbingMouse += (sender, args) => { args.Cancel = true; };

        // Act
        handler.UngrabMouse ();

        // Assert - Ungrab was cancelled, view should still be grabbed
        Assert.NotNull (handler.MouseGrabView);
        Assert.Equal (view, handler.MouseGrabView);

        view.Dispose ();
    }

    #endregion

    #region UnGrabbedMouse Event

    [Fact]
    public void MouseGrabHandler_UnGrabbedMouse_FiredAfterUngrab ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View { Id = "testView" };
        handler.GrabMouse (view);

        View capturedView = null;
        var eventFired = false;

        handler.UnGrabbedMouse += (sender, args) =>
        {
            eventFired = true;
            capturedView = args.View;
            // At this point, MouseGrabView should be null
            Assert.Null (handler.MouseGrabView);
        };

        // Act
        handler.UngrabMouse ();

        // Assert
        Assert.True (eventFired);
        Assert.Equal (view, capturedView);

        view.Dispose ();
    }

    #endregion

    #region HandleMouseGrab

    [Fact]
    public void MouseGrabHandler_HandleMouseGrab_NoGrab_ReturnsFalse ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var mouseEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (10, 10),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        bool result = handler.HandleMouseGrab (null, mouseEvent);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void MouseGrabHandler_HandleMouseGrab_WithGrabbedView_RoutesEventToView ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View
        {
            Width = 20,
            Height = 20
        };

        var eventReceived = false;
        view.MouseEvent += (sender, args) =>
        {
            eventReceived = true;
            args.Handled = true;
        };

        handler.GrabMouse (view);

        var mouseEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (10, 10),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        bool result = handler.HandleMouseGrab (null, mouseEvent);

        // Assert
        Assert.True (result);
        Assert.True (eventReceived);

        view.Dispose ();
    }

    [Fact]
    public void MouseGrabHandler_HandleMouseGrab_ViewDoesNotHandle_ReturnsFalse ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view = new View
        {
            Width = 20,
            Height = 20
        };

        var eventReceived = false;
        view.MouseEvent += (sender, args) =>
        {
            eventReceived = true;
            // Don't set Handled = true
        };

        handler.GrabMouse (view);

        var mouseEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (10, 10),
            Flags = MouseFlags.Button1Pressed
        };

        // Act
        bool result = handler.HandleMouseGrab (null, mouseEvent);

        // Assert
        Assert.False (result);
        Assert.True (eventReceived);

        view.Dispose ();
    }

    #endregion

    #region Multiple Views

    [Fact]
    public void MouseGrabHandler_GrabMouse_MultipleTimes_ReplacesGrab ()
    {
        // Arrange
        var handler = new MouseGrabHandler ();
        var view1 = new View { Id = "view1" };
        var view2 = new View { Id = "view2" };

        // Act
        handler.GrabMouse (view1);
        Assert.Equal (view1, handler.MouseGrabView);

        handler.GrabMouse (view2);

        // Assert
        Assert.Equal (view2, handler.MouseGrabView);

        view1.Dispose ();
        view2.Dispose ();
    }

    #endregion

    #region Handler Isolation

    [Fact]
    public void MouseGrabHandler_Instances_AreIndependent ()
    {
        // Arrange
        var handler1 = new MouseGrabHandler ();
        var handler2 = new MouseGrabHandler ();
        var view1 = new View { Id = "view1" };
        var view2 = new View { Id = "view2" };

        // Act
        handler1.GrabMouse (view1);
        handler2.GrabMouse (view2);

        // Assert
        Assert.Equal (view1, handler1.MouseGrabView);
        Assert.Equal (view2, handler2.MouseGrabView);
        Assert.NotEqual (handler1.MouseGrabView, handler2.MouseGrabView);

        view1.Dispose ();
        view2.Dispose ();
    }

    [Fact]
    public void MouseGrabHandler_Events_AreIndependent ()
    {
        // Arrange
        var handler1 = new MouseGrabHandler ();
        var handler2 = new MouseGrabHandler ();
        var view = new View { Id = "testView" };

        var handler1GrabbedFired = false;
        var handler2GrabbedFired = false;

        handler1.GrabbedMouse += (sender, args) => { handler1GrabbedFired = true; };

        handler2.GrabbedMouse += (sender, args) => { handler2GrabbedFired = true; };

        // Act
        handler1.GrabMouse (view);

        // Assert
        Assert.True (handler1GrabbedFired);
        Assert.False (handler2GrabbedFired);

        view.Dispose ();
    }

    #endregion
}
