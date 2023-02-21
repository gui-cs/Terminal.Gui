using System;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Generic", Description: "Generic sample - A template for creating new Scenarios")]
	[ScenarioCategory ("Controls")]
	public class MyScenario : Scenario {
		public override void Setup ()
		{
			// Put your scenario code here, e.g.
			//var button = new Button ("Press me!") {
			//	X = Pos.Center (),
			//	Y = Pos.Center (),
			//};
			//button.Clicked += () => MessageBox.Query (20, 7, "Hi", "Neat?", "Yes", "No");
			//Win.Add (button);

			var text = $"First line{Environment.NewLine}Second line";
			var horizontalView = new View () {
				Width = 20,
				Text = text
			};
			var verticalView = new View () {
				Y = 3,
				Height = 20,
				Text = text,
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			Win.Add (horizontalView, verticalView);
			verticalView.Text = $"最初の行{Environment.NewLine}二行目";
			//Application.Top.Redraw (Application.Top.Bounds);

		}
	}
}