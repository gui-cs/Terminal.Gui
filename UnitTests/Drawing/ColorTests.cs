#nullable enable

namespace Terminal.Gui.DrawingTests;

public partial class ColorTests
{
    [Theory]
    [CombinatorialData]
    public void Argb_Returns_Expected_Value (
        [CombinatorialValues (0, 255)] byte a,
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 153, 51)] byte g,
        [CombinatorialRange (0, 128, 32)] byte b
    )
    {
        Color color = new (r, g, b, a);

        // Color.Rgba is expected to be a signed int32 in little endian order (a,b,g,r)
        ReadOnlySpan<byte> littleEndianBytes = [b, g, r, a];
        var expectedArgb = BitConverter.ToUInt32 (littleEndianBytes);
        Assert.Equal (expectedArgb, color.Argb);
    }

    [Fact (Skip = "Relies on old ColorName mapping")]
    public void Color_ColorName_Get_ReturnsClosestColorName ()
    {
        // Arrange
        var color = new Color (128, 64, 40); // Custom RGB color, closest to Yellow
        var expectedColorName = ColorName16.Yellow;

        // Act
        ColorName16 colorName = color.GetClosestNamedColor16 ();

        // Assert
        Assert.Equal (expectedColorName, colorName);
    }

    [Fact]
    public void Color_IsClosestToNamedColor_ReturnsExpectedValue ()
    {
        // Arrange
        var color1 = new Color (ColorName16.Red);
        var color2 = new Color (197, 15, 31); // Red in RGB

        Assert.True (color1.IsClosestToNamedColor16 (ColorName16.Red));

        Assert.True (color2.IsClosestToNamedColor16 (ColorName16.Red));
    }

    [Theory (Skip = "Test data is now bogus")]
    [MemberData (
                    nameof (ColorTestsTheoryDataGenerators.FindClosestColor_ReturnsClosestColor),
                    MemberType = typeof (ColorTestsTheoryDataGenerators)
                )]
    public void FindClosestColor_ReturnsClosestColor (Color inputColor, ColorName16 expectedColorName)
    {
        ColorName16 actualColorName = Color.GetClosestNamedColor16 (inputColor);

        Assert.Equal (expectedColorName, actualColorName);
    }

    [Theory]
    [CombinatorialData]
    public void Rgba_Returns_Expected_Value (
        [CombinatorialValues (0, 255)] byte a,
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 153, 51)] byte g,
        [CombinatorialRange (0, 128, 32)] byte b
    )
    {
        Color color = new (r, g, b, a);

        // Color.Rgba is expected to be a signed int32 in little endian order (a,b,g,r)
        ReadOnlySpan<byte> littleEndianBytes = [b, g, r, a];
        var expectedRgba = BitConverter.ToInt32 (littleEndianBytes);
        Assert.Equal (expectedRgba, color.Rgba);
    }
}

public static partial class ColorTestsTheoryDataGenerators
{
    public static TheoryData<Color, ColorName16> FindClosestColor_ReturnsClosestColor ()
    {
        TheoryData<Color, ColorName16> data = [];
        data.Add (new Color (0, 0), ColorName16.Black);
        data.Add (new Color (255, 255, 255), ColorName16.White);
        data.Add (new Color (5, 100, 255), ColorName16.BrightBlue);
        data.Add (new Color (0, 255), ColorName16.BrightGreen);
        data.Add (new Color (255, 70, 8), ColorName16.BrightRed);
        data.Add (new Color (0, 128, 128), ColorName16.Cyan);
        data.Add (new Color (128, 64, 32), ColorName16.Yellow);

        return data;
    }
}
