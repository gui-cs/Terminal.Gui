using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminal.Gui;
/// <summary>
/// Extension to <see cref="string"/> to support TUI text manipulation.
/// </summary>
public static class StringExtensions {
	/// <summary>
	/// Repeats the string <paramref name="n"/> times.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The text to repeat.</param>
	/// <param name="n">Number of times to repeat the text.</param>
	/// <returns>
	///  The text repeated if <paramref name="n"/> is greater than zero, 
	///  otherwise <see langword="null"/>.
	/// </returns>
	public static string Repeat (this string str, int n)
	{
		if (n <= 0) {
			return null;
		}

		if (string.IsNullOrEmpty (str) || n == 1) {
			return str;
		}

		return new StringBuilder (str.Length * n)
			.Insert (0, str, n)
			.ToString ();
	}

	/// <summary>
	/// Gets the number of columns the string occupies in the terminal.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to measure.</param>
	/// <returns></returns>
	public static int GetColumns (this string str)
	{
		return str == null ? 0 : str.EnumerateRunes ().Sum (r => Math.Max (r.GetColumns (), 0));
	}

	/// <summary>
	/// Gets the number of runes in the string.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to count.</param>
	/// <returns></returns>
	public static int GetRuneCount (this string str) => str.EnumerateRunes ().Count ();

	/// <summary>
	/// Converts the string into a <see cref="Rune"/> array.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to convert.</param>
	/// <returns></returns>
	public static Rune [] ToRunes (this string str) => str.EnumerateRunes ().ToArray ();

	/// <summary>
	/// Converts the string into a <see cref="List{Rune}"/>.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to convert.</param>
	/// <returns></returns>
	public static List<Rune> ToRuneList (this string str) => str.EnumerateRunes ().ToList ();

	/// <summary>
	/// Unpacks the first UTF-8 encoding in the string and returns the rune and its width in bytes.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to decode.</param>
	/// <param name="start">Starting offset.</param>
	/// <param name="count">Number of bytes in the buffer, or -1 to make it the length of the buffer.</param>
	/// <returns></returns>
	public static (Rune Rune, int Size) DecodeRune (this string str, int start = 0, int count = -1)
	{
		var rune = str.EnumerateRunes ().ToArray () [start];
		var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
		if (count == -1) {
			count = bytes.Length;
		}
		var operationStatus = Rune.DecodeFromUtf8 (bytes, out rune, out int bytesConsumed);
		if (operationStatus == System.Buffers.OperationStatus.Done && bytesConsumed >= count) {
			return (rune, bytesConsumed);
		}
		return (Rune.ReplacementChar, 1);
	}

	/// <summary>
	/// Unpacks the last UTF-8 encoding in the string.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to decode.</param>
	/// <param name="end">Index in string to stop at; if -1, use the buffer length.</param>
	/// <returns></returns>
	public static (Rune rune, int size) DecodeLastRune (this string str, int end = -1)
	{
		var rune = str.EnumerateRunes ().ToArray () [end == -1 ? ^1 : end];
		var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
		var operationStatus = Rune.DecodeFromUtf8 (bytes, out rune, out int bytesConsumed);
		if (operationStatus == System.Buffers.OperationStatus.Done) {
			return (rune, bytesConsumed);
		}
		return (Rune.ReplacementChar, 1);
	}

	/// <summary>
	/// Reports whether the string is a surrogate pair and can be decoded from UTF-16.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to decode.</param>
	/// <param name="chars">The chars if the string was decoded. <see langword="null"/> otherwise.</param>
	/// <returns></returns>
	public static bool DecodeSurrogatePair (this string str, out char [] chars)
	{
		chars = null;
		if (str.Length == 2) {
			var charsArray = str.ToCharArray ();
			if (RuneExtensions.EncodeSurrogatePair (charsArray [0], charsArray [1], out _)) {
				chars = charsArray;
				return true;
			}
		}
		return false;
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

