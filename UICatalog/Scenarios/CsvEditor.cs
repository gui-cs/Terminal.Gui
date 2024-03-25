using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Csv Editor", "Open and edit simple CSV files using the TableView class.")]
[ScenarioCategory ("TableView")]
[ScenarioCategory ("TextView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Top Level Windows")]
[ScenarioCategory ("Files and IO")]
public class CsvEditor : Scenario
{
    private string _currentFile;
    private DataTable _currentTable;
    private MenuItem _miCentered;
    private MenuItem _miLeft;
    private MenuItem _miRight;
    private TextField _selectedCellLabel;
    private TableView _tableView;

    public override void Setup ()
    {
        Win.Title = GetName ();
        Win.Y = 1; // menu
        Win.Height = Dim.Fill (1); // status bar

        _tableView = new TableView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill (1) };

        var fileMenu = new MenuBarItem (
                                        "_File",
                                        new MenuItem []
                                        {
                                            new ("_Open CSV", "", () => Open ()),
                                            new ("_Save", "", () => Save ()),
                                            new ("_Quit", "Quits The App", () => Quit ())
                                        }
                                       );

        //fileMenu.Help = "Help";
        var menu = new MenuBar
        {
            Menus =
            [
                fileMenu,
                new MenuBarItem (
                                 "_Edit",
                                 new MenuItem []
                                 {
                                     new ("_New Column", "", () => AddColumn ()),
                                     new ("_New Row", "", () => AddRow ()),
                                     new (
                                          "_Rename Column",
                                          "",
                                          () => RenameColumn ()
                                         ),
                                     new ("_Delete Column", "", () => DeleteColum ()),
                                     new ("_Move Column", "", () => MoveColumn ()),
                                     new ("_Move Row", "", () => MoveRow ()),
                                     new ("_Sort Asc", "", () => Sort (true)),
                                     new ("_Sort Desc", "", () => Sort (false))
                                 }
                                ),
                new MenuBarItem (
                                 "_View",
                                 new []
                                 {
                                     _miLeft = new MenuItem (
                                                             "_Align Left",
                                                             "",
                                                             () => Align (TextAlignment.Left)
                                                            ),
                                     _miRight = new MenuItem (
                                                              "_Align Right",
                                                              "",
                                                              () => Align (TextAlignment.Right)
                                                             ),
                                     _miCentered = new MenuItem (
                                                                 "_Align Centered",
                                                                 "",
                                                                 () => Align (TextAlignment.Centered)
                                                                ),

                                     // Format requires hard typed data table, when we read a CSV everything is untyped (string) so this only works for new columns in this demo
                                     _miCentered = new MenuItem (
                                                                 "_Set Format Pattern",
                                                                 "",
                                                                 () => SetFormat ()
                                                                )
                                 }
                                )
            ]
        };
        Top.Add (menu);

        var statusBar = new StatusBar (
                                       new StatusItem []
                                       {
                                           new (
                                                KeyCode.CtrlMask | KeyCode.O,
                                                "~^O~ Open",
                                                () => Open ()
                                               ),
                                           new (
                                                KeyCode.CtrlMask | KeyCode.S,
                                                "~^S~ Save",
                                                () => Save ()
                                               ),
                                           new (
                                                Application.QuitKey,
                                                $"{Application.QuitKey} to Quit",
                                                () => Quit ()
                                               )
                                       }
                                      );
        Top.Add (statusBar);

        Win.Add (_tableView);

        _selectedCellLabel = new TextField
        {
            X = 0,
            Y = Pos.Bottom (_tableView),
            Text = "0,0",
            Width = Dim.Fill (),
            TextAlignment = TextAlignment.Right
        };
        _selectedCellLabel.TextChanged += SelectedCellLabel_TextChanged;

        Win.Add (_selectedCellLabel);

        _tableView.SelectedCellChanged += OnSelectedCellChanged;
        _tableView.CellActivated += EditCurrentCell;
        _tableView.KeyDown += TableViewKeyPress;

