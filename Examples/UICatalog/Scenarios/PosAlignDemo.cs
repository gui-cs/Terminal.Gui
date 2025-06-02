using System;
using System.Collections.Generic;
using System.Linq;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Pos.Align", "Demonstrates Pos.Align")]
[ScenarioCategory ("Layout")]
public sealed class PosAlignDemo : Scenario
{
    private readonly Aligner _horizAligner = new () { AlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems};
    private int _leftMargin;
    private readonly Aligner _vertAligner = new () { AlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems };
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

        SetupControls (appWindow, Dimension.Width, Schemes.Toplevel);

        SetupControls (appWindow, Dimension.Height, Schemes.Error);

        Setup3By3Grid (appWindow);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void SetupControls (Window appWindow, Dimension dimension, Schemes scheme)
    {
        RadioGroup alignRadioGroup = new ()
        {
            RadioLabels = Enum.GetNames<Alignment> (),
            SchemeName = SchemeManager.SchemesToSchemeName (scheme),
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
                                                           (Alignment)Enum.Parse (
                                                                                  typeof (Alignment),
                                                                                  alignRadioGroup.RadioLabels [alignRadioGroup.SelectedItem]);
                                                       UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                                   }
                                                   else
                                                   {
                                                       _vertAligner.Alignment =
                                                           (Alignment)Enum.Parse (
                                                                                  typeof (Alignment),
                                                                                  alignRadioGroup.RadioLabels [alignRadioGroup.SelectedItem]);
                                                       UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                                   }
                                               };
        appWindow.Add (alignRadioGroup);

        CheckBox endToStartCheckBox = new ()
        {
            SchemeName = SchemeManager.SchemesToSchemeName (scheme),
            Text = "EndToStart"
        };

        if (dimension == Dimension.Width)
        {
            endToStartCheckBox.CheckedState = _horizAligner.AlignmentModes.HasFlag (AlignmentModes.EndToStart) ? CheckState.Checked : CheckState.UnChecked;
            endToStartCheckBox.X = Pos.Align (_horizAligner.Alignment);
            endToStartCheckBox.Y = Pos.Top (alignRadioGroup);
        }
        else
        {
            endToStartCheckBox.CheckedState = _vertAligner.AlignmentModes.HasFlag (AlignmentModes.EndToStart) ? CheckState.Checked : CheckState.UnChecked;
            endToStartCheckBox.X = Pos.Left (alignRadioGroup);
            endToStartCheckBox.Y = Pos.Align (_vertAligner.Alignment);
        }

        endToStartCheckBox.CheckedStateChanging += (s, e) =>
                                      {
                                          if (dimension == Dimension.Width)
                                          {
                                              _horizAligner.AlignmentModes = e.Result == CheckState.Checked
                                                                                 ? _horizAligner.AlignmentModes | AlignmentModes.EndToStart
                                                                                 : _horizAligner.AlignmentModes & ~AlignmentModes.EndToStart;
                                              UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                          }
                                          else
                                          {
                                              _vertAligner.AlignmentModes = e.Result == CheckState.Checked
                                                                                ? _vertAligner.AlignmentModes | AlignmentModes.EndToStart
                                                                                : _vertAligner.AlignmentModes & ~AlignmentModes.EndToStart;
                                              UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                          }
                                      };
        appWindow.Add (endToStartCheckBox);

        CheckBox ignoreFirstOrLast = new ()
        {
            SchemeName = SchemeManager.SchemesToSchemeName (scheme),
            Text = "IgnoreFirstOrLast"
        };

        if (dimension == Dimension.Width)
        {
            ignoreFirstOrLast.CheckedState = _horizAligner.AlignmentModes.HasFlag (AlignmentModes.IgnoreFirstOrLast) ? CheckState.Checked : CheckState.UnChecked;
            ignoreFirstOrLast.X = Pos.Align (_horizAligner.Alignment);
            ignoreFirstOrLast.Y = Pos.Top (alignRadioGroup);
        }
        else
        {
            ignoreFirstOrLast.CheckedState = _vertAligner.AlignmentModes.HasFlag (AlignmentModes.IgnoreFirstOrLast) ? CheckState.Checked : CheckState.UnChecked;
            ignoreFirstOrLast.X = Pos.Left (alignRadioGroup);
            ignoreFirstOrLast.Y = Pos.Align (_vertAligner.Alignment);
        }

