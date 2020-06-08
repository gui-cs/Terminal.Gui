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
		public Button () : this (string.Empty) { }

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
			CanFocus = true;
			Text = text ?? string.Empty;
			this.IsDefault = is_default;
			int w = SetWidthHeight (text, is_default);
			Frame = new Rect (Frame.Location, new Size (w, 1));
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
			CanFocus = true;
			Text = text ?? string.Empty;
			this.IsDefault = is_default;
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
				if (text?.Length != value?.Length) {
					SetWidthHeight (value, is_default);
				}
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
				SetNeedsDisplay ();
			}
		}

		Rune _leftBracket = new Rune ('[');
		Rune _rightBracket = new Rune (']');
		Rune _leftDefault = new Rune ('<');
		Rune _rightDefault = new Rune ('>');

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

			var caption = shown_text;
			c_hot_pos = hot_pos;
			int start;

			if (Frame.Width > shown_text.Length + 1) {
				switch (TextAlignment) {
				case TextAlignment.Left:
					caption += new string (' ', Frame.Width - caption.RuneCount);
					break;
				case TextAlignment.Right:
					start = Frame.Width - caption.RuneCount;
					caption = $"{new string (' ', Frame.Width - caption.RuneCount)}{caption}";
					if (c_hot_pos > -1) {
						c_hot_pos += start;
					}
					break;
				case TextAlignment.Centered:
					start = Frame.Width / 2 - caption.RuneCount / 2;
					caption = $"{new string (' ', start)}{caption}{new string (' ', Frame.Width - caption.RuneCount - start)}";
					if (c_hot_pos > -1) {
						c_hot_pos += start;
					}
					break;
				case TextAlignment.Justified:
					var words = caption.Split (" ");
					var wLen = GetWordsLength (words, out int runeCount);
					var space = (Frame.Width - runeCount) / (caption.Length - wLen);
					caption = "";
					for (int i = 0; i < words.Length; i++) {
						if (i == words.Length - 1) {
							caption += new string (' ', Frame.Width - caption.RuneCount - 1);
							caption += words [i];
						} else {
							caption += words [i];
						}
						if (i < words.Length - 1) {
							caption += new string (' ', space);
						}
					}
					if (c_hot_pos > -1) {
						c_hot_pos += space - 1 + (wLen - runeCount == 0 ? 0 : wLen - runeCount + 1);
					}
					break;
				}
			}

			Driver.AddStr (caption);

			if (c_hot_pos != -1) {
				Move (c_hot_pos, 0);
				Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.HotNormal);
				Driver.AddRune (hot_key);
			}
		}

		int GetWordsLength (ustring [] words, out int runeCount)
		{
			int length = 0;
			int rCount = 0;
			for (int i = 0; i < words.Length; i++) {
				length += words [i].Length;
				rCount += words [i].RuneCount;
			}
			runeCount = rCount;
			return length;
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
