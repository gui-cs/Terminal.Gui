#nullable enable
using System.Diagnostics.Metrics;
using System.Text;
using Xunit.Abstractions;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeManagerTests (ITestOutputHelper output)
{
    [Fact]
    public void ResetToCurrentValues_Adds_Default_Theme ()
    {
        try
        {
            Enable ();
            Assert.NotEmpty (ThemeManager.Themes!);

            ThemeManager.UpdateToCurrentValues ();

            Assert.NotEmpty (ThemeManager.Themes!);

            // Default theme exists
            Assert.NotNull (ThemeManager.Themes? [ThemeManager.DEFAULT_THEME_NAME]);

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
            ResetToCurrentValues ();
        }
    }

    // ResetToCurrentValues

    //    OnThemeChanged

    #region Tests Settings["Theme"] and ThemeManager.Theme

    [Fact]
    public void Theme_Settings_Theme_Equals_ThemeManager_Theme ()
    {
        Assert.False (IsEnabled);

        Assert.Equal (Settings! ["Theme"].PropertyValue, ThemeManager.Theme);
        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);
    }

    [Fact]
    public void Theme_Enabled_Settings_Theme_Equals_ThemeManager_Theme ()
    {
        Assert.False (IsEnabled);

        Enable ();
        ResetToHardCodedDefaults ();

        Assert.Equal (Settings! ["Theme"].PropertyValue, ThemeManager.Theme);
        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);

        ResetToHardCodedDefaults ();
        Disable ();
    }

    [Fact]
    public void Theme_Set_Sets ()
    {
        Assert.False (IsEnabled);

        Enable ();
        ResetToHardCodedDefaults ();

        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);

        ThemeManager.Theme = "Test";
        Assert.Equal ("Test", ThemeManager.Theme);
        Assert.Equal (Settings! ["Theme"].PropertyValue, ThemeManager.Theme);
        Assert.Equal ("Test", Settings! ["Theme"].PropertyValue);

        ResetToHardCodedDefaults ();
        Disable ();
    }

    [Fact]
    public void Theme_Set_Throws_If_Not_Enabled ()
    {
        Assert.False (IsEnabled);

        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);
        Assert.Throws<InvalidOperationException> (() => ThemeManager.Theme = "Test");
        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);
    }

    [Fact]
    public void Theme_ResetToHardCodedDefaults_Sets_To_Default ()
    {
        Assert.False (IsEnabled);
        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);

        Enable ();
        ResetToHardCodedDefaults ();
        Assert.Equal ("Default", ThemeManager.Theme);

        ThemeManager.Theme = "Test";
        Assert.Equal ("Test", ThemeManager.Theme);
        Assert.Equal (Settings! ["Theme"].PropertyValue, ThemeManager.Theme);
        Assert.Equal ("Test", Settings! ["Theme"].PropertyValue);

        ResetToHardCodedDefaults ();
        Assert.Equal ("Default", ThemeManager.Theme);
        Disable ();
    }

    #endregion Tests Settings["Theme"] and ThemeManager.Theme

    #region Tests Settings["Themes"] and ThemeManager.Themes

    [Fact]
    public void Themes_Set_Throws_If_Not_Enabled ()
    {
        Assert.False (IsEnabled);

        Assert.Single (ThemeManager.Themes!);
        Assert.Throws<InvalidOperationException> (() => ThemeManager.Themes = new ());
        Assert.Single (ThemeManager.Themes!);
    }

    [Fact]
    public void Themes_Set_Sets_If_Enabled ()
    {
        Assert.False (IsEnabled);

        Enable ();
        ResetToHardCodedDefaults ();

        Assert.Single (ThemeManager.Themes!);

        ThemeManager.Themes = new ()
        {
            { "Default", new () },
            { "test", new () }
        };
        Assert.Contains ("test", ThemeManager.Themes!);

        ResetToHardCodedDefaults ();
        Disable ();
    }

    [Fact]
    public void Themes_Set_Throws_If_No_Default_Theme_In_Dictionary ()
    {
        Assert.False (IsEnabled);

        Enable ();
        ResetToHardCodedDefaults ();

        Assert.Single (ThemeManager.Themes!);

        Assert.Throws<InvalidOperationException> (
                                                  () => ThemeManager.Themes = new ()
                                                  {
                                                      { "not default", new () },
                                                      { "test", new () }
                                                  });
        Assert.Single (ThemeManager.Themes!);

        ResetToHardCodedDefaults ();
        Disable ();
    }

    [Fact]
    public void Themes_Get () { }

    #endregion Tests Settings["Themes"] and ThemeManager.Themes

    [Fact]
    public void Apply_Applies ()
    {
        Enable ();
        ResetToCurrentValues ();

        var theme = new ThemeScope ();
        Assert.NotEmpty (theme);

        ThemeManager.Themes!.Add ("testTheme", theme);

        Assert.Equal (LineStyle.Single, FrameView.DefaultBorderStyle);
        theme ["FrameView.DefaultBorderStyle"].PropertyValue = LineStyle.Double; // default is Single

        ThemeManager.Theme = "testTheme";
        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();

        Assert.Equal (LineStyle.Double, FrameView.DefaultBorderStyle);

        ResetToHardCodedDefaults ();
        Disable ();
    }

    [Fact]
    public void Theme_Reload_Consistency ()
    {
        try
        {
            Enable ();

            // First load with a custom theme
            //  Locations = ConfigLocations.Runtime;
            ResetToCurrentValues ();

            // Create a test theme
            RuntimeConfig = """
                            {
                                 "Theme": "TestTheme",
                                 "Themes": [
                                   {
                                     "TestTheme": {
                                       "Schemes": []
                                     }
                                   }
                                 ]
                            }
                            """;

            // Load the test theme
            Load (ConfigLocations.Runtime);
            Assert.Equal ("TestTheme", ThemeManager.Theme);

            // Now reset everything and reload
            //  Locations = ConfigLocations.HardCoded;
            ResetToCurrentValues ();

            // Verify we're back to default
            Assert.Equal ("Default", ThemeManager.Theme);
        }
        finally
        {
            ResetToCurrentValues ();
            Disable ();
        }
    }

    [Fact]
    public void In_Memory_Themes_Size_Is_Reasonable ()
    {
        output.WriteLine ($"Start: Themes dictionary size: {(MemorySizeEstimator.EstimateSize (ThemeManager.Themes!)) / 1024} Kb");
        Enable ();
        output.WriteLine ($"After Enable: Themes dictionary size: {(MemorySizeEstimator.EstimateSize (ThemeManager.Themes!)) / 1024} Kb");

        ResetToHardCodedDefaults ();
        Assert.Single (ThemeManager.Themes!);
        output.WriteLine ($"After ResetToHardCodedDefaults: Themes dictionary size: {(MemorySizeEstimator.EstimateSize (ThemeManager.Themes!)) / 1024} Kb");

        Load (ConfigLocations.LibraryResources);
        Assert.Equal (6, ThemeManager.Themes!.Count);
        output.WriteLine ($"After Load: Themes dictionary size: {(MemorySizeEstimator.EstimateSize (ThemeManager.Themes!)) / 1024} Kb");

        output.WriteLine ($"Total Settings Size: {(MemorySizeEstimator.EstimateSize (Settings!)) / 1024} Kb");

        // Assert that the size is within a reasonable range (e.g., less than 1 MB)
        //Assert.True (MemorySizeEstimator.EstimateSize (ThemeManager.Themes!) < (64 * 1024), $"Themes dictionary size is too large: {MemorySizeEstimator.EstimateSize (ThemeManager.Themes!) / 1024} Kb");

        ResetToHardCodedDefaults ();
        Disable ();
    }
}