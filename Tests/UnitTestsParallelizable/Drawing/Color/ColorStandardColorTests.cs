using Xunit;

namespace Terminal.Gui.DrawingTests;

public class ColorStandardColorTests
{
    [Fact]
    public void ToString_Returns_Standard_Name_For_StandardColor_CadetBlue()
    {
        // Without the fix, this uses Color(in StandardColor) -> this((int)colorName),
        // which sets A=0x00 and prevents name resolution (expects A=0xFF).
        var c = new Terminal.Gui.Drawing.Color(Terminal.Gui.Drawing.StandardColor.CadetBlue);

        // Expected: named color
        Assert.Equal("CadetBlue", c.ToString());
    }

    [Fact]
    public void ToString_G_Prints_Opaque_ARGB_For_StandardColor_CadetBlue()
    {
        // Without the fix, A=0x00, so "G" prints "#005F9EA0" instead of "#FF5F9EA0".
        var c = new Terminal.Gui.Drawing.Color(Terminal.Gui.Drawing.StandardColor.CadetBlue);

        // Expected: #AARRGGBB with A=FF (opaque)
        Assert.Equal("#FF5F9EA0", c.ToString("G", null));
    }
}