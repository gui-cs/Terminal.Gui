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
		int selectedStart = -1; // -1 represents there is no text selection.
		ustring selectedText;

		/// <summary>
		/// Tracks whether the text field should be considered "used", that is, that the user has moved in the entry, so new input should be appended at the cursor position, rather than clearing the entry
		/// </summary>
		public bool Used { get; set; }

		/// <summary>
		/// If set to true its not allow any changes in the text.
		/// </summary>
		public bool ReadOnly { get; set; } = false;

		/// <summary>
		/// Changing event, raised before the <see cref="Text"/> changes and can be canceled or changing the new text.
		/// </summary>
		public event Action<TextChangingEventArgs> TextChanging;

		/// <summary>
		///   Changed event, raised when the text has changed.
		/// </summary>
		/// <remarks>
		///   This event is raised when the <see cref="Text"/> changes. 
		/// </remarks>
		/// <remarks>
		///   The passed <see cref="EventArgs"/> is a <see cref="ustring"/> containing the old value. 
		/// </remarks>
		public event Action<ustring> TextChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (string text) : this (ustring.Make (text)) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public TextField () : this (string.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (ustring text) : base (text)
		{
			Initialize (text, text.RuneCount + 1);
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
			Height = 1;

			if (text == null)
				text = "";

			this.text = TextModel.ToRunes (text.Split ("\n") [0]);
			point = text.RuneCount;
			first = point > w + 1 ? point - w + 1 : 0;
			CanFocus = true;
			Used = true;
			WantMousePositionReports = true;
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			if (Application.mouseGrabView != null && Application.mouseGrabView == this)
				Application.UngrabMouse ();
			//if (SelectedLength != 0 && !(Application.mouseGrabView is MenuBar))
			//	ClearAllSelection ();

			return base.OnLeave (view);
		}

		///<inheritdoc/>
		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;
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
		public new ustring Text {
			get {
				return ustring.Make (text);
			}

			set {
				var oldText = ustring.Make (text);

				if (oldText == value)
					return;

				var newText = OnTextChanging (value.Replace ("\t", "").Split ("\n") [0]);
				if (newText.Cancel) {
					if (point > text.Count) {
						point = text.Count;
					}
					return;
				}
				text = TextModel.ToRunes (newText.NewText);
				if (!Secret && !isFromHistory) {
					if (historyText == null)
						historyText = new List<ustring> () { oldText };
					if (idxhistoryText > 0 && idxhistoryText + 1 < historyText.Count)
						historyText.RemoveRange (idxhistoryText + 1, historyText.Count - idxhistoryText - 1);
					historyText.Add (ustring.Make (text));
					idxhistoryText++;
				}
				TextChanged?.Invoke (oldText);

				if (point > text.Count) {
					point = Math.Max (TextModel.DisplaySize (text, 0).size - 1, 0);
				}

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
		public virtual int CursorPosition {
			get { return point; }
			set {
				if (value < 0) {
					point = 0;
				} else if (value > text.Count) {
					point = text.Count;
				} else {
					point = value;
				}
				PrepareSelection (selectedStart, point - selectedStart);
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
				TextModel.SetCol (ref col, Frame.Width - 1, cols);
			}
			Move (col, 0);
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			var selColor = new Attribute (ColorScheme.Focus.Background, ColorScheme.Focus.Foreground);
			SetSelectedStartSelectedLength ();

			Driver.SetAttribute (ColorScheme.Focus);
			Move (0, 0);

			int p = first;
			int col = 0;
			int width = Frame.Width + OffSetBackground ();
			var tcount = text.Count;
			var roc = GetReadOnlyColor ();
			for (int idx = p; idx < tcount; idx++) {
				var rune = text [idx];
				var cols = Rune.ColumnWidth (rune);
				if (idx == point && HasFocus && !Used && length == 0 && !ReadOnly) {
					Driver.SetAttribute (selColor);
				} else if (ReadOnly) {
					Driver.SetAttribute (idx >= start && length > 0 && idx < start + length ? selColor : roc);
				} else if (!HasFocus && Enabled) {
					Driver.SetAttribute (ColorScheme.Focus);
				} else if (!Enabled) {
					Driver.SetAttribute (roc);
				} else {
					Driver.SetAttribute (idx >= start && length > 0 && idx < start + length ? selColor : ColorScheme.Focus);
				}
				if (col + cols <= width) {
					Driver.AddRune ((Rune)(Secret ? '*' : rune));
				}
				if (!TextModel.SetCol (ref col, width, cols)) {
					break;
				}
				if (idx + 1 < tcount && col + Rune.ColumnWidth (text [idx + 1]) > width) {
					break;
				}
			}

			Driver.SetAttribute (ColorScheme.Focus);
			for (int i = col; i < width; i++) {
				Driver.AddRune (' ');
			}

			PositionCursor ();
		}

		Attribute GetReadOnlyColor ()
		{
			if (ColorScheme.Disabled.Foreground == ColorScheme.Focus.Background) {
				return new Attribute (ColorScheme.Focus.Foreground, ColorScheme.Focus.Background);
			}
			return new Attribute (ColorScheme.Disabled.Foreground, ColorScheme.Focus.Background);
		}

		void Adjust ()
		{
			int offB = OffSetBackground ();
			if (point < first) {
				first = point;
			} else if (first + point - (Frame.Width + offB) == 0 ||
				  TextModel.DisplaySize (text, first, point).size >= Frame.Width + offB) {
				first = Math.Max (TextModel.CalculateLeftColumn (text, first,
					point, Frame.Width + offB), 0);
			}
			SetNeedsDisplay ();
		}

		int OffSetBackground ()
		{
			int offB = 0;
			if (SuperView?.Frame.Right - Frame.Right < 0) {
				offB = SuperView.Frame.Right - Frame.Right - 1;
			}

			return offB;
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
			get => base.CanFocus;
			set { base.CanFocus = value; }
		}

		void SetClipboard (IEnumerable<Rune> text)
		{
			if (!Secret)
				Clipboard.Contents = ustring.Make (text.ToList ());
		}

		int oldCursorPos;

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
			oldCursorPos = point;

			switch (ShortcutHelper.GetModifiersKey (kb)) {
			case Key.DeleteChar:
			case Key.D | Key.CtrlMask: // Delete
				DeleteCharRight ();
				break;

			case Key.Delete:
			case Key.Backspace:
				DeleteCharLeft ();
				break;

			case Key.Home | Key.ShiftMask:
			case Key.Home | Key.ShiftMask | Key.CtrlMask:
			case Key.A | Key.ShiftMask | Key.CtrlMask:
				MoveHomeExtend ();
				break;

			case Key.End | Key.ShiftMask:
			case Key.End | Key.ShiftMask | Key.CtrlMask:
			case Key.E | Key.ShiftMask | Key.CtrlMask:
				MoveEndExtend ();
				break;

			// Home, C-A
			case Key.Home:
			case Key.Home | Key.CtrlMask:
			case Key.A | Key.CtrlMask:
				MoveHome ();
				break;

			case Key.CursorLeft | Key.ShiftMask:
			case Key.CursorUp | Key.ShiftMask:
				MoveLeftExtend ();
				break;

			case Key.CursorRight | Key.ShiftMask:
			case Key.CursorDown | Key.ShiftMask:
				MoveRightExtend ();
				break;

			case Key.CursorLeft | Key.ShiftMask | Key.CtrlMask:
			case Key.CursorUp | Key.ShiftMask | Key.CtrlMask:
			case (Key)((int)'B' + Key.ShiftMask | Key.AltMask):
				MoveWordLeftExtend ();
				break;

			case Key.CursorRight | Key.ShiftMask | Key.CtrlMask:
			case Key.CursorDown | Key.ShiftMask | Key.CtrlMask:
			case (Key)((int)'F' + Key.ShiftMask | Key.AltMask):
				MoveWordRightExtend ();
				break;

			case Key.CursorLeft:
			case Key.B | Key.CtrlMask:
				MoveLeft ();
				break;

			case Key.End:
			case Key.End | Key.CtrlMask:
			case Key.E | Key.CtrlMask: // End
				MoveEnd ();
				break;

			case Key.CursorRight:
			case Key.F | Key.CtrlMask:
				MoveRight ();
				break;

			case Key.K | Key.CtrlMask: // kill-to-end
				KillToEnd ();
				break;

			case Key.K | Key.AltMask: // kill-to-start
				KillToStart ();
				break;

			// Undo
			case Key.Z | Key.CtrlMask:
			case Key.Backspace | Key.AltMask:
				UndoChanges ();
				break;

			//Redo
			case Key.Y | Key.CtrlMask: // Control-y, yank
				RedoChanges ();
				break;

			case Key.CursorLeft | Key.CtrlMask:
			case Key.CursorUp | Key.CtrlMask:
			case (Key)((int)'B' + Key.AltMask):
				MoveWordLeft ();
				break;

			case Key.CursorRight | Key.CtrlMask:
			case Key.CursorDown | Key.CtrlMask:
			case (Key)((int)'F' + Key.AltMask):
				MoveWordRight ();
				break;

			case Key.DeleteChar | Key.CtrlMask: // kill-word-forwards
				KillWordForwards ();
				break;

			case Key.Backspace | Key.CtrlMask: // kill-word-backwards
				KillWordBackwards ();
				break;

			case Key.InsertChar:
				InsertChar ();
				break;

			case Key.C | Key.CtrlMask:
				Copy ();
				break;

			case Key.X | Key.CtrlMask:
				Cut ();
				break;

			case Key.V | Key.CtrlMask:
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

				if (length > 0) {
					DeleteSelectedText ();
					oldCursorPos = point;
				}
				var kbstr = TextModel.ToRunes (ustring.Make ((uint)kb.Key));
				if (Used) {
					point++;
					if (point == text.Count + 1) {
						SetText (text.Concat (kbstr).ToList ());
					} else {
						if (oldCursorPos > text.Count) {
							oldCursorPos = text.Count;
						}
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

		void InsertChar ()
		{
			Used = !Used;
			SetNeedsDisplay ();
		}

		void KillWordBackwards ()
		{
			ClearAllSelection ();
			int bw = WordBackward (point);
			if (bw != -1) {
				SetText (text.GetRange (0, bw).Concat (text.GetRange (point, text.Count - point)));
				point = bw;
			}
			Adjust ();
		}

		void KillWordForwards ()
		{
			ClearAllSelection ();
			int fw = WordForward (point);
			if (fw != -1) {
				SetText (text.GetRange (0, point).Concat (text.GetRange (fw, text.Count - fw)));
			}
			Adjust ();
		}

		void MoveWordRight ()
		{
			ClearAllSelection ();
			int fw = WordForward (point);
			if (fw != -1)
				point = fw;
			Adjust ();
		}

		void MoveWordLeft ()
		{
			ClearAllSelection ();
			int bw = WordBackward (point);
			if (bw != -1)
				point = bw;
			Adjust ();
		}

		void RedoChanges ()
		{
			if (ReadOnly)
				return;

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
		}

		void UndoChanges ()
		{
			if (ReadOnly)
				return;

			if (historyText != null && historyText.Count > 0) {
				isFromHistory = true;
				if (idxhistoryText > 0)
					idxhistoryText--;
				if (idxhistoryText > -1)
					Text = historyText [idxhistoryText];
				point = text.Count;
				isFromHistory = false;
			}
		}

		void KillToStart ()
		{
			if (ReadOnly)
				return;

			ClearAllSelection ();
			if (point == 0)
				return;
			SetClipboard (text.GetRange (0, point));
			SetText (text.GetRange (point, text.Count - point));
			point = 0;
			Adjust ();
		}

		void KillToEnd ()
		{
			if (ReadOnly)
				return;

			ClearAllSelection ();
			if (point >= text.Count)
				return;
			SetClipboard (text.GetRange (point, text.Count - point));
			SetText (text.GetRange (0, point));
			Adjust ();
		}

		void MoveRight ()
		{
			ClearAllSelection ();
			if (point == text.Count)
				return;
			point++;
			Adjust ();
		}

		void MoveEnd ()
		{
			ClearAllSelection ();
			point = text.Count;
			Adjust ();
		}

		void MoveLeft ()
		{
			ClearAllSelection ();
			if (point > 0) {
				point--;
				Adjust ();
			}
		}

		void MoveWordRightExtend ()
		{
			if (point < text.Count) {
				int x = start > -1 && start > point ? start : point;
				int sfw = WordForward (x);
				if (sfw != -1)
					point = sfw;
				PrepareSelection (x, sfw - x);
			}
		}

		void MoveWordLeftExtend ()
		{
			if (point > 0) {
				int x = Math.Min (start > -1 && start > point ? start : point, text.Count);
				if (x > 0) {
					int sbw = WordBackward (x);
					if (sbw != -1)
						point = sbw;
					PrepareSelection (x, sbw - x);
				}
			}
		}

		void MoveRightExtend ()
		{
			if (point < text.Count) {
				PrepareSelection (point++, 1);
			}
		}

		void MoveLeftExtend ()
		{
			if (point > 0) {
				PrepareSelection (point--, -1);
			}
		}

		void MoveHome ()
		{
			ClearAllSelection ();
			point = 0;
			Adjust ();
		}

		void MoveEndExtend ()
		{
			if (point <= text.Count) {
				int x = point;
				point = text.Count;
				PrepareSelection (x, point - x);
			}
		}

		void MoveHomeExtend ()
		{
			if (point > 0) {
				int x = point;
				point = 0;
				PrepareSelection (x, point - x);
			}
		}

		void DeleteCharLeft ()
		{
			if (ReadOnly)
				return;

			if (length == 0) {
				if (point == 0)
					return;

				point--;
				if (oldCursorPos < text.Count) {
					SetText (text.GetRange (0, oldCursorPos - 1).Concat (text.GetRange (oldCursorPos, text.Count - oldCursorPos)));
				} else {
					SetText (text.GetRange (0, oldCursorPos - 1));
				}
				Adjust ();
			} else {
				DeleteSelectedText ();
			}
		}

		void DeleteCharRight ()
		{
			if (ReadOnly)
				return;

			if (length == 0) {
				if (text.Count == 0 || text.Count == point)
					return;

				SetText (text.GetRange (0, point).Concat (text.GetRange (point + 1, text.Count - (point + 1))));
				Adjust ();
			} else {
				DeleteSelectedText ();
			}
		}

		int WordForward (int p)
		{
			if (p >= text.Count)
				return -1;

			int i = p + 1;
			if (i == text.Count)
				return text.Count;

			var ti = text [i];
			if (Rune.IsLetterOrDigit (ti) && Rune.IsWhiteSpace (text [p]))
				return i;

			if (Rune.IsPunctuation (ti) || Rune.IsSymbol (ti) || Rune.IsWhiteSpace (ti)) {
				for (; i < text.Count; i++) {
					if (Rune.IsLetterOrDigit (text [i]))
						return i;
				}
			} else {
				for (; i < text.Count; i++) {
					if (!Rune.IsLetterOrDigit (text [i]))
						break;
				}
				for (; i < text.Count; i++) {
					if (Rune.IsLetterOrDigit (text [i]) ||
						(Rune.IsPunctuation (text [i]) && Rune.IsWhiteSpace (text [i - 1])))
						break;
				}
			}

			if (i != p)
				return Math.Min (i, text.Count);

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
			var lastValidCol = -1;
			if (Rune.IsPunctuation (ti) || Rune.IsSymbol (ti) || Rune.IsWhiteSpace (ti)) {
				for (; i >= 0; i--) {
					if (Rune.IsLetterOrDigit (text [i])) {
						lastValidCol = i;
						break;
					}
					if (i - 1 > 0 && !Rune.IsWhiteSpace (text [i]) && Rune.IsWhiteSpace (text [i - 1])) {
						return i;
					}
				}
				for (; i >= 0; i--) {
					if (!Rune.IsLetterOrDigit (text [i]))
						break;
					lastValidCol = i;
				}
				if (lastValidCol > -1) {
					return lastValidCol;
				}
			} else {
				for (; i >= 0; i--) {
					if (!Rune.IsLetterOrDigit (text [i]))
						break;
					lastValidCol = i;
				}
				if (lastValidCol > -1) {
					return lastValidCol;
				}
			}

			if (i != p)
				return Math.Max (i, 0);

			return -1;
		}

		/// <summary>
		/// Start position of the selected text.
		/// </summary>
		public int SelectedStart {
			get => selectedStart;
			set {
				if (value < -1) {
					selectedStart = -1;
				} else if (value > text.Count) {
					selectedStart = text.Count;
				} else {
					selectedStart = value;
				}
				PrepareSelection (selectedStart, point - selectedStart);
			}
		}

		/// <summary>
		/// Length of the selected text.
		/// </summary>
		public int SelectedLength { get => length; }

		/// <summary>
		/// The selected text.
		/// </summary>
		public ustring SelectedText {
			get => Secret ? null : selectedText;
			private set => selectedText = value;
		}

		int start, length;
		bool isButtonPressed;
		bool isButtonReleased = true;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Pressed) && !ev.Flags.HasFlag (MouseFlags.ReportMousePosition) &&
				!ev.Flags.HasFlag (MouseFlags.Button1Released) && !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				!ev.Flags.HasFlag (MouseFlags.Button1TripleClicked)) {
				return false;
			}

			if (!CanFocus) {
				return true;
			}

			if (ev.Flags == MouseFlags.Button1Pressed) {
				EnsureHasFocus ();
				PositionCursor (ev);
				if (isButtonReleased) {
					ClearAllSelection ();
				}
				isButtonReleased = true;
				isButtonPressed = true;
			} else if (ev.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && isButtonPressed) {
				int x = PositionCursor (ev);
				isButtonReleased = false;
				PrepareSelection (x);
				if (Application.mouseGrabView == null) {
					Application.GrabMouse (this);
				}
			} else if (ev.Flags == MouseFlags.Button1Released) {
				isButtonReleased = true;
				isButtonPressed = false;
				Application.UngrabMouse ();
			} else if (ev.Flags == MouseFlags.Button1DoubleClicked) {
				EnsureHasFocus ();
				int x = PositionCursor (ev);
				int sbw = x;
				if (x == text.Count || (x > 0 && (char)Text [x - 1] != ' '
					|| (x > 0 && (char)Text [x] == ' '))) {

					sbw = WordBackward (x);
				}
				if (sbw != -1) {
					x = sbw;
					PositionCursor (x);
				}
				int sfw = WordForward (x);
				ClearAllSelection ();
				if (sfw != -1 && sbw != -1) {
					point = sfw;
				}
				PrepareSelection (sbw, sfw - sbw);
			} else if (ev.Flags == MouseFlags.Button1TripleClicked) {
				EnsureHasFocus ();
				PositionCursor (0);
				ClearAllSelection ();
				PrepareSelection (0, text.Count);
			}

			SetNeedsDisplay ();
			return true;

			void EnsureHasFocus ()
			{
				if (!HasFocus) {
					SetFocus ();
				}
			}
		}

		int PositionCursor (MouseEvent ev)
		{
			// We could also set the cursor position.
			int x;
			var pX = TextModel.GetColFromX (text, first, ev.X);
			if (text.Count == 0) {
				x = pX - ev.OfX;
			} else {
				x = pX;
			}
			return PositionCursor (x, false);
		}

		int PositionCursor (int x, bool getX = true)
		{
			int pX = x;
			if (getX) {
				pX = TextModel.GetColFromX (text, first, x);
			}
			if (first + pX > text.Count) {
				point = text.Count;
			} else if (first + pX < first) {
				point = 0;
			} else {
				point = first + pX;
			}

			return point;
		}

		void PrepareSelection (int x, int direction = 0)
		{
			x = x + first < -1 ? 0 : x;
			selectedStart = selectedStart == -1 && text.Count > 0 && x >= 0 && x <= text.Count ? x : selectedStart;
			if (selectedStart > -1) {
				length = Math.Abs (x + direction <= text.Count ? x + direction - selectedStart : text.Count - selectedStart);
				SetSelectedStartSelectedLength ();
				if (start > -1 && length > 0) {
					selectedText = length > 0 ? ustring.Make (text).ToString ().Substring (
						start < 0 ? 0 : start, length > text.Count ? text.Count : length) : "";
					if (first > start) {
						first = start;
					}
				} else if (start > -1 && length == 0) {
					selectedText = null;
				}
			} else if (length > 0 || selectedText != null) {
				ClearAllSelection ();
			}
			Adjust ();
		}

		/// <summary>
		/// Clear the selected text.
		/// </summary>
		public void ClearAllSelection ()
		{
			if (selectedStart == -1 && length == 0 && selectedText == "")
				return;

			selectedStart = -1;
			length = 0;
			selectedText = null;
			start = 0;
			length = 0;
			SetNeedsDisplay ();
		}

		void SetSelectedStartSelectedLength ()
		{
			if (SelectedStart > -1 && point < SelectedStart) {
				start = point;
			} else {
				start = SelectedStart;
			}
		}

		/// <summary>
		/// Copy the selected text to the clipboard.
		/// </summary>
		public virtual void Copy ()
		{
			if (Secret || length == 0)
				return;

			Clipboard.Contents = SelectedText;
		}

		/// <summary>
		/// Cut the selected text to the clipboard.
		/// </summary>
		public virtual void Cut ()
		{
			if (ReadOnly || Secret || length == 0)
				return;

			Clipboard.Contents = SelectedText;
			DeleteSelectedText ();
		}

		void DeleteSelectedText ()
		{
			ustring actualText = Text;
			SetSelectedStartSelectedLength ();
			int selStart = SelectedStart > -1 ? start : point;
			(var _, var len) = TextModel.DisplaySize (text, 0, selStart, false);
			(var _, var len2) = TextModel.DisplaySize (text, selStart, selStart + length, false);
			(var _, var len3) = TextModel.DisplaySize (text, selStart + length, actualText.RuneCount, false);
			Text = actualText [0, len] +
				actualText [len + len2, len + len2 + len3];
			ClearAllSelection ();
			point = selStart >= Text.RuneCount ? Text.RuneCount : selStart;
			Adjust ();
		}

		/// <summary>
		/// Paste the selected text from the clipboard.
		/// </summary>
		public virtual void Paste ()
		{
			if (ReadOnly || Clipboard.Contents == null) {
				return;
			}

			SetSelectedStartSelectedLength ();
			int selStart = start == -1 ? CursorPosition : start;
			ustring actualText = Text;
			(int _, int len) = TextModel.DisplaySize (text, 0, selStart, false);
			(var _, var len2) = TextModel.DisplaySize (text, selStart, selStart + length, false);
			(var _, var len3) = TextModel.DisplaySize (text, selStart + length, actualText.RuneCount, false);
			ustring cbTxt = Clipboard.Contents.Split ("\n") [0] ?? "";
			Text = actualText [0, len] +
				cbTxt +
				actualText [len + len2, len + len2 + len3];
			point = selStart + cbTxt.RuneCount;
			ClearAllSelection ();
			SetNeedsDisplay ();
			Adjust ();
		}

		/// <summary>
		/// Virtual method that invoke the <see cref="TextChanging"/> event if it's defined.
		/// </summary>
		/// <param name="newText">The new text to be replaced.</param>
		/// <returns>Returns the <see cref="TextChangingEventArgs"/></returns>
		public virtual TextChangingEventArgs OnTextChanging (ustring newText)
		{
			var ev = new TextChangingEventArgs (newText);
			TextChanging?.Invoke (ev);
			return ev;
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
			Application.Driver.SetCursorVisibility (DesiredCursorVisibility);

			return base.OnEnter (view);
		}
	}

	/// <summary>
	/// An <see cref="EventArgs"/> which allows passing a cancelable new text value event.
	/// </summary>
	public class TextChangingEventArgs : EventArgs {
		/// <summary>
		/// The new text to be replaced.
		/// </summary>
		public ustring NewText { get; set; }
		/// <summary>
		/// Flag which allows to cancel the new text value.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Initializes a new instance of <see cref="TextChangingEventArgs"/>
		/// </summary>
		/// <param name="newText">The new <see cref="TextField.Text"/> to be replaced.</param>
		public TextChangingEventArgs (ustring newText)
		{
			NewText = newText;
		}
	}
}
