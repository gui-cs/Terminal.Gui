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
			// BUGBUG: I think this is buggy w.r.t Unicode. content.Length is bytes, and content[i] is bytes
			// and content[i] == 10 may be the middle of a Rune.
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
		public List<Rune> GetLine (int line) => line < Count ? lines [line] : lines [Count - 1];

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
			lines.RemoveAt (pos);
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

		public TextModel Model { get; }

		public WordWrapManager (TextModel model)
		{
			Model = model;
		}

		public TextModel WrapModel (int width, out int nRow, out int nCol, int row = 0, int col = 0)
		{
			var wrappedModel = new TextModel ();
			int lines = 0;
			var modelRow = GetModelLineFromWrappedLines (row);
			var modelCol = GetModelColFromWrappedLines (row, col);
			nRow = 0;
			nCol = 0;
			bool isRowAndColSetted = row == 0 && col == 0;
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
			ResetCursorVisibility ();
		}

		/// <summary>
		///   Sets or gets the text in the <see cref="TextView"/>.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override ustring Text {
			get {
				return model.ToString ();
			}

			set {
				ResetPosition ();
				model.LoadString (value);
				TextChanged?.Invoke ();
				SetNeedsDisplay ();
			}
		}

		///<inheritdoc/>
		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;
				if (wordWrap && wrapManager != null) {
					model = wrapManager.WrapModel (Frame.Width - 2,
						out int nRow, out int nCol,
						currentRow, currentColumn);
					currentRow = nRow;
					currentColumn = nCol;
					SetNeedsDisplay ();
				}
				Adjust ();
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
					model = wrapManager.WrapModel (Frame.Width - 2, out _, out _);
				} else if (!wordWrap && wrapManager != null) {
					model = wrapManager.Model;
				}
				SetNeedsDisplay ();
			}
		}

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
			if (HasFocus)
				Driver.SetAttribute (ColorScheme.Focus);
			else
				Driver.SetAttribute (ColorScheme.Normal);
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
			return q >= start && q <= end;
		}

		//
		// Returns a ustring with the text in the selected 
		// region.
		//
		ustring GetRegion ()
		{
			long start, end;
			GetEncodedRegionBounds (out start, out end);
			int startRow = (int)(start >> 32);
			var maxrow = ((int)(end >> 32));
			int startCol = (int)(start & 0xffffffff);
			var endCol = (int)(end & 0xffffffff);
			var line = model.GetLine (startRow);

			if (startRow == maxrow)
				return StringFromRunes (line.GetRange (startCol, endCol));

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
				SetNeedsDisplay (new Rect (0, startRow - topRow, Frame.Width, startRow - topRow + 1));
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

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			ColorNormal ();

			int bottom = bounds.Bottom;
			int right = bounds.Right;
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
					if (selecting && PointInSelection (idx, row)) {
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
			Clipboard.Contents = text;
		}

		void AppendClipboard (ustring text)
		{
			Clipboard.Contents = Clipboard.Contents + text;
		}

		void Insert (Rune rune)
		{
			var line = GetCurrentLine ();
			line.Insert (currentColumn, rune);
			var prow = currentRow - topRow;

			SetNeedsDisplay (new Rect (0, prow, Frame.Width, prow + 1));
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
				if (currentColumn - leftColumn > Frame.Width) {
					leftColumn = currentColumn - Frame.Width + 1;
				}
				SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, currentRow - topRow + 1));
				return;
			}

			// Keep a copy of the rest of the line
			var restCount = line.Count - currentColumn;
			var rest = line.GetRange (currentColumn, restCount);
			line.RemoveRange (currentColumn, restCount);

			// First line is inserted at the current location, the rest is appended
			line.InsertRange (currentColumn, lines [0]);

			for (int i = 1; i < lines.Count; i++) {
				model.AddLine (currentRow + i, lines [i]);
			}

			var last = model.GetLine (currentRow + lines.Count - 1);
			var lastp = last.Count;
			last.InsertRange (last.Count, rest);

			// Now adjust column and row positions
			currentRow += lines.Count - 1;
			currentColumn = lastp;
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
			bool need = !NeedDisplay.IsEmpty;
			if (currentColumn < leftColumn) {
				leftColumn = currentColumn;
				need = true;
			} else if (currentColumn - leftColumn > Frame.Width + offB.width ||
				TextModel.DisplaySize (line, leftColumn, currentColumn).size >= Frame.Width + offB.width) {
				leftColumn = Math.Max (TextModel.CalculateLeftColumn (line, leftColumn,
					currentColumn, Frame.Width - 1 + offB.width, currentColumn), 0);
				need = true;
			}
			if (currentRow < topRow) {
				topRow = currentRow;
				need = true;
			} else if (currentRow - topRow >= Frame.Height + offB.height) {
				topRow = Math.Min (Math.Max (currentRow - Frame.Height + 1, 0), currentRow);
				need = true;
			}
			if (need) {
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
			} else {
				var maxlength = model.GetMaxVisibleLine (topRow, topRow + Frame.Height);
				leftColumn = Math.Max (idx > maxlength - 1 ? maxlength - 1 : idx, 0);
			}
			SetNeedsDisplay ();
		}

		bool lastWasKill;

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			int restCount;
			List<Rune> rest;

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
				MoveDown ();
				break;

			case Key.P | Key.CtrlMask:
			case Key.CursorUp:
				MoveUp ();
				break;

			case Key.F | Key.CtrlMask:
			case Key.CursorRight:
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
				if (currentColumn > 0) {
					// Delete backwards 
					currentLine = GetCurrentLine ();
					currentLine.RemoveAt (currentColumn - 1);
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
					currentRow--;
					currentColumn = prevCount;
					Adjust ();
				}
				break;

			// Home, C-A
			case Key.Home:
			case Key.A | Key.CtrlMask:
				currentColumn = 0;
				Adjust ();
				break;
			case Key.DeleteChar:
			case Key.D | Key.CtrlMask: // Delete
				if (isReadOnly)
					break;
				currentLine = GetCurrentLine ();
				if (currentColumn == currentLine.Count) {
					if (currentRow + 1 == model.Count)
						break;
					var nextLine = model.GetLine (currentRow + 1);
					currentLine.AddRange (nextLine);
					model.RemoveLine (currentRow + 1);
					var sr = currentRow - topRow;
					SetNeedsDisplay (new Rect (0, sr, Frame.Width, sr + 1));
				} else {
					currentLine.RemoveAt (currentColumn);
					var r = currentRow - topRow;
					SetNeedsDisplay (new Rect (currentColumn - leftColumn, r, Frame.Width, r + 1));
				}
				break;

			case Key.End:
			case Key.E | Key.CtrlMask: // End
				currentLine = GetCurrentLine ();
				currentColumn = currentLine.Count;
				int pcol = leftColumn;
				Adjust ();
				break;

			case Key.K | Key.CtrlMask: // kill-to-end
				if (isReadOnly)
					break;
				currentLine = GetCurrentLine ();
				if (currentLine.Count == 0) {
					model.RemoveLine (currentRow);
					var val = ustring.Make ((Rune)'\n');
					if (lastWasKill)
						AppendClipboard (val);
					else
						SetClipboard (val);
				} else {
					restCount = currentLine.Count - currentColumn;
					rest = currentLine.GetRange (currentColumn, restCount);
					var val = StringFromRunes (rest);
					if (lastWasKill)
						AppendClipboard (val);
					else
						SetClipboard (val);
					currentLine.RemoveRange (currentColumn, restCount);
				}
				SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, Frame.Height));
				lastWasKill = true;
				break;

			case Key.Y | Key.CtrlMask: // Control-y, yank
				if (isReadOnly)
					break;
				InsertText (Clipboard.Contents);
				selecting = false;
				break;

			case Key.Space | Key.CtrlMask:
				selecting = true;
				selectionStartColumn = currentColumn;
				selectionStartRow = currentRow;
				break;

			case ((int)'W' + Key.AltMask):
			case Key.W | Key.CtrlMask:
				SetClipboard (GetRegion ());
				if (!isReadOnly)
					ClearRegion ();
				selecting = false;
				break;

			case Key.CtrlMask | Key.CursorLeft:
			case (Key)((int)'B' + Key.AltMask):
				var newPos = WordBackward (currentColumn, currentRow);
				if (newPos.HasValue) {
					currentColumn = newPos.Value.col;
					currentRow = newPos.Value.row;
				}
				Adjust ();

				break;

			case Key.CtrlMask | Key.CursorRight:
			case (Key)((int)'F' + Key.AltMask):
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
				currentRow++;
				bool fullNeedsDisplay = false;
				if (currentRow >= topRow + Frame.Height) {
					topRow++;
					fullNeedsDisplay = true;
				}
				currentColumn = 0;
				if (currentColumn < leftColumn) {
					fullNeedsDisplay = true;
					leftColumn = 0;
				}

				if (fullNeedsDisplay)
					SetNeedsDisplay ();
				else
					SetNeedsDisplay (new Rect (0, currentRow - topRow, 2, Frame.Height));
				break;

			case Key.CtrlMask | Key.End:
				MoveEnd ();
				break;

			case Key.CtrlMask | Key.Home:
				MoveHome ();
				break;

			default:
				// Ignore control characters and other special keys
				if (kb.Key < Key.Space || kb.Key > Key.CharMask)
					return false;
				//So that special keys like tab can be processed
				if (isReadOnly)
					return true;
				Insert ((uint)kb.Key);
				currentColumn++;
				if (currentColumn >= leftColumn + Frame.Width) {
					leftColumn++;
					SetNeedsDisplay ();
				}
				PositionCursor ();
				return true;
			}
			return true;
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
				if (currentRow >= topRow + Frame.Height) {
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

		Rune RuneAt (int col, int row) => model.GetLine (row) [col];

		/// <summary>
		/// Will scroll the <see cref="TextView"/> to the last line and position the cursor there.
		/// </summary>
		public void MoveEnd ()
		{
			currentRow = model.Count - 1;
			TrackColumn ();
			PositionCursor ();
		}

		/// <summary>
		/// Will scroll the <see cref="TextView"/> to the first line and position the cursor there.
		/// </summary>
		public void MoveHome ()
		{
			currentRow = 0;
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
					return (col, row);
				return null;
			} catch (Exception) {
				return null;
			}
		}

		(int col, int row)? WordBackward (int fromCol, int fromRow)
		{
			if (fromRow == 0 && fromCol == 0)
				return null;

			var col = fromCol;
			var row = fromRow;
			try {
				var rune = RuneAt (col, row);

				if (Rune.IsPunctuation (rune) || Rune.IsSymbol (rune) || Rune.IsWhiteSpace (rune)) {
					while (MovePrev (ref col, ref row, out rune)) {
						if (Rune.IsLetterOrDigit (rune))
							break;
					}
					while (MovePrev (ref col, ref row, out rune)) {
						if (!Rune.IsLetterOrDigit (rune))
							break;
					}
				} else {
					while (MovePrev (ref col, ref row, out rune)) {
						if (!Rune.IsLetterOrDigit (rune))
							break;
					}
				}
				if (fromCol != col || fromRow != row)
					return (col, row);
				return null;
			} catch (Exception) {
				return null;
			}
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Clicked) &&
				!ev.Flags.HasFlag (MouseFlags.WheeledDown) && !ev.Flags.HasFlag (MouseFlags.WheeledUp)) {
				return false;
			}

			if (!CanFocus) {
				return true;
			}

			if (!HasFocus) {
				SetFocus ();
			}

			if (ev.Flags == MouseFlags.Button1Clicked) {
				if (model.Count > 0) {
					var maxCursorPositionableLine = Math.Max ((model.Count - 1) - topRow, 0);
					if (ev.Y > maxCursorPositionableLine) {
						currentRow = maxCursorPositionableLine;
					} else {
						currentRow = ev.Y + topRow;
					}
					var r = GetCurrentLine ();
					var idx = TextModel.GetColFromX (r, leftColumn, ev.X);
					if (idx - leftColumn >= r.Count) {
						currentColumn = r.Count - leftColumn;
					} else {
						currentColumn = idx + leftColumn;
					}
				}
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
			}

			return true;
		}
	}

}
