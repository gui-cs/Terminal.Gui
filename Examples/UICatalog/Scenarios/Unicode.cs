#nullable enable

using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Unicode", "Tries to test Unicode in all controls (#204)")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Controls")]
public class UnicodeInMenu : Scenario
{
    public override void Main ()
    {
        string unicode =
            "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

        string gitString =
            $"gui.cs 糊 (hú) {Glyphs.IdenticalTo} {Glyphs.DownArrow}18 {Glyphs.UpArrow}10 {Glyphs.VerticalFourDots}1 {Glyphs.HorizontalEllipsis}";

        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        // MenuBar
        MenuBar menu = new ();

        menu.Add (
                  new MenuBarItem (
                                   "_Файл",
                                   [
                                       new MenuItem
                                       {
                                           Title = "_Создать",
                                           HelpText = "Creates new file"
                                       },
                                       new MenuItem
                                       {
                                           Title = "_Открыть"
                                       },
                                       new MenuItem
                                       {
                                           Title = "Со_хранить"
                                       },
                                       new MenuItem
                                       {
                                           Title = "_Выход",
                                           Action = () => appWindow.RequestStop ()
                                       }
                                   ]
                                  )
                 );

        menu.Add (
                  new MenuBarItem (
                                   "_Edit",
                                   [
                                       new MenuItem
                                       {
                                           Title = Strings.cmdCopy
                                       },
                                       new MenuItem
                                       {
                                           Title = Strings.cmdCut
                                       },
                                       new MenuItem
                                       {
                                           Title = "_糊",
                                           HelpText = "hú (Paste)"
                                       }
                                   ]
                                  )
                 );

        appWindow.Add (menu);

        Label label = new () { X = 0, Y = Pos.Bottom (menu), Text = "Label:" };
        appWindow.Add (label);

        Label testlabel = new ()
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (50),
            Text = gitString
        };
        appWindow.Add (testlabel);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "Label (CanFocus):" };
        appWindow.Add (label);

        StringBuilder sb = new ();
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

        Button button = new () { X = 20, Y = Pos.Y (label), Text = "A123456789♥♦♣♠JQK" };
        appWindow.Add (button);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (label) + 1, Text = "CheckBox:" };
        appWindow.Add (label);

        CheckBox checkBox = new ()
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (50),
            Height = 1,
            Text = gitString
        };
        appWindow.Add (checkBox);

        CheckBox checkBoxRight = new ()
        {
            X = 20,
            Y = Pos.Bottom (checkBox),
            Width = Dim.Percent (50),
            Height = 1,
            TextAlignment = Alignment.End,
            Text = $"End - {gitString}"
        };
        appWindow.Add (checkBoxRight);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (checkBoxRight) + 2, Text = "HexView:" };
        appWindow.Add (label);

        HexView hexView = new (new MemoryStream (Encoding.ASCII.GetBytes (gitString + " Со_хранить")))
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Height = 5
        };
        appWindow.Add (hexView);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (hexView) + 1, Text = "ListView:" };
        appWindow.Add (label);

        ListView listView = new ()
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

        label = new () { X = Pos.X (label), Y = Pos.Bottom (listView) + 1, Text = "OptionSelector:" };
        appWindow.Add (label);

        OptionSelector optionSelector = new ()
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Labels = ["item #1", gitString, "Со_хранить", "𝔽𝕆𝕆𝔹𝔸ℝ"]
        };
        appWindow.Add (optionSelector);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (optionSelector) + 1, Text = "TextField:" };
        appWindow.Add (label);

        TextField textField = new ()
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Text = gitString + " = Со_хранить"
        };
        appWindow.Add (textField);

        label = new () { X = Pos.X (label), Y = Pos.Bottom (textField) + 1, Text = "TextView:" };
        appWindow.Add (label);

        TextView textView = new ()
        {
            X = 20,
            Y = Pos.Y (label),
            Width = Dim.Percent (60),
            Height = 5,
            Text = unicode
        };
        appWindow.Add (textView);

        // StatusBar
        StatusBar statusBar = new (
                                   [
                                       new (
                                            Application.QuitKey,
                                            "Выход",
                                            () => appWindow.RequestStop ()
                                           ),
                                       new (Key.F2, "Создать", null),
                                       new (Key.F3, "Со_хранить", null)
                                   ]
                                  );
        appWindow.Add (statusBar);

        app.Run (appWindow);
    }
}
