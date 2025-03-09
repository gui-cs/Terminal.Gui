using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Unicode", "Tries to test Unicode in all controls (#204)")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Controls")]
public class UnicodeInMenu : Scenario
{
    public override void Main ()
    {
        var unicode =
            "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

        var gitString =
            $"gui.cs 糊 (hú) {
                Glyphs.IdenticalTo
            } {
                Glyphs.DownArrow
            }18 {
                Glyphs.UpArrow
            }10 {
                Glyphs.VerticalFourDots
            }1 {
                Glyphs.HorizontalEllipsis
            }";

        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        var menu = new MenuBar
        {
            Menus =
            [
                new (
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
                new (
                     "_Edit",
                     new MenuItem []
                     {
                         new ("_Copy", "", null), new ("C_ut", "", null),
                         new ("_糊", "hú (Paste)", null)
                     }
                    )
            ]
        };
        appWindow.Add (menu);

        var statusBar = new StatusBar (
                                       new Shortcut []
                                       {
                                           new (
                                                Application.QuitKey,
                                                "Выход",
                                                () => Application.RequestStop ()
                                               ),
                                           new (Key.F2, "Создать", null),
                                           new (Key.F3, "Со_хранить", null)
                                       }
                                      );
        appWindow.Add (statusBar);

        var label = new Label { X = 0, Y = 1, Text = "Label:" };
        appWindow.Add (label);

        var testlabel = new Label
        {
            X = 20,
            Y = Pos.Y (label),

            Width = Dim.Percent (50),
            Text = gitString
        };
        appWindow.Add (testlabel);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "Label (CanFocus):" };
        appWindow.Add (label);
        var sb = new StringBuilder ();
        sb.Append ('e');
        sb.Append ('\u0301');
        sb.Append ('\u0301');

        testlabel = new ()
        {
            X = 20,
            Y = Pos.Y (label),

            Width = Dim.Percent (50),
            CanFocus = true,
            HotKeySpecifier = new ('&'),
            Text = $"Should be [e with two accents, but isn't due to #2616]: [{sb}]"
        };
        appWindow.Add (testlabel);
        label = new () { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "Button:" };
        appWindow.Add (label);
        var button = new Button { X = 20, Y = Pos.Y (label), Text = "A123456789♥♦♣♠JQK" };
        appWindow.Add (button);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "CheckBox:" };
        appWindow.Add (label);

        var checkBox = new CheckBox
        {
            X = 20,
            Y = Pos.Y (label),

            Width = Dim.Percent (50),
            Height = 1,
            Text = gitString
        };

        var checkBoxRight = new CheckBox
        {
            X = 20,
            Y = Pos.Bottom (checkBox),

            Width = Dim.Percent (50),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = $"End - {gitString}"
        };
        appWindow.Add (checkBox, checkBoxRight);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (checkBoxRight) + 1, Text = "ComboBox:" };
        appWindow.Add (label);
        var comboBox = new ComboBox { X = 20, Y = Pos.Y (label), Width = Dim.Percent (50) };
        comboBox.SetSource (new ObservableCollection<string> { gitString, "Со_хранить" });

        appWindow.Add (comboBox);
        comboBox.Text = gitString;

        label = new () { X = Pos.X (label), Y = Pos.Bottom (label) + 2, Text = "HexView:" };
        appWindow.Add (label);

        var hexView = new HexView (new MemoryStream (Encoding.ASCII.GetBytes (gitString + " Со_хранить")))
        {
            X = 20, Y = Pos.Y (label), Width = Dim.Percent (60), Height = 5
        };
        appWindow.Add (hexView);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (hexView) + 1, Text = "ListView:" };
        appWindow.Add (label);

        var listView = new ListView
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Height = 3,
            Source = new ListWrapper<string> (
                                              ["item #1", gitString, "Со_хранить", unicode]
                                             )
        };
        appWindow.Add (listView);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (listView) + 1, Text = "RadioGroup:" };
        appWindow.Add (label);

        var radioGroup = new RadioGroup
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            RadioLabels = new [] { "item #1", gitString, "Со_хранить", "𝔽𝕆𝕆𝔹𝔸ℝ" }
        };
        appWindow.Add (radioGroup);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (radioGroup) + 1, Text = "TextField:" };
        appWindow.Add (label);

        var textField = new TextField
        {
            X = 20, Y = Pos.Y (label), Width = Dim.Percent (60), Text = gitString + " = Со_хранить"
        };
        appWindow.Add (textField);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (textField) + 1, Text = "TextView:" };
        appWindow.Add (label);

        var textView = new TextView
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Height = 5,
            Text = unicode
        };
        appWindow.Add (textView);

        // Run - Start the application.
        Application.Run (appWindow);

        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
