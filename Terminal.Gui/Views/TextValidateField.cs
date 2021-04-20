//
// TextValidateField.cs: single-line text editor with validation through providers.
//
// Authors:
//	José Miguel Perricone (jmperricone@hotmail.com)
//

using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Terminal.Gui.TextValidateProviders;

namespace Terminal.Gui {

	namespace TextValidateProviders {
		/// <summary>
		/// TextValidateField Providers Interface.
		/// All TextValidateField are created with a ITextValidateProvider.
		/// </summary>
		public interface ITextValidateProvider {
			/// <summary>
			/// Set that this provider uses a fixed width.
			/// e.g. Masked ones are fixed.
			/// </summary>
			bool Fixed { get; }

			/// <summary>
			/// Set Cursor position to <paramref name="pos"/>.
			/// </summary>
			/// <param name="pos"></param>
			/// <returns>Return first valid position.</returns>
			int Cursor (int pos);

			/// <summary>
			/// First valid position before <paramref name="pos"/>.
			/// </summary>
			/// <param name="pos"></param>
			/// <returns>New cursor position if any, otherwise returns <paramref name="pos"/></returns>
			int CursorLeft (int pos);

			/// <summary>
			/// First valid position after <paramref name="pos"/>.
			/// </summary>
			/// <param name="pos">Current position.</param>
			/// <returns>New cursor position if any, otherwise returns <paramref name="pos"/></returns>
			int CursorRight (int pos);

			/// <summary>
			/// Find the first valid character position.
			/// </summary>
			/// <returns>New cursor position.</returns>
			int CursorStart ();

			/// <summary>
			/// Find the last valid character position.
			/// </summary>
			/// <returns>New cursor position.</returns>
			int CursorEnd ();

			/// <summary>
			/// Deletes the current character in <paramref name="pos"/>.
			/// </summary>
			/// <param name="pos"></param>
			/// <returns>true if the character was successfully removed, otherwise false.</returns>
			bool Delete (int pos);

			/// <summary>
			/// Insert character <paramref name="ch"/> in position <paramref name="pos"/>.
			/// </summary>
			/// <param name="ch"></param>
			/// <param name="pos"></param>
			/// <returns>true if the character was successfully inserted, otherwise false.</returns>
			bool InsertAt (char ch, int pos);

			/// <summary>
			/// True if the input is valid, otherwise false.
			/// </summary>
			bool IsValid { get; }

			/// <summary>
			/// Set the input text, and get the formatted string for display.
			/// </summary>
			ustring Text { get; set; }

			/// <summary>
			/// Mask used for validation.
			/// Not always a mask, can by a regex expression.
			/// TODO: Maybe we can change the name.
			/// </summary>
			ustring Mask { get; set; }
		}

		//////////////////////////////////////////////////////////////////////////////
		// PROVIDERS
		//////////////////////////////////////////////////////////////////////////////

		#region NetMaskedTextProvider

		/// <summary>
		/// .Net MaskedTextProvider Provider for TextValidateField.
		/// <para></para>
		/// <para><a href="https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.maskedtextprovider?view=net-5.0">Wrapper around MaskedTextProvider</a></para>
		/// <para><a href="https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.maskedtextbox.mask?view=net-5.0">Masking elements</a></para>
		/// </summary>
		public class NetMaskedTextProvider : ITextValidateProvider {
			MaskedTextProvider provider;

			/// <summary>
			/// Empty Constructor
			/// </summary>
			public NetMaskedTextProvider () { }

			///<inheritdoc/>
			public ustring Mask {
				get {
					return provider?.Mask;
				}
				set {
					provider = new MaskedTextProvider (value == ustring.Empty ? "&&&&&&" : value.ToString ());
				}
			}

			///<inheritdoc/>
			public ustring Text {
				get {
					return provider.ToDisplayString ();
				}
				set {
					provider.Set (value.ToString ());
				}
			}

			///<inheritdoc/>
			public bool IsValid => provider.MaskCompleted;

			///<inheritdoc/>
			public bool Fixed => true;

			///<inheritdoc/>
			public int Cursor (int pos)
			{
				if (pos < 0) {
					return CursorStart ();
				} else if (pos > provider.Length) {
					return CursorEnd ();
				} else {
					var p = provider.FindEditPositionFrom (pos, false);
					if (p == -1) p = provider.FindEditPositionFrom (pos, true);
					return p;
				}
			}

			///<inheritdoc/>
			public int CursorStart ()
			{
				return
					provider.IsEditPosition (0)
					? 0
					: provider.FindEditPositionFrom (0, true);
			}

