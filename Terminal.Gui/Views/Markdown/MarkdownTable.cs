namespace Terminal.Gui.Views;

/// <summary>
///     A read-only view that renders a single Markdown table with box-drawing borders via
///     <see cref="LineCanvas"/> and styled header/body text with inline Markdown formatting.
/// </summary>
/// <remarks>
///     <para>
///         When used inside a <see cref="MarkdownView"/>, instances are created automatically during
///         layout and positioned as SubViews at the correct content coordinate so that they scroll
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
///     <para>
///         This view can also be used standalone. Use the parameterless constructor and set
///         <see cref="Data"/> to provide table content.
///     </para>
/// </remarks>
public sealed class MarkdownTable : View, IDesignable
{
    private TableData _data;
    private int [] _columnWidths;

    // Pre-parsed inline segments for each cell
    private List<StyledSegment> [] _headerSegments;
    private List<StyledSegment> [] [] _rowSegments;

    // Pre-computed wrapped line counts per row
    private int _headerRowHeight;
    private int [] _bodyRowHeights;

    // Last width used for column computation — tracks when recalculation is needed
    private int _lastComputedWidth;

    private static readonly TableData _emptyData = new ([], [], []);

    /// <summary>Initializes a new empty <see cref="MarkdownTable"/>.</summary>
    public MarkdownTable () : this (_emptyData, 80) { }

    /// <summary>
    ///     Gets or sets the <see cref="Views.TableData"/> that defines the table content. Setting this
    ///     recomputes column widths, row heights, and redraws the table.
    /// </summary>
    public new TableData Data
    {
        get => _data;
        set
        {
            _data = value;
            _headerSegments = ParseCellSegments (value.Headers, MarkdownStyleRole.Heading);
            _rowSegments = new List<StyledSegment> [value.Rows.Length] [];

            for (var r = 0; r < value.Rows.Length; r++)
            {
                _rowSegments [r] = ParseCellSegments (value.Rows [r], MarkdownStyleRole.Normal);
            }

            _lastComputedWidth = -1;
            SetNeedsLayout ();
            SetNeedsDraw ();
        }
    }

    /// <summary>Initializes a new <see cref="MarkdownTable"/> for the given parsed table data.</summary>
    /// <param name="data">The parsed table structure.</param>
    /// <param name="maxWidth">
    ///     The maximum available width. Column widths are clamped so the total table width
    ///     does not exceed this value when possible.
    /// </param>
    public MarkdownTable (TableData data, int maxWidth)
    {
        // data is non-nullable but reflection-based AllViews tests may pass null!
        _data = data ?? _emptyData;
        CanFocus = false;
        TabStop = TabBehavior.NoStop;

        // Let the parent (MarkdownView) merge and render our LineCanvas borders
       // SuperViewRendersLineCanvas = true;

        // Parse inline markdown for all cells upfront
        _headerSegments = ParseCellSegments (_data.Headers, MarkdownStyleRole.Heading);
        _rowSegments = new List<StyledSegment> [_data.Rows.Length] [];

        for (var r = 0; r < _data.Rows.Length; r++)
        {
            _rowSegments [r] = ParseCellSegments (_data.Rows [r], MarkdownStyleRole.Normal);
        }

        _columnWidths = ComputeColumnWidths (_data, maxWidth);
        _lastComputedWidth = maxWidth;

        // Compute row heights based on word-wrapped cell content
        _headerRowHeight = ComputeRowHeight (_headerSegments, _columnWidths);
        _bodyRowHeights = new int [_data.Rows.Length];

        for (var r = 0; r < _data.Rows.Length; r++)
        {
            _bodyRowHeights [r] = ComputeRowHeight (_rowSegments [r], _columnWidths);
        }

        Height = CalculateTableHeightWrapped (_headerRowHeight, _bodyRowHeights);

        // No adornments — we draw everything ourselves
        BorderStyle = LineStyle.None;
        Border.Thickness = new Thickness (0);
        Padding.Thickness = new Thickness (0);
        Margin.Thickness = new Thickness (0);
    }

