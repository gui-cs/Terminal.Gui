using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "TextFormatter Demo", Description: "Demos and tests the TextFormatter class.")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("POC")]
	class TextFormatterDemo : Scenario {
		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			Application.Init ();

			Top = top;
			if (Top == null) {
				Top = Application.Top;
			}
			Win = null;
		}

		public override void Setup ()
		{
			Top.Text = "Press CTRL-Q to Quit. This is the Text for the TopLevel View. TextAlignment.Centered was specified. It is intentionally very long to illustrate word wrap.\n" +
				"<-- There is a new line here to show a hard line break. You should see this text bleed underneath the subviews, which start at Y = 3.";
			Top.TextAlignment = TextAlignment.Centered;
			Top.ColorScheme = Colors.Base;

			string text = "Hello world, how are you today? Pretty neat!\nSecond line\n\nFourth Line.";
			string unicode = "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

			var unicodeCheckBox = new CheckBox ("Unicode", Top.HotKeySpecifier == (Rune)' ') {
				X = 0,
				Y = 3,
			};

			Top.Add (unicodeCheckBox);

			var alignments = Enum.GetValues (typeof (Terminal.Gui.TextAlignment)).Cast<Terminal.Gui.TextAlignment> ().ToList ();
			var singleLines = new Label [alignments.Count];
			var multipleLines = new Label [alignments.Count];

			var multiLineHeight = 5;

			foreach (var alignment in alignments) {
				singleLines [(int)alignment] = new Label (text) { TextAlignment = alignment, X = 0, Width = Dim.Fill (), Height = 1, ColorScheme = Colors.Dialog };
				multipleLines [(int)alignment] = new Label (text) { TextAlignment = alignment, X = 0, Width = Dim.Fill (), Height = multiLineHeight, ColorScheme = Colors.Dialog };
			}

			var label = new Label ($"Demonstrating single-line (should clip):") { Y = Pos.Bottom (unicodeCheckBox) + 1 };
			Top.Add (label);
			foreach (var alignment in alignments) {
				label = new Label ($"{alignment}:") { Y = Pos.Bottom (label) };
				Top.Add (label);
				singleLines [(int)alignment].Y = Pos.Bottom (label);
				Top.Add (singleLines [(int)alignment]);
				label = singleLines [(int)alignment];
			}

			label = new Label ($"Demonstrating multi-line and word wrap:") { Y = Pos.Bottom (label) };
			Top.Add (label);
			foreach (var alignment in alignments) {
				label = new Label ($"{alignment}:") { Y = Pos.Bottom (label) };
				Top.Add (label);
				multipleLines [(int)alignment].Y = Pos.Bottom (label);
				Top.Add (multipleLines [(int)alignment]);
				label = multipleLines [(int)alignment];
			}

			unicodeCheckBox.Toggled += (previous) => {
				foreach (var alignment in alignments) {
					singleLines [(int)alignment].Text = previous ? text : unicode;
					multipleLines [(int)alignment].Text = previous ? text : unicode;
				}
			};
		}
	}
}