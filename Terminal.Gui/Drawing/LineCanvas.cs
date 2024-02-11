﻿#nullable enable
namespace Terminal.Gui;

/// <summary>Defines the style of lines for a <see cref="LineCanvas"/>.</summary>
public enum LineStyle
{
    /// <summary>No border is drawn.</summary>
    None,

    /// <summary>The border is drawn using thin line CM.Glyphs.</summary>
    Single,

    /// <summary>The border is drawn using thin line glyphs with dashed (double and triple) straight lines.</summary>
    Dashed,

    /// <summary>The border is drawn using thin line glyphs with short dashed (triple and quadruple) straight lines.</summary>
    Dotted,

    /// <summary>The border is drawn using thin double line CM.Glyphs.</summary>
    Double,

    /// <summary>The border is drawn using heavy line CM.Glyphs.</summary>
    Heavy,

    /// <summary>The border is drawn using heavy line glyphs with dashed (double and triple) straight lines.</summary>
    HeavyDashed,

    /// <summary>The border is drawn using heavy line glyphs with short dashed (triple and quadruple) straight lines.</summary>
    HeavyDotted,

    /// <summary>The border is drawn using thin line glyphs with rounded corners.</summary>
    Rounded,

    /// <summary>The border is drawn using thin line glyphs with rounded corners and dashed (double and triple) straight lines.</summary>
    RoundedDashed,

    /// <summary>
    ///     The border is drawn using thin line glyphs with rounded corners and short dashed (triple and quadruple) straight lines.
    /// </summary>
    RoundedDotted

    // TODO: Support Ruler
    ///// <summary> 
    ///// The border is drawn as a diagnostic ruler ("|123456789...").
    ///// </summary>
    //Ruler
}

/// <summary>Facilitates box drawing and line intersection detection and rendering.  Does not support diagonal lines.</summary>
public class LineCanvas : IDisposable
{
    /// <summary>Creates a new instance.</summary>
    public LineCanvas ()
    {
        // TODO: Refactor ConfigurationManager to not use an event handler for this.
        // Instead, have it call a method on any class appropriately attributed
        // to update the cached values. See Issue #2871
        Applied += ConfigurationManager_Applied;
    }

    /// <summary>Creates a new instance with the given <paramref name="lines"/>.</summary>
    /// <param name="lines">Initial lines for the canvas.</param>
    public LineCanvas (IEnumerable<StraightLine> lines) : this () { _lines = lines.ToList (); }

    private readonly List<StraightLine> _lines = new ();

    private readonly Dictionary<IntersectionRuneType, IntersectionRuneResolver> runeResolvers = new ()
    {
        {
            IntersectionRuneType.ULCorner,
            new ULIntersectionRuneResolver ()
        },
        {
            IntersectionRuneType.URCorner,
            new URIntersectionRuneResolver ()
        },
        {
            IntersectionRuneType.LLCorner,
            new LLIntersectionRuneResolver ()
        },
        {
            IntersectionRuneType.LRCorner,
            new LRIntersectionRuneResolver ()
        },
        {
            IntersectionRuneType.TopTee,
            new TopTeeIntersectionRuneResolver ()
        },
        {
            IntersectionRuneType.LeftTee,
            new LeftTeeIntersectionRuneResolver ()
        },
        {
            IntersectionRuneType.RightTee,
            new RightTeeIntersectionRuneResolver ()
        },
        {
            IntersectionRuneType.BottomTee,
            new BottomTeeIntersectionRuneResolver ()
        },
        {
            IntersectionRuneType.Cross,
            new CrossIntersectionRuneResolver ()
        }

        // TODO: Add other resolvers
    };

    private Rect _cachedBounds;

