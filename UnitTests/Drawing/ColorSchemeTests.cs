using System.Reflection;

namespace Terminal.Gui.DrawingTests;

public class ColorSchemeTests {
    [Fact]
    public void Colors_ColorSchemes_Built_Ins () {
        Colors.Reset ();
        Dictionary<string, ColorScheme> schemes = Colors.ColorSchemes;
        Assert.NotNull (schemes);
        Assert.Equal (5, schemes.Count);
        Assert.True (schemes.ContainsKey ("TopLevel"));
        Assert.True (schemes.ContainsKey ("Base"));
        Assert.True (schemes.ContainsKey ("Dialog"));
        Assert.True (schemes.ContainsKey ("Menu"));
        Assert.True (schemes.ContainsKey ("Error"));
    }

    [Fact]
    public void Colors_ColorSchemes_Property_Has_Private_Setter () {
        // Resharper Code Cleanup likes to remove the `private set; `
        // from the ColorSchemes property.  This test will fail if
        // that happens.
        PropertyInfo property = typeof (Colors).GetProperty ("ColorSchemes");
        Assert.NotNull (property);
        Assert.NotNull (property.SetMethod);
        Assert.True (property.GetSetMethod (true).IsPrivate);
    }

    [Fact]
    public void ColorScheme_New () {
        var scheme = new ColorScheme ();
        var lbl = new Label ();
        lbl.ColorScheme = scheme;
        lbl.Draw ();
    }
}
