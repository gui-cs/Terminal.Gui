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
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        ObservableCollection<string> items = ["one", "two", "three"];

        using Window win = new () { Title = GetQuitKeyAndName () };
        Label lbListView = new () { Width = 10, Height = 1 };
        win.Add (lbListView);

        ListView listview = new ()
        {
            Y = Pos.Bottom (lbListView) + 1, Width = 10, Height = Dim.Fill (2), Source = new ListWrapper<string> (items)
        };
        win.Add (listview);

        Label lbComboBox = new ()
        {
            SchemeName = "Runnable",
            X = Pos.Right (lbListView) + 1,
            Width = Dim.Percent (40)
        };

        ComboBox comboBox = new ()
        {
            X = Pos.Right (listview) + 1,
            Y = Pos.Bottom (lbListView) + 1,
            Height = Dim.Fill (2),
            Width = Dim.Percent (40),
            HideDropdownListOnClick = true
        };
        comboBox.SetSource (items);

        listview.SelectedItemChanged += (_, e) =>
                                        {
                                            lbListView.Text = items [e.Item!.Value];
                                            comboBox.SelectedItem = e.Item.Value;
                                        };

        comboBox.SelectedItemChanged += (_, text) =>
                                        {
                                            if (text.Item != -1)
                                            {
                                                lbComboBox.Text = text.Value!.ToString ()!;
                                                listview.SelectedItem = text.Item;
                                            }
                                        };
        win.Add (lbComboBox, comboBox);
        win.Add (new TextField { X = Pos.Right (listview) + 1, Y = Pos.Top (comboBox) + 3, Height = 1, Width = 20 });

        Button btnTwo = new () { X = Pos.Right (comboBox) + 1, Text = "Two" };

        btnTwo.Accepting += (_, _) =>
                          {
                              items = ["one", "two"];
                              comboBox.SetSource (items);
                              listview.SetSource (items);
                              listview.SelectedItem = 0;
                          };
        win.Add (btnTwo);

        Button btnThree = new () { X = Pos.Right (comboBox) + 1, Y = Pos.Top (comboBox), Text = "Three" };

        btnThree.Accepting += (_, _) =>
                            {
                                items = ["one", "two", "three"];
                                comboBox.SetSource (items);
                                listview.SetSource (items);
                                listview.SelectedItem = 0;
                            };
        win.Add (btnThree);

        app.Run (win);
    }
}
