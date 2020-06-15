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
	///   Button is a <see cref="View"/> that provides an item that invokes an <see cref="Action"/> when activated by the user.
	/// </summary>
	/// <remarks>
	/// <para>
	///   Provides a button showing text invokes an <see cref="Action"/> when clicked on with a mouse
	///   or when the user presses SPACE, ENTER, or hotkey. The hotkey is specified by the first uppercase
	///   letter in the button.
	/// </para>
	/// <para>
	///   When the button is configured as the default (<see cref="IsDefault"/>) and the user presses
	///   the ENTER key, if no other <see cref="View"/> processes the <see cref="KeyEvent"/>, the <see cref="Button"/>'s
	///   <see cref="Action"/> will be invoked.
	/// </para>
	/// </remarks>
	public class Button : View {
		ustring text;
		ustring shown_text;
		Rune hot_key;
		int hot_pos = -1;
		bool is_default;
		TextAlignment textAlignment = TextAlignment.Centered;

		/// <summary>
		/// Gets or sets whether the <see cref="Button"/> is the default action to activate in a dialog.
		/// </summary>
		/// <value><c>true</c> if is default; otherwise, <c>false</c>.</value>
		public bool IsDefault {
			get => is_default;
			set {
				is_default = value;
				SetWidthHeight (Text, is_default);
				Update ();
			}
		}

		/// <summary>
		///   Clicked <see cref="Action"/>, raised when the button is clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the button is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public Action Clicked;

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		///   The width of the <see cref="Button"/> is computed based on the
		///   text length. The height will always be 1.
		/// </remarks>
		public Button () : this (text: string.Empty, is_default: false) { }

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		///   The width of the <see cref="Button"/> is computed based on the
		///   text length. The height will always be 1.
		/// </remarks>
		/// <param name="text">The button's text</param>
		/// <param name="is_default">
		///   If <c>true</c>, a special decoration is used, and the user pressing the enter key 
		///   in a <see cref="Dialog"/> will implicitly activate this button.
		/// </param>
		public Button (ustring text, bool is_default = false) : base ()
		{
			Init (text, is_default);
		}

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Absolute"/> layout, based on the given text
		/// </summary>
		/// <remarks>
		///   The width of the <see cref="Button"/> is computed based on the
		///   text length. The height will always be 1.
		/// </remarks>
		/// <param name="x">X position where the button will be shown.</param>
		/// <param name="y">Y position where the button will be shown.</param>
		/// <param name="text">The button's text</param>
		public Button (int x, int y, ustring text) : this (x, y, text, false) { }

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Absolute"/> layout, based on the given text.
		/// </summary>
		/// <remarks>
		///   The width of the <see cref="Button"/> is computed based on the
		///   text length. The height will always be 1.
		/// </remarks>
		/// <param name="x">X position where the button will be shown.</param>
		/// <param name="y">Y position where the button will be shown.</param>
		/// <param name="text">The button's text</param>
		/// <param name="is_default">
		///   If <c>true</c>, a special decoration is used, and the user pressing the enter key 
		///   in a <see cref="Dialog"/> will implicitly activate this button.
		/// </param>
		public Button (int x, int y, ustring text, bool is_default)
		    : base (new Rect (x, y, text.Length + 4 + (is_default ? 2 : 0), 1))
		{
			Init (text, is_default);
		}

		Rune _leftBracket;
		Rune _rightBracket;
		Rune _leftDefault;
		Rune _rightDefault;

		void Init (ustring text, bool is_default)
		{
			_leftBracket = new Rune (Driver != null ? Driver.LeftBracket : '[');
			_rightBracket = new Rune (Driver != null ? Driver.RightBracket : ']');
			_leftDefault = new Rune (Driver != null ? Driver.LeftDefaultIndicator : '<');
			_rightDefault = new Rune (Driver != null ? Driver.RightDefaultIndicator : '>');

			CanFocus = true;
			Text = text ?? string.Empty;
			this.IsDefault = is_default;
			int w = SetWidthHeight (text, is_default);
			Frame = new Rect (Frame.Location, new Size (w, 1));
		}

		int SetWidthHeight (ustring text, bool is_default)
		{
			int w = text.Length + 4 + (is_default ? 2 : 0);
			Width = w;
			Height = 1;
			Frame = new Rect (Frame.Location, new Size (w, 1));
			return w;
		}

		/// <summary>
		///   The text displayed by this <see cref="Button"/>.
		/// </summary>
		public ustring Text {
			get {
				return text;
			}

			set {
				SetWidthHeight (value, is_default);
				text = value;
				Update ();
			}
		}

		/// <summary>
		/// Sets or gets the text alignment for the <see cref="Button"/>.
		/// </summary>
		public TextAlignment TextAlignment {
			get => textAlignment;
			set {
				textAlignment = value;
				Update ();
			}
		}

		internal void Update ()
		{
			if (IsDefault)
				shown_text = ustring.Make (_leftBracket) + ustring.Make (_leftDefault) + " " + text + " " + ustring.Make (_rightDefault) + ustring.Make (_rightBracket);
			else
				shown_text = ustring.Make (_leftBracket) + " " + text + " " + ustring.Make (_rightBracket);

			shown_text = GetTextFromHotKey (shown_text, '_', out hot_pos, out hot_key);

			SetNeedsDisplay ();
		}

		int c_hot_pos;

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
			Move (0, 0);

			var caption = GetTextAlignment (shown_text, hot_pos, out int s_hot_pos, TextAlignment);
			c_hot_pos = s_hot_pos;

			Driver.AddStr (caption);

			if (c_hot_pos != -1) {
				Move (c_hot_pos, 0);
				Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.HotNormal);
				Driver.AddRune (hot_key);
			}
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			Move (c_hot_pos == -1 ? 1 : c_hot_pos, 0);
		}

		bool CheckKey (KeyEvent key)
		{
			if ((char)key.KeyValue == hot_key) {
				this.SuperView.SetFocus (this);
				Clicked?.Invoke ();
				return true;
			}
			return false;
		}

		///<inheritdoc/>
		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.IsAlt)
				return CheckKey (kb);

			return false;
		}

		///<inheritdoc/>
		public override bool ProcessColdKey (KeyEvent kb)
		{
			if (IsDefault && kb.KeyValue == '\n') {
				Clicked?.Invoke ();
				return true;
			}
			return CheckKey (kb);
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			var c = kb.KeyValue;
			if (c == '\n' || c == ' ' || Rune.ToUpper ((uint)c) == hot_key) {
				Clicked?.Invoke ();
				return true;
			}
			return base.ProcessKey (kb);
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags == MouseFlags.Button1Clicked) {
				if (!HasFocus) {
					SuperView.SetFocus (this);
					SetNeedsDisplay ();
				}

				Clicked?.Invoke ();
				return true;
			}
			return false;
		}
	}
}
