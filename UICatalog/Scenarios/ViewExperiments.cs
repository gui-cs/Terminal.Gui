using Terminal.Gui;
using Terminal.Gui.Configuration;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "_ View Experiments", Description: "v2 View Experiments")]
	[ScenarioCategory ("Controls")]
	public class ViewExperiments : Scenario {
		public override void Init ()
		{
			Application.Init ();
			//Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();
			Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

		}

		public override void Setup ()
		{
			//ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FramePadding;
			var containerLabel = new Label () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = 3,
			};
			Application.Top.Add (containerLabel);

			var view = new View () {
				X = 2,
				Y = Pos.Bottom(containerLabel),
				Height = Dim.Fill (2),
				Width = Dim.Fill (2),
				Title = "View with 2xMargin, 2xBorder, & 2xPadding",
				ColorScheme = Colors.ColorSchemes ["Base"],
			};

			Application.Top.Add (view);

			//view.InitializeFrames ();
			view.Margin.Thickness = new Thickness (2, 2, 2, 2);
			view.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view.Margin.Data = "Margin";
			view.BorderFrame.Thickness = new Thickness (2);
			view.BorderFrame.BorderStyle = BorderStyle.Single;
			view.BorderFrame.ColorScheme = view.ColorScheme;
			view.BorderFrame.Data = "BorderFrame";
			view.Padding.Thickness = new Thickness (2);
			view.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view.Padding.Data = "Padding";

			var view2 = new View () {
				X = 2,
				Y = 3,
				Height = 7,
				Width = 17,
				Title = "View2",
				Text = "View #2",
				TextAlignment = TextAlignment.Centered
			};

			//view2.InitializeFrames ();
			view2.Margin.Thickness = new Thickness (1);
			view2.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view2.Margin.Data = "Margin";
			view2.BorderFrame.Thickness = new Thickness (1);
			view2.BorderFrame.BorderStyle = BorderStyle.Single;
			view2.BorderFrame.ColorScheme = view.ColorScheme;
			view2.BorderFrame.Data = "BorderFrame";
			view2.Padding.Thickness = new Thickness (1);
			view2.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view2.Padding.Data = "Padding";

			view.Add (view2);

			var view3 = new View () {
				X = Pos.Right (view2) + 1,
				Y = 3,
				Height = 5,
				Width = 37,
				Title = "View3",
				Text = "View #3 (Right(view2)+1",
				TextAlignment = TextAlignment.Centered
			};

			//view3.InitializeFrames ();
			view3.Margin.Thickness = new Thickness (1, 1, 0, 0);
			view3.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view3.Margin.Data = "Margin";
			view3.BorderFrame.Thickness = new Thickness (1, 1, 1, 1);
			view3.BorderFrame.BorderStyle = BorderStyle.Single;
			view3.BorderFrame.ColorScheme = view.ColorScheme;
			view3.BorderFrame.Data = "BorderFrame";
			view3.Padding.Thickness = new Thickness (1, 1, 0, 0);
			view3.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view3.Padding.Data = "Padding";

			view.Add (view3);

			var view4 = new View () {
				X = Pos.Right (view3) + 1,
				Y = 3,
				Height = 5,
				Width = 37,
				Title = "View4",
				Text = "View #4 (Right(view3)+1",
				TextAlignment = TextAlignment.Centered
			};

			//view4.InitializeFrames ();
			view4.Margin.Thickness = new Thickness (0, 0, 1, 1);
			view4.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view4.Margin.Data = "Margin";
			view4.BorderFrame.Thickness = new Thickness (1, 1, 1, 1);
			view4.BorderFrame.BorderStyle = BorderStyle.Single;
			view4.BorderFrame.ColorScheme = view.ColorScheme;
			view4.BorderFrame.Data = "BorderFrame";
			view4.Padding.Thickness = new Thickness (0, 0, 1, 1);
			view4.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view4.Padding.Data = "Padding";

			view.Add (view4);

			var view5 = new View () {
				X = Pos.Right (view4) + 1,
				Y = 3,
				Height = Dim.Fill (2),
				Width = Dim.Fill (),
				Title = "View5",
				Text = "View #5 (Right(view4)+1 Fill",
				TextAlignment = TextAlignment.Centered
			};
			//view5.InitializeFrames ();
			view5.Margin.Thickness = new Thickness (0, 0, 0, 0);
			view5.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view5.Margin.Data = "Margin";
			view5.BorderFrame.Thickness = new Thickness (1, 1, 1, 1);
			view5.BorderFrame.BorderStyle = BorderStyle.Single;
			view5.BorderFrame.ColorScheme = view.ColorScheme;
			view5.BorderFrame.Data = "BorderFrame";
			view5.Padding.Thickness = new Thickness (0, 0, 0, 0);
			view5.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view5.Padding.Data = "Padding";

			view.Add (view5);

			var label = new Label () {
				Text = "AutoSize true; 1;1:",
				AutoSize = true,
				X = 1,
				Y = 1,

			};
			view.Add (label);

			var edit = new TextField () {
				Text = "Right (label)",
				X = Pos.Right (label),
				Y = 1,
				Width = 15,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "Right (edit) + 1",
				X = Pos.Right (edit) + 1,
				Y = 1,
				Width = 20,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "Center();50%",
				X = Pos.Center (),
				Y = Pos.Percent (50),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "Center() - 1;60%",
				X = Pos.Center () - 1,
				Y = Pos.Percent (60),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "0 + Percent(50);70%",
				X = 0 + Pos.Percent (50),
				Y = Pos.Percent (70),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "AnchorEnd[Right];AnchorEnd (1)",
				Y = Pos.AnchorEnd (1),
				Width = 30,
				Height = 1
			};
			edit.X = Pos.AnchorEnd () - (Pos.Right (edit) - Pos.Left (edit));
			view.Add (edit);

			edit = new TextField () {
				Text = "Left;AnchorEnd (2)",
				X = 0,
				Y = Pos.AnchorEnd (2),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			containerLabel.LayoutComplete += (a) => {
				containerLabel.Text = $"Container.Frame: {Application.Top.Frame} .Bounds: {Application.Top.Bounds}\nView.Frame: {view.Frame} .Bounds: {view.Bounds}\nView.ContentArea: {view.ContentArea}";
			};

		}
	}
}