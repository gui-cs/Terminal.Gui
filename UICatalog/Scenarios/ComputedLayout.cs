using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_Settings", new MenuItem [] {
					null,
					new MenuItem ("_Quit", "", () => Quit()),
				}),
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);

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

			Win.Add (horizontalRuler);

			// Demonstrate using Dim to create a vertical ruler that always measures the parent window's height
			// TODO: Either build a custom control for this or implement linewrap in Label #352
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

			// Demonstrate At - Absolute Layout using Pos
			var absoluteButton = new Button ("Absolute At(2,1)") {
				X = Pos.At (2),
				Y = Pos.At (1)
			};
			Win.Add (absoluteButton);

			// Demonstrate using Dim to create a window that fills the parent with a margin
			int margin = 10;
			var subWin = new Window ($"Centered Sub Window with {margin} character margin") {
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
				Height = Dim.Percent (30),
				ColorScheme = Colors.TopLevel,
			};
			textView.Text = "This text view should be half-way down the terminal,\n20% of its height, and 80% of its width.";
			Win.Add (textView);

			// Demonstrate AnchorEnd - Button is anchored to bottom/right
			var anchorButton = new Button ("Anchor End") {
				Y = Pos.AnchorEnd () - 1,
			};
			// TODO: Use Pos.Width instead of (Right-Left) when implemented (#502)
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


			// Centering multiple controls horizontally. 
			// This is intentionally convoluted to illustrate potential bugs.
			var bottomLabel = new Label ("This should be the 2nd to last line (Bug #xxx).") {
				TextAlignment = Terminal.Gui.TextAlignment.Centered,
				ColorScheme = Colors.Menu,
				Width = Dim.Fill (),
				X = Pos.Center (),
				Y = Pos.Bottom (Win) - 4  // BUGBUG: -2 should be two lines above border; but it has to be -4
			};
			Win.Add (bottomLabel);

			// Show positioning vertically using Pos.Bottom 
			// BUGBUG: -1 should be just above border; but it has to be -3
			var leftButton = new Button ("Left") {
				Y = Pos.Bottom (Win) - 3
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
				Y = Pos.AnchorEnd () - 1
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
			// TODO: Use Pos.Width instead of (Right-Left) when implemented (#502)
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