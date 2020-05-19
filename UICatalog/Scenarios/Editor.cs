using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Editor", Description: "A Terminal.Gui-based Text Editor via TextView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Text")]
	class Editor : Scenario {
		string _fileName = "demo.txt";

		public override void Init (Toplevel top)
		{
			Top = top;
		}

		public override void Setup ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Open", "", () => Save()),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", () => Copy()),
					new MenuItem ("C_ut", "", () => Cut()),
					new MenuItem ("_Paste", "", () => Paste())
				}),
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ Open", () => Open()),
				new StatusItem(Key.F3, "~F3~ Save", () => Save()),
				new StatusItem(Key.ControlQ, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);

			CreateDemoFile (_fileName);

			Win = new Window (_fileName ?? "Untitled") {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			Top.Add (Win);

			var text = new TextView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			if (_fileName != null)
				text.Text = System.IO.File.ReadAllText (_fileName);
			Win.Add (text);
		}

		private void Paste ()
		{
			MessageBox.ErrorQuery (0, 10, "Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Cut ()
		{
			MessageBox.ErrorQuery (0, 10, "Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Copy ()
		{
			MessageBox.ErrorQuery (0, 10, "Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Open ()
		{
			MessageBox.ErrorQuery (0, 10, "Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Save ()
		{
			MessageBox.ErrorQuery (0, 10, "Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private void CreateDemoFile(string fileName)
		{
			var sb = new StringBuilder ();
			// BUGBUG: #279 TextView does not know how to deal with \r\n, only \r
			sb.Append ("Hello world.\n");
			sb.Append ("This is a test of the Emergency Broadcast System.\n");

			var sw = System.IO.File.CreateText (fileName);
			sw.Write (sb.ToString ());
			sw.Close ();
		}

		public override void Run ()
		{
			Application.Run (Top);
		}
	}
}
