using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Mouse", Description: "Demonstrates how to capture mouse events")]
	[ScenarioCategory ("Input")]
	class Mouse : Scenario {
		public override void Setup ()
		{
			Label ml;
			int count = 0;
			ml = new Label (new Rect (1, 1, 50, 1), "Mouse: ");
			List<string> rme = new List<string> ();

			var test = new Label (1, 2, "Se iniciará el análisis");
			Win.Add (test);
			Win.Add (ml);

			var rmeList = new ListView (rme) {
				X = Pos.Right (test) + 25,
				Y = Pos.Top (test) + 1,
				Width = Dim.Fill () - 1,
				Height = Dim.Fill (),
				ColorScheme = Colors.TopLevel
			};
			Win.Add (rmeList);

			Application.RootMouseEvent += delegate (MouseEvent me) {
				ml.Text = $"Mouse: ({me.X},{me.Y}) - {me.Flags} {count}";
				rme.Add ($"({me.X},{me.Y}) - {me.Flags} {count++}");
				rmeList.MoveDown ();
			};

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
