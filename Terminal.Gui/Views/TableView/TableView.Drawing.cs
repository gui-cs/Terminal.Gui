#nullable disable
namespace Terminal.Gui.Views;

/// <summary>
///     Displays and enables infinite scrolling through tabular data based on a <see cref="ITableSource"/>.
///     <a href="../docs/tableview.md">See the TableView Deep Dive for more</a>.
/// </summary>
public partial class TableView
{
    ///<inheritdoc/>
    protected override bool OnDrawingContent (DrawContext context)
    {
        Move (0, 0);
        _scrollRightPoint = null;
        _scrollLeftPoint = null;

        // What columns to render at what X offset in viewport
        ColumnToRender [] columnsToRender = CalculateViewport (Viewport).ToArray ();
        SetAttribute (GetAttributeForRole (VisualRole.Normal));

        //invalidate current row (prevents scrolling around leaving old characters in the frame
        AddStr (new string (' ', Viewport.Width));
        var line = 0;

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
                RenderHeaderOverline (line, Viewport.Width, columnsToRender);
                line++;
            }

            if (Style.ShowHeaders)
            {
                RenderHeaderMidline (line, columnsToRender);
                line++;
            }

            if (Style.ShowHorizontalHeaderUnderline)
            {
                RenderHeaderUnderline (line, Viewport.Width, columnsToRender);
                line++;
            }
        }

        int headerLinesConsumed = line;

        //render the cells
        for (; line < Viewport.Height; line++)
        {
            ClearLine (line, Viewport.Width);

            //work out what Row to render
            int rowToRender = RowOffset + (line - headerLinesConsumed);

            //if we have run off the end of the table
            if (TableIsNullOrInvisible () || rowToRender < 0)
            {
                continue;
            }

            // No more data
            if (rowToRender >= Table.Rows)
            {
                if (rowToRender == Table.Rows && Style.ShowHorizontalBottomline)
                {
                    RenderBottomLine (line, Viewport.Width, columnsToRender);
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
    /// <param name="cellAttribute"></param>
    /// <param name="render"></param>
    /// <param name="isPrimaryCell"></param>
    protected virtual void RenderCell (Attribute cellAttribute, string render, bool isPrimaryCell)
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
            AddRune ((Rune)render [0]);

            if (render.Length <= 1)
            {
                return;
            }

            SetAttribute (cellAttribute);
            AddStr (render [1..]);
        }
        else
        {
            SetAttribute (cellAttribute);
            AddStr (render);
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

            AddRuneAt (c, row, rune);
        }
    }

    private void RenderHeaderMidline (int row, ColumnToRender [] columnsToRender)
    {
        // Renders something like:
        // │ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│
        ClearLine (row, Viewport.Width);

        //render start of line
        if (_style.ShowVerticalHeaderLines)
        {
            AddRune (0, row, Glyphs.VLine);
        }

        for (var i = 0; i < columnsToRender.Length; i++)
        {
            ColumnToRender current = columnsToRender [i];
            ColumnStyle colStyle = Style.GetColumnStyleIfAny (current.Column);
            string colName = _table.ColumnNames [current.Column];
            RenderSeparator (current.X - 1, row, true);
            Move (current.X, row);
            AddStr (TruncateOrPad (colName, colName, current.Width, colStyle));

            if (!Style.ExpandLastColumn && current.IsVeryLast)
            {
                RenderSeparator (current.X + current.Width - 1, row, true);
            }
        }

        //render end of line
        if (_style.ShowVerticalHeaderLines)
        {
            AddRune (Viewport.Width - 1, row, Glyphs.VLine);
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
                AddRuneAt (c, row, rune);
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

            AddRuneAt (c, row, rune);
        }
    }

    private void RenderRow (int row, int rowToRender, ColumnToRender [] columnsToRender)
    {
        bool focused = HasFocus;
        Scheme rowScheme = Style.RowColorGetter?.Invoke (new RowColorGetterArgs (Table, rowToRender)) ?? GetScheme ();

        //start by clearing the entire line
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

            // move to start of cell (in line with header positions)
            Move (current.X, row);

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
            RenderCell (cellColor, render, isPrimaryCell);

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

        //render start and end of line
        AddRune (0, row, Glyphs.VLine);
        AddRune (Viewport.Width - 1, row, Glyphs.VLine);
    }

    private void RenderSeparator (int col, int row, bool isHeader)
    {
        if (col < 0)
        {
            return;
        }

        bool renderLines = isHeader ? _style.ShowVerticalHeaderLines : _style.ShowVerticalCellLines;
        Rune symbol = renderLines ? Glyphs.VLine : (Rune)SeparatorSymbol;
        AddRune (col, row, symbol);
    }

    private bool ShouldRenderHeaders ()
    {
        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        return Style.AlwaysShowHeaders || _rowOffset == 0;
    }
}
