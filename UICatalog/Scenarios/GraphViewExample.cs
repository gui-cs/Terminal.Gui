using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Terminal.Gui;
using Terminal.Gui.Graphs;

using Color = Terminal.Gui.Color;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Graph View", Description: "Demos GraphView control")]
	[ScenarioCategory ("Controls")]
	class GraphViewExample : Scenario {

		GraphView graphView;
		private TextView about;

		int currentGraph = 0;
		Action [] graphs;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			graphs = new Action [] {
				 ()=>SetupPeriodicTableScatterPlot(),   //0
				 ()=>SetupLifeExpectancyBarGraph(true), //1
				 ()=>SetupLifeExpectancyBarGraph(false),//2
				 ()=>SetupPopulationPyramid(),          //3
				 ()=>SetupLineGraph(),                  //4
				 ()=>SetupSineWave(),                   //5
				 ()=>SetupDisco(),                      //6
				 ()=>MultiBarGraph()                    //7
			};


			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("Scatter _Plot", "",()=>graphs[currentGraph = 0]()),
					new MenuItem ("_V Bar Graph", "", ()=>graphs[currentGraph = 1]()),
					new MenuItem ("_H Bar Graph", "", ()=>graphs[currentGraph = 2]()) ,
					new MenuItem ("P_opulation Pyramid","",()=>graphs[currentGraph = 3]()),
					new MenuItem ("_Line Graph","",()=>graphs[currentGraph = 4]()),
					new MenuItem ("Sine _Wave","",()=>graphs[currentGraph = 5]()),
					new MenuItem ("Silent _Disco","",()=>graphs[currentGraph = 6]()),
					new MenuItem ("_Multi Bar Graph","",()=>graphs[currentGraph = 7]()),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					new MenuItem ("Zoom _In", "", () => Zoom(0.5M)),
					 new MenuItem ("Zoom _Out", "", () =>  Zoom(2M)),
				}),

				});
			Top.Add (menu);

			graphView = new GraphView () {
				X = 1,
				Y = 1,
				Width = 60,
				Height = 20,
			};


			Win.Add (graphView);


			var frameRight = new FrameView ("About") {
				X = Pos.Right (graphView) + 1,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};


			frameRight.Add (about = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			});

			Win.Add (frameRight);


			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
				new StatusItem(Key.CtrlMask | Key.G, "~^G~ Next", ()=>graphs[currentGraph++%graphs.Length]()),
			});
			Top.Add (statusBar);
		}

		private void MultiBarGraph ()
		{
			graphView.Reset ();

			about.Text = "Housing Expenditures by income thirds 1996-2003";

			var black = Application.Driver.MakeAttribute (graphView.ColorScheme.Normal.Foreground, Color.Black);
			var cyan = Application.Driver.MakeAttribute (Color.BrightCyan, Color.Black);
			var magenta = Application.Driver.MakeAttribute (Color.BrightMagenta, Color.Black);
			var red = Application.Driver.MakeAttribute (Color.BrightRed, Color.Black);

			graphView.GraphColor = black;

			var series = new MultiBarSeries (3, 1, 0.25M, new [] { magenta, cyan, red });

			var stiple = Application.Driver.Stipple;

			series.AddBars ("'96", stiple, 5900, 9000, 14000);
			series.AddBars ("'97", stiple, 6100, 9200, 14800);
			series.AddBars ("'98", stiple, 6000, 9300, 14600);
			series.AddBars ("'99", stiple, 6100, 9400, 14950);
			series.AddBars ("'00", stiple, 6200, 9500, 15200);
			series.AddBars ("'01", stiple, 6250, 9900, 16000);
			series.AddBars ("'02", stiple, 6600, 11000, 16700);
			series.AddBars ("'03", stiple, 7000, 12000, 17000);

			graphView.CellSize = new PointD (0.25M, 1000);
			graphView.Series.Add (series);
			graphView.SetNeedsDisplay ();

			graphView.MarginLeft = 3;
			graphView.MarginBottom = 1;

			graphView.AxisY.LabelGetter = (v) => '$' + (v.GraphSpace.Y / 1000M).ToString ("N0") + 'k';

			graphView.AxisX.Increment = 1;
			graphView.AxisX.ShowLabelsEvery = 1;
			graphView.AxisX.LabelGetter = (v) => series.SubSeries.First ().GetLabelText (v);

			var legend = new LegendAnnotation (new Rect (graphView.Bounds.Width - 20,0, 20, 5));
			legend.AddEntry (new GraphCellToRender (stiple, series.SubSeries.ElementAt (0).OverrideBarColor), "Lower Third");
			legend.AddEntry (new GraphCellToRender (stiple, series.SubSeries.ElementAt (1).OverrideBarColor), "Middle Third");
			legend.AddEntry (new GraphCellToRender (stiple, series.SubSeries.ElementAt (2).OverrideBarColor), "Upper Third");
			graphView.Annotations.Add (legend);
		}

		private void SetupLineGraph ()
		{
			graphView.Reset ();

			about.Text = "This graph shows random points";

			var black = Application.Driver.MakeAttribute (graphView.ColorScheme.Normal.Foreground, Color.Black);
			var cyan = Application.Driver.MakeAttribute (Color.BrightCyan, Color.Black);
			var magenta = Application.Driver.MakeAttribute (Color.BrightMagenta, Color.Black);
			var red = Application.Driver.MakeAttribute (Color.BrightRed, Color.Black);

			graphView.GraphColor = black;

			List<PointD> randomPoints = new List<PointD> ();

			Random r = new Random ();

			for (int i = 0; i < 10; i++) {
				randomPoints.Add (new PointD (r.Next (100), r.Next (100)));
			}

			var points = new ScatterSeries () {
				Points = randomPoints
			};

			var line = new PathAnnotation () {
				LineColor = cyan,
				Points = randomPoints.OrderBy (p => p.X).ToList (),
				BeforeSeries = true,
			};

			graphView.Series.Add (points);
			graphView.Annotations.Add (line);


			randomPoints = new List<PointD> ();

			for (int i = 0; i < 10; i++) {
				randomPoints.Add (new PointD (r.Next (100), r.Next (100)));
			}


			var points2 = new ScatterSeries () {
				Points = randomPoints,
				Fill = new GraphCellToRender ('x', red)
			};

			var line2 = new PathAnnotation () {
				LineColor = magenta,
				Points = randomPoints.OrderBy (p => p.X).ToList (),
				BeforeSeries = true,
			};

			graphView.Series.Add (points2);
			graphView.Annotations.Add (line2);

			// How much graph space each cell of the console depicts
			graphView.CellSize = new PointD (2, 5);

			// leave space for axis labels
			graphView.MarginBottom = 2;
			graphView.MarginLeft = 3;

			// One axis tick/label per
			graphView.AxisX.Increment = 20;
			graphView.AxisX.ShowLabelsEvery = 1;
			graphView.AxisX.Text = "X →";

			graphView.AxisY.Increment = 20;
			graphView.AxisY.ShowLabelsEvery = 1;
			graphView.AxisY.Text = "↑Y";

			var max = line.Points.Union (line2.Points).OrderByDescending (p => p.Y).First ();
			graphView.Annotations.Add (new TextAnnotation () { Text = "(Max)", GraphPosition = new PointD (max.X + (2 * graphView.CellSize.X), max.Y) });

			graphView.SetNeedsDisplay ();
		}

		private void SetupSineWave ()
		{
			graphView.Reset ();

			about.Text = "This graph shows a sine wave";

			var points = new ScatterSeries ();
			var line = new PathAnnotation ();

			// Generate line graph with 2,000 points
			for (decimal x = -500; x < 500; x += 0.5M) {
				points.Points.Add (new PointD (x, (decimal)Math.Sin ((double)x)));
				line.Points.Add (new PointD (x, (decimal)Math.Sin ((double)x)));
			}

			graphView.Series.Add (points);
			graphView.Annotations.Add (line);

			// How much graph space each cell of the console depicts
			graphView.CellSize = new PointD (0.1M, 0.1M);

			// leave space for axis labels
			graphView.MarginBottom = 2;
			graphView.MarginLeft = 3;

			// One axis tick/label per
			graphView.AxisX.Increment = 0.5M;
			graphView.AxisX.ShowLabelsEvery = 2;
			graphView.AxisX.Text = "X →";
			graphView.AxisX.LabelGetter = (v) => v.GraphSpace.X.ToString ("N2");

			graphView.AxisY.Increment = 0.2M;
			graphView.AxisY.ShowLabelsEvery = 2;
			graphView.AxisY.Text = "↑Y";
			graphView.AxisY.LabelGetter = (v) => v.GraphSpace.Y.ToString ("N2");

			graphView.ScrollOffset = new PointD (-2.5M, -1);

			graphView.SetNeedsDisplay ();
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

		private void SetupLifeExpectancyBarGraph (bool verticalBars)
		{
			graphView.Reset ();

			about.Text = "This graph shows the life expectancy at birth of a range of countries";

			var softStiple = new GraphCellToRender ('\u2591');
			var mediumStiple = new GraphCellToRender ('\u2592');

			var barSeries = new BarSeries () {
				Bars = new List<BarSeries.Bar> () {
					new BarSeries.Bar ("Switzerland", softStiple, 83.4M),
					new BarSeries.Bar ("South Korea", !verticalBars?mediumStiple:softStiple, 83.3M),
					new BarSeries.Bar ("Singapore", softStiple, 83.2M),
					new BarSeries.Bar ("Spain", !verticalBars?mediumStiple:softStiple, 83.2M),
					new BarSeries.Bar ("Cyprus", softStiple, 83.1M),
					new BarSeries.Bar ("Australia", !verticalBars?mediumStiple:softStiple, 83),
					new BarSeries.Bar ("Italy", softStiple, 83),
					new BarSeries.Bar ("Norway", !verticalBars?mediumStiple:softStiple, 83),
					new BarSeries.Bar ("Israel", softStiple, 82.6M),
					new BarSeries.Bar ("France", !verticalBars?mediumStiple:softStiple, 82.5M),
					new BarSeries.Bar ("Luxembourg", softStiple, 82.4M),
					new BarSeries.Bar ("Sweden", !verticalBars?mediumStiple:softStiple, 82.4M),
					new BarSeries.Bar ("Iceland", softStiple, 82.3M),
					new BarSeries.Bar ("Canada", !verticalBars?mediumStiple:softStiple, 82.2M),
					new BarSeries.Bar ("New Zealand", softStiple, 82),
					new BarSeries.Bar ("Malta", !verticalBars?mediumStiple:softStiple, 81.9M),
					new BarSeries.Bar ("Ireland", softStiple, 81.8M)
				}
			};

			graphView.Series.Add (barSeries);

			if (verticalBars) {

				barSeries.Orientation = Orientation.Vertical;

				// How much graph space each cell of the console depicts
				graphView.CellSize = new PointD (0.1M, 0.25M);
				graphView.AxisX.Increment = 1M;
				graphView.AxisX.ShowLabelsEvery = 1;
				graphView.AxisX.LabelGetter = v => barSeries.GetLabelText (v);
				graphView.AxisX.Text = "Country";

				graphView.AxisY.Increment = 1M;
				graphView.AxisY.ShowLabelsEvery = 1;
				graphView.AxisY.LabelGetter = v => v.GraphSpace.Y.ToString ("N2");
				graphView.AxisY.Text = "Age";

				// leave space for axis labels and title
				graphView.MarginBottom = 2;
				graphView.MarginLeft = 6;

				// Start the graph at 80 years because that is where most of our data is
				graphView.ScrollOffset = new PointD (0, 80);

			} else {
				barSeries.Orientation = Orientation.Horizontal;

				// How much graph space each cell of the console depicts
				graphView.CellSize = new PointD (0.1M, 1M);
				graphView.AxisY.Increment = 1M;
				graphView.AxisY.ShowLabelsEvery = 1;
				graphView.AxisY.LabelGetter = v => barSeries.GetLabelText (v);
				graphView.AxisY.Text = "Country";

				graphView.AxisX.Increment = 1M;
				graphView.AxisX.ShowLabelsEvery = 1;
				graphView.AxisX.LabelGetter = v => v.GraphSpace.X.ToString ("N2");
				graphView.AxisX.Text = "Age";

				// leave space for axis labels and title
				graphView.MarginBottom = 2;
				graphView.MarginLeft = (uint)barSeries.Bars.Max (b => b.Text.Length) + 2;

				// Start the graph at 80 years because that is where most of our data is
				graphView.ScrollOffset = new PointD (80, 0);
			}

			graphView.SetNeedsDisplay ();
		}

		private void SetupPopulationPyramid ()
		{
			/*
			Age,M,F
0-4,2009363,1915127
5-9,2108550,2011016
10-14,2022370,1933970
15-19,1880611,1805522
20-24,2072674,2001966
25-29,2275138,2208929
30-34,2361054,2345774
35-39,2279836,2308360
40-44,2148253,2159877
45-49,2128343,2167778
50-54,2281421,2353119
55-59,2232388,2306537
60-64,1919839,1985177
65-69,1647391,1734370
70-74,1624635,1763853
75-79,1137438,1304709
80-84,766956,969611
85-89,438663,638892
90-94,169952,320625
95-99,34524,95559
100+,3016,12818*/

			about.Text = "This graph shows population of each age divided by gender";

			graphView.Reset ();

			// How much graph space each cell of the console depicts
			graphView.CellSize = new PointD (100_000, 1);

			//center the x axis in middle of screen to show both sides
			graphView.ScrollOffset = new PointD (-3_000_000, 0);

			graphView.AxisX.Text = "Number Of People";
			graphView.AxisX.Increment = 500_000;
			graphView.AxisX.ShowLabelsEvery = 2;

			// use Abs to make negative axis labels positive
			graphView.AxisX.LabelGetter = (v) => Math.Abs (v.GraphSpace.X / 1_000_000).ToString ("N2") + "M";

			// leave space for axis labels
			graphView.MarginBottom = 2;
			graphView.MarginLeft = 1;

			graphView.AxisY.ShowLabelsEvery = 1;

			var stiple = new GraphCellToRender (Application.Driver.Stipple);

			// Bars in 2 directions

			// Males (negative to make the bars go left)
			var malesSeries = new BarSeries () {
				Orientation = Orientation.Horizontal,
				Bars = new List<BarSeries.Bar> ()
				{
					new BarSeries.Bar("0-4",stiple,-2009363),
					new BarSeries.Bar("5-9",stiple,-2108550),
					new BarSeries.Bar("10-14",stiple,-2022370),
					new BarSeries.Bar("15-19",stiple,-1880611),
					new BarSeries.Bar("20-24",stiple,-2072674),
					new BarSeries.Bar("25-29",stiple,-2275138),
					new BarSeries.Bar("30-34",stiple,-2361054),
					new BarSeries.Bar("35-39",stiple,-2279836),
					new BarSeries.Bar("40-44",stiple,-2148253),
					new BarSeries.Bar("45-49",stiple,-2128343),
					new BarSeries.Bar("50-54",stiple,-2281421),
					new BarSeries.Bar("55-59",stiple,-2232388),
					new BarSeries.Bar("60-64",stiple,-1919839),
					new BarSeries.Bar("65-69",stiple,-1647391),
					new BarSeries.Bar("70-74",stiple,-1624635),
					new BarSeries.Bar("75-79",stiple,-1137438),
					new BarSeries.Bar("80-84",stiple,-766956),
					new BarSeries.Bar("85-89",stiple,-438663),
					new BarSeries.Bar("90-94",stiple,-169952),
					new BarSeries.Bar("95-99",stiple,-34524),
					new BarSeries.Bar("100+",stiple,-3016)

				}
			};
			graphView.Series.Add (malesSeries);


			// Females
			var femalesSeries = new BarSeries () {
				Orientation = Orientation.Horizontal,
				Bars = new List<BarSeries.Bar> ()
				{
					new BarSeries.Bar("0-4",stiple,1915127),
					new BarSeries.Bar("5-9",stiple,2011016),
					new BarSeries.Bar("10-14",stiple,1933970),
					new BarSeries.Bar("15-19",stiple,1805522),
					new BarSeries.Bar("20-24",stiple,2001966),
					new BarSeries.Bar("25-29",stiple,2208929),
					new BarSeries.Bar("30-34",stiple,2345774),
					new BarSeries.Bar("35-39",stiple,2308360),
					new BarSeries.Bar("40-44",stiple,2159877),
					new BarSeries.Bar("45-49",stiple,2167778),
					new BarSeries.Bar("50-54",stiple,2353119),
					new BarSeries.Bar("55-59",stiple,2306537),
					new BarSeries.Bar("60-64",stiple,1985177),
					new BarSeries.Bar("65-69",stiple,1734370),
					new BarSeries.Bar("70-74",stiple,1763853),
					new BarSeries.Bar("75-79",stiple,1304709),
					new BarSeries.Bar("80-84",stiple,969611),
					new BarSeries.Bar("85-89",stiple,638892),
					new BarSeries.Bar("90-94",stiple,320625),
					new BarSeries.Bar("95-99",stiple,95559),
					new BarSeries.Bar("100+",stiple,12818)
				}
			};


			var softStiple = new GraphCellToRender ('\u2591');
			var mediumStiple = new GraphCellToRender ('\u2592');

			for (int i = 0; i < malesSeries.Bars.Count; i++) {
				malesSeries.Bars [i].Fill = i % 2 == 0 ? softStiple : mediumStiple;
				femalesSeries.Bars [i].Fill = i % 2 == 0 ? softStiple : mediumStiple;
			}

			graphView.Series.Add (femalesSeries);

			graphView.AxisY.LabelGetter = (v) => femalesSeries.GetLabelText (v);


			graphView.Annotations.Add (new TextAnnotation () { Text = "M", ScreenPosition = new Terminal.Gui.Point (0, 10) });
			graphView.Annotations.Add (new TextAnnotation () { Text = "F", ScreenPosition = new Terminal.Gui.Point (graphView.Bounds.Width - 1, 10) });

			graphView.SetNeedsDisplay ();

		}


		private void SetupDisco ()
		{
			graphView.Reset ();

			about.Text = "This graph shows a graphic equaliser for an imaginary song";

			graphView.GraphColor = Application.Driver.MakeAttribute (Color.White, Color.Black);

			var green = Application.Driver.MakeAttribute (Color.BrightGreen, Color.Black);
			var brightgreen = Application.Driver.MakeAttribute (Color.Green, Color.Black);
			var brightyellow = Application.Driver.MakeAttribute (Color.BrightYellow, Color.Black);
			var red = Application.Driver.MakeAttribute (Color.Red, Color.Black);
			var brightred = Application.Driver.MakeAttribute (Color.BrightRed, Color.Black);

			var colorDelegate = new GraphAttributeGetterDelegate (
				(g) => {
					if (g.Top >= 85M) {
						return red;
					}
					if (g.Top >= 66M) {
						return brightred;
					}
					if (g.Top >= 45M) {
						return brightyellow;
					}
					if (g.Top >= 25M) {
						return brightgreen;
					}

					return green;
				}
				);

			var stiple = new GraphCellToRender ('\u2593');

			Random r = new Random ();
			var series = new BarSeries ();
			var bars = new List<BarSeries.Bar> ();

			Func<MainLoop, bool> genSample = (l) => {

				bars.Clear ();
				// generate an imaginary sample
				for (int i = 0; i < 31; i++) {
					bars.Add (
						new BarSeries.Bar (null, stiple, r.Next (0, 100)) {
							ColorGetter = colorDelegate
						});
				}
				graphView.SetNeedsDisplay ();


				// while the equaliser is showing
				return graphView.Series.Contains (series);
			};

			Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (250), genSample);

			series.Bars = bars;

			graphView.Series.Add (series);

			// How much graph space each cell of the console depicts
			graphView.CellSize = new PointD (1, 10);
			graphView.AxisX.Increment = 0; // No graph ticks
			graphView.AxisX.ShowLabelsEvery = 0; // no labels

			graphView.AxisY.Visible = false;

			graphView.SetNeedsDisplay ();
		}
		private void SetupPeriodicTableScatterPlot ()
		{
			graphView.Reset ();

			about.Text = "This graph shows the atomic weight of each element in the periodic table.\nStarting with Hydrogen (atomic Number 1 with a weight of 1.007)";

			//AtomicNumber and AtomicMass of all elements in the periodic table
			graphView.Series.Add (
				new ScatterSeries () {
					Points = new List<PointD>{
						new PointD(1,1.007M),new PointD(2,4.002M),new PointD(3,6.941M),new PointD(4,9.012M),new PointD(5,10.811M),new PointD(6,12.011M),
						new PointD(7,14.007M),new PointD(8,15.999M),new PointD(9,18.998M),new PointD(10,20.18M),new PointD(11,22.99M),new PointD(12,24.305M),
						new PointD(13,26.982M),new PointD(14,28.086M),new PointD(15,30.974M),new PointD(16,32.065M),new PointD(17,35.453M),new PointD(18,39.948M),
						new PointD(19,39.098M),new PointD(20,40.078M),new PointD(21,44.956M),new PointD(22,47.867M),new PointD(23,50.942M),new PointD(24,51.996M),
						new PointD(25,54.938M),new PointD(26,55.845M),new PointD(27,58.933M),new PointD(28,58.693M),new PointD(29,63.546M),new PointD(30,65.38M),
						new PointD(31,69.723M),new PointD(32,72.64M),new PointD(33,74.922M),new PointD(34,78.96M),new PointD(35,79.904M),new PointD(36,83.798M),
						new PointD(37,85.468M),new PointD(38,87.62M),new PointD(39,88.906M),new PointD(40,91.224M),new PointD(41,92.906M),new PointD(42,95.96M),
						new PointD(43,98M),new PointD(44,101.07M),new PointD(45,102.906M),new PointD(46,106.42M),new PointD(47,107.868M),new PointD(48,112.411M),
						new PointD(49,114.818M),new PointD(50,118.71M),new PointD(51,121.76M),new PointD(52,127.6M),new PointD(53,126.904M),new PointD(54,131.293M),
						new PointD(55,132.905M),new PointD(56,137.327M),new PointD(57,138.905M),new PointD(58,140.116M),new PointD(59,140.908M),new PointD(60,144.242M),
						new PointD(61,145),new PointD(62,150.36M),new PointD(63,151.964M),new PointD(64,157.25M),new PointD(65,158.925M),new PointD(66,162.5M),
						new PointD(67,164.93M),new PointD(68,167.259M),new PointD(69,168.934M),new PointD(70,173.054M),new PointD(71,174.967M),new PointD(72,178.49M),
						new PointD(73,180.948M),new PointD(74,183.84M),new PointD(75,186.207M),new PointD(76,190.23M),new PointD(77,192.217M),new PointD(78,195.084M),
						new PointD(79,196.967M),new PointD(80,200.59M),new PointD(81,204.383M),new PointD(82,207.2M),new PointD(83,208.98M),new PointD(84,210),
						new PointD(85,210),new PointD(86,222),new PointD(87,223),new PointD(88,226),new PointD(89,227),new PointD(90,232.038M),new PointD(91,231.036M),
						new PointD(92,238.029M),new PointD(93,237),new PointD(94,244),new PointD(95,243),new PointD(96,247),new PointD(97,247),new PointD(98,251),
						new PointD(99,252),new PointD(100,257),new PointD(101,258),new PointD(102,259),new PointD(103,262),new PointD(104,261),new PointD(105,262),
						new PointD(106,266),new PointD(107,264),new PointD(108,267),new PointD(109,268),new PointD(113,284),new PointD(114,289),new PointD(115,288),
						new PointD(116,292),new PointD(117,295),new PointD(118,294)
			}
				});

			// How much graph space each cell of the console depicts
			graphView.CellSize = new PointD (1, 5);

			// leave space for axis labels
			graphView.MarginBottom = 2;
			graphView.MarginLeft = 3;

			// One axis tick/label per 5 atomic numbers
			graphView.AxisX.Increment = 5;
			graphView.AxisX.ShowLabelsEvery = 1;
			graphView.AxisX.Text = "Atomic Number";

			// One label every 5 atomic weight
			graphView.AxisY.Increment = 5;
			graphView.AxisY.ShowLabelsEvery = 1;

			graphView.SetNeedsDisplay ();
		}

		private void Zoom (decimal factor)
		{
			graphView.CellSize = new PointD (
				graphView.CellSize.X * factor,
				graphView.CellSize.Y * factor
			);

			graphView.AxisX.Increment *= factor;
			graphView.AxisY.Increment *= factor;

			graphView.SetNeedsDisplay ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
