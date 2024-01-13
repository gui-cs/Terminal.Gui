using System;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AdornmentTests {
	readonly ITestOutputHelper _output;

	public AdornmentTests (ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void Setting_SuperView_Throws ()
	{
		var adornment = new Adornment (null);
		Assert.Throws<NotImplementedException> (() => adornment.SuperView = new View ());
	}

	[Fact]
	public void Setting_SuperViewRendersLineCanvas_Throws ()
	{
		var adornment = new Adornment (null);
		Assert.Throws<NotImplementedException> (() => adornment.SuperViewRendersLineCanvas = true);
	}

	[Fact]
	public void Setting_Thickness_Changes_Parent_Bounds ()
	{
		var parent = new View () {
			Width = 10,
			Height = 10
		};
		parent.BeginInit ();
		parent.EndInit ();

		Assert.Equal (new Rect (0, 0, 10, 10), parent.Frame);
		Assert.Equal (new Rect (0, 0, 10, 10), parent.Bounds);

		parent.Margin.Thickness = new Thickness (1);
		Assert.Equal (new Rect (0, 0, 10, 10), parent.Frame);
		Assert.Equal (new Rect (0, 0, 8, 8), parent.Bounds);
	}

	[Fact]
	public void Setting_Thickness_Raises_ThicknessChanged ()
	{
		var adornment = new Adornment (null);
		var super = new View ();
		var raised = false;
		adornment.ThicknessChanged += (s, e) => {
			raised = true;
			Assert.Equal (Thickness.Empty, e.PreviousThickness);
			Assert.Equal (new Thickness (1, 2, 3, 4), e.Thickness);
			Assert.Equal (new Thickness (1, 2, 3, 4), adornment.Thickness);
		};
		adornment.Thickness = new Thickness (1, 2, 3, 4);
		Assert.True (raised);
	}

	[Fact]
	public void Setting_Bounds_Throws ()
	{
		var adornment = new Adornment (null);
		Assert.Throws<InvalidOperationException> (() => adornment.Bounds = new Rect (1, 2, 3, 4));
	}

	[Fact]
	public void GetAdornmentsThickness ()
	{
		var view = new View ();
		Assert.Equal (Thickness.Empty, view.GetAdornmentsThickness ());

		view.Margin.Thickness = new Thickness (1);
		Assert.Equal (new Thickness (1), view.GetAdornmentsThickness ());

		view.Border.Thickness = new Thickness (1);
		Assert.Equal (new Thickness (2), view.GetAdornmentsThickness ());

		view.Padding.Thickness = new Thickness (1);
		Assert.Equal (new Thickness (3), view.GetAdornmentsThickness ());

		view.Padding.Thickness = new Thickness (2);
		Assert.Equal (new Thickness (4), view.GetAdornmentsThickness ());

		view.Padding.Thickness = new Thickness (1, 2, 3, 4);
		Assert.Equal (new Thickness (3, 4, 5, 6), view.GetAdornmentsThickness ());

		view.Margin.Thickness = new Thickness (1, 2, 3, 4);
		Assert.Equal (new Thickness (3, 5, 7, 9), view.GetAdornmentsThickness ());
		view.Dispose ();
	}

	[Fact]
	public void FrameToScreen_Uses_Parent_Not_SuperView ()
	{
		var parent = new View () {
			X = 1,
			Y = 2,
			Width = 10,
			Height = 10
		};

		parent.BeginInit ();
		parent.EndInit ();

		Assert.Equal (new Rect (1, 2, 10, 10), parent.Frame);
		Assert.Equal (new Rect (0, 0, 10, 10), parent.Bounds);
		Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Frame);
		Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Bounds);

		Assert.Null (parent.Margin.SuperView);
		Assert.Equal (new Rect (1, 2, 10, 10), parent.Margin.FrameToScreen ());
	}

	[Fact]
	public void BoundsToScreen_Uses_Parent_Not_SuperView ()
	{
		var parent = new View () {
			X = 1,
			Y = 2,
			Width = 10,
			Height = 10
		};

		parent.BeginInit ();
		parent.EndInit ();

		Assert.Equal (new Rect (1, 2, 10, 10), parent.Frame);
		Assert.Equal (new Rect (0, 0, 10, 10), parent.Bounds);
		Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Frame);
		Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Bounds);

		Assert.Null (parent.Margin.SuperView);
		var boundsAsScreen = parent.Margin.BoundsToScreen (new Rect (1, 2, 5, 5));
		Assert.Equal (new Rect (2, 4, 5, 5), boundsAsScreen);
	}
}