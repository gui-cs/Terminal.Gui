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
    [InlineData (nameof (W3cColor.AliceBlue))]
    [InlineData (nameof (W3cColor.BlanchedAlmond))]
    [InlineData (nameof (W3cColor.CadetBlue))]
    [InlineData (nameof (W3cColor.DarkBlue))]
    [InlineData (nameof (W3cColor.FireBrick))]
    [InlineData (nameof (W3cColor.Gainsboro))]
    [InlineData (nameof (W3cColor.HoneyDew))]
    [InlineData (nameof (W3cColor.Indigo))]
    [InlineData (nameof (W3cColor.Khaki))]
    [InlineData (nameof (W3cColor.Lavender))]
    [InlineData (nameof (W3cColor.Maroon))]
    [InlineData (nameof (W3cColor.Navy))]
    [InlineData (nameof (W3cColor.Olive))]
    [InlineData (nameof (W3cColor.Plum))]
    [InlineData (nameof (W3cColor.RoyalBlue))]
    [InlineData (nameof (W3cColor.Silver))]
    [InlineData (nameof (W3cColor.Tomato))]
    [InlineData (nameof (W3cColor.Violet))]
    [InlineData (nameof (W3cColor.WhiteSmoke))]
    [InlineData (nameof (W3cColor.YellowGreen))]
    // W3C alternatives.
    [InlineData (nameof (W3cColor.Grey))]
    [InlineData (nameof (W3cColor.DarkGrey))]
    [InlineData (nameof (W3cColor.Aqua))]
    [InlineData (nameof (W3cColor.Fuchsia))]
    [InlineData (nameof (W3cColor.DarkSlateGray))]
    [InlineData (nameof (W3cColor.DarkSlateGrey))]
    [InlineData (nameof (W3cColor.DimGray))]
    [InlineData (nameof (W3cColor.DimGrey))]
    [InlineData (nameof (W3cColor.LightGray))]
    [InlineData (nameof (W3cColor.LightGrey))]
    [InlineData (nameof (W3cColor.SlateGray))]
    [InlineData (nameof (W3cColor.SlateGrey))]
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
    [InlineData (240, 248, 255, nameof (W3cColor.AliceBlue))]
    [InlineData (255, 235, 205, nameof (W3cColor.BlanchedAlmond))]
    [InlineData (95, 158, 160, nameof (W3cColor.CadetBlue))]
    [InlineData (0, 0, 139, nameof (W3cColor.DarkBlue))]
    [InlineData (178, 34, 34, nameof (W3cColor.FireBrick))]
    [InlineData (220, 220, 220, nameof (W3cColor.Gainsboro))]
    [InlineData (240, 255, 240, nameof (W3cColor.HoneyDew))]
    [InlineData (75, 0, 130, nameof (W3cColor.Indigo))]
    [InlineData (240, 230, 140, nameof (W3cColor.Khaki))]
    [InlineData (230, 230, 250, nameof (W3cColor.Lavender))]
    [InlineData (128, 0, 0, nameof (W3cColor.Maroon))]
    [InlineData (0, 0, 128, nameof (W3cColor.Navy))]
    [InlineData (128, 128, 0, nameof (W3cColor.Olive))]
    [InlineData (221, 160, 221, nameof (W3cColor.Plum))]
    [InlineData (65, 105, 225, nameof (W3cColor.RoyalBlue))]
    [InlineData (192, 192, 192, nameof (W3cColor.Silver))]
    [InlineData (255, 99, 71, nameof (W3cColor.Tomato))]
    [InlineData (238, 130, 238, nameof (W3cColor.Violet))]
    [InlineData (245, 245, 245, nameof (W3cColor.WhiteSmoke))]
    [InlineData (154, 205, 50, nameof (W3cColor.YellowGreen))]
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
    [InlineData (nameof (W3cColor.Fuchsia), 255, 0, 255)] // ANSI Magenta
    [InlineData (nameof (W3cColor.DarkGrey), 118, 118, 118)] // ANSI Dark Gray
    [InlineData (nameof (W3cColor.Grey), 128, 128, 128)] // ANSI Gray
    [InlineData (nameof (W3cColor.Aqua), 0, 255, 255)] // ANSI Cyan
    // W3C colors
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
