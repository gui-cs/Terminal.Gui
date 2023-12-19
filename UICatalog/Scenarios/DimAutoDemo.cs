using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("DimAuto", "Demonstrates Dim.Auto")]
[ScenarioCategory ("Layout")]
public class DimAutoDemo : Scenario {
	public override void Init ()
	{
		Application.Init ();
		ConfigurationManager.Themes.Theme = Theme;
		ConfigurationManager.Apply ();
		Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
	}

	public override void Setup ()
	{
		var textField = new TextField { Text = "Type here", X = 1, Y = 0, Width = 20, Height = 1 };

		var label = new Label {
			X = Pos.Left (textField),
			Y = Pos.Bottom (textField),
			AutoSize = true,
			ColorScheme = Colors.Error
		};

		textField.TextChanged += (s, e) => {
			label.Text = textField.Text;
		};

		var resetButton = new Button () {
			Text = "P_ut Button Back",
			X = Pos.Center (),
			Y = Pos.Bottom(label)
		};

		var movingButton = new Button () { Text = "Press to make button move down.", 
			X = 0, 
			Y = Pos.Bottom (resetButton), 
			Width = 10
		};
		movingButton.Clicked += (s, e) => {
			movingButton.Y = movingButton.Frame.Y + 1;
		};

		resetButton.Clicked += (s, e) => {
			movingButton.Y = Pos.Bottom (resetButton);
		};

		var view = new FrameView () {
			Title = "Type in the TextField to make View grow.",
			X = 3,
			Y = 3,
			Width = Dim.Auto (min: Dim.Percent(50)),
			Height = Dim.Auto (min: 10)
		};
		view.ValidatePosDim = true;
		view.Add (textField, label, resetButton, movingButton);

		Application.Top.Add (view);
	}
}