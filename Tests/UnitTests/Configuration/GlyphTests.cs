using System.Reflection;
using System.Text;
using System.Text.Json;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class GlyphTests
{
    [Fact]
    public void Overrides_Defaults ()
    {
        // arrange
        Locations = ConfigLocations.Default;
        Load (true);

        Assert.Equal ((Rune)'⟦', Glyphs.LeftBracket);

        var glyph = (Rune)Themes ["Default"] ["Glyphs.LeftBracket"].PropertyValue;
        Assert.Equal ((Rune)'⟦', glyph);

        ThrowOnJsonErrors = true;

        // act
        var json = """
                   {
                       "Themes": [
                           {
                             "Default": 
                                {
                                    "Glyphs.LeftBracket": "["
                                }
                           }
                       ]
                   }
                   """;

        Settings!.Update (json, "Overrides_Defaults", ConfigLocations.Runtime);
        Apply();

        // assert
        glyph = glyph = (Rune)Themes ["Default"] ["Glyphs.LeftBracket"].PropertyValue;
        Assert.Equal ((Rune)'[', glyph);
        Assert.Equal((Rune)'[', Glyphs.LeftBracket);

        // clean up
        Reset ();
    }
}