    /// <summary>
    ///     Gets the rectangle that describes the bounds of the canvas. Location is the coordinates of the line that is furthest left/top and Size is defined by the line that extends the furthest right/bottom.
    /// </summary>
    public Rect Bounds
    {
        get
        {
            if (_cachedBounds.IsEmpty)
            {
                if (_lines.Count == 0)
                {
                    return _cachedBounds;
                }

                Rect bounds = _lines [0].Bounds;

                for (var i = 1; i < _lines.Count; i++)
                {
                    StraightLine line = _lines [i];
                    Rect lineBounds = line.Bounds;
                    bounds = Rect.Union (bounds, lineBounds);
                }

                if (bounds.Width == 0)
                {
                    bounds.Width = 1;
                }

                if (bounds.Height == 0)
                {
                    bounds.Height = 1;
                }

                _cachedBounds = new Rect (bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }

            return _cachedBounds;
        }
    }

    /// <summary>Gets the lines in the canvas.</summary>
    public IReadOnlyCollection<StraightLine> Lines => _lines.AsReadOnly ();

    /// <inheritdoc/>
    public void Dispose () { Applied -= ConfigurationManager_Applied; }

    /// <summary>
    ///     <para>Adds a new <paramref name="length"/> long line to the canvas starting at <paramref name="start"/>.</para>
    ///     <para>
    ///         Use positive <paramref name="length"/> for the line to extend Right and negative for Left when
    ///         <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>.
    ///     </para>
    ///     <para>
    ///         Use positive <paramref name="length"/> for the line to extend Down and negative for Up when
    ///         <see cref="Orientation"/> is <see cref="Orientation.Vertical"/>.
    ///     </para>
    /// </summary>
    /// <param name="start">Starting point.</param>
    /// <param name="length">The length of line. 0 for an intersection (cross or T). Positive for Down/Right. Negative for Up/Left.</param>
    /// <param name="orientation">The direction of the line.</param>
    /// <param name="style">The style of line to use</param>
    /// <param name="attribute"></param>
    public void AddLine (
        Point start,
        int length,
        Orientation orientation,
        LineStyle style,
        Attribute? attribute = default
    )
    {
        _cachedBounds = Rect.Empty;
        _lines.Add (new StraightLine (start, length, orientation, style, attribute));
    }

    /// <summary>Adds a new line to the canvas</summary>
    /// <param name="line"></param>
    public void AddLine (StraightLine line)
    {
        _cachedBounds = Rect.Empty;
        _lines.Add (line);
    }

    /// <summary>Clears all lines from the LineCanvas.</summary>
    public void Clear ()
    {
        _cachedBounds = Rect.Empty;
        _lines.Clear ();
    }

    /// <summary>Clears any cached states from the canvas Call this method if you make changes to lines that have already been added.</summary>
    public void ClearCache () { _cachedBounds = Rect.Empty; }

    /// <summary>
    ///     Evaluates the lines that have been added to the canvas and returns a map containing the glyphs and their locations. The glyphs are the characters that should be rendered so that all lines connect up with the appropriate intersection symbols.
    /// </summary>
    /// <returns>A map of all the points within the canvas.</returns>
    public Dictionary<Point, Cell> GetCellMap ()
    {
        Dictionary<Point, Cell> map = new ();

        // walk through each pixel of the bitmap
        for (int y = Bounds.Y; y < Bounds.Y + Bounds.Height; y++)
        {
            for (int x = Bounds.X; x < Bounds.X + Bounds.Width; x++)
            {
                IntersectionDefinition? [] intersects = _lines
                                                        .Select (l => l.Intersects (x, y))
                                                        .Where (i => i != null)
                                                        .ToArray ();

                Cell? cell = GetCellForIntersects (Application.Driver, intersects);

                if (cell != null)
                {
                    map.Add (new Point (x, y), cell);
                }
            }
        }

        return map;
    }

    // TODO: Unless there's an obvious use case for this API we should delete it in favor of the
    // simpler version that doensn't take an area.
    /// <summary>
    ///     Evaluates the lines that have been added to the canvas and returns a map containing the glyphs and their locations. The glyphs are the characters that should be rendered so that all lines connect up with the appropriate intersection symbols.
    /// </summary>
    /// <param name="inArea">A rectangle to constrain the search by.</param>
    /// <returns>A map of the points within the canvas that intersect with <paramref name="inArea"/>.</returns>
    public Dictionary<Point, Rune> GetMap (Rect inArea)
    {
        Dictionary<Point, Rune> map = new ();

        // walk through each pixel of the bitmap
        for (int y = inArea.Y; y < inArea.Y + inArea.Height; y++)
        {
            for (int x = inArea.X; x < inArea.X + inArea.Width; x++)
            {
                IntersectionDefinition? [] intersects = _lines
                                                        .Select (l => l.Intersects (x, y))
                                                        .Where (i => i != null)
                                                        .ToArray ();

                Rune? rune = GetRuneForIntersects (Application.Driver, intersects);

                if (rune != null)
                {
                    map.Add (new Point (x, y), rune.Value);
                }
            }
        }

        return map;
    }

    /// <summary>
    ///     Evaluates the lines that have been added to the canvas and returns a map containing the glyphs and their locations. The glyphs are the characters that should be rendered so that all lines connect up with the appropriate intersection symbols.
    /// </summary>
    /// <returns>A map of all the points within the canvas.</returns>
    public Dictionary<Point, Rune> GetMap () { return GetMap (Bounds); }

    /// <summary>Merges one line canvas into this one.</summary>
    /// <param name="lineCanvas"></param>
    public void Merge (LineCanvas lineCanvas)
    {
        foreach (StraightLine line in lineCanvas._lines)
        {
            AddLine (line);
        }
    }

    /// <summary>Removes the last line added to the canvas</summary>
    /// <returns></returns>
    public StraightLine RemoveLastLine ()
    {
        StraightLine? l = _lines.LastOrDefault ();

        if (l != null)
        {
            _lines.Remove (l);
        }

        return l!;
    }

    /// <summary>
    ///     Returns the contents of the line canvas rendered to a string. The string will include all columns and rows, even if
    ///     <see cref="Bounds"/> has negative coordinates. For example, if the canvas contains a single line that starts at (-1,-1) with a length of 2, the rendered string will have a length of 2.
    /// </summary>
    /// <returns>The canvas rendered to a string.</returns>
    public override string ToString ()
    {
        if (Bounds.IsEmpty)
        {
            return string.Empty;
        }

        // Generate the rune map for the entire canvas
        Dictionary<Point, Rune> runeMap = GetMap ();

        // Create the rune canvas
        Rune [,] canvas = new Rune [Bounds.Height, Bounds.Width];

        // Copy the rune map to the canvas, adjusting for any negative coordinates
        foreach (KeyValuePair<Point, Rune> kvp in runeMap)
        {
            int x = kvp.Key.X - Bounds.X;
            int y = kvp.Key.Y - Bounds.Y;
            canvas [y, x] = kvp.Value;
        }

        // Convert the canvas to a string
        var sb = new StringBuilder ();

        for (var y = 0; y < canvas.GetLength (0); y++)
        {
            for (var x = 0; x < canvas.GetLength (1); x++)
            {
                Rune r = canvas [y, x];
                sb.Append (r.Value == 0 ? ' ' : r.ToString ());
            }

            if (y < canvas.GetLength (0) - 1)
            {
                sb.AppendLine ();
            }
        }

        return sb.ToString ();
    }

    private bool All (IntersectionDefinition? [] intersects, Orientation orientation) { return intersects.All (i => i!.Line.Orientation == orientation); }

    private void ConfigurationManager_Applied (object? sender, ConfigurationManagerEventArgs e)
    {
        foreach (KeyValuePair<IntersectionRuneType, IntersectionRuneResolver> irr in runeResolvers)
        {
            irr.Value.SetGlyphs ();
        }
    }

    /// <summary>
    ///     Returns true if all requested <paramref name="types"/> appear in <paramref name="intersects"/> and there are no additional
    ///     <see cref="IntersectionRuneType"/>
    /// </summary>
    /// <param name="intersects"></param>
    /// <param name="types"></param>
    /// <returns></returns>
    private bool Exactly (HashSet<IntersectionType> intersects, params IntersectionType [] types) { return intersects.SetEquals (types); }

    private Attribute? GetAttributeForIntersects (IntersectionDefinition? [] intersects) { return intersects [0]!.Line.Attribute; }

    private Cell? GetCellForIntersects (ConsoleDriver driver, IntersectionDefinition? [] intersects)
    {
        if (!intersects.Any ())
        {
            return null;
        }

        var cell = new Cell ();
        Rune? rune = GetRuneForIntersects (driver, intersects);

        if (rune.HasValue)
        {
            cell.Rune = rune.Value;
        }

        cell.Attribute = GetAttributeForIntersects (intersects);

        return cell;
    }

    private Rune? GetRuneForIntersects (ConsoleDriver driver, IntersectionDefinition? [] intersects)
    {
        if (!intersects.Any ())
        {
            return null;
        }

        IntersectionRuneType runeType = GetRuneTypeForIntersects (intersects);

        if (runeResolvers.TryGetValue (runeType, out IntersectionRuneResolver? resolver))
        {
            return resolver.GetRuneForIntersects (driver, intersects);
        }

        // TODO: Remove these once we have all of the below ported to IntersectionRuneResolvers
        bool useDouble = intersects.Any (i => i?.Line.Style == LineStyle.Double);

        bool useDashed = intersects.Any (
                                         i => i?.Line.Style == LineStyle.Dashed
                                              || i?.Line.Style == LineStyle.RoundedDashed
                                        );

        bool useDotted = intersects.Any (
                                         i => i?.Line.Style == LineStyle.Dotted
                                              || i?.Line.Style == LineStyle.RoundedDotted
                                        );

        // horiz and vert lines same as Single for Rounded
        bool useThick = intersects.Any (i => i?.Line.Style == LineStyle.Heavy);
        bool useThickDashed = intersects.Any (i => i?.Line.Style == LineStyle.HeavyDashed);
        bool useThickDotted = intersects.Any (i => i?.Line.Style == LineStyle.HeavyDotted);

        // TODO: Support ruler
        //var useRuler = intersects.Any (i => i.Line.Style == LineStyle.Ruler && i.Line.Length != 0);

        // TODO: maybe make these resolvers too for simplicity?
        switch (runeType)
        {
            case IntersectionRuneType.None:
                return null;
            case IntersectionRuneType.Dot:
                return Glyphs.Dot;
            case IntersectionRuneType.HLine:
                if (useDouble)
                {
                    return Glyphs.HLineDbl;
                }

                if (useDashed)
                {
                    return Glyphs.HLineDa2;
                }

                if (useDotted)
                {
                    return Glyphs.HLineDa3;
                }

                return useThick ? Glyphs.HLineHv :
                       useThickDashed ? Glyphs.HLineHvDa2 :
                       useThickDotted ? Glyphs.HLineHvDa3 : Glyphs.HLine;
            case IntersectionRuneType.VLine:
                if (useDouble)
                {
                    return Glyphs.VLineDbl;
                }

                if (useDashed)
                {
                    return Glyphs.VLineDa3;
                }

                if (useDotted)
                {
                    return Glyphs.VLineDa4;
                }

                return useThick ? Glyphs.VLineHv :
                       useThickDashed ? Glyphs.VLineHvDa3 :
                       useThickDotted ? Glyphs.VLineHvDa4 : Glyphs.VLine;

            default:
                throw new Exception (
                                     "Could not find resolver or switch case for "
                                     + nameof (runeType)
                                     + ":"
                                     + runeType
                                    );
        }
    }

    private IntersectionRuneType GetRuneTypeForIntersects (IntersectionDefinition? [] intersects)
    {
        HashSet<IntersectionType> set = new (intersects.Select (i => i!.Type));

        #region Cross Conditions

        if (Has (
                 set,
                 IntersectionType.PassOverHorizontal,
                 IntersectionType.PassOverVertical
                ))
        {
            return IntersectionRuneType.Cross;
        }

        if (Has (
                 set,
                 IntersectionType.PassOverVertical,
                 IntersectionType.StartLeft,
                 IntersectionType.StartRight
                ))
        {
            return IntersectionRuneType.Cross;
        }

        if (Has (
                 set,
                 IntersectionType.PassOverHorizontal,
                 IntersectionType.StartUp,
                 IntersectionType.StartDown
                ))
        {
            return IntersectionRuneType.Cross;
        }

        if (Has (
                 set,
                 IntersectionType.StartLeft,
                 IntersectionType.StartRight,
                 IntersectionType.StartUp,
                 IntersectionType.StartDown
                ))
        {
            return IntersectionRuneType.Cross;
        }

        #endregion

        #region Corner Conditions

        if (Exactly (
                     set,
                     IntersectionType.StartRight,
                     IntersectionType.StartDown
                    ))
        {
            return IntersectionRuneType.ULCorner;
        }

        if (Exactly (
                     set,
                     IntersectionType.StartLeft,
                     IntersectionType.StartDown
                    ))
        {
            return IntersectionRuneType.URCorner;
        }

        if (Exactly (
                     set,
                     IntersectionType.StartUp,
                     IntersectionType.StartLeft
                    ))
        {
            return IntersectionRuneType.LRCorner;
        }

        if (Exactly (
                     set,
                     IntersectionType.StartUp,
                     IntersectionType.StartRight
                    ))
        {
            return IntersectionRuneType.LLCorner;
        }

        #endregion Corner Conditions

        #region T Conditions

        if (Has (
                 set,
                 IntersectionType.PassOverHorizontal,
                 IntersectionType.StartDown
                ))
        {
            return IntersectionRuneType.TopTee;
        }

        if (Has (
                 set,
                 IntersectionType.StartRight,
                 IntersectionType.StartLeft,
                 IntersectionType.StartDown
                ))
        {
            return IntersectionRuneType.TopTee;
        }

        if (Has (
                 set,
                 IntersectionType.PassOverHorizontal,
                 IntersectionType.StartUp
                ))
        {
            return IntersectionRuneType.BottomTee;
        }

        if (Has (
                 set,
                 IntersectionType.StartRight,
                 IntersectionType.StartLeft,
                 IntersectionType.StartUp
                ))
        {
            return IntersectionRuneType.BottomTee;
        }

        if (Has (
                 set,
                 IntersectionType.PassOverVertical,
                 IntersectionType.StartRight
                ))
        {
            return IntersectionRuneType.LeftTee;
        }

        if (Has (
                 set,
                 IntersectionType.StartRight,
                 IntersectionType.StartDown,
                 IntersectionType.StartUp
                ))
        {
            return IntersectionRuneType.LeftTee;
        }

        if (Has (
                 set,
                 IntersectionType.PassOverVertical,
                 IntersectionType.StartLeft
                ))
        {
            return IntersectionRuneType.RightTee;
        }

        if (Has (
                 set,
                 IntersectionType.StartLeft,
                 IntersectionType.StartDown,
                 IntersectionType.StartUp
                ))
        {
            return IntersectionRuneType.RightTee;
        }

        #endregion

        if (All (intersects, Orientation.Horizontal))
        {
            return IntersectionRuneType.HLine;
        }

        if (All (intersects, Orientation.Vertical))
        {
            return IntersectionRuneType.VLine;
        }

        return IntersectionRuneType.Dot;
    }

    /// <summary>
    ///     Returns true if the <paramref name="intersects"/> collection has all the <paramref name="types"/> specified (i.e. AND).
    /// </summary>
    /// <param name="intersects"></param>
    /// <param name="types"></param>
    /// <returns></returns>
    private bool Has (HashSet<IntersectionType> intersects, params IntersectionType [] types) { return types.All (t => intersects.Contains (t)); }

    private class BottomTeeIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.BottomTee;
            _doubleH = Glyphs.BottomTeeDblH;
            _doubleV = Glyphs.BottomTeeDblV;
            _doubleBoth = Glyphs.BottomTeeDbl;
            _thickH = Glyphs.BottomTeeHvH;
            _thickV = Glyphs.BottomTeeHvV;
            _thickBoth = Glyphs.BottomTeeHvDblH;
            _normal = Glyphs.BottomTee;
        }
    }

    private class CrossIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.Cross;
            _doubleH = Glyphs.CrossDblH;
            _doubleV = Glyphs.CrossDblV;
            _doubleBoth = Glyphs.CrossDbl;
            _thickH = Glyphs.CrossHvH;
            _thickV = Glyphs.CrossHvV;
            _thickBoth = Glyphs.CrossHv;
            _normal = Glyphs.Cross;
        }
    }

    private abstract class IntersectionRuneResolver
    {
        public IntersectionRuneResolver () { SetGlyphs (); }
        internal Rune _doubleBoth;
        internal Rune _doubleH;
        internal Rune _doubleV;
        internal Rune _normal;
        internal Rune _round;
        internal Rune _thickBoth;
        internal Rune _thickH;
        internal Rune _thickV;

        public Rune? GetRuneForIntersects (ConsoleDriver driver, IntersectionDefinition? [] intersects)
        {
            bool useRounded = intersects.Any (
                                              i => i?.Line.Length != 0
                                                   && (
                                                          i?.Line.Style == LineStyle.Rounded
                                                          || i?.Line.Style
                                                          == LineStyle.RoundedDashed
                                                          || i?.Line.Style
                                                          == LineStyle.RoundedDotted)
                                             );

            // Note that there aren't any glyphs for intersections of double lines with heavy lines

            bool doubleHorizontal = intersects.Any (
                                                    l => l?.Line.Orientation == Orientation.Horizontal
                                                         && l.Line.Style == LineStyle.Double
                                                   );

            bool doubleVertical = intersects.Any (
                                                  l => l?.Line.Orientation == Orientation.Vertical
                                                       && l.Line.Style == LineStyle.Double
                                                 );

            bool thickHorizontal = intersects.Any (
                                                   l => l?.Line.Orientation == Orientation.Horizontal
                                                        && (
                                                               l.Line.Style == LineStyle.Heavy
                                                               || l.Line.Style == LineStyle.HeavyDashed
                                                               || l.Line.Style == LineStyle.HeavyDotted)
                                                  );

            bool thickVertical = intersects.Any (
                                                 l => l?.Line.Orientation == Orientation.Vertical
                                                      && (
                                                             l.Line.Style == LineStyle.Heavy
                                                             || l.Line.Style == LineStyle.HeavyDashed
                                                             || l.Line.Style == LineStyle.HeavyDotted)
                                                );

            if (doubleHorizontal)
            {
                return doubleVertical ? _doubleBoth : _doubleH;
            }

            if (doubleVertical)
            {
                return _doubleV;
            }

            if (thickHorizontal)
            {
                return thickVertical ? _thickBoth : _thickH;
            }

            if (thickVertical)
            {
                return _thickV;
            }

            return useRounded ? _round : _normal;
        }

        /// <summary>Sets the glyphs used. Call this method after construction and any time ConfigurationManager has updated the settings.</summary>
        public abstract void SetGlyphs ();
    }

    private class LeftTeeIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.LeftTee;
            _doubleH = Glyphs.LeftTeeDblH;
            _doubleV = Glyphs.LeftTeeDblV;
            _doubleBoth = Glyphs.LeftTeeDbl;
            _thickH = Glyphs.LeftTeeHvH;
            _thickV = Glyphs.LeftTeeHvV;
            _thickBoth = Glyphs.LeftTeeHvDblH;
            _normal = Glyphs.LeftTee;
        }
    }

    private class LLIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.LLCornerR;
            _doubleH = Glyphs.LLCornerSingleDbl;
            _doubleV = Glyphs.LLCornerDblSingle;
            _doubleBoth = Glyphs.LLCornerDbl;
            _thickH = Glyphs.LLCornerLtHv;
            _thickV = Glyphs.LLCornerHvLt;
            _thickBoth = Glyphs.LLCornerHv;
            _normal = Glyphs.LLCorner;
        }
    }

    private class LRIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.LRCornerR;
            _doubleH = Glyphs.LRCornerSingleDbl;
            _doubleV = Glyphs.LRCornerDblSingle;
            _doubleBoth = Glyphs.LRCornerDbl;
            _thickH = Glyphs.LRCornerLtHv;
            _thickV = Glyphs.LRCornerHvLt;
            _thickBoth = Glyphs.LRCornerHv;
            _normal = Glyphs.LRCorner;
        }
    }

    private class RightTeeIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.RightTee;
            _doubleH = Glyphs.RightTeeDblH;
            _doubleV = Glyphs.RightTeeDblV;
            _doubleBoth = Glyphs.RightTeeDbl;
            _thickH = Glyphs.RightTeeHvH;
            _thickV = Glyphs.RightTeeHvV;
            _thickBoth = Glyphs.RightTeeHvDblH;
            _normal = Glyphs.RightTee;
        }
    }

    private class TopTeeIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.TopTee;
            _doubleH = Glyphs.TopTeeDblH;
            _doubleV = Glyphs.TopTeeDblV;
            _doubleBoth = Glyphs.TopTeeDbl;
            _thickH = Glyphs.TopTeeHvH;
            _thickV = Glyphs.TopTeeHvV;
            _thickBoth = Glyphs.TopTeeHvDblH;
            _normal = Glyphs.TopTee;
        }
    }

    private class ULIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.ULCornerR;
            _doubleH = Glyphs.ULCornerSingleDbl;
            _doubleV = Glyphs.ULCornerDblSingle;
            _doubleBoth = Glyphs.ULCornerDbl;
            _thickH = Glyphs.ULCornerLtHv;
            _thickV = Glyphs.ULCornerHvLt;
            _thickBoth = Glyphs.ULCornerHv;
            _normal = Glyphs.ULCorner;
        }
    }

    private class URIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.URCornerR;
            _doubleH = Glyphs.URCornerSingleDbl;
            _doubleV = Glyphs.URCornerDblSingle;
            _doubleBoth = Glyphs.URCornerDbl;
            _thickH = Glyphs.URCornerHvLt;
            _thickV = Glyphs.URCornerLtHv;
            _thickBoth = Glyphs.URCornerHv;
            _normal = Glyphs.URCorner;
        }
    }
}

