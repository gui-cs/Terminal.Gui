using System.Data;
using System.Globalization;

namespace Terminal.Gui.Views;

public partial class TableView
{
    /// <summary>The default minimum cell width for <see cref="ColumnStyle.MinAcceptableWidth"/></summary>
    public const int DEFAULT_MIN_ACCEPTABLE_WIDTH = 100;

    private ITableSource? _table;

    /// <summary>The data table to render in the view.  Setting this property automatically updates and redraws the control.</summary>
    public ITableSource? Table
    {
        get => _table;
        set
        {
            _table = value;

            if (_table is null || _table.Columns <= 0 || _table.Rows <= 0)
            {
                Value = null;
            }
            else
            {
                SetSelection (0, 0, false);
            }

            RefreshContentSize ();
            Update ();
        }
    }

    /// <summary>
    ///     Gets or sets whether all rows should be used when calculating content size. When <see langword="false"/>,
    ///     only visible rows are used for column width calculations.
    /// </summary>
    public bool UseAllRowsForContentCalculation
    {
        get;
        set
        {
            field = value;
            RefreshContentSize ();
        }
    }

    /// <summary>
    ///     Recalculates and updates the content size based on the current state.
    /// </summary>
    /// <remarks>
    ///     Call this method after making changes that affect the content's dimensions to ensure the
    ///     layout remains accurate.
    ///     Also call this if data in Table has changed.
    /// </remarks>
    public void RefreshContentSize () => SetContentSize (CalculateContentSize ());

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);

        if (_inCalculatingContentSize)
        {
            return;
        }

        if (e.OldViewport.Size != e.NewViewport.Size || (!UseAllRowsForContentCalculation && e.OldViewport.Y != e.NewViewport.Y))
        {
            RefreshContentSize (); //mainly needed only for ExpandLastColumn?!
        }
    }

    private bool? HandleRight (ICommandContext? ctx)
    {
        int oldSelectedCol = _selectedColumn;
        int oldViewportX = Viewport.X;
        bool result = ChangeSelectionByOffsetWithReturn (1, 0, ctx);

        if (oldSelectedCol != _selectedColumn || Viewport.X >= MaxViewPort ().X)
        {
            return result;
        }
        Point maxViewPort = MaxViewPort ();
        Viewport = Viewport with { X = Math.Min (oldViewportX + 1, maxViewPort.X) };

        return result;
    }

    private bool? HandleUp (ICommandContext? ctx)
    {
        if (_selectedRow != 0)
        {
            return ChangeSelectionByOffsetWithReturn (0, -1, ctx);
        }

        if (Viewport.Y <= 0)
        {
            return false;
        }
        Viewport = Viewport with { Y = Viewport.Y - 1 };

        return true;
    }

    private bool? HandleDown (ICommandContext? ctx)
    {
        if (Table == null || _selectedRow < Table.Rows - 1)
        {
            return ChangeSelectionByOffsetWithReturn (0, 1, ctx);
        }

        if (Viewport.Y >= GetContentHeight () - Viewport.Height)
        {
            return false;
        }
        Viewport = Viewport with { Y = Viewport.Y + 1 };

        return true;

    }

    /// <summary>Moves the selection down by one page</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public void PageDown (bool extend, ICommandContext? ctx)
    {
        int oldSelectedRow = _selectedRow;
        ChangeSelectionByOffset (0, Viewport.Height /* - CurrentHeaderHeightVisible ()*/, extend, ctx);

        //after scrolling the cells, also scroll to lower line
        int remainingJump = Viewport.Height - (_selectedRow - oldSelectedRow);
        Point maxViewPort = MaxViewPort ();

        if (remainingJump > 0 && Viewport.Y < maxViewPort.Y)
        {
            Viewport = Viewport with { Y = Math.Min (Viewport.Y + remainingJump, maxViewPort.Y) };
        }

        Update ();
    }

    /// <summary>Moves the selection up by one page</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public void PageUp (bool extend, ICommandContext? ctx)
    {
        int oldSelectedRow = _selectedRow;
        ChangeSelectionByOffset (0, -Viewport.Height /* - CurrentHeaderHeightVisible ()*/, extend, ctx);

        //after scrolling the cells, also scroll to header
        int remainingJump = Viewport.Height - (oldSelectedRow - _selectedRow);

        if (remainingJump > 0 && Viewport.Y > 0)
        {
            Viewport = Viewport with { Y = Math.Max (Viewport.Y - remainingJump, 0) };
        }

        Update ();
    }

    /// <summary>
    ///     Moves or extends the selection to the final cell in the table (nX,nY). If <see cref="FullRowSelect"/> is
    ///     enabled then selection instead moves to (cursor.X, nY) — no horizontal scrolling.
    /// </summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public void ChangeSelectionToEndOfTable (bool extend, ICommandContext? ctx)
    {
        if (TableIsNullOrInvisible ())
        {
            return;
        }

        int finalColumn = Table!.Columns - 1;
        SetSelection (FullRowSelect ? _selectedColumn : finalColumn, Table.Rows - 1, extend, ctx);
        Update ();
    }

    /// <summary>
    ///     Moves or extends the selection to the first cell in the table (0,0). If <see cref="FullRowSelect"/> is enabled
    ///     then selection instead moves to (cursor.X, 0) — no horizontal scrolling.
    /// </summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public void ChangeSelectionToStartOfTable (bool extend, ICommandContext? ctx)
    {
        if (TableIsNullOrInvisible ())
        {
            return;
        }

        SetSelection (FullRowSelect ? _selectedColumn : 0, 0, extend, ctx);
        Update ();
    }

    /// <summary>
    ///     Returns a new rectangle between the two points with positive width/height regardless of relative positioning
    ///     of the points.  pt1 is always considered the <see cref="TableSelectionRegion.Origin"/> point
    /// </summary>
    /// <param name="pt1X">Origin point for the selection in X</param>
    /// <param name="pt1Y">Origin point for the selection in Y</param>
    /// <param name="pt2X">End point for the selection in X</param>
    /// <param name="pt2Y">End point for the selection in Y</param>
    /// <param name="toggle">True if selection is result of <see cref="Command.ToggleExtend"/></param>
    /// <returns></returns>
    private TableSelectionRegion CreateTableSelectionRegion (int pt1X, int pt1Y, int pt2X, int pt2Y, bool toggle = false)
    {
        int top = Math.Max (Math.Min (pt1Y, pt2Y), 0);
        int bot = Math.Max (Math.Max (pt1Y, pt2Y), 0);
        int left = Math.Max (Math.Min (pt1X, pt2X), 0);
        int right = Math.Max (Math.Max (pt1X, pt2X), 0);

        // Rect class is inclusive of Top Left but exclusive of Bottom Right so extend by 1
        return new TableSelectionRegion (new Point (pt1X, pt1Y), new Rectangle (left, top, right - left + 1, bot - top + 1)) { IsExtended = toggle };
    }

    /// <summary>Returns a single point as a <see cref="TableSelectionRegion"/></summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private TableSelectionRegion CreateTableSelectionRegion (int x, int y) => CreateTableSelectionRegion (x, y, x, y);

    private bool CycleToNextTableEntryBeginningWith (Key key)
    {
        int row = _selectedRow;

        // There is a multi select going on and not just for the current row
        if (GetAllSelectedCells ().Any (c => c.Y != row))
        {
            return false;
        }

        int? match = CollectionNavigator.GetNextMatchingItem (row, (char)key);

        if (match == null)
        {
            return false;
        }

        _selectedRow = match.Value;
        CommitSelectionState ();
        EnsureValidSelection ();
        EnsureCursorIsVisible ();
        SetNeedsDraw ();

        return true;
    }

    /// <summary>
    ///     Returns true if the <see cref="Table"/> is not set or all the columns in the <see cref="Table"/> have an
    ///     explicit <see cref="ColumnStyle"/> that marks them <see cref="ColumnStyle.Visible"/> <see langword="false"/>.
    /// </summary>
    /// <returns></returns>
    private bool TableIsNullOrInvisible () =>
        Table is not { Columns: > 0 } || Enumerable.Range (0, Table.Columns).All (c => Style.GetColumnStyleIfAny (c)?.Visible is false);

    /// <summary>
    ///     Generates a new demo <see cref="DataTable"/> with the given number of <paramref name="cols"/> (min 5) and
    ///     <paramref name="rows"/>
    /// </summary>
    /// <param name="cols"></param>
    /// <param name="rows"></param>
    /// <returns></returns>
    public static DataTable BuildDemoDataTable (int cols, int rows)
    {
        var dt = new DataTable ();
        var explicitCols = 6;
        dt.Columns.Add (new DataColumn ("StrCol", typeof (string)));
        dt.Columns.Add (new DataColumn ("DateCol", typeof (DateTime)));
        dt.Columns.Add (new DataColumn ("IntCol", typeof (int)));
        dt.Columns.Add (new DataColumn ("DoubleCol", typeof (double)));
        dt.Columns.Add (new DataColumn ("NullsCol", typeof (string)));
        dt.Columns.Add (new DataColumn ("Unicode", typeof (string)));
        dt.Columns.Add (new DataColumn ("VarLength", typeof (string))); //ColIdx = 6

        for (var i = 0; i < cols - explicitCols; i++)
        {
            dt.Columns.Add ("Column" + (i + explicitCols));
        }

        var r = new Random (100);

        string numberText = NumberText (rows);

        for (var i = 0; i < rows; i++)
        {
            List<object> row =
            [
                $"Demo text in row {i}",
                new DateTime (2000 + i, 12, 25),
                r.Next (i),
                r.NextDouble () * i - 0.5 /*add some negatives to demo styles*/,
                DBNull.Value,
                "Les Mise" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "rables",
                numberText [..i]
            ];

            for (var j = 0; j < cols - explicitCols; j++)
            {
                row.Add ("SomeValue" + r.Next (100));
            }

            dt.Rows.Add (row.ToArray ());
        }

        return dt;

        static string NumberText (int len)
        {
            var result = string.Empty;

            for (var i = 1; i <= len; i++)
            {
                result += i % 10;
            }

            return result;
        }
    }
}
