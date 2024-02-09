// TextView.cs: multi-line text editing

namespace Terminal.Gui;

internal partial class HistoryText {
    public class HistoryTextItem : EventArgs {
        public HistoryTextItem (List<List<RuneCell>> lines, Point curPos, LineStatus linesStatus) {
            Lines = lines;
            CursorPosition = curPos;
            LineStatus = linesStatus;
        }

        public HistoryTextItem (HistoryTextItem historyTextItem) {
            Lines = new List<List<RuneCell>> (historyTextItem.Lines);
            CursorPosition = new Point (historyTextItem.CursorPosition.X, historyTextItem.CursorPosition.Y);
            LineStatus = historyTextItem.LineStatus;
        }

        public bool IsUndoing;
        public HistoryTextItem RemovedOnAdded;
        public LineStatus LineStatus;
        public List<List<RuneCell>> Lines;
        public Point CursorPosition;
        public Point FinalCursorPosition;
        public override string ToString () => $"(Count: {Lines.Count}, Cursor: {CursorPosition}, Status: {LineStatus})";
    }
}
