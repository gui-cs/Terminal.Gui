using System.Collections.ObjectModel;

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
    private const uint TIMER_TICK = 20;
    private Timer _fractionTimer;
    private Timer _pulseTimer;
    private ListView _pbList;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new () { Title = GetQuitKeyAndName (), BorderStyle = LineStyle.Single };

        var editor = new AdornmentsEditor { AutoSelectViewToEdit = false, ShowViewIdentifier = true };
        win.Add (editor);

        View container = new () { X = Pos.Right (editor), Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (container);

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

        var fgColorPickerBtn = new Button { Text = "Foreground HotNormal Color", X = Pos.Center (), Y = Pos.Align (Alignment.Start) };
        container.Add (fgColorPickerBtn);

        fgColorPickerBtn.Accepting += (s, e) =>
                                      {
                                          Color? result =
                                              app.TopRunnable?.Prompt<ColorPicker, Color?> (input: editor.ViewToEdit!.GetAttributeForRole (VisualRole.Normal)
                                                                                                         .Foreground,
                                                                                            beginInitHandler: prompt => { prompt.Title = "Foreground Color"; });

                                          if (result is { } selectedColor)
                                          {
                                              var cs = new Scheme (editor.ViewToEdit.GetScheme ())
                                              {
                                                  Active = new Attribute (selectedColor,
                                                                          editor.ViewToEdit.GetAttributeForRole (VisualRole.Active).Background,
                                                                          editor.ViewToEdit.GetAttributeForRole (VisualRole.Active).Style)
                                              };
                                              editor.ViewToEdit.SetScheme (cs);
                                          }
                                      };

        var bgColorPickerBtn = new Button { X = Pos.Center (), Y = Pos.Align (Alignment.Start), Text = "Background HotNormal Color" };
        container.Add (bgColorPickerBtn);

        bgColorPickerBtn.Accepting += (s, e) =>
                                      {
                                          Color? result =
                                              app.TopRunnable?.Prompt<ColorPicker, Color?> (input: editor.ViewToEdit!.GetAttributeForRole (VisualRole.Normal)
                                                                                                         .Background,
                                                                                            beginInitHandler: prompt => { prompt.Title = "Background Color"; });

                                          if (result is { } selectedColor)
                                          {
                                              var cs = new Scheme (editor.ViewToEdit.GetScheme ())
                                              {
                                                  Active = new Attribute (editor.ViewToEdit.GetAttributeForRole (VisualRole.Active).Foreground,
                                                                          selectedColor,
                                                                          editor.ViewToEdit.GetAttributeForRole (VisualRole.Active).Style)
                                              };
                                              editor.ViewToEdit.SetScheme (cs);
                                          }
                                      };

        #endregion

        List<ProgressBarFormat> pbFormatEnum = Enum.GetValues (typeof (ProgressBarFormat)).Cast<ProgressBarFormat> ().ToList ();

        OptionSelector<ProgressBarFormat> osPbFormat = new ()
        {
            BorderStyle = LineStyle.Single,
            Title = "ProgressBarFormat",
            X = Pos.Center (),
            Y = Pos.Align (Alignment.Start),
            AssignHotKeys = true
        };
        container.Add (osPbFormat);

        var button = new Button { X = Pos.Center (), Y = Pos.Align (Alignment.Start), Text = "Start timer" };
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

        osPbFormat.Value = blocksPB.ProgressBarFormat;

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

        button.Accepting += (s, e) =>
                            {
                                if (_fractionTimer == null)
                                {
                                    //blocksPB.Enabled = false;
                                    blocksPB.Fraction = 0;
                                    continuousPB.Fraction = 0;
                                    float fractionSum = 0;

                                    _fractionTimer = new Timer (_ =>
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
                                                                },
                                                                null,
                                                                0,
                                                                TIMER_TICK);
                                }
                            };

        var ckbBidirectional = new CheckBox
        {
            X = Pos.Center (), Y = Pos.Bottom (continuousPB), Text = "BidirectionalMarquee", CheckedState = CheckState.Checked
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

        _pbList.SetSource (new ObservableCollection<string> (container.SubViews.Where (v => v.GetType () == typeof (ProgressBar))
                                                                      .Select (v => v.Title)
                                                                      .ToList ()));

        _pbList.SelectedItemChanged += (sender, e) =>
                                       {
                                           editor.ViewToEdit =
                                               container.SubViews.First (v => v.GetType () == typeof (ProgressBar) && v.Title == (string)e.Value);
                                       };

        osPbFormat.ValueChanged += (s, e) =>
                                   {
                                       if (e.Value is null)
                                       {
                                           return;
                                       }

                                       blocksPB.ProgressBarFormat = e.Value.Value;
                                       continuousPB.ProgressBarFormat = e.Value.Value;
                                       marqueesBlocksPB.ProgressBarFormat = e.Value.Value;
                                       marqueesContinuousPB.ProgressBarFormat = e.Value.Value;
                                   };

        ckbBidirectional.CheckedStateChanging += (s, e) =>
                                                 {
                                                     marqueesBlocksPB.BidirectionalMarquee =
                                                         marqueesContinuousPB.BidirectionalMarquee = e.Result == CheckState.Checked;
                                                 };

        win.Initialized += Win_Initialized;
        win.IsRunningChanged += Win_IsRunningChanged;

        _pulseTimer = new Timer (_ =>
                                 {
                                     marqueesBlocksPB.Text = marqueesContinuousPB.Text = DateTime.Now.TimeOfDay.ToString ();
                                     marqueesBlocksPB.Pulse ();
                                     marqueesContinuousPB.Pulse ();
                                 },
                                 null,
                                 0,
                                 300);
        app.Run (win);

        return;

        void Win_IsRunningChanged (object sender, EventArgs<bool> args)
        {
            if (args.Value)
            {
                return;
            }

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

            win.IsRunningChanged -= Win_IsRunningChanged;
        }
    }

    private void Win_Initialized (object sender, EventArgs e) => _pbList.SelectedItem = 0;
}
