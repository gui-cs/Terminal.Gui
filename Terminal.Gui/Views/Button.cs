//
// Button.cs: Button control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	///   Button is a view that provides an item that invokes a callback when activated.
	/// </summary>
	/// <remarks>
	/// <para>
	///   Provides a button that can be clicked, or pressed with
	///   the enter key and processes hotkeys (the first uppercase
	///   letter in the button becomes the hotkey).
	/// </para>
	/// <para>
	///   If the button is configured as the default (IsDefault) the button
	///   will respond to the return key is no other view processes it, and
	///   turns this into a clicked event.
	/// </para>
	/// </remarks>
	public class Button : View {
		ustring text;
		ustring shown_text;
		Rune hot_key;
		int hot_pos = -1;
		bool is_default;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.Button"/> is the default action to activate on return on a dialog.
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
		/// <param name="text">The button's text</param>
		/// <param name="is_default">If set, this makes the button the default button in the current view, which means that if the user presses return on a view that does not handle return, it will be treated as if he had clicked on the button</param>
		public Button (ustring text, bool is_default = false) : base ()
		{
			CanFocus = true;
			this.IsDefault = is_default;
			Text = text;
			int w = text.Length + 4 + (is_default ? 2 : 0);
			Width = w;
			Height = 1;
			Frame = new Rect (0, 0, w, 1);
		}

		/// <summary>
		///   Public constructor, creates a button based on
		///   the given text at the given position.
		/// </summary>
		/// <remarks>
		///   The size of the button is computed based on the
		///   text length.   This button is not a default button.
		/// </remarks>
		/// <param name="x">X position where the button will be shown.</param>
		/// <param name="y">Y position where the button will be shown.</param>
		/// <param name="text">The button's text</param>
		public Button (int x, int y, ustring text) : this (x, y, text, false) { }

		/// <summary>
		///   The text displayed by this widget.
		/// </summary>
		public ustring Text {
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
			hot_key = (Rune)0;
			int i = 0;
			foreach (Rune c in shown_text) {
				if (Rune.IsUpper (c)) {
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
		/// <param name="x">X position where the button will be shown.</param>
		/// <param name="y">Y position where the button will be shown.</param>
		/// <param name="text">The button's text</param>
		/// <param name="is_default">If set, this makes the button the default button in the current view, which means that if the user presses return on a view that does not handle return, it will be treated as if he had clicked on the button</param>
		public Button (int x, int y, ustring text, bool is_default)
		    : base (new Rect (x, y, text.Length + 4 + (is_default ? 2 : 0), 1))
		{
			CanFocus = true;

			this.IsDefault = is_default;
			Text = text;
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
			Move (0, 0);
			Driver.AddStr (shown_text);

			if (hot_pos != -1) {
				Move (hot_pos, 0);
				Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.HotNormal);
				Driver.AddRune (hot_key);
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
			if (c == '\n' || c == ' ' || Rune.ToUpper ((Rune)c) == hot_key) {
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
