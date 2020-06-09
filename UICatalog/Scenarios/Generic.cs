using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Generic", Description: "Generic sample - A template for creating new Scenarios")]
	[ScenarioCategory ("Controls")]
	class MyScenario : Scenario {
		public override void Setup ()
		{
			// Put your scenario code here, e.g.
			Win.Add (new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
				Clicked = () => MessageBox.Query (20, 7, "Hi", "Neat?", "Yes", "No")
			});

			var label = new Label ("Label: ") { 
				X = 0,
				Y = 0
			};
			Win.Add (label);

			var button = new Button ("test") {
				X = Pos.Y (label),
				Y = Pos.Bottom (label),
			};
			button.Width = Dim.Width (label);
			label.X = button.X;


			Win.Add (button);
		}
	}
}