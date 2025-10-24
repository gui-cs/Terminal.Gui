#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.App;

public static partial class Application // Mouse handling
{
    /// <summary>
    ///     Gets the <see cref="IMouse"/> instance that manages mouse event handling and state.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property provides access to mouse-related functionality in a way that supports
    ///         parallel test execution by avoiding static state.
    ///     </para>
    ///     <para>
    ///         New code should use <c>Application.Mouse</c> instead of the static properties and methods
    ///         for better testability. Legacy static properties like <see cref="IsMouseDisabled"/> and
    ///         <see cref="GetLastMousePosition"/> are retained for backward compatibility.
    ///     </para>
    /// </remarks>
    public static IMouse Mouse
    {
        get => ApplicationImpl.Instance.Mouse;
    }

    /// <summary>
    /// INTERNAL API: Holds the last mouse position.
    /// </summary>
    internal static Point? LastMousePosition
    {
        get => Mouse.LastMousePosition;
        set => Mouse.LastMousePosition = value;
    }

    /// <summary>
    ///     Gets the most recent position of the mouse.
    /// </summary>
    public static Point? GetLastMousePosition () { return Mouse.GetLastMousePosition (); }

    /// <summary>Disable or enable the mouse. The mouse is enabled by default.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool IsMouseDisabled
    {
        get => Mouse.IsMouseDisabled;
        set => Mouse.IsMouseDisabled = value;
    }

    /// <summary>
    /// Static reference to the current <see cref="IApplication"/> <see cref="IMouseGrabHandler"/>.
    /// </summary>
    public static IMouseGrabHandler MouseGrabHandler
    {
        get => ApplicationImpl.Instance.MouseGrabHandler;
        set => ApplicationImpl.Instance.MouseGrabHandler = value ??
                                                           throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     INTERNAL API: Called when a mouse event is raised by the driver. Determines the view under the mouse and
    ///     calls the appropriate View mouse event handlers.
    /// </summary>
    /// <remarks>This method can be used to simulate a mouse event, e.g. in unit tests.</remarks>
    /// <param name="mouseEvent">The mouse event with coordinates relative to the screen.</param>
    internal static void RaiseMouseEvent (MouseEventArgs mouseEvent)
    {
        Mouse.RaiseMouseEvent (mouseEvent);
    }


#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
    /// <summary>
    /// Raised when a mouse event occurs. Can be cancelled by setting <see cref="HandledEventArgs.Handled"/> to <see langword="true"/>.
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
    public static event EventHandler<MouseEventArgs>? MouseEvent
    {
        add => Mouse.MouseEvent += value;
        remove => Mouse.MouseEvent -= value;
    }
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved

    internal static bool HandleMouseGrab (View? deepestViewUnderMouse, MouseEventArgs mouseEvent)
    {
        return MouseGrabHandler.HandleMouseGrab (deepestViewUnderMouse, mouseEvent);
    }

    /// <summary>
    ///     INTERNAL: Holds the non-<see cref="ViewportSettingsFlags.TransparentMouse"/> views that are currently under the mouse.
    /// </summary>
    internal static List<View?> CachedViewsUnderMouse => Mouse.CachedViewsUnderMouse;

    /// <summary>
    ///     INTERNAL: Raises the MouseEnter and MouseLeave events for the views that are under the mouse.
    /// </summary>
    /// <param name="screenPosition">The position of the mouse.</param>
    /// <param name="currentViewsUnderMouse">The most recent result from GetViewsUnderLocation().</param>
    internal static void RaiseMouseEnterLeaveEvents (Point screenPosition, List<View?> currentViewsUnderMouse)
    {
        Mouse.RaiseMouseEnterLeaveEvents (screenPosition, currentViewsUnderMouse);
    }

    /// <summary>
    ///     INTERNAL: Clears mouse state during application reset.
    /// </summary>
    internal static void ResetMouseState ()
    {
        Mouse.ResetState ();
    }
}
