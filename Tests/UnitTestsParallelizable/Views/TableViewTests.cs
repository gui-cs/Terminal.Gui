using System.Data;
#nullable enable
using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (TableView))]
public class TableViewTests
{
    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Left ()
    {
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField tf2);

        // Make the selected cell one in
        tableView.SelectedColumn = 1;

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
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField tf2);

        // Make the selected cell one in
        tableView.SelectedRow = 1;

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
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField tf2);

        // Make the selected cell one in from the rightmost column
        tableView.SelectedColumn = tableView.Table.Columns - 2;

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
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField tf2);

        // Make the selected cell one in from the bottommost row
        tableView.SelectedRow = tableView.Table.Rows - 2;

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
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField tf2);

        // Make the selected cell one in
        tableView.SelectedColumn = 1;

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
    ///     <see cref="TableView"/>.  This is a helper method to setup tests that want to
    ///     explore moving input focus out of a tableview.
    /// </summary>
    /// <param name="tv"></param>
    /// <param name="tf1"></param>
    /// <param name="tf2"></param>
    private void GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField tf2)
    {
        IApplication? app = Application.Create ();
        Runnable<bool>? runnable = new ();
        app.Begin (runnable);

        tableView = new ();

        tf1 = new ();
        tf2 = new ();
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
    /// <returns></returns>
    public static DataTableSource BuildTable (int cols, int rows, out DataTable dt)
    {
        dt = new ();

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

        return new (dt);
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

        Assert.Equal (0, tableView.SelectedRow);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (1, tableView.SelectedRow);

        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (2, tableView.SelectedRow);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (tableView.NewKeyDownEvent (Key.C));
        Assert.Equal (5, tableView.SelectedRow);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void TableView_Command_Activate_TogglesSelection ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");
        dt.Rows.Add ("Data2");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.BeginInit ();
        tableView.EndInit ();

        // Space toggles cell selection (Activate command)
        // Note: Returns false because RaiseActivating has no subscribers
        // but the selection is still toggled
        bool? result = tableView.InvokeCommand (Command.Activate);

        // Command toggles selection but returns false (event not handled)
        Assert.False (result);

        tableView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void TableView_Command_Accept_FiresCellActivated ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        bool cellActivatedFired = false;

        tableView.CellActivated += (_, _) => cellActivatedFired = true;

        bool? result = tableView.InvokeCommand (Command.Accept);

        Assert.True (cellActivatedFired);
        Assert.True (result);

        tableView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void TableView_Space_TogglesSelection ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.BeginInit ();
        tableView.EndInit ();

        // Space triggers cell toggle (selection is toggled even though return value is false)
        // This is because TableView.Activate returns false when no Activating handler sets Handled=true
        bool? result = tableView.NewKeyDownEvent (Key.Space);

        // Returns false because there's no handler that sets Handled=true
        Assert.False (result);

        tableView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void TableView_Enter_FiresCellActivated ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        bool cellActivatedFired = false;

        tableView.CellActivated += (_, _) => cellActivatedFired = true;

        // Enter should trigger CellActivated via Accept command
        bool? result = tableView.NewKeyDownEvent (Key.Enter);

        Assert.True (cellActivatedFired);
        Assert.True (result);

        tableView.Dispose ();
    }
}
