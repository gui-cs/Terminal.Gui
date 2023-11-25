﻿using System.Linq;
using Xunit;

namespace Terminal.Gui.DrawingTests {
	public class StraightLineExtensionsTests
	{
		#region Parallel Tests
		[Fact, AutoInitShutdown]
		public void TestExcludeParallel_HorizontalLines_LeftOnly ()
		{
			// x=1 to x=10
			var l1 = new StraightLine (new Point (1, 2), 10, Orientation.Horizontal, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude x=3 to x=103
						.Exclude (new Point (3, 2), 100, Orientation.Horizontal)
						.ToArray ();
			// x=1 to x=2
			var afterLine = Assert.Single (after);
			Assert.Equal (l1.Start, afterLine.Start);
			Assert.Equal (2, afterLine.Length);
		}


		[Fact, AutoInitShutdown]
		public void TestExcludeParallel_HorizontalLines_RightOnly ()
		{
			// x=1 to x=10
			var l1 = new StraightLine (new Point (1, 2), 10, Orientation.Horizontal, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude x=0 to x=2
						.Exclude (new Point (0, 2), 3, Orientation.Horizontal)
						.ToArray ();
			// x=3 to x=10
			var afterLine = Assert.Single (after);
			Assert.Equal (3, afterLine.Start.X);
			Assert.Equal (2, afterLine.Start.Y);
			Assert.Equal (8, afterLine.Length);
		}


		[Fact, AutoInitShutdown]
		public void TestExcludeParallel_HorizontalLines_HorizontalSplit ()
		{
			// x=1 to x=10
			var l1 = new StraightLine (new Point (1, 2), 10, Orientation.Horizontal, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude x=4 to x=5
						.Exclude (new Point (4, 2), 2, Orientation.Horizontal)
						.ToArray ();
			// x=1 to x=3,
			// x=6 to x=10
			Assert.Equal (2, after.Length);
			var afterLeft = after [0];
			var afterRight = after [1];

			Assert.Equal (1, afterLeft.Start.X);
			Assert.Equal (2, afterLeft.Start.Y);
			Assert.Equal (3, afterLeft.Length);

			Assert.Equal (6, afterRight.Start.X);
			Assert.Equal (2, afterRight.Start.Y);
			Assert.Equal (5, afterRight.Length);
		}


		[Fact, AutoInitShutdown]
		public void TestExcludeParallel_HorizontalLines_CoverCompletely ()
		{
			// x=1 to x=10
			var l1 = new StraightLine (new Point (1, 2), 10, Orientation.Horizontal, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude x=4 to x=5
						.Exclude (new Point (1, 2), 10, Orientation.Horizontal)
						.ToArray ();
			Assert.Empty (after);
		}

		[Fact, AutoInitShutdown]
		public void TestExcludeParallel_VerticalLines_TopOnly ()
		{
			// y=1 to y=10
			var l1 = new StraightLine (new Point (2, 1), 10, Orientation.Vertical, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude y=3 to y=103
						.Exclude (new Point (2, 3), 100, Orientation.Vertical)
						.ToArray ();
			// y=1 to y=2
			var afterLine = Assert.Single (after);
			Assert.Equal (l1.Start, afterLine.Start);
			Assert.Equal (2, afterLine.Length);
		}


		[Fact, AutoInitShutdown]
		public void TestExcludeParallel_HorizontalLines_BottomOnly ()
		{
			// y=1 to y=10
			var l1 = new StraightLine (new Point (2, 1), 10, Orientation.Vertical, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude y=0 to y=2
						.Exclude (new Point (2,0), 3, Orientation.Vertical)
						.ToArray ();
			// y=3 to y=10
			var afterLine = Assert.Single (after);
			Assert.Equal (3, afterLine.Start.Y);
			Assert.Equal (2, afterLine.Start.X);
			Assert.Equal (8, afterLine.Length);
		}


		[Fact, AutoInitShutdown]
		public void TestExcludeParallel_VerticalLines_VerticalSplit ()
		{
			// y=1 to y=10
			var l1 = new StraightLine (new Point (2,1), 10, Orientation.Vertical, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude y=4 to y=5
						.Exclude (new Point (2, 4), 2, Orientation.Vertical)
						.ToArray ();
			// y=1 to y=3,
			// y=6 to y=10
			Assert.Equal (2, after.Length);
			var afterLeft = after [0];
			var afterRight = after [1];

			Assert.Equal (1, afterLeft.Start.Y);
			Assert.Equal (2, afterLeft.Start.X);
			Assert.Equal (3, afterLeft.Length);

			Assert.Equal (6, afterRight.Start.Y);
			Assert.Equal (2, afterRight.Start.X);
			Assert.Equal (5, afterRight.Length);
		}


		[Fact, AutoInitShutdown]
		public void TestExcludeParallel_VerticalLines_CoverCompletely ()
		{
			// y=1 to y=10
			var l1 = new StraightLine (new Point (2,1), 10, Orientation.Vertical, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude y=4 to y=5
						.Exclude (new Point (2,1), 10, Orientation.Vertical)
						.ToArray ();
			Assert.Empty (after);
		}


		#endregion

		#region Perpendicular Intersection Tests
		[Fact, AutoInitShutdown]
		public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_Splits ()
		{
			// x=1 to x=10
			var l1 = new StraightLine (new Point (1, 2), 10, Orientation.Horizontal, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude x=3 y=0-10
						.Exclude (new Point (3, 0), 10, Orientation.Vertical)
						.ToArray ();
			// x=1 to x=2,
			// x=4 to x=10
			Assert.Equal (2, after.Length);
			var afterLeft = after [0];
			var afterRight = after [1];

			Assert.Equal (1, afterLeft.Start.X);
			Assert.Equal (2, afterLeft.Start.Y);
			Assert.Equal (2, afterLeft.Length);

			Assert.Equal (4, afterRight.Start.X);
			Assert.Equal (2, afterRight.Start.Y);
			Assert.Equal (7, afterRight.Length);
		}

		[Fact, AutoInitShutdown]
		public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_ClipLeft ()
		{
			// x=1 to x=10
			var l1 = new StraightLine (new Point (1, 2), 10, Orientation.Horizontal, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude x=1 y=0-10
						.Exclude (new Point (1, 0), 10, Orientation.Vertical)
						.ToArray ();
			// x=2 to x=10,
			var lineAfter = Assert.Single(after);

			Assert.Equal (2, lineAfter.Start.X);
			Assert.Equal (2, lineAfter.Start.Y);
			Assert.Equal (9, lineAfter.Length);
		}

		[Fact, AutoInitShutdown]
		public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_ClipRight ()
		{
			// x=1 to x=10
			var l1 = new StraightLine (new Point (1, 2), 10, Orientation.Horizontal, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude x=10 y=0-10
						.Exclude (new Point (10, 0), 10, Orientation.Vertical)
						.ToArray ();
			// x=1 to x=9,
			var lineAfter = Assert.Single (after);

			Assert.Equal (1, lineAfter.Start.X);
			Assert.Equal (2, lineAfter.Start.Y);
			Assert.Equal (9, lineAfter.Length);
		}


		[Fact, AutoInitShutdown]
		public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_MissLeft ()
		{
			// x=1 to x=10
			var l1 = new StraightLine (new Point (1, 2), 10, Orientation.Horizontal, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude x=0 y=0-10
						.Exclude (new Point (0, 0), 10, Orientation.Vertical)
						.ToArray ();
			// Exclusion line is too far to the left so hits nothing
			Assert.Same(Assert.Single (after),l1);
		}
		[Fact, AutoInitShutdown]
		public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_MissRight ()
		{
			// x=1 to x=10
			var l1 = new StraightLine (new Point (1, 2), 10, Orientation.Horizontal, LineStyle.Single);
			var after = new StraightLine [] { l1 }
						// exclude x=11 y=0-10
						.Exclude (new Point (11, 0), 10, Orientation.Vertical)
						.ToArray ();
			// Exclusion line is too far to the right so hits nothing
			Assert.Same (Assert.Single (after), l1);
		}


		// TODO: Replicate above tests for Vertical lines (Horizontal exclusion)

		#endregion Perpendicular Intersection Tests
	}
}
