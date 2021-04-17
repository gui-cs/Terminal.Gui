//
// TextMaskField.cs: single-line text editor with Emacs keybindings
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NStack;

namespace Terminal.Gui {

	/// <summary>
	/// TextField with Mask
	/// 
	/// Mask:
	///	000-9000
	/// Input:
	///	123- 456
	/// Output:
	///	123456
	///	123 456
	///	123-456
	///	123- 456
	/// </summary>
	public class TextMaskField : View {

		/// <summary>
		/// Mask Type 
		/// </summary>
		public enum TextMaskType {
			/// <summary>
			/// Validates input with <see cref="MaskRunes"/>.
			/// </summary>
			Mask,
			/// <summary>
			/// Validetes input with a Regex Pattern.
			/// </summary>
			Regex
		}

		Regex regex;
		List<Rune> text;
		List<Rune> mask;
		TextMaskType masktype;
		bool error = false;

		/// <summary>
		/// When masktype == regex and text is right aligned, cursorPosition goes from 1 to x, leaving 0 for insert.
		/// </summary>
		int cursorPosition;

		/// <summary>
		/// Raised when the <see cref="Text"/> of the <see cref="TextMaskField"/> changes.
		/// </summary>
		public event Action<ustring> TextChanged;

		/// <summary>
		/// Raised when the validation of the <see cref="TextMaskField"/> occurs.
		/// </summary>
		public event Action<bool> OnValidation;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextMaskField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		protected TextMaskField ()
		{
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextMaskField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="mask">Mask</param>
		public TextMaskField (ustring mask) : this (mask, ustring.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextMaskField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="mask">Mask or Regex Pattern</param>
		/// <param name="masktype">Mask or Regex</param>
		public TextMaskField (ustring mask, TextMaskType masktype) : this (mask, ustring.Empty, masktype) { }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mask">Mask or Regex Pattern<see cref="masktype"/></param>
		/// <param name="text">Initial Value(TODO)</param>
		/// <param name="masktype">Mask or Regex</param>
		public TextMaskField (ustring mask, ustring text, TextMaskType masktype = TextMaskType.Mask) : base ()
		{
			this.masktype = masktype;
			Mask = mask;
			Text = text;
			Initialize ();
		}

		void Initialize ()
		{
			this.Width = masktype == TextMaskType.Mask ? text.Count : 20;
			this.Height = 1;
			this.CanFocus = true;
		}

		/// <summary>
		/// Input value
		/// </summary>
		public new ustring Text {
			get {
				return ustring.Make (text);
			}
			set {
				text = value != ustring.Empty ? value.ToRuneList () : null;

				SetupText ();

				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Mask
		/// </summary>
		public ustring Mask {
			get {
				return ustring.Make (mask);
			}
			set {
				mask = value.ToRuneList ();

				CompileMask ();

				SetupText ();

				SetNeedsDisplay ();
			}
		}

		void SetupText ()
		{
			if (text != null) {
				// TODO: Implement logic to match non-perfect inputs (mask == text)
				switch (masktype) {
				case TextMaskType.Mask:
					if (Validate (text) == false) {
						text = new List<Rune> ();
					}
					break;
				case TextMaskType.Regex:
					if (Validate (text) == false) {
						text = new List<Rune> ();
					}
					break;
				}
				return;
			}

			text = new List<Rune> ();

			switch (masktype) {
			case TextMaskType.Mask:
				for (int i = 0; i < mask.Count; i++) {
					if (MaskRunes.ContainsKey (mask [i])) {
						text.Add (EmptyRune);
					} else {
						text.Add (mask [i]);
					}
					cursorPosition = mask.FindIndex (e => MaskRunes.ContainsKey (e));
				}
				break;
			case TextMaskType.Regex:
				break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Rune EmptyRune { get; set; } = ' ';

		/// <summary>
		/// Character to show for input.
		/// </summary>
		public Rune InputRune { get; set; } = '_';

		/// <summary>
		/// If char is not set, shows the mask. If this is set to false it shows <see cref="InputRune"/>.
		/// </summary>
		public bool ShowMaskOnEmpty { get; set; } = false;

		///inheritdoc/>
		public override void PositionCursor ()
		{
			var margin = GetMargins (Frame.Width);

			if (masktype == TextMaskType.Regex && TextAlignment == TextAlignment.Right) {
				Move (cursorPosition + margin.left - 1, 0);
			} else {
				Move (cursorPosition + margin.left, 0);
			}
		}

		/// <summary>
		/// Margins for text alignment.
		/// </summary>
		/// <param name="width">Total width</param>
		/// <returns>Left and right margins</returns>
		(int left, int right) GetMargins (int width)
		{
			// Only 1 char runes for now.
			var count = text.Count;
			var total = width - count;
			switch (TextAlignment) {
			case TextAlignment.Left:
				return (0, total);
			case TextAlignment.Centered:
				return (total / 2, (total / 2) + (total % 2));
			case TextAlignment.Right:
				return (total, 0);
			default:
				return (0, total);
			}
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			// TODO: Make this work with ColorSchemes.
			var bg = error ? Color.BrightRed : Color.Gray;
			var maskColor = new Attribute (Color.Black, bg);
			var inputColor = new Attribute (error ? Color.White : Color.DarkGray, bg);
			var textColor = new Attribute (Color.Black, bg);

			Move (0, 0);
			var margins = GetMargins (bounds.Width);

			// Left Margin
			Driver.SetAttribute (textColor);
			for (int i = 0; i < margins.left; i++) {
				Driver.AddRune (' ');
			}

			// Content
			if (masktype == TextMaskType.Mask) {
				for (int i = 0; i < text.Count; i++) {
					if (text [i] == EmptyRune) {
						Driver.SetAttribute (inputColor);
						if (ShowMaskOnEmpty || mask [i] == EmptyRune) {
							Driver.AddRune (mask [i]);
						} else {
							Driver.AddRune (InputRune);
						}
					} else {
						if (MaskRunes.ContainsKey (mask [i])) {
							Driver.SetAttribute (textColor);
						} else {
							Driver.SetAttribute (maskColor);
						}
						Driver.AddRune (text [i]);
					}
				}
			} else if (masktype == TextMaskType.Regex) {
				Driver.AddStr (ustring.Make (text));
			}

			// Right Margin
			Driver.SetAttribute (textColor);
			for (int i = 0; i < margins.right; i++) {
				Driver.AddRune (' ');
			}
		}

		/// <summary>
		/// Moves cursor to the left. In mask mode, skip until it finds the an editable char.
		/// </summary>
		/// <returns>True if moved.</returns>
		bool DecCursorPosition ()
		{
			var moved = false;
			switch (masktype) {
			case TextMaskType.Mask:
				if (cursorPosition > 0) {
					// loop until we find the prev editable mask char.
					for (int i = cursorPosition - 1; i >= 0; i--) {
						if (MaskRunes.ContainsKey (mask [i])) {
							moved = true;
							cursorPosition = i;
							break;
						}
					}
				}
				break;
			case TextMaskType.Regex:
				if (cursorPosition > 0) {
					moved = true;
					cursorPosition -= 1;
				}
				break;
			}
			return moved;
		}

		/// <summary>
		/// Moves cursor to the right. In mask mode, skip until it finds an editable char.
		/// </summary>
		/// <returns>True if moved.</returns>
		bool IncCursorPosition ()
		{
			var moved = false;
			switch (masktype) {
			case TextMaskType.Mask:
				if (cursorPosition < mask.Count - 1) {
					// loop until we find the prev editable mask char.
					for (int i = cursorPosition + 1; i < mask.Count; i++) {
						if (MaskRunes.ContainsKey (mask [i])) {
							moved = true;
							cursorPosition = i;
							break;
						}
					}
				}
				break;
			case TextMaskType.Regex:
				if (cursorPosition < text.Count) {
					moved = true;
					cursorPosition += 1;
				}
				break;
			}
			return moved;
		}

		void BackspaceKeyHandler ()
		{
			switch (masktype) {
			case TextMaskType.Mask:
				DecCursorPosition ();
				text [cursorPosition] = EmptyRune;
				TextChanged?.Invoke (Text);
				break;
			case TextMaskType.Regex:
				if (TextAlignment == TextAlignment.Right) {
					if (cursorPosition > 1) {
						text.RemoveAt (--cursorPosition);
						TextChanged?.Invoke (Text);
					}
				} else {
					if (cursorPosition > 0) {
						text.RemoveAt (--cursorPosition);
						TextChanged?.Invoke (Text);
					}
				}
				break;
			}
		}

		void DeleteKeyHandler ()
		{
			switch (masktype) {
			case TextMaskType.Mask:
				text [cursorPosition] = EmptyRune;
				TextChanged?.Invoke (Text);
				break;
			case TextMaskType.Regex:
				if (TextAlignment == TextAlignment.Right) {
					if (cursorPosition > 0) {
						text.RemoveAt (--cursorPosition);
						TextChanged?.Invoke (Text);
					}
				} else {
					if (text.Count > 0 && cursorPosition < text.Count) {
						text.RemoveAt (cursorPosition);
						TextChanged?.Invoke (Text);
					}
				}
				break;
			}
		}

		void HomeKeyHandler ()
		{
			switch (masktype) {
			case TextMaskType.Mask:
				cursorPosition = -1;
				IncCursorPosition ();
				break;
			case TextMaskType.Regex:
				cursorPosition = 0;
				break;
			}
		}

		void EndKeyHandler ()
		{
			switch (masktype) {
			case TextMaskType.Mask:
				cursorPosition = text.Count;
				DecCursorPosition ();
				break;
			case TextMaskType.Regex:
				cursorPosition = text.Count;
				break;
			}
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.Home: HomeKeyHandler (); break;
			case Key.End: EndKeyHandler (); break;
			case Key.DeleteChar: DeleteKeyHandler (); break;
			case Key.Backspace: BackspaceKeyHandler (); break;
			case Key.CursorLeft: DecCursorPosition (); break;
			case Key.CursorRight: IncCursorPosition (); break;
			case Key.Delete:
			default:
				if (kb.Key < Key.Space || kb.Key > Key.CharMask)
					return false;

				var key = new Rune ((uint)kb.KeyValue);

				switch (masktype) {
				case TextMaskType.Mask:
					if (ValidateRune (cursorPosition, key)) {
						text [cursorPosition] = key;
						TextChanged?.Invoke (Text);
						IncCursorPosition ();
					}
					break;
				case TextMaskType.Regex:
					// Validate text copy first.
					var aux = text.ToList ();
					aux.Insert (cursorPosition, key);
					if (Validate (aux)) {
						text.Insert (cursorPosition, key);
						TextChanged?.Invoke (Text);
						IncCursorPosition ();
					}
					break;
				default:
					throw new ArgumentOutOfRangeException ($"{masktype} not implemented.");
				}
				break;
			}

			// used for styling the field
			if (masktype == TextMaskType.Mask) {
				error = Validate (text) ? false : true;
			}

			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Valid mask characters and regex validator.
		/// </summary>
		protected Dictionary<Rune, string> MaskRunes { get; set; } = new Dictionary<Rune, string> {
			['0'] = "[0-9]",
			['9'] = "[0-9\\s]?",
			['L'] = "[a-zA-Z]",
			['?'] = "[a-zA-Z\\s]?",
			['A'] = "[0-9a-zA-Z]",
			['a'] = "[0-9a-zA-Z\\s]?"
		};

		/// <summary>
		/// This characters must be escaped in the regex pattern. 
		/// </summary>
		static readonly List<Rune> RegexSpecialsChars = new List<Rune> { '.', '$', '^', '{', '[', '(', '|', ')', '*', '+', '?', '\\' };

		/// <summary>
		/// Compiles the regex pattern for validation. <see cref="Validate(List{Rune})"/>
		/// </summary>
		void CompileMask ()
		{
			if (masktype == TextMaskType.Regex) {
				regex = new Regex (ustring.Make (mask).ToString (), RegexOptions.Compiled);
				return;
			}

			// <see cref="MaskRunes"/>
			// Join all the individual pattern of the mask. 
			ustring pattern = "^";

			for (int i = 0; i < mask.Count; i++) {
				var m = mask [i];
				if (RegexSpecialsChars.Contains (m)) {
					pattern += $@"\{m}";
				} else if (MaskRunes.ContainsKey (m)) {
					pattern += $@"{MaskRunes [m]}";
				} else {
					pattern += $@"{m}";
				}
			}

			pattern += "$";

			regex = new Regex (pattern.ToString (), RegexOptions.Compiled);
		}

		/// <summary>
		/// This property returns true if the input is valid.
		/// </summary>
		public virtual bool IsValid {
			get {
				return Validate (text);
			}
		}

		/// <summary>
		/// MaskType.Mask : Validates after input.
		/// MaskType.Regex: Validates before input.
		/// </summary>
		/// <param name="text">List of runes.</param>
		/// <returns>True if valid.</returns>
		protected virtual bool Validate (List<Rune> text)
		{
			var a = regex.Match (ustring.Make (text).ToString ());
			OnValidation?.Invoke (a.Success);
			return a.Success;
		}

		/// <summary>
		/// MaskType.Mask only. This validation is done before inserting the character.
		/// </summary>
		/// <param name="pos">Position in the mask.</param>
		/// <param name="rune">Input rune.</param>
		/// <returns>True if valid.</returns>
		protected virtual bool ValidateRune (int pos, Rune rune)
		{
			var match = Regex.Match (rune.ToString (), MaskRunes [mask [pos]]);
			return match.Success && match.Value == rune.ToString ();
		}
	}
}
