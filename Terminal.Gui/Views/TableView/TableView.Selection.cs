using System.Data;

namespace Terminal.Gui.Views;

/// <summary>
///     Displays and enables infinite scrolling through tabular data based on a <see cref="ITableSource"/>.
///     <a href="../docs/tableview.md">See the TableView Deep Dive for more</a>.
/// </summary>
public partial class TableView
{
    #region IValue<TableSelection?> Implementation

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<TableSelection?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<TableSelection?>>? ValueChanged;

    /// <summary>
    ///     Called when <see cref="Value"/> is about to change. Return <see langword="true"/> to cancel the change.
    /// </summary>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<TableSelection?> args) => false;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    protected virtual void OnValueChanged (ValueChangedEventArgs<TableSelection?> args) { }

    private TableSelection? _value;

    /// <inheritdoc/>
    public TableSelection? Value
    {
        get => _value;
        set
        {
            if (Equals (_value, value))
            {
                return;
            }

            TableSelection? oldValue = _value;
            ValueChangingEventArgs<TableSelection?> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            _value = changingArgs.NewValue;
            SetNeedsDraw ();

            // Sync internal cursor state from Value
            SyncCursorFromValue ();

            ValueChangedEventArgs<TableSelection?> changedArgs = new (oldValue, _value);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));
        }
    }

    /// <summary>
    ///     Syncs the internal <see cref="SelectedColumn"/>/<see cref="SelectedRow"/> and <see cref="MultiSelectedRegions"/>
    ///     from the current <see cref="Value"/>. This bridges the new <see cref="IValue{T}"/> model with the legacy
    ///     internal state during the transition.
    /// </summary>
    private void SyncCursorFromValue ()
    {
        if (_value is null)
        {
            _selectedColumn = -1;
            _selectedRow = -1;

            return;
        }

        _selectedColumn = _value.Cursor.X;
        _selectedRow = _value.Cursor.Y;
    }

    /// <summary>
    ///     Builds a <see cref="TableSelection"/> from the current internal state and sets <see cref="Value"/>.
    /// </summary>
    private void UpdateValueFromInternalState ()
    {
        if (TableIsNullOrInvisible ())
        {
            Value = null;

            return;
        }

        List<TableSelectionRegion> regions = [.. MultiSelectedRegions.Reverse ()];
        TableSelection newSelection = new (new Point (SelectedColumn, SelectedRow), regions);
        Value = newSelection;
    }

    #endregion

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
    public Stack<TableSelectionRegion> MultiSelectedRegions { get; } = new ();

    /// <summary>
    ///     The index of <see cref="DataTable.Columns"/> in <see cref="Table"/> that the user has currently selected. This is
    ///     the cursor.
    /// </summary>
    internal int SelectedColumn
    {
        get => _selectedColumn;
        set
        {
            if (_selectedColumn == value)
            {
                return;
            }

            int oldValue = _selectedColumn;

            // try to prevent this being set to an out-of-bounds column
            _selectedColumn = TableIsNullOrInvisible () ? 0 : Math.Min (Table!.Columns - 1, Math.Max (0, value));

            if (oldValue != _selectedColumn)
            {
                RaiseCursorChanged (new CursorChangedEventArgs (Table!, oldValue, _selectedColumn, _selectedRow, _selectedRow));
            }
        }
    }

    /// <summary>
    ///     The index of <see cref="DataTable.Rows"/> in <see cref="Table"/> that the user has currently selected. This is the
    ///     cursor.
    /// </summary>
    internal int SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (value == _selectedRow)
            {
                return;
            }

            int oldValue = _selectedRow;
            _selectedRow = TableIsNullOrInvisible () ? 0 : Math.Min (Table!.Rows - 1, Math.Max (0, value));

            if (oldValue != _selectedRow)
            {
                RaiseCursorChanged (new CursorChangedEventArgs (Table!, _selectedColumn, _selectedColumn, oldValue, _selectedRow));
            }
        }
    }

    private int _selectedColumn = -1;
    private int _selectedRow = -1;

    /// <summary>
    ///     Private override of <see cref="ChangeSelectionByOffset"/> that returns true if the selection has
    ///     changed as a result of moving the selection. Used by key handling logic to determine whether e.g.
    ///     the cursor right resulted in a change or should be forwarded on to toggle logic handling.
    /// </summary>
    /// <param name="offsetX"></param>
    /// <param name="offsetY"></param>
    /// <param name="ctx">The command context.</param>
    /// <returns></returns>
    private bool ChangeSelectionByOffsetWithReturn (int offsetX, int offsetY, ICommandContext? ctx)
    {
        TableViewSelectionSnapshot oldSelection = GetSelectionSnapshot ();
        SetSelection (SelectedColumn + offsetX, SelectedRow + offsetY, false, ctx);
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
    /// <param name="ctx">The command context.</param>
    public void ChangeSelectionByOffset (int offsetX, int offsetY, bool extendExistingSelection, ICommandContext? ctx)
    {
        SetSelection (SelectedColumn + offsetX, SelectedRow + offsetY, extendExistingSelection, ctx);
        Update ();
    }

    /// <summary>Moves or extends the selection to the last cell in the current row</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public void ChangeSelectionToEndOfRow (bool extend, ICommandContext? ctx)
    {
        if (TableIsNullOrInvisible ())
        {
            return;
        }

        SetSelection (Table!.Columns - 1, SelectedRow, extend, ctx);
        Update ();
    }

    /// <summary>Moves or extends the selection to the first cell in the current row</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public void ChangeSelectionToStartOfRow (bool extend, ICommandContext? ctx)
    {
        if (TableIsNullOrInvisible ())
        {
            return;
        }

        SetSelection (0, SelectedRow, extend, ctx);
        Update ();
    }

    /// <summary>
    ///     Updates scroll offsets to ensure that the selected cell is visible.  Has no effect if <see cref="Table"/> has
    ///     not been set.
    /// </summary>
    /// <remarks>
    ///     Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>
    /// </remarks>
    public void EnsureCursorIsVisible ()
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
        IEnumerable<TableSelectionRegion> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
        MultiSelectedRegions.Clear ();

        // evaluate
        foreach (TableSelectionRegion region in oldRegions)
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
        MultiSelectedRegions.Push (new TableSelectionRegion (new Point (SelectedColumn, SelectedRow), new Rectangle (0, 0, Table.Columns, _table!.Rows)));
        Update ();
    }

    /// <summary>
    ///     Moves the <see cref="SelectedRow"/> and <see cref="SelectedColumn"/> to the given col/row in
    ///     <see cref="Table"/>. Optionally starting a box selection (see <see cref="MultiSelect"/>)
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <param name="extendExistingSelection">True to create a multi cell selection or adjust an existing one</param>
    /// <param name="ctx">The command context.</param>
    public void SetSelection (int col, int row, bool extendExistingSelection, ICommandContext? ctx = null)
    {
        // if we are trying to increase the column index then
        // we are moving right otherwise we are moving left
        bool lookRight = col > SelectedColumn;
        col = GetNearestVisibleColumn (col, lookRight, true);

        if (!MultiSelect || !extendExistingSelection)
        {
            ClearMultiSelectedRegions (true);
        }

        if (extendExistingSelection)
        {
            // If we are extending current selection but there isn't one
            if (MultiSelectedRegions.Count == 0 || MultiSelectedRegions.All (m => m.IsExtended))
            {
                // Create a new region between the old active cell and the new cell
                TableSelectionRegion rect = CreateTableSelectionRegion (SelectedColumn, SelectedRow, col, row);
                MultiSelectedRegions.Push (rect);
            }
            else
            {
                // Extend the current head selection to include the new cell
                TableSelectionRegion head = MultiSelectedRegions.Pop ();
                TableSelectionRegion newRect = CreateTableSelectionRegion (head.Origin.X, head.Origin.Y, col, row);
                MultiSelectedRegions.Push (newRect);
            }
        }

        SelectedColumn = col;
        SelectedRow = row;
    }

    // TODO: Refactor to use CWP
    /// <summary>Invokes the <see cref="CursorChanged"/> event and updates <see cref="Value"/>.</summary>
    private void RaiseCursorChanged (CursorChangedEventArgs args)
    {
        // Legacy
        CursorChanged?.Invoke (this, args);

        UpdateValueFromInternalState ();
    }

    private void ClearMultiSelectedRegions (bool keepToggledSelections)
    {
        if (!keepToggledSelections)
        {
            MultiSelectedRegions.Clear ();

            return;
        }

        IEnumerable<TableSelectionRegion> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
        MultiSelectedRegions.Clear ();

        foreach (TableSelectionRegion region in oldRegions)
        {
            if (region.IsExtended)
            {
                MultiSelectedRegions.Push (region);
            }
        }
    }

    private IEnumerable<TableSelectionRegion> GetMultiSelectedRegionsContaining (int col, int row)
    {
        if (!MultiSelect)
        {
            return Enumerable.Empty<TableSelectionRegion> ();
        }

        if (FullRowSelect)
        {
            return MultiSelectedRegions.Where (r => r.Rectangle.Bottom > row && r.Rectangle.Top <= row);
        }

        return MultiSelectedRegions.Where (r => r.Rectangle.Contains (col, row));
    }

    /// <summary>
    ///     Handles <see cref="Command.ToggleExtend"/>: extends or un-extends a cell from the multi-selection.
    ///     For keyboard (Space): toggles the current cell's extended state.
    ///     For mouse with Ctrl: unions the clicked cell into the selection.
    ///     For mouse with Alt: extends/creates a rectangular region to the clicked cell.
    /// </summary>
    private bool? ToggleExtend (ICommandContext? ctx)
    {
        // Mouse-based extend (Ctrl+Click or Alt+Click)
        if (ctx?.Binding is MouseBinding { MouseEvent: { } } mouseBinding)
        {
            return ToggleExtendMouse (mouseBinding);
        }

        // Keyboard-based toggle (Space)
        return ToggleExtendKeyboard ();
    }

    /// <summary>Handles keyboard-based ToggleExtend (Space key): toggles the current cell's extended state.</summary>
    private bool? ToggleExtendKeyboard ()
    {
        CellToggledEventArgs e = new (Table!, SelectedColumn, SelectedRow);
        OnCellToggled (e);

        if (e.Cancel)
        {
            return false;
        }

        if (!MultiSelect)
        {
            return null;
        }

        TableSelectionRegion [] regions = GetMultiSelectedRegionsContaining (SelectedColumn, SelectedRow).ToArray ();
        TableSelectionRegion [] toggles = regions.Where (s => s.IsExtended).ToArray ();

        // Toggle it off
        if (toggles.Any ())
        {
            IEnumerable<TableSelectionRegion> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
            MultiSelectedRegions.Clear ();

            foreach (TableSelectionRegion region in oldRegions)
            {
                if (!toggles.Contains (region))
                {
                    MultiSelectedRegions.Push (region);
                }
            }
        }
        else
        {
            // User is toggling selection within a rectangular select — toggle the full region
            if (regions.Any ())
            {
                foreach (TableSelectionRegion r in regions)
                {
                    r.IsExtended = true;
                }
            }
            else
            {
                // Toggle on a single cell selection
                MultiSelectedRegions.Push (CreateTableSelectionRegion (SelectedColumn, SelectedRow, SelectedColumn, SelectedRow, true));
            }
        }

        return true;
    }

    /// <summary>Handles mouse-based ToggleExtend: Ctrl+Click unions, Alt+Click extends.</summary>
    private bool? ToggleExtendMouse (MouseBinding mouseBinding)
    {
        int boundsX = mouseBinding.MouseEvent!.Position!.Value.X;
        int boundsY = mouseBinding.MouseEvent.Position!.Value.Y;
        Point? hit = ScreenToCell (boundsX, boundsY);

        if (hit is null || !MultiSelect)
        {
            return false;
        }

        if (mouseBinding.MouseEvent.Flags.FastHasFlags (MouseFlags.Ctrl))
        {
            UnionSelection (hit.Value.X, hit.Value.Y);
            Update ();

            return true;
        }

        if (!mouseBinding.MouseEvent.Flags.FastHasFlags (MouseFlags.Alt))
        {
            return false;
        }

        SetSelection (hit.Value.X, hit.Value.Y, true);
        Update ();

        return false;
    }

    /// <summary>Unions the current selected cell (and/or regions) with the provided cell and makes it the active one.</summary>
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
        MultiSelectedRegions.Push (CreateTableSelectionRegion (col, row));

        // if the old cell was not part of a rectangular select
        // or otherwise selected we need to retain it in the selection
        if (!IsSelected (oldColumn, oldRow))
        {
            MultiSelectedRegions.Push (CreateTableSelectionRegion (oldColumn, oldRow));
        }
    }
}
