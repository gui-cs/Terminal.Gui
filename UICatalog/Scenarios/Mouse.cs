using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Mouse", "Demonstrates how to capture mouse events")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Mouse : Scenario
{
    public override void Setup ()
    {
        Label ml;
        var count = 0;
        ml = new Label { X = 1, Y = 1, Text = "Mouse: " };
        List<string> rme = new ();

        Win.Add (ml);

        var logList = new ListView
        {
            X = Pos.AnchorEnd (41),
            Y = 0,
            Width = 41,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper (rme)
        };
        Win.Add (logList);

        Application.MouseEvent += (sender, a) =>
                                  {
                                      ml.Text = $"Mouse: ({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count}";
                                      rme.Add ($"({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count++}");
                                      logList.MoveDown ();
                                  };

        Win.Add (new MouseDemo ()
        {
            X = 0,
            Y = 3,
            Width = 15,
            Height = 10,
            Text = "Mouse Demo",
            TextAlignment = TextAlignment.Centered,
            VerticalTextAlignment = VerticalTextAlignment.Middle,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
        });

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
