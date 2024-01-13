using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;
public class FrameTests {
	readonly ITestOutputHelper _output;

	public FrameTests (ITestOutputHelper output)
	{
		_output = output;
	}

	// Test FrameToScreen
	[Theory]
	[InlineData (0, 0, 0, 0)]
	[InlineData (1, 0, 1, 0)]
	[InlineData (0, 1, 0, 1)]
	[InlineData (1, 1, 1, 1)]
	[InlineData (10, 10, 10, 10)]
	public void FrameToScreen_NoSuperView (int frameX, int frameY, int expectedScreenX, int expectedScreenY)
	{
		var view = new View () {
			X = frameX,
			Y = frameY,
			Width = 10,
			Height = 10
		};
		var expected = new Rect (expectedScreenX, expectedScreenY, 10, 10);
		var actual = view.FrameToScreen ();
		Assert.Equal (expected, actual);
	}

	[Theory]
	[InlineData (0, 0, 0, 0, 0)]
	[InlineData (1, 0, 0, 1, 1)]
	[InlineData (2, 0, 0, 2, 2)]
	[InlineData (1, 1, 0, 2, 1)]
	[InlineData (1, 0, 1, 1, 2)]
	[InlineData (1, 1, 1, 2, 2)]
	[InlineData (1, 10, 10, 11, 11)]
	public void FrameToScreen_SuperView (int superOffset, int frameX, int frameY, int expectedScreenX, int expectedScreenY)
	{
		var super = new View() {
			X = superOffset,
			Y = superOffset,
			Width = 20,
			Height = 20
		};
		
		var view = new View () {
			X = frameX,
			Y = frameY,
			Width = 10,
			Height = 10
		};
		super.Add (view);
		var expected = new Rect (expectedScreenX, expectedScreenY, 10, 10);
		var actual = view.FrameToScreen ();
		Assert.Equal (expected, actual);
	}
}
