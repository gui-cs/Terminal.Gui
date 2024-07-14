﻿using System;
using System.Collections.ObjectModel;
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
            filterSlider.SetOption (i);
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
        cbWantContinuousPresses.Toggle += (s, e) => { win.WantContinuousButtonPressed = !win.WantContinuousButtonPressed; };

        win.Add (cbWantContinuousPresses);

        CheckBox cbHighlightOnPress = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbWantContinuousPresses),
            Title = "_Highlight on Press"
        };
        cbHighlightOnPress.State = win.HighlightStyle == (HighlightStyle.Pressed | HighlightStyle.PressedOutside) ? CheckState.Checked : CheckState.UnChecked;

        cbHighlightOnPress.Toggle += (s, e) =>
                                      {
                                          if (e.NewValue == CheckState.Checked)
                                          {
                                              win.HighlightStyle = HighlightStyle.Pressed | HighlightStyle.PressedOutside;
                                          }
                                          else
                                          {
                                              win.HighlightStyle = HighlightStyle.None;
                                          }
                                      };

        win.Add (cbHighlightOnPress);

        var demo = new MouseDemo
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbHighlightOnPress),
            Width = 20,
            Height = 3,
            Text = "Enter/Leave Demo",
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.Center,
            ColorScheme = Colors.ColorSchemes ["Dialog"]
        };
        win.Add (demo);

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

        clearButton.Accept += (s, e) =>
                              {
                                  appLogList.Clear ();
                                  appLog.SetSource (appLogList);
                                  winLogList.Clear ();
                                  winLog.SetSource (winLogList);
                              };

        win.MouseEvent += (sender, a) =>
                          {
                              int i = filterSlider.Options.FindIndex (o => o.Data == a.MouseEvent.Flags);

                              if (filterSlider.GetSetOptions ().Contains (i))
                              {
                                  winLogList.Add ($"MouseEvent: ({a.MouseEvent.Position}) - {a.MouseEvent.Flags} {count++}");
                                  winLog.MoveDown ();
                              }
                          };

        win.MouseClick += (sender, a) =>
                          {
                              winLogList.Add ($"MouseClick: ({a.MouseEvent.Position}) - {a.MouseEvent.Flags} {count++}");
                              winLog.MoveDown ();
                          };

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }

    public class MouseDemo : View
    {
        private bool _button1PressedOnEnter;

        public MouseDemo ()
        {
            CanFocus = true;

            MouseEvent += (s, e) =>
                          {
                              if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
                              {
                                  if (!_button1PressedOnEnter)
                                  {
                                      ColorScheme = Colors.ColorSchemes ["Toplevel"];
                                  }
                              }

                              if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Released))
                              {
                                  ColorScheme = Colors.ColorSchemes ["Dialog"];
                                  _button1PressedOnEnter = false;
                              }
                          };

            MouseLeave += (s, e) =>
                          {
                              ColorScheme = Colors.ColorSchemes ["Dialog"];
                              _button1PressedOnEnter = false;
                          };
            MouseEnter += (s, e) => { _button1PressedOnEnter = e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed); };
        }
    }
}
