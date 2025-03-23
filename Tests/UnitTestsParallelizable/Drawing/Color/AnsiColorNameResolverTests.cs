#nullable enable

namespace Terminal.Gui.DrawingTests;

public class AnsiColorNameResolverTests
{
    private readonly AnsiColorNameResolver _candidate;

    public AnsiColorNameResolverTests ()
    {
        _candidate = new AnsiColorNameResolver ();
    }

    [Fact]
    public void GetNames_Returns16ColorNames ()
    {
        string[] expected = Enum.GetNames<ColorName16>();

        string[] actual = _candidate.GetColorNames ().ToArray();

        Assert.Equal (expected, actual);
    }

    [Theory]
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
    // Fail
    [InlineData (1, 2, 3, false, null)]
    public void TryNameColor_ReturnsExpectedColorName (byte r, byte g, byte b, bool expectedSuccess, string? expectedName)
    {
        bool actualSuccess = _candidate.TryNameColor(new Color(r, g, b), out string? actualName);

        Assert.Equal ((expectedSuccess, expectedName), (actualSuccess, actualName));
    }

    [Theory]
    [InlineData (nameof (ColorName16.Black), true, 0, 0, 0)]
    [InlineData (nameof (ColorName16.Blue), true, 0, 0, 255)]
    [InlineData (nameof (ColorName16.BrightBlue), true, 59, 120, 255)]
    [InlineData (nameof(ColorName16.BrightCyan), true, 97, 214, 214)]
    [InlineData (nameof(ColorName16.BrightGreen), true, 22, 198, 12)]
    [InlineData (nameof(ColorName16.BrightMagenta), true, 180, 0, 158)]
    [InlineData (nameof(ColorName16.BrightRed), true, 231, 72, 86)]
    [InlineData (nameof(ColorName16.BrightYellow), true, 249, 241, 165)]
    [InlineData (nameof(ColorName16.Cyan), true, 0, 255, 255)]
    [InlineData (nameof(ColorName16.DarkGray), true, 118, 118, 118)]
    [InlineData (nameof(ColorName16.Gray), true, 128, 128, 128)]
    [InlineData (nameof(ColorName16.Green), true, 0, 128, 0)]
    [InlineData (nameof(ColorName16.Magenta), true, 255, 0, 255)]
    [InlineData (nameof(ColorName16.Red), true, 255, 0, 0)]
    [InlineData (nameof(ColorName16.White), true, 255, 255, 255)]
    [InlineData (nameof(ColorName16.Yellow), true, 255, 255, 0)]
    // Case-insensitive
    [InlineData ("BRIGHTBLUE", true, 59, 120, 255)]
    [InlineData ("brightblue", true, 59, 120, 255)]
    // Fail
    [InlineData ("brightlight", false, 0, 0, 0)]
    public void TryParseColor_ReturnsExpectedColor (string inputName, bool expectedSuccess, byte r, byte g, byte b)
    {
        Color expectedColor = expectedSuccess
            ? new(r, g, b)
            : default;

        bool actualSuccess = _candidate.TryParseColor (inputName, out Color actualColor);

        Assert.Equal((expectedSuccess, expectedColor), (actualSuccess, actualColor));
    }
}
