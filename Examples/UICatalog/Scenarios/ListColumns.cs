#nullable enable

using System.Collections;
using System.Data;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ListColumns", "Implements a columned list via a data table.")]
[ScenarioCategory ("TableView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Scrolling")]
public class ListColumns : Scenario
{
    private IApplication? _app;
    private Scheme? _alternatingScheme;
    private DataTable? _currentTable;
    private TableView? _listColView;
    private CheckBox? _alternatingColorsCheckBox;
    private CheckBox? _alwaysUseNormalColorForVerticalCellLinesCheckBox;
    private CheckBox? _bottomLineCheckBox;
    private CheckBox? _cellLinesCheckBox;
    private CheckBox? _cursorCheckBox;
    private CheckBox? _expandLastColumnCheckBox;
    private CheckBox? _orientVerticalCheckBox;
    private CheckBox? _scrollParallelCheckBox;
    private CheckBox? _smoothScrollingCheckBox;
    private CheckBox? _topLineCheckBox;

    /// <summary>
    ///     Builds a simple list in which values are the index. This helps test that scrolling etc. is working
    ///     correctly and not skipping out values when paging
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public static IList BuildSimpleList (int items)
    {
        List<object> list = [];

        for (var i = 0; i < items; i++)
        {
            list.Add ("Item " + i);
        }

        return list;
    }

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        // MenuBar
        MenuBar menuBar = new ();

        _listColView = new ()
        {
            Y = Pos.Bottom (menuBar),
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            Style = new ()
            {
                ShowHeaders = false,
                ShowHorizontalHeaderOverline = false,
                ShowHorizontalHeaderUnderline = false,
                ShowHorizontalBottomline = false,
                ExpandLastColumn = false
            }
        };
        ListColumnStyle listColStyle = new ();


        // Status Bar
        StatusBar statusBar = new (
                                   [
                                       new (Key.F2, "OpenBigListEx", () => OpenSimpleList (true)),
                                       new (Key.F3, "CloseExample", CloseExample),
                                       new (Key.F4, "OpenSmListEx", () => OpenSimpleList (false)),
                                       new (Application.QuitKey, "Quit", Quit)
                                   ]
                                  );

        // Selected cell label
        Label selectedCellLabel = new ()
        {
            X = 0,
            Y = Pos.Bottom (_listColView),
            Text = "0,0",
            Width = Dim.Fill (),
            TextAlignment = Alignment.End
        };

        _listColView.SelectedCellChanged += (s, e) =>
                                            {
                                                if (_listColView is not null)
                                                {
                                                    selectedCellLabel.Text = $"{_listColView.SelectedRow},{_listColView.SelectedColumn}";
                                                }
                                            };
        _listColView.KeyDown += TableViewKeyPress;

        _alternatingScheme = new ()
        {
            Disabled = appWindow.GetAttributeForRole (VisualRole.Disabled),
            HotFocus = appWindow.GetAttributeForRole (VisualRole.HotFocus),
            Focus = appWindow.GetAttributeForRole (VisualRole.Focus),
            Normal = new (Color.White, Color.BrightBlue)
        };

        _listColView.KeyBindings.ReplaceCommands (Key.Space, Command.Accept);

        // Setup menu checkboxes
        _topLineCheckBox = new ()
        {
            Title = "_TopLine",
            CheckedState = _listColView.Style.ShowHorizontalHeaderOverline ? CheckState.Checked : CheckState.UnChecked
        };
        _topLineCheckBox.CheckedStateChanged += (s, e) => ToggleTopline ();

        _bottomLineCheckBox = new ()
        {
            Title = "_BottomLine",
            CheckedState = _listColView.Style.ShowHorizontalBottomline ? CheckState.Checked : CheckState.UnChecked
        };
        _bottomLineCheckBox.CheckedStateChanged += (s, e) => ToggleBottomline ();

        _cellLinesCheckBox = new ()
        {
            Title = "_CellLines",
            CheckedState = _listColView.Style.ShowVerticalCellLines ? CheckState.Checked : CheckState.UnChecked
        };
        _cellLinesCheckBox.CheckedStateChanged += (s, e) => ToggleCellLines ();

        _expandLastColumnCheckBox = new ()
        {
            Title = "_ExpandLastColumn",
            CheckedState = _listColView.Style.ExpandLastColumn ? CheckState.Checked : CheckState.UnChecked
        };
        _expandLastColumnCheckBox.CheckedStateChanged += (s, e) => ToggleExpandLastColumn ();

        _alwaysUseNormalColorForVerticalCellLinesCheckBox = new ()
        {
            Title = "_AlwaysUseNormalColorForVerticalCellLines",
            CheckedState = _listColView.Style.AlwaysUseNormalColorForVerticalCellLines ? CheckState.Checked : CheckState.UnChecked
        };
        _alwaysUseNormalColorForVerticalCellLinesCheckBox.CheckedStateChanged += (s, e) => ToggleAlwaysUseNormalColorForVerticalCellLines ();

