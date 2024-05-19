using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Pos.Align", "Shows off Pos.Align")]
[ScenarioCategory ("Layout")]
public sealed class PosAlignDemo : Scenario
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

        SetupControls (appWindow, Dimension.Width, Colors.ColorSchemes ["Toplevel"]);

        SetupControls (appWindow, Dimension.Height, Colors.ColorSchemes ["Error"]);

        //Setup3by3Grid (appWindow);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void SetupControls (Window appWindow, Dimension dimension, ColorScheme colorScheme)
    {
        RadioGroup alignRadioGroup = new ()
        {
            RadioLabels = Enum.GetNames<Alignment> (),
            ColorScheme = colorScheme
        };

        if (dimension == Dimension.Width)
        {
            alignRadioGroup.X = Pos.Align (_horizAligner.Alignment);
            alignRadioGroup.Y = Pos.Center ();
        }
        else
        {
            alignRadioGroup.X = Pos.Center ();
            alignRadioGroup.Y = Pos.Align (_vertAligner.Alignment);
        }

        alignRadioGroup.SelectedItemChanged += (s, e) =>
                                               {
                                                   if (dimension == Dimension.Width)
                                                   {
                                                       _horizAligner.Alignment =
                                                         (Alignment)Enum.Parse (typeof (Alignment), alignRadioGroup.RadioLabels [alignRadioGroup.SelectedItem]);
                                                       UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                                   }
                                                   else
                                                   {
                                                       _vertAligner.Alignment =
                                                         (Alignment)Enum.Parse (typeof (Alignment), alignRadioGroup.RadioLabels [alignRadioGroup.SelectedItem]);
                                                       UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                                   }

                                               };
        appWindow.Add (alignRadioGroup);


        CheckBox ignoreFirstOrLast = new ()
        {
            ColorScheme = colorScheme,
            Text = "IgnoreFirstOrLast",
        };

        if (dimension == Dimension.Width)
        {
            ignoreFirstOrLast.Checked = _horizAligner.AlignmentMode.HasFlag (AlignmentModes.IgnoreFirstOrLast);
            ignoreFirstOrLast.X = Pos.Align (_horizAligner.Alignment);
            ignoreFirstOrLast.Y = Pos.Top (alignRadioGroup);
        }
        else
        {
            ignoreFirstOrLast.Checked = _vertAligner.AlignmentMode.HasFlag (AlignmentModes.IgnoreFirstOrLast);
            ignoreFirstOrLast.X = Pos.Left (alignRadioGroup);
            ignoreFirstOrLast.Y = Pos.Align (_vertAligner.Alignment);
        }

        ignoreFirstOrLast.Toggled += (s, e) =>
                               {
                                   if (dimension == Dimension.Width)
                                   {
                                       _horizAligner.AlignmentMode = e.NewValue is { } &&
                                                                 e.NewValue.Value ?
                                                                     _horizAligner.AlignmentMode | AlignmentModes.IgnoreFirstOrLast :
                                                                     _horizAligner.AlignmentMode & ~AlignmentModes.IgnoreFirstOrLast;
                                       UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                   }
                                   else
                                   {
                                       _vertAligner.AlignmentMode = e.NewValue is { } &&
                                                                 e.NewValue.Value ?
                                                                     _vertAligner.AlignmentMode | AlignmentModes.IgnoreFirstOrLast :
                                                                     _vertAligner.AlignmentMode & ~AlignmentModes.IgnoreFirstOrLast;
                                       UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                   }
                               };
        appWindow.Add (ignoreFirstOrLast);

        CheckBox addSpacesBetweenItems = new ()
        {
            ColorScheme = colorScheme,
            Text = "AddSpaceBetweenItems",
        };

        if (dimension == Dimension.Width)
        {
            addSpacesBetweenItems.Checked = _horizAligner.AlignmentMode.HasFlag (AlignmentModes.AddSpaceBetweenItems);
            addSpacesBetweenItems.X = Pos.Align (_horizAligner.Alignment);
            addSpacesBetweenItems.Y = Pos.Top (alignRadioGroup);
        }
        else
        {
            addSpacesBetweenItems.Checked = _vertAligner.AlignmentMode.HasFlag (AlignmentModes.AddSpaceBetweenItems);
            addSpacesBetweenItems.X = Pos.Left (alignRadioGroup);
            addSpacesBetweenItems.Y = Pos.Align (_vertAligner.Alignment);
        }

        addSpacesBetweenItems.Toggled += (s, e) =>
                             {
                                 if (dimension == Dimension.Width)
                                 {
                                     _horizAligner.AlignmentMode = e.NewValue is { } &&
                                                             e.NewValue.Value ?
                                                                 _horizAligner.AlignmentMode | AlignmentModes.AddSpaceBetweenItems :
                                                                 _horizAligner.AlignmentMode & ~AlignmentModes.AddSpaceBetweenItems;
                                     UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                 }
                                 else
                                 {
                                     _vertAligner.AlignmentMode = e.NewValue is { } &&
                                                             e.NewValue.Value ?
                                                                 _vertAligner.AlignmentMode | AlignmentModes.AddSpaceBetweenItems :
                                                                 _vertAligner.AlignmentMode & ~AlignmentModes.AddSpaceBetweenItems;
                                     UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                 }

                             };

        appWindow.Add (addSpacesBetweenItems);

        CheckBox margin = new ()
        {
            ColorScheme = colorScheme,
            Text = "Margin"
        };

        if (dimension == Dimension.Width)
        {
            margin.X = Pos.Align (_horizAligner.Alignment);
            margin.Y = Pos.Top (alignRadioGroup);
        }
        else
        {
            margin.X = Pos.Left (addSpacesBetweenItems);
            margin.Y = Pos.Align (_vertAligner.Alignment);
        }

        margin.Toggled += (s, e) =>
                          {
                              if (dimension == Dimension.Width)
                              {
                                  _leftMargin = e.NewValue is { } && e.NewValue.Value ? 1 : 0;
                                  UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                              }
                              else
                              {
                                  _topMargin = e.NewValue is { } && e.NewValue.Value ? 1 : 0;
                                  UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                              }
                          };
        appWindow.Add (margin);

        List<Button> addedViews =
        [
            new ()
                                                       {
                                                           X = dimension == Dimension.Width ? Pos.Align (_horizAligner.Alignment) : Pos.Left (alignRadioGroup),
                                                           Y = dimension == Dimension.Width ? Pos.Top(alignRadioGroup): Pos.Align (_vertAligner.Alignment),
                                                           Text = NumberToWords.Convert (0)
                                                       }
        ];

        Buttons.NumericUpDown<int> addedViewsUpDown = new Buttons.NumericUpDown<int>
        {
            Width = 9,
            Title = "Added",
            ColorScheme = colorScheme,
            BorderStyle = LineStyle.None,
            Value = addedViews.Count
        };

        if (dimension == Dimension.Width)
        {
            addedViewsUpDown.X = Pos.Align (_horizAligner.Alignment);
            addedViewsUpDown.Y = Pos.Top (alignRadioGroup);
            addedViewsUpDown.Border.Thickness = new (0, 1, 0, 0);
        }
        else
        {
            addedViewsUpDown.X = Pos.Left (alignRadioGroup);
            addedViewsUpDown.Y = Pos.Align (_vertAligner.Alignment);
            addedViewsUpDown.Border.Thickness = new (1, 0, 0, 0);
        }

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
                                                          X = dimension == Dimension.Width ? Pos.Align (_horizAligner.Alignment) : Pos.Left (alignRadioGroup),
                                                          Y = dimension == Dimension.Width ? Pos.Top (alignRadioGroup) : Pos.Align (_vertAligner.Alignment),
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

    private void UpdatePosAlignObjects (Window appWindow, Dimension dimension, Aligner aligner)
    {
        foreach (View view in appWindow.Subviews.Where (v => dimension == Dimension.Width ? v.X is PosAlign : v.Y is PosAlign))
        {
            if (dimension == Dimension.Width ? view.X is PosAlign posAlign : view.Y is PosAlign)
            {
                //posAlign.Aligner.Alignment = _horizAligner.Alignment;
                //posAlign.Aligner.AlignmentMode = _horizAligner.AlignmentMode;

                // BUGBUG: Create and assign a new Pos object because we currently have no way for X to be notified
                // BUGBUG: of changes in the Pos object. See https://github.com/gui-cs/Terminal.Gui/issues/3485
                if (dimension == Dimension.Width)
                {
                    view.X = new PosAlign (
                                           aligner.Alignment,
                                           aligner.AlignmentMode);
                    view.Margin.Thickness = new (_leftMargin, 0, 0, 0);

                }
                else
                {
                    view.Y = new PosAlign (
                                           aligner.Alignment,
                                           aligner.AlignmentMode);

                    view.Margin.Thickness = new (0, _topMargin, 0, 0);
                }
            }
        }

        appWindow.LayoutSubviews ();
    }
}
