using NStack;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Unicode", Description: "Tries to test Unicode in all controls (#204)")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("Controls")]
	class UnicodeInMenu : Scenario {
		public override void Setup ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_Файл", new MenuItem [] {
					new MenuItem ("_Создать", "Creates new file", null),
					new MenuItem ("_Открыть", "", null),
					new MenuItem ("Со_хранить", "", null),
					new MenuItem ("_Выход", "", () => Application.RequestStop() )
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				})
			});
			Top.Add (menu);

			var label = new Label ("Button:") { X = 0, Y = 1 };
			Win.Add (label);
			var button2 = new Button ("Со_хранить") { X = 15, Y = Pos.Y (label), Width = Dim.Percent (50), };
			Win.Add (button2);

			label = new Label ("CheckBox:") { X = Pos.X (label), Y = Pos.Bottom (label) + 1 };
			Win.Add (label);
			var checkBox = new CheckBox (" ~  s  gui.cs   master ↑10") { X = 15, Y = Pos.Y (label), Width = Dim.Percent (50) };
			Win.Add (checkBox);

			label = new Label ("ComboBox:") { X = Pos.X (label), Y = Pos.Bottom (label) + 1 };
			Win.Add (label);
			var comboBox = new ComboBox () {
				X = 15,
				Y = Pos.Y (label),
				Width = Dim.Percent (50),
				ColorScheme = Colors.Error
			};
			comboBox.SetSource (new List<string> () { "item #1", " ~  s  gui.cs   master ↑10", "Со_хранить" });

			Win.Add (comboBox);
			comboBox.Text = " ~  s  gui.cs   master ↑10";

			label = new Label ("HexView:") { X = Pos.X (label), Y = Pos.Bottom (label) + 2 };
			Win.Add (label);
			var hexView = new HexView (new System.IO.MemoryStream (Encoding.ASCII.GetBytes (" ~  s  gui.cs   master ↑10 Со_хранить"))) {
				X = 15,
				Y = Pos.Y (label),
				Width = Dim.Percent (60),
				Height = 5
			};
			Win.Add (hexView);

			label = new Label ("ListView:") { X = Pos.X (label), Y = Pos.Bottom (hexView) + 1 };
			Win.Add (label);
			var listView = new ListView (new List<string> () { "item #1", " ~  s  gui.cs   master ↑10", "Со_хранить" }) {
				X = 15,
				Y = Pos.Y (label),
				Width = Dim.Percent (60),
				Height = 3,
				ColorScheme = Colors.Menu
			};
			Win.Add (listView);

			label = new Label ("RadioGroup:") { X = Pos.X (label), Y = Pos.Bottom (listView) + 1 };
			Win.Add (label);
			var radioGroup = new RadioGroup (new ustring [] { "item #1", " ~  s  gui.cs   master ↑10", "Со_хранить" }, selected: 0) {
				X = 15,
				Y = Pos.Y (label),
				Width = Dim.Percent (60),
				ColorScheme = Colors.Menu
			};
			Win.Add (radioGroup);

			label = new Label ("TextField:") { X = Pos.X (label), Y = Pos.Bottom (radioGroup) + 1 };
			Win.Add (label);
			var textField = new TextField (" ~  s  gui.cs   master ↑10 = Со_хранить") { X = 15, Y = Pos.Y (label), Width = Dim.Percent (60) };
			Win.Add (textField);

			label = new Label ("TextView:") { X = Pos.X (label), Y = Pos.Bottom (textField) + 1 };
			Win.Add (label);
			var textView = new TextView () {
				X = 15,
				Y = Pos.Y (label),
				Width = Dim.Percent (60),
				Height = 3,
				ColorScheme = Colors.Menu,
				Text = " ~  s  gui.cs   master ↑10\nСо_хранить",
			};
			Win.Add (textView);

			//label = new Label ("Charset:") { 
			//	X = Pos.Percent(75) + 1, 
			//	Y = 0,
			//};
			//Win.Add (label);
			//var charset = new Label ("") { 
			//	X = Pos.Percent(75) + 1, 
			//	Y = Pos.Y (label) + 1,
			//	Width = Dim.Fill (1),
			//	Height = Dim.Fill (),
			//	ColorScheme = Colors.Dialog
			//};
			//Win.Add (charset);

			// Move Win down to row 1, below menu
			Win.Y = 1;
			Top.LayoutSubviews ();
		}
	}
}
