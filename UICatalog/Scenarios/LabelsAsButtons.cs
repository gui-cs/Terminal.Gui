using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Labels As Buttons", "Illustrates that Button is really just a Label++")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Proof of Concept")]
public class LabelsAsLabels : Scenario {
    public override void Setup () {
        // Add a label & text field so we can demo IsDefault
        var editLabel = new Label { X = 0, Y = 0, Text = "TextField (to demo IsDefault):" };
        Win.Add (editLabel);

        // Add a TextField using Absolute layout. 
        var edit = new TextField { X = 31, Width = 15 };
        Win.Add (edit);

        // This is the default Label (IsDefault = true); if user presses ENTER in the TextField
        // the scenario will quit
        var defaultLabel = new Label {
                                         X = Pos.Center (),
                                         Y = Pos.Bottom (Win) - 3,
                                         HotKeySpecifier = (Rune)'_',
                                         CanFocus = true,
                                         Text = "_Quit"
                                     };
        defaultLabel.Clicked += (s, e) => Application.RequestStop ();
        Win.Add (defaultLabel);

        var swapLabel = new Label {
                                      X = 50,
                                      HotKeySpecifier = (Rune)'_',
                                      CanFocus = true,
                                      Text = "S_wap Default (Absolute Layout)"
                                  };
        swapLabel.Clicked += (s, e) => {
            //defaultLabel.IsDefault = !defaultLabel.IsDefault;
            //swapLabel.IsDefault = !swapLabel.IsDefault;
        };
        Win.Add (swapLabel);

        static void DoMessage (Label Label, string txt) {
            Label.Clicked += (s, e) => {
                string btnText = Label.Text;
                MessageBox.Query ("Message", $"Did you click {txt}?", "Yes", "No");
            };
        }

        var colorLabelsLabel = new Label { X = 0, Y = Pos.Bottom (editLabel) + 1, Text = "Color Labels:" };
        Win.Add (colorLabelsLabel);

        //With this method there is no need to call Application.TopReady += () => Application.TopRedraw (Top.Bounds);
        Pos x = Pos.Right (colorLabelsLabel) + 2;
        foreach (KeyValuePair<string, ColorScheme> colorScheme in Colors.ColorSchemes) {
            var colorLabel = new Label {
                                           ColorScheme = colorScheme.Value,
                                           X = x,
                                           Y = Pos.Y (colorLabelsLabel),
                                           HotKeySpecifier = (Rune)'_',
                                           CanFocus = true,
                                           Text = $"{colorScheme.Key}"
                                       };
            DoMessage (colorLabel, colorLabel.Text);
            Win.Add (colorLabel);
            x += colorLabel.Text.Length + 2;
        }

        Application.Top.Ready += (s, e) => Application.Top.Draw ();

        Label Label;
        Win.Add (
                 Label = new Label {
                                       X = 2,
                                       Y = Pos.Bottom (colorLabelsLabel) + 1,
                                       HotKeySpecifier = (Rune)'_',
                                       CanFocus = true,
                                       Text =
                                           "A super long _Label that will probably expose a bug in clipping or wrapping of text. Will it?"
                                   });
        DoMessage (Label, Label.Text);

        // Note the 'N' in 'Newline' will be the hotkey
        Win.Add (
                 Label = new Label {
                                       X = 2,
                                       Y = Pos.Bottom (Label) + 1,
                                       HotKeySpecifier = (Rune)'_',
                                       CanFocus = true,
                                       TextAlignment = TextAlignment.Centered,
                                       VerticalTextAlignment = VerticalTextAlignment.Middle,
                                       Text = "a Newline\nin the Label"
                                   });
        Label.Clicked += (s, e) => MessageBox.Query ("Message", "Question?", "Yes", "No");

        var textChanger = new Label {
                                        X = 2,
                                        Y = Pos.Bottom (Label) + 1,
                                        HotKeySpecifier = (Rune)'_',
                                        CanFocus = true,
                                        Text = "Te_xt Changer"
                                    };
        Win.Add (textChanger);
        textChanger.Clicked += (s, e) => textChanger.Text += "!";

        Win.Add (
                 Label = new Label {
                                       X = Pos.Right (textChanger) + 2,
                                       Y = Pos.Y (textChanger),
                                       HotKeySpecifier = (Rune)'_',
                                       CanFocus = true,
                                       Text = "Lets see if this will move as \"Text Changer\" grows"
                                   });

        var removeLabel = new Label {
                                        X = 2,
                                        Y = Pos.Bottom (Label) + 1,
                                        ColorScheme = Colors.ColorSchemes["Error"],
                                        HotKeySpecifier = (Rune)'_',
                                        CanFocus = true,
                                        Text = "Remove this Label"
                                    };
        Win.Add (removeLabel);

        // This in interesting test case because `moveBtn` and below are laid out relative to this one!
        removeLabel.Clicked += (s, e) => {
            // Now this throw a InvalidOperationException on the TopologicalSort method as is expected.
            //Win.Remove (removeLabel);

            removeLabel.Visible = false;
            Win.SetNeedsDisplay ();
        };

        var computedFrame = new FrameView {
                                              X = 0,
                                              Y = Pos.Bottom (removeLabel) + 1,
                                              Width = Dim.Percent (50),
                                              Height = 5,
                                              Title = "Computed Layout"
                                          };
        Win.Add (computedFrame);

        // Demonstrates how changing the View.Frame property can move Views
        var moveBtn = new Label {
                                    X = 0,
                                    Y = Pos.Center () - 1,
                                    Width = 30,
                                    ColorScheme = Colors.ColorSchemes["Error"],
                                    HotKeySpecifier = (Rune)'_',
                                    CanFocus = true,
                                    Text = "Move This \u263b Label _via Pos"
                                };
        moveBtn.Clicked += (s, e) => {
            moveBtn.X = moveBtn.Frame.X + 5;

            // This is already fixed with the call to SetNeedDisplay() in the Pos Dim.
            //computedFrame.LayoutSubviews (); // BUGBUG: This call should not be needed. View.X is not causing relayout correctly
        };
        computedFrame.Add (moveBtn);

        // Demonstrates how changing the View.Frame property can SIZE Views (#583)
        var sizeBtn = new Label {
                                    X = 0,
                                    Y = Pos.Center () + 1,
                                    Width = 30,
                                    ColorScheme = Colors.ColorSchemes["Error"],
                                    HotKeySpecifier = (Rune)'_',
                                    CanFocus = true,
                                    AutoSize = false,
                                    Text = "Size This \u263a Label _via Pos"
                                };
        sizeBtn.Clicked += (s, e) => {
            sizeBtn.Width = sizeBtn.Frame.Width + 5;

            //computedFrame.LayoutSubviews (); // FIXED: This call should not be needed. View.X is not causing relayout correctly
        };
        computedFrame.Add (sizeBtn);

        var absoluteFrame = new FrameView {
                                              X = Pos.Right (computedFrame),
                                              Y = Pos.Bottom (removeLabel) + 1,
                                              Width = Dim.Fill (),
                                              Height = 5,
                                              Title = "Absolute Layout"
                                          };
        Win.Add (absoluteFrame);

        // Demonstrates how changing the View.Frame property can move Views
        var moveBtnA = new Label {
                                     ColorScheme = Colors.ColorSchemes["Error"],
                                     HotKeySpecifier = (Rune)'_',
                                     CanFocus = true,
                                     Text = "Move This Label via Frame"
                                 };
        moveBtnA.Clicked += (s, e) => {
            moveBtnA.Frame = new Rect (
                                       moveBtnA.Frame.X + 5,
                                       moveBtnA.Frame.Y,
                                       moveBtnA.Frame.Width,
                                       moveBtnA.Frame.Height);
        };
        absoluteFrame.Add (moveBtnA);

        // Demonstrates how changing the View.Frame property can SIZE Views (#583)
        var sizeBtnA = new Label {
                                     Y = 2,
                                     ColorScheme = Colors.ColorSchemes["Error"],
                                     HotKeySpecifier = (Rune)'_',
                                     CanFocus = true,
                                     AutoSize = false,
                                     Text = " ~  s  gui.cs   master ↑10 = Со_хранить"
                                 };
        sizeBtnA.Clicked += (s, e) => {
            sizeBtnA.Frame = new Rect (
                                       sizeBtnA.Frame.X,
                                       sizeBtnA.Frame.Y,
                                       sizeBtnA.Frame.Width + 5,
                                       sizeBtnA.Frame.Height);
        };
        absoluteFrame.Add (sizeBtnA);

        var label = new Label {
                                  X = 2,
                                  Y = Pos.Bottom (computedFrame) + 1,
                                  HotKeySpecifier = (Rune)'_',
                                  CanFocus = true,
                                  Text = "Text Alignment (changes the four Labels above): "
                              };
        Win.Add (label);

        var radioGroup = new RadioGroup {
                                            X = 4,
                                            Y = Pos.Bottom (label) + 1,
                                            SelectedItem = 2,
                                            RadioLabels = new[] { "Left", "Right", "Centered", "Justified" }
                                        };
        Win.Add (radioGroup);

        // Demo changing hotkey
        string MoveHotkey (string txt) {
            // Remove the '_'
            List<Rune> runes = txt.ToRuneList ();

            int i = runes.IndexOf ((Rune)'_');
            var start = "";
            if (i > -1) {
                start = StringExtensions.ToString (runes.GetRange (0, i));
            }

            txt = start + StringExtensions.ToString (runes.GetRange (i + 1, runes.Count - (i + 1)));

            runes = txt.ToRuneList ();

            // Move over one or go to start
            i++;
            if (i >= runes.Count) {
                i = 0;
            }

            // Slip in the '_'
            start = StringExtensions.ToString (runes.GetRange (0, i));

            return start + '_' + StringExtensions.ToString (runes.GetRange (i, runes.Count - i));
        }

        var mhkb = "Click to Change th_is Label's Hotkey";
        var moveHotKeyBtn = new Label {
                                          X = 2,
                                          Y = Pos.Bottom (radioGroup) + 1,
                                          Width = Dim.Width (computedFrame) - 2,
                                          ColorScheme = Colors.ColorSchemes["TopLevel"],
                                          HotKeySpecifier = (Rune)'_',
                                          CanFocus = true,
                                          Text = mhkb
                                      };
        moveHotKeyBtn.Clicked += (s, e) => { moveHotKeyBtn.Text = MoveHotkey (moveHotKeyBtn.Text); };
        Win.Add (moveHotKeyBtn);

        var muhkb = " ~  s  gui.cs   master ↑10 = Сохранить";
        var moveUnicodeHotKeyBtn = new Label {
                                                 X = Pos.Left (absoluteFrame) + 1,
                                                 Y = Pos.Bottom (radioGroup) + 1,
                                                 Width = Dim.Width (absoluteFrame) - 2,
                                                 ColorScheme = Colors.ColorSchemes["TopLevel"],
                                                 HotKeySpecifier = (Rune)'_',
                                                 CanFocus = true,
                                                 Text = muhkb
                                             };
        moveUnicodeHotKeyBtn.Clicked += (s, e) => {
            moveUnicodeHotKeyBtn.Text = MoveHotkey (moveUnicodeHotKeyBtn.Text);
        };
        Win.Add (moveUnicodeHotKeyBtn);

        radioGroup.SelectedItemChanged += (s, args) => {
            switch (args.SelectedItem) {
                case 0:
                    moveBtn.TextAlignment = TextAlignment.Left;
                    sizeBtn.TextAlignment = TextAlignment.Left;
                    moveBtnA.TextAlignment = TextAlignment.Left;
                    sizeBtnA.TextAlignment = TextAlignment.Left;
                    moveHotKeyBtn.TextAlignment = TextAlignment.Left;
                    moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Left;

                    break;
                case 1:
                    moveBtn.TextAlignment = TextAlignment.Right;
                    sizeBtn.TextAlignment = TextAlignment.Right;
                    moveBtnA.TextAlignment = TextAlignment.Right;
                    sizeBtnA.TextAlignment = TextAlignment.Right;
                    moveHotKeyBtn.TextAlignment = TextAlignment.Right;
                    moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Right;

                    break;
                case 2:
                    moveBtn.TextAlignment = TextAlignment.Centered;
                    sizeBtn.TextAlignment = TextAlignment.Centered;
                    moveBtnA.TextAlignment = TextAlignment.Centered;
                    sizeBtnA.TextAlignment = TextAlignment.Centered;
                    moveHotKeyBtn.TextAlignment = TextAlignment.Centered;
                    moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Centered;

                    break;
                case 3:
                    moveBtn.TextAlignment = TextAlignment.Justified;
                    sizeBtn.TextAlignment = TextAlignment.Justified;
                    moveBtnA.TextAlignment = TextAlignment.Justified;
                    sizeBtnA.TextAlignment = TextAlignment.Justified;
                    moveHotKeyBtn.TextAlignment = TextAlignment.Justified;
                    moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Justified;

                    break;
            }
        };

        Application.Top.Ready += (s, e) => radioGroup.Refresh ();
    }
}
