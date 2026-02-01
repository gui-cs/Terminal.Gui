#nullable disable
namespace Terminal.Gui.Views;

/// <summary>
///     Displays and enables infinite scrolling through tabular data based on a <see cref="ITableSource"/>.
///     <a href="../docs/tableview.md">See the TableView Deep Dive for more</a>.
/// </summary>
public partial class TableView
{
    /// <summary>
    /// Gets or sets a value indicating whether to use scrollbars.
    /// This will change the behavior of the TableView to use the Viewport and ContentSize
    /// The tableview takes care of showing and hiding the scrollbars as needed.
    /// It takes as much space as needed to render the content, in case you can use the scrollbars.
    /// </summary>
    public bool UseScrollbars
    {
        get => field;
        set
        {
            field = value;

            //refresh content size
            SetContentSize (value ? CalculateContentSize () : null);
        }
    }

    /// <summary>
    /// calculates the current header height based on what is visible
    /// This respects the viewport Y position and the AlwaysShowHeaders style
    /// </summary>
    /// <returns>height</returns>
    protected int CurrentHeaderHeightVisible ()
    {
        if (!ShouldRenderHeaders ())
        {
            return 0;
        }

        if (Style.AlwaysShowHeaders)
        {
            return Math.Min (GetHeaderHeight (), Viewport.Height);
        }
        else
        {
            return Math.Min (Math.Max (GetHeaderHeight () - Viewport.Y, 0), Viewport.Height);
        }
    }

    ///<inheritdoc/>
    protected override bool OnDrawingContent (DrawContext context)
    {
        Move (0, 0);
        _scrollRightPoint = null;
        _scrollLeftPoint = null;

        // What columns to render at what X offset in viewport
        ColumnToRender [] columnsToRender = CalculateViewport (Viewport).ToArray ();
        SetAttribute (GetAttributeForRole (VisualRole.Normal));

        // invalidate current row (prevents scrolling around leaving old characters in the frame
        AddStr (new string (' ', Viewport.Width));
        var line = 0;
        int headerLinesHandled = 0;
        var availableWidth = UseScrollbars ? GetContentSize ().Width : Viewport.Width;

        if (ShouldRenderHeaders ())
        {
            // Render something like:
            /*
                ┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐
                │ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│
                └────────────────────┴──────────┴───────────┴──────────────┴─────────┘
            */

            if (Style.ShowHorizontalHeaderOverline)
            {
                if ((Viewport.Y <= line || Style.AlwaysShowHeaders) && line < Viewport.Height)
                {
                    RenderHeaderOverline (line, availableWidth, columnsToRender);
                    line++;
                }
                headerLinesHandled++;
            }

            if (Style.ShowHeaders)
            {
                if ((Viewport.Y <= headerLinesHandled || Style.AlwaysShowHeaders) && line < Viewport.Height)
                {
                    RenderHeaderMidline (line, availableWidth, columnsToRender);
                    line++;
                }
                headerLinesHandled++;
            }

            if (Style.ShowHorizontalHeaderUnderline)
            {
                if ((Viewport.Y <= headerLinesHandled || Style.AlwaysShowHeaders) && line < Viewport.Height)
                {
                    RenderHeaderUnderline (line, availableWidth, columnsToRender);
                    line++;
                }
                headerLinesHandled++;
            }
        }

        int headerLinesConsumed = line;

        var locRowOffset = UseScrollbars ? Style.AlwaysShowHeaders ? Viewport.Y : Math.Max (Viewport.Y - headerLinesHandled, 0) : RowOffset;

        // render the cells
        for (; line < Viewport.Height; line++)
        {
            ClearLine (line, Viewport.Width);

            // work out what Row to render
            int rowToRender = locRowOffset + (line - headerLinesConsumed);

            // if we have run off the end of the table
            if (TableIsNullOrInvisible () || rowToRender < 0)
            {
                continue;
            }

            // No more data
            if (rowToRender >= Table.Rows)
            {
                if (rowToRender == Table.Rows && Style.ShowHorizontalBottomline)
                {
                    RenderBottomLine (line, availableWidth, columnsToRender);
                }

                continue;
            }

            RenderRow (line, rowToRender, columnsToRender);
        }

        return true;
    }

