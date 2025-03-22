#nullable enable

namespace Terminal.Gui.DrawingTests;

public class W3cColorsTests
{
    [Fact]
    public void GetColorNames_NamesAreInAlphabeticalOrder ()
    {
        List<string> alphabeticallyOrderedNames = Enum.GetNames<W3cColor> ().Order ().ToList ();

        Assert.Equal (alphabeticallyOrderedNames, W3cColors.GetColorNames ());
    }

    [Theory]
    // TODO: Solve conflicts with ColorName16
    [InlineData (nameof (W3cColor.Aqua))]
    [InlineData (nameof (W3cColor.Cyan))]
    [InlineData (nameof (W3cColor.DarkGray))]
    [InlineData (nameof (W3cColor.DarkGrey))]
    [InlineData (nameof (W3cColor.DarkSlateGray))]
    [InlineData (nameof (W3cColor.DarkSlateGrey))]
    [InlineData (nameof (W3cColor.DimGray))]
    [InlineData (nameof (W3cColor.DimGrey))]
    [InlineData (nameof (W3cColor.Fuchsia))]
    [InlineData (nameof (W3cColor.LightGray))]
    [InlineData (nameof (W3cColor.LightGrey))]
    [InlineData (nameof (W3cColor.LightSlateGray))]
    [InlineData (nameof (W3cColor.LightSlateGrey))]
    [InlineData (nameof (W3cColor.Magenta))]
    [InlineData (nameof (W3cColor.SlateGray))]
    [InlineData (nameof (W3cColor.SlateGrey))]
    public void GetColorNames_IncludesNamesWithSameValues (string name)
    {
        IReadOnlyList<string> names = W3cColors.GetColorNames ();

        Assert.True (names.Contains (name), $"W3C color names is missing '{name}'.");
    }

    [Theory]
    [InlineData (240, 248, 255, true, nameof(W3cColor.AliceBlue))]
    [InlineData (0, 255, 255, true, nameof(W3cColor.Aqua))]
    [InlineData (255, 0, 0, true, nameof(W3cColor.Red))]
    [InlineData (0, 128, 0, true, nameof(W3cColor.Green))]
    [InlineData (0, 0, 255, true, nameof(W3cColor.Blue))]
    [InlineData (0, 255, 0, true, nameof(W3cColor.Lime))]
    [InlineData (0, 0, 0, true, nameof(W3cColor.Black))]
    [InlineData (255, 255, 255, true, nameof(W3cColor.White))]
    [InlineData (154, 205, 50, true, nameof(W3cColor.YellowGreen))]
    [InlineData (1, 2, 3, false, null)]
    public void TryNameColor_ReturnsExpectedColorName(int r, int g, int b, bool expectedSuccess, string? expectedName)
    {
        Color inputColor = new(r, g, b);
        bool actualSuccess = W3cColors.TryNameColor (inputColor, out string? actualName);

        Assert.Equal ((expectedSuccess, expectedName), (actualSuccess, actualName));
    }

    [Theory]
    [InlineData ("Red", true, 255, 0, 0)]
    [InlineData ("red", true, 255, 0, 0)]
    [InlineData ("RED", true, 255, 0, 0)]
    [InlineData ("Green", true, 0, 128, 0)]
    [InlineData ("green", true, 0, 128, 0)]
    [InlineData ("GREEN", true, 0, 128, 0)]
    [InlineData ("Blue", true, 0, 0, 255)]
    [InlineData ("blue", true, 0, 0, 255)]
    [InlineData ("BLUE", true, 0, 0, 255)]
    [InlineData ("Nada", false, 0, 0, 0)]
    // Aliases also work
    // TODO: Solve conflicts with ColorName16
    [InlineData (nameof(W3cColor.Aqua), true, 0, 255, 255)]
    [InlineData (nameof(W3cColor.Cyan), true, 0, 255, 255)]
    [InlineData (nameof(W3cColor.DarkGray), true, 169, 169, 169)]
    [InlineData (nameof(W3cColor.DarkGrey), true, 169, 169, 169)]
    public void TryParseColor_ReturnsExpectedColor(string inputName, bool expectedSuccess, int r, int g, int b)
    {
        Color expectedColor = expectedSuccess
            ? new (r, g, b)
            : default;

        bool actualSuccess = W3cColors.TryParseColor (inputName, out Color actualColor);

        Assert.Equal ((expectedSuccess, expectedColor), (actualSuccess, actualColor));
    }
}
