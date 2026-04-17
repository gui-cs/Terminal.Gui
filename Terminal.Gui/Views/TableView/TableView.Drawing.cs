namespace Terminal.Gui.Views;

/// <summary>
///     Displays and enables infinite scrolling through tabular data based on a <see cref="ITableSource"/>.
///     <a href="../docs/tableview.md">See the TableView Deep Dive for more</a>.
/// </summary>
public partial class TableView
{
    /// <summary>
    ///     calculates the current header height based on what is visible
    ///     This respects the viewport Y position and the AlwaysShowHeaders style
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

        return Math.Min (Math.Max (GetHeaderHeight () - Viewport.Y, 0), Viewport.Height);
    }

    ///<inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        Move (0, 0);

        // What columns to render at what X offset in viewport
        ColumnToRender [] cellInfos = NonHiddenCellInfos ();
        SetAttribute (GetAttributeForRole (VisualRole.Normal));

        // invalidate current row (prevents scrolling around leaving old characters in the frame
        AddStr (new string (' ', Viewport.Width));
        var line = 0;
        var headerLinesHandled = 0;
        int availableWidth = GetContentWidth ();

        if (ShouldRenderHeaders ())
        {
            // Render something like:
            /*
                ┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐
                │ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│
                └────────────────────┴──────────┴───────────┴──────────────┴─────────┘
            */

            bool ShouldRenderNextHeaderLine () =>

                //is the header line not scrolled or shall it always be shown? and do we have space to render it?
                (Viewport.Y <= headerLinesHandled || Style.AlwaysShowHeaders) && line < Viewport.Height;

            if (Style.ShowHorizontalHeaderOverline)
            {
                if (ShouldRenderNextHeaderLine ())
                {
                    RenderHeaderOverline (line, availableWidth, cellInfos);
                    line++;
                }
                headerLinesHandled++;
            }

            if (Style.ShowHeaders)
            {
                if (ShouldRenderNextHeaderLine ())
                {
                    RenderHeaderMidline (line, availableWidth, cellInfos);
                    line++;
                }
                headerLinesHandled++;
            }

            if (Style.ShowHorizontalHeaderUnderline)
            {
                if (ShouldRenderNextHeaderLine ())
                {
                    RenderHeaderUnderline (line, availableWidth, cellInfos);
                    line++;
                }
                headerLinesHandled++;
            }
        }

        int headerLinesConsumed = line;

        int locRowOffset = Style.AlwaysShowHeaders ? Viewport.Y : Math.Max (Viewport.Y - headerLinesHandled, 0);

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
            if (rowToRender >= Table!.Rows)
            {
                if (rowToRender == Table.Rows && Style.ShowHorizontalBottomLine)
                {
                    RenderBottomLine (line, availableWidth, cellInfos);
                }

                continue;
            }

            RenderRow (line, rowToRender, cellInfos);
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
            AddRune (render [0]);

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

            RenderRune (c, row, rune);
        }
    }

    private void RenderRune (int col, int row, Rune rune)
    {
        if (col >= Viewport.X && col < Viewport.X + Viewport.Width)
        {
            AddRuneAt (col - Viewport.X, row, rune);
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

        foreach (ColumnToRender current in columnsToRender)
        {
            ColumnStyle? colStyle = Style.GetColumnStyleIfAny (current.Column);
            string colName = _table!.ColumnNames [current.Column];
            RenderSeparator (current.X - 1, row, true);
            Move (current.X - Viewport.X, row);
            AddStr (TruncateOrPad (colName, colName, current.Width, colStyle));

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
        Scheme rowScheme = Style.RowColorGetter?.Invoke (new RowColorGetterArgs (Table!, rowToRender)) ?? GetScheme ();

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
        foreach (ColumnToRender current in columnsToRender)
        {
            ColumnStyle? colStyle = Style.GetColumnStyleIfAny (current.Column);

            // Set scheme based on whether the current cell is the selected one
            bool isSelectedCell = IsSelected (current.Column, rowToRender);
            object val = Table! [rowToRender, current.Column];

            // Render the (possibly truncated) cell value
            string representation = GetRepresentation (val, colStyle);

            // to get the colour scheme
            CellColorGetterDelegate? schemeGetter = colStyle?.ColorGetter;
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
            Move (current.X - Viewport.X, row);
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

            if (_style is { AlwaysUseNormalColorForVerticalCellLines: true, ShowVerticalCellLines: true })
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

        ColumnToRender? lastCol = columnsToRender.LastOrDefault ();

        if (lastCol != null)
        {
            RenderRune (lastCol.X + lastCol.Width - 1, row, Glyphs.VLine);
        }
    }

    private void RenderSeparator (int col, int row, bool isHeader)
    {
        if (col < 0)
        {
            return;
        }

        bool renderLines = isHeader ? _style.ShowVerticalHeaderLines : _style.ShowVerticalCellLines;
        Rune symbol = renderLines ? Glyphs.VLine : (Rune)SeparatorSymbol;
        RenderRune (col, row, symbol);
    }

    /// <summary>
    ///     This decides if we should render headers at all (no matter what the style settings are)
    ///     This may be a candidate to remove in future
    ///     (old implementation needed this logic to decide if the header is in current view (RowOffset))
    /// </summary>
    /// <returns></returns>

    // TODO: a candidate to remove
    private bool ShouldRenderHeaders ()
    {
        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        return true;
    }
}
