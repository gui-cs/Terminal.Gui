#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("Dialogs", "Demonstrates how to use the Dialog and Dialog<TResult> classes")]
[ScenarioCategory ("Dialogs")]
public class Dialogs : Scenario
{
    /// <summary>
    ///     Example of a custom Dialog that returns a <see cref="Color"/> instead of a button index.
    ///     This demonstrates how to use <see cref="Dialog{TResult}"/> to create type-safe dialogs.
    /// </summary>
    private class ColorPickerDialog : Dialog<Color>
    {
        private readonly ColorPicker _colorPicker;

        public ColorPickerDialog (Color initialColor)
        {
            Title = "Pick a Color";
            _colorPicker = new ()
            {
                SelectedColor = initialColor
            };
            Add (_colorPicker);

            // Add Cancel and OK buttons
            AddButton (new () { Text = "_Cancel" });
            AddButton (new () { Text = "_OK" });
        }

        /// <inheritdoc/>
        protected override void OnButtonPressed (int buttonIndex)
        {
            if (buttonIndex == 1) // OK button
            {
                // Set the typed Result before closing
                Result = _colorPicker.SelectedColor;
            }
            // If Cancel (buttonIndex == 0), Result remains null (canceled)

            RequestStop ();
        }
    }

    private const int CODE_POINT = '你'; // We know this is a wide char

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ();
        mainWindow.Title = GetQuitKeyAndName ();

        FrameView frame = new ()
        {
            TabStop = TabBehavior.TabStop, // FrameView normally sets to TabGroup
            X = Pos.Center (),
            Y = 1,
            Width = Dim.Percent (75),
            Height = Dim.Auto (DimAutoStyle.Content),
            Title = "Dialog Options",
            Arrangement = ViewArrangement.Resizable
        };

        Label numButtonsLabel = new ()
        {
            X = 0,
            TextAlignment = Alignment.End,
            Text = "_Number of Buttons:"
        };

