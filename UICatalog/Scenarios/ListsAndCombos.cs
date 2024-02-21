using System;
using System.Collections.Generic;
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
        List<string> items = new ();

        foreach (string dir in new [] { "/etc", @$"{Environment.GetEnvironmentVariable ("SystemRoot")}\System32" })
        {
            if (Directory.Exists (dir))
            {
                items = Directory.GetFiles (dir)
                                 .Union (Directory.GetDirectories (dir))
                                 .Select (Path.GetFileName)
                                 .Where (x => char.IsLetterOrDigit (x [0]))
                                 .OrderBy (x => x)
                                 .Select (x => x)
                                 .ToList ();
            }
        }

        // ListView
        var lbListView = new Label
        {
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            X = 0,
            AutoSize = false,
            Width = Dim.Percent (40),
            Text = "Listview"
        };

        var listview = new ListView
        {
            X = 0,
            Y = Pos.Bottom (lbListView) + 1,
            Height = Dim.Fill (2),
            Width = Dim.Percent (40),
            Source = new ListWrapper (items)
        };
        listview.Padding.ScrollBarType = ScrollBarType.Both;
        listview.SelectedItemChanged += (s, e) => lbListView.Text = items [listview.SelectedItem];
        Win.Add (lbListView, listview);

        // ComboBox
        var lbComboBox = new Label
        {
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            X = Pos.Right (lbListView) + 1,
            AutoSize = false,
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
        comboBox.Padding.ScrollBarType = ScrollBarType.Both;
        comboBox.SetSource (items);

        comboBox.SelectedItemChanged += (s, text) => lbComboBox.Text = text.Value.ToString ();
        Win.Add (lbComboBox, comboBox);

        var btnMoveUp = new Button { X = 1, Y = Pos.Bottom (lbListView), Text = "Move _Up" };
        btnMoveUp.Clicked += (s, e) => { listview.MoveUp (); };

        var btnMoveDown = new Button
        {
            X = Pos.Right (btnMoveUp) + 1, Y = Pos.Bottom (lbListView), Text = "Move _Down"
        };
        btnMoveDown.Clicked += (s, e) => { listview.MoveDown (); };

        Win.Add (btnMoveUp, btnMoveDown);
    }
}
