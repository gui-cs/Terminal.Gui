//
// TextView.cs: multi-line text editing
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// 
// TODO:
// In ReadOnly mode backspace/space behave like pageup/pagedown
// Attributed text on spans
// Replace insertion with Insert method
// String accumulation (Control-k, control-k is not preserving the last new line, see StringToRunes
// Alt-D, Alt-Backspace
// API to set the cursor position
// API to scroll to a particular place
// keybindings to go to top/bottom
// public API to insert, remove ranges
// Add word forward/word backwards commands
// Save buffer API
// Mouse
//
// Desirable:
//   Move all the text manipulation into the TextModel


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NStack;

using Rune = System.Rune;

namespace Terminal.Gui {
	class TextModel {
		List<List<Rune>> lines = new List<List<Rune>> ();

		public bool LoadFile (string file)
		{
			if (file == null)
				throw new ArgumentNullException (nameof (file));
			try {
				FilePath = file;
				var stream = File.OpenRead (file);
			} catch {
				return false;
			}
			LoadStream (File.OpenRead (file));
			return true;
		}

		public bool CloseFile ()
		{
			if (FilePath == null)
				throw new ArgumentNullException (nameof (FilePath));
			try {
				FilePath = null;
				lines = new List<List<Rune>> ();
			} catch {
				return false;
			}
			return true;
		}

		// Turns the ustring into runes, this does not split the 
		// contents on a newline if it is present.
		internal static List<Rune> ToRunes (ustring str)
		{
			List<Rune> runes = new List<Rune> ();
			foreach (var x in str.ToRunes ()) {
				runes.Add (x);
			}
			return runes;
		}

		// Splits a string into a List that contains a List<Rune> for each line
		public static List<List<Rune>> StringToRunes (ustring content)
		{
			var lines = new List<List<Rune>> ();
			int start = 0, i = 0;
			// ASCII code 10 = Line Feed.
			for (; i < content.Length; i++) {
				if (content [i] == 10) {
					if (i - start > 0)
						lines.Add (ToRunes (content [start, i]));
					else
						lines.Add (ToRunes (ustring.Empty));
					start = i + 1;
				}
			}
			if (i - start >= 0)
				lines.Add (ToRunes (content [start, null]));
			return lines;
		}

		void Append (List<byte> line)
		{
			var str = ustring.Make (line.ToArray ());
			lines.Add (ToRunes (str));
		}

		public void LoadStream (Stream input)
		{
			if (input == null)
				throw new ArgumentNullException (nameof (input));

			lines = new List<List<Rune>> ();
			var buff = new BufferedStream (input);
			int v;
			var line = new List<byte> ();
			while ((v = buff.ReadByte ()) != -1) {
				if (v == 10) {
					Append (line);
					line.Clear ();
					continue;
				}
				line.Add ((byte)v);
			}
			if (line.Count > 0)
				Append (line);
		}

		public void LoadString (ustring content)
		{
			lines = StringToRunes (content);
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < lines.Count; i++) {
				sb.Append (ustring.Make (lines [i]));
				if ((i + 1) < lines.Count) {
					sb.AppendLine ();
				}
			}
			return sb.ToString ();
		}

		public string FilePath { get; set; }

		/// <summary>
		/// The number of text lines in the model
		/// </summary>
		public int Count => lines.Count;

		/// <summary>
		/// Returns the specified line as a List of Rune
		/// </summary>
		/// <returns>The line.</returns>
		/// <param name="line">Line number to retrieve.</param>
		public List<Rune> GetLine (int line)
		{
			if (lines.Count > 0) {
				if (line < Count) {
					return lines [line];
				} else {
					return lines [Count - 1];
				}
			} else {
				lines.Add (new List<Rune> ());
				return lines [0];
			}
		}

		/// <summary>
		/// Adds a line to the model at the specified position.
		/// </summary>
		/// <param name="pos">Line number where the line will be inserted.</param>
		/// <param name="runes">The line of text, as a List of Rune.</param>
		public void AddLine (int pos, List<Rune> runes)
		{
			lines.Insert (pos, runes);
		}

		/// <summary>
		/// Removes the line at the specified position
		/// </summary>
		/// <param name="pos">Position.</param>
		public void RemoveLine (int pos)
		{
			if (lines.Count > 0) {
				lines.RemoveAt (pos);
			}
		}

		/// <summary>
		/// Returns the maximum line length of the visible lines.
		/// </summary>
		/// <param name="first">The first line.</param>
		/// <param name="last">The last line.</param>
		public int GetMaxVisibleLine (int first, int last)
		{
			int maxLength = 0;
			last = last < lines.Count ? last : lines.Count;
			for (int i = first; i < last; i++) {
				var l = GetLine (i).Count;
				if (l > maxLength) {
					maxLength = l;
				}
			}

			return maxLength;
		}

		internal static int SetCol (int col, int width, int cols)
		{
			if (col + cols <= width) {
				col += cols;
			}

			return col;
		}

		internal static int GetColFromX (List<Rune> t, int start, int x)
		{
			if (x < 0) {
				return x;
			}
			int size = start;
			var pX = x + start;
			for (int i = start; i < t.Count; i++) {
				var r = t [i];
				size += Rune.ColumnWidth (r);
				if (i == pX || (size > pX)) {
					return i - start;
				}
			}
			return t.Count - start;
		}

		// Returns the size and length in a range of the string.
		internal static (int size, int length) DisplaySize (List<Rune> t, int start = -1, int end = -1, bool checkNextRune = true)
		{
			if (t == null || t.Count == 0) {
				return (0, 0);
			}
			int size = 0;
			int len = 0;
			int tcount = end == -1 ? t.Count : end > t.Count ? t.Count : end;
			int i = start == -1 ? 0 : start;
			for (; i < tcount; i++) {
				var rune = t [i];
				size += Rune.ColumnWidth (rune);
				len += Rune.RuneLen (rune);
				if (checkNextRune && i == tcount - 1 && t.Count > tcount && Rune.ColumnWidth (t [i + 1]) > 1) {
					size += Rune.ColumnWidth (t [i + 1]);
					len += Rune.RuneLen (t [i + 1]);
				}
			}
			return (size, len);
		}

