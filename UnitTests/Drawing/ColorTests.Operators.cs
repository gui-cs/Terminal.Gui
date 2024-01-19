using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;
namespace Terminal.Gui.DrawingTests;

public partial class ColorTests {

	[Theory]
    [CombinatorialData]
	public void ImplicitOperator_FromInt_RoundTripsCorrectly ( [CombinatorialRandomData(Count=16)]int expectedValue )
	{
		Color color = expectedValue;

		Assert.Equal ( expectedValue, color.Rgba );
	}

	[Theory]
	[CombinatorialData]
	public void ImplicitOperator_ToInt_RoundTripsCorrectly ( [CombinatorialRandomData(Count=16)]int expectedValue )
	{
		var color = new Color ( expectedValue );

		var Rgba = (int)color;

		Assert.Equal ( expectedValue, Rgba );
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