    /// <summary>Recalculates column widths and row heights for the given available width.</summary>
    private void Recalculate (int maxWidth)
    {
        if (maxWidth == _lastComputedWidth)
        {
            return;
        }

        _lastComputedWidth = maxWidth;
        _columnWidths = ComputeColumnWidths (_data, maxWidth);

        _headerRowHeight = ComputeRowHeight (_headerSegments, _columnWidths);

        if (_bodyRowHeights.Length != _rowSegments.Length)
        {
            _bodyRowHeights = new int [_rowSegments.Length];
        }

        for (var r = 0; r < _rowSegments.Length; r++)
        {
            _bodyRowHeights [r] = ComputeRowHeight (_rowSegments [r], _columnWidths);
        }

        Height = CalculateTableHeightWrapped (_headerRowHeight, _bodyRowHeights);
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
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        base.OnSubViewLayout (args);

        Recalculate (Frame.Width);
    }

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
                if (lineInRow >= wrappedLines.Count)
                {
                    // Advance past column width + separator character
                    x += colWidth + 1;

                    continue;
                }

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
        int tableWidth = Frame.Width;
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

                    // If maxWidth is too narrow for even one grapheme, take the first grapheme
                    // to guarantee forward progress and avoid an infinite loop.
                    if (hardChunk.Length == 0)
                    {
                        string firstGrapheme = GraphemeHelper.GetGraphemes (chunk).FirstOrDefault () ?? chunk [..1];
                        hardChunk = firstGrapheme;
                    }

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

    /// <summary>
    ///     Computes column widths using a Rich-style collapse algorithm:
    ///     <list type="number">
    ///         <item>Measure each column's min (longest word + padding) and max (full content + padding) widths.</item>
    ///         <item>If total max fits within <paramref name="maxWidth"/>, use max widths.</item>
    ///         <item>
    ///             Otherwise, iteratively collapse the widest column toward the second-widest.
    ///             Left columns win ties (preserving their width).
    ///         </item>
    ///         <item>Never shrink below min width (longest word) if possible.</item>
    ///         <item>Last resort: reduce all columns evenly if still over budget.</item>
    ///     </list>
    /// </summary>
    internal static int [] ComputeColumnWidths (TableData data, int maxWidth)
    {
        int cols = data.ColumnCount;
        var maxWidths = new int [cols];
        var minWidths = new int [cols];

        // Measure max (full content) and min (longest word) for each column
        for (var c = 0; c < cols; c++)
        {
            int headerMax = MeasureRenderedWidth (data.Headers [c]);
            int headerMin = MeasureLongestWord (data.Headers [c]);
            maxWidths [c] = headerMax;
            minWidths [c] = headerMin;
        }

        foreach (string [] row in data.Rows)
        {
            for (var c = 0; c < cols && c < row.Length; c++)
            {
                int cellMax = MeasureRenderedWidth (row [c]);
                int cellMin = MeasureLongestWord (row [c]);
                maxWidths [c] = Math.Max (maxWidths [c], cellMax);
                minWidths [c] = Math.Max (minWidths [c], cellMin);
            }
        }

        // Add padding (1 space each side)
        for (var c = 0; c < cols; c++)
        {
            maxWidths [c] = Math.Max (maxWidths [c] + 2, 3);
            minWidths [c] = Math.Max (minWidths [c] + 2, 3);
        }

        // If total max fits, use max widths
        var widths = (int [])maxWidths.Clone ();
        int borderChars = cols + 1; // left border + separators + right border

        if (widths.Sum () + borderChars <= maxWidth)
        {
            return widths;
        }

        // Iteratively collapse the widest column toward the second-widest (Rich-style).
        // Left columns win ties: when multiple columns share the max width,
        // we shrink the rightmost one first.
        int available = maxWidth - borderChars;

        if (available < cols * 3)
        {
            // Not enough room even for minimum columns — give each 3
            for (var c = 0; c < cols; c++)
            {
                widths [c] = 3;
            }

            return widths;
        }

        CollapseWidths (widths, minWidths, available);

        // Last resort: if still over budget, reduce all evenly
        int total = widths.Sum ();

        if (total <= available)
        {
            return widths;
        }

        int excess = total - available;
        ReduceEvenly (widths, minWidths, excess);

        return widths;
    }

