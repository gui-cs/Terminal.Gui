using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "TopLevelNoWindowBug", Description: "Illustrates that not having a Window causes Application.Run to wedge. #437")]
	[ScenarioCategory ("Bug Repro")]

	class TopLevelNoWindowBug : Scenario {

		public override void Run ()
		{
			var ntop = new Toplevel (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows));

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_Файл", new MenuItem [] {
					new MenuItem ("_Создать", "Creates new file", null),
					new MenuItem ("_Открыть", "", null),
					new MenuItem ("Со_хранить", "", null),
					new MenuItem ("_Выход", "", () => ntop.Running = false )
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				})
			});
			ntop.Add (menu);

			// BUGBUG: #437 This being commmented out causes Application.Run to wedge.
			//var win = new Window ($"Scenario: {GetName ()}") {
			//	X = 0,
			//	Y = 1,
			//	Width = Dim.Fill (),
			//	Height = Dim.Fill ()
			//};
			//ntop.Add (win);

			Application.Run (ntop);
		}
	}
}
