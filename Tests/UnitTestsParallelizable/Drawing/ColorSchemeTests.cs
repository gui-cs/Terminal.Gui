using System.Reflection;

namespace Terminal.Gui.DrawingTests;

public class SchemeTests
{
    [Fact]
    public void Schemes_Built_Ins ()
    {
        Dictionary<string, Scheme> schemes = SchemeManager.GetSchemes ();
        Assert.NotNull (schemes);
        Assert.Equal (5, schemes.Count);
        Assert.True (schemes.ContainsKey ("TopLevel"));
        Assert.True (schemes.ContainsKey ("Base"));
        Assert.True (schemes.ContainsKey ("Dialog"));
        Assert.True (schemes.ContainsKey ("Menu"));
        Assert.True (schemes.ContainsKey ("Error"));
    }

    [Fact]
    public void Colors_Schemes_Property_Has_Private_Setter ()
    {
        // Resharper Code Cleanup likes to remove the `private set; `
        // from the Schemes property.  This test will fail if
        // that happens.
        PropertyInfo property = typeof (SchemeManager).GetProperty ("Schemes");
        Assert.NotNull (property);
        Assert.NotNull (property.SetMethod);
        Assert.True (property.GetSetMethod (true).IsPrivate);
    }

    [Fact]
    public void Scheme_New ()
    {
        var scheme = new Scheme ();
        var lbl = new Label ();
        lbl.SetScheme (scheme);
        lbl.Draw ();
    }
}
