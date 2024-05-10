using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("PosJustification", "Shows off Pos.Justify")]
[ScenarioCategory ("Layout")]
public sealed class PosJustification : Scenario
{
    private readonly Aligner _horizJustifier = new ();
    private int _leftMargin;
    private readonly Aligner _vertJustifier = new ();
    private int _topMargin;

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

        SetupVerticalControls (appWindow);

        Setup3by3Grid (appWindow);

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
            X = Pos.Justify (_horizJustifier.Alignment),
            Y = Pos.Center (),
            RadioLabels = GetUniqueEnumNames<Alignment> (false).ToArray (),
            ColorScheme = colorScheme
        };

        justification.SelectedItemChanged += (s, e) =>
                                             {
                                                 _horizJustifier.Alignment =
                                                     (Alignment)Enum.Parse (typeof (Alignment), justification.SelectedItem.ToString ());

                                                 foreach (View view in appWindow.Subviews.Where (v => v.X is Pos.PosJustify))
                                                 {
                                                     if (view.X is Pos.PosJustify j)
                                                     {
                                                         var newJust = new Pos.PosJustify (_horizJustifier.Alignment)
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
            X = Pos.Justify (_horizJustifier.Alignment),
            Y = Pos.Top (justification),
            ColorScheme = colorScheme,
            Text = "Spaces"
        };

        putSpaces.Toggled += (s, e) =>
                             {
                                 _horizJustifier.PutSpaceBetweenItems = e.NewValue is { } && e.NewValue.Value;

                                 foreach (View view in appWindow.Subviews.Where (v => v.X is Pos.PosJustify))
                                 {
                                     if (view.X is Pos.PosJustify j)
                                     {
                                         j.Justifier.PutSpaceBetweenItems = _horizJustifier.PutSpaceBetweenItems;
                                     }
                                 }
                             };
        appWindow.Add (putSpaces);

        CheckBox margin = new ()
        {
            X = Pos.Left (putSpaces),
            Y = Pos.Bottom (putSpaces),
            ColorScheme = colorScheme,
            Text = "Margin"
        };

        margin.Toggled += (s, e) =>
                          {
                              _leftMargin = e.NewValue is { } && e.NewValue.Value ? 1 : 0;

                              foreach (View view in appWindow.Subviews.Where (v => v.X is Pos.PosJustify))
                              {
                                  // Skip the justification radio group
                                  if (view != justification)
                                  {
                                      view.Margin.Thickness = new (_leftMargin, 0, 0, 0);
                                  }
                              }

                              appWindow.LayoutSubviews ();
                          };
        appWindow.Add (margin);

        List<Button> addedViews = new List<Button> ();

        addedViews.Add (
                        new()
                        {
                            X = Pos.Justify (_horizJustifier.Alignment),
                            Y = Pos.Center (),
                            Text = NumberToWords.Convert (0)
                        });

        Buttons.NumericUpDown<int> addedViewsUpDown = new Buttons.NumericUpDown<int>
        {
            X = Pos.Justify (_horizJustifier.Alignment),
            Y = Pos.Top (justification),
            Width = 9,
            Title = "Added",
            ColorScheme = colorScheme,
            BorderStyle = LineStyle.None,
            Value = addedViews.Count
        };
        addedViewsUpDown.Border.Thickness = new (0, 1, 0, 0);

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
                                                      Button button = addedViews [i];
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
                                                          X = Pos.Justify (_horizJustifier.Alignment),
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

    private void SetupVerticalControls (Window appWindow)
    {
        ColorScheme colorScheme = Colors.ColorSchemes ["Error"];

        RadioGroup justification = new ()
        {
            X = 0,
            Y = Pos.Justify (_vertJustifier.Alignment),
            RadioLabels = GetUniqueEnumNames<Alignment> (true).Reverse ().ToArray (),
            ColorScheme = colorScheme
        };

        justification.SelectedItemChanged += (s, e) =>
                                             {
                                                 _vertJustifier.Alignment =
                                                     (Alignment)Enum.Parse (typeof (Alignment), justification.SelectedItem.ToString ());

                                                 foreach (View view in appWindow.Subviews.Where (v => v.Y is Pos.PosJustify))
                                                 {
                                                     if (view.Y is Pos.PosJustify j)
                                                     {
                                                         var newJust = new Pos.PosJustify (_vertJustifier.Alignment)
                                                         {
                                                             Justifier =
                                                             {
                                                                 PutSpaceBetweenItems = _vertJustifier.PutSpaceBetweenItems
                                                             }
                                                         };
                                                         view.Y = newJust;
                                                     }
                                                 }
                                             };
        appWindow.Add (justification);

        CheckBox putSpaces = new ()
        {
            X = 0,
            Y = Pos.Justify (_vertJustifier.Alignment),
            ColorScheme = colorScheme,
            Text = "Spaces"
        };

        putSpaces.Toggled += (s, e) =>
                             {
                                 _vertJustifier.PutSpaceBetweenItems = e.NewValue is { } && e.NewValue.Value;

                                 foreach (View view in appWindow.Subviews.Where (v => v.Y is Pos.PosJustify))
                                 {
                                     if (view.Y is Pos.PosJustify j)
                                     {
                                         j.Justifier.PutSpaceBetweenItems = _vertJustifier.PutSpaceBetweenItems;
                                     }
                                 }
                             };
        appWindow.Add (putSpaces);

        CheckBox margin = new ()
        {
            X = Pos.Right (putSpaces) + 1,
            Y = Pos.Top (putSpaces),
            ColorScheme = colorScheme,
            Text = "Margin"
        };

        margin.Toggled += (s, e) =>
                          {
                              _topMargin = e.NewValue is { } && e.NewValue.Value ? 1 : 0;

                              foreach (View view in appWindow.Subviews.Where (v => v.Y is Pos.PosJustify))
                              {
                                  // Skip the justification radio group
                                  if (view != justification)
                                  {
                                      view.Margin.Thickness = new (0, _topMargin, 0, 0);
                                  }
                              }

                              appWindow.LayoutSubviews ();
                          };
        appWindow.Add (margin);

        List<CheckBox> addedViews = new List<CheckBox> ();

        addedViews.Add (
                        new()
                        {
                            X = 0,
                            Y = Pos.Justify (_vertJustifier.Alignment),
                            Text = NumberToWords.Convert (0)
                        });

        Buttons.NumericUpDown<int> addedViewsUpDown = new Buttons.NumericUpDown<int>
        {
            X = 0,
            Y = Pos.Justify (_vertJustifier.Alignment),
            Width = 9,
            Title = "Added",
            ColorScheme = colorScheme,
            BorderStyle = LineStyle.None,
            Value = addedViews.Count
        };
        addedViewsUpDown.Border.Thickness = new (0, 1, 0, 0);

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
                                                      CheckBox button = addedViews [i];
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
                                                      var button = new CheckBox
                                                      {
                                                          X = 0,
                                                          Y = Pos.Justify (_vertJustifier.Alignment),
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

    private static IEnumerable<string> GetUniqueEnumNames<T> (bool reverse) where T : Enum
    {
        HashSet<int> values = new HashSet<int> ();
        string [] names = Enum.GetNames (typeof (T));

        if (reverse)
        {
            names = Enum.GetNames (typeof (T)).Reverse ().ToArray ();
        }

        foreach (string name in names)
        {
            var value = (int)Enum.Parse (typeof (T), name);

            if (values.Add (value))
            {
                yield return name;
            }
        }
    }

    private void Setup3by3Grid (Window appWindow)
    {
        var container = new View
        {
            Title = "3 by 3",
            BorderStyle = LineStyle.Single,
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = Dim.Percent (30),
            Height = Dim.Percent (30)
        };

        for (var i = 0; i < 9; i++)

        {
            var v = new View
            {
                Title = $"{i}",
                BorderStyle = LineStyle.Dashed,
                Height = 3,
                Width = 5
            };

            v.X = Pos.Justify (Alignment.Right, i / 3);
            v.Y = Pos.Justify (Alignment.Justified, i % 3 + 10);

            container.Add (v);
        }

        appWindow.Add (container);
    }
}