    /// <summary>
    ///     Override to provide custom multi-coloring to cells. Use methods like <see cref="View.AddStr(string)"/>.
    ///     The cursor will already be in the correct position when rendering. You must render the full
    ///     <paramref name="render"/> or the view will not look right. For simpler color provision use
    ///     <see cref="ColumnStyle.ColorGetter"/>. For changing the content that is rendered use
    ///     <see cref="ColumnStyle.RepresentationGetter"/>.
    /// </summary>
    /// <param name="ypos">the x value where to render the cell (absolute value in context of GetContentSize)</param>
    /// <param name="xpos">the y value where to render the cell (absolute value in context of GetContentSize)</param>
    /// <param name="cellAttribute"></param>
    /// <param name="render"></param>
    /// <param name="isPrimaryCell"></param>
    protected virtual void RenderCell (int xpos, int ypos, Attribute cellAttribute, string render, bool isPrimaryCell)
    {
        // If the cell is the selected col/row then draw the first rune in inverted colors
        // this allows the user to track which cell is the active one during a multi cell
        // selection or in full row select mode
        if (Style.InvertSelectedCellFirstCharacter && isPrimaryCell)
        {
            if (render.Length == 0)
            {
                return;
            }

            // invert the color of the current cell for the first character
            SetAttribute (new Attribute (cellAttribute.Foreground, cellAttribute.Background, TextStyle.Reverse));
            RenderRune (xpos, ypos, (Rune) render [0]);

            if (render.Length <= 1)
            {
                return;
            }

            SetAttribute (cellAttribute);
            RenderStr (xpos, ypos, render [1..]);
        }
        else
        {
            SetAttribute (cellAttribute);
            RenderStr (xpos, ypos, render);
        }
    }

