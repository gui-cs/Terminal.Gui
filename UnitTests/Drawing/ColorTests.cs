using Terminal.Gui;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;
using static Unix.Terminal.Curses;
namespace Terminal.Gui.DrawingTests;

public class ColorTests {
	[Fact]
	public void ColorNames_Has16Elements ()
	{
		Assert.Equal (16, Enum.GetValues (typeof (ColorNames)).Length);
	}

	[Fact]
	public void ColorNames_HaveCorrectOrdinals ()
	{
		Assert.Equal (0, (int)ColorNames.Black);
		Assert.Equal (1, (int)ColorNames.Blue);
		Assert.Equal (2, (int)ColorNames.Green);
		Assert.Equal (3, (int)ColorNames.Cyan);
		Assert.Equal (4, (int)ColorNames.Red);
		Assert.Equal (5, (int)ColorNames.Magenta);
		Assert.Equal (6, (int)ColorNames.Brown);
		Assert.Equal (7, (int)ColorNames.Gray);
		Assert.Equal (8, (int)ColorNames.DarkGray);
		Assert.Equal (9, (int)ColorNames.BrightBlue);
		Assert.Equal (10, (int)ColorNames.BrightGreen);
		Assert.Equal (11, (int)ColorNames.BrightCyan);
		Assert.Equal (12, (int)ColorNames.BrightRed);
		Assert.Equal (13, (int)ColorNames.BrightMagenta);
		Assert.Equal (14, (int)ColorNames.BrightYellow);
		Assert.Equal (15, (int)ColorNames.White);
	}

	[Fact]
	public void Color_Constructor_WithRGBValues ()
	{
		// Arrange
		int expectedR = 255;
		int expectedG = 0;
		int expectedB = 128;

		// Act
		var color = new Color (expectedR, expectedG, expectedB);

		// Assert
		Assert.Equal (expectedR, color.R);
		Assert.Equal (expectedG, color.G);
		Assert.Equal (expectedB, color.B);
		Assert.Equal (0xFF, color.A); // Alpha should be FF by default
	}

	[Fact]
	public void Color_Constructor_WithAlphaAndRGBValues ()
	{
		// Arrange
		int expectedA = 128;
		int expectedR = 255;
		int expectedG = 0;
		int expectedB = 128;

		// Act
		var color = new Color (expectedA, expectedR, expectedG, expectedB);

		// Assert
		Assert.Equal (expectedA, color.A);
		Assert.Equal (expectedR, color.R);
		Assert.Equal (expectedG, color.G);
		Assert.Equal (expectedB, color.B);
	}

	[Fact]
	public void Color_Constructor_WithArgbValue ()
	{
		// Arrange
		int expectedArgb = unchecked((int)0xFF804040); // Alpha: 255, R: 128, G: 64, B: 64

		// Act
		var color = new Color (expectedArgb);

		// Assert
		Assert.Equal (255, color.A);
		Assert.Equal (128, color.R);
		Assert.Equal (64, color.G);
		Assert.Equal (64, color.B);
	}

	[Fact]
	public void Color_Constructor_WithColorName ()
	{
		// Arrange
		ColorNames colorName = ColorNames.Blue;
		var expectedColor = new Color (0, 55, 218); // Blue

		// Act
		var color = new Color (colorName);

		// Assert
		Assert.Equal (expectedColor, color);
	}

	[Fact]
	public void Color_ToString_WithNamedColor ()
	{
		// Arrange
		Color color = new Color (0, 55, 218); // Blue

		// Act
		string colorString = color.ToString ();

		// Assert
		Assert.Equal ("Blue", colorString);
	}

	[Fact]
	public void Color_ToString_WithRGBColor ()
	{
		// Arrange
		Color color = new Color (1, 64, 32); // Custom RGB color

		// Act
		string colorString = color.ToString ();

		// Assert
		Assert.Equal ("#014020", colorString);
	}

	[Fact]
	public void Color_ImplicitOperator_FromInt ()
	{
		// Arrange
		int argb = unchecked((int)0xFF804020); // Alpha: 255, R: 128, G: 64, B: 32
		var expectedColor = new Color (255, 128, 64, 32);

		// Act
		Color color = argb;

		// Assert
		Assert.Equal (expectedColor, color);
	}

	[Fact]
	public void Color_ExplicitOperator_ToInt ()
	{
		// Arrange
		var color = new Color (255, 128, 64, 32);
		int expectedArgb = unchecked((int)0xFF804020); // Alpha: 255, R: 128, G: 64, B: 32

		// Act
		int argb = (int)color;

		// Assert
		Assert.Equal (expectedArgb, argb);
	}


