using System.Collections.Generic;
using System.Drawing;
using Terminal.Gui;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Graph View", Description: "Demos GraphView control")]
	[ScenarioCategory ("Controls")]
	class GraphViewExample : Scenario {

		GraphView graphView;
		private Label xAxisLabel;
		private TextView about;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Open Scatter Plot", "", () => SetupPeriodicTableScatterPlot()),
					new MenuItem ("_Open Bar Graph", "", () => SetupLifeExpectancyBarGraph()),

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


			Win.Add (graphView);


			var frameRight = new FrameView ("About") {
				X = Pos.Right (graphView),
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};


			frameRight.Add (about = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			});

			Win.Add (frameRight);

			xAxisLabel = new Label ("AxisLabel") {
				X = 0,
				Y = Pos.Bottom (graphView),
				Width = graphView.Width,
				Height = 1,
				TextAlignment = TextAlignment.Centered
			};

			Win.Add (xAxisLabel);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);
		}

			/*
			Country,Both,Male,Female

"Switzerland",83.4,81.8,85.1
"South Korea",83.3,80.3,86.1
"Singapore",83.2,81,85.5
"Spain",83.2,80.7,85.7
"Cyprus",83.1,81.1,85.1
"Australia",83,81.3,84.8
"Italy",83,80.9,84.9
"Norway",83,81.2,84.7
"Israel",82.6,80.8,84.4
"France",82.5,79.8,85.1
"Luxembourg",82.4,80.6,84.2
"Sweden",82.4,80.8,84
"Iceland",82.3,80.8,83.9
"Canada",82.2,80.4,84.1
"New Zealand",82,80.4,83.5
"Malta,81.9",79.9,83.8
"Ireland",81.8,80.2,83.5
"Netherlands",81.8,80.4,83.1
"Germany",81.7,78.7,84.8
"Austria",81.6,79.4,83.8
"Finland",81.6,79.2,84
"Portugal",81.6,78.6,84.4
"Belgium",81.4,79.3,83.5
"United Kingdom",81.4,79.8,83
"Denmark",81.3,79.6,83
"Slovenia",81.3,78.6,84.1
"Greece",81.1,78.6,83.6
"Kuwait",81,79.3,83.9
"Costa Rica",80.8,78.3,83.4*/

		private void SetupLifeExpectancyBarGraph()
		{
			xAxisLabel.Text = "Country";
			about.Text = "This graph shows the life expectancy at birth of a range of countries";

			var stiple = Application.Driver.Stipple;
			var barSeries = new BarSeries () {
				Bars = new List<BarSeries.Bar> () {
					new BarSeries.Bar ("Switzerland", stiple, 83.4f),
					new BarSeries.Bar ("South Korea", stiple, 83.3f),
					new BarSeries.Bar ("Singapore", stiple, 83.2),
					new BarSeries.Bar ("Spain", stiple, 83.2),
					new BarSeries.Bar ("Cyprus", stiple, 83.1),
					new BarSeries.Bar ("Australia", stiple, 83),
					new BarSeries.Bar ("Italy", stiple, 83),
					new BarSeries.Bar ("Norway", stiple, 83),
					new BarSeries.Bar ("Israel", stiple, 82.6),
					new BarSeries.Bar ("France", stiple, 82.5),
					new BarSeries.Bar ("Luxembourg", stiple, 82.4),
					new BarSeries.Bar ("Sweden", stiple, 82.4),
					new BarSeries.Bar ("Iceland", stiple, 82.3),
					new BarSeries.Bar ("Canada", stiple, 82.2),
					new BarSeries.Bar ("New Zealand", stiple, 82),
					new BarSeries.Bar ("Malta", stiple, 81.9),
					new BarSeries.Bar ("Ireland", stiple, 81.8)
				}
			};

			graphView.Series.Add (barSeries);

			// How much graph space each cell of the console depicts
			graphView.CellSize = new PointF (0.1f, 0.25f);
			graphView.AxisX.Increment = 0.1f;
			graphView.AxisX.ShowLabelsEvery = 1;
			graphView.AxisX.LabelGetter = v => barSeries.GetLabelText(v);

			graphView.AxisY.Increment = 1f;
			graphView.AxisY.ShowLabelsEvery = 1;
			graphView.AxisY.LabelGetter = v => v.GraphSpace.Y.ToString ("N2");

			// leave space for axis labels
			graphView.MarginBottom = 1;
			graphView.MarginLeft = 2;

			// Start the graph at 80 years because that is where most of our data is
			graphView.ScrollOffset = new PointF (0, 80);
		}

		private void SetupPeriodicTableScatterPlot ()
		{

			xAxisLabel.Text = "Atomic Number";
			about.Text = "This graph shows the atomic weight of each element in the periodic table.\nStarting with Hydrogen (atomic Number 1 with a weight of 1.007)";
			//about.WordWrap = true;

			//AtomicNumber and AtomicMass of all elements in the periodic table
			graphView.Series.Add (
				new ScatterSeries () {
					Points = new List<PointF>{
						new PointF(1,1.007f),new PointF(2,4.002f),new PointF(3,6.941f),new PointF(4,9.012f),new PointF(5,10.811f),new PointF(6,12.011f),
						new PointF(7,14.007f),new PointF(8,15.999f),new PointF(9,18.998f),new PointF(10,20.18f),new PointF(11,22.99f),new PointF(12,24.305f),
						new PointF(13,26.982f),new PointF(14,28.086f),new PointF(15,30.974f),new PointF(16,32.065f),new PointF(17,35.453f),new PointF(18,39.948f),
						new PointF(19,39.098f),new PointF(20,40.078f),new PointF(21,44.956f),new PointF(22,47.867f),new PointF(23,50.942f),new PointF(24,51.996f),
						new PointF(25,54.938f),new PointF(26,55.845f),new PointF(27,58.933f),new PointF(28,58.693f),new PointF(29,63.546f),new PointF(30,65.38f),
						new PointF(31,69.723f),new PointF(32,72.64f),new PointF(33,74.922f),new PointF(34,78.96f),new PointF(35,79.904f),new PointF(36,83.798f),
						new PointF(37,85.468f),new PointF(38,87.62f),new PointF(39,88.906f),new PointF(40,91.224f),new PointF(41,92.906f),new PointF(42,95.96f),
						new PointF(43,98f),new PointF(44,101.07f),new PointF(45,102.906f),new PointF(46,106.42f),new PointF(47,107.868f),new PointF(48,112.411f),
						new PointF(49,114.818f),new PointF(50,118.71f),new PointF(51,121.76f),new PointF(52,127.6f),new PointF(53,126.904f),new PointF(54,131.293f),
						new PointF(55,132.905f),new PointF(56,137.327f),new PointF(57,138.905f),new PointF(58,140.116f),new PointF(59,140.908f),new PointF(60,144.242f),
						new PointF(61,145),new PointF(62,150.36f),new PointF(63,151.964f),new PointF(64,157.25f),new PointF(65,158.925f),new PointF(66,162.5f),
						new PointF(67,164.93f),new PointF(68,167.259f),new PointF(69,168.934f),new PointF(70,173.054f),new PointF(71,174.967f),new PointF(72,178.49f),
						new PointF(73,180.948f),new PointF(74,183.84f),new PointF(75,186.207f),new PointF(76,190.23f),new PointF(77,192.217f),new PointF(78,195.084f),
						new PointF(79,196.967f),new PointF(80,200.59f),new PointF(81,204.383f),new PointF(82,207.2f),new PointF(83,208.98f),new PointF(84,210),
						new PointF(85,210),new PointF(86,222),new PointF(87,223),new PointF(88,226),new PointF(89,227),new PointF(90,232.038f),new PointF(91,231.036f),
						new PointF(92,238.029f),new PointF(93,237),new PointF(94,244),new PointF(95,243),new PointF(96,247),new PointF(97,247),new PointF(98,251),
						new PointF(99,252),new PointF(100,257),new PointF(101,258),new PointF(102,259),new PointF(103,262),new PointF(104,261),new PointF(105,262),
						new PointF(106,266),new PointF(107,264),new PointF(108,267),new PointF(109,268),new PointF(113,284),new PointF(114,289),new PointF(115,288),
						new PointF(116,292),new PointF(117,295),new PointF(118,294)
			}
				});

			// How much graph space each cell of the console depicts
			graphView.CellSize = new PointF (1, 5);

			// leave space for axis labels
			graphView.MarginBottom = 1;
			graphView.MarginLeft = 3;

			// One axis tick/label per 5 atomic numbers
			graphView.AxisX.Increment = 5;
			graphView.AxisX.ShowLabelsEvery = 1;

			// One label every 5 atomic weight
			graphView.AxisY.Increment = 5;
			graphView.AxisY.ShowLabelsEvery = 1;
		}

		private void Zoom (float factor)
		{
			graphView.CellSize = new PointF (
				graphView.CellSize.X * factor,
				graphView.CellSize.Y * factor
			);

			graphView.SetNeedsDisplay ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
