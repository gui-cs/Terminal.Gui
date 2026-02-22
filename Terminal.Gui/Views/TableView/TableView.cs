#nullable disable
using System.Data;

namespace Terminal.Gui.Views;

/// <summary>Delegate for providing color to <see cref="TableView"/> cells based on the value being rendered</summary>
/// <param name="args">Contains information about the cell for which color is needed</param>
/// <returns></returns>
public delegate Scheme CellColorGetterDelegate (CellColorGetterArgs args);

/// <summary>Delegate for providing color for a whole row of a <see cref="TableView"/></summary>
/// <param name="args"></param>
/// <returns></returns>
public delegate Scheme RowColorGetterDelegate (RowColorGetterArgs args);

/// <summary>
///     Displays and enables infinite scrolling through tabular data based on a <see cref="ITableSource"/>.
///     <a href="../docs/tableview.md">See the TableView Deep Dive for more</a>.
/// </summary>
public partial class TableView : View, IDesignable
{
    /// <summary>
    ///     The default maximum cell width for <see cref="TableView.MaxCellWidth"/> and <see cref="ColumnStyle.MaxWidth"/>
    /// </summary>
    public const int DEFAULT_MAX_CELL_WIDTH = 100;

    /// <summary>Initializes a <see cref="TableView"/> class.</summary>
    /// <param name="table">The table to display in the control</param>
    public TableView (ITableSource table) : this () { Table = table; }

