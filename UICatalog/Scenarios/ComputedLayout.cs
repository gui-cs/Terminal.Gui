using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	/// <summary>
	/// This Scenario demonstrates how to use Termina.gui's Dim and Pos Layout System. 
	/// [x] - Using Dim.Fill to fill a window
	/// [x] - Using Dim.Fill and Dim.Pos to automatically align controls based on an initial control
	/// [ ] - ...
	/// </summary>
	[ScenarioMetadata (Name: "Computed Layout", Description: "Demonstrates the Computed (Dim and Pos) Layout System.")]
	[ScenarioCategory ("Layout")]
	public class ComputedLayout : Scenario {

		public override void Setup ()
		{
			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Application.Top.Add (statusBar);

			// Demonstrate using Dim to create a horizontal ruler that always measures the parent window's width
			const string rule = "|123456789";
			var horizontalRuler = new Label ("") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				ColorScheme = Colors.Error
			};

			Win.Add (horizontalRuler);

			// Demonstrate using Dim to create a vertical ruler that always measures the parent window's height
			const string vrule = "|\n1\n2\n3\n4\n5\n6\n7\n8\n9\n";

			var verticalRuler = new Label ("") {
				X = 0,
				Y = 0,
				Width = 1,
				Height = Dim.Fill (),
				ColorScheme = Colors.Error
			};

			Win.LayoutComplete += (a) => {
				horizontalRuler.Text = rule.Repeat ((int)Math.Ceiling ((double)(horizontalRuler.Bounds.Width) / (double)rule.Length)) [0..(horizontalRuler.Bounds.Width)];
				verticalRuler.Text = vrule.Repeat ((int)Math.Ceiling ((double)(verticalRuler.Bounds.Height * 2) / (double)rule.Length)) [0..(verticalRuler.Bounds.Height * 2)];
			};

			Win.Add (verticalRuler);

			// Demonstrate At - Using Pos.At to locate a view in an absolute location
			var atButton = new Button ("At(2,1)") {
				X = Pos.At (2),
				Y = Pos.At (1)
			};
			Win.Add (atButton);

			// Throw in a literal absolute - Should funciton identically to above
			var absoluteButton = new Button ("X = 30, Y = 1") {
				X = 30,
				Y = 1
			};
			Win.Add (absoluteButton);

			// Demonstrate using Dim to create a window that fills the parent with a margin
			int margin = 10;
			var subWin = new Window ($"Centered Window with {margin} character margin") {
				X = Pos.Center (),
				Y = 2,
				Width = Dim.Fill (margin),
				Height = 7
			};
			Win.Add (subWin);

			int i = 1;
			string txt = "Resize the terminal to see computed layout in action.";
			var labelList = new List<Label> ();
			labelList.Add (new Label ($"The lines below show different TextAlignments"));
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Left, Width = Dim.Fill (), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Right, Width = Dim.Fill (), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Centered, Width = Dim.Fill (), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Justified, Width = Dim.Fill (), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			subWin.Add (labelList.ToArray ());

			// #522 repro?
			var frameView = new FrameView ($"Centered FrameView with {margin} character margin") {
				X = Pos.Center (),
				Y = Pos.Bottom (subWin),
				Width = Dim.Fill (margin),
				Height = 7
			};
			Win.Add (frameView);
			i = 1;
			labelList = new List<Label> ();
			labelList.Add (new Label ($"The lines below show different TextAlignments"));
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Left, Width = Dim.Fill (), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Right, Width = Dim.Fill (), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Centered, Width = Dim.Fill (), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			labelList.Add (new Label ($"{i++}-{txt}") { TextAlignment = Terminal.Gui.TextAlignment.Justified, Width = Dim.Fill (), X = 0, Y = Pos.Bottom (labelList.LastOrDefault ()), ColorScheme = Colors.Dialog });
			frameView.Add (labelList.ToArray ());

			// Demonstrate Dim & Pos using percentages - a TextField that is 30% height and 80% wide
			var textView = new TextView () {
				X = Pos.Center (),
				Y = Pos.Percent (50),
				Width = Dim.Percent (80),
				Height = Dim.Percent (10),
				ColorScheme = Colors.TopLevel,
			};
			textView.Text = $"This TextView should horizontally & vertically centered and \n10% of the screeen height, and 80% of its width.";
			Win.Add (textView);

			var oddballButton = new Button ("The Buttons below should be centered") {
				X = Pos.Center (),
				Y = Pos.Bottom (textView) + 1
			};
			Win.Add (oddballButton);

			#region Issue2358
			// Demonstrate odd-ball Combine scenarios
			// Until https://github.com/gui-cs/Terminal.Gui/issues/2358 is fixed these won't work right

			oddballButton = new Button ("Center + 0") {
				X = Pos.Center () + 0,
				Y = Pos.Bottom (oddballButton)
			};
			Win.Add (oddballButton);

			oddballButton = new Button ("0 + Center") {
				X = 0 + Pos.Center (),
				Y = Pos.Bottom (oddballButton)
			};
			Win.Add (oddballButton);

			// This demonstrates nonsense: it the same as using Pos.AnchorEnd (100/2=50 + 100/2=50 = 100 - 50)
			// The `- Pos.Percent(5)` is there so at least something is visible
			oddballButton = new Button ("Center + Center - Percent(50)") {
				X = Pos.Center () + Pos.Center () - Pos.Percent(50),
				Y = Pos.Bottom (oddballButton)
			};
			Win.Add (oddballButton);

			// This demonstrates nonsense: it the same as using Pos.AnchorEnd (100/2=50 + 100/2=50 = 100 - 50)
			// The `- Pos.Percent(5)` is there so at least something is visible
			oddballButton = new Button ("Percent(50) + Center - Percent(50)") {
				X = Pos.Percent (50) + Pos.Center () - Pos.Percent (50),
				Y = Pos.Bottom (oddballButton)
			};
			Win.Add (oddballButton);

			// This demonstrates nonsense: it the same as using Pos.AnchorEnd (100/2=50 + 100/2=50 = 100 - 50)
			// The `- Pos.Percent(5)` is there so at least something is visible
			oddballButton = new Button ("Center + Percent(50) - Percent(50)") {
				X = Pos.Center () + Pos.Percent (50) - Pos.Percent (50),
				Y = Pos.Bottom (oddballButton)
			};
			Win.Add (oddballButton);

			#endregion
			// This demonstrates nonsense: Same as At(0)
			oddballButton = new Button ("Center - Center - Percent(50)") {
				X = Pos.Center () + Pos.Center () - Pos.Percent (50),
				Y = Pos.Bottom (oddballButton)
			};
			Win.Add (oddballButton);

			// This demonstrates combining Percents)
			oddballButton = new Button ("Percent(40) + Percent(10)") {
				X = Pos.Percent (40) + Pos.Percent(10),
				Y = Pos.Bottom (oddballButton)
			};
			Win.Add (oddballButton);

			// Demonstrate AnchorEnd - Button is anchored to bottom/right
			var anchorButton = new Button ("Button using AnchorEnd") {
				Y = Pos.AnchorEnd () - 1,
			};
			anchorButton.X = Pos.AnchorEnd () - (Pos.Right (anchorButton) - Pos.Left (anchorButton));
			anchorButton.Clicked += () => {
				// Ths demonstrates how to have a dynamically sized button
				// Each time the button is clicked the button's text gets longer
				// The call to Win.LayoutSubviews causes the Computed layout to
				// get updated. 
				anchorButton.Text += "!";
				Win.LayoutSubviews ();
			};
			Win.Add (anchorButton);

			// Demonstrate AnchorEnd(n) 
			// This is intentionally convoluted to illustrate potential bugs.
			var anchorEndLabel1 = new Label ("This Button should be the 2nd to last line (AnchorEnd (2)).") {
				TextAlignment = Terminal.Gui.TextAlignment.Centered,
				ColorScheme = Colors.Menu,
				Width = Dim.Fill (1),
				X = 1,
				Y = Pos.AnchorEnd (2)
			};
			Win.Add (anchorEndLabel1);

			// Demonstrate DimCombine (via AnchorEnd(n) - 1)
			// This is intentionally convoluted to illustrate potential bugs.
			var anchorEndLabel2 = new Label ("This Button should be the 3rd to last line (AnchorEnd (2) - 1).") {
				TextAlignment = Terminal.Gui.TextAlignment.Centered,
				ColorScheme = Colors.Menu,
				Width = Dim.Fill (1),
				X = 1,
				Y = Pos.AnchorEnd (2) - 1 // Pos.Combine
			};
			Win.Add (anchorEndLabel2);

			// Show positioning vertically using Pos.AnchorEnd via Pos.Combine
			var leftButton = new Button ("Left") {
				Y = Pos.AnchorEnd () - 1 // Pos.Combine
			};
			leftButton.Clicked += () => {
				// Ths demonstrates how to have a dynamically sized button
				// Each time the button is clicked the button's text gets longer
				// The call to Win.LayoutSubviews causes the Computed layout to
				// get updated. 
				leftButton.Text += "!";
				Win.LayoutSubviews ();
			};


			// show positioning vertically using Pos.AnchorEnd
			var centerButton = new Button ("Center") {
				X = Pos.Center (),
				Y = Pos.AnchorEnd (1)  // Pos.AnchorEnd(1)
			};
			centerButton.Clicked += () => {
				// Ths demonstrates how to have a dynamically sized button
				// Each time the button is clicked the button's text gets longer
				// The call to Win.LayoutSubviews causes the Computed layout to
				// get updated. 
				centerButton.Text += "!";
				Win.LayoutSubviews ();
			};

			// show positioning vertically using another window and Pos.Bottom
			var rightButton = new Button ("Right") {
				Y = Pos.Y (centerButton)
			};
			rightButton.Clicked += () => {
				// Ths demonstrates how to have a dynamically sized button
				// Each time the button is clicked the button's text gets longer
				// The call to Win.LayoutSubviews causes the Computed layout to
				// get updated. 
				rightButton.Text += "!";
				Win.LayoutSubviews ();
			};

			// Center three buttons with 5 spaces between them
			leftButton.X = Pos.Left (centerButton) - (Pos.Right (leftButton) - Pos.Left (leftButton)) - 5;
			rightButton.X = Pos.Right (centerButton) + 5;

			Win.Add (leftButton);
			Win.Add (centerButton);
			Win.Add (rightButton);
		}

		public override void Run ()
		{
			base.Run ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}

	internal static class StringExtensions {
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