using System;
using System.Collections.Generic;

namespace Terminal.Gui {
#nullable enable
	// TODO: Add events that notify when StraightLine changes to enable dynamic layout
	/// <summary>
	/// A line between two points on a horizontal or vertical <see cref="Orientation"/>
	/// and a given style/color.
	/// </summary>
	public class StraightLine {

		/// <summary>
		/// Gets or sets where the line begins.
		/// </summary>
		public Point Start { get; set; }

		/// <summary>
		/// Gets or sets the length of the line.
		/// </summary>
		public int Length { get; set; }

		/// <summary>
		/// Gets or sets the orientation (horizontal or vertical) of the line.
		/// </summary>
		public Orientation Orientation { get; set; }

		/// <summary>
		/// Gets or sets the line style of the line (e.g. dotted, double).
		/// </summary>
		public LineStyle Style { get; set; }

		/// <summary>
		/// Gets or sets the color of the line.
		/// </summary>
		public Attribute? Attribute { get; set; }

		/// <summary>
		/// Creates a new instance of the <see cref="StraightLine"/> class.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="length"></param>
		/// <param name="orientation"></param>
		/// <param name="style"></param>
		/// <param name="attribute"></param>
		public StraightLine (Point start, int length, Orientation orientation, LineStyle style, Attribute? attribute = default)
		{
			this.Start = start;
			this.Length = length;
			this.Orientation = orientation;
			this.Style = style;
			this.Attribute = attribute;
		}

		internal IntersectionDefinition? Intersects (int x, int y)
		{
			switch (Orientation) {
			case Orientation.Horizontal: return IntersectsHorizontally (x, y);
			case Orientation.Vertical: return IntersectsVertically (x, y);
			default: throw new ArgumentOutOfRangeException (nameof (Orientation));
			}

		}

		private IntersectionDefinition? IntersectsHorizontally (int x, int y)
		{
			if (Start.Y != y) {
				return null;
			} else {
				if (StartsAt (x, y)) {

					return new IntersectionDefinition (
						Start,
						GetTypeByLength (IntersectionType.StartLeft, IntersectionType.PassOverHorizontal, IntersectionType.StartRight),
						this
						);

				}

				if (EndsAt (x, y)) {

					return new IntersectionDefinition (
						Start,
						Length < 0 ? IntersectionType.StartRight : IntersectionType.StartLeft,
						this
						);

				} else {
					var xmin = Math.Min (Start.X, Start.X + Length);
					var xmax = Math.Max (Start.X, Start.X + Length);

					if (xmin < x && xmax > x) {
						return new IntersectionDefinition (
						new Point (x, y),
						IntersectionType.PassOverHorizontal,
						this
						);
					}
				}

				return null;
			}
		}

		private IntersectionDefinition? IntersectsVertically (int x, int y)
		{
			if (Start.X != x) {
				return null;
			} else {
				if (StartsAt (x, y)) {

					return new IntersectionDefinition (
						Start,
						GetTypeByLength (IntersectionType.StartUp, IntersectionType.PassOverVertical, IntersectionType.StartDown),
						this
						);

				}

				if (EndsAt (x, y)) {

					return new IntersectionDefinition (
						Start,
						Length < 0 ? IntersectionType.StartDown : IntersectionType.StartUp,
						this
						);

				} else {
					var ymin = Math.Min (Start.Y, Start.Y + Length);
					var ymax = Math.Max (Start.Y, Start.Y + Length);

					if (ymin < y && ymax > y) {
						return new IntersectionDefinition (
						new Point (x, y),
						IntersectionType.PassOverVertical,
						this
						);
					}
				}

				return null;
			}
		}

		private IntersectionType GetTypeByLength (IntersectionType typeWhenNegative, IntersectionType typeWhenZero, IntersectionType typeWhenPositive)
		{
			if (Length == 0) {
				return typeWhenZero;
			}

			return Length < 0 ? typeWhenNegative : typeWhenPositive;
		}

		private bool EndsAt (int x, int y)
		{
			var sub = (Length == 0) ? 0 : (Length > 0) ? 1 : -1;
			if (Orientation == Orientation.Horizontal) {
				return Start.X + Length - sub == x && Start.Y == y;
			}

			return Start.X == x && Start.Y + Length - sub == y;
		}

		private bool StartsAt (int x, int y)
		{
			return Start.X == x && Start.Y == y;
		}

