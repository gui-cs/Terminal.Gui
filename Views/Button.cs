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
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Button"/> is the default action to activate on return on a dialog.
		/// </summary>
		/// <value><c>true</c> if is default; otherwise, <c>false</c>.</value>
		public bool IsDefault {
			get => is_default;
			set {
				is_default = value;
				Update ();
			}
		}

		/// <summary>
		///   Clicked event, raised when the button is clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the button is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public Action Clicked;

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
				Update ();
			}
		}

		internal void Update ()
		{
			if (IsDefault)
				shown_text = "[< " + text + " >]";
			else
				shown_text = "[ " + text + " ]";

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
			SetNeedsDisplay ();
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

			this.IsDefault = is_default;
			Text = s;
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
			Move (0, 0);
			Driver.AddStr (shown_text);

			if (hot_pos != -1) {
				Move (hot_pos, 0);
				Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.HotNormal);
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
					Clicked ();
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
			if (IsDefault && kb.KeyValue == '\n') {
				if (Clicked != null)
					Clicked ();
				return true;
			}
			return CheckKey (kb);
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			var c = kb.KeyValue;
			if (c == '\n' || c == ' ' || Char.ToUpper ((char)c) == hot_key) {
				if (Clicked != null)
					Clicked ();
				return true;
			}
			return base.ProcessKey (kb);
		}

		public override bool MouseEvent(MouseEvent me)
		{
			if (me.Flags == MouseFlags.Button1Clicked) {
				SuperView.SetFocus (this);
				SetNeedsDisplay ();

				if (Clicked != null)
					Clicked ();
				return true;
			}
			return false;
		}
	}
}
