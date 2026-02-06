using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UICatalog.Scenarios;

// 
// This would be a great scenario to show of threading (Issue #471)
//
[ScenarioMetadata ("Progress", "Shows off ProgressBar and Threading.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Progress")]
public class Progress : Scenario
{
    private Window _win;
    private uint _mainLoopTimeoutTick = 100; // ms
    private object _mainLoopTimeout;
    private Timer _systemTimer;
    private uint _systemTimerTick = 100; // ms
    private IApplication _app;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window win = new () { Title = GetQuitKeyAndName () };
        _win = win;
        // Demo #1 - Use System.Timer (and threading)
        var systemTimerDemo = new ProgressDemo
        {
            X = 0, Y = 0, Width = Dim.Percent (100), Title = "System.Timer (threads)"
        };

        systemTimerDemo.StartBtnClick = () =>
                                        {
                                            _systemTimer?.Dispose ();
                                            _systemTimer = null;

                                            systemTimerDemo.ActivityProgressBar.Fraction = 0F;
                                            systemTimerDemo.PulseProgressBar.Fraction = 0F;

                                            _systemTimer = new Timer (
                                                                      _ =>
                                                                      {
                                                                          // Note the check for Mainloop being valid. System.Timers can run after they are Disposed.
                                                                          // This code must be defensive for that.
                                                                          _app?.Invoke (_ => systemTimerDemo.Pulse ());
                                                                      },
                                                                      null,
                                                                      0,
                                                                      _systemTimerTick
                                                                     );
                                        };

        systemTimerDemo.StopBtnClick = () =>
                                       {
                                           _systemTimer?.Dispose ();
                                           _systemTimer = null;

                                           systemTimerDemo.ActivityProgressBar.Fraction = 1F;
                                           systemTimerDemo.PulseProgressBar.Fraction = 1F;
                                       };
        systemTimerDemo.Speed.Text = $"{_systemTimerTick}";

        systemTimerDemo.Speed.TextChanged += (_, _) =>
                                             {
                                                 if (uint.TryParse (systemTimerDemo.Speed.Text, out uint result))
                                                 {
                                                     _systemTimerTick = result;
                                                     Debug.WriteLine ($"{_systemTimerTick}");

                                                     if (systemTimerDemo.Started)
                                                     {
                                                         systemTimerDemo.Start ();
                                                     }
                                                 }
                                                 else
                                                 {
                                                     Debug.WriteLine ("bad entry");
                                                 }
                                             };
        _win.Add (systemTimerDemo);

        // Demo #2 - Use Application.AddTimeout (no threads)
        var mainLoopTimeoutDemo = new ProgressDemo
        {
            X = 0,
            Y = Pos.Bottom (systemTimerDemo),
            Width = Dim.Percent (100),
            Title = "Application.AddTimer (no threads)"
        };

        mainLoopTimeoutDemo.StartBtnClick = () =>
                                            {
                                                mainLoopTimeoutDemo.StopBtnClick ();

                                                mainLoopTimeoutDemo.ActivityProgressBar.Fraction = 0F;
                                                mainLoopTimeoutDemo.PulseProgressBar.Fraction = 0F;

                                                _mainLoopTimeout = _app?.AddTimeout (
                                                                                           TimeSpan.FromMilliseconds (_mainLoopTimeoutTick),
                                                                                           () =>
                                                                                           {
                                                                                               mainLoopTimeoutDemo.Pulse ();

                                                                                               return true;
                                                                                           }
                                                                                          );
                                            };

        mainLoopTimeoutDemo.StopBtnClick = () =>
                                           {
                                               if (_mainLoopTimeout != null)
                                               {
                                                   _app?.RemoveTimeout (_mainLoopTimeout);
                                                   _mainLoopTimeout = null;
                                               }

                                               mainLoopTimeoutDemo.ActivityProgressBar.Fraction = 1F;
                                               mainLoopTimeoutDemo.PulseProgressBar.Fraction = 1F;
                                           };

        mainLoopTimeoutDemo.Speed.Text = $"{_mainLoopTimeoutTick}";

        mainLoopTimeoutDemo.Speed.TextChanged += (_, _) =>
                                                 {
                                                     if (uint.TryParse (mainLoopTimeoutDemo.Speed.Text, out uint result))
                                                     {
                                                         _mainLoopTimeoutTick = result;

                                                         if (mainLoopTimeoutDemo.Started)
                                                         {
                                                             mainLoopTimeoutDemo.Start ();
                                                         }
                                                     }
                                                 };
        _win.Add (mainLoopTimeoutDemo);

        var startBoth = new Button { X = Pos.Center (), Y = Pos.Bottom (mainLoopTimeoutDemo) + 1, Text = "Start Both" };

        startBoth.Accepting += (_, _) =>
                             {
                                 systemTimerDemo.Start ();
                                 mainLoopTimeoutDemo.Start ();
                             };
        _win.Add (startBoth);

