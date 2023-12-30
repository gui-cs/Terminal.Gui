using Xunit;
using Terminal.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.ViewsTests;

public class SliderOptionTests {


	[Fact]
	public void Slider_Option_Default_Constructor ()
	{
		var o = new SliderOption<int> ();
		Assert.Null (o.Legend);
		Assert.Equal (default, o.LegendAbbr);
		Assert.Equal (default, o.Data);
	}

	[Fact]
	public void Slider_Option_Values_Constructor ()
	{
		var o = new SliderOption<int> ("1 thousand", new Rune ('y'), 1000);
		Assert.Equal ("1 thousand", o.Legend);
		Assert.Equal (new Rune ('y'), o.LegendAbbr);
		Assert.Equal (1000, o.Data);
	}

	[Fact]
	public void OnSet_Should_Raise_SetEvent ()
	{
		// Arrange
		var sliderOption = new SliderOption<int> ();
		var eventRaised = false;
		sliderOption.Set += (sender, args) => eventRaised = true;

		// Act
		sliderOption.OnSet ();

		// Assert
		Assert.True (eventRaised);
	}

	[Fact]
	public void OnUnSet_Should_Raise_UnSetEvent ()
	{
		// Arrange
		var sliderOption = new SliderOption<int> ();
		var eventRaised = false;
		sliderOption.UnSet += (sender, args) => eventRaised = true;

		// Act
		sliderOption.OnUnSet ();

		// Assert
		Assert.True (eventRaised);
	}

	[Fact]
	public void OnChanged_Should_Raise_ChangedEvent ()
	{
		// Arrange
		var sliderOption = new SliderOption<int> ();
		var eventRaised = false;
		sliderOption.Changed += (sender, args) => eventRaised = true;

		// Act
		sliderOption.OnChanged (true);

		// Assert
		Assert.True (eventRaised);
	}
}

public class SliderEventArgsTests {
	[Fact]
	public void Constructor_Sets_Options ()
	{
		// Arrange
		var options = new Dictionary<int, SliderOption<int>> ();

		// Act
		var sliderEventArgs = new SliderEventArgs<int> (options);

		// Assert
		Assert.Equal (options, sliderEventArgs.Options);
	}

	[Fact]
	public void Constructor_Sets_Focused ()
	{
		// Arrange
		var options = new Dictionary<int, SliderOption<int>> ();
		var focused = 42;

		// Act
		var sliderEventArgs = new SliderEventArgs<int> (options, focused);

		// Assert
		Assert.Equal (focused, sliderEventArgs.Focused);
	}

	[Fact]
	public void Constructor_Sets_Cancel_Default_To_False ()
	{
		// Arrange
		var options = new Dictionary<int, SliderOption<int>> ();
		var focused = 42;

		// Act
		var sliderEventArgs = new SliderEventArgs<int> (options, focused);

		// Assert
		Assert.False (sliderEventArgs.Cancel);
	}
}


public class SliderTests {
	[Fact]
	public void Constructor_Default ()
	{
		// Arrange & Act
		var slider = new Slider<int> ();

		// Assert
		Assert.NotNull (slider);
		Assert.NotNull (slider.Options);
		Assert.Empty (slider.Options);
		Assert.Equal (Orientation.Horizontal, slider.Orientation);
		Assert.False (slider.AllowEmpty);
		Assert.True (slider.ShowLegends);
		Assert.False (slider.ShowSpacing);
		Assert.Equal (SliderType.Single, slider.Type);
		Assert.Equal (0, slider.InnerSpacing);
		Assert.False (slider.AutoSize);
		Assert.Equal (0, slider.FocusedOption);
	}

	[Fact]
	public void Constructor_With_Options ()
	{
		// Arrange
		var options = new List<int> { 1, 2, 3 };

		// Act
		var slider = new Slider<int> (options);

		// Assert
		Assert.NotNull (slider);
		Assert.NotNull (slider.Options);
		Assert.Equal (options.Count, slider.Options.Count);
	}

