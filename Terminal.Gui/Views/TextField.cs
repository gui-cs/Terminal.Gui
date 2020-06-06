//
// TextField.cs: single-line text editor with Emacs keybindings
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	///   Single-line text entry <see cref="View"/>
	/// </summary>
	/// <remarks>
	///   The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.
	/// </remarks>
	public class TextField : View {
		List<Rune> text;
		int first, point;
		bool used;

		/// <summary>
		/// Tracks whether the text field should be considered "used", that is, that the user has moved in the entry, so new input should be appended at the cursor position, rather than clearing the entry
		/// </summary>
		public bool Used { get => used; set { used = value; } }

		/// <summary>
		/// If set to true its not allow any changes in the text.
		/// </summary>
		public bool ReadOnly { get; set; } = false;

		/// <summary>
		///   Changed event, raised when the text has clicked.
		/// </summary>
		/// <remarks>
		///   This event is raised when the <see cref="Text"/> changes. 
		/// </remarks>
		/// <remarks>
		///   The passed <see cref="EventArgs"/> is a <see cref="ustring"/> containing the old value. 
		/// </remarks>
		public Action<ustring> TextChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (string text) : this (ustring.Make (text))
		{
			Height = 1;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public TextField () : this (string.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (ustring text)
		{
			Initialize (text, Frame.Width);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Absolute"/> positioning.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="w">The width.</param>
		/// <param name="text">Initial text contents.</param>
		public TextField (int x, int y, int w, ustring text) : base (new Rect (x, y, w, 1))
		{
			Initialize (text, w);
		}

		void Initialize (ustring text, int w)
		{
			if (text == null)
				text = "";

			this.text = TextModel.ToRunes (text);
			point = text.Length;
			first = point > w ? point - w : 0;
			CanFocus = true;
			Used = true;
			WantMousePositionReports = true;
		}

		///<inheritdoc/>
		public override bool OnLeave ()
		{
			if (Application.mouseGrabView != null && Application.mouseGrabView == this)
				Application.UngrabMouse ();
			if (SelectedLength != 0 && !(Application.mouseGrabView is MenuBar))
				ClearAllSelection ();

			return base.OnLeave ();
		}

		///<inheritdoc/>
		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;
				var w = base.Frame.Width;
				first = point > w ? point - w : 0;
				Adjust ();
			}
		}

		List<ustring> historyText;
		int idxhistoryText;
		bool isFromHistory;

		/// <summary>
		///   Sets or gets the text held by the view.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public ustring Text {
			get {
				return ustring.Make (text);
			}

			set {
				if (ReadOnly)
					return;

				var oldText = ustring.Make (text);

				if (oldText == value)
					return;

				text = TextModel.ToRunes (value);
				if (!Secret && !isFromHistory) {
					if (historyText == null)
						historyText = new List<ustring> () { oldText };
					if (idxhistoryText > 0 && idxhistoryText + 1 < historyText.Count)
						historyText.RemoveRange (idxhistoryText + 1, historyText.Count - idxhistoryText - 1);
					historyText.Add (ustring.Make (text));
					idxhistoryText++;
				}
				TextChanged?.Invoke (oldText);

				if (point > text.Count)
					point = Math.Max (DisplaySize (text, 0) - 1, 0);

				// FIXME: this needs to be updated to use Rune.ColumnWidth
				//first = point > Frame.Width ? point - Frame.Width : 0;
				Adjust ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		///   Sets the secret property.
		/// </summary>
		/// <remarks>
		///   This makes the text entry suitable for entering passwords.
		/// </remarks>
		public bool Secret { get; set; }

		/// <summary>
		///    Sets or gets the current cursor position.
		/// </summary>
		public int CursorPosition {
			get { return point; }
			set {
				point = value;
				Adjust ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		///   Sets the cursor position.
		/// </summary>
		public override void PositionCursor ()
		{
			var col = 0;
			for (int idx = first < 0 ? 0 : first; idx < text.Count; idx++) {
				if (idx == point)
					break;
				var cols = Rune.ColumnWidth (text [idx]);
				col += cols;
			}
			Move (col, 0);
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			ColorScheme color = Colors.Menu;
			SetSelectedStartSelectedLength ();

			Driver.SetAttribute (ColorScheme.Focus);
			Move (0, 0);

			int p = first;
			int col = 0;
			int width = Frame.Width;
			var tcount = text.Count;
			var roc = new Attribute (Color.DarkGray, Color.Gray);
			for (int idx = 0; idx < tcount; idx++) {
				var rune = text [idx];
				if (idx < p)
					continue;
				var cols = Rune.ColumnWidth (rune);
				if (col == point && HasFocus && !Used && SelectedLength == 0 && !ReadOnly)
					Driver.SetAttribute (Colors.Menu.HotFocus);
				else if (ReadOnly)
					Driver.SetAttribute (idx >= start && length > 0 && idx < start + length ? color.Focus : roc);
				else
					Driver.SetAttribute (idx >= start && length > 0 && idx < start + length ? color.Focus : ColorScheme.Focus);
				if (col + cols <= width)
					Driver.AddRune ((Rune)(Secret ? '*' : rune));
				col += cols;
			}

			Driver.SetAttribute (ColorScheme.Focus);
			for (int i = col; i < Frame.Width; i++)
				Driver.AddRune (' ');

			PositionCursor ();
		}

		// Returns the size of the string starting at position start
		int DisplaySize (List<Rune> t, int start)
		{
			int size = 0;
			int tcount = t.Count;
			for (int i = start; i < tcount; i++) {
				var rune = t [i];
				size += Rune.ColumnWidth (rune);
			}
			return size;
		}

		void Adjust ()
		{
			int offB = 0;
			if (SuperView != null && SuperView.Frame.Right - Frame.Right < 0)
				offB = SuperView.Frame.Right - Frame.Right - 1;
			if (point < first)
				first = point;
			else if (first + point >= Frame.Width + offB) {
				first = point - (Frame.Width - 1 + offB);
			}
			SetNeedsDisplay ();
		}

		void SetText (List<Rune> newText)
		{
			Text = ustring.Make (newText);
		}

		void SetText (IEnumerable<Rune> newText)
		{
			SetText (newText.ToList ());
		}

		///<inheritdoc/>
		public override bool CanFocus {
			get => true;
			set { base.CanFocus = value; }
		}

		void SetClipboard (IEnumerable<Rune> text)
		{
			if (!Secret)
				Clipboard.Contents = ustring.Make (text.ToList ());
		}

		/// <summary>
		/// Processes key presses for the <see cref="TextField"/>.
		/// </summary>
		/// <param name="kb"></param>
		/// <returns></returns>
		/// <remarks>
		/// The <see cref="TextField"/> control responds to the following keys:
		/// <list type="table">
		///    <listheader>
		///        <term>Keys</term>
		///        <description>Function</description>
		///    </listheader>
		///    <item>
		///        <term><see cref="Key.Delete"/>, <see cref="Key.Backspace"/></term>
		///        <description>Deletes the character before cursor.</description>
		///    </item>
		/// </list>
		/// </remarks>
		public override bool ProcessKey (KeyEvent kb)
		{
			// remember current cursor position
			// because the new calculated cursor position is needed to be set BEFORE the change event is triggest
			// Needed for the Elmish Wrapper issue https://github.com/DieselMeister/Terminal.Gui.Elmish/issues/2
			var oldCursorPos = point;

			switch (kb.Key) {
			case Key.DeleteChar:
			case Key.ControlD:
				if (ReadOnly)
					return true;

				if (SelectedLength == 0) {
					if (text.Count == 0 || text.Count == point)
						return true;

					SetText (text.GetRange (0, point).Concat (text.GetRange (point + 1, text.Count - (point + 1))));
					Adjust ();

				} else {
					DeleteSelectedText ();
				}
				break;

			case Key.Delete:
			case Key.Backspace:
				if (ReadOnly)
					return true;

				if (SelectedLength == 0) {
					if (point == 0)
						return true;

					point--;
					SetText (text.GetRange (0, oldCursorPos - 1).Concat (text.GetRange (oldCursorPos, text.Count - (oldCursorPos))));
					Adjust ();
				} else {
					DeleteSelectedText ();
				}
				break;

			case Key.Home | Key.ShiftMask:
				if (point > 0) {
					int x = point;
					point = 0;
					PrepareSelection (x, point - x);
				}
				break;

			case Key.End | Key.ShiftMask:
				if (point < text.Count) {
					int x = point;
					point = text.Count;
					PrepareSelection (x, point - x);
				}
				break;

			// Home, C-A
			case Key.Home:
			case Key.ControlA:
				ClearAllSelection ();
				point = 0;
				Adjust ();
				break;

			case Key.CursorLeft | Key.ShiftMask:
			case Key.CursorUp | Key.ShiftMask:
				if (point > 0) {
					PrepareSelection (point--, -1);
				}
				break;

			case Key.CursorRight | Key.ShiftMask:
			case Key.CursorDown | Key.ShiftMask:
				if (point < text.Count) {
					PrepareSelection (point++, 1);
				}
				break;

			case Key.CursorLeft | Key.ShiftMask | Key.CtrlMask:
			case Key.CursorUp | Key.ShiftMask | Key.CtrlMask:
				if (point > 0) {
					int x = start > -1 ? start : point;
					int sbw = WordBackward (point);
					if (sbw != -1)
						point = sbw;
					PrepareSelection (x, sbw - x);
				}
				break;

			case Key.CursorRight | Key.ShiftMask | Key.CtrlMask:
			case Key.CursorDown | Key.ShiftMask | Key.CtrlMask:
				if (point < text.Count) {
					int x = start > -1 ? start : point;
					int sfw = WordForward (point);
					if (sfw != -1)
						point = sfw;
					PrepareSelection (x, sfw - x);
				}
				break;

			case Key.CursorLeft:
			case Key.ControlB:
				ClearAllSelection ();
				if (point > 0) {
					point--;
					Adjust ();
				}
				break;

			case Key.End:
			case Key.ControlE: // End
				ClearAllSelection ();
				point = text.Count;
				Adjust ();
				break;

			case Key.CursorRight:
			case Key.ControlF:
				ClearAllSelection ();
				if (point == text.Count)
					break;
				point++;
				Adjust ();
				break;

			case Key.ControlK: // kill-to-end
				if (ReadOnly)
					return true;

				ClearAllSelection ();
				if (point >= text.Count)
					return true;
				SetClipboard (text.GetRange (point, text.Count - point));
				SetText (text.GetRange (0, point));
				Adjust ();
				break;

			// Undo
			case Key.ControlZ:
				if (ReadOnly)
					return true;

				if (historyText != null && historyText.Count > 0) {
					isFromHistory = true;
					if (idxhistoryText > 0)
						idxhistoryText--;
					if (idxhistoryText > -1)
						Text = historyText [idxhistoryText];
					point = text.Count;
					isFromHistory = false;
				}
				break;

			//Redo
			case Key.ControlY: // Control-y, yank
				if (ReadOnly)
					return true;

				if (historyText != null && historyText.Count > 0) {
					isFromHistory = true;
					if (idxhistoryText < historyText.Count - 1) {
						idxhistoryText++;
						if (idxhistoryText < historyText.Count) {
							Text = historyText [idxhistoryText];
						} else if (idxhistoryText == historyText.Count - 1) {
							Text = historyText [historyText.Count - 1];
						}
						point = text.Count;
					}
					isFromHistory = false;
				}

				//if (Clipboard.Contents == null)
				//	return true;
				//var clip = TextModel.ToRunes (Clipboard.Contents);
				//if (clip == null)
				//	return true;

				//if (point == text.Count) {
				//	point = text.Count;
				//	SetText(text.Concat(clip).ToList());
				//} else {
				//	point += clip.Count;
				//	SetText(text.GetRange(0, oldCursorPos).Concat(clip).Concat(text.GetRange(oldCursorPos, text.Count - oldCursorPos)));
				//}
				//Adjust ();

				break;

			case Key.CursorLeft | Key.CtrlMask:
			case (Key)((int)'b' + Key.AltMask):
				ClearAllSelection ();
				int bw = WordBackward (point);
				if (bw != -1)
					point = bw;
				Adjust ();
				break;

			case Key.CursorRight | Key.CtrlMask:
			case (Key)((int)'f' + Key.AltMask):
				ClearAllSelection ();
				int fw = WordForward (point);
				if (fw != -1)
					point = fw;
				Adjust ();
				break;

			case Key.InsertChar:
				Used = !Used;
				SetNeedsDisplay ();
				break;

			case Key.ControlC:
				Copy ();
				break;

			case Key.ControlX:
				if (ReadOnly)
					return true;

				Cut ();
				break;

			case Key.ControlV:
				Paste ();
				break;

			// MISSING:
			// Alt-D, Alt-backspace
			// Alt-Y
			// Delete adding to kill buffer

			default:
				// Ignore other control characters.
				if (kb.Key < Key.Space || kb.Key > Key.CharMask)
					return false;

				if (ReadOnly)
					return true;

				if (SelectedLength != 0) {
					DeleteSelectedText ();
					oldCursorPos = point;
				}
				var kbstr = TextModel.ToRunes (ustring.Make ((uint)kb.Key));
				if (used) {
					point++;
					if (point == text.Count + 1) {
						SetText (text.Concat (kbstr).ToList ());
					} else {
						SetText (text.GetRange (0, oldCursorPos).Concat (kbstr).Concat (text.GetRange (oldCursorPos, Math.Min (text.Count - oldCursorPos, text.Count))));
					}
				} else {
					SetText (text.GetRange (0, oldCursorPos).Concat (kbstr).Concat (text.GetRange (Math.Min (oldCursorPos + 1, text.Count), Math.Max (text.Count - oldCursorPos - 1, 0))));
					point++;
				}
				Adjust ();
				return true;
			}
			return true;
		}

		int WordForward (int p)
		{
			if (p >= text.Count)
				return -1;

			int i = p;
			if (Rune.IsPunctuation (text [p]) || Rune.IsWhiteSpace (text [p])) {
				for (; i < text.Count; i++) {
					var r = text [i];
					if (Rune.IsLetterOrDigit (r))
						break;
				}
				for (; i < text.Count; i++) {
					var r = text [i];
					if (!Rune.IsLetterOrDigit (r))
						break;
				}
			} else {
				for (; i < text.Count; i++) {
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

		/// <summary>
		/// Start position of the selected text.
		/// </summary>
		public int SelectedStart { get; set; } = -1;

		/// <summary>
		/// Length of the selected text.
		/// </summary>
		public int SelectedLength { get; set; } = 0;

		/// <summary>
		/// The selected text.
		/// </summary>
		public ustring SelectedText { get; set; }

		int start, length;
		bool isButtonReleased = true;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Pressed) && !ev.Flags.HasFlag (MouseFlags.ReportMousePosition) &&
				!ev.Flags.HasFlag (MouseFlags.Button1Released) && !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				!ev.Flags.HasFlag (MouseFlags.Button1TripleClicked))
				return false;

			if (ev.Flags == MouseFlags.Button1Pressed) {
				if (!HasFocus)
					SuperView.SetFocus (this);
				PositionCursor (ev);
				if (isButtonReleased)
					ClearAllSelection ();
				isButtonReleased = true;
			} else if (ev.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
				int x = PositionCursor (ev);
				isButtonReleased = false;
				PrepareSelection (x);
				if (Application.mouseGrabView == null) {
					Application.GrabMouse (this);
				}
			} else if (ev.Flags == MouseFlags.Button1Released) {
				isButtonReleased = true;
				Application.UngrabMouse ();
			} else if (ev.Flags == MouseFlags.Button1DoubleClicked) {
				int x = PositionCursor (ev);
				int sbw = x;
				if (x > 0 && (char)Text [x - 1] != ' ')
					sbw = WordBackward (x);
				if (sbw != -1) {
					x = sbw;
					PositionCursor (x);
				}
				int sfw = WordForward (x);
				ClearAllSelection ();
				PrepareSelection (sbw, sfw - sbw);
			} else if (ev.Flags == MouseFlags.Button1TripleClicked) {
				PositionCursor (0);
				ClearAllSelection ();
				PrepareSelection (0, text.Count);
			}

			SetNeedsDisplay ();
			return true;
		}

		int PositionCursor (MouseEvent ev)
		{
			// We could also set the cursor position.
			int x;
			if (text.Count == 0)
				x = ev.X - ev.OfX;
			else
				x = ev.X;

			return PositionCursor (x);
		}

		private int PositionCursor (int x)
		{
			point = first + x;
			if (point > text.Count)
				point = text.Count;
			if (point < first)
				point = 0;
			return point;
		}

		void PrepareSelection (int x, int direction = 0)
		{
			x = x + first < 0 ? 0 : x;
			SelectedStart = SelectedStart == -1 && text.Count > 0 && x >= 0 && x <= text.Count ? x : SelectedStart;
			if (SelectedStart > -1) {
				SelectedLength = x + direction <= text.Count ? x + direction - SelectedStart : text.Count - SelectedStart;
				SetSelectedStartSelectedLength ();
				SelectedText = length > 0 ? ustring.Make (text).ToString ().Substring (
					start < 0 ? 0 : start, length > text.Count ? text.Count : length) : "";
			}
			Adjust ();
		}

		/// <summary>
		/// Clear the selected text.
		/// </summary>
		public void ClearAllSelection ()
		{
			if (SelectedStart == -1 && SelectedLength == 0)
				return;
			SelectedStart = -1;
			SelectedLength = 0;
			SelectedText = "";
			start = 0;
		}

		void SetSelectedStartSelectedLength ()
		{
			if (SelectedLength < 0) {
				start = SelectedLength + SelectedStart;
				length = Math.Abs (SelectedLength);
			} else {
				start = SelectedStart;
				length = SelectedLength;
			}
		}

		/// <summary>
		/// Copy the selected text to the clipboard.
		/// </summary>
		public virtual void Copy ()
		{
			if (Secret)
				return;

			if (SelectedLength != 0) {
				Clipboard.Contents = SelectedText;
			}
		}

		/// <summary>
		/// Cut the selected text to the clipboard.
		/// </summary>
		public virtual void Cut ()
		{
			if (SelectedLength != 0) {
				Clipboard.Contents = SelectedText;
				DeleteSelectedText ();
			}
		}

		void DeleteSelectedText ()
		{
			string actualText = Text.ToString ();
			int selStart = SelectedLength < 0 ? SelectedLength + SelectedStart : SelectedStart;
			int selLength = Math.Abs (SelectedLength);
			Text = actualText.Substring (0, selStart) +
				actualText.Substring (selStart + selLength, actualText.Length - selStart - selLength);
			ClearAllSelection ();
			CursorPosition = selStart >= Text.Length ? Text.Length : selStart;
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Paste the selected text from the clipboard.
		/// </summary>
		public virtual void Paste ()
		{
			if (ReadOnly)
				return;

			string actualText = Text.ToString ();
			int start = SelectedStart == -1 ? CursorPosition : SelectedStart;
			ustring cbTxt = Clipboard.Contents?.ToString () ?? "";
			Text = actualText.Substring (0, start) +
				cbTxt +
				actualText.Substring (start + SelectedLength, actualText.Length - start - SelectedLength);
			point = start + cbTxt.Length;
			SelectedLength = 0;
			ClearAllSelection ();
			SetNeedsDisplay ();
		}

	}
}
