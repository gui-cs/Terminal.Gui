using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	/// <summary>
	/// This Scenario demonstrates how to use Termina.gui's Dim and Pos Layout System. 
	/// [x] - Using Dim.Fill to fill a window
	/// [x] - Using Dim.Fill and Dim.Pos to automatically align controls based on an initial control
	/// [ ] - ...
	/// </summary>
	[ScenarioMetadata (Name: "DimAndPosLayout", Description: "Demonstrates using the Dim and Pos Layout System")]
	[ScenarioCategory ("Layout")]
	class DimAndPosLayout : Scenario {

		public override void Setup ()
		{
			Top = new Toplevel (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows)) {
				LayoutStyle = LayoutStyle.Computed,
			};

			Win = new Window ($"ESC to Close - Scenario: {GetName ()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			// Implement our own exit logic
			Win.OnKeyUp += (KeyEvent ke) => {
				if (ke.Key == Key.Esc) {
					// BUGBUG: This causes a StackOverflow 
					Top.Running = false;
				}
			};

			Top.Add (Win);


			// Demonstrate using Dim to create a ruler that always measures the top-level window's width
			// BUGBUG: Dim.Fill returns too big a value sometimes.
			//const string rule = "|123456789";
			//var labelRuler = new Label ("ruler") {
			//	X = 0,
			//	Y = 0,
			//	Width = Dim.Fill (1),  // BUGBUG: I don't think this should be needed; DimFill() should respect container's frame. X does.
			//	ColorScheme = Colors.Error
			//};

			//Application.OnResized += () => {
			//	labelRuler.Text = rule.Repeat ((int)Math.Ceiling((double)(labelRuler.Bounds.Width) / (double)rule.Length))[0..(labelRuler.Bounds.Width)];
			//};

			//win.Add (labelRuler);

			// Demonstrate using Dim to create a window that fills the parent with a margin
			int margin = 20;
			var subWin = new Window ($"Sub Windoww with {margin} character margin") {
				X = margin,
				Y = 2,
				Width = Dim.Fill (margin),
				Height = Dim.Fill ()
			};
			Win.Add (subWin);

			int i = 1;
			string txt = "Hello world, how are you doing today";
			var labelList = new List<Label> ();
			labelList.Add (new Label ($"Label:"));
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Left, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1, ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Right, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1, ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Centered, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1, ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Justified, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()) + 1, ColorScheme = Colors.Dialog });

			subWin.Add (labelList.ToArray ());
			//subWin.LayoutSubviews ();
		}

		public override void Run ()
		{
			base.Run ();
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