using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Buttons", Description: "Demonstrates all sorts of Buttons")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Layout")]
	class Buttons : Scenario {
		public override void Setup ()
		{
			var button = new Button (1, 1, "This is a button") { Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No") };
			Win.Add (button);
			Win.Add (new Button (25, 1, "PRESS ME") { Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No") });
		}
	}
}
