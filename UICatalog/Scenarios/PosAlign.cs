using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Pos.Align", "Shows off Pos.Align")]
[ScenarioCategory ("Layout")]
public sealed class PosAlign : Scenario
{
    private readonly Aligner _horizAligner = new ();
    private int _leftMargin;
    private readonly Aligner _vertAligner = new ();
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

        RadioGroup alignRadioGroup = new ()
        {
            X = Pos.Align (_horizAligner.Alignment),
            Y = Pos.Center (),
            RadioLabels = new [] { "Left", "Right", "Centered", "Justified", "FirstLeftRestRight", "LastRightRestLeft" },
            ColorScheme = colorScheme
        };

        alignRadioGroup.SelectedItemChanged += (s, e) =>
                                             {
                                                 _horizAligner.Alignment =
                                                     (Alignment)Enum.Parse (typeof (Alignment), alignRadioGroup.RadioLabels [alignRadioGroup.SelectedItem]);

                                                 foreach (View view in appWindow.Subviews.Where (v => v.X is Pos.PosAlign))
                                                 {
                                                     if (view.X is Pos.PosAlign j)
                                                     {
                                                         var newJust = new Pos.PosAlign (_horizAligner.Alignment)
                                                         {
                                                             Aligner =
                                                             {
                                                                 SpaceBetweenItems = _horizAligner.SpaceBetweenItems
                                                             }
                                                         };
                                                         view.X = newJust;
                                                     }
                                                 }
                                             };
        appWindow.Add (alignRadioGroup);

        CheckBox putSpaces = new ()
        {
            X = Pos.Align (_horizAligner.Alignment),
            Y = Pos.Top (alignRadioGroup),
            ColorScheme = colorScheme,
            Text = "Spaces",
            Checked = true
        };

        putSpaces.Toggled += (s, e) =>
                             {
                                 _horizAligner.SpaceBetweenItems = e.NewValue is { } && e.NewValue.Value;

                                 foreach (View view in appWindow.Subviews.Where (v => v.X is Pos.PosAlign))
                                 {
                                     if (view.X is Pos.PosAlign j)
                                     {
                                         j.Aligner.SpaceBetweenItems = _horizAligner.SpaceBetweenItems;
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

                              foreach (View view in appWindow.Subviews.Where (v => v.X is Pos.PosAlign))
                              {
                                  // Skip the justification radio group
                                  if (view != alignRadioGroup)
                                  {
                                      view.Margin.Thickness = new (_leftMargin, 0, 0, 0);
                                  }
                              }
                          };
        appWindow.Add (margin);

        List<Button> addedViews =
        [
            new ()
            {
                X = Pos.Align (_horizAligner.Alignment),
                Y = Pos.Center (),
                Text = NumberToWords.Convert (0)
            }
        ];

        Buttons.NumericUpDown<int> addedViewsUpDown = new Buttons.NumericUpDown<int>
        {
            X = Pos.Align (_horizAligner.Alignment),
            Y = Pos.Top (alignRadioGroup),
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
                                                          X = Pos.Align (_horizAligner.Alignment),
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

        RadioGroup alignRadioGroup = new ()
        {
            X = 0,
            Y = Pos.Align (_vertAligner.Alignment),
            RadioLabels = new [] { "Top", "Bottom", "Centered", "Justified", "FirstTopRestBottom", "LastBottomRestTop" },
            ColorScheme = colorScheme
        };

        alignRadioGroup.SelectedItemChanged += (s, e) =>
                                             {
                                                 _vertAligner.Alignment =
                                                     (Alignment)Enum.Parse (typeof (Alignment), alignRadioGroup.RadioLabels [alignRadioGroup.SelectedItem]);

                                                 foreach (View view in appWindow.Subviews.Where (v => v.Y is Pos.PosAlign))
                                                 {
                                                     if (view.Y is Pos.PosAlign j)
                                                     {
                                                         var newJust = new Pos.PosAlign (_vertAligner.Alignment)
                                                         {
                                                             Aligner =
                                                             {
                                                                 SpaceBetweenItems = _vertAligner.SpaceBetweenItems
                                                             }
                                                         };
                                                         view.Y = newJust;
                                                     }
                                                 }
                                             };
        appWindow.Add (alignRadioGroup);

        CheckBox putSpaces = new ()
        {
            X = 0,
            Y = Pos.Align (_vertAligner.Alignment),
            ColorScheme = colorScheme,
            Text = "Spaces",
            Checked = true
        };

        putSpaces.Toggled += (s, e) =>
                             {
                                 _vertAligner.SpaceBetweenItems = e.NewValue is { } && e.NewValue.Value;

                                 foreach (View view in appWindow.Subviews.Where (v => v.Y is Pos.PosAlign))
                                 {
                                     if (view.Y is Pos.PosAlign j)
                                     {
                                         j.Aligner.SpaceBetweenItems = _vertAligner.SpaceBetweenItems;
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

                              foreach (View view in appWindow.Subviews.Where (v => v.Y is Pos.PosAlign))
                              {
                                  // Skip the justification radio group
                                  if (view != alignRadioGroup)
                                  {
                                      // BUGBUG: This is a hack to work around #3469
                                      if (view is CheckBox)
                                      {
                                          view.Height = 1 + _topMargin;
                                      }

                                      view.Margin.Thickness = new (0, _topMargin, 0, 0);
                                  }
                              }
                          };
        appWindow.Add (margin);

        List<CheckBox> addedViews =
        [
            new ()
            {
                X = 0,
                Y = Pos.Align (_vertAligner.Alignment),
                Text = NumberToWords.Convert (0)
            }
        ];

        Buttons.NumericUpDown<int> addedViewsUpDown = new Buttons.NumericUpDown<int>
        {
            X = 0,
            Y = Pos.Align (_vertAligner.Alignment),
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
                                                          Y = Pos.Align (_vertAligner.Alignment),
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

            v.X = Pos.Align (Alignment.Right, i / 3);
            v.Y = Pos.Align (Alignment.Justified, i % 3 + 10);

            container.Add (v);
        }

        appWindow.Add (container);
    }
}
