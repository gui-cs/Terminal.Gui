#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text;
using Xunit.Abstractions;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeManagerTests (ITestOutputHelper output)
{
    [Fact]
    public void ResetToCurrentValues_Adds_Default_Theme ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);
            Assert.NotEmpty (ThemeManager.Themes!);

            ThemeManager.UpdateToCurrentValues ();

            Assert.NotEmpty (ThemeManager.Themes!);

            // Default theme exists
            Assert.NotNull (ThemeManager.Themes? [ThemeManager.DEFAULT_THEME_NAME]);
        }
        finally
        {
            Disable (resetToHardCodedDefaults: true);
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

        Enable (ConfigLocations.HardCoded);

        Assert.Equal (Settings! ["Theme"].PropertyValue, ThemeManager.Theme);
        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);

        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void Theme_Set_Sets ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);

        ThemeManager.Theme = "Test";
        Assert.Equal ("Test", ThemeManager.Theme);
        Assert.Equal (Settings! ["Theme"].PropertyValue, ThemeManager.Theme);
        Assert.Equal ("Test", Settings! ["Theme"].PropertyValue);

        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void Theme_ResetToHardCodedDefaults_Sets_To_Default ()
    {
        Assert.False (IsEnabled);
        Assert.Equal (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Theme);

        Enable (ConfigLocations.HardCoded);
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

        Enable (ConfigLocations.HardCoded);

        Assert.Single (ThemeManager.Themes!);

        // Use ConcurrentDictionary instead of a regular dictionary
        ThemeManager.Themes = new ConcurrentDictionary<string, ThemeScope> (
                                                                            new Dictionary<string, ThemeScope>
                                                                            {
                                                                                { "Default", new ThemeScope() },
                                                                                { "test", new ThemeScope() }
                                                                            },
                                                                            StringComparer.InvariantCultureIgnoreCase
                                                                           );

        Assert.Contains ("test", ThemeManager.Themes!);

        Disable (resetToHardCodedDefaults: true);
    }


    [Fact]
    public void Themes_Set_Throws_If_No_Default_Theme_In_Dictionary ()
    {
        Assert.False (IsEnabled);

        Enable (ConfigLocations.HardCoded);

        Assert.Single (ThemeManager.Themes!);

        Assert.Throws<InvalidOperationException> (
                                                  () => ThemeManager.Themes = new ConcurrentDictionary<string, ThemeScope> (
                                                             new Dictionary<string, ThemeScope>
                                                             {
                                                                 { "test", new ThemeScope() }
                                                             },
                                                             StringComparer.InvariantCultureIgnoreCase
                                                            ));
        Assert.Single (ThemeManager.Themes!);

        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void Themes_Get () { }

    #endregion Tests Settings["Themes"] and ThemeManager.Themes

    [Fact]
    public void Themes_TryAdd_Adds ()
    {
        Enable (ConfigLocations.HardCoded);

        // Verify that the Themes dictionary contains only the Default theme
        Assert.Single (ThemeManager.Themes!);
        Assert.Contains (ThemeManager.DEFAULT_THEME_NAME, ThemeManager.Themes!);

        var theme = new ThemeScope ();
        theme.LoadHardCodedDefaults ();
        Assert.NotEmpty (theme);

        Assert.True (ThemeManager.Themes!.TryAdd ("testTheme", theme));
        Assert.Equal (2, ThemeManager.Themes.Count);

        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void Apply_Applies ()
    {
        Assert.False (IsEnabled);
        Enable (ConfigLocations.HardCoded);

        var theme = new ThemeScope ();
        theme.LoadHardCodedDefaults ();
        Assert.NotEmpty (theme);

        Assert.True (ThemeManager.Themes!.TryAdd ("testTheme", theme));
        Assert.Equal (2, ThemeManager.Themes.Count);

        Assert.Equal (LineStyle.Rounded, FrameView.DefaultBorderStyle);
        theme ["FrameView.DefaultBorderStyle"].PropertyValue = LineStyle.Double; // default is Single

        ThemeManager.Theme = "testTheme";
        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();

        Assert.Equal (LineStyle.Double, FrameView.DefaultBorderStyle);

        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void Theme_Reload_Consistency ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);

            // BUGBUG: Setting Schemes to empty array is not valid!
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
            ThrowOnJsonErrors = true;
            Load (ConfigLocations.Runtime);
            Assert.Equal ("TestTheme", ThemeManager.Theme);

            // Now reset everything and reload
            ResetToCurrentValues ();

            // Verify we're back to default
            Assert.Equal ("Default", ThemeManager.Theme);
        }
        finally
        {
            Disable (resetToHardCodedDefaults: true);
        }
    }

    [Fact]
    public void In_Memory_Themes_Size_Is_Reasonable ()
    {
        output.WriteLine ($"Start: Color size: {(MemorySizeEstimator.EstimateSize (Color.Red))} b");

        output.WriteLine ($"Start: Attribute size: {(MemorySizeEstimator.EstimateSize (Attribute.Default))} b");

        output.WriteLine ($"Start: Base Scheme size: {(MemorySizeEstimator.EstimateSize (Scheme.GetHardCodedSchemes ()))} b");

        output.WriteLine ($"Start: PropertyInfo size: {(MemorySizeEstimator.EstimateSize (ConfigurationManager.Settings! ["Application.QuitKey"]))} b");

        ThemeScope themeScope = new ThemeScope ();
        output.WriteLine ($"Start: ThemeScope ({themeScope.Count}) size: {(MemorySizeEstimator.EstimateSize (themeScope))} b");

        themeScope.AddValue ("Schemes", Scheme.GetHardCodedSchemes ());
        output.WriteLine ($"Start: ThemeScope ({themeScope.Count}) size: {(MemorySizeEstimator.EstimateSize (themeScope))} b");

        output.WriteLine ($"Start: HardCoded Schemes ({SchemeManager.Schemes!.Count}) size: {(MemorySizeEstimator.EstimateSize (SchemeManager.Schemes!))} b");

        output.WriteLine ($"Start: Themes dictionary ({ThemeManager.Themes!.Count}) size: {(MemorySizeEstimator.EstimateSize (ThemeManager.Themes!)) / 1024} Kb");

        Enable (ConfigLocations.HardCoded);

        output.WriteLine ($"Enabled: Themes dictionary ({ThemeManager.Themes.Count}) size: {(MemorySizeEstimator.EstimateSize (ThemeManager.Themes!)) / 1024} Kb");

        Load (ConfigLocations.LibraryResources);
        output.WriteLine ($"After Load: Themes dictionary ({ThemeManager.Themes!.Count}) size: {(MemorySizeEstimator.EstimateSize (ThemeManager.Themes!)) / 1024} Kb");

        output.WriteLine ($"Total Settings Size: {(MemorySizeEstimator.EstimateSize (Settings!)) / 1024} Kb");

        string json = ConfigurationManager.SourcesManager?.ToJson (Settings)!;

        // In memory size should be less than the size of the json
        output.WriteLine ($"JSON size: {json.Length / 1024} Kb");

        Assert.True (70000 > MemorySizeEstimator.EstimateSize (ThemeManager.Themes!), $"In memory size ({(MemorySizeEstimator.EstimateSize (Settings!)) / 1024} Kb) is > json size ({json.Length / 1024} Kb)");

        Disable (resetToHardCodedDefaults: true);
    }
}