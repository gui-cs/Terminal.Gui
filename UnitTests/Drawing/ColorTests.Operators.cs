using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;
namespace Terminal.Gui.DrawingTests;

public partial class ColorTests {

	[Theory]
	[CombinatorialData]
	[Trait ( "Category", "Operators" )]
	public void ImplicitOperator_FromInt_RoundTripsCorrectly ( [CombinatorialRandomData ( Count = 16 )] int expectedValue )
	{
		Color color = expectedValue;

		Assert.Equal ( expectedValue, color.Rgba );
	}

	[Theory]
	[CombinatorialData]
	[Trait ( "Category", "Operators" )]
	public void ImplicitOperator_ToInt_RoundTripsCorrectly ( [CombinatorialRandomData ( Count = 16 )] int expectedValue )
	{
		var color = new Color ( expectedValue );

		var Rgba = (int)color;

		Assert.Equal ( expectedValue, Rgba );
	}

	[Theory]
	[Trait ( "Category", "Operators" )]
	[MemberData(nameof(ColorTestsTheoryDataGenerators.ExplicitOperator_FromColorName_RoundTripsCorrectly), MemberType = typeof(ColorTestsTheoryDataGenerators))]
	public void ExplicitOperator_FromColorName_RoundTripsCorrectly ( ColorName cname, Color expectedColor )
	{
		Color color = (Color)cname;

		Assert.Equal ( expectedColor, color );
	}

	[Theory]
	[Trait ( "Category", "Operators" )]
	[MemberData(nameof(ColorTestsTheoryDataGenerators.ExplicitOperator_FromColorName_RoundTripsCorrectly), MemberType = typeof(ColorTestsTheoryDataGenerators))]
	public void ExplicitOperator_ToColorName_RoundTripsCorrectly (ColorName expectedColorName, Color color )
	{
		var colorName = (ColorName)color;

		Assert.Equal ( expectedColorName, colorName );
	}

	[Theory]
	[CombinatorialData]
	public void GeneratedEqualityOperators_BehaveAsExpected ( [CombinatorialValues ( 0, short.MaxValue, int.MaxValue, uint.MaxValue )] uint u1, [CombinatorialValues ( 0, short.MaxValue, int.MaxValue, uint.MaxValue )] uint u2 )
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

	public static TheoryData<ColorName, Color> ExplicitOperator_FromColorName_RoundTripsCorrectly ( )
	{
		TheoryData<ColorName, Color> data = [];
		data.Add ( ColorName.Black, new ( 12, 12, 12 ) );
		data.Add ( ColorName.Blue, new ( 0, 55, 218 ) );
		data.Add ( ColorName.Green, new ( 19, 161, 14 ) );
		data.Add ( ColorName.Cyan, new ( 58, 150, 221 ) );
		data.Add ( ColorName.Red, new ( 197, 15, 31 ) );
		data.Add ( ColorName.Magenta, new ( 136, 23, 152 ) );
		data.Add ( ColorName.Yellow, new ( 128, 64, 32 ) );
		data.Add ( ColorName.Gray, new ( 204, 204, 204 ) );
		data.Add ( ColorName.DarkGray, new ( 118, 118, 118 ) );
		data.Add ( ColorName.BrightBlue, new ( 59, 120, 255 ) );
		data.Add ( ColorName.BrightGreen, new ( 22, 198, 12 ) );
		data.Add ( ColorName.BrightCyan, new ( 97, 214, 214 ) );
		data.Add ( ColorName.BrightRed, new ( 231, 72, 86 ) );
		data.Add ( ColorName.BrightMagenta, new ( 180, 0, 158 ) );
		data.Add ( ColorName.BrightYellow, new ( 249, 241, 165 ) );
		data.Add ( ColorName.White, new ( 242, 242, 242 ) );
		return data;
	}

}