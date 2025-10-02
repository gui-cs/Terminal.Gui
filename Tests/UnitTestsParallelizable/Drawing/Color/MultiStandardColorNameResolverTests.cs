#nullable enable

using System.Collections.Generic;
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests;

public class MultiStandardColorNameResolverTests (ITestOutputHelper output)
{
    private readonly MultiStandardColorNameResolver _candidate = new ();

    public static IEnumerable<object []> StandardColors =>
        Enum.GetValues<StandardColor> ().Select (sc => new object [] { sc });

    [Theory]
    [MemberData (nameof (StandardColors))]
    public void TryParseColor_ResolvesAllStandardColorNames (StandardColor standardColor)
    {
        string name = standardColor.ToString ();
        bool parsed = _candidate.TryParseColor (name, out Color actualColor);

        Assert.True (parsed, $"TryParseColor should succeed for {name}");

        Color expectedColor = new (Drawing.StandardColors.GetArgb (standardColor));

        Assert.Equal (expectedColor.R, actualColor.R);
        Assert.Equal (expectedColor.G, actualColor.G);
        Assert.Equal (expectedColor.B, actualColor.B);
    }

    [Theory]
    [MemberData (nameof (StandardColors))]
    public void TryNameColor_ResolvesAllStandardColors (StandardColor standardColor)
    {
        Color color = new (Drawing.StandardColors.GetArgb (standardColor));

        bool success = _candidate.TryNameColor (color, out string? resolvedName);

        if (!success)
        {
            output.WriteLine ($"Unmapped: {standardColor} → {color}");
        }

        Assert.True (success, $"TryNameColor should succeed for {standardColor}");

        List<string> expectedNames = Enum.GetNames<StandardColor> ()
            .Where (name => Drawing.StandardColors.GetArgb (Enum.Parse<StandardColor> (name)) == color.Argb)
            .ToList ();

        Assert.Contains (resolvedName, expectedNames);
    }

    [Fact]
    public void TryNameColor_Logs_Unmapped_StandardColors ()
    {
        List<StandardColor> unmapped = new ();

        foreach (StandardColor sc in Enum.GetValues<StandardColor> ())
        {
            Color color = new (Drawing.StandardColors.GetArgb (sc));
            if (!_candidate.TryNameColor (color, out _))
            {
                unmapped.Add (sc);
            }
        }

        output.WriteLine ("Unmapped StandardColor entries:");
        foreach (StandardColor sc in unmapped.Distinct ())
        {
            output.WriteLine ($"- {sc}");
        }

        Assert.True (unmapped.Count < 10, $"Too many StandardColor values are not name-resolvable. Got {unmapped.Count}.");
    }

    [Theory]
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
    [InlineData (nameof (StandardColor.AliceBlue))]
    [InlineData (nameof (StandardColor.BlanchedAlmond))]
    public void GetNames_ContainsKnownNames (string name)
    {
        string [] names = _candidate.GetColorNames ().ToArray ();
        Assert.Contains (name, names);
    }

    [Theory]
    [InlineData (0, 0, 0, nameof (ColorName16.Black))]
    [InlineData (0, 0, 255, nameof (ColorName16.Blue))]
    [InlineData (59, 120, 255, nameof (ColorName16.BrightBlue))]
    [InlineData (255, 0, 0, nameof (ColorName16.Red))]
    [InlineData (255, 255, 255, nameof (ColorName16.White))]
    [InlineData (240, 248, 255, nameof (StandardColor.AliceBlue))]
    [InlineData (178, 34, 34, nameof (StandardColor.FireBrick))]
    [InlineData (245, 245, 245, nameof (StandardColor.WhiteSmoke))]
    public void TryNameColor_ReturnsExpectedColorNames (byte r, byte g, byte b, string expectedName)
    {
        Color color = new (r, g, b);
        bool actualSuccess = _candidate.TryNameColor (color, out string? actualName);

        Assert.True (actualSuccess);
        Assert.Equal (expectedName, actualName);
    }

    [Fact]
    public void TryNameColor_NoMatchFails ()
    {
        Color input = new (1, 2, 3);
        bool success = _candidate.TryNameColor (input, out string? actualName);
        Assert.False (success);
        Assert.Null (actualName);
    }

    [Theory]
    [InlineData ("12", 231, 72, 86)] // ColorName16.BrightRed
    [InlineData ("16737095", 255, 99, 71)] // StandardColor.Tomato
    [InlineData ("#FF0000", 255, 0, 0)] // Red
    public void TryParseColor_ResolvesValidEnumNumber (string inputName, byte r, byte g, byte b)
    {
        bool success = _candidate.TryParseColor (inputName, out Color actualColor);
        Assert.True (success);
        Assert.Equal (r, actualColor.R);
        Assert.Equal (g, actualColor.G);
        Assert.Equal (b, actualColor.B);
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    [InlineData ("brightlight")]
    public void TryParseColor_FailsOnInvalidColorName (string? input)
    {
        bool success = _candidate.TryParseColor (input, out Color actualColor);
        Assert.False (success);
        Assert.Equal (default, actualColor);
    }

    [Theory]
    [InlineData ("-12")]
    [InlineData ("-16737095")]
    public void TryParseColor_FailsOnInvalidEnumNumber (string input)
    {
        bool success = _candidate.TryParseColor (input, out Color actualColor);
        Assert.False (success);
        Assert.Equal (default, actualColor);
    }
}
