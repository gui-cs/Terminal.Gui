#nullable enable


namespace Terminal.Gui.Views;

/// <summary>
///     Event arguments for <see cref="HistoryText"/> item events. Used by <see cref="TextField"/> and <see cref="TextView"/>.
/// </summary>
/// <remarks>
///     This class encapsulates information about changes to text, such as the affected lines, cursor positions, and the type of change.
///     It is primarily used to support undo/redo functionality in text editing controls like <see cref="TextView"/> and <see cref="TextField"/>.
/// </remarks>
public class HistoryTextItemEventArgs : EventArgs
{
    // TODO: these should all be properties
    /// <summary>
    ///     Gets or sets the cursor position at the time of the change.
    /// </summary>
    public Point CursorPosition;

    /// <summary>
    ///     Gets or sets the final cursor position after the change is applied.
    /// </summary>
    public Point FinalCursorPosition;

    /// <summary>
    ///     Gets or sets a value indicating whether the change is part of an undo operation.
    /// </summary>
    public bool IsUndoing;

    /// <summary>
    ///     Gets or sets the lines of text affected by the change.
    /// </summary>
    /// <remarks>
    ///     Each line is represented as a list of <see cref="Cell"/> objects, which include the text and its attributes.
    /// </remarks>
    public List<List<Cell>> Lines;

    /// <summary>
    ///     Gets or sets the status of the line(s) affected by the change.
    /// </summary>
    /// <seealso cref="TextEditingLineStatus"/>
    public TextEditingLineStatus LineStatus;

    /// <summary>
    ///     Gets or sets the associated <see cref="HistoryTextItemEventArgs"/> for a removed line when an added line replaces it.
    /// </summary>
    /// <remarks>
    ///     This property is used to track the relationship between added and removed lines during undo/redo operations.
    /// </remarks>
    public HistoryTextItemEventArgs? RemovedOnAdded;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryTextItemEventArgs"/> class with the specified lines, cursor position, line status, and associated removed line.
    /// </summary>
    /// <param name="lines">The lines of text affected by the change.</param>
    /// <param name="curPos">The cursor position at the time of the change.</param>
    /// <param name="linesStatus">The status of the line(s) affected by the change.</param>
    /// <param name="removedOnAdded">The associated <see cref="HistoryTextItemEventArgs"/> for a removed line when an added line replaces it.</param>
    public HistoryTextItemEventArgs (List<List<Cell>> lines, Point curPos, TextEditingLineStatus linesStatus, HistoryTextItemEventArgs removedOnAdded)
    {
        Lines = lines;
        CursorPosition = curPos;
        LineStatus = linesStatus;
        RemovedOnAdded = removedOnAdded;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryTextItemEventArgs"/> class by copying an existing instance and associating it with a removed line.
    /// </summary>
    /// <param name="historyTextItem">The existing <see cref="HistoryTextItemEventArgs"/> to copy.</param>
    /// <param name="removedOnAdded">The associated <see cref="HistoryTextItemEventArgs"/> for a removed line when an added line replaces it.</param>
    public HistoryTextItemEventArgs (HistoryTextItemEventArgs historyTextItem, HistoryTextItemEventArgs removedOnAdded)
    {
        RemovedOnAdded = removedOnAdded;
        Lines = [.. historyTextItem.Lines];
        CursorPosition = new Point (historyTextItem.CursorPosition.X, historyTextItem.CursorPosition.Y);
        LineStatus = historyTextItem.LineStatus;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryTextItemEventArgs"/> class with the specified lines, cursor position, and line status.
    /// </summary>
    /// <param name="lines">The lines of text affected by the change.</param>
    /// <param name="curPos">The cursor position at the time of the change.</param>
    /// <param name="linesStatus">The status of the line(s) affected by the change.</param>
    public HistoryTextItemEventArgs (List<List<Cell>> lines, Point curPos, TextEditingLineStatus linesStatus)
    {
        Lines = lines;
        CursorPosition = curPos;
        LineStatus = linesStatus;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryTextItemEventArgs"/> class by copying an existing instance.
    /// </summary>
    /// <param name="historyTextItem">The existing <see cref="HistoryTextItemEventArgs"/> to copy.</param>
    public HistoryTextItemEventArgs (HistoryTextItemEventArgs historyTextItem)
    {
        Lines = [.. historyTextItem.Lines];
        CursorPosition = new Point (historyTextItem.CursorPosition.X, historyTextItem.CursorPosition.Y);
        LineStatus = historyTextItem.LineStatus;
    }

    /// <summary>
    ///     Returns a string representation of the <see cref="HistoryTextItemEventArgs"/> instance.
    /// </summary>
    /// <returns>A string containing the count of lines, cursor position, and line status.</returns>
    public override string ToString ()
    {
        return $"(Count: {Lines.Count}, Cursor: {CursorPosition}, Status: {LineStatus})";
    }
}
