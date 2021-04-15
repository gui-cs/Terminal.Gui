﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Terminal.Gui;
using Xunit;

namespace UnitTests {


	public class GraphViewTests {


		private void InitFakeDriver ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });
		}

		#region Screen to Graph Tests

		[Fact]
		public void ScreenToGraphSpace_DefaultCellSize ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.ScreenToGraphSpace (0, 9);
			Assert.Equal (0, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);


			// up 2 rows of the console and along 1 col
			var up2along1 = gv.ScreenToGraphSpace (1, 7);
			Assert.Equal (1, up2along1.X);
			Assert.Equal (2, up2along1.Y);
		}
		[Fact]
		public void ScreenToGraphSpace_DefaultCellSize_WithMargin ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.ScreenToGraphSpace (0, 9);
			Assert.Equal (0, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);

			gv.MarginLeft = 1;

			botLeft = gv.ScreenToGraphSpace (0, 9);
			// Origin should be at 1,9 now to leave a margin of 1
			// so screen position 0,9 would be data space -1,0
			Assert.Equal (-1, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);

			gv.MarginLeft = 1;
			gv.MarginBottom = 1;

			botLeft = gv.ScreenToGraphSpace (0, 9);
			// Origin should be at 1,0 (to leave a margin of 1 in both sides)
			// so screen position 0,9 would be data space -1,-1
			Assert.Equal (-1, botLeft.X);
			Assert.Equal (-1, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);
		}
		[Fact]
		public void ScreenToGraphSpace_CustomCellSize ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// Each cell of screen measures 5 units in graph data model vertically and 1/4 horizontally
			gv.CellSize = new PointD (0.25M, 5);

			// origin should be bottom left 
			// (note that y=10 is actually overspilling the control, the last row is 9)
			var botLeft = gv.ScreenToGraphSpace (0, 9);
			Assert.Equal (0, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (0.25M, botLeft.Width);
			Assert.Equal (5, botLeft.Height);

			// up 2 rows of the console and along 1 col
			var up2along1 = gv.ScreenToGraphSpace (1, 7);
			Assert.Equal (0.25M, up2along1.X);
			Assert.Equal (10, up2along1.Y);
			Assert.Equal (0.25M, botLeft.Width);
			Assert.Equal (5, botLeft.Height);
		}

		#endregion

		#region Graph to Screen Tests

		[Fact]
		public void GraphSpaceToScreen_DefaultCellSize ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.GraphSpaceToScreen (new PointD (0, 0));
			Assert.Equal (0, botLeft.X);
			Assert.Equal (9, botLeft.Y); // row 9 of the view is the bottom left

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointD (2, 1));
			Assert.Equal (2, along2up1.X);
			Assert.Equal (8, along2up1.Y);
		}

		[Fact]
		public void GraphSpaceToScreen_DefaultCellSize_WithMargin ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.GraphSpaceToScreen (new PointD (0, 0));
			Assert.Equal (0, botLeft.X);
			Assert.Equal (9, botLeft.Y); // row 9 of the view is the bottom left

			gv.MarginLeft = 1;

			// With a margin of 1 the origin should be at x=1 y= 9
			botLeft = gv.GraphSpaceToScreen (new PointD (0, 0));
			Assert.Equal (1, botLeft.X);
			Assert.Equal (9, botLeft.Y); // row 9 of the view is the bottom left

			gv.MarginLeft = 1;
			gv.MarginBottom = 1;

			// With a margin of 1 in both directions the origin should be at x=1 y= 9
			botLeft = gv.GraphSpaceToScreen (new PointD (0, 0));
			Assert.Equal (1, botLeft.X);
			Assert.Equal (8, botLeft.Y); // row 8 of the view is the bottom left up 1 cell
		}

		[Fact]
		public void GraphSpaceToScreen_ScrollOffset ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			//graph is scrolled to present chart space -5 to 5 in both axes
			gv.ScrollOffset = new PointD (-5, -5);

			// origin should be right in the middle of the control
			var botLeft = gv.GraphSpaceToScreen (new PointD (0, 0));
			Assert.Equal (5, botLeft.X);
			Assert.Equal (4, botLeft.Y);

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointD (2, 1));
			Assert.Equal (7, along2up1.X);
			Assert.Equal (3, along2up1.Y);
		}
		[Fact]
		public void GraphSpaceToScreen_CustomCellSize ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// Each cell of screen is responsible for rendering 5 units in graph data model
			// vertically and 1/4 horizontally
			gv.CellSize = new PointD (0.25M, 5);

			// origin should be bottom left
			var botLeft = gv.GraphSpaceToScreen (new PointD (0, 0));
			Assert.Equal (0, botLeft.X);
			// row 9 of the view is the bottom left (height is 10 so 0,1,2,3..9)
			Assert.Equal (9, botLeft.Y);

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointD (2, 1));
			Assert.Equal (8, along2up1.X);
			Assert.Equal (9, along2up1.Y);

			// Y value 4 should be rendered in bottom most row
			Assert.Equal (9, gv.GraphSpaceToScreen (new PointD (2, 4)).Y);

			// Cell height is 5 so this is the first point of graph space that should
			// be rendered in the graph in next row up (row 9)
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointD (2, 5)).Y);

			// More boundary testing for this cell size
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointD (2, 6)).Y);
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointD (2, 7)).Y);
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointD (2, 8)).Y);
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointD (2, 9)).Y);
			Assert.Equal (7, gv.GraphSpaceToScreen (new PointD (2, 10)).Y);
			Assert.Equal (7, gv.GraphSpaceToScreen (new PointD (2, 11)).Y);
		}


		[Fact]
		public void GraphSpaceToScreen_CustomCellSize_WithScrollOffset ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// Each cell of screen is responsible for rendering 5 units in graph data model
			// vertically and 1/4 horizontally
			gv.CellSize = new PointD (0.25M, 5);

			//graph is scrolled to present some negative chart (4 negative cols and 2 negative rows)
			gv.ScrollOffset = new PointD (-1, -10);

			// origin should be in the lower left (but not right at the bottom)
			var botLeft = gv.GraphSpaceToScreen (new PointD (0, 0));
			Assert.Equal (4, botLeft.X);
			Assert.Equal (7, botLeft.Y);

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointD (2, 1));
			Assert.Equal (12, along2up1.X);
			Assert.Equal (7, along2up1.Y);


			// More boundary testing for this cell size/offset
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointD (2, 6)).Y);
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointD (2, 7)).Y);
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointD (2, 8)).Y);
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointD (2, 9)).Y);
			Assert.Equal (5, gv.GraphSpaceToScreen (new PointD (2, 10)).Y);
			Assert.Equal (5, gv.GraphSpaceToScreen (new PointD (2, 11)).Y);
		}

		#endregion


		/// <summary>
		/// A cell size of 0 would result in mapping all graph space into the
		/// same cell of the console.  Since <see cref="GraphView.CellSize"/>
		/// is mutable a sensible place to check this is in redraw.
		/// </summary>
		[Fact]
		public void CellSizeZero()
		{
			InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();
			gv.Bounds = new Rect (0, 0, 50, 30);
			gv.Series.Add (new ScatterSeries () { Points = new List<PointD> { new PointD (1, 1) } });
			gv.CellSize.X = 0;
			var ex = Assert.Throws<Exception>(()=>gv.Redraw (gv.Bounds));

			Assert.Equal ("CellSize cannot be 0", ex.Message);
		}

		#region ISeries tests
		[Fact]
		public void Series_GetsPassedCorrectBounds_AllAtOnce ()
		{
			InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			RectangleD fullGraphBounds = null;
			Rect graphScreenBounds = Rect.Empty;

			var series = new FakeSeries ((v,c,s,g)=> { graphScreenBounds = s; fullGraphBounds = g; },(g)=>throw new Exception("Unexpected call"));
			series.DrawMode = SeriesDrawMode.AllAtOnce;
			gv.Series.Add (series);


			gv.Redraw (gv.Bounds);
			Assert.Equal (new RectangleD(0,0,50,30), fullGraphBounds);
			Assert.Equal (new Rect (0, 0, 50, 30), graphScreenBounds);

			// Now we put a margin in
			// Graph should not spill into the margins

			gv.MarginBottom = 2;
			gv.MarginLeft = 5;

			// Even with a margin the graph should be drawn from 
			// the origin, we just get less visible width/height
			gv.Redraw (gv.Bounds);
			Assert.Equal (new RectangleD (0, 0, 45, 28), fullGraphBounds);

			// The screen space the graph will be rendered into should
			// not overspill the margins
			Assert.Equal (new Rect (5, 0, 45, 28), graphScreenBounds);
		}

		/// <summary>
		/// Tests that the bounds passed to the ISeries for drawing into are 
		/// correct even when the <see cref="GraphView.CellSize"/> results in
		/// multiple units of graph space being condensed into each cell of
		/// console
		/// </summary>
		[Fact]
		public void Series_GetsPassedCorrectBounds_AllAtOnce_LargeCellSize ()
		{
			InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			// the larger the cell size the more condensed (smaller) the graph space is
			gv.CellSize = new PointD (2, 5);

			RectangleD fullGraphBounds = null;
			Rect graphScreenBounds = Rect.Empty;

			var series = new FakeSeries ((v, c, s, g) => { graphScreenBounds = s; fullGraphBounds = g; }, (g) => throw new Exception ("Unexpected call"));
			series.DrawMode = SeriesDrawMode.AllAtOnce;
			gv.Series.Add (series);

			gv.Redraw (gv.Bounds);
			// Since each cell of the console is 2x5 of graph space the graph
			// bounds to be rendered are larger
			Assert.Equal (new RectangleD (0, 0, 100, 150), fullGraphBounds);
			Assert.Equal (new Rect (0, 0, 50, 30), graphScreenBounds);

			// Graph should not spill into the margins

			gv.MarginBottom = 2;
			gv.MarginLeft = 5;

			// Even with a margin the graph should be drawn from 
			// the origin, we just get less visible width/height
			gv.Redraw (gv.Bounds);
			Assert.Equal (new RectangleD (0, 0, 90, 140), fullGraphBounds);

			// The screen space the graph will be rendered into should
			// not overspill the margins
			Assert.Equal (new Rect (5, 0, 45, 28), graphScreenBounds);
		}

		[Fact]
		public void Series_GetsPassedCorrectBounds_CellByCell ()
		{
			InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			List<RectangleD> cells = new List<RectangleD>();
			var series = new FakeSeries ((v, c, s, g) => { throw new Exception ("Unexpected call"); }, (g) => { cells.Add (g);return null; });
			series.DrawMode = SeriesDrawMode.CellByCell;
			gv.Series.Add (series);

			gv.Redraw (gv.Bounds);
			// expect 1 call per cell in graph (50x30)
			Assert.Equal (1500, cells.Count);

			// first cell we get passed will be the top left of the screen
			// which is high on the Y axis and 0 on the x axis since screen
			// space starts at 0 in upper left while graph space starts with
			// lowest numbers (e.g. 0) in lower left
			Assert.Equal (new RectangleD(0,29,1,1), cells.First ());

			// Graph should not spill into the margins

			gv.MarginBottom = 2;
			gv.MarginLeft = 5;

			// reset the test recording list and redraw
			cells.Clear ();
			gv.Redraw (gv.Bounds);

			// all cells should be within this space

			// expect 1 call per console cell in graph view excluding margins (45x28)
			Assert.Equal (1260, cells.Count);
			var expectedBounds = new RectangleD (0, 0, 45, 28);
			Assert.True (cells.All (c => expectedBounds.Contains (c.X, c.Y)));
			Assert.True (cells.All (c => expectedBounds.Contains (c.X + c.Width-0.000001M, c.Y + c.Height - 0.0000001M)));
		}

		[Fact]
		public void Series_GetsPassedCorrectBounds_CellByCell_LargeCellSize ()
		{
			InitFakeDriver ();

			var gv = new GraphView ();
			gv.CellSize = new PointD (2, 5);
			gv.ColorScheme = new ColorScheme ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			List<RectangleD> cells = new List<RectangleD> ();
			var series = new FakeSeries ((v, c, s, g) => { throw new Exception ("Unexpected call"); }, (g) => { cells.Add (g); return null; });
			series.DrawMode = SeriesDrawMode.CellByCell;
			gv.Series.Add (series);

			gv.Redraw (gv.Bounds);
			// expect 1 call per cell in graph (50x30)
			Assert.Equal (1500, cells.Count);

			// Each console cell is 2x5 and graph space at the top of the screen
			// is 145
			Assert.Equal (new RectangleD (0, 145, 2, 5), cells.First ());

			// Graph should not spill into the margins

			gv.MarginBottom = 2;
			gv.MarginLeft = 5;

			// reset the test recording list and redraw
			cells.Clear ();
			gv.Redraw (gv.Bounds);

			// all cells should be within this space

			// expect 1 call per console cell in graph view excluding margins (45x28)
			Assert.Equal (1260, cells.Count);
			var expectedBounds = new RectangleD (0, 0, 90/*45 console cells = 90 graph units*/, 140);
			Assert.True (cells.All (c => expectedBounds.Contains (c.X, c.Y)));
			Assert.True (cells.All (c => expectedBounds.Contains (c.X + c.Width - 0.000001M, c.Y + c.Height - 0.0000001M)));
		}

		private class FakeSeries : ISeries {

			readonly Action<GraphView, ConsoleDriver, Rect, RectangleD> drawSeries;
			readonly Func<RectangleD, GraphCellToRender> getCellVal;

			public SeriesDrawMode DrawMode { get; set; }

			public FakeSeries (
				Action<GraphView, ConsoleDriver, Rect, RectangleD> drawSeries,
				Func<RectangleD, GraphCellToRender> getCellVal
				)
			{
				this.drawSeries = drawSeries;
				this.getCellVal = getCellVal;
			}

			public void DrawSeries (GraphView graph, ConsoleDriver driver, Rect bounds, RectangleD graphBounds)
			{
				drawSeries (graph,driver,bounds,graphBounds);
			}

			public GraphCellToRender GetCellValueIfAny (RectangleD graphSpace)
			{
				return getCellVal (graphSpace);
			}
		}

		#endregion

		[Fact]
		public void TestNoOverlappingCells ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			// How much graph space each cell of the console depicts
			gv.CellSize = new PointD (0.1M, 0.25M);
			gv.AxisX.Increment = 1M;
			gv.AxisX.ShowLabelsEvery = 1;

			gv.AxisY.Increment = 1M;
			gv.AxisY.ShowLabelsEvery = 1;

			// Start the graph at 80 years because that is where most of our data is
			gv.ScrollOffset = new PointD (0, 80);

			List<RectangleD> otherRects = new List<RectangleD> ();

			List<System.Drawing.Point> overlappingPoints = new List<System.Drawing.Point> ();

			for (int x = 0; x < gv.Bounds.Width; x++) {
				for (int y = 0; y < gv.Bounds.Height; y++) {

					var graphSpace = gv.ScreenToGraphSpace (x, y);
					var overlapping = otherRects.Where (r => r.IntersectsWith (graphSpace)).ToArray ();

					if (overlapping.Any ()) {
						overlappingPoints.Add (new System.Drawing.Point (x, y));
					}

					otherRects.Add (graphSpace);
				}
			}

			// There are 1,500 grid positions in the control, none should overlap in graph space
			Assert.Empty (overlappingPoints);
		}

		/// <summary>
		/// Tests that each point in the screen space maps to a rectangle of
		/// (float) graph space and that each corner of that rectangle of graph
		/// space maps back to the same row/col of the graph that was fed in
		/// </summary>
		[Fact]
		public void TestReversing_ScreenToGraphSpace ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			// How much graph space each cell of the console depicts
			gv.CellSize = new PointD (0.1M, 0.25M);
			gv.AxisX.Increment = 1;
			gv.AxisX.ShowLabelsEvery = 1;

			gv.AxisY.Increment = 1;
			gv.AxisY.ShowLabelsEvery = 1;

			// Start the graph at 80 years because that is where most of our data is
			gv.ScrollOffset = new PointD (0, 80);

			for (int x = 0; x < gv.Bounds.Width; x++) {
				for (int y = 0; y < gv.Bounds.Height; y++) {

					var graphSpace = gv.ScreenToGraphSpace (x, y);

					var p = gv.GraphSpaceToScreen (new PointD (graphSpace.Left, graphSpace.Top));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

					p = gv.GraphSpaceToScreen (new PointD (graphSpace.Right - 0.000001M, graphSpace.Top));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

					p = gv.GraphSpaceToScreen (new PointD (graphSpace.Left, graphSpace.Bottom - 0.000001M));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

					p = gv.GraphSpaceToScreen (new PointD (graphSpace.Right - 0.000001M, graphSpace.Bottom - 0.000001M));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

				}
			}
		}
	}
}
