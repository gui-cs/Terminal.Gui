#nullable enable

namespace Terminal.Gui.DrawingTests;

public class MultiStandardColorNameResolverTests
{
    private readonly MultiStandardColorNameResolver _candidate;

    public MultiStandardColorNameResolverTests ()
    {
        _candidate = new MultiStandardColorNameResolver ();
    }

    [Theory]
    // Contains ANSI color names.
    [InlineData (nameof (ColorName16.Black), true)]
    [InlineData (nameof (ColorName16.White), true)]
    [InlineData (nameof (ColorName16.Red), true)]
    [InlineData (nameof (ColorName16.Green), true)]
    [InlineData (nameof (ColorName16.Blue), true)]
    [InlineData (nameof (ColorName16.Cyan), true)]
    [InlineData (nameof (ColorName16.Magenta), true)]
    [InlineData (nameof (ColorName16.DarkGray), true)]
    [InlineData (nameof (ColorName16.BrightGreen), true)]
    [InlineData (nameof (ColorName16.BrightMagenta), true)]
    // Contains regular W3C color.
    [InlineData (nameof (W3cColor.AliceBlue), true)]
    [InlineData (nameof (W3cColor.BlanchedAlmond), true)]
    [InlineData (nameof (W3cColor.CadetBlue), true)]
    [InlineData (nameof (W3cColor.DarkBlue), true)]
    [InlineData (nameof (W3cColor.FireBrick), true)]
    [InlineData (nameof (W3cColor.Gainsboro), true)]
    [InlineData (nameof (W3cColor.HoneyDew), true)]
    [InlineData (nameof (W3cColor.Indigo), true)]
    [InlineData (nameof (W3cColor.Khaki), true)]
    [InlineData (nameof (W3cColor.Lavender), true)]
    [InlineData (nameof (W3cColor.Maroon), true)]
    [InlineData (nameof (W3cColor.Navy), true)]
    [InlineData (nameof (W3cColor.Olive), true)]
    [InlineData (nameof (W3cColor.Plum), true)]
    [InlineData (nameof (W3cColor.RoyalBlue), true)]
    [InlineData (nameof (W3cColor.Silver), true)]
    [InlineData (nameof (W3cColor.Tomato), true)]
    [InlineData (nameof (W3cColor.Violet), true)]
    [InlineData (nameof (W3cColor.WhiteSmoke), true)]
    [InlineData (nameof (W3cColor.YellowGreen), true)]
    // Contains W3C color names that do not clash with ANSI colors.
    [InlineData (nameof (W3cColor.DarkSlateGray), true)]
    [InlineData (nameof (W3cColor.DarkSlateGrey), true)]
    [InlineData (nameof (W3cColor.DimGray), true)]
    [InlineData (nameof (W3cColor.DimGrey), true)]
    [InlineData (nameof (W3cColor.LightGray), true)]
    [InlineData (nameof (W3cColor.LightGrey), true)]
    [InlineData (nameof (W3cColor.SlateGray), true)]
    [InlineData (nameof (W3cColor.SlateGrey), true)]
    // Does not contain W3C color alternative names that clash with ANSI colors.
    [InlineData (nameof (W3cColor.Grey), false)]
    [InlineData (nameof (W3cColor.DarkGrey), false)]
    [InlineData (nameof (W3cColor.Aqua), false)]
    [InlineData (nameof (W3cColor.Fuchsia), false)]
    public void GetNames_ContainsExpectedNames (string name, bool shouldContain)
    {
        string[] names = _candidate.GetColorNames ().ToArray();

        if (shouldContain)
        {
            Assert.Contains (name, names);
        }
        else
        {
            Assert.DoesNotContain (name, names);
        }
    }

