using System;
using System.Linq;
using Xunit;

namespace Terminal.Gui.DrawingTests;

public class ColorTests {
	[Fact]
	public void Color_Is_Value_Type () =>
		// prove that Color is a value type
		Assert.True (typeof (Color).IsValueType);

	[Fact]
	public void TestAllColors ()
	{
		var colorNames = Enum.GetValues (typeof (ColorName)).Cast<int> ().Distinct ().ToList ();
		var attrs = new Attribute [colorNames.Count];

		var idx = 0;
		foreach (ColorName bg in colorNames) {
			attrs [idx] = new Attribute (bg, colorNames.Count - 1 - bg);
			idx++;
		}
		Assert.Equal (16, attrs.Length);
		Assert.Equal (new Attribute (Color.Black, Color.White), attrs [0]);
		Assert.Equal (new Attribute (Color.Blue, Color.BrightYellow), attrs [1]);
		Assert.Equal (new Attribute (Color.Green, Color.BrightMagenta), attrs [2]);
		Assert.Equal (new Attribute (Color.Cyan, Color.BrightRed), attrs [3]);
		Assert.Equal (new Attribute (Color.Red, Color.BrightCyan), attrs [4]);
		Assert.Equal (new Attribute (Color.Magenta, Color.BrightGreen), attrs [5]);
		Assert.Equal (new Attribute (Color.Yellow, Color.BrightBlue), attrs [6]);
		Assert.Equal (new Attribute (Color.Gray, Color.DarkGray), attrs [7]);
		Assert.Equal (new Attribute (Color.DarkGray, Color.Gray), attrs [8]);
		Assert.Equal (new Attribute (Color.BrightBlue, Color.Yellow), attrs [9]);
		Assert.Equal (new Attribute (Color.BrightGreen, Color.Magenta), attrs [10]);
		Assert.Equal (new Attribute (Color.BrightCyan, Color.Red), attrs [11]);
		Assert.Equal (new Attribute (Color.BrightRed, Color.Cyan), attrs [12]);
		Assert.Equal (new Attribute (Color.BrightMagenta, Color.Green), attrs [13]);
		Assert.Equal (new Attribute (Color.BrightYellow, Color.Blue), attrs [14]);
		Assert.Equal (new Attribute (Color.White, Color.Black), attrs [^1]);
	}

	[Fact]
	public void ColorNames_HasOnly16DistinctElements () => Assert.Equal (16, Enum.GetValues (typeof (ColorName)).Cast<int> ().Distinct ().Count ());

	[Fact]
	public void ColorNames_HaveCorrectOrdinals ()
	{
		Assert.Equal (0, (int)ColorName.Black);
		Assert.Equal (1, (int)ColorName.Blue);
		Assert.Equal (2, (int)ColorName.Green);
		Assert.Equal (3, (int)ColorName.Cyan);
		Assert.Equal (4, (int)ColorName.Red);
		Assert.Equal (5, (int)ColorName.Magenta);
		Assert.Equal (6, (int)ColorName.Yellow);
		Assert.Equal (7, (int)ColorName.Gray);
		Assert.Equal (8, (int)ColorName.DarkGray);
		Assert.Equal (9, (int)ColorName.BrightBlue);
		Assert.Equal (10, (int)ColorName.BrightGreen);
		Assert.Equal (11, (int)ColorName.BrightCyan);
		Assert.Equal (12, (int)ColorName.BrightRed);
		Assert.Equal (13, (int)ColorName.BrightMagenta);
		Assert.Equal (14, (int)ColorName.BrightYellow);
		Assert.Equal (15, (int)ColorName.White);
	}

	[Fact]
	public void Color_Constructor_WithRGBValues ()
	{
		// Arrange
		var expectedR = 255;
		var expectedG = 0;
		var expectedB = 128;

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
		var expectedA = 128;
		var expectedR = 255;
		var expectedG = 0;
		var expectedB = 128;

		// Act
		var color = new Color (expectedR, expectedG, expectedB, expectedA);

		// Assert
		Assert.Equal (expectedR, color.R);
		Assert.Equal (expectedG, color.G);
		Assert.Equal (expectedB, color.B);
		Assert.Equal (expectedA, color.A);
	}

	[Fact]
	public void Color_Constructor_WithRgbaValue ()
	{
		// Arrange
		var expectedRgba = unchecked((int)0xFF804040); // R: 128, G: 64, B: 64, Alpha: 255

		// Act
		var color = new Color (expectedRgba);

		// Assert
		Assert.Equal (128, color.R);
		Assert.Equal (64, color.G);
		Assert.Equal (64, color.B);
		Assert.Equal (255, color.A);
	}