		// Returns the left column in a range of the string.
		internal static int CalculateLeftColumn (List<Rune> t, int start, int end, int width, int currentColumn)
		{
			if (t == null) {
				return 0;
			}
			(var dSize, _) = TextModel.DisplaySize (t, start, end);
			if (dSize < width) {
				return start;
			}
			int size = 0;
			int tcount = end > t.Count - 1 ? t.Count - 1 : end;
			int col = 0;
			for (int i = tcount; i > start; i--) {
				var rune = t [i];
				var s = Rune.ColumnWidth (rune);
				size += s;
				if (size >= dSize - width) {
					col = tcount - i + start;
					if (start == 0 || col == start || (currentColumn == t.Count && (currentColumn - col > width))) {
						col++;
					}
					break;
				}
			}
			return col;
		}
	}

	class WordWrapManager {
		class WrappedLine {
			public int ModelLine;
			public int Row;
			public int RowIndex;
			public int ColWidth;
		}

		List<WrappedLine> wrappedModelLines = new List<WrappedLine> ();
		int frameWidth;
		bool isWrapModelRefreshing;

		public TextModel Model { get; private set; }

		public WordWrapManager (TextModel model)
		{
			Model = model;
		}

		public TextModel WrapModel (int width, out int nRow, out int nCol, out int nStartRow, out int nStartCol,
			int row = 0, int col = 0, int startRow = 0, int startCol = 0)
		{
			frameWidth = width;

			var modelRow = isWrapModelRefreshing ? row : GetModelLineFromWrappedLines (row);
			var modelCol = isWrapModelRefreshing ? col : GetModelColFromWrappedLines (row, col);
			var modelStartRow = isWrapModelRefreshing ? startRow : GetModelLineFromWrappedLines (startRow);
			var modelStartCol = isWrapModelRefreshing ? startCol : GetModelColFromWrappedLines (startRow, startCol);
			var wrappedModel = new TextModel ();
			int lines = 0;
			nRow = 0;
			nCol = 0;
			nStartRow = 0;
			nStartCol = 0;
			bool isRowAndColSetted = row == 0 && col == 0;
			bool isStartRowAndColSetted = startRow == 0 && startCol == 0;
			List<WrappedLine> wModelLines = new List<WrappedLine> ();

			for (int i = 0; i < Model.Count; i++) {
				var line = Model.GetLine (i);
				var wrappedLines = ToListRune (
					TextFormatter.Format (ustring.Make (line), width, TextAlignment.Left, true, true));
				int sumColWidth = 0;
				for (int j = 0; j < wrappedLines.Count; j++) {
					var wrapLine = wrappedLines [j];
					if (!isRowAndColSetted && modelRow == i) {
						if (nCol + wrapLine.Count <= modelCol) {
							nCol += wrapLine.Count;
							nRow = lines;
							if (nCol == modelCol) {
								nCol = wrapLine.Count;
								isRowAndColSetted = true;
							} else if (j == wrappedLines.Count - 1) {
								nCol = wrapLine.Count - j + modelCol - nCol;
								isRowAndColSetted = true;
							}
						} else {
							var offset = nCol + wrapLine.Count - modelCol;
							nCol = wrapLine.Count - offset;
							nRow = lines;
							isRowAndColSetted = true;
						}
					}
					if (!isStartRowAndColSetted && modelStartRow == i) {
						if (nStartCol + wrapLine.Count <= modelStartCol) {
							nStartCol += wrapLine.Count;
							nStartRow = lines;
							if (nStartCol == modelStartCol) {
								nStartCol = wrapLine.Count;
								isStartRowAndColSetted = true;
							} else if (j == wrappedLines.Count - 1) {
								nStartCol = wrapLine.Count - j + modelStartCol - nStartCol;
								isStartRowAndColSetted = true;
							}
						} else {
							var offset = nStartCol + wrapLine.Count - modelStartCol;
							nStartCol = wrapLine.Count - offset;
							nStartRow = lines;
							isStartRowAndColSetted = true;
						}
					}
					wrappedModel.AddLine (lines, wrapLine);
					sumColWidth += wrapLine.Count;
					var wrappedLine = new WrappedLine () {
						ModelLine = i,
						Row = lines,
						RowIndex = j,
						ColWidth = wrapLine.Count,
					};
					wModelLines.Add (wrappedLine);
					lines++;
				}
			}
			wrappedModelLines = wModelLines;

			return wrappedModel;
		}

		public List<List<Rune>> ToListRune (List<ustring> textList)
		{
			var runesList = new List<List<Rune>> ();

			foreach (var text in textList) {
				runesList.Add (text.ToRuneList ());
			}

			return runesList;
		}

		public int GetModelLineFromWrappedLines (int line) => wrappedModelLines.Count > 0
			? wrappedModelLines [Math.Min (line, wrappedModelLines.Count - 1)].ModelLine
			: 0;

		public int GetModelColFromWrappedLines (int line, int col)
		{
			if (wrappedModelLines?.Count == 0) {
				return 0;
			}

			var modelLine = GetModelLineFromWrappedLines (line);
			var firstLine = wrappedModelLines.IndexOf (r => r.ModelLine == modelLine);
			int modelCol = 0;

			for (int i = firstLine; i <= line; i++) {
				var wLine = wrappedModelLines [i];

				if (i < line) {
					modelCol += wLine.ColWidth;
				} else {
					modelCol += col;
				}
			}

			return modelCol;
		}

		List<Rune> GetCurrentLine (int row) => Model.GetLine (row);

		public void AddLine (int row, int col)
		{
			var modelRow = GetModelLineFromWrappedLines (row);
			var modelCol = GetModelColFromWrappedLines (row, col);
			var line = GetCurrentLine (modelRow);
			var restCount = line.Count - modelCol;
			var rest = line.GetRange (modelCol, restCount);
			line.RemoveRange (modelCol, restCount);
			Model.AddLine (modelRow + 1, rest);
			isWrapModelRefreshing = true;
			WrapModel (frameWidth, out _, out _, out _, out _, modelRow + 1, 0);
			isWrapModelRefreshing = false;
		}

		public bool Insert (int row, int col, Rune rune)
		{
			var line = GetCurrentLine (GetModelLineFromWrappedLines (row));
			line.Insert (GetModelColFromWrappedLines (row, col), rune);
			if (line.Count > frameWidth) {
				return true;
			} else {
				return false;
			}
		}

		public bool RemoveAt (int row, int col)
		{
			var modelRow = GetModelLineFromWrappedLines (row);
			var line = GetCurrentLine (modelRow);
			var modelCol = GetModelColFromWrappedLines (row, col);

			if (modelCol >= line.Count) {
				Model.RemoveLine (modelRow);
				RemoveAt (row, 0);
				return false;
			}
			line.RemoveAt (modelCol);
			if (line.Count > frameWidth || (row + 1 < wrappedModelLines.Count
				&& wrappedModelLines [row + 1].ModelLine == modelRow)) {
				return true;
			}

			return false;
		}

		public bool RemoveLine (int row, int col, out bool lineRemoved, bool forward = true)
		{
			lineRemoved = false;
			var modelRow = GetModelLineFromWrappedLines (row);
			var line = GetCurrentLine (modelRow);
			var modelCol = GetModelColFromWrappedLines (row, col);

			if (modelCol == 0 && line.Count == 0) {
				Model.RemoveLine (modelRow);
				return false;
			} else if (modelCol < line.Count) {
				if (forward) {
					line.RemoveAt (modelCol);
					return true;
				} else if (modelCol - 1 > -1) {
					line.RemoveAt (modelCol - 1);
					return true;
				}
			}
			lineRemoved = true;
			if (forward) {
				if (modelRow + 1 == Model.Count) {
					return false;
				}

				var nextLine = Model.GetLine (modelRow + 1);
				line.AddRange (nextLine);
				Model.RemoveLine (modelRow + 1);
				if (line.Count > frameWidth) {
					return true;
				}
			} else {
				if (modelRow == 0) {
					return false;
				}

				var prevLine = Model.GetLine (modelRow - 1);
				prevLine.AddRange (line);
				Model.RemoveLine (modelRow);
				if (prevLine.Count > frameWidth) {
					return true;
				}
			}

			return false;
		}

		public void UpdateModel (TextModel model, out int nRow, out int nCol, out int nStartRow, out int nStartCol,
			int row, int col, int startRow, int startCol)
		{
			isWrapModelRefreshing = true;
			Model = model;
			WrapModel (frameWidth, out nRow, out nCol, out nStartRow, out nStartCol, row, col, startRow, startCol);
			isWrapModelRefreshing = false;
		}
	}

	/// <summary>
	///   Multi-line text editing <see cref="View"/>
	/// </summary>
	/// <remarks>
	///   <para>
	///     <see cref="TextView"/> provides a multi-line text editor. Users interact
	///     with it with the standard Emacs commands for movement or the arrow
	///     keys. 
	///   </para> 
	///   <list type="table"> 
	///     <listheader>
	///       <term>Shortcut</term>
	///       <description>Action performed</description>
	///     </listheader>
	///     <item>
	///        <term>Left cursor, Control-b</term>
	///        <description>
	///          Moves the editing point left.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Right cursor, Control-f</term>
	///        <description>
	///          Moves the editing point right.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Alt-b</term>
	///        <description>
	///          Moves one word back.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Alt-f</term>
	///        <description>
	///          Moves one word forward.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Up cursor, Control-p</term>
	///        <description>
	///          Moves the editing point one line up.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Down cursor, Control-n</term>
	///        <description>
	///          Moves the editing point one line down
	///        </description>
	///     </item>
	///     <item>
	///        <term>Home key, Control-a</term>
	///        <description>
	///          Moves the cursor to the beginning of the line.
	///        </description>
	///     </item>
	///     <item>
	///        <term>End key, Control-e</term>
	///        <description>
	///          Moves the cursor to the end of the line.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Control-Home</term>
	///        <description>
	///          Scrolls to the first line and moves the cursor there.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Control-End</term>
	///        <description>
	///          Scrolls to the last line and moves the cursor there.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Delete, Control-d</term>
	///        <description>
	///          Deletes the character in front of the cursor.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Backspace</term>
	///        <description>
	///          Deletes the character behind the cursor.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Control-k</term>
	///        <description>
	///          Deletes the text until the end of the line and replaces the kill buffer
	///          with the deleted text.   You can paste this text in a different place by
	///          using Control-y.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Control-y</term>
	///        <description>
	///           Pastes the content of the kill ring into the current position.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Alt-d</term>
	///        <description>
	///           Deletes the word above the cursor and adds it to the kill ring.  You 
	///           can paste the contents of the kill ring with Control-y.
	///        </description>
	///     </item>
	///     <item>
	///        <term>Control-q</term>
	///        <description>
	///          Quotes the next input character, to prevent the normal processing of
	///          key handling to take place.
	///        </description>
	///     </item>
	///   </list>
	/// </remarks>
	public class TextView : View {
		TextModel model = new TextModel ();
		int topRow;
		int leftColumn;
		int currentRow;
		int currentColumn;
		int selectionStartColumn, selectionStartRow;
		bool selecting;
		//bool used;
		bool wordWrap;
		WordWrapManager wrapManager;

		/// <summary>
		/// Raised when the <see cref="Text"/> of the <see cref="TextView"/> changes.
		/// </summary>
		public event Action TextChanged;

#if false
		/// <summary>
		///   Changed event, raised when the text has clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the text in the entry changes.
		/// </remarks>
		public Action Changed;
#endif
		/// <summary>
		///   Initializes a <see cref="TextView"/> on the specified area, with absolute position and size.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public TextView (Rect frame) : base (frame)
		{
			CanFocus = true;
		}

		/// <summary>
		///   Initializes a <see cref="TextView"/> on the specified area, 
		///   with dimensions controlled with the X, Y, Width and Height properties.
		/// </summary>
		public TextView () : base ()
		{
			CanFocus = true;
		}

		void ResetPosition ()
		{
			topRow = leftColumn = currentRow = currentColumn = 0;
			selecting = false;
			shiftSelecting = false;
			ResetCursorVisibility ();
		}

		/// <summary>
		///   Sets or gets the text in the <see cref="TextView"/>.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override ustring Text {
			get {
				if (wordWrap) {
					return wrapManager.Model.ToString ();
				} else {
					return model.ToString ();
				}
			}

			set {
				ResetPosition ();
				model.LoadString (value);
				if (wordWrap) {
					wrapManager = new WordWrapManager (model);
					model = wrapManager.WrapModel (Frame.Width - 2, out _, out _, out _, out _);
				}
				TextChanged?.Invoke ();
				SetNeedsDisplay ();
			}
		}

		///<inheritdoc/>
		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;
				WrapTextModel ();
				Adjust ();
			}
		}

		void WrapTextModel ()
		{
			if (wordWrap && wrapManager != null) {
				model = wrapManager.WrapModel (Frame.Width - 2,
					out int nRow, out int nCol,
					out int nStartRow, out int nStartCol,
					currentRow, currentColumn,
					selectionStartRow, selectionStartColumn);
				currentRow = nRow;
				currentColumn = nCol;
				selectionStartRow = nStartRow;
				selectionStartColumn = nStartCol;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the top row.
		/// </summary>
		public int TopRow { get => topRow; set => topRow = Math.Max (Math.Min (value, Lines - 1), 0); }

		/// <summary>
		/// Gets or sets the left column.
		/// </summary>
		public int LeftColumn { get => leftColumn; set => leftColumn = Math.Max (Math.Min (value, Maxlength - 1), 0); }

		/// <summary>
		/// Gets the maximum visible length line.
		/// </summary>
		public int Maxlength => model.GetMaxVisibleLine (topRow, topRow + Frame.Height);

		/// <summary>
		/// Gets the  number of lines.
		/// </summary>
		public int Lines => model.Count;

		/// <summary>
		/// Allows word wrap the to fit the available container width.
		/// </summary>
		public bool WordWrap {
			get => wordWrap;
			set {
				if (value == wordWrap) {
					return;
				}
				wordWrap = value;
				ResetPosition ();
				if (wordWrap) {
					wrapManager = new WordWrapManager (model);
					model = wrapManager.WrapModel (Math.Max (Frame.Width - 2, 0), out _, out _, out _, out _);
				} else if (!wordWrap && wrapManager != null) {
					model = wrapManager.Model;
				}
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// The bottom offset needed to use a horizontal scrollbar or for another reason.
		/// This is only needed with the keyboard navigation.
		/// </summary>
		public int BottomOffset { get; set; }

		/// <summary>
		/// The right offset needed to use a vertical scrollbar or for another reason.
		/// This is only needed with the keyboard navigation.
		/// </summary>
		public int RightOffset { get; set; }

		CursorVisibility savedCursorVisibility = CursorVisibility.Default;

		void SaveCursorVisibility ()
		{
			if (desiredCursorVisibility != CursorVisibility.Invisible) {
				savedCursorVisibility = desiredCursorVisibility;
				DesiredCursorVisibility = CursorVisibility.Invisible;
			}
		}

		void ResetCursorVisibility ()
		{
			if (savedCursorVisibility != desiredCursorVisibility) {
				DesiredCursorVisibility = savedCursorVisibility;
				savedCursorVisibility = CursorVisibility.Default;
			} else {
				DesiredCursorVisibility = CursorVisibility.Underline;
			}
		}

		/// <summary>
		/// Loads the contents of the file into the  <see cref="TextView"/>.
		/// </summary>
		/// <returns><c>true</c>, if file was loaded, <c>false</c> otherwise.</returns>
		/// <param name="path">Path to the file to load.</param>
		public bool LoadFile (string path)
		{
			if (path == null)
				throw new ArgumentNullException (nameof (path));
			ResetPosition ();
			var res = model.LoadFile (path);
			SetNeedsDisplay ();
			return res;
		}

		/// <summary>
		/// Loads the contents of the stream into the  <see cref="TextView"/>.
		/// </summary>
		/// <returns><c>true</c>, if stream was loaded, <c>false</c> otherwise.</returns>
		/// <param name="stream">Stream to load the contents from.</param>
		public void LoadStream (Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException (nameof (stream));
			ResetPosition ();
			model.LoadStream (stream);
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Closes the contents of the stream into the  <see cref="TextView"/>.
		/// </summary>
		/// <returns><c>true</c>, if stream was closed, <c>false</c> otherwise.</returns>
		public bool CloseFile ()
		{
			ResetPosition ();
			var res = model.CloseFile ();
			SetNeedsDisplay ();
			return res;
		}

		/// <summary>
		///    Gets the current cursor row.
		/// </summary>
		public int CurrentRow => currentRow;

		/// <summary>
		/// Gets the cursor column.
		/// </summary>
		/// <value>The cursor column.</value>
		public int CurrentColumn => currentColumn;

		/// <summary>
		///   Positions the cursor on the current row and column
		/// </summary>
		public override void PositionCursor ()
		{
			if (selecting) {
				var minRow = Math.Min (Math.Max (Math.Min (selectionStartRow, currentRow) - topRow, 0), Frame.Height);
				var maxRow = Math.Min (Math.Max (Math.Max (selectionStartRow, currentRow) - topRow, 0), Frame.Height);

				SetNeedsDisplay (new Rect (0, minRow, Frame.Width, maxRow));
			}
			var line = model.GetLine (currentRow);
			var retreat = 0;
			var col = 0;
			if (line.Count > 0) {
				retreat = Math.Max (SpecialRune (line [Math.Min (Math.Max (currentColumn - leftColumn - 1, 0), line.Count - 1)])
				? 1 : 0, 0);
				for (int idx = leftColumn < 0 ? 0 : leftColumn; idx < line.Count; idx++) {
					if (idx == currentColumn)
						break;
					var cols = Rune.ColumnWidth (line [idx]);
					col += cols - 1;
				}
			}
			var ccol = currentColumn - leftColumn - retreat + col;
			if (leftColumn <= currentColumn && ccol < Frame.Width
				&& topRow <= currentRow && currentRow - topRow < Frame.Height) {
				ResetCursorVisibility ();
				Move (ccol, currentRow - topRow);
			} else {
				SaveCursorVisibility ();
			}
		}

		void ClearRegion (int left, int top, int right, int bottom)
		{
			for (int row = top; row < bottom; row++) {
				Move (left, row);
				for (int col = left; col < right; col++)
					AddRune (col, row, ' ');
			}
		}

		void ColorNormal ()
		{
			Driver.SetAttribute (ColorScheme.Normal);
		}

		void ColorSelection ()
		{
			Driver.SetAttribute (ColorScheme.Focus);
		}

		bool isReadOnly = false;

		/// <summary>
		/// Gets or sets whether the  <see cref="TextView"/> is in read-only mode or not
		/// </summary>
		/// <value>Boolean value(Default false)</value>
		public bool ReadOnly {
			get => isReadOnly;
			set {
				isReadOnly = value;
			}
		}

		CursorVisibility desiredCursorVisibility = CursorVisibility.Default;

		/// <summary>
		/// Get / Set the wished cursor when the field is focused
		/// </summary>
		public CursorVisibility DesiredCursorVisibility {
			get => desiredCursorVisibility;
			set {
				if (desiredCursorVisibility != value && HasFocus) {
					Application.Driver.SetCursorVisibility (value);
				}

				desiredCursorVisibility = value;
			}
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			//TODO: Improve it by handling read only mode of the text field
			Application.Driver.SetCursorVisibility (DesiredCursorVisibility);

			return base.OnEnter (view);
		}

		// Returns an encoded region start..end (top 32 bits are the row, low32 the column)
		void GetEncodedRegionBounds (out long start, out long end)
		{
			long selection = ((long)(uint)selectionStartRow << 32) | (uint)selectionStartColumn;
			long point = ((long)(uint)currentRow << 32) | (uint)currentColumn;
			if (selection > point) {
				start = point;
				end = selection;
			} else {
				start = selection;
				end = point;
			}
		}

		bool PointInSelection (int col, int row)
		{
			long start, end;
			GetEncodedRegionBounds (out start, out end);
			var q = ((long)(uint)row << 32) | (uint)col;
			return q >= start && q <= end - 1;
		}

		//
		// Returns a ustring with the text in the selected 
		// region.
		//
		ustring GetRegion ()
		{
			long start, end;
			GetEncodedRegionBounds (out start, out end);
			if (start == end) {
				return ustring.Empty;
			}
			int startRow = (int)(start >> 32);
			var maxrow = ((int)(end >> 32));
			int startCol = (int)(start & 0xffffffff);
			var endCol = (int)(end & 0xffffffff);
			var line = model.GetLine (startRow);

			if (startRow == maxrow)
				return StringFromRunes (line.GetRange (startCol, endCol - startCol));

			ustring res = StringFromRunes (line.GetRange (startCol, line.Count - startCol));

			for (int row = startRow + 1; row < maxrow; row++) {
				res = res + ustring.Make ((Rune)10) + StringFromRunes (model.GetLine (row));
			}
			line = model.GetLine (maxrow);
			res = res + ustring.Make ((Rune)10) + StringFromRunes (line.GetRange (0, endCol));
			return res;
		}

		//
		// Clears the contents of the selected region
		//
		void ClearRegion ()
		{
			long start, end;
			long currentEncoded = ((long)(uint)currentRow << 32) | (uint)currentColumn;
			GetEncodedRegionBounds (out start, out end);
			int startRow = (int)(start >> 32);
			var maxrow = ((int)(end >> 32));
			int startCol = (int)(start & 0xffffffff);
			var endCol = (int)(end & 0xffffffff);
			var line = model.GetLine (startRow);

			if (startRow == maxrow) {
				line.RemoveRange (startCol, endCol - startCol);
				currentColumn = startCol;
				if (wordWrap) {
					SetNeedsDisplay ();
				} else {
					SetNeedsDisplay (new Rect (0, startRow - topRow, Frame.Width, startRow - topRow + 1));
				}
				return;
			}

			line.RemoveRange (startCol, line.Count - startCol);
			var line2 = model.GetLine (maxrow);
			line.AddRange (line2.Skip (endCol));
			for (int row = startRow + 1; row <= maxrow; row++) {
				model.RemoveLine (startRow + 1);
			}
			if (currentEncoded == end) {
				currentRow -= maxrow - (startRow);
			}
			currentColumn = startCol;

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Restore from original model.
		/// </summary>
		void SetWrapModel ()
		{
			if (wordWrap) {
				currentColumn = wrapManager.GetModelColFromWrappedLines (currentRow, currentColumn);
				currentRow = wrapManager.GetModelLineFromWrappedLines (currentRow);
				selectionStartColumn = wrapManager.GetModelColFromWrappedLines (selectionStartRow, selectionStartColumn);
				selectionStartRow = wrapManager.GetModelLineFromWrappedLines (selectionStartRow);
				model = wrapManager.Model;
			}
		}

		/// <summary>
		/// Update the original model.
		/// </summary>
		void UpdateWrapModel ()
		{
			if (wordWrap) {
				wrapManager.UpdateModel (model, out int nRow, out int nCol,
					out int nStartRow, out int nStartCol,
					currentRow, currentColumn,
					selectionStartRow, selectionStartColumn);
				currentRow = nRow;
				currentColumn = nCol;
				selectionStartRow = nStartRow;
				selectionStartColumn = nStartCol;
				wrapNeeded = true;
			}
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			ColorNormal ();

			int bottom = bounds.Bottom;
			int right = bounds.Right + 1;
			for (int row = bounds.Top; row < bottom; row++) {
				int textLine = topRow + row;
				if (textLine >= model.Count) {
					ColorNormal ();
					ClearRegion (bounds.Left, row, bounds.Right, row + 1);
					continue;
				}
				var line = model.GetLine (textLine);
				int lineRuneCount = line.Count;
				if (line.Count < bounds.Left) {
					ClearRegion (bounds.Left, row, bounds.Right, row + 1);
					continue;
				}

				Move (bounds.Left, row);
				var col = 0;
				for (int idx = bounds.Left; idx < right; idx++) {
					var lineCol = leftColumn + idx;
					var rune = lineCol >= lineRuneCount ? ' ' : line [lineCol];
					var cols = Rune.ColumnWidth (rune);
					if (lineCol < line.Count && selecting && PointInSelection (idx + leftColumn, row + topRow)) {
						ColorSelection ();
					} else {
						ColorNormal ();
					}

					if (!SpecialRune (rune)) {
						AddRune (col, row, rune);
					}
					col = TextModel.SetCol (col, bounds.Right, cols);
				}
			}
			PositionCursor ();
		}

		bool SpecialRune (Rune rune)
		{
			switch (rune) {
			case (uint)Key.Enter:
			case 0xd:
				return true;
			default:
				return false;
			}
		}

		///<inheritdoc/>
		public override bool CanFocus {
			get => base.CanFocus;
			set { base.CanFocus = value; }
		}

		void SetClipboard (ustring text)
		{
			if (!text.IsEmpty) {
				Clipboard.Contents = text;
			}
		}

		void AppendClipboard (ustring text)
		{
			Clipboard.Contents += text;
		}

		void Insert (Rune rune)
		{
			var line = GetCurrentLine ();
			line.Insert (Math.Min (currentColumn, line.Count), rune);
			if (wordWrap) {
				wrapNeeded = wrapManager.Insert (currentRow, currentColumn, rune);
				if (wrapNeeded) {
					SetNeedsDisplay ();
				}
			}
			var prow = currentRow - topRow;
			if (!wrapNeeded) {
				SetNeedsDisplay (new Rect (0, prow, Frame.Width, prow + 1));
			}
		}

		ustring StringFromRunes (List<Rune> runes)
		{
			if (runes == null)
				throw new ArgumentNullException (nameof (runes));
			int size = 0;
			foreach (var rune in runes) {
				size += Utf8.RuneLen (rune);
			}
			var encoded = new byte [size];
			int offset = 0;
			foreach (var rune in runes) {
				offset += Utf8.EncodeRune (rune, encoded, offset);
			}
			return ustring.Make (encoded);
		}

		List<Rune> GetCurrentLine () => model.GetLine (currentRow);

		void InsertText (ustring text)
		{
			if (ustring.IsNullOrEmpty (text)) {
				return;
			}

			var lines = TextModel.StringToRunes (text);

			if (lines.Count == 0) {
				return;
			}

			var line = GetCurrentLine ();

			// Optimize single line
			if (lines.Count == 1) {
				line.InsertRange (currentColumn, lines [0]);
				currentColumn += lines [0].Count;
				if (!wordWrap && currentColumn - leftColumn > Frame.Width) {
					leftColumn = Math.Max (currentColumn - Frame.Width + 1, 0);
				}
				if (wordWrap) {
					SetNeedsDisplay ();
				} else {
					SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, currentRow - topRow + 1));
				}
				return;
			}

			List<Rune> rest = null;
			int lastp = 0;

			if (model.Count > 0 && currentColumn > 0) {
				// Keep a copy of the rest of the line
				var restCount = line.Count - currentColumn;
				rest = line.GetRange (currentColumn, restCount);
				line.RemoveRange (currentColumn, restCount);
			}

			// First line is inserted at the current location, the rest is appended
			line.InsertRange (currentColumn, lines [0]);
			//model.AddLine (currentRow, lines [0]);

			for (int i = 1; i < lines.Count; i++) {
				model.AddLine (currentRow + i, lines [i]);
			}

			if (rest != null) {
				var last = model.GetLine (currentRow + lines.Count - 1);
				lastp = last.Count;
				last.InsertRange (last.Count, rest);
			}

			// Now adjust column and row positions
			currentRow += lines.Count - 1;
			currentColumn = rest != null ? lastp : lines [lines.Count - 1].Count;
			Adjust ();
		}

		// The column we are tracking, or -1 if we are not tracking any column
		int columnTrack = -1;

		// Tries to snap the cursor to the tracking column
		void TrackColumn ()
		{
			// Now track the column
			var line = GetCurrentLine ();
			if (line.Count < columnTrack)
				currentColumn = line.Count;
			else if (columnTrack != -1)
				currentColumn = columnTrack;
			else if (currentColumn > line.Count)
				currentColumn = line.Count;
			Adjust ();
		}

		void Adjust ()
		{
			var offB = OffSetBackground ();
			var line = GetCurrentLine ();
			bool need = !NeedDisplay.IsEmpty || wrapNeeded;
			if (!wordWrap && currentColumn < leftColumn) {
				leftColumn = currentColumn;
				need = true;
			} else if (!wordWrap && (currentColumn - leftColumn + RightOffset > Frame.Width + offB.width ||
				TextModel.DisplaySize (line, leftColumn, currentColumn).size + RightOffset >= Frame.Width + offB.width)) {
				leftColumn = Math.Max (TextModel.CalculateLeftColumn (line, leftColumn,
					currentColumn + RightOffset, Frame.Width - 1 + offB.width, currentColumn), 0);
				need = true;
			}
			if (currentRow < topRow) {
				topRow = currentRow;
				need = true;
			} else if (currentRow - topRow + BottomOffset >= Frame.Height + offB.height) {
				topRow = Math.Min (Math.Max (currentRow - Frame.Height + 1 + BottomOffset, 0), currentRow);
				need = true;
			}
			if (need) {
				if (wrapNeeded) {
					WrapTextModel ();
					wrapNeeded = false;
				}
				SetNeedsDisplay ();
			} else {
				PositionCursor ();
			}
		}

		(int width, int height) OffSetBackground ()
		{
			int w = 0;
			int h = 0;
			if (SuperView?.Frame.Right - Frame.Right < 0) {
				w = SuperView.Frame.Right - Frame.Right - 1;
			}
			if (SuperView?.Frame.Bottom - Frame.Bottom < 0) {
				h = SuperView.Frame.Bottom - Frame.Bottom - 1;
			}
			return (w, h);
		}

		/// <summary>
		/// Will scroll the <see cref="TextView"/> to display the specified row at the top if <paramref name="isRow"/> is true or
		/// will scroll the <see cref="TextView"/> to display the specified column at the left if <paramref name="isRow"/> is false.
		/// </summary>
		/// <param name="idx">Row that should be displayed at the top or Column that should be displayed at the left,
		///  if the value is negative it will be reset to zero</param>
		/// <param name="isRow">If true (default) the <paramref name="idx"/> is a row, column otherwise.</param>
		public void ScrollTo (int idx, bool isRow = true)
		{
			if (idx < 0) {
				idx = 0;
			}
			if (isRow) {
				topRow = Math.Max (idx > model.Count - 1 ? model.Count - 1 : idx, 0);
			} else if (!wordWrap) {
				var maxlength = model.GetMaxVisibleLine (topRow, topRow + Frame.Height + RightOffset);
				leftColumn = Math.Max (idx > maxlength - 1 ? maxlength - 1 : idx, 0);
			}
			SetNeedsDisplay ();
		}

		bool lastWasKill;
		bool wrapNeeded;
		bool shiftSelecting;

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			int restCount;
			List<Rune> rest;
			bool lineRemoved = false;

			// Handle some state here - whether the last command was a kill
			// operation and the column tracking (up/down)
			switch (kb.Key) {
			case Key.N | Key.CtrlMask:
			case Key.CursorDown:
			case Key.P | Key.CtrlMask:
			case Key.CursorUp:
				lastWasKill = false;
				break;
			case Key.K | Key.CtrlMask:
				break;
			default:
				lastWasKill = false;
				columnTrack = -1;
				break;
			}

			// Dispatch the command.
			switch (kb.Key) {
			case Key.PageDown:
			case Key.V | Key.CtrlMask:
			case Key.PageDown | Key.ShiftMask:
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				int nPageDnShift = Frame.Height - 1;
				if (currentRow < model.Count) {
					if (columnTrack == -1)
						columnTrack = currentColumn;
					currentRow = (currentRow + nPageDnShift) > model.Count ? model.Count : currentRow + nPageDnShift;
					if (topRow < currentRow - nPageDnShift) {
						topRow = currentRow >= model.Count ? currentRow - nPageDnShift : topRow + nPageDnShift;
						SetNeedsDisplay ();
					}
					TrackColumn ();
					PositionCursor ();
				}
				break;

			case Key.PageUp:
			case ((int)'V' + Key.AltMask):
			case Key.PageUp | Key.ShiftMask:
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				int nPageUpShift = Frame.Height - 1;
				if (currentRow > 0) {
					if (columnTrack == -1)
						columnTrack = currentColumn;
					currentRow = currentRow - nPageUpShift < 0 ? 0 : currentRow - nPageUpShift;
					if (currentRow < topRow) {
						topRow = topRow - nPageUpShift < 0 ? 0 : topRow - nPageUpShift;
						SetNeedsDisplay ();
					}
					TrackColumn ();
					PositionCursor ();
				}
				break;

			case Key.N | Key.CtrlMask:
			case Key.CursorDown:
			case Key.CursorDown | Key.ShiftMask:
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				MoveDown ();
				break;

			case Key.P | Key.CtrlMask:
			case Key.CursorUp:
			case Key.CursorUp | Key.ShiftMask:
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				MoveUp ();
				break;

			case Key.F | Key.CtrlMask:
			case Key.CursorRight:
			case Key.CursorRight | Key.ShiftMask:
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				var currentLine = GetCurrentLine ();
				if (currentColumn < currentLine.Count) {
					currentColumn++;
				} else {
					if (currentRow + 1 < model.Count) {
						currentRow++;
						currentColumn = 0;
						if (currentRow >= topRow + Frame.Height) {
							topRow++;
							SetNeedsDisplay ();
						}
					}
				}
				Adjust ();
				break;

			case Key.B | Key.CtrlMask:
			case Key.CursorLeft:
			case Key.CursorLeft | Key.ShiftMask:
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				if (currentColumn > 0) {
					currentColumn--;
				} else {
					if (currentRow > 0) {
						currentRow--;
						if (currentRow < topRow) {
							topRow--;
							SetNeedsDisplay ();
						}
						currentLine = GetCurrentLine ();
						currentColumn = currentLine.Count;
					}
				}
				Adjust ();
				break;

			case Key.Delete:
			case Key.Backspace:
				if (isReadOnly)
					break;
				if (selecting) {
					Cut ();
					return true;
				}
				if (currentColumn > 0) {
					// Delete backwards 
					currentLine = GetCurrentLine ();
					currentLine.RemoveAt (currentColumn - 1);
					if (wordWrap && wrapManager.RemoveAt (currentRow, currentColumn - 1)) {
						wrapNeeded = true;
					}
					currentColumn--;
					if (currentColumn < leftColumn) {
						leftColumn--;
						SetNeedsDisplay ();
					} else
						SetNeedsDisplay (new Rect (0, currentRow - topRow, 1, Frame.Width));
				} else {
					// Merges the current line with the previous one.
					if (currentRow == 0)
						return true;
					var prowIdx = currentRow - 1;
					var prevRow = model.GetLine (prowIdx);
					var prevCount = prevRow.Count;
					model.GetLine (prowIdx).AddRange (GetCurrentLine ());
					model.RemoveLine (currentRow);
					if (wordWrap && wrapManager.RemoveLine (currentRow, currentColumn, out lineRemoved, false)) {
						wrapNeeded = true;
					}
					currentRow--;
					if (wrapNeeded && !lineRemoved) {
						currentColumn = Math.Max (prevCount - 1, 0);
					} else {
						currentColumn = prevCount;
					}
					Adjust ();
				}
				break;

			// Home, C-A
			case Key.Home:
			case Key.Home | Key.ShiftMask:
			case Key.A | Key.CtrlMask:
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				currentColumn = 0;
				Adjust ();
				break;
			case Key.DeleteChar:
			case Key.D | Key.CtrlMask: // Delete
				if (isReadOnly)
					break;
				if (selecting) {
					Cut ();
					return true;
				}
				currentLine = GetCurrentLine ();
				if (currentColumn == currentLine.Count) {
					if (currentRow + 1 == model.Count)
						break;
					var nextLine = model.GetLine (currentRow + 1);
					currentLine.AddRange (nextLine);
					model.RemoveLine (currentRow + 1);
					if (wordWrap && wrapManager.RemoveLine (currentRow, currentColumn, out _)) {
						wrapNeeded = true;
					}
					var sr = currentRow - topRow;
					SetNeedsDisplay (new Rect (0, sr, Frame.Width, sr + 1));
				} else {
					currentLine.RemoveAt (currentColumn);
					if (wordWrap && wrapManager.RemoveAt (currentRow, currentColumn)) {
						wrapNeeded = true;
					}
					var r = currentRow - topRow;
					SetNeedsDisplay (new Rect (currentColumn - leftColumn, r, Frame.Width, r + 1));
				}
				break;

			case Key.End:
			case Key.End | Key.ShiftMask:
			case Key.E | Key.CtrlMask: // End
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				currentLine = GetCurrentLine ();
				currentColumn = currentLine.Count;
				int pcol = leftColumn;
				Adjust ();
				break;

			case Key.K | Key.CtrlMask: // kill-to-end
				if (isReadOnly)
					break;
				var cRow = currentRow;
				SetWrapModel ();
				currentLine = GetCurrentLine ();
				var setLastWasKill = true;
				if (currentLine.Count == 0) {
					model.RemoveLine (currentRow);
					if (model.Count > 0 || lastWasKill) {
						var val = ustring.Make ((Rune)'\n');
						if (lastWasKill) {
							AppendClipboard (val);
						} else {
							SetClipboard (val);
						}
					}
					if (model.Count == 0) {
						// Prevents from adding line feeds if there is no more lines.
						setLastWasKill = false;
					}
					if (currentRow > 0) {
						currentRow--;
					}
				} else {
					restCount = currentLine.Count - currentColumn;
					rest = currentLine.GetRange (currentColumn, restCount);
					var val = ustring.Empty;
					if (currentColumn == 0 && lastWasKill && currentLine.Count > 0) {
						val = ustring.Make ((Rune)'\n');
					}
					val += StringFromRunes (rest);
					if (lastWasKill) {
						AppendClipboard (val);
					} else {
						SetClipboard (val);
					}
					if (currentColumn == 0) {
						model.RemoveLine (currentRow);
						if (currentRow > 0) {
							currentRow--;
						}
					} else {
						currentLine.RemoveRange (currentColumn, restCount);
					}
					if (model.Count == 0) {
						// Prevents from adding line feeds if there is no more lines.
						setLastWasKill = false;
					}
				}
				UpdateWrapModel ();
				SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, Frame.Height));
				lastWasKill = setLastWasKill;
				break;

			case Key.Y | Key.CtrlMask: // Control-y, yank
				if (isReadOnly)
					break;
				Paste ();
				return true;

			case Key.Space | Key.CtrlMask:
				selecting = !selecting;
				selectionStartColumn = currentColumn;
				selectionStartRow = currentRow;
				break;

			case ((int)'C' + Key.AltMask):
			case Key.C | Key.CtrlMask:
				Copy ();
				return true;

			case ((int)'W' + Key.AltMask):
			case Key.W | Key.CtrlMask:
			case Key.X | Key.CtrlMask:
				Cut ();
				return true;

			case Key.CtrlMask | Key.CursorLeft:
			case Key.CtrlMask | Key.CursorLeft | Key.ShiftMask:
			case (Key)((int)'B' + Key.AltMask):
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				var newPos = WordBackward (currentColumn, currentRow);
				if (newPos.HasValue) {
					currentColumn = newPos.Value.col;
					currentRow = newPos.Value.row;
				}
				Adjust ();

				break;

			case Key.CtrlMask | Key.CursorRight:
			case Key.CtrlMask | Key.CursorRight | Key.ShiftMask:
			case (Key)((int)'F' + Key.AltMask):
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				newPos = WordForward (currentColumn, currentRow);
				if (newPos.HasValue) {
					currentColumn = newPos.Value.col;
					currentRow = newPos.Value.row;
				}
				Adjust ();
				break;

			case Key.Enter:
				if (isReadOnly)
					break;
				currentLine = GetCurrentLine ();
				restCount = currentLine.Count - currentColumn;
				rest = currentLine.GetRange (currentColumn, restCount);
				currentLine.RemoveRange (currentColumn, restCount);
				model.AddLine (currentRow + 1, rest);
				if (wordWrap) {
					wrapManager.AddLine (currentRow, currentColumn);
					wrapNeeded = true;
				}
				currentRow++;
				bool fullNeedsDisplay = false;
				if (currentRow >= topRow + Frame.Height) {
					topRow++;
					fullNeedsDisplay = true;
				}
				currentColumn = 0;
				if (!wordWrap && currentColumn < leftColumn) {
					fullNeedsDisplay = true;
					leftColumn = 0;
				}

				if (fullNeedsDisplay)
					SetNeedsDisplay ();
				else
					SetNeedsDisplay (new Rect (0, currentRow - topRow, 2, Frame.Height));
				break;

			case Key.CtrlMask | Key.End:
			case Key.CtrlMask | Key.End | Key.ShiftMask:
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				MoveEnd ();
				break;

			case Key.CtrlMask | Key.Home:
			case Key.CtrlMask | Key.Home | Key.ShiftMask:
				if (kb.Key.HasFlag (Key.ShiftMask)) {
					StartSelecting ();
				} else if (shiftSelecting && selecting) {
					StopSelecting ();
				}
				MoveHome ();
				break;

			default:
				// Ignore control characters and other special keys
				if (kb.Key < Key.Space || kb.Key > Key.CharMask)
					return false;
				//So that special keys like tab can be processed
				if (isReadOnly)
					return true;
				if (selecting) {
					Cut ();
				}
				Insert ((uint)kb.Key);
				currentColumn++;
				if (currentColumn >= leftColumn + Frame.Width) {
					leftColumn++;
					SetNeedsDisplay ();
				}
				break;
			}
			DoNeededAction ();

			return true;
		}

		///<inheritdoc/>
		public override bool OnKeyUp (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.Space | Key.CtrlMask:
				return true;
			}

			return false;
		}

		void DoNeededAction ()
		{
			if (NeedDisplay.IsEmpty) {
				PositionCursor ();
			} else {
				Adjust ();
			}
		}

		/// <summary>
		/// Copy the selected text to the clipboard contents.
		/// </summary>
		public void Copy ()
		{
			SetWrapModel ();
			SetClipboard (GetRegion ());
			UpdateWrapModel ();
			DoNeededAction ();
		}

		/// <summary>
		/// Cut the selected text to the clipboard contents.
		/// </summary>
		public void Cut ()
		{
			SetWrapModel ();
			SetClipboard (GetRegion ());
			if (!isReadOnly) {
				ClearRegion ();
			}
			UpdateWrapModel ();
			selecting = false;
			DoNeededAction ();
		}

		/// <summary>
		/// Paste the clipboard contents into the current selected position.
		/// </summary>
		public void Paste ()
		{
			if (isReadOnly) {
				return;
			}

			SetWrapModel ();
			if (selecting) {
				ClearRegion ();
			}
			InsertText (Clipboard.Contents);
			UpdateWrapModel ();
			selecting = false;
			DoNeededAction ();
		}

		void StartSelecting ()
		{
			if (shiftSelecting && selecting) {
				return;
			}
			shiftSelecting = true;
			selecting = true;
			selectionStartColumn = currentColumn;
			selectionStartRow = currentRow;
		}

		void StopSelecting ()
		{
			shiftSelecting = false;
			selecting = false;
		}

		void MoveUp ()
		{
			if (currentRow > 0) {
				if (columnTrack == -1) {
					columnTrack = currentColumn;
				}
				currentRow--;
				if (currentRow < topRow) {
					topRow--;
					SetNeedsDisplay ();
				}
				TrackColumn ();
				PositionCursor ();
			}
		}

		void MoveDown ()
		{
			if (currentRow + 1 < model.Count) {
				if (columnTrack == -1) {
					columnTrack = currentColumn;
				}
				currentRow++;
				if (currentRow + BottomOffset >= topRow + Frame.Height) {
					topRow++;
					SetNeedsDisplay ();
				}
				TrackColumn ();
				PositionCursor ();
			} else if (currentRow > Frame.Height) {
				Adjust ();
			}
		}

		IEnumerable<(int col, int row, Rune rune)> ForwardIterator (int col, int row)
		{
			if (col < 0 || row < 0)
				yield break;
			if (row >= model.Count)
				yield break;
			var line = GetCurrentLine ();
			if (col >= line.Count)
				yield break;

			while (row < model.Count) {
				for (int c = col; c < line.Count; c++) {
					yield return (c, row, line [c]);
				}
				col = 0;
				row++;
				line = GetCurrentLine ();
			}
		}

		Rune RuneAt (int col, int row)
		{
			var line = model.GetLine (row);
			return line [col > line.Count - 1 ? line.Count - 1 : col];
		}

		/// <summary>
		/// Will scroll the <see cref="TextView"/> to the last line and position the cursor there.
		/// </summary>
		public void MoveEnd ()
		{
			currentRow = model.Count - 1;
			var line = GetCurrentLine ();
			currentColumn = line.Count;
			TrackColumn ();
			PositionCursor ();
		}

		/// <summary>
		/// Will scroll the <see cref="TextView"/> to the first line and position the cursor there.
		/// </summary>
		public void MoveHome ()
		{
			currentRow = 0;
			currentColumn = 0;
			TrackColumn ();
			PositionCursor ();
		}

		bool MoveNext (ref int col, ref int row, out Rune rune)
		{
			var line = model.GetLine (row);
			if (col + 1 < line.Count) {
				col++;
				rune = line [col];
				return true;
			}
			while (row + 1 < model.Count) {
				col = 0;
				row++;
				line = model.GetLine (row);
				if (line.Count > 0) {
					rune = line [0];
					return true;
				}
			}
			rune = 0;
			return false;
		}

		bool MovePrev (ref int col, ref int row, out Rune rune)
		{
			var line = model.GetLine (row);

			if (col > 0) {
				col--;
				rune = line [col];
				return true;
			}
			if (row == 0) {
				rune = 0;
				return false;
			}
			while (row > 0) {
				row--;
				line = model.GetLine (row);
				col = line.Count - 1;
				if (col >= 0) {
					rune = line [col];
					return true;
				}
			}
			rune = 0;
			return false;
		}

		(int col, int row)? WordForward (int fromCol, int fromRow)
		{
			var col = fromCol;
			var row = fromRow;
			try {
				var rune = RuneAt (col, row);

				var srow = row;
				if (Rune.IsPunctuation (rune) || Rune.IsWhiteSpace (rune)) {
					while (MoveNext (ref col, ref row, out rune)) {
						if (Rune.IsLetterOrDigit (rune))
							break;
					}
					if (row != fromRow && Rune.IsLetterOrDigit (rune)) {
						return (col, row);
					}
					while (MoveNext (ref col, ref row, out rune)) {
						if (!Rune.IsLetterOrDigit (rune))
							break;
					}
				} else {
					while (MoveNext (ref col, ref row, out rune)) {
						if (!Rune.IsLetterOrDigit (rune))
							break;
					}
				}
				if (fromCol != col || fromRow != row)
					return (col + 1, row);
				return null;
			} catch (Exception) {
				return null;
			}
		}

		(int col, int row)? WordBackward (int fromCol, int fromRow)
		{
			if (fromRow == 0 && fromCol == 0)
				return null;

			var col = Math.Max (fromCol - 1, 0);
			var row = fromRow;
			try {
				var rune = RuneAt (col, row);

				if (Rune.IsPunctuation (rune) || Rune.IsSymbol (rune) || Rune.IsWhiteSpace (rune)) {
					while (MovePrev (ref col, ref row, out rune)) {
						if (Rune.IsLetterOrDigit (rune))
							break;
					}
					int lastValidCol = -1;
					while (MovePrev (ref col, ref row, out rune)) {
						if (col == 0 && Rune.IsLetterOrDigit (rune)) {
							return (col, row);
						} else if (col == 0 && !Rune.IsLetterOrDigit (rune) && lastValidCol > -1) {
							col = lastValidCol;
							return (col, row);
						}
						if (!Rune.IsLetterOrDigit (rune)) {
							break;
						}
						lastValidCol = Rune.IsLetterOrDigit (rune) ? col : -1;
					}
				} else {
					while (MovePrev (ref col, ref row, out rune)) {
						if (!Rune.IsLetterOrDigit (rune))
							break;
					}
				}
				if (fromCol != col && fromRow == row) {
					return (col == 0 ? col : col + 1, row);
				} else if (fromCol != col && fromRow != row) {
					return (col + 1, row);
				}
				return null;
			} catch (Exception) {
				return null;
			}
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Clicked) && !ev.Flags.HasFlag (MouseFlags.Button1Pressed)
				&& !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)
				&& !ev.Flags.HasFlag (MouseFlags.Button1Released)
				&& !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift)
				&& !ev.Flags.HasFlag (MouseFlags.WheeledDown) && !ev.Flags.HasFlag (MouseFlags.WheeledUp)) {
				return false;
			}

			if (!CanFocus) {
				return true;
			}

			if (!HasFocus) {
				SetFocus ();
			}

			if (ev.Flags == MouseFlags.Button1Clicked) {
				if (shiftSelecting) {
					shiftSelecting = false;
					selecting = false;
				}
				ProcessMouseClick (ev, out _);
				PositionCursor ();
				lastWasKill = false;
				columnTrack = currentColumn;
			} else if (ev.Flags == MouseFlags.WheeledDown) {
				lastWasKill = false;
				columnTrack = currentColumn;
				ScrollTo (topRow + 1);
			} else if (ev.Flags == MouseFlags.WheeledUp) {
				lastWasKill = false;
				columnTrack = currentColumn;
				ScrollTo (topRow - 1);
			} else if (ev.Flags == MouseFlags.WheeledRight) {
				lastWasKill = false;
				columnTrack = currentColumn;
				ScrollTo (leftColumn + 1, false);
			} else if (ev.Flags == MouseFlags.WheeledLeft) {
				lastWasKill = false;
				columnTrack = currentColumn;
				ScrollTo (leftColumn - 1, false);
			} else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
				ProcessMouseClick (ev, out List<Rune> line);
				PositionCursor ();
				if (model.Count > 0 && shiftSelecting && selecting) {
					if (currentRow - topRow + BottomOffset >= Frame.Height - 1
						&& model.Count + BottomOffset > topRow + currentRow) {
						ScrollTo (topRow + Frame.Height);
					} else if (topRow > 0 && currentRow <= topRow) {
						ScrollTo (topRow - Frame.Height);
					} else if (ev.Y >= Frame.Height) {
						ScrollTo (model.Count + BottomOffset);
					} else if (ev.Y < 0 && topRow > 0) {
						ScrollTo (0);
					}
					if (currentColumn - leftColumn + RightOffset >= Frame.Width - 1
						&& line.Count + RightOffset > leftColumn + currentColumn) {
						ScrollTo (leftColumn + Frame.Width, false);
					} else if (leftColumn > 0 && currentColumn <= leftColumn) {
						ScrollTo (leftColumn - Frame.Width, false);
					} else if (ev.X >= Frame.Width) {
						ScrollTo (line.Count + RightOffset, false);
					} else if (ev.X < 0 && leftColumn > 0) {
						ScrollTo (0, false);
					}
				}
				lastWasKill = false;
				columnTrack = currentColumn;
			} else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift)) {
				if (!shiftSelecting) {
					StartSelecting ();
				}
				ProcessMouseClick (ev, out _);
				PositionCursor ();
				lastWasKill = false;
				columnTrack = currentColumn;
			} else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed)) {
				if (shiftSelecting) {
					shiftSelecting = false;
					selecting = false;
				}
				ProcessMouseClick (ev, out _);
				PositionCursor ();
				if (!selecting) {
					StartSelecting ();
				}
				lastWasKill = false;
				columnTrack = currentColumn;
				if (Application.mouseGrabView == null) {
					Application.GrabMouse (this);
				}
			} else if (ev.Flags.HasFlag (MouseFlags.Button1Released)) {
				Application.UngrabMouse ();
			}

			return true;
		}

		void ProcessMouseClick (MouseEvent ev, out List<Rune> line)
		{
			List<Rune> r = null;
			if (model.Count > 0) {
				var maxCursorPositionableLine = Math.Max ((model.Count - 1) - topRow, 0);
				if (Math.Max (ev.Y, 0) > maxCursorPositionableLine) {
					currentRow = maxCursorPositionableLine + topRow;
				} else {
					currentRow = Math.Max (ev.Y + topRow, 0);
				}
				r = GetCurrentLine ();
				var idx = TextModel.GetColFromX (r, leftColumn, Math.Max (ev.X, 0));
				if (idx - leftColumn >= r.Count + RightOffset) {
					currentColumn = Math.Max (r.Count - leftColumn + RightOffset, 0);
				} else {
					currentColumn = idx + leftColumn;
				}
			}

			line = r;
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			if (Application.mouseGrabView != null && Application.mouseGrabView == this) {
				Application.UngrabMouse ();
			}

			return base.OnLeave (view);
		}
	}

}
