#nullable enable

namespace Terminal.Gui.DrawingTests;

public class MultiStandardColorNameResolverTests
{
    private readonly MultiStandardColorNameResolver _candidate = new();

    [Theory]
    // ANSI color names.
    [InlineData (nameof (ColorName16.Black))]
    [InlineData (nameof (ColorName16.White))]
    [InlineData (nameof (ColorName16.Red))]
    [InlineData (nameof (ColorName16.Green))]
    [InlineData (nameof (ColorName16.Blue))]
    [InlineData (nameof (ColorName16.Cyan))]
    [InlineData (nameof (ColorName16.Magenta))]
    [InlineData (nameof (ColorName16.DarkGray))]
    [InlineData (nameof (ColorName16.BrightGreen))]
    [InlineData (nameof (ColorName16.BrightMagenta))]
    // Regular W3C color.
    [InlineData (nameof (StandardColor.AliceBlue))]
    [InlineData (nameof (StandardColor.BlanchedAlmond))]
    [InlineData (nameof (StandardColor.CadetBlue))]
    [InlineData (nameof (StandardColor.DarkBlue))]
    [InlineData (nameof (StandardColor.FireBrick))]
    [InlineData (nameof (StandardColor.Gainsboro))]
    [InlineData (nameof (StandardColor.HoneyDew))]
    [InlineData (nameof (StandardColor.Indigo))]
    [InlineData (nameof (StandardColor.Khaki))]
    [InlineData (nameof (StandardColor.Lavender))]
    [InlineData (nameof (StandardColor.Maroon))]
    [InlineData (nameof (StandardColor.Navy))]
    [InlineData (nameof (StandardColor.Olive))]
    [InlineData (nameof (StandardColor.Plum))]
    [InlineData (nameof (StandardColor.RoyalBlue))]
    [InlineData (nameof (StandardColor.Silver))]
    [InlineData (nameof (StandardColor.Tomato))]
    [InlineData (nameof (StandardColor.Violet))]
    [InlineData (nameof (StandardColor.WhiteSmoke))]
    [InlineData (nameof (StandardColor.YellowGreen))]
    // W3C alternatives.
    [InlineData (nameof (StandardColor.Grey))]
    [InlineData (nameof (StandardColor.DarkGrey))]
    [InlineData (nameof (StandardColor.Aqua))]
    [InlineData (nameof (StandardColor.Fuchsia))]
    [InlineData (nameof (StandardColor.DarkSlateGray))]
    [InlineData (nameof (StandardColor.DarkSlateGrey))]
    [InlineData (nameof (StandardColor.DimGray))]
    [InlineData (nameof (StandardColor.DimGrey))]
    [InlineData (nameof (StandardColor.LightGray))]
    [InlineData (nameof (StandardColor.LightGrey))]
    [InlineData (nameof (StandardColor.SlateGray))]
    [InlineData (nameof (StandardColor.SlateGrey))]
    public void GetNames_ContainsCombinationOfAnsiAndW3cNames (string name)
    {
        string[] names = _candidate.GetColorNames ().ToArray();
        Assert.Contains (name, names);
    }

