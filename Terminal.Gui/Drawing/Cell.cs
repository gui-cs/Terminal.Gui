#nullable enable


namespace Terminal.Gui.Drawing;

/// <summary>
///     Represents a single row/column in a Terminal.Gui rendering surface (e.g. <see cref="LineCanvas"/> and
///     <see cref="IConsoleDriver"/>).
/// </summary>
public record struct Cell (Attribute? Attribute = null, bool IsDirty = false, Rune Rune = default)
{
    /// <summary>The attributes to use when drawing the Glyph.</summary>
    public Attribute? Attribute { get; set; } = Attribute;

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.Cell"/> has been modified since the
    ///     last time it was drawn.
    /// </summary>
    public bool IsDirty { get; set; } = IsDirty;

    private Rune _rune = Rune;

    /// <summary>The character to display. If <see cref="Rune"/> is <see langword="null"/>, then <see cref="Rune"/> is ignored.</summary>
    public Rune Rune
    {
        get => _rune;
        set
        {
            _combiningMarks?.Clear ();
            _rune = value;
        }
    }

    private List<Rune>? _combiningMarks;

    /// <summary>
    ///     The combining marks for <see cref="Rune"/> that when combined makes this Cell a combining sequence. If
    ///     <see cref="CombiningMarks"/> empty, then <see cref="CombiningMarks"/> is ignored.
    /// </summary>
    /// <remarks>
    ///     Only valid in the rare case where <see cref="Rune"/> is a combining sequence that could not be normalized to a
    ///     single Rune.
    /// </remarks>
    internal IReadOnlyList<Rune> CombiningMarks
    {
        // PERFORMANCE: Downside of the interface return type is that List<T> struct enumerator cannot be utilized, i.e. enumerator is allocated.
        // If enumeration is used heavily in the future then might be better to expose the List<T> Enumerator directly via separate mechanism.
        get
        {
            // Avoid unnecessary list allocation.
            if (_combiningMarks == null)
            {
                return Array.Empty<Rune> ();
            }
            return _combiningMarks;
        }
    }

    /// <summary>
    ///     Adds combining mark to the cell.
    /// </summary>
    /// <param name="combiningMark">The combining mark to add to the cell.</param>
    internal void AddCombiningMark (Rune combiningMark)
    {
        _combiningMarks ??= [];
        _combiningMarks.Add (combiningMark);
    }

    /// <summary>
    ///     Clears combining marks of the cell.
    /// </summary>
    internal void ClearCombiningMarks ()
    {
        _combiningMarks?.Clear ();
    }

    /// <inheritdoc/>
    public override string ToString () { return $"['{Rune}':{Attribute}]"; }

    /// <summary>Converts the string into a <see cref="List{Cell}"/>.</summary>
    /// <param name="str">The string to convert.</param>
    /// <param name="attribute">The <see cref="Scheme"/> to use.</param>
    /// <returns></returns>
    public static List<Cell> ToCellList (string str, Attribute? attribute = null)
    {
        List<Cell> cells = new ();

        foreach (Rune rune in str.EnumerateRunes ())
        {
            cells.Add (new () { Rune = rune, Attribute = attribute });
        }

        return cells;
    }

    /// <summary>
    ///     Splits a string into a List that will contain a <see cref="List{Cell}"/> for each line.
    /// </summary>
    /// <param name="content">The string content.</param>
    /// <param name="attribute">The scheme.</param>
    /// <returns>A <see cref="List{Cell}"/> for each line.</returns>
    public static List<List<Cell>> StringToLinesOfCells (string content, Attribute? attribute = null)
    {
        List<Cell> cells = content.EnumerateRunes ()
                                  .Select (x => new Cell { Rune = x, Attribute = attribute })
                                  .ToList ();

        return SplitNewLines (cells);
    }

    /// <summary>Converts a <see cref="Cell"/> generic collection into a string.</summary>
    /// <param name="cells">The enumerable cell to convert.</param>
    /// <returns></returns>
    public static string ToString (IEnumerable<Cell> cells)
    {
        var str = string.Empty;

        foreach (Cell cell in cells)
        {
            str += cell.Rune.ToString ();
        }

        return str;
    }

    /// <summary>Converts a <see cref="List{Cell}"/> generic collection into a string.</summary>
    /// <param name="cellsList">The enumerable cell to convert.</param>
    /// <returns></returns>
    public static string ToString (List<List<Cell>> cellsList)
    {
        var str = string.Empty;

        for (var i = 0; i < cellsList.Count; i++)
        {
            IEnumerable<Cell> cellList = cellsList [i];
            str += ToString (cellList);

            if (i + 1 < cellsList.Count)
            {
                str += Environment.NewLine;
            }
        }

        return str;
    }

    // Turns the string into cells, this does not split the contents on a newline if it is present.

    internal static List<Cell> StringToCells (string str, Attribute? attribute = null)
    {
        List<Cell> cells = [];

        foreach (Rune rune in str.ToRunes ())
        {
            cells.Add (new () { Rune = rune, Attribute = attribute });
        }

        return cells;
    }

    internal static List<Cell> ToCells (IEnumerable<Rune> runes, Attribute? attribute = null)
    {
        List<Cell> cells = new ();

        foreach (Rune rune in runes)
        {
            cells.Add (new () { Rune = rune, Attribute = attribute });
        }

        return cells;
    }

    private static List<List<Cell>> SplitNewLines (List<Cell> cells)
    {
        List<List<Cell>> lines = [];
        int start = 0, i = 0;
        var hasCR = false;

        // ASCII code 13 = Carriage Return.
        // ASCII code 10 = Line Feed.
        for (; i < cells.Count; i++)
        {
            if (cells [i].Rune.Value == 13)
            {
                hasCR = true;

                continue;
            }

            if (cells [i].Rune.Value == 10)
            {
                if (i - start > 0)
                {
                    lines.Add (cells.GetRange (start, hasCR ? i - 1 - start : i - start));
                }
                else
                {
                    lines.Add (StringToCells (string.Empty));
                }

                start = i + 1;
                hasCR = false;
            }
        }

        if (i - start >= 0)
        {
            lines.Add (cells.GetRange (start, i - start));
        }

        return lines;
    }

    /// <summary>
    ///     Splits a rune cell list into a List that will contain a <see cref="List{Cell}"/> for each line.
    /// </summary>
    /// <param name="cells">The cells list.</param>
    /// <returns></returns>
    public static List<List<Cell>> ToCells (List<Cell> cells) { return SplitNewLines (cells); }
}
