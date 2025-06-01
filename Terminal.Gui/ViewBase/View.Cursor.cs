#nullable enable

namespace Terminal.Gui.ViewBase;

public partial class View
{
    /// <summary>
    ///     Gets or sets the cursor style to be used when the view is focused. The default is
    ///     <see cref="Drivers.CursorVisibility.Invisible"/>.
    /// </summary>
    public CursorVisibility CursorVisibility { get; set; } = CursorVisibility.Invisible;

    /// <summary>
    ///     Positions the cursor in the right position based on the currently focused view in the chain.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Views that are focusable should override <see cref="PositionCursor()"/> to make sure that the cursor is
    ///         placed in a location that makes sense. Some terminals do not have a way of hiding the cursor, so it can be
    ///         distracting to have the cursor left at the last focused view. So views should make sure that they place the
    ///         cursor in a visually sensible place. The default implementation of <see cref="PositionCursor()"/> will place the
    ///         cursor at either the hotkey (if defined) or <c>0,0</c>.
    ///     </para>
    /// </remarks>
    /// <returns>Viewport-relative cursor position. Return <see langword="null"/> to ensure the cursor is not visible.</returns>
    public virtual Point? PositionCursor ()
    {
        if (IsInitialized && CanFocus && HasFocus)
        {
            // By default, position the cursor at the hotkey (if any) or 0, 0.
            Move (TextFormatter.HotKeyPos == -1 ? 0 : TextFormatter.CursorPosition, 0);
        }

        // Returning null will hide the cursor.
        return null;
    }
}
