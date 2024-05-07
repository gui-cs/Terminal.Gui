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
    public override void Main ()
    {
        Application.Init ();

        var app = new Window
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}"
        };

        // Make Win smaller so sizing the window horizontally will make the
        // labels shrink to zero-width
        app.X = 10;
        app.Width = Dim.Fill (10);

        var text = "Hello world, how are you today? Pretty neat!\nSecond line\n\nFourth Line.";

        var unicode =
            "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

        var blockText = new Label
        {
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            X = 0,
            Y = 0,

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
        app.Add (blockText);

        var unicodeCheckBox = new CheckBox
        {
            X = 0,
            Y = Pos.Bottom (blockText) + 1,
            Text = "Unicode",
            Checked = app.HotKeySpecifier == (Rune)' '
        };

        app.Add (unicodeCheckBox);

        List<TextAlignment> alignments = Enum.GetValues (typeof (TextAlignment)).Cast<TextAlignment> ().ToList ();
        Label [] singleLines = new Label [alignments.Count];
        Label [] multipleLines = new Label [alignments.Count];

        var multiLineHeight = 5;

        foreach (TextAlignment alignment in alignments)
        {
            singleLines [(int)alignment] = new()
            {
                TextAlignment = alignment,
                X = 0,

                Width = Dim.Fill (),
                Height = 1,
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                Text = text
            };

            multipleLines [(int)alignment] = new()
            {
                TextAlignment = alignment,
                X = 0,

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
        app.Add (label);

        foreach (TextAlignment alignment in alignments)
        {
            label = new() { Y = Pos.Bottom (label), Text = $"{alignment}:" };
            app.Add (label);
            singleLines [(int)alignment].Y = Pos.Bottom (label);
            app.Add (singleLines [(int)alignment]);
            label = singleLines [(int)alignment];
        }

        label = new() { Y = Pos.Bottom (label), Text = "Demonstrating multi-line and word wrap:" };
        app.Add (label);

        foreach (TextAlignment alignment in alignments)
        {
            label = new() { Y = Pos.Bottom (label), Text = $"{alignment}:" };
            app.Add (label);
            multipleLines [(int)alignment].Y = Pos.Bottom (label);
            app.Add (multipleLines [(int)alignment]);
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

        Application.Run (app);
        app.Dispose ();
    }
}
