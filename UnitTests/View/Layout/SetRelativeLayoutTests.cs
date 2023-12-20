using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class SetRelativeLayoutTests {
	readonly ITestOutputHelper _output;

	public SetRelativeLayoutTests (ITestOutputHelper output) => _output = output;

	[Fact] [TestRespondersDisposed]
	public void SetRelativeLayout_PosCombine_Center_Plus_Absolute ()
	{
		var superView = new View () {
			AutoSize = false,
			Width = 10,
			Height = 10
		};

		var testView = new View () {
			AutoSize = false,
			X = Pos.Center (),
			Y = Pos.Center (),
			Width = 1,
			Height = 1
		};
		superView.Add (testView);
		testView.SetRelativeLayout (superView.Frame);
		Assert.Equal (4, testView.Frame.X);
		Assert.Equal (4, testView.Frame.Y);

		testView = new View () {
			AutoSize = false,
			X = Pos.Center () + 1,
			Y = Pos.Center () + 1,
			Width = 1,
			Height = 1
		};
		superView.Add (testView);
		testView.SetRelativeLayout (superView.Frame);
		Assert.Equal (5, testView.Frame.X);
		Assert.Equal (5, testView.Frame.Y);

		testView = new View () {
			AutoSize = false,
			X = 1 + Pos.Center (),
			Y = 1 + Pos.Center (),
			Width = 1,
			Height = 1
		};
		superView.Add (testView);
		testView.SetRelativeLayout (superView.Frame);
		Assert.Equal (5, testView.Frame.X);
		Assert.Equal (5, testView.Frame.Y);

		testView = new View () {
			AutoSize = false,
			X = 1 + Pos.Percent (50),
			Y = Pos.Percent (50) + 1,
			Width = 1,
			Height = 1
		};
		superView.Add (testView);
		testView.SetRelativeLayout (superView.Frame);
		Assert.Equal (6, testView.Frame.X);
		Assert.Equal (6, testView.Frame.Y);

		testView = new View () {
			AutoSize = false,
			X = Pos.Percent (10) + Pos.Percent (40),
			Y = Pos.Percent (10) + Pos.Percent (40),
			Width = 1,
			Height = 1
		};
		superView.Add (testView);
		testView.SetRelativeLayout (superView.Frame);
		Assert.Equal (5, testView.Frame.X);
		Assert.Equal (5, testView.Frame.Y);

		testView = new View () {
			AutoSize = false,
			X = 1 + Pos.Percent (10) + Pos.Percent (40) - 1,
			Y = 5 + Pos.Percent (10) + Pos.Percent (40) - 5,
			Width = 1,
			Height = 1
		};
		superView.Add (testView);
		testView.SetRelativeLayout (superView.Frame);
		Assert.Equal (5, testView.Frame.X);
		Assert.Equal (5, testView.Frame.Y);

		testView = new View () {
			AutoSize = false,
			X = Pos.Left (testView),
			Y = Pos.Left (testView),
			Width = 1,
			Height = 1
		};
		superView.Add (testView);
		testView.SetRelativeLayout (superView.Frame);
		Assert.Equal (5, testView.Frame.X);
		Assert.Equal (5, testView.Frame.Y);

		testView = new View () {
			AutoSize = false,
			X = 1 + Pos.Left (testView),
			Y = Pos.Top (testView) + 1,
			Width = 1,
			Height = 1
		};
		superView.Add (testView);
		testView.SetRelativeLayout (superView.Frame);
		Assert.Equal (6, testView.Frame.X);
		Assert.Equal (6, testView.Frame.Y);

		superView.Dispose ();

	}
}