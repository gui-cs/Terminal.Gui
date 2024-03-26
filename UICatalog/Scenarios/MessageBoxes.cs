using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MessageBoxes", "Demonstrates how to use the MessageBox class.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
public class MessageBoxes : Scenario
{
    public override void Setup ()
    {
        var frame = new FrameView { X = Pos.Center (), Y = 1, Width = Dim.Percent (75), Title = "MessageBox Options" };
        Win.Add (frame);

        var label = new Label { X = 0, Y = 0, TextAlignment = TextAlignment.Right, Text = "Width:" };
        frame.Add (label);

        var widthEdit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "0"
        };
        frame.Add (widthEdit);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            AutoSize = false,
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = TextAlignment.Right,
            Text = "Height:"
        };
        frame.Add (label);

        var heightEdit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "0"
        };
        frame.Add (heightEdit);

        frame.Add (
                   new Label { X = Pos.Right (widthEdit) + 2, Y = Pos.Top (widthEdit), Text = "If height & width are both 0," }
                  );

        frame.Add (
                   new Label
                   {
                       X = Pos.Right (heightEdit) + 2,
                       Y = Pos.Top (heightEdit),
                       Text = "the MessageBox will be sized automatically."
                   }
                  );

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            AutoSize = false,
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = TextAlignment.Right,
            Text = "Title:"
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

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            AutoSize = false,
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = TextAlignment.Right,
            Text = "Message:"
        };
        frame.Add (label);

        var messageEdit = new TextView
        {
            Text = "Message",
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = 5
        };
        frame.Add (messageEdit);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (messageEdit),
            AutoSize = false,
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = TextAlignment.Right,
            Text = "Num Buttons:"
        };
        frame.Add (label);

        var numButtonsEdit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "3"
        };
        frame.Add (numButtonsEdit);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            AutoSize = false,
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = TextAlignment.Right,
            Text = "Default Button:"
        };
        frame.Add (label);

        var defaultButtonEdit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1,
            Text = "0"
        };
        frame.Add (defaultButtonEdit);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            AutoSize = false,
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = TextAlignment.Right,
            Text = "Style:"
        };
        frame.Add (label);

        var styleRadioGroup = new RadioGroup
        {
            X = Pos.Right (label) + 1, Y = Pos.Top (label), RadioLabels = new [] { "_Query", "_Error" }
        };
        frame.Add (styleRadioGroup);

        var ckbWrapMessage = new CheckBox
        {
            X = Pos.Right (label) + 1, Y = Pos.Bottom (styleRadioGroup), Text = "_Wrap Message", Checked = true
        };
        frame.Add (ckbWrapMessage);

        frame.ValidatePosDim = true;

        void Top_LayoutComplete (object sender, EventArgs args)
        {
            frame.Height =
                widthEdit.Frame.Height
                + heightEdit.Frame.Height
                + titleEdit.Frame.Height
                + messageEdit.Frame.Height
                + numButtonsEdit.Frame.Height
                + defaultButtonEdit.Frame.Height
                + styleRadioGroup.Frame.Height
                + ckbWrapMessage.Frame.Height
                + frame.GetAdornmentsThickness ().Vertical;
            Top.Loaded -= Top_LayoutComplete;
        }

        Top.LayoutComplete += Top_LayoutComplete;

        label = new Label
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 2, TextAlignment = TextAlignment.Right, Text = "Button Pressed:"
        };
        Win.Add (label);

        var buttonPressedLabel = new Label
        {
            X = Pos.Center (),
            Y = Pos.Bottom (label) + 1,
            ColorScheme = Colors.ColorSchemes ["Error"],
            TextAlignment = TextAlignment.Centered,
            Text = " "
        };

        //var btnText = new [] { "_Zero", "_One", "T_wo", "_Three", "_Four", "Fi_ve", "Si_x", "_Seven", "_Eight", "_Nine" };

        var showMessageBoxButton = new Button
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 2, IsDefault = true, Text = "_Show MessageBox"
        };

        showMessageBoxButton.Accept += (s, e) =>
                                        {
                                            try
                                            {
                                                int width = int.Parse (widthEdit.Text);
                                                int height = int.Parse (heightEdit.Text);
                                                int numButtons = int.Parse (numButtonsEdit.Text);
                                                int defaultButton = int.Parse (defaultButtonEdit.Text);

                                                List<string> btns = new ();

                                                for (var i = 0; i < numButtons; i++)
                                                {
                                                    //btns.Add(btnText[i % 10]);
                                                    btns.Add (NumberToWords.Convert (i));
                                                }

                                                if (styleRadioGroup.SelectedItem == 0)
                                                {
                                                    buttonPressedLabel.Text =
                                                        $"{
                                                            MessageBox.Query (
                                                                              width,
                                                                              height,
                                                                              titleEdit.Text,
                                                                              messageEdit.Text,
                                                                              defaultButton,
                                                                              (bool)ckbWrapMessage.Checked,
                                                                              btns.ToArray ()
                                                                             )
                                                        }";
                                                }
                                                else
                                                {
                                                    buttonPressedLabel.Text =
                                                        $"{
                                                            MessageBox.ErrorQuery (
                                                                                   width,
                                                                                   height,
                                                                                   titleEdit.Text,
                                                                                   messageEdit.Text,
                                                                                   defaultButton,
                                                                                   (bool)ckbWrapMessage.Checked,
                                                                                   btns.ToArray ()
                                                                                  )
                                                        }";
                                                }
                                            }
                                            catch (FormatException)
                                            {
                                                buttonPressedLabel.Text = "Invalid Options";
                                            }
                                        };
        Win.Add (showMessageBoxButton);

        Win.Add (buttonPressedLabel);
    }
}
