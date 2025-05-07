#nullable enable
using System.Diagnostics;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeManagerTests
{
   
    [Fact]
    public void Reset_Adds_Default_Theme ()
    {
        try
        {
            CM.Enable();
            Assert.NotEmpty (ThemeManager.Themes!);

            ThemeManager.Reset ();

            Assert.NotEmpty (ThemeManager.Themes!);

            // Default theme exists
            Assert.NotNull (ThemeManager.Themes? ["Default"]);

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
        finally
        {
            CM.Reset ();
        }

    }

    // ResetToCurrentValues

//    OnThemeChanged

    [Fact]
    public void SelectedTheme_Set ()
    {
       
    }

    [Fact]
    public void SelectedTheme_Get ()
    {

    }


    [Fact]
    public void Themes_Set ()
    {

    }

    [Fact]
    public void Themes_Get ()
    {

    }
}
