namespace Terminal.Gui.Views;

public partial class TableView
{
    private int _selectedColumn = -1;
    private int _selectedRow = -1;

    /// <summary>
    ///     Moves the cursor by the provided offsets. Optionally starting a box selection (see <see cref="MultiSelect"/>).
    /// </summary>
    /// <param name="offsetX">Offset in number of columns</param>
    /// <param name="offsetY">Offset in number of rows</param>
    /// <param name="extendExistingSelection">True to create a multi cell selection or adjust an existing one</param>
    /// <param name="ctx">The command context.</param>
    public void ChangeSelectionByOffset (int offsetX, int offsetY, bool extendExistingSelection, ICommandContext? ctx)
    {
        SetSelection (_selectedColumn + offsetX, _selectedRow + offsetY, extendExistingSelection, ctx);
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

        SetSelection (Table!.Columns - 1, _selectedRow, extend, ctx);
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

        SetSelection (0, _selectedRow, extend, ctx);
        Update ();
    }

    #region Cursor

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

        ColumnToRender? selectedColToRender = cellInfos.FirstOrDefault (c => c.Column == _selectedColumn);

        if (_selectedColumn < 0 || selectedColToRender == null || _selectedRow < 0 || _selectedRow >= Table.Rows)
        {
            return;
        }

        int rowStart = Style.AlwaysShowHeaders ? Viewport.Y : Math.Max (Viewport.Y - headerHeight, 0);
        int rowEnd = Viewport.Y + Viewport.Height - headerHeight - 1;

        if (rowEnd < rowStart)
        {
            return;
        }

        if (_selectedRow < rowStart)
        {
            Viewport = Viewport with { Y = Viewport.Y - (rowStart - _selectedRow) };
        }

        if (_selectedRow > rowEnd)
        {
            Viewport = Viewport with { Y = Viewport.Y + (_selectedRow - rowEnd) };
        }

        //first column that is visible from start
        ColumnToRender? colStart = cellInfos.FirstOrDefault (c => c.X - 1 > Viewport.Left);

        //last column that is visible (at least the start)
        ColumnToRender? colEnd = cellInfos.LastOrDefault (c => c.X < Viewport.Right);

        if (colEnd is { } && _selectedColumn >= colEnd.Column)
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

        if (colStart is { } && _selectedColumn >= colStart.Column)
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
    ///     Syncs the internal cursor and <see cref="MultiSelectedRegions"/> from the current <see cref="Value"/>.
    /// </summary>
    private void SyncCursorFromValue ()
    {
        if (_value is null)
        {
            _selectedColumn = -1;
            _selectedRow = -1;
            MultiSelectedRegions.Clear ();

            return;
        }

        _selectedColumn = _value.Cursor.X;
        _selectedRow = _value.Cursor.Y;

        // Rebuild MultiSelectedRegions from Value.Regions (deep copy)
        MultiSelectedRegions.Clear ();

        foreach (TableSelectionRegion region in _value.Regions)
        {
            MultiSelectedRegions.Push (new TableSelectionRegion (region.Origin, region.Rectangle) { IsExtended = region.IsExtended });
        }
    }

    #endregion Cursor

    /// <summary>
    ///     Updates the cursor position, the <see cref="MultiSelectedRegions"/>, and <see cref="Value"/> to ensure they are
    ///     within the bounds of the table (by adjusting them to the nearest existing cell). Has no effect if
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

        _selectedColumn = Math.Max (Math.Min (_selectedColumn, Table!.Columns - 1), 0);
        _selectedRow = Math.Max (Math.Min (_selectedRow, Table.Rows - 1), 0);

        // If _selectedColumn is invisible move it to a visible one
        _selectedColumn = GetNearestVisibleColumn (_selectedColumn, true, true);
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

            // Clamp region to table bounds
            Point clampedOrigin = new (Math.Max (Math.Min (region.Origin.X, Table.Columns - 1), 0), Math.Max (Math.Min (region.Origin.Y, Table.Rows - 1), 0));

            Rectangle clampedRect = Rectangle.FromLTRB (region.Rectangle.Left,
                                                        region.Rectangle.Top,
                                                        Math.Max (Math.Min (region.Rectangle.Right, Table.Columns), 0),
                                                        Math.Max (Math.Min (region.Rectangle.Bottom, Table.Rows), 0));

