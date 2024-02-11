namespace Terminal.Gui.FileServicesTests;

public class NerdFontTests
{
    [Fact]
    public void TestAllExtensionsMapToKnownGlyphs ()
    {
        var f = new NerdFonts ();

        foreach (KeyValuePair<string, string> k in f.ExtensionToIcon)
        {
            Assert.Contains (k.Value, f.Glyphs.Keys);
        }
    }

    [Fact]
    public void TestAllFilenamesMapToKnownGlyphs ()
    {
        var f = new NerdFonts ();

        foreach (KeyValuePair<string, string> k in f.FilenameToIcon)
        {
            Assert.Contains (k.Value, f.Glyphs.Keys);
        }
    }
}
