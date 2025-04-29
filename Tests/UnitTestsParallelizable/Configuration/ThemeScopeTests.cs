using System.Diagnostics;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeScopeTests
{
    [Fact]
    public void CM_AllConfigurationProperties_Null ()
    {
        Assert.Null (ConfigurationManager._allConfigProperties);
    }

    [Fact]
    public void Constructor_Initializes ()
    {
        CM_AllConfigurationProperties_Null ();

        var themeScope = new ThemeScope ();
        CM_AllConfigurationProperties_Null ();

        Assert.NotEmpty (themeScope);

        // Schemes exists, but is not initialized
        Assert.Null (themeScope ["Schemes"].PropertyValue);
    }


    [Fact]
    public void RetrievValues_Gets_Default_Values ()
    {
        CM_AllConfigurationProperties_Null ();

        var themeScope = new ThemeScope ();
        CM_AllConfigurationProperties_Null ();

        Assert.NotEmpty (themeScope);

        // Schemes exists, but is not initialized
        Assert.Null (themeScope ["Schemes"].PropertyValue);

        themeScope.RetrieveValues ();
        CM_AllConfigurationProperties_Null ();

        Assert.NotEmpty (themeScope);

        // Schemes exists, and has correct # of eleements
        var schemes = themeScope ["Schemes"].PropertyValue as Dictionary<string, Scheme>;
        Assert.NotNull (schemes);
        Assert.Equal (5, schemes!.Count);

        // Base has correct values
        var baseSchemee = schemes ["Base"];
        Assert.Equal (new Attribute(Color.White, Color.Blue), baseSchemee.Normal);

    }
}
