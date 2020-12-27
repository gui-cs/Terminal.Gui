using System;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Clipping", Description: "Used to test that things clip correctly")]
	[ScenarioCategory ("Bug Repro")]

	class Clipping : Scenario {

		public override void Init (Toplevel top, ColorScheme colorScheme)
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
			var label = new Label ("ScrollView (new Rect (3, 3, 50, 20)) with a 200, 100 ContentSize...") {
				X = 0, Y = 0,
				//ColorScheme = Colors.Dialog
			};
			Top.Add (label);

			var scrollView = new ScrollView (new Rect (3, 3, 50, 20));
			scrollView.ColorScheme = Colors.Menu;
			scrollView.ContentSize = new Size (200, 100);
			//ContentOffset = new Point (0, 0),
			//scrollView.ShowVerticalScrollIndicator = true;
			//scrollView.ShowHorizontalScrollIndicator = true;

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

			var testButton = new Button (2, 2, "click me");
			testButton.Clicked += () => {
				MessageBox.Query (10, 5, "Test", "test message", "Ok");
			};
			embedded3.Add (testButton);
			embedded2.Add (embedded3);

			scrollView.Add (embedded1);

			Top.Add (scrollView);
		}
	}
}