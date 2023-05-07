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

		public static string Make (Rune [] runes)
		{
			return runes.ToString ();
		}

		public static string Make (List<Rune> runes)
		{
			return runes.ToString ();
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
