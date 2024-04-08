using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("True Colors", "Demonstration of true color support.")]
[ScenarioCategory ("Colors")]
public class TrueColors : Scenario
{
    public override void Setup ()
    {
        var x = 2;
        var y = 1;

        bool canTrueColor = Application.Driver.SupportsTrueColor;

        var lblDriverName = new Label
        {
            X = x, Y = y++, Text = $"Current driver is {Application.Driver.GetType ().Name}"
        };
        Win.Add (lblDriverName);
        y++;

        var cbSupportsTrueColor = new CheckBox
        {
            X = x,
            Y = y++,
            Checked = canTrueColor,
            CanFocus = false,
            Text = "Driver supports true color "
        };
        Win.Add (cbSupportsTrueColor);

        var cbUseTrueColor = new CheckBox
        {
            X = x,
            Y = y++,
            Checked = Application.Force16Colors,
            Enabled = canTrueColor,
            Text = "Force 16 colors"
        };
        cbUseTrueColor.Toggled += (_, evt) => { Application.Force16Colors = evt.NewValue ?? false; };
        Win.Add (cbUseTrueColor);

        y += 2;
        SetupGradient ("Red gradient", x, ref y, i => new Color (i, 0));
        SetupGradient ("Green gradient", x, ref y, i => new Color (0, i));
        SetupGradient ("Blue gradient", x, ref y, i => new Color (0, 0, i));
        SetupGradient ("Yellow gradient", x, ref y, i => new Color (i, i));
        SetupGradient ("Magenta gradient", x, ref y, i => new Color (i, 0, i));
        SetupGradient ("Cyan gradient", x, ref y, i => new Color (0, i, i));
        SetupGradient ("Gray gradient", x, ref y, i => new Color (i, i, i));

        Win.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 2, Text = "Mouse over to get the gradient view color:" }
                );

        Win.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 4, Text = "Red:" }
                );

        Win.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 5, Text = "Green:" }
                );

        Win.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 6, Text = "Blue:" }
                );

        Win.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 8, Text = "Darker:" }
                );

        Win.Add (
                 new Label { X = Pos.AnchorEnd (44), Y = 9, Text = "Lighter:" }
                );

        var lblRed = new Label { X = Pos.AnchorEnd (32), Y = 4, Text = "na" };
        Win.Add (lblRed);
        var lblGreen = new Label { X = Pos.AnchorEnd (32), Y = 5, Text = "na" };
        Win.Add (lblGreen);
        var lblBlue = new Label { X = Pos.AnchorEnd (32), Y = 6, Text = "na" };
        Win.Add (lblBlue);

        var lblDarker = new Label { X = Pos.AnchorEnd (32), Y = 8, Text = "     " };
        Win.Add (lblDarker);

        var lblLighter = new Label { X = Pos.AnchorEnd (32), Y = 9, Text = "    " };
        Win.Add (lblLighter);

        Application.MouseEvent += (s, e) =>
                                  {
                                      if (e.View == null)
                                      {
                                          return;
                                      }
                                      if (e.Flags == MouseFlags.Button1Clicked)
                                      {
                                          Attribute normal = e.View.GetNormalColor ();
                                          
                                          lblLighter.ColorScheme = new ColorScheme(e.View.ColorScheme)
                                          {
                                              Normal = new Attribute (
                                                                      normal.Foreground,
                                                                      normal.Background.GetHighlightColor ()
                                                                     )
                                          };
                                      }
                                      else
                                      {
                                          Attribute normal = e.View.GetNormalColor ();
                                          lblRed.Text = normal.Foreground.R.ToString ();
                                          lblGreen.Text = normal.Foreground.G.ToString ();
                                          lblBlue.Text = normal.Foreground.B.ToString ();
                                      }
                                  };
    }

    private void SetupGradient (string name, int x, ref int y, Func<int, Color> colorFunc)
    {
        var gradient = new Label { X = x, Y = y++, Text = name };
        Win.Add (gradient);

        for (int dx = x, i = 0; i <= 256; i += 4)
        {
            var l = new Label
            {
                X = dx++,
                Y = y,
                ColorScheme = new ColorScheme
                {
                    Normal = new Attribute (
                                            colorFunc (Math.Clamp (i, 0, 255)),
                                            colorFunc (Math.Clamp (i, 0, 255))
                                           )
                },
                Text = " "
            };
            Win.Add (l);
        }

        y += 2;
    }
}
