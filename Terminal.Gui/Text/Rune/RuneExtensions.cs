using System;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Extension helper of <see cref="System.Text.Rune"/> to work with specific text manipulation./>
	/// </summary>
	public static class RuneExtensions {
		/// <summary>
		/// Maximum valid Unicode code point.
		/// </summary>
		public static Rune MaxRune = new Rune (0x10FFFF);

		/// <summary>
		/// Number of column positions of a wide-character code. 
		/// This is used to measure runes as displayed by text-based terminals.
		/// </summary>
		/// <param name="rune">The rune to measure.</param>
		/// <returns>
		/// The width in columns, 0 if the argument is the null character, 
		/// -1 if the value is not printable, 
		/// otherwise the number of columns that the rune occupies.
		/// </returns>
		public static int ColumnWidth (this Rune rune)
		{
			return RuneUtilities.ColumnWidth (rune);
		}

		/// <summary>
		/// Check if the rune is a non-spacing character.
		/// </summary>
		/// <param name="rune">The rune to inspect.</param>
		/// <returns>True if is a non-spacing character, false otherwise.</returns>
		public static bool IsNonSpacingChar (this Rune rune)
		{
			return RuneUtilities.IsNonSpacingChar (rune.Value);
		}

		/// <summary>
		/// Check if the rune is a wide character.
		/// </summary>
		/// <param name="rune">The rune to inspect.</param>
		/// <returns>True if is a wide character, false otherwise.</returns>
		public static bool IsWideChar (this Rune rune)
		{
			return RuneUtilities.IsWideChar (rune.Value);
		}

		/// <summary>
		/// Get number of bytes required to encode the rune, based on the provided encoding.
		/// </summary>
		/// <param name="rune">The rune to probe.</param>
		/// <param name="encoding">The encoding used, default is UTF8.</param>
		/// <returns></returns>
		public static int RuneUnicodeLength (this Rune rune, Encoding encoding = null)
		{
			if (encoding == null) {
				encoding = Encoding.UTF8;
			}
			var bytes = encoding.GetBytes (rune.ToString ().ToCharArray ());
			var offset = 0;
			if (bytes [bytes.Length - 1] == 0) {
				offset++;
			}
			return bytes.Length - offset;
		}

		/// <summary>
		/// Writes into the destination buffer starting at offset the UTF8 encoded version of the rune.
		/// </summary>
		/// <param name="rune">The rune to encode.</param>
		/// <param name="dest">The destination buffer.</param>
		/// <param name="start">Starting offset to look into.</param>
		/// <param name="nbytes">Number of bytes valid in the buffer, or -1 to make it the length of the buffer.</param>
		/// <returns>he number of bytes written into the destination buffer.</returns>
		public static int EncodeRune (this Rune rune, byte [] dest, int start = 0, int nbytes = -1)
		{
			var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
			int length = 0;
			for (int i = 0; i < (nbytes == -1 ? bytes.Length : nbytes); i++) {
				if (bytes [i] == 0) {
					break;
				}
				dest [start + i] = bytes [i];
				length++;
			}
			return length;
		}

		/// <summary>
		/// DecodeRune unpacks the first UTF-8 encoding in the string returns the rune and its width in bytes.
		/// </summary>
		/// <param name="buffer">The byte array to look into.</param>
		/// <param name="start">Starting offset to look into.</param>
		/// <param name="nbytes">Number of bytes valid in the buffer, or -1 to make it the length of the buffer.</param>
		/// <returns></returns>
		public static (Rune Rune, int Size) DecodeRune (byte [] buffer, int start = 0, int nbytes = -1)
		{
			if (nbytes == -1) {
				nbytes = buffer.Length - start;
			}
			var str = Encoding.UTF8.GetString (buffer, start, nbytes);

			return str.DecodeRune (0);
		}

		/// <summary>
		/// DecodeLastRune unpacks the last UTF-8 encoding in the byte array.
		/// </summary>
		/// <param name="buffer">Buffer to decode rune from.</param>
		/// <param name="end">Scan up to that point, if the value is -1, it sets the value to the length of the buffer.</param>
		/// <returns></returns>
		public static (Rune Rune, int Size) DecodeLastRune (byte [] buffer, int end = -1)
		{
			var str = Encoding.UTF8.GetString (buffer, 0, end == -1 ? buffer.Length : end);

			return str.DecodeLastRune (-1);
		}

		/// <summary>
		/// Reports whether this <see cref="Rune"/> is a valid surrogate pair and can be decoded from UTF-16.
		/// </summary>
		/// <param name="rune">The rune to decode.</param>
		/// <param name="chars">The chars if is valid. Null otherwise.</param>
		/// <returns><c>true</c>If is a valid surrogate pair, <c>false</c>otherwise.</returns>
		public static bool DecodeSurrogatePair (this Rune rune, out char [] chars)
		{
			if (rune.IsSurrogatePair ()) {
				chars = rune.ToString ().ToCharArray ();
				return true;
			}
			chars = null;
			return false;
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="Rune"/> can be 
		/// encoded as UTF-16 from a surrogate pair or zero otherwise.
		/// </summary>
		/// <param name="highsurrogate">The high surrogate code point.</param>
		/// <param name="lowSurrogate">The low surrogate code point.</param>
		/// <param name="result">The returning rune.</param>
		/// <returns><c>True</c>if the returning rune is greater than 0 <c>False</c>otherwise.</returns>
		public static bool EncodeSurrogatePair (char highsurrogate, char lowSurrogate, out Rune result)
		{
			result = default;
			if (char.IsSurrogatePair (highsurrogate, lowSurrogate)) {
				result = (Rune)char.ConvertToUtf32 (highsurrogate, lowSurrogate);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Reports whether a rune is a surrogate code point.
		/// </summary>
		/// <param name="rune">The rune to probe.</param>
		/// <returns><c>true</c>If is a surrogate code point, <c>false</c>otherwise.</returns>
		public static bool IsSurrogatePair (this Rune rune)
		{
			return char.IsSurrogatePair (rune.ToString (), 0);
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="Rune"/> can be encoded as UTF-8.
		/// </summary>
		/// <param name="buffer">The byte array to probe.</param>
		/// <value><c>true</c> if is valid; otherwise, <c>false</c>.</value>
		public static bool IsValid (byte [] buffer)
		{
			var str = Encoding.Unicode.GetString (buffer);
			foreach (var rune in str.EnumerateRunes ()) {
				if (rune == Rune.ReplacementChar) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="Rune"/> is valid.
		/// </summary>
		/// <param name="rune">The rune to probe.</param>
		/// <returns></returns>
		public static bool IsValid (this Rune rune)
		{
			return Rune.IsValid (rune.Value);
		}
	}
}
