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
		ustring text;
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
		public TextField (ustring text)
		{
			if (text == null)
				text = "";

			this.text = text;
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

			this.text = text;
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
				return text;
			}

			set {
				text = value;
				if (point > text.Length)
					point = text.Length;
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
			Move (point - first, 0);
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (ColorScheme.Focus);
			Move (0, 0);

			for (int i = 0; i < Frame.Width; i++) {
				int p = first + i;

				if (p < text.Length) {
					Driver.AddRune (Secret ? (Rune)'*' : text [p]);
				} else
					Driver.AddRune (' ');
			}
			PositionCursor ();
		}

		void Adjust ()
		{
			if (point < first)
				first = point;
			else if (first + point >= Frame.Width)
				first = point - (Frame.Width / 3);
			SetNeedsDisplay ();
		}

		void SetText (ustring new_text)
		{
			text = new_text;
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}

		public override bool CanFocus {
			get => true;
			set { base.CanFocus = value; }
		}

		void SetClipboard (ustring text)
		{
			if (!Secret)
				Clipboard.Contents = text;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.DeleteChar:
				if (text.Length == 0 || text.Length == point)
					return true;

				SetText (text [0, point] + text [point + 1, null]);
				Adjust ();
				break;

			case Key.Delete:
			case Key.Backspace:
				if (point == 0)
					return true;

				SetText (text [0, point - 1] + text [point, null]);
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

			case Key.ControlD: // Delete
				if (point == text.Length)
					break;
				SetText (text [0, point] + text [point + 1, null]);
				Adjust ();
				break;

			case Key.End:
			case Key.ControlE: // End
				point = text.Length;
				Adjust ();
				break;

			case Key.CursorRight:
			case Key.ControlF:
				if (point == text.Length)
					break;
				point++;
				Adjust ();
				break;

			case Key.ControlK: // kill-to-end
				SetClipboard (text.Substring (point));
				SetText (text [0, point]);
				Adjust ();
				break;

			case Key.ControlY: // Control-y, yank
				var clip = Clipboard.Contents;
				if (clip== null)
					return true;

				if (point == text.Length) {
					SetText (text + clip);
					point = text.Length;
				} else {
					SetText (text [0, point] + clip + text.Substring (point));
					point += clip.RuneCount;
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

				var kbstr = ustring.Make ((uint)kb.Key);
				if (used) {
					if (point == text.Length) {
						SetText (text + kbstr);
					} else {
						SetText (text [0, point] + kbstr + text [point, null]);
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
			if (p >= text.Length)
				return -1;

			int i = p;
			if (Rune.IsPunctuation (text [p]) || Rune.IsWhiteSpace(text [p])) {
				for (; i < text.Length; i++) {
					var r = text [i];
					if (Rune.IsLetterOrDigit(r))
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
			if (point > text.Length)
				point = text.Length;
			if (point < first)
				point = 0;
	
			SetNeedsDisplay ();
			return true;
		}
	}


}