        _smoothScrollingCheckBox = new ()
        {
            Title = "_SmoothHorizontalScrolling",
            CheckedState = _listColView.Style.SmoothHorizontalScrolling ? CheckState.Checked : CheckState.UnChecked
        };
        _smoothScrollingCheckBox.CheckedStateChanged += (s, e) => ToggleSmoothScrolling ();

        _alternatingColorsCheckBox = new ()
        {
            Title = "Alternating Colors"
        };
        _alternatingColorsCheckBox.CheckedStateChanged += (s, e) => ToggleAlternatingColors ();

        _cursorCheckBox = new ()
        {
            Title = "Invert Selected Cell First Character",
            CheckedState = _listColView.Style.InvertSelectedCellFirstCharacter ? CheckState.Checked : CheckState.UnChecked
        };
        _cursorCheckBox.CheckedStateChanged += (s, e) => ToggleInvertSelectedCellFirstCharacter ();

        _orientVerticalCheckBox = new ()
        {
            Title = "_OrientVertical",
            CheckedState = listColStyle.Orientation == Orientation.Vertical ? CheckState.Checked : CheckState.UnChecked
        };
        _orientVerticalCheckBox.CheckedStateChanged += (s, e) => ToggleVerticalOrientation ();

        _scrollParallelCheckBox = new ()
        {
            Title = "_ScrollParallel",
            CheckedState = listColStyle.ScrollParallel ? CheckState.Checked : CheckState.UnChecked
        };
        _scrollParallelCheckBox.CheckedStateChanged += (s, e) => ToggleScrollParallel ();

        menuBar.Add (
                  new MenuBarItem (
                                   Strings.menuFile,
                                   [
                                       new MenuItem
                                       {
                                           Title = "Open_BigListExample",
                                           Action = () => OpenSimpleList (true)
                                       },
                                       new MenuItem
                                       {
                                           Title = "Open_SmListExample",
                                           Action = () => OpenSimpleList (false)
                                       },
                                       new MenuItem
                                       {
                                           Title = "_CloseExample",
                                           Action = CloseExample
                                       },
                                       new MenuItem
                                       {
                                           Title = Strings.cmdQuit,
                                           Action = Quit
                                       }
                                   ]
                                  )
                 );

        menuBar.Add (
                  new MenuBarItem (
                                   "_View",
                                   [
                                       new MenuItem
                                       {
                                           CommandView = _topLineCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _bottomLineCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _cellLinesCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _expandLastColumnCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _alwaysUseNormalColorForVerticalCellLinesCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _smoothScrollingCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _alternatingColorsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _cursorCheckBox
                                       }
                                   ]
                                  )
                 );

        menuBar.Add (
                  new MenuBarItem (
                                   "_List",
                                   [
                                       new MenuItem
                                       {
                                           CommandView = _orientVerticalCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _scrollParallelCheckBox
                                       },
                                       new MenuItem
                                       {
                                           Title = "Set _Max Cell Width",
                                           Action = SetListMaxWidth
                                       },
                                       new MenuItem
                                       {
                                           Title = "Set Mi_n Cell Width",
                                           Action = SetListMinWidth
                                       }
                                   ]
                                  )
                 );

        // Add views in order of visual appearance
        appWindow.Add (menuBar, _listColView, selectedCellLabel, statusBar);

