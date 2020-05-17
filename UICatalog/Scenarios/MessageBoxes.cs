using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "MessageBoxes", Description: "Demonstrates how to use MessageBoxes")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	class MessageBoxes : Scenario {
		public override void Run () {
			var top = new Toplevel ();
			var win = new Window ($"ESC to Close - Scenario: {GetName ()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			top.Add (win);

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_Simple Query...", "A simple query message box", () =>  MessageBox.Query (0, 6, "MessageBox.Query", "Minimum size was specified", "Ok")),
				new MenuBarItem ("_Error Query...", "A error query message box", () =>  MessageBox.ErrorQuery (0, 6, "MessageBox.Query", "Minimum size was specified", "Ok")),
				new MenuBarItem ("_Long Text...", "Demo long text", () =>  MessageBox.Query (0, 6, "About UI Catalog", "This is a very long title. It is longer than the width of the screen. Will it Wrap? I bet  it will not wrap", "Ok")),
			});
			top.Add (menu);
			Application.Run (top);
		}

	}
}
