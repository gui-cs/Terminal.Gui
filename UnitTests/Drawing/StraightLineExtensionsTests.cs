using System.Linq;
using Xunit;

namespace Terminal.Gui.DrawingTests {
	public class StraightLineExtensionsTests
	{
		[Fact, AutoInitShutdown]
		public void TestExcludeParallel_LeftOnly ()
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
		public void TestExcludeParallel_RightOnly ()
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
		public void TestExcludeParallel_HorizontalSplit ()
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
	}
}