    /// <summary>
    ///     Initializes a <see cref="TableView"/> class. Set the
    ///     <see cref="Table"/> property to begin editing
    /// </summary>
    public TableView ()
    {
        CanFocus = true;
        CollectionNavigator = new TableCollectionNavigator (this);

        // Things this view knows how to do
        AddCommand (Command.Right, ctx => HandleRight (ctx));

        AddCommand (Command.Left,
                    () =>
                    {
                        return ChangeSelectionByOffsetWithReturn (-1, 0);
                    });

        AddCommand (Command.Up, ctx => HandleUp (ctx));

        AddCommand (Command.Down, ctx => HandleDown (ctx));

        AddCommand (Command.PageUp,
                    () =>
                    {
                        PageUp (false);

                        return true;
                    });

        AddCommand (Command.PageDown,
                    () =>
                    {
                        PageDown (false);

                        return true;
                    });

        AddCommand (Command.LeftStart,
                    () =>
                    {
                        ChangeSelectionToStartOfRow (false);

                        return true;
                    });

        AddCommand (Command.RightEnd,
                    () =>
                    {
                        ChangeSelectionToEndOfRow (false);

                        return true;
                    });

        AddCommand (Command.Start,
                    () =>
                    {
                        ChangeSelectionToStartOfTable (false);

                        return true;
                    });

        AddCommand (Command.End,
                    () =>
                    {
                        ChangeSelectionToEndOfTable (false);

                        return true;
                    });

        AddCommand (Command.RightExtend,
                    () =>
                    {
                        ChangeSelectionByOffset (1, 0, true);

                        return true;
                    });

        AddCommand (Command.LeftExtend,
                    () =>
                    {
                        ChangeSelectionByOffset (-1, 0, true);

                        return true;
                    });

        AddCommand (Command.UpExtend,
                    () =>
                    {
                        ChangeSelectionByOffset (0, -1, true);

                        return true;
                    });

        AddCommand (Command.DownExtend,
                    () =>
                    {
                        ChangeSelectionByOffset (0, 1, true);

                        return true;
                    });

        AddCommand (Command.PageUpExtend,
                    () =>
                    {
                        PageUp (true);

                        return true;
                    });

        AddCommand (Command.PageDownExtend,
                    () =>
                    {
                        PageDown (true);

                        return true;
                    });

        AddCommand (Command.LeftStartExtend,
                    () =>
                    {
                        ChangeSelectionToStartOfRow (true);

                        return true;
                    });

        AddCommand (Command.RightEndExtend,
                    () =>
                    {
                        ChangeSelectionToEndOfRow (true);

                        return true;
                    });

        AddCommand (Command.StartExtend,
                    () =>
                    {
                        ChangeSelectionToStartOfTable (true);

                        return true;
                    });

        AddCommand (Command.EndExtend,
                    () =>
                    {
                        ChangeSelectionToEndOfTable (true);

                        return true;
                    });

        AddCommand (Command.SelectAll,
                    () =>
                    {
                        SelectAll ();

                        return true;
                    });
        AddCommand (Command.Accept, () => OnCellActivated (new CellActivatedEventArgs (Table, SelectedColumn, SelectedRow)));

        AddCommand (Command.Toggle,
                    ctx =>
                    {
                        return ToggleCurrentCellSelection () is true;
                    });

        AddCommand (Command.Activate,
            ctx =>
            {
                    return RaiseActivating (ctx) is true;
            });

        // Default keybindings for this view
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Add (Key.Home.WithCtrl, Command.Start);
        KeyBindings.Add (Key.End.WithCtrl, Command.End);
        KeyBindings.Add (Key.CursorLeft.WithShift, Command.LeftExtend);
        KeyBindings.Add (Key.CursorRight.WithShift, Command.RightExtend);
        KeyBindings.Add (Key.CursorUp.WithShift, Command.UpExtend);
        KeyBindings.Add (Key.CursorDown.WithShift, Command.DownExtend);
        KeyBindings.Add (Key.PageUp.WithShift, Command.PageUpExtend);
        KeyBindings.Add (Key.PageDown.WithShift, Command.PageDownExtend);
        KeyBindings.Add (Key.Home.WithShift, Command.LeftStartExtend);
        KeyBindings.Add (Key.End.WithShift, Command.RightEndExtend);
        KeyBindings.Add (Key.Home.WithCtrl.WithShift, Command.StartExtend);
        KeyBindings.Add (Key.End.WithCtrl.WithShift, Command.EndExtend);
        KeyBindings.Add (Key.A.WithCtrl, Command.SelectAll);
        KeyBindings.Remove (CellActivationKey);
        KeyBindings.Add (CellActivationKey, Command.Accept);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate);
    }

    /// <summary>Navigator for cycling the selected item in the table by typing. Set to null to disable this feature.</summary>
    public ICollectionNavigator CollectionNavigator { get; set; }

    /// <summary>
    ///     Horizontal scroll offset.  The index of the first column in <see cref="Table"/> to display when rendering
    ///     the view.
    /// </summary>
    /// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
    public int ColumnOffset
    {
        get => _columnsToRenderCache?.Count(c => c.X + c.Width <= Viewport.X) ?? 0;
        set
        {
            if (value < 0)
            {
                value = 0;
            }

            if (_columnsToRenderCache == null)
            {
                CalculateContentSize ();
            }

            if (value >= (_columnsToRenderCache?.Length ?? 0))
            {
                value = (_columnsToRenderCache?.Length ?? 0) - 1;
            }
            int prev = ColumnOffset;
            Viewport = Viewport with { X = _columnsToRenderCache [value].X };
            if (prev != ColumnOffset)
            {
                SetNeedsDraw ();
            }
        }
    }

    /// <summary>
    ///     The maximum number of characters to render in any given column.  This prevents one long column from pushing
    ///     out all the others
    /// </summary>
    public int MaxCellWidth { get; set; } = DEFAULT_MAX_CELL_WIDTH;

    /// <summary>The minimum number of characters to render in any given column.</summary>
    public int MinCellWidth { get; set; }

    /// <summary>The text representation that should be rendered for cells with the value <see cref="DBNull.Value"/></summary>
    public string NullSymbol { get; set; } = "-";

    /// <summary>
    ///     Vertical scroll offset.  The index of the first row in <see cref="Table"/> to display in the first non header
    ///     line of the control when rendering the view.
    /// </summary>
    public int RowOffset
    {
        get => Style.AlwaysShowHeaders
                   ? Viewport.Y
                   : Math.Max(Viewport.Y - GetHeaderHeightIfAny (), 0);
        set
        {
            var oldViewportY = Viewport.Y;

            Viewport = Viewport with
            {
                Y = value == 0
                        ? 0
                        : Style.AlwaysShowHeaders
                            ? value
                            : GetHeaderHeightIfAny () + value
            };

            if (Viewport.Y != oldViewportY)
            {
                SetNeedsDraw ();
            }
        }
    }

    /// <summary>
    ///     The symbol to add after each cell value and header value to visually separate values (if not using vertical
    ///     gridlines)
    /// </summary>
    public char SeparatorSymbol { get; set; } = ' ';

    private TableStyle _style = new ();

    /// <summary>Contains options for changing how the table is rendered</summary>
    public TableStyle Style
    {
        get => _style;
        set
        {
            _style = value;
            Update ();
        }
    }

    private ColumnToRender[] _columnsToRenderCache = null;

    private bool inCalculatingContentSize = false;

    /// <summary>
    ///     This event is raised when a cell is activated e.g. by double-clicking or pressing
    ///     <see cref="CellActivationKey"/>
    /// </summary>
    public event EventHandler<CellActivatedEventArgs> CellActivated;

    /// <summary>This event is raised when a cell is toggled (see <see cref="Command.Activate"/></summary>
    public event EventHandler<CellToggledEventArgs> CellToggled;

    /// <summary>
    ///     Returns the screen position (relative to the control client area) that the given cell is rendered or null if
    ///     it is outside the current scroll area or no table is loaded
    /// </summary>
    /// <param name="tableColumn">The index of the <see cref="Table"/> column you are looking for</param>
    /// <param name="tableRow">The index of the row in <see cref="Table"/> that you are looking for</param>
    /// <returns></returns>
    public Point? CellToScreen (int tableColumn, int tableRow)
    {
        if (TableIsNullOrInvisible ())
        {
            return null;
        }

        ColumnToRender [] cellInfos = NonHiddenCellInfos ();
        int headerHeight = GetHeaderHeightIfAny ();
        ColumnToRender colHit = cellInfos.FirstOrDefault (c => c.Column == tableColumn);

        // current column is outside the scroll area
        if (colHit is null)
        {
            return null;
        }

        int y;
        var x = Math.Max (colHit.X, 0) - Viewport.X;

        if (x >= Viewport.Width
            || // column starts after the visible viewport
            x + colHit.Width < 0) // column ends before the visible viewport
        {
            // column is outside the horizontal scroll area
            return null;
        }

        if (Style.AlwaysShowHeaders)
        {
            y = CurrentHeaderHeightVisible () + tableRow - Viewport.Y; // + GetHeaderHeightIfAny();

            if (y < CurrentHeaderHeightVisible ()
                || // the cell is too far up above the current scroll area
                y >= Viewport.Y + Viewport.Height) // the cell is way down below the scroll area and off the screen
            {
                // column is outside the vertical scroll area
                return null;
            }
        }
        else
        {
            y = tableRow - Viewport.Y + GetHeaderHeightIfAny ();

            if (y < 0
                || // the cell is too far up above the current scroll area
                y >= Viewport.Y + Viewport.Height) // the cell is way down below the scroll area and off the screen
            {
                // column is outside the vertical scroll area
                return null;
            }
        }

        return new Point (x, y);
    }

    private record TableViewSelectionSnapshot (int SelectedColumn, int SelectedRow, Rectangle [] MultiSelection);

    private bool? HandleRight (ICommandContext? ctx)
    {
        var oldSelecteCol = SelectedColumn;
        var oldViewportX = Viewport.X;
        var result = ChangeSelectionByOffsetWithReturn (1, 0);

        if (oldSelecteCol == SelectedColumn && Viewport.X < MaxViewPort ().X)
        {
            var maxViewPort = MaxViewPort ();
            Viewport = Viewport with { X = Math.Min (oldViewportX + 1, maxViewPort.X) };
        }
        return result;
    }

    private bool? HandleUp (ICommandContext? ctx)
    {
        if (SelectedRow == 0)
        {
            if (Viewport.Y > 0)
            {
                Viewport = Viewport with { Y = Viewport.Y - 1 };

                return true;
            }
            else
            {
                return false;
            }
        }

        return ChangeSelectionByOffsetWithReturn (0, -1);
    }

    private bool? HandleDown (ICommandContext? ctx)
    {
        if (Table != null && SelectedRow >= Table.Rows - 1)
        {
            if (Viewport.Y < GetContentSize ().Height - Viewport.Height)
            {
                Viewport = Viewport with { Y = Viewport.Y + 1 };

                return true;
            }
            else
            {
                return false;
            }
        }

        return ChangeSelectionByOffsetWithReturn (0, 1);
    }

    /// <summary>Moves the selection down by one page</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    public void PageDown (bool extend)
    {
        var oldSelectedRow = SelectedRow;
        ChangeSelectionByOffset (0, Viewport.Height /* - CurrentHeaderHeightVisible ()*/, extend);

        //after scrolling the cells, also scroll to lower line
        var remainingJump = Viewport.Height - (SelectedRow - oldSelectedRow);
        var maxViewPort = MaxViewPort();
        if (remainingJump > 0 && Viewport.Y < maxViewPort.Y)
        {
            Viewport = Viewport with {Y = Math.Min (Viewport.Y + remainingJump, maxViewPort.Y)};
        }

        Update ();
    }

    /// <summary>Moves the selection up by one page</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    public void PageUp (bool extend)
    {
        var oldSelectedRow = SelectedRow;
        ChangeSelectionByOffset (0, -(Viewport.Height /* - CurrentHeaderHeightVisible ()*/), extend);

        //after scrolling the cells, also scroll to header
        var remainingJump = Viewport.Height - (oldSelectedRow - SelectedRow);
        if (remainingJump > 0 && Viewport.Y > 0)
        {
            Viewport = Viewport with {Y = Math.Max (Viewport.Y - remainingJump, 0)};
        }

        Update ();
    }

    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="clientX">X offset from the top left of the control.</param>
    /// <param name="clientY">Y offset from the top left of the control.</param>
    /// <returns>Cell clicked or null.</returns>
    public Point? ScreenToCell (int clientX, int clientY) => ScreenToCell (clientX, clientY, out _, out _);

    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="client">offset from the top left of the control.</param>
    /// <returns>The position.</returns>
    public Point? ScreenToCell (Point client) => ScreenToCell (client, out _, out _);

    /// <summary>
    ///     . Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="clientX">X offset from the top left of the control.</param>
    /// <param name="clientY">Y offset from the top left of the control.</param>
    /// <param name="headerIfAny">If the click is in a header this is the column clicked.</param>
    public Point? ScreenToCell (int clientX, int clientY, out int? headerIfAny) => ScreenToCell (clientX, clientY, out headerIfAny, out _);

    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="client">offset from the top left of the control.</param>
    /// <param name="headerIfAny">If the click is in a header this is the column clicked.</param>
    public Point? ScreenToCell (Point client, out int? headerIfAny) => ScreenToCell (client, out headerIfAny, out _);

    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="clientX">X offset from the top left of the control.</param>
    /// <param name="clientY">Y offset from the top left of the control.</param>
    /// <param name="headerIfAny">If the click is in a header this is the column clicked.</param>
    /// <param name="offsetX">The horizontal offset of the click within the returned cell.</param>
    public Point? ScreenToCell (int clientX, int clientY, out int? headerIfAny, out int? offsetX)
    {
        headerIfAny = null;
        offsetX = null;

        if (TableIsNullOrInvisible ())
        {
            return null;
        }

        ColumnToRender [] cellInfos = NonHiddenCellInfos ();
        int rowIdx;
        ColumnToRender col;

        var currentHeaderHeightVisible = CurrentHeaderHeightVisible ();
        col = cellInfos.LastOrDefault (c => c.X <= clientX + Viewport.X);
        offsetX = clientX + Viewport.X - col?.X;

        if (clientY < currentHeaderHeightVisible)
        {
            // header clicked
            headerIfAny = col?.Column;
        }

        if (Style.AlwaysShowHeaders)
        {
            rowIdx = clientY - currentHeaderHeightVisible + Viewport.Y;
        }
        else
        {
            rowIdx = clientY + Viewport.Y - GetHeaderHeightIfAny ();
        }

        // if click is off bottom of the rows don't give an
        // invalid index back to user!
        if (rowIdx >= Table.Rows)
        {
            return null;
        }

        if (col is not { } || rowIdx < 0)
        {
            return null;
        }

        offsetX = clientX - col.X;

        return new Point (col.Column, rowIdx);
    }

    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="client">offset from the top left of the control.</param>
    /// <param name="headerIfAny">If the click is in a header this is the column clicked.</param>
    /// <param name="offsetX">The horizontal offset of the click within the returned cell.</param>
    public Point? ScreenToCell (Point client, out int? headerIfAny, out int? offsetX) => ScreenToCell (client.X, client.Y, out headerIfAny, out offsetX);

    /// <summary>This event is raised when the selected cell in the table changes.</summary>
    public event EventHandler<SelectedCellChangedEventArgs> SelectedCellChanged;

    /// <summary>
    ///     Updates the view to reflect changes to <see cref="Table"/> and to (<see cref="ColumnOffset"/> /
    ///     <see cref="RowOffset"/>) etc.
    /// </summary>
    /// <remarks>This always calls <see cref="View.SetNeedsDraw()"/></remarks>
    public void Update ()
    {
        _columnsToRenderCache = null; //this will trigger a recalculation of the size and the columns when needed

        if (!IsInitialized || TableIsNullOrInvisible ())
        {
            SetNeedsDraw ();

            return;
        }

        EnsureValidScrollOffsets ();
        EnsureValidSelection ();
        EnsureSelectedCellIsVisible ();
        SetNeedsDraw ();
    }

    // TODO: Update this to follow CWP.
    /// <summary>Invokes the <see cref="CellActivated"/> event</summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> if the CellActivated event was raised.</returns>
    protected virtual bool OnCellActivated (CellActivatedEventArgs args)
    {
        CellActivated?.Invoke (this, args);

        return CellActivated is { };
    }

    /// <summary>Invokes the <see cref="CellToggled"/> event</summary>
    /// <param name="args"></param>
    protected virtual void OnCellToggled (CellToggledEventArgs args) => CellToggled?.Invoke (this, args);

    /// <summary>Returns the amount of vertical space required to display the header</summary>
    /// <returns></returns>
    internal int GetHeaderHeight ()
    {
        int heightRequired = Style.ShowHeaders ? 1 : 0;

        if (Style.ShowHorizontalHeaderOverline)
        {
            heightRequired++;
        }

        if (Style.ShowHorizontalHeaderUnderline)
        {
            heightRequired++;
        }

        return heightRequired;
    }

    /// <summary>Returns the amount of vertical space currently occupied by the header or 0 if it is not visible.</summary>
    /// <returns></returns>
    internal int GetHeaderHeightIfAny () => ShouldRenderHeaders () ? GetHeaderHeight () : 0;

    private void AddRuneAt (int col, int row, Rune ch)
    {
        Move (col, row);
        AddRune (ch);
    }

    /// <summary>
    ///     Returns the maximum of the <paramref name="col"/> name and the maximum length of data that will be rendered
    ///     starting at <see cref="RowOffset"/> and rendering <paramref name="rowsToRender"/>
    /// </summary>
    /// <param name="col">ColumnIndex</param>
    /// <param name="colStyle"></param>
    /// <param name="startRow">index of first row</param>
    /// <param name="rowsToRender">Count of rows to inspect</param>
    /// <returns></returns>
    private int CalculateMaxCellWidth (int col, ColumnStyle colStyle, int startRow, int rowsToRender)
    {
        int spaceRequired = _table.ColumnNames [col].EnumerateRunes ().Sum (c => c.GetColumns ());

        // if table has no rows
        if (Table == null || Table.Rows <= 0)
        {
            return spaceRequired;
        }

        for (int i = startRow; i < startRow + rowsToRender; i++)
        {
            // expand required space if cell is bigger than the last biggest cell or header
            spaceRequired = Math.Max (spaceRequired, GetRepresentation (Table [i, col], colStyle).EnumerateRunes ().Sum (c => c.GetColumns ()));
        }

        // Don't require more space than the style allows
        if (colStyle is { })
        {
            // enforce maximum cell width based on style
            if (spaceRequired > colStyle.MaxWidth)
            {
                spaceRequired = colStyle.MaxWidth;
            }

            // enforce minimum cell width based on style
            if (spaceRequired < colStyle.MinWidth)
            {
                spaceRequired = colStyle.MinWidth;
            }
        }

        // enforce maximum cell width based on global table style
        if (spaceRequired > MaxCellWidth)
        {
            spaceRequired = MaxCellWidth;
        }

        return spaceRequired;
    }

    /// <summary>
    ///     Returns the cells that shall be shown (all cells except the hidden ones)
    /// </summary>
    /// <returns></returns>
    private ColumnToRender [] NonHiddenCellInfos ()
    {
        if (TableIsNullOrInvisible ())
        {
            return Array.Empty<ColumnToRender> ();
        }

        if (_columnsToRenderCache == null)
        {
            RefreshContentSize();
        }

        return _columnsToRenderCache ?? Array.Empty<ColumnToRender> ();
    }

    private Size? CalculateContentSize ()
    {
        var contentSize = new Size (0, 0);
        inCalculatingContentSize = true;

        try
        {
            var headerHeight = GetHeaderHeightIfAny ();
            var headerHeightVisible = CurrentHeaderHeightVisible ();
            contentSize.Height += headerHeight + Table?.Rows ?? 0;

            if (Style.ShowHorizontalBottomline)
            {
                contentSize.Height++;
            }

            // we assume that padding is 0 here
            var padding = 0;
            var columnsToRender = new List<ColumnToRender> ();

            if (Table != null)
            {
                List<(int colIdx, ColumnStyle colStyle)> nonHiddenColumns = Enumerable.Range (0, Table.Columns)
                                                                                    .Select (c => (colIdx: c, colStyle: Style.GetColumnStyleIfAny (c)))
                                                                                    .Where (e => e.colStyle?.Visible != false)
                                                                                    .ToList ();

                int lastColIdx = nonHiddenColumns.Any ()
                                     ? nonHiddenColumns.Last ().colIdx
                                     : -1;

                //right border
                contentSize.Width += (Style.ShowVerticalHeaderLines || Style.ShowVerticalCellLines ? 1 : 0);

                int startRow = 0;
                int rowsToRender = Table.Rows;
                if (!UseAllRowsForContentCalculation)
                {
                    startRow = Style.AlwaysShowHeaders
                        ? Viewport.Y
                        : Math.Max (Viewport.Y - headerHeight, 0);

                    rowsToRender = Math.Min (Viewport.Height - headerHeightVisible, Table.Rows - startRow);
                }

                // Calculate the content size based on the table's data
                foreach ((int colIdx, ColumnStyle colStyle) in nonHiddenColumns)
                {
                    int maxContentSize = CalculateMaxCellWidth (colIdx, colStyle, startRow, rowsToRender) + padding;
                    int colWidth = maxContentSize + padding;

                    if (MinCellWidth > 0 && colWidth < MinCellWidth + padding)
                    {
                        if (MinCellWidth > MaxCellWidth)
                        {
                            colWidth = MaxCellWidth + padding;
                        }
                        else
                        {
                            colWidth = MinCellWidth + padding;
                        }
                    }

                    // ToDo: MinAcceptableWidth handling?
                    // if (colStyle is { MinAcceptableWidth: > 0 }

                    bool isVeryLast = colIdx == lastColIdx;

                    if (isVeryLast)
                    {
                        //remaining space for last column
                        int remainingSpace = Viewport.Width - contentSize.Width - (Style.ShowVerticalHeaderLines || Style.ShowVerticalCellLines ? 1 : 0);
                        if (Style.ExpandLastColumn && colWidth < remainingSpace)
                        {
                            colWidth = remainingSpace;
                        }
                    }

                    columnsToRender.Add (new ColumnToRender (colIdx, contentSize.Width, colWidth + 1, maxContentSize, lastColIdx == colIdx));

                    contentSize.Width += colWidth;

                    if (!isVeryLast)
                    {
                        // for separator symbols between columns
                        contentSize.Width += 1;
                    }
                }

                // for left border
                contentSize.Width += Style.ShowVerticalHeaderLines || Style.ShowVerticalCellLines ? 1 : 0;
            }
            else
            {
                contentSize.Width = 0;
            }

            _columnsToRenderCache = columnsToRender.ToArray ();

            //check if it makes sense to scroll to left or up if the scrolled viewport is bigger than needed
            if (Viewport.X + Viewport.Width > contentSize.Width)
            {
                Viewport = Viewport with { X = Math.Max (contentSize.Width - Viewport.Width, 0) };
            }
            if (Viewport.Y + Viewport.Height > contentSize.Height)
            {
                Viewport = Viewport with { Y = Math.Max (contentSize.Height - Viewport.Height, 0) };
            }

        }
        finally
        {
            inCalculatingContentSize = false;
        }

        return contentSize;
    }

    /// <summary>Clears a line of the console by filling it with spaces</summary>
    /// <param name="row"></param>
    /// <param name="width"></param>
    private void ClearLine (int row, int width)
    {
        if (App?.Screen.Height == 0)
        {
            return;
        }

        Move (0, row);
        SetAttribute (GetAttributeForRole (VisualRole.Normal));
        AddStr (new string (' ', width));
    }

    /// <summary>
    ///     Returns <paramref name="columnIndex"/> unless the <see cref="ColumnStyle.Visible"/> is false for the indexed
    ///     column.  If so then the index returned is nudged to the nearest visible column.
    /// </summary>
    /// <remarks>Returns <paramref name="columnIndex"/> unchanged if it is invalid (e.g. out of bounds).</remarks>
    /// <param name="columnIndex">The input column index.</param>
    /// <param name="lookRight">
    ///     When nudging invisible selections look right first. <see langword="true"/> to look right,
    ///     <see langword="false"/> to look left.
    /// </param>
    /// <param name="allowBumpingInOppositeDirection">
    ///     If we cannot find anything visible when looking in direction of
    ///     <paramref name="lookRight"/> then should we look in the opposite direction instead? Use true if you want to push a
    ///     selection to a valid index no matter what. Use false if you are primarily interested in learning about directional
    ///     column visibility.
    /// </param>
    private int GetNearestVisibleColumn (int columnIndex, bool lookRight, bool allowBumpingInOppositeDirection)
    {
        if (TryGetNearestVisibleColumn (columnIndex, lookRight, allowBumpingInOppositeDirection, out int answer))
        {
            return answer;
        }

        return columnIndex;
    }

    /// <summary>
    ///     Returns the value that should be rendered to best represent a strongly typed <paramref name="value"/> read
    ///     from <see cref="Table"/>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="colStyle">Optional style defining how to represent cell values</param>
    /// <returns></returns>
    private string GetRepresentation (object value, ColumnStyle colStyle)
    {
        if (value is null || value == DBNull.Value)
        {
            return NullSymbol;
        }

        return colStyle is { } ? colStyle.GetRepresentation (value) : value.ToString ();
    }

    private bool HasControlOrAlt (Mouse me) => me.Flags.HasFlag (MouseFlags.Alt) || me.Flags.HasFlag (MouseFlags.Ctrl);

    /// <summary>
    ///     Returns true if the given <paramref name="columnIndex"/> indexes a visible column otherwise false.  Returns
    ///     false for indexes that are out of bounds.
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <returns></returns>
    private bool IsColumnVisible (int columnIndex)
    {
        // if the column index provided is out of bounds
        if (columnIndex < 0 || columnIndex >= _table.Columns)
        {
            return false;
        }

        return Style.GetColumnStyleIfAny (columnIndex)?.Visible ?? true;
    }

    /// <summary>
    ///     Truncates or pads <paramref name="representation"/> so that it occupies exactly
    ///     <paramref name="availableHorizontalSpace"/> using the alignment specified in <paramref name="colStyle"/> (or left
    ///     if no style is defined)
    /// </summary>
    /// <param name="originalCellValue">The object in this cell of the <see cref="Table"/></param>
    /// <param name="representation">The string representation of <paramref name="originalCellValue"/></param>
    /// <param name="availableHorizontalSpace"></param>
    /// <param name="colStyle">Optional style indicating custom alignment for the cell</param>
    /// <returns></returns>
    private string TruncateOrPad (object originalCellValue, string representation, int availableHorizontalSpace, ColumnStyle colStyle)
    {
        if (string.IsNullOrEmpty (representation))
        {
            return new string (' ', availableHorizontalSpace);
        }

        // if value is too wide
        if (representation.EnumerateRunes ().Sum (c => c.GetColumns ()) >= availableHorizontalSpace)
        {
            return new string (representation.TakeWhile (c => (availableHorizontalSpace -= ((Rune) c).GetColumns ()) > 0).ToArray ());
        }

        // pad it out with spaces to the given alignment
        int toPad = availableHorizontalSpace - (representation.EnumerateRunes ().Sum (c => c.GetColumns ()) + 1 /*leave 1 space for cell boundary*/);

        return (colStyle?.GetAlignment (originalCellValue) ?? Alignment.Start) switch
               {
                   Alignment.Start => representation + new string (' ', toPad),
                   Alignment.End => new string (' ', toPad) + representation,

                   // TODO: With single line cells, centered and justified are the same right?
                   Alignment.Center or Alignment.Fill => new string (' ', (int) Math.Floor (toPad / 2.0))
                                                         + // round down
                                                         representation
                                                         + new string (' ', (int) Math.Ceiling (toPad / 2.0)), // round up
                   _ => representation + new string (' ', toPad)
               };
    }

    private bool TryGetNearestVisibleColumn (int columnIndex, bool lookRight, bool allowBumpingInOppositeDirection, out int idx)
    {
        // if the column index provided is out of bounds
        if (_table is null || columnIndex < 0 || columnIndex >= _table.Columns)
        {
            idx = columnIndex;

            return false;
        }

        // get the column visibility by index (if no style visible is true)
        bool [] columnVisibility = Enumerable.Range (0, Table.Columns).Select (c => Style.GetColumnStyleIfAny (c)?.Visible ?? true).ToArray ();

        // column is visible
        if (columnVisibility [columnIndex])
        {
            idx = columnIndex;

            return true;
        }

        int increment = lookRight ? 1 : -1;

        // move in that direction
        for (int i = columnIndex; i >= 0 && i < columnVisibility.Length; i += increment)
        {
            // if we find a visible column
            if (!columnVisibility [i])
            {
                continue;
            }

            idx = i;

            return true;
        }

        // Caller only wants to look in one direction, and we did not find any
        // visible columns in that direction
        if (!allowBumpingInOppositeDirection)
        {
            idx = columnIndex;

            return false;
        }

        // Caller will let us look in the other direction so
        // now look other way
        increment = -increment;

        for (int i = columnIndex; i >= 0 && i < columnVisibility.Length; i += increment)
        {
            // if we find a visible column
            if (!columnVisibility [i])
            {
                continue;
            }

            idx = i;

            return true;
        }

        // nothing seems to be visible so just return input index
        idx = columnIndex;

        return false;
    }

    /// <summary>Describes a desire to render a column at a given horizontal position in the UI</summary>
    internal class ColumnToRender (int col, int x, int width, int maxContentSize, bool isVeryLast)
    {
        /// <summary>The column to render</summary>
        public int Column { get; set; } = col;

        /// <summary>True if this column is the very last column in the <see cref="Table"/> (not just the last visible column)</summary>
        public bool IsVeryLast { get; } = isVeryLast;

        /// <summary>
        ///     The width that the column should occupy as calculated by <see cref="TableView.CalculateContentSize"/>.  Note
        ///     that this includes space for padding i.e. the separator between columns.
        /// </summary>
        public int Width { get; internal set; } = width;

        /// <summary>
        ///     The maximum size of the content that will be rendered in this column as calculated by <see cref="CalculateMaxCellWidth(int, ColumnStyle)"/>.
        /// </summary>
        public int MaxContentSize { get; internal set; } = maxContentSize;

        /// <summary>The horizontal position to begin rendering the column at</summary>
        public int X { get; set; } = x;
    }

    bool IDesignable.EnableForDesign ()
    {
        DataTable dt = BuildDemoDataTable (5, 5);
        Table = new DataTableSource (dt);

        return true;
    }

    // TODO: Update to use Key instead of KeyCode
    private KeyCode _cellActivationKey = KeyCode.Enter;

    // TODO: Update to use Key instead of KeyCode
    /// <summary>The key which when pressed should trigger <see cref="CellActivated"/> event.  Defaults to Enter.</summary>
    public KeyCode CellActivationKey
    {
        get => _cellActivationKey;
        set
        {
            if (_cellActivationKey == value)
            {
                return;
            }

            if (KeyBindings.TryGet (_cellActivationKey, out _))
            {
                KeyBindings.Replace (_cellActivationKey, value);
            }
            else
            {
                KeyBindings.Add (value, Command.Accept);
            }

            _cellActivationKey = value;
        }
    }

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        // If the key was bound to key command, let normal KeyDown processing happen. This enables overriding the default handling.
        // See: https://github.com/gui-cs/Terminal.Gui/issues/3950#issuecomment-2807350939
        if (KeyBindings.TryGet (key, out _))
        {
            return false;
        }

        if (CollectionNavigator != null
            && HasFocus
            && Table.Rows != 0
            && key != KeyBindings.GetFirstFromCommands (Command.Accept)
            && key != CellActivationKey
            && CollectionNavigator.Matcher.IsCompatibleKey (key)
            && !key.KeyCode.HasFlag (KeyCode.CtrlMask)
            && !key.KeyCode.HasFlag (KeyCode.AltMask)
            && Rune.IsLetterOrDigit ((Rune) key))
        {
            return CycleToNextTableEntryBeginningWith (key);
        }

        return false;
    }