			///<inheritdoc/>
			public int CursorEnd ()
			{
				return
					provider.IsEditPosition (provider.Length - 1)
					? provider.Length - 1
					: provider.FindEditPositionFrom (provider.Length, false);
			}

			///<inheritdoc/>
			public int CursorLeft (int pos)
			{
				var c = provider.FindEditPositionFrom (pos - 1, false);
				return c == -1 ? CursorEnd () : c;
			}

			///<inheritdoc/>
			public int CursorRight (int pos)
			{
				var c = provider.FindEditPositionFrom (pos + 1, true);
				return c == -1 ? CursorStart () : c;
			}

			///<inheritdoc/>
			public bool Delete (int pos)
			{
				return provider.RemoveAt (pos);
			}

			///<inheritdoc/>
			public bool InsertAt (char ch, int pos)
			{
				return provider.Replace (ch, pos);
			}
		}
		#endregion

		#region TextMaskProvider
		/// <summary>
		/// Regex based Mask Provider for TextValidateField.
		/// </summary>
		public class TextMaskProvider : ITextValidateProvider {
			Regex regex;
			List<Rune> text;
			List<Rune> mask;

			/// <summary>
			/// Empty Constructor
			/// </summary>
			public TextMaskProvider () { }

			///<inheritdoc/>
			public ustring Mask {
				get {
					return ustring.Make (mask);
				}
				set {
					mask = value.ToRuneList ();
					CompileMask ();
					SetupText ();
				}
			}

			///<inheritdoc/>
			public ustring Text {
				get {
					var str = "";
					for (int i = 0; i < text.Count; i++) {
						if (text [i] == ' ' && MaskRunes.ContainsKey (mask [i]))
							str += PromptRune;
						else
							str += text [i];
					}
					return str;
				}
				set {
					text = value != ustring.Empty ? value.ToRuneList () : null;
					SetupText ();
				}
			}

			///<inheritdoc/>
			public bool IsValid {
				get {
					var match = regex.Match (ustring.Make (text).ToString ());
					return match.Success;
				}
			}

			///<inheritdoc/>
			public int CursorStart ()
			{
				return CursorRight (-1);
			}

			///<inheritdoc/>
			public int CursorEnd ()
			{
				return CursorLeft (text.Count);
			}

			///<inheritdoc/>
			public int Cursor (int pos)
			{
				if (pos < 0) {
					return CursorStart ();
				} else if (pos >= text.Count) {
					return CursorEnd ();
				} else {
					if (MaskRunes.ContainsKey (mask [pos])) {
						return pos;
					}
					var p = CursorLeft (pos);
					if (p == pos) p = CursorRight (pos);
					return p;
				}
			}

			///<inheritdoc/>
			public int CursorLeft (int pos)
			{
				if (pos > 0) {
					for (int i = pos - 1; i >= 0; i--) {
						if (MaskRunes.ContainsKey (mask [i])) {
							return i;
						}
					}
				}
				return pos;
			}

			///<inheritdoc/>
			public int CursorRight (int pos)
			{
				if (pos < mask.Count - 1) {
					for (int i = pos + 1; i < mask.Count; i++) {
						if (MaskRunes.ContainsKey (mask [i])) {
							return i;
						}
					}
				}
				return pos;
			}

			/// <summary>
			/// Character to show as prompt for input.
			/// </summary>
			public Rune PromptRune { get; set; } = '_';

			///<inheritdoc/>
			public bool Delete (int pos)
			{
				text [pos] = ' ';
				return true;
			}

			///<inheritdoc/>
			public bool InsertAt (char ch, int pos)
			{
				if (ValidateRune (pos, ch)) {
					text [pos] = ch;
					return true;
				}
				return false;
			}

			void SetupText ()
			{
				if (text != null && IsValid) {
					return;
				}

				text = new List<Rune> ();

				// I go through the characters of the mask and replace the editable ones with the prompt char.
				for (int i = 0; i < mask.Count; i++) {
					if (MaskRunes.ContainsKey (mask [i])) {
						text.Add (' ');
					} else {
						text.Add (mask [i]);
					}
				}
			}

			static readonly List<Rune> RegexSpecialsChars = new List<Rune> { '.', '$', '^', '{', '[', '(', '|', ')', '*', '+', '?', '\\' };

			/// <summary>
			/// Valid mask characters and regex validator.
			/// </summary>
			protected Dictionary<Rune, string> MaskRunes { get; set; } = new Dictionary<Rune, string> {
				['0'] = "[0-9]",
				['9'] = "[0-9\\s]?",
				['L'] = "[a-zA-Z]",
				['?'] = "[a-zA-Z\\s]?",
				['A'] = "[0-9a-zA-Z]",
				['a'] = "[0-9a-zA-Z\\s]?",
				['&'] = "."
			};

