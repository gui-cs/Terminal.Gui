using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Buttons", Description: "Demonstrates all sorts of Buttons")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Layout")]
	class Buttons : Scenario {
		public override void Run () {
			var top = new Toplevel ();
			var win = new Window ($"ESC to Close - Scenario: {GetName()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			top.Add (win);

			var button = new Button (1, 1, "This is a button") { Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No") };
			win.Add (button);

			win.Add (new Button (20, 1, "PRESS ME") { Clicked = () => MessageBox.Query (30, 7, "Message", "Question?", "Yes", "No") });

			Application.Run (top);
		}
	}
}
