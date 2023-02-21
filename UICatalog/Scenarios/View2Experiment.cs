using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "View2Experiment", Description: "View2 Experiment")]
	[ScenarioCategory ("Controls")]
	public class View2Experiment : Scenario {
		public override void Init (ColorScheme colorScheme)
		{
			Application.Init ();

			Application.Top.ColorScheme = Colors.Base;
		}

		public override void Setup ()
		{
			var containerLabel = new Label () {
				X = 0,
				Y = 0,
				Width = Dim.Fill(),
			};
			Application.Top.Add (containerLabel);

			var view2 = new View () {
				X = 2,
				Y = 2,
				Height = Dim.Fill (2),
				Width = Dim.Fill (2),
				Title = "View2"
			};
			view2.EnableFrames ();
			view2.Margin.Thickness = new Thickness (2);
			view2.BorderFrame.Thickness = new Thickness (2);
			view2.BorderFrame.BorderStyle = BorderStyle.Single;
			view2.Padding.Thickness = new Thickness (2);

			containerLabel.LayoutComplete += (a) => {
				containerLabel.Text = $"Container.Frame: {Application.Top.Frame} .Bounds: {Application.Top.Bounds}\nView2.Frame: {view2.Frame} .Bounds: {view2.Bounds}";

			};

			var label = new Label () {
				ColorScheme = Colors.ColorSchemes ["Error"],
				Text = "AutoSize true; 1;1:",
				AutoSize = true,
				X = 1,
				Y = 1,

			};
			view2.Add (label);

			var edit = new TextField () {
				Text = "Right (label)",
				X = Pos.Right (label),
				Y = 1,
				Width = 15,
				Height = 1
			};
			view2.Add (edit);

			edit = new TextField () {
				Text = "Right (edit) + 1",
				X = Pos.Right (edit) + 1,
				Y = 1,
				Width = 20	,
				Height = 1
			};
			view2.Add (edit);

			edit = new TextField () {
				Text = "Center();50%",
				X = Pos.Center(),
				Y = Pos.Percent(50),
				Width = 30,
				Height = 1
			};
			view2.Add (edit);

			edit = new TextField () {
				Text = "Center() - 1;60%",
				X = Pos.Center () - 1,
				Y = Pos.Percent (60),
				Width = 30,
				Height = 1
			};
			view2.Add (edit);

			edit = new TextField () {
				Text = "0 + Percent(50);70%",
				X = 0 + Pos.Percent (50),
				Y = Pos.Percent (70),
				Width = 30,
				Height = 1
			};
			view2.Add (edit);

			edit = new TextField () {
				Text = "AnchorEnd[Right];AnchorEnd (1)",
				Y = Pos.AnchorEnd (1),
				Width = 30,
				Height = 1
			};
			edit.X = Pos.AnchorEnd () - (Pos.Right (edit) - Pos.Left (edit));
			view2.Add (edit);

			edit = new TextField () {
				Text = "Left;AnchorEnd (2)",
				Y = 1 + Pos.Center(),
				Width = 30,
				Height = 1
			};
			edit.X = 0;
			view2.Add (edit);

			Application.Top.Add (view2);

		}
	}
}