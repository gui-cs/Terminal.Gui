using System.Collections.Generic;
using System;
using Terminal.Gui;
using System.Linq;
using System.Reflection.Emit;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("PosJustification", "Shows off Pos.Justify")]
[ScenarioCategory ("Layout")]
public sealed class PosJustification : Scenario
{

    private Justifier _horizJustifier = new Justifier ();
    private int _leftMargin = 0;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()} - {GetDescription ()}"
        };

        SetupHorizontalControls (appWindow);

        var checkbox = new CheckBox { X = 5, Y = Pos.Justify (_horizJustifier.Justification), Text = "Check boxes!" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed the checkbox!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (_horizJustifier.Justification), Text = "CheckTwo" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Two!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (_horizJustifier.Justification), Text = "CheckThree" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (_horizJustifier.Justification), Text = "CheckFour" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (_horizJustifier.Justification), Text = "CheckFive" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (_horizJustifier.Justification), Text = "CheckSix" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (_horizJustifier.Justification), Text = "CheckSeven" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        checkbox = new CheckBox { X = 5, Y = Pos.Justify (_horizJustifier.Justification), Text = "CheckEight" };
        checkbox.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed Three!", "Ok");
        appWindow.Add (checkbox);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void SetupHorizontalControls (Window appWindow)
    {
        ColorScheme colorScheme = Colors.ColorSchemes ["Toplevel"];
        RadioGroup justification = new ()
        {
            X = Pos.Justify (_horizJustifier.Justification),
            Y = Pos.Center (),
            RadioLabels = GetUniqueEnumNames<Justification> ().ToArray (),
            ColorScheme = colorScheme
        };

        justification.SelectedItemChanged += (s, e) =>
        {
            _horizJustifier.Justification = (Justification)Enum.Parse (typeof (Justification), justification.SelectedItem.ToString ());
            foreach (var view in appWindow.Subviews.Where (v => v.X is Pos.PosJustify))
            {
                if (view.X is Pos.PosJustify j)
                {
                    var newJust = new Pos.PosJustify (_horizJustifier.Justification)
                    {
                        Justifier =
                        {
                            PutSpaceBetweenItems = _horizJustifier.PutSpaceBetweenItems
                        }
                    };
                    view.X = newJust;
                }
            }
        };
        appWindow.Add (justification);

        CheckBox putSpaces = new ()
        {
            X = Pos.Justify (_horizJustifier.Justification),
            Y = Pos.Top (justification),
            ColorScheme = colorScheme,
            Text = "Spaces",
        };
        putSpaces.Toggled += (s, e) =>
                             {
                                 _horizJustifier.PutSpaceBetweenItems = e.NewValue is { } && e.NewValue.Value;
                                 foreach (var view in appWindow.Subviews.Where (v => v.X is Pos.PosJustify))
                                 {
                                     if (view != justification)
                                     {
                                         if (view.X is Pos.PosJustify j)
                                         {
                                             j.Justifier.PutSpaceBetweenItems = _horizJustifier.PutSpaceBetweenItems;
                                         }

                                     }
                                 }
                             };
        appWindow.Add (putSpaces);

        CheckBox margin = new ()
        {
            X = Pos.Left (putSpaces),
            Y = Pos.Bottom(putSpaces),
            ColorScheme = colorScheme,
            Text = "Margin",
        };
        margin.Toggled += (s, e) =>
        {
            _leftMargin = e.NewValue is { } && e.NewValue.Value ? 1 : 0;
            foreach (var view in appWindow.Subviews.Where (v => v.X is Pos.PosJustify))
            {
                if (view != justification)
                {
                    view.Margin.Thickness = new Thickness (_leftMargin, 0, 0, 0);
                }
            }
        };
        appWindow.Add (margin);

        var addedViews = new List<Button> ();
        addedViews.Add (new Button
        {
            X = Pos.Justify (_horizJustifier.Justification),
            Y = Pos.Center (),
            Text = NumberToWords.Convert (0)
        });

        var addedViewsUpDown = new Buttons.NumericUpDown<int> ()
        {
            X = Pos.Justify (_horizJustifier.Justification),
            Y = Pos.Top (justification),
            Height = 3,
            Width = 9,
            Title = "Added",
            ColorScheme = colorScheme,
            BorderStyle = LineStyle.None,
            Value = addedViews.Count
        };
        addedViewsUpDown.Border.Thickness = new Thickness (0, 1, 0, 0);
        addedViewsUpDown.ValueChanging += (s, e) =>
                                            {
                                                if (e.NewValue < 0)
                                                {
                                                    e.Cancel = true;

                                                    return;
                                                }

                                                // Add or remove buttons
                                                if (e.NewValue < e.OldValue)
                                                {
                                                    // Remove buttons
                                                    for (int i = e.OldValue - 1; i >= e.NewValue; i--)
                                                    {
                                                        var button = addedViews [i];
                                                        appWindow.Remove (button);
                                                        addedViews.RemoveAt (i);
                                                        button.Dispose ();
                                                    }
                                                }

                                                if (e.NewValue > e.OldValue)
                                                {
                                                    // Add buttons
                                                    for (int i = e.OldValue; i < e.NewValue; i++)
                                                    {
                                                        var button = new Button
                                                        {
                                                            X = Pos.Justify (_horizJustifier.Justification),
                                                            Y = Pos.Center (), 
                                                            Text = NumberToWords.Convert (i + 1)
                                                        };
                                                        appWindow.Add (button);
                                                        addedViews.Add (button);
                                                    }
                                                }
                                            };
        appWindow.Add (addedViewsUpDown);

        appWindow.Add (addedViews [0]);

    }

    static IEnumerable<string> GetUniqueEnumNames<T> () where T : Enum
    {
        var values = new HashSet<int> ();
        foreach (var name in Enum.GetNames (typeof (T)))
        {
            var value = (int)Enum.Parse (typeof (T), name);
            if (values.Add (value))
            {
                yield return name;
            }
        }
    }
}
