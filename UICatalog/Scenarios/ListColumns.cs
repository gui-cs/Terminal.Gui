using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ListColumns", "Implements a columned list via a data table.")]
[ScenarioCategory ("TableView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Overlapped")]
[ScenarioCategory ("Scrolling")]
public class ListColumns : Scenario
{
    private ColorScheme _alternatingColorScheme;
    private DataTable _currentTable;
    private TableView _listColView;
    private MenuItem _miAlternatingColors;
    private MenuItem _miAlwaysUseNormalColorForVerticalCellLines;
    private MenuItem _miBottomline;
    private MenuItem _miCellLines;
    private MenuItem _miCursor;
    private MenuItem _miExpandLastColumn;
    private MenuItem _miOrientVertical;
    private MenuItem _miScrollParallel;
    private MenuItem _miSmoothScrolling;
    private MenuItem _miTopline;

    /// <summary>
    ///     Builds a simple list in which values are the index.  This helps testing that scrolling etc is working
    ///     correctly and not skipping out values when paging
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public static IList BuildSimpleList (int items)
    {
        List<object> list = new ();

        for (var i = 0; i < items; i++)
        {
            list.Add ("Item " + i);
        }

        return list;
    }

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Toplevel top = new ();
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        _listColView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Style = new ()
            {
                ShowHeaders = false,
                ShowHorizontalHeaderOverline = false,
                ShowHorizontalHeaderUnderline = false,
                ShowHorizontalBottomline = false,
                ExpandLastColumn = false
            }
        };
        var listColStyle = new ListColumnStyle ();

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new (
                              "Open_BigListExample",
                              "",
                              () => OpenSimpleList (true)
                             ),
                         new (
                              "Open_SmListExample",
                              "",
                              () => OpenSimpleList (false)
                             ),
                         new (
                              "_CloseExample",
                              "",
                              () => CloseExample ()
                             ),
                         new ("_Quit", "", () => Quit ())
                     }
                    ),
                new (
                     "_View",
                     new []
                     {
                         _miTopline =
                             new ("_TopLine", "", () => ToggleTopline ())
                             {
                                 Checked = _listColView.Style
                                                       .ShowHorizontalHeaderOverline,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         _miBottomline = new (
                                              "_BottomLine",
                                              "",
                                              () => ToggleBottomline ()
                                             )
                         {
                             Checked = _listColView.Style
                                                   .ShowHorizontalBottomline,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miCellLines = new (
                                             "_CellLines",
                                             "",
                                             () => ToggleCellLines ()
                                            )
                         {
                             Checked = _listColView.Style
                                                   .ShowVerticalCellLines,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miExpandLastColumn = new (
                                                    "_ExpandLastColumn",
                                                    "",
                                                    () => ToggleExpandLastColumn ()
                                                   )
                         {
                             Checked = _listColView.Style.ExpandLastColumn,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miAlwaysUseNormalColorForVerticalCellLines =
                             new (
                                  "_AlwaysUseNormalColorForVerticalCellLines",
                                  "",
                                  () =>
                                      ToggleAlwaysUseNormalColorForVerticalCellLines ()
                                 )
                             {
                                 Checked = _listColView.Style
                                                       .AlwaysUseNormalColorForVerticalCellLines,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         _miSmoothScrolling = new (
                                                   "_SmoothHorizontalScrolling",
                                                   "",
                                                   () => ToggleSmoothScrolling ()
                                                  )
                         {
                             Checked = _listColView.Style
                                                   .SmoothHorizontalScrolling,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miAlternatingColors = new (
                                                     "Alternating Colors",
                                                     "",
                                                     () => ToggleAlternatingColors ()
                                                    ) { CheckType = MenuItemCheckStyle.Checked },
                         _miCursor = new (
                                          "Invert Selected Cell First Character",
                                          "",
                                          () =>
                                              ToggleInvertSelectedCellFirstCharacter ()
                                         )
                         {
                             Checked = _listColView.Style
                                                   .InvertSelectedCellFirstCharacter,
                             CheckType = MenuItemCheckStyle.Checked
                         }
                     }
                    ),
                new (
                     "_List",
                     new []
                     {
                         //new MenuItem ("_Hide Headers", "", HideHeaders),
                         _miOrientVertical = new (
                                                  "_OrientVertical",
                                                  "",
                                                  () => ToggleVerticalOrientation ()
                                                 )
                         {
                             Checked = listColStyle.Orientation
                                       == Orientation.Vertical,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miScrollParallel = new (
                                                  "_ScrollParallel",
                                                  "",
                                                  () => ToggleScrollParallel ()
                                                 )
                         {
                             Checked = listColStyle.ScrollParallel,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         new ("Set _Max Cell Width", "", SetListMaxWidth),
                         new ("Set Mi_n Cell Width", "", SetListMinWidth)
                     }
                    )
            ]
        };

        var statusBar = new StatusBar (
                                       new Shortcut []
                                       {
                                           new (Key.F2, "OpenBigListEx", () => OpenSimpleList (true)),
                                           new (Key.F3, "CloseExample", CloseExample),
                                           new (Key.F4, "OpenSmListEx", () => OpenSimpleList (false)),
                                           new (Application.QuitKey, "Quit", Quit)
                                       }
                                      );
        appWindow.Add (_listColView);

        var selectedCellLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom (_listColView),
            Text = "0,0",

            Width = Dim.Fill (),
            TextAlignment = Alignment.End
        };

        appWindow.Add (selectedCellLabel);

        _listColView.SelectedCellChanged += (s, e) => { selectedCellLabel.Text = $"{_listColView.SelectedRow},{_listColView.SelectedColumn}"; };
        _listColView.KeyDown += TableViewKeyPress;

        SetupScrollBar ();

        _alternatingColorScheme = new ()
        {
            Disabled = appWindow.ColorScheme.Disabled,
            HotFocus = appWindow.ColorScheme.HotFocus,
            Focus = appWindow.ColorScheme.Focus,
            Normal = new (Color.White, Color.BrightBlue)
        };

        // if user clicks the mouse in TableView
        _listColView.MouseClick += (s, e) => { _listColView.ScreenToCell (e.Position, out int? clickedCol); };

        _listColView.KeyBindings.ReplaceCommands (Key.Space, Command.Accept);

        top.Add (menu, appWindow, statusBar);
        appWindow.Y = 1;
        appWindow.Height = Dim.Fill(Dim.Func (() => statusBar.Frame.Height));

        // Run - Start the application.
        Application.Run (top);
        top.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void CloseExample () { _listColView.Table = null; }
    private void OpenSimpleList (bool big) { SetTable (BuildSimpleList (big ? 1023 : 31)); }
    private void Quit () { Application.RequestStop (); }

    private void RunListWidthDialog (string prompt, Action<TableView, int> setter, Func<TableView, int> getter)
    {
        var accepted = false;
        var ok = new Button { Text = "Ok", IsDefault = true };

        ok.Accepting += (s, e) =>
                     {
                         accepted = true;
                         Application.RequestStop ();
                     };
        var cancel = new Button { Text = "Cancel" };
        cancel.Accepting += (s, e) => { Application.RequestStop (); };
        var d = new Dialog { Title = prompt, Buttons = [ok, cancel] };

        var tf = new TextField { Text = getter (_listColView).ToString (), X = 0, Y = 0, Width = Dim.Fill () };

        d.Add (tf);
        tf.SetFocus ();

        Application.Run (d);
        d.Dispose ();

        if (accepted)
        {
            try
            {
                setter (_listColView, int.Parse (tf.Text));
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery (60, 20, "Failed to set", ex.Message, "Ok");
            }
        }
    }

    private void SetListMaxWidth ()
    {
        RunListWidthDialog ("MaxCellWidth", (s, v) => s.MaxCellWidth = v, s => s.MaxCellWidth);
        _listColView.SetNeedsDisplay ();
    }

    private void SetListMinWidth ()
    {
        RunListWidthDialog ("MinCellWidth", (s, v) => s.MinCellWidth = v, s => s.MinCellWidth);
        _listColView.SetNeedsDisplay ();
    }

    private void SetTable (IList list)
    {
        _listColView.Table = new ListTableSource (list, _listColView);

        if ((ListTableSource)_listColView.Table != null)
        {
            _currentTable = ((ListTableSource)_listColView.Table).DataTable;
        }
    }

    private void SetupScrollBar ()
    {
        var scrollBar = new ScrollBarView (_listColView, true); // (listColView, true, true);

        scrollBar.ChangedPosition += (s, e) =>
                                     {
                                         _listColView.RowOffset = scrollBar.Position;

                                         if (_listColView.RowOffset != scrollBar.Position)
                                         {
                                             scrollBar.Position = _listColView.RowOffset;
                                         }

                                         _listColView.SetNeedsDisplay ();
                                     };
        /*
        scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
            listColView.ColumnOffset = scrollBar.OtherScrollBarView.Position;
            if (listColView.ColumnOffset != scrollBar.OtherScrollBarView.Position) {
                scrollBar.OtherScrollBarView.Position = listColView.ColumnOffset;
            }
            listColView.SetNeedsDisplay ();
        };
        */

        _listColView.DrawContent += (s, e) =>
                                    {
                                        scrollBar.Size = _listColView.Table?.Rows ?? 0;
                                        scrollBar.Position = _listColView.RowOffset;

                                        //scrollBar.OtherScrollBarView.Size = listColView.Table?.Columns - 1 ?? 0;
                                        //scrollBar.OtherScrollBarView.Position = listColView.ColumnOffset;
                                        scrollBar.Refresh ();
                                    };
    }

    private void TableViewKeyPress (object sender, Key e)
    {
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
        //toggle menu item
        _miAlternatingColors.Checked = !_miAlternatingColors.Checked;

        if (_miAlternatingColors.Checked == true)
        {
            _listColView.Style.RowColorGetter = a => { return a.RowIndex % 2 == 0 ? _alternatingColorScheme : null; };
        }
        else
        {
            _listColView.Style.RowColorGetter = null;
        }

        _listColView.SetNeedsDisplay ();
    }

    private void ToggleAlwaysUseNormalColorForVerticalCellLines ()
    {
        _miAlwaysUseNormalColorForVerticalCellLines.Checked =
            !_miAlwaysUseNormalColorForVerticalCellLines.Checked;

        _listColView.Style.AlwaysUseNormalColorForVerticalCellLines =
            (bool)_miAlwaysUseNormalColorForVerticalCellLines.Checked;

        _listColView.Update ();
    }

    private void ToggleBottomline ()
    {
        _miBottomline.Checked = !_miBottomline.Checked;
        _listColView.Style.ShowHorizontalBottomline = (bool)_miBottomline.Checked;
        _listColView.Update ();
    }

    private void ToggleCellLines ()
    {
        _miCellLines.Checked = !_miCellLines.Checked;
        _listColView.Style.ShowVerticalCellLines = (bool)_miCellLines.Checked;
        _listColView.Update ();
    }

    private void ToggleExpandLastColumn ()
    {
        _miExpandLastColumn.Checked = !_miExpandLastColumn.Checked;
        _listColView.Style.ExpandLastColumn = (bool)_miExpandLastColumn.Checked;

        _listColView.Update ();
    }

    private void ToggleInvertSelectedCellFirstCharacter ()
    {
        //toggle menu item
        _miCursor.Checked = !_miCursor.Checked;
        _listColView.Style.InvertSelectedCellFirstCharacter = (bool)_miCursor.Checked;
        _listColView.SetNeedsDisplay ();
    }

    private void ToggleScrollParallel ()
    {
        _miScrollParallel.Checked = !_miScrollParallel.Checked;

        if ((ListTableSource)_listColView.Table != null)
        {
            ((ListTableSource)_listColView.Table).Style.ScrollParallel = (bool)_miScrollParallel.Checked;
            _listColView.SetNeedsDisplay ();
        }
    }

    private void ToggleSmoothScrolling ()
    {
        _miSmoothScrolling.Checked = !_miSmoothScrolling.Checked;
        _listColView.Style.SmoothHorizontalScrolling = (bool)_miSmoothScrolling.Checked;

        _listColView.Update ();
    }

    private void ToggleTopline ()
    {
        _miTopline.Checked = !_miTopline.Checked;
        _listColView.Style.ShowHorizontalHeaderOverline = (bool)_miTopline.Checked;
        _listColView.Update ();
    }

    private void ToggleVerticalOrientation ()
    {
        _miOrientVertical.Checked = !_miOrientVertical.Checked;

        if ((ListTableSource)_listColView.Table != null)
        {
            ((ListTableSource)_listColView.Table).Style.Orientation = (bool)_miOrientVertical.Checked
                                                                          ? Orientation.Vertical
                                                                          : Orientation.Horizontal;
            _listColView.SetNeedsDisplay ();
        }
    }
}
