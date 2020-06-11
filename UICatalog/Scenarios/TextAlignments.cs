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
#if true
			string txt = "Hello world, how are you today? Pretty neat!";
#else
			string txt = "Hello world, how are you today? Unicode:  ~  gui.cs  . Neat?";
#endif
			var alignments = Enum.GetValues (typeof (Terminal.Gui.TextAlignment)).Cast<Terminal.Gui.TextAlignment> ().ToList ();
			var label = new Label ($"Demonstrating single-line (should clip!):") { Y = 0 };
			Win.Add (label);
			foreach (var alignment in alignments) {
				label = new Label ($"{alignment}:") { Y = Pos.Bottom (label) };
				Win.Add (label);
				label = new Label (txt) { TextAlignment = alignment, Y = Pos.Bottom (label), Width = Dim.Fill (), Height = 1, ColorScheme = Colors.Dialog };
				Win.Add (label);
			}

			txt += "\nSecond line\n\nFourth Line.";
			label = new Label ($"Demonstrating multi-line and word wrap:") { Y = Pos.Bottom (label) + 1 };
			Win.Add (label);
			foreach (var alignment in alignments) {
				label = new Label ($"{alignment}:") { Y = Pos.Bottom (label) };
				Win.Add (label);
				label = new Label (txt) { TextAlignment = alignment, Width = Dim.Fill (), Height = 6, ColorScheme = Colors.Dialog, Y = Pos.Bottom (label) };
				Win.Add (label);
			}
		}
	}
}