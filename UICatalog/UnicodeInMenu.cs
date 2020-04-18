using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	class UnicodeInMenu : Scenario {

		public UnicodeInMenu ()
		{
			Name = "Unicode In Menu";
			Description = "Unicode menus per PR #204";
		}

		public override void Run (Toplevel top)
		{
			base.Run (top);

			var tframe = top.Frame;
			var ntop = new Toplevel (tframe);

			var win = new Window ($"ESC to Close - Scenario: {Name}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			ntop.Add (win);

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_Файл", new MenuItem [] {
					new MenuItem ("_Создать", "Creates new file", null),
					new MenuItem ("_Открыть", "", null),
					new MenuItem ("Со_хранить", "", null),
					new MenuItem ("_Выход", "", () => top.Running = false )
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				})
			});

			ntop.Add (menu);

			Application.Run (ntop);
		}
	}
}
