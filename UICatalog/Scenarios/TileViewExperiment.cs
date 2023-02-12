using System;
using Terminal.Gui;
using Terminal.Gui.Graphs;
using System.Linq;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Tile View Experiments", Description: "Experiments with Tile View")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("LineView")]
	public class TileViewExperiment : Scenario {

		class NewFrameView : FrameView {
			public NewFrameView () : base ()
			{
				LayoutStarted += NewFrameView_LayoutStarted;
			}

			private void NewFrameView_LayoutStarted (LayoutEventArgs obj)
			{
				foreach (var subview in Subviews.ToArray () [0].Subviews) {
					if (subview.Border?.BorderStyle != BorderStyle.None &&
					subview.Text == "View 1 Text") {

						subview.Text = subview.X.ToString ();

					}
				}
			}
		}

		class TileFrameView : TileView {

		}

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

			var frame = new NewFrameView () {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			frame.Border.BorderStyle = BorderStyle.Double;

			Application.Top.Add (frame);

			var view1 = new FrameView () {
				Title = "View 1",
				Text = "View 1 Text",
				X = -1,
				Y = -1,
				Width = Dim.Percent (30),
				Height = Dim.Fill (-1),
				ColorScheme = Colors.ColorSchemes ["Dialog"]
				//Border = new Border () { BorderStyle = BorderStyle.Single }
			};

			frame.Add (view1);

			var view2 = new FrameView () {
				Title = "View 2",
				Text = "View 2 Text",
				X = Pos.Right (view1),
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = Colors.ColorSchemes ["Error"]
				//Border = new Border () { BorderStyle = BorderStyle.Single }
			};

			frame.Add (view2);
		}
	}
}