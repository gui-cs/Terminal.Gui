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
		var textEdit = new TextView { Text = "", X = 1, Y = 0, Width = 20, Height = 4 };

		var hlabel = new Label {
			Text = textEdit.Text,
			X = Pos.Left (textEdit) + 1,
			Y = Pos.Bottom (textEdit),
			AutoSize = false,
			Width = Dim.Auto (style: DimAutoStyle.Text, min: 20),
			ColorScheme = Colors.ColorSchemes["Error"]
		};

		var vlabel = new Label {
			Text = textEdit.Text,
			X = Pos.Left (textEdit),
			Y = Pos.Bottom (textEdit) + 1,
			AutoSize = false,
			Height = Dim.Auto (style: DimAutoStyle.Text, min: 8),
			ColorScheme = Colors.ColorSchemes ["Error"],
			TextDirection = TextDirection.TopBottom_LeftRight
		};

		var heightAuto = new View () {
			X = Pos.Right (vlabel) + 1,
			Y = Pos.Bottom (hlabel) + 1,
			Width = 20,
			Height = Dim.Auto(),
			ColorScheme = Colors.ColorSchemes ["Error"],
			Title = "W: 20, H: Auto",
			BorderStyle = LineStyle.Rounded
		};
		heightAuto.Id = "heightAuto";

		var widthAuto = new View () {
			X = Pos.Right (heightAuto) + 1,
			Y = Pos.Bottom (hlabel) + 1,
			Width = Dim.Auto (),
			Height = 5,
			ColorScheme = Colors.ColorSchemes ["Error"],
			Title = "W: Auto, H: 5",
			BorderStyle = LineStyle.Rounded
		};
		widthAuto.Id = "widthAuto";

		var bothAuto = new View () {
			X = Pos.Right (widthAuto) + 1,
			Y = Pos.Bottom (hlabel) + 1,
			Width = Dim.Auto (),
			Height = Dim.Auto (),
			ColorScheme = Colors.ColorSchemes ["Error"],
			Title = "W: Auto, H: Auto",
			BorderStyle = LineStyle.Rounded
		};
		bothAuto.Id = "bothAuto";

		textEdit.ContentsChanged += (s, e) => {
			hlabel.Text = textEdit.Text;
			vlabel.Text = textEdit.Text;
			heightAuto.Text = textEdit.Text;
			widthAuto.Text = textEdit.Text;
			bothAuto.Text = textEdit.Text;
		};

		var movingButton = new Button () {
			Text = "_Move down",
			X = Pos.Right (vlabel),
			Y = Pos.Bottom (heightAuto),
			Width = 10
		};
		movingButton.Clicked += (s, e) => {
			movingButton.Y = movingButton.Frame.Y + 1;
		};

		var resetButton = new Button () {
			Text = "_Reset Button",
			X = Pos.Right(movingButton),
			Y = Pos.Top (movingButton),
		};

		var view = new FrameView () {
			Title = "Type to make View grow",
			X = 1,
			Y = 1,
			Width = Dim.Auto (style: DimAutoStyle.Subviews, min: 40),
			Height = Dim.Auto (style: DimAutoStyle.Subviews, min: 10)
		};
		view.ValidatePosDim = true;
		view.Add (textEdit, hlabel, vlabel, heightAuto, widthAuto, bothAuto, resetButton, movingButton);

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