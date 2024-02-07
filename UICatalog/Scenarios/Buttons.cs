using System.Collections.Generic;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Buttons", "Demonstrates all sorts of Buttons.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]

[ScenarioMetadata ("Buttons", "Demonstrates all sorts of Buttons.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
public class Buttons : Scenario {
    public override void Setup () {
        // Add a label & text field so we can demo IsDefault
        var editLabel = new Label { X = 0, Y = 0, TabStop = true, Text = "TextField (to demo IsDefault):" };
        Win.Add (editLabel);

        // Add a TextField using Absolute layout. 
        var edit = new TextField {
                                     X = 31,
                                     Width = 15,
                                     HotKey = Key.Y.WithAlt
                                 };
        Win.Add (edit);
    public override void Setup () {
        // Add a label & text field so we can demo IsDefault
        var editLabel = new Label {
                                      Text = "TextField (to demo IsDefault):",
                                      X = 0,
                                      Y = 0,
                                      TabStop = true
                                  };
        Win.Add (editLabel);

        // Add a TextField using Absolute layout. 
        var edit = new TextField (31, 0, 15, "") {
                                                     HotKey = Key.Y.WithAlt
                                                 };
        Win.Add (edit);

        // This is the default button (IsDefault = true); if user presses ENTER in the TextField
        // the scenario will quit
        var defaultButton = new Button {
                                           Text = "_Quit",
                                           X = Pos.Center (),

                                           //TODO: Change to use Pos.AnchorEnd()
                                           Y = Pos.Bottom (Win) - 3,
                                           IsDefault = true
                                       };
        defaultButton.Clicked += (s, e) => Application.RequestStop ();
        Win.Add (defaultButton);

        var swapButton = new Button (50, 0, "S_wap Default (Absolute Layout)");
        swapButton.Clicked += (s, e) => {
            defaultButton.IsDefault = !defaultButton.IsDefault;
            swapButton.IsDefault = !swapButton.IsDefault;
        };
        Win.Add (swapButton);

        static void DoMessage (Button button, string txt) {
            button.Clicked += (s, e) => {
                string btnText = button.Text;
                MessageBox.Query ("Message", $"Did you click {txt}?", "Yes", "No");
            };
        }
        static void DoMessage (Button button, string txt) {
            button.Clicked += (s, e) => {
                string btnText = button.Text;
                MessageBox.Query ("Message", $"Did you click {txt}?", "Yes", "No");
            };
        }

        var colorButtonsLabel = new Label {
                                              Text = "Color Buttons:",
                                              X = 0,
                                              Y = Pos.Bottom (editLabel) + 1
                                          };
        Win.Add (colorButtonsLabel);

        //View prev = colorButtonsLabel;

        //With this method there is no need to call Application.TopReady += () => Application.TopRedraw (Top.Bounds);
        Pos x = Pos.Right (colorButtonsLabel) + 2;
        foreach (KeyValuePair<string, ColorScheme> colorScheme in Colors.ColorSchemes) {
            var colorButton = new Button ($"_{colorScheme.Key}") {
                                                                     ColorScheme = colorScheme.Value,

                                                                     //X = Pos.Right (prev) + 2,
                                                                     X = x,
                                                                     Y = Pos.Y (colorButtonsLabel)
                                                                 };
            DoMessage (colorButton, colorButton.Text);
            Win.Add (colorButton);

            //prev = colorButton;
            x += colorButton.Frame.Width + 2;
        }

        Button button;
        Win.Add (
                 button = new Button {
                                         Text =
                                             "A super l_öng Button that will probably expose a bug in clipping or wrapping of text. Will it?",
                                         X = 2,
                                         Y = Pos.Bottom (colorButtonsLabel) + 1
                                     });
        DoMessage (button, button.Text);

        // Note the 'N' in 'Newline' will be the hotkey
        Win.Add (
                 button = new Button {
                                         Text = "a Newline\nin the button",
                                         X = 2,
                                         Y = Pos.Bottom (button) + 1
                                     });
        button.Clicked += (s, e) => MessageBox.Query ("Message", "Question?", "Yes", "No");

        var textChanger = new Button {
                                         Text = "Te_xt Changer",
                                         X = 2,
                                         Y = Pos.Bottom (button) + 1
                                     };
        Win.Add (textChanger);
        textChanger.Clicked += (s, e) => textChanger.Text += "!";

        Win.Add (
                 button = new Button {
                                         Text = "Lets see if this will move as \"Text Changer\" grows",
                                         X = Pos.Right (textChanger) + 2,
                                         Y = Pos.Y (textChanger)
                                     });

        var removeButton = new Button {
                                          Text = "Remove this button",
                                          X = 2,
                                          Y = Pos.Bottom (button) + 1,
                                          ColorScheme = Colors.ColorSchemes["Error"]
                                      };
        Win.Add (removeButton);

        // This in interesting test case because `moveBtn` and below are laid out relative to this one!
        removeButton.Clicked += (s, e) => {
            // Now this throw a InvalidOperationException on the TopologicalSort method as is expected.
            //Win.Remove (removeButton);

            removeButton.Visible = false;
        };
            removeButton.Visible = false;
        };

        var computedFrame = new FrameView {
                                              Title = "Computed Layout",
                                              X = 0,
                                              Y = Pos.Bottom (removeButton) + 1,
                                              Width = Dim.Percent (50),
                                              Height = 5
                                          };
        Win.Add (computedFrame);

        // Demonstrates how changing the View.Frame property can move Views
        var moveBtn = new Button {
                                     Text = "Move This \u263b Button v_ia Pos",
                                     AutoSize = false,
                                     X = 0,
                                     Y = Pos.Center () - 1,
                                     Width = 30,
                                     Height = 1,
                                     ColorScheme = Colors.ColorSchemes["Error"]
                                 };
        moveBtn.Clicked += (s, e) => {
            moveBtn.X = moveBtn.Frame.X + 5;

            // This is already fixed with the call to SetNeedDisplay() in the Pos Dim.
            //computedFrame.LayoutSubviews (); // BUGBUG: This call should not be needed. View.X is not causing relayout correctly
        };
        computedFrame.Add (moveBtn);

        // Demonstrates how changing the View.Frame property can SIZE Views (#583)
        var sizeBtn = new Button {
                                     Text = "Size This \u263a Button _via Pos",
                                     AutoSize = false,
                                     X = 0,
                                     Y = Pos.Center () + 1,
                                     Width = 30,
                                     Height = 1,
                                     ColorScheme = Colors.ColorSchemes["Error"]
                                 };
        sizeBtn.Clicked += (s, e) => {
            sizeBtn.Width = sizeBtn.Frame.Width + 5;

            //computedFrame.LayoutSubviews (); // FIXED: This call should not be needed. View.X is not causing relayout correctly
        };
        computedFrame.Add (sizeBtn);

        var absoluteFrame = new FrameView {
                                              Title = "Absolute Layout",
                                              X = Pos.Right (computedFrame),
                                              Y = Pos.Bottom (removeButton) + 1,
                                              Width = Dim.Fill (),
                                              Height = 5
                                          };
        Win.Add (absoluteFrame);

        // Demonstrates how changing the View.Frame property can move Views
        var moveBtnA = new Button (0, 0, "Move This Button via Frame") {
                                                                           ColorScheme = Colors.ColorSchemes["Error"]
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
        var sizeBtnA = new Button (0, 2, " ~  s  gui.cs   master ↑_10 = Сохранить") {
                           ColorScheme = Colors.ColorSchemes["Error"]
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
                                  Text = "Text Alignment (changes the four buttons above): ",
                                  X = 2,
                                  Y = Pos.Bottom (computedFrame) + 1
                              };
        Win.Add (label);

        var radioGroup = new RadioGroup (new[] { "Left", "Right", "Centered", "Justified" }) {
                             X = 4,
                             Y = Pos.Bottom (label) + 1,
                             SelectedItem = 2
                         };
        Win.Add (radioGroup);

        // Demo changing hotkey
        string MoveHotkey (string txt) {
            // Remove the '_'
            List<Rune> runes = txt.ToRuneList ();
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
            int i = runes.IndexOf ((Rune)'_');
            var start = "";
            if (i > -1) {
                start = StringExtensions.ToString (runes.GetRange (0, i));
            }

            txt = start + StringExtensions.ToString (runes.GetRange (i + 1, runes.Count - (i + 1)));

            runes = txt.ToRuneList ();
            runes = txt.ToRuneList ();

            // Move over one or go to start
            i++;
            if (i >= runes.Count) {
                i = 0;
            }
            // Move over one or go to start
            i++;
            if (i >= runes.Count) {
                i = 0;
            }

            // Slip in the '_'
            start = StringExtensions.ToString (runes.GetRange (0, i));

            return start + '_' + StringExtensions.ToString (runes.GetRange (i, runes.Count - i));
        }
            // Slip in the '_'
            start = StringExtensions.ToString (runes.GetRange (0, i));

            return start + '_' + StringExtensions.ToString (runes.GetRange (i, runes.Count - i));
        }

        var mhkb = "Click to Change th_is Button's Hotkey";
        var moveHotKeyBtn = new Button (mhkb) {
                                                  AutoSize = false,
                                                  X = 2,
                                                  Y = Pos.Bottom (radioGroup) + 1,
                                                  Height = 1,
                                                  Width = Dim.Width (computedFrame) - 2,
                                                  ColorScheme = Colors.ColorSchemes["TopLevel"]
                                              };
        moveHotKeyBtn.Clicked += (s, e) => { moveHotKeyBtn.Text = MoveHotkey (moveHotKeyBtn.Text); };
        Win.Add (moveHotKeyBtn);

        var muhkb = " ~  s  gui.cs   master ↑10 = Сохранить";
        var moveUnicodeHotKeyBtn = new Button (muhkb) {
                                                          AutoSize = false,
                                                          X = Pos.Left (absoluteFrame) + 1,
                                                          Y = Pos.Bottom (radioGroup) + 1,
                                                          Width = Dim.Width (absoluteFrame)
                                                                  - 2, // BUGBUG: Not always the width isn't calculated correctly.
                                                          Height = 1,
                                                          ColorScheme = Colors.ColorSchemes["TopLevel"]
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
        Application.Top.Ready += (s, e) => radioGroup.Refresh ();
    }
}
