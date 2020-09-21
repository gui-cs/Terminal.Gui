using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "TopLevelNoWindowBug", Description: "Illustrates that not having a Window causes MenuBar to misbehave. #437")]
	[ScenarioCategory ("Bug Repro")]

	class TopLevelNoWindowBug : Scenario {

		public override void Run ()
		{
			Top?.Dispose ();

			//Top = new Toplevel (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows));

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_Файл", new MenuItem [] {
					new MenuItem ("_Создать", "Creates new file", null),
					new MenuItem ("_Открыть", "", null),
					new MenuItem ("Со_хранить", "", null),
					new MenuItem ("_Выход", "", () => { if (Quit ()) { Application.RequestStop(); } })
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				})
			});
			Top.Add (menu);

			// BUGBUG: #437 This being commented out causes menu to mis-behave
			var win = new Window ($"Scenario: {GetName ()}") {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			Top.Add (win);

			base.Run ();
		}

		private bool Quit ()
		{
			var n = MessageBox.Query (50, 7, $"Quit {GetName ()}", $"Are you sure you want to quit this {GetName ()}?", "Yes", "No");
			return n == 0;
		}
	}
}