#warning a candidate to remove
    private Point? _scrollLeftPoint;
    private Point? _scrollRightPoint;

    /// <summary>
    /// Gets the maximum top-left coordinates to which the viewport can be scrolled within the content area.
    /// </summary>
    /// <remarks>The returned point represents the largest X and Y values for the viewport's position such
    /// that the entire viewport remains within the bounds of the content.</remarks>
    public Point MaxViewPort ()
    {
        var contentSize = GetContentSize ();
        var maxX = Math.Max(contentSize.Width - Viewport.Width, 0);
        var maxY = Math.Max (contentSize.Height - Viewport.Height, 0);
        return new Point (maxX, maxY);
    }

    /// <summary>
    ///     Updates <see cref="ColumnOffset"/> and <see cref="RowOffset"/> where they are outside the bounds of the table
    ///     (by adjusting them to the nearest existing cell).  Has no effect if <see cref="Table"/> has not been set.
    /// </summary>
    /// <remarks>
    ///     Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>
    /// </remarks>
    public void EnsureValidScrollOffsets ()
    {
        if (TableIsNullOrInvisible ())
        {
            return;
        }

        var maxViewPort = MaxViewPort ();

        if (Viewport.Y > maxViewPort.Y)
        {
            Viewport = Viewport with {Y = Math.Max (maxViewPort.Y, 0)};
        }

        if (Viewport.X > maxViewPort.X)
        {
            Viewport = Viewport with {X = Math.Max (maxViewPort.X, 0)};
        }
    }
}