            MultiSelectedRegions.Push (new TableSelectionRegion (clampedOrigin, clampedRect) { IsExtended = region.IsExtended });
        }
    }

    /// <summary>True to select the entire row at once. False to select individual cells. Defaults to <see langword="false"/>.</summary>
    public bool FullRowSelect { get; set; }

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
        if (MultiSelect && MultiSelectedRegions.Count == 0)
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
                toReturn.Add (new Point (x, _selectedRow));
            }
        }
        else
        {
            // Not full row select and no multi selections
            toReturn.Add (new Point (_selectedColumn, _selectedRow));
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

        return row == _selectedRow && (col == _selectedColumn || FullRowSelect);
    }

    /// <summary>True to allow multi-cell region selections. Defaults to <see langword="true"/>.</summary>
    public bool MultiSelect { get; set; } = true;

    /// <summary>
    ///     When <see cref="MultiSelect"/> is enabled, contains all rectangles of selected cells.  Rectangles
    ///     describe column/row regions selected in <see cref="Table"/> (not screen coordinates).
    ///     Use <see cref="Value"/> to read the current selection state (cursor + regions).
    /// </summary>
    public Stack<TableSelectionRegion> MultiSelectedRegions { get; } = new ();

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
        MultiSelectedRegions.Push (new TableSelectionRegion (new Point (_selectedColumn, _selectedRow), new Rectangle (0, 0, Table.Columns, _table!.Rows)));
        CommitSelectionState ();
        Update ();
    }

    /// <summary>
    ///     Moves the cursor to the given col/row in <see cref="Table"/>.
    ///     Optionally starts a box selection (see <see cref="MultiSelect"/>).
    /// </summary>
    /// <param name="col">Column index.</param>
    /// <param name="row">Row index.</param>
    /// <param name="extendExistingSelection">True to create a multi cell selection or adjust an existing one</param>
    /// <param name="ctx">The command context.</param>
    public void SetSelection (int col, int row, bool extendExistingSelection, ICommandContext? ctx = null)
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
            if (MultiSelectedRegions.Count == 0 || MultiSelectedRegions.All (m => m.IsExtended))
            {
                // Create a new region between the old active cell and the new cell
                TableSelectionRegion rect = CreateTableSelectionRegion (_selectedColumn, _selectedRow, col, row);
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

        // Write backing fields directly and commit once to avoid double-fire
        _selectedColumn = TableIsNullOrInvisible () ? 0 : Math.Min (Table!.Columns - 1, Math.Max (0, col));
        _selectedRow = TableIsNullOrInvisible () ? 0 : Math.Min (Table!.Rows - 1, Math.Max (0, row));
        CommitSelectionState ();
    }

    /// <summary>
    ///     Private override of <see cref="ChangeSelectionByOffset"/> that returns <see langword="true"/> if the
    ///     <see cref="Value"/> changed as a result of moving the selection.
    /// </summary>
    /// <param name="offsetX"></param>
    /// <param name="offsetY"></param>
    /// <param name="ctx">The command context.</param>
    /// <returns></returns>
    private bool ChangeSelectionByOffsetWithReturn (int offsetX, int offsetY, ICommandContext? ctx)
    {
        TableSelection? oldValue = Value;
        SetSelection (_selectedColumn + offsetX, _selectedRow + offsetY, false, ctx);
        Update ();

        return !Equals (oldValue, Value);
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

    /// <summary>Syncs the <see cref="Value"/> from the internal cursor/region state.</summary>
    private void CommitSelectionState () => UpdateValueFromInternalState ();

    private IEnumerable<TableSelectionRegion> GetMultiSelectedRegionsContaining (int col, int row)
    {
        if (!MultiSelect)
        {
            return [];
        }

        return FullRowSelect
                   ? MultiSelectedRegions.Where (r => r.Rectangle.Bottom > row && r.Rectangle.Top <= row)
                   : MultiSelectedRegions.Where (r => r.Rectangle.Contains (col, row));
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
        if (!MultiSelect)
        {
            return null;
        }

        TableSelectionRegion [] regions = GetMultiSelectedRegionsContaining (_selectedColumn, _selectedRow).ToArray ();
        TableSelectionRegion [] toggles = regions.Where (s => s.IsExtended).ToArray ();

        // Toggle it off
        if (toggles.Length == 0)
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
            // User is toggling selection within a rectangular select — mark the matching regions as extended
            if (regions.Length == 0)
            {
                IEnumerable<TableSelectionRegion> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
                MultiSelectedRegions.Clear ();

                foreach (TableSelectionRegion region in oldRegions)
                {
                    MultiSelectedRegions.Push (regions.Contains (region)
                                                   ? new TableSelectionRegion (region.Origin, region.Rectangle) { IsExtended = true }
                                                   : region);
                }
            }
            else
            {
                // Toggle on a single cell selection
                MultiSelectedRegions.Push (CreateTableSelectionRegion (_selectedColumn, _selectedRow, _selectedColumn, _selectedRow, true));
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
        int oldColumn = _selectedColumn;
        int oldRow = _selectedRow;

        // move us to the new cell
        _selectedColumn = col;
        _selectedRow = row;
        MultiSelectedRegions.Push (CreateTableSelectionRegion (col, row));

        // if the old cell was not part of a rectangular select
        // or otherwise selected we need to retain it in the selection
        if (!IsSelected (oldColumn, oldRow))
        {
            MultiSelectedRegions.Push (CreateTableSelectionRegion (oldColumn, oldRow));
        }

        CommitSelectionState ();
    }

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
    ///     Builds a <see cref="TableSelection"/> from the current internal state and sets <see cref="Value"/>.
    /// </summary>
    private void UpdateValueFromInternalState ()
    {
        if (TableIsNullOrInvisible ())
        {
            Value = null;

            return;
        }

        // Deep-copy regions so Value snapshots are immutable
        List<TableSelectionRegion> regions = MultiSelectedRegions.Reverse ()
                                                                 .Select (r => new TableSelectionRegion (r.Origin, r.Rectangle) { IsExtended = r.IsExtended })
                                                                 .ToList ();
        TableSelection newSelection = new (new Point (_selectedColumn, _selectedRow), regions);
        Value = newSelection;
    }

    #endregion
}
