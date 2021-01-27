using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Windows & FrameViews", Description: "Shows Windows, sub-Windows, FrameViews, and how TAB doesn't work right (#434, #522)")]
	[ScenarioCategory ("Views")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Bug Repro")]
	class WindowsAndFrameViews : Scenario {
		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			Application.Init ();

			Top = top;
			if (Top == null) {
				Top = Application.Top;
			}
		}

		public override void RequestStop ()
		{
			base.RequestStop ();
		}

		public override void Run ()
		{
			base.Run ();
		}

		public override void Setup ()
		{
			static int About ()
			{
				return MessageBox.Query ("About UI Catalog", "UI Catalog is a comprehensive sample library for Terminal.Gui", "Ok");

				//var about = new Window (new Rect (0, 0, 50, 10), "About UI catalog", 0) {
				//	X = Pos.Center (),
				//	Y = Pos.Center (),
				//	Width = 50,
				//	Height = 10,
				//	LayoutStyle = LayoutStyle.Computed,
				//	ColorScheme = Colors.Error,

				//};

				//Application.Run (about);
				//return 0;

			}

			int margin = 2;
			int padding = 1;
			int contentHeight = 7;
			var listWin = new List<View> ();
			Win = new Window ($"{listWin.Count} - Scenario: {GetName ()}", padding) {
				X = Pos.Center (),
				Y = 1,
				Width = Dim.Fill (10),
				Height = Dim.Percent (15)
			};
			Win.ColorScheme = Colors.Dialog;
			var paddingButton = new Button ($"Padding of container is {padding}") {
				X = Pos.Center (),
				Y = 0,
				ColorScheme = Colors.Error,
			};
			paddingButton.Clicked += () => About ();
			Win.Add (paddingButton);
			Win.Add (new Button ("Press ME! (Y = Pos.AnchorEnd(1))") {
				X = Pos.Center (),
				Y = Pos.AnchorEnd (1),
				ColorScheme = Colors.Error
			});
			Top.Add (Win);
			listWin.Add (Win);

			for (var i = 0; i < 3; i++) {
				Window win = null;
				win = new Window ($"{listWin.Count} - Window Loop - padding = {i}", i) {
					X = margin,
					Y = Pos.Bottom (listWin.Last ()) + (margin),
					Width = Dim.Fill (margin),
					Height = contentHeight + (i * 2) + 2,
				};
				win.ColorScheme = Colors.Dialog;
				var pressMeButton = new Button ("Press me! (Y = 0)") {
					X = Pos.Center (),
					Y = 0,
					ColorScheme = Colors.Error,
				};
				pressMeButton.Clicked += () =>
					MessageBox.ErrorQuery (win.Title.ToString (), "Neat?", "Yes", "No");
				win.Add (pressMeButton);
				var subWin = new Window ("Sub Window") {
					X = Pos.Percent (0),
					Y = 1,
					Width = Dim.Percent (50),
					Height = 5,
					ColorScheme = Colors.Base,
					Text = "The Text in the Window",
				};
				subWin.Add (new TextField ("Edit me! " + win.Title.ToString ()) {
					Y = 1,
					ColorScheme = Colors.Error
				});
				win.Add (subWin);
				var frameView = new FrameView ("This is a Sub-FrameView") {
					X = Pos.Percent (50),
					Y = 1,
					Width = Dim.Percent (100, true), // Or Dim.Percent (50)
					Height = 5,
					ColorScheme = Colors.Base,
					Text = "The Text in the FrameView",

				};
				frameView.Add (new TextField ("Edit Me!") {
					Y = 1,
				});
				win.Add (frameView);

				Top.Add (win);
				listWin.Add (win);
			}


			FrameView frame = null;
			frame = new FrameView ($"This is a FrameView") {
				X = margin,
				Y = Pos.Bottom (listWin.Last ()) + (margin / 2),
				Width = Dim.Fill (margin),
				Height = contentHeight + 2,  // 2 for default padding
			};
			frame.ColorScheme = Colors.Dialog;
			frame.Add (new Label ("This is a Label! (Y = 0)") {
				X = Pos.Center (),
				Y = 0,
				ColorScheme = Colors.Error,
				//Clicked = () => MessageBox.ErrorQuery (frame.Title.ToString (), "Neat?", "Yes", "No")
			});
			var subWinofFV = new Window ("this is a Sub-Window") {
				X = Pos.Percent (0),
				Y = 1,
				Width = Dim.Percent (50),
				Height = Dim.Fill () - 1,
				ColorScheme = Colors.Base,
				Text = "The Text in the Window",
			};
			subWinofFV.Add (new TextField ("Edit Me") {
				ColorScheme = Colors.Error
			});

			subWinofFV.Add (new CheckBox (0, 1, "Check me"));
			subWinofFV.Add (new CheckBox (0, 2, "Or, Check me"));

			frame.Add (subWinofFV);
			var subFrameViewofFV = new FrameView ("this is a Sub-FrameView") {
				X = Pos.Percent (50),
				Y = 1,
				Width = Dim.Percent (100),
				Height = Dim.Fill () - 1,
				ColorScheme = Colors.Base,
				Text = "The Text in the FrameView",
			};
			subFrameViewofFV.Add (new TextField (0, 0, 15, "Edit Me"));

			subFrameViewofFV.Add (new CheckBox (0, 1, "Check me"));
			// BUGBUG: This checkbox is not shown even though frameViewFV has 3 rows in 
			// its client area. #522
			subFrameViewofFV.Add (new CheckBox (0, 2, "Or, Check me"));

			frame.Add (new CheckBox ("Btn1 (Y = Pos.AnchorEnd (1))") {
				X = 0,
				Y = Pos.AnchorEnd (1),
			});
			CheckBox c = new CheckBox ("Btn2 (Y = Pos.AnchorEnd (1))") {
				Y = Pos.AnchorEnd (1),
			};
			c.X = Pos.AnchorEnd () - (Pos.Right (c) - Pos.Left (c));
			frame.Add (c);

			frame.Add (subFrameViewofFV);

			Top.Add (frame);
			listWin.Add (frame);
		}
	}
}