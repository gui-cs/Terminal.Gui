using System.Drawing;
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
	}
}