        app.Run (_win);
    }

    protected override void Dispose (bool disposing)
    {
        foreach (ProgressDemo v in _win.SubViews.OfType<ProgressDemo> ())
        {
            v.StopBtnClick ();
        }

        base.Dispose (disposing);
    }

    private class ProgressDemo : FrameView
    {
        private const int VerticalSpace = 1;
        internal readonly Action PulseBtnClick = null;
        internal Action StartBtnClick;
        internal Action StopBtnClick;

        private readonly Label _startedLabel;

        internal ProgressDemo ()
        {
            SchemeName = "Dialog";

            LeftFrame = new FrameView
            {
                X = 0,
                Y = 0,
                Height = Dim.Percent (100),
                Width = Dim.Percent (25),
                Title = "Settings"
            };
            Label lbl = new () { X = 1, Y = 1, Text = "Tick every (ms):" };
            LeftFrame.Add (lbl);
            Speed = new TextField { X = Pos.X (lbl), Y = Pos.Bottom (lbl), Width = 7 };
            LeftFrame.Add (Speed);

            Add (LeftFrame);

            var startButton = new Button { X = Pos.Right (LeftFrame) + 1, Y = 0, Text = "Start Timer" };
            startButton.Accepting += (s, e) => Start ();
            var pulseButton = new Button { X = Pos.Right (startButton) + 2, Y = Pos.Y (startButton), Text = "Pulse" };
            pulseButton.Accepting += (s, e) => Pulse ();

            var stopbutton = new Button
            {
                X = Pos.Right (pulseButton) + 2, Y = Pos.Top (pulseButton), Text = "Stop Timer"
            };
            stopbutton.Accepting += (s, e) => Stop ();

            Add (startButton);
            Add (pulseButton);
            Add (stopbutton);

            ActivityProgressBar = new ProgressBar
            {
                X = Pos.Right (LeftFrame) + 1,
                Y = Pos.Bottom (startButton) + 1,
                Width = Dim.Fill () - 1,
                Height = 1,
                Fraction = 0.25F,
                SchemeName = "Error"
            };
            Add (ActivityProgressBar);

            Spinner = new SpinnerView
            {
                Style = new SpinnerStyle.Dots2 (), SpinReverse = true, Y = ActivityProgressBar.Y, Visible = false
            };
            ActivityProgressBar.Width = Dim.Fill () - Spinner.Width;
            Spinner.X = Pos.Right (ActivityProgressBar);

            Add (Spinner);

            PulseProgressBar = new ProgressBar
            {
                X = Pos.Right (LeftFrame) + 1,
                Y = Pos.Bottom (ActivityProgressBar) + 1,
                Width = Dim.Fill () - Spinner.Width,
                Height = 1,
                SchemeName = "Error"
            };
            Add (PulseProgressBar);

            _startedLabel = new Label
            {
                X = Pos.Right (LeftFrame) + 1, Y = Pos.Bottom (PulseProgressBar), Text = "Stopped"
            };
            Add (_startedLabel);

            // TODO: Great use of Dim.Auto
            Initialized += (s, e) =>
                           {
                               // Set height to height of controls + spacing + frame
                               Height = 2
                                        + VerticalSpace
                                        + startButton.Frame.Height
                                        + VerticalSpace
                                        + ActivityProgressBar.Frame.Height
                                        + VerticalSpace
                                        + PulseProgressBar.Frame.Height
                                        + VerticalSpace;
                           };
        }

        internal ProgressBar ActivityProgressBar { get; }
        internal FrameView LeftFrame { get; }
        internal ProgressBar PulseProgressBar { get; }
        internal TextField Speed { get; }
        internal SpinnerView Spinner { get; }

        internal bool Started
        {
            get => _startedLabel.Text == "Started";
            private set => _startedLabel.Text = value ? "Started" : "Stopped";
        }

        internal void Pulse ()
        {
            Spinner.Visible = true;

            if (PulseBtnClick != null)
            {
                PulseBtnClick?.Invoke ();
            }
            else
            {
                if (ActivityProgressBar.Fraction + 0.01F >= 1)
                {
                    ActivityProgressBar.Fraction = 0F;
                }
                else
                {
                    ActivityProgressBar.Fraction += 0.01F;
                }

                PulseProgressBar.Pulse ();
                Spinner.AdvanceAnimation ();
            }
        }

        internal void Start ()
        {
            Started = true;
            StartBtnClick?.Invoke ();

            App?.Invoke (
                                () =>
                                {
                                    Spinner.Visible = true;
                                    ActivityProgressBar.Width = Dim.Fill () - Spinner.Width;
                                }
                               );
        }

        internal void Stop ()
        {
            Started = false;
            StopBtnClick?.Invoke ();

            App?.Invoke (
                                () =>
                                {
                                    Spinner.Visible = false;
                                    ActivityProgressBar.Width = Dim.Fill () - Spinner.Width;
                                }
                               );
        }
    }
}
