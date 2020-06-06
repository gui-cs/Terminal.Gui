using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Buttons", Description: "Demonstrates all sorts of Buttons")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Layout")]
	class Buttons : Scenario {
		public override void Setup ()
		{
			// Add a label & text field so we can demo IsDefault
			var editLabel = new Label ("TextField (to demo IsDefault):") {
				X = 0,
				Y = 0,
			};
			Win.Add (editLabel);
			// Add a TextField using Absolute layout. Use buttons to move/grow.
			var edit = new TextField (31, 0, 15, "");
			Win.Add (edit);

			// This is the default button (IsDefault = true); if user presses ENTER in the TextField
			// the scenario will quit
			var defaultButton = new Button ("_Quit") {
				X = Pos.Center (),
				//TODO: Change to use Pos.AnchorEnd()
				Y = Pos.Bottom (Win) - 3,
				IsDefault = true,
				Clicked = () => Application.RequestStop (),
			};
			Win.Add (defaultButton);

			var swapButton = new Button (50, 0, "Swap Default (Absolute Layout)");
			swapButton.Clicked = () => {
				defaultButton.IsDefault = !defaultButton.IsDefault;
				swapButton.IsDefault = !swapButton.IsDefault;
			};
			Win.Add (swapButton);

			static void DoMessage (Button button, ustring txt)
			{
				button.Clicked = () => {
					var btnText = button.Text.ToString ();
					MessageBox.Query (30, 7, "Message", $"Did you click {txt.ToString ()}?", "Yes", "No");
				};
			}

			var colorButtonsLabel = new Label ("Color Buttons:") {
				X = 0,
				Y = Pos.Bottom (editLabel) + 1,
			};
			Win.Add (colorButtonsLabel);

			View prev = colorButtonsLabel;
			foreach (var colorScheme in Colors.ColorSchemes) {
				var colorButton = new Button ($"{colorScheme.Key}") {
					ColorScheme = colorScheme.Value,
					X = Pos.Right (prev) + 2,
					Y = Pos.Y (colorButtonsLabel),
				};
				DoMessage (colorButton, colorButton.Text);
				Win.Add (colorButton);
				prev = colorButton;
			}

			Button button;
			Win.Add (button = new Button ("A super long _Button that will probably expose a bug in clipping or wrapping of text. Will it?") {
				X = 2,
				Y = Pos.Bottom (colorButtonsLabel) + 1,
			});
			DoMessage (button, button.Text);

			// Note the 'N' in 'Newline' will be the hotkey
			Win.Add (button = new Button ("a Newline\nin the button") {
				X = 2,
				Y = Pos.Bottom (button) + 1,
				Clicked = () => MessageBox.Query ("Message", "Question?", "Yes", "No")
			});

			var textChanger = new Button ("Te_xt Changer") {
				X = 2,
				Y = Pos.Bottom (button) + 1,
			};
			Win.Add (textChanger);
			textChanger.Clicked = () => textChanger.Text += "!";

			Win.Add (button = new Button ("Lets see if this will move as \"Text Changer\" grows") {
				X = Pos.Right(textChanger) + 2,
				Y = Pos.Y (textChanger),
			});

			var removeButton = new Button ("Remove this button") {
				X = 2,
				Y = Pos.Bottom (button) + 1,
				ColorScheme = Colors.Error
			};
			Win.Add (removeButton);
			// This in intresting test case because `moveBtn` and below are laid out relative to this one!
			removeButton.Clicked = () => Win.Remove (removeButton);

			// Demonstrates how changing the View.Frame property can move Views
			var moveBtn = new Button ("Move This Button via Frame") {
				X = 2,
				Y = Pos.Bottom (removeButton) + 1,
				ColorScheme = Colors.Error,
			};
			moveBtn.Clicked = () => {
				moveBtn.Frame = new Rect (moveBtn.Frame.X + 5, moveBtn.Frame.Y, moveBtn.Frame.Width, moveBtn.Frame.Height);
			};
			Win.Add (moveBtn);

			// Demonstrates how changing the View.Frame property can SIZE Views (#583)
			var sizeBtn = new Button ("Size This Button via Frame") {
				X = Pos.Right(moveBtn) + 2,
				Y = Pos.Y (moveBtn),
				Width = 30,
				ColorScheme = Colors.Error,
			};
			sizeBtn.Clicked = () => {
				sizeBtn.Frame = new Rect (sizeBtn.Frame.X, sizeBtn.Frame.Y, sizeBtn.Frame.Width + 5, sizeBtn.Frame.Height);
			};
			Win.Add (sizeBtn);

			var label = new Label ("Text Alignment (changes the two buttons above): ") {
				X = 2,
				Y = Pos.Bottom (sizeBtn) + 1,
			};
			Win.Add (label);

			var radioGroup = new RadioGroup (new [] { "Left", "Right", "Centered", "Justified" }) {
				X = 4,
				Y = Pos.Bottom (label) + 1,
				//SelectionChanged = (selected) => {
				//	switch (selected) {
				//	case 0:
				//		moveBtn.TextAlignment = TextAlignment.Left;
				//		sizeBtn.TextAlignment = TextAlignment.Left;
				//		break;
				//	case 1:
				//		moveBtn.TextAlignment = TextAlignment.Right;
				//		sizeBtn.TextAlignment = TextAlignment.Right;
				//		break;
				//	case 2:
				//		moveBtn.TextAlignment = TextAlignment.Centered;
				//		sizeBtn.TextAlignment = TextAlignment.Centered;
				//		break;
				//	case 3:
				//		moveBtn.TextAlignment = TextAlignment.Justified;
				//		sizeBtn.TextAlignment = TextAlignment.Justified;
				//		break;
				//	}
				//}
			};
			Win.Add (radioGroup);

			// Demo changing hotkey
			ustring MoveHotkey (ustring txt)
			{
				// Remove the '_'
				var i = txt.IndexOf ('_');
				var start = txt [0, i];
				txt = start + txt [i + 1, txt.Length];

				// Move over one or go to start
				i++;
				if (i >= txt.Length) {
					i = 0;
				}

				// Slip in the '_'
				start = txt [0, i];
				txt = start + ustring.Make ('_') + txt [i, txt.Length];

				return txt;
			}

			var moveHotKeyBtn = new Button ("Click to Change th_is Button's Hotkey") {
				X = 2,
				Y = Pos.Bottom (radioGroup) + 1,
				ColorScheme = Colors.TopLevel,
			};
			moveHotKeyBtn.Clicked = () => {
				moveHotKeyBtn.Text = MoveHotkey (moveHotKeyBtn.Text);
			};
			Win.Add (moveHotKeyBtn);
		}
	}
}
