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
		///   Changed event, raised when the text has clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the text in the entry changes.
		/// </remarks>
		public event EventHandler Changed;

		/// <summary>
		///    Public constructor that creates a text field, with layout controlled with X, Y, Width and Height.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (string text) : this (ustring.Make (text))
		{
			
		}

		/// <summary>
		///    Public constructor that creates a text field, with layout controlled with X, Y, Width and Height.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (ustring text)
		{
			if (text == null)
				text = "";

			this.text = TextModel.ToRunes (text);
			point = text.Length;
			CanFocus = true;
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
			if (text == null)
				text = "";

			this.text = TextModel.ToRunes (text);
			point = text.Length;
			first = point > w ? point - w : 0;
			CanFocus = true;
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
				text = TextModel.ToRunes (value);
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
		///    The current cursor position.
		/// </summary>
		public int CursorPosition { get { return point; } }

		/// <summary>
		///   Sets the cursor position.
		/// </summary>
		public override void PositionCursor ()
		{
			var col = 0;
			for (int idx = first; idx < text.Count; idx++) {
				if (idx == point)
					break;
				var cols = Rune.ColumnWidth (text [idx]);
				col += cols;
			}
			Move (col, 0);
		}

		public override void Redraw (Rect region)
		{
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
				if (col + cols < width)
					Driver.AddRune ((Rune)(Secret ? '*' : rune));
				col += cols;
			}

			for (int i = col; i < Frame.Width; i++) 
				Driver.AddRune (' ');
			
			PositionCursor ();
		}

		// Returns the size of the string starting at position start
		int DisplaySize (List<Rune> t, int start)
		{
			int size = 0;
			int tcount = text.Count;
			for (int i = start; i < tcount; i++) {
				var rune = text [i];
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
			text = newText;
			if (Changed != null)
				Changed (this, EventArgs.Empty);
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
			switch (kb.Key) {
			case Key.DeleteChar:
			case Key.ControlD:
				if (text.Count == 0 || text.Count== point)
					return true;

				SetText (text.GetRange (0, point).Concat (text.GetRange (point + 1, text.Count - (point + 1))));
				Adjust ();
				break;

			case Key.Delete:
			case Key.Backspace:
				if (point == 0)
					return true;

				SetText (text.GetRange (0, point - 1).Concat (text.GetRange (point, text.Count - (point))));
				point--;
				Adjust ();
				break;

			// Home, C-A
			case Key.Home:
			case Key.ControlA:
				point = 0;
				Adjust ();
				break;

			case Key.CursorLeft:
			case Key.ControlB:
				if (point > 0) {
					point--;
					Adjust ();
				}
				break;

			case Key.End:
			case Key.ControlE: // End
				point = text.Count;
				Adjust ();
				break;

			case Key.CursorRight:
			case Key.ControlF:
				if (point == text.Count)
					break;
				point++;
				Adjust ();
				break;

			case Key.ControlK: // kill-to-end
				if (point >= text.Count)
					return true;
				SetClipboard (text.GetRange (point, text.Count - point));
				SetText (text.GetRange (0, point));
				Adjust ();
				break;

			case Key.ControlY: // Control-y, yank
				var clip = TextModel.ToRunes (Clipboard.Contents);
				if (clip== null)
					return true;

				if (point == text.Count) {
					SetText (text.Concat (clip).ToList ());
					point = text.Count;
				} else {
					SetText (text.GetRange (0, point).Concat (clip).Concat (text.GetRange (point, text.Count - point)));
					point += clip.Count;
				}
				Adjust ();
				break;

			case (Key)((int)'b' + Key.AltMask):
				int bw = WordBackward (point);
				if (bw != -1)
					point = bw;
				Adjust ();
				break;

			case (Key)((int)'f' + Key.AltMask):
				int fw = WordForward (point);
				if (fw != -1)
					point = fw;
				Adjust ();
				break;

				// MISSING:
				// Alt-D, Alt-backspace
				// Alt-Y
				// Delete adding to kill buffer

			default:
				// Ignore other control characters.
				if (kb.Key < Key.Space || kb.Key > Key.CharMask)
					return false;

				var kbstr = TextModel.ToRunes (ustring.Make ((uint)kb.Key));
				if (used) {
					if (point == text.Count) {
						SetText (text.Concat (kbstr).ToList ());
					} else {
						SetText (text.GetRange (0, point).Concat (kbstr).Concat (text.GetRange (point, text.Count - point)));
					}
					point++;
				} else {
					SetText (kbstr);
					first = 0;
					point = 1;
				}
				used = true;
				Adjust ();
				return true;
			}
			used = true;
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

        	public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Clicked))
				return false;

			if (!HasFocus) 
				SuperView.SetFocus (this);
			
			// We could also set the cursor position.
			point = first + ev.X;
			if (point > text.Count)
				point = text.Count;
			if (point < first)
				point = 0;
	
			SetNeedsDisplay ();
			return true;
		}
	}


}
