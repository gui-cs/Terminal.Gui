using System;
using System.Collections.Generic;
using System.CommandLine;
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
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
        };


        var filterSlider = new Slider<MouseFlags> ()
        {
            Title = "_Filter",
            X = 0,
            Y = 0,
            AutoSize = true,
            BorderStyle = LineStyle.Single,
            Type = SliderType.Multiple,
            Orientation = Orientation.Vertical,
        };
        filterSlider.Options = Enum.GetValues (typeof (MouseFlags))
                                   .Cast<MouseFlags> ()
                                   .Where (value => !value.ToString ().Contains ("None") && 
                                                    !value.ToString().Contains("All"))
                                   .Select (value => new SliderOption<MouseFlags>
                                   {
                                       Legend = value.ToString (),
                                       Data = value,
                                   })
                                   .ToList ();
        for (int i = 0; i < filterSlider.Options.Count; i++)
        {
            filterSlider.SetOption (i);
        }
        win.Add (filterSlider);

        var clearButton = new Button ()
        {
            Title = "_Clear Logs",
            X = 1,
            Y = Pos.Bottom (filterSlider) + 1,
        };
        win.Add (clearButton);
        Label ml;
        var count = 0;
        ml = new Label { X = Pos.Right(filterSlider), Y = 0, Text = "Mouse: " };

        win.Add (ml);

        CheckBox cbWantContinuousPresses = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (ml),
            Title = "_Want Continuous Button Pressed",
        };
        cbWantContinuousPresses.Toggled += (s, e) =>
        {
            win.WantContinuousButtonPressed = !win.WantContinuousButtonPressed;
        };

        win.Add (cbWantContinuousPresses);
        CheckBox cbHighlightOnPress = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbWantContinuousPresses),
            Title = "_Highlight on Press",
        };
        cbHighlightOnPress.Checked = win.HighlightStyle == (HighlightStyle.Pressed | HighlightStyle.PressedOutside);
        cbHighlightOnPress.Toggled += (s, e) =>
                                           {
                                               if (e.NewValue == true)
                                               {
                                                   win.HighlightStyle = HighlightStyle.Pressed | HighlightStyle.PressedOutside;
                                               }
                                               else
                                               {
                                                   win.HighlightStyle = HighlightStyle.None;
                                               }
                                           };

        win.Add (cbHighlightOnPress);

        var demo = new MouseDemo ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbHighlightOnPress),
            Width = 20,
            Height = 3,
            Text = "Enter/Leave Demo",
            TextAlignment = TextAlignment.Centered,
            VerticalTextAlignment = VerticalTextAlignment.Middle,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
        };
        win.Add (demo);

        var label = new Label ()
        {
            Text = "_App Events:",
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (demo),
        };

        List<string> appLogList = new ();
        var appLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = 50,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper (appLogList)
        };
        win.Add (label, appLog);

        Application.MouseEvent += (sender, a) =>
                                  {
                                      var i = filterSlider.Options.FindIndex (o => o.Data == a.Flags);
                                      if (filterSlider.GetSetOptions().Contains(i))
                                      {
                                          ml.Text = $"MouseEvent: ({a.X},{a.Y}) - {a.Flags} {count}";
                                          appLogList.Add ($"({a.X},{a.Y}) - {a.Flags} {count++}");
                                          appLog.MoveDown ();
                                      }
                                  };

        label = new Label ()
        {
            Text = "_Window Events:",
            X = Pos.Right (appLog)+1,
                          Y = Pos.Top (label),
        };
        List<string> winLogList = new ();
        var winLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper (winLogList)
        };
        win.Add (label, winLog);

        clearButton.Accept += (s, e) =>
                              {
                                  appLogList.Clear ();
                                  appLog.SetSource (appLogList);
                                  winLogList.Clear ();
                                  winLog.SetSource(winLogList);
                              };

        win.MouseEvent += (sender, a) =>
                          {
                              var i = filterSlider.Options.FindIndex (o => o.Data == a.MouseEvent.Flags);
                              if (filterSlider.GetSetOptions ().Contains (i))
                              {
                                  winLogList.Add ($"MouseEvent: ({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count++}");
                                  winLog.MoveDown ();
                              }
                          };
        win.MouseClick += (sender, a) =>
                          {
                              winLogList.Add ($"MouseClick: ({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count++}");
                              winLog.MoveDown ();
                          };

        Application.Run (win);
        win.Dispose ();
    }

    public class MouseDemo : View
    {
        private bool _button1PressedOnEnter = false;
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
            MouseEnter += (s, e) =>
                          {
                              _button1PressedOnEnter = e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed);
                          };
        }
    }
}
