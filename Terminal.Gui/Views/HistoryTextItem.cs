﻿// TextView.cs: multi-line text editing
using System;
using System.Collections.Generic;
using System.Text;

namespace Terminal.Gui {
	partial class HistoryText {
		public class HistoryTextItem : EventArgs {
			public List<List<RuneCell>> Lines;
			public Point CursorPosition;
			public LineStatus LineStatus;
			public bool IsUndoing;
			public Point FinalCursorPosition;
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

			public override string ToString ()
			{
				return $"(Count: {Lines.Count}, Cursor: {CursorPosition}, Status: {LineStatus})";
			}
		}
	}
}
