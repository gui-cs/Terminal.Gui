using System;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Scrolling", Description: "Demonstrates ScrollView etc...")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Bug Repro")]

	class Scrolling : Scenario {
		public override void Setup ()
		{
			Win.X = 1;
			Win.Y = 2;
			Win.Width = Dim.Fill () - 4;
			Win.Height = Dim.Fill () - 2;
			var label = new Label ("ScrollView (new Rect (2, 2, 50, 20)) with a 200, 100 ContentSize...") {
				X = 0, Y = 0,
				ColorScheme = Colors.Dialog
			};
			Win.Add (label);

			// BUGBUG: ScrollView only supports Absolute Positioning (#72)
			var scrollView = new ScrollView (new Rect (2, 2, 50, 20)) {
				ColorScheme = Colors.TopLevel,
				ContentSize = new Size (200, 100),
				//ContentOffset = new Point (0, 0),
				ShowVerticalScrollIndicator = true,
				ShowHorizontalScrollIndicator = true,
			};

			const string rule = "|123456789";
			var horizontalRuler = new Label ("") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (1),  // BUGBUG: I don't think this should be needed; DimFill() should respect container's frame. X does.
				ColorScheme = Colors.Error
			};
			scrollView.Add (horizontalRuler);
			const string vrule = "|\n1\n2\n3\n4\n5\n6\n7\n8\n9\n";

			var verticalRuler = new Label ("") {
				X = 0,
				Y = 0,
				Width = 1,
				Height = Dim.Fill (),
				ColorScheme = Colors.Error
			};
			scrollView.Add (verticalRuler);

			Application.Resized += (sender, a) => {
				horizontalRuler.Text = rule.Repeat ((int)Math.Ceiling ((double)(horizontalRuler.Bounds.Width) / (double)rule.Length)) [0..(horizontalRuler.Bounds.Width)];
				verticalRuler.Text = vrule.Repeat ((int)Math.Ceiling ((double)(verticalRuler.Bounds.Height * 2) / (double)rule.Length)) [0..(verticalRuler.Bounds.Height * 2)];
			};

			scrollView.Add (new Button ("Press me!") {
				X = 3,
				Y = 3,
				Clicked = () => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No")
			});

			scrollView.Add (new Button ("A very long button. Should be wide enough to demo clipping!") {
				X = 3,
				Y = 4,
				Width = 50,
				Clicked = () => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No")
			});

			scrollView.Add (new TextField ("This is a test of...") {
				X = 3,
				Y = 5,
				Width = 50,
				ColorScheme = Colors.Dialog
			});

			scrollView.Add (new TextField ("... the emergency broadcast sytem.") {
				X = 3,
				Y = 10,
				Width = 50,
				ColorScheme = Colors.Dialog
			});

			scrollView.Add (new TextField ("Last line") {
				X = 3,
				Y = 99,
				Width = 50,
				ColorScheme = Colors.Dialog
			});

			// Demonstrate AnchorEnd - Button is anchored to bottom/right
			var anchorButton = new Button ("Bottom Right") {
				Y = Pos.AnchorEnd () - 1,
			};
			// TODO: Use Pos.Width instead of (Right-Left) when implemented (#502)
			anchorButton.X = Pos.AnchorEnd () - (Pos.Right (anchorButton) - Pos.Left (anchorButton));
			anchorButton.Clicked = () => {
				// Ths demonstrates how to have a dynamically sized button
				// Each time the button is clicked the button's text gets longer
				// The call to Win.LayoutSubviews causes the Computed layout to
				// get updated. 
				anchorButton.Text += "!";
				Win.LayoutSubviews ();
			};
			scrollView.Add (anchorButton);

			var hCheckBox = new CheckBox ("Horizontal Scrollbar", scrollView.ShowHorizontalScrollIndicator) {
				X = Pos.X(scrollView),
				Y = Pos.Bottom(scrollView) + 1,
			};
			hCheckBox.Toggled += (sender, previousChecked) => {
				scrollView.ShowHorizontalScrollIndicator = ((CheckBox)sender).Checked;
			};
			Win.Add (hCheckBox);

			var vCheckBox = new CheckBox ("Vertical Scrollbar", scrollView.ShowVerticalScrollIndicator) {
				X = Pos.Right (hCheckBox) + 3,
				Y = Pos.Bottom (scrollView) + 1,
			};
			vCheckBox.Toggled += (sender, previousChecked) => {
				scrollView.ShowVerticalScrollIndicator = ((CheckBox)sender).Checked;
			};
			Win.Add (vCheckBox);

			var scrollView2 = new ScrollView (new Rect (55, 2, 20, 8)) {
				ContentSize = new Size (20, 50),
				//ContentOffset = new Point (0, 0),
				ShowVerticalScrollIndicator = true,
				ShowHorizontalScrollIndicator = true
			};
			scrollView2.Add (new Filler (new Rect (0, 0, 60, 40)));

			// This is just to debug the visuals of the scrollview when small
			var scrollView3 = new ScrollView (new Rect (55, 15, 3, 3)) {
				ContentSize = new Size (100, 100),
				ShowVerticalScrollIndicator = true,
				ShowHorizontalScrollIndicator = true
			};
			scrollView3.Add (new Box10x (0, 0));

			int count = 0;
			var mousePos = new Label ("Mouse: ");
			mousePos.X = Pos.Center ();
			mousePos.Y = Pos.AnchorEnd (1);
			mousePos.Width = 50;
			Application.RootMouseEvent += delegate (MouseEvent me) {
				mousePos.TextColor = Colors.TopLevel.Normal;
				mousePos.Text = $"Mouse: ({me.X},{me.Y}) - {me.Flags} {count++}";
			};

			var progress = new ProgressBar ();
			progress.X = 5;
			progress.Y = Pos.AnchorEnd (2);
			progress.Width = 50;
			bool timer (MainLoop caller)
			{
				progress.Pulse ();
				return true;
			}
			Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300), timer);

			Win.Add (scrollView, scrollView2, scrollView3, mousePos, progress);
		}
	}
}