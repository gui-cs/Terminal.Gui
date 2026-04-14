namespace Terminal.Gui.Views;

/// <summary>
///     A read-only view that renders a single Markdown table with box-drawing borders via
///     <see cref="LineCanvas"/> and styled header/body text with inline Markdown formatting.
/// </summary>
/// <remarks>
///     <para>
///         This view is created and managed internally by <see cref="MarkdownView"/> during layout.
///         It is positioned as a SubView at the correct content coordinate so that it scrolls
///         naturally with the parent's viewport.
///     </para>
///     <para>
///         Borders are rendered using <see cref="LineStyle.Single"/> via the view's
///         <see cref="LineCanvas"/> with <see cref="View.SuperViewRendersLineCanvas"/> set to
///         <see langword="true"/>, so that the parent <see cref="MarkdownView"/> merges the table's
///         border lines into its own <see cref="LineCanvas"/> and renders them. This ensures correct
///         coordinate transformation for SubViews within scrolling parents.
///     </para>
///     <para>
///         Header cells are bold; body cells support inline Markdown formatting (bold, italic, code,
///         links) via <see cref="MarkdownInlineParser"/>. Column alignment (left, center, right)
///         parsed from the Markdown separator row is respected. Cells that exceed their column width
///         are word-wrapped, and row heights expand to accommodate wrapped content.
///     </para>
/// </remarks>
internal sealed class MarkdownTable : View
{
    private readonly TableData _data;
    private readonly int [] _columnWidths;

    // Pre-parsed inline segments for each cell
    private readonly List<StyledSegment> [] _headerSegments;
    private readonly List<StyledSegment> [] [] _rowSegments;

    // Pre-computed wrapped line counts per row
    private readonly int _headerRowHeight;
    private readonly int [] _bodyRowHeights;

    /// <summary>Initializes a new <see cref="MarkdownTable"/> for the given parsed table data.</summary>
    /// <param name="data">The parsed table structure.</param>
    /// <param name="maxWidth">
    ///     The maximum available width. Column widths are clamped so the total table width
    ///     does not exceed this value when possible.
    /// </param>
    public MarkdownTable (TableData data, int maxWidth)
    {
        _data = data;
        CanFocus = false;
        TabStop = TabBehavior.NoStop;

        // Let the parent (MarkdownView) merge and render our LineCanvas borders
        SuperViewRendersLineCanvas = true;

        // Parse inline markdown for all cells upfront
        _headerSegments = ParseCellSegments (data.Headers, MarkdownStyleRole.Heading);
        _rowSegments = new List<StyledSegment> [data.Rows.Length] [];

        for (var r = 0; r < data.Rows.Length; r++)
        {
            _rowSegments [r] = ParseCellSegments (data.Rows [r], MarkdownStyleRole.Normal);
        }

        _columnWidths = ComputeColumnWidths (data, maxWidth);

        // Compute row heights based on word-wrapped cell content
        _headerRowHeight = ComputeRowHeight (_headerSegments, _columnWidths);
        _bodyRowHeights = new int [data.Rows.Length];

        for (var r = 0; r < data.Rows.Length; r++)
        {
            _bodyRowHeights [r] = ComputeRowHeight (_rowSegments [r], _columnWidths);
        }

        int tableWidth = CalculateTableWidth (_columnWidths);
        int tableHeight = CalculateTableHeightWrapped (_headerRowHeight, _bodyRowHeights);

        Width = tableWidth;
        Height = tableHeight;

        // No adornments — we draw everything ourselves
        BorderStyle = LineStyle.None;
        Border.Thickness = new Thickness (0);
        Padding.Thickness = new Thickness (0);
        Margin.Thickness = new Thickness (0);
    }

    /// <summary>Gets the total rendered height of this table in lines (simple estimate).</summary>
    /// <remarks>
    ///     This simple estimation assumes single-line rows. Used by external callers that don't have
    ///     wrapped row heights. For the actual rendered height, use the instance's <see cref="View.Height"/>.
    /// </remarks>
    public static int CalculateTableHeight (TableData data) =>

