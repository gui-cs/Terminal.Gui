//
// TextView.cs: multi-line text editing
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// 
// TODO:
// Attributed text on spans
// Render selection
// Mark/Delete/Cut commands
// Replace insertion with Insert method
// String accumulation (Control-k, control-k is not preserving the last new line, see StringToRunes
//
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
			if (i - start > 0)
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
	///   Text data entry widget
	/// </summary>
	/// <remarks>
	///   The Entry widget provides Emacs-like editing
	///   functionality,  and mouse support.
	/// </remarks>
	public class TextView : View {
		TextModel model = new TextModel ();
		int topRow;
		int leftColumn;
		int currentRow;
		int currentColumn;
		bool used;

		/// <summary>
		///   Changed event, raised when the text has clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the text in the entry changes.
		/// </remarks>
		public event EventHandler Changed;

		/// <summary>
		///   Public constructor.
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
		///   Sets the cursor position.
		/// </summary>
		public override void PositionCursor ()
		{
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

		/// <summary>
		/// Redraw the text editor region 
		/// </summary>
		/// <param name="region">The region to redraw.</param>
		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (ColorScheme.Focus);
			Move (0, 0);

			int bottom = region.Bottom;
			int right = region.Right;
			for (int row = region.Top; row < bottom; row++) {
				int textLine = topRow + row;
				if (textLine >= model.Count) {
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

