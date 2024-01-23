using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Unicode", "Tries to test Unicode in all controls (#204)")]
[ScenarioCategory ("Text and Formatting"), ScenarioCategory ("Controls")]
public class UnicodeInMenu : Scenario {
	public override void Setup ()
	{
		string unicode = "Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴ\nτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.\nΜονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.";

		string gitString = $"gui.cs 糊 (hú) {ConfigurationManager.Glyphs.IdenticalTo} {ConfigurationManager.Glyphs.DownArrow}18 {ConfigurationManager.Glyphs.UpArrow}10 {ConfigurationManager.Glyphs.VerticalFourDots}1 {ConfigurationManager.Glyphs.HorizontalEllipsis}";

		var menu = new MenuBar (new MenuBarItem [] {
			new ("_Файл", new MenuItem [] {
				new ("_Создать", "Creates new file", null),
				new ("_Открыть", "", null),
				new ("Со_хранить", "", null),
				new ("_Выход", "", () => Application.RequestStop ())
			}),
			new ("_Edit", new MenuItem [] {
				new ("_Copy", "", null),
				new ("C_ut", "", null),
				new ("_糊", "hú (Paste)", null)
			})
		});
		Application.Top.Add (menu);

		var statusBar = new StatusBar (new StatusItem [] {
			new (Application.QuitKey, $"{Application.QuitKey} Выход", () => Application.RequestStop ()),
			new (KeyCode.Null, "~F2~ Создать", null),
			new (KeyCode.Null, "~F3~ Со_хранить", null)
		});
		Application.Top.Add (statusBar);

		var label = new Label () {
			Text = "Label:",
			X = 0,
			Y = 1
		};
		Win.Add (label);
		var testlabel = new Label (gitString) { AutoSize = false, X = 20, Y = Pos.Y (label), Width = Dim.Percent (50) };
		Win.Add (testlabel);

		label = new Label () {
			Text = "Label (CanFocus):",
			X = Pos.X (label),
			Y = Pos.Bottom (label) + 1
		};
		Win.Add (label);
		var sb = new StringBuilder ();
		sb.Append ('e');
		sb.Append ('\u0301');
		sb.Append ('\u0301');
		testlabel = new Label ($"Should be [e with two accents, but isn't due to #2616]: [{sb}]") { AutoSize = false, X = 20, Y = Pos.Y (label), Width = Dim.Percent (50), CanFocus = true, HotKeySpecifier = new Rune ('&') };
		Win.Add (testlabel);
		label = new Label () {
			Text = "Button:",
			X = Pos.X (label),
			Y = Pos.Bottom (label) + 1
		};
		Win.Add (label);
		var button = new Button () {
			Text = "A123456789♥♦♣♠JQK",
			X = 20,
			Y = Pos.Y (label)
		};
		Win.Add (button);

		label = new Label () {
			Text = "CheckBox:",
			X = Pos.X (label),
			Y = Pos.Bottom (label) + 1
		};
		Win.Add (label);
		var checkBox = new CheckBox (gitString) { AutoSize = false, X = 20, Y = Pos.Y (label), Width = Dim.Percent (50) };
		var checkBoxRight = new CheckBox ($"Align Right - {gitString}") { AutoSize = false, X = 20, Y = Pos.Bottom (checkBox), Width = Dim.Percent (50), TextAlignment = TextAlignment.Right };
		Win.Add (checkBox, checkBoxRight);

		label = new Label () {
			Text = "ComboBox:",
			X = Pos.X (label),
			Y = Pos.Bottom (checkBoxRight) + 1
		};
		Win.Add (label);
		var comboBox = new ComboBox () {
			AutoSize = false,
			X = 20,
			Y = Pos.Y (label),
			Width = Dim.Percent (50)
		};
		comboBox.SetSource (new List<string> () { gitString, "Со_хранить" });

		Win.Add (comboBox);
		comboBox.Text = gitString;

		label = new Label () {
			Text = "HexView:",
			X = Pos.X (label),
			Y = Pos.Bottom (label) + 2
		};
		Win.Add (label);
		var hexView = new HexView (new System.IO.MemoryStream (Encoding.ASCII.GetBytes (gitString + " Со_хранить"))) {
			X = 20,
			Y = Pos.Y (label),
			Width = Dim.Percent (60),
			Height = 5
		};
		Win.Add (hexView);

		label = new Label () {
			Text = "ListView:",
			X = Pos.X (label),
			Y = Pos.Bottom (hexView) + 1
		};
		Win.Add (label);
		var listView = new ListView (new List<string> () { "item #1", gitString, "Со_хранить", unicode }) {
			X = 20,
			Y = Pos.Y (label),
			Width = Dim.Percent (60),
			Height = 3
		};
		Win.Add (listView);

		label = new Label () {
			Text = "RadioGroup:",
			X = Pos.X (label),
			Y = Pos.Bottom (listView) + 1
		};
		Win.Add (label);
		var radioGroup = new RadioGroup (new string [] { "item #1", gitString, "Со_хранить", "𝔽𝕆𝕆𝔹𝔸ℝ" }, 0) {
			X = 20,
			Y = Pos.Y (label),
			Width = Dim.Percent (60)
		};
		Win.Add (radioGroup);

		label = new Label () {
			Text = "TextField:",
			X = Pos.X (label),
			Y = Pos.Bottom (radioGroup) + 1
		};
		Win.Add (label);
		var textField = new TextField (gitString + " = Со_хранить") { X = 20, Y = Pos.Y (label), Width = Dim.Percent (60) };
		Win.Add (textField);

		label = new Label () {
			Text = "TextView:",
			X = Pos.X (label),
			Y = Pos.Bottom (textField) + 1
		};
		Win.Add (label);
		var textView = new TextView () {
			X = 20,
			Y = Pos.Y (label),
			Width = Dim.Percent (60),
			Height = 5,
			Text = unicode
		};
		Win.Add (textView);
	}
}