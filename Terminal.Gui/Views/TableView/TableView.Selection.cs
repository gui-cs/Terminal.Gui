using System.Data;

namespace Terminal.Gui.Views;

/// <summary>
///     Displays and enables infinite scrolling through tabular data based on a <see cref="ITableSource"/>.
///     <a href="../docs/tableview.md">See the TableView Deep Dive for more</a>.
/// </summary>
public partial class TableView
{
    /// <summary>True to select the entire row at once.  False to select individual cells.  Defaults to false</summary>
    public bool FullRowSelect { get; set; }

    /// <summary>True to allow regions to be selected</summary>
    /// <value></value>
    public bool MultiSelect { get; set; } = true;

    /// <summary>
    ///     When <see cref="MultiSelect"/> is enabled this property contain all rectangles of selected cells.  Rectangles
    ///     describe column/rows selected in <see cref="Table"/> (not screen coordinates)
    /// </summary>
    /// <returns></returns>
    public Stack<TableSelection> MultiSelectedRegions { get; } = new ();

    private int _selectedColumn;

    /// <summary>The index of <see cref="DataTable.Columns"/> in <see cref="Table"/> that the user has currently selected</summary>
    public int SelectedColumn
    {
        get => _selectedColumn;
        set
        {
            int oldValue = _selectedColumn;

            // try to prevent this being set to an out-of-bounds column
            _selectedColumn = TableIsNullOrInvisible () ? 0 : Math.Min (Table!.Columns - 1, Math.Max (0, value));

            if (oldValue != _selectedColumn)
            {
                RaiseSelectedCellChanged (new SelectedCellChangedEventArgs (Table!, oldValue, SelectedColumn, SelectedRow, SelectedRow));
            }
        }
    }

    private int _selectedRow;

    /// <summary>The index of <see cref="DataTable.Rows"/> in <see cref="Table"/> that the user has currently selected</summary>
    public int SelectedRow
    {
        get => _selectedRow;
        set
        {
            int oldValue = _selectedRow;
            _selectedRow = TableIsNullOrInvisible () ? 0 : Math.Min (Table!.Rows - 1, Math.Max (0, value));

            if (oldValue != _selectedRow)
            {
                RaiseSelectedCellChanged (new SelectedCellChangedEventArgs (Table!, SelectedColumn, SelectedColumn, oldValue, _selectedRow));
            }
        }
    }

    /// <summary>
    ///     Private override of <see cref="ChangeSelectionByOffset"/> that returns true if the selection has
    ///     changed as a result of moving the selection. Used by key handling logic to determine whether e.g.
    ///     the cursor right resulted in a change or should be forwarded on to toggle logic handling.
    /// </summary>
    /// <param name="offsetX"></param>
    /// <param name="offsetY"></param>
    /// <returns></returns>
    private bool ChangeSelectionByOffsetWithReturn (int offsetX, int offsetY)
    {
        TableViewSelectionSnapshot oldSelection = GetSelectionSnapshot ();
        SetSelection (SelectedColumn + offsetX, SelectedRow + offsetY, false);
        Update ();

        return !SelectionIsSame (oldSelection);
    }

    private TableViewSelectionSnapshot GetSelectionSnapshot () => new (SelectedColumn, SelectedRow, MultiSelectedRegions.Select (s => s.Rectangle).ToArray ());

    private bool SelectionIsSame (TableViewSelectionSnapshot oldSelection)
    {
        TableViewSelectionSnapshot newSelection = GetSelectionSnapshot ();

        return oldSelection.SelectedColumn == newSelection.SelectedColumn
               && oldSelection.SelectedRow == newSelection.SelectedRow
               && oldSelection.MultiSelection.SequenceEqual (newSelection.MultiSelection);
    }

    /// <summary>
    ///     Moves the <see cref="SelectedRow"/> and <see cref="SelectedColumn"/> by the provided offsets. Optionally
    ///     starting a box selection (see <see cref="MultiSelect"/>)
    /// </summary>
    /// <param name="offsetX">Offset in number of columns</param>
    /// <param name="offsetY">Offset in number of rows</param>
    /// <param name="extendExistingSelection">True to create a multi cell selection or adjust an existing one</param>
    public void ChangeSelectionByOffset (int offsetX, int offsetY, bool extendExistingSelection)
    {
        SetSelection (SelectedColumn + offsetX, SelectedRow + offsetY, extendExistingSelection);
        Update ();
    }

