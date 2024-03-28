using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("TextFormatter Demo", "Demos and tests the TextFormatter class.")]
[ScenarioCategory ("Text and Formatting")]
public class TextFormatterDemo : Scenario
{
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

        var text = "Hello world, how are you today? Pretty neat!\nSecond line\n\nFourth Line.";

        var unicode =
            "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

        var blockText = new Label
        {
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            X = 0,
            Y = 0,
            AutoSize = false,
            Height = 10,
            Width = Dim.Fill ()
        };

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

        var unicodeCheckBox = new CheckBox
        {
            X = 0,
            Y = Pos.Bottom (blockText) + 1,
            Text = "Unicode",
            Checked = Top.HotKeySpecifier == (Rune)' '
        };

        Win.Add (unicodeCheckBox);

        List<TextAlignment> alignments = Enum.GetValues (typeof (TextAlignment)).Cast<TextAlignment> ().ToList ();
        Label [] singleLines = new Label [alignments.Count];
        Label [] multipleLines = new Label [alignments.Count];

        var multiLineHeight = 5;

        foreach (TextAlignment alignment in alignments)
        {
            singleLines [(int)alignment] = new Label
            {
                TextAlignment = alignment,
                X = 0,
                AutoSize = false,
                Width = Dim.Fill (),
                Height = 1,
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                Text = text
            };

            multipleLines [(int)alignment] = new Label
            {
                TextAlignment = alignment,
                X = 0,
                AutoSize = false,
                Width = Dim.Fill (),
                Height = multiLineHeight,
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                Text = text
            };
        }

        var label = new Label
        {
            Y = Pos.Bottom (unicodeCheckBox) + 1, Text = "Demonstrating multi-line and word wrap:"
        };
        Win.Add (label);

        foreach (TextAlignment alignment in alignments)
        {
            label = new Label { Y = Pos.Bottom (label), Text = $"{alignment}:" };
            Win.Add (label);
            singleLines [(int)alignment].Y = Pos.Bottom (label);
            Win.Add (singleLines [(int)alignment]);
            label = singleLines [(int)alignment];
        }

        label = new Label { Y = Pos.Bottom (label), Text = "Demonstrating multi-line and word wrap:" };
        Win.Add (label);

        foreach (TextAlignment alignment in alignments)
        {
            label = new Label { Y = Pos.Bottom (label), Text = $"{alignment}:" };
            Win.Add (label);
            multipleLines [(int)alignment].Y = Pos.Bottom (label);
            Win.Add (multipleLines [(int)alignment]);
            label = multipleLines [(int)alignment];
        }

        unicodeCheckBox.Toggled += (s, e) =>
                                   {
                                       foreach (TextAlignment alignment in alignments)
                                       {
                                           singleLines [(int)alignment].Text = e.OldValue == true ? text : unicode;
                                           multipleLines [(int)alignment].Text = e.OldValue == true ? text : unicode;
                                       }
                                   };
    }
}
