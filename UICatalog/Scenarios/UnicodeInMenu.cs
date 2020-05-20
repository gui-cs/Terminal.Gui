using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Unicode In Menu", Description: "Unicode menus per PR #204")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("Controls")]
	class UnicodeInMenu : Scenario {
		public override void Setup ()
		{
			Top = new Toplevel (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows));
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

			Win = new Window ($"Scenario: {GetName ()}") {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			Top.Add (Win);
		}
	}
}
