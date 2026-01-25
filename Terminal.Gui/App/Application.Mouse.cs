using System.ComponentModel;

namespace Terminal.Gui.App;

public static partial class Application // Mouse handling
{
    private static bool _isMouseDisabled = false; // Resources/config.json overrides

    /// <summary>Disable or enable the mouse. The mouse is enabled by default.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool IsMouseDisabled
    {
        get => _isMouseDisabled;
        set
        {
            bool oldValue = _isMouseDisabled;
            _isMouseDisabled = value;
            IsMouseDisabledChanged?.Invoke (null, new ValueChangedEventArgs<bool> (oldValue, _isMouseDisabled));
        }
    }

    /// <summary>Raised when <see cref="IsMouseDisabled"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<bool>>? IsMouseDisabledChanged;

    /// <summary>
    ///     Gets the <see cref="IMouse"/> instance that manages mouse event handling and state.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property provides access to mouse-related functionality in a way that supports
    ///         parallel test execution by avoiding static state.
    ///     </para>
    /// </remarks>
    [Obsolete ("The legacy static Application object is going away.")]
    public static IMouse Mouse => ApplicationImpl.Instance.Mouse;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
    /// <summary>
    ///     Raised when a mouse event occurs. Can be cancelled by setting <see cref="HandledEventArgs.Handled"/> to
    ///     <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Mouse.ScreenPosition"/> coordinates are screen-relative.
    ///     </para>
    ///     <para>
    ///         <see cref="Mouse.View"/> will be the deepest view under the mouse.
    ///     </para>
    ///     <para>
    ///         <see cref="Mouse.Position"/> coordinates are view-relative. Only valid if
    ///         <see cref="Mouse.View"/> is set.
    ///     </para>
    ///     <para>
    ///         Use this even to handle mouse events at the application level, before View-specific handling.
    ///     </para>
    /// </remarks>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<Mouse>? MouseEvent
    {
        add => Mouse.MouseEvent += value;
        remove => Mouse.MouseEvent -= value;
    }
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved

    /// <summary>
    ///     INTERNAL: Holds the non-<see cref="ViewportSettingsFlags.TransparentMouse"/> views that are currently under the
    ///     mouse.
    /// </summary>
    [Obsolete ("The legacy static Application object is going away.")]
    internal static List<View?> CachedViewsUnderMouse => Mouse.CachedViewsUnderMouse;

    /// <summary>
    ///     INTERNAL API: Holds the last mouse position.
    /// </summary>
    [Obsolete ("The legacy static Application object is going away.")]
    internal static Point? LastMousePosition
    {
        get => Mouse.LastMousePosition;
        set => Mouse.LastMousePosition = value;
    }

    /// <summary>
    ///     INTERNAL: Raises the MouseEnter and MouseLeave events for the views that are under the mouse.
    /// </summary>
    /// <param name="screenPosition">The position of the mouse.</param>
    /// <param name="currentViewsUnderMouse">The most recent result from GetViewsUnderLocation().</param>
    [Obsolete ("The legacy static Application object is going away.")]
    internal static void RaiseMouseEnterLeaveEvents (Point screenPosition, List<View?> currentViewsUnderMouse)
    {
        Mouse.RaiseMouseEnterLeaveEvents (screenPosition, currentViewsUnderMouse);
    }

    /// <summary>
    ///     INTERNAL API: Called when a mouse event is raised by the driver. Determines the view under the mouse and
    ///     calls the appropriate View mouse event handlers.
    /// </summary>
    /// <remarks>This method can be used to simulate a mouse event, e.g. in unit tests.</remarks>
    /// <param name="mouse">The mouse event with coordinates relative to the screen.</param>
    [Obsolete ("The legacy static Application object is going away.")]
    internal static void RaiseMouseEvent (Mouse mouse)
    {
        Mouse.RaiseMouseEvent (mouse);
    }
}
