using System.Data;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Generic", Description: "Generic sample - A template for creating new Scenarios")]
	[ScenarioCategory ("Controls")]
	public class MyScenario : Scenario {
		public override void Init (ColorScheme colorScheme)
		{
			// The base `Scenario.Init` implementation:
			//  - Calls `Application.Init ()`
			//  - Adds a full-screen Window to Application.Top with a title
			//    that reads "Press <hotkey> to Quit". Access this Window with `this.Win`.
			//  - Sets the ColorScheme property of `this.Win` to `colorScheme`.
			// To overrride this, implement an override of `Init`.
			base.Init (colorScheme);

			// A common, alternate, implementation where `this.Win` is not used:
			//   Application.Init ();
			//   Application.Top.ColorScheme = colorScheme;
		}

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