	[Fact]
	public void Color_ImplicitOperator_FromColorNames ()
	{
		// Arrange
		ColorNames colorName = ColorNames.Blue;
		var expectedColor = new Color (0, 55, 218); // Blue

		// Act
		Color color = (Color)colorName;

		// Assert
		Assert.Equal (expectedColor, color);
	}

	[Fact]
	public void Color_ExplicitOperator_ToColorNames ()
	{
		// Arrange
		var color = new Color (0, 0, 0x80); // Blue
		ColorNames expectedColorName = ColorNames.Blue;

		// Act
		ColorNames colorName = (ColorNames)color;

		// Assert
		Assert.Equal (expectedColorName, colorName);
	}



	[Fact]
	public void Color_EqualityOperator_WithColorAndColor ()
	{
		// Arrange
		var color1 = new Color (255, 128, 64, 32);
		var color2 = new Color (255, 128, 64, 32);

		// Act & Assert
		Assert.True (color1 == color2);
		Assert.False (color1 != color2);
	}

	[Fact]
	public void Color_InequalityOperator_WithColorAndColor ()
	{
		// Arrange
		var color1 = new Color (255, 128, 64, 32);
		var color2 = new Color (128, 64, 32, 16);

		// Act & Assert
		Assert.False (color1 == color2);
		Assert.True (color1 != color2);
	}

	[Fact]
	public void Color_EqualityOperator_WithColorNamesAndColor ()
	{
		// Arrange
		var color1 = new Color (ColorNames.Red);
		var color2 = new Color (197, 15, 31); // Red in RGB

		// Act & Assert
		Assert.True (ColorNames.Red == color1);
		Assert.False (ColorNames.Red != color1);

		Assert.True (color1 == ColorNames.Red);
		Assert.False (color1 != ColorNames.Red);

		Assert.True (color2 == ColorNames.Red);
		Assert.False (color2 != ColorNames.Red);
	}

	[Fact]
	public void Color_InequalityOperator_WithColorNamesAndColor ()
	{
		// Arrange
		var color1 = new Color (ColorNames.Red);
		var color2 = new Color (58, 150, 221); // Cyan in RGB

		// Act & Assert
		Assert.False (ColorNames.Red == color2);
		Assert.True (ColorNames.Red != color2);

		Assert.False (color2 == ColorNames.Red);
		Assert.True (color2 != ColorNames.Red);
	}

	[Fact]
	public void Color_FromColorName_ConvertsColorNamesToColor ()
	{
		// Arrange
		var colorName = ColorNames.Red;
		var expectedColor = new Color (197, 15, 31); // Red in RGB

		// Act
		var convertedColor = Color.FromColorName (colorName);

		// Assert
		Assert.Equal (expectedColor, convertedColor);
	}

	[Fact]
	public void Color_FromColorName_ConvertsColorNameIdToColor ()
	{
		// Arrange
		int colorNameId = (int)ColorNames.Green;
		var expectedColor = new Color (19, 161, 14); // Green in RGB

		// Act
		var convertedColor = Color.FromColorName (colorNameId);

		// Assert
		Assert.Equal (expectedColor, convertedColor);
	}

	[Fact]
	public void Color_ColorName_Get_ReturnsClosestColorName ()
	{
		// Arrange
		var color = new Color (128, 64, 40); // Custom RGB color, closest to Brown 
		var expectedColorName = ColorNames.Brown;

		// Act
		var colorName = color.ColorName;

		// Assert
		Assert.Equal (expectedColorName, colorName);
	}

	[Fact]
	public void FindClosestColor_ReturnsClosestColor ()
	{
		// Test cases with RGB values and expected closest color names
		var testCases = new []
		{
			(new Color(0, 0, 0), ColorNames.Black),
			(new Color(255, 255, 255), ColorNames.White),
			(new Color(5, 100, 255), ColorNames.BrightBlue),
			(new Color(0, 255, 0), ColorNames.BrightGreen),
			(new Color(255, 70, 8), ColorNames.BrightRed),
			(new Color(0, 128, 128), ColorNames.Cyan),
			(new Color(128, 64, 32), ColorNames.Brown),
		};

		foreach (var testCase in testCases) {
			var inputColor = testCase.Item1;
			var expectedColorName = testCase.Item2;

			var actualColorName = Color.FindClosestColor (inputColor);

			Assert.Equal (expectedColorName, actualColorName);
		}
	}

	[Fact]
	public void Color_ColorName_Set_SetsColorBasedOnColorName ()
	{
		// Arrange
		var color = new Color (0, 0, 0); // Black
		var expectedColor = new Color (ColorNames.Magenta);

		// Act
		color.ColorName = ColorNames.Magenta;

		// Assert
		Assert.Equal (expectedColor, color);
	}
}


