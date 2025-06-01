using System.Reflection;
using System.Text;
using System.Text.Json;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class GlyphTests
{
    [Fact]
    public void Apply_Applies_Over_Defaults ()
    {
        // arrange
        Enable (ConfigLocations.HardCoded);

        Assert.Equal ((Rune)'⟦', Glyphs.LeftBracket);

        var glyph = (Rune)ThemeManager.GetCurrentTheme () ["Glyphs.LeftBracket"].PropertyValue!;
        Assert.Equal ((Rune)'⟦', glyph);

        ThrowOnJsonErrors = true;

        // act
        RuntimeConfig = """
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

        Load (ConfigLocations.Runtime);
        Apply ();

        // assert
        glyph = (Rune)ThemeManager.GetCurrentTheme () ["Glyphs.LeftBracket"].PropertyValue!;
        Assert.Equal ((Rune)'[', glyph);
        Assert.Equal ((Rune)'[', Glyphs.LeftBracket);

        // clean up
        Disable (resetToHardCodedDefaults: true);
    }
}
