using Terminal.Gui.Graphs;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.Core {
	public class LineCanvasTests {

		readonly ITestOutputHelper output;

		public LineCanvasTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Dot ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 0, Orientation.Horizontal, BorderStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
.";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData (BorderStyle.Single)]
		[InlineData (BorderStyle.Rounded)]
		[Theory, AutoInitShutdown]
		public void TestLineCanvas_Horizontal (BorderStyle style)
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, style);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
──";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Horizontal_Double ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, BorderStyle.Double);

			v.Redraw (v.Bounds);

			string looksLike =
@" 
══";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[InlineData (BorderStyle.Single)]
		[InlineData(BorderStyle.Rounded)]
		[Theory, AutoInitShutdown]
		public void TestLineCanvas_Vertical (BorderStyle style)
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Vertical, style);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
│
│";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Vertical_Double ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Vertical, BorderStyle.Double);

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
		public void TestLineCanvas_Corner_NoOverlap()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (0, 1), 1, Orientation.Vertical, BorderStyle.Single);

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
		public void TestLineCanvas_Corner_Correct ()
		{
			var v = GetCanvas (out var canvas);
			canvas.AddLine (new Point (0, 0), 1, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (0, 0), 2, Orientation.Vertical, BorderStyle.Single);

			v.Redraw (v.Bounds);

			string looksLike =
@"    
┌─
│
│";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		
		}
		[Fact,AutoInitShutdown]
		public void TestLineCanvas_Window ()
		{
			var v = GetCanvas (out var canvas);
			
			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, BorderStyle.Single);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, BorderStyle.Single);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, BorderStyle.Single);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, BorderStyle.Single);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, BorderStyle.Single);

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

		[Fact, AutoInitShutdown]
		public void TestLineCanvas_Window_Double ()
		{
			var v = GetCanvas (out var canvas);

			// outer box
			canvas.AddLine (new Point (0, 0), 9, Orientation.Horizontal, BorderStyle.Double);
			canvas.AddLine (new Point (9, 0), 4, Orientation.Vertical, BorderStyle.Double);
			canvas.AddLine (new Point (9, 4), -9, Orientation.Horizontal, BorderStyle.Double);
			canvas.AddLine (new Point (0, 4), -4, Orientation.Vertical, BorderStyle.Double);


			canvas.AddLine (new Point (5, 0), 4, Orientation.Vertical, BorderStyle.Double);
			canvas.AddLine (new Point (0, 2), 9, Orientation.Horizontal, BorderStyle.Double);

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

		private View GetCanvas (out LineCanvas canvas)
		{
			var v = new View {
				Width = 10,
				Height = 5,
				Bounds = new Rect (0, 0, 10, 5)
			};

			var canvasCopy = canvas =  new LineCanvas (Application.Driver);
			v.DrawContentComplete += (r)=> canvasCopy.Draw (v, v.Bounds);

			return v;
		}
	}
}