			///<inheritdoc/>
			public bool Fixed => true;

			/// <summary>
			/// Compiles the regex pattern for validation. <see cref="IsValid"/>
			/// </summary>
			private void CompileMask ()
			{
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
			/// Tests whether the specified character could be set successfully at the specified position.
			/// </summary>
			/// <param name="pos"></param>
			/// <param name="rune"></param>
			/// <returns></returns>
			protected virtual bool ValidateRune (int pos, Rune rune)
			{
				var match = Regex.Match (rune.ToString (), MaskRunes [mask [pos]]);
				return match.Success && match.Value == rune.ToString ();
			}
		}
		#endregion

		#region TextRegexProvider

		/// <summary>
		/// Regex Provider for TextValidateField.
		/// </summary>
		public class TextRegexProvider : ITextValidateProvider {
			Regex regex;
			List<Rune> text;
			List<Rune> mask;

			/// <summary>
			/// Empty Constructor
			/// </summary>
			public TextRegexProvider () { }

			///<inheritdoc/>
			public ustring Mask {
				get {
					return ustring.Make (mask);
				}
				set {
					mask = value.ToRuneList ();
					CompileMask ();
					SetupText ();
				}
			}

			///<inheritdoc/>
			public ustring Text {
				get {
					return ustring.Make (text);
				}
				set {
					text = value != ustring.Empty ? value.ToRuneList () : null;
					SetupText ();
				}
			}

			///<inheritdoc/>
			public bool IsValid {
				get {
					return Validate (text);
				}
			}

			///<inheritdoc/>
			public bool Fixed => false;

			/// <summary>
			/// When true, validates with the regex pattern on each input, preventing the input if it's not valid.
			/// </summary>
			public bool ValidateOnInput { get; set; } = true;

			bool Validate (List<Rune> text)
			{
				var match = regex.Match (ustring.Make (text).ToString ());
				return match.Success;
			}

			///<inheritdoc/>
			public int Cursor (int pos)
			{
				if (pos < 0) {
					return CursorStart ();
				} else if (pos > text.Count) {
					return CursorEnd ();
				} else {
					return pos + 1;
				}
			}

			///<inheritdoc/>
			public int CursorStart ()
			{
				return 0;
			}

			///<inheritdoc/>
			public int CursorEnd ()
			{
				return text.Count;
			}

			///<inheritdoc/>
			public int CursorLeft (int pos)
			{
				if (pos > 0) {
					return pos - 1;
				}
				return pos;
			}

			///<inheritdoc/>
			public int CursorRight (int pos)
			{
				if (pos < text.Count) {
					return pos + 1;
				}
				return pos;
			}

			///<inheritdoc/>
			public bool Delete (int pos)
			{
				if (text.Count > 0 && pos < text.Count) {
					text.RemoveAt (pos);
				}
				return true;
			}

			///<inheritdoc/>
			public bool InsertAt (char ch, int pos)
			{
				var aux = text.ToList ();
				aux.Insert (pos, ch);
				if (Validate (aux) || ValidateOnInput == false) {
					text.Insert (pos, ch);
					return true;
				}
				return false;
			}

			void SetupText ()
			{
				if (text != null && IsValid) {
					return;
				}

				text = new List<Rune> ();
			}

			/// <summary>
			/// <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-escapes-in-regular-expressions"></a>
			/// </summary>
			static readonly List<Rune> RegexSpecialsChars = new List<Rune> { '.', '$', '^', '{', '[', '(', '|', ')', '*', '+', '?', '\\' };

			/// <summary>
			/// Compiles the regex pattern for validation./>
			/// </summary>
			private void CompileMask ()
			{
				regex = new Regex (ustring.Make (mask).ToString (), RegexOptions.Compiled);
			}
		}
		#endregion
	}

	/// <summary>
	/// Text field that validates input through a  <see cref="ITextValidateProvider"/>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class TextValidateField<T> : View where T : ITextValidateProvider {

