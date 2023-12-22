using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using static Terminal.Gui.SpinnerStyle;

namespace Terminal.Gui.ViewTests;

public class SetRelativeLayoutTests {
	readonly ITestOutputHelper _output;

	public SetRelativeLayoutTests (ITestOutputHelper output) => _output = output;

	[Fact]
	public void Null_Pos_Is_Same_As_PosAbsolute0 ()
	{
		var view = new View () {
			X = null,
			Y = null,
		};

		// Default layout style is Computed
		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.Null (view.X);
		Assert.Null (view.Y);

		view.BeginInit(); view.EndInit();

		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.Null (view.X);
		Assert.Null (view.Y);

		view.SetRelativeLayout(new Rect(5, 5, 10, 10));
		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.Null (view.X);
		Assert.Null (view.Y);

		Assert.Equal (0, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
	}

	[Theory]
	[InlineData (1, 1)]
	[InlineData (0, 0)]
	public void NonNull_Pos (int pos, int expectedPos)
	{
		var view = new View () {
			X = pos,
			Y = pos,
		};

		// Default layout style is Computed
		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.NotNull (view.X);
		Assert.NotNull (view.Y);

		view.BeginInit (); view.EndInit ();

		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.NotNull (view.X);
		Assert.NotNull (view.Y);

		view.SetRelativeLayout (new Rect (5, 5, 10, 10));
		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.NotNull (view.X);
		Assert.NotNull (view.Y);

		Assert.Equal (expectedPos, view.Frame.X);
		Assert.Equal (expectedPos, view.Frame.Y);
	}

	[Fact]
	public void Null_Dim_Is_Same_As_DimFill0 ()
	{
		var view = new View () {
			Width = null,
			Height = null,
		};

		// Default layout style is Computed
		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.Null (view.Width);
		Assert.Null (view.Height);
		view.BeginInit (); view.EndInit ();

		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.Null (view.Width);
		Assert.Null (view.Height);

		view.SetRelativeLayout (new Rect (5, 5, 10, 10));
		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.Null (view.Width);
		Assert.Null (view.Height);
		
		Assert.Equal (0, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);

		Assert.Equal (10, view.Frame.Width);
		Assert.Equal (10, view.Frame.Height);

		view.Width = Dim.Fill (0);
		view.Height = Dim.Fill (0);
		view.SetRelativeLayout (new Rect (5, 5, 10, 10));
		Assert.Equal (10, view.Frame.Width);
		Assert.Equal (10, view.Frame.Height);

	}


	[Theory]
	[InlineData(1, 1)]
	[InlineData (0, 0)]
	public void NonNull_Dim (int dim, int expectedDim)
	{
		var view = new View () {
			Width = dim,
			Height = dim,
		};

		// Default layout style is Computed
		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.NotNull (view.Width);
		Assert.NotNull (view.Height);
		view.BeginInit (); view.EndInit ();

		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.NotNull (view.Width);
		Assert.NotNull (view.Height);

		view.SetRelativeLayout (new Rect (5, 5, 10, 10));
		Assert.Equal (view.LayoutStyle, LayoutStyle.Computed);
		Assert.NotNull (view.Width);
		Assert.NotNull (view.Height);
		
		Assert.Equal (0, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
		// BUGBUG: Width == null is same as Dim.Absolute (0) (or should be). Thus this is a bug.
		Assert.Equal (expectedDim, view.Frame.Width);
		Assert.Equal (expectedDim, view.Frame.Height);
	}

	[Fact]
	public void PosCombine_PosCenter_Minus_Absolute ()
	{
		// This test used to be in ViewTests.cs Internal_Tests. It was moved here because it is testing
		// SetRelativeLayout. In addition, the old test was bogus because it was testing the wrong thing (and 
		// because in v1 Pos.Center was broken in this regard!

		var screen = new Rect (0, 0, 80, 25);
		var view = new View () {
			X = Pos.Center () - 41,  // ((80 / 2) - (5 / 2)) - 41 = (40 - 2 - 41) = -3
			Y = Pos.Center () - 13,  // ((25 / 2) - (4 / 2)) - 13 = (12 - 2 - 13) = -3
			Width = 5,
			Height = 4
		};

		view.SetRelativeLayout (screen);
		Assert.Equal (-21, view.Frame.X); // BUGBUG: Should be -3
		Assert.Equal (-7, view.Frame.Y);  // BUGBUG: Should be -3
	}

	[Fact]
	public void PosCombine_PosCenter_Plus_Absolute ()
	{
		var screen = new Rect (0, 0, 80, 25);
		var view = new View () {
			X = Pos.Center () + 41,  // ((80 / 2) - (5 / 2)) + 41 = (40 - 2 + 41) = 79
			Y = Pos.Center () + 13,  // ((25 / 2) - (4 / 2)) + 13 = (12 - 2 + 13) = 23
			Width = 5,
			Height = 4
		};

		view.SetRelativeLayout (screen);
		Assert.Equal (79, view.Frame.X); // BUGBUG: Should be 79
		Assert.Equal (23, view.Frame.Y);  // BUGBUG: Should be 23
	}

	[Fact] [TestRespondersDisposed]
	public void PosCombine_Plus_Absolute ()
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
			X = Pos.Center () + 1, // ((10 / 2) - (1 / 2)) + 1 = 5 - 1 + 1 = 5
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