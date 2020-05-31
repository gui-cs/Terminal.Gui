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
			var edit = new TextField ("") {
				X = Pos.Right (editLabel) + 1,
				Y = Pos.Top (editLabel),
				Width = Dim.Fill (2),
			};
			Win.Add (edit);

			// This is the default button (IsDefault = true); if user presses ENTER in the TextField
			// the scenario will quit
			var defaultButton = new Button ("Quit") {
				X = Pos.Center (),
				//TODO: Change to use Pos.AnchorEnd()
				Y= Pos.Bottom(Win) - 3,
				IsDefault = true,
				Clicked = () => Application.RequestStop (),
			};
			Win.Add (defaultButton);

			var y = 2;
			var button = new Button (10, y, "Base Color") {
				ColorScheme = Colors.Base,
				Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No") 
			};
			Win.Add (button);

			y += 2;
			Win.Add (new Button (10, y, "Error Color") { 
				ColorScheme = Colors.Error,
				Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No") 
			});

			y += 2;
			Win.Add (new Button (10, y, "Dialog Color") {
				ColorScheme = Colors.Dialog,
				Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No")
			});

			y += 2;
			Win.Add (new Button (10, y, "Menu Color") {
				ColorScheme = Colors.Menu,
				Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No")
			});

			y += 2;
			Win.Add (new Button (10, y, "TopLevel Color") {
				ColorScheme = Colors.TopLevel,
				Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No")
			});

			y += 2;
			Win.Add (new Button (10, y, "A super long button that will probably expose a bug in clipping or wrapping of text. Will it?") {
				Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No")
			});

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
			button.Clicked = () => button.Text += $"{y++}";

			Win.Add (new Button ("Lets see if this will move as \"Text Changer\" grows") {
				X = Pos.Right(button) + 10,
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


		}
	}
}
