﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ListView & ComboBox", "Demonstrates a ListView populating a ComboBox that acts as a filter.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
[ScenarioCategory ("ComboBox")]
public class ListsAndCombos : Scenario
{
    public override void Setup ()
    {
        //TODO: Duplicated code in Demo.cs Consider moving to shared assembly
        ObservableCollection<string> items = [];

        foreach (string dir in new [] { "/etc", @$"{Environment.GetEnvironmentVariable ("SystemRoot")}\System32" })
        {
            if (Directory.Exists (dir))
            {
                items = new (Directory.GetFiles (dir)
                                      .Union (Directory.GetDirectories (dir))
                                      .Select (Path.GetFileName)
                                      .Where (x => char.IsLetterOrDigit (x [0]))
                                      .OrderBy (x => x)
                                      .Select (x => x)
                                      .ToList ());
            }
        }

        // ListView
        var lbListView = new Label
        {
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            X = 0,

            Width = Dim.Percent (40),
            Text = "Listview"
        };

        var listview = new ListView
        {
            X = 0,
            Y = Pos.Bottom (lbListView) + 1,
            Height = Dim.Fill (2),
            Width = Dim.Percent (40),
            Source = new ListWrapper<string> (items)
        };
        listview.SelectedItemChanged += (s, e) => lbListView.Text = items [listview.SelectedItem];
        Win.Add (lbListView, listview);

        var scrollBar = new ScrollBarView (listview, true);

        scrollBar.ChangedPosition += (s, e) =>
                                     {
                                         listview.TopItem = scrollBar.Position;

                                         if (listview.TopItem != scrollBar.Position)
                                         {
                                             scrollBar.Position = listview.TopItem;
                                         }

                                         listview.SetNeedsDisplay ();
                                     };

        scrollBar.OtherScrollBarView.ChangedPosition += (s, e) =>
                                                        {
                                                            listview.LeftItem = scrollBar.OtherScrollBarView.Position;

                                                            if (listview.LeftItem != scrollBar.OtherScrollBarView.Position)
                                                            {
                                                                scrollBar.OtherScrollBarView.Position = listview.LeftItem;
                                                            }

                                                            listview.SetNeedsDisplay ();
                                                        };

        listview.DrawContent += (s, e) =>
                                {
                                    scrollBar.Size = listview.Source.Count - 1;
                                    scrollBar.Position = listview.TopItem;
                                    scrollBar.OtherScrollBarView.Size = listview.MaxLength - 1;
                                    scrollBar.OtherScrollBarView.Position = listview.LeftItem;
                                    scrollBar.Refresh ();
                                };

        // ComboBox
        var lbComboBox = new Label
        {
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            X = Pos.Right (lbListView) + 1,

            Width = Dim.Percent (40),
            Text = "ComboBox"
        };

        var comboBox = new ComboBox
        {
            X = Pos.Right (listview) + 1,
            Y = Pos.Bottom (lbListView) + 1,
            Height = Dim.Fill (2),
            Width = Dim.Percent (40)
        };
        comboBox.SetSource (items);

        comboBox.SelectedItemChanged += (s, text) => lbComboBox.Text = text.Value.ToString ();
        Win.Add (lbComboBox, comboBox);

        var scrollBarCbx = new ScrollBarView (comboBox.Subviews [1], true);

        scrollBarCbx.ChangedPosition += (s, e) =>
                                        {
                                            ((ListView)comboBox.Subviews [1]).TopItem = scrollBarCbx.Position;

                                            if (((ListView)comboBox.Subviews [1]).TopItem != scrollBarCbx.Position)
                                            {
                                                scrollBarCbx.Position = ((ListView)comboBox.Subviews [1]).TopItem;
                                            }

                                            comboBox.SetNeedsDisplay ();
                                        };

        scrollBarCbx.OtherScrollBarView.ChangedPosition += (s, e) =>
                                                           {
                                                               ((ListView)comboBox.Subviews [1]).LeftItem = scrollBarCbx.OtherScrollBarView.Position;

                                                               if (((ListView)comboBox.Subviews [1]).LeftItem != scrollBarCbx.OtherScrollBarView.Position)
                                                               {
                                                                   scrollBarCbx.OtherScrollBarView.Position = ((ListView)comboBox.Subviews [1]).LeftItem;
                                                               }

                                                               comboBox.SetNeedsDisplay ();
                                                           };

        comboBox.DrawContent += (s, e) =>
                                {
                                    scrollBarCbx.Size = comboBox.Source.Count;
                                    scrollBarCbx.Position = ((ListView)comboBox.Subviews [1]).TopItem;
                                    scrollBarCbx.OtherScrollBarView.Size = ((ListView)comboBox.Subviews [1]).MaxLength - 1;
                                    scrollBarCbx.OtherScrollBarView.Position = ((ListView)comboBox.Subviews [1]).LeftItem;
                                    scrollBarCbx.Refresh ();
                                };

        var btnMoveUp = new Button { X = 1, Y = Pos.Bottom (lbListView), Text = "Move _Up" };
        btnMoveUp.Accept += (s, e) => { listview.MoveUp (); };

        var btnMoveDown = new Button
        {
            X = Pos.Right (btnMoveUp) + 1, Y = Pos.Bottom (lbListView), Text = "Move _Down"
        };
        btnMoveDown.Accept += (s, e) => { listview.MoveDown (); };

        Win.Add (btnMoveUp, btnMoveDown);
    }
}
