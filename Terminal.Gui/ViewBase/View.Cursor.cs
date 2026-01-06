namespace Terminal.Gui.ViewBase;
public partial class View
{
    private Point? _cursorPosition;
    private CursorVisibility _cursorVisibility = CursorVisibility.Invisible;

    /// <summary>
    ///     Gets the viewport-relative cursor position, or <see langword="null"/> to hide the cursor.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is set by calling <see cref="SetCursor(Point?, CursorVisibility)"/>.
    ///         Views should call <see cref="SetCursor(Point?, CursorVisibility)"/> when the cursor position
    ///         or visibility changes.
    ///     </para>
    ///     <para>
    ///         The cursor will only be visible when:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description><see cref="Enabled"/> == <see langword="true"/></description></item>
    ///         <item><description><see cref="Visible"/> == <see langword="true"/></description></item>
    ///         <item><description><see cref="CanFocus"/> == <see langword="true"/></description></item>
    ///         <item><description><see cref="HasFocus"/> == <see langword="true"/></description></item>
    ///         <item><description>The view is the most focused view (deepest in focus chain)</description></item>
    ///         <item><description>The cursor position is within the viewport (not scrolled out of view)</description></item>
    ///     </list>
    /// </remarks>
    public Point? CursorPosition => _cursorPosition;

    /// <summary>
    ///     Gets the cursor style for this view. The default is <see cref="Drivers.CursorVisibility.Invisible"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is set by calling <see cref="SetCursor"/>.
    ///     </para>
    ///     <para>
    ///         The cursor will only be visible when the view is enabled, visible, can focus, has focus,
    ///         is the most focused view, and the cursor position is within the viewport.
    ///     </para>
    /// </remarks>
    public CursorVisibility CursorVisibility => _cursorVisibility;

    /// <summary>
    ///     Sets the cursor position and visibility for the view.
    /// </summary>
    /// <param name="position">
    ///     The content area-relative cursor position, or <see langword="null"/> to hide the cursor.
    /// </param>
    /// <param name="visibility">The cursor visibility style.</param>
    /// <remarks>
    ///     <para>
    ///         Call this method when the cursor position or visibility changes. This is more efficient
    ///         than calling <see cref="View.SetNeedsDraw()"/> when only the cursor needs to move.
    ///     </para>
    ///     <para>
    ///         Common use cases include:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description>User typing in a text field - cursor moves right after each character</description></item>
    ///         <item><description>User presses arrow keys in a text view - cursor moves without content changing</description></item>
    ///         <item><description>Programmatic cursor position changes</description></item>
    ///     </list>
    ///     <para>
    ///         <b>IMPORTANT:</b> The position should be content area-relative coordinates.
    ///         The framework will convert to screen coordinates and check viewport bounds.
    ///     </para>
    /// </remarks>
    public void SetCursor (Point? position, CursorVisibility visibility)
    {
        bool changed = false;

        if (_cursorPosition != position)
        {
            _cursorPosition = position;
            changed = true;
        }

        if (_cursorVisibility != visibility)
        {
            _cursorVisibility = visibility;
            changed = true;
        }

        if (changed && HasFocus)
        {
            SetCursorNeedsUpdate ();
        }
    }

    /// <summary>
    ///     Signals that the cursor position needs to be updated without requiring a full redraw.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is automatically called by <see cref="SetCursor"/> when
    ///         the cursor position or visibility changes. You typically don't need to call this directly.
    ///     </para>
    ///     <para>
    ///         Call this method manually only if you need to force a cursor update without changing
    ///         the position or visibility (e.g., after a layout change that might affect cursor visibility).
    ///     </para>
    /// </remarks>
    public void SetCursorNeedsUpdate ()
    {
        App?.Driver?.SetCursorNeedsUpdate (true);
    }
}
