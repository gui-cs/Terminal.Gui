using System;
using System.Data;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MultiColouredTable", "Demonstrates how to multi color cell contents.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("TableView")]
public class MultiColouredTable : Scenario
{
    private DataTable _table;
    private TableViewColors _tableView;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Toplevel appWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        _tableView = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        var menu = new MenuBar
        {
            Menus =
            [
                new ("_File", new MenuItem [] { new ("_Quit", "", Quit) })
            ]
        };
        appWindow.Add (menu);

        var statusBar = new StatusBar (new Shortcut [] { new (Application.QuitKey, "Quit", Quit) });

        appWindow.Add (statusBar);

        appWindow.Add (_tableView);

        _tableView.CellActivated += EditCurrentCell;

        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Columns.Add ("Col2");

        dt.Rows.Add ("some text", "Rainbows and Unicorns are so fun!");
        dt.Rows.Add ("some text", "When it rains you get rainbows");
        dt.Rows.Add (DBNull.Value, DBNull.Value);
        dt.Rows.Add (DBNull.Value, DBNull.Value);
        dt.Rows.Add (DBNull.Value, DBNull.Value);
        dt.Rows.Add (DBNull.Value, DBNull.Value);

        _tableView.ColorScheme = new ()
        {
            Disabled = appWindow.ColorScheme.Disabled,
            HotFocus = appWindow.ColorScheme.HotFocus,
            Focus = appWindow.ColorScheme.Focus,
            Normal = new (Color.DarkGray, Color.Black)
        };

        _tableView.Table = new DataTableSource (_table = dt);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void EditCurrentCell (object sender, CellActivatedEventArgs e)
    {
        if (e.Table == null)
        {
            return;
        }

        var oldValue = e.Table [e.Row, e.Col].ToString ();

        if (GetText ("Enter new value", e.Table.ColumnNames [e.Col], oldValue, out string newText))
        {
            try
            {
                _table.Rows [e.Row] [e.Col] =
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

        ok.Accepting += (s, e) =>
                     {
                         okPressed = true;
                         Application.RequestStop ();
                     };
        var cancel = new Button { Text = "Cancel" };
        cancel.Accepting += (s, e) => { Application.RequestStop (); };
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

    private void Quit () { Application.RequestStop (); }

    private class TableViewColors : TableView
    {
        protected override void RenderCell (Attribute cellColor, string render, bool isPrimaryCell)
        {
            int unicorns = render.IndexOf ("unicorns", StringComparison.CurrentCultureIgnoreCase);
            int rainbows = render.IndexOf ("rainbows", StringComparison.CurrentCultureIgnoreCase);

            for (var i = 0; i < render.Length; i++)
            {
                if (unicorns != -1 && i >= unicorns && i <= unicorns + 8)
                {
                    SetAttribute (new (Color.White, cellColor.Background));
                }

                if (rainbows != -1 && i >= rainbows && i <= rainbows + 8)
                {
                    int letterOfWord = i - rainbows;

                    switch (letterOfWord)
                    {
                        case 0:
                            SetAttribute (new (Color.Red, cellColor.Background));

                            break;
                        case 1:
                            SetAttribute (
                                                 new (
                                                      Color.BrightRed,
                                                      cellColor.Background
                                                     )
                                                );

                            break;
                        case 2:
                            SetAttribute (
                                                 new (
                                                      Color.BrightYellow,
                                                      cellColor.Background
                                                     )
                                                );

                            break;
                        case 3:
                            SetAttribute (new (Color.Green, cellColor.Background));

                            break;
                        case 4:
                            SetAttribute (
                                                 new (
                                                      Color.BrightGreen,
                                                      cellColor.Background
                                                     )
                                                );

                            break;
                        case 5:
                            SetAttribute (
                                                 new (
                                                      Color.BrightBlue,
                                                      cellColor.Background
                                                     )
                                                );

                            break;
                        case 6:
                            SetAttribute (
                                                 new (
                                                      Color.BrightCyan,
                                                      cellColor.Background
                                                     )
                                                );

                            break;
                        case 7:
                            SetAttribute (new (Color.Cyan, cellColor.Background));

                            break;
                    }
                }

                AddRune ((Rune)render [i]);
                SetAttribute (cellColor);
            }
        }
    }
}
