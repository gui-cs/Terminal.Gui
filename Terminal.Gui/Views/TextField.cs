﻿//
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
	///   Text data entry widget
	/// </summary>
	/// <remarks>
	///   The Entry widget provides Emacs-like editing
	///   functionality,  and mouse support.
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
		///   Changed event, raised when the text has clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the text in the entry changes.
		/// </remarks>
		public event EventHandler<ustring> Changed;

		/// <summary>
		///    Public constructor that creates a text field, with layout controlled with X, Y, Width and Height.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (string text) : this (ustring.Make (text))
		{
			Height = 1;
		}

		/// <summary>
		///    Public constructor that creates a text field, with layout controlled with X, Y, Width and Height.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (ustring text)
		{
			Initialize (text, Frame.Width);
		}

		/// <summary>
		///    Public constructor that creates a text field at an absolute position and size.
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
			OnLeave += TextField_OnLeave;
		}

		void TextField_OnLeave (object sender, EventArgs e)
		{
			if (Application.mouseGrabView != null && Application.mouseGrabView == this)
				Application.UngrabMouse ();
		}

		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;
				var w = base.Frame.Width;
				first = point > w ? point - w : 0;
			}
		}

		/// <summary>
		///   Sets or gets the text in the entry.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public ustring Text {
			get {
				return ustring.Make (text);
			}

			set {
				ustring oldText = ustring.Make (text);
				text = TextModel.ToRunes (value);
				Changed?.Invoke (this, oldText);

				if (point > text.Count)
					point = Math.Max (text.Count-1, 0);

				// FIXME: this needs to be updated to use Rune.ColumnWidth
				first = point > Frame.Width ? point - Frame.Width : 0;
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

		public override void Redraw (Rect region)
		{
			ColorScheme color = Colors.Menu;
			SetSelectedStartSelectedLength ();

			Driver.SetAttribute (ColorScheme.Focus);
			Move (0, 0);

			int p = first;
			int col = 0;
			int width = Frame.Width;
			var tcount = text.Count;
			for (int idx = 0; idx < tcount; idx++){
				var rune = text [idx];
				if (idx < first)
					continue;
				var cols = Rune.ColumnWidth (rune);
				if (col == point && HasFocus && !Used && SelectedLength == 0)
					Driver.SetAttribute (Colors.Menu.HotFocus);
				else
					Driver.SetAttribute (idx >= start && length > 0 && idx < start + length ? color.Focus : ColorScheme.Focus);
				if (col + cols < width)
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
			if (point < first)
				first = point;
			else if (first + point >= Frame.Width) {
				first = point - (Frame.Width - 1);
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

		public override bool CanFocus {
			get => true;
			set { base.CanFocus = value; }
		}

		void SetClipboard (IEnumerable<Rune> text)
		{
			if (!Secret)
				Clipboard.Contents = ustring.Make (text.ToList ());
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			// remember current cursor position
			// because the new calculated cursor position is needed to be set BEFORE the change event is triggest
			// Needed for the Elmish Wrapper issue https://github.com/DieselMeister/Terminal.Gui.Elmish/issues/2
			var oldCursorPos = point;

			switch (kb.Key) {
			case Key.DeleteChar:
			case Key.ControlD:
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

			// Home, C-A
			case Key.Home:
			case Key.ControlA:
				ClearAllSelection ();
				point = 0;
				Adjust ();
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
				ClearAllSelection ();
				if (point >= text.Count)
					return true;
				SetClipboard (text.GetRange (point, text.Count - point));
				SetText (text.GetRange (0, point));
				Adjust ();
				break;

			case Key.ControlY: // Control-y, yank
				if (Clipboard.Contents == null)
					return true;
				var clip = TextModel.ToRunes (Clipboard.Contents);
				if (clip == null)
					return true;

				if (point == text.Count) {
					point = text.Count;
					SetText(text.Concat(clip).ToList());
				} else {
					point += clip.Count;
					SetText(text.GetRange(0, oldCursorPos).Concat(clip).Concat(text.GetRange(oldCursorPos, text.Count - oldCursorPos)));
				}
				Adjust ();
				break;

			case (Key)((int)'b' + Key.AltMask):
				ClearAllSelection ();
				int bw = WordBackward (point);
				if (bw != -1)
					point = bw;
				Adjust ();
				break;

			case (Key)((int)'f' + Key.AltMask):
				ClearAllSelection ();
				int fw = WordForward (point);
				if (fw != -1)
					point = fw;
				Adjust ();
				break;

			case Key.AltMask | Key.ControlI:
				Used = !Used;
				SetNeedsDisplay ();
				break;

			case Key.AltMask | Key.ControlC:
				Copy ();
				break;

			case Key.AltMask | Key.ControlX:
				Cut ();
				break;

			case Key.AltMask | Key.ControlV:
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

				if (SelectedLength != 0) {
					DeleteSelectedText ();
					oldCursorPos = point;
				}
				var kbstr = TextModel.ToRunes (ustring.Make ((uint)kb.Key));
				if (used) {
					point++;
					if (point == text.Count) {
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
			if (Rune.IsPunctuation (text [p]) || Rune.IsWhiteSpace(text [p])) {
				for (; i < text.Count; i++) {
					var r = text [i];
					if (Rune.IsLetterOrDigit(r))
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
			if (Rune.IsPunctuation (ti) || Rune.IsSymbol(ti) || Rune.IsWhiteSpace(ti)) {
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

		public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Clicked) && !ev.Flags.HasFlag (MouseFlags.Button1Pressed) &&
				!ev.Flags.HasFlag (MouseFlags.ReportMousePosition))
				return false;

			if (ev.Flags == MouseFlags.Button1Clicked) {
				if (!HasFocus)
					SuperView.SetFocus (this);
				int x = PositionCursor (ev);
				if (isButtonReleased)
					ClearAllSelection ();
				isButtonReleased = true;
				Application.UngrabMouse ();
			} else if (ev.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
				int x = PositionCursor (ev);
				isButtonReleased = false;
				PrepareSelection (x);
				if (Application.mouseGrabView == null) {
					Application.GrabMouse (this);
				}
			} else if (ev.Flags == MouseFlags.Button1Pressed) {
				int x = PositionCursor (ev);
				if (SelectedLength != 0)
					ClearAllSelection ();
			}

			SetNeedsDisplay ();
			return true;
		}

		int PositionCursor (MouseEvent ev)
		{
			// We could also set the cursor position.
			int x;
			if (Application.mouseGrabView == null) {
				x = ev.X;
			} else {
				x = ev.X;// - (text.Count > Frame.Width ? text.Count - Frame.Width : 0);
			}

			point = first + x;
			if (point > text.Count)
				point = text.Count;
			if (point < first)
				point = 0;
			return x;
		}

		void PrepareSelection (int x)
		{
			x = x + first < 0 ? 0 : x + first;
			SelectedStart = SelectedStart == -1 && text.Count > 0 && x >= 0 && x <= text.Count ? x : SelectedStart;
			if (SelectedStart > -1) {
				SelectedLength = x <= text.Count ? x - SelectedStart : text.Count - SelectedStart;
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
			if (SelectedLength == 0)
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
		public void Copy ()
		{
			if (SelectedLength != 0) {
				Clipboard.Contents = SelectedText;
			}
		}

		/// <summary>
		/// Cut the selected text to the clipboard.
		/// </summary>
		public void Cut ()
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
		public void Paste ()
		{
			string actualText = Text.ToString ();
			int start = SelectedStart == -1 ? CursorPosition : SelectedStart;
			Text = actualText.Substring (0, start) +
				Clipboard.Contents?.ToString () +
				actualText.Substring (start + SelectedLength, actualText.Length - start - SelectedLength);
			SelectedLength = 0;
			SetNeedsDisplay ();
		}

	}
}
