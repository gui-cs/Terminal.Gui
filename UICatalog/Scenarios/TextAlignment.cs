using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Text Alignment", Description: "Demonstrates text alignment")]
	[ScenarioCategory ("Text")]

	class TextAlignment : Scenario {
		public override void Run ()
		{
			var ntop = new Toplevel (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows));
			var win = new Window ($"ESC to Close - Scenario: {GetName()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			ntop.Add (win);

			int i = 1;
			string txt = "Hello world, how are you doing today";
			var labelList = new List<Label> ();
			labelList.Add (new Label ($"Label:"));
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Left, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1 });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Right, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1 });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Centered, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1 });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Justified, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1 });
			txt += "\nSecond line";
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Left, Width = Dim.Fill (1), Height = 4, X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1 });

			win.Add (labelList.ToArray ());
			win.LayoutSubviews ();

			Application.Run (ntop);
		}

	}
}