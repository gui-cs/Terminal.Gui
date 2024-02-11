// TextView.cs: multi-line text editing

namespace Terminal.Gui;

internal partial class HistoryText
{
    public class HistoryTextItem : EventArgs
    {
        public Point CursorPosition;
        public Point FinalCursorPosition;
        public bool IsUndoing;
        public List<List<RuneCell>> Lines;
        public LineStatus LineStatus;
        public HistoryTextItem RemovedOnAdded;

        public HistoryTextItem (List<List<RuneCell>> lines, Point curPos, LineStatus linesStatus)
        {
            Lines = lines;
            CursorPosition = curPos;
            LineStatus = linesStatus;
        }

        public HistoryTextItem (HistoryTextItem historyTextItem)
        {
            Lines = new List<List<RuneCell>> (historyTextItem.Lines);
            CursorPosition = new Point (historyTextItem.CursorPosition.X, historyTextItem.CursorPosition.Y);
            LineStatus = historyTextItem.LineStatus;
        }

        public override string ToString () { return $"(Count: {Lines.Count}, Cursor: {CursorPosition}, Status: {LineStatus})"; }
    }
}
