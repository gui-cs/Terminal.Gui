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

    public override void Setup ()
    {
        Win.Title = GetName ();
        Win.Y = 1; // menu
        Win.Height = Dim.Fill (1); // status bar

        _tableView = new TableViewColors { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill (1) };

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem ("_File", new MenuItem [] { new ("_Quit", "", () => Quit ()) })
            ]
        };
        Top.Add (menu);

        var statusBar = new StatusBar (
                                       new StatusItem []
                                       {
                                           new (
                                                Application.QuitKey,
                                                $"{Application.QuitKey} to Quit",
                                                () => Quit ()
                                               )
                                       }
                                      );
        Top.Add (statusBar);

        Win.Add (_tableView);

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

        _tableView.ColorScheme = new ColorScheme
        {
            Disabled = Win.ColorScheme.Disabled,
            HotFocus = Win.ColorScheme.HotFocus,
            Focus = Win.ColorScheme.Focus,
            Normal = new Attribute (Color.DarkGray, Color.Black)
        };

        _tableView.Table = new DataTableSource (_table = dt);
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
                    Driver.SetAttribute (new Attribute (Color.White, cellColor.Background));
                }

                if (rainbows != -1 && i >= rainbows && i <= rainbows + 8)
                {
                    int letterOfWord = i - rainbows;

                    switch (letterOfWord)
                    {
                        case 0:
                            Driver.SetAttribute (new Attribute (Color.Red, cellColor.Background));

                            break;
                        case 1:
                            Driver.SetAttribute (
                                                 new Attribute (
                                                                Color.BrightRed,
                                                                cellColor.Background
                                                               )
                                                );

                            break;
                        case 2:
                            Driver.SetAttribute (
                                                 new Attribute (
                                                                Color.BrightYellow,
                                                                cellColor.Background
                                                               )
                                                );

                            break;
                        case 3:
                            Driver.SetAttribute (new Attribute (Color.Green, cellColor.Background));

                            break;
                        case 4:
                            Driver.SetAttribute (
                                                 new Attribute (
                                                                Color.BrightGreen,
                                                                cellColor.Background
                                                               )
                                                );

                            break;
                        case 5:
                            Driver.SetAttribute (
                                                 new Attribute (
                                                                Color.BrightBlue,
                                                                cellColor.Background
                                                               )
                                                );

                            break;
                        case 6:
                            Driver.SetAttribute (
                                                 new Attribute (
                                                                Color.BrightCyan,
                                                                cellColor.Background
                                                               )
                                                );

                            break;
                        case 7:
                            Driver.SetAttribute (new Attribute (Color.Cyan, cellColor.Background));

                            break;
                    }
                }

                Driver.AddRune ((Rune)render [i]);
                Driver.SetAttribute (cellColor);
            }
        }
    }
}
