using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal {
	/// <summary>
	///   Text data entry widget
	/// </summary>
	/// <remarks>
	///   The Entry widget provides Emacs-like editing
	///   functionality,  and mouse support.
	/// </remarks>
	public class TextField : View {
		string text, kill;
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
		///   Public constructor.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public TextField (int x, int y, int w, string s) : base (new Rect (x, y, w, 1))
		{
			if (s == null)
				s = "";

			text = s;
			point = s.Length;
			first = point > w ? point - w : 0;
			CanFocus = true;
			Color = Colors.Dialog.Focus;
		}

		/// <summary>
		///   Sets or gets the text in the entry.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public string Text {
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

		Attribute color;
		/// <summary>
		/// Sets the color attribute to use (includes foreground and background).
		/// </summary>
		/// <value>The color.</value>
		public Attribute Color {
			get => color;
			set {
				color = value;
				SetNeedsDisplay ();
			}
		}

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
			Driver.SetAttribute (Color);
			Move (0, 0);

			for (int i = 0; i < Frame.Width; i++) {
				int p = first + i;

				if (p < text.Length) {
					Driver.AddCh (Secret ? '*' : text [p]);
				} else
					Driver.AddCh (' ');
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

		void SetText (string new_text)
		{
			text = new_text;
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}

		public override bool CanFocus {
			get => true;
			set { base.CanFocus = value; }
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.Delete:
			case Key.Backspace:
				if (point == 0)
					return true;

				SetText (text.Substring (0, point - 1) + text.Substring (point));
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
				SetText (text.Substring (0, point) + text.Substring (point + 1));
				Adjust ();
				break;

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
				kill = text.Substring (point);
				SetText (text.Substring (0, point));
				Adjust ();
				break;

			case Key.ControlY: // Control-y, yank
				if (kill == null)
					return true;

				if (point == text.Length) {
					SetText (text + kill);
					point = text.Length;
				} else {
					SetText (text.Substring (0, point) + kill + text.Substring (point));
					point += kill.Length;
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

			default:
				// Ignore other control characters.
				if (kb.Key < Key.Space || kb.Key > Key.CharMask)
					return false;

				if (used) {
					if (point == text.Length) {
						SetText (text + (char)kb.Key);
					} else {
						SetText (text.Substring (0, point) + (char)kb.Key + text.Substring (point));
					}
					point++;
				} else {
					SetText ("" + (char)kb.Key);
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
			if (Char.IsPunctuation (text [p]) || Char.IsWhiteSpace (text [p])) {
				for (; i < text.Length; i++) {
					if (Char.IsLetterOrDigit (text [i]))
						break;
				}
				for (; i < text.Length; i++) {
					if (!Char.IsLetterOrDigit (text [i]))
						break;
				}
			} else {
				for (; i < text.Length; i++) {
					if (!Char.IsLetterOrDigit (text [i]))
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

			if (Char.IsPunctuation (text [i]) || Char.IsSymbol (text [i]) || Char.IsWhiteSpace (text [i])) {
				for (; i >= 0; i--) {
					if (Char.IsLetterOrDigit (text [i]))
						break;
				}
				for (; i >= 0; i--) {
					if (!Char.IsLetterOrDigit (text [i]))
						break;
				}
			} else {
				for (; i >= 0; i--) {
					if (!Char.IsLetterOrDigit (text [i]))
						break;
				}
			}
			i++;

			if (i != p)
				return i;

			return -1;
		}

#if false
        public override void ProcessMouse (Curses.MouseEvent ev)
        {
            if ((ev.ButtonState & Curses.Event.Button1Clicked) == 0)
                return;

            .SetFocus (this);

            // We could also set the cursor position.
            point = first + (ev.X - x);
            if (point > text.Length)
                point = text.Length;
            if (point < first)
                point = 0;

            SetNeedsDisplay ();
        }
#endif
	}


}
