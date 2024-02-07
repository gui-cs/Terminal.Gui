using System;
using Terminal.Gui;

namespace UICatalog.Scenarios; 

[ScenarioMetadata ("True Colors", "Demonstration of true color support.")]
[ScenarioCategory ("Colors")]
public class TrueColors : Scenario {
    public override void Setup () {
        var x = 2;
        var y = 1;

        bool canTrueColor = Application.Driver.SupportsTrueColor;

        var lblDriverName = new Label ($"Current driver is {Application.Driver.GetType ().Name}") {
                                X = x,
                                Y = y++
                            };
        Win.Add (lblDriverName);
        y++;

        var cbSupportsTrueColor = new CheckBox {
                                                   Text = "Driver supports true color ",
                                                   X = x,
                                                   Y = y++,
                                                   Checked = canTrueColor,
                                                   CanFocus = false
                                               };
        Win.Add (cbSupportsTrueColor);

        var cbUseTrueColor = new CheckBox {
                                              Text = "Force 16 colors",
                                              X = x,
                                              Y = y++,
                                              Checked = Application.Force16Colors,
                                              Enabled = canTrueColor
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
                 new Label {
                               Text = "Mouse over to get the gradient view color:",
                               X = Pos.AnchorEnd (44),
                               Y = 2
                           });
        Win.Add (
                 new Label {
                               Text = "Red:",
                               X = Pos.AnchorEnd (44),
                               Y = 4
                           });
        Win.Add (
                 new Label {
                               Text = "Green:",
                               X = Pos.AnchorEnd (44),
                               Y = 5
                           });
        Win.Add (
                 new Label {
                               Text = "Blue:",
                               X = Pos.AnchorEnd (44),
                               Y = 6
                           });

        var lblRed = new Label {
                                   Text = "na",
                                   X = Pos.AnchorEnd (32),
                                   Y = 4
                               };
        Win.Add (lblRed);
        var lblGreen = new Label {
                                     Text = "na",
                                     X = Pos.AnchorEnd (32),
                                     Y = 5
                                 };
        Win.Add (lblGreen);
        var lblBlue = new Label {
                                    Text = "na",
                                    X = Pos.AnchorEnd (32),
                                    Y = 6
                                };
        Win.Add (lblBlue);

        Application.MouseEvent += (s, e) => {
            if (e.MouseEvent.View != null) {
                Attribute normal = e.MouseEvent.View.GetNormalColor ();
                lblRed.Text = normal.Foreground.R.ToString ();
                lblGreen.Text = normal.Foreground.G.ToString ();
                lblBlue.Text = normal.Foreground.B.ToString ();
            }
        };
    }

    private void SetupGradient (string name, int x, ref int y, Func<int, Color> colorFunc) {
        var gradient = new Label (name) {
                                            X = x,
                                            Y = y++
                                        };
        Win.Add (gradient);
        for (int dx = x, i = 0; i <= 256; i += 4) {
            var l = new Label {
                                  Text = " ",
                                  X = dx++,
                                  Y = y,
                                  ColorScheme = new ColorScheme {
                                                                    Normal = new Attribute (
                                                                         colorFunc (Math.Clamp (i, 0, 255)),
                                                                         colorFunc (Math.Clamp (i, 0, 255))
                                                                        )
                                                                }
                              };
            Win.Add (l);
        }

        y += 2;
    }
}
