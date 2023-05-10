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

		public static int ConsoleWidth (this string instr)
		{
			return instr.EnumerateRunes ().Sum (r => Math.Max (r.ColumnWidth (), 0));
		}

		public static int RuneCount (this string instr)
		{
			return instr.EnumerateRunes ().Count ();
		}

		public static Rune [] ToRunes (this string instr)
		{
			return instr.EnumerateRunes ().ToArray ();
		}

		public static List<Rune> ToRuneList (this string instr)
		{
			return instr.EnumerateRunes ().ToList ();
		}

		public static (Rune Rune, int Size) DecodeRune (this string instr, int start = 0, int nbytes = -1)
		{
			return RuneExtensions.DecodeRune (Encoding.UTF8.GetBytes (instr), start, nbytes);
		}

		public static (Rune rune, int size) DecodeLastRune (this string instr, int end = -1)
		{
			var bytes = Encoding.UTF8.GetBytes (instr);
			return RuneExtensions.DecodeLastRune (bytes, end);
		}

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

		public static byte [] ToByteArray (this string instr)
		{
			return Encoding.Unicode.GetBytes (instr.ToCharArray ());
		}

		public static string Make (Rune [] runes)
		{
			var str = string.Empty;

			foreach (var rune in runes) {
				str += rune.ToString ();
			}

			return str;
		}

		public static string Make (List<Rune> runes)
		{
			var str = string.Empty;
			foreach (var rune in runes) {
				str += rune.ToString ();
			}
			return str;
		}

		public static string Make (Rune rune)
		{
			return rune.ToString ();
		}

		public static string Make (uint rune)
		{
			return ((Rune)rune).ToString ();
		}

		public static string Make (byte [] bytes, Encoding encoding = null)
		{
			if (encoding == null) {
				encoding = Encoding.UTF8;
			}
			return encoding.GetString (bytes);
		}

		public static string Make (params char [] chars)
		{
			var c = new char [chars.Length];

			return new string (chars);
		}
	}
}
