using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "View Text", Description: "Demos and tests View's Text capabilities.")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("POC")]
	class ViewWithText : Scenario {
		public override void Setup ()
		{
			Win.Text = "This is the Te_xt for the host Win object. TextAlignment.Centered was specified. It is intentionally very long to illustrate word wrap.\n" +
				"<-- There is a new line here to show a hard line break. You should see this text bleed underneath the subviews, which start at Y = 3.";
			Win.TextAlignment = TextAlignment.Centered;
#if true
			string txt = "Hello world, how are you today? Pretty neat!";
#else
			string txt = "Hello world, how are you today? Unicode:  ~  gui.cs  . Neat?";
#endif
			var alignments = Enum.GetValues (typeof (Terminal.Gui.TextAlignment)).Cast<Terminal.Gui.TextAlignment> ().ToList ();
			var label = new View ($"Demonstrating single-line (should clip!):") { Y = 3 };
			Win.Add (label);
			foreach (var alignment in alignments) {
				label = new Label ($"{alignment}:") { Y = Pos.Bottom (label) };
				Win.Add (label);
				label = new Label (txt) {
					TextAlignment = alignment,
					Y = Pos.Bottom (label),
					Width = Dim.Fill (),
					Height = 1,
					ColorScheme = Colors.Dialog,
				};
				Win.Add (label);
			}

			txt += "\nSecond line\n\nFourth Line.";
			label = new View ($"Demonstrating multi-line and word wrap:") { Y = Pos.Bottom (label) + 1 };
			Win.Add (label);
			foreach (var alignment in alignments) {
				label = new View ($"{alignment}:") { Y = Pos.Bottom (label) };
				Win.Add (label);
				label = new View (txt) { TextAlignment = alignment, Width = Dim.Fill (), Height = 6, ColorScheme = Colors.Dialog, Y = Pos.Bottom (label) };
				Win.Add (label);
			}
		}
	}
}