﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Terminal.Gui;
using Xunit;

namespace UnitTests {
	public class GraphViewTests {

		#region Screen to Graph Tests

		[Fact]
		public void ScreenToGraphSpace_DefaultCellSize()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect(0,0,20,10);

			// origin should be bottom left
			var botLeft = gv.ScreenToGraphSpace (0, 10);
			Assert.Equal (0, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);
			

			// up 2 rows of the console and along 1 col
			var up2along1 = gv.ScreenToGraphSpace (1, 8);
			Assert.Equal (1, up2along1.X);
			Assert.Equal (2, up2along1.Y);
		}
		[Fact]
		public void ScreenToGraphSpace_DefaultCellSize_WithMargin ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.ScreenToGraphSpace (0, 10);
			Assert.Equal (0, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);

			gv.MarginLeft = 1;

			botLeft = gv.ScreenToGraphSpace (0, 10);
			// Origin should be at 1,10 now to leave a margin of 1
			// so screen position 0,10 would be data space -1,0
			Assert.Equal (-1, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);

			gv.MarginLeft = 1;
			gv.MarginBottom = 1;

			botLeft = gv.ScreenToGraphSpace (0, 10);
			// Origin should be at 1,0 (to leave a margin of 1 in both sides)
			// so screen position 0,10 would be data space -1,-1
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
			gv.CellSize = new PointF (0.25f, 5);

