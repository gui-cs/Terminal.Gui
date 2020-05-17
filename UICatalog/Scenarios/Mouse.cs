using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Mouse", Description: "Demonstrates how to capture mouse events")]
	[ScenarioCategory ("Input")]
	class Mouse : Scenario {
		public override void Run () {
			var top = new Toplevel ();
			var win = new Window ($"ESC to Close - Scenario: {GetName()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			top.Add (win);

			Label ml;
			int count = 0;
			ml = new Label (new Rect (1, 1, 50, 1), "Mouse: ");
			Application.RootMouseEvent += delegate (MouseEvent me) {
				ml.TextColor = Colors.TopLevel.Normal;
				ml.Text = $"Mouse: ({me.X},{me.Y}) - {me.Flags} {count++}";
			};


			var test = new Label (1, 2, "Se iniciará el análisis");
			win.Add (test);
			win.Add (ml);

			// I have no idea what this was intended to show off in demo.c
			var drag = new Label ("Drag: ") { X = 1, Y = 4 };
			var dragText = new TextField ("") {
				X = Pos.Right (drag),
				Y = Pos.Top (drag),
				Width = 40
			};
			win.Add (drag, dragText);

			Application.Run (top);
		}
	}
}
