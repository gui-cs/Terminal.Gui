using NStack;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests {
	public class LineCanvasTests {

		readonly ITestOutputHelper output;

		public LineCanvasTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[InlineData (0, 0, 0, 0)]
		[InlineData (0, 0, 1, 0)]
		[InlineData (0, 0, 0, 1)]
		[InlineData (0, 0, 1, 1)]
		[InlineData (0, 0, 2, 2)]
		[InlineData (0, 0, 10, 10)]
		[InlineData (1, 0, 0, 0)]
		[InlineData (1, 0, 1, 0)]
		[InlineData (1, 0, 0, 1)]
		[InlineData (1, 0, 1, 1)]
		[InlineData (1, 0, 2, 2)]
		[InlineData (1, 0, 10, 10)]
		[InlineData (1, 1, 1, 0)]
		[InlineData (1, 1, 0, 1)]
		[InlineData (1, 1, 1, 1)]
		[InlineData (1, 1, 2, 2)]
		[InlineData (1, 1, 10, 10)]
		[InlineData (-1, -1, 1, 0)]
		[InlineData (-1, -1, 0, 1)]
		[InlineData (-1, -1, 1, 1)]
		[InlineData (-1, -1, 2, 2)]
		[InlineData (-1, -1, 10, 10)]
		[Theory, AutoInitShutdown]
		public void Canvas_Has_Correct_Bounds (int x, int y, int length, int height)
		{
			var canvas = new LineCanvas ();
			canvas.AddLine (new Point (x, y), length, Orientation.Horizontal, LineStyle.Single);
			canvas.AddLine (new Point (x, y), length, Orientation.Vertical, LineStyle.Single);

			int expectedWidth = Math.Max (length, 1);
			int expectedHeight = Math.Max (length, 1);

			Assert.Equal (new Rect (x, y, expectedWidth, expectedHeight), canvas.Canvas);
		}

		[Fact, AutoInitShutdown]
		public void Canvas_Has_Correct_Bounds_Specific ()
		{
			// Draw at 1,1 within client area of View (i.e. leave a top and left margin of 1)
			// This proves we aren't drawing excess above
			int x = 1;
			int y = 2;
			int width = 3;
			int height = 2;

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width * 2, height * 2);

			var lc = new LineCanvas ();

			// 01230
			// ╔╡╞╗1
			// ║  ║2

			// Add a short horiz line for ╔╡
			lc.AddLine (new Point (x, y), 2, Orientation.Horizontal, LineStyle.Double);
			Assert.Equal (new Rect (x, y, 2, 1), lc.Canvas);

			//LHS line down
			lc.AddLine (new Point (x, y), height, Orientation.Vertical, LineStyle.Double);
			Assert.Equal (new Rect (x, y, 2, 2), lc.Canvas);

			//Vertical line before Title, results in a ╡
			lc.AddLine (new Point (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);
			Assert.Equal (new Rect (x, y, 2, 2), lc.Canvas);

			//Vertical line after Title, results in a ╞
			lc.AddLine (new Point (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);
			Assert.Equal (new Rect (x, y, 2, 2), lc.Canvas);

			// remainder of top line
			lc.AddLine (new Point (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);
			Assert.Equal (new Rect (x, y, 4, 2), lc.Canvas);

			//RHS line down
			lc.AddLine (new Point (x + width, y), height, Orientation.Vertical, LineStyle.Double);
			Assert.Equal (new Rect (x, y, 4, 2), lc.Canvas);

			Application.Driver.Move (0, 0);
			Application.Driver.AddStr ("01234");
			Application.Driver.Move (0, 1);
			Application.Driver.AddStr ("01234");
			foreach (var p in lc.GetMap ()) {
				Application.Driver.Move (p.Key.X, p.Key.Y);
				Application.Driver.AddRune (p.Value);
			}

			string looksLike =
		@"
01234
01234
 ╔╡╞╗
 ║  ║
";
			TestHelpers.AssertDriverContentsWithFrameAre (looksLike, output);
		}

		[InlineData (0, 0, Orientation.Horizontal, "─")]
		[InlineData (1, 0, Orientation.Horizontal, "─")]
		[InlineData (0, 1, Orientation.Horizontal, "─")]
		[InlineData (0, 0, Orientation.Vertical, "│")]
		[InlineData (1, 0, Orientation.Vertical, "│")]
		[InlineData (0, 1, Orientation.Vertical, "│")]
		[Theory, AutoInitShutdown]
		public void Length_Zero_Alone_Is_Line (int x, int y, Orientation orientation, string expected)
		{
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 20);

			var canvas = new LineCanvas ();
			// Add a line at 0, 0 that's has length of 0
			canvas.AddLine (new Point (0, 0), 0, orientation, LineStyle.Single);

			foreach (var p in canvas.GetMap ()) {
				Application.Driver.Move (p.Key.X, p.Key.Y);
				Application.Driver.AddRune (p.Value);
			}

			string looksLike = $"{Environment.NewLine}{expected}";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData (0, 0, Orientation.Horizontal, "┼")]
		[InlineData (1, 0, Orientation.Horizontal, "┼")]
		[InlineData (0, 1, Orientation.Horizontal, "┼")]
		[InlineData (0, 0, Orientation.Vertical, "┼")]
		[InlineData (1, 0, Orientation.Vertical, "┼")]
		[InlineData (0, 1, Orientation.Vertical, "┼")]
		[Theory, AutoInitShutdown]
		public void Length_Zero_Cross_Is_Cross (int x, int y, Orientation orientation, string expected)
		{
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 20);

			var canvas = new LineCanvas ();

			// Add point at opposite orientation
			canvas.AddLine (new Point (0, 0), 0, orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal, LineStyle.Single);

			// Add a line at 0, 0 that's has length of 0
			canvas.AddLine (new Point (0, 0), 0, orientation, LineStyle.Single);

			foreach (var p in canvas.GetMap ()) {
				Application.Driver.Move (p.Key.X, p.Key.Y);
				Application.Driver.AddRune (p.Value);
			}

			string looksLike = $"{Environment.NewLine}{expected}";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData (0, 0, Orientation.Horizontal, "╥")]
		[InlineData (1, 0, Orientation.Horizontal, "╥")]
		[InlineData (0, 1, Orientation.Horizontal, "╥")]
		[InlineData (0, 0, Orientation.Vertical, "╞")]
		[InlineData (1, 0, Orientation.Vertical, "╞")]
		[InlineData (0, 1, Orientation.Vertical, "╞")]
		[Theory, AutoInitShutdown]
		public void Length_Zero_NextTo_Opposite_Is_T (int x, int y, Orientation orientation, string expected)
		{
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 20);

			var canvas = new LineCanvas ();

			// Add line with length of 1 in opposite orientation starting at same location
			if (orientation == Orientation.Horizontal) {
				canvas.AddLine (new Point (0, 0), 1, Orientation.Vertical, LineStyle.Double);
			} else {
				canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, LineStyle.Double);

			}

			// Add a line at 0, 0 that's has length of 0
			canvas.AddLine (new Point (0, 0), 0, orientation, LineStyle.Single);

			foreach (var p in canvas.GetMap ()) {
				Application.Driver.Move (p.Key.X, p.Key.Y);
				Application.Driver.AddRune (p.Value);
			}

			string looksLike = $"{Environment.NewLine}{expected}";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData (0, 0, Orientation.Horizontal, "─")]
		[InlineData (1, 0, Orientation.Horizontal, "─")]
		[InlineData (0, 1, Orientation.Horizontal, "─")]
		[InlineData (0, 0, Orientation.Vertical, "│")]
		[InlineData (1, 0, Orientation.Vertical, "│")]
		[InlineData (0, 1, Orientation.Vertical, "│")]
		[Theory, AutoInitShutdown]
		public void Length_0_Is_1_Long (int x, int y, Orientation orientation, string expected)
		{
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 20);

			var canvas = new LineCanvas ();
			// Add a line at 5, 5 that's has length of 1
			canvas.AddLine (new Point (5, 5), 1, orientation, LineStyle.Single);

			foreach (var p in canvas.GetMap ()) {
				Application.Driver.Move (p.Key.X, p.Key.Y);
				Application.Driver.AddRune (p.Value);
			}

			string looksLike = $"{Environment.NewLine}{expected}";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData (0, 0, 1, Orientation.Horizontal, "─")]
		[InlineData (1, 0, 1, Orientation.Horizontal, "─")]
		[InlineData (0, 1, 1, Orientation.Horizontal, "─")]
		[InlineData (0, 0, 1, Orientation.Vertical, "│")]
		[InlineData (1, 0, 1, Orientation.Vertical, "│")]
		[InlineData (0, 1, 1, Orientation.Vertical, "│")]
		[InlineData (-1, 0, 1, Orientation.Horizontal, "─")]
		[InlineData (0, -1, 1, Orientation.Horizontal, "─")]
		[InlineData (-1, 0, 1, Orientation.Vertical, "│")]
		[InlineData (0, -1, 1, Orientation.Vertical, "│")]

		[InlineData (0, 0, -1, Orientation.Horizontal, "─")]
		[InlineData (1, 0, -1, Orientation.Horizontal, "─")]
		[InlineData (0, 1, -1, Orientation.Horizontal, "─")]
		[InlineData (0, 0, -1, Orientation.Vertical, "│")]
		[InlineData (1, 0, -1, Orientation.Vertical, "│")]
		[InlineData (0, 1, -1, Orientation.Vertical, "│")]
		[InlineData (-1, 0, -1, Orientation.Horizontal, "─")]
		[InlineData (0, -1, -1, Orientation.Horizontal, "─")]
		[InlineData (-1, 0, -1, Orientation.Vertical, "│")]
		[InlineData (0, -1, -1, Orientation.Vertical, "│")]

		[InlineData (0, 0, 2, Orientation.Horizontal, "──")]
		[InlineData (1, 0, 2, Orientation.Horizontal, "──")]
		[InlineData (0, 1, 2, Orientation.Horizontal, "──")]
		[InlineData (0, 0, 2, Orientation.Vertical, "│\n│")]
		[InlineData (1, 0, 2, Orientation.Vertical, "│\n│")]
		[InlineData (0, 1, 2, Orientation.Vertical, "│\n│")]
		[InlineData (-1, 0, 2, Orientation.Horizontal, "──")]
		[InlineData (0, -1, 2, Orientation.Horizontal, "──")]
		[InlineData (-1, 0, 2, Orientation.Vertical, "│\n│")]
		[InlineData (0, -1, 2, Orientation.Vertical, "│\n│")]

		[InlineData (0, 0, -2, Orientation.Horizontal, "──")]
		[InlineData (1, 0, -2, Orientation.Horizontal, "──")]
		[InlineData (0, 1, -2, Orientation.Horizontal, "──")]
		[InlineData (0, 0, -2, Orientation.Vertical, "│\n│")]
		[InlineData (1, 0, -2, Orientation.Vertical, "│\n│")]
		[InlineData (0, 1, -2, Orientation.Vertical, "│\n│")]
		[InlineData (-1, 0, -2, Orientation.Horizontal, "──")]
		[InlineData (0, -1, -2, Orientation.Horizontal, "──")]
		[InlineData (-1, 0, -2, Orientation.Vertical, "│\n│")]
		[InlineData (0, -1, -2, Orientation.Vertical, "│\n│")]
		[Theory, AutoInitShutdown]
		public void Length_n_Is_n_Long (int x, int y, int length, Orientation orientation, string expected)
		{
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 20);

			var offset = new Point (5, 5);

			var canvas = new LineCanvas ();
			canvas.AddLine (new Point (x, y), length, orientation, LineStyle.Single);

			foreach (var p in canvas.GetMap ()) {
				Application.Driver.Move (offset.X + p.Key.X, offset.Y + p.Key.Y);
				Application.Driver.AddRune (p.Value);
			}

			string looksLike = $"{Environment.NewLine}{expected}";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void Length_Negative ()
		{
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 20);

			var offset = new Point (5, 5);

			var canvas = new LineCanvas ();
			canvas.AddLine (offset, -2, Orientation.Horizontal, LineStyle.Single);

			foreach (var p in canvas.GetMap ()) {
				Application.Driver.Move (offset.X + p.Key.X, offset.Y + p.Key.Y);
				Application.Driver.AddRune (p.Value);
			}

			string looksLike = "──";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void Zero_Length_Intersections ()
		{
			// Draw at 1,2 within client area of View (i.e. leave a top and left margin of 1)
			// This proves we aren't drawing excess above
			int x = 1;
			int y = 2;
			int width = 5;
			int height = 2;

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width * 2, height * 2);

			var lc = new LineCanvas ();

			// ╔╡╞═════╗
			// Add a short horiz line for ╔╡
			lc.AddLine (new Point (x, y), 2, Orientation.Horizontal, LineStyle.Double);
			//LHS line down
			lc.AddLine (new Point (x, y), height, Orientation.Vertical, LineStyle.Double);

			//Vertical line before Title, results in a ╡
			lc.AddLine (new Point (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);

			//Vertical line after Title, results in a ╞
			lc.AddLine (new Point (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);

			// remainder of top line
			lc.AddLine (new Point (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);

			//RHS line down
			lc.AddLine (new Point (x + width, y), height, Orientation.Vertical, LineStyle.Double);

			foreach (var p in lc.GetMap ()) {
				Application.Driver.Move (p.Key.X, p.Key.Y);
				Application.Driver.AddRune (p.Value);
			}

			string looksLike =
		@"
 ╔╡╞══╗
 ║    ║
";
			TestHelpers.AssertDriverContentsWithFrameAre (looksLike, output);
		}

		[InlineData (LineStyle.Single)]
		[InlineData (LineStyle.Rounded)]
		[Theory, AutoInitShutdown]
		public void View_Draws_Horizontal (LineStyle style)
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 2, Orientation.Horizontal, style);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
──";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void View_Draws_Horizontal_Double ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 2, Orientation.Horizontal, LineStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
@" 
══";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData (LineStyle.Single)]
		[InlineData (LineStyle.Rounded)]
		[Theory, AutoInitShutdown]
		public void View_Draws_Vertical (LineStyle style)
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 2, Orientation.Vertical, style);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
│
│";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void View_Draws_Vertical_Double ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 2, Orientation.Vertical, LineStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
║
║";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		/// <summary>
		/// This test demonstrates that corners are only drawn when lines overlap.
		/// Not when they terminate adjacent to one another.
		/// </summary>
		[Fact, AutoInitShutdown]
		public void View_Draws_Corner_NoOverlap ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 2, Orientation.Horizontal, LineStyle.Single);
			canvas.AddLine (new Point (0, 1), 2, Orientation.Vertical, LineStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
──
│
│";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}
		/// <summary>
		/// This test demonstrates how to correctly trigger a corner.  By
		/// overlapping the lines in the same cell
		/// </summary>
		[Fact, AutoInitShutdown]
		public void View_Draws_Corner_Correct ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 2, Orientation.Horizontal, LineStyle.Single);
			canvas.AddLine (new Point (0, 0), 2, Orientation.Vertical, LineStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
┌─
│";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}

		[Fact, AutoInitShutdown]
		public void View_Draws_Window ()
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, LineStyle.Single);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, LineStyle.Single);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, LineStyle.Single);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, LineStyle.Single);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, LineStyle.Single);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, LineStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
┌────┬───┐
│    │   │
├────┼───┤
│    │   │
└────┴───┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		/// <summary>
		/// Demonstrates when <see cref="LineStyle.Rounded"/> corners are used. Notice how
		/// not all lines declare rounded.  If there are 1+ lines intersecting and a corner is
		/// to be used then if any of them are rounded a rounded corner is used.
		/// </summary>
		[Fact, AutoInitShutdown]
		public void View_Draws_Window_Rounded ()
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, LineStyle.Rounded);

			// BorderStyle.Single is ignored because corner overlaps with the above line which is Rounded
			// this results in a rounded corner being used.
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, LineStyle.Single);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, LineStyle.Rounded);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, LineStyle.Single);

			// These lines say rounded but they will result in the T sections which are never rounded.
			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, LineStyle.Rounded);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, LineStyle.Rounded);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