	[Fact]
	public void OnOptionsChanged_Event_Raised ()
	{
		// Arrange
		var slider = new Slider<int> ();
		bool eventRaised = false;
		slider.OptionsChanged += (sender, args) => eventRaised = true;

		// Act
		slider.OnOptionsChanged ();

		// Assert
		Assert.True (eventRaised);
	}

	[Fact]
	public void OnOptionFocused_Event_Raised ()
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3 });
		bool eventRaised = false;
		slider.OptionFocused += (sender, args) => eventRaised = true;
		int newFocusedOption = 1;
		var args = new SliderEventArgs<int> (new Dictionary<int, SliderOption<int>> (), newFocusedOption);

		// Act
		slider.OnOptionFocused (newFocusedOption, args);

		// Assert
		Assert.True (eventRaised);
	}

	[Fact]
	public void OnOptionFocused_Event_Cancelled ()
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3 });
		bool eventRaised = false;
		bool cancel = false;
		slider.OptionFocused += (sender, args) => eventRaised = true;
		int newFocusedOption = 1;

		// Create args with cancel set to false
		cancel = false;
		var args = new SliderEventArgs<int> (new Dictionary<int, SliderOption<int>> (), newFocusedOption) {
			Cancel = cancel
		};
		Assert.Equal (0, slider.FocusedOption);

		// Act
		slider.OnOptionFocused (newFocusedOption, args);

		// Assert
		Assert.True (eventRaised); // Event should be raised
		Assert.Equal (newFocusedOption, slider.FocusedOption); // Focused option should change

		// Create args with cancel set to true
		cancel = true;
		args = new SliderEventArgs<int> (new Dictionary<int, SliderOption<int>> (), newFocusedOption) {
			Cancel = cancel
		};

		// Act
		slider.OnOptionFocused (2, args);

		// Assert
		Assert.True (eventRaised); // Event should be raised
		Assert.Equal (newFocusedOption, slider.FocusedOption); // Focused option should not change
	}

	[Theory]
	[InlineData (0, 0, 0)]
	[InlineData (1, 3, 0)]
	[InlineData (3, 9, 0)]
	public void TryGetPositionByOption_ValidOptionHorizontal_Success (int option, int expectedX, int expectedY)
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3, 4 });
		slider.AutoSize = true; // Set auto size to true to enable testing
		slider.InnerSpacing = 2;
		// 0123456789
		// 1--2--3--4

		// Act
		bool result = slider.TryGetPositionByOption (option, out var position);

		// Assert
		Assert.True (result);
		Assert.Equal (expectedX, position.x);
		Assert.Equal (expectedY, position.y);
	}

	[Theory]
	[InlineData (0, 0, 0)]
	[InlineData (1, 0, 3)]
	[InlineData (3, 0, 9)]
	public void TryGetPositionByOption_ValidOptionVertical_Success (int option, int expectedX, int expectedY)
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3, 4 });
		slider.Orientation = Orientation.Vertical;
		slider.AutoSize = true; // Set auto size to true to enable testing
		slider.InnerSpacing = 2;

		// Act
		bool result = slider.TryGetPositionByOption (option, out var position);

		// Assert
		Assert.True (result);
		Assert.Equal (expectedX, position.x);
		Assert.Equal (expectedY, position.y);
	}

	[Fact]
	public void TryGetPositionByOption_InvalidOption_Failure ()
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3 });
		int option = -1;
		var expectedPosition = (-1, -1);

		// Act
		bool result = slider.TryGetPositionByOption (option, out var position);

		// Assert
		Assert.False (result);
		Assert.Equal (expectedPosition, position);
	}

	[Theory]
	[InlineData (0, 0, 0, 1)]
	[InlineData (3, 0, 0, 2)]
	[InlineData (9, 0, 0, 4)]
	[InlineData (0, 0, 1, 1)]
	[InlineData (3, 0, 1, 2)]
	[InlineData (9, 0, 1, 4)]
	public void TryGetOptionByPosition_ValidPositionHorizontal_Success (int x, int y, int threshold, int expectedData)
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3, 4 });
		slider.AutoSize = true; // Set auto size to true to enable testing
		slider.InnerSpacing = 2;
		// 0123456789
		// 1--2--3--4

		// Arrange

		// Act
		bool result = slider.TryGetOptionByPosition (x, y, threshold, out int option);

		// Assert
		Assert.True (result);
		Assert.Equal (expectedData, slider.Options [option].Data);
	}

	[Theory]
	[InlineData (0, 0, 0, 1)]
	[InlineData (0, 3, 0, 2)]
	[InlineData (0, 9, 0, 4)]
	[InlineData (0, 0, 1, 1)]
	[InlineData (0, 3, 1, 2)]
	[InlineData (0, 9, 1, 4)]
	public void TryGetOptionByPosition_ValidPositionVertical_Success (int x, int y, int threshold, int expectedData)
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3, 4 });
		slider.Orientation = Orientation.Vertical;
		slider.AutoSize = true; // Set auto size to true to enable testing
		slider.InnerSpacing = 2;
		// 0 1
		// 1 |
		// 2 |
		// 3 2
		// 4 |
		// 5 |
		// 6 3
		// 7 |
		// 8 |
		// 9 4
		slider.CalcSpacingConfig ();

		// Arrange

		// Act
		bool result = slider.TryGetOptionByPosition (x, y, threshold, out int option);

		// Assert
		Assert.True (result);
		Assert.Equal (expectedData, slider.Options [option].Data);
	}


	[Fact]
	public void TryGetOptionByPosition_InvalidPosition_Failure ()
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3 });
		int x = 10;
		int y = 10;
		int threshold = 2;
		int expectedOption = -1;

		// Act
		bool result = slider.TryGetOptionByPosition (x, y, threshold, out int option);

		// Assert
		Assert.False (result);
		Assert.Equal (expectedOption, option);
	}

	[Fact]
	public void MovePlus_Should_MoveFocusRight_When_OptionIsAvailable ()
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3, 4 });
		slider.AutoSize = true;

		// Act
		bool result = slider.MovePlus ();

		// Assert
		Assert.True (result);
		Assert.Equal (1, slider.FocusedOption);
	}

	[Fact]
	public void MovePlus_Should_NotMoveFocusRight_When_AtEnd ()
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3, 4 });
		slider.AutoSize = true;
		slider.FocusedOption = 3;

		// Act
		bool result = slider.MovePlus ();

		// Assert
		Assert.False (result);
		Assert.Equal (3, slider.FocusedOption);
	}

	// Add similar tests for other methods like MoveMinus, MoveStart, MoveEnd, Set, etc.

	[Fact]
	public void Set_Should_SetFocusedOption ()
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3, 4 });
		slider.AutoSize = true;

		// Act
		slider.FocusedOption = 2;
		bool result = slider.Set ();

		// Assert
		Assert.True (result);
		Assert.Equal (2, slider.FocusedOption);
		Assert.Single (slider.GetSetOptions ());
	}

	[Fact]
	public void Set_Should_Not_UnSetFocusedOption_When_EmptyNotAllowed ()
	{
		// Arrange
		var slider = new Slider<int> (new List<int> { 1, 2, 3, 4 }) {
			AllowEmpty = false
		};
		slider.AutoSize = true;

		Assert.NotEmpty (slider.GetSetOptions ());

		// Act
		bool result = slider.UnSetOption (slider.FocusedOption);

		// Assert
		Assert.False (result);
		Assert.NotEmpty (slider.GetSetOptions ());
	}

	// Add more tests for different scenarios and edge cases.
}


