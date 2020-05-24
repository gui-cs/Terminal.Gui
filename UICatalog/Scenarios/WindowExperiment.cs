using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Windows & FrameViews", Description: "Shows Windows, sub-Windows, FrameViews, and how TAB doesn't work right (#434, #522)")]
	[ScenarioCategory ("Views")]
	class WindowExperiment : Scenario {
		public override void Init (Toplevel top)
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
			int margin = 3;
			int padding = 1;
			int height = 10;
			var listWin = new List<View> ();
			Win = new Window ($"{listWin.Count} - Scenario: {GetName ()}", padding) {
				X = margin,
				Y = margin,
				Width = Dim.Fill (margin),
				Height = height,
			};
			Win.ColorScheme = Colors.Dialog;
			Win.Add (new Button ("Press me!") {
				X = Pos.Center (),
				Y = 0,
				ColorScheme = Colors.Error,
				Clicked = () => MessageBox.ErrorQuery (30, 10, Win.Title.ToString (), "Neat?", "Yes", "No")
			});
			Top.Add (Win);
			listWin.Add (Win);

			for (var i = 0; i < 2; i++) {
				Window win = null;
				win = new Window ($"{listWin.Count} - Scenario: {GetName ()}", padding) {
					X = margin,
					Y = Pos.Bottom(listWin.Last()) + (margin/2),
					Width = Dim.Fill (margin),
					Height = height,
				};
				win.ColorScheme = Colors.Dialog;
				win.Add (new Button ("Press me!") {
					X = Pos.Center (),
					Y = 0,
					ColorScheme = Colors.Error,
					Clicked = () => MessageBox.ErrorQuery (30, 10, win.Title.ToString (), "Neat?", "Yes", "No")
				});
				var subWin = new Window("Sub Window") {
					X = Pos.Percent (0),
					Y = Pos.AnchorEnd() - 5,
					Width = Dim.Percent (50),
					Height = 5,
					ColorScheme = Colors.Base,
				};
				subWin.Add (new TextField (win.Title.ToString ()));
				win.Add (subWin);
				var frameView = new FrameView ("This is a Sub-FrameView") {
					X = Pos.Percent(50),
					Y = Pos.AnchorEnd () - 5,
					Width = Dim.Percent (100),
					Height = 5,
					ColorScheme = Colors.Base,
				};
				frameView.Add (new TextField ("Edit Me"));
				win.Add (frameView);

				Top.Add (win);
				listWin.Add (win);
			}


			FrameView frame = null;
			frame = new FrameView ($"This is a FrameView") {
				X = margin,
				Y = Pos.Bottom (listWin.Last ()) + (margin / 2),
				Width = Dim.Fill (margin),
				Height = height,
			};
			frame.ColorScheme = Colors.Dialog;
			frame.Add (new Button ("Press me!") {
				X = Pos.Center (),
				Y = 0,
				ColorScheme = Colors.Error,
				Clicked = () => MessageBox.ErrorQuery (30, 10, frame.Title.ToString (), "Neat?", "Yes", "No")
			});
			var subWinFV = new Window ("this is a Sub-Window") {
				X = Pos.Percent (0),
				Y = Pos.AnchorEnd () - (height - 4),
				Width = Dim.Percent (50),
				Height = Dim.Fill () - 1,
				ColorScheme = Colors.Base,
			};
			subWinFV.Add (new TextField (frame.Title.ToString ()));
			frame.Add (subWinFV);
			var frameViewFV = new FrameView ("this is a Sub-FrameView") {
				X = Pos.Percent (50),
				Y = Pos.AnchorEnd () - (height - 4),
				Width = Dim.Percent (100),
				Height = Dim.Fill() - 1, 
				ColorScheme = Colors.Base,
			};
			frameViewFV.Add (new TextField ("Edit Me"));

			frameViewFV.Add (new CheckBox (0, 1, "Check me"));
			// BUGBUG: This checkbox is not shown even though frameViewFV has 3 rows in 
			// it's client area. #522
			frameViewFV.Add (new CheckBox (0, 2, "Or, Check me"));

			frame.Add (new CheckBox ("No, Check me!") { 
				X = 0,
				Y = Pos.AnchorEnd() - 1, // BUGBUG: #522 If I don't do the -1 it doesn't draw, but it should!
			});
			frame.Add (new CheckBox ("Really, Check me!") {
				X = Pos.Percent(50),
				Y = Pos.AnchorEnd () - 1, // BUGBUG: #522 If I don't do the -1 it doesn't draw, but it should!
			});

			frame.Add (frameViewFV);

			Top.Add (frame);
			listWin.Add (frame);
		}
	}
}