using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// Suppports text formatting, including horizontal alignment and word wrap for <see cref="View"/>.
	/// </summary>
	public class TextFormatter {
		List<ustring> lines = new List<ustring> ();
		ustring text;
		TextAlignment textAlignment;
		Attribute textColor = -1;
		bool recalcPending = false;
		Key hotKey;

		/// <summary>
		///  Inititalizes a new <see cref="TextFormatter"/> object.
		/// </summary>
		/// <param name="view"></param>
		public TextFormatter (View view)
		{
			recalcPending = true;
		}

		/// <summary>
		///   The text to be displayed.
		/// </summary>
		public virtual ustring Text {
			get => text;
			set {
				text = value;
				recalcPending = true;
			}
		}

		// TODO: Add Vertical Text Alignment
		/// <summary>
		/// Controls the horizontal text-alignment property. 
		/// </summary>
		/// <value>The text alignment.</value>
		public TextAlignment Alignment {
			get => textAlignment;
			set {
				textAlignment = value;
				recalcPending = true;
			}
		}

		/// <summary>
		///  Gets the size of the area the text will be drawn in. 
		/// </summary>
		public Size Size { get; internal set; }


		/// <summary>
		/// The specifier character for the hotkey (e.g. '_'). Set to '\xffff' to disable hotkey support for this View instance. The default is '\xffff'. 
		/// </summary>
		public Rune HotKeySpecifier { get; set; } = (Rune)0xFFFF;

		/// <summary>
		/// The position in the text of the hotkey. The hotkey will be rendered using the hot color.
		/// </summary>
		public int HotKeyPos { get => hotKeyPos; set => hotKeyPos = value; }

		/// <summary>
		/// Gets the hotkey. Will be an upper case letter or digit.
		/// </summary>
		public Key HotKey { get => hotKey; internal set => hotKey = value; }

		/// <summary>
		/// Causes the Text to be formatted, based on <see cref="Alignment"/> and <see cref="Size"/>.
		/// </summary>
		public void ReFormat ()
		{
			// With this check, we protect against subclasses with overrides of Text
			if (ustring.IsNullOrEmpty (Text)) {
				return;
			}
			recalcPending = false;
			var shown_text = text;
			if (FindHotKey (text, HotKeySpecifier, true, out hotKeyPos, out hotKey)) {
				shown_text = RemoveHotKeySpecifier (Text, hotKeyPos, HotKeySpecifier);
				shown_text = ReplaceHotKeyWithTag (shown_text, hotKeyPos);
			}
			Reformat (shown_text, lines, Size.Width, textAlignment, Size.Height > 1);
		}

		static ustring StripWhiteCRLF (ustring str)
		{
			var runes = new List<Rune> ();
			foreach (var r in str.ToRunes ()) {
				if (r != '\r' && r != '\n') {
					runes.Add (r);
				}
			}
			return ustring.Make (runes); ;
		}
		static ustring ReplaceCRLFWithSpace (ustring str)
		{
			var runes = new List<Rune> ();
			foreach (var r in str.ToRunes ()) {
				if (r == '\r' || r == '\n') {
					runes.Add (new Rune (' '));
				} else {
					runes.Add (r);
				}
			}
			return ustring.Make (runes); ;
		}

		/// <summary>
		/// Formats the provided text to fit within the width provided using word wrapping.
		/// </summary>
		/// <param name="text">The text to word warp</param>
		/// <param name="width">The width to contrain the text to</param>
		/// <returns>Returns a list of lines.</returns>
		/// <remarks>
		/// Newlines ('\n' and '\r\n') sequences are honored.
		/// </remarks>
		public static List<ustring> WordWrap (ustring text, int width)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}

			int start = 0, end;
			var lines = new List<ustring> ();

			if (ustring.IsNullOrEmpty (text)) {
				return lines;
			}

			text = StripWhiteCRLF (text);

			while ((end = start + width) < text.RuneCount) {
				while (text [end] != ' ' && end > start)
					end -= 1;
				if (end == start)
					end = start + width;

				lines.Add (text [start, end].TrimSpace ());
				start = end;
			}

			if (start < text.RuneCount)
				lines.Add (text.Substring (start).TrimSpace ());

			return lines;
		}

		public static ustring ClipAndJustify (ustring text, int width, TextAlignment talign)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}
			if (ustring.IsNullOrEmpty (text)) {
				return text;
			}

			int slen = text.RuneCount;
			if (slen > width) {
				return text [0, width];
			} else {
				if (talign == TextAlignment.Justified) {
					return Justify (text, width);
				}
				return text;
			}
		}

		/// <summary>
		/// Justifies the text to fill the width provided. Space will be added between words (demarked by spaces and tabs) to
		/// make the text just fit <c>width</c>. Spaces will not be added to the ends.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width"></param>
		/// <param name="spaceChar">Character to replace whitespace and pad with. For debugging purposes.</param>
		/// <returns>The justifed text.</returns>
		public static ustring Justify (ustring text, int width, char spaceChar = ' ')
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}
			if (ustring.IsNullOrEmpty (text)) {
				return text;
			}

			// TODO: Use ustring
			var words = text.ToString ().Split (whitespace, StringSplitOptions.RemoveEmptyEntries);
			int textCount = words.Sum (arg => arg.Length);

			var spaces = words.Length > 1 ? (width - textCount) / (words.Length - 1) : 0;
			var extras = words.Length > 1 ? (width - textCount) % words.Length : 0;

			var s = new System.Text.StringBuilder ();
			//s.Append ($"tc={textCount} sp={spaces},x={extras} - ");
			for (int w = 0; w < words.Length; w++) {
				var x = words [w];
				s.Append (x);
				if (w + 1 < words.Length)
					for (int i = 0; i < spaces; i++)
						s.Append (spaceChar);
				if (extras > 0) {
					//s.Append ('_');
					extras--;
				}
			}
			return ustring.Make (s.ToString ());
		}

		static char [] whitespace = new char [] { ' ', '\t' };
		private int hotKeyPos;

		/// <summary>
		/// Reformats text into lines, applying text alignment and word wraping.
		/// </summary>
		/// <param name="textStr"></param>
		/// <param name="lineResult"></param>
		/// <param name="width"></param>
		/// <param name="talign"></param>
		/// <param name="wordWrap">if <c>false</c>, forces text to fit a single line. Line breaks are converted to spaces.</param>
		static void Reformat (ustring textStr, List<ustring> lineResult, int width, TextAlignment talign, bool wordWrap)
		{
			lineResult.Clear ();

			if (wordWrap == false) {
				textStr = ReplaceCRLFWithSpace (textStr);
				lineResult.Add (ClipAndJustify (textStr, width, talign));
				return;
			}

			int runeCount = textStr.RuneCount;
			int lp = 0;
			for (int i = 0; i < runeCount; i++) {
				Rune c = textStr [i];
				if (c == '\n') {
					var wrappedLines = WordWrap (textStr [lp, i], width);
					foreach (var line in wrappedLines) {
						lineResult.Add (ClipAndJustify (line, width, talign));
					}
					if (wrappedLines.Count == 0) {
						lineResult.Add (ustring.Empty);
					}
					lp = i + 1;
				}
			}
			foreach (var line in WordWrap (textStr [lp, runeCount], width)) {
				lineResult.Add (ClipAndJustify (line, width, talign));
			}
		}

		/// <summary>
		/// Computes the number of lines needed to render the specified text given the width.
		/// </summary>
		/// <returns>Number of lines.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="width">The minimum width for the text.</param>
		public static int MaxLines (ustring text, int width)
		{
			var result = new List<ustring> ();
			TextFormatter.Reformat (text, result, width, TextAlignment.Left, true);
			return result.Count;
		}

		/// <summary>
		/// Computes the maximum width needed to render the text (single line or multple lines).
		/// </summary>
		/// <returns>Max width of lines.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="width">The minimum width for the text.</param>
		public static int MaxWidth (ustring text, int width)
		{
			var result = new List<ustring> ();
			TextFormatter.Reformat (text, result, width, TextAlignment.Left, true);
			return result.Max (s => s.RuneCount);
		}

		internal void Draw (Rect bounds, Attribute normalColor, Attribute hotColor)
		{
			// With this check, we protect against subclasses with overrides of Text
			if (ustring.IsNullOrEmpty (text)) {
				return;
			}

			if (recalcPending) {
				ReFormat ();
			}

			Application.Driver.SetAttribute (normalColor);

			for (int line = 0; line < lines.Count; line++) {
				if (line < (bounds.Height - bounds.Top) || line >= bounds.Height)
					continue;
				var str = lines [line];
				int x;
				switch (textAlignment) {
				case TextAlignment.Left:
					x = bounds.Left;
					break;
				case TextAlignment.Justified:
					x = bounds.Left;
					break;
				case TextAlignment.Right:
					x = bounds.Right - str.RuneCount;
					break;
				case TextAlignment.Centered:
					x = bounds.Left + (bounds.Width - str.RuneCount) / 2;
					break;
				default:
					throw new ArgumentOutOfRangeException ();
				}
				int col = 0;
				foreach (var rune in str) {
					Application.Driver.Move (x + col, bounds.Y + line);
					if ((rune & 0x100000) == 0x100000) {
						Application.Driver.SetAttribute (hotColor);
						Application.Driver.AddRune ((Rune)((uint)rune & ~0x100000));
						Application.Driver.SetAttribute (normalColor);
					} else {
						Application.Driver.AddRune (rune);
					}
					col++;
				}
			}
		}

		/// <summary>
		///  Calculates the rectangle requried to hold text, assuming no word wrapping.
		/// </summary>
		/// <param name="x">The x location of the rectangle</param>
		/// <param name="y">The y location of the rectangle</param>
		/// <param name="text">The text to measure</param>
		/// <returns></returns>
		public static Rect CalcRect (int x, int y, ustring text)
		{
			if (ustring.IsNullOrEmpty (text))
				return Rect.Empty;

			int mw = 0;
			int ml = 1;

			int cols = 0;
			foreach (var rune in text) {
				if (rune == '\n') {
					ml++;
					if (cols > mw)
						mw = cols;
					cols = 0;
				} else {
					if (rune != '\r') {
						cols++;
					}
				}
			}
			if (cols > mw)
				mw = cols;

			return new Rect (x, y, mw, ml);
		}

		public static bool FindHotKey (ustring text, Rune hotKeySpecifier, bool firstUpperCase, out int hotPos, out Key hotKey)
		{
			if (ustring.IsNullOrEmpty (text) || hotKeySpecifier == (Rune)0xFFFF) {
				hotPos = -1;
				hotKey = Key.Unknown;
				return false;
			}

			Rune hot_key = (Rune)0;
			int hot_pos = -1;

			// Use first hot_key char passed into 'hotKey'.
			// TODO: Ignore hot_key of two are provided
			// TODO: Do not support non-alphanumeric chars that can't be typed
			int i = 0;
			foreach (Rune c in text) {
				if ((char)c != 0xFFFD) {
					if (c == hotKeySpecifier) {
						hot_pos = i;
					} else if (hot_pos > -1) {
						hot_key = c;
						break;
					}
				}
				i++;
			}


			// Legacy support - use first upper case char if the specifier was not found
			if (hot_pos == -1 && firstUpperCase) {
				i = 0;
				foreach (Rune c in text) {
					if ((char)c != 0xFFFD) {
						if (Rune.IsUpper (c)) {
							hot_key = c;
							hot_pos = i;
							break;
						}
					}
					i++;
				}
			}

			if (hot_key != (Rune)0 && hot_pos != -1) {
				hotPos = hot_pos;

				if (hot_key.IsValid && char.IsLetterOrDigit ((char)hot_key)) {
					hotKey = (Key)char.ToUpperInvariant ((char)hot_key);
					return true;
				}
			}

			hotPos = -1;
			hotKey = Key.Unknown;
			return false;
		}

		public static ustring ReplaceHotKeyWithTag (ustring text, int hotPos)
		{
			// Set the high bit
			var runes = text.ToRuneList ();
			if (Rune.IsLetterOrNumber (runes [hotPos])) {
				runes [hotPos] = new Rune ((uint)runes [hotPos] | 0x100000);
			}
			return ustring.Make (runes);
		}

		/// <summary>
		/// Removes the hotkey specifier from text.
		/// </summary>
		/// <param name="text">The text to manipulate.</param>
		/// <param name="hotKeySpecifier">The hot-key specifier (e.g. '_') to look for.</param>
		/// <param name="hotPos">Returns the postion of the hot-key in the text. -1 if not found.</param>
		/// <returns>The input text with the hotkey specifier ('_') removed.</returns>
		public static ustring RemoveHotKeySpecifier (ustring text, int hotPos, Rune hotKeySpecifier)
		{
			if (ustring.IsNullOrEmpty (text)) {
				return text;
			}

			// Scan 
			ustring start = ustring.Empty;
			int i = 0;
			foreach (Rune c in text) {
				if (c == hotKeySpecifier && i == hotPos) {
					i++;
					continue;
				}
				start += ustring.Make (c);
				i++;
			}
			return start;
		}

		/// <summary>
		/// Formats a single line of text with a hot-key and <see cref="Alignment"/>.
		/// </summary>
		/// <param name="shown_text">The text to align.</param>
		/// <param name="width">The maximum width for the text.</param>
		/// <param name="hot_pos">The hot-key position before reformatting.</param>
		/// <param name="c_hot_pos">The hot-key position after reformatting.</param>
		/// <param name="textAlignment">The <see cref="Alignment"/> to align to.</param>
		/// <returns>The aligned text.</returns>
		public static ustring GetAlignedText (ustring shown_text, int width, int hot_pos, out int c_hot_pos, TextAlignment textAlignment)
		{
			int start;
			var caption = shown_text;
			c_hot_pos = hot_pos;

			if (width > shown_text.RuneCount + 1) {
				switch (textAlignment) {
				case TextAlignment.Left:
					caption += new string (' ', width - caption.RuneCount);
					break;
				case TextAlignment.Right:
					start = width - caption.RuneCount;
					caption = $"{new string (' ', width - caption.RuneCount)}{caption}";
					if (c_hot_pos > -1) {
						c_hot_pos += start;
					}
					break;
				case TextAlignment.Centered:
					start = width / 2 - caption.RuneCount / 2;
					caption = $"{new string (' ', start)}{caption}{new string (' ', width - caption.RuneCount - start)}";
					if (c_hot_pos > -1) {
						c_hot_pos += start;
					}
					break;
				case TextAlignment.Justified:
					var words = caption.Split (" ");
					var wLen = GetWordsLength (words, c_hot_pos, out int runeCount, out int w_hot_pos);
					var space = (width - runeCount) / (caption.RuneCount - wLen);
					caption = "";
					for (int i = 0; i < words.Length; i++) {
						if (i == words.Length - 1) {
							caption += new string (' ', width - caption.RuneCount - 1);
							caption += words [i];
						} else {
							caption += words [i];
						}
						if (i < words.Length - 1) {
							caption += new string (' ', space);
						}
					}
					if (c_hot_pos > -1) {
						c_hot_pos += w_hot_pos * space - space - w_hot_pos + 1;
					}
					break;
				}
			}

			return caption;
		}

		static int GetWordsLength (ustring [] words, int hotPos, out int runeCount, out int wordHotPos)
		{
			int length = 0;
			int rCount = 0;
			int wHotPos = -1;
			for (int i = 0; i < words.Length; i++) {
				if (wHotPos == -1 && rCount + words [i].RuneCount >= hotPos)
					wHotPos = i;
				length += words [i].Length;
				rCount += words [i].RuneCount;
			}
			if (wHotPos == -1 && hotPos > -1)
				wHotPos = words.Length;
			runeCount = rCount;
			wordHotPos = wHotPos;
			return length;
		}
	}
}