	[Fact]
	public void Color_Constructor_WithColorName ()
	{
		// Arrange
		var colorName = ColorName.Blue;
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
		var color = new Color (0, 55, 218); // Blue

		// Act
		var colorString = color.ToString ();

		// Assert
		Assert.Equal ("Blue", colorString);
	}

	[Fact]
	public void Color_ToString_WithRGBColor ()
	{
		// Arrange
		var color = new Color (1, 64, 32); // Custom RGB color

		// Act
		var colorString = color.ToString ();

		// Assert
		Assert.Equal ("#014020", colorString);
	}

	[Fact]
	public void Color_ImplicitOperator_FromInt ()
	{
		// Arrange
		var Rgba = unchecked((int)0xFF804020); // R: 128, G: 64, B: 32, Alpha: 255
		var expectedColor = new Color (128, 64, 32);

		// Act
		Color color = Rgba;

		// Assert
		Assert.Equal (expectedColor, color);
	}

	[Fact]
	public void Color_ExplicitOperator_ToInt ()
	{
		// Arrange
		var color = new Color (128, 64, 32);
		var expectedRgba = unchecked((int)0xFF804020); // R: 128, G: 64, B: 32, Alpha: 255

		// Act
		var Rgba = (int)color;

		// Assert
		Assert.Equal (expectedRgba, Rgba);
	}


	[Fact]
	public void Color_ImplicitOperator_FromColorNames ()
	{
		// Arrange
		var colorName = ColorName.Blue;
		var expectedColor = new Color (0, 55, 218); // Blue

		// Act
		var color = new Color (colorName);

		// Assert
		Assert.Equal (expectedColor, color);
	}

	[Fact]
	public void Color_ExplicitOperator_ToColorNames ()
	{
		// Arrange
		var color = new Color (0, 0, 0x80); // Blue
		var expectedColorName = ColorName.Blue;

		// Act
		var colorName = (ColorName)color;

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
		var color1 = new Color (ColorName.Red);
		var color2 = new Color (197, 15, 31); // Red in RGB

		// Act & Assert
		Assert.True (ColorName.Red == color1);
		Assert.False (ColorName.Red != color1);

		Assert.True (color1 == ColorName.Red);
		Assert.False (color1 != ColorName.Red);

		Assert.True (color2 == ColorName.Red);
		Assert.False (color2 != ColorName.Red);
	}

	[Fact]
	public void Color_InequalityOperator_WithColorNamesAndColor ()
	{
		// Arrange
		var color1 = new Color (ColorName.Red);
		var color2 = new Color (58, 150, 221); // Cyan in RGB

		// Act & Assert
		Assert.False (ColorName.Red == color2);
		Assert.True (ColorName.Red != color2);

		Assert.False (color2 == ColorName.Red);
		Assert.True (color2 != ColorName.Red);
	}

	[Fact]
	public void Color_FromColorName_ConvertsColorNamesToColor ()
	{
		// Arrange
		var colorName = ColorName.Red;
		var expectedColor = new Color (197, 15, 31); // Red in RGB

		// Act
		var convertedColor = new Color (colorName);

		// Assert
		Assert.Equal (expectedColor, convertedColor);
	}

	[Fact]
	public void Color_ColorName_Get_ReturnsClosestColorName ()
	{
		// Arrange
		var color = new Color (128, 64, 40); // Custom RGB color, closest to Yellow 
		var expectedColorName = ColorName.Yellow;

		// Act
		var colorName = color.ColorName;

		// Assert
		Assert.Equal (expectedColorName, colorName);
	}

	[Fact]
	public void FindClosestColor_ReturnsClosestColor ()
	{
		// Test cases with RGB values and expected closest color names
		var testCases = new [] {
			(new Color (0, 0, 0), ColorName.Black),
			(new Color (255, 255, 255), ColorName.White),
			(new Color (5, 100, 255), ColorName.BrightBlue),
			(new Color (0, 255, 0), ColorName.BrightGreen),
			(new Color (255, 70, 8), ColorName.BrightRed),
			(new Color (0, 128, 128), ColorName.Cyan),
			(new Color (128, 64, 32), ColorName.Yellow)
		};

		foreach (var testCase in testCases) {
			var inputColor = testCase.Item1;
			var expectedColorName = testCase.Item2;

			var actualColorName = Color.FindClosestColor (inputColor);

			Assert.Equal (expectedColorName, actualColorName);
		}
	}
}