╭────┬───╮
│    │   │
├────┼───┤
│    │   │
╰────┴───╯";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void View_Draws_Window_Double ()
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, LineStyle.Double);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, LineStyle.Double);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, LineStyle.Double);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, LineStyle.Double);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, LineStyle.Double);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, LineStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
╔════╦═══╗
║    ║   ║
╠════╬═══╣
║    ║   ║
╚════╩═══╝";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Theory, AutoInitShutdown]
		[InlineData (LineStyle.Single)]
		[InlineData (LineStyle.Rounded)]
		public void View_Draws_Window_DoubleTop_SingleSides (LineStyle thinStyle)
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, LineStyle.Double);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, thinStyle);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, LineStyle.Double);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, thinStyle);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, thinStyle);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, LineStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
╒════╤═══╕
│    │   │
╞════╪═══╡
│    │   │
╘════╧═══╛
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Theory, AutoInitShutdown]
		[InlineData (LineStyle.Single)]
		[InlineData (LineStyle.Rounded)]
		public void View_Draws_Window_SingleTop_DoubleSides (LineStyle thinStyle)
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, thinStyle);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, LineStyle.Double);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, thinStyle);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, LineStyle.Double);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, LineStyle.Double);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, thinStyle);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
╓────╥───╖
║    ║   ║
╟────╫───╢
║    ║   ║
╙────╨───╜

