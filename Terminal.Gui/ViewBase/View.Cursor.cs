
namespace Terminal.Gui.ViewBase;

public partial class View
{
    /// <summary>
    ///     Gets or sets the cursor style to be used when the view is focused. The default is
    ///     <see cref="Drivers.CursorVisibility.Invisible"/>.
    /// </summary>
    public CursorVisibility CursorVisibility { get; set; } = CursorVisibility.Invisible;

    /// <summary>
    ///     Signals that the cursor position needs to be updated without requiring a full redraw.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Call this method when the cursor position changes but the view content does not need to be redrawn.
    ///         This is more efficient than calling <see cref="View.SetNeedsDraw"/> when only the cursor needs to move.
    ///     </para>
    ///     <para>
    ///         Common use cases include:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>User typing in a text field - cursor moves right after each character</item>
    ///         <item>User presses arrow keys in a text view - cursor moves without content changing</item>
    ///         <item>Programmatic cursor position changes</item>
    ///     </list>
    ///     <para>
    ///         NOTE: Current implementation updates cursor every iteration, so this is a no-op.
    ///         Future optimization may implement cursor position caching.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     public class MyTextField : View
    ///     {
    ///         private int _cursorPosition;
    ///         
    ///         public int CursorPosition
    ///         {
    ///             get => _cursorPosition;
    ///             set
    ///             {
    ///                 if (_cursorPosition != value)
    ///                 {
    ///                     _cursorPosition = value;
    ///                     SetCursorNeedsUpdate();  // Update cursor without full redraw
    ///                 }
    ///             }
    ///         }
    ///     }
    ///     </code>
    /// </example>
    public void SetCursorNeedsUpdate ()
    {
        // Currently a no-op since cursor is updated every iteration
        // Future optimization may implement cursor position caching
    }

    /// <summary>
    ///     Calculates where the cursor should be positioned for this view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is ONLY called on the most focused view in the application (the deepest view
    ///         in the focus chain with <see cref="HasFocus"/> == <see langword="true"/>).
    ///     </para>
    ///     <para>
    ///         <b>IMPORTANT:</b> Do NOT call <see cref="View.Move"/>, <see cref="View.AddRune"/>, or any drawing
    ///         methods in this method. Those methods affect the "Draw Cursor" (where characters are rendered), not
    ///         the Terminal Cursor (the visible cursor indicator). This method should only calculate and return a position.
    ///     </para>
    ///     <para>
    ///         Return viewport-relative coordinates. The framework will convert to screen coordinates.
    ///         Return <see langword="null"/> to hide the cursor.
    ///     </para>
    ///     <para>
    ///         The base implementation returns <see langword="null"/> (cursor hidden). Override to position the cursor
    ///         at the appropriate location for your view.
    ///     </para>
    ///     <para>
    ///         Views that want the cursor visible should also set <see cref="CursorVisibility"/> to a value
    ///         other than <see cref="Drivers.CursorVisibility.Invisible"/>.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     Viewport-relative cursor position (col, row), or <see langword="null"/> to hide the cursor.
    /// </returns>
    /// <example>
    ///     <code>
    ///     public override Point? PositionCursor()
    ///     {
    ///         // Don't call base - it just returns null
    ///         
    ///         if (!CanFocus || !HasFocus)
    ///             return null;  // Shouldn't happen, but be defensive
    ///         
    ///         // Calculate cursor position based on your view's state
    ///         int visualColumn = CalculateVisualColumn();
    ///         int visualRow = CalculateVisualRow();
    ///         
    ///         return new Point(visualColumn, visualRow);
    ///     }
    ///     </code>
    /// </example>
    public virtual Point? PositionCursor ()
    {
        // Base implementation: hide cursor
        // Returning null will hide the cursor.
        return null;
    }
}
