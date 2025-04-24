using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MessageBoxes", "Demonstrates how to use the MessageBox class.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
public class MessageBoxes : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
        };

        var frame = new FrameView
        {
            X = Pos.Center (),
            Y = 1,
            Width = Dim.Percent (75),
            Height = Dim.Auto (DimAutoStyle.Content),
            Title = "MessageBox Options"

        };
        app.Add (frame);

        // TODO: Use Pos.Align her to demo aligning labels and fields
        var label = new Label { X = 0, Y = 0, Width = 15, TextAlignment = Alignment.End, Text = "W_idth:" };
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

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Height:"
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
                   new Label
                   {
                       X = Pos.Right (widthEdit) + 2,
                       Y = Pos.Top (widthEdit),
                       Text = $"If width is 0, the dimension will be greater than {MessageBox.DefaultMinimumWidth}%."
                   }
                  );

        frame.Add (
                   new Label
                   {
                       X = Pos.Right (heightEdit) + 2,
                       Y = Pos.Top (heightEdit),
                       Text = $"If height is 0, the dimension will be greater than {MessageBox.DefaultMinimumHeight}%."
                   }
                  );

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),

            Width = Dim.Width (label),
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
            Text = "The title"
        };
        frame.Add (titleEdit);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),

            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Message:"
        };
        frame.Add (label);

        var messageEdit = new TextView
        {
            Text = "Message line 1.\nMessage line two. This is a really long line to force wordwrap. It needs to be long for it to work.",
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = 5
        };
        frame.Add (messageEdit);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (messageEdit),

            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Num Buttons:"
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

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),

            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Default Button:"
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

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),

            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "St_yle:"
        };
        frame.Add (label);

        var styleRadioGroup = new RadioGroup
        {
            X = Pos.Right (label) + 1, 
            Y = Pos.Top (label), 
            RadioLabels = ["_Query", "_Error"],
        };
        frame.Add (styleRadioGroup);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (styleRadioGroup),

            Width = Dim.Width (label),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "Wra_p:"
        };
        var ckbWrapMessage = new CheckBox
        {
            X = Pos.Right (label) + 1, Y = Pos.Bottom (styleRadioGroup),
            CheckedState = CheckState.Checked,
            Text = "_Wrap Message",
        };
        frame.Add (label, ckbWrapMessage);

        frame.ValidatePosDim = true;

        label = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 2, TextAlignment = Alignment.End, Text = "Button Pressed:"
        };
        app.Add (label);

        var buttonPressedLabel = new Label
        {
            X = Pos.Center (),
            Y = Pos.Bottom (label) + 1,
            ColorScheme = Colors.ColorSchemes ["Error"],
            TextAlignment = Alignment.Center,
            Text = " "
        };

        var showMessageBoxButton = new Button
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 2, IsDefault = true, Text = "_Show MessageBox"
        };

        app.Accepting += (s, e) =>
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
                                                   btns.Add ($"_{NumberToWords.Convert (i)}");
                                               }

                                               if (styleRadioGroup.SelectedItem == 0)
                                               {
                                                   buttonPressedLabel.Text =
                                                       $"{MessageBox.Query (
                                                                             width,
                                                                             height,
                                                                             titleEdit.Text,
                                                                             messageEdit.Text,
                                                                             defaultButton,
                                                                             ckbWrapMessage.CheckedState == CheckState.Checked,
                                                                             btns.ToArray ()
                                                                            )}";
                                               }
                                               else
                                               {
                                                   buttonPressedLabel.Text =
                                                       $"{MessageBox.ErrorQuery (
                                                                                  width,
                                                                                  height,
                                                                                  titleEdit.Text,
                                                                                  messageEdit.Text,
                                                                                  defaultButton,
                                                                                  ckbWrapMessage.CheckedState == CheckState.Checked,
                                                                                  btns.ToArray ()
                                                                                 )}";
                                               }
                                           }
                                           catch (FormatException)
                                           {
                                               buttonPressedLabel.Text = "Invalid Options";
                                           }

                                           e.Cancel = true;
                                       };
        app.Add (showMessageBoxButton);

        app.Add (buttonPressedLabel);

        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();
    }


    public override List<Key> GetDemoKeyStrokes ()
    {
        var keys = new List<Key> ();

        keys.Add (Key.S.WithAlt);
        keys.Add (Key.Esc);

        keys.Add (Key.E.WithAlt);
        keys.Add (Key.S.WithAlt);
        keys.Add (Key.Esc);

        keys.Add (Key.N.WithAlt);
        keys.Add (Key.D5);
        keys.Add (Key.S.WithAlt);
        keys.Add (Key.Enter);

        keys.Add (Key.T.WithAlt);
        keys.Add (Key.T.WithCtrl);
        keys.AddRange (GetKeysFromText ("This is a really long title"));
        keys.Add (Key.M.WithAlt);
        keys.Add (Key.T.WithCtrl);
        keys.AddRange (GetKeysFromText ("This is a long,\nmulti-line message.\nThis is a test of the emergency\nbroadcast\nsystem."));
        keys.Add (Key.S.WithAlt);

        for (int i = 0; i < 10; i++)
        {
            keys.Add (Key.Tab);
        }
        keys.Add (Key.Enter);

        keys.Add (Key.W.WithAlt);
        keys.Add (Key.S.WithAlt);

        for (int i = 0; i < 10; i++)
        {
            keys.Add (Key.Tab);
        }
        keys.Add (Key.Enter);

        return keys;
    }

    List<Key> GetKeysFromText (string text)
    {
        List<Key> keys = new ();

        foreach (var r in text)
        {
            keys.Add (r);
        }

        return keys;
    }
}
