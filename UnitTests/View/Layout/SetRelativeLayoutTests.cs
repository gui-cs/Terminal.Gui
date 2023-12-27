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
		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
		Assert.Null (view.X);
		Assert.Null (view.Y);

		view.BeginInit(); view.EndInit();

		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
		Assert.Null (view.X);
		Assert.Null (view.Y);

		view.SetRelativeLayout (new Rect (5, 5, 10, 10));
		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
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
		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
		Assert.NotNull (view.X);
		Assert.NotNull (view.Y);

		view.BeginInit (); view.EndInit ();

		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
		Assert.NotNull (view.X);
		Assert.NotNull (view.Y);

		view.SetRelativeLayout (new Rect (5, 5, 10, 10));
		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
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
		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
		Assert.Null (view.Width);
		Assert.Null (view.Height);
		view.BeginInit (); view.EndInit ();

		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
		Assert.Null (view.Width);
		Assert.Null (view.Height);

		view.SetRelativeLayout (new Rect (5, 5, 10, 10));
		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
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
		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
		Assert.NotNull (view.Width);
		Assert.NotNull (view.Height);
		view.BeginInit (); view.EndInit ();

		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
		Assert.NotNull (view.Width);
		Assert.NotNull (view.Height);

		view.SetRelativeLayout (new Rect (5, 5, 10, 10));
		Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
		Assert.NotNull (view.Width);
		Assert.NotNull (view.Height);
		
		Assert.Equal (0, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
		// BUGBUG: Width == null is same as Dim.Absolute (0) (or should be). Thus this is a bug.
		Assert.Equal (expectedDim, view.Frame.Width);
		Assert.Equal (expectedDim, view.Frame.Height);
	}

	[Fact]
	public void Fill_Pos_Within_Bounds ()
	{
		var screen = new Rect (0, 0, 80, 25);
		var view = new View () {
			X = 1,
			Y = 1,
			Width = 5,
			Height = 4
		};

		view.SetRelativeLayout (screen);
		Assert.Equal (1, view.Frame.X);
		Assert.Equal (1, view.Frame.Y);
		Assert.Equal (5, view.Frame.Width);
		Assert.Equal (4, view.Frame.Height);

		view.Width = 80;
		view.Height = 25;
		view.SetRelativeLayout (screen);
		Assert.Equal (1, view.Frame.X);
		Assert.Equal (1, view.Frame.Y);
		Assert.Equal (80, view.Frame.Width);
		Assert.Equal (25, view.Frame.Height);

		view.Width = Dim.Fill ();
		view.Height = Dim.Fill ();
		view.SetRelativeLayout (screen);
		Assert.Equal (1, view.Frame.X);
		Assert.Equal (1, view.Frame.Y);
		Assert.Equal (79, view.Frame.Width); // proof (80 - 1)
		Assert.Equal (24, view.Frame.Height); // proof (25 - 1)

		view.X = 79;
		view.Width = Dim.Fill ();
		view.Height = Dim.Fill ();
		view.SetRelativeLayout (screen);
		Assert.Equal (79, view.Frame.X);
		Assert.Equal (1, view.Frame.Y);
		Assert.Equal (1, view.Frame.Width); // proof (80 - 79)
		Assert.Equal (24, view.Frame.Height);

		view.X = 80;
		view.Width = Dim.Fill ();
		view.Height = Dim.Fill ();
		view.SetRelativeLayout (screen);
		Assert.Equal (80, view.Frame.X);
		Assert.Equal (1, view.Frame.Y);
		Assert.Equal (0, view.Frame.Width);  // proof (80 - 80)
		Assert.Equal (24, view.Frame.Height);
	}

	[Fact]
	public void FIll_Pos_Outside_Bounds ()
	{
		var screen = new Rect (0, 0, 80, 25);
		var view = new View () {
			X = 90,  // outside of screen +10
			Y = -10,   // outside of screen -10
			Width = 15,
			Height = 15
		};

		view.SetRelativeLayout (screen);
		Assert.Equal (90, view.Frame.X);
		Assert.Equal (-10, view.Frame.Y);
		Assert.Equal (15, view.Frame.Width);
		Assert.Equal (15, view.Frame.Height);

		// prove Width=Height= same as screen size
		view.Width = 80;
		view.Height = 25;
		view.SetRelativeLayout (screen);
		Assert.Equal (90, view.Frame.X);
		Assert.Equal (-10, view.Frame.Y);
		Assert.Equal (80, view.Frame.Width);
		Assert.Equal (25, view.Frame.Height);

		view.Width = Dim.Fill ();
		view.Height = Dim.Fill ();
		view.SetRelativeLayout (screen);
		Assert.Equal (90, view.Frame.X);
		Assert.Equal (-10, view.Frame.Y);
		Assert.Equal (0, view.Frame.Width);    // proof: 15x15 view is placed beyond right side of screen, so fill width is 0
		Assert.Equal (35, view.Frame.Height);  // proof: 15x15 view is placed beyond top of screen 10 rows, screen is 25 rows. so fill height is 25 + 10 = 35
	}

	[Fact]
	public void PosCombine_PosCenter_Minus_Absolute ()
	{
		// This test used to be in ViewTests.cs Internal_Tests. It was moved here because it is testing
		// SetRelativeLayout. In addition, the old test was bogus because it was testing the wrong thing (and 
		// because in v1 Pos.Center was broken in this regard!

		var screen = new Rect (0, 0, 80, 25);
		var view = new View () {
			X = Pos.Center () - 41,  // -2 off left edge of screen
			Y = Pos.Center () - 13,  // -1 off top edge of screen
			Width = 1,
			Height = 1
		};

		view.SetRelativeLayout (screen);
		Assert.Equal (-2, view.Frame.X); // proof: 1x1 view centered in 80x25 screen has x of 39, so -41 is -2
		Assert.Equal (-1, view.Frame.Y); // proof: 1x1 view centered in 80x25 screen has y of 12, so -13 is -1

		view.Width = 80;
		view.Height = 25;
		view.SetRelativeLayout (screen);
		Assert.Equal (-41, view.Frame.X);
		Assert.Equal (-13, view.Frame.Y);
		Assert.Equal (80, view.Frame.Width);
		Assert.Equal (25, view.Frame.Height);

		view.Width = Dim.Fill (); 
		view.Height = Dim.Fill ();
		view.SetRelativeLayout (screen);
		Assert.Equal (-41, view.Frame.X); 
		Assert.Equal (-13, view.Frame.Y);
		Assert.Equal (121, view.Frame.Width);  // 121 = screen.Width - (-Center - 41)
		Assert.Equal (38, view.Frame.Height);
	}

	[Fact]
	public void FIll_And_PosCenter ()
	{
		var screen = new Rect (0, 0, 80, 25);
		var view = new View () {
			X = Pos.Center (),
			Y = Pos.Center (),
			Width = Dim.Fill(),
			Height = Dim.Fill()
		};

		view.SetRelativeLayout (screen);
		Assert.Equal (0, view.Frame.X); 
		Assert.Equal (0, view.Frame.Y);
		Assert.Equal (80, view.Frame.Width);
		Assert.Equal (25, view.Frame.Height);

		view.X = Pos.Center () + 1;
		view.SetRelativeLayout (screen);
		Assert.Equal (1, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
		Assert.Equal (79, view.Frame.Width);
		Assert.Equal (25, view.Frame.Height);

		view.X = Pos.Center () + 79;
		view.SetRelativeLayout (screen);
		Assert.Equal (79, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
		Assert.Equal (1, view.Frame.Width);
		Assert.Equal (25, view.Frame.Height);

		view.X = Pos.Center () + 80;
		view.SetRelativeLayout (screen);
		Assert.Equal (80, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
		Assert.Equal (0, view.Frame.Width);
		Assert.Equal (25, view.Frame.Height);

		view.X = Pos.Center () - 1;
		view.SetRelativeLayout (screen);
		Assert.Equal (-1, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
		Assert.Equal (81, view.Frame.Width); 
		Assert.Equal (25, view.Frame.Height);

		view.X = Pos.Center () - 2; // Fill means all the way to right. So width will be 82. (dim gets calc'd before pos).
		view.SetRelativeLayout (screen);
		Assert.Equal (-2, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
		Assert.Equal (82, view.Frame.Width); 
		Assert.Equal (25, view.Frame.Height);

		view.X = Pos.Center () - 3; // Fill means all the way to right. So width will be 83. (dim gets calc'd before pos).
		view.SetRelativeLayout (screen);
		Assert.Equal (-3, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
		Assert.Equal (83, view.Frame.Width);
		Assert.Equal (25, view.Frame.Height);

		view.X = Pos.Center () - 41; // Fill means all the way to right. So width will be . (dim gets calc'd before pos).
		view.SetRelativeLayout (screen);
		Assert.Equal (-41, view.Frame.X);
		Assert.Equal (0, view.Frame.Y);
		Assert.Equal (121, view.Frame.Width);
		Assert.Equal (25, view.Frame.Height);

	}
	[Fact]
	public void PosCombine_PosCenter_Plus_Absolute ()
	{
		var screen = new Rect (0, 0, 80, 25);
		var view = new View () {
			X = Pos.Center () + 41,  // ((80 / 2) - (5 / 2)) + 41 = (40 - 3 + 41) = 78
			Y = Pos.Center () + 13,  // ((25 / 2) - (4 / 2)) + 13 = (12 - 2 + 13) = 23
			Width = 5,
			Height = 4
		};

		view.SetRelativeLayout (screen);
		Assert.Equal (78, view.Frame.X);
		Assert.Equal (23, view.Frame.Y);
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