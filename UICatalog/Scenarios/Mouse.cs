using System.Collections.Generic;
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

        Label ml;
        var count = 0;
        ml = new Label { X = 1, Y = 1, Text = "Mouse: " };

        win.Add (ml);

        CheckBox cbWantContinuousPresses = new CheckBox ()
        {
            X = 0,
            Y = Pos.Bottom(ml) + 1,
            Title = "_Want Continuous Button Presses",
        };
        cbWantContinuousPresses.Toggled += (s,e) =>
        {
            win.WantContinuousButtonPressed = !win.WantContinuousButtonPressed;
        };

        win.Add (cbWantContinuousPresses);

        var demo = new MouseDemo ()
        {
            X = 0,
            Y = Pos.Bottom (cbWantContinuousPresses) + 1,
            Width = 20,
            Height = 5,
            Text = "Enter/Leave Demo",
            TextAlignment = TextAlignment.Centered,
            VerticalTextAlignment = VerticalTextAlignment.Middle,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
        };
        win.Add (demo);

        var label = new Label ()
        {
            Text = "_App Events:",
            X = 0,
            Y = Pos.Bottom (demo),
        };
        List<string> appLogList = new ();
        var appLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Percent(49),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper (appLogList)
        };
        win.Add (label, appLog);

        Application.MouseEvent += (sender, a) =>
                                  {
                                      ml.Text = $"MouseEvent: ({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count}";
                                      appLogList.Add ($"({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count++}");
                                      appLog.MoveDown ();
                                  };


        label = new Label ()
        {
            Text = "_Window Events:",
            X = Pos.Percent(50),
            Y = Pos.Bottom (demo),
        };
        List<string> winLogList = new ();
        var winLog = new ListView
        {
            X = Pos.Left(label),
            Y = Pos.Bottom (label),
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper (winLogList)
        };
        win.Add (label, winLog);
        win.MouseEvent += (sender, a) =>
                          {
                              winLogList.Add ($"MouseEvent: ({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count++}");
                              winLog.MoveDown ();
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