		/// <summary>
		/// Gets the rectangle that describes the bounds of the canvas. Location is the coordinates of the 
		/// line that is furthest left/top and Size is defined by the line that extends the furthest
		/// right/bottom.
		/// </summary>
		internal Rect Bounds {
			get {

				// 0 and 1/-1 Length means a size (width or height) of 1
				var size = Math.Max (1, Math.Abs (Length));

				// How much to offset x or y to get the start of the line
				var offset = Math.Abs (Length < 0 ? Length + 1 : 0);
				var x = Start.X - (Orientation == Orientation.Horizontal ? offset : 0);
				var y = Start.Y - (Orientation == Orientation.Vertical ? offset : 0);
				var width = Orientation == Orientation.Horizontal ? size : 1;
				var height = Orientation == Orientation.Vertical ? size : 1;

				return new Rect (x, y, width, height);
			}
		}

		/// <summary>
		/// Formats the Line as a string in (Start.X,Start.Y,Length,Orientation) notation.
		/// </summary>
		public override string ToString ()
		{
			return $"({Start.X},{Start.Y},{Length},{Orientation})";
		}
	}

	public static class StraightLineExtensions
	{
		/// <summary>
		/// Splits or removes all lines in the <paramref name="collection"/> such that none cover the given
		/// exclusion area.
		/// </summary>
		/// <param name="collection">Lines to adjust</param>
		/// <param name="start">First point to remove from collection</param>
		/// <param name="length">The number of sequential points to exclude</param>
		/// <param name="orientation">Orientation of the exclusion line</param>
		/// <returns></returns>
		public static IEnumerable<StraightLine> Exclude (this IEnumerable<StraightLine> collection, Point start, int length, Orientation orientation)
		{
			var toReturn = new List<StraightLine> ();
			if(length == 0) {
				return collection;
			}

			// Calculate exclude max and min points
			int exMin = Math.Min (start.X + length, start.X);
			int exMax = Math.Max (start.X + length, start.X);
			int eyMin = Math.Min (start.Y + length, start.Y);
			int eyMax = Math.Max (start.Y + length, start.Y);

			foreach (var l in collection) {

				int lxMin = Math.Min (l.Start.X + l.Length, l.Start.X);
				int lxMax = Math.Max (l.Start.X + l.Length, l.Start.X);
				int lyMin = Math.Min (l.Start.Y + l.Length, l.Start.Y);
				int lyMax = Math.Max (l.Start.Y + l.Length, l.Start.Y);

				// line is parallel to exclusion
				if (l.Orientation == orientation) {

					// lines are parallel.  For any straight line one dimension (x or y) is constant
					// e.g. Horizontal lines have constant y
					int econstPoint = orientation == Orientation.Horizontal ? start.Y : start.X;
					int lconstPoint = orientation == Orientation.Horizontal ? l.Start.Y : l.Start.X;

					// Do the parallel lines share constant plane
					if (econstPoint != lconstPoint) {

						// No, so no way they overlap
						toReturn.Add(l);
					}
					else {

						// For the varying axis what is the max/mins
						// i.e. points on horizontal lines vary by x, vertical lines vary by y
						int eDiffMin = orientation == Orientation.Horizontal ? exMin : eyMin;
						int eDiffMax = orientation == Orientation.Horizontal ? exMax : eyMax;
						int lDiffMin = orientation == Orientation.Horizontal ? lxMin : lyMin;
						int lDiffMax = orientation == Orientation.Horizontal ? lxMax : lyMax;

						if (lDiffMax < eDiffMin) {
							// Line ends before exclusion starts
							toReturn.Add (l);
						}
						else if(lDiffMin > eDiffMax) {
							// Line starts after exclusion ends
							toReturn.Add (l);
						}
						else {
							//lines overlap!

							// Is there a bit we can keep on the left?
							if(lDiffMin < eDiffMin) {
								// Create line up to exclusion point
								int from = lDiffMin;
								int len = eDiffMin - lDiffMin;

								if (len > 0) {
									toReturn.Add (CreateLineFromDiff (l, from, len));
								}
							}

							// Is there a bit we can keep on the right?
							if (lDiffMax > eDiffMax) {
								// Create line up to exclusion point
								int from = eDiffMax+1;
								int len = lDiffMax - lDiffMax;

								if(len>0) {
									toReturn.Add (CreateLineFromDiff (l, from, len));
								}
							}
						}
					}

				}
				else {
					// line is perpendicular to exclusion

					// TODO: Cut out at most 1 cell
				}
			}

			return toReturn;
		}

		/// <summary>
		/// Creates a new line which is part of <paramref name="l"/> from the point on the varying
		/// axis <paramref name="from"/> to <paramref name="length"/>.  Horizontal lines have points that
		/// vary by x while vertical lines have points that vary by y
		/// </summary>
		/// <param name="l"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		private static StraightLine CreateLineFromDiff (StraightLine l, int from, int length)
		{
			var start = new Point (
				l.Orientation == Orientation.Horizontal ? from : l.Start.X,
				l.Orientation == Orientation.Horizontal ? l.Start.Y : from);

			return new StraightLine (start, length, l.Orientation,l.Style, l.Attribute);
		}
	}
}
