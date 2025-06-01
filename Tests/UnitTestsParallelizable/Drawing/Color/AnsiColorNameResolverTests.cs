#nullable enable

namespace Terminal.Gui.DrawingTests;

public class AnsiColorNameResolverTests
{
    private readonly AnsiColorNameResolver _candidate = new ();
    [Fact]
    public void TryNameColor_Resolves_All_ColorName16 ()
    {
        var resolver = new AnsiColorNameResolver ();

        foreach (ColorName16 name in Enum.GetValues<ColorName16> ())
        {
            var color = new Color (name);
            bool success = resolver.TryNameColor (color, out string? resultName);

            Assert.True (success, $"Expected TryNameColor to succeed for {name}");
            Assert.Equal (name.ToString (), resultName);
        }
    }

    [Fact]
    public void TryParseColor_Resolves_All_ColorName16_Names ()
    {
        var resolver = new AnsiColorNameResolver ();

        foreach (ColorName16 name in Enum.GetValues<ColorName16> ())
        {
            bool success = resolver.TryParseColor (name.ToString (), out Color parsed);

            Assert.True (success, $"Expected TryParseColor to succeed for {name}");
            Assert.Equal (new Color (name), parsed);
        }
    }

    public static IEnumerable<object []> AnsiColorName16NumericValues =>
        Enum.GetValues<ColorName16> ()
            .Select (e => new object [] { ((int)e).ToString () });
    [Theory]
    [MemberData (nameof (AnsiColorName16NumericValues))]
    public void TryParseColor_Accepts_Enum_UnderlyingNumbers (string numeric)
    {
        var resolver = new AnsiColorNameResolver ();

        bool success = resolver.TryParseColor (numeric, out _);

        Assert.True (success, $"Expected numeric enum value '{numeric}' to resolve successfully.");
    }



    [Fact]
    public void GetNames_Returns16ColorNames ()
    {
        string [] expected = Enum.GetNames<ColorName16> ();

        string [] actual = _candidate.GetColorNames ().ToArray ();

        Assert.Equal (expected, actual);
    }

    [Theory]
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
    public void TryNameColor_ReturnsExpectedColorName (byte r, byte g, byte b, string expectedName)
    {
        var expected = (true, expectedName);

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
    // Case-insensitive
    [InlineData ("BRIGHTBLUE", 59, 120, 255)]
    [InlineData ("brightblue", 59, 120, 255)]
    public void TryParseColor_ReturnsExpectedColor (string inputName, byte r, byte g, byte b)
    {
        var expected = (true, new Color (r, g, b));

        bool actualSuccess = _candidate.TryParseColor (inputName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("12", 231, 72, 86)] // ColorName16.BrightRed
    public void TryParseColor_ResolvesValidEnumNumber (string inputName, byte r, byte g, byte b)
    {
        var expected = (true, new Color (r, g, b));

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
        var expected = (false, default (Color));

        bool actualSuccess = _candidate.TryParseColor (invalidName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("-12")]
    public void TryParseColor_FailsOnInvalidEnumNumber (string invalidName)
    {
        var expected = (false, default (Color));

        bool actualSuccess = _candidate.TryParseColor (invalidName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }
}
