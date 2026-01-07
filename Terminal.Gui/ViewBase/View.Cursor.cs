namespace Terminal.Gui.ViewBase;

public partial class View
{
    private Cursor _cursor = new () { Position = null };

    /// <summary>
    ///     Gets or sets the cursor for this view. Position must be in screen coordinates.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <c>ViewportToScreen()</c> to convert from view-relative coordinates.
    ///         Set Position to null to hide the cursor.
    ///     </para>
    ///     <para>
    ///         Common patterns:
    ///         <code>
    /// // Text cursor at column 5 in viewport - convert to screen coords
    /// Point screenPos = ViewportToScreen(new Point(5, 0));
    /// SetCursor(new Cursor { Position = screenPos, Shape = CursorShape.BlinkingBar });
    /// 
    /// // Hide cursor
    /// SetCursor(new Cursor { Position = null });
    /// 
    /// // Update position keeping same shape
    /// Point newScreenPos = ViewportToScreen(new Point(6, 0));
    /// SetCursor(_cursor with { Position = newScreenPos });
    /// </code>
    ///     </para>
    /// </remarks>
    public Cursor Cursor
    {
        get => _cursor;
        set => SetCursor (value);
    }

    /// <summary>
    ///     INTERNAL: Sets the cursor for this view.
    /// </summary>
    private void SetCursor (Cursor cursor)
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
    public void SetCursorNeedsUpdate () { App?.Driver?.SetCursorNeedsUpdate (true); }
}
