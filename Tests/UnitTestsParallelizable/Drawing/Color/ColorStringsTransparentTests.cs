namespace DrawingTests.ColorTests;

public class ColorStringsTransparentTests
{
    [Fact]
    public void GetColorName_ReturnsTransparent_ForTransparentColor ()
    {
        string? name = ColorStrings.GetColorName (Color.Transparent);

        Assert.Equal ("Transparent", name);
    }

    [Fact]
    public void GetColorName_Transparent_TakesPriority_OverAlphaIgnoringMatch ()
    {
        // Color.Transparent has RGB (255, 255, 255) which would match "White" if alpha is ignored.
        // But the Transparent check should come first.
        string? name = ColorStrings.GetColorName (Color.Transparent);

        Assert.Equal ("Transparent", name);
    }

    [Theory]
    [InlineData ("Transparent")]
    [InlineData ("transparent")]
    [InlineData ("TRANSPARENT")]
    public void TryParseStandardColorName_ParsesTransparent_CaseInsensitively (string colorName)
    {
        bool result = ColorStrings.TryParseStandardColorName (colorName, out Color color);

        Assert.True (result);
        Assert.Equal (Color.Transparent, color);
    }

    [Theory]
    [InlineData ("Transparent")]
    [InlineData ("transparent")]
    [InlineData ("TRANSPARENT")]
    public void TryParseNamedColor_ParsesTransparent_CaseInsensitively (string colorName)
    {
        bool result = ColorStrings.TryParseNamedColor (colorName, out Color color);

        Assert.True (result);
        Assert.Equal (Color.Transparent, color);
    }

    [Fact]
    public void TryParseStandardColorName_Transparent_HasAlphaZero ()
    {
        ColorStrings.TryParseStandardColorName ("Transparent", out Color color);

        Assert.Equal (0, color.A);
    }

    [Fact]
    public void TryParseNamedColor_Transparent_HasAlphaZero ()
    {
        ColorStrings.TryParseNamedColor ("Transparent", out Color color);

        Assert.Equal (0, color.A);
    }

    [Fact]
    public void GetColorName_And_TryParseNamedColor_RoundTrip_Transparent ()
    {
        // Get the name
        string? name = ColorStrings.GetColorName (Color.Transparent);
        Assert.NotNull (name);
        Assert.Equal ("Transparent", name);

        // Parse it back
        bool result = ColorStrings.TryParseNamedColor (name, out Color parsedColor);
        Assert.True (result);
        Assert.Equal (Color.Transparent, parsedColor);
    }

    [Fact]
    public void GetStandardColorNames_DoesNotContain_Transparent ()
    {
        // Transparent is not a W3C standard color — it should NOT appear in the enum-based list
        IEnumerable<string> names = ColorStrings.GetStandardColorNames ();
        string [] namesArray = names.ToArray ();

        Assert.DoesNotContain ("Transparent", namesArray);
    }

    [Fact]
    public void TryParseStandardColorName_Transparent_WorksWithReadOnlySpan ()
    {
        ReadOnlySpan<char> span = "Transparent".AsSpan ();
        bool result = ColorStrings.TryParseStandardColorName (span, out Color color);

        Assert.True (result);
        Assert.Equal (Color.Transparent, color);
    }

    [Fact]
    public void TryParseNamedColor_Transparent_WorksWithReadOnlySpan ()
    {
        ReadOnlySpan<char> span = "transparent".AsSpan ();
        bool result = ColorStrings.TryParseNamedColor (span, out Color color);

        Assert.True (result);
        Assert.Equal (Color.Transparent, color);
    }
}