    [Theory]
    // ANSI color names
    [InlineData (0, 0, 0, true, nameof (ColorName16.Black))]
    [InlineData (0, 0, 255, true, nameof (ColorName16.Blue))]
    [InlineData (59, 120, 255, true, nameof (ColorName16.BrightBlue))]
    [InlineData (97, 214, 214, true, nameof (ColorName16.BrightCyan))]
    [InlineData (22, 198, 12, true, nameof (ColorName16.BrightGreen))]
    [InlineData (180, 0, 158, true, nameof (ColorName16.BrightMagenta))]
    [InlineData (231, 72, 86, true, nameof (ColorName16.BrightRed))]
    [InlineData (249, 241, 165, true, nameof (ColorName16.BrightYellow))]
    [InlineData (0, 255, 255, true, nameof (ColorName16.Cyan))]
    [InlineData (118, 118, 118, true, nameof (ColorName16.DarkGray))]
    [InlineData (128, 128, 128, true, nameof (ColorName16.Gray))]
    [InlineData (0, 128, 0, true, nameof (ColorName16.Green))]
    [InlineData (255, 0, 255, true, nameof (ColorName16.Magenta))]
    [InlineData (255, 0, 0, true, nameof (ColorName16.Red))]
    [InlineData (255, 255, 255, true, nameof (ColorName16.White))]
    [InlineData (255, 255, 0, true, nameof (ColorName16.Yellow))]
    // W3C color names
    [InlineData (240, 248, 255, true, nameof (W3cColor.AliceBlue))]
    [InlineData (255, 235, 205, true, nameof (W3cColor.BlanchedAlmond))]
    [InlineData (95, 158, 160, true, nameof (W3cColor.CadetBlue))]
    [InlineData (0, 0, 139, true, nameof (W3cColor.DarkBlue))]
    [InlineData (178, 34, 34, true, nameof (W3cColor.FireBrick))]
    [InlineData (220, 220, 220, true, nameof (W3cColor.Gainsboro))]
    [InlineData (240, 255, 240, true, nameof (W3cColor.HoneyDew))]
    [InlineData (75, 0, 130, true, nameof (W3cColor.Indigo))]
    [InlineData (240, 230, 140, true, nameof (W3cColor.Khaki))]
    [InlineData (230, 230, 250, true, nameof (W3cColor.Lavender))]
    [InlineData (128, 0, 0, true, nameof (W3cColor.Maroon))]
    [InlineData (0, 0, 128, true, nameof (W3cColor.Navy))]
    [InlineData (128, 128, 0, true, nameof (W3cColor.Olive))]
    [InlineData (221, 160, 221, true, nameof (W3cColor.Plum))]
    [InlineData (65, 105, 225, true, nameof (W3cColor.RoyalBlue))]
    [InlineData (192, 192, 192, true, nameof (W3cColor.Silver))]
    [InlineData (255, 99, 71, true, nameof (W3cColor.Tomato))]
    [InlineData (238, 130, 238, true, nameof (W3cColor.Violet))]
    [InlineData (245, 245, 245, true, nameof (W3cColor.WhiteSmoke))]
    [InlineData (154, 205, 50, true, nameof (W3cColor.YellowGreen))]
    // Blocked W3C colors
    [InlineData (169, 169, 169, false, null)] // W3cColor.DarkGr(a|e)y
    // Fail
    [InlineData (1, 2, 3, false, null)]
    public void TryNameColor_ReturnsExpectedColorNames (byte r, byte g, byte b, bool expectedSuccess, string? expectedName)
    {
        bool actualSuccess = _candidate.TryNameColor(new Color(r, g, b), out string? actualName);

        Assert.Equal ((expectedSuccess, expectedName), (actualSuccess, actualName));
    }

