﻿using System;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Scrolling", Description: "Demonstrates ScrollView etc...")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Bug Repro")]

	class Scrolling : Scenario {

		class Box10x : View {
			int w = 40;
			int h = 50;

			public bool WantCursorPosition { get; set; } = false;

			public Box10x (int x, int y) : base (new Rect (x, y, 20, 10))
			{
			}

			public Size GetContentSize ()
			{
				return new Size (w, h);
			}

			public void SetCursorPosition (Point pos)
			{
				throw new NotImplementedException ();
			}

			public override void Redraw (Rect bounds)
			{
				//Point pos = new Point (region.X, region.Y);
				Driver.SetAttribute (ColorScheme.Focus);

				for (int y = 0; y < h; y++) {
					Move (0, y);
					Driver.AddStr (y.ToString ());
					for (int x = 0; x < w - y.ToString ().Length; x++) {
						//Driver.AddRune ((Rune)('0' + (x + y) % 10));
						if (y.ToString ().Length < w)
							Driver.AddStr (" ");
					}
				}
				//Move (pos.X, pos.Y);
			}
		}

		class Filler : View {
			int w = 40;
			int h = 50;

			public Filler (Rect rect) : base (rect)
			{
				w = rect.Width;
				h = rect.Height;
			}

			public Size GetContentSize ()
			{
				return new Size (w, h);
			}

			public override void Redraw (Rect bounds)
			{
				Driver.SetAttribute (ColorScheme.Focus);
				var f = Frame;
				w = 0;
				h = 0;

				for (int y = 0; y < f.Width; y++) {
					Move (0, y);
					var nw = 0;
					for (int x = 0; x < f.Height; x++) {
						Rune r;
						switch (x % 3) {
						case 0:
							var er = y.ToString ().ToCharArray (0, 1) [0];
							nw += er.ToString ().Length;
							Driver.AddRune (er);
							if (y > 9) {
								er = y.ToString ().ToCharArray (1, 1) [0];
								nw += er.ToString ().Length;
								Driver.AddRune (er);
							}
							r = '.';
							break;
						case 1:
							r = 'o';
							break;
						default:
							r = 'O';
							break;
						}
						Driver.AddRune (r);
						nw += Rune.RuneLen (r);
					}
					if (nw > w)
						w = nw;
					h = y + 1;
				}
			}
		}

		public override void Setup ()
		{
			Win.X = 3;
			Win.Y = 3;
			Win.Width = Dim.Fill () - 3;
			Win.Height = Dim.Fill () - 3;
			var label = new Label ("ScrollView (new Rect (2, 2, 50, 20)) with a 200, 100 ContentSize...") {
				X = 0,
				Y = 0,
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

			const string rule = "0123456789";
			var horizontalRuler = new Label () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),  // FIXED: I don't think this should be needed; DimFill() should respect container's frame. X does.
				ColorScheme = Colors.Error
			};
			scrollView.Add (horizontalRuler);
			const string vrule = "0\n1\n2\n3\n4\n5\n6\n7\n8\n9\n";

			var verticalRuler = new Label () {
				X = 0,
				Y = 0,
				Width = 1,
				Height = Dim.Fill (),
				ColorScheme = Colors.Error
			};
			scrollView.Add (verticalRuler);

			void Top_Loaded()  {
				horizontalRuler.Text = rule.Repeat ((int)Math.Ceiling ((double)(horizontalRuler.Bounds.Width) / (double)rule.Length)) [0..(horizontalRuler.Bounds.Width)] +
				"\n" + "|         ".Repeat ((int)Math.Ceiling ((double)(horizontalRuler.Bounds.Width) / (double)rule.Length)) [0..(horizontalRuler.Bounds.Width)];
				verticalRuler.Text = vrule.Repeat ((int)Math.Ceiling ((double)(verticalRuler.Bounds.Height * 2) / (double)rule.Length)) [0..(verticalRuler.Bounds.Height * 2)];
				Top.Loaded -= Top_Loaded;
			};
			Top.Loaded += Top_Loaded;

			var pressMeButton = new Button ("Press me!") {
				X = 3,
				Y = 3,
			};
			pressMeButton.Clicked += () => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
			scrollView.Add (pressMeButton);

			var aLongButton = new Button ("A very long button. Should be wide enough to demo clipping!") {
				X = 3,
				Y = 4,
				Width = Dim.Fill (6),
			};
			aLongButton.Clicked += () => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
			scrollView.Add (aLongButton);

			scrollView.Add (new TextField ("This is a test of...") {
				X = 3,
				Y = 5,
				Width = 50,
				ColorScheme = Colors.Dialog
			});

			scrollView.Add (new TextField ("... the emergency broadcast system.") {
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
			anchorButton.Clicked += () => {
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
			Win.Add (hCheckBox);

			var vCheckBox = new CheckBox ("Vertical Scrollbar", scrollView.ShowVerticalScrollIndicator) {
				X = Pos.Right (hCheckBox) + 3,
				Y = Pos.Bottom (scrollView) + 1,
			};
			Win.Add (vCheckBox);

			var t = "Auto Hide Scrollbars";
			var ahCheckBox = new CheckBox (t, scrollView.AutoHideScrollBars) {
				X = Pos.Left (scrollView) + (scrollView.Bounds.Width / 2) - (t.Length / 2),
				Y = Pos.Bottom (scrollView) + 3,
			};
			var k = "Keep Content Always In Viewport";
			var keepCheckBox = new CheckBox (k, scrollView.AutoHideScrollBars) {
				X = Pos.Left (scrollView) + (scrollView.Bounds.Width / 2) - (k.Length / 2),
				Y = Pos.Bottom (scrollView) + 4,
			};
			hCheckBox.Toggled += (_) => {
				if (!ahCheckBox.Checked) {
					scrollView.ShowHorizontalScrollIndicator = hCheckBox.Checked;
				} else {
					hCheckBox.Checked = true;
					MessageBox.Query ("Message", "Disable Auto Hide Scrollbars first.", "Ok");
				}
			};
			vCheckBox.Toggled += (_) => {
				if (!ahCheckBox.Checked) {
					scrollView.ShowVerticalScrollIndicator = vCheckBox.Checked;
				} else {
					vCheckBox.Checked = true;
					MessageBox.Query ("Message", "Disable Auto Hide Scrollbars first.", "Ok");
				}
			};
			ahCheckBox.Toggled += (_) => {
				scrollView.AutoHideScrollBars = ahCheckBox.Checked;
				hCheckBox.Checked = true;
				vCheckBox.Checked = true;
			};
			Win.Add (ahCheckBox);

			keepCheckBox.Toggled += (_) => scrollView.KeepContentAlwaysInViewport = keepCheckBox.Checked;
			Win.Add (keepCheckBox);

			var scrollView2 = new ScrollView (new Rect (55, 2, 20, 8)) {
				ContentSize = new Size (20, 50),
				//ContentOffset = new Point (0, 0),
				ShowVerticalScrollIndicator = true,
				ShowHorizontalScrollIndicator = true
			};
			var filler = new Filler (new Rect (0, 0, 60, 40));
			scrollView2.Add (filler);
			scrollView2.DrawContent += (r) => {
				scrollView2.ContentSize = filler.GetContentSize ();
			};

			// This is just to debug the visuals of the scrollview when small
			var scrollView3 = new ScrollView (new Rect (55, 15, 3, 3)) {
				ContentSize = new Size (100, 100),
				ShowVerticalScrollIndicator = true,
				ShowHorizontalScrollIndicator = true
			};
			scrollView3.Add (new Box10x (0, 0));

			int count = 0;
			var mousePos = new Label ("Mouse: ");
			mousePos.X = Pos.Right(scrollView) + 1;
			mousePos.Y = Pos.AnchorEnd (1);
			mousePos.Width = 50;
			Application.RootMouseEvent += delegate (MouseEvent me) {
				mousePos.Text = $"Mouse: ({me.X},{me.Y}) - {me.Flags} {count++}";
			};

			var progress = new ProgressBar ();
			progress.X = Pos.Right (scrollView) + 1;
			progress.Y = Pos.AnchorEnd (2);
			progress.Width = 50;
			bool pulsing = true;
			bool timer (MainLoop caller)
			{
				progress.Pulse ();
				return pulsing;
			}
			Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300), timer);

			void Top_Unloaded ()
			{
				pulsing = false;
				Top.Unloaded -= Top_Unloaded;
			}
			Top.Unloaded += Top_Unloaded;

			Win.Add (scrollView, scrollView2, scrollView3, mousePos, progress);
		}
	}
}