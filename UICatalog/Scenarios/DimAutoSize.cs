using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("DimAutoSize", "Demonstrates Dim.AutoSize")]
[ScenarioCategory ("Layout")]
public class DimAutoSize : Scenario {
	public override void Init ()
	{
		// The base `Scenario.Init` implementation:
		//  - Calls `Application.Init ()`
		//  - Adds a full-screen Window to Application.Top with a title
		//    that reads "Press <hotkey> to Quit". Access this Window with `this.Win`.
		//  - Sets the Theme & the ColorScheme property of `this.Win` to `colorScheme`.
		// To override this, implement an override of `Init`.

		//base.Init ();

		// A common, alternate, implementation where `this.Win` is not used is below. This code
		// leverages ConfigurationManager to borrow the color scheme settings from UICatalog:

		Application.Init ();
		ConfigurationManager.Themes.Theme = Theme;
		ConfigurationManager.Apply ();
		Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
	}

	public override void Setup ()
	{
		// Put scenario code here (in a real app, this would be the code
		// that would setup the app before `Application.Run` is called`).
		// With a Scenario, after UI Catalog calls `Scenario.Setup` it calls
		// `Scenario.Run` which calls `Application.Run`. Example:

		var textField = new TextField { Text = "Type here", X = 1, Y = 0, Width = 20, Height = 1 };

		var label = new Label {
			X = Pos.Left (textField),
			Y = Pos.Bottom (textField),
			AutoSize = true,
		};

		textField.TextChanged += (s, e) => {
			label.Text = textField.Text;
		};

		var button = new Button () { Text = "Press to make button move down.", 
			X = 0, 
			Y = Pos.Bottom (label), 
		};
		button.Clicked += (s, e) => {
			button.Y = button.Frame.Y + 1;
		};

		var view = new FrameView () {
			Title = "Type in the TextField to make it grow.",
			X = 3,
			Y = 3,
			Width = Dim.AutoSize (),
			Height = Dim.AutoSize ()
		};

		view.Add (textField, label, button);

		Application.Top.Add (view);
	}
}