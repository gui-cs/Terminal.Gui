using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Mouse", "Demonstrates how to capture mouse events")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Mouse : Scenario {
	public override void Setup ()
	{
		Label ml;
		var count = 0;
		ml = new Label { X = 1, Y = 1, Width = 50, Height = 1, Text = "Mouse: " };
		var rme = new List<string> ();

		var test = new Label { X = 1, Y = 2, Text = "Se iniciará el análisis" };
		Win.Add (test);
		Win.Add (ml);

		var rmeList = new ListView {
			X = Pos.Right (test) + 25,
			Y = Pos.Top (test) + 1,
			Width = Dim.Fill () - 1,
			Height = Dim.Fill (),
			ColorScheme = Colors.ColorSchemes ["TopLevel"],
			Source = new ListWrapper (rme)
		};
		Win.Add (rmeList);

		Application.MouseEvent += (sender, a) => {
			ml.Text = $"Mouse: ({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count}";
			rme.Add ($"({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags} {count++}");
			rmeList.MoveDown ();
		};

		// I have no idea what this was intended to show off in demo.c
		var drag = new Label { X = 1, Y = 4, Text = "Drag: " };
		var dragText = new TextField {
			X = Pos.Right (drag),
			Y = Pos.Top (drag),
			Width = 40
		};
		Win.Add (drag, dragText);
	}
}