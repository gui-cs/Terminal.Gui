using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Generic", Description: "Generic sample - A template for creating new Scenarios")]
	[ScenarioCategory ("Controls")]
	class MyScenario : Scenario {
		public override void Setup ()
		{
			// Put your scenario code here, e.g.
			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += () => MessageBox.Query (20, 7, "Hi", "Neat?", "Yes", "No");
			Win.Add (button);
		}
	}
}