    /// <summary>Moves or extends the selection to the last cell in the current row</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    public void ChangeSelectionToEndOfRow (bool extend)
    {
        SetSelection (Table!.Columns - 1, SelectedRow, extend);
        Update ();
    }

    /// <summary>Moves or extends the selection to the first cell in the current row</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    public void ChangeSelectionToStartOfRow (bool extend)
    {
        SetSelection (0, SelectedRow, extend);
        Update ();
    }

    /// <summary>
    ///     Updates scroll offsets to ensure that the selected cell is visible.  Has no effect if <see cref="Table"/> has
    ///     not been set.
    /// </summary>
    /// <remarks>
    ///     Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>
    /// </remarks>
    public void EnsureSelectedCellIsVisible ()
    {
        if (Table is null || Table.Columns <= 0)
        {
            return;
        }

        ColumnToRender [] cellInfos = NonHiddenCellInfos ();
        int headerHeight = GetHeaderHeightIfAny ();

        ColumnToRender? selectedColToRender = cellInfos.FirstOrDefault (c => c.Column == SelectedColumn);

        if (SelectedColumn < 0 || selectedColToRender == null || SelectedRow < 0 || SelectedRow >= Table.Rows)
        {
            return;
        }

        int rowStart;
        int rowEnd;

        if (Style.AlwaysShowHeaders)
        {
            rowStart = Viewport.Y;
            rowEnd = Viewport.Y + Viewport.Height - headerHeight - 1;
        }
        else
        {
            rowStart = Math.Max (Viewport.Y - headerHeight, 0);
            rowEnd = Viewport.Y + Viewport.Height - headerHeight - 1;
        }

        if (rowEnd < rowStart)
        {
            return;
        }

        if (SelectedRow < rowStart)
        {
            Viewport = Viewport with { Y = Viewport.Y - (rowStart - SelectedRow) };
        }

        if (SelectedRow > rowEnd)
        {
            Viewport = Viewport with { Y = Viewport.Y + (SelectedRow - rowEnd) };
        }

        //first column that is visible from start
        ColumnToRender? colStart = cellInfos.FirstOrDefault (c => c.X - 1 > Viewport.Left);

        //last column that is visible (at least the start)
        ColumnToRender? colEnd = cellInfos.LastOrDefault (c => c.X < Viewport.Right);

        if (colEnd is { } && SelectedColumn >= colEnd.Column)
        {
            if (Style.SmoothHorizontalScrolling)
            {
                //bring selected col into view
                Viewport = Viewport with { X = Math.Min (selectedColToRender.X, selectedColToRender.X + selectedColToRender.Width - Viewport.Width) };
            }
            else
            {
                //bring selected col to start of viewport
                Viewport = Viewport with { X = selectedColToRender.X };
            }
        }

        if (colStart is { } && SelectedColumn >= colStart.Column)
        {
            return;
        }

        if (Style.SmoothHorizontalScrolling)
        {
            //bring selected col into view
            Viewport = Viewport with { X = selectedColToRender.X - 1 };
        }
        else
        {
            //bring selected col to end of viewport
            Viewport = Viewport with { X = selectedColToRender.X - Math.Max (Viewport.Width - selectedColToRender.Width, 0) };
        }
    }

    /// <summary>
    ///     Updates <see cref="SelectedColumn"/>, <see cref="SelectedRow"/> and <see cref="MultiSelectedRegions"/> where
    ///     they are outside the bounds of the table (by adjusting them to the nearest existing cell).  Has no effect if
    ///     <see cref="Table"/> has not been set.
    /// </summary>
    /// <remarks>
    ///     Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>
    /// </remarks>
    public void EnsureValidSelection ()
    {
        if (TableIsNullOrInvisible ())
        {
            // Table doesn't exist, we should probably clear those selections
            ClearMultiSelectedRegions (false);

            return;
        }

        SelectedColumn = Math.Max (Math.Min (SelectedColumn, Table!.Columns - 1), 0);
        SelectedRow = Math.Max (Math.Min (SelectedRow, Table.Rows - 1), 0);

        // If SelectedColumn is invisible move it to a visible one
        SelectedColumn = GetNearestVisibleColumn (SelectedColumn, true, true);
        IEnumerable<TableSelection> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
        MultiSelectedRegions.Clear ();

        // evaluate
        foreach (TableSelection region in oldRegions)
        {
            // ignore regions entirely below current table state
            if (region.Rectangle.Top >= Table.Rows)
            {
                continue;
            }

            // ignore regions entirely too far right of table columns
            if (region.Rectangle.Left >= Table.Columns)
            {
                continue;
            }

            // ensure region's origin exists
            region.Origin = new Point (Math.Max (Math.Min (region.Origin.X, Table.Columns - 1), 0), Math.Max (Math.Min (region.Origin.Y, Table.Rows - 1), 0));

            // ensure regions do not go over edge of table bounds
            region.Rectangle = Rectangle.FromLTRB (region.Rectangle.Left,
                                                   region.Rectangle.Top,
                                                   Math.Max (Math.Min (region.Rectangle.Right, Table.Columns), 0),
                                                   Math.Max (Math.Min (region.Rectangle.Bottom, Table.Rows), 0));
            MultiSelectedRegions.Push (region);
        }
    }

