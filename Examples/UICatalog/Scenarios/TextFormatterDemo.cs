using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            Title = GetQuitKeyAndName ()
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
            SchemeName = "TopLevel",
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
            CheckedState = app.HotKeySpecifier == (Rune)' ' ? CheckState.Checked : CheckState.UnChecked
        };

        app.Add (unicodeCheckBox);
        
        List<Alignment> alignments = new () { Alignment.Start, Alignment.End, Alignment.Center, Alignment.Fill };
        Label [] singleLines = new Label [alignments.Count];
        Label [] multipleLines = new Label [alignments.Count];

        var multiLineHeight = 5;

        for (int i = 0; i < alignments.Count; i++)
        {
            singleLines [i] = new ()
            {
                TextAlignment = alignments [i],
                X = 0,

                Width = Dim.Fill (),
                Height = 1,
                SchemeName = "Dialog",
                Text = text
            };

            multipleLines [i] = new ()
            {
                TextAlignment = alignments [i],
                X = 0,

                Width = Dim.Fill (),
                Height = multiLineHeight,
                SchemeName = "Dialog",
                Text = text
            };
        }

        var label = new Label
        {
            Y = Pos.Bottom (unicodeCheckBox) + 1, Text = "Demonstrating multi-line and word wrap:"
        };
        app.Add (label);

        for (int i = 0; i < alignments.Count; i++)
        {
            label = new () { Y = Pos.Bottom (label), Text = $"{alignments [i]}:" };
            app.Add (label);
            singleLines [i].Y = Pos.Bottom (label);
            app.Add (singleLines [i]);
            label = singleLines [i];
        }

        label = new () { Y = Pos.Bottom (label), Text = "Demonstrating multi-line and word wrap:" };
        app.Add (label);

        for (int i = 0; i < alignments.Count; i++)
        {
            label = new () { Y = Pos.Bottom (label), Text = $"{alignments [i]}:" };
            app.Add (label);
            multipleLines [i].Y = Pos.Bottom (label);
            app.Add (multipleLines [i]);
            label = multipleLines [i];
        }

        unicodeCheckBox.CheckedStateChanging += (s, e) =>
                                   {
                                       for (int i = 0; i < alignments.Count; i++)
                                       {
                                           singleLines [i].Text = e.Result == CheckState.Checked ? text : unicode;
                                           multipleLines [i].Text = e.Result == CheckState.Checked ? text : unicode;
                                       }
                                   };

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
