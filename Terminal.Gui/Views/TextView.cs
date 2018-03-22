//
// TextView.cs: multi-line text editing
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// 
// TODO:
// Attributed text on spans
// Replace insertion with Insert method
// String accumulation (Control-k, control-k is not preserving the last new line, see StringToRunes
// Alt-D, Alt-Backspace
// API to set the cursor position
// API to scroll to a particular place
// keybindings to go to top/bottom
// public API to insert, remove ranges
// Add word forward/word backwards commands

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NStack;

namespace Terminal.Gui {
	class TextModel {
		List<List<Rune>> lines;

		public bool LoadFile (string file)
		{
			if (file == null)
				throw new ArgumentNullException (nameof (file));
			try {
				var stream = File.OpenRead (file);
				if (stream == null)
					return false;
			} catch {
				return false;
			}
			LoadStream (File.OpenRead (file));
			return true;
		}

		// Turns the ustring into runes, this does not split the 
		// contents on a newline if it is present.
		static List<Rune> ToRunes (ustring str)
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
			foreach (var line in lines) {
				sb.Append (line);
				sb.AppendLine ();
			}
			return sb.ToString ();
		}

		public int Count => lines.Count;

		public List<Rune> GetLine (int line) => lines [line];

		public void AddLine (int pos, List<Rune> runes)
		{
			lines.Insert (pos, runes);
		}

		public void RemoveLine (int pos)
		{
			lines.RemoveAt (pos);
		}
	}

	/// <summary>
	///   Multi-line text editing view
	/// </summary>
	/// <remarks>
	///   <para>
	///     The text view provides a multi-line text view.   Users interact
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

#if false
		/// <summary>
		///   Changed event, raised when the text has clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the text in the entry changes.
		/// </remarks>
		public event EventHandler Changed;
