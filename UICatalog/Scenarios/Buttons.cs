using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Buttons", "Demonstrates all sorts of Buttons.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
public class Buttons : Scenario
{
    public override void Setup ()
    {
        // Add a label & text field so we can demo IsDefault
        var editLabel = new Label { X = 0, Y = 0, TabStop = true, Text = "TextField (to demo IsDefault):" };
        Win.Add (editLabel);

        // Add a TextField using Absolute layout. 
        var edit = new TextField { X = 31, Width = 15, HotKey = Key.Y.WithAlt };
        Win.Add (edit);

        // This is the default button (IsDefault = true); if user presses ENTER in the TextField
        // the scenario will quit
        var defaultButton = new Button { X = Pos.Center (), Y = Pos.AnchorEnd (1), IsDefault = true, Text = "_Quit" };
        defaultButton.Accept += (s, e) => Application.RequestStop ();
        Win.Add (defaultButton);

        var swapButton = new Button { X = 50, Text = "S_wap Default (Absolute Layout)" };

        swapButton.Accept += (s, e) =>
                              {
                                  defaultButton.IsDefault = !defaultButton.IsDefault;
                                  swapButton.IsDefault = !swapButton.IsDefault;
                              };
        Win.Add (swapButton);

        static void DoMessage (Button button, string txt)
        {
            button.Accept += (s, e) =>
                              {
                                  string btnText = button.Text;
                                  MessageBox.Query ("Message", $"Did you click {txt}?", "Yes", "No");
                              };
        }

        var colorButtonsLabel = new Label { X = 0, Y = Pos.Bottom (editLabel) + 1, Text = "Color Buttons:" };
        Win.Add (colorButtonsLabel);

        View prev = colorButtonsLabel;

        //With this method there is no need to call Application.TopReady += () => Application.TopRedraw (Top.Bounds);
        Pos x = Pos.Right (colorButtonsLabel) + 2;

        foreach (KeyValuePair<string, ColorScheme> colorScheme in Colors.ColorSchemes)
        {
            var colorButton = new Button
            {
                ColorScheme = colorScheme.Value,
                X = Pos.Right (prev) + 2,

                //X = x,
                Y = Pos.Y (colorButtonsLabel),
                Text = $"_{colorScheme.Key}"
            };
            DoMessage (colorButton, colorButton.Text);
            Win.Add (colorButton);
            prev = colorButton;

            // BUGBUG: AutoSize is true and the X doesn't change
            //x += colorButton.Frame.Width + 2;
        }

        Button button;

        Win.Add (
                 button = new Button
                 {
                     X = 2,
                     Y = Pos.Bottom (colorButtonsLabel) + 1,
                     Text =
                         "A super l_öng Button that will probably expose a bug in clipping or wrapping of text. Will it?"
                 }
                );
        DoMessage (button, button.Text);

        // Note the 'N' in 'Newline' will be the hotkey
        Win.Add (
                 button = new Button { X = 2, Y = Pos.Bottom (button) + 1, Text = "a Newline\nin the button" }
                );
        button.Accept += (s, e) => MessageBox.Query ("Message", "Question?", "Yes", "No");

        var textChanger = new Button { X = 2, Y = Pos.Bottom (button) + 1, Text = "Te_xt Changer" };
        Win.Add (textChanger);
        textChanger.Accept += (s, e) => textChanger.Text += "!";

        Win.Add (
                 button = new Button
                 {
                     X = Pos.Right (textChanger) + 2,
                     Y = Pos.Y (textChanger),
                     Text = "Lets see if this will move as \"Text Changer\" grows"
                 }
                );

        var removeButton = new Button
        {
            X = 2, Y = Pos.Bottom (button) + 1, ColorScheme = Colors.ColorSchemes ["Error"], Text = "Remove this button"
        };
        Win.Add (removeButton);

        // This in interesting test case because `moveBtn` and below are laid out relative to this one!
        removeButton.Accept += (s, e) =>
                                {
                                    // Now this throw a InvalidOperationException on the TopologicalSort method as is expected.
                                    //Win.Remove (removeButton);

                                    removeButton.Visible = false;
                                };

        var computedFrame = new FrameView
        {
            X = 0,
            Y = Pos.Bottom (removeButton) + 1,
            Width = Dim.Percent (50),
            Height = 5,
            Title = "Computed Layout"
        };
        Win.Add (computedFrame);

        // Demonstrates how changing the View.Frame property can move Views
        var moveBtn = new Button
        {
            X = 0,
            Y = Pos.Center () - 1,
            AutoSize = false,
            Width = 30,
            Height = 1,
            ColorScheme = Colors.ColorSchemes ["Error"],
            Text = "Move This \u263b Button v_ia Pos"
        };

        moveBtn.Accept += (s, e) =>
                           {
                               moveBtn.X = moveBtn.Frame.X + 5;

                               // This is already fixed with the call to SetNeedDisplay() in the Pos Dim.
                               //computedFrame.LayoutSubviews (); // BUGBUG: This call should not be needed. View.X is not causing relayout correctly
                           };
        computedFrame.Add (moveBtn);

        // Demonstrates how changing the View.Frame property can SIZE Views (#583)
        var sizeBtn = new Button
        {
            X = 0,
            Y = Pos.Center () + 1,
            AutoSize = false,
            Width = 30,
            Height = 1,
            ColorScheme = Colors.ColorSchemes ["Error"],
            Text = "Size This \u263a Button _via Pos"
        };

        sizeBtn.Accept += (s, e) =>
                           {
                               sizeBtn.Width = sizeBtn.Frame.Width + 5;

                               //computedFrame.LayoutSubviews (); // FIXED: This call should not be needed. View.X is not causing relayout correctly
                           };
        computedFrame.Add (sizeBtn);

        var absoluteFrame = new FrameView
        {
            X = Pos.Right (computedFrame),
            Y = Pos.Bottom (removeButton) + 1,
            Width = Dim.Fill (),
            Height = 5,
            Title = "Absolute Layout"
        };
        Win.Add (absoluteFrame);

        // Demonstrates how changing the View.Frame property can move Views
        var moveBtnA = new Button { ColorScheme = Colors.ColorSchemes ["Error"], Text = "Move This Button via Frame" };

        moveBtnA.Accept += (s, e) =>
                            {
                                moveBtnA.Frame = new Rectangle (
                                                           moveBtnA.Frame.X + 5,
                                                           moveBtnA.Frame.Y,
                                                           moveBtnA.Frame.Width,
                                                           moveBtnA.Frame.Height
                                                          );
                            };
        absoluteFrame.Add (moveBtnA);

        // Demonstrates how changing the View.Frame property can SIZE Views (#583)
        var sizeBtnA = new Button
        {
            Y = 2, ColorScheme = Colors.ColorSchemes ["Error"], Text = " ~  s  gui.cs   master ↑_10 = Сохранить"
        };

        sizeBtnA.Accept += (s, e) =>
                            {
                                sizeBtnA.Frame = new Rectangle (
                                                           sizeBtnA.Frame.X,
                                                           sizeBtnA.Frame.Y,
                                                           sizeBtnA.Frame.Width + 5,
                                                           sizeBtnA.Frame.Height
                                                          );
                            };
        absoluteFrame.Add (sizeBtnA);

        var label = new Label
        {
            X = 2, Y = Pos.Bottom (computedFrame) + 1, Text = "Text Alignment (changes the four buttons above): "
        };
        Win.Add (label);

        var radioGroup = new RadioGroup
        {
            X = 4,
            Y = Pos.Bottom (label) + 1,
            SelectedItem = 2,
            RadioLabels = new [] { "Left", "Right", "Centered", "Justified" }
        };
        Win.Add (radioGroup);

        // Demo changing hotkey
        string MoveHotkey (string txt)
        {
            // Remove the '_'
            List<Rune> runes = txt.ToRuneList ();

            int i = runes.IndexOf ((Rune)'_');
            var start = "";

            if (i > -1)
            {
                start = StringExtensions.ToString (runes.GetRange (0, i));
            }

            txt = start + StringExtensions.ToString (runes.GetRange (i + 1, runes.Count - (i + 1)));

            runes = txt.ToRuneList ();

            // Move over one or go to start
            i++;

            if (i >= runes.Count)
            {
                i = 0;
            }

            // Slip in the '_'
            start = StringExtensions.ToString (runes.GetRange (0, i));

            return start + '_' + StringExtensions.ToString (runes.GetRange (i, runes.Count - i));
        }

        var mhkb = "Click to Change th_is Button's Hotkey";

        var moveHotKeyBtn = new Button
        {
            X = 2,
            Y = Pos.Bottom (radioGroup) + 1,
            AutoSize = false,
            Height = 1,
            Width = Dim.Width (computedFrame) - 2,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Text = mhkb
        };
        moveHotKeyBtn.Accept += (s, e) => { moveHotKeyBtn.Text = MoveHotkey (moveHotKeyBtn.Text); };
        Win.Add (moveHotKeyBtn);

        var muhkb = " ~  s  gui.cs   master ↑10 = Сохранить";

        var moveUnicodeHotKeyBtn = new Button
        {
            X = Pos.Left (absoluteFrame) + 1,
            Y = Pos.Bottom (radioGroup) + 1,
            AutoSize = false,
            Height = 1,
            Width = Dim.Width (absoluteFrame) - 2, // BUGBUG: Not always the width isn't calculated correctly.
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Text = muhkb
        };
        moveUnicodeHotKeyBtn.Accept += (s, e) => { moveUnicodeHotKeyBtn.Text = MoveHotkey (moveUnicodeHotKeyBtn.Text); };
        Win.Add (moveUnicodeHotKeyBtn);

        radioGroup.SelectedItemChanged += (s, args) =>
                                          {
                                              switch (args.SelectedItem)
                                              {
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

        Top.Ready += (s, e) => radioGroup.Refresh ();
    }
}
