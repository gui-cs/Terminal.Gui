using System;
using System.Buffers.Binary;
using System.Globalization;
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
	public void Color_IsClosestToNamedColor_ReturnsExpectedValue ()
	{
		// Arrange
		var color1 = new Color (ColorName.Red);
		var color2 = new Color (197, 15, 31); // Red in RGB

		Assert.True ( color1.IsClosestToNamedColor ( ColorName.Red ) );

		Assert.True ( color2.IsClosestToNamedColor ( ColorName.Red ) );
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

	[Theory]
	[MemberData ( nameof ( ColorTestsTheoryDataGenerators.FindClosestColor_ReturnsClosestColor ), MemberType = typeof ( ColorTestsTheoryDataGenerators ) )]
	public void FindClosestColor_ReturnsClosestColor ( Color inputColor, ColorName expectedColorName )
	{
		var actualColorName = Color.FindClosestColor ( inputColor );

		Assert.Equal ( expectedColorName, actualColorName );
	}

#nullable enable
	[Theory]
	[MemberData ( nameof ( ColorTestsTheoryDataGenerators.TryParse_string_Returns_True_For_Valid_Inputs ), MemberType = typeof ( ColorTestsTheoryDataGenerators ) )]
	public void TryParse_string_Returns_True_For_Valid_Inputs ( string? input, int expectedColorArgb )
	{
		bool tryParseStatus = Color.TryParse ( input, out Color? color );
		Assert.True ( tryParseStatus );
		Assert.NotNull ( color );
		Assert.IsType<Color> ( color );
		Assert.Equal ( expectedColorArgb, color.Value.Rgba );
	}

	[Theory]
    [CombinatorialData]
	public void Rgba_Returns_Expected_Value ( [CombinatorialValues(0,255)]byte a, [CombinatorialRange(0,255,51)]byte r, [CombinatorialRange(0,153,51)]byte g, [CombinatorialRange(0,128,32)]byte b )
	{
		Color color = new ( r, g, b, a );
		// Color.Rgba is expected to be a signed int32 in little endian order (a,b,g,r)
		ReadOnlySpan<byte> littleEndianBytes = [b, g, r, a];
		int expectedRgba = BitConverter.ToInt32 ( littleEndianBytes );
		Assert.Equal ( expectedRgba, color.Rgba );
	}

	[Theory]
    [CombinatorialData]
	public void Argb_Returns_Expected_Value ( [CombinatorialValues(0,255)]byte a, [CombinatorialRange(0,255,51)]byte r, [CombinatorialRange(0,153,51)]byte g, [CombinatorialRange(0,128,32)]byte b )
	{
		Color color = new ( r, g, b, a );
		// Color.Rgba is expected to be a signed int32 in little endian order (a,b,g,r)
		ReadOnlySpan<byte> littleEndianBytes = [b, g, r, a];
		uint expectedArgb = BitConverter.ToUInt32( littleEndianBytes );
		Assert.Equal ( expectedArgb, color.Argb );
	}

	[Theory]
	[MemberData ( nameof ( ColorTestsTheoryDataGenerators.TryParse_string_Returns_False_For_Invalid_Inputs ), MemberType = typeof ( ColorTestsTheoryDataGenerators ) )]
	public void TryParse_string_Returns_False_For_Invalid_Inputs ( string? input )
	{
		bool tryParseStatus = Color.TryParse ( input, out Color? color );
		Assert.False ( tryParseStatus );
		Assert.Null ( color );
	}

	[Theory]
    [CombinatorialData]
	public void ToString_WithInvariantCultureAndNullString_IsSameAsParameterless ([CombinatorialValues(0,64,128,255)]byte r, [CombinatorialValues(0,64,128,255)]byte g, [CombinatorialValues(0,64,128,255)]byte b )
	{
		string expected = $"#{r:X2}{g:X2}{b:X2}";
		Color testColor = new ( r, g, b );

		string testStringWithExplicitInvariantCulture = testColor.ToString ( null, CultureInfo.InvariantCulture );
		Assert.Equal ( expected, testStringWithExplicitInvariantCulture );


		string parameterlessToStringValue = testColor.ToString ( );
		Assert.Equal ( parameterlessToStringValue, testStringWithExplicitInvariantCulture );
	}

	[Theory]
	[CombinatorialData]
	public void Parse_And_ToString_RoundTrip_For_Known_FormatStrings ([CombinatorialValues(null,"","g","G","d","D")] string? formatString,[CombinatorialValues(0,64,255)] byte r,[CombinatorialValues(0,64,255)] byte g,[CombinatorialValues(0,64,255)] byte b  )
	{
		Color constructedColor = new ( r, g, b, 255 );

		// Pre-conditions for the rest of the test to be valid
		Assert.Equal ( r, constructedColor.R );
		Assert.Equal ( g, constructedColor.G );
		Assert.Equal ( b, constructedColor.B );
		Assert.Equal ( 255, constructedColor.A);

		//Get the ToString result with the specified format string
		string formattedColorString = constructedColor.ToString ( formatString );

		// Now parse that string
		Color parsedColor = Color.Parse ( formattedColorString );

		// They should have identical underlying values
		Assert.Equal ( constructedColor.Argb, parsedColor.Argb );
	}
}

