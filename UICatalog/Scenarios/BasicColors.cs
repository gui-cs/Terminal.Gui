using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Basic Colors", "Show all basic colors.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Text and Formatting")]
public class BasicColors : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
        };

        var vx = 30;
        var x = 30;
        var y = 14;
        Array colors = Enum.GetValues (typeof (ColorName));

        foreach (ColorName bg in colors)
        {
            var attr = new Attribute (bg, colors.Length - 1 - bg);

            var vl = new Label
            {
                X = vx,
                Y = 0,
                Width = 1,
                Height = 13,
                VerticalTextAlignment = VerticalTextAlignment.Bottom,
                ColorScheme = new ColorScheme { Normal = attr },
                Text = bg.ToString (),
                TextDirection = TextDirection.TopBottom_LeftRight
            };
            app.Add (vl);

            var hl = new Label
            {
                X = 15,
                Y = y,
                Width = 13,
                Height = 1,
                TextAlignment = TextAlignment.Right,
                ColorScheme = new ColorScheme { Normal = attr },
                Text = bg.ToString ()
            };
            app.Add (hl);
            vx++;

            foreach (ColorName fg in colors)
            {
                var c = new Attribute (fg, bg);
                var t = x.ToString ();

                var l = new Label
                {
                    ColorScheme = new ColorScheme { Normal = c }, X = x, Y = y, Text = t [^1].ToString ()
                };
                app.Add (l);
                x++;
            }

            x = 30;
            y++;
        }

        app.Add (
                 new Label { X = Pos.AnchorEnd (36), Text = "Mouse over to get the Attribute:" }
                );
        app.Add (new Label { X = Pos.AnchorEnd (35), Y = 2, Text = "Foreground:" });

        var lblForeground = new Label { X = Pos.AnchorEnd (23), Y = 2 };
        app.Add (lblForeground);

        var viewForeground = new View { X = Pos.AnchorEnd (2), Y = 2, ColorScheme = new ColorScheme (), Text = "  " };
        app.Add (viewForeground);

        app.Add (new Label { X = Pos.AnchorEnd (35), Y = 4, Text = "Background:" });

        var lblBackground = new Label { X = Pos.AnchorEnd (23), Y = 4 };
        app.Add (lblBackground);

        var viewBackground = new View { X = Pos.AnchorEnd (2), Y = 4, ColorScheme = new ColorScheme (), Text = "  " };
        app.Add (viewBackground);

        Application.MouseEvent += (s, e) =>
                                  {
                                      if (e.View != null)
                                      {
                                          Color fore = e.View.GetNormalColor ().Foreground;
                                          Color back = e.View.GetNormalColor ().Background;

                                          lblForeground.Text =
                                              $"#{fore.R:X2}{fore.G:X2}{fore.B:X2} {fore.GetClosestNamedColor ()} ";

                                          viewForeground.ColorScheme =
                                              new ColorScheme (viewForeground.ColorScheme) { Normal = new Attribute (fore, fore) };

                                          lblBackground.Text =
                                              $"#{back.R:X2}{back.G:X2}{back.B:X2} {back.GetClosestNamedColor ()} ";

                                          viewBackground.ColorScheme =
                                              new ColorScheme (viewBackground.ColorScheme) { Normal = new Attribute (back, back) };
                                      }
                                  };

        Application.Run (app);
        app.Dispose ();
    }
}
