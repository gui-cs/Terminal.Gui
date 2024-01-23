#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Terminal.Gui {

	/// <summary>
	/// Defines the style of lines for a <see cref="LineCanvas"/>.
	/// </summary>
	public enum LineStyle {
		/// <summary>
		/// No border is drawn.
		/// </summary>
		None,
		/// <summary>
		/// The border is drawn using thin line CM.Glyphs.
		/// </summary>
		Single,
		/// <summary>
		/// The border is drawn using thin line glyphs with dashed (double and triple) straight lines.
		/// </summary>
		Dashed,
		/// <summary>
		/// The border is drawn using thin line glyphs with short dashed (triple and quadruple) straight lines.
		/// </summary>
		Dotted,
		/// <summary>
		/// The border is drawn using thin double line CM.Glyphs.
		/// </summary>
		Double,
		/// <summary>
		/// The border is drawn using heavy line CM.Glyphs.
		/// </summary>
		Heavy,
		/// <summary>
		/// The border is drawn using heavy line glyphs with dashed (double and triple) straight lines.
		/// </summary>
		HeavyDashed,
		/// <summary>
		/// The border is drawn using heavy line glyphs with short dashed (triple and quadruple) straight lines.
		/// </summary>
		HeavyDotted,
		/// <summary>
		/// The border is drawn using thin line glyphs with rounded corners.
		/// </summary>
		Rounded,
		/// <summary>
		/// The border is drawn using thin line glyphs with rounded corners and dashed (double and triple) straight lines.
		/// </summary>
		RoundedDashed,
		/// <summary>
		/// The border is drawn using thin line glyphs with rounded corners and short dashed (triple and quadruple) straight lines.
		/// </summary>
		RoundedDotted,
		// TODO: Support Ruler
		///// <summary> 
		///// The border is drawn as a diagnostic ruler ("|123456789...").
		///// </summary>
		//Ruler
	}

	/// <summary>
	/// Facilitates box drawing and line intersection detection
	/// and rendering.  Does not support diagonal lines.
	/// </summary>
	public class LineCanvas : IDisposable {
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public LineCanvas ()
		{
			// TODO: Refactor ConfigurationManager to not use an event handler for this.
			// Instead, have it call a method on any class appropriately attributed
			// to update the cached values. See Issue #2871
			ConfigurationManager.Applied += ConfigurationManager_Applied;
		}

		/// <summary>
		/// Creates a new instance with the given <paramref name="lines"/>.
		/// </summary>
		/// <param name="lines">Initial lines for the canvas.</param>
		public LineCanvas (IEnumerable<StraightLine> lines) : this ()
		{
			_lines = lines.ToList ();
		}

		private void ConfigurationManager_Applied (object? sender, ConfigurationManagerEventArgs e)
		{
			foreach (var irr in runeResolvers) {
				irr.Value.SetGlyphs ();
			}
		}

		private List<StraightLine> _lines = new List<StraightLine> ();

		/// <summary>
		/// Gets the lines in the canvas.
		/// </summary>
		public IReadOnlyCollection<StraightLine> Lines { get { return _lines.AsReadOnly (); } }

		Dictionary<IntersectionRuneType, IntersectionRuneResolver> runeResolvers = new Dictionary<IntersectionRuneType, IntersectionRuneResolver> {
			{IntersectionRuneType.ULCorner,new ULIntersectionRuneResolver()},
			{IntersectionRuneType.URCorner,new URIntersectionRuneResolver()},
			{IntersectionRuneType.LLCorner,new LLIntersectionRuneResolver()},
			{IntersectionRuneType.LRCorner,new LRIntersectionRuneResolver()},

			{IntersectionRuneType.TopTee,new TopTeeIntersectionRuneResolver()},
			{IntersectionRuneType.LeftTee,new LeftTeeIntersectionRuneResolver()},
			{IntersectionRuneType.RightTee,new RightTeeIntersectionRuneResolver()},
			{IntersectionRuneType.BottomTee,new BottomTeeIntersectionRuneResolver()},

			{IntersectionRuneType.Cross,new CrossIntersectionRuneResolver()},
			// TODO: Add other resolvers
		};

		/// <summary>
		/// <para>
		/// Adds a new <paramref name="length"/> long line to the canvas starting at <paramref name="start"/>.
		/// </para>
		/// <para>
		/// Use positive <paramref name="length"/> for the line to extend Right and negative for Left
		/// when <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>.
		/// </para>
		/// <para>
		/// Use positive <paramref name="length"/> for the line to extend Down and negative for Up
		/// when <see cref="Orientation"/> is <see cref="Orientation.Vertical"/>.
		/// </para>
		/// </summary>
		/// <param name="start">Starting point.</param>
		/// <param name="length">The length of line. 0 for an intersection (cross or T). Positive for Down/Right. Negative for Up/Left.</param>
		/// <param name="orientation">The direction of the line.</param>
		/// <param name="style">The style of line to use</param>
		/// <param name="attribute"></param>
		public void AddLine (Point start, int length, Orientation orientation, LineStyle style, Attribute? attribute = default)
		{
			_cachedBounds = Rect.Empty;
			_lines.Add (new StraightLine (start, length, orientation, style, attribute));
		}

		/// <summary>
		/// Adds a new line to the canvas
		/// </summary>
		/// <param name="line"></param>
		public void AddLine (StraightLine line)
		{
			_cachedBounds = Rect.Empty;
			_lines.Add (line);
		}

		/// <summary>
		/// Removes the last line added to the canvas
		/// </summary>
		/// <returns></returns>
		public StraightLine RemoveLastLine ()
		{
			var l = _lines.LastOrDefault ();
			if (l != null) {
				_lines.Remove (l);
			}

			return l!;
		}

		/// <summary>
		/// Clears all lines from the LineCanvas.
		/// </summary>
		public void Clear ()
		{
			_cachedBounds = Rect.Empty;
			_lines.Clear ();
		}

		/// <summary>
		/// Clears any cached states from the canvas
		/// Call this method if you make changes to lines
		/// that have already been added.
		/// </summary>
		public void ClearCache ()
		{
			_cachedBounds = Rect.Empty;
		}
		private Rect _cachedBounds;

		/// <summary>
		/// Gets the rectangle that describes the bounds of the canvas. Location is the coordinates of the 
		/// line that is furthest left/top and Size is defined by the line that extends the furthest
		/// right/bottom.
		/// </summary>
		public Rect Bounds {
			get {
				if (_cachedBounds.IsEmpty) {
					if (_lines.Count == 0) {
						return _cachedBounds;
					}

					Rect bounds = _lines [0].Bounds;

					for (var i = 1; i < _lines.Count; i++) {
						var line = _lines [i];
						var lineBounds = line.Bounds;
						bounds = Rect.Union (bounds, lineBounds);
					}

					if (bounds.Width == 0) {
						bounds.Width = 1;
					}

					if (bounds.Height == 0) {
						bounds.Height = 1;
					}
					_cachedBounds = new Rect (bounds.X, bounds.Y, bounds.Width, bounds.Height);
				}

				return _cachedBounds;
			}
		}

		// TODO: Unless there's an obvious use case for this API we should delete it in favor of the
		// simpler version that doensn't take an area.
		/// <summary>
		/// Evaluates the lines that have been added to the canvas and returns a map containing
		/// the glyphs and their locations. The glyphs are the characters that should be rendered
		/// so that all lines connect up with the appropriate intersection symbols. 
		/// </summary>
		/// <param name="inArea">A rectangle to constrain the search by.</param>
		/// <returns>A map of the points within the canvas that intersect with <paramref name="inArea"/>.</returns>
		public Dictionary<Point, Rune> GetMap (Rect inArea)
		{
			var map = new Dictionary<Point, Rune> ();

			// walk through each pixel of the bitmap
			for (int y = inArea.Y; y < inArea.Y + inArea.Height; y++) {
				for (int x = inArea.X; x < inArea.X + inArea.Width; x++) {

					var intersects = _lines
						.Select (l => l.Intersects (x, y))
						.Where (i => i != null)
						.ToArray ();

					var rune = GetRuneForIntersects (Application.Driver, intersects);

					if (rune != null) {
						map.Add (new Point (x, y), rune.Value);
					}
				}
			}

			return map;
		}

		/// <summary>
		/// Evaluates the lines that have been added to the canvas and returns a map containing
		/// the glyphs and their locations. The glyphs are the characters that should be rendered
		/// so that all lines connect up with the appropriate intersection symbols. 
		/// </summary>
		/// <returns>A map of all the points within the canvas.</returns>
		public Dictionary<Point, Cell> GetCellMap ()
		{
			var map = new Dictionary<Point, Cell> ();

			// walk through each pixel of the bitmap
			for (int y = Bounds.Y; y < Bounds.Y + Bounds.Height; y++) {
				for (int x = Bounds.X; x < Bounds.X + Bounds.Width; x++) {

					var intersects = _lines
						.Select (l => l.Intersects (x, y))
						.Where (i => i != null)
						.ToArray ();

					var cell = GetCellForIntersects (Application.Driver, intersects);

					if (cell != null) {
						map.Add (new Point (x, y), cell);
					}
				}
			}

			return map;
		}

		/// <summary>
		/// Evaluates the lines that have been added to the canvas and returns a map containing
		/// the glyphs and their locations. The glyphs are the characters that should be rendered
		/// so that all lines connect up with the appropriate intersection symbols. 
		/// </summary>
		/// <returns>A map of all the points within the canvas.</returns>
		public Dictionary<Point, Rune> GetMap () => GetMap (Bounds);

		/// <summary>
		/// Returns the contents of the line canvas rendered to a string. The string
		/// will include all columns and rows, even if <see cref="Bounds"/> has negative coordinates. 
		/// For example, if the canvas contains a single line that starts at (-1,-1) with a length of 2, the
		/// rendered string will have a length of 2.
		/// </summary>
		/// <returns>The canvas rendered to a string.</returns>
		public override string ToString ()
		{
			if (Bounds.IsEmpty) {
				return string.Empty;
			}

			// Generate the rune map for the entire canvas
			var runeMap = GetMap ();

			// Create the rune canvas
			Rune [,] canvas = new Rune [Bounds.Height, Bounds.Width];

			// Copy the rune map to the canvas, adjusting for any negative coordinates
			foreach (var kvp in runeMap) {
				int x = kvp.Key.X - Bounds.X;
				int y = kvp.Key.Y - Bounds.Y;
				canvas [y, x] = kvp.Value;
			}

			// Convert the canvas to a string
			StringBuilder sb = new StringBuilder ();
			for (int y = 0; y < canvas.GetLength (0); y++) {
				for (int x = 0; x < canvas.GetLength (1); x++) {
					Rune r = canvas [y, x];
					sb.Append (r.Value == 0 ? ' ' : r.ToString ());
				}
				if (y < canvas.GetLength (0) - 1) {
					sb.AppendLine ();
				}
			}

			return sb.ToString ();
		}

		private abstract class IntersectionRuneResolver {
			internal Rune _round;
			internal Rune _doubleH;
			internal Rune _doubleV;
			internal Rune _doubleBoth;
			internal Rune _thickH;
			internal Rune _thickV;
			internal Rune _thickBoth;
			internal Rune _normal;

			public IntersectionRuneResolver ()
			{
				SetGlyphs ();
			}

			/// <summary>
			/// Sets the glyphs used. Call this method after construction and any time 
			/// ConfigurationManager has updated the settings.
			/// </summary>
			public abstract void SetGlyphs ();

			public Rune? GetRuneForIntersects (ConsoleDriver driver, IntersectionDefinition? [] intersects)
			{
				var useRounded = intersects.Any (i => i?.Line.Length != 0 && (
					i?.Line.Style == LineStyle.Rounded || i?.Line.Style == LineStyle.RoundedDashed || i?.Line.Style == LineStyle.RoundedDotted));

				// Note that there aren't any glyphs for intersections of double lines with heavy lines

				bool doubleHorizontal = intersects.Any (l => l?.Line.Orientation == Orientation.Horizontal && l.Line.Style == LineStyle.Double);
				bool doubleVertical = intersects.Any (l => l?.Line.Orientation == Orientation.Vertical && l.Line.Style == LineStyle.Double);

				bool thickHorizontal = intersects.Any (l => l?.Line.Orientation == Orientation.Horizontal && (
					l.Line.Style == LineStyle.Heavy || l.Line.Style == LineStyle.HeavyDashed || l.Line.Style == LineStyle.HeavyDotted));
				bool thickVertical = intersects.Any (l => l?.Line.Orientation == Orientation.Vertical && (
					l.Line.Style == LineStyle.Heavy || l.Line.Style == LineStyle.HeavyDashed || l.Line.Style == LineStyle.HeavyDotted));

				if (doubleHorizontal) {
					return doubleVertical ? _doubleBoth : _doubleH;
				}
				if (doubleVertical) {
					return _doubleV;
				}

				if (thickHorizontal) {
					return thickVertical ? _thickBoth : _thickH;
				}
				if (thickVertical) {
					return _thickV;
				}

				return useRounded ? _round : _normal;
			}
		}

		private class ULIntersectionRuneResolver : IntersectionRuneResolver {
			public override void SetGlyphs ()
			{
				_round = CM.Glyphs.ULCornerR;
				_doubleH = CM.Glyphs.ULCornerSingleDbl;
				_doubleV = CM.Glyphs.ULCornerDblSingle;
				_doubleBoth = CM.Glyphs.ULCornerDbl;
				_thickH = CM.Glyphs.ULCornerLtHv;
				_thickV = CM.Glyphs.ULCornerHvLt;
				_thickBoth = CM.Glyphs.ULCornerHv;
				_normal = CM.Glyphs.ULCorner;
			}
		}
		private class URIntersectionRuneResolver : IntersectionRuneResolver {
			public override void SetGlyphs ()
			{
				_round = CM.Glyphs.URCornerR;
				_doubleH = CM.Glyphs.URCornerSingleDbl;
				_doubleV = CM.Glyphs.URCornerDblSingle;
				_doubleBoth = CM.Glyphs.URCornerDbl;
				_thickH = CM.Glyphs.URCornerHvLt;
				_thickV = CM.Glyphs.URCornerLtHv;
				_thickBoth = CM.Glyphs.URCornerHv;
				_normal = CM.Glyphs.URCorner;
			}
		}
		private class LLIntersectionRuneResolver : IntersectionRuneResolver {
			public override void SetGlyphs ()
			{
				_round = CM.Glyphs.LLCornerR;
				_doubleH = CM.Glyphs.LLCornerSingleDbl;
				_doubleV = CM.Glyphs.LLCornerDblSingle;
				_doubleBoth = CM.Glyphs.LLCornerDbl;
				_thickH = CM.Glyphs.LLCornerLtHv;
				_thickV = CM.Glyphs.LLCornerHvLt;
				_thickBoth = CM.Glyphs.LLCornerHv;
				_normal = CM.Glyphs.LLCorner;
			}

		}
		private class LRIntersectionRuneResolver : IntersectionRuneResolver {
			public override void SetGlyphs ()
			{
				_round = CM.Glyphs.LRCornerR;
				_doubleH = CM.Glyphs.LRCornerSingleDbl;
				_doubleV = CM.Glyphs.LRCornerDblSingle;
				_doubleBoth = CM.Glyphs.LRCornerDbl;
				_thickH = CM.Glyphs.LRCornerLtHv;
				_thickV = CM.Glyphs.LRCornerHvLt;
				_thickBoth = CM.Glyphs.LRCornerHv;
				_normal = CM.Glyphs.LRCorner;
			}
		}

		private class TopTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public override void SetGlyphs ()
			{
				_round = CM.Glyphs.TopTee;
				_doubleH = CM.Glyphs.TopTeeDblH;
				_doubleV = CM.Glyphs.TopTeeDblV;
				_doubleBoth = CM.Glyphs.TopTeeDbl;
				_thickH = CM.Glyphs.TopTeeHvH;
				_thickV = CM.Glyphs.TopTeeHvV;
				_thickBoth = CM.Glyphs.TopTeeHvDblH;
				_normal = CM.Glyphs.TopTee;
			}
		}
		private class LeftTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public override void SetGlyphs ()
			{
				_round = CM.Glyphs.LeftTee;
				_doubleH = CM.Glyphs.LeftTeeDblH;
				_doubleV = CM.Glyphs.LeftTeeDblV;
				_doubleBoth = CM.Glyphs.LeftTeeDbl;
				_thickH = CM.Glyphs.LeftTeeHvH;
				_thickV = CM.Glyphs.LeftTeeHvV;
				_thickBoth = CM.Glyphs.LeftTeeHvDblH;
				_normal = CM.Glyphs.LeftTee;
			}
		}
		private class RightTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public override void SetGlyphs ()
			{
				_round = CM.Glyphs.RightTee;
				_doubleH = CM.Glyphs.RightTeeDblH;
				_doubleV = CM.Glyphs.RightTeeDblV;
				_doubleBoth = CM.Glyphs.RightTeeDbl;
				_thickH = CM.Glyphs.RightTeeHvH;
				_thickV = CM.Glyphs.RightTeeHvV;
				_thickBoth = CM.Glyphs.RightTeeHvDblH;
				_normal = CM.Glyphs.RightTee;
			}
		}
		private class BottomTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public override void SetGlyphs ()
			{
				_round = CM.Glyphs.BottomTee;
				_doubleH = CM.Glyphs.BottomTeeDblH;
				_doubleV = CM.Glyphs.BottomTeeDblV;
				_doubleBoth = CM.Glyphs.BottomTeeDbl;
				_thickH = CM.Glyphs.BottomTeeHvH;
				_thickV = CM.Glyphs.BottomTeeHvV;
				_thickBoth = CM.Glyphs.BottomTeeHvDblH;
				_normal = CM.Glyphs.BottomTee;
			}
		}
		private class CrossIntersectionRuneResolver : IntersectionRuneResolver {
			public override void SetGlyphs ()
			{
				_round = CM.Glyphs.Cross;
				_doubleH = CM.Glyphs.CrossDblH;
				_doubleV = CM.Glyphs.CrossDblV;
				_doubleBoth = CM.Glyphs.CrossDbl;
				_thickH = CM.Glyphs.CrossHvH;
				_thickV = CM.Glyphs.CrossHvV;
				_thickBoth = CM.Glyphs.CrossHv;
				_normal = CM.Glyphs.Cross;
			}
		}

		private Rune? GetRuneForIntersects (ConsoleDriver driver, IntersectionDefinition? [] intersects)
		{
			if (!intersects.Any ()) {
				return null;
			}

			var runeType = GetRuneTypeForIntersects (intersects);

			if (runeResolvers.TryGetValue (runeType, out var resolver)) {
				return resolver.GetRuneForIntersects (driver, intersects);
			}

			// TODO: Remove these once we have all of the below ported to IntersectionRuneResolvers
			var useDouble = intersects.Any (i => i?.Line.Style == LineStyle.Double);
			var useDashed = intersects.Any (i => i?.Line.Style == LineStyle.Dashed || i?.Line.Style == LineStyle.RoundedDashed);
			var useDotted = intersects.Any (i => i?.Line.Style == LineStyle.Dotted || i?.Line.Style == LineStyle.RoundedDotted);
			// horiz and vert lines same as Single for Rounded
			var useThick = intersects.Any (i => i?.Line.Style == LineStyle.Heavy);
			var useThickDashed = intersects.Any (i => i?.Line.Style == LineStyle.HeavyDashed);
			var useThickDotted = intersects.Any (i => i?.Line.Style == LineStyle.HeavyDotted);
			// TODO: Support ruler
			//var useRuler = intersects.Any (i => i.Line.Style == LineStyle.Ruler && i.Line.Length != 0);

			// TODO: maybe make these resolvers too for simplicity?
			switch (runeType) {
			case IntersectionRuneType.None:
				return null;
			case IntersectionRuneType.Dot:
				return (Rune)CM.Glyphs.Dot;
			case IntersectionRuneType.HLine:
				if (useDouble) {
					return CM.Glyphs.HLineDbl;
				}
				if (useDashed) {
					return CM.Glyphs.HLineDa2;
				}
				if (useDotted) {
					return CM.Glyphs.HLineDa3;
				}
				return useThick ? CM.Glyphs.HLineHv : (useThickDashed ? CM.Glyphs.HLineHvDa2 : (useThickDotted ? CM.Glyphs.HLineHvDa3 : CM.Glyphs.HLine));
			case IntersectionRuneType.VLine:
				if (useDouble) {
					return CM.Glyphs.VLineDbl;
				}
				if (useDashed) {
					return CM.Glyphs.VLineDa3;
				}
				if (useDotted) {
					return CM.Glyphs.VLineDa4;
				}
				return useThick ? CM.Glyphs.VLineHv : (useThickDashed ? CM.Glyphs.VLineHvDa3 : (useThickDotted ? CM.Glyphs.VLineHvDa4 : CM.Glyphs.VLine));

			default: throw new Exception ("Could not find resolver or switch case for " + nameof (runeType) + ":" + runeType);
			}
		}

		private Attribute? GetAttributeForIntersects (IntersectionDefinition? [] intersects) => intersects [0]!.Line.Attribute;

		private Cell? GetCellForIntersects (ConsoleDriver driver, IntersectionDefinition? [] intersects)
		{
			if (!intersects.Any ()) {
				return null;
			}

			var cell = new Cell ();
			var rune = GetRuneForIntersects (driver, intersects);
			if (rune.HasValue) {
				cell.Rune = rune.Value;
			}
			cell.Attribute = GetAttributeForIntersects (intersects);
			return cell;
		}

		private IntersectionRuneType GetRuneTypeForIntersects (IntersectionDefinition? [] intersects)
		{
			var set = new HashSet<IntersectionType> (intersects.Select (i => i!.Type));

			#region Cross Conditions
			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.PassOverVertical
				)) {
				return IntersectionRuneType.Cross;
			}

			if (Has (set,
				IntersectionType.PassOverVertical,
				IntersectionType.StartLeft,
				IntersectionType.StartRight
				)) {
				return IntersectionRuneType.Cross;
			}

			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.StartUp,
				IntersectionType.StartDown
				)) {
				return IntersectionRuneType.Cross;
			}

			if (Has (set,
				IntersectionType.StartLeft,
				IntersectionType.StartRight,
				IntersectionType.StartUp,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.Cross;
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

		private bool All (IntersectionDefinition? [] intersects, Orientation orientation)
		{
			return intersects.All (i => i!.Line.Orientation == orientation);
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

		/// <summary>
		/// Merges one line canvas into this one.
		/// </summary>
		/// <param name="lineCanvas"></param>
		public void Merge (LineCanvas lineCanvas)
		{
			foreach (var line in lineCanvas._lines) {
				AddLine (line);
			}
		}

		/// <inheritdoc />
		public void Dispose ()
		{
			ConfigurationManager.Applied -= ConfigurationManager_Applied;
		}
	}
	internal class IntersectionDefinition {
		/// <summary>
		/// The point at which the intersection happens
		/// </summary>
		internal Point Point { get; }

		/// <summary>
		/// Defines how <see cref="Line"/> position relates
		/// to <see cref="Point"/>.
		/// </summary>
		internal IntersectionType Type { get; }

		/// <summary>
		/// The line that intersects <see cref="Point"/>
		/// </summary>
		internal StraightLine Line { get; }

		internal IntersectionDefinition (Point point, IntersectionType type, StraightLine line)
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
	internal enum IntersectionRuneType {
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
		Cross,
		HLine,
		VLine,
	}

	internal enum IntersectionType {
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
}
