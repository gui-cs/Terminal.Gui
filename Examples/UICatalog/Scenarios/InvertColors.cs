using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Invert Colors", "Invert the foreground and the background colors.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Text and Formatting")]
public class InvertColors : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var win = new Window
        {
            Title = GetQuitKeyAndName (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"]
        };

        List<Label> labels = new ();
        ColorName16 [] foreColors = Enum.GetValues (typeof (ColorName16)).Cast<ColorName16> ().ToArray ();

        for (var y = 0; y < foreColors.Length; y++)
        {
            ColorName16 fore = foreColors [y];
            ColorName16 back = foreColors [(y + 1) % foreColors.Length];
            var color = new Attribute (fore, back);

            var label = new Label { ColorScheme = new ColorScheme (), Y = y, Text = $"{fore} on {back}" };
            label.ColorScheme = new ColorScheme (label.ColorScheme) { Normal = color };
            win.Add (label);
            labels.Add (label);
        }

        var button = new Button { X = Pos.Center (), Y = foreColors.Length + 1, Text = "Invert color!" };

        button.Accepting += (s, e) =>
                          {
                              foreach (Label label in labels)
                              {
                                  Attribute color = label.ColorScheme.Normal;
                                  color = new Attribute (color.Background, color.Foreground);

                                  label.ColorScheme = new ColorScheme (label.ColorScheme) { Normal = color };
                                  label.Text = $"{color.Foreground} on {color.Background}";
                                  label.SetNeedsDraw ();
                              }
                          };
        win.Add (button);

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }
}
