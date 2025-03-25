#nullable enable

namespace Terminal.Gui.DrawingTests;

public class W3cColorNameResolverTests
{
    private readonly W3cColorNameResolver _candidate = new();

    [Fact]
    public void GetColorNames_NamesAreInAlphabeticalOrder ()
    {
        string[] alphabeticallyOrderedNames = Enum.GetNames<W3cColor> ().Order ().ToArray ();

        Assert.Equal (alphabeticallyOrderedNames, _candidate.GetColorNames ());
    }

    [Theory]
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
        string[] names = _candidate.GetColorNames ().ToArray();

        Assert.True (names.Contains (name), $"W3C color names is missing '{name}'.");
    }

    [Theory]
    [InlineData (240, 248, 255, nameof (W3cColor.AliceBlue))]
    [InlineData (0, 255, 255, nameof (W3cColor.Aqua))]
    [InlineData (255, 0, 0, nameof (W3cColor.Red))]
    [InlineData (0, 128, 0, nameof (W3cColor.Green))]
    [InlineData (0, 0, 255, nameof (W3cColor.Blue))]
    [InlineData (0, 255, 0, nameof (W3cColor.Lime))]
    [InlineData (0, 0, 0, nameof (W3cColor.Black))]
    [InlineData (255, 255, 255, nameof (W3cColor.White))]
    [InlineData (154, 205, 50, nameof (W3cColor.YellowGreen))]
    public void TryNameColor_ReturnsExpectedColorName (int r, int g, int b, string expectedName)
    {
        var expected = (true, expectedName);

        Color inputColor = new(r, g, b);
        bool actualSuccess = _candidate.TryNameColor (inputColor, out string? actualName);
        var actual = (actualSuccess, actualName);

        Assert.Equal (expected, actual);
    }

    [Fact]
    public void TryNameColor_NoMatchFails ()
    {
        (bool, string?) expected = (false, null);

        bool actualSuccess = _candidate.TryNameColor (new Color (1, 2, 3), out string? actualName);
        var actual = (actualSuccess, actualName);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (nameof (W3cColor.AliceBlue), 240, 248, 255)]
    [InlineData (nameof (W3cColor.BlanchedAlmond), 255, 235, 205)]
    [InlineData (nameof (W3cColor.CadetBlue), 95, 158, 160)]
    [InlineData (nameof (W3cColor.DarkBlue), 0, 0, 139)]
    [InlineData (nameof (W3cColor.FireBrick), 178, 34, 34)]
    [InlineData (nameof (W3cColor.Gainsboro), 220, 220, 220)]
    [InlineData (nameof (W3cColor.HoneyDew), 240, 255, 240)]
    [InlineData (nameof (W3cColor.Indigo), 75, 0, 130)]
    [InlineData (nameof (W3cColor.Khaki), 240, 230, 140)]
    [InlineData (nameof (W3cColor.Lavender), 230, 230, 250)]
    [InlineData (nameof (W3cColor.Maroon), 128, 0, 0)]
    [InlineData (nameof (W3cColor.Navy), 0, 0, 128)]
    [InlineData (nameof (W3cColor.Olive), 128, 128, 0)]
    [InlineData (nameof (W3cColor.Plum), 221, 160, 221)]
    [InlineData (nameof (W3cColor.RoyalBlue), 65, 105, 225)]
    [InlineData (nameof (W3cColor.Silver), 192, 192, 192)]
    [InlineData (nameof (W3cColor.Tomato), 255, 99, 71)]
    [InlineData (nameof (W3cColor.Violet), 238, 130, 238)]
    [InlineData (nameof (W3cColor.WhiteSmoke), 245, 245, 245)]
    [InlineData (nameof (W3cColor.YellowGreen), 154, 205, 50)]
    // Aliases also work
    [InlineData (nameof (W3cColor.Aqua), 0, 255, 255)]
    [InlineData (nameof (W3cColor.Cyan), 0, 255, 255)]
    [InlineData (nameof (W3cColor.DarkGray), 169, 169, 169)]
    [InlineData (nameof (W3cColor.DarkGrey), 169, 169, 169)]
    // Case-insensitive
    [InlineData ("Red", 255, 0, 0)]
    [InlineData ("red", 255, 0, 0)]
    [InlineData ("RED", 255, 0, 0)]
    public void TryParseColor_ReturnsExpectedColor (string inputName, int r, int g, int b)
    {
        var expected = (true, new Color(r, g, b));

        bool actualSuccess = _candidate.TryParseColor (inputName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("16737095", 255, 99, 71)] // W3cColor.Tomato
    public void TryParseColor_ResolvesValidEnumNumber (string inputName, byte r, byte g, byte b)
    {
        var expected = (true, new Color(r, g, b));

        bool actualSuccess = _candidate.TryParseColor (inputName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    [InlineData ("brightlight")]
    public void TryParseColor_FailsOnInvalidColorName (string? invalidName)
    {
        var expected = (false, default(Color));

        bool actualSuccess = _candidate.TryParseColor (invalidName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("-16737095")]
    public void TryParseColor_FailsOnInvalidEnumNumber (string invalidName)
    {
        var expected = (false, default(Color));

        bool actualSuccess = _candidate.TryParseColor (invalidName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }
}
