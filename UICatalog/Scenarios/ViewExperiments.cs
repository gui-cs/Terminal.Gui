using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "_ View Experiments", Description: "v2 View Experiments")]
	[ScenarioCategory ("Controls")]
	public class ViewExperiments : Scenario {
		public override void Init (ColorScheme colorScheme)
		{
			Application.Init ();
			Application.Top.ColorScheme = colorScheme;
		}

		public override void Setup ()
		{
			var containerLabel = new Label () {
				X = 0,
				Y = 0,
				Width = Dim.Fill(),
			};
			Application.Top.Add (containerLabel);

			var view = new View () {
				X = 2,
				Y = 3,
				Height = Dim.Fill (2),
				Width = Dim.Fill (2),
				Title = "View"
			};
			Application.Top.Add (view);

			view.InitializeFrames ();
			view.Margin.Thickness = new Thickness (2);
			view.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view.Margin.Data = "Margin";
			view.BorderFrame.Thickness = new Thickness (2);
			view.BorderFrame.BorderStyle = BorderStyle.Single;
			view.BorderFrame.ColorScheme = view.ColorScheme;
			view.BorderFrame.Data = "BorderFrame";
			view.Padding.Thickness = new Thickness (2);
			view.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view.Padding.Data = "Padding";


			containerLabel.LayoutComplete += (a) => {
				containerLabel.Text = $"Container.Frame: {Application.Top.Frame} .Bounds: {Application.Top.Bounds}\nView.Frame: {view.Frame} .Bounds: {view.Bounds}\nView.ContentArea: {view.ContentArea}";

			};

			var label = new Label () {
				ColorScheme = Colors.ColorSchemes ["Error"],
				Text = "AutoSize true; 1;1:",
				AutoSize = true,
				X = 1,
				Y = 1,

			};
			view.Add (label);

			var edit = new TextField () {
				Text = "Right (label)",
				X = Pos.Right (label),
				Y = 1,
				Width = 15,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "Right (edit) + 1",
				X = Pos.Right (edit) + 1,
				Y = 1,
				Width = 20	,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "Center();50%",
				X = Pos.Center(),
				Y = Pos.Percent(50),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "Center() - 1;60%",
				X = Pos.Center () - 1,
				Y = Pos.Percent (60),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "0 + Percent(50);70%",
				X = 0 + Pos.Percent (50),
				Y = Pos.Percent (70),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "AnchorEnd[Right];AnchorEnd (1)",
				Y = Pos.AnchorEnd (1),
				Width = 30,
				Height = 1
			};
			edit.X = Pos.AnchorEnd () - (Pos.Right (edit) - Pos.Left (edit));
			view.Add (edit);

			edit = new TextField () {
				Text = "Left;AnchorEnd (2)",
				Y = 1 + Pos.Center(),
				Width = 30,
				Height = 1
			};
			edit.X = 0;
			view.Add (edit);
		}
	}
}