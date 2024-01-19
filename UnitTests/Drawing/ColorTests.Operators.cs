using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;
namespace Terminal.Gui.DrawingTests;

public partial class ColorTests {

	[Fact]
	public void Color_ImplicitOperator_FromInt ( )
	{
		// Arrange
		var Rgba = unchecked( (int)0xFF804020 ); // R: 128, G: 64, B: 32, Alpha: 255
		var expectedColor = new Color ( 128, 64, 32 );

		// Act
		Color color = Rgba;

		// Assert
		Assert.Equal ( expectedColor, color );
	}
	[Fact]
	public void Color_ExplicitOperator_ToInt ( )
	{
		// Arrange
		var color = new Color ( 128, 64, 32 );
		var expectedRgba = unchecked( (int)0xFF804020 ); // R: 128, G: 64, B: 32, Alpha: 255

		// Act
		var Rgba = (int)color;

		// Assert
		Assert.Equal ( expectedRgba, Rgba );
	}
	[Fact]
	public void Color_ImplicitOperator_FromColorNames ( )
	{
		// Arrange
		var colorName = ColorName.Blue;
		var expectedColor = new Color ( 0, 55, 218 ); // Blue

		// Act
		var color = new Color ( colorName );

		// Assert
		Assert.Equal ( expectedColor, color );
	}
	[Fact]
	public void Color_ExplicitOperator_ToColorNames ( )
	{
		// Arrange
		var color = new Color ( 0, 0, 0x80 ); // Blue
		var expectedColorName = ColorName.Blue;

		// Act
		var colorName = (ColorName)color;

		// Assert
		Assert.Equal ( expectedColorName, colorName );
	}
	[Theory]
	[CombinatorialData]
	public void Color_GeneratedEqualityOperators ( [CombinatorialValues ( 0, short.MaxValue, int.MaxValue, uint.MaxValue )] uint u1, [CombinatorialValues ( 0, short.MaxValue, int.MaxValue, uint.MaxValue )] uint u2 )
	{
		Color color1 = u1;
		Color color2 = u2;

		if ( u1 == u2 ) {
			Assert.True ( color1 == color2 );
			Assert.False ( color1 != color2 );
		}
		else {
			Assert.True ( color1 != color2 );
			Assert.False ( color1 == color2 );
		}
	}
}

public static partial class ColorTestsTheoryDataGenerators {
	public static TheoryData<FieldInfo, int> Fields_At_Expected_Offsets ( )
	{
		TheoryData<FieldInfo, int> data = [];
		data.Add ( typeof ( Color ).GetField ( "Argb", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding ), 0 );
		data.Add ( typeof ( Color ).GetField ( "Rgba", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding ), 0 );
		data.Add ( typeof ( Color ).GetField ( "B", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding ), 0 );
		data.Add ( typeof ( Color ).GetField ( "G", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding ), 1 );
		data.Add ( typeof ( Color ).GetField ( "R", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding ), 2 );
		data.Add ( typeof ( Color ).GetField ( "A", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding ), 3 );
		return data;
	}
}