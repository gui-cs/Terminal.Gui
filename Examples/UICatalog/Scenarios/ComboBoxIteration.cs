using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ComboBoxIteration", "ComboBox iteration.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ComboBox")]
public class ComboBoxIteration : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        ObservableCollection<string> items = ["one", "two", "three"];

        var win = new Window { Title = GetQuitKeyAndName () };
        var lbListView = new Label { Width = 10, Height = 1 };
        win.Add (lbListView);

        var listview = new ListView
        {
            Y = Pos.Bottom (lbListView) + 1, Width = 10, Height = Dim.Fill (2), Source = new ListWrapper<string> (items)
        };
        win.Add (listview);

        var lbComboBox = new Label
        {
            SchemeName = "TopLevel",
            X = Pos.Right (lbListView) + 1,
            Width = Dim.Percent (40)
        };

        var comboBox = new ComboBox
        {
            X = Pos.Right (listview) + 1,
            Y = Pos.Bottom (lbListView) + 1,
            Height = Dim.Fill (2),
            Width = Dim.Percent (40),
            HideDropdownListOnClick = true
        };
        comboBox.SetSource (items);

        listview.SelectedItemChanged += (s, e) =>
                                        {
                                            lbListView.Text = items [e.Item];
                                            comboBox.SelectedItem = e.Item;
                                        };

        comboBox.SelectedItemChanged += (sender, text) =>
                                        {
                                            if (text.Item != -1)
                                            {
                                                lbComboBox.Text = text.Value.ToString ();
                                                listview.SelectedItem = text.Item;
                                            }
                                        };
        win.Add (lbComboBox, comboBox);
        win.Add (new TextField { X = Pos.Right (listview) + 1, Y = Pos.Top (comboBox) + 3, Height = 1, Width = 20 });

        var btnTwo = new Button { X = Pos.Right (comboBox) + 1, Text = "Two" };

        btnTwo.Accepting += (s, e) =>
                          {
                              items = ["one", "two"];
                              comboBox.SetSource (items);
                              listview.SetSource (items);
                              listview.SelectedItem = 0;
                          };
        win.Add (btnTwo);

        var btnThree = new Button { X = Pos.Right (comboBox) + 1, Y = Pos.Top (comboBox), Text = "Three" };

        btnThree.Accepting += (s, e) =>
                            {
                                items =["one", "two", "three"];
                                comboBox.SetSource (items);
                                listview.SetSource (items);
                                listview.SelectedItem = 0;
                            };
        win.Add (btnThree);

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }
}
