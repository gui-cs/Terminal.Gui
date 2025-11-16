using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
///     Defines a contract for mouse event handling and state management in a Terminal.Gui application.
///     <para>
///         This interface allows for decoupling of mouse-related functionality from the static <see cref="Application"/> class,
///         enabling better testability and parallel test execution.
///     </para>
/// </summary>
public interface IMouse : IMouseGrabHandler
{
    /// <summary>
    /// Sets the application instance that this mouse handler is associated with.
    /// This provides access to application state without coupling to static Application class.
    /// </summary>
    IApplication? Application { get; set; }

    /// <summary>
    ///     Gets or sets the last known position of the mouse.
    /// </summary>
    Point? LastMousePosition { get; set; }

    /// <summary>
    ///     Gets the most recent position of the mouse.
    /// </summary>
    Point? GetLastMousePosition ();

    /// <summary>
    ///     Gets or sets whether the mouse is disabled. The mouse is enabled by default.
    /// </summary>
    bool IsMouseDisabled { get; set; }

    /// <summary>
    ///     Gets the list of non-<see cref="ViewportSettingsFlags.TransparentMouse"/> views that are currently under the mouse.
    /// </summary>
    List<View?> CachedViewsUnderMouse { get; }

    /// <summary>
    ///     Raised when a mouse event occurs. Can be cancelled by setting <see cref="HandledEventArgs.Handled"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="MouseEventArgs.ScreenPosition"/> coordinates are screen-relative.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseEventArgs.View"/> will be the deepest view under the mouse.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseEventArgs.Position"/> coordinates are view-relative. Only valid if <see cref="MouseEventArgs.View"/> is set.
    ///     </para>
    ///     <para>
    ///         Use this even to handle mouse events at the application level, before View-specific handling.
    ///     </para>
    /// </remarks>
    event EventHandler<MouseEventArgs>? MouseEvent;

    /// <summary>
    ///     INTERNAL API: Called when a mouse event is raised by the driver. Determines the view under the mouse and
    ///     calls the appropriate View mouse event handlers.
    /// </summary>
    /// <remarks>This method can be used to simulate a mouse event, e.g. in unit tests.</remarks>
    /// <param name="mouseEvent">The mouse event with coordinates relative to the screen.</param>
    void RaiseMouseEvent (MouseEventArgs mouseEvent);

    /// <summary>
    ///     INTERNAL: Raises the MouseEnter and MouseLeave events for the views that are under the mouse.
    /// </summary>
    /// <param name="screenPosition">The position of the mouse.</param>
    /// <param name="currentViewsUnderMouse">The most recent result from GetViewsUnderLocation().</param>
    void RaiseMouseEnterLeaveEvents (Point screenPosition, List<View?> currentViewsUnderMouse);

    /// <summary>
    ///     INTERNAL: Resets mouse state, clearing event handlers and cached views.
    /// </summary>
    void ResetState ();
}
