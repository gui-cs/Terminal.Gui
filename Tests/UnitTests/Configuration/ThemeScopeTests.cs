#nullable enable
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeScopeTests
{
    [Fact]
    public void Load_AllThemesPresent ()
    {
        Enable (ConfigLocations.HardCoded);

        Load (ConfigLocations.All);
        Assert.True (ThemeManager.Themes!.ContainsKey ("Default"));
        Assert.True (ThemeManager.Themes.ContainsKey ("Dark"));
        Assert.True (ThemeManager.Themes.ContainsKey ("Light"));
        Disable (true);
    }

    [Fact]
    public void Apply_ShouldApplyUpdatedProperties ()
    {
        Enable (ConfigLocations.HardCoded);
        Assert.NotEmpty (ThemeManager.Themes!);

        Alignment savedButtonAlignment = Dialog.DefaultButtonAlignment;
        Alignment newButtonAlignment = Alignment.Center != savedButtonAlignment ? Alignment.Center : Alignment.Start;
        ThemeManager.GetCurrentTheme () ["Dialog.DefaultButtonAlignment"].PropertyValue = newButtonAlignment;

        LineStyle savedBorderStyle = Dialog.DefaultBorderStyle;
        var newBorderStyle = LineStyle.HeavyDotted;
        ThemeManager.GetCurrentTheme () ["Dialog.DefaultBorderStyle"].PropertyValue = newBorderStyle;

        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();
        Assert.Equal (newButtonAlignment, Dialog.DefaultButtonAlignment);
        Assert.Equal (newBorderStyle, Dialog.DefaultBorderStyle);

        // Replace with the savedValue to avoid failures on other unit tests that rely on the default value
        ThemeManager.GetCurrentTheme () ["Dialog.DefaultButtonAlignment"].PropertyValue = savedButtonAlignment;
        ThemeManager.GetCurrentTheme () ["Dialog.DefaultBorderStyle"].PropertyValue = savedBorderStyle;
        ThemeManager.GetCurrentTheme ().Apply ();
        Assert.Equal (savedButtonAlignment, Dialog.DefaultButtonAlignment);
        Assert.Equal (savedBorderStyle, Dialog.DefaultBorderStyle);
        Disable (true);
    }

    [Fact]
    public void UpdateToHardCodedDefaults_Resets_Config_Does_Not_Apply ()
    {
        Enable (ConfigLocations.HardCoded);

        Load (ConfigLocations.LibraryResources);

        Assert.Equal ("Default", ThemeManager.Theme);
        ThemeManager.Theme = "Dark";
        Assert.Equal ("Dark", ThemeManager.Theme);
        Apply ();
        Assert.Equal ("Dark", ThemeManager.Theme);

        // Act
        ThemeManager.LoadHardCodedDefaults ();
        Assert.Equal ("Default", ThemeManager.Theme);

        Disable (true);
    }

    [Fact]
    public void Serialize_Themes_RoundTrip ()
    {
        Enable (ConfigLocations.HardCoded);

        IDictionary<string, ThemeScope> initial = ThemeManager.Themes!;

        string serialized = JsonSerializer.Serialize (ThemeManager.Themes, SerializerContext.Options);

        ConcurrentDictionary<string, ThemeScope>? deserialized =
            JsonSerializer.Deserialize<ConcurrentDictionary<string, ThemeScope>> (serialized, SerializerContext.Options);

        Assert.NotEqual (initial, deserialized);
        Assert.Equal (deserialized!.Count, initial!.Count);

        Disable (true);
    }

    [Fact]
    public void Serialize_New_RoundTrip ()
    {
        Enable (ConfigLocations.HardCoded);

        var theme = new ThemeScope ();
        theme.LoadHardCodedDefaults ();
        theme ["Dialog.DefaultButtonAlignment"].PropertyValue = Alignment.End;

        string json = JsonSerializer.Serialize (theme, SerializerContext.Options);

        var deserialized = JsonSerializer.Deserialize<ThemeScope> (json, SerializerContext.Options);

        Assert.Equal (
                      Alignment.End,
                      (Alignment)deserialized! ["Dialog.DefaultButtonAlignment"].PropertyValue!
                     );

        Disable (true);
    }

    [Fact (Skip = "Temp work arounds for #4288 prevent corruption.")]
    public void UpdateFrom_Corrupts_Schemes_HardCodeDefaults ()
    {
        // BUGBUG: ThemeScope is broken and needs to be fixed to not have the hard coded schemes get overwritten.
        // BUGBUG: This test demonstrates the problem.
        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/4288

        // Create a test theme
        var json = """
                   {
                      "Schemes": [
                       {
                         "Base": {
                           "Normal": {
                             "Foreground": "White",
                             "Background": "Blue"
                           }
                         }
                       }
                      ]
                   }
                   """;

        //var json = """
        //           {
        //                "Themes": [
        //                  {
        //                    "Default": {
        //                      "Schemes": [
        //                           {
        //                             "Base": {
        //                               "Normal": {
        //                                 "Foreground": "White",
        //                                 "Background": "Blue"
        //                               }
        //                             }
        //                           }
        //                          ]
        //                        }
        //                    }
        //            ]
        //           }
        //           """;

        try
        {
            Assert.False (IsEnabled);
            ThrowOnJsonErrors = true;
           // Enable (ConfigLocations.HardCoded);
            //ResetToCurrentValues ();

            // Capture dynamically created hardCoded hard-coded scheme colors
            ImmutableSortedDictionary<string, Scheme> hardCodedSchemes = SchemeManager.GetHardCodedSchemes ()!;
            Color hardCodedBaseNormalFg = hardCodedSchemes ["Base"].Normal.Foreground;
            Assert.Equal (new Color (StandardColor.LightBlue).ToString (), hardCodedBaseNormalFg.ToString ());

            // Capture hard-coded scheme colors via cache
            Dictionary<string, Scheme>? hardCodedSchemesViaCache =
                GetHardCodedConfigPropertiesByScope ("ThemeScope")!.ToFrozenDictionary () ["Schemes"].PropertyValue as Dictionary<string, Scheme>;
            Assert.Equal (hardCodedBaseNormalFg.ToString (), hardCodedSchemesViaCache! ["Base"].Normal.Foreground.ToString ());

            // (ConfigLocations.HardCoded);

            // Capture current scheme 
            Dictionary<string, Scheme> currentSchemes = SchemeManager.GetSchemes ()!;
            Color currentBaseNormalFg = currentSchemes ["Base"].Normal.Foreground;
            Assert.Equal (hardCodedBaseNormalFg.ToString (), currentBaseNormalFg.ToString ());

            //ConfigurationManager.SourcesManager?.Load (Settings, json, "UpdateFromJson", ConfigLocations.Runtime);

            ThemeScope scope = (JsonSerializer.Deserialize (json, typeof (ThemeScope), SerializerContext.Options) as ThemeScope)!;

            ThemeScope defaultTheme = ThemeManager.Themes! ["Default"]!;
            Dictionary<string, Scheme?> schemesScope = (defaultTheme ["Schemes"].PropertyValue as Dictionary<string, Scheme?>)!;
            defaultTheme ["Schemes"].UpdateFrom (scope ["Schemes"].PropertyValue!);
            defaultTheme.UpdateFrom (scope);

            Assert.Equal (Color.White.ToString (), hardCodedSchemesViaCache! ["Base"].Normal.Foreground.ToString ());
        }
        finally
        {
            ResetToHardCodedDefaults ();
        }
    }
}
