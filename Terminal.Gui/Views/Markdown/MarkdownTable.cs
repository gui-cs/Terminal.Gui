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
///         <see cref="View.LineCanvas"/>. Header cells are bold; body cells support inline
///         Markdown formatting (bold, italic, code, links) via <see cref="MarkdownInlineParser"/>.
///         Column alignment (left, center, right) parsed from the Markdown separator row is respected.
///     </para>
/// </remarks>
internal sealed class MarkdownTable : View
{
    private readonly TableData _data;
    private readonly int [] _columnWidths;

    // Pre-parsed inline segments for each cell
    private readonly List<StyledSegment> [] _headerSegments;
    private readonly List<StyledSegment> [] [] _rowSegments;

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

        // Parse inline markdown for all cells upfront
        _headerSegments = ParseCellSegments (data.Headers, MarkdownStyleRole.Heading);
        _rowSegments = new List<StyledSegment> [data.Rows.Length] [];

        for (var r = 0; r < data.Rows.Length; r++)
        {
            _rowSegments [r] = ParseCellSegments (data.Rows [r], MarkdownStyleRole.Normal);
        }

        _columnWidths = ComputeColumnWidths (data, maxWidth);

        int tableWidth = CalculateTableWidth (_columnWidths);
        int tableHeight = CalculateTableHeight (data);

        Width = tableWidth;
        Height = tableHeight;

        // No adornments — we draw everything ourselves via LineCanvas + content
        BorderStyle = LineStyle.None;
        Border.Thickness = new Thickness (0);
        Padding.Thickness = new Thickness (0);
        Margin.Thickness = new Thickness (0);
    }

    /// <summary>Gets the total rendered height of this table in lines.</summary>
    public static int CalculateTableHeight (TableData data) =>
        // top border + header + header separator + body rows + bottom border
        data.Rows.Length + 4;

    /// <inheritdoc />
    protected override bool OnDrawingContent (DrawContext? context)
    {
        DrawCellContents ();

        return true;
    }

    /// <inheritdoc />
    protected override bool OnRenderingLineCanvas ()
    {
        DrawBorders ();

        return false;
    }

    private void DrawCellContents ()
    {
        // Header row at Y=1 (below top border)
        DrawRow (_headerSegments, _data.ColumnAlignments, 1, isHeader: true);

        // Body rows starting at Y=3 (below header separator)
        for (var r = 0; r < _rowSegments.Length; r++)
        {
            DrawRow (_rowSegments [r], _data.ColumnAlignments, r + 3, isHeader: false);
        }
    }

    private void DrawRow (List<StyledSegment> [] cellSegments, Alignment [] alignments, int y, bool isHeader)
    {
        Attribute normal = GetAttributeForRole (VisualRole.Normal);
        Attribute headerBold = normal with { Style = normal.Style | TextStyle.Bold };

        // Column content starts after the left border character
        var x = 1;

        for (var col = 0; col < _columnWidths.Length; col++)
        {
            int colWidth = _columnWidths [col];
            List<StyledSegment> segments = col < cellSegments.Length ? cellSegments [col] : [];

            // Calculate total display width of all segments
            int textWidth = 0;

            foreach (StyledSegment seg in segments)
            {
                textWidth += seg.Text.GetColumns ();
            }

            // Calculate padding based on alignment
            Alignment alignment = col < alignments.Length ? alignments [col] : Alignment.Start;
            int padLeft = CalculateLeftPadding (colWidth, Math.Min (textWidth, colWidth - 2), alignment);

            // Fill the cell with spaces first (using normal attribute)
            SetAttribute (normal);

            for (var i = 0; i < colWidth; i++)
            {
                AddStr (x + i, y, " ");
            }

            // Draw styled segments at the padded position
            int drawX = x + padLeft;
            int available = colWidth - 2; // padding on each side
            var drawn = 0;

            foreach (StyledSegment seg in segments)
            {
                if (drawn >= available)
                {
                    break;
                }

                // Resolve attribute: for headers, force bold on top of any inline style
                Attribute attr = MarkdownAttributeHelper.GetAttributeForSegment (this, seg);

                if (isHeader)
                {
                    attr = attr with { Style = attr.Style | TextStyle.Bold };
                }

                SetAttribute (attr);

                foreach (string grapheme in GraphemeHelper.GetGraphemes (seg.Text))
                {
                    int gw = Math.Max (grapheme.GetColumns (), 1);

                    if (drawn + gw > available)
                    {
                        break;
                    }

                    AddStr (drawX, y, grapheme);
                    drawX += gw;
                    drawn += gw;
                }
            }

            // Advance past column width + separator character
            x += colWidth + 1;
        }
    }

    private void DrawBorders ()
    {
        int tableWidth = CalculateTableWidth (_columnWidths);
        int tableHeight = CalculateTableHeight (_data);

        Attribute borderAttr = GetAttributeForRole (VisualRole.Normal);

        // Top border (row 0)
        LineCanvas.AddLine (new Point (0, 0), tableWidth, Orientation.Horizontal, LineStyle.Single, borderAttr);

        // Header separator (row 2)
        LineCanvas.AddLine (new Point (0, 2), tableWidth, Orientation.Horizontal, LineStyle.Single, borderAttr);

        // Bottom border (last row)
        LineCanvas.AddLine (new Point (0, tableHeight - 1), tableWidth, Orientation.Horizontal, LineStyle.Single, borderAttr);

        // Left border (full height)
        LineCanvas.AddLine (new Point (0, 0), tableHeight, Orientation.Vertical, LineStyle.Single, borderAttr);

        // Right border (full height)
        LineCanvas.AddLine (new Point (tableWidth - 1, 0), tableHeight, Orientation.Vertical, LineStyle.Single, borderAttr);

        // Column separators (vertical lines between columns)
        var x = 0;

        for (var col = 0; col < _columnWidths.Length; col++)
        {
            x += _columnWidths [col] + 1; // column width + border char

            if (col < _columnWidths.Length - 1)
            {
                LineCanvas.AddLine (new Point (x, 0), tableHeight, Orientation.Vertical, LineStyle.Single, borderAttr);
            }
        }
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

    private static int [] ComputeColumnWidths (TableData data, int maxWidth)
    {
        int [] widths = new int [data.ColumnCount];

        // Start with header widths
        for (var c = 0; c < data.ColumnCount; c++)
        {
            widths [c] = Math.Max (data.Headers [c].GetColumns (), 1);
        }

        // Expand to fit body cell content
        foreach (string [] row in data.Rows)
        {
            for (var c = 0; c < data.ColumnCount && c < row.Length; c++)
            {
                widths [c] = Math.Max (widths [c], row [c].GetColumns ());
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
            // Minimum 3 chars per column (1 char + 2 padding)
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
    private static int CalculateTableWidth (int [] columnWidths)
    {
        // Left border + (column widths + separator between each) + right border
        // = 1 + sum(widths) + (columnCount - 1) + 1 = sum(widths) + columnCount + 1
        return columnWidths.Sum () + columnWidths.Length + 1;
    }

    private static int CalculateLeftPadding (int cellWidth, int textWidth, Alignment alignment)
    {
        // Cell already includes 2 padding chars (1 each side), so offset by 1
        int innerWidth = cellWidth - 2;
        int usableTextWidth = Math.Min (textWidth, innerWidth);

        return alignment switch
        {
            Alignment.Center => 1 + Math.Max ((innerWidth - usableTextWidth) / 2, 0),
            Alignment.End => 1 + Math.Max (innerWidth - usableTextWidth, 0),
            _ => 1 // Start — 1 char left padding
        };
    }
}
