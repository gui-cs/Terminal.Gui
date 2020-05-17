using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "MessageBoxes", Description: "Demonstrates how to use MessageBoxes")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Bug Repro")]
	class MessageBoxes : Scenario {
		public override void Setup ()
		{
			var top = new Toplevel ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Application.RequestStop() )
				}),
				new MenuBarItem ("_Simple Query...", "A simple query message box", () =>  MessageBox.Query (0, 6, "MessageBox.Query", "Minimum size was specified", "Ok")),
				new MenuBarItem ("_Error Query...", "A error query message box", () =>  MessageBox.ErrorQuery (0, 6, "MessageBox.Query", "Minimum size was specified", "Ok")),
				// BUGBUG: Illustrates MessageBoxes do not deal with long text gracefully. Issue #432
				new MenuBarItem ("_Long Text...", "Demo long text", () =>  MessageBox.Query (0, 6, "About UI Catalog", "This is a very long title. It is longer than the width of the screen. Will it Wrap? I bet  it will not wrap", "Ok")),
			});
			top.Add (menu);

			var win = new Window ($"Scenario: {GetName ()}") {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			top.Add (win);

			Application.Run (top);
		}
	}
}
