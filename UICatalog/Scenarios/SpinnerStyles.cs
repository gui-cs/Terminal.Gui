using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios; 

[ScenarioMetadata ("SpinnerView Styles", "Shows the SpinnerView Styles.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Progress")]
public class SpinnerViewStyles : Scenario {
    public override void Setup () {
        const int DEFAULT_DELAY = 130;
        const string DEFAULT_CUSTOM = @"-\|/";
        Dictionary<int, KeyValuePair<string, Type>> styleDict = new Dictionary<int, KeyValuePair<string, Type>> ();
        var i = 0;
        foreach (Type style in typeof (SpinnerStyle).GetNestedTypes ()) {
            styleDict.Add (i, new KeyValuePair<string, Type> (style.Name, style));
            i++;
        }

        var preview = new View {
                                   X = Pos.Center (),
                                   Y = 0,
                                   Width = 22,
                                   Height = 3,

                                   //Title = "Preview",
                                   BorderStyle = LineStyle.Single
                               };
        Win.Add (preview);

        var spinner = new SpinnerView {
                                          X = Pos.Center (),
                                          Y = 0
                                      };
        preview.Add (spinner);
        spinner.AutoSpin = true;

        var ckbAscii = new CheckBox ("Ascii Only") {
                                                       X = Pos.Center () - 7,
                                                       Y = Pos.Bottom (preview),
                                                       Enabled = false,
                                                       Checked = true
                                                   };
        Win.Add (ckbAscii);

        var ckbNoSpecial = new CheckBox ("No Special") {
                                                           X = Pos.Center () + 7,
                                                           Y = Pos.Bottom (preview),
                                                           Enabled = false,
                                                           Checked = true
                                                       };
        Win.Add (ckbNoSpecial);

        var ckbReverse = new CheckBox ("Reverse") {
                                                      X = Pos.Center () - 22,
                                                      Y = Pos.Bottom (preview) + 1,
                                                      Checked = false
                                                  };
        Win.Add (ckbReverse);

        var ckbBounce = new CheckBox ("Bounce") {
                                                    X = Pos.Right (ckbReverse) + 2,
                                                    Y = Pos.Bottom (preview) + 1,
                                                    Checked = false
                                                };
        Win.Add (ckbBounce);

        var delayLabel = new Label {
                                       Text = "Delay:",
                                       X = Pos.Right (ckbBounce) + 2,
                                       Y = Pos.Bottom (preview) + 1
                                   };
        Win.Add (delayLabel);
        var delayField = new TextField (DEFAULT_DELAY.ToString ()) {
                                                                       X = Pos.Right (delayLabel),
                                                                       Y = Pos.Bottom (preview) + 1,
                                                                       Width = 5
                                                                   };
        Win.Add (delayField);
        delayField.TextChanged += (s, e) => {
            if (ushort.TryParse (delayField.Text, out ushort i)) {
                spinner.SpinDelay = i;
            }
        };

        var customLabel = new Label {
                                        Text = "Custom:",
                                        X = Pos.Right (delayField) + 2,
                                        Y = Pos.Bottom (preview) + 1
                                    };
        Win.Add (customLabel);
        var customField = new TextField (DEFAULT_CUSTOM) {
                                                             X = Pos.Right (customLabel),
                                                             Y = Pos.Bottom (preview) + 1,
                                                             Width = 12
                                                         };
        Win.Add (customField);

        string[] styleArray = styleDict.Select (e => e.Value.Key).ToArray ();
        if (styleArray.Length < 1) {
            return;
        }

        var styles = new ListView {
                                      X = Pos.Center (),
                                      Y = Pos.Bottom (preview) + 2,
                                      Height = Dim.Fill (),
                                      Width = Dim.Fill (1)
                                  };
        styles.SetSource (styleArray);
        styles.SelectedItem = 0; // SpinnerStyle.Custom;
        Win.Add (styles);
        SetCustom ();

        customField.TextChanged += (s, e) => {
            if (customField.Text.Length > 0) {
                if (styles.SelectedItem != 0) {
                    styles.SelectedItem = 0; // SpinnerStyle.Custom
                }

                SetCustom ();
            }
        };

        styles.SelectedItemChanged += (s, e) => {
            if (e.Item == 0) { // SpinnerStyle.Custom
                if (customField.Text.Length < 1) {
                    customField.Text = DEFAULT_CUSTOM;
                }

                if (delayField.Text.Length < 1) {
                    delayField.Text = DEFAULT_DELAY.ToString ();
                }

                SetCustom ();
            } else {
                spinner.Visible = true;
                spinner.Style = (SpinnerStyle)Activator.CreateInstance (styleDict[e.Item].Value);
                delayField.Text = spinner.SpinDelay.ToString ();
                ckbBounce.Checked = spinner.SpinBounce;
                ckbNoSpecial.Checked = !spinner.HasSpecialCharacters;
                ckbAscii.Checked = spinner.IsAsciiOnly;
                ckbReverse.Checked = false;
            }
        };

        ckbReverse.Toggled += (s, e) => { spinner.SpinReverse = (bool)!e.OldValue; };

        ckbBounce.Toggled += (s, e) => { spinner.SpinBounce = (bool)!e.OldValue; };

        Application.Top.Unloaded += Top_Unloaded;

        void SetCustom () {
            if (customField.Text.Length > 0) {
                spinner.Visible = true;
                if (ushort.TryParse (delayField.Text, out ushort d)) {
                    spinner.SpinDelay = d;
                } else {
                    delayField.Text = DEFAULT_DELAY.ToString ();
                    spinner.SpinDelay = DEFAULT_DELAY;
                }

                List<string> str = new List<string> ();
                foreach (char c in customField.Text.ToCharArray ()) {
                    str.Add (c.ToString ());
                }

                spinner.Sequence = str.ToArray ();
            } else {
                spinner.Visible = false;
            }
        }

        void Top_Unloaded (object sender, EventArgs args) {
            if (spinner != null) {
                spinner.Dispose ();
                spinner = null;
            }

            Application.Top.Unloaded -= Top_Unloaded;
        }
    }

    private class Property {
        public string Name { get; set; }
    }
}
