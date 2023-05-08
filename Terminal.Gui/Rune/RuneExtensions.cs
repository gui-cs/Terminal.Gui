﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	public static class RuneExtensions {
		public static Rune MaxRune = new Rune (0x10FFFF);

		public static int ColumnWidth (this Rune rune)
		{
			return RuneUtilities.ColumnWidth(rune);
		}

		public static bool IsNonSpacingChar (this Rune rune)
		{
			return RuneUtilities.IsNonSpacingChar (rune.Value);
		}

		public static bool IsWideChar (this Rune rune)
		{
			return RuneUtilities.IsWideChar (rune.Value);
		}

		public static int RuneLen (this Rune rune)
		{
			return 0;
		}

		public static int EncodeRune (this Rune rune, byte [] dest, int offset = 0)
		{
			return 0;
		}

		public static bool DecodeSurrogatePair (this Rune rune, out char [] spair)
		{
			spair = null;

			return true;
		}

		public static bool EncodeSurrogatePair (char highsurrogate, char lowSurrogate, out Rune result)
		{
			try {
				result = (Rune)char.ConvertToUtf32 (highsurrogate, lowSurrogate);
			} catch (Exception) {
				result = default;
				return false;
			}

			return true;
		}

		public static bool IsSurrogatePair (this Rune rune)
		{
			return char.IsSurrogatePair (rune.ToString (), 0);
		}

		public static bool IsSurrogate (this Rune rune)
		{
			return char.IsSurrogate (rune.ToString (), 0);
		}

		public static bool IsValid (byte [] buffer)
		{
			var str = Encoding.Unicode.GetString (buffer);

			return Rune.IsValid(str.EnumerateRunes ().Current.Value);
		}

		public static bool IsValid (this Rune rune)
		{
			return Rune.IsValid (rune.Value);
		}

		internal static bool IsHighSurrogate (this Rune rune)
		{
			return char.IsHighSurrogate (rune.ToString (), 0);
		}

		internal static bool IsLowSurrogate (this Rune rune)
		{
			return char.IsLowSurrogate (rune.ToString (), 0);
		}

		//public static bool operator==(this Rune a, Rune b) {  return a.Equals(b); }

		//public static bool operator!=(this Rune  a, Rune b) {  return a.Equals(b); }

		//public static bool Equals(this Rune rune, ) { }
	}

	///// <summary>
	///// Implements the <see cref="IEqualityComparer{T}"/> for comparing two <see cref="Toplevel"/>s
	///// used by <see cref="StackExtensions"/>.
	///// </summary>
	//public class RuneEqualityComparer : IEqualityComparer<Rune> {
	//	/// <summary>Determines whether the specified objects are equal.</summary>
	//	/// <param name="x">The first object of type <see cref="Rune" /> to compare.</param>
	//	/// <param name="y">The second object of type <see cref="Rune" /> to compare.</param>
	//	/// <returns>
	//	///     <see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
	//	public bool Equals (Rune x, Rune y)
	//	{
	//		return x.Equals (y);
	//	}

	//	/// <summary>Returns a hash code for the specified object.</summary>
	//	/// <param name="obj">The <see cref="Toplevel" /> for which a hash code is to be returned.</param>
	//	/// <returns>A hash code for the specified object.</returns>
	//	/// <exception cref="ArgumentNullException">The type of <paramref name="obj" /> 
	//	/// is a reference type and <paramref name="obj" /> is <see langword="null" />.</exception>
	//	public int GetHashCode (Rune obj)
	//	{
	//		return obj.GetHashCode ();
	//	}
	//}

	///// <summary>
	///// Implements the <see cref="IComparer{T}"/> to sort the <see cref="Rune"/> 
	///// from the <see cref="Application.OverlappedChildren"/> if needed.
	///// </summary>
	//public sealed class RuneComparer : IComparer<Rune> {
	//	/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
	//	/// <param name="x">The first object to compare.</param>
	//	/// <param name="y">The second object to compare.</param>
	//	/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero
	//	///             <paramref name="x" /> is less than <paramref name="y" />.Zero
	//	///             <paramref name="x" /> equals <paramref name="y" />.Greater than zero
	//	///             <paramref name="x" /> is greater than <paramref name="y" />.</returns>
	//	public int Compare (Rune x, Rune y)
	//	{
	//		return x.CompareTo (y);
	//	}
	//}

}
