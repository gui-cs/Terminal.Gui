﻿using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.UI;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Dialogs", Description: "Demonstrates how to the Dialog class")]
	[ScenarioCategory ("Dialogs")]
	public class Dialogs : Scenario {
		static int CODE_POINT = '你'; // We know this is a wide char
		public override void Setup ()
		{
			var frame = new FrameView ("Dialog Options") {
				X = Pos.Center (),
				Y = 0,
				Width = Dim.Percent (75)
			};
			Win.Add (frame);

			var label = new Label ("width:") {
				X = 0,
				Y = 0,
				Width = 15,
				Height = 1,
				TextAlignment = Terminal.UI.TextAlignment.Right,
			};
			frame.Add (label);
			var widthEdit = new TextField ("0") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (widthEdit);

			label = new Label ("height:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.UI.TextAlignment.Right,
			};
			frame.Add (label);
			var heightEdit = new TextField ("0") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (heightEdit);

			frame.Add (new Label ("If height & width are both 0,") {
				X = Pos.Right (widthEdit) + 2,
				Y = Pos.Top (widthEdit),
			});
			frame.Add (new Label ("the Dialog will size to 80% of container.") {
				X = Pos.Right (heightEdit) + 2,
				Y = Pos.Top (heightEdit),
			});

			label = new Label ("Title:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.UI.TextAlignment.Right,
			};
			frame.Add (label);
			var titleEdit = new TextField ("Title") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = Dim.Fill (),
				Height = 1
			};
			frame.Add (titleEdit);

			label = new Label ("Num Buttons:") {
				X = 0,
				Y = Pos.Bottom (titleEdit),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.UI.TextAlignment.Right,
			};
			frame.Add (label);
			var numButtonsEdit = new TextField ("3") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (numButtonsEdit);

			var glyphsNotWords = new CheckBox ($"Add {Char.ConvertFromUtf32 (CODE_POINT)} to button text to stress wide char support", false) {
				X = Pos.Left (numButtonsEdit),
				Y = Pos.Bottom (label),
				TextAlignment = Terminal.UI.TextAlignment.Right,
			};
			frame.Add (glyphsNotWords);


			label = new Label ("Button Style:") {
				X = 0,
				Y = Pos.Bottom (glyphsNotWords),
				TextAlignment = Terminal.UI.TextAlignment.Right
			};
			frame.Add (label);
			var styleRadioGroup = new RadioGroup (new ustring [] { "Center", "Justify", "Left", "Right" }) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
			};
			frame.Add (styleRadioGroup);

			void Top_Loaded ()
			{
				frame.Height = Dim.Height (widthEdit) + Dim.Height (heightEdit) + Dim.Height (titleEdit)
					+ Dim.Height (numButtonsEdit) + Dim.Height (styleRadioGroup) + Dim.Height (glyphsNotWords) + 2;
				Application.Top.Loaded -= Top_Loaded;
			}
			Application.Top.Loaded += Top_Loaded;

			label = new Label ("Button Pressed:") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 4,
				Height = 1,
				TextAlignment = Terminal.UI.TextAlignment.Right,
			};
			Win.Add (label);
			var buttonPressedLabel = new Label (" ") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 5,
				Width = 25,
				Height = 1,
				ColorScheme = Colors.Error,
			};
			// glyphsNotWords
			// false:var btnText = new [] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
			// true: var btnText = new [] { "0", "\u2780", "➁", "\u2783", "\u2784", "\u2785", "\u2786", "\u2787", "\u2788", "\u2789" };
			// \u2781 is ➁ dingbats \ufb70 is	

			var showDialogButton = new Button ("Show Dialog") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 2,
				IsDefault = true,
			};
			showDialogButton.Clicked += () => {
				try {
					Dialog dialog = null;

					int width = 0;
					int.TryParse (widthEdit.Text.ToString (), out width);
					int height = 0;
					int.TryParse (heightEdit.Text.ToString (), out height);
					int numButtons = 3;
					int.TryParse (numButtonsEdit.Text.ToString (), out numButtons);

					var buttons = new List<Button> ();
					var clicked = -1;
					for (int i = 0; i < numButtons; i++) {
						int buttonId = i;
						Button button = null;
						if (glyphsNotWords.Checked == true) {
							buttonId = i;
							button = new Button (NumberToWords.Convert (buttonId) + " " + Char.ConvertFromUtf32 (buttonId + CODE_POINT),
								is_default: buttonId == 0);
						} else {
							button = new Button (NumberToWords.Convert (buttonId),
							       is_default: buttonId == 0);
						}
						button.Clicked += () => {
							clicked = buttonId;
							Application.RequestStop ();
						};
						buttons.Add (button);
					}
					//if (buttons.Count > 1) {
					//	buttons [1].Text = "Accept";
					//	buttons [1].IsDefault = true;
					//	buttons [0].Visible = false;
					//	buttons [0].Text = "_Back";
					//	buttons [0].IsDefault = false;
					//}

					// This tests dynamically adding buttons; ensuring the dialog resizes if needed and 
					// the buttons are laid out correctly
					dialog = new Dialog (titleEdit.Text, width, height,
						buttons.ToArray ()) {
						ButtonAlignment = (Dialog.ButtonAlignments)styleRadioGroup.SelectedItem
					};

					var add = new Button ("Add a button") {
						X = Pos.Center (),
						Y = Pos.Center ()
					};
					add.Clicked += () => {
						var buttonId = buttons.Count;
						Button button;
						if (glyphsNotWords.Checked == true) {
							button = new Button (NumberToWords.Convert (buttonId) + " " + Char.ConvertFromUtf32 (buttonId + CODE_POINT),
								is_default: buttonId == 0);
						} else {
							button = new Button (NumberToWords.Convert (buttonId),
								is_default: buttonId == 0);
						}
						button.Clicked += () => {
							clicked = buttonId;
							Application.RequestStop ();

						};
						buttons.Add (button);
						dialog.AddButton (button);
						if (buttons.Count > 1) {
							button.TabIndex = buttons [buttons.Count - 2].TabIndex + 1;
						}
					};
					dialog.Add (add);

					var addChar = new Button ($"Add a {Char.ConvertFromUtf32 (CODE_POINT)} to each button") {
						X = Pos.Center (),
						Y = Pos.Center () + 1
					};
					addChar.Clicked += () => {
						foreach (var button in buttons) {
							button.Text += Char.ConvertFromUtf32 (CODE_POINT);
						}
						dialog.LayoutSubviews ();
					};
					dialog.Closed += (args) => {
						buttonPressedLabel.Text = $"{clicked}";
					};
					dialog.Add (addChar);

					Application.Run (dialog);

				} catch (FormatException) {
					buttonPressedLabel.Text = "Invalid Options";
				}
			};
			Win.Add (showDialogButton);

			Win.Add (buttonPressedLabel);
		}
	}
}