        Label label = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Width (numButtonsLabel),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = "_Width:"
        };
        frame.Add (label);

        TextField widthEdit = new ()
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

        TextField heightEdit = new ()
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
                       Text = $"If width is 0, the dimension will sized to fit the content."
                   }
                  );

        frame.Add (
                   new Label
                   {
                       X = Pos.Right (heightEdit) + 2,
                       Y = Pos.Top (heightEdit),
                       Text = $"If height is 0, the dimension will sized to fit the content."
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

        TextField titleEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = 1,
            Text = "Dialog Title"
        };
        frame.Add (titleEdit);

        numButtonsLabel.Y = Pos.Bottom (label);
        frame.Add (numButtonsLabel);

        TextField numButtonsEdit = new ()
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Top (numButtonsLabel),
            Width = 5,
            Height = 1,
            Text = "3"
        };
        frame.Add (numButtonsEdit);

        CheckBox glyphsNotWords = new ()
        {
            X = Pos.Right (numButtonsLabel) + 1,
            Y = Pos.Bottom (numButtonsLabel),
            TextAlignment = Alignment.End,
            Text = $"_Add {char.ConvertFromUtf32 (CODE_POINT)} to button text to stress wide char support",
            CheckedState = CheckState.UnChecked
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

        OptionSelector<Alignment> alignmentOptionSelector = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Title = "Ali_gn",
            AssignHotKeys = true
        };
        frame.Add (alignmentOptionSelector);
        alignmentOptionSelector.Value = Dialog.DefaultButtonAlignment;

        frame.ValidatePosDim = true;

        mainWindow.Add (frame);

        label = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 4, TextAlignment = Alignment.End, Text = "Button Pressed:"
        };
        mainWindow.Add (label);

        Label buttonPressedLabel = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 5, SchemeName = "Error", Text = " "
        };

        Button showDialogButton = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (frame) + 2, IsDefault = true, Text = "_Show Dialog"
        };

        mainWindow.Accepting += (s, e) =>
                                   {
                                       using Dialog? dlg = CreateDemoDialog (
                                                                       widthEdit,
                                                                       heightEdit,
                                                                       titleEdit,
                                                                       numButtonsEdit,
                                                                       glyphsNotWords,
                                                                       alignmentOptionSelector,
                                                                       buttonPressedLabel
                                                                      );

                                       if (dlg is null)
                                       {
                                           MessageBox.ErrorQuery ((s as View)!.App!, "Error", "Could not create Dialog. Invalid options.", "_Ok");
                                       }
                                       else
                                       {
                                           if (app.Run (dlg) is int result)
                                           {
                                               buttonPressedLabel.Text = $"Button {(int?)result} pressed.";
                                           }
                                           else
                                           {
                                               buttonPressedLabel.Text = "Dialog canceled.";
                                           }

                                       }

                                       e.Handled = true;
                                   };

        mainWindow.Add (showDialogButton, buttonPressedLabel);

        // --- Dialog<TResult> Demo ---
        // Demonstrates using Dialog<Color> to return a typed result instead of a button index

        Label colorLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (buttonPressedLabel) + 2,
            Text = "Dialog<T> Demo - Selected Color:"
        };
        mainWindow.Add (colorLabel);

        Label selectedColorLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (colorLabel),
            Width = 20,
            Height = 1,
            Text = "None",
            SchemeName = "Error"
        };
        mainWindow.Add (selectedColorLabel);

        Button showColorDialogButton = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (selectedColorLabel) + 1,
            Text = "Show _Color Dialog<Color>"
        };
        mainWindow.Add (showColorDialogButton);

        showColorDialogButton.Accepting += (_, e) =>
        {
            // Parse current color or default to Blue
            Color currentColor = Color.Blue;

            using ColorPickerDialog colorDialog = new (currentColor);

            // Run the dialog and get the typed result
            Color? result = app.Run (colorDialog) as Color?;

            if (result.HasValue)
            {
                selectedColorLabel.Text = result.Value.ToString ();
                selectedColorLabel.SetScheme (new () { Normal = new (result.Value, result.Value) });
            }
            else
            {
                selectedColorLabel.Text = "Canceled";
                selectedColorLabel.SchemeName = "Error";
            }

            e.Handled = true;
        };

        app.Run (mainWindow);
    }

    private static Dialog? CreateDemoDialog (
        TextField widthEdit,
        TextField heightEdit,
        TextField titleEdit,
        TextField numButtonsEdit,
        CheckBox glyphsNotWords,
        OptionSelector alignmentGroup,
        Label buttonPressedLabel
    )
    {
        if (!int.TryParse (widthEdit.Text, out int width)
            || !int.TryParse (heightEdit.Text, out int height)
            || !int.TryParse (numButtonsEdit.Text, out int numButtons))
        {
            return null;
        }

        // Add the buttons that go on the bottom of the dialog
        List<Button> dlgButtons = [];

        for (var i = 0; i < numButtons; i++)
        {
            int buttonId = i;
            Button button;

            if (glyphsNotWords.CheckedState == CheckState.Checked)
            {
                buttonId = i;

                button = new ()
                {
                    Text = "_" + NumberToWords.Convert (buttonId) + " " + char.ConvertFromUtf32 (buttonId + CODE_POINT),
                };
            }
            else
            {
                button = new () { Text = "_" + NumberToWords.Convert (buttonId) };
            }

            dlgButtons.Add (button);
        }

        // This tests dynamically adding buttons; ensuring the dialog resizes if needed and
        // the buttons are laid out correctly
        Dialog dialog = new ()
        {
            Title = titleEdit.Text,
            ButtonAlignment = (Alignment)Enum.Parse (typeof (Alignment), alignmentGroup.Labels! [alignmentGroup.Value!.Value] [0..]),
            Buttons = dlgButtons.ToArray ()
        };

        Label label = new ()
        {
            Title = "_Enter text:"
        };
        dialog.Add (label);

        TextField textField = new ()
        {
            Y = Pos.Bottom (label),
            Width = Dim.Fill (0, minimumContentDim: 60),
            Text = new string ("0123456789").Repeat (6)!
        };
        dialog.Add (textField);

        CheckBox checkBox = new ()
        {
            Title = "_Check Me",
            Y = Pos.Bottom (textField),
            CheckedState = CheckState.UnChecked
        };
        dialog.Add (checkBox);

        OptionSelector<Schemes> optionSelector = new ()
        {
            Y = Pos.Bottom (checkBox),
            Value = Schemes.Error,
            AssignHotKeys = true,
        };
        dialog.Add (optionSelector);

        FrameView frame = new ()
        {
            Title = "Frame (Fill)",
            X = Pos.Right(checkBox) +1,
            Y = Pos.Bottom (textField),
            Width = Dim.Fill (),
            Height = Dim.Fill (0, minimumContentDim: 1)
        };
        dialog.Add (frame);

        Button addButtonButton = new ()
        {
            Title = "_Add Button",
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
        };
        dialog.Add (addButtonButton);
        addButtonButton.Accepting += (s, e) =>
        {
            int newButtonId = dialog.Buttons.Length;
            Button newButton = new ()
            {
                Text = "_" + NumberToWords.Convert (newButtonId)
            };
            List<Button> buttons = dialog.Buttons.ToList ();
            dialog.AddButton (newButton);
            e.Handled = true;
        };

        if (width != 0)
        {
            dialog.Width = width;
        }
        if (height != 0)
        {
            dialog.Height = height;
        }

        return dialog;
    }

    public override List<Key> GetDemoKeyStrokes (IApplication? app)
    {
        List<Key> keys =
        [
            Key.D6,
            Key.D5,
            Key.Tab,
            Key.D2,
            Key.D0,
            Key.Enter
        ];

        for (int i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        keys.Add (Key.S.WithAlt);
        keys.Add (Key.Enter);

        for (var i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        keys.Add (Key.E.WithAlt);
        keys.Add (Key.Enter);

        for (var i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        keys.Add (Key.C.WithAlt);
        keys.Add (Key.Enter);

        for (var i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        keys.Add (Key.F.WithAlt);
        keys.Add (Key.Enter);

        for (var i = 0; i < 5; i++)
        {
            keys.Add (Key.A);
        }

        keys.Add (Key.Enter);

        return keys;
    }
}
