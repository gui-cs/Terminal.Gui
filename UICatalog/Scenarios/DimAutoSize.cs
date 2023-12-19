using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("DimAutoSize", "Demonstrates Dim.AutoSize")]
[ScenarioCategory ("Layout")]
public class DimAutoSize : Scenario {
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
		};

		textField.TextChanged += (s, e) => {
			label.Text = textField.Text;
		};

		var button = new Button () { Text = "Press to make button move down.", 
			X = 0, 
			Y = Pos.Bottom (label), 
			Width = 10
		};
		button.Clicked += (s, e) => {
			button.Y = button.Frame.Y + 1;
		};

		var view = new FrameView () {
			Title = "Type in the TextField to make it grow.",
			X = 3,
			Y = 3,
			Width = Dim.Auto (),
			Height = Dim.Auto ()
		};
		view.ValidatePosDim = true;
		view.Add (textField, label, button);

		Application.Top.Add (view);
	}
}