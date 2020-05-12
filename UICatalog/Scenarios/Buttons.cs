using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	class Buttons : Scenario {
		public Buttons()
		{
			Name = "Buttons";
			Description = "Demonstrates all sorts of Buttons";
		}

		public override void Run (Toplevel top) {
			var tframe = top.Frame;
			var ntop = new Toplevel (tframe);

			var win = new Window ($"ESC to Close - Scenario: {Name}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			ntop.Add (win);

			var button = new Button (10, 10, "This is a button") { Clicked = () => MessageBox.Query (50, 7, "Message", "Question?", "Yes", "No") };
			win.Add (button);

			Application.Run (ntop);
		}

	}
}