        SetupScrollBar ();
    }

    private void AddColumn ()
    {
        if (NoTableLoaded ())
        {
            return;
        }

        if (GetText ("Enter column name", "Name:", "", out string colName))
        {
            var col = new DataColumn (colName);

            int newColIdx = Math.Min (
                                      Math.Max (0, _tableView.SelectedColumn + 1),
                                      _tableView.Table.Columns
                                     );

            int result = MessageBox.Query (
                                           "Column Type",
                                           "Pick a data type for the column",
                                           "Date",
                                           "Integer",
                                           "Double",
                                           "Text",
                                           "Cancel"
                                          );

            if (result <= -1 || result >= 4)
            {
                return;
            }

            switch (result)
            {
                case 0:
                    col.DataType = typeof (DateTime);

                    break;
                case 1:
                    col.DataType = typeof (int);

                    break;
                case 2:
                    col.DataType = typeof (double);

                    break;
                case 3:
                    col.DataType = typeof (string);

                    break;
            }

            _currentTable.Columns.Add (col);
            col.SetOrdinal (newColIdx);
            _tableView.Update ();
        }
    }

    private void AddRow ()
    {
        if (NoTableLoaded ())
        {
            return;
        }

        DataRow newRow = _currentTable.NewRow ();

        int newRowIdx = Math.Min (Math.Max (0, _tableView.SelectedRow + 1), _tableView.Table.Rows);

        _currentTable.Rows.InsertAt (newRow, newRowIdx);
        _tableView.Update ();
    }

    private void Align (TextAlignment newAlignment)
    {
        if (NoTableLoaded ())
        {
            return;
        }

        ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (_tableView.SelectedColumn);
        style.Alignment = newAlignment;

        _miLeft.Checked = style.Alignment == TextAlignment.Left;
        _miRight.Checked = style.Alignment == TextAlignment.Right;
        _miCentered.Checked = style.Alignment == TextAlignment.Centered;

        _tableView.Update ();
    }

    private void ClearColumnStyles ()
    {
        _tableView.Style.ColumnStyles.Clear ();
        _tableView.Update ();
    }

    private void CloseExample () { _tableView.Table = null; }

    private void DeleteColum ()
    {
        if (NoTableLoaded ())
        {
            return;
        }

        if (_tableView.SelectedColumn == -1)
        {
            MessageBox.ErrorQuery ("No Column", "No column selected", "Ok");

            return;
        }

        try
        {
            _currentTable.Columns.RemoveAt (_tableView.SelectedColumn);
            _tableView.Update ();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery ("Could not remove column", ex.Message, "Ok");
        }
    }

    private void EditCurrentCell (object sender, CellActivatedEventArgs e)
    {
        if (e.Table == null)
        {
            return;
        }

        var oldValue = _currentTable.Rows [e.Row] [e.Col].ToString ();

        if (GetText ("Enter new value", _currentTable.Columns [e.Col].ColumnName, oldValue, out string newText))
        {
            try
            {
                _currentTable.Rows [e.Row] [e.Col] =
                    string.IsNullOrWhiteSpace (newText) ? DBNull.Value : newText;
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery (60, 20, "Failed to set text", ex.Message, "Ok");
            }

            _tableView.Update ();
        }
    }

    private bool GetText (string title, string label, string initialText, out string enteredText)
    {
        var okPressed = false;

        var ok = new Button { Text = "Ok", IsDefault = true };

        ok.Accept += (s, e) =>
                      {
                          okPressed = true;
                          Application.RequestStop ();
                      };
        var cancel = new Button { Text = "Cancel" };
        cancel.Accept += (s, e) => { Application.RequestStop (); };
        var d = new Dialog { Title = title, Buttons = [ok, cancel] };

        var lbl = new Label { X = 0, Y = 1, Text = label };

        var tf = new TextField { Text = initialText, X = 0, Y = 2, Width = Dim.Fill () };

        d.Add (lbl, tf);
        tf.SetFocus ();

        Application.Run (d);
        d.Dispose ();

        enteredText = okPressed ? tf.Text : null;

        return okPressed;
    }

    private void MoveColumn ()
    {
        if (NoTableLoaded ())
        {
            return;
        }

        if (_tableView.SelectedColumn == -1)
        {
            MessageBox.ErrorQuery ("No Column", "No column selected", "Ok");

            return;
        }

        try
        {
            DataColumn currentCol = _currentTable.Columns [_tableView.SelectedColumn];

            if (GetText ("Move Column", "New Index:", currentCol.Ordinal.ToString (), out string newOrdinal))
            {
                int newIdx = Math.Min (
                                       Math.Max (0, int.Parse (newOrdinal)),
                                       _tableView.Table.Columns - 1
                                      );

                currentCol.SetOrdinal (newIdx);

                _tableView.SetSelection (newIdx, _tableView.SelectedRow, false);
                _tableView.EnsureSelectedCellIsVisible ();
                _tableView.SetNeedsDisplay ();
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery ("Error moving column", ex.Message, "Ok");
        }
    }

    private void MoveRow ()
    {
        if (NoTableLoaded ())
        {
            return;
        }

        if (_tableView.SelectedRow == -1)
        {
            MessageBox.ErrorQuery ("No Rows", "No row selected", "Ok");

            return;
        }

        try
        {
            int oldIdx = _tableView.SelectedRow;

            DataRow currentRow = _currentTable.Rows [oldIdx];

            if (GetText ("Move Row", "New Row:", oldIdx.ToString (), out string newOrdinal))
            {
                int newIdx = Math.Min (Math.Max (0, int.Parse (newOrdinal)), _tableView.Table.Rows - 1);

                if (newIdx == oldIdx)
                {
                    return;
                }

                object [] arrayItems = currentRow.ItemArray;
                _currentTable.Rows.Remove (currentRow);

                // Removing and Inserting the same DataRow seems to result in it loosing its values so we have to create a new instance
                DataRow newRow = _currentTable.NewRow ();
                newRow.ItemArray = arrayItems;

                _currentTable.Rows.InsertAt (newRow, newIdx);

                _tableView.SetSelection (_tableView.SelectedColumn, newIdx, false);
                _tableView.EnsureSelectedCellIsVisible ();
                _tableView.SetNeedsDisplay ();
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery ("Error moving column", ex.Message, "Ok");
        }
    }

    private bool NoTableLoaded ()
    {
        if (_tableView.Table == null)
        {
            MessageBox.ErrorQuery ("No Table Loaded", "No table has currently be opened", "Ok");

            return true;
        }

        return false;
    }

    private void OnSelectedCellChanged (object sender, SelectedCellChangedEventArgs e)
    {
        // only update the text box if the user is not manually editing it
        if (!_selectedCellLabel.HasFocus)
        {
            _selectedCellLabel.Text = $"{_tableView.SelectedRow},{_tableView.SelectedColumn}";
        }

        if (_tableView.Table == null || _tableView.SelectedColumn == -1)
        {
            return;
        }

        ColumnStyle style = _tableView.Style.GetColumnStyleIfAny (_tableView.SelectedColumn);

        _miLeft.Checked = style?.Alignment == TextAlignment.Left;
        _miRight.Checked = style?.Alignment == TextAlignment.Right;
        _miCentered.Checked = style?.Alignment == TextAlignment.Centered;
    }

    private void Open ()
    {
        var ofd = new FileDialog
        {
            AllowedTypes = new List<IAllowedType> { new AllowedType ("Comma Separated Values", ".csv") }
        };
        ofd.Style.OkButtonText = "Open";

        Application.Run (ofd);

        if (!ofd.Canceled && !string.IsNullOrWhiteSpace (ofd.Path))
        {
            Open (ofd.Path);
        }
        ofd.Dispose ();
    }

    private void Open (string filename)
    {
        var lineNumber = 0;
        _currentFile = null;

        try
        {
            using var reader = new CsvReader (File.OpenText (filename), CultureInfo.InvariantCulture);

            var dt = new DataTable ();

            reader.Read ();

            if (reader.ReadHeader ())
            {
                foreach (string h in reader.HeaderRecord)
                {
                    dt.Columns.Add (h);
                }
            }

            while (reader.Read ())
            {
                lineNumber++;

                DataRow newRow = dt.Rows.Add ();

                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    newRow [i] = reader [i];
                }
            }

            SetTable (dt);

            // Only set the current filename if we successfully loaded the entire file
            _currentFile = filename;
            Win.Title = $"{GetName ()} - {Path.GetFileName (_currentFile)}";
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (
                                   "Open Failed",
                                   $"Error on line {lineNumber}{Environment.NewLine}{ex.Message}",
                                   "Ok"
                                  );
        }
    }

    private void Quit () { Application.RequestStop (); }

    private void RenameColumn ()
    {
        if (NoTableLoaded ())
        {
            return;
        }

        DataColumn currentCol = _currentTable.Columns [_tableView.SelectedColumn];

        if (GetText ("Rename Column", "Name:", currentCol.ColumnName, out string newName))
        {
            currentCol.ColumnName = newName;
            _tableView.Update ();
        }
    }

    private void Save ()
    {
        if (_tableView.Table == null || string.IsNullOrWhiteSpace (_currentFile))
        {
            MessageBox.ErrorQuery ("No file loaded", "No file is currently loaded", "Ok");

            return;
        }

        using var writer = new CsvWriter (
                                          new StreamWriter (File.OpenWrite (_currentFile)),
                                          CultureInfo.InvariantCulture
                                         );

        foreach (string col in _currentTable.Columns.Cast<DataColumn> ().Select (c => c.ColumnName))
        {
            writer.WriteField (col);
        }

        writer.NextRecord ();

        foreach (DataRow row in _currentTable.Rows)
        {
            foreach (object item in row.ItemArray)
            {
                writer.WriteField (item);
            }

            writer.NextRecord ();
        }
    }

    private void SelectedCellLabel_TextChanged (object sender, StateEventArgs<string> e)
    {
        // if user is in the text control and editing the selected cell
        if (!_selectedCellLabel.HasFocus)
        {
            return;
        }

        // change selected cell to the one the user has typed into the box
        Match match = Regex.Match (_selectedCellLabel.Text, "^(\\d+),(\\d+)$");

        if (match.Success)
        {
            _tableView.SelectedColumn = int.Parse (match.Groups [1].Value);
            _tableView.SelectedRow = int.Parse (match.Groups [2].Value);
        }
    }

    private void SetFormat ()
    {
        if (NoTableLoaded ())
        {
            return;
        }

        DataColumn col = _currentTable.Columns [_tableView.SelectedColumn];

        if (col.DataType == typeof (string))
        {
            MessageBox.ErrorQuery (
                                   "Cannot Format Column",
                                   "String columns cannot be Formatted, try adding a new column to the table with a date/numerical Type",
                                   "Ok"
                                  );

            return;
        }

        ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (col.Ordinal);

        if (GetText ("Format", "Pattern:", style.Format ?? "", out string newPattern))
        {
            style.Format = newPattern;
            _tableView.Update ();
        }
    }

    private void SetTable (DataTable dataTable) { _tableView.Table = new DataTableSource (_currentTable = dataTable); }

    private void SetupScrollBar ()
    {
        var scrollBar = new ScrollBarView (_tableView, true);

        scrollBar.ChangedPosition += (s, e) =>
                                     {
                                         _tableView.RowOffset = scrollBar.Position;

                                         if (_tableView.RowOffset != scrollBar.Position)
                                         {
                                             scrollBar.Position = _tableView.RowOffset;
                                         }

                                         _tableView.SetNeedsDisplay ();
                                     };
        /*
        scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
            tableView.LeftItem = scrollBar.OtherScrollBarView.Position;
            if (tableView.LeftItem != scrollBar.OtherScrollBarView.Position) {
                scrollBar.OtherScrollBarView.Position = tableView.LeftItem;
            }
            tableView.SetNeedsDisplay ();
        };*/

        _tableView.DrawContent += (s, e) =>
                                  {
                                      scrollBar.Size = _tableView.Table?.Rows ?? 0;
                                      scrollBar.Position = _tableView.RowOffset;

                                      //scrollBar.OtherScrollBarView.Size = tableView.Maxlength - 1;
                                      //scrollBar.OtherScrollBarView.Position = tableView.LeftItem;
                                      scrollBar.Refresh ();
                                  };
    }

    private void Sort (bool asc)
    {
        if (NoTableLoaded ())
        {
            return;
        }

        if (_tableView.SelectedColumn == -1)
        {
            MessageBox.ErrorQuery ("No Column", "No column selected", "Ok");

            return;
        }

        string colName = _tableView.Table.ColumnNames [_tableView.SelectedColumn];

        _currentTable.DefaultView.Sort = colName + (asc ? " asc" : " desc");
        SetTable (_currentTable.DefaultView.ToTable ());
    }

    private void TableViewKeyPress (object sender, Key e)
    {
        if (e.KeyCode == KeyCode.Delete)
        {
            if (_tableView.FullRowSelect)
            {
                // Delete button deletes all rows when in full row mode
                foreach (int toRemove in _tableView.GetAllSelectedCells ()
                                                   .Select (p => p.Y)
                                                   .Distinct ()
                                                   .OrderByDescending (i => i))
                {
                    _currentTable.Rows.RemoveAt (toRemove);
                }
            }
            else
            {
                // otherwise set all selected cells to null
                foreach (Point pt in _tableView.GetAllSelectedCells ())
                {
                    _currentTable.Rows [pt.Y] [pt.X] = DBNull.Value;
                }
            }

            _tableView.Update ();
            e.Handled = true;
        }
    }
}
