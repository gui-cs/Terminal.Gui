// TextView.cs: multi-line text editing

namespace Terminal.Gui;

internal partial class HistoryText
{
    public class HistoryTextItemEventArgs : EventArgs
    {
        public Point CursorPosition;
        public Point FinalCursorPosition;
        public bool IsUndoing;
        public List<List<Cell>> Lines;
        public LineStatus LineStatus;
        public HistoryTextItemEventArgs RemovedOnAdded;

        public HistoryTextItemEventArgs (List<List<Cell>> lines, Point curPos, LineStatus linesStatus)
        {
            Lines = lines;
            CursorPosition = curPos;
            LineStatus = linesStatus;
        }

        public HistoryTextItemEventArgs (HistoryTextItemEventArgs historyTextItem)
        {
            Lines = new List<List<Cell>> (historyTextItem.Lines);
            CursorPosition = new Point (historyTextItem.CursorPosition.X, historyTextItem.CursorPosition.Y);
            LineStatus = historyTextItem.LineStatus;
        }

        public override string ToString () { return $"(Count: {Lines.Count}, Cursor: {CursorPosition}, Status: {LineStatus})"; }
    }
}