        ignoreFirstOrLast.CheckedStateChanging += (s, e) =>
                                     {
                                         if (dimension == Dimension.Width)
                                         {
                                             _horizAligner.AlignmentModes = e.Result == CheckState.Checked
                                                     ? _horizAligner.AlignmentModes | AlignmentModes.IgnoreFirstOrLast
                                                     : _horizAligner.AlignmentModes & ~AlignmentModes.IgnoreFirstOrLast;
                                             UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                         }
                                         else
                                         {
                                             _vertAligner.AlignmentModes = e.Result == CheckState.Checked
                                                                               ? _vertAligner.AlignmentModes | AlignmentModes.IgnoreFirstOrLast
                                                                               : _vertAligner.AlignmentModes & ~AlignmentModes.IgnoreFirstOrLast;
                                             UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                         }
                                     };
        appWindow.Add (ignoreFirstOrLast);

        CheckBox addSpacesBetweenItems = new ()
        {
            SchemeName = SchemeManager.SchemesToSchemeName (scheme),
            Text = "AddSpaceBetweenItems"
        };

        if (dimension == Dimension.Width)
        {
            addSpacesBetweenItems.CheckedState = _horizAligner.AlignmentModes.HasFlag (AlignmentModes.AddSpaceBetweenItems) ? CheckState.Checked : CheckState.UnChecked;
            addSpacesBetweenItems.X = Pos.Align (_horizAligner.Alignment);
            addSpacesBetweenItems.Y = Pos.Top (alignRadioGroup);
        }
        else
        {
            addSpacesBetweenItems.CheckedState = _vertAligner.AlignmentModes.HasFlag (AlignmentModes.AddSpaceBetweenItems) ? CheckState.Checked : CheckState.UnChecked;
            addSpacesBetweenItems.X = Pos.Left (alignRadioGroup);
            addSpacesBetweenItems.Y = Pos.Align (_vertAligner.Alignment);
        }

        addSpacesBetweenItems.CheckedStateChanging += (s, e) =>
                                         {
                                             if (dimension == Dimension.Width)
                                             {
                                                 _horizAligner.AlignmentModes = e.Result == CheckState.Checked
                                                                                    ? _horizAligner.AlignmentModes | AlignmentModes.AddSpaceBetweenItems
                                                                                    : _horizAligner.AlignmentModes & ~AlignmentModes.AddSpaceBetweenItems;
                                                 UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                                             }
                                             else
                                             {
                                                 _vertAligner.AlignmentModes = e.Result == CheckState.Checked
                                                                                   ? _vertAligner.AlignmentModes | AlignmentModes.AddSpaceBetweenItems
                                                                                   : _vertAligner.AlignmentModes & ~AlignmentModes.AddSpaceBetweenItems;
                                                 UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                                             }
                                         };

        appWindow.Add (addSpacesBetweenItems);

