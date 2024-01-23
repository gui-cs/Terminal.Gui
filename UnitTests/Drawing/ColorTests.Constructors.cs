namespace Terminal.Gui.Drawing.Tests;

public partial class ColorTests {
	[Theory]
    [CombinatorialData]
	public void Constructor_WithRGBValues_AllValuesCorrect ( [CombinatorialValues(0,1,254)]byte r, [CombinatorialValues(0,1,253)]byte g, [CombinatorialValues(0,1,252)]byte b )
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
	public void Constructor_WithRGBAValues_AllValuesCorrect ( [CombinatorialValues ( 0, 1, 254 )] byte r, [CombinatorialValues ( 0, 1, 253 )] byte g, [CombinatorialValues ( 0, 1, 252 )] byte b, [CombinatorialValues ( 0, 1, 251 )] byte a )
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

	[Theory]
	[CombinatorialData]
	public void Constructor_WithSignedInteger_AllValuesCorrect ([CombinatorialValues ( 0, 1, 254 )] byte r, [CombinatorialValues ( 0, 1, 253 )] byte g, [CombinatorialValues ( 0, 1, 252 )] byte b, [CombinatorialValues ( 0, 1, 251 )] byte a )
	{
		ReadOnlySpan<byte> bytes = [b, g, r, a];
		int expectedRgba = BitConverter.ToInt32 ( bytes );
		uint expectedArgb = BitConverter.ToUInt32 ( bytes );

		var color = new Color ( expectedRgba );

		Assert.Multiple (
			( ) => Assert.Equal ( r, color.R ),
			( ) => Assert.Equal ( g, color.G ),
			( ) => Assert.Equal ( b, color.B ),
			( ) => Assert.Equal ( a, color.A ),
			( ) => Assert.Equal ( expectedRgba, color.Rgba ),
			( ) => Assert.Equal ( expectedArgb, color.Argb )
		);
	}

	[Theory]
	[CombinatorialData]
	public void Constructor_WithUnsignedInteger_AllChannelsCorrect ([CombinatorialValues ( 0, 1, 254 )] byte r, [CombinatorialValues ( 0, 1, 253 )] byte g, [CombinatorialValues ( 0, 1, 252 )] byte b, [CombinatorialValues ( 0, 1, 251 )] byte a )
	{
		ReadOnlySpan<byte> bytes = [b, g, r, a];
		int expectedRgba = BitConverter.ToInt32 ( bytes );
		uint expectedArgb = BitConverter.ToUInt32 ( bytes );

		var color = new Color ( expectedArgb );

		Assert.Multiple (
			( ) => Assert.Equal ( r, color.R ),
			( ) => Assert.Equal ( g, color.G ),
			( ) => Assert.Equal ( b, color.B ),
			( ) => Assert.Equal ( a, color.A )
		);
	}

	[Theory]
    [MemberData(nameof(ColorTestsTheoryDataGenerators.Constructor_WithColorName_AllChannelsCorrect), MemberType = typeof(ColorTestsTheoryDataGenerators))]
	public void Constructor_WithColorName_AllChannelsCorrect (ColorName cname, Color expectedColor, ValueTuple<byte,byte,byte> expectedColorValues )
	{
		var color = new Color ( cname );

		Assert.Multiple (
			( ) => Assert.Equal (expectedColorValues.Item3, color.B),
			( ) => Assert.Equal (expectedColorValues.Item2, color.G),
			( ) => Assert.Equal (expectedColorValues.Item1, color.R),
			( ) => Assert.Equal (255, color.A)
		);
	}
}

public static partial class ColorTestsTheoryDataGenerators {
	public static TheoryData<ColorName, Color, ValueTuple<byte,byte,byte>> Constructor_WithColorName_AllChannelsCorrect ( )
	{
		TheoryData<ColorName, Color, ValueTuple<byte,byte,byte>> data = [];
		data.Add ( ColorName.Black, new ( 12, 12, 12 ), new ( 12, 12, 12 ) );
		data.Add ( ColorName.Blue, new ( 0, 55, 218 ), new ( 0, 55, 218 ) );
		data.Add ( ColorName.Green, new ( 19, 161, 14 ), new ( 19, 161, 14 ) );
		data.Add ( ColorName.Cyan, new ( 58, 150, 221 ), new ( 58, 150, 221 ) );
		data.Add ( ColorName.Red, new ( 197, 15, 31 ), new ( 197, 15, 31 ) );
		data.Add ( ColorName.Magenta, new ( 136, 23, 152 ), new ( 136, 23, 152 ) );
		data.Add ( ColorName.Yellow, new ( 128, 64, 32 ), new ( 128, 64, 32 ) );
		data.Add ( ColorName.Gray, new ( 204, 204, 204 ), new ( 204, 204, 204 ) );
		data.Add ( ColorName.DarkGray, new ( 118, 118, 118 ), new ( 118, 118, 118 ) );
		data.Add ( ColorName.BrightBlue, new ( 59, 120, 255 ), new ( 59, 120, 255 ) );
		data.Add ( ColorName.BrightGreen, new ( 22, 198, 12 ), new ( 22, 198, 12 ) );
		data.Add ( ColorName.BrightCyan, new ( 97, 214, 214 ), new ( 97, 214, 214 ) );
		data.Add ( ColorName.BrightRed, new ( 231, 72, 86 ), new ( 231, 72, 86 ) );
		data.Add ( ColorName.BrightMagenta, new ( 180, 0, 158 ), new ( 180, 0, 158 ) );
		data.Add ( ColorName.BrightYellow, new ( 249, 241, 165 ), new ( 249, 241, 165 ) );
		data.Add ( ColorName.White, new ( 242, 242, 242 ), new ( 242, 242, 242 ) );
		return data;
	}
}