public static class ColorTestsTheoryDataGenerators {
	public static TheoryData<string?> TryParse_string_Returns_False_For_Invalid_Inputs ( )
	{
		TheoryData<string?> values = [
			null
		];
		for ( char i = char.MinValue; i < 255; i++ ) {
			if ( !char.IsAsciiDigit ( i ) ) {
				values.Add ( $"rgb({i},{i},{i})" );
				values.Add ( $"rgba({i},{i},{i})" );
			}
			if ( !char.IsAsciiHexDigit ( i ) ) {
				values.Add ( $"#{i}{i}{i}{i}{i}{i}" );
				values.Add ( $"#{i}{i}{i}{i}{i}{i}{i}{i}" );
			}
		}
		//Also throw in a couple of just badly formatted strings
		values.Add ( "rgbaa(1,2,3,4))" );
		values.Add ( "#rgb(1,FF,3,4)" );
		values.Add ( "rgb(1,FF,3,4" );
		values.Add ( "rgb(1,2,3,4.5))" );
		return values;
	}
	public static TheoryData<string?, int> TryParse_string_Returns_True_For_Valid_Inputs ( )
	{
		TheoryData<string?, int> values = [];
		for ( byte i = 16; i < 224; i += 32 ) {
			// Using this so the span only has to be written one way.
			int expectedRgb = BinaryPrimitives.ReadInt32LittleEndian ( [(byte)( i + 16 ), i, (byte)( i - 16 ), 255] );
			int expectedRgba = BinaryPrimitives.ReadInt32LittleEndian ( [(byte)( i + 16 ), i, (byte)( i - 16 ), i] );
			values.Add ( $"rgb({i - 16:D},{i:D},{i + 16:D})", expectedRgb );
			values.Add ( $"rgb({i - 16:D},{i:D},{i + 16:D},{i:D})", expectedRgba );
			values.Add ( $"rgb({i - 16:D},{i:D},{i + 16:D})", expectedRgb );
			values.Add ( $"rgba({i - 16:D},{i:D},{i + 16:D},{i:D})", expectedRgba );
			values.Add ( $"#{i - 16:X2}{i:X2}{i + 16:X2}", expectedRgb );
			values.Add ( $"#{i:X2}{i - 16:X2}{i:X2}{i + 16:X2}", expectedRgba );
		}
		for ( byte i = 1; i < 0xE; i++ ) {
			values.Add ( $"#{i - 1:X0}{i:X0}{i + 1:X0}", BinaryPrimitives.ReadInt32LittleEndian (
			[
				// Have to stick the least significant 4 bits in the most significant 4 bits to duplicate the hex values
				// Breaking this out just so it's easier to see.
				(byte)( i + 1 | i + 1 << 4 ),
				(byte)( i | i << 4 ),
				(byte)( i - 1 | i - 1 << 4 ),
				255
			] ) );
			values.Add ( $"#{i:X0}{i - 1:X0}{i:X0}{i + 1:X0}", BinaryPrimitives.ReadInt32LittleEndian (
			[
				(byte)( i + 1 | i + 1 << 4 ),
				(byte)( i | i << 4 ),
				(byte)( i - 1 | i - 1 << 4 ),
				(byte)( i | i << 4 )
			] ) );
		}
		return values;
	}
	public static TheoryData<Color, ColorName> FindClosestColor_ReturnsClosestColor ( )
	{
		TheoryData<Color, ColorName> data = [];
		data.Add ( new Color ( 0, 0, 0 ), ColorName.Black );
		data.Add ( new Color ( 255, 255, 255 ), ColorName.White );
		data.Add ( new Color ( 5, 100, 255 ), ColorName.BrightBlue );
		data.Add ( new Color ( 0, 255, 0 ), ColorName.BrightGreen );
		data.Add ( new Color ( 255, 70, 8 ), ColorName.BrightRed );
		data.Add ( new Color ( 0, 128, 128 ), ColorName.Cyan );
		data.Add ( new Color ( 128, 64, 32 ), ColorName.Yellow );
		return data;
	}
}
#nullable restore