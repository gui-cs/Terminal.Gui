using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Text Alignment", Description: "Demonstrates text alignment")]
	[ScenarioCategory ("Text")]
	class TextAlignments : Scenario {
		public override void Setup ()
		{
			var alignments = Enum.GetValues (typeof (Terminal.Gui.TextAlignment)).Cast<Terminal.Gui.TextAlignment> ().ToList ();
			//var label = new Label ($"Demonstrating single-line (should clip!):") { Y = 0 };
			//Win.Add (label);
			string txt = "Hello world, how are you doing today? This is a test of the emergency.";

			//foreach (var alignment in alignments) {
			//	label = new Label ($"{alignment}:") { Y = Pos.Bottom (label) };
			//	Win.Add (label);
			//	label = new Label (txt) { TextAlignment = alignment, Y = Pos.Bottom (label), Width = Dim.Fill (), Height = 1, ColorScheme = Colors.Dialog };
			//	Win.Add (label);
			//}

			// Demonstrate that wrapping labels are not yet implemented (#352)
			//txt += "\nSecond line\n\nFourth Line.";
			//label = new Label ($"Demonstrating multi-line (note wrap is not yet implemented):") { Y = Pos.Bottom (label) + 1 };
			//Win.Add (label);

			//foreach (var alignment in alignments) {
			var alignment = TextAlignment.Left;
			//label = new Label ($"{alignment}:") { Y = Pos.Bottom (label)};
			//Win.Add (label);
			var label = new Label (txt) { TextAlignment = alignment, Width = Dim.Fill (), Height = 6, ColorScheme = Colors.Dialog, Y = 0 };// Pos.Bottom (label) };
				Win.Add (label);
			//}
		}
	}
}