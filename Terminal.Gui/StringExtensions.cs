using System.Collections.Generic;
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
			return 0;
		}

		public static int IndexOf (this string instr, Rune rune)
		{
			return 0;
		}

		public static int RuneCount (this string instr)
		{
			return 0;
		}

		public static Rune[] ToRunes (this string instr)
		{
			return null;
		}

		public static List<Rune> ToRuneList (this string instr)
		{
			return new List<Rune>();
		}

		public static (Rune rune, int size) DecodeRune (this string instr, int start = 0, int nbytes = -1)
		{
			return new (new Rune (), 0);
		}

		public static (Rune rune, int size) DecodeLastRune (this string instr, int end = -1)
		{
			return new (new Rune (), 0);
		}

		public static bool DecodeSurrogatePair (this string instr, out char [] chars)
		{
			chars = null;
			if (instr.Length == 2) {
				chars = instr.ToCharArray ();
				if (RuneExtensions.EncodeSurrogatePair (chars [0], chars [1], out _)) {
					chars = new char[] { chars [0], chars [1] };
					return true;
				}
			}
			return false;
		}

		public static byte[] ToByteArray (this string instr)
		{
			return Encoding.Unicode.GetBytes (instr.ToCharArray ());
		}

		public static string Make (Rune [] runes)
		{
			return runes.ToString ();
		}

		public static string Make (List<Rune> runes)
		{
			return runes.ToString ();
		}

		public static string Make (Rune rune)
		{
			return rune.ToString ();
		}


		public static string Make (uint rune)
		{
			return rune.ToString ();
		}

		public static string Make (byte [] bytes)
		{
			return bytes.ToString ();
		}

	}
}
