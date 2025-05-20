using System.Diagnostics;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeScopeTests
{
    [Fact]
    public void Schemes_Property_Exists ()
    {
        var scope = new ThemeScope ();
        scope.LoadHardCodedDefaults();

        Assert.NotEmpty (scope);

        Assert.NotNull(scope ["Schemes"].PropertyValue);

        Assert.NotEmpty (scope);
    }

    //[Fact]
    //public void RetrievValues_Gets_Default_Values ()
    //{
    //    // Need to call Initialize to setup readonly statics
    //    ConfigurationManager.Initialize ();

    //    var themeScope = new ThemeScope ();

    //    Assert.NotEmpty (themeScope);

    //    // Schemes exists, but is not initialized
    //    Assert.Null (themeScope ["Schemes"].PropertyValue);

    //    themeScope.RetrieveValues ();

    //    Assert.NotEmpty (themeScope);

    //    // Schemes exists, and has correct # of eleements
    //    var schemes = themeScope ["Schemes"].PropertyValue as Dictionary<string, Scheme>;
    //    Assert.NotNull (schemes);
    //    Assert.Equal (5, schemes!.Count);

    //    // Base has correct values
    //    var baseSchemee = schemes ["Base"];
    //    Assert.Equal (new Attribute(Color.White, Color.Blue), baseSchemee.Normal);

    //}
}
