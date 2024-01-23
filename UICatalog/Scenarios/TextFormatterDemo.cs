using System;
using System.Linq;
using System.Text;
using Terminal.Gui;


namespace UICatalog.Scenarios;

[ScenarioMetadata (Name: "TextFormatter Demo", Description: "Demos and tests the TextFormatter class.")]
[ScenarioCategory ("Text and Formatting")]
public class TextFormatterDemo : Scenario {
	public override void Setup ()
	{
		// TODO: Move this to another Scenario that specifically tests `Views` that have no subviews.
		//Top.Text = "Press CTRL-Q to Quit. This is the Text for the TopLevel View. TextAlignment.Centered was specified. It is intentionally very long to illustrate word wrap.\n" +
		//	"<-- There is a new line here to show a hard line break. You should see this text bleed underneath the subviews, which start at Y = 3.";
		//Top.TextAlignment = TextAlignment.Centered;
		//Top.ColorScheme = Colors.ColorSchemes ["Base"];

		// Make Win smaller so sizing the window horizontally will make the
		// labels shrink to zero-width
		Win.X = 10;
		Win.Width = Dim.Fill (10);

		string text = "Hello world, how are you today? Pretty neat!\nSecond line\n\nFourth Line.";
		string unicode = "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

		Label blockText = new Label () { ColorScheme = Colors.ColorSchemes ["TopLevel"], X = 0, Y = 0, Height = 10, Width = Dim.Fill (0), AutoSize = false };

		var block = new StringBuilder ();
		block.AppendLine ("  ▄████  █    ██  ██▓      ▄████▄    ██████ ");
		block.AppendLine (" ██▒ ▀█▒ ██  ▓██▒▓██▒     ▒██▀ ▀█  ▒██    ▒ ");
		block.AppendLine ("▒██░▄▄▄░▓██  ▒██░▒██▒     ▒▓█    ▄ ░ ▓██▄   ");
		block.AppendLine ("░▓█  ██▓▓▓█  ░██░░██░     ▒▓▓▄ ▄██▒  ▒   ██▒");
		block.AppendLine ("░▒▓███▀▒▒▒█████▓ ░██░ ██▓ ▒ ▓███▀ ░▒██████▒▒");
		block.AppendLine (" ░▒   ▒ ░▒▓▒ ▒ ▒ ░▓   ▒▓▒ ░ ░▒ ▒  ░▒ ▒▓▒ ▒ ░");
		block.AppendLine ("  ░   ░ ░░▒░ ░ ░  ▒ ░ ░▒    ░  ▒   ░ ░▒  ░ ░");
		block.AppendLine ("░ ░   ░  ░░░ ░ ░  ▒ ░ ░   ░        ░  ░  ░  ");
		block.AppendLine ("      ░    ░      ░    ░  ░ ░            ░  ");
		block.AppendLine ("                       ░  ░                 ");
		blockText.Text = block.ToString (); // .Replace(" ", "\u00A0"); // \u00A0 is 'non-breaking space
		Win.Add (blockText);

		var unicodeCheckBox = new CheckBox {
			X = 0,
			Y = Pos.Bottom (blockText) + 1,
			Text = "Unicode",
			Checked = Application.Top.HotKeySpecifier == (Rune)' '
		};

		Win.Add (unicodeCheckBox);

		var alignments = Enum.GetValues (typeof (Terminal.Gui.TextAlignment)).Cast<Terminal.Gui.TextAlignment> ().ToList ();
		var singleLines = new Label [alignments.Count];
		var multipleLines = new Label [alignments.Count];

		var multiLineHeight = 5;

		foreach (var alignment in alignments) {
			singleLines [(int)alignment] = new Label { TextAlignment = alignment, X = 0, Width = Dim.Fill (), Height = 1, ColorScheme = Colors.ColorSchemes ["Dialog"], AutoSize = false, Text = text };
			multipleLines [(int)alignment] = new Label { TextAlignment = alignment, X = 0, Width = Dim.Fill (), Height = multiLineHeight, ColorScheme = Colors.ColorSchemes ["Dialog"], AutoSize = false, Text = text };
		}

		var label = new Label { Y = Pos.Bottom (unicodeCheckBox) + 1, Text = "Demonstrating multi-line and word wrap:" };
		Win.Add (label);
		foreach (var alignment in alignments) {
			label = new Label { Y = Pos.Bottom (label), Text = $"{alignment}:" };
			Win.Add (label);
			singleLines [(int)alignment].Y = Pos.Bottom (label);
			Win.Add (singleLines [(int)alignment]);
			label = singleLines [(int)alignment];
		}

		label = new Label { Y = Pos.Bottom (label), Text = "Demonstrating multi-line and word wrap:" };
		Win.Add (label);
		foreach (var alignment in alignments) {
			label = new Label { Y = Pos.Bottom (label), Text = $"{alignment}:" };
			Win.Add (label);
			multipleLines [(int)alignment].Y = Pos.Bottom (label);
			Win.Add (multipleLines [(int)alignment]);
			label = multipleLines [(int)alignment];
		}

		unicodeCheckBox.Toggled += (s, e) => {
			foreach (var alignment in alignments) {
				singleLines [(int)alignment].Text = e.OldValue == true ? text : unicode;
				multipleLines [(int)alignment].Text = e.OldValue == true ? text : unicode;
			}
		};
	}
}