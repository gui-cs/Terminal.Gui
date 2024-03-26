using System.Collections.Generic;
using System.IO;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Unicode", "Tries to test Unicode in all controls (#204)")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Controls")]
public class UnicodeInMenu : Scenario
{
    public override void Setup ()
    {
        var unicode =
            "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

        var gitString =
            $"gui.cs 糊 (hú) {
                CM.Glyphs.IdenticalTo
            } {
                CM.Glyphs.DownArrow
            }18 {
                CM.Glyphs.UpArrow
            }10 {
                CM.Glyphs.VerticalFourDots
            }1 {
                CM.Glyphs.HorizontalEllipsis
            }";

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem (
                                 "_Файл",
                                 new MenuItem []
                                 {
                                     new (
                                          "_Создать",
                                          "Creates new file",
                                          null
                                         ),
                                     new ("_Открыть", "", null),
                                     new ("Со_хранить", "", null),
                                     new (
                                          "_Выход",
                                          "",
                                          () => Application.RequestStop ()
                                         )
                                 }
                                ),
                new MenuBarItem (
                                 "_Edit",
                                 new MenuItem []
                                 {
                                     new ("_Copy", "", null), new ("C_ut", "", null),
                                     new ("_糊", "hú (Paste)", null)
                                 }
                                )
            ]
        };
        Top.Add (menu);

        var statusBar = new StatusBar (
                                       new StatusItem []
                                       {
                                           new (
                                                Application.QuitKey,
                                                $"{Application.QuitKey} Выход",
                                                () => Application.RequestStop ()
                                               ),
                                           new (KeyCode.Null, "~F2~ Создать", null),
                                           new (KeyCode.Null, "~F3~ Со_хранить", null)
                                       }
                                      );
        Top.Add (statusBar);

        var label = new Label { X = 0, Y = 1, Text = "Label:" };
        Win.Add (label);

        var testlabel = new Label
        {
            X = 20,
            Y = Pos.Y (label),
            AutoSize = false,
            Width = Dim.Percent (50),
            Text = gitString
        };
        Win.Add (testlabel);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "Label (CanFocus):" };
        Win.Add (label);
        var sb = new StringBuilder ();
        sb.Append ('e');
        sb.Append ('\u0301');
        sb.Append ('\u0301');

        testlabel = new Label
        {
            X = 20,
            Y = Pos.Y (label),
            AutoSize = false,
            Width = Dim.Percent (50),
            CanFocus = true,
            HotKeySpecifier = new Rune ('&'),
            Text = $"Should be [e with two accents, but isn't due to #2616]: [{sb}]"
        };
        Win.Add (testlabel);
        label = new Label { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "Button:" };
        Win.Add (label);
        var button = new Button { X = 20, Y = Pos.Y (label), Text = "A123456789♥♦♣♠JQK" };
        Win.Add (button);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "CheckBox:" };
        Win.Add (label);

        var checkBox = new CheckBox
        {
            X = 20,
            Y = Pos.Y (label),
            AutoSize = false,
            Width = Dim.Percent (50),
            Height = 1,
            Text = gitString
        };

        var checkBoxRight = new CheckBox
        {
            X = 20,
            Y = Pos.Bottom (checkBox),
            AutoSize = false,
            Width = Dim.Percent (50),
            Height = 1,
            TextAlignment = TextAlignment.Right,
            Text = $"Align Right - {gitString}"
        };
        Win.Add (checkBox, checkBoxRight);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (checkBoxRight) + 1, Text = "ComboBox:" };
        Win.Add (label);
        var comboBox = new ComboBox { X = 20, Y = Pos.Y (label), Width = Dim.Percent (50) };
        comboBox.SetSource (new List<string> { gitString, "Со_хранить" });

        Win.Add (comboBox);
        comboBox.Text = gitString;

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (label) + 2, Text = "HexView:" };
        Win.Add (label);

        var hexView = new HexView (new MemoryStream (Encoding.ASCII.GetBytes (gitString + " Со_хранить")))
        {
            X = 20, Y = Pos.Y (label), Width = Dim.Percent (60), Height = 5
        };
        Win.Add (hexView);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (hexView) + 1, Text = "ListView:" };
        Win.Add (label);

        var listView = new ListView
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Height = 3,
            Source = new ListWrapper (
                                      new List<string> { "item #1", gitString, "Со_хранить", unicode }
                                     )
        };
        Win.Add (listView);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (listView) + 1, Text = "RadioGroup:" };
        Win.Add (label);

        var radioGroup = new RadioGroup
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            RadioLabels = new [] { "item #1", gitString, "Со_хранить", "𝔽𝕆𝕆𝔹𝔸ℝ" }
        };
        Win.Add (radioGroup);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (radioGroup) + 1, Text = "TextField:" };
        Win.Add (label);

        var textField = new TextField
        {
            X = 20, Y = Pos.Y (label), Width = Dim.Percent (60), Text = gitString + " = Со_хранить"
        };
        Win.Add (textField);

        label = new Label { X = Pos.X (label), Y = Pos.Bottom (textField) + 1, Text = "TextView:" };
        Win.Add (label);

        var textView = new TextView
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Height = 5,
            Text = unicode
        };
        Win.Add (textView);
    }
}
