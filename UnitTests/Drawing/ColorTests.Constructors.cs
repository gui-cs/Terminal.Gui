using System;
using Xunit;
namespace Terminal.Gui.DrawingTests;

public partial class ColorTests {
	[Theory]
    [CombinatorialData]
	public void Color_Constructor_WithRGBValues_AllValuesCorrect ( [CombinatorialValues(0,1,254)]byte r, [CombinatorialValues(0,1,253)]byte g, [CombinatorialValues(0,1,252)]byte b )
	{
		var color = new Color ( r, g, b );

		ReadOnlySpan<byte> bytes = [b, g, r, 255];
		int expectedRgba = BitConverter.ToInt32 ( bytes );
		uint expectedArgb = BitConverter.ToUInt32 ( bytes );

		Assert.Multiple (
			( ) => Assert.Equal ( r, color.R ),
			( ) => Assert.Equal ( g, color.G ),
			( ) => Assert.Equal ( b, color.B ),
			( ) => Assert.Equal ( byte.MaxValue, color.A ),
			( ) => Assert.Equal ( expectedRgba, color.Rgba ),
			( ) => Assert.Equal ( expectedArgb, color.Argb )
		);
	}

	[Theory]
	[CombinatorialData]
	public void Color_Constructor_WithRGBAValues_AllValuesCorrect ( [CombinatorialValues ( 0, 1, 254 )] byte r, [CombinatorialValues ( 0, 1, 253 )] byte g, [CombinatorialValues ( 0, 1, 252 )] byte b, [CombinatorialValues ( 0, 1, 251 )] byte a )
	{
		var color = new Color ( r, g, b, a );

		ReadOnlySpan<byte> bytes = [b, g, r, a];
		int expectedRgba = BitConverter.ToInt32 ( bytes );
		uint expectedArgb = BitConverter.ToUInt32 ( bytes );

		Assert.Multiple (
			( ) => Assert.Equal ( r, color.R ),
			( ) => Assert.Equal ( g, color.G ),
			( ) => Assert.Equal ( b, color.B ),
			( ) => Assert.Equal ( a, color.A ),
			( ) => Assert.Equal ( expectedRgba, color.Rgba ),
			( ) => Assert.Equal ( expectedArgb, color.Argb )
		);
	}

	[Fact]
	public void Color_Constructor_WithAlphaAndRGBValues ( )
	{
		// Arrange
		var expectedA = 128;
		var expectedR = 255;
		var expectedG = 0;
		var expectedB = 128;

		// Act
		var color = new Color ( expectedR, expectedG, expectedB, expectedA );

		// Assert
		Assert.Equal ( expectedR, color.R );
		Assert.Equal ( expectedG, color.G );
		Assert.Equal ( expectedB, color.B );
		Assert.Equal ( expectedA, color.A );
	}
	[Fact]
	public void Color_Constructor_WithRgbaValue ( )
	{
		// Arrange
		var expectedRgba = unchecked( (int)0xFF804040 ); // R: 128, G: 64, B: 64, Alpha: 255

		// Act
		var color = new Color ( expectedRgba );

		// Assert
		Assert.Equal ( 128, color.R );
		Assert.Equal ( 64, color.G );
		Assert.Equal ( 64, color.B );
		Assert.Equal ( 255, color.A );
	}
	[Fact]
	public void Color_Constructor_WithColorName ( )
	{
		// Arrange
		var colorName = ColorName.Blue;
		var expectedColor = new Color ( 0, 55, 218 ); // Blue

		// Act
		var color = new Color ( colorName );

		// Assert
		Assert.Equal ( expectedColor, color );
	}
}
