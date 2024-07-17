using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Dialogs", "Demonstrates how to the Dialog class")]
[ScenarioCategory ("Dialogs")]
public class Dialogs : Scenario
{
    private static readonly int CODE_POINT = '你'; // We know this is a wide char

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        var frame = new FrameView
        {
            X = Pos.Center (),
            Y = 1,
            Width = Dim.Percent (75),
            Height = Dim.Auto (DimAutoStyle.Content),
            Title = "Dialog Options"
        };

        var numButtonsLabel = new Label
        {
            X = 0,
            TextAlignment = Alignment.End,
            Text = "_Number of Buttons:"
        };

        var label = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Width (numButtonsLabel),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Width:"
        };
        frame.Add (label);

        var widthEdit = new TextField
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "0"
        };
        frame.Add (widthEdit);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Width (numButtonsLabel),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Height:"
        };
        frame.Add (label);

        var heightEdit = new TextField
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "0"
        };
        frame.Add (heightEdit);

        frame.Add (
                   new Label
                   {
                       X = Pos.Right (widthEdit) + 2,
                       Y = Pos.Top (widthEdit),
                       Text = $"If width is 0, the dimension will be {Dialog.DefaultMinimumWidth}%."
                   }
                  );

        frame.Add (
                   new Label
                   {
                       X = Pos.Right (heightEdit) + 2,
                       Y = Pos.Top (heightEdit),
                       Text = $"If height is 0, the dimension will be {Dialog.DefaultMinimumWidth}%."
                   }
                  );

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Width (numButtonsLabel),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Title:"
        };
        frame.Add (label);

        var titleEdit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = 1,
            Text = "Title"
        };
        frame.Add (titleEdit);

        numButtonsLabel.Y = Pos.Bottom (label);
        frame.Add (numButtonsLabel);

        var numButtonsEdit = new TextField
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Top (numButtonsLabel),
            Width = 5,
            Height = 1,
            Text = "3"
        };
        frame.Add (numButtonsEdit);

        var glyphsNotWords = new CheckBox
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Bottom (numButtonsLabel),
            TextAlignment = Alignment.End,
            Text = $"_Add {char.ConvertFromUtf32 (CODE_POINT)} to button text to stress wide char support",
            State = CheckState.UnChecked
        };
        frame.Add (glyphsNotWords);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (glyphsNotWords),
            Width = Dim.Width (numButtonsLabel),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "Button A_lignment:"
        };
        frame.Add (label);

        var labels = Enum.GetNames<Alignment> ();
        var alignmentGroup = new RadioGroup
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            RadioLabels = labels.ToArray (),
        };
        frame.Add (alignmentGroup);
        alignmentGroup.SelectedItem = labels.ToList ().IndexOf (Dialog.DefaultButtonAlignment.ToString ());

        frame.ValidatePosDim = true;

        app.Add (frame);

        label = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 4, TextAlignment = Alignment.End, Text = "Button Pressed:"
        };
        app.Add (label);

        var buttonPressedLabel = new Label
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 5, ColorScheme = Colors.ColorSchemes ["Error"], Text = " "
        };

        var showDialogButton = new Button
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 2, IsDefault = true, Text = "_Show Dialog"
        };

        showDialogButton.Accept += (s, e) =>
                                   {
                                       Dialog dlg = CreateDemoDialog (
                                                                      widthEdit,
                                                                      heightEdit,
                                                                      titleEdit,
                                                                      numButtonsEdit,
                                                                      glyphsNotWords,
                                                                      alignmentGroup,
                                                                      buttonPressedLabel
                                                                     );
                                       Application.Run (dlg);
                                       dlg.Dispose ();
                                   };

        app.Add (showDialogButton);

        app.Add (buttonPressedLabel);

        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();
    }

    private Dialog CreateDemoDialog (
        TextField widthEdit,
        TextField heightEdit,
        TextField titleEdit,
        TextField numButtonsEdit,
        CheckBox glyphsNotWords,
        RadioGroup alignmentRadioGroup,
        Label buttonPressedLabel
    )
    {
        Dialog dialog = null;

        try
        {
            var width = 0;
            int.TryParse (widthEdit.Text, out width);
            var height = 0;
            int.TryParse (heightEdit.Text, out height);
            var numButtons = 3;
            int.TryParse (numButtonsEdit.Text, out numButtons);

            List<Button> buttons = new ();
            int clicked = -1;

            for (var i = 0; i < numButtons; i++)
            {
                int buttonId = i;
                Button button = null;

                if (glyphsNotWords.State == CheckState.Checked)
                {
                    buttonId = i;

                    button = new ()
                    {
                        Text = NumberToWords.Convert (buttonId) + " " + char.ConvertFromUtf32 (buttonId + CODE_POINT),
                        IsDefault = buttonId == 0
                    };
                }
                else
                {
                    button = new () { Text = NumberToWords.Convert (buttonId), IsDefault = buttonId == 0 };
                }

                button.Accept += (s, e) =>
                                 {
                                     clicked = buttonId;
                                     Application.RequestStop ();
                                 };
                buttons.Add (button);
            }

            // This tests dynamically adding buttons; ensuring the dialog resizes if needed and 
            // the buttons are laid out correctly
            dialog = new ()
            {
                Title = titleEdit.Text,
                ButtonAlignment = (Alignment)Enum.Parse (typeof (Alignment), alignmentRadioGroup.RadioLabels [alignmentRadioGroup.SelectedItem]),

                Buttons = buttons.ToArray ()
            };

            if (width != 0)
            {
                dialog.Width = width;
            }
            if (height != 0)
            {
                dialog.Height = height;
            }

            var add = new Button
            {
                X = Pos.Center (),
                Y = Pos.Center () - 1,
                Text = "_Add a button"
            };

            add.Accept += (s, e) =>
                          {
                              int buttonId = buttons.Count;
                              Button button;

                              if (glyphsNotWords.State == CheckState.Checked)
                              {
                                  button = new ()
                                  {
                                      Text = NumberToWords.Convert (buttonId) + " " + char.ConvertFromUtf32 (buttonId + CODE_POINT),
                                      IsDefault = buttonId == 0
                                  };
                              }
                              else
                              {
                                  button = new () { Text = NumberToWords.Convert (buttonId), IsDefault = buttonId == 0 };
                              }

                              button.Accept += (s, e) =>
                                               {
                                                   clicked = buttonId;
                                                   Application.RequestStop ();
                                               };
                              buttons.Add (button);
                              dialog.AddButton (button);

                              if (buttons.Count > 1)
                              {
                                  button.TabIndex = buttons [buttons.Count - 2].TabIndex + 1;
                              }
                          };
            dialog.Add (add);

            var addChar = new Button
            {
                X = Pos.Center (),
                Y = Pos.Center () + 1,
                Text = $"A_dd a {char.ConvertFromUtf32 (CODE_POINT)} to each button. This text is really long for a reason."
            };

            addChar.Accept += (s, e) =>
                              {
                                  foreach (Button button in buttons)
                                  {
                                      button.Text += char.ConvertFromUtf32 (CODE_POINT);
                                  }

                                  dialog.LayoutSubviews ();
                              };
            dialog.Add (addChar);

            dialog.Closed += (s, e) => { buttonPressedLabel.Text = $"{clicked}"; };
        }
        catch (FormatException)
        {
            buttonPressedLabel.Text = "Invalid Options";
        }

        return dialog;
    }
}
