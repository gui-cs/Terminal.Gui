namespace Terminal.Gui.Views;

/// <summary>Represents the parsed structure of a Markdown table.</summary>
public sealed class TableData
{
    /// <summary>Initializes a new <see cref="TableData"/> from parsed table rows.</summary>
    /// <param name="headers">The header cell texts.</param>
    /// <param name="alignments">The column alignments parsed from the separator row.</param>
    /// <param name="rows">The body row cell texts.</param>
    public TableData (string [] headers, Alignment [] alignments, string [] [] rows)
    {
        Headers = headers;
        ColumnAlignments = alignments;
        Rows = rows;
        ColumnCount = headers.Length;
    }

    /// <summary>Gets the header cell texts.</summary>
    public string [] Headers { get; }

    /// <summary>Gets the alignment for each column, parsed from the separator row.</summary>
    public Alignment [] ColumnAlignments { get; }

    /// <summary>Gets the body rows. Each row is an array of cell texts.</summary>
    public string [] [] Rows { get; }

    /// <summary>Gets the number of columns in the table.</summary>
    public int ColumnCount { get; }

    /// <summary>Parses consecutive raw table lines into a <see cref="TableData"/> instance.</summary>
    /// <param name="lines">Raw markdown table lines (header, separator, body rows).</param>
    /// <returns>
    ///     A <see cref="TableData"/> if at least a header and separator row are present; otherwise <see langword="null"/>
    ///     .
    /// </returns>
    public static TableData? TryParse (IReadOnlyList<string> lines)
    {
        if (lines.Count < 2)
        {
            return null;
        }

        string [] headers = SplitRow (lines [0]);

        if (headers.Length == 0)
        {
            return null;
        }

        // Second line must be the separator row
        string [] separators = SplitRow (lines [1]);

        if (separators.Length == 0 || !IsSeparatorRow (separators))
        {
            return null;
        }

        Alignment [] alignments = ParseAlignments (separators, headers.Length);

        List<string []> rows = [];

        for (var i = 2; i < lines.Count; i++)
        {
            string [] cells = SplitRow (lines [i]);

            // Pad or truncate to match header column count
            var normalized = new string [headers.Length];

            for (var c = 0; c < headers.Length; c++)
            {
                normalized [c] = c < cells.Length ? cells [c] : string.Empty;
            }

            rows.Add (normalized);
        }

        return new TableData (headers, alignments, rows.ToArray ());
    }

    private static string [] SplitRow (string line)
    {
        string trimmed = line.Trim ().Trim ('|');

        if (string.IsNullOrEmpty (trimmed))
        {
            return [];
        }

        string [] cells = trimmed.Split ('|');

        for (var i = 0; i < cells.Length; i++)
        {
            cells [i] = cells [i].Trim ();
        }

        return cells;
    }

    private static bool IsSeparatorRow (string [] cells)
    {
        foreach (string cell in cells)
        {
            string trimmed = cell.Trim (':').Trim ();

            if (trimmed.Length == 0 || trimmed.Any (c => c != '-'))
            {
                return false;
            }
        }

        return true;
    }

    private static Alignment [] ParseAlignments (string [] separators, int columnCount)
    {
        Alignment [] alignments = new Alignment [columnCount];

        for (var i = 0; i < columnCount; i++)
        {
            if (i >= separators.Length)
            {
                alignments [i] = Alignment.Start;

                continue;
            }

            string sep = separators [i].Trim ();
            bool leftColon = sep.StartsWith (':');
            bool rightColon = sep.EndsWith (':');

            if (leftColon && rightColon)
            {
                alignments [i] = Alignment.Center;
            }
            else if (rightColon)
            {
                alignments [i] = Alignment.End;
            }
            else
            {
                alignments [i] = Alignment.Start;
            }
        }

        return alignments;
    }
}
