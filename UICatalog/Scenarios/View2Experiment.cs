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
			// Put your scenario code here, e.g.
			var newFrameView = new View2 () {
				X = 4,
				Y = 4,
				Height = Dim.Fill (4),
				Width = Dim.Fill (4)
			};

			var label = new Label () {
				Text = "Label: ",
				AutoSize = true,
				X = 2,
				Y = 2
			};
			newFrameView.Add (label);

			var edit = new TextField () {
				Text = "Edit me",
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = Dim.Fill (4),
				Height = 1
			};
			newFrameView.Add (edit);

			Application.Top.Add (newFrameView);

		}
	}
}