#endif
		/// <summary>
		///   Public constructor, creates a view on the specfied area
		/// </summary>
		/// <remarks>
		/// </remarks>
		public TextView (Rect frame) : base (frame)
		{
			CanFocus = true;
		}

		void ResetPosition ()
		{
			topRow = leftColumn = currentRow = currentColumn = 0;
		}

		/// <summary>
		///   Sets or gets the text in the entry.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public ustring Text {
			get {
				return model.ToString ();
			}

			set {
				ResetPosition ();
				model.LoadString (value);
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		///    The current cursor row.
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
				var minRow = Math.Min (Math.Max (Math.Min (selectionStartRow, currentRow)-topRow, 0), Frame.Height);
				var maxRow = Math.Min (Math.Max (Math.Max (selectionStartRow, currentRow) - topRow, 0), Frame.Height);

				SetNeedsDisplay (new Rect (0, minRow, Frame.Width, maxRow));
			}
			Move (CurrentColumn - leftColumn, CurrentRow - topRow);
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

			for (int row = startRow+1; row < maxrow; row++) {
				res = res + ustring.Make (10) + StringFromRunes (model.GetLine (row));
			}
			line = model.GetLine (maxrow);
			res = res + ustring.Make (10) + StringFromRunes (line.GetRange (0, endCol));
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
				model.RemoveLine (startRow+1);
			}
			if (currentEncoded == end) {
				currentRow -= maxrow - (startRow);
			}
			currentColumn = startCol;

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Redraw the text editor region 
		/// </summary>
		/// <param name="region">The region to redraw.</param>
		public override void Redraw (Rect region)
		{
			ColorNormal ();

			int bottom = region.Bottom;
			int right = region.Right;
			for (int row = region.Top; row < bottom; row++) {
				int textLine = topRow + row;
				if (textLine >= model.Count) {
					ColorNormal ();
					ClearRegion (region.Left, row, region.Right, row + 1);
					continue;
				}
				var line = model.GetLine (textLine);
				int lineRuneCount = line.Count;
				if (line.Count < region.Left){
					ClearRegion (region.Left, row, region.Right, row + 1);
					continue;
				}

				Move (region.Left, row);
				for (int col = region.Left; col < right; col++) {
					var lineCol = leftColumn + col;
					var rune = lineCol >= lineRuneCount ? ' ' : line [lineCol];
					if (selecting && PointInSelection (col, row))
						ColorSelection ();
					else
						ColorNormal ();
					
					AddRune (col, row, rune);
				}
			}
			PositionCursor ();
		}

		public override bool CanFocus {
			get => true;
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
			var lines = TextModel.StringToRunes (text);

			if (lines.Count == 0)
				return;

			var line = GetCurrentLine ();

			// Optmize single line
			if (lines.Count == 1) {
				line.InsertRange (currentColumn, lines [0]);
				currentColumn += lines [0].Count;
				if (currentColumn - leftColumn > Frame.Width)
					leftColumn = currentColumn - Frame.Width + 1;
				SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, currentRow - topRow + 1));
				return;
			}

			// Keep a copy of the rest of the line
			var restCount = line.Count - currentColumn;
			var rest = line.GetRange (currentColumn, restCount);
			line.RemoveRange (currentColumn, restCount);

			// First line is inserted at the current location, the rest is appended
			line.InsertRange (currentColumn, lines [0]);

			for (int i = 1; i < lines.Count; i++)
				model.AddLine (currentRow + i, lines [i]);

			var last = model.GetLine (currentRow + lines.Count-1);
			var lastp = last.Count;
			last.InsertRange (last.Count, rest);

			// Now adjjust column and row positions
			currentRow += lines.Count-1;
			currentColumn = lastp;
			if (currentRow - topRow > Frame.Height) {
				topRow = currentRow - Frame.Height + 1;
				if (topRow < 0)
					topRow = 0;
			}
			if (currentColumn < leftColumn)
				leftColumn = currentColumn;
			if (currentColumn-leftColumn >= Frame.Width)
				leftColumn = currentColumn - Frame.Width + 1;
			SetNeedsDisplay ();
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
			
			if (currentColumn < leftColumn) {
				leftColumn = currentColumn;
				SetNeedsDisplay ();
			}
			if (currentColumn - leftColumn > Frame.Width) {
				leftColumn = currentColumn - Frame.Width + 1;
				SetNeedsDisplay ();
			}
		}

		bool lastWasKill;

		public override bool ProcessKey (KeyEvent kb)
		{
			int restCount;
			List<Rune> rest;

			// Handle some state here - whether the last command was a kill
			// operation and the column tracking (up/down)
			switch (kb.Key) {
			case Key.ControlN:
			case Key.CursorDown:
			case Key.ControlP:
			case Key.CursorUp:
				lastWasKill = false;
				break;
			case Key.ControlK:
				break;
			default:
				lastWasKill = false;
				columnTrack = -1;
				break;
			}

			// Dispatch the command.
			switch (kb.Key) {
			case Key.ControlN:
			case Key.CursorDown:
				if (currentRow + 1 < model.Count) {
					if (columnTrack == -1)
						columnTrack = currentColumn;
					currentRow++;
					if (currentRow >= topRow + Frame.Height) {
						topRow++;
						SetNeedsDisplay ();
					}
					TrackColumn ();
					PositionCursor ();
				}
				break;

			case Key.ControlP:
			case Key.CursorUp:
				if (currentRow > 0) {
					if (columnTrack == -1)
						columnTrack = currentColumn;
					currentRow--;
					if (currentRow < topRow) {
						topRow--;
						SetNeedsDisplay ();
					}
					TrackColumn ();
					PositionCursor ();
				}
				break;

			case Key.ControlF:
			case Key.CursorRight:
				var currentLine = GetCurrentLine ();
				if (currentColumn < currentLine.Count) {
					currentColumn++;
					if (currentColumn >= leftColumn + Frame.Width) {
						leftColumn++;
						SetNeedsDisplay ();
					}
					PositionCursor ();
				} else {
					if (currentRow + 1 < model.Count) {
						currentRow++;
						currentColumn = 0;
						leftColumn = 0;
						if (currentRow >= topRow + Frame.Height) {
							topRow++;
						}
						SetNeedsDisplay ();
						PositionCursor ();
					}
					break;
				}
				break;

			case Key.ControlB:
			case Key.CursorLeft:
				if (currentColumn > 0) {
					currentColumn--;
					if (currentColumn < leftColumn) {
						leftColumn--;
						SetNeedsDisplay ();
					}
					PositionCursor ();
				} else {
					if (currentRow > 0) {
						currentRow--;
						if (currentRow < topRow) {
							topRow--;

						}
						currentLine = GetCurrentLine ();
						currentColumn = currentLine.Count;
						int prev = leftColumn;
						leftColumn = currentColumn - Frame.Width + 1;
						if (leftColumn < 0)
							leftColumn = 0;
						if (prev != leftColumn)
							SetNeedsDisplay ();
						PositionCursor ();
					}
				}
				break;

			case Key.Delete:
			case Key.Backspace:
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
					currentRow--;
					currentColumn = prevCount;
					leftColumn = currentColumn - Frame.Width + 1;
					if (leftColumn < 0)
						leftColumn = 0;
					SetNeedsDisplay ();
				}
				break;

			// Home, C-A
			case Key.Home:
			case Key.ControlA:
				currentColumn = 0;
				if (currentColumn < leftColumn) {
					leftColumn = 0;
					SetNeedsDisplay ();
				} else
					PositionCursor ();
				break;

			case Key.ControlD: // Delete
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

			case Key.ControlE: // End
				currentLine = GetCurrentLine ();
				currentColumn = currentLine.Count;
				int pcol = leftColumn;
				leftColumn = currentColumn - Frame.Width + 1;
				if (leftColumn < 0)
					leftColumn = 0;
				if (pcol != leftColumn)
					SetNeedsDisplay ();
				PositionCursor ();
				break;

			case Key.ControlK: // kill-to-end
				currentLine = GetCurrentLine ();
				if (currentLine.Count == 0) {
					model.RemoveLine (currentRow);
					var val = ustring.Make ('\n');
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

			case Key.ControlY: // Control-y, yank
				InsertText (Clipboard.Contents);
				selecting = false;
				break;

			case Key.ControlSpace:
				selecting = true;
				selectionStartColumn = currentColumn;
				selectionStartRow = currentRow;
				break;

			case (Key)((int)'w' + Key.AltMask):
				SetClipboard (GetRegion ());
				selecting = false;
				break;

			case Key.ControlW:
				SetClipboard (GetRegion ());
				ClearRegion ();
				selecting = false;
				break;

			case (Key)((int)'b' + Key.AltMask):
				break;

			case (Key)((int)'f' + Key.AltMask):
				break;

			case Key.Enter:
				var orow = currentRow;
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
					SetNeedsDisplay (new Rect (0, currentRow - topRow, 0, Frame.Height));
				break;

			default:
				// Ignore control characters and other special keys
				if (kb.Key < Key.Space || kb.Key > Key.CharMask)
					return false;
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

#if false
		int WordForward (int p)
		{
			if (p >= text.Length)
				return -1;

			int i = p;
			if (Rune.IsPunctuation (text [p]) || Rune.IsWhiteSpace (text [p])) {
				for (; i < text.Length; i++) {
					var r = text [i];
					if (Rune.IsLetterOrDigit (r))
						break;
				}
				for (; i < text.Length; i++) {
					var r = text [i];
					if (!Rune.IsLetterOrDigit (r))
						break;
				}
			} else {
				for (; i < text.Length; i++) {
					var r = text [i];
					if (!Rune.IsLetterOrDigit (r))
						break;
				}
			}
			if (i != p)
				return i;
			return -1;
		}

		int WordBackward (int p)
		{
			if (p == 0)
				return -1;

			int i = p - 1;
			if (i == 0)
				return 0;

			var ti = text [i];
			if (Rune.IsPunctuation (ti) || Rune.IsSymbol (ti) || Rune.IsWhiteSpace (ti)) {
				for (; i >= 0; i--) {
					if (Rune.IsLetterOrDigit (text [i]))
						break;
				}
				for (; i >= 0; i--) {
					if (!Rune.IsLetterOrDigit (text [i]))
						break;
				}
			} else {
				for (; i >= 0; i--) {
					if (!Rune.IsLetterOrDigit (text [i]))
						break;
				}
			}
			i++;

			if (i != p)
				return i;

			return -1;
		}

		public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Clicked))
				return false;

			if (!HasFocus)
				SuperView.SetFocus (this);

			// We could also set the cursor position.
			point = first + ev.X;
			if (point > text.Length)
				point = text.Length;
			if (point < first)
				point = 0;

			SetNeedsDisplay ();
			return true;
		}
#endif
	}

}

