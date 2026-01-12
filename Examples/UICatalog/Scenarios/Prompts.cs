using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Prompts", "Demonstrates how to use the Prompt class to show dialogs with custom views.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
public class Prompts : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        Label resultLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center () + 5,
            Width = Dim.Fill (),
            Height = 1,
            TextAlignment = Alignment.Center,
            Text = "Result will appear here"
        };
        window.Add (resultLabel);

        // Button to show a TextField prompt
        Button textFieldButton = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center () - 3,
            Text = "Prompt for _Text Input"
        };

        textFieldButton.Accepting += (_, e) =>
        {
            TextField textField = new ()
            {
                Width = 40,
                Height = 1,
                Text = "Default text"
            };

            string result = Prompt.Show (
                                          app,
                                          "Enter Text",
                                          textField,
                                          tf => tf.Text);

            resultLabel.Text = result is { }
                                   ? $"You entered: {result}"
                                   : "Cancelled";

            e.Handled = true;
        };
        window.Add (textFieldButton);

        // Button to show a TextView prompt
        Button textViewButton = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center () - 1,
            Text = "Prompt for _Multi-line Text"
        };

        textViewButton.Accepting += (_, e) =>
        {
            TextView textView = new ()
            {
                Width = 50,
                Height = 10,
                Text = "Enter multiple lines here...\nLine 2\nLine 3"
            };

            string result = Prompt.Show (
                                          app,
                                          "Enter Multi-line Text",
                                          textView,
                                          tv => tv.Text);

            resultLabel.Text = result is { }
                                   ? $"You entered {result.Split ('\n').Length} lines"
                                   : "Cancelled";

            e.Handled = true;
        };
        window.Add (textViewButton);

        // Button to show a CheckBox prompt
        Button checkBoxButton = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center () + 1,
            Text = "Prompt for _Checkbox Selection"
        };

        checkBoxButton.Accepting += (_, e) =>
        {
            View container = new ()
            {
                Width = 40,
                Height = 5
            };

            CheckBox checkBox1 = new () { X = 0, Y = 0, Text = "Option 1", CheckedState = CheckState.Checked };
            CheckBox checkBox2 = new () { X = 0, Y = 1, Text = "Option 2", CheckedState = CheckState.UnChecked };
            CheckBox checkBox3 = new () { X = 0, Y = 2, Text = "Option 3", CheckedState = CheckState.UnChecked };

            container.Add (checkBox1, checkBox2, checkBox3);

            bool accepted = Prompt.Show (
                                         app,
                                         "Select Options",
                                         container);

            if (accepted)
            {
                List<string> selected = [];

                if (checkBox1.CheckedState == CheckState.Checked)
                {
                    selected.Add ("Option 1");
                }

                if (checkBox2.CheckedState == CheckState.Checked)
                {
                    selected.Add ("Option 2");
                }

                if (checkBox3.CheckedState == CheckState.Checked)
                {
                    selected.Add ("Option 3");
                }

                resultLabel.Text = selected.Count > 0
                                       ? $"Selected: {string.Join (", ", selected)}"
                                       : "No options selected";
            }
            else
            {
                resultLabel.Text = "Cancelled";
            }

            e.Handled = true;
        };
        window.Add (checkBoxButton);

        // Button to show a ListView prompt
        Button listViewButton = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center () + 3,
            Text = "Prompt for _List Selection"
        };

        listViewButton.Accepting += (_, e) =>
        {
            ObservableCollection<string> fruits = new ()
            {
                "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape"
            };

            ListView listView = new ()
            {
                Width = 40,
                Height = 10,
                Source = new ListWrapper<string> (fruits)
            };

            int? selectedIndex = Prompt.Show (
                                               app,
                                               "Select a Fruit",
                                               listView,
                                               lv => lv.SelectedItem);

            if (selectedIndex.HasValue && selectedIndex.Value >= 0 && selectedIndex.Value < fruits.Count)
            {
                string selected = fruits [selectedIndex.Value];
                resultLabel.Text = $"You selected: {selected}";
            }
            else
            {
                resultLabel.Text = "Cancelled";
            }

            e.Handled = true;
        };
        window.Add (listViewButton);

        app.Run (window);
    }
}
