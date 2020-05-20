using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Mouse", Description: "Demonstrates how to capture mouse events")]
	[ScenarioCategory ("Input")]
	class Mouse : Scenario {
		public override void Setup () {
			Label ml;
			int count = 0;
			ml = new Label (new Rect (1, 1, 50, 1), "Mouse: ");
			Application.RootMouseEvent += delegate (MouseEvent me) {
				ml.TextColor = Colors.TopLevel.Normal;
				ml.Text = $"Mouse: ({me.X},{me.Y}) - {me.Flags} {count++}";
			};


			var test = new Label (1, 2, "Se iniciará el análisis");
			Win.Add (test);
			Win.Add (ml);

			// I have no idea what this was intended to show off in demo.c
			var drag = new Label ("Drag: ") { X = 1, Y = 4 };
			var dragText = new TextField ("") {
				X = Pos.Right (drag),
				Y = Pos.Top (drag),
				Width = 40
			};
			Win.Add (drag, dragText);
		}
	}
}
