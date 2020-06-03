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
			int i = 1;
			string txt = "Hello world, how are you doing today?";

			var alignments = Enum.GetValues (typeof (Terminal.Gui.TextAlignment)).Cast<Terminal.Gui.TextAlignment> ().ToList();

			foreach (var alignment in alignments) {
				Win.Add (new Label ($"{alignment}:") { Y = ++i });
				Win.Add (new Label (txt) { TextAlignment = alignment, Y = i++, Width = Dim.Fill(), ColorScheme = Colors.Dialog });
			}

			// Demonstrate that wrapping labels are not yet implemented (#352)
			txt += "\nSecond line";
			Win.Add (new Label ($"Demonstrating multi-line (note wrap is not yet implemented):") { Y = ++i });

			foreach (var alignment in alignments) {
				Win.Add (new Label ($"{alignment}:") { Y = ++i });
				Win.Add (new Label (txt) { TextAlignment = alignment, Y = ++i, Width = Dim.Fill (), Height = 2, ColorScheme = Colors.Dialog });
				i += 2;
			}
		}
	}
}