        app.Run (appWindow);
    }

    private void CloseExample ()
    {
        if (_listColView is not null)
        {
            _listColView.Table = null;
        }
    }

    private void OpenSimpleList (bool big) { SetTable (BuildSimpleList (big ? 1023 : 31)); }

    private void Quit () { _listColView?.App?.RequestStop (); }

    private void RunListWidthDialog (string prompt, Action<TableView, int> setter, Func<TableView, int> getter)
    {
        if (_listColView is null)
        {
            return;
        }

        var accepted = false;
        Dialog d = new Dialog
        {
            Title = prompt,
            Buttons = [new () { Title = Strings.btnCancel }, new () { Title = Strings.btnOk }]
        };

        TextField tf = new () { Text = getter (_listColView).ToString (), X = 0, Y = 0, Width = Dim.Fill (0, minimumContentDim: 50) };

        d.Add (tf);
        tf.SetFocus ();

        _app?.Run (d);
        accepted = d.Result == 1;
        d.Dispose ();

        if (accepted)
        {
            try
            {
                setter (_listColView, int.Parse (tf.Text));
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery (_app!, "Failed to set", ex.Message, "Ok");
            }
        }
    }

    private void SetListMaxWidth ()
    {
        RunListWidthDialog ("MaxCellWidth", (s, v) => s.MaxCellWidth = v, s => s.MaxCellWidth);
        _listColView?.SetNeedsDraw ();
    }

    private void SetListMinWidth ()
    {
        RunListWidthDialog ("MinCellWidth", (s, v) => s.MinCellWidth = v, s => s.MinCellWidth);
        _listColView?.SetNeedsDraw ();
    }

    private void SetTable (IList list)
    {
        if (_listColView is null)
        {
            return;
        }

        _listColView.Table = new ListTableSource (list, _listColView);

        if (_listColView.Table is ListTableSource listTableSource)
        {
            _currentTable = listTableSource.DataTable;
        }
    }

    private void TableViewKeyPress (object? sender, Key e)
    {
        if (_currentTable is null || _listColView is null)
        {
            return;
        }

        if (e.KeyCode == Key.Delete)
        {
            // set all selected cells to null
            foreach (Point pt in _listColView.GetAllSelectedCells ())
            {
                _currentTable.Rows [pt.Y] [pt.X] = DBNull.Value;
            }

            _listColView.Update ();
            e.Handled = true;
        }
    }

    private void ToggleAlternatingColors ()
    {
        if (_listColView is null || _alternatingColorsCheckBox is null)
        {
            return;
        }

        if (_alternatingColorsCheckBox.CheckedState == CheckState.Checked)
        {
            _listColView.Style.RowColorGetter = a => a.RowIndex % 2 == 0 ? _alternatingScheme : null;
        }
        else
        {
            _listColView.Style.RowColorGetter = null;
        }

        _listColView.SetNeedsDraw ();
    }

    private void ToggleAlwaysUseNormalColorForVerticalCellLines ()
    {
        if (_listColView is null || _alwaysUseNormalColorForVerticalCellLinesCheckBox is null)
        {
            return;
        }

        _listColView.Style.AlwaysUseNormalColorForVerticalCellLines =
            _alwaysUseNormalColorForVerticalCellLinesCheckBox.CheckedState == CheckState.Checked;

        _listColView.Update ();
    }

    private void ToggleBottomline ()
    {
        if (_listColView is null || _bottomLineCheckBox is null)
        {
            return;
        }

        _listColView.Style.ShowHorizontalBottomline = _bottomLineCheckBox.CheckedState == CheckState.Checked;
        _listColView.Update ();
    }

    private void ToggleCellLines ()
    {
        if (_listColView is null || _cellLinesCheckBox is null)
        {
            return;
        }

        _listColView.Style.ShowVerticalCellLines = _cellLinesCheckBox.CheckedState == CheckState.Checked;
        _listColView.Update ();
    }

    private void ToggleExpandLastColumn ()
    {
        if (_listColView is null || _expandLastColumnCheckBox is null)
        {
            return;
        }

        _listColView.Style.ExpandLastColumn = _expandLastColumnCheckBox.CheckedState == CheckState.Checked;

        _listColView.Update ();
    }

    private void ToggleInvertSelectedCellFirstCharacter ()
    {
        if (_listColView is null || _cursorCheckBox is null)
        {
            return;
        }

        _listColView.Style.InvertSelectedCellFirstCharacter = _cursorCheckBox.CheckedState == CheckState.Checked;
        _listColView.SetNeedsDraw ();
    }

    private void ToggleScrollParallel ()
    {
        if (_listColView?.Table is not ListTableSource listTableSource || _scrollParallelCheckBox is null)
        {
            return;
        }

        listTableSource.Style.ScrollParallel = _scrollParallelCheckBox.CheckedState == CheckState.Checked;
        _listColView.SetNeedsDraw ();
    }

    private void ToggleSmoothScrolling ()
    {
        if (_listColView is null || _smoothScrollingCheckBox is null)
        {
            return;
        }

        _listColView.Style.SmoothHorizontalScrolling = _smoothScrollingCheckBox.CheckedState == CheckState.Checked;

        _listColView.Update ();
    }

    private void ToggleTopline ()
    {
        if (_listColView is null || _topLineCheckBox is null)
        {
            return;
        }

        _listColView.Style.ShowHorizontalHeaderOverline = _topLineCheckBox.CheckedState == CheckState.Checked;
        _listColView.Update ();
    }

    private void ToggleVerticalOrientation ()
    {
        if (_listColView?.Table is not ListTableSource listTableSource || _orientVerticalCheckBox is null)
        {
            return;
        }

        listTableSource.Style.Orientation = _orientVerticalCheckBox.CheckedState == CheckState.Checked
                                                ? Orientation.Vertical
                                                : Orientation.Horizontal;
        _listColView.SetNeedsDraw ();
    }
}
