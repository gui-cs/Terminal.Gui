using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	public static class RuneExtensions {
		public static int ColumnWidth (this Rune rune)
		{
			return 0;
		}

		public static int RuneLen (this Rune rune)
		{
			return 0;
		}

		public static int EncodeRune (this Rune rune, byte [] bytes, int offset)
		{
			return 0;
		}

		public static (Rune, int) DecodeRune (string str, int index, int Length)
		{
			return new (new Rune(), 0);
		}

		public static bool DecodeSurrogatePair (this Rune rune, out char [] spair)
		{
			spair = null;

			return true;
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
