using System.Data;
using System.Reflection;
using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests;

[TestSubject (typeof (TableView))]
public class TableViewTests : TestDriverBase
{
    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Left ()
    {
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField _);

        // Make the selected cell one in
        tableView.SetSelection (1, tableView.Value?.Cursor.Y ?? 0, false);

        // Pressing left should move us to the first column without changing focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // Because we are now on the leftmost cell a further left press should move focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf1, tableView.App!.TopRunnableView.MostFocused);
        Assert.True (tf1.HasFocus);
    }

    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Up ()
    {
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField _);

        // Make the selected cell one in
        tableView.SetSelection (tableView.Value?.Cursor.X ?? 0, 1, false);

        // First press should move us up
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorUp);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // Because we are now on the top row a further press should move focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorUp);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf1, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tf1.HasFocus);
    }

    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Right ()
    {
        GetTableViewWithSiblings (out TextField _, out TableView tableView, out TextField tf2);

        // Make the selected cell one in from the rightmost column
        tableView.SetSelection (tableView.Table!.Columns - 2, tableView.Value?.Cursor.Y ?? 0, false);

        // First press should move us to the rightmost column without changing focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorRight);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // Because we are now on the rightmost cell, a further right press should move focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorRight);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf2, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tf2.HasFocus);
    }

    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Down ()
    {
        GetTableViewWithSiblings (out TextField _, out TableView tableView, out TextField tf2);

        // Make the selected cell one in from the bottommost row
        tableView.SetSelection (tableView.Value?.Cursor.X ?? 0, tableView.Table!.Rows - 2, false);

        // First press should move us to the bottommost row without changing focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // Because we are now on the bottommost cell, a further down press should move focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorDown);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf2, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tf2.HasFocus);
    }

    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Left_ClearsSelectionFirst ()
    {
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField _);

        // Make the selected cell one in
        tableView.SetSelection (1, tableView.Value?.Cursor.Y ?? 0, false);

        // Pressing shift-left should give us a multi selection
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithShift);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);
        Assert.Equal (2, tableView.GetAllSelectedCells ().Count ());

        // Because we are now on the leftmost cell a further left press would normally move focus
        // However there is an ongoing selection so instead the operation clears the selection and
        // gets swallowed (not resulting in a focus change)
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft);

        // Selection 'clears' just to the single cell and we remain focused
        Assert.Single (tableView.GetAllSelectedCells ());
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // A further left will switch focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf1, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tf1.HasFocus);
    }

    /// <summary>
    ///     Creates 3 views on <see cref="Application.TopRunnableView"/> with the focus in the
    ///     <see cref="TableView"/>.  This is a helper method to set up tests that want to
    ///     explore moving input focus out of a tableview.
    /// </summary>
    /// <param name="tf1"></param>
    /// <param name="tableView"></param>
    /// <param name="tf2"></param>
    private static void GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField tf2)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        tableView = new TableView ();
        tableView.Viewport = new Rectangle (0, 0, 25, 10);

        tf1 = new TextField ();
        tf2 = new TextField ();
        runnable.Add (tf1);
        runnable.Add (tableView);
        runnable.Add (tf2);

        tableView.SetFocus ();

        Assert.Same (tableView, runnable.MostFocused);
        Assert.True (tableView.HasFocus);

        // Set big table
        tableView.Table = BuildTable (25, 50);
    }

    public static DataTableSource BuildTable (int cols, int rows) => BuildTable (cols, rows, out _);

    /// <summary>Builds a simple table of string columns with the requested number of columns and rows</summary>
    /// <param name="cols"></param>
    /// <param name="rows"></param>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DataTableSource BuildTable (int cols, int rows, out DataTable dt)
    {
        dt = new DataTable ();

        for (var c = 0; c < cols; c++)
        {
            dt.Columns.Add ("Col" + c);
        }

        for (var r = 0; r < rows; r++)
        {
            DataRow newRow = dt.NewRow ();

            for (var c = 0; c < cols; c++)
            {
                newRow [c] = $"R{r}C{c}";
            }

            dt.Rows.Add (newRow);
        }

        return new DataTableSource (dt);
    }

    [Fact]
    public void TableView_CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("blah");

        dt.Rows.Add ("apricot");
        dt.Rows.Add ("arm");
        dt.Rows.Add ("bat");
        dt.Rows.Add ("batman");
        dt.Rows.Add ("bates hotel");
        dt.Rows.Add ("candle");

        var tableView = new TableView ();
        tableView.Table = new DataTableSource (dt);
        tableView.HasFocus = true;
        tableView.KeyBindings.Add (Key.B, Command.Down);

        Assert.Equal (0, tableView.Value!.Cursor.Y);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (1, tableView.Value!.Cursor.Y);

        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (2, tableView.Value!.Cursor.Y);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (tableView.NewKeyDownEvent (Key.C));
        Assert.Equal (5, tableView.Value!.Cursor.Y);
    }

    [Fact]
    public void TableView_CollectionNavigatorMatcher_HotKey_Finds_Item ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("blah");

        dt.Rows.Add ("apricot");
        dt.Rows.Add ("arm");
        dt.Rows.Add ("bat");
        dt.Rows.Add ("batman");
        dt.Rows.Add ("bates hotel");
        dt.Rows.Add ("candle");

        var tableView = new TableView ();
        tableView.HotKey = Key.B;
        tableView.Table = new DataTableSource (dt);
        tableView.HasFocus = true;

        Assert.Equal (0, tableView.Value!.Cursor.Y);

        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (2, tableView.Value!.Cursor.Y);
    }

    // Copilot
    // Behavior: Space toggles multi-selection via ToggleExtend command
    [Fact]
    public void TableView_ToggleExtend_TogglesSelection ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");
        dt.Rows.Add ("Data2");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.BeginInit ();
        tableView.EndInit ();

        tableView.InvokeCommand (Command.ToggleExtend);

        Assert.True (tableView.MultiSelectedRegions.Count > 0);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void TableView_Command_Accept_FiresAccepted ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        var acceptedFired = false;

        tableView.Accepted += (_, _) => acceptedFired = true;

        tableView.InvokeCommand (Command.Accept);

        Assert.True (acceptedFired);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void TableView_Space_AddsToMultiSelectedRegions ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.BeginInit ();
        tableView.EndInit ();

        tableView.NewKeyDownEvent (Key.Space);

        Assert.True (tableView.MultiSelectedRegions.Count > 0);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void TableView_Enter_FiresAccepted ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        var acceptedFired = false;

        tableView.Accepted += (_, _) => acceptedFired = true;

        tableView.NewKeyDownEvent (Key.Enter);

        Assert.True (acceptedFired);

        tableView.Dispose ();
    }

    // Copilot - regression: TableCollectionNavigator must not throw when table is null and view is focused
    [Fact]
    public void TableCollectionNavigator_NullTable_HasFocus_DoesNotThrow ()
    {
        TableView tableView = new ();
        tableView.HasFocus = true;

        // Table is null + HasFocus=true - keystroke navigation reached via OnKeyDownNotHandled
        // should not throw InvalidOperationException from GetCollectionLength
        Exception? ex = Record.Exception (() => tableView.NewKeyDownEvent (Key.A));

        Assert.Null (ex);
    }

    // Copilot - regression: TableCollectionNavigator must not throw when a cell value is null (custom ITableSource)
    [Fact]
    public void TableCollectionNavigator_NullCellValue_DoesNotThrow ()
    {
        // Use a custom ITableSource that can return null for cell values
        // (DataTable wraps null as DBNull.Value, so we need a custom source to test actual null)
        TableView tableView = new () { Table = new NullCellTableSource () };
        tableView.HasFocus = true;

        // Pressing 'a' triggers keystroke navigation; row 0 has null cell, row 1 has "apple"
        // Should not throw InvalidOperationException from ElementAt
        Exception? ex = Record.Exception (() => tableView.NewKeyDownEvent (Key.A));

        Assert.Null (ex);

        // Should land on "apple" (row 1), skipping the null-cell row gracefully
        Assert.Equal (1, tableView.Value!.Cursor.Y);

        tableView.Dispose ();
    }

    // Copilot - regression: TableCollectionNavigator returns string.Empty for DBNull cells
    [Fact]
    public void TableCollectionNavigator_DBNullCellValue_DoesNotThrow ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add (DBNull.Value); // DataTable stores this as DBNull.Value
        dt.Rows.Add ("banana");
        dt.Rows.Add ("berry");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.HasFocus = true;

        Exception? ex = Record.Exception (() => tableView.NewKeyDownEvent (Key.B));

        Assert.Null (ex);
        Assert.Equal (1, tableView.Value!.Cursor.Y);

        tableView.Dispose ();
    }

    /// <summary>A minimal <see cref="ITableSource"/> that returns <see langword="null"/> for the first cell.</summary>
    private sealed class NullCellTableSource : ITableSource
    {
        // Row 0 intentionally holds null to exercise null-cell handling in TableCollectionNavigator
        private readonly object? [] _data = [null, "apple", "apricot"];

        public object this [int row, int col]
        {
#pragma warning disable CS8603 // Possible null reference return - intentional for testing null-cell handling
            get => _data [row];
#pragma warning restore CS8603
        }

        public int Rows => _data.Length;

        public int Columns => 1;

        public string [] ColumnNames => ["Col1"];
    }

    // Copilot - regression: ColumnOffset setter must not throw when all columns are hidden (0 visible columns)
    [Fact]
    public void ColumnOffset_AllColumnsHidden_DoesNotThrow ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("a");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.BeginInit ();
        tableView.EndInit ();

        // Hide the only column — this makes the cache empty (0 visible columns)
        tableView.Style.GetOrCreateColumnStyle (0).Visible = false;
        tableView.Update ();

        // Setting ColumnOffset=0 with an empty render cache previously computed value=-1
        // and then indexed _columnsToRenderCache![-1], causing IndexOutOfRangeException
        Exception? ex = Record.Exception (() => tableView.ColumnOffset = 0);

        Assert.Null (ex);
        Assert.Equal (0, tableView.ColumnOffset);

        tableView.Dispose ();
    }

    // Copilot - regression: ColumnOffset setter must not throw when table is null
    [Fact]
    public void ColumnOffset_NullTable_DoesNotThrow ()
    {
        TableView tableView = new ();
        tableView.BeginInit ();
        tableView.EndInit ();

        Exception? ex = Record.Exception (() => tableView.ColumnOffset = 0);

        Assert.Null (ex);
        Assert.Equal (0, tableView.ColumnOffset);

        tableView.Dispose ();
    }

    [Fact]
    public void Test_SumColumnWidth_GraphemeClusters ()
    {
        var family = "\U0001F468\u200D\U0001F469\u200D\U0001F466\u200D\U0001F466"; // 👨‍👩‍👦‍👦
        Assert.Equal (8, family.EnumerateRunes ().Sum (c => c.GetColumns ()));
        Assert.Equal (2, family.GetColumns ());

        var technologist = "\U0001F469\u200D\U0001F4BB"; // 👩‍💻
        Assert.Equal (4, technologist.EnumerateRunes ().Sum (c => c.GetColumns ()));
        Assert.Equal (2, technologist.GetColumns ());
    }

    // Copilot
    [Fact]
    public void TruncateOrPad_SurrogatePairs_DoesNotThrowOrCorrupt ()
    {
        // TruncateOrPad iterates `char` values and casts each to `Rune`.
        // Surrogate pairs (emoji, CJK supplementary) are two `char`s in UTF-16.
        // Casting an isolated high/low surrogate to Rune throws ArgumentOutOfRangeException.
        const string CELL_VALUE = "\U0001F389Hello"; // 🎉Hello — emoji is a surrogate pair

        // Sanity checks
        Assert.True (char.IsHighSurrogate (CELL_VALUE [0]));
        Assert.True (char.IsLowSurrogate (CELL_VALUE [1]));
        Assert.Equal (7, CELL_VALUE.GetColumns ()); // emoji=2 + Hello=5

        // Call private static TruncateOrPad via reflection with availableHorizontalSpace < string width
        MethodInfo? method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull (method);

        // availableHorizontalSpace=4 forces the truncation branch (7 >= 4)
        Exception? ex = Record.Exception (() => method.Invoke (null, [CELL_VALUE, CELL_VALUE, 4, null]));

        // Bug: this throws TargetInvocationException wrapping ArgumentOutOfRangeException
        // because (Rune)highSurrogate is invalid
        Assert.Null (ex);

        var result = (string)method.Invoke (null, [CELL_VALUE, CELL_VALUE, 4, null])!;

        // Result must not contain isolated surrogates (paired surrogates in emoji are fine)
        for (var i = 0; i < result.Length; i++)
        {
            if (char.IsHighSurrogate (result [i]))
            {
                Assert.True (i + 1 < result.Length && char.IsLowSurrogate (result [i + 1]), $"Isolated high surrogate at index {i}");
                i++; // skip the low surrogate
            }
            else
            {
                Assert.False (char.IsLowSurrogate (result [i]), $"Isolated low surrogate 0x{(int)result [i]:X4} at index {i}");
            }
        }

        // Result width should not exceed available space
        Assert.True (result.GetColumns () <= 4, $"Truncated result '{result}' exceeds available space");
    }

    // Claude - Opus 4.5 — regression for issue #5075
    // When ShowVerticalCellLines is false AND a custom ColorGetter is used, the position between cells
    // (the separator column, current.X - 1) was being overdrawn with a space using the row's normal
    // scheme. This punched a 1-cell hole in the cell's custom color. The fix skips the separator draw
    // when the symbol would be the default space and lines are off — the cell padding has already
    // filled that position with the cell's color.
    [Fact]
    public void ShowVerticalCellLines_False_WithCustomColorGetter_PreservesCellColorAtSeparator ()
    {
        IDriver driver = CreateTestDriver (40, 5);

        TableView tableView = new () { Driver = driver };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        tableView.Viewport = new Rectangle (0, 0, 40, 5);

        tableView.Style.ShowHeaders = true;
        tableView.Style.ShowHorizontalHeaderUnderline = false;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.ShowVerticalCellLines = false;
        tableView.Style.ShowVerticalHeaderLines = false;
        tableView.Style.ExpandLastColumn = false;
        tableView.FullRowSelect = false;

        // A custom scheme that is visibly distinct from the row scheme.
        Scheme customScheme = new ()
        {
            Normal = new Attribute (Color.White, Color.Red),
            Focus = new Attribute (Color.Red, Color.White),
            HotNormal = new Attribute (Color.White, Color.Red),
            HotFocus = new Attribute (Color.Red, Color.White),
            Disabled = new Attribute (Color.White, Color.Red),
            Active = new Attribute (Color.White, Color.Red)
        };

        tableView.Style.GetOrCreateColumnStyle (0).ColorGetter = _ => customScheme;
        tableView.Style.GetOrCreateColumnStyle (1).ColorGetter = _ => customScheme;

        DataTable dt = new ();
        dt.Columns.Add ("A");
        dt.Columns.Add ("B");
        dt.Rows.Add ("aa", "bb");
        tableView.Table = new DataTableSource (dt);

        tableView.Layout ();
        tableView.SetClipToScreen ();
        tableView.Draw ();

        // Find the row that contains the data ("aa" then "bb").
        Cell [,] contents = driver.Contents!;
        var dataRow = -1;

        for (var r = 0; r < 5; r++)
        {
            var rowText = string.Empty;

            for (var c = 0; c < 10; c++)
            {
                rowText += contents [r, c].Grapheme;
            }

            if (rowText.Contains ("aa") && rowText.Contains ("bb"))
            {
                dataRow = r;

                break;
            }
        }

        Assert.True (dataRow >= 0, "Expected a rendered data row containing 'aa' and 'bb'");

        // Locate the columns for "aa" and "bb".
        var aaCol = -1;
        var bbCol = -1;

        for (var c = 0; c < 39; c++)
        {
            if (aaCol < 0 && contents [dataRow, c].Grapheme == "a" && contents [dataRow, c + 1].Grapheme == "a")
            {
                aaCol = c;
            }

            if (bbCol < 0 && contents [dataRow, c].Grapheme == "b" && contents [dataRow, c + 1].Grapheme == "b")
            {
                bbCol = c;
            }
        }

        Assert.True (aaCol >= 0 && bbCol > aaCol + 1, $"Expected 'aa' before 'bb'. aaCol={aaCol}, bbCol={bbCol}");

        // The cells "aa" and "bb" must use the custom red background.
        Assert.Equal (customScheme.Normal, contents [dataRow, aaCol].Attribute);
        Assert.Equal (customScheme.Normal, contents [dataRow, bbCol].Attribute);

        // The separator position is the gap between the end of "aa" and the start of "bb".
        // Before the fix, this position was overdrawn with the row scheme attribute.
        // After the fix, the cell padding's custom red attribute remains.
        for (int c = aaCol + 2; c < bbCol; c++)
        {
            Assert.Equal (customScheme.Normal, contents [dataRow, c].Attribute);
        }

        tableView.Dispose ();
    }

    [Fact]
    public void Test_CalculateMaxCellWidth_UsesGraphemeWidth ()
    {
        // setup
        IDriver driver = CreateTestDriver ();
        var family = "\U0001F468\u200D\U0001F469\u200D\U0001F466\u200D\U0001F466"; // 👨‍👩‍👦‍👦

        var tableView = new TableView { Driver = driver };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        tableView.Viewport = new Rectangle (0, 0, 25, 5);
        tableView.Style.ShowHorizontalHeaderUnderline = true;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;

        var dt = new DataTable ();
        dt.Columns.Add ("A");
        dt.Columns.Add ("B");
        dt.Rows.Add (family, "ok");
        tableView.Table = new DataTableSource (dt);

        // execute
        tableView.Layout ();
        tableView.SetClipToScreen ();
        tableView.Draw ();

        // verify
        var actual = driver.ToString ();
        string [] lines = actual.Replace ("\r\n", "\n").Split ('\n');
        string headerRow = lines.First (l => l.Contains ('A') && l.Contains ('B'));
        int separatorIndex = headerRow.IndexOf ('│', 1);
        int separatorColumn = headerRow [..separatorIndex].GetColumns ();

        Assert.True (separatorColumn <= 5,
                     $"Column A should be narrow (grapheme width 2), but separator at column {
                         separatorColumn
                     } suggests over-sized column. Header: '{
                         headerRow
                     }'");
    }
}