        // top border + header + header separator + body rows + bottom border
        data.Rows.Length + 4;

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        DrawBorders ();
        DrawCellContents ();

        return true;
    }

    private void DrawCellContents ()
    {
        var y = 1; // Below top border

        // Header row
        DrawWrappedRow (_headerSegments, _data.ColumnAlignments, y, _headerRowHeight, true);
        y += _headerRowHeight;

        // Skip header separator (1 line)
        y++;

        // Body rows
        for (var r = 0; r < _rowSegments.Length; r++)
        {
            DrawWrappedRow (_rowSegments [r], _data.ColumnAlignments, y, _bodyRowHeights [r], false);
            y += _bodyRowHeights [r];
        }
    }

    private void DrawWrappedRow (List<StyledSegment> [] cellSegments, Alignment [] alignments, int startY, int rowHeight, bool isHeader)
    {
        Attribute normal = GetAttributeForRole (VisualRole.Normal);

        for (var lineInRow = 0; lineInRow < rowHeight; lineInRow++)
        {
            int y = startY + lineInRow;
            var x = 1; // After left border

            for (var col = 0; col < _columnWidths.Length; col++)
            {
                int colWidth = _columnWidths [col];
                int innerWidth = colWidth - 2; // 1 space padding each side

                List<StyledSegment> segments = col < cellSegments.Length ? cellSegments [col] : [];

                // Word-wrap segments into lines for this cell
                List<List<StyledSegment>> wrappedLines = WrapSegments (segments, innerWidth);

                // Fill the cell area with spaces first
                SetAttribute (normal);

                for (var i = 0; i < colWidth; i++)
                {
                    AddStr (x + i, y, " ");
                }

                // Draw this line's content (if we have it)
                if (lineInRow < wrappedLines.Count)
                {
                    List<StyledSegment> lineSegs = wrappedLines [lineInRow];

                    // Calculate text width for alignment
                    var textWidth = 0;

                    foreach (StyledSegment seg in lineSegs)
                    {
                        textWidth += seg.Text.GetColumns ();
                    }

                    Alignment alignment = col < alignments.Length ? alignments [col] : Alignment.Start;
                    int padLeft = CalculateLeftPadding (colWidth, Math.Min (textWidth, innerWidth), alignment);

                    int drawX = x + padLeft;

                    foreach (StyledSegment seg in lineSegs)
                    {
                        Attribute attr = MarkdownAttributeHelper.GetAttributeForSegment (this, seg);

                        if (isHeader)
                        {
                            attr = attr with { Style = attr.Style | TextStyle.Bold };
                        }

                        SetAttribute (attr);

                        foreach (string grapheme in GraphemeHelper.GetGraphemes (seg.Text))
                        {
                            int gw = Math.Max (grapheme.GetColumns (), 1);

                            if (drawX - x >= colWidth - 1)
                            {
                                break;
                            }

                            AddStr (drawX, y, grapheme);
                            drawX += gw;
                        }
                    }
                }

                // Advance past column width + separator character
                x += colWidth + 1;
            }
        }
    }

    /// <summary>
    ///     Adds border lines to <see cref="View.LineCanvas"/> using screen coordinates so that the
    ///     parent view can merge and render them via <see cref="View.SuperViewRendersLineCanvas"/>.
    /// </summary>
    private void DrawBorders ()
    {
        int tableWidth = CalculateTableWidth (_columnWidths);
        int tableHeight = CalculateTableHeightWrapped (_headerRowHeight, _bodyRowHeights);

        Point screenOrigin = ViewportToScreen (Viewport).Location;
        Attribute borderAttr = GetAttributeForRole (VisualRole.Normal);

        // Top border (row 0)
        LineCanvas.AddLine (screenOrigin, tableWidth, Orientation.Horizontal, LineStyle.Single, borderAttr);

        // Header separator (below header rows)
        int headerSepY = screenOrigin.Y + 1 + _headerRowHeight;
        LineCanvas.AddLine (screenOrigin with { Y = headerSepY }, tableWidth, Orientation.Horizontal, LineStyle.Single, borderAttr);

        // Bottom border (last row)
        LineCanvas.AddLine (screenOrigin with { Y = screenOrigin.Y + tableHeight - 1 }, tableWidth, Orientation.Horizontal, LineStyle.Single, borderAttr);

        // Left border (full height)
        LineCanvas.AddLine (screenOrigin, tableHeight, Orientation.Vertical, LineStyle.Single, borderAttr);

        // Right border (full height)
        LineCanvas.AddLine (screenOrigin with { X = screenOrigin.X + tableWidth - 1 }, tableHeight, Orientation.Vertical, LineStyle.Single, borderAttr);

        // Column separators (vertical lines between columns)
        var xOffset = 0;

        for (var col = 0; col < _columnWidths.Length; col++)
        {
            xOffset += _columnWidths [col] + 1; // column width + border char

            if (col < _columnWidths.Length - 1)
            {
                LineCanvas.AddLine (screenOrigin with { X = screenOrigin.X + xOffset }, tableHeight, Orientation.Vertical, LineStyle.Single, borderAttr);
            }
        }
    }

    /// <summary>
    ///     Word-wraps a list of styled segments into multiple lines, each fitting within
    ///     <paramref name="maxWidth"/> display columns.
    /// </summary>
    internal static List<List<StyledSegment>> WrapSegments (List<StyledSegment> segments, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return [[]];
        }

        List<List<StyledSegment>> lines = [];
        List<StyledSegment> currentLine = [];
        var currentWidth = 0;

        foreach (StyledSegment segment in segments)
        {
            string remaining = segment.Text;

            while (remaining.Length > 0)
            {
                int spaceIdx = remaining.IndexOf (' ');
                string word;
                string trailing;

                if (spaceIdx >= 0)
                {
                    word = remaining [..spaceIdx];
                    trailing = " ";
                    remaining = remaining [(spaceIdx + 1)..];
                }
                else
                {
                    word = remaining;
                    trailing = "";
                    remaining = "";
                }

                string chunk = word + trailing;
                int chunkWidth = chunk.GetColumns ();

                // If adding this word would exceed the line, wrap
                if (currentWidth > 0 && currentWidth + chunkWidth > maxWidth)
                {
                    lines.Add (currentLine);
                    currentLine = [];
                    currentWidth = 0;
                }

                // If a single word is wider than maxWidth, hard-break it
                if (chunkWidth > maxWidth && currentWidth == 0)
                {
                    string hardChunk = TruncateToWidth (chunk, maxWidth);
                    currentLine.Add (new StyledSegment (hardChunk, segment.StyleRole, segment.Url, segment.ImageSource));
                    lines.Add (currentLine);
                    currentLine = [];
                    currentWidth = 0;

                    int usedChars = hardChunk.TrimEnd ().Length;
                    remaining = chunk [usedChars..].TrimStart () + remaining;

                    continue;
                }

                currentLine.Add (new StyledSegment (chunk, segment.StyleRole, segment.Url, segment.ImageSource));
                currentWidth += chunkWidth;
            }
        }

        if (currentLine.Count > 0)
        {
            lines.Add (currentLine);
        }

        if (lines.Count == 0)
        {
            lines.Add ([]);
        }

        return lines;
    }

    private static List<StyledSegment> [] ParseCellSegments (string [] cells, MarkdownStyleRole defaultRole)
    {
        List<StyledSegment> [] result = new List<StyledSegment> [cells.Length];

        for (var i = 0; i < cells.Length; i++)
        {
            List<InlineRun> runs = MarkdownInlineParser.ParseInlines (cells [i], defaultRole);
            result [i] = MarkdownAttributeHelper.ToStyledSegments (runs);
        }

        return result;
    }

    /// <summary>Computes the row height needed for a row given word wrapping of cell contents.</summary>
    private static int ComputeRowHeight (List<StyledSegment> [] cellSegments, int [] columnWidths)
    {
        var maxLines = 1;

        for (var col = 0; col < columnWidths.Length && col < cellSegments.Length; col++)
        {
            int innerWidth = columnWidths [col] - 2;
            List<List<StyledSegment>> wrapped = WrapSegments (cellSegments [col], innerWidth);
            maxLines = Math.Max (maxLines, wrapped.Count);
        }

        return maxLines;
    }

    /// <summary>Gets the total rendered height given pre-computed row heights.</summary>
    private static int CalculateTableHeightWrapped (int headerHeight, int [] bodyRowHeights)
    {
        // top border (1) + header rows + header separator (1) + body rows + bottom border (1)
        int total = 3 + headerHeight;

        foreach (int h in bodyRowHeights)
        {
            total += h;
        }

        return total;
    }

    private static int [] ComputeColumnWidths (TableData data, int maxWidth)
    {
        var widths = new int [data.ColumnCount];

        // Start with header widths (strip markdown formatting for measurement)
        for (var c = 0; c < data.ColumnCount; c++)
        {
            List<InlineRun> runs = MarkdownInlineParser.ParseInlines (data.Headers [c], MarkdownStyleRole.Normal);
            var textWidth = 0;

            foreach (InlineRun run in runs)
            {
                textWidth += run.Text.GetColumns ();
            }

            widths [c] = Math.Max (textWidth, 1);
        }

        // Expand to fit body cell content (strip markdown for measurement)
        foreach (string [] row in data.Rows)
        {
            for (var c = 0; c < data.ColumnCount && c < row.Length; c++)
            {
                List<InlineRun> runs = MarkdownInlineParser.ParseInlines (row [c], MarkdownStyleRole.Normal);
                var textWidth = 0;

                foreach (InlineRun run in runs)
                {
                    textWidth += run.Text.GetColumns ();
                }

                widths [c] = Math.Max (widths [c], textWidth);
            }
        }

        // Add 2 for cell padding (1 space each side)
        for (var c = 0; c < widths.Length; c++)
        {
            widths [c] += 2;
        }

        // Check if total exceeds maxWidth; if so, shrink proportionally
        int totalWidth = CalculateTableWidth (widths);

        if (totalWidth > maxWidth && maxWidth > data.ColumnCount * 3 + data.ColumnCount + 1)
        {
            int available = maxWidth - data.ColumnCount - 1; // subtract border chars
            int currentContent = widths.Sum ();
            double ratio = (double)available / currentContent;

            for (var c = 0; c < widths.Length; c++)
            {
                widths [c] = Math.Max ((int)(widths [c] * ratio), 3);
            }
        }

        return widths;
    }

    /// <summary>Calculates the total table width including all borders.</summary>
    private static int CalculateTableWidth (int [] columnWidths) => columnWidths.Sum () + columnWidths.Length + 1;

    private static int CalculateLeftPadding (int cellWidth, int textWidth, Alignment alignment)
    {
        int innerWidth = cellWidth - 2;
        int usableTextWidth = Math.Min (textWidth, innerWidth);

        return alignment switch
               {
                   Alignment.Center => 1 + Math.Max ((innerWidth - usableTextWidth) / 2, 0),
                   Alignment.End => 1 + Math.Max (innerWidth - usableTextWidth, 0),
                   _ => 1
               };
    }

    private static string TruncateToWidth (string text, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var width = 0;
        var charCount = 0;

        foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
        {
            int gw = Math.Max (grapheme.GetColumns (), 1);

            if (width + gw > maxWidth)
            {
                break;
            }

            width += gw;
            charCount += grapheme.Length;
        }

        return text [..charCount];
    }
}
