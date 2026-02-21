namespace DrawingTests.ColorTests;

public class ColorTransparentTests
{
    [Fact]
    public void Transparent_HasAlphaZero ()
    {
        Assert.Equal (0, Color.Transparent.A);
    }

    [Fact]
    public void Transparent_HasRgbWhite ()
    {
        Assert.Equal (255, Color.Transparent.R);
        Assert.Equal (255, Color.Transparent.G);
        Assert.Equal (255, Color.Transparent.B);
    }

    [Fact]
    public void Transparent_Argb_Is_0x00FFFFFF ()
    {
        // ARGB 0x00FFFFFF = alpha 0, R 255, G 255, B 255
        Assert.Equal (0x00FFFFFFu, Color.Transparent.Argb);
    }

    [Fact]
    public void Transparent_Is_Not_Default ()
    {
        // This is critical: Color.Transparent must be distinguishable from default(Color)
        // which is all-zeros (ARGB 0x00000000) since Color is a struct.
        Assert.NotEqual (default (Color), Color.Transparent);
    }

    [Fact]
    public void Transparent_Equals_Itself ()
    {
        Assert.Equal (Color.Transparent, Color.Transparent);
    }

    [Fact]
    public void Transparent_Equals_Manually_Constructed_Equivalent ()
    {
        Color manualTransparent = new (255, 255, 255, 0);
        Assert.Equal (Color.Transparent, manualTransparent);
    }

    [Fact]
    public void Transparent_Does_Not_Equal_Black ()
    {
        Color black = new (0, 0, 0);
        Assert.NotEqual (Color.Transparent, black);
    }

    [Fact]
    public void Transparent_Does_Not_Equal_White ()
    {
        // White has alpha 255 (opaque), Transparent has alpha 0
        Color white = new (255, 255, 255);
        Assert.NotEqual (Color.Transparent, white);
    }

    [Fact]
    public void Default_Color_Has_All_Zeros ()
    {
        Color defaultColor = default;
        Assert.Equal (0, defaultColor.R);
        Assert.Equal (0, defaultColor.G);
        Assert.Equal (0, defaultColor.B);
        Assert.Equal (0, defaultColor.A);
        Assert.Equal (0u, defaultColor.Argb);
    }
}
