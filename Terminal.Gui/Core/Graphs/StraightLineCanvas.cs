using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui.Graphs {


	/// <summary>
	/// Facilitates box drawing and line intersection detection
	/// and rendering.
	/// </summary>
	public class StraightLineCanvas {

		private List<StraightLine> lines = new List<StraightLine> ();
		private ConsoleDriver driver;

		public StraightLineCanvas (ConsoleDriver driver)
		{
			this.driver = driver;
		}

		/// <summary>
		/// Add a new line to the canvas starting at <paramref name="from"/>.
		/// Use positive <paramref name="length"/> for Right and negative for Left
		/// when <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>.
		/// Use positive <paramref name="length"/> for Down and negative for Up
		/// when <see cref="Orientation"/> is <see cref="Orientation.Vertical"/>.
		/// </summary>
		/// <param name="from">Starting point.</param>
		/// <param name="length">Length of line.  0 for a dot.  
		/// Positive for Down/Right.  Negative for Up/Left.</param>
		/// <param name="orientation">Direction of the line.</param>
		public void AddLine (Point from, int length, Orientation orientation, BorderStyle style)
		{
			lines.Add (new StraightLine (from, length, orientation, style));
		}
		/// <summary>
		/// Evaluate all currently defined lines that lie within 
		/// <paramref name="inArea"/> and generate a 'bitmap' that
		/// shows what characters (if any) should be rendered at each
		/// point so that all lines connect up correctly with appropriate
		/// intersection symbols.
		/// <returns></returns>
		/// </summary>
		/// <param name="inArea"></param>
		/// <returns>Map as 2D array where first index is rows and second is column</returns>
		public Rune? [,] GenerateImage (Rect inArea)
		{
			Rune? [,] canvas = new Rune? [inArea.Height, inArea.Width];

			// walk through each pixel of the bitmap
			for (int y = 0; y < inArea.Height; y++) {
				for (int x = 0; x < inArea.Width; x++) {

					var intersects = lines
						.Select (l => l.Intersects (x, y))
						.Where (i => i != null)
						.ToArray ();

					// TODO: use Driver and LineStyle to map
					canvas [y, x] = GetRuneForIntersects (intersects);

				}
			}

			return canvas;
		}

		/// <summary>
		/// Draws all the lines that lie within the <paramref name="bounds"/> onto
		/// the <paramref name="view"/> client area.  This method should be called from
		/// <see cref="View.Redraw(Rect)"/>.
		/// </summary>
		/// <param name="view"></param>
		/// <param name="bounds"></param>
		public void Draw (View view, Rect bounds)
		{
			var runes = GenerateImage (bounds);

			for (int y = bounds.Y; y < bounds.Height; y++) {
				for (int x = bounds.X; x < bounds.Width; x++) {
					var rune = runes [y, x];

					if (rune.HasValue) {
						view.AddRune (x, y, rune.Value);
					}
				}
			}
		}

		private Rune? GetRuneForIntersects (IntersectionDefinition [] intersects)
		{
			if (!intersects.Any ())
				return null;

			var runeType = GetRuneTypeForIntersects (intersects);
			var useDouble = intersects.Any (i => i.Line.Style == BorderStyle.Double && i.Line.Length != 0);

			switch (runeType) {
			case IntersectionRuneType.None: 
				return null;
			case IntersectionRuneType.Dot: 
				return (Rune)'.';
			case IntersectionRuneType.ULCorner:
				return useDouble ? driver.ULDCorner : driver.ULCorner;
			case IntersectionRuneType.URCorner: 
				return useDouble ? driver.URDCorner : driver.URCorner;
			case IntersectionRuneType.LLCorner: 
				return useDouble ? driver.LLDCorner : driver.LLCorner;
			case IntersectionRuneType.LRCorner: 
				return useDouble ? driver.LRDCorner : driver.LRCorner;
			case IntersectionRuneType.TopTee: 
				return useDouble ? '╦' : driver.TopTee;
			case IntersectionRuneType.BottomTee: 
				return useDouble ? '╩' : driver.BottomTee;
			case IntersectionRuneType.RightTee: 
				return useDouble ? '╣' : driver.RightTee;
			case IntersectionRuneType.LeftTee: 
				return useDouble ? '╠' : driver.LeftTee;
			case IntersectionRuneType.Crosshair: 
				return useDouble ? '╬' : '┼';
			case IntersectionRuneType.HLine: 
				return useDouble ? driver.HDLine : driver.HLine;
			case IntersectionRuneType.VLine: 
				return useDouble ? driver.VDLine : driver.VLine;
			default: throw new ArgumentOutOfRangeException (nameof (runeType));
			}

		}


		private IntersectionRuneType GetRuneTypeForIntersects (IntersectionDefinition [] intersects)
		{
			if(intersects.All(i=>i.Line.Length == 0)) {
				return IntersectionRuneType.Dot;
			}

			// ignore dots
			intersects = intersects.Where (i => i.Type != IntersectionType.Dot).ToArray ();

			var set = new HashSet<IntersectionType> (intersects.Select (i => i.Type));

			#region Crosshair Conditions
			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.PassOverVertical
				)) {
				return IntersectionRuneType.Crosshair;
			}

			if (Has (set,
				IntersectionType.PassOverVertical,
				IntersectionType.StartLeft,
				IntersectionType.StartRight
				)) {
				return IntersectionRuneType.Crosshair;
			}

			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.StartUp,
				IntersectionType.StartDown
				)) {
				return IntersectionRuneType.Crosshair;
			}


			if (Has (set,
				IntersectionType.StartLeft,
				IntersectionType.StartRight,
				IntersectionType.StartUp,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.Crosshair;
			}
			#endregion


			#region Corner Conditions
			if (Exactly (set,
				IntersectionType.StartRight,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.ULCorner;
			}

			if (Exactly (set,
				IntersectionType.StartLeft,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.URCorner;
			}

			if (Exactly (set,
				IntersectionType.StartUp,
				IntersectionType.StartLeft)) {
				return IntersectionRuneType.LRCorner;
			}

			if (Exactly (set,
				IntersectionType.StartUp,
				IntersectionType.StartRight)) {
				return IntersectionRuneType.LLCorner;
			}
			#endregion Corner Conditions

			#region T Conditions
			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.TopTee;
			}
			if (Has (set,
				IntersectionType.StartRight,
				IntersectionType.StartLeft,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.TopTee;
			}

			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.StartUp)) {
				return IntersectionRuneType.BottomTee;
			}
			if (Has (set,
				IntersectionType.StartRight,
				IntersectionType.StartLeft,
				IntersectionType.StartUp)) {
				return IntersectionRuneType.BottomTee;
			}


			if (Has (set,
				IntersectionType.PassOverVertical,
				IntersectionType.StartRight)) {
				return IntersectionRuneType.LeftTee;
			}
			if (Has (set,
				IntersectionType.StartRight,
				IntersectionType.StartDown,
				IntersectionType.StartUp)) {
				return IntersectionRuneType.LeftTee;
			}


			if (Has (set,
				IntersectionType.PassOverVertical,
				IntersectionType.StartLeft)) {
				return IntersectionRuneType.RightTee;
			}
			if (Has (set,
				IntersectionType.StartLeft,
				IntersectionType.StartDown,
				IntersectionType.StartUp)) {
				return IntersectionRuneType.RightTee;
			}
			#endregion

			if (All (intersects, Orientation.Horizontal)) {
				return IntersectionRuneType.HLine;
			}

			if (All (intersects, Orientation.Vertical)) {
				return IntersectionRuneType.VLine;
			}

			return IntersectionRuneType.Dot;
		}

		private bool All (IntersectionDefinition [] intersects, Orientation orientation)
		{
			return intersects.All (i => i.Line.Orientation == orientation);
		}

		/// <summary>
		/// Returns true if the <paramref name="intersects"/> collection has all the <paramref name="types"/>
		/// specified (i.e. AND).
		/// </summary>
		/// <param name="intersects"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		private bool Has (HashSet<IntersectionType> intersects, params IntersectionType [] types)
		{
			return types.All (t => intersects.Contains (t));
		}

		/// <summary>
		/// Returns true if all requested <paramref name="types"/> appear in <paramref name="intersects"/>
		/// and there are no additional <see cref="IntersectionRuneType"/>
		/// </summary>
		/// <param name="intersects"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		private bool Exactly (HashSet<IntersectionType> intersects, params IntersectionType [] types)
		{
			return intersects.SetEquals (types);
		}

		class IntersectionDefinition {
			/// <summary>
			/// The point at which the intersection happens
			/// </summary>
			public Point Point { get; }

			/// <summary>
			/// Defines how <see cref="Line"/> position relates
			/// to <see cref="Point"/>.
			/// </summary>
			public IntersectionType Type { get; }

			/// <summary>
			/// The line that intersects <see cref="Point"/>
			/// </summary>
			public StraightLine Line { get; }

			public IntersectionDefinition (Point point, IntersectionType type, StraightLine line)
			{
				Point = point;
				Type = type;
				Line = line;
			}
		}

		/// <summary>
		/// The type of Rune that we will use before considering
		/// double width, curved borders etc
		/// </summary>
		enum IntersectionRuneType {
			None,
			Dot,
			ULCorner,
			URCorner,
			LLCorner,
			LRCorner,
			TopTee,
			BottomTee,
			RightTee,
			LeftTee,
			Crosshair,
			HLine,
			VLine,
		}

		enum IntersectionType {
			/// <summary>
			/// There is no intersection
			/// </summary>
			None,

			/// <summary>
			///  A line passes directly over this point traveling along
			///  the horizontal axis
			/// </summary>
			PassOverHorizontal,

			/// <summary>
			///  A line passes directly over this point traveling along
			///  the vertical axis
			/// </summary>
			PassOverVertical,

			/// <summary>
			/// A line starts at this point and is traveling up
			/// </summary>
			StartUp,

			/// <summary>
			/// A line starts at this point and is traveling right
			/// </summary>
			StartRight,

			/// <summary>
			/// A line starts at this point and is traveling down
			/// </summary>
			StartDown,

			/// <summary>
			/// A line starts at this point and is traveling left
			/// </summary>
			StartLeft,

			/// <summary>
			/// A line exists at this point who has 0 length
			/// </summary>
			Dot
		}

		class StraightLine {
			public Point Start { get; }
			public int Length { get; }
			public Orientation Orientation { get; }
			public BorderStyle Style { get; }

			public StraightLine (Point start, int length, Orientation orientation, BorderStyle style)
			{
				this.Start = start;
				this.Length = length;
				this.Orientation = orientation;
				this.Style = style;
			}

			internal IntersectionDefinition Intersects (int x, int y)
			{
				if (IsDot ()) {
					if (StartsAt (x, y)) {
						return new IntersectionDefinition (Start, IntersectionType.Dot, this);
					} else {
						return null;
					}
				}

				switch (Orientation) {
				case Orientation.Horizontal: return IntersectsHorizontally (x, y);
				case Orientation.Vertical: return IntersectsVertically (x, y);
				default: throw new ArgumentOutOfRangeException (nameof (Orientation));
				}

			}

			private IntersectionDefinition IntersectsHorizontally (int x, int y)
			{
				if (Start.Y != y) {
					return null;
				} else {
					if (StartsAt (x, y)) {

						return new IntersectionDefinition (
							Start,
							Length < 0 ? IntersectionType.StartLeft : IntersectionType.StartRight,
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

			private IntersectionDefinition IntersectsVertically (int x, int y)
			{
				if (Start.X != x) {
					return null;
				} else {
					if (StartsAt (x, y)) {

						return new IntersectionDefinition (
							Start,
							Length < 0 ? IntersectionType.StartUp : IntersectionType.StartDown,
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

			private bool EndsAt (int x, int y)
			{
				if (Orientation == Orientation.Horizontal) {
					return Start.X + Length == x && Start.Y == y;
				}

				return Start.X == x && Start.Y + Length == y;
			}

			private bool StartsAt (int x, int y)
			{
				return Start.X == x && Start.Y == y;
			}

			private bool IsDot ()
			{
				return Length == 0;
			}
		}
	}
}
