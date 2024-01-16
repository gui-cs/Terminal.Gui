using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Text alignment enumeration, controls how text is displayed.
	/// </summary>
	public enum TextAlignment {
		/// <summary>
		/// The text will be left-aligned.
		/// </summary>
		Left,
		/// <summary>
		/// The text will be right-aligned.
		/// </summary>
		Right,
		/// <summary>
		/// The text will be centered horizontally.
		/// </summary>
		Centered,
		/// <summary>
		/// The text will be justified (spaces will be added to existing spaces such that
		/// the text fills the container horizontally).
		/// </summary>
		Justified
	}

	/// <summary>
	/// Vertical text alignment enumeration, controls how text is displayed.
	/// </summary>
	public enum VerticalTextAlignment {
		/// <summary>
		/// The text will be top-aligned.
		/// </summary>
		Top,
		/// <summary>
		/// The text will be bottom-aligned.
		/// </summary>
		Bottom,
		/// <summary>
		/// The text will centered vertically.
		/// </summary>
		Middle,
		/// <summary>
		/// The text will be justified (spaces will be added to existing spaces such that
		/// the text fills the container vertically).
		/// </summary>
		Justified
	}

	/// <summary>
	/// Text direction enumeration, controls how text is displayed.
	/// </summary>
	/// <remarks>
	/// <para>TextDirection  [H] = Horizontal  [V] = Vertical</para>
	/// <table>
	///   <tr>
	///     <th>TextDirection</th>
	///     <th>Description</th>
	///   </tr>
	///   <tr>
	///     <td>LeftRight_TopBottom [H]</td>
	///     <td>Normal</td>
	///   </tr>
	///   <tr>
	///     <td>TopBottom_LeftRight [V]</td>
	///     <td>Normal</td>
	///   </tr>
	///   <tr>
	///     <td>RightLeft_TopBottom [H]</td>
	///     <td>Invert Text</td>
	///   </tr>
	///   <tr>
	///     <td>TopBottom_RightLeft [V]</td>
	///     <td>Invert Lines</td>
	///   </tr>
	///   <tr>
	///     <td>LeftRight_BottomTop [H]</td>
	///     <td>Invert Lines</td>
	///   </tr>
	///   <tr>
	///     <td>BottomTop_LeftRight [V]</td>
	///     <td>Invert Text</td>
	///   </tr>
	///   <tr>
	///     <td>RightLeft_BottomTop [H]</td>
	///     <td>Invert Text + Invert Lines</td>
	///   </tr>
	///   <tr>
	///     <td>BottomTop_RightLeft [V]</td>
	///     <td>Invert Text + Invert Lines</td>
	///   </tr>
	/// </table>
	/// </remarks>
	public enum TextDirection {
		/// <summary>
		/// Normal horizontal direction.
		/// <code>HELLO<br/>WORLD</code>
		/// </summary>
		LeftRight_TopBottom,
		/// <summary>
		/// Normal vertical direction.
		/// <code>H W<br/>E O<br/>L R<br/>L L<br/>O D</code>
		/// </summary>
		TopBottom_LeftRight,
		/// <summary>
		/// This is a horizontal direction. <br/> RTL
		/// <code>OLLEH<br/>DLROW</code>
		/// </summary>
		RightLeft_TopBottom,
		/// <summary>
		/// This is a vertical direction.
		/// <code>W H<br/>O E<br/>R L<br/>L L<br/>D O</code>
		/// </summary>
		TopBottom_RightLeft,
		/// <summary>
		/// This is a horizontal direction.
		/// <code>WORLD<br/>HELLO</code>
		/// </summary>
		LeftRight_BottomTop,
		/// <summary>
		/// This is a vertical direction.
		/// <code>O D<br/>L L<br/>L R<br/>E O<br/>H W</code>
		/// </summary>
		BottomTop_LeftRight,
		/// <summary>
		/// This is a horizontal direction.
		/// <code>DLROW<br/>OLLEH</code>
		/// </summary>
		RightLeft_BottomTop,
		/// <summary>
		/// This is a vertical direction.
		/// <code>D O<br/>L L<br/>R L<br/>O E<br/>W H</code>
		/// </summary>
		BottomTop_RightLeft
	}

	/// <summary>
	/// Provides text formatting. Supports <see cref="View.HotKey"/>s, horizontal alignment, vertical alignment, multiple lines, and word-based line wrap.
	/// </summary>
	public class TextFormatter {

		#region Static Members

		static string StripCRLF (string str, bool keepNewLine = false)
		{
			var runes = str.ToRuneList ();
			for (int i = 0; i < runes.Count; i++) {
				switch ((char)runes [i].Value) {
				case '\n':
					if (!keepNewLine) {
						runes.RemoveAt (i);
					}
					break;

				case '\r':
					if ((i + 1) < runes.Count && runes [i + 1].Value == '\n') {
						runes.RemoveAt (i);
						if (!keepNewLine) {
							runes.RemoveAt (i);
						}
						i++;
					} else {
						if (!keepNewLine) {
							runes.RemoveAt (i);
						}
					}
					break;
				}
			}
			return StringExtensions.ToString (runes);
		}
		static string ReplaceCRLFWithSpace (string str)
		{
			var runes = str.ToRuneList ();
			for (int i = 0; i < runes.Count; i++) {
				switch (runes [i].Value) {
				case '\n':
					runes [i] = (Rune)' ';
					break;

				case '\r':
					if ((i + 1) < runes.Count && runes [i + 1].Value == '\n') {
						runes [i] = (Rune)' ';
						runes.RemoveAt (i + 1);
						i++;
					} else {
						runes [i] = (Rune)' ';
					}
					break;
				}
			}
			return StringExtensions.ToString (runes);
		}

		static string ReplaceTABWithSpaces (string str, int tabWidth)
		{
			if (tabWidth == 0) {
				return str.Replace ("\t", "");
			}

			return str.Replace ("\t", new string (' ', tabWidth));
		}

		/// <summary>
		/// Splits all newlines in the <paramref name="text"/> into a list
		/// and supports both CRLF and LF, preserving the ending newline.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>A list of text without the newline characters.</returns>
		public static List<string> SplitNewLine (string text)
		{
			var runes = text.ToRuneList ();
			var lines = new List<string> ();
			var start = 0;
			var end = 0;

			for (int i = 0; i < runes.Count; i++) {
				end = i;
				switch (runes [i].Value) {
				case '\n':
					lines.Add (StringExtensions.ToString (runes.GetRange (start, end - start)));
					i++;
					start = i;
					break;

				case '\r':
					if ((i + 1) < runes.Count && runes [i + 1].Value == '\n') {
						lines.Add (StringExtensions.ToString (runes.GetRange (start, end - start)));
						i += 2;
						start = i;
					} else {
						lines.Add (StringExtensions.ToString (runes.GetRange (start, end - start)));
						i++;
						start = i;
					}
					break;
				}
			}
			if (runes.Count > 0 && lines.Count == 0) {
				lines.Add (StringExtensions.ToString (runes));
			} else if (runes.Count > 0 && start < runes.Count) {
				lines.Add (StringExtensions.ToString (runes.GetRange (start, runes.Count - start)));
			} else {
				lines.Add ("");
			}
			return lines;
		}

		/// <summary>
		/// Adds trailing whitespace or truncates <paramref name="text"/>
		/// so that it fits exactly <paramref name="width"/> console units.
		/// Note that some unicode characters take 2+ columns
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public static string ClipOrPad (string text, int width)
		{
			if (string.IsNullOrEmpty (text))
				return text;

			// if value is not wide enough
			if (text.EnumerateRunes ().Sum (c => c.GetColumns ()) < width) {

				// pad it out with spaces to the given alignment
				int toPad = width - (text.EnumerateRunes ().Sum (c => c.GetColumns ()));

				return text + new string (' ', toPad);
			}

			// value is too wide
			return new string (text.TakeWhile (c => (width -= ((Rune)c).GetColumns ()) >= 0).ToArray ());
		}

		/// <summary>
		/// Formats the provided text to fit within the width provided using word wrapping.
		/// </summary>
		/// <param name="text">The text to word wrap</param>
		/// <param name="width">The number of columns to constrain the text to</param>
		/// <param name="preserveTrailingSpaces">If <see langword="true"/> trailing spaces at the end of wrapped lines will be preserved.
		///  If <see langword="false"/>, trailing spaces at the end of wrapped lines will be trimmed.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>A list of word wrapped lines.</returns>
		/// <remarks>
		/// <para>
		/// This method does not do any justification.
		/// </para>
		/// <para>
		/// This method strips Newline ('\n' and '\r\n') sequences before processing.
		/// </para>
		/// <para>
		/// If <paramref name="preserveTrailingSpaces"/> is <see langword="false"/> at most one space will be preserved at the end of the last line.
		/// </para>
		/// </remarks>
		public static List<string> WordWrapText (string text, int width, bool preserveTrailingSpaces = false, int tabWidth = 0,
			TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}

			int start = 0, end;
			var lines = new List<string> ();

			if (string.IsNullOrEmpty (text)) {
				return lines;
			}

			var runes = StripCRLF (text).ToRuneList ();
			if (preserveTrailingSpaces) {
				while ((end = start) < runes.Count) {
					end = GetNextWhiteSpace (start, width, out bool incomplete);
					if (end == 0 && incomplete) {
						start = text.GetRuneCount ();
						break;
					}
					lines.Add (StringExtensions.ToString (runes.GetRange (start, end - start)));
					start = end;
					if (incomplete) {
						start = text.GetRuneCount ();
						break;
					}
				}
			} else {
				if (IsHorizontalDirection (textDirection)) {
					//if (GetLengthThatFits (runes.GetRange (start, runes.Count - start), width) > 0) {
					//	// while there's still runes left and end is not past end...
					//	while (start < runes.Count &&
					//		(end = start + Math.Max (GetLengthThatFits (runes.GetRange (start, runes.Count - start), width) - 1, 0)) < runes.Count) {
					//		// end now points to start + LengthThatFits
					//		// Walk back over trailing spaces
					//		while (runes [end] == ' ' && end > start) {
					//			end--;
					//		}
					//		// end now points to start + LengthThatFits - any trailing spaces; start saving new line
					//		var line = runes.GetRange (start, end - start + 1);

					//		if (end == start && width > 1) {
					//			// it was all trailing spaces; now walk forward to next non-space
					//			do {
					//				start++;
					//			} while (start < runes.Count && runes [start] == ' ');

					//			// start now points to first non-space we haven't seen yet or we're done
					//			if (start < runes.Count) {
					//				// we're not done. we have remaining = width - line.Count columns left; 
					//				var remaining = width - line.Count;
					//				if (remaining > 1) {
					//					// add a space for all the spaces we walked over 
					//					line.Add (' ');
					//				}
					//				var count = GetLengthThatFits (runes.GetRange (start, runes.Count - start), width - line.Count);

					//				// [start..count] now has rest of line
					//				line.AddRange (runes.GetRange (start, count));
					//				start += count;
					//			}
					//		} else {
					//			start += line.Count;
					//		}

					//		//// if the previous line was just a ' ' and the new line is just a ' '
					//		//// don't add new line
					//		//if (line [0] == ' ' && (lines.Count > 0 && lines [lines.Count - 1] [0] == ' ')) {
					//		//} else {
					//		//}
					//		lines.Add (string.Make (line));

					//		// move forward to next non-space
					//		while (width > 1 && start < runes.Count && runes [start] == ' ') {
					//			start++;
					//		}
					//	}
					//}

					while ((end = start + GetLengthThatFits (runes.GetRange (start, runes.Count - start), width, tabWidth)) < runes.Count) {
						while (runes [end].Value != ' ' && end > start)
							end--;
						if (end == start)
							end = start + GetLengthThatFits (runes.GetRange (end, runes.Count - end), width, tabWidth);
						var str = StringExtensions.ToString (runes.GetRange (start, end - start));
						if (end > start && GetRuneWidth (str, tabWidth) <= width) {
							lines.Add (str);
							start = end;
							if (runes [end].Value == ' ') {
								start++;
							}
						} else {
							end++;
							start = end;
						}
					}

				} else {
					while ((end = start + width) < runes.Count) {
						while (runes [end].Value != ' ' && end > start) {
							end--;
						}
						if (end == start) {
							end = start + width;
						}
						var zeroLength = 0;
						for (int i = end; i < runes.Count - start; i++) {
							var r = runes [i];
							if (r.GetColumns () == 0) {
								zeroLength++;
							} else {
								break;
							}
						}
						lines.Add (StringExtensions.ToString (runes.GetRange (start, end - start + zeroLength)));
						end += zeroLength;
						start = end;
						if (runes [end].Value == ' ') {
							start++;
						}
					}
				}
			}

			int GetNextWhiteSpace (int from, int cWidth, out bool incomplete, int cLength = 0)
			{
				var lastFrom = from;
				var to = from;
				var length = cLength;
				incomplete = false;

				while (length < cWidth && to < runes.Count) {
					var rune = runes [to];
					if (IsHorizontalDirection (textDirection)) {
						length += rune.GetColumns ();
					} else {
						length++;
					}
					if (length > cWidth) {
						if (to >= runes.Count || (length > 1 && cWidth <= 1)) {
							incomplete = true;
						}
						return to;
					}
					if (rune.Value == ' ') {
						if (length == cWidth) {
							return to + 1;
						} else if (length > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
						}
					} else if (rune.Value == '\t') {
						length += tabWidth + 1;
						if (length == tabWidth && tabWidth > cWidth) {
							return to + 1;
						} else if (length > cWidth && tabWidth > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
						}
					}
					to++;
				}
				if (cLength > 0 && to < runes.Count && runes [to].Value != ' ' && runes [to].Value != '\t') {
					return from;
				} else if (cLength > 0 && to < runes.Count && (runes [to].Value == ' ' || runes [to].Value == '\t')) {
					return lastFrom;
				} else {
					return to;
				}
			}

			if (start < text.GetRuneCount ()) {
				var str = ReplaceTABWithSpaces (StringExtensions.ToString (runes.GetRange (start, runes.Count - start)), tabWidth);
				if (IsVerticalDirection (textDirection) || preserveTrailingSpaces || (!preserveTrailingSpaces && str.GetColumns () <= width)) {
					lines.Add (str);
				}
			}

			return lines;
		}

		/// <summary>
		/// Justifies text within a specified width. 
		/// </summary>
		/// <param name="text">The text to justify.</param>
		/// <param name="width">The number of columns to clip the text to. Text longer than <paramref name="width"/> will be clipped.</param>
		/// <param name="talign">Alignment.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns>Justified and clipped text.</returns>
		public static string ClipAndJustify (string text, int width, TextAlignment talign, TextDirection textDirection = TextDirection.LeftRight_TopBottom, int tabWidth = 0)
		{
			return ClipAndJustify (text, width, talign == TextAlignment.Justified, textDirection, tabWidth);
		}

		/// <summary>
		/// Justifies text within a specified width. 
		/// </summary>
		/// <param name="text">The text to justify.</param>
		/// <param name="width">The number of columns to clip the text to. Text longer than <paramref name="width"/> will be clipped.</param>
		/// <param name="justify">Justify.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns>Justified and clipped text.</returns>
		public static string ClipAndJustify (string text, int width, bool justify, TextDirection textDirection = TextDirection.LeftRight_TopBottom, int tabWidth = 0)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			text = ReplaceTABWithSpaces (text, tabWidth);
			var runes = text.ToRuneList ();
			int slen = runes.Count;
			if (slen > width) {
				if (IsHorizontalDirection (textDirection)) {
					return StringExtensions.ToString (runes.GetRange (0, GetLengthThatFits (text, width, tabWidth)));
				} else {
					var zeroLength = runes.Sum (r => r.GetColumns () == 0 ? 1 : 0);
					return StringExtensions.ToString (runes.GetRange (0, width + zeroLength));
				}
			} else {
				if (justify) {
					return Justify (text, width, ' ', textDirection, tabWidth);
				} else if (IsHorizontalDirection (textDirection) && GetRuneWidth (text, tabWidth) > width) {
					return StringExtensions.ToString (runes.GetRange (0, GetLengthThatFits (text, width, tabWidth)));
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
		/// <param name="textDirection">The text direction.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns>The justified text.</returns>
		public static string Justify (string text, int width, char spaceChar = ' ', TextDirection textDirection = TextDirection.LeftRight_TopBottom, int tabWidth = 0)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			text = ReplaceTABWithSpaces (text, tabWidth);
			var words = text.Split (' ');
			int textCount;
			if (IsHorizontalDirection (textDirection)) {
				textCount = words.Sum (arg => GetRuneWidth (arg, tabWidth));
			} else {
				textCount = words.Sum (arg => arg.GetRuneCount ());
			}
			var spaces = words.Length > 1 ? (width - textCount) / (words.Length - 1) : 0;
			var extras = words.Length > 1 ? (width - textCount) % (words.Length - 1) : 0;

			var s = new System.Text.StringBuilder ();
			for (int w = 0; w < words.Length; w++) {
				var x = words [w];
				s.Append (x);
				if (w + 1 < words.Length)
					for (int i = 0; i < spaces; i++)
						s.Append (spaceChar);
				if (extras > 0) {
					for (int i = 0; i < 1; i++)
						s.Append (spaceChar);
					extras--;
				}
				if (w + 1 == words.Length - 1) {
					for (int i = 0; i < extras; i++)
						s.Append (spaceChar);
				}
			}
			return s.ToString ();
		}

		//static char [] whitespace = new char [] { ' ', '\t' };

		/// <summary>
		/// Reformats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
		/// <param name="talign">Specifies how the text will be aligned horizontally.</param>
		/// <param name="wordWrap">If <see langword="true"/>, the text will be wrapped to new lines no longer than <paramref name="width"/>.	
		/// If <see langword="false"/>, forces text to fit a single line. Line breaks are converted to spaces. The text will be clipped to <paramref name="width"/>.</param>
		/// <param name="preserveTrailingSpaces">If <see langword="true"/> trailing spaces at the end of wrapped lines will be preserved.
		///  If <see langword="false"/>, trailing spaces at the end of wrapped lines will be trimmed.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <param name="multiLine">If <see langword="true"/> new lines are allowed.</param>
		/// <returns>A list of word wrapped lines.</returns>
		/// <remarks>
		/// <para>
		/// An empty <paramref name="text"/> string will result in one empty line.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is 0, a single, empty line will be returned.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible. 
		/// </para>
		/// </remarks>
		public static List<string> Format (string text, int width, TextAlignment talign, bool wordWrap, bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom, bool multiLine = false)
		{
			return Format (text, width, talign == TextAlignment.Justified, wordWrap, preserveTrailingSpaces, tabWidth, textDirection, multiLine);
		}

		/// <summary>
		/// Reformats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
		/// <param name="justify">Specifies whether the text should be justified.</param>
		/// <param name="wordWrap">If <see langword="true"/>, the text will be wrapped to new lines no longer than <paramref name="width"/>.	
		/// If <see langword="false"/>, forces text to fit a single line. Line breaks are converted to spaces. The text will be clipped to <paramref name="width"/>.</param>
		/// <param name="preserveTrailingSpaces">If <see langword="true"/> trailing spaces at the end of wrapped lines will be preserved.
		///  If <see langword="false"/>, trailing spaces at the end of wrapped lines will be trimmed.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <param name="multiLine">If <see langword="true"/> new lines are allowed.</param>
		/// <returns>A list of word wrapped lines.</returns>
		/// <remarks>
		/// <para>
		/// An empty <paramref name="text"/> string will result in one empty line.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is 0, a single, empty line will be returned.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible. 
		/// </para>
		/// </remarks>
		public static List<string> Format (string text, int width, bool justify, bool wordWrap,
			bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom, bool multiLine = false)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("width cannot be negative");
			}
			List<string> lineResult = new List<string> ();

			if (string.IsNullOrEmpty (text) || width == 0) {
				lineResult.Add (string.Empty);
				return lineResult;
			}

			if (!wordWrap) {
				text = ReplaceTABWithSpaces (text, tabWidth);
				if (multiLine) {
					string [] lines = null;
					if (text.Contains ("\r\n")) {
						lines = text.Split ("\r\n");
					} else if (text.Contains ('\n')) {
						lines = text.Split ('\n');
					}
					if (lines == null) {
						lines = new [] { text };
					}
					foreach (var line in lines) {
						lineResult.Add (ClipAndJustify (line, width, justify, textDirection, tabWidth));
					}
					return lineResult;
				} else {
					text = ReplaceCRLFWithSpace (text);
					lineResult.Add (ClipAndJustify (text, width, justify, textDirection, tabWidth));
					return lineResult;
				}
			}

			var runes = StripCRLF (text, true).ToRuneList ();
			int runeCount = runes.Count;
			int lp = 0;
			for (int i = 0; i < runeCount; i++) {
				Rune c = runes [i];
				if (c.Value == '\n') {
					var wrappedLines = WordWrapText (StringExtensions.ToString (runes.GetRange (lp, i - lp)), width, preserveTrailingSpaces, tabWidth, textDirection);
					foreach (var line in wrappedLines) {
						lineResult.Add (ClipAndJustify (line, width, justify, textDirection, tabWidth));
					}
					if (wrappedLines.Count == 0) {
						lineResult.Add (string.Empty);
					}
					lp = i + 1;
				}
			}
			foreach (var line in WordWrapText (StringExtensions.ToString (runes.GetRange (lp, runeCount - lp)), width, preserveTrailingSpaces, tabWidth, textDirection)) {
				lineResult.Add (ClipAndJustify (line, width, justify, textDirection, tabWidth));
			}

			return lineResult;
		}

		/// <summary>
		/// Computes the number of lines needed to render the specified text given the width.
		/// </summary>
		/// <returns>Number of lines.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="width">The minimum width for the text.</param>
		public static int MaxLines (string text, int width)
		{
			var result = TextFormatter.Format (text, width, false, true);
			return result.Count;
		}

		/// <summary>
		/// Computes the maximum width needed to render the text (single line or multiple lines, word wrapped) given 
		/// a number of columns to constrain the text to.
		/// </summary>
		/// <returns>Width of the longest line after formatting the text constrained by <paramref name="maxColumns"/>.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="maxColumns">The number of columns to constrain the text to for formatting.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		public static int MaxWidth (string text, int maxColumns, int tabWidth = 0)
		{
			var result = TextFormatter.Format (text: text, width: maxColumns, justify: false, wordWrap: true);
			var max = 0;
			result.ForEach (s => {
				var m = 0;
				s.ToRuneList ().ForEach (r => m += GetRuneWidth (r, tabWidth));
				if (m > max) {
					max = m;
				}
			});
			return max;
		}

		/// <summary>
		/// Returns the width of the widest line in the text, accounting for wide-glyphs (uses <see cref="StringExtensions.GetColumns"/>).
		/// <paramref name="text"/> if it contains newlines.
		/// </summary>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns>The length of the longest line.</returns>
		public static int MaxWidthLine (string text, int tabWidth = 0)
		{
			var result = TextFormatter.SplitNewLine (text);
			return result.Max (x => GetRuneWidth (x, tabWidth));
		}

		/// <summary>
		/// Gets the maximum characters width from the list based on the <paramref name="startIndex"/>
		/// and the <paramref name="length"/>.
		/// </summary>
		/// <param name="lines">The lines.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="length">The length.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns>The maximum characters width.</returns>
		public static int GetSumMaxCharWidth (List<string> lines, int startIndex = -1, int length = -1, int tabWidth = 0)
		{
			var max = 0;
			for (int i = (startIndex == -1 ? 0 : startIndex); i < (length == -1 ? lines.Count : startIndex + length); i++) {
				var runes = lines [i];
				if (runes.Length > 0)
					max += runes.EnumerateRunes ().Max (r => GetRuneWidth (r, tabWidth));
			}
			return max;
		}

		/// <summary>
		/// Gets the maximum characters width from the text based on the <paramref name="startIndex"/>
		/// and the <paramref name="length"/>.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="length">The length.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns>The maximum characters width.</returns>
		public static int GetSumMaxCharWidth (string text, int startIndex = -1, int length = -1, int tabWidth = 0)
		{
			var max = 0;
			var runes = text.ToRunes ();
			for (int i = (startIndex == -1 ? 0 : startIndex); i < (length == -1 ? runes.Length : startIndex + length); i++) {
				max += GetRuneWidth (runes [i], tabWidth);
			}
			return max;
		}

		/// <summary>
		/// Gets the number of the Runes in a <see cref="string"/> that will fit in <paramref name="columns"/>.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="columns">The width.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns>The index of the text that fit the width.</returns>
		public static int GetLengthThatFits (string text, int columns, int tabWidth = 0) => GetLengthThatFits (text?.ToRuneList (), columns, tabWidth);

		/// <summary>
		/// Gets the number of the Runes in a list of Runes that will fit in <paramref name="columns"/>.
		/// </summary>
		/// <param name="runes">The list of runes.</param>
		/// <param name="columns">The width.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns>The index of the last Rune in <paramref name="runes"/> that fit in <paramref name="columns"/>.</returns>
		public static int GetLengthThatFits (List<Rune> runes, int columns, int tabWidth = 0)
		{
			if (runes == null || runes.Count == 0) {
				return 0;
			}

			var runesLength = 0;
			var runeIdx = 0;
			for (; runeIdx < runes.Count; runeIdx++) {
				var runeWidth = GetRuneWidth (runes [runeIdx], tabWidth);
				if (runesLength + runeWidth > columns) {
					break;
				}
				runesLength += runeWidth;
			}
			return runeIdx;
		}

		private static int GetRuneWidth (string str, int tabWidth)
		{
			return GetRuneWidth (str.EnumerateRunes ().ToList (), tabWidth);
		}

		private static int GetRuneWidth (List<Rune> runes, int tabWidth)
		{
			return runes.Sum (r => GetRuneWidth (r, tabWidth));
		}

		private static int GetRuneWidth (Rune rune, int tabWidth)
		{
			var runeWidth = rune.GetColumns ();
			if (rune.Value == '\t') {
				return tabWidth;
			}
			if (runeWidth < 0 || runeWidth > 0) {
				return Math.Max (runeWidth, 1);
			}

			return runeWidth;
		}

		/// <summary>
		/// Gets the index position from the list based on the <paramref name="width"/>.
		/// </summary>
		/// <param name="lines">The lines.</param>
		/// <param name="width">The width.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns>The index of the list that fit the width.</returns>
		public static int GetMaxColsForWidth (List<string> lines, int width, int tabWidth = 0)
		{
			var runesLength = 0;
			var lineIdx = 0;
			for (; lineIdx < lines.Count; lineIdx++) {
				var runes = lines [lineIdx].ToRuneList ();
				var maxRruneWidth = runes.Count > 0
					? runes.Max (r => GetRuneWidth (r, tabWidth)) : 1;
				if (runesLength + maxRruneWidth > width) {
					break;
				}
				runesLength += maxRruneWidth;
			}
			return lineIdx;
		}

		/// <summary>
		///  Calculates the rectangle required to hold text, assuming no word wrapping or justification.
		/// </summary>
		/// <param name="x">The x location of the rectangle</param>
		/// <param name="y">The y location of the rectangle</param>
		/// <param name="text">The text to measure</param>
		/// <param name="direction">The text direction.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <returns></returns>
		public static Rect CalcRect (int x, int y, string text, TextDirection direction = TextDirection.LeftRight_TopBottom, int tabWidth = 0)
		{
			if (string.IsNullOrEmpty (text)) {
				return new Rect (new Point (x, y), Size.Empty);
			}

			int w, h;

			if (IsHorizontalDirection (direction)) {
				int mw = 0;
				int ml = 1;

				int cols = 0;
				foreach (var rune in text.EnumerateRunes ()) {
					if (rune.Value == '\n') {
						ml++;
						if (cols > mw) {
							mw = cols;
						}
						cols = 0;
					} else if (rune.Value != '\r') {
						cols++;
						var rw = 0;
						if (rune.Value == '\t') {
							rw += tabWidth - 1;
						} else {
							rw = ((Rune)rune).GetColumns ();
							if (rw > 0) {
								rw--;
							} else if (rw == 0) {
								cols--;
							}
						}
						cols += rw;
					}
				}
				if (cols > mw) {
					mw = cols;
				}
				w = mw;
				h = ml;
			} else {
				int vw = 1, cw = 1;
				int vh = 0;

				int rows = 0;
				foreach (var rune in text.EnumerateRunes ()) {
					if (rune.Value == '\n') {
						vw++;
						if (rows > vh) {
							vh = rows;
						}
						rows = 0;
						cw = 1;
					} else if (rune.Value != '\r') {
						rows++;
						var rw = 0;
						if (rune.Value == '\t') {
							rw += tabWidth - 1;
							rows += rw;
						} else {
							rw = ((Rune)rune).GetColumns ();
							if (rw == 0) {
								rows--;
							} else if (cw < rw) {
								cw = rw;
								vw++;
							}
						}
					}
				}
				if (rows > vh) {
					vh = rows;
				}
				w = vw;
				h = vh;
			}

			return new Rect (x, y, w, h);
		}

		/// <summary>
		/// Finds the HotKey and its location in text. 
		/// </summary>
		/// <param name="text">The text to look in.</param>
		/// <param name="hotKeySpecifier">The HotKey specifier (e.g. '_') to look for.</param>
		/// <param name="hotPos">Outputs the Rune index into <c>text</c>.</param>
		/// <param name="hotKey">Outputs the hotKey. <see cref="Key.Empty"/> if not found.</param>
		/// <param name="firstUpperCase">If <c>true</c> the legacy behavior of identifying the
		/// first upper case character as the HotKey will be enabled.
		/// Regardless of the value of this parameter, <c>hotKeySpecifier</c> takes precedence.
		/// Defaults to <see langword="false"/>.</param>
		/// <returns><c>true</c> if a HotKey was found; <c>false</c> otherwise.</returns>
		public static bool FindHotKey (string text, Rune hotKeySpecifier, out int hotPos, out Key hotKey, bool firstUpperCase = false)
		{
			if (string.IsNullOrEmpty (text) || hotKeySpecifier == (Rune)0xFFFF) {
				hotPos = -1;
				hotKey = KeyCode.Null;
				return false;
			}

			Rune hot_key = (Rune)0;
			int hot_pos = -1;

			// Use first hot_key char passed into 'hotKey'.
			// TODO: Ignore hot_key of two are provided
			// TODO: Do not support non-alphanumeric chars that can't be typed
			int i = 0;
			foreach (Rune c in text.EnumerateRunes ()) {
				if ((char)c.Value != 0xFFFD) {
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
				foreach (Rune c in text.EnumerateRunes ()) {
					if ((char)c.Value != 0xFFFD) {
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

				var newHotKey = (KeyCode)hot_key.Value;
				if (newHotKey != KeyCode.Null && !(newHotKey == KeyCode.Space || Rune.IsControl (hot_key))) {
					if ((newHotKey & ~KeyCode.Space) is >= KeyCode.A and <= KeyCode.Z) {
						newHotKey &= ~KeyCode.Space;
					}
					hotKey = newHotKey;
					return true;
				}
			}

			hotPos = -1;
			hotKey = KeyCode.Null;
			return false;
		}

		/// <summary>
		/// Replaces the Rune at the index specified by the <c>hotPos</c> parameter with a tag identifying 
		/// it as the hotkey.
		/// </summary>
		/// <param name="text">The text to tag the hotkey in.</param>
		/// <param name="hotPos">The Rune index of the hotkey in <c>text</c>.</param>
		/// <returns>The text with the hotkey tagged.</returns>
		/// <remarks>
		/// The returned string will not render correctly without first un-doing the tag. To undo the tag, search for 
		/// </remarks>
		public string ReplaceHotKeyWithTag (string text, int hotPos)
		{
			// Set the high bit
			var runes = text.ToRuneList ();
			if (Rune.IsLetterOrDigit (runes [hotPos])) {
				runes [hotPos] = new Rune ((uint)runes [hotPos].Value);
			}
			return StringExtensions.ToString (runes);
		}

		/// <summary>
		/// Removes the hotkey specifier from text.
		/// </summary>
		/// <param name="text">The text to manipulate.</param>
		/// <param name="hotKeySpecifier">The hot-key specifier (e.g. '_') to look for.</param>
		/// <param name="hotPos">Returns the position of the hot-key in the text. -1 if not found.</param>
		/// <returns>The input text with the hotkey specifier ('_') removed.</returns>
		public static string RemoveHotKeySpecifier (string text, int hotPos, Rune hotKeySpecifier)
		{
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			// Scan 
			string start = string.Empty;
			int i = 0;
			foreach (Rune c in text) {
				if (c == hotKeySpecifier && i == hotPos) {
					i++;
					continue;
				}
				start += c;
				i++;
			}
			return start;
		}
		#endregion // Static Members

		List<string> _lines = new List<string> ();
		string _text = null;
		TextAlignment _textAlignment;
		VerticalTextAlignment _textVerticalAlignment;
		TextDirection _textDirection;
		Key _hotKey = new Key ();
		int _hotKeyPos = -1;
		Size _size;
		private bool _autoSize;
		private bool _preserveTrailingSpaces;
		private int _tabWidth = 4;
		private bool _wordWrap = true;
		private bool _multiLine;

		/// <summary>
		/// Event invoked when the <see cref="HotKey"/> is changed.
		/// </summary>
		public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

		/// <summary>
		///   The text to be displayed. This string is never modified.
		/// </summary>
		public virtual string Text {
			get => _text;
			set {
				var textWasNull = _text == null && value != null;
				_text = EnableNeedsFormat (value);

				if ((AutoSize && Alignment != TextAlignment.Justified && VerticalAlignment != VerticalTextAlignment.Justified) || (textWasNull && Size.IsEmpty)) {
					Size = CalcRect (0, 0, _text, Direction, TabWidth).Size;
				}

				//if (_text != null && _text.GetRuneCount () > 0 && (Size.Width == 0 || Size.Height == 0 || Size.Width != _text.GetColumns ())) {
				//	// Provide a default size (width = length of longest line, height = 1)
				//	// TODO: It might makes more sense for the default to be width = length of first line?
				//	Size = new Size (TextFormatter.MaxWidth (Text, int.MaxValue), 1);
				//}
			}
		}

		/// <summary>
		/// Gets or sets whether the <see cref="Size"/> should be automatically changed to fit the <see cref="Text"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Used by <see cref="View.AutoSize"/> to resize the view's <see cref="View.Bounds"/> to fit <see cref="Size"/>.
		/// </para>
		/// <para>
		/// AutoSize is ignored if <see cref="TextAlignment.Justified"/> and <see cref="VerticalTextAlignment.Justified"/> are used.
		/// </para>
		/// </remarks>
		public bool AutoSize {
			get => _autoSize;
			set {
				_autoSize = EnableNeedsFormat (value);
				if (_autoSize && Alignment != TextAlignment.Justified && VerticalAlignment != VerticalTextAlignment.Justified) {
					Size = CalcRect (0, 0, _text, Direction, TabWidth).Size;
				}
			}
		}

		/// <summary>
		/// Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
		/// or not when <see cref="TextFormatter.WordWrap"/> is enabled. 
		/// If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when 
		/// <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
		/// </summary>
		public bool PreserveTrailingSpaces {
			get => _preserveTrailingSpaces;
			set => _preserveTrailingSpaces = EnableNeedsFormat (value);
		}

		/// <summary>
		/// Controls the horizontal text-alignment property.
		/// </summary>
		/// <value>The text alignment.</value>
		public TextAlignment Alignment {
			get => _textAlignment;
			set => _textAlignment = EnableNeedsFormat (value);
		}

		/// <summary>
		/// Controls the vertical text-alignment property. 
		/// </summary>
		/// <value>The text vertical alignment.</value>
		public VerticalTextAlignment VerticalAlignment {
			get => _textVerticalAlignment;
			set => _textVerticalAlignment = EnableNeedsFormat (value);
		}

		/// <summary>
		/// Controls the text-direction property. 
		/// </summary>
		/// <value>The text vertical alignment.</value>
		public TextDirection Direction {
			get => _textDirection;
			set {
				_textDirection = EnableNeedsFormat (value);
				if (AutoSize && Alignment != TextAlignment.Justified && VerticalAlignment != VerticalTextAlignment.Justified) {
					Size = CalcRect (0, 0, Text, Direction, TabWidth).Size;
				}
			}
		}

		/// <summary>
		/// Check if it is a horizontal direction
		/// </summary>
		public static bool IsHorizontalDirection (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.LeftRight_TopBottom:
			case TextDirection.LeftRight_BottomTop:
			case TextDirection.RightLeft_TopBottom:
			case TextDirection.RightLeft_BottomTop:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check if it is a vertical direction
		/// </summary>
		public static bool IsVerticalDirection (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.TopBottom_LeftRight:
			case TextDirection.TopBottom_RightLeft:
			case TextDirection.BottomTop_LeftRight:
			case TextDirection.BottomTop_RightLeft:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check if it is Left to Right direction
		/// </summary>
		public static bool IsLeftToRight (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.LeftRight_TopBottom:
			case TextDirection.LeftRight_BottomTop:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check if it is Top to Bottom direction
		/// </summary>
		public static bool IsTopToBottom (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.TopBottom_LeftRight:
			case TextDirection.TopBottom_RightLeft:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Gets or sets whether word wrap will be used to fit <see cref="Text"/> to <see cref="Size"/>.
		/// </summary>
		public bool WordWrap {
			get => _wordWrap;
			set => _wordWrap = EnableNeedsFormat (value);
		}

		/// <summary>
		/// Gets or sets the size <see cref="Text"/> will be constrained to when formatted.
		/// </summary>
		/// <remarks>
		/// Does not return the size of the formatted text but the size that will be used to constrain the text when formatted.
		/// </remarks>
		public Size Size {
			get => _size;
			set {
				if (AutoSize && Alignment != TextAlignment.Justified && VerticalAlignment != VerticalTextAlignment.Justified) {
					_size = EnableNeedsFormat (CalcRect (0, 0, Text, Direction, TabWidth).Size);
				} else {
					_size = EnableNeedsFormat (value);
				}
			}
		}

		/// <summary>
		/// The specifier character for the hot key (e.g. '_'). Set to '\xffff' to disable hot key support for this View instance. The default is '\xffff'.
		/// </summary>
		public Rune HotKeySpecifier { get; set; } = (Rune)0xFFFF;

		/// <summary>
		/// The position in the text of the hot key. The hot key will be rendered using the hot color.
		/// </summary>
		public int HotKeyPos { get => _hotKeyPos; internal set => _hotKeyPos = value; }

		/// <summary>
		/// Gets or sets the hot key. Must be be an upper case letter or digit. Fires the <see cref="HotKeyChanged"/> event.
		/// </summary>
		public Key HotKey {
			get => _hotKey;
			internal set {
				if (_hotKey != value) {
					var oldKey = _hotKey;
					_hotKey = value;
					HotKeyChanged?.Invoke (this, new KeyChangedEventArgs (oldKey, value));
				}
			}
		}

		/// <summary>
		/// Gets the cursor position from <see cref="HotKey"/>. If the <see cref="HotKey"/> is defined, the cursor will be positioned over it.
		/// </summary>
		public int CursorPosition { get; internal set; }

		/// <summary>
		/// Gets the size required to hold the formatted text, given the constraints placed by <see cref="Size"/>.
		/// </summary>
		/// <remarks>
		/// Causes a format, resetting <see cref="NeedsFormat"/>.
		/// </remarks>
		/// <returns></returns>
		public Size GetFormattedSize ()
		{
			var lines = Lines;
			var width = Lines.Max (line => line.GetColumns ());
			var height = Lines.Count;
			return new Size (width, height);
		}

		/// <summary>
		/// Gets the formatted lines.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Upon a 'get' of this property, if the text needs to be formatted (if <see cref="NeedsFormat"/> is <c>true</c>)
		/// <see cref="Format(string, int, bool, bool, bool, int, TextDirection, bool)"/> will be called internally. 
		/// </para>
		/// </remarks>
		public List<string> Lines {
			get {
				// With this check, we protect against subclasses with overrides of Text
				if (string.IsNullOrEmpty (Text) || Size.IsEmpty) {
					_lines = new List<string> {
						string.Empty
					};
					NeedsFormat = false;
					return _lines;
				}

				if (NeedsFormat) {
					var shown_text = _text;
					if (FindHotKey (_text, HotKeySpecifier, out _hotKeyPos, out var newHotKey)) {
						HotKey = newHotKey;
						shown_text = RemoveHotKeySpecifier (Text, _hotKeyPos, HotKeySpecifier);
						shown_text = ReplaceHotKeyWithTag (shown_text, _hotKeyPos);
					}

					if (IsVerticalDirection (Direction)) {
						var colsWidth = GetSumMaxCharWidth (shown_text, 0, 1, TabWidth);
						_lines = Format (shown_text, Size.Height, VerticalAlignment == VerticalTextAlignment.Justified, Size.Width > colsWidth && WordWrap,
							PreserveTrailingSpaces, TabWidth, Direction, MultiLine);
						if (!AutoSize) {
							colsWidth = GetMaxColsForWidth (_lines, Size.Width, TabWidth);
							if (_lines.Count > colsWidth) {
								_lines.RemoveRange (colsWidth, _lines.Count - colsWidth);
							}
						}
					} else {
						_lines = Format (shown_text, Size.Width, Alignment == TextAlignment.Justified, Size.Height > 1 && WordWrap,
							PreserveTrailingSpaces, TabWidth, Direction, MultiLine);
						if (!AutoSize && _lines.Count > Size.Height) {
							_lines.RemoveRange (Size.Height, _lines.Count - Size.Height);
						}
					}

					NeedsFormat = false;
				}
				return _lines;
			}
		}

		/// <summary>
		/// Gets or sets whether the <see cref="TextFormatter"/> needs to format the text. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// If <c>false</c> when Draw is called, the Draw call will be faster.
		/// </para>
		/// <para>
		/// Used by <see cref="Draw(Rect, Attribute, Attribute, Rect, bool, ConsoleDriver)"/>
		/// </para>
		/// <para>
		/// This is set to true when the properties of <see cref="TextFormatter"/> are set.
		/// </para>
		/// </remarks>
		public bool NeedsFormat { get; set; }

		/// <summary>
		/// Gets or sets the number of columns used for a tab.
		/// </summary>
		public int TabWidth {
			get => _tabWidth;
			set => _tabWidth = EnableNeedsFormat (value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether multi line is allowed.
		/// </summary>
		/// <remarks>
		///   Multi line is ignored if <see cref="WordWrap"/> is <see langword="true"/>.
		/// </remarks>
		public bool MultiLine {
			get => _multiLine;
			set {
				_multiLine = EnableNeedsFormat (value);
			}
		}

		private T EnableNeedsFormat<T> (T value)
		{
			NeedsFormat = true;
			return value;
		}

		/// <summary>
		/// Causes the <see cref="TextFormatter"/> to reformat the text. 
		/// </summary>
		/// <returns>The formatted text.</returns>
		public string Format ()
		{
			var sb = new StringBuilder ();
			// Lines_get causes a Format
			foreach (var line in Lines) {
				sb.AppendLine (line);
			}
			return sb.ToString ().TrimEnd (Environment.NewLine.ToCharArray ());
		}

		/// <summary>
		/// Draws the text held by <see cref="TextFormatter"/> to <see cref="ConsoleDriver"/> using the colors specified.
		/// </summary>
		/// <param name="bounds">Specifies the screen-relative location and maximum size for drawing the text.</param>
		/// <param name="normalColor">The color to use for all text except the hotkey</param>
		/// <param name="hotColor">The color to use to draw the hotkey</param>
		/// <param name="containerBounds">Specifies the screen-relative location and maximum container size.</param>
		/// <param name="fillRemaining">Determines if the bounds width will be used (default) or only the text width will be used.</param>
		/// <param name="driver">The console driver currently used by the application.</param>
		public void Draw (Rect bounds, Attribute normalColor, Attribute hotColor, Rect containerBounds = default, bool fillRemaining = true, ConsoleDriver driver = null)
		{
			// With this check, we protect against subclasses with overrides of Text (like Button)
			if (string.IsNullOrEmpty (_text)) {
				return;
			}

			if (driver == null) {
				driver = Application.Driver;
			}
			driver?.SetAttribute (normalColor);

			// Use "Lines" to ensure a Format (don't use "lines"))

			var linesFormated = Lines;
			switch (Direction) {
			case TextDirection.TopBottom_RightLeft:
			case TextDirection.LeftRight_BottomTop:
			case TextDirection.RightLeft_BottomTop:
			case TextDirection.BottomTop_RightLeft:
				linesFormated.Reverse ();
				break;
			}

			var isVertical = IsVerticalDirection (Direction);
			var maxBounds = bounds;
			if (driver != null) {
				maxBounds = containerBounds == default
					? bounds
					: new Rect (Math.Max (containerBounds.X, bounds.X),
					Math.Max (containerBounds.Y, bounds.Y),
					Math.Max (Math.Min (containerBounds.Width, containerBounds.Right - bounds.Left), 0),
					Math.Max (Math.Min (containerBounds.Height, containerBounds.Bottom - bounds.Top), 0));
			}
			if (maxBounds.Width == 0 || maxBounds.Height == 0) {
				return;
			}

			// BUGBUG: v2 - TextFormatter should not change the clip region. If a caller wants to break out of the clip region it should do
			// so explicitly.
			//var savedClip = Application.Driver?.Clip;
			//if (Application.Driver != null) {
			//	Application.Driver.Clip = maxBounds;
			//}
			var lineOffset = !isVertical && bounds.Y < 0 ? Math.Abs (bounds.Y) : 0;

			for (int line = lineOffset; line < linesFormated.Count; line++) {
				if ((isVertical && line > bounds.Width) || (!isVertical && line > bounds.Height))
					continue;
				if ((isVertical && line >= maxBounds.Left + maxBounds.Width)
					|| (!isVertical && line >= maxBounds.Top + maxBounds.Height + lineOffset))

					break;

				var runes = _lines [line].ToRunes ();

				switch (Direction) {
				case TextDirection.RightLeft_BottomTop:
				case TextDirection.RightLeft_TopBottom:
				case TextDirection.BottomTop_LeftRight:
				case TextDirection.BottomTop_RightLeft:
					runes = runes.Reverse ().ToArray ();
					break;
				}

				// When text is justified, we lost left or right, so we use the direction to align. 

				int x, y;
				// Horizontal Alignment
				if (_textAlignment == TextAlignment.Right || (_textAlignment == TextAlignment.Justified && !IsLeftToRight (Direction))) {
					if (isVertical) {
						var runesWidth = GetSumMaxCharWidth (Lines, line, TabWidth);
						x = bounds.Right - runesWidth;
						CursorPosition = bounds.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
					} else {
						var runesWidth = StringExtensions.ToString (runes).GetColumns ();
						x = bounds.Right - runesWidth;
						CursorPosition = bounds.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
					}
				} else if (_textAlignment == TextAlignment.Left || _textAlignment == TextAlignment.Justified) {
					if (isVertical) {
						var runesWidth = line > 0 ? GetSumMaxCharWidth (Lines, 0, line, TabWidth) : 0;
						x = bounds.Left + runesWidth;
					} else {
						x = bounds.Left;
					}
					CursorPosition = _hotKeyPos > -1 ? _hotKeyPos : 0;
				} else if (_textAlignment == TextAlignment.Centered) {
					if (isVertical) {
						var runesWidth = GetSumMaxCharWidth (Lines, line, TabWidth);
						x = bounds.Left + line + ((bounds.Width - runesWidth) / 2);
						CursorPosition = (bounds.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
					} else {
						var runesWidth = StringExtensions.ToString (runes).GetColumns ();
						x = bounds.Left + (bounds.Width - runesWidth) / 2;
						CursorPosition = (bounds.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
					}
				} else {
					throw new ArgumentOutOfRangeException ();
				}

				// Vertical Alignment
				if (_textVerticalAlignment == VerticalTextAlignment.Bottom || (_textVerticalAlignment == VerticalTextAlignment.Justified && !IsTopToBottom (Direction))) {
					if (isVertical) {
						y = bounds.Bottom - runes.Length;
					} else {
						y = bounds.Bottom - Lines.Count + line;
					}
				} else if (_textVerticalAlignment == VerticalTextAlignment.Top || _textVerticalAlignment == VerticalTextAlignment.Justified) {
					if (isVertical) {
						y = bounds.Top;
					} else {
						y = bounds.Top + line;
					}
				} else if (_textVerticalAlignment == VerticalTextAlignment.Middle) {
					if (isVertical) {
						var s = (bounds.Height - runes.Length) / 2;
						y = bounds.Top + s;
					} else {
						var s = (bounds.Height - Lines.Count) / 2;
						y = bounds.Top + line + s;
					}
				} else {
					throw new ArgumentOutOfRangeException ();
				}

				var colOffset = bounds.X < 0 ? Math.Abs (bounds.X) : 0;
				var start = isVertical ? bounds.Top : bounds.Left;
				var size = isVertical ? bounds.Height : bounds.Width;
				var current = start + colOffset;
				List<Point?> lastZeroWidthPos = null;
				Rune rune = default;
				Rune lastRuneUsed;
				var zeroLengthCount = isVertical ? runes.Sum (r => r.GetColumns () == 0 ? 1 : 0) : 0;

				for (var idx = (isVertical ? start - y : start - x) + colOffset; current < start + size + zeroLengthCount; idx++) {
					lastRuneUsed = rune;
					if (lastZeroWidthPos == null) {
						if (idx < 0 || x + current + colOffset < 0) {
							current++;
							continue;
						} else if (!fillRemaining && idx > runes.Length - 1) {
							break;
						}
						if ((!isVertical && current - start > maxBounds.Left + maxBounds.Width - bounds.X + colOffset)
							|| (isVertical && idx > maxBounds.Top + maxBounds.Height - bounds.Y)) {

							break;
						}
					}
					//if ((!isVertical && idx > maxBounds.Left + maxBounds.Width - bounds.X + colOffset)
					//	|| (isVertical && idx > maxBounds.Top + maxBounds.Height - bounds.Y))

					//	break;

					rune = (Rune)' ';
					if (isVertical) {
						if (idx >= 0 && idx < runes.Length) {
							rune = runes [idx];
						}
						if (lastZeroWidthPos == null) {
							driver?.Move (x, current);
						} else {
							var foundIdx = lastZeroWidthPos.IndexOf (p => p.Value.Y == current);
							if (foundIdx > -1) {
								if (rune.IsCombiningMark ()) {
									lastZeroWidthPos [foundIdx] = (new Point (lastZeroWidthPos [foundIdx].Value.X + 1, current));

									driver?.Move (lastZeroWidthPos [foundIdx].Value.X, current);
								} else if (!rune.IsCombiningMark () && lastRuneUsed.IsCombiningMark ()) {
									current++;
									driver?.Move (x, current);
								} else {
									driver?.Move (x, current);
								}
							} else {
								driver?.Move (x, current);
							}
						}
					} else {
						driver?.Move (current, y);
						if (idx >= 0 && idx < runes.Length) {
							rune = runes [idx];
						}
					}

					var runeWidth = GetRuneWidth (rune, TabWidth);

					if (HotKeyPos > -1 && idx == HotKeyPos) {
						if ((isVertical && _textVerticalAlignment == VerticalTextAlignment.Justified) ||
						(!isVertical && _textAlignment == TextAlignment.Justified)) {
							CursorPosition = idx - start;
						}
						driver?.SetAttribute (hotColor);
						driver?.AddRune (rune);
						driver?.SetAttribute (normalColor);
					} else {
						if (isVertical) {
							if (runeWidth == 0) {
								if (lastZeroWidthPos == null) {
									lastZeroWidthPos = new List<Point?> ();
								}
								var foundIdx = lastZeroWidthPos.IndexOf (p => p.Value.Y == current);
								if (foundIdx == -1) {
									current--;
									lastZeroWidthPos.Add ((new Point (x + 1, current)));
								}
								driver?.Move (x + 1, current);
							}
						}

						driver?.AddRune (rune);
					}

					if (isVertical) {
						if (runeWidth > 0) {
							current++;
						}
					} else {
						current += runeWidth;
					}
					var nextRuneWidth = idx + 1 > -1 && idx + 1 < runes.Length ? runes [idx + 1].GetColumns () : 0;
					if (!isVertical && idx + 1 < runes.Length && current + nextRuneWidth > start + size) {
						break;
					}
				}
			}
			//if (Application.Driver != null) {
			//	Application.Driver.Clip = (Rect)savedClip;
			//}
		}
	}
}
