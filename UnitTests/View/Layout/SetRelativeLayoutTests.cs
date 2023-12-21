using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

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

	[Fact] [TestRespondersDisposed]
	public void PosCombine_Center_Plus_Absolute ()
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