			// origin should be bottom left
			var botLeft = gv.ScreenToGraphSpace (0, 10);
			Assert.Equal (0, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (0.25, botLeft.Width);
			Assert.Equal (5, botLeft.Height);

			// up 2 rows of the console and along 1 col
			var up2along1 = gv.ScreenToGraphSpace (1, 8);
			Assert.Equal (0.25, up2along1.X);
			Assert.Equal (10, up2along1.Y);
			Assert.Equal (0.25, botLeft.Width);
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
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (0, botLeft.X);
			Assert.Equal (10, botLeft.Y); // row 10 of the view is the bottom left

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointF (2, 1));
			Assert.Equal (2, along2up1.X);
			Assert.Equal (9, along2up1.Y);
		}

		[Fact]
		public void GraphSpaceToScreen_DefaultCellSize_WithMargin ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (0, botLeft.X);
			Assert.Equal (10, botLeft.Y); // row 10 of the view is the bottom left

			gv.MarginLeft = 1;

			// With a margin of 1 the origin should be at x=1 y= 10
			botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (1, botLeft.X);
			Assert.Equal (10, botLeft.Y); // row 10 of the view is the bottom left

			gv.MarginLeft = 1;
			gv.MarginBottom = 1;

			// With a margin of 1 in both directions the origin should be at x=1 y= 9
			botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (1, botLeft.X);
			Assert.Equal (9, botLeft.Y); // row 9 of the view is the bottom left up 1 cell
		}

		[Fact]
		public void GraphSpaceToScreen_ScrollOffset ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			//graph is scrolled to present chart space -5 to 5 in both axes
			gv.ScrollOffset = new PointF (-5, -5);

			// origin should be right in the middle of the control
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (5, botLeft.X);
			Assert.Equal (5, botLeft.Y);

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointF (2, 1));
			Assert.Equal (7, along2up1.X);
			Assert.Equal (4, along2up1.Y);
		}
		[Fact]
		public void GraphSpaceToScreen_CustomCellSize ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// Each cell of screen is responsible for rendering 5 units in graph data model
			// vertically and 1/4 horizontally
			gv.CellSize = new PointF (0.25f, 5);

			// origin should be bottom left
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (0, botLeft.X);
			Assert.Equal (10, botLeft.Y); // row 10 of the view is the bottom left

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointF (2, 1));
			Assert.Equal (8, along2up1.X);
			Assert.Equal (10, along2up1.Y);

			// Y value 4 should be rendered in bottom most row
			Assert.Equal (10, gv.GraphSpaceToScreen(new PointF (2, 4)).Y);
			
			// Cell height is 5 so this is the first point of graph space that should
			// be rendered in the graph in next row up (row 9)
			Assert.Equal (9, gv.GraphSpaceToScreen (new PointF (2, 5)).Y);
			
			// More boundary testing for this cell size
			Assert.Equal (9, gv.GraphSpaceToScreen (new PointF (2, 6)).Y);
			Assert.Equal (9, gv.GraphSpaceToScreen (new PointF (2, 7)).Y);
			Assert.Equal (9, gv.GraphSpaceToScreen (new PointF (2, 8)).Y);
			Assert.Equal (9, gv.GraphSpaceToScreen (new PointF (2, 9)).Y);
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointF (2, 10)).Y);
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointF (2, 11)).Y);
		}


		[Fact]
		public void GraphSpaceToScreen_CustomCellSize_WithScrollOffset ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// Each cell of screen is responsible for rendering 5 units in graph data model
			// vertically and 1/4 horizontally
			gv.CellSize = new PointF (0.25f, 5);

			//graph is scrolled to present some negative chart (4 negative cols and 2 negative rows)
			gv.ScrollOffset = new PointF (-1, -10);

			// origin should be in the lower left (but not right at the bottom)
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (4, botLeft.X);
			Assert.Equal (8, botLeft.Y);

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointF (2, 1));
			Assert.Equal (12, along2up1.X);
			Assert.Equal (8, along2up1.Y);


			// More boundary testing for this cell size/offset
			Assert.Equal (7, gv.GraphSpaceToScreen (new PointF (2, 6)).Y);
			Assert.Equal (7, gv.GraphSpaceToScreen (new PointF (2, 7)).Y);
			Assert.Equal (7, gv.GraphSpaceToScreen (new PointF (2, 8)).Y);
			Assert.Equal (7, gv.GraphSpaceToScreen (new PointF (2, 9)).Y);
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointF (2, 10)).Y);
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointF (2, 11)).Y);
		}

		#endregion
		
		[Fact]
		public void TestNoOverlappingCells()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			// How much graph space each cell of the console depicts
			gv.CellSize = new PointF (0.1f, 0.25f);
			gv.AxisX.Increment = 1f;
			gv.AxisX.ShowLabelsEvery = 1;

			gv.AxisY.Increment = 1f;
			gv.AxisY.ShowLabelsEvery = 1;

			// Start the graph at 80 years because that is where most of our data is
			gv.ScrollOffset = new PointF (0, 80);

			List<RectangleF> otherRects = new List<RectangleF> ();

			List<System.Drawing.Point> overlappingPoints = new List<System.Drawing.Point> ();

			for(int x = 0;x<gv.Bounds.Width; x++) {
				for (int y = 0;y < gv.Bounds.Height; y++) {

					var graphSpace = gv.ScreenToGraphSpace (x, y);
					var overlapping = otherRects.Where (r=>r.IntersectsWith (graphSpace)).ToArray();

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
			gv.CellSize = new PointF (0.1f, 0.25f);
			gv.AxisX.Increment = 1f;
			gv.AxisX.ShowLabelsEvery = 1;

			gv.AxisY.Increment = 1f;
			gv.AxisY.ShowLabelsEvery = 1;

			// Start the graph at 80 years because that is where most of our data is
			gv.ScrollOffset = new PointF (0, 80);

			for (int x = 0; x < gv.Bounds.Width; x++) {
				for (int y = 0; y < gv.Bounds.Height; y++) {

					var graphSpace = gv.ScreenToGraphSpace (x, y);
					
					var p = gv.GraphSpaceToScreen (new PointF (graphSpace.Left, graphSpace.Top));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

					p = gv.GraphSpaceToScreen (new PointF (graphSpace.Right - 0.000001f, graphSpace.Top));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

					p = gv.GraphSpaceToScreen (new PointF (graphSpace.Left, graphSpace.Bottom - 0.000001f));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

					p = gv.GraphSpaceToScreen (new PointF (graphSpace.Right - 0.000001f, graphSpace.Bottom - 0.000001f));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

				}
			}
		}
	}
}
