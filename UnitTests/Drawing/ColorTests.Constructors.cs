namespace Terminal.Gui.DrawingTests;

public partial class ColorTests {

	[Fact]
	public void Constructor_Empty_ReturnsColorWithZeroValue ()
	{
		Color color = new ();
		Assert.Multiple (
			() => Assert.Equal (0, color.Rgba),
			() => Assert.Equal (0u, color.Argb),
			() => Assert.Equal (0, color.R),
			() => Assert.Equal (0, color.G),
			() => Assert.Equal (0, color.B),
			() => Assert.Equal (0, color.A)
		);
	}

	[Theory]
	[CombinatorialData]
	public void Constructor_WithByteRGBAValues_AllValuesCorrect ([CombinatorialValues (0, 1, 254)] byte r, [CombinatorialValues (0, 1, 253)] byte g, [CombinatorialValues (0, 1, 252)] byte b, [CombinatorialValues (0, 1, 251)] byte a)
	{
		var color = new Color (r, g, b, a);

		ReadOnlySpan<byte> bytes = [b, g, r, a];
		int expectedRgba = BitConverter.ToInt32 (bytes);
		uint expectedArgb = BitConverter.ToUInt32 (bytes);

		Assert.Multiple (
			() => Assert.Equal (r, color.R),
			() => Assert.Equal (g, color.G),
			() => Assert.Equal (b, color.B),
			() => Assert.Equal (a, color.A),
			() => Assert.Equal (expectedRgba, color.Rgba),
			() => Assert.Equal (expectedArgb, color.Argb)
		);
	}
	[Theory]
	[CombinatorialData]
	public void Constructor_WithByteRGBValues_AllValuesCorrect ([CombinatorialValues (0, 1, 254)] byte r, [CombinatorialValues (0, 1, 253)] byte g, [CombinatorialValues (0, 1, 252)] byte b)
	{
		var color = new Color (r, g, b);

		ReadOnlySpan<byte> bytes = [b, g, r, 255];
		int expectedRgba = BitConverter.ToInt32 (bytes);
		uint expectedArgb = BitConverter.ToUInt32 (bytes);

		Assert.Multiple (
			() => Assert.Equal (r, color.R),
			() => Assert.Equal (g, color.G),
			() => Assert.Equal (b, color.B),
			() => Assert.Equal (byte.MaxValue, color.A),
			() => Assert.Equal (expectedRgba, color.Rgba),
			() => Assert.Equal (expectedArgb, color.Argb)
		);
	}

	[Theory]
	[MemberData (nameof (ColorTestsTheoryDataGenerators.Constructor_WithColorName_AllChannelsCorrect), MemberType = typeof (ColorTestsTheoryDataGenerators))]
	public void Constructor_WithColorName_AllChannelsCorrect (ColorName cname, ValueTuple<byte, byte, byte> expectedColorValues)
	{
		var color = new Color (cname);

		(byte r, byte g, byte b) = expectedColorValues;
		Assert.Multiple (
			() => Assert.Equal (r, color.R),
			() => Assert.Equal (g, color.G),
			() => Assert.Equal (b, color.B),
			() => Assert.Equal (byte.MaxValue, color.A)
		);
	}

	[Theory]
	[CombinatorialData]
	public void Constructor_WithInt32_AllValuesCorrect ([CombinatorialValues (0, 1, 254)] byte r, [CombinatorialValues (0, 1, 253)] byte g, [CombinatorialValues (0, 1, 252)] byte b, [CombinatorialValues (0, 1, 251)] byte a)
	{
		ReadOnlySpan<byte> bytes = [b, g, r, a];
		int expectedRgba = BitConverter.ToInt32 (bytes);
		uint expectedArgb = BitConverter.ToUInt32 (bytes);

		var color = new Color (expectedRgba);

		Assert.Multiple (
			() => Assert.Equal (r, color.R),
			() => Assert.Equal (g, color.G),
			() => Assert.Equal (b, color.B),
			() => Assert.Equal (a, color.A),
			() => Assert.Equal (expectedRgba, color.Rgba),
			() => Assert.Equal (expectedArgb, color.Argb)
		);
	}

