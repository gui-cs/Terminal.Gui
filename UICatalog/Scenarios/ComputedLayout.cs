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
	[ScenarioMetadata (Name: "Computed Layout", Description: "Demonstrates using the Computed (Dim and Pos) Layout System")]
	[ScenarioCategory ("Layout")]
	class ComputedLayout : Scenario {

		public override void Setup ()
		{
			//Top.LayoutStyle = LayoutStyle.Computed;
			// Demonstrate using Dim to create a horizontal ruler that always measures the parent window's width
			// BUGBUG: Dim.Fill returns too big a value sometimes.
			const string rule = "|123456789";
			var horizontalRuler = new Label ("") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (1),  // BUGBUG: I don't think this should be needed; DimFill() should respect container's frame. X does.
				ColorScheme = Colors.Error
			};

			Application.Resized += (sender, a) => {
				horizontalRuler.Text = rule.Repeat ((int)Math.Ceiling ((double)(horizontalRuler.Bounds.Width) / (double)rule.Length)) [0..(horizontalRuler.Bounds.Width)];
			};

			Win.Add (horizontalRuler);

			// Demonstrate using Dim to create a vertical ruler that always measures the parent window's height
			// TODO: Either build a custom control for this or implement linewrap in Label #352
			//var verticalRuler = new Label ("") {
			//	X = 0,
			//	Y = 0,
			//	Width = 1,
			//	Height = Dim.Fill (),
			//	ColorScheme = Colors.Error
			//};

			//Application.OnResized += () => {
			//	verticalRuler.Text = rule.Repeat ((int)Math.Ceiling ((double)(verticalRuler.Bounds.Height) / (double)rule.Length)) [0..(verticalRuler.Bounds.Height)];
			//};

			//Win.Add (verticalRuler);


			// Demonstrate using Dim to create a window that fills the parent with a margin
			int margin = 10;
			var subWin = new Window ($"Centered Sub Window with {margin} character margin") {
				X = Pos.Center(),
				Y = 2,
				Width = Dim.Fill (margin),
				Height = 7
			};
			Win.Add (subWin);

			int i = 1;
			string txt = "Resize the terminal to see computed layout in action.";
			var labelList = new List<Label> ();
			labelList.Add (new Label ($"The lines below show different TextAlignments"));
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Left, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Right, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Centered, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Justified, Width = Dim.Fill (1), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });

			subWin.Add (labelList.ToArray ());

			// Demonstrate Dim & Pos using percentages - a TextField that is 20% height and 80% wide
			var textView= new TextView () {
				X = Pos.Center (),
				Y = Pos.Percent (50),
				Width = Dim.Percent (80),
				Height = Dim.Percent (20),
				ColorScheme = Colors.TopLevel,
			};
			textView.Text = "This text view should be half-way down the terminal,\n20% of its height, and 80% of its width.";
			Win.Add (textView);

			//// Demonstrate AnchorEnd - Button anchored to bottom of textView
			//var clearButton = new Button ("Clear") {
			//	X = Pos.AnchorEnd (),
			//	Y = Pos.AnchorEnd (),
			//	Width = 15,
			//	Height = 1
			//};
			//Win.Add (clearButton);

			// Demonstrate At - Absolute Layout using Pos
			var absoluteButton = new Button ("At(10,10)") {
				X = Pos.At(10),
				Y = Pos.At(10)
			};
			Win.Add (absoluteButton);
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