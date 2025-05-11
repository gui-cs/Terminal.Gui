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
        Enable ();
        Load (ConfigLocations.LibraryResources);


        Assert.Equal ((Rune)'⟦', Glyphs.LeftBracket);

        var glyph = (Rune)ThemeManager.Themes ["Default"] ["Glyphs.LeftBracket"].PropertyValue;
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

        CM.SourcesManager?.Load(Settings, json, "Overrides_Defaults", ConfigLocations.Runtime);
        Apply();

        // assert
        glyph = glyph = (Rune)ThemeManager.Themes ["Default"] ["Glyphs.LeftBracket"].PropertyValue;
        Assert.Equal ((Rune)'[', glyph);
        Assert.Equal((Rune)'[', Glyphs.LeftBracket);

        // clean up
        Disable ();
        ResetToHardCodedDefaults ();
    }
}
