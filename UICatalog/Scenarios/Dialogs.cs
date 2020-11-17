using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Dialogs", Description: "Demonstrates how to the Dialog class")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	class Dialogs : Scenario {
		public override void Setup ()
		{
			var frame = new FrameView ("Dialog Options") {
				X = Pos.Center(),
				Y = 1,
				Width = Dim.Percent(75),
				Height = 10
			};
			Win.Add (frame);

			var label = new Label ("width:") {
				X = 0,
				Y = 0,
				Width = 15,
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
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
				TextAlignment = Terminal.Gui.TextAlignment.Right,
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
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var titleEdit = new TextField ("Title") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = Dim.Fill(),
				Height = 1
			};
			frame.Add (titleEdit);

			label = new Label ("Num Buttons:") {
				X = 0,
				Y = Pos.Bottom (titleEdit),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var numButtonsEdit = new TextField ("3") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (numButtonsEdit);

			void Top_Loaded ()
			{
				frame.Height = Dim.Height (widthEdit) + Dim.Height (heightEdit) + Dim.Height (titleEdit)
					+ Dim.Height (numButtonsEdit) + 2;
				Top.Loaded -= Top_Loaded;
			}
			Top.Loaded += Top_Loaded;

			label = new Label ("Button Pressed:") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 4,
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			Win.Add (label);
			var buttonPressedLabel = new Label (" ") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 5,
				Width = 25,
				Height = 1,
				ColorScheme = Colors.Error,
			};

			//var btnText = new [] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
			var showDialogButton = new Button ("Show Dialog") {
				X = Pos.Center(),
				Y = Pos.Bottom (frame) + 2,
				IsDefault = true,
			};
			showDialogButton.Clicked += () => {
				try {
					int width = int.Parse (widthEdit.Text.ToString ());
					int height = int.Parse (heightEdit.Text.ToString ());
					int numButtons = int.Parse (numButtonsEdit.Text.ToString ());

					var buttons = new List<Button> ();
					var clicked = -1;
					for (int i = 0; i < numButtons; i++) {
						var buttonId = i;
						//var button = new Button (btnText [buttonId % 10],
						//	is_default: buttonId == 0);
						var button = new Button (NumberToWords.Convert(buttonId),
							is_default: buttonId == 0);
						button.Clicked += () => {
							clicked = buttonId;
							Application.RequestStop ();
						};
						buttons.Add (button);
					}

					// This tests dynamically adding buttons; ensuring the dialog resizes if needed and 
					// the buttons are laid out correctly
					var dialog = new Dialog (titleEdit.Text, width, height,
						buttons.ToArray ());
					var add = new Button ("Add a button") {
						X = Pos.Center (),
						Y = Pos.Center ()
					};
					add.Clicked += () => {
						var buttonId = buttons.Count;
						//var button = new Button (btnText [buttonId % 10],
						//	is_default: buttonId == 0);
						var button = new Button (NumberToWords.Convert (buttonId),
							is_default: buttonId == 0);
						button.Clicked += () => {
							clicked = buttonId;
							Application.RequestStop ();
						};
						buttons.Add (button);
						dialog.AddButton (button);
						button.TabIndex = buttons [buttons.Count - 2].TabIndex + 1;
					};
					dialog.Add (add);

					Application.Run (dialog);
					buttonPressedLabel.Text = $"{clicked}";

				} catch (FormatException) {
					buttonPressedLabel.Text = "Invalid Options";
				}
			};
			Win.Add (showDialogButton);

			Win.Add (buttonPressedLabel);
		}
	}
}
