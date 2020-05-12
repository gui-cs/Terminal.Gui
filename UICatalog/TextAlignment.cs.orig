using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	class TextAlignment : Scenario {
		public TextAlignment ()
		{
			Name = "Text Alignment";
			Description = "Demonstrates text alignment";
		}

		public override void Run (Toplevel top)
		{
			var tframe = top.Frame;
			var ntop = new Toplevel (tframe);

			var win = new Window ($"ESC to Close - Scenario: {Name}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			ntop.Add (win);

			int i = 0;
			string txt = "Hello world, how are you doing today";
			win.Add (
				new Label (new Rect (0, 1, 40, 3), $"{i+1}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Left },
				new Label (new Rect (0, 3, 40, 3), $"{i+2}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Right },
				new Label (new Rect (0, 5, 40, 3), $"{i+3}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Centered },
				new Label (new Rect (0, 7, 40, 3), $"{i+4}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Justified }
			);

			Application.Run (ntop);
		}

	}
}