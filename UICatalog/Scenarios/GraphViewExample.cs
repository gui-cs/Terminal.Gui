using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static UICatalog.Scenario;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Graph View", Description: "Demos GraphView control")]
	[ScenarioCategory ("Controls")]
	class GraphViewExample : Scenario {

		GraphView graphView;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {

					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					new MenuItem ("Zoom _In", "", () => Zoom(0.5f)),
					 new MenuItem ("Zoom _Out", "", () =>  Zoom(2f)),
				}),

				});
			Top.Add (menu);

			graphView = new GraphView () {
				X = 0,
				Y = 0,
				Width = 60,
				Height = 20,
			};

			graphView.Series.Add(new ScatterSeries(){
				Points = new List<PointF>{
					new PointF(1,2),
				 new PointF(2,3),
				 new PointF(5,5)}
			});

			// One graph line per 2 squares
			graphView.AxisX.Increment = 2;

			// One graph per 10%
			graphView.AxisY.Increment = 10;

			graphView.CellSize = new PointF(1,5);


			Win.Add (graphView);


			var frameRight = new FrameView ("About") {
				X = Pos.Right (graphView),
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};


			frameRight.Add (new TextView () {
				Text = "This demos the tabs control\nSwitch between tabs using cursor keys",
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			});

			Win.Add (frameRight);

			var frameBelow = new FrameView ("Bottom Frame") {
				X = 0,
				Y = Pos.Bottom (graphView),
				Width = graphView.Width,
				Height = Dim.Fill (),
			};


			frameBelow.Add (new TextView () {
				Text = "This frame exists to check you can still tab here\nand that the tab control doesn't overspill it's bounds",
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			});

			Win.Add (frameBelow);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);
		}

		private void Zoom (float factor)
		{
			graphView.CellSize = new PointF(
				graphView.CellSize.X * factor,
				graphView.CellSize.Y * factor
			);

			graphView.SetNeedsDisplay();
			graphView.AxisX.SetNeedsDisplay();
			graphView.AxisY.SetNeedsDisplay();
			
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
