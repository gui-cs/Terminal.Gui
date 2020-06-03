using NStack;
using System;
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
			var edit = new TextField (31, 0, 25, "");
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

			static void DoMessage (Button button, ustring txt)
			{
				button.Clicked = () => {
					var btnText = button.Text.ToString ();
					MessageBox.Query (30, 7, "Message", $"Did you click {txt.ToString ()}?", "Yes", "No");
				};
			}

			var y = 2;
			var button = new Button (10, y, "Ba_se Color") {
				ColorScheme = Colors.Base,
			};
			DoMessage (button, button.Text);
			Win.Add (button);

			y += 2;
			Win.Add (button = new Button (10, y, "Error Color") {
				ColorScheme = Colors.Error,
			});
			DoMessage (button, button.Text);

			y += 2;
			Win.Add (button = new Button (10, y, "Dialog Color") {
				ColorScheme = Colors.Dialog,
			});
			DoMessage (button, button.Text);

			y += 2;
			Win.Add (button = new Button (10, y, "Menu Color") {
				ColorScheme = Colors.Menu,
			});
			DoMessage (button, button.Text);

			y += 2;
			Win.Add (button = new Button (10, y, "TopLevel Color") {
				ColorScheme = Colors.TopLevel,
			});
			DoMessage (button, button.Text);

			y += 2;
			Win.Add (button = new Button (10, y, "A super long _Button that will probably expose a bug in clipping or wrapping of text. Will it?") {
			});
			DoMessage (button, button.Text);

			y += 2;
			// Note the 'N' in 'Newline' will be the hotkey
			Win.Add (new Button (10, y, "a Newline\nin the button") {
				Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No")
			});

			y += 2;
			// BUGBUG: Buttons don't support specifying hotkeys with _?!?
			Win.Add (button = new Button ("Te_xt Changer") {
				X = 10,
				Y = y
			});

			button.Clicked = () => button.Text += "!"; 

			Win.Add (new Button ("Lets see if this will move as \"Text Changer\" grows") {
				X = Pos.Right (button) + 10,
				Y = y,
			});

			y += 2;
			Win.Add (new Button (10, y, "Delete") {
				ColorScheme = Colors.Error,
				Clicked = () => Win.Remove (button)
			});

			y += 2;
			Win.Add (new Button (10, y, "Change Default") {
				Clicked = () => {
					defaultButton.IsDefault = !defaultButton.IsDefault;
					button.IsDefault = !button.IsDefault;
				},
			});

			// Demonstrates how changing the View.Frame property can move Views
			y += 2;
			var moveBtn = new Button (10, y, "Move This Button via Frame") {
				ColorScheme = Colors.Error,
			};
			moveBtn.Clicked = () => {
				moveBtn.Frame = new Rect (moveBtn.Frame.X + 5, moveBtn.Frame.Y, moveBtn.Frame.Width, moveBtn.Frame.Height);
			};
			Win.Add (moveBtn);

			// Demonstrates how changing the View.Frame property can SIZE Views (#583)
			y += 2;
			var sizeBtn = new Button (10, y, "Size This Button via Frame") {
				ColorScheme = Colors.Error,
			};
			moveBtn.Clicked = () => {
				sizeBtn.Frame = new Rect (sizeBtn.Frame.X, sizeBtn.Frame.Y, sizeBtn.Frame.Width + 5, sizeBtn.Frame.Height);
			};
			Win.Add (sizeBtn);

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

			y += 2;
			var moveHotKeyBtn = new Button (10, y, "Click to Change th_is Button's Hotkey") {
				ColorScheme = Colors.TopLevel,
			};
			moveHotKeyBtn.Clicked = () => {
				moveHotKeyBtn.Text = MoveHotkey (moveHotKeyBtn.Text);
			};
			Win.Add (moveHotKeyBtn);
		}
	}
}