";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void View_Draws_LeaveMargin_Top1_Left1 ()
		{
			// Draw at 1,1 within client area of View (i.e. leave a top and left margin of 1)
			var v = GetCanvas (out var canvas, 1, 1);

			// outer box
			canvas.AddLine (new Point (0, 0), 8, Orientation.Horizontal, LineStyle.Single);
			canvas.AddLine (new Point (8, 0), 3, Orientation.Vertical, LineStyle.Single);
			canvas.AddLine (new Point (8, 3), -8, Orientation.Horizontal, LineStyle.Single);
			canvas.AddLine (new Point (0, 3), -3, Orientation.Vertical, LineStyle.Single);


			canvas.AddLine (new Point (5, 0), 3, Orientation.Vertical, LineStyle.Single);
			canvas.AddLine (new Point (0, 2), 8, Orientation.Horizontal, LineStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"
 ┌────┬──┐
 │    │  │
 ├────┼──┤
 └────┴──┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Double, "═")]
		[InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Double, "║")]
		[InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Single, "─")]
		[InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Single, "│")]
		[InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Double, "═")]
		[InlineData (0, 0, 1, Orientation.Vertical, LineStyle.Double, "║")]
		[InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Single, "─")]
		[InlineData (0, 0, 1, Orientation.Vertical, LineStyle.Single, "│")]
		[InlineData (0, 0, 2, Orientation.Horizontal, LineStyle.Double, "══")]
		[InlineData (0, 0, 2, Orientation.Vertical, LineStyle.Double, "║\n║")]
		[InlineData (0, 0, 2, Orientation.Horizontal, LineStyle.Single, "──")]
		[InlineData (0, 0, 2, Orientation.Vertical, LineStyle.Single, "│\n│")]
		[AutoInitShutdown, Theory]
		public void View_Draws_1LineTests (
			int x1, int y1, int length, Orientation o1, LineStyle s1,
			string expected
		)
		{
			var v = GetCanvas (out var lc);
			v.Width = 10;
			v.Height = 10;
			v.Bounds = new Rect (0, 0, 10, 10);

			lc.AddLine (new Point (x1, y1), length, o1, s1);

			v.Redraw (v.Bounds);

			TestHelpers.AssertDriverContentsAre (expected, output);
		}


		[Theory, AutoInitShutdown]
		// Horizontal lines with a vertical zero-length
		[InlineData (
			0, 0, 1, Orientation.Horizontal, LineStyle.Double,
			0, 0, 0, Orientation.Vertical, LineStyle.Single, "╞"
		)]
		[InlineData (
			0, 0, -1, Orientation.Horizontal, LineStyle.Double,
			0, 0, 0, Orientation.Vertical, LineStyle.Single, "╡"
		)]
		[InlineData (
			0, 0, 1, Orientation.Horizontal, LineStyle.Single,
			0, 0, 0, Orientation.Vertical, LineStyle.Double, "╟"
		)]
		[InlineData (
			0, 0, -1, Orientation.Horizontal, LineStyle.Single,
			0, 0, 0, Orientation.Vertical, LineStyle.Double, "╢"
		)]
		[InlineData (
			0, 0, 1, Orientation.Horizontal, LineStyle.Single,
			0, 0, 0, Orientation.Vertical, LineStyle.Single, "├"
		)]
		[InlineData (
			0, 0, -1, Orientation.Horizontal, LineStyle.Single,
			0, 0, 0, Orientation.Vertical, LineStyle.Single, "┤"
		)]
		[InlineData (
			0, 0, 1, Orientation.Horizontal, LineStyle.Double,
			0, 0, 0, Orientation.Vertical, LineStyle.Double, "╠"
		)]
		[InlineData (
			0, 0, -1, Orientation.Horizontal, LineStyle.Double,
			0, 0, 0, Orientation.Vertical, LineStyle.Double, "╣"
		)]

		// Vertical lines with a horizontal zero-length
		[InlineData (
			0, 0, 1, Orientation.Vertical, LineStyle.Double,
			0, 0, 0, Orientation.Horizontal, LineStyle.Single, "╥"
		)]
		[InlineData (
			0, 0, -1, Orientation.Vertical, LineStyle.Double,
			0, 0, 0, Orientation.Horizontal, LineStyle.Single, "╨"
		)]
		[InlineData (
			0, 0, 1, Orientation.Vertical, LineStyle.Single,
			0, 0, 0, Orientation.Horizontal, LineStyle.Double, "╤"
		)]
		[InlineData (
			0, 0, -1, Orientation.Vertical, LineStyle.Single,
			0, 0, 0, Orientation.Horizontal, LineStyle.Double, "╧"
		)]
		[InlineData (
			0, 0, 1, Orientation.Vertical, LineStyle.Single,
			0, 0, 0, Orientation.Horizontal, LineStyle.Single, "┬"
		)]
		[InlineData (
			0, 0, -1, Orientation.Vertical, LineStyle.Single,
			0, 0, 0, Orientation.Horizontal, LineStyle.Single, "┴"
		)]
		[InlineData (
			0, 0, 1, Orientation.Vertical, LineStyle.Double,
			0, 0, 0, Orientation.Horizontal, LineStyle.Double, "╦"
		)]
		[InlineData (
			0, 0, -1, Orientation.Vertical, LineStyle.Double,
			0, 0, 0, Orientation.Horizontal, LineStyle.Double, "╩"
		)]

		// Crosses (two zero-length)
		[InlineData (
			0, 0, 0, Orientation.Vertical, LineStyle.Double,
			0, 0, 0, Orientation.Horizontal, LineStyle.Single, "╫"
		)]
		[InlineData (
			0, 0, 0, Orientation.Vertical, LineStyle.Single,
			0, 0, 0, Orientation.Horizontal, LineStyle.Double, "╪"
		)]
		[InlineData (
			0, 0, 0, Orientation.Vertical, LineStyle.Single,
			0, 0, 0, Orientation.Horizontal, LineStyle.Single, "┼"
		)]
		[InlineData (
			0, 0, 0, Orientation.Vertical, LineStyle.Double,
			0, 0, 0, Orientation.Horizontal, LineStyle.Double, "╬"
		)]
		public void View_Draws_2LineTests (
			int x1, int y1, int l1, Orientation o1, LineStyle s1,
			int x2, int y2, int l2, Orientation o2, LineStyle s2,
			string expected
		)
		{
			var v = GetCanvas (out var lc);
			v.Width = 10;
			v.Height = 10;
			v.Bounds = new Rect (0, 0, 10, 10);

			lc.AddLine (new Point (x1, y1), l1, o1, s1);
			lc.AddLine (new Point (x2, y2), l2, o2, s2);

			v.Redraw (v.Bounds);

			TestHelpers.AssertDriverContentsAre (expected, output);
		}


		/// <summary>
		/// Creates a new <see cref="View"/> into which a <see cref="LineCanvas"/> is rendered
		/// at <see cref="View.DrawContentComplete"/> time.
		/// </summary>
		/// <param name="canvas">The <see cref="LineCanvas"/> you can draw into.</param>
		/// <param name="offsetX">How far to offset drawing in X</param>
		/// <param name="offsetY">How far to offset drawing in Y</param>
		/// <returns></returns>
		private View GetCanvas (out LineCanvas canvas, int offsetX = 0, int offsetY = 0)
		{
			var v = new View {
				Width = 10,
				Height = 5,
				Bounds = new Rect (0, 0, 10, 5)
			};
			Application.Top.Add (v);
			Application.Begin (Application.Top);

			var canvasCopy = canvas = new LineCanvas ();
			v.DrawContentComplete += (s, e) => {
				v.Clear ();
				foreach (var p in canvasCopy.GetMap ()) {
					v.AddRune (
						offsetX + p.Key.X,
						offsetY + p.Key.Y,
						p.Value);
				}
			};

			return v;
		}
	}
}