		ITextValidateProvider provider;
		int cursorPosition = 0;
		bool error = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextValidateField{T}"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public TextValidateField () : this (ustring.Empty)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextValidateField{T}"></see>  class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="mask">Mask</param>
		public TextValidateField (ustring mask) : this (mask, ustring.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextValidateField{T}"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="mask"></param>
		/// <param name="text">Initial Value</param>
		/// <param name="options">Provider specific options</param>
		public TextValidateField (ustring mask, ustring text) : base ()
		{
			provider = Activator.CreateInstance (typeof (T)) as ITextValidateProvider;

			Mask = mask;
			Text = text;

			this.Width = text == ustring.Empty ? 20 : Text.Length;
			this.Height = 1;
			this.CanFocus = true;
		}

		/// <summary>
		/// Get the Provider
		/// </summary>
		public T Provider => (T)provider;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent mouseEvent)
		{
			cursorPosition = provider.Cursor (mouseEvent.X - GetMargins (Frame.Width).left);
			SetFocus ();
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Text
		/// </summary>
		public new ustring Text {
			get {
				return provider.Text;
			}
			set {
				provider.Text = value;

				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Mask
		/// </summary>
		public ustring Mask {
			get {
				return provider.Mask;
			}
			set {
				provider.Mask = value;

				cursorPosition = provider.CursorStart ();

				SetNeedsDisplay ();
			}
		}

		///inheritdoc/>
		public override void PositionCursor ()
		{
			var margin = GetMargins (Frame.Width);

			// Fixed = true, is for inputs thar have fixed width, like masked ones.
			// Fixed = false, is for normal input.
			// When it's right-aligned and it's a normal input, the cursor behaves differently.
			if (provider.Fixed == false && TextAlignment == TextAlignment.Right) {
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
			var count = Text.Length;
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
			var bgcolor = error ? Color.BrightRed : ColorScheme.Focus.Background;
			var textColor = new Attribute (ColorScheme.Focus.Foreground, bgcolor);

			var margins = GetMargins (bounds.Width);

			Move (0, 0);

			// Left Margin
			Driver.SetAttribute (textColor);
			for (int i = 0; i < margins.left; i++) {
				Driver.AddRune (' ');
			}

			// Content
			Driver.SetAttribute (textColor);
			// Content
			for (int i = 0; i < provider.Text.Length; i++) {
				Driver.AddRune (provider.Text [i]);
			}

			// Right Margin
			Driver.SetAttribute (textColor);
			for (int i = 0; i < margins.right; i++) {
				Driver.AddRune (' ');
			}
		}

		/// <summary>
		/// Try to move the cursor to the left.
		/// </summary>
		/// <returns>True if moved.</returns>
		bool CursorLeft ()
		{
			var current = cursorPosition;
			cursorPosition = provider.CursorLeft (cursorPosition);
			return current != cursorPosition;
		}

		/// <summary>
		/// Try to move the cursor to the right.
		/// </summary>
		/// <returns>True if moved.</returns>
		bool CursorRight ()
		{
			var current = cursorPosition;
			cursorPosition = provider.CursorRight (cursorPosition);
			return current != cursorPosition;
		}

		/// <summary>
		/// Delete char at cursor position - 1, moving the cursor.
		/// </summary>
		/// <returns></returns>
		bool BackspaceKeyHandler ()
		{
			if (provider.Fixed == false && TextAlignment == TextAlignment.Right && cursorPosition <= 1) {
				return false;
			}
			cursorPosition = provider.CursorLeft (cursorPosition);
			provider.Delete (cursorPosition);
			return true;
		}

		/// <summary>
		/// Deletes char at current position.
		/// </summary>
		/// <returns></returns>
		bool DeleteKeyHandler ()
		{
			if (provider.Fixed == false && TextAlignment == TextAlignment.Right) {
				cursorPosition = provider.CursorLeft (cursorPosition);
			}
			provider.Delete (cursorPosition);
			return true;
		}

		/// <summary>
		/// Moves the cursor to first char.
		/// </summary>
		/// <returns></returns>
		bool HomeKeyHandler ()
		{
			cursorPosition = provider.CursorStart ();
			return true;
		}

		/// <summary>
		/// Moves the cursor to the last char.
		/// </summary>
		/// <returns></returns>
		bool EndKeyHandler ()
		{
			cursorPosition = provider.CursorEnd ();
			return true;
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.Home: HomeKeyHandler (); break;
			case Key.End: EndKeyHandler (); break;
			case Key.Delete:
			case Key.DeleteChar: DeleteKeyHandler (); break;
			case Key.Backspace: BackspaceKeyHandler (); break;
			case Key.CursorLeft: CursorLeft (); break;
			case Key.CursorRight: CursorRight (); break;
			default:
				if (kb.Key < Key.Space || kb.Key > Key.CharMask)
					return false;

				var key = new Rune ((uint)kb.KeyValue);

				var inserted = provider.InsertAt ((char)key, cursorPosition);

				if (inserted) {
					CursorRight ();
				}

				break;
			}

			// Used for styling the field when it's invalid.
			error = IsValid ? false : true;

			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// This property returns true if the input is valid.
		/// </summary>
		public virtual bool IsValid {
			get {
				return provider.IsValid;
			}
		}
	}
}