    [Theory]
    // ANSI color names
    [InlineData (0, 0, 0, nameof (ColorName16.Black))]
    [InlineData (0, 0, 255, nameof (ColorName16.Blue))]
    [InlineData (59, 120, 255, nameof (ColorName16.BrightBlue))]
    [InlineData (97, 214, 214, nameof (ColorName16.BrightCyan))]
    [InlineData (22, 198, 12, nameof (ColorName16.BrightGreen))]
    [InlineData (180, 0, 158, nameof (ColorName16.BrightMagenta))]
    [InlineData (231, 72, 86, nameof (ColorName16.BrightRed))]
    [InlineData (249, 241, 165, nameof (ColorName16.BrightYellow))]
    [InlineData (0, 255, 255, nameof (ColorName16.Cyan))]
    [InlineData (118, 118, 118, nameof (ColorName16.DarkGray))]
    [InlineData (128, 128, 128, nameof (ColorName16.Gray))]
    [InlineData (0, 128, 0, nameof (ColorName16.Green))]
    [InlineData (255, 0, 255, nameof (ColorName16.Magenta))]
    [InlineData (255, 0, 0, nameof (ColorName16.Red))]
    [InlineData (255, 255, 255, nameof (ColorName16.White))]
    [InlineData (255, 255, 0, nameof (ColorName16.Yellow))]
    // W3C color names
    [InlineData (240, 248, 255, nameof (StandardColor.AliceBlue))]
    [InlineData (255, 235, 205, nameof (StandardColor.BlanchedAlmond))]
    [InlineData (95, 158, 160, nameof (StandardColor.CadetBlue))]
    [InlineData (0, 0, 139, nameof (StandardColor.DarkBlue))]
    [InlineData (178, 34, 34, nameof (StandardColor.FireBrick))]
    [InlineData (220, 220, 220, nameof (StandardColor.Gainsboro))]
    [InlineData (240, 255, 240, nameof (StandardColor.HoneyDew))]
    [InlineData (75, 0, 130, nameof (StandardColor.Indigo))]
    [InlineData (240, 230, 140, nameof (StandardColor.Khaki))]
    [InlineData (230, 230, 250, nameof (StandardColor.Lavender))]
    [InlineData (128, 0, 0, nameof (StandardColor.Maroon))]
    [InlineData (0, 0, 128, nameof (StandardColor.Navy))]
    [InlineData (128, 128, 0, nameof (StandardColor.Olive))]
    [InlineData (221, 160, 221, nameof (StandardColor.Plum))]
    [InlineData (65, 105, 225, nameof (StandardColor.RoyalBlue))]
    [InlineData (192, 192, 192, nameof (StandardColor.Silver))]
    [InlineData (255, 99, 71, nameof (StandardColor.Tomato))]
    [InlineData (238, 130, 238, nameof (StandardColor.Violet))]
    [InlineData (245, 245, 245, nameof (StandardColor.WhiteSmoke))]
    [InlineData (154, 205, 50, nameof (StandardColor.YellowGreen))]
    public void TryNameColor_ReturnsExpectedColorNames (byte r, byte g, byte b, string expectedName)
    {
        var expected = (true, expectedName);

        bool actualSuccess = _candidate.TryNameColor(new Color(r, g, b), out string? actualName);
        var actual = (actualSuccess, actualName);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (169, 169, 169)] // W3cColor.DarkGr(a|e)y
    public void TryNameColor_OmitsBlockedW3cColors (byte r, byte g, byte b)
    {
        (bool, string?) expected = (false, null);

        bool actualSuccess = _candidate.TryNameColor (new Color (r, g, b), out string? actualName);
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
    // ANSI colors
    [InlineData (nameof (ColorName16.Black), 0, 0, 0)]
    [InlineData (nameof (ColorName16.Blue), 0, 0, 255)]
    [InlineData (nameof (ColorName16.BrightBlue), 59, 120, 255)]
    [InlineData (nameof (ColorName16.BrightCyan), 97, 214, 214)]
    [InlineData (nameof (ColorName16.BrightGreen), 22, 198, 12)]
    [InlineData (nameof (ColorName16.BrightMagenta), 180, 0, 158)]
    [InlineData (nameof (ColorName16.BrightRed), 231, 72, 86)]
    [InlineData (nameof (ColorName16.BrightYellow), 249, 241, 165)]
    [InlineData (nameof (ColorName16.Cyan), 0, 255, 255)]
    [InlineData (nameof (ColorName16.DarkGray), 118, 118, 118)]
    [InlineData (nameof (ColorName16.Gray), 128, 128, 128)]
    [InlineData (nameof (ColorName16.Green), 0, 128, 0)]
    [InlineData (nameof (ColorName16.Magenta), 255, 0, 255)]
    [InlineData (nameof (ColorName16.Red), 255, 0, 0)]
    [InlineData (nameof (ColorName16.White), 255, 255, 255)]
    [InlineData (nameof (ColorName16.Yellow), 255, 255, 0)]
    // W3C color name => substituted ANSI color
    [InlineData (nameof (StandardColor.Fuchsia), 255, 0, 255)] // ANSI Magenta
    [InlineData (nameof (StandardColor.DarkGrey), 118, 118, 118)] // ANSI Dark Gray
    [InlineData (nameof (StandardColor.Grey), 128, 128, 128)] // ANSI Gray
    [InlineData (nameof (StandardColor.Aqua), 0, 255, 255)] // ANSI Cyan
    // W3C colors
    [InlineData (nameof (StandardColor.AliceBlue), 240, 248, 255)]
    [InlineData (nameof (StandardColor.BlanchedAlmond), 255, 235, 205)]
    [InlineData (nameof (StandardColor.CadetBlue), 95, 158, 160)]
    [InlineData (nameof (StandardColor.DarkBlue), 0, 0, 139)]
    [InlineData (nameof (StandardColor.FireBrick), 178, 34, 34)]
    [InlineData (nameof (StandardColor.Gainsboro), 220, 220, 220)]
    [InlineData (nameof (StandardColor.HoneyDew), 240, 255, 240)]
    [InlineData (nameof (StandardColor.Indigo), 75, 0, 130)]
    [InlineData (nameof (StandardColor.Khaki), 240, 230, 140)]
    [InlineData (nameof (StandardColor.Lavender), 230, 230, 250)]
    [InlineData (nameof (StandardColor.Maroon), 128, 0, 0)]
    [InlineData (nameof (StandardColor.Navy), 0, 0, 128)]
    [InlineData (nameof (StandardColor.Olive), 128, 128, 0)]
    [InlineData (nameof (StandardColor.Plum), 221, 160, 221)]
    [InlineData (nameof (StandardColor.RoyalBlue), 65, 105, 225)]
    [InlineData (nameof (StandardColor.Silver), 192, 192, 192)]
    [InlineData (nameof (StandardColor.Tomato), 255, 99, 71)]
    [InlineData (nameof (StandardColor.Violet), 238, 130, 238)]
    [InlineData (nameof (StandardColor.WhiteSmoke), 245, 245, 245)]
    [InlineData (nameof (StandardColor.YellowGreen), 154, 205, 50)]
    // Case-insensitive
    [InlineData ("BRIGHTBLUE", 59, 120, 255)]
    [InlineData ("brightblue", 59, 120, 255)]
    [InlineData ("TOMATO", 255, 99, 71)]
    [InlineData ("tomato", 255, 99, 71)]

    public void TryParseColor_ResolvesCombinationOfAnsiAndW3cColors (string inputName, byte r, byte g, byte b)
    {
        var expected = (true, new Color(r, g, b));

        bool actualSuccess = _candidate.TryParseColor (inputName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("12", 231, 72, 86)] // ColorName16.BrightRed
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
    [InlineData ("-12")]
    [InlineData ("-16737095")]
    public void TryParseColor_FailsOnInvalidEnumNumber (string invalidName)
    {
        var expected = (false, default(Color));

        bool actualSuccess = _candidate.TryParseColor (invalidName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }
}
