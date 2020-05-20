﻿using System;
using System.IO;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "HexEditor", Description: "A Terminal.Gui binary (hex) editor via HexView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Text")]
	class HexEditor : Scenario {
		private string _fileName = "demo.bin";
		private HexView _hexView;
		private bool _saved = true;


		public override void Init (Toplevel top)
		{
			Top = top;
		}

		public override void Setup ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", () => New()),
					new MenuItem ("_Open", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					null,
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
				//new StatusItem(Key.Enter, "~ENTER~ ApplyEdits", () => { _hexView.ApplyEdits(); }),
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

			_hexView = new HexView (LoadFile()) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};


			Win.Add (_hexView);
		}

		private void New ()
		{
			Win.Title = _fileName = "Untitled";
			throw new NotImplementedException ();
		}

		private Stream LoadFile ()
		{
			MemoryStream stream = null;
			if (!_saved) {
				MessageBox.ErrorQuery (0, 10, "Not Implemented", "Functionality not yet implemented.", "Ok");
			}

			if (_fileName != null) {
				var bin = System.IO.File.ReadAllBytes (_fileName);
				stream = new MemoryStream (bin);
				Win.Title = _fileName;
				_saved = true;
			}
			return stream;
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
			//if (_textView != null && _textView.SelectedLength != 0) {
			//	_textView.Copy ();
			//}
		}

		private void Open ()
		{
			var d = new OpenDialog ("Open", "Open a file") { AllowsMultipleSelection = false };
			Application.Run (d);

			if (!d.Canceled) {
				_fileName = d.FilePaths [0];
				_hexView.Source = LoadFile ();
				_hexView.DisplayStart = 0;
			}
		}

		private void Save ()
		{
			if (_fileName != null) {
				using (FileStream fs = new FileStream (_fileName, FileMode.OpenOrCreate)) {
					_hexView.ApplyEdits ();
					_hexView.Source.CopyTo (fs);
					fs.Flush ();
				}
				_saved = true;
			}
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
