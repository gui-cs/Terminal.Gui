namespace Terminal.Gui.ViewBase;

public partial class View
{
    private Cursor _cursor = new () { Position = null };

    /// <summary>
    /// Gets the current cursor for this view.
    /// </summary>
    /// <remarks>
    /// Use <see cref="SetCursor"/> to update the cursor position and shape.
    /// The cursor will only be visible when the view has focus and is the most focused view.
    /// Position is always in screen-absolute coordinates.
    /// </remarks>
    public Cursor Cursor => _cursor;

    /// <summary>
    ///     Sets the cursor for this view.
    /// </summary>
    /// <param name="cursor">
    ///     The cursor to set. Position must be in screen-absolute coordinates.
    ///     Use <c>ContentToScreen()</c> or <c>ViewportToScreen()</c> to convert from view-relative coordinates.
    ///     Set Position to null to hide the cursor.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         Common patterns:
    ///         <code>
    /// // Text cursor at column 5 in content area - convert to screen coords
    /// Point screenPos = ContentToScreen(new Point(5, 0));
    /// SetCursor(new Cursor { Position = screenPos, Shape = CursorShape.BlinkingBar });
    /// 
    /// // Hide cursor
    /// SetCursor(new Cursor { Position = null });
    /// 
    /// // Update position keeping same shape
    /// Point newScreenPos = ContentToScreen(new Point(6, 0));
    /// SetCursor(_cursor with { Position = newScreenPos });
    /// </code>
    ///     </para>
    ///     <para>
    ///         This is more efficient than calling <see cref="SetNeedsDraw()"/> when only
    ///         the cursor needs to move.
    ///     </para>
    /// </remarks>
    public void SetCursor (Cursor cursor)
    {
        if (_cursor == cursor)
        {
            return;
        }

        _cursor = cursor;

        if (HasFocus)
        {
            SetCursorNeedsUpdate ();
        }
    }

    /// <summary>
    ///     Signals that the cursor position needs to be updated without requiring a full redraw.
    /// </summary>
    public void SetCursorNeedsUpdate ()
    {
        App?.Driver?.SetCursorNeedsUpdate (true);
    }
}