        CheckBox margin = new ()
        {
            SchemeName = SchemeManager.SchemesToSchemeName (scheme),
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

        margin.CheckedStateChanging += (s, e) =>
                          {
                              if (dimension == Dimension.Width)
                              {
                                  _leftMargin = e.Result == CheckState.Checked ? 1 : 0;
                                  UpdatePosAlignObjects (appWindow, dimension, _horizAligner);
                              }
                              else
                              {
                                  _topMargin = e.Result == CheckState.Checked ? 1 : 0;
                                  UpdatePosAlignObjects (appWindow, dimension, _vertAligner);
                              }
                          };
        appWindow.Add (margin);

        List<Button> addedViews =
        [
            new ()
            {
                X = dimension == Dimension.Width ? Pos.Align (_horizAligner.Alignment) : Pos.Left (alignRadioGroup),
                Y = dimension == Dimension.Width ? Pos.Top (alignRadioGroup) : Pos.Align (_vertAligner.Alignment),
                Text = NumberToWords.Convert (0)
            }
        ];

        NumericUpDown<int> addedViewsUpDown = new()
        {
            Width = 9,
            Title = "Added",
            SchemeName = SchemeManager.SchemesToSchemeName (scheme),
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
                                              if (e.NewValue < e.CurrentValue)
                                              {
                                                  // Remove buttons
                                                  for (int i = e.CurrentValue - 1; i >= e.NewValue; i--)
                                                  {
                                                      Button button = addedViews [i];
                                                      appWindow.Remove (button);
                                                      addedViews.RemoveAt (i);
                                                      button.Dispose ();
                                                  }
                                              }

                                              if (e.NewValue > e.CurrentValue)
                                              {
                                                  // Add buttons
                                                  for (int i = e.CurrentValue; i < e.NewValue; i++)
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

    private void UpdatePosAlignObjects (View superView, Dimension dimension, Aligner aligner)
    {
        foreach (View view in superView.SubViews.Where (v => dimension == Dimension.Width ? v.X is PosAlign : v.Y is PosAlign))
        {
            if (dimension == Dimension.Width ? view.X is PosAlign : view.Y is PosAlign)
            {
                //posAlign.Aligner.Alignment = _horizAligner.Alignment;
                //posAlign.Aligner.AlignmentMode = _horizAligner.AlignmentMode;

                // BUGBUG: Create and assign a new Pos object because we currently have no way for X to be notified
                // BUGBUG: of changes in the Pos object. See https://github.com/gui-cs/Terminal.Gui/issues/3485
                if (dimension == Dimension.Width)
                {
                    var posAlign = view.X as PosAlign;

                    view.X = Pos.Align (
                                        aligner.Alignment,
                                        aligner.AlignmentModes,
                                        posAlign!.GroupId);
                    view.Margin.Thickness = new (_leftMargin, view.Margin.Thickness.Top, view.Margin.Thickness.Right, view.Margin.Thickness.Bottom);
                }
                else
                {
                    var posAlign = view.Y as PosAlign;

                    view.Y = Pos.Align (
                                        aligner.Alignment,
                                        aligner.AlignmentModes,
                                        posAlign!.GroupId);

                    view.Margin.Thickness = new (view.Margin.Thickness.Left, _topMargin, view.Margin.Thickness.Right, view.Margin.Thickness.Bottom);
                }
            }
        }
    }

    /// <summary>
    ///     Creates a 3x3 grid of views with two GroupIds: One for aligning X and one for aligning Y.
    ///     Demonstrates using PosAlign to create a grid of views that flow.
    /// </summary>
    /// <param name="appWindow"></param>
    private void Setup3By3Grid (View appWindow)
    {
        var container = new FrameView
        {
            Title = "3 by 3",
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Width = Dim.Percent (40),
            Height = Dim.Percent (40)
        };
        container.Padding.Thickness = new (8, 1, 0, 0);
        container.Padding.SchemeName = "Error";

        Aligner widthAligner = new () { AlignmentModes = AlignmentModes.StartToEnd };

        RadioGroup widthAlignRadioGroup = new ()
        {
            RadioLabels = Enum.GetNames<Alignment> (),
            Orientation = Orientation.Horizontal,
            X = Pos.Center ()
        };
        container.Padding.Add (widthAlignRadioGroup);

        widthAlignRadioGroup.SelectedItemChanged += (sender, e) =>
                                                    {
                                                        widthAligner.Alignment =
                                                            (Alignment)Enum.Parse (
                                                                                   typeof (Alignment),
                                                                                   widthAlignRadioGroup.RadioLabels [widthAlignRadioGroup.SelectedItem]);
                                                        UpdatePosAlignObjects (container, Dimension.Width, widthAligner);
                                                    };

        Aligner heightAligner = new () { AlignmentModes = AlignmentModes.StartToEnd };

        RadioGroup heightAlignRadioGroup = new ()
        {
            RadioLabels = Enum.GetNames<Alignment> (),
            Orientation = Orientation.Vertical,
            Y = Pos.Center ()
        };
        container.Padding.Add (heightAlignRadioGroup);

        heightAlignRadioGroup.SelectedItemChanged += (sender, e) =>
                                                     {
                                                         heightAligner.Alignment =
                                                             (Alignment)Enum.Parse (
                                                                                    typeof (Alignment),
                                                                                    heightAlignRadioGroup.RadioLabels [heightAlignRadioGroup.SelectedItem]);
                                                         UpdatePosAlignObjects (container, Dimension.Height, heightAligner);
                                                     };

        for (var i = 0; i < 9; i++)

        {
            var v = new View
            {
                Title = $"{i}",
                Text = $"{i}",
                BorderStyle = LineStyle.Dashed,
                Height = Dim.Auto (),
                Width = Dim.Auto() + 2
            };

            v.X = Pos.Align (widthAligner.Alignment, widthAligner.AlignmentModes, i / 3);
            v.Y = Pos.Align (heightAligner.Alignment, heightAligner.AlignmentModes, i % 3 + 10);

            container.Add (v);
        }

        appWindow.Add (container);
    }
}