    /// <summary>
    ///     Returns all cells in any <see cref="MultiSelectedRegions"/> (if <see cref="MultiSelect"/> is enabled) and the
    ///     selected cell
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Point> GetAllSelectedCells ()
    {
        if (TableIsNullOrInvisible () || Table!.Rows == 0)
        {
            return Enumerable.Empty<Point> ();
        }

        EnsureValidSelection ();
        HashSet<Point> toReturn = [];

        // If there are one or more rectangular selections
        if (MultiSelect && MultiSelectedRegions.Any ())
        {
            // Quiz any cells for whether they are selected.  For performance, we only need to check those between the top left and lower right vertex of
            // selection regions
            int yMin = MultiSelectedRegions.Min (r => r.Rectangle.Top);
            int yMax = MultiSelectedRegions.Max (r => r.Rectangle.Bottom);
            int xMin = FullRowSelect ? 0 : MultiSelectedRegions.Min (r => r.Rectangle.Left);
            int xMax = FullRowSelect ? Table.Columns : MultiSelectedRegions.Max (r => r.Rectangle.Right);

            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    if (IsSelected (x, y))
                    {
                        toReturn.Add (new Point (x, y));
                    }
                }
            }
        }

        // if there are no region selections then it is just the active cell
        // if we are selecting the full row
        if (FullRowSelect)
        {
            // all cells in active row are selected
            for (var x = 0; x < Table.Columns; x++)
            {
                toReturn.Add (new Point (x, SelectedRow));
            }
        }
        else
        {
            // Not full row select and no multi selections
            toReturn.Add (new Point (SelectedColumn, SelectedRow));
        }

        return toReturn;
    }

    /// <summary>
    ///     <para>
    ///         Returns true if the given cell is selected either because it is the active cell or part of a multi cell
    ///         selection (e.g. <see cref="FullRowSelect"/>).
    ///     </para>
    ///     <remarks>Returns <see langword="false"/> if <see cref="ColumnStyle.Visible"/> is <see langword="false"/>.</remarks>
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool IsSelected (int col, int row)
    {
        if (!IsColumnVisible (col))
        {
            return false;
        }

        if (GetMultiSelectedRegionsContaining (col, row).Any ())
        {
            return true;
        }

        return row == SelectedRow && (col == SelectedColumn || FullRowSelect);
    }

    /// <summary>
    ///     When <see cref="MultiSelect"/> is on, creates selection over all cells in the table (replacing any old
    ///     selection regions)
    /// </summary>
    public void SelectAll ()
    {
        if (TableIsNullOrInvisible () || !MultiSelect || Table!.Rows == 0)
        {
            return;
        }

        ClearMultiSelectedRegions (true);

        // Create a single region over entire table, set the origin of the selection to the active cell so that a followup spread selection e.g. shift-right
        // behaves properly
        MultiSelectedRegions.Push (new TableSelection (new Point (SelectedColumn, SelectedRow), new Rectangle (0, 0, Table.Columns, _table!.Rows)));
        Update ();
    }

    /// <summary>
    ///     Moves the <see cref="SelectedRow"/> and <see cref="SelectedColumn"/> to the given col/row in
    ///     <see cref="Table"/>. Optionally starting a box selection (see <see cref="MultiSelect"/>)
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <param name="extendExistingSelection">True to create a multi cell selection or adjust an existing one</param>
    public void SetSelection (int col, int row, bool extendExistingSelection)
    {
        // if we are trying to increase the column index then
        // we are moving right otherwise we are moving left
        bool lookRight = col > _selectedColumn;
        col = GetNearestVisibleColumn (col, lookRight, true);

        if (!MultiSelect || !extendExistingSelection)
        {
            ClearMultiSelectedRegions (true);
        }

        if (extendExistingSelection)
        {
            // If we are extending current selection but there isn't one
            if (MultiSelectedRegions.Count == 0 || MultiSelectedRegions.All (m => m.IsToggled))
            {
                // Create a new region between the old active cell and the new cell
                TableSelection rect = CreateTableSelection (SelectedColumn, SelectedRow, col, row);
                MultiSelectedRegions.Push (rect);
            }
            else
            {
                // Extend the current head selection to include the new cell
                TableSelection head = MultiSelectedRegions.Pop ();
                TableSelection newRect = CreateTableSelection (head.Origin.X, head.Origin.Y, col, row);
                MultiSelectedRegions.Push (newRect);
            }
        }

        SelectedColumn = col;
        SelectedRow = row;
    }

    // TODO: Refactor to use CWP
    /// <summary>Invokes the <see cref="SelectedCellChanged"/> event</summary>
    private void RaiseSelectedCellChanged (SelectedCellChangedEventArgs args) => SelectedCellChanged?.Invoke (this, args);

    private void ClearMultiSelectedRegions (bool keepToggledSelections)
    {
        if (!keepToggledSelections)
        {
            MultiSelectedRegions.Clear ();

            return;
        }

        IEnumerable<TableSelection> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
        MultiSelectedRegions.Clear ();

        foreach (TableSelection region in oldRegions)
        {
            if (region.IsToggled)
            {
                MultiSelectedRegions.Push (region);
            }
        }
    }

    private IEnumerable<TableSelection> GetMultiSelectedRegionsContaining (int col, int row)
    {
        if (!MultiSelect)
        {
            return Enumerable.Empty<TableSelection> ();
        }

        if (FullRowSelect)
        {
            return MultiSelectedRegions.Where (r => r.Rectangle.Bottom > row && r.Rectangle.Top <= row);
        }

        return MultiSelectedRegions.Where (r => r.Rectangle.Contains (col, row));
    }

    private bool? ToggleCurrentCellSelection ()
    {
        var e = new CellToggledEventArgs (Table!, _selectedColumn, _selectedRow);
        OnCellToggled (e);

        if (e.Cancel)
        {
            return false;
        }

        if (!MultiSelect)
        {
            return null;
        }

        TableSelection [] regions = GetMultiSelectedRegionsContaining (_selectedColumn, _selectedRow).ToArray ();
        TableSelection [] toggles = regions.Where (s => s.IsToggled).ToArray ();

        // Toggle it off
        if (toggles.Any ())
        {
            IEnumerable<TableSelection> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
            MultiSelectedRegions.Clear ();

            foreach (TableSelection region in oldRegions)
            {
                if (!toggles.Contains (region))
                {
                    MultiSelectedRegions.Push (region);
                }
            }
        }
        else
        {
            // user is toggling selection within a rectangular
            // select.  So toggle the full region
            if (regions.Any ())
            {
                foreach (TableSelection r in regions)
                {
                    r.IsToggled = true;
                }
            }
            else
            {
                // Toggle on a single cell selection
                MultiSelectedRegions.Push (CreateTableSelection (_selectedColumn, SelectedRow, _selectedColumn, _selectedRow, true));
            }
        }

        return true;
    }

    /// <summary>Unions the current selected cell (and/or regions) with the provided cell and makes it the active one.</summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    private void UnionSelection (int col, int row)
    {
        if (!MultiSelect || TableIsNullOrInvisible ())
        {
            return;
        }

        EnsureValidSelection ();
        int oldColumn = SelectedColumn;
        int oldRow = SelectedRow;

        // move us to the new cell
        SelectedColumn = col;
        SelectedRow = row;
        MultiSelectedRegions.Push (CreateTableSelection (col, row));

        // if the old cell was not part of a rectangular select
        // or otherwise selected we need to retain it in the selection
        if (!IsSelected (oldColumn, oldRow))
        {
            MultiSelectedRegions.Push (CreateTableSelection (oldColumn, oldRow));
        }
    }
}
