using Terminal.Gui;
using static Terminal.Gui.Dim;

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
		var textField = new TextField { Text = "", X = 1, Y = 0, Width = 20, Height = 1 };

		var hlabel = new Label {
			Text = textField.Text,
			X = Pos.Left (textField) + 1,
			Y = Pos.Bottom (textField),
			AutoSize = false,
			Width = Dim.Auto (style: DimAutoStyle.Text, min: 20),
			ColorScheme = Colors.ColorSchemes["Error"]
		};

		var vlabel = new Label {
			Text = textField.Text,
			X = Pos.Left (textField),
			Y = Pos.Bottom (textField) + 1,
			AutoSize = false,
			Height = Dim.Auto (style: DimAutoStyle.Text, min: 10),
			ColorScheme = Colors.ColorSchemes ["Error"],
			TextDirection = TextDirection.TopBottom_LeftRight
		};

		textField.TextChanged += (s, e) => {
			hlabel.Text = textField.Text;
			vlabel.Text = textField.Text;
		};

		var movingButton = new Button () {
			Text = "P_ress to make button move down.",
			X = 2,
			Y = Pos.Bottom (hlabel),
			Width = 10
		};
		movingButton.Clicked += (s, e) => {
			movingButton.Y = movingButton.Frame.Y + 1;
		};

		var resetButton = new Button () {
			Text = "P_ut Button Back",
			X = 30,//Pos.AnchorEnd () - 19,
			Y = Pos.Top (movingButton),
		};


		var view = new FrameView () {
			Title = "Type in the TextField to make View grow.",
			X = 3,
			Y = 3,
			Width = Dim.Auto (min: 50),
			Height = Dim.Auto (min: 10)
		};
		view.ValidatePosDim = true;
		view.Add (textField, hlabel, vlabel, resetButton, movingButton);

		resetButton.Clicked += (s, e) => {
			movingButton.Y = Pos.Bottom (hlabel);
		};

		var dlgButton = new Button () {
			Text = "Open Test _Dialog",
			X = Pos.Right (view),
			Y = Pos.Top (view)
		};
		dlgButton.Clicked += DlgButton_Clicked;

		Application.Top.Add (view, dlgButton);
	}

	private void DlgButton_Clicked (object sender, System.EventArgs e)
	{
		var dlg = new Dialog () {
			Title = "Test Dialog"
		};

		//var ok = new Button ("Bye") { IsDefault = true };
		//ok.Clicked += (s, _) => Application.RequestStop (dlg);
		//dlg.AddButton (ok);

		//var cancel = new Button ("Abort") { };
		//cancel.Clicked += (s, _) => Application.RequestStop (dlg);
		//dlg.AddButton (cancel);

		var label = new Label ("This is a label (AutoSize = false; Dim.Auto(3/20). Press Esc to close. Even more text.") {
			AutoSize = false,
			X = Pos.Center (),
			Y = 0,
			Height = Dim.Auto (min: 3),
			Width = Dim.Auto (min: 20),
			ColorScheme = Colors.ColorSchemes ["Menu"]
		};

		var text = new TextField () {
			Text = "TextField... X = 1; Y = Pos.Bottom (label), Width = Dim.Fill (1); Height = Dim.Fill(1)",
			TextFormatter = new TextFormatter () { WordWrap = true },
			X = 20,
			Y = Pos.Bottom (label),
			Width = Dim.Fill (20),
			Height = Dim.Fill (10)
		};
		var btn = new Button ("AnchorEnd") {
			Y = Pos.AnchorEnd (1)
		};
		// TODO: We should really fix AnchorEnd to do this automatically. 
		btn.X = Pos.AnchorEnd () - (Pos.Right (btn) - Pos.Left (btn));
		dlg.Add (label);
		dlg.Add (text);
		dlg.Add (btn);
		Application.Run (dlg);
	}
}