internal class IntersectionDefinition
{
    internal IntersectionDefinition (Point point, IntersectionType type, StraightLine line)
    {
        Point = point;
        Type = type;
        Line = line;
    }

    /// <summary>The line that intersects <see cref="Point"/></summary>
    internal StraightLine Line { get; }

    /// <summary>The point at which the intersection happens</summary>
    internal Point Point { get; }

    /// <summary>Defines how <see cref="Line"/> position relates to <see cref="Point"/>.</summary>
    internal IntersectionType Type { get; }
}

/// <summary>The type of Rune that we will use before considering double width, curved borders etc</summary>
internal enum IntersectionRuneType
{
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
    VLine
}

internal enum IntersectionType
{
    /// <summary>There is no intersection</summary>
    None,

    /// <summary>A line passes directly over this point traveling along the horizontal axis</summary>
    PassOverHorizontal,

    /// <summary>A line passes directly over this point traveling along the vertical axis</summary>
    PassOverVertical,

    /// <summary>A line starts at this point and is traveling up</summary>
    StartUp,

    /// <summary>A line starts at this point and is traveling right</summary>
    StartRight,

    /// <summary>A line starts at this point and is traveling down</summary>
    StartDown,

    /// <summary>A line starts at this point and is traveling left</summary>
    StartLeft,

    /// <summary>A line exists at this point who has 0 length</summary>
    Dot
}
