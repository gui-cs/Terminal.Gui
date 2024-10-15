using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Mouse", "Demonstrates how to capture mouse events")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Mouse : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window win = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        Slider<MouseFlags> filterSlider = new ()
        {
            Title = "_Filter",
            X = 0,
            Y = 0,
            BorderStyle = LineStyle.Single,
            Type = SliderType.Multiple,
            Orientation = Orientation.Vertical,
            UseMinimumSize = true,
            MinimumInnerSpacing = 0
        };

        filterSlider.Options = Enum.GetValues (typeof (MouseFlags))
                                   .Cast<MouseFlags> ()
                                   .Where (value => !value.ToString ().Contains ("None") && !value.ToString ().Contains ("All"))
                                   .Select (
                                            value => new SliderOption<MouseFlags>
                                            {
                                                Legend = value.ToString (),
                                                Data = value
                                            })
                                   .ToList ();

        for (var i = 0; i < filterSlider.Options.Count; i++)
        {
            if (filterSlider.Options [i].Data != MouseFlags.ReportMousePosition)
            {
                filterSlider.SetOption (i);
            }
        }

        win.Add (filterSlider);

        var clearButton = new Button
        {
            Title = "_Clear Logs",
            X = 1,
            Y = Pos.Bottom (filterSlider) + 1
        };
        win.Add (clearButton);
        Label ml;
        var count = 0;
        ml = new () { X = Pos.Right (filterSlider), Y = 0, Text = "Mouse: " };

        win.Add (ml);

        CheckBox cbWantContinuousPresses = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (ml),
            Title = "_Want Continuous Button Pressed"
        };

        win.Add (cbWantContinuousPresses);

        CheckBox cbHighlightOnPress = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbWantContinuousPresses),
            Title = "_Highlight on Press"
        };

        win.Add (cbHighlightOnPress);

        var demo = new MouseEventDemoView
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbHighlightOnPress),
            Width = Dim.Fill (),
            Height = 15,
            Title = "Enter/Leave Demo",
        };

        demo.Padding.Initialized += DemoPaddingOnInitialized;

        void DemoPaddingOnInitialized (object o, EventArgs eventArgs)
        {
            demo.Padding.Add (
                              new MouseEventDemoView ()
                              {
                                  X = 0,
                                  Y = 0,
                                  Width = Dim.Fill (),
                                  Height = Dim.Func (() => demo.Padding.Thickness.Top),
                                  Title = "inPadding",
                                  Id = "inPadding"
                              });
            demo.Padding.Thickness = demo.Padding.Thickness with { Top = 5 };
        }

        View sub1 = new MouseEventDemoView ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent (20),
            Height = Dim.Fill (),
            Title = "sub1",
            Id = "sub1",
        };
        demo.Add (sub1);

        demo.Add (
                  new MouseEventDemoView ()
                  {
                      X = Pos.Right (sub1) - 4,
                      Y = Pos.Top (sub1) + 1,
                      Width = Dim.Percent (20),
                      Height = Dim.Fill (1),
                      Title = "sub2",
                      Id = "sub2",
                  });

        win.Add (demo);

        cbHighlightOnPress.CheckedState = demo.HighlightStyle == (HighlightStyle.Pressed | HighlightStyle.PressedOutside) ? CheckState.Checked : CheckState.UnChecked;

        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/3753
        cbHighlightOnPress.CheckedStateChanging += (s, e) =>
                                                   {
                                                       if (e.NewValue == CheckState.Checked)
                                                       {
                                                           demo.HighlightStyle = HighlightStyle.Pressed | HighlightStyle.PressedOutside;
                                                       }
                                                       else
                                                       {
                                                           demo.HighlightStyle = HighlightStyle.None;
                                                       }

                                                       foreach (View subview in demo.Subviews)
                                                       {
                                                           if (e.NewValue == CheckState.Checked)
                                                           {
                                                               subview.HighlightStyle = HighlightStyle.Pressed | HighlightStyle.PressedOutside;
                                                           }
                                                           else
                                                           {
                                                               subview.HighlightStyle = HighlightStyle.None;
                                                           }
                                                       }

                                                       foreach (View subview in demo.Padding.Subviews)
                                                       {
                                                           if (e.NewValue == CheckState.Checked)
                                                           {
                                                               subview.HighlightStyle = HighlightStyle.Pressed | HighlightStyle.PressedOutside;
                                                           }
                                                           else
                                                           {
                                                               subview.HighlightStyle = HighlightStyle.None;
                                                           }
                                                       }

                                                   };

        cbWantContinuousPresses.CheckedStateChanging += (s, e) =>
                                                        {
                                                            demo.WantContinuousButtonPressed = !demo.WantContinuousButtonPressed;

                                                            foreach (View subview in demo.Subviews)
                                                            {
                                                                subview.WantContinuousButtonPressed = demo.WantContinuousButtonPressed;
                                                            }

                                                            foreach (View subview in demo.Padding.Subviews)
                                                            {
                                                                subview.WantContinuousButtonPressed = demo.WantContinuousButtonPressed;
                                                            }

                                                        };


        var label = new Label
        {
            Text = "_App Events:",
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (demo)
        };

        ObservableCollection<string> appLogList = new ();

        var appLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = 50,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper<string> (appLogList)
        };
        win.Add (label, appLog);

        Application.MouseEvent += (sender, a) =>
                                  {
                                      int i = filterSlider.Options.FindIndex (o => o.Data == a.Flags);

                                      if (filterSlider.GetSetOptions ().Contains (i))
                                      {
                                          ml.Text = $"MouseEvent: ({a.Position}) - {a.Flags} {count}";
                                          appLogList.Add ($"({a.Position}) - {a.Flags} {count++}");
                                          appLog.MoveDown ();
                                      }
                                  };

        label = new ()
        {
            Text = "_Window Events:",
            X = Pos.Right (appLog) + 1,
            Y = Pos.Top (label)
        };
        ObservableCollection<string> winLogList = new ();

        var winLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper<string> (winLogList)
        };
        win.Add (label, winLog);

        clearButton.Accepting += (s, e) =>
                              {
                                  appLogList.Clear ();
                                  appLog.SetSource (appLogList);
                                  winLogList.Clear ();
                                  winLog.SetSource (winLogList);
                              };

        win.MouseEvent += (sender, a) =>
                          {
                              int i = filterSlider.Options.FindIndex (o => o.Data == a.Flags);

                              if (filterSlider.GetSetOptions ().Contains (i))
                              {
                                  winLogList.Add ($"MouseEvent: ({a.Position}) - {a.Flags} {count++}");
                                  winLog.MoveDown ();
                              }
                          };

        win.MouseClick += (sender, a) =>
                          {
                              winLogList.Add ($"MouseClick: ({a.Position}) - {a.Flags} {count++}");
                              winLog.MoveDown ();
                          };

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }

    public class MouseEventDemoView : View
    {
        public MouseEventDemoView ()
        {
            CanFocus = true;
            Id = "mouseEventDemoView";

            Padding.Thickness = new Thickness (1, 1, 1, 1);

            Initialized += OnInitialized;

            void OnInitialized (object sender, EventArgs e)
            {
                TextAlignment = Alignment.Center;
                VerticalTextAlignment = Alignment.Center;

                Padding.ColorScheme = new ColorScheme (new Attribute (Color.Black));

                Padding.MouseEnter += PaddingOnMouseEnter;
                Padding.MouseLeave += PaddingOnMouseLeave;

                void PaddingOnMouseEnter (object o, CancelEventArgs e)
                {
                    Padding.ColorScheme = Colors.ColorSchemes ["Error"];
                }

                void PaddingOnMouseLeave (object o, EventArgs e)
                {
                    Padding.ColorScheme = Colors.ColorSchemes ["Dialog"];
                }

                Border.Thickness = new Thickness (1);
                Border.LineStyle = LineStyle.Rounded;
            }

            MouseLeave += (s, e) =>
                          {
                              Text = "Leave";
                          };
            MouseEnter += (s, e) =>
                          {
                              Text = "Enter";
                          };
        }
    }
}
