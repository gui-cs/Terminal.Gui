using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ComboBoxIteration", "ComboBox iteration.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ComboBox")]
public class ComboBoxIteration : Scenario {
    public override void Setup () {
        List<string> items = new () { "one", "two", "three" };

        var lbListView = new Label { AutoSize = false, Width = 10, Height = 1 };
        Win.Add (lbListView);

        var listview = new ListView {
            Y = Pos.Bottom (lbListView) + 1, Width = 10, Height = Dim.Fill (2), Source = new ListWrapper (items)
        };
        Win.Add (listview);

        var lbComboBox = new Label {
            ColorScheme = Colors.ColorSchemes["TopLevel"],
            X = Pos.Right (lbListView) + 1,
            AutoSize = false,
            Width = Dim.Percent (40)
        };

        var comboBox = new ComboBox {
            X = Pos.Right (listview) + 1,
            Y = Pos.Bottom (lbListView) + 1,
            Height = Dim.Fill (2),
            Width = Dim.Percent (40),
            HideDropdownListOnClick = true
        };
        comboBox.SetSource (items);

        listview.SelectedItemChanged += (s, e) => {
            lbListView.Text = items[e.Item];
            comboBox.SelectedItem = e.Item;
        };

        comboBox.SelectedItemChanged += (sender, text) => {
            if (text.Item != -1) {
                lbComboBox.Text = text.Value.ToString ();
                listview.SelectedItem = text.Item;
            }
        };
        Win.Add (lbComboBox, comboBox);
        Win.Add (new TextField { X = Pos.Right (listview) + 1, Y = Pos.Top (comboBox) + 3, Height = 1, Width = 20 });

        var btnTwo = new Button { X = Pos.Right (comboBox) + 1, Text = "Two" };
        btnTwo.Clicked += (s, e) => {
            items = new List<string> { "one", "two" };
            comboBox.SetSource (items);
            listview.SetSource (items);
            listview.SelectedItem = 0;
        };
        Win.Add (btnTwo);

        var btnThree = new Button { X = Pos.Right (comboBox) + 1, Y = Pos.Top (comboBox), Text = "Three" };
        btnThree.Clicked += (s, e) => {
            items = new List<string> { "one", "two", "three" };
            comboBox.SetSource (items);
            listview.SetSource (items);
            listview.SelectedItem = 0;
        };
        Win.Add (btnThree);
    }
}
