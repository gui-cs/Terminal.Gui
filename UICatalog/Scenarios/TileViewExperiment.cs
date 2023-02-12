using System;
using Terminal.Gui;
using Terminal.Gui.Graphs;
using System.Linq;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Tile View Experiments", Description: "Experiments with Tile View")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("LineView")]
	public class TileViewExperiment : Scenario {


		public override void Init (ColorScheme colorScheme)
		{
			Application.Init ();
		}

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Quit", "", () => Application.RequestStop()),
			}) });

			Application.Top.Add (menu);

			var frame = new FrameView () {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				IgnoreBorderPropertyOnRedraw = true
			};
			frame.Border.BorderStyle = BorderStyle.Double;

			Application.Top.Add (frame);

			var view1 = new FrameView () {
				Title = "View 1",
				Text = "View1 30%/50% Single",
				X = -1,
				Y = -1,
				Width = Dim.Percent (30),
				Height = Dim.Percent (50),
				ColorScheme = Colors.ColorSchemes ["Dialog"],
				Border = new Border () { BorderStyle = BorderStyle.Single }
			};

			frame.Add (view1);

			var view2 = new FrameView () {
				Title = "View 2",
				Text = "View2 right of view1, 30%/Fill Single.",
				X = Pos.Right (view1) - 1,
				Y = -1,
				Width = Dim.Percent (30),
				Height = Dim.Fill (-1),
				ColorScheme = Colors.ColorSchemes ["Error"],
				Border = new Border () { BorderStyle = BorderStyle.Single }
			};

			frame.Add (view2);

			var view3 = new FrameView () {
				Title = "View 3",
				Text = "View3 right of View2 Fill/Fill Single",
				X = Pos.Right (view2) - 1,
				Y = -1,
				Width = Dim.Fill (-1),
				Height = Dim.Fill (-1),
				ColorScheme = Colors.ColorSchemes ["Menu"],
				Border = new Border () { BorderStyle = BorderStyle.Single }
			};

			frame.Add (view3);

			var view4 = new FrameView () {
				Title = "View 4",
				Text = "View4 below View1 30%/5 Single",
				X = -1,
				Y = Pos.Bottom (view1)-1,
				Width = Dim.Percent (30),
				Height = 5,
				ColorScheme = Colors.ColorSchemes ["TopLevel"],
				Border = new Border () { BorderStyle = BorderStyle.Single }
			};

			frame.Add (view4);

			var view5 = new FrameView () {
				Title = "View 5",
				Text = "View5 below View4 view4.Width/5 Double",
				X = -1,
				Y = Pos.Bottom (view4) - 1,
				Width = view4.Width,
				Height = 5,
				ColorScheme = Colors.ColorSchemes ["TopLevel"],
				Border = new Border () { BorderStyle = BorderStyle.Double }
			};

			frame.Add (view5);
		}
	}
}