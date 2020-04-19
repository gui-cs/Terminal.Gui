using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	class DimAndPosLayout : Scenario {
		public DimAndPosLayout ()
		{
			Name = "DimAndPosLayout";
			Description = "Demonstrates using the Dim and Pos Layout System.";
		}

		public override void Run ()
		{
			var ntop = new Toplevel (new Rect(0, 0, Application.Driver.Cols, Application.Driver.Rows));
			var win = new FrameView ($"ESC to Close - Scenario: {Name}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.OnKeyUp += (KeyEvent ke) => {
				if (ke.Key == Key.Esc) {
					// BUGBUG: This causes a StackOverflow 
					ntop.Running = false;
				}
			};

			// Demonstrate using Dim to create a ruler that always measures the top-level window's width
			// BUGBUG: Dim.Fill returns too big a value sometimes.
			var labelRuler = new Label ("ruler") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (1),  // BUGBUG: I don't think this should be needed; DimFill() should respect container's frame. X does.
				ColorScheme = Colors.Error
			};

			Application.OnResized += () => {
				const string rule = "|123456789";
				labelRuler.Text = rule.Repeat ((int)Math.Ceiling((double)(labelRuler.Bounds.Width) / (double)rule.Length))[0..(labelRuler.Bounds.Width)];
			};

			win.Add (labelRuler);

			// Demonstrate using Dim to create a window that fills the parent with a margin
			int margin = 8;
			var subWin = new Window ($"Sub Windoww with 8 character margin") {
				X = margin,
				Y = margin,
				Width = Dim.Fill (margin),
				Height = Dim.Fill (margin)
			};
			win.Add (subWin);

			int i = 1;
			string txt = "Hello world, how are you doing today";
			var labelList = new List<Label> ();
			labelList.Add (new Label ($"Label:"));
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Left, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1 });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Right, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1 });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Centered, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1 });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Justified, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1});

			subWin.Add (labelList.ToArray ());
			//subWin.LayoutSubviews ();

			ntop.Add (win);
			Application.Run (ntop);
		}

	}

	public static class StringExtensions {
		public static string Repeat (this string instr, int n)
		{
			if (n <= 0) {
				return null;
			}

			if (string.IsNullOrEmpty (instr) || n == 1) {
				return instr;
			}

			return new StringBuilder (instr.Length * n)
									.Insert (0, instr, n)
									.ToString ();
		}
	}
}