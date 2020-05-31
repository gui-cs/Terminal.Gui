using System;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Clipping", Description: "Used to test that things clip correctly")]
	[ScenarioCategory ("Bug Repro")]

	class Clipping : Scenario {

		public override void Init (Toplevel top)
		{
			Application.Init ();

			Top = top;
			if (Top == null) {
				Top = Application.Top;
			}

			Top.ColorScheme = Colors.Base;
			//Win = new TopLevel($"CTRL-Q to Close - Scenario: {GetName ()}") {
			//	X = 0,
			//	Y = 0,
			//	Width = Dim.Fill (),
			//	Height = Dim.Fill ()
			//};
			//Top.Add (Win);
		}

		public override void Setup ()
		{
			//Win.X = 1;
			//Win.Y = 2;
			//Win.Width = Dim.Fill () - 4;
			//Win.Height = Dim.Fill () - 2;
			var label = new Label ("ScrollView (new Rect (5, 5, 100, 60)) with a 200, 100 ContentSize...") {
				X = 0, Y = 0,
				ColorScheme = Colors.Dialog
			};
			Top.Add (label);

			var scrollView = new ScrollView (new Rect (3, 3, 50, 20));
			scrollView.ColorScheme = Colors.Menu;
			scrollView.ContentSize = new Size (100, 60);
			//ContentOffset = new Point (0, 0),
			scrollView.ShowVerticalScrollIndicator = true;
			scrollView.ShowHorizontalScrollIndicator = true;

			//const string rule = "|123456789";
			//var horizontalRuler = new Label ("") {
			//	X = 0,
			//	Y = 0,
			//	Width = Dim.Fill (1),  // BUGBUG: I don't think this should be needed; DimFill() should respect container's frame. X does.
			//	ColorScheme = Colors.Error
			//};
			//scrollView.Add (horizontalRuler);
			//const string vrule = "|\n1\n2\n3\n4\n5\n6\n7\n8\n9\n";

			//var verticalRuler = new Label ("") {
			//	X = 0,
			//	Y = 0,
			//	Width = 1,
			//	Height = Dim.Fill (),
			//	ColorScheme = Colors.Error
			//};
			//scrollView.Add (verticalRuler);

			//Application.Resized += (sender, a) => {
			//	horizontalRuler.Text = rule.Repeat ((int)Math.Ceiling ((double)(horizontalRuler.Bounds.Width) / (double)rule.Length)) [0..(horizontalRuler.Bounds.Width)];
			//	verticalRuler.Text = vrule.Repeat ((int)Math.Ceiling ((double)(verticalRuler.Bounds.Height * 2) / (double)rule.Length)) [0..(verticalRuler.Bounds.Height * 2)];
			//};

			var embedded1 = new Window ("1") {
				X = 3,
				Y = 3,
				Width = Dim.Fill (3),
				Height = Dim.Fill (3),
				ColorScheme = Colors.Dialog
			};

			var embedded2 = new Window ("2") {
				X = 3,
				Y = 3,
				Width = Dim.Fill (3),
				Height = Dim.Fill (3),
				ColorScheme = Colors.Error
			};
			embedded1.Add (embedded2);

			var embedded3 = new Window ("3") {
				X = 3,
				Y = 3,
				Width = Dim.Fill (3),
				Height = Dim.Fill (3),
				ColorScheme = Colors.TopLevel
			};
			embedded2.Add (embedded3);

			scrollView.Add (embedded1);

			//scrollView.Add (new Button ("Press me!") {
			//	X = 3,
			//	Y = 3,
			//	Clicked = () => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No")
			//});

			//scrollView.Add (new Button ("A very long button. Should be wide enough to demo clipping!") {
			//	X = 3,
			//	Y = 4,
			//	Width = 50,
			//	Clicked = () => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No")
			//});

			//scrollView.Add (new TextField ("This is a test of...") {
			//	X = 3,
			//	Y = 5,
			//	Width = 50,
			//	ColorScheme = Colors.Dialog
			//});

			//scrollView.Add (new TextField ("... the emergency broadcast sytem.") {
			//	X = 3,
			//	Y = 10,
			//	Width = 50,
			//	ColorScheme = Colors.Dialog
			//});

			//scrollView.Add (new TextField ("Last line") {
			//	X = 3,
			//	Y = 99,
			//	Width = 50,
			//	ColorScheme = Colors.Dialog
			//});

			//// Demonstrate AnchorEnd - Button is anchored to bottom/right
			//var anchorButton = new Button ("Bottom Right") {
			//	Y = Pos.AnchorEnd () - 1,
			//};
			//// TODO: Use Pos.Width instead of (Right-Left) when implemented (#502)
			//anchorButton.X = Pos.AnchorEnd () - (Pos.Right (anchorButton) - Pos.Left (anchorButton));
			//anchorButton.Clicked = () => {
			//	// Ths demonstrates how to have a dynamically sized button
			//	// Each time the button is clicked the button's text gets longer
			//	// The call to Win.LayoutSubviews causes the Computed layout to
			//	// get updated. 
			//	anchorButton.Text += "!";
			//	Win.LayoutSubviews ();
			//};
			//scrollView.Add (anchorButton);

			Top.Add (scrollView);
		}
	}
}