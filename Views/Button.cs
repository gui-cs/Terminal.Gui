//
// Button.cs: Button control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal {
	/// <summary>
	///   Button view
	/// </summary>
	/// <remarks>
	///   Provides a button that can be clicked, or pressed with
	///   the enter key and processes hotkeys (the first uppercase
	///   letter in the button becomes the hotkey).
	/// </remarks>
	public class Button : View {
		string text;
		string shown_text;
		char hot_key;
		int hot_pos = -1;
		bool is_default;

		/// <summary>
		///   Clicked event, raised when the button is clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the button is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public event EventHandler Clicked;

		/// <summary>
		///   Public constructor, creates a button based on
		///   the given text at position 0,0
		/// </summary>
		/// <remarks>
		///   The size of the button is computed based on the
		///   text length.   This button is not a default button.
		/// </remarks>
		public Button (string s) : this (0, 0, s) { }

		/// <summary>
		///   Public constructor, creates a button based on
		///   the given text.
		/// </summary>
		/// <remarks>
		///   If the value for is_default is true, a special
		///   decoration is used, and the enter key on a
		///   dialog would implicitly activate this button.
		/// </remarks>
		public Button (string s, bool is_default) : this (0, 0, s, is_default) { }

		/// <summary>
		///   Public constructor, creates a button based on
		///   the given text at the given position.
		/// </summary>
		/// <remarks>
		///   The size of the button is computed based on the
		///   text length.   This button is not a default button.
		/// </remarks>
		public Button (int x, int y, string s) : this (x, y, s, false) { }

		/// <summary>
		///   The text displayed by this widget.
		/// </summary>
		public string Text {
			get {
				return text;
			}

			set {
				text = value;
				if (is_default)
					shown_text = "[< " + value + " >]";
				else
					shown_text = "[ " + value + " ]";

				hot_pos = -1;
				hot_key = (char)0;
				int i = 0;
				foreach (char c in shown_text) {
					if (Char.IsUpper (c)) {
						hot_key = c;
						hot_pos = i;
						break;
					}
					i++;
				}
			}
		}

		/// <summary>
		///   Public constructor, creates a button based on
		///   the given text at the given position.
		/// </summary>
		/// <remarks>
		///   If the value for is_default is true, a special
		///   decoration is used, and the enter key on a
		///   dialog would implicitly activate this button.
		/// </remarks>
		public Button (int x, int y, string s, bool is_default)
		    : base (new Rect (x, y, s.Length + 4 + (is_default ? 2 : 0), 1))
		{
			CanFocus = true;

			this.is_default = is_default;
			Text = s;
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (HasFocus ? Colors.Base.Focus : Colors.Base.Normal);
			Move (0, 0);
			Driver.AddStr (shown_text);

			if (hot_pos != -1) {
				Move (hot_pos, 0);
				Driver.SetAttribute (HasFocus ? Colors.Base.HotFocus : Colors.Base.HotNormal);
				Driver.AddCh (hot_key);
			}
		}

		public override void PositionCursor ()
		{
			Move (hot_pos, 0);
		}

		bool CheckKey (KeyEvent key)
		{
			if (Char.ToUpper ((char)key.KeyValue) == hot_key) {
				this.SuperView.SetFocus (this);
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.IsAlt)
				return CheckKey (kb);

			return false;
		}

		public override bool ProcessColdKey (KeyEvent kb)
		{
			if (is_default && kb.KeyValue == '\n') {
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
				return true;
			}
			return CheckKey (kb);
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			var c = kb.KeyValue;
			if (c == '\n' || c == ' ' || Char.ToUpper ((char)c) == hot_key) {
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
				return true;
			}
			return base.ProcessKey (kb);
		}

#if false
        public override void ProcessMouse (Curses.MouseEvent ev)
        {
            if ((ev.ButtonState & Curses.Event.Button1Clicked) != 0) {
                Container.SetFocus (this);
                Container.Redraw ();
                if (Clicked != null)
                    Clicked (this, EventArgs.Empty);
            }
        }
#endif
	}
}