    [Theory]
    // ANSI colors.
    [InlineData (nameof (ColorName16.Black), true, 0, 0, 0)]
    [InlineData (nameof (ColorName16.Blue), true, 0, 0, 255)]
    [InlineData (nameof (ColorName16.BrightBlue), true, 59, 120, 255)]
    [InlineData (nameof (ColorName16.BrightCyan), true, 97, 214, 214)]
    [InlineData (nameof (ColorName16.BrightGreen), true, 22, 198, 12)]
    [InlineData (nameof (ColorName16.BrightMagenta), true, 180, 0, 158)]
    [InlineData (nameof (ColorName16.BrightRed), true, 231, 72, 86)]
    [InlineData (nameof (ColorName16.BrightYellow), true, 249, 241, 165)]
    [InlineData (nameof (ColorName16.Cyan), true, 0, 255, 255)]
    [InlineData (nameof (ColorName16.DarkGray), true, 118, 118, 118)]
    [InlineData (nameof (ColorName16.Gray), true, 128, 128, 128)]
    [InlineData (nameof (ColorName16.Green), true, 0, 128, 0)]
    [InlineData (nameof (ColorName16.Magenta), true, 255, 0, 255)]
    [InlineData (nameof (ColorName16.Red), true, 255, 0, 0)]
    [InlineData (nameof (ColorName16.White), true, 255, 255, 255)]
    [InlineData (nameof (ColorName16.Yellow), true, 255, 255, 0)]
    // W3C colors
    [InlineData (nameof (W3cColor.AliceBlue), true, 240, 248, 255)]
    [InlineData (nameof (W3cColor.BlanchedAlmond), true, 255, 235, 205)]
    [InlineData (nameof (W3cColor.CadetBlue), true, 95, 158, 160)]
    [InlineData (nameof (W3cColor.DarkBlue), true, 0, 0, 139)]
    [InlineData (nameof (W3cColor.FireBrick), true, 178, 34, 34)]
    [InlineData (nameof (W3cColor.Gainsboro), true, 220, 220, 220)]
    [InlineData (nameof (W3cColor.HoneyDew), true, 240, 255, 240)]
    [InlineData (nameof (W3cColor.Indigo), true, 75, 0, 130)]
    [InlineData (nameof (W3cColor.Khaki), true, 240, 230, 140)]
    [InlineData (nameof (W3cColor.Lavender), true, 230, 230, 250)]
    [InlineData (nameof (W3cColor.Maroon), true, 128, 0, 0)]
    [InlineData (nameof (W3cColor.Navy), true, 0, 0, 128)]
    [InlineData (nameof (W3cColor.Olive), true, 128, 128, 0)]
    [InlineData (nameof (W3cColor.Plum), true, 221, 160, 221)]
    [InlineData (nameof (W3cColor.RoyalBlue), true, 65, 105, 225)]
    [InlineData (nameof (W3cColor.Silver), true, 192, 192, 192)]
    [InlineData (nameof (W3cColor.Tomato), true, 255, 99, 71)]
    [InlineData (nameof (W3cColor.Violet), true, 238, 130, 238)]
    [InlineData (nameof (W3cColor.WhiteSmoke), true, 245, 245, 245)]
    [InlineData (nameof (W3cColor.YellowGreen), true, 154, 205, 50)]
    // Case-insensitive
    [InlineData ("BRIGHTBLUE", true, 59, 120, 255)]
    [InlineData ("brightblue", true, 59, 120, 255)]
    [InlineData ("TOMATO", true, 255, 99, 71)]
    [InlineData ("tomato", true, 255, 99, 71)]
    // Not existing
    [InlineData ("brightlight", false, 0, 0, 0)]
    // Existing enum numeric
    [InlineData ("12", true, 231, 72, 86)] // ColorName16.BrightRed
    [InlineData ("16737095", true, 255, 99, 71)] // W3cColor.Tomato
    // Non-existing enum numeric
    [InlineData ("-12", false, 0, 0, 0)]
    [InlineData ("-16737095", false, 0, 0, 0)]
    public void TryParseColor_ReturnsExpectedColors (string inputName, bool expectedSuccess, byte r, byte g, byte b)
    {
        Color expectedColor = expectedSuccess
            ? new(r, g, b)
            : default;

        bool actualSuccess = _candidate.TryParseColor (inputName, out Color actualColor);

        Assert.Equal ((expectedSuccess, expectedColor), (actualSuccess, actualColor));
    }
}