    private void RenderBottomLine (int row, int availableWidth, ColumnToRender [] columnsToRender)
    {
        // Renders a line at the bottom of the table after all the data like:
        // └─────────────────────────────────┴──────────┴──────┴──────────┴────────┴────────────────────────────────────────────┘
        for (var c = 0; c < availableWidth; c++)
        {
            // Start by assuming we just draw a straight line the
            // whole way but update to instead draw BottomTee / Corner etc
            Rune rune = Glyphs.HLine;

            if (Style.ShowVerticalCellLines)
            {
                if (c == 0)
                {
                    // for first character render line
                    rune = Glyphs.LLCorner;
                }
                else if (columnsToRender.Any (r => r.X == c + 1))
                {
                    // if the next column is the start of a header
                    rune = Glyphs.BottomTee;
                }
                else if (c == availableWidth - 1)
                {
                    // for the last character in the table
                    rune = Glyphs.LRCorner;
                }
                else if (!Style.ExpandLastColumn && columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c))
                {
                    // if the next console column is the last column's end
                    rune = Glyphs.BottomTee;
                }
            }

            RenderRune (c, row, rune);
        }
    }

    private void RenderRune (int col, int row, Rune rune)
    {
        if (UseScrollbars)
        {
            if (col >= Viewport.X && col < Viewport.X + Viewport.Width)
            {
                AddRuneAt (col - Viewport.X, row, rune);
            }
        }
        else
        {
            AddRuneAt (col, row, rune);
        }
    }

    /// <summary>
    /// renders the text but does clipping if needed
    /// </summary>
    /// <param name="col">col is absolute, see GetContentSize</param>
    /// <param name="row"></param>
    /// <param name="text"></param>
    private void RenderStr (int col, int row, string text)
    {
        if (UseScrollbars)
        {
            // check if within visible viewport
            if (col + text.Length >= Viewport.X && col < Viewport.Right)
            {
                var x = col - Viewport.X;

                if (x < 0)
                {
                    text = text [-x..];
                    x = 0;
                }
                var clipEnd = (col + text.Length) - Viewport.Right;

                if (clipEnd > 0)
                {
                    text = text [..^clipEnd];
                }

                AddStr (x, row, text);
            }
        }
        else
        {
            AddStr (col, row, text);
        }
    }

    private void RenderHeaderMidline (int row, int availableWidth, ColumnToRender [] columnsToRender)
    {
        // Renders something like:
        // │ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│
        ClearLine (row, Viewport.Width);

        // render start of line
        if (_style.ShowVerticalHeaderLines)
        {
            RenderRune (0, row, Glyphs.VLine);
        }

        for (var i = 0; i < columnsToRender.Length; i++)
        {
            ColumnToRender current = columnsToRender [i];
            ColumnStyle colStyle = Style.GetColumnStyleIfAny (current.Column);
            string colName = _table.ColumnNames [current.Column];
            RenderSeparator (current.X - 1, row, true);

            RenderStr (current.X, row, TruncateOrPad (colName, colName, current.Width, colStyle));

            if (!Style.ExpandLastColumn && current.IsVeryLast)
            {
                RenderSeparator (current.X + current.Width - 1, row, true);
            }
        }

        // render end of line
        if (_style.ShowVerticalHeaderLines)
        {
            RenderRune (availableWidth - 1, row, Glyphs.VLine);
        }
    }

    private void RenderHeaderOverline (int row, int availableWidth, ColumnToRender [] columnsToRender)
    {
        // Renders a line above table headers (when visible) like:
        // ┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐
        for (var c = 0; c < availableWidth; c++)
        {
            Rune rune = Glyphs.HLine;

            if (Style.ShowVerticalHeaderLines)
            {
                if (c == 0)
                {
                    rune = Glyphs.ULCorner;
                }

                // if the next column is the start of a header
                else if (columnsToRender.Any (r => r.X == c + 1))
                {
                    rune = Glyphs.TopTee;
                }
                else if (c == availableWidth - 1)
                {
                    rune = Glyphs.URCorner;
                }

                // if the next console column is the last column's end
                else if (!Style.ExpandLastColumn && columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c))
                {
                    rune = Glyphs.TopTee;
                }
            }

            if (App?.Screen.Height > 0)
            {
                RenderRune (c, row, rune);
            }
        }
    }

    private void RenderHeaderUnderline (int row, int availableWidth, ColumnToRender [] columnsToRender)
    {
        /*
         *  First lets work out if we should be rendering scroll indicators
         */
        // are there are visible columns to the left that have been pushed
        // off the screen due to horizontal scrolling?
        bool moreColumnsToLeft = ColumnOffset > 0;

        // if we moved left would we find a new column (or are they all invisible?)
        if (!TryGetNearestVisibleColumn (ColumnOffset - 1, false, false, out _))
        {
            moreColumnsToLeft = false;
        }

        // are there visible columns to the right that have not yet been reached?
        // lets find out, what is the column index of the last column we are rendering
        int lastColumnIdxRendered = ColumnOffset + columnsToRender.Length - 1;

        // are there more valid indexes?
        bool moreColumnsToRight = lastColumnIdxRendered < Table.Columns;

        // if we went right from the last column would we find a new visible column?
        if (!TryGetNearestVisibleColumn (lastColumnIdxRendered + 1, true, false, out _))
        {
            // no we would not
            moreColumnsToRight = false;
        }

        /*
         *  Now lets draw the line itself
         */
        // Renders a line below the table headers (when visible) like:
        // ├──────────┼───────────┼───────────────────┼──────────┼────────┼─────────────┤
        for (var c = 0; c < availableWidth; c++)
        {
            // Start by assuming we just draw a straight line the
            // whole way but update to instead draw a header indicator
            // or scroll arrow etc
            Rune rune = Glyphs.HLine;

            if (Style.ShowVerticalHeaderLines)
            {
                if (c == 0)
                {
                    // for first character render line
                    rune = Style.ShowVerticalCellLines ? Glyphs.LeftTee : Glyphs.LLCorner;

                    // unless we have horizontally scrolled along
                    // in which case render an arrow, to indicate user
                    // can scroll left
                    if (Style.ShowHorizontalScrollIndicators && moreColumnsToLeft)
                    {
                        rune = Glyphs.LeftArrow;
                        _scrollLeftPoint = new Point (c, row);
                    }
                }

                // if the next column is the start of a header
                else if (columnsToRender.Any (r => r.X == c + 1))
                {
                    /*TODO: is ┼ symbol in Driver?*/
                    rune = Style.ShowVerticalCellLines ? Glyphs.Cross : Glyphs.BottomTee;
                }
                else if (c == availableWidth - 1)
                {
                    // for the last character in the table
                    rune = Style.ShowVerticalCellLines ? Glyphs.RightTee : Glyphs.LRCorner;

                    // unless there is more of the table we could horizontally
                    // scroll along to see. In which case render an arrow,
                    // to indicate user can scroll right
                    if (Style.ShowHorizontalScrollIndicators && moreColumnsToRight)
                    {
                        rune = Glyphs.RightArrow;
                        _scrollRightPoint = new Point (c, row);
                    }
                }

                // if the next console column is the last column's end
                else if (!Style.ExpandLastColumn && columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c))
                {
                    rune = Style.ShowVerticalCellLines ? Glyphs.Cross : Glyphs.BottomTee;
                }
            }

            RenderRune (c, row, rune);
        }
    }

    private void RenderRow (int row, int rowToRender, ColumnToRender [] columnsToRender)
    {
        bool focused = HasFocus;
        Scheme rowScheme = Style.RowColorGetter?.Invoke (new RowColorGetterArgs (Table, rowToRender)) ?? GetScheme ();

        // start by clearing the entire line
        // not needed, see Attribute below:
        ClearLine (row, Viewport.Width);
        Move (0, row);
        Attribute? attribute;

        if (FullRowSelect && IsSelected (0, rowToRender))
        {
            attribute = focused ? rowScheme.Focus : rowScheme.Active;
        }
        else
        {
            attribute = Enabled ? rowScheme.Normal : rowScheme.Active;
        }

        SetAttribute (attribute.Value);
        AddStr (new string (' ', Viewport.Width));

        // Render cells for each visible header for the current row
        for (var i = 0; i < columnsToRender.Length; i++)
        {
            ColumnToRender current = columnsToRender [i];
            ColumnStyle colStyle = Style.GetColumnStyleIfAny (current.Column);

            // Set scheme based on whether the current cell is the selected one
            bool isSelectedCell = IsSelected (current.Column, rowToRender);
            object val = Table [rowToRender, current.Column];

            // Render the (possibly truncated) cell value
            string representation = GetRepresentation (val, colStyle);

            // to get the colour scheme
            CellColorGetterDelegate schemeGetter = colStyle?.ColorGetter;
            Scheme scheme;

            if (schemeGetter is { })
            {
                // user has a delegate for defining row color per cell, call it
                // if users custom color getter returned null, use the row scheme
                scheme = schemeGetter (new CellColorGetterArgs (Table, rowToRender, current.Column, val, representation, rowScheme)) ?? rowScheme;
            }
            else
            {
                // There is no custom cell coloring delegate so use the scheme for the row
                scheme = rowScheme;
            }

            Attribute cellColor = isSelectedCell ? focused ? scheme.Focus : scheme.Active : Enabled ? scheme.Normal : scheme.Disabled;
            string render = TruncateOrPad (val, representation, current.Width, colStyle);

            // While many cells can be selected (see MultiSelectedRegions) only one cell is the primary (drives navigation etc)
            bool isPrimaryCell = current.Column == _selectedColumn && rowToRender == _selectedRow;
            RenderCell (current.X, row, cellColor, render, isPrimaryCell);

            // Reset scheme to normal for drawing separators if we drew text with custom scheme
            if (scheme != rowScheme)
            {
                if (isSelectedCell)
                {
                    attribute = focused ? rowScheme.Focus : rowScheme.Active;
                }
                else
                {
                    attribute = Enabled ? rowScheme.Normal : rowScheme.Disabled;
                }

                SetAttribute (attribute.Value);
            }

            // If not in full row select mode always, reset scheme to normal and render the vertical line (or space) at the end of the cell
            if (!FullRowSelect)
            {
                SetAttribute (Enabled ? rowScheme.Normal : rowScheme.Disabled);
            }

            if (_style.AlwaysUseNormalColorForVerticalCellLines && _style.ShowVerticalCellLines)
            {
                SetAttribute (rowScheme.Normal);
            }

            RenderSeparator (current.X - 1, row, false);

            if (!Style.ExpandLastColumn && current.IsVeryLast)
            {
                RenderSeparator (current.X + current.Width - 1, row, false);
            }
        }

        if (!_style.ShowVerticalCellLines)
        {
            return;
        }

        SetAttribute (rowScheme.Normal);

        // render start and end of line
        RenderRune (0, row, Glyphs.VLine);

        if (UseScrollbars)
        {
            var lastCol = columnsToRender.LastOrDefault ();

            if (lastCol != null)
            {
                RenderRune (lastCol.X + lastCol.Width - 1, row, Glyphs.VLine);
            }
        }
        else
        {
            AddRune (Viewport.Width - 1, row, Glyphs.VLine);
        }
    }

    private void RenderSeparator (int col, int row, bool isHeader)
    {
        if (col < 0)
        {
            return;
        }

        bool renderLines = isHeader ? _style.ShowVerticalHeaderLines : _style.ShowVerticalCellLines;
        Rune symbol = renderLines ? Glyphs.VLine : (Rune) SeparatorSymbol;
        RenderRune (col, row, symbol);
    }

    private bool ShouldRenderHeaders ()
    {
        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        return Style.AlwaysShowHeaders || _rowOffset == 0 || UseScrollbars;
    }
}