	[Theory]
	[CombinatorialData]
	public void Constructor_WithInt32RGBAValues_AllValuesCorrect ([CombinatorialRandomData (Count = 4, Minimum = 0, Maximum = 255)] int r, [CombinatorialRandomData (Count = 4, Minimum = 0, Maximum = 255)] int g, [CombinatorialRandomData (Count = 4, Minimum = 0, Maximum = 255)] int b, [CombinatorialRandomData (Count = 4, Minimum = 0, Maximum = 255)] int a)
	{
		var color = new Color (r, g, b, a);

		Assert.Multiple (
			() => Assert.Equal (r, color.R),
			() => Assert.Equal (g, color.G),
			() => Assert.Equal (b, color.B),
			() => Assert.Equal (a, color.A)
		);
	}

	[Theory]
	[CombinatorialData]
	public void Constructor_WithInt32RGBValues_AllValuesCorrect ([CombinatorialRandomData (Count = 4, Minimum = 0, Maximum = 255)] int r, [CombinatorialRandomData (Count = 4, Minimum = 0, Maximum = 255)] int g, [CombinatorialRandomData (Count = 4, Minimum = 0, Maximum = 255)] int b)
	{
		var color = new Color (r, g, b);

		Assert.Multiple (
			() => Assert.Equal (r, color.R),
			() => Assert.Equal (g, color.G),
			() => Assert.Equal (b, color.B),
			() => Assert.Equal (byte.MaxValue, color.A)
		);
	}

	[Theory]
	[CombinatorialData]
	public void Constructor_WithString_EmptyOrWhitespace_ThrowsArgumentException ([CombinatorialValues ("", "\t", " ", "\r", "\r\n", "\n", "   ")] string badString)
	{
		Assert.Throws<ArgumentException> (() => Color.Parse (badString));
	}

	[Fact]
	public void Constructor_WithString_Null_ThrowsArgumentNullException ()
	{
		Assert.Throws<ArgumentNullException> (static () => Color.Parse (null));
	}

	[Theory]
	[CombinatorialData]
	public void Constructor_WithUInt32_AllChannelsCorrect ([CombinatorialValues (0, 1, 254)] byte r, [CombinatorialValues (0, 1, 253)] byte g, [CombinatorialValues (0, 1, 252)] byte b, [CombinatorialValues (0, 1, 251)] byte a)
	{
		ReadOnlySpan<byte> bytes = [b, g, r, a];
		uint expectedArgb = BitConverter.ToUInt32 (bytes);

		var color = new Color (expectedArgb);

		Assert.Multiple (
			() => Assert.Equal (r, color.R),
			() => Assert.Equal (g, color.G),
			() => Assert.Equal (b, color.B),
			() => Assert.Equal (a, color.A)
		);
	}
}
public static partial class ColorTestsTheoryDataGenerators {
	public static TheoryData<ColorName, ValueTuple<byte, byte, byte>> Constructor_WithColorName_AllChannelsCorrect ()
	{
		TheoryData<ColorName, ValueTuple<byte, byte, byte>> data = [];
		data.Add (ColorName.Black, new ValueTuple<byte, byte, byte> (12, 12, 12));
		data.Add (ColorName.Blue, new ValueTuple<byte, byte, byte> (0, 55, 218));
		data.Add (ColorName.Green, new ValueTuple<byte, byte, byte> (19, 161, 14));
		data.Add (ColorName.Cyan, new ValueTuple<byte, byte, byte> (58, 150, 221));
		data.Add (ColorName.Red, new ValueTuple<byte, byte, byte> (197, 15, 31));
		data.Add (ColorName.Magenta, new ValueTuple<byte, byte, byte> (136, 23, 152));
		data.Add (ColorName.Yellow, new ValueTuple<byte, byte, byte> (128, 64, 32));
		data.Add (ColorName.Gray, new ValueTuple<byte, byte, byte> (204, 204, 204));
		data.Add (ColorName.DarkGray, new ValueTuple<byte, byte, byte> (118, 118, 118));
		data.Add (ColorName.BrightBlue, new ValueTuple<byte, byte, byte> (59, 120, 255));
		data.Add (ColorName.BrightGreen, new ValueTuple<byte, byte, byte> (22, 198, 12));
		data.Add (ColorName.BrightCyan, new ValueTuple<byte, byte, byte> (97, 214, 214));
		data.Add (ColorName.BrightRed, new ValueTuple<byte, byte, byte> (231, 72, 86));
		data.Add (ColorName.BrightMagenta, new ValueTuple<byte, byte, byte> (180, 0, 158));
		data.Add (ColorName.BrightYellow, new ValueTuple<byte, byte, byte> (249, 241, 165));
		data.Add (ColorName.White, new ValueTuple<byte, byte, byte> (242, 242, 242));
		return data;
	}
}
