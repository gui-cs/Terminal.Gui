﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Terminal.Gui;
using static UICatalog.Scenarios.Adornments;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ProgressBar Styles", "Shows the ProgressBar Styles.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Progress")]
[ScenarioCategory ("Threading")]

// TODO: Add enable/disable to show that that is working
// TODO: Clean up how FramesEditor works 
// TODO: Better align rpPBFormat
public class ProgressBarStyles : Scenario
{
    private const uint _timerTick = 20;
    private Timer _fractionTimer;
    private Timer _pulseTimer;
    private ListView _pbList;

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (), BorderStyle = LineStyle.Single,
        };

        var editor = new AdornmentsEditor ()
        {
            AutoSelectViewToEdit = false
        };
        app.Add (editor);

        View container = new ()
        {
            X = Pos.Right (editor),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
        };
        app.Add (container);

        const float fractionStep = 0.01F;

        _pbList = new ListView
        {
            Title = "Focused ProgressBar",
            Y = Pos.Align (Alignment.Start),
            X = Pos.Center (),
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            BorderStyle = LineStyle.Single
        };
        container.Add (_pbList);

        #region ColorPicker

        ColorName ChooseColor (string text, ColorName colorName)
        {
            var colorPicker = new ColorPicker { Title = text, SelectedColor = colorName };

            var dialog = new Dialog { Title = text };

            dialog.Initialized += (sender, args) =>
                                     {
                                         // TODO: Replace with Dim.Auto
                                         dialog.X = _pbList.Frame.X;
                                         dialog.Y = _pbList.Frame.Height;
                                     };

            dialog.LayoutComplete += (sender, args) =>
                                    {
                                        dialog.Viewport = Rectangle.Empty with
                                        {
                                            Width = colorPicker.Frame.Width,
                                            Height = colorPicker.Frame.Height
                                        };
                                        Application.Top.LayoutSubviews ();
                                    };

            dialog.Add (colorPicker);
            colorPicker.ColorChanged += (s, e) => { dialog.RequestStop (); };
            Application.Run (dialog);
            dialog.Dispose ();

            ColorName retColor = colorPicker.SelectedColor;
            colorPicker.Dispose ();

            return retColor;
        }

        var fgColorPickerBtn = new Button
        {
            Text = "Foreground HotNormal Color",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
        };
        container.Add (fgColorPickerBtn);

        fgColorPickerBtn.Accept += (s, e) =>
                                    {
                                        ColorName newColor = ChooseColor (
                                                                          fgColorPickerBtn.Text,
                                                                          editor.ViewToEdit.ColorScheme.HotNormal.Foreground
                                                                                .GetClosestNamedColor ()
                                                                         );

                                        var cs = new ColorScheme (editor.ViewToEdit.ColorScheme)
                                        {
                                            HotNormal = new Attribute (
                                                                       newColor,
                                                                       editor.ViewToEdit.ColorScheme.HotNormal
                                                                             .Background
                                                                      )
                                        };
                                        editor.ViewToEdit.ColorScheme = cs;
                                    };

        var bgColorPickerBtn = new Button
        {
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Text = "Background HotNormal Color"
        };
        container.Add (bgColorPickerBtn);

        bgColorPickerBtn.Accept += (s, e) =>
                                    {
                                        ColorName newColor = ChooseColor (
                                                                          fgColorPickerBtn.Text,
                                                                          editor.ViewToEdit.ColorScheme.HotNormal.Background
                                                                                .GetClosestNamedColor ()
                                                                         );

                                        var cs = new ColorScheme (editor.ViewToEdit.ColorScheme)
                                        {
                                            HotNormal = new Attribute (
                                                                       editor.ViewToEdit.ColorScheme.HotNormal
                                                                             .Foreground,
                                                                       newColor
                                                                      )
                                        };
                                        editor.ViewToEdit.ColorScheme = cs;
                                    };

        #endregion

        List<ProgressBarFormat> pbFormatEnum =
            Enum.GetValues (typeof (ProgressBarFormat)).Cast<ProgressBarFormat> ().ToList ();

        var rbPBFormat = new RadioGroup
        {
            BorderStyle = LineStyle.Single,
            Title = "ProgressBarFormat",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            RadioLabels = pbFormatEnum.Select (e => e.ToString ()).ToArray ()
        };
        container.Add (rbPBFormat);

        var button = new Button
        {
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Text = "Start timer"
        };
        container.Add (button);

        var blocksPB = new ProgressBar
        {
            Title = "Blocks",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single,
            CanFocus = true
        };
        container.Add (blocksPB);

        rbPBFormat.SelectedItem = (int)blocksPB.ProgressBarFormat;

        var continuousPB = new ProgressBar
        {
            Title = "Continuous",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Width = Dim.Percent (50),
            ProgressBarStyle = ProgressBarStyle.Continuous,
            BorderStyle = LineStyle.Single,
            CanFocus = true
        };
        container.Add (continuousPB);

        button.Accept += (s, e) =>
                          {
                              if (_fractionTimer == null)
                              {
                                  //blocksPB.Enabled = false;
                                  blocksPB.Fraction = 0;
                                  continuousPB.Fraction = 0;
                                  float fractionSum = 0;

                                  _fractionTimer = new Timer (
                                                              _ =>
                                                              {
                                                                  fractionSum += fractionStep;
                                                                  blocksPB.Fraction = fractionSum;
                                                                  continuousPB.Fraction = fractionSum;

                                                                  if (fractionSum > 1)
                                                                  {
                                                                      _fractionTimer.Dispose ();
                                                                      _fractionTimer = null;
                                                                      button.Enabled = true;
                                                                  }

                                                                  Application.Wakeup ();
                                                              },
                                                              null,
                                                              0,
                                                              _timerTick
                                                             );
                              }
                          };

        var ckbBidirectional = new CheckBox
        {
            X = Pos.Center (),
            Y = Pos.Bottom (continuousPB),
            Text = "BidirectionalMarquee", 
            CheckedState = CheckState.Checked
        };
        container.Add (ckbBidirectional);

        var marqueesBlocksPB = new ProgressBar
        {
            Title = "Marquee Blocks",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Width = Dim.Percent (50),
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks,
            BorderStyle = LineStyle.Single,
            CanFocus = true
        };
        container.Add (marqueesBlocksPB);

        var marqueesContinuousPB = new ProgressBar
        {
            Title = "Marquee Continuous",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            Width = Dim.Percent (50),
            ProgressBarStyle = ProgressBarStyle.MarqueeContinuous,
            BorderStyle = LineStyle.Single,
            CanFocus = true
        };
        container.Add (marqueesContinuousPB);

        _pbList.SetSource (
                          new ObservableCollection<string> (
                                                            container.Subviews.Where (v => v.GetType () == typeof (ProgressBar))
                                                                     .Select (v => v.Title)
                                                                     .ToList ())
                         );

        _pbList.SelectedItemChanged += (sender, e) =>
                                      {
                                          editor.ViewToEdit = container.Subviews.First (
                                                                                        v =>
                                                                                            v.GetType () == typeof (ProgressBar)
                                                                                            && v.Title == (string)e.Value
                                                                                       );
                                      };


        rbPBFormat.SelectedItemChanged += (s, e) =>
                                          {
                                              blocksPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
                                              continuousPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
                                              marqueesBlocksPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
                                              marqueesContinuousPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
                                          };

        ckbBidirectional.CheckedStateChanging += (s, e) =>
                                   {
                                       ckbBidirectional.CheckedState = e.NewValue;
                                       marqueesBlocksPB.BidirectionalMarquee =
                                                                  marqueesContinuousPB.BidirectionalMarquee = e.NewValue == CheckState.Checked;
                                   };



        app.Initialized += App_Initialized;
        app.Unloaded += App_Unloaded;

        _pulseTimer = new Timer (
                                 _ =>
                                 {
                                     marqueesBlocksPB.Text = marqueesContinuousPB.Text = DateTime.Now.TimeOfDay.ToString ();
                                     marqueesBlocksPB.Pulse ();
                                     marqueesContinuousPB.Pulse ();
                                     Application.Wakeup ();
                                 },
                                 null,
                                 0,
                                 300
                                );
        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();

        return;

        void App_Unloaded (object sender, EventArgs args)
        {
            if (_fractionTimer != null)
            {
                _fractionTimer.Dispose ();
                _fractionTimer = null;
            }

            if (_pulseTimer != null)
            {
                _pulseTimer.Dispose ();
                _pulseTimer = null;
            }

            app.Unloaded -= App_Unloaded;
        }
    }

    private void App_Initialized (object sender, EventArgs e)
    {
        _pbList.SelectedItem = 0;
    }
}