    /// <summary>
    ///     Iteratively collapses the widest wrappable column toward the second-widest.
    ///     When multiple columns tie for widest, the rightmost is reduced first (left-wins).
    /// </summary>
    internal static void CollapseWidths (int [] widths, int [] minWidths, int available)
    {
        while (widths.Sum () > available)
        {
            // Find the widest column (rightmost if tied — left-wins means we shrink right first)
            var maxVal = 0;
            int maxIdx = -1;

            for (var c = 0; c < widths.Length; c++)
            {
                if (widths [c] < maxVal)
                {
                    continue;
                }
                maxVal = widths [c];
                maxIdx = c;
            }

            if (maxIdx < 0)
            {
                break;
            }

            // Find the second-widest value (excluding columns at maxVal)
            var secondMax = 0;

            foreach (int t in widths)
            {
                if (t < maxVal && t > secondMax)
                {
                    secondMax = t;
                }
            }

            // Can't reduce below min width
            int floor = Math.Max (minWidths [maxIdx], secondMax);

            if (floor >= maxVal)
            {
                // This column is already at its minimum or can't shrink further.
                // Try the next widest non-minimum column (scan right-to-left for left-wins).
                var shrank = false;

                for (int c = widths.Length - 1; c >= 0; c--)
                {
                    if (widths [c] <= minWidths [c])
                    {
                        continue;
                    }
                    widths [c]--;
                    shrank = true;

                    break;
                }

                if (!shrank)
                {
                    break;
                }

                continue;
            }

            // Reduce to the greater of secondMax and min width, but don't overshoot available
            int excess = widths.Sum () - available;
            int reduction = Math.Min (maxVal - floor, excess);
            widths [maxIdx] = maxVal - reduction;
        }
    }

    /// <summary>
    ///     Reduces all columns evenly as a last resort, respecting minimum widths first,
    ///     then going below minimum (floor at 3) if necessary.
    /// </summary>
    private static void ReduceEvenly (int [] widths, int [] minWidths, int excess)
    {
        // First pass: reduce above min widths (right-to-left for left-wins)
        while (excess > 0)
        {
            var reduced = false;

            for (int c = widths.Length - 1; c >= 0 && excess > 0; c--)
            {
                if (widths [c] <= minWidths [c])
                {
                    continue;
                }
                widths [c]--;
                excess--;
                reduced = true;
            }

            if (!reduced)
            {
                break;
            }
        }

        // Second pass: reduce below min (absolute last resort, floor at 3)
        while (excess > 0)
        {
            var reduced = false;

            for (int c = widths.Length - 1; c >= 0 && excess > 0; c--)
            {
                if (widths [c] <= 3)
                {
                    continue;
                }
                widths [c]--;
                excess--;
                reduced = true;
            }

            if (!reduced)
            {
                break;
            }
        }
    }

    /// <summary>Measures the display width of a cell's rendered text (stripping markdown formatting).</summary>
    internal static int MeasureRenderedWidth (string cellText)
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines (cellText, MarkdownStyleRole.Normal);
        var width = 0;

        foreach (InlineRun run in runs)
        {
            width += run.Text.GetColumns ();
        }

        return Math.Max (width, 1);
    }

    /// <summary>
    ///     Measures the display width of the longest single word in a cell's rendered text.
    ///     This determines the minimum column width (below which words would be hard-broken).
    /// </summary>
    internal static int MeasureLongestWord (string cellText)
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines (cellText, MarkdownStyleRole.Normal);
        var longest = 1;

        foreach (InlineRun run in runs)
        {
            string [] words = run.Text.Split (' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                int w = word.GetColumns ();
                longest = Math.Max (longest, w);
            }
        }

        return longest;
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

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Data = new TableData (["Feature", "Status"], [Alignment.Start, Alignment.Center], [["Markdown", "✅ Very"], ["Tables", "✅ Amaze"]]);

        Width = 40;

        return true;
    }
}
