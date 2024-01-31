﻿#region

using System;
using System.Data;
using System.Text;
using Terminal.Gui;

#endregion

namespace UICatalog.Scenarios {
    [ScenarioMetadata (Name: "MultiColouredTable", Description: "Demonstrates how to multi color cell contents.")]
    [ScenarioCategory ("Controls")]
    [ScenarioCategory ("Colors")]
    [ScenarioCategory ("TableView")]
    public class MultiColouredTable : Scenario {
        TableViewColors tableView;
        private DataTable table;

        public override void Setup () {
            Win.Title = this.GetName ();
            Win.Y = 1; // menu
            Win.Height = Dim.Fill (1); // status bar

            this.tableView = new TableViewColors () {
                                                        X = 0,
                                                        Y = 0,
                                                        Width = Dim.Fill (),
                                                        Height = Dim.Fill (1),
                                                    };

            var menu = new MenuBar (
                                    new MenuBarItem[] {
                                                          new MenuBarItem (
                                                                           "_File",
                                                                           new MenuItem[] {
                                                                               new MenuItem (
                                                                                "_Quit",
                                                                                "",
                                                                                () => Quit ()),
                                                                           }),
                                                      });
            Application.Top.Add (menu);

            var statusBar = new StatusBar (
                                           new StatusItem[] {
                                                                new StatusItem (
                                                                                Application.QuitKey,
                                                                                $"{Application.QuitKey} to Quit",
                                                                                () => Quit ()),
                                                            });
            Application.Top.Add (statusBar);

            Win.Add (tableView);

            tableView.CellActivated += EditCurrentCell;

            var dt = new DataTable ();
            dt.Columns.Add ("Col1");
            dt.Columns.Add ("Col2");

            dt.Rows.Add ("some text", "Rainbows and Unicorns are so fun!");
            dt.Rows.Add ("some text", "When it rains you get rainbows");
            dt.Rows.Add (DBNull.Value, DBNull.Value);
            dt.Rows.Add (DBNull.Value, DBNull.Value);
            dt.Rows.Add (DBNull.Value, DBNull.Value);
            dt.Rows.Add (DBNull.Value, DBNull.Value);

            tableView.ColorScheme = new ColorScheme () {
                                                           Disabled = Win.ColorScheme.Disabled,
                                                           HotFocus = Win.ColorScheme.HotFocus,
                                                           Focus = Win.ColorScheme.Focus,
                                                           Normal = new Attribute (Color.DarkGray, Color.Black)
                                                       };

            tableView.Table = new DataTableSource (this.table = dt);
        }

        private void Quit () { Application.RequestStop (); }

        private bool GetText (string title, string label, string initialText, out string enteredText) {
            bool okPressed = false;

            var ok = new Button ("Ok", is_default: true);
            ok.Clicked += (s, e) => {
                okPressed = true;
                Application.RequestStop ();
            };
            var cancel = new Button ("Cancel");
            cancel.Clicked += (s, e) => { Application.RequestStop (); };
            var d = new Dialog (ok, cancel) { Title = title };

            var lbl = new Label () {
                                       X = 0,
                                       Y = 1,
                                       Text = label
                                   };

            var tf = new TextField () {
                                          Text = initialText,
                                          X = 0,
                                          Y = 2,
                                          Width = Dim.Fill ()
                                      };

            d.Add (lbl, tf);
            tf.SetFocus ();

            Application.Run (d);

            enteredText = okPressed ? tf.Text : null;

            return okPressed;
        }

        private void EditCurrentCell (object sender, CellActivatedEventArgs e) {
            if (e.Table == null)
                return;

            var oldValue = e.Table[e.Row, e.Col].ToString ();

            if (GetText ("Enter new value", e.Table.ColumnNames[e.Col], oldValue, out string newText)) {
                try {
                    table.Rows[e.Row][e.Col] = string.IsNullOrWhiteSpace (newText) ? DBNull.Value : (object)newText;
                }
                catch (Exception ex) {
                    MessageBox.ErrorQuery (60, 20, "Failed to set text", ex.Message, "Ok");
                }

                tableView.Update ();
            }
        }

        class TableViewColors : TableView {
            protected override void RenderCell (Attribute cellColor, string render, bool isPrimaryCell) {
                int unicorns = render.IndexOf ("unicorns", StringComparison.CurrentCultureIgnoreCase);
                int rainbows = render.IndexOf ("rainbows", StringComparison.CurrentCultureIgnoreCase);

                for (int i = 0; i < render.Length; i++) {
                    if (unicorns != -1 && i >= unicorns && i <= unicorns + 8) {
                        Driver.SetAttribute (new Attribute (Color.White, cellColor.Background));
                    }

                    if (rainbows != -1 && i >= rainbows && i <= rainbows + 8) {
                        var letterOfWord = i - rainbows;
                        switch (letterOfWord) {
                            case 0:
                                Driver.SetAttribute (new Attribute (Color.Red, cellColor.Background));

                                break;
                            case 1:
                                Driver.SetAttribute (new Attribute (Color.BrightRed, cellColor.Background));

                                break;
                            case 2:
                                Driver.SetAttribute (new Attribute (Color.BrightYellow, cellColor.Background));

                                break;
                            case 3:
                                Driver.SetAttribute (new Attribute (Color.Green, cellColor.Background));

                                break;
                            case 4:
                                Driver.SetAttribute (new Attribute (Color.BrightGreen, cellColor.Background));

                                break;
                            case 5:
                                Driver.SetAttribute (new Attribute (Color.BrightBlue, cellColor.Background));

                                break;
                            case 6:
                                Driver.SetAttribute (new Attribute (Color.BrightCyan, cellColor.Background));

                                break;
                            case 7:
                                Driver.SetAttribute (new Attribute (Color.Cyan, cellColor.Background));

                                break;
                        }
                    }

                    Driver.AddRune ((Rune)render[i]);
                    Driver.SetAttribute (cellColor);
                }
            }
        }
    }
}
