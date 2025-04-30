using System.Diagnostics;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeManagerTests
{
    [Fact]
    public void Intialize_Clears ()
    {
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();

        Assert.Empty (ConfigurationManager.ThemeManager!);

    }

    [Fact]
    public void Reset_Adds_Default_Theme ()
    {
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();

        ConfigurationManager.ThemeManager!.Reset ();

        Assert.NotEmpty (ConfigurationManager.ThemeManager!);

        // Default theme exists
        Assert.NotNull (ConfigurationManager.ThemeManager ["Default"]);

        //// Schemes exists, but is not initialized
        //Assert.Null (manager ["Default"].);

        //manager.RetrieveValues ();

        //Assert.NotEmpty (manager);

        //// Schemes exists, and has correct # of eleements
        //var schemes = manager ["Schemes"].PropertyValue as Dictionary<string, Scheme>;
        //Assert.NotNull (schemes);
        //Assert.Equal (5, schemes!.Count);

        //// Base has correct values
        //var baseSchemee = schemes ["Base"];
        //Assert.Equal (new Attribute (Color.White, Color.Blue), baseSchemee.Normal);

    }
}
