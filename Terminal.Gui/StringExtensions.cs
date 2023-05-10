using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Extension helper of <see cref="System.String"/> to work with specific text manipulation./>
	/// </summary>
	public static class StringExtensions {
		/// <summary>
		/// Repeats the <paramref name="instr"/> <paramref name="n"/>  times.
		/// </summary>
		/// <param name="instr">The text to repeat.</param>
		/// <param name="n">Number of times to repeat the text.</param>
		/// <returns>
		///  The text repeated if <paramref name="n"/> is greater than zero, 
		///  otherwise <see langword="null"/>.
		/// </returns>
		public static string Repeat (this string instr, int n)
		{
			if (n <= 0) {
				return null;
			}

			if (string.IsNullOrEmpty (instr) || n == 1) {
				return instr;
			}

			return new StringBuilder (instr.Length * n)
				.Insert (0, instr, n)
				.ToString ();
		}

		/// <summary>
		/// Returns the number of columns used by the string on console applications.
		/// It's never return less than 0, like the <see cref="RuneExtensions.ColumnWidth(Rune)"/>.
		/// </summary>
		/// <param name="instr">The string to measure.</param>
		/// <returns></returns>
		public static int ConsoleWidth (this string instr)
		{
			return instr.EnumerateRunes ().Sum (r => Math.Max (r.ColumnWidth (), 0));
		}

		/// <summary>
		/// Returns the number of runes in a string.
		/// </summary>
		/// <param name="instr">The string to count.</param>
		/// <returns></returns>
		public static int RuneCount (this string instr)
		{
			return instr.EnumerateRunes ().Count ();
		}

		/// <summary>
		/// Converts a string into a <see cref="Rune"/> array.
		/// </summary>
		/// <param name="instr">The string to convert.</param>
		/// <returns></returns>
		public static Rune [] ToRunes (this string instr)
		{
			return instr.EnumerateRunes ().ToArray ();
		}

		/// <summary>
		/// Converts a string into a List of runes.
		/// </summary>
		/// <param name="instr">The string to convert.</param>
		/// <returns></returns>
		public static List<Rune> ToRuneList (this string instr)
		{
			return instr.EnumerateRunes ().ToList ();
		}

		/// <summary>
		/// DecodeRune unpacks the first UTF-8 encoding in the string returns the rune and its width in bytes.
		/// </summary>
		/// <param name="instr">The string to decode.</param>
		/// <param name="start">Starting offset to look into.</param>
		/// <param name="nbytes">Number of bytes valid in the buffer, or -1 to make it the length of the buffer.</param>
		/// <returns></returns>
		public static (Rune Rune, int Size) DecodeRune (this string instr, int start = 0, int nbytes = -1)
		{
			var rune = instr.EnumerateRunes ().ToArray () [start];
			var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
			if (nbytes == -1) {
				nbytes = bytes.Length;
			}
			var operationStatus = Rune.DecodeFromUtf8 (bytes, out rune, out int bytesConsumed);
			if (operationStatus == System.Buffers.OperationStatus.Done && bytesConsumed >= nbytes) {
				return (rune, bytesConsumed);
			} else {
				return (Rune.ReplacementChar, 1);
			}
		}

		/// <summary>
		/// DecodeLastRune unpacks the last UTF-8 encoding in the string.
		/// </summary>
		/// <param name="instr">The string to decode.</param>
		/// <param name="end">Scan up to that point, if the value is -1, it sets the value to the length of the buffer.</param>
		/// <returns></returns>
		public static (Rune rune, int size) DecodeLastRune (this string instr, int end = -1)
		{
			var rune = instr.EnumerateRunes ().ToArray () [end == -1 ? ^1 : end];
			var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
			var operationStatus = Rune.DecodeFromUtf8 (bytes, out rune, out int bytesConsumed);
			if (operationStatus == System.Buffers.OperationStatus.Done) {
				return (rune, bytesConsumed);
			} else {
				return (Rune.ReplacementChar, 1);
			}
		}

		/// <summary>
		/// Reports whether this <see cref="Rune"/> is a valid surrogate pair and can be decoded from UTF-16.
		/// </summary>
		/// <param name="instr">The string to decode.</param>
		/// <param name="chars">The chars if is valid. Null otherwise.</param>
		/// <returns></returns>
		public static bool DecodeSurrogatePair (this string instr, out char [] chars)
		{
			chars = null;
			if (instr.Length == 2) {
				var charsArray = instr.ToCharArray ();
				if (RuneExtensions.EncodeSurrogatePair (charsArray [0], charsArray [1], out _)) {
					chars = charsArray;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns a version of the string as a byte array, it might allocate or return 
		/// the internal byte buffer, depending on the backing implementation.
		/// </summary>
		/// <param name="instr">The string to convert.</param>
		/// <returns></returns>
		public static byte [] ToByteArray (this string instr)
		{
			return Encoding.Unicode.GetBytes (instr.ToCharArray ());
		}

		/// <summary>
		/// Converts a <see cref="Rune"/> array into a string.
		/// </summary>
		/// <param name="runes">The rune array to convert.</param>
		/// <returns></returns>
		public static string Make (Rune [] runes)
		{
			var str = string.Empty;

			foreach (var rune in runes) {
				str += rune.ToString ();
			}

			return str;
		}

		/// <summary>
		/// Converts a List of runes into a string.
		/// </summary>
		/// <param name="runes">The List of runes to convert.</param>
		/// <returns></returns>
		public static string Make (List<Rune> runes)
		{
			var str = string.Empty;
			foreach (var rune in runes) {
				str += rune.ToString ();
			}
			return str;
		}

		/// <summary>
		/// Converts a rune into a string.
		/// </summary>
		/// <param name="rune">The rune to convert.</param>
		/// <returns></returns>
		public static string Make (Rune rune)
		{
			return rune.ToString ();
		}

		/// <summary>
		/// Converts a numeric value of a rune into a string.
		/// </summary>
		/// <param name="rune">The rune to convert.</param>
		/// <returns></returns>
		public static string Make (uint rune)
		{
			return ((Rune)rune).ToString ();
		}

		/// <summary>
		/// Converts a byte array into a string in te provided encoding (default is UTF8)
		/// </summary>
		/// <param name="bytes">The byte array to convert.</param>
		/// <param name="encoding">The encoding to be used.</param>
		/// <returns></returns>
		public static string Make (byte [] bytes, Encoding encoding = null)
		{
			if (encoding == null) {
				encoding = Encoding.UTF8;
			}
			return encoding.GetString (bytes);
		}

		/// <summary>
		/// Converts a array of characters into a string.
		/// </summary>
		/// <param name="chars">The array of characters to convert.</param>
		/// <returns></returns>
		public static string Make (params char [] chars)
		{
			var c = new char [chars.Length];

			return new string (chars);
		}
	}
}
