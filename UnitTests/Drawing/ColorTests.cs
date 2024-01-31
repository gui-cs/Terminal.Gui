﻿#nullable enable

namespace Terminal.Gui.DrawingTests;

public partial class ColorTests {
    [Theory]
    [CombinatorialData]
    public void Argb_Returns_Expected_Value (
        [CombinatorialValues (0, 255)] byte a,
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 153, 51)] byte g,
        [CombinatorialRange (0, 128, 32)] byte b
    ) {
        Color color = new (r, g, b, a);

        // Color.Rgba is expected to be a signed int32 in little endian order (a,b,g,r)
        ReadOnlySpan<byte> littleEndianBytes =  [b, g, r, a]

        ;
        uint expectedArgb = BitConverter.ToUInt32 (littleEndianBytes);
        Assert.Equal (expectedArgb, color.Argb);
    }

    [Fact]
    public void Color_ColorName_Get_ReturnsClosestColorName () {
        // Arrange
        var color = new Color (128, 64, 40); // Custom RGB color, closest to Yellow
        var expectedColorName = ColorName.Yellow;

        // Act
        var colorName = color.GetClosestNamedColor ();

        // Assert
        Assert.Equal (expectedColorName, colorName);
    }

    [Fact]
    public void Color_IsClosestToNamedColor_ReturnsExpectedValue () {
        // Arrange
        var color1 = new Color (ColorName.Red);
        var color2 = new Color (197, 15, 31); // Red in RGB

        Assert.True (color1.IsClosestToNamedColor (ColorName.Red));

        Assert.True (color2.IsClosestToNamedColor (ColorName.Red));
    }

    [Theory]
    [MemberData (
                    nameof (ColorTestsTheoryDataGenerators.FindClosestColor_ReturnsClosestColor),
                    MemberType = typeof (ColorTestsTheoryDataGenerators))]
    public void FindClosestColor_ReturnsClosestColor (Color inputColor, ColorName expectedColorName) {
        var actualColorName = Color.GetClosestNamedColor (inputColor);

        Assert.Equal (expectedColorName, actualColorName);
    }

    [Theory]
    [CombinatorialData]
    public void Rgba_Returns_Expected_Value (
        [CombinatorialValues (0, 255)] byte a,
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 153, 51)] byte g,
        [CombinatorialRange (0, 128, 32)] byte b
    ) {
        Color color = new (r, g, b, a);

        // Color.Rgba is expected to be a signed int32 in little endian order (a,b,g,r)
        ReadOnlySpan<byte> littleEndianBytes =  [b, g, r, a]

        ;
        int expectedRgba = BitConverter.ToInt32 (littleEndianBytes);
        Assert.Equal (expectedRgba, color.Rgba);
    }
}

public static partial class ColorTestsTheoryDataGenerators {
    public static TheoryData<Color, ColorName> FindClosestColor_ReturnsClosestColor () {
        TheoryData<Color, ColorName> data =  []

        ;
        data.Add (new Color (0, 0), ColorName.Black);
        data.Add (new Color (255, 255, 255), ColorName.White);
        data.Add (new Color (5, 100, 255), ColorName.BrightBlue);
        data.Add (new Color (0, 255), ColorName.BrightGreen);
        data.Add (new Color (255, 70, 8), ColorName.BrightRed);
        data.Add (new Color (0, 128, 128), ColorName.Cyan);
        data.Add (new Color (128, 64, 32), ColorName.Yellow);

        return data;
    }
}
