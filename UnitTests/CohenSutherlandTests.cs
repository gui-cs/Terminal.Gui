using Terminal.Gui;
using Xunit;

namespace UnitTests {
	public class CohenSutherlandTests {

		[Theory]
		[InlineData (-1, 0.5, 1,0.5,true)] // strike through (middle) →
		[InlineData (0.5, -1, 0.5, 1, true)] // strike through (middle) ↓
		[InlineData (0.75, -0.1, 1.1, 0.75, true)] // strike through (clip UR corner) ↘
		[InlineData (-0.1, 0.25, 0.5, 1.1, true)] // strike through (clip LL corner) ↘
		[InlineData (-0.1, 0.75, 0.5, -0.1, true)] // strike through (clip UL corner) ↗
		[InlineData (0.5, 1.1, 2, 0.5, true)] // strike through (clip LR corner) ↗
		[InlineData (-1, -1, 1, -1, false)] // miss because above →
		[InlineData (-1, 1.1, 1, 1.1, false)] // miss because below →
		public void ClipsSquareTests(decimal x1,decimal y1, decimal x2, decimal y2, bool expectClip)
		{
			var clip = new RectangleD (0,0,1,1);
			var line = new LineD (new PointD(x1, y1),new PointD(x2, y2));

			// we are supposed to be testing lines clipping the rectangle not inside it!
			Assert.False(clip.Contains (line.Start));
			Assert.False(clip.Contains (line.End));

			if (expectClip) {
				Assert.NotNull (CohenSutherland.CohenSutherlandLineClip (clip, line.Start, line.End));
			}
			else {
				Assert.Null (CohenSutherland.CohenSutherlandLineClip (clip, line.Start, line.End));
			}

			// also check reversing the line direction (which should have no impact on test result)
			line = new LineD (new PointD (x2, y2), new PointD (x1, y1));

			if (expectClip) {
				Assert.NotNull (CohenSutherland.CohenSutherlandLineClip (clip, line.Start, line.End));
			} else {
				Assert.Null (CohenSutherland.CohenSutherlandLineClip (clip, line.Start, line.End));
			}

		}
	}
}
