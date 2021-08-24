using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Borders Comparisons", Description: "Compares Window, Toplevel and FrameView borders.")]
	[ScenarioCategory ("Border")]
	class BordersComparisons : Scenario {
		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			top.Dispose ();
			Application.Init ();

			top = Application.Top;

			var borderStyle = BorderStyle.Double;
			var drawMarginFrame = false;
			var borderThickness = new Thickness (1, 2, 3, 4);
			var borderBrush = Colors.Base.HotFocus.Foreground;
			var padding = new Thickness (1, 2, 3, 4);
			var background = Colors.Base.HotNormal.Foreground;
			var effect3D = true;

			var win = new Window (new Rect (5, 5, 40, 20), "Test", 8,
				new Border () {
					BorderStyle = borderStyle,
					DrawMarginFrame = drawMarginFrame,
					BorderThickness = borderThickness,
					BorderBrush = borderBrush,
					Padding = padding,
					Background = background,
					Effect3D = effect3D
				});

			var tf1 = new TextField ("1234567890") { Width = 10 };

			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += () => MessageBox.Query (20, 7, "Hi", "I'm a Window?", "Yes", "No");
			var label = new Label ("I'm a Window") {
				X = Pos.Center (),
				Y = Pos.Center () - 3,
			};
			var tf2 = new TextField ("1234567890") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			var tv = new TextView () {
				Y = Pos.AnchorEnd (2),
				Width = 10,
				Height = Dim.Fill (),
				ColorScheme = Colors.Dialog,
				Text = "1234567890"
			};
			win.Add (tf1, button, label, tf2, tv);
			top.Add (win);

			var top2 = new Border.ToplevelContainer (new Rect (50, 5, 40, 20),
				new Border () {
					BorderStyle = borderStyle,
					DrawMarginFrame = drawMarginFrame,
					BorderThickness = borderThickness,
					BorderBrush = borderBrush,
					Padding = padding,
					Background = background,
					Effect3D = effect3D
				},
				"Test2") {
				ColorScheme = Colors.Base,
			};

			var tf3 = new TextField ("1234567890") { Width = 10 };

			var button2 = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button2.Clicked += () => MessageBox.Query (20, 7, "Hi", "I'm a Toplevel?", "Yes", "No");
			var label2 = new Label ("I'm a Toplevel") {
				X = Pos.Center (),
				Y = Pos.Center () - 3,
			};
			var tf4 = new TextField ("1234567890") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			var tv2 = new TextView () {
				Y = Pos.AnchorEnd (2),
				Width = 10,
				Height = Dim.Fill (),
				ColorScheme = Colors.Dialog,
				Text = "1234567890"
			};
			top2.Add (tf3, button2, label2, tf4, tv2);
			top.Add (top2);

			var frm = new FrameView (new Rect (95, 5, 40, 20), "Test3", null,
				new Border () {
					BorderStyle = borderStyle,
					DrawMarginFrame = drawMarginFrame,
					BorderThickness = borderThickness,
					BorderBrush = borderBrush,
					Padding = padding,
					Background = background,
					Effect3D = effect3D
				}) { ColorScheme = Colors.Base };

			var tf5 = new TextField ("1234567890") { Width = 10 };

			var button3 = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button3.Clicked += () => MessageBox.Query (20, 7, "Hi", "I'm a FrameView?", "Yes", "No");
			var label3 = new Label ("I'm a FrameView") {
				X = Pos.Center (),
				Y = Pos.Center () - 3,
			};
			var tf6 = new TextField ("1234567890") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			var tv3 = new TextView () {
				Y = Pos.AnchorEnd (2),
				Width = 10,
				Height = Dim.Fill (),
				ColorScheme = Colors.Dialog,
				Text = "1234567890"
			};
			frm.Add (tf5, button3, label3, tf6, tv3);
			top.Add (frm);

			Application.Run ();
		}

		public override void Run ()
		{
			// Do nothing
		}
	}
}