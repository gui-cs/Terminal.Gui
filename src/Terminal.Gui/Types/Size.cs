//
// System.Drawing.Size.cs
//
// Author:
//   Mike Kestner (mkestner@speakeasy.net)
//
// Copyright (C) 2001 Mike Kestner
// Copyright (C) 2004 Novell, Inc. http://www.novell.com
//

using System;

namespace Terminal.Gui {
	/// <summary>
	/// Stores an ordered pair of integers, which specify a Height and Width.
	/// </summary>
	public struct Size
	{ 
		private int width, height;

		/// <summary>
		/// Gets a Size structure that has a Height and Width value of 0.
		/// </summary>
		public static readonly Size Empty;

		/// <summary>
		///	Addition Operator
		/// </summary>
		///
		/// <remarks>
		///	Addition of two Size structures.
		/// </remarks>

		public static Size operator + (Size sz1, Size sz2)
		{
			return new Size (sz1.Width + sz2.Width, 
					 sz1.Height + sz2.Height);
		}
		
		/// <summary>
		///	Equality Operator
		/// </summary>
		///
		/// <remarks>
		///	Compares two Size objects. The return value is
		///	based on the equivalence of the Width and Height 
		///	properties of the two Sizes.
		/// </remarks>

		public static bool operator == (Size sz1, Size sz2)
		{
			return ((sz1.Width == sz2.Width) && 
				(sz1.Height == sz2.Height));
		}
		
		/// <summary>
		///	Inequality Operator
		/// </summary>
		///
		/// <remarks>
		///	Compares two Size objects. The return value is
		///	based on the equivalence of the Width and Height 
		///	properties of the two Sizes.
		/// </remarks>

		public static bool operator != (Size sz1, Size sz2)
		{
			return ((sz1.Width != sz2.Width) || 
				(sz1.Height != sz2.Height));
		}
		
		/// <summary>
		///	Subtraction Operator
		/// </summary>
		///
		/// <remarks>
		///	Subtracts two Size structures.
		/// </remarks>

		public static Size operator - (Size sz1, Size sz2)
		{
			return new Size (sz1.Width - sz2.Width, 
					 sz1.Height - sz2.Height);
		}
		
		/// <summary>
		///	Size to Point Conversion
		/// </summary>
		///
		/// <remarks>
		///	Returns a Point based on the dimensions of a given 
		///	Size. Requires explicit cast.
		/// </remarks>

		public static explicit operator Point (Size size)
		{
			return new Point (size.Width, size.Height);
		}

		/// <summary>
		///	Size Constructor
		/// </summary>
		///
		/// <remarks>
		///	Creates a Size from a Point value.
		/// </remarks>
		
		public Size (Point pt)
		{
			width = pt.X;
			height = pt.Y;
		}

		/// <summary>
		///	Size Constructor
		/// </summary>
		///
		/// <remarks>
		///	Creates a Size from specified dimensions.
		/// </remarks>
		
		public Size (int width, int height)
		{
			this.width = width;
			this.height = height;
		}

		/// <summary>
		///	IsEmpty Property
		/// </summary>
		///
		/// <remarks>
		///	Indicates if both Width and Height are zero.
		/// </remarks>
		
		public bool IsEmpty {
			get {
				return ((width == 0) && (height == 0));
			}
		}

		/// <summary>
		///	Width Property
		/// </summary>
		///
		/// <remarks>
		///	The Width coordinate of the Size.
		/// </remarks>
		
		public int Width {
			get {
				return width;
			}
			set {
				width = value;
			}
		}

		/// <summary>
		///	Height Property
		/// </summary>
		///
		/// <remarks>
		///	The Height coordinate of the Size.
		/// </remarks>
		
		public int Height {
			get {
				return height;
			}
			set {
				height = value;
			}
		}

		/// <summary>
		///	Equals Method
		/// </summary>
		///
		/// <remarks>
		///	Checks equivalence of this Size and another object.
		/// </remarks>
		
		public override bool Equals (object obj)
		{
			if (!(obj is Size))
				return false;

			return (this == (Size) obj);
		}

		/// <summary>
		///	GetHashCode Method
		/// </summary>
		///
		/// <remarks>
		///	Calculates a hashing value.
		/// </remarks>
		
		public override int GetHashCode ()
		{
			return width^height;
		}

		/// <summary>
		///	ToString Method
		/// </summary>
		///
		/// <remarks>
		///	Formats the Size as a string in coordinate notation.
		/// </remarks>
		
		public override string ToString ()
		{
			return String.Format ("{{Width={0}, Height={1}}}", width, height);
		}

		/// <summary>
		/// Adds the width and height of one Size structure to the width and height of another Size structure.
		/// </summary>
		/// <returns>The add.</returns>
		/// <param name="sz1">The first Size structure to add.</param>
		/// <param name="sz2">The second Size structure to add.</param>
		public static Size Add (Size sz1, Size sz2)
		{
			return new Size (sz1.Width + sz2.Width, 
					 sz1.Height + sz2.Height);

		}
		
		public static Size Subtract (Size sz1, Size sz2)
		{
			return new Size (sz1.Width - sz2.Width, 
					 sz1.Height - sz2.Height);
		}

	}
}
