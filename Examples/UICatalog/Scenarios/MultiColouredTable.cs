#nullable enable

using System.Data;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MultiColouredTable", "Demonstrates how to multi color cell contents.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("TableView")]
public class MultiColouredTable : Scenario
{
    private IApplication? _app;
    private DataTable? _table;
    private TableViewColors? _tableView;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None,
        };

        // MenuBar
        MenuBar menu = new ();

        menu.Add (
                  new MenuBarItem (
                                   Strings.menuFile,
                                   [
                                       new MenuItem
                                       {
                                           Title = Strings.cmdQuit,
                                           Action = Quit
                                       }
                                   ]
                                  )
                 );

        _tableView = new () { X = 0, Y = Pos.Bottom (menu), Width = Dim.Fill (), Height = Dim.Fill (1) };

        // StatusBar
        StatusBar statusBar = new (
                                       [
                                           new (Application.QuitKey, "Quit", Quit)
                                       ]
                                      );

        appWindow.Add (menu, _tableView, statusBar);

        _tableView.CellActivated += EditCurrentCell;

        DataTable dt = new ();
        dt.Columns.Add ("Col1");
        dt.Columns.Add ("Col2");

        dt.Rows.Add ("some text", "Rainbows and Unicorns are so fun!");
        dt.Rows.Add ("some text", "When it rains you get rainbows");
        dt.Rows.Add (DBNull.Value, DBNull.Value);
        dt.Rows.Add (DBNull.Value, DBNull.Value);
        dt.Rows.Add (DBNull.Value, DBNull.Value);
        dt.Rows.Add (DBNull.Value, DBNull.Value);

        _tableView.SetScheme (
                              new ()
                              {
                                  Disabled = appWindow.GetAttributeForRole (VisualRole.Disabled),
                                  HotFocus = appWindow.GetAttributeForRole (VisualRole.HotFocus),
                                  Focus = appWindow.GetAttributeForRole (VisualRole.Focus),
                                  Normal = new (Color.DarkGray, Color.Black)
                              }
                             );

        _tableView.Table = new DataTableSource (_table = dt);

        app.Run (appWindow);
    }

    private void EditCurrentCell (object? sender, CellActivatedEventArgs e)
    {
        if (e.Table is null || _table is null || _tableView is null)
        {
            return;
        }

        string? oldValue = e.Table [e.Row, e.Col].ToString ();

        if (GetText ("Enter new value", e.Table.ColumnNames [e.Col], oldValue ?? "", out string newText))
        {
            try
            {
                _table.Rows [e.Row] [e.Col] =
                    string.IsNullOrWhiteSpace (newText) ? DBNull.Value : newText;
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery (_tableView!.App!, "Failed to set text", ex.Message, "Ok");
            }

            _tableView.Update ();
        }
    }

    private bool GetText (string title, string label, string initialText, out string enteredText)
    {
        Dialog d = new ()
        {
            Title = title,
            Buttons = [new () { Title = Strings.btnCancel }, new () { Title = Strings.btnOk }]
        };

        Label lbl = new () { X = 0, Y = 1, Text = label };

        TextField tf = new () { Text = initialText, X = 0, Y = 2, Width = Dim.Fill (0, minimumContentDim: 50) };

        d.Add (lbl, tf);
        tf.SetFocus ();

        _app?.Run (d);
        bool okPressed = d.Result == 1;
        d.Dispose ();

        enteredText = okPressed ? tf.Text : string.Empty;

        return okPressed;
    }

    private void Quit () { _tableView?.App?.RequestStop (); }

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
