#nullable enable
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace UnitTests.ConfigurationTests;

public class SettingsScopeTests
{
    [Fact]
    public void Load_Overrides_Defaults ()
    {
        // arrange
        Enable (ConfigLocations.HardCoded);

        Dictionary<Command, PlatformKeyBinding> defaultBindings =
            (Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!;
        Assert.NotNull (defaultBindings);
        Assert.Contains (Command.Quit, defaultBindings.Keys);

        ThrowOnJsonErrors = true;

        // act
        RuntimeConfig = """
                        {
                            "Application.DefaultKeyBindings": {
                                "Quit": { "All": ["Ctrl+Q"] }
                            }
                        }
                        """;

        Load (ConfigLocations.Runtime);

        // assert
        Dictionary<Command, PlatformKeyBinding> updatedBindings =
            (Dictionary<Command, PlatformKeyBinding>)Settings ["Application.DefaultKeyBindings"].PropertyValue!;
        Assert.NotNull (updatedBindings);
        Key [] quitKeys = updatedBindings [Command.Quit].All!;
        Assert.Single (quitKeys);
        Assert.Equal (Key.Q.WithCtrl, quitKeys [0]);

        // clean up
        Disable (true);
    }

    [Fact]
    public void Load_Dictionary_Property_Overrides_Defaults ()
    {
        // arrange
        Enable (ConfigLocations.HardCoded);
        ThrowOnJsonErrors = true;

        ConfigProperty themesConfigProperty = Settings! ["Themes"];
        ConcurrentDictionary<string, ThemeScope> dict = (themesConfigProperty.PropertyValue as ConcurrentDictionary<string, ThemeScope>)!;

        Assert.NotNull (dict);
        Assert.Single (dict);
        Assert.NotEmpty (((ConcurrentDictionary<string, ThemeScope>)themesConfigProperty.PropertyValue!)!);

        ThemeScope scope = dict [ThemeManager.DEFAULT_THEME_NAME];
        Assert.NotNull (scope);
        Assert.Equal (MouseState.In | MouseState.Pressed | MouseState.PressedOutside, scope ["Button.DefaultMouseHighlightStates"].PropertyValue);

        RuntimeConfig = """
                        {
                            "Themes": [
                                {
                                  "Default": 
                                     {
                                         "Button.DefaultMouseHighlightStates": "None"
                                     }
                                },
                                {
                                  "NewTheme":
                                    {
                                        "Button.DefaultMouseHighlightStates": "In"
                                    }
                                }                        
                            ]
                        }
                        """;

        Load (ConfigLocations.Runtime);

        // assert
        Assert.Equal (2, ThemeManager.Themes!.Count);
        Assert.Equal (MouseState.None, (MouseState)ThemeManager.GetCurrentTheme () ["Button.DefaultMouseHighlightStates"].PropertyValue!);
        Assert.Equal (MouseState.In, (MouseState)ThemeManager.Themes ["NewTheme"] ["Button.DefaultMouseHighlightStates"].PropertyValue!);

        RuntimeConfig = """
                        {
                            "Themes": [
                                {
                                  "Default": 
                                     {
                                         "Button.DefaultMouseHighlightStates": "Pressed"
                                     }
                                }
                            ]
                        }
                        """;
        Load (ConfigLocations.Runtime);

        // assert
        Assert.Equal (2, ThemeManager.Themes.Count);
        Assert.Equal (MouseState.Pressed, (MouseState)ThemeManager.Themes! [ThemeManager.DEFAULT_THEME_NAME] ["Button.DefaultMouseHighlightStates"].PropertyValue!);
        Assert.Equal (MouseState.In, (MouseState)ThemeManager.Themes! ["NewTheme"] ["Button.DefaultMouseHighlightStates"].PropertyValue!);

        // clean up
        Disable (true);
    }

    [Fact]
    public void Apply_ShouldApplyProperties ()
    {
        Enable (ConfigLocations.HardCoded);
        Load (ConfigLocations.LibraryResources);

        // arrange — verify default bindings are present
        Dictionary<Command, PlatformKeyBinding> bindings =
            (Dictionary<Command, PlatformKeyBinding>)Settings! ["Application.DefaultKeyBindings"].PropertyValue!;
        Assert.Contains (Command.Quit, bindings.Keys);

        // act — replace with new bindings
        Dictionary<Command, PlatformKeyBinding> newBindings = new ()
        {
            [Command.Quit] = Bind.All (Key.Q),
            [Command.NextTabGroup] = Bind.All (Key.F),
            [Command.PreviousTabGroup] = Bind.All (Key.B),
        };
        Settings ["Application.DefaultKeyBindings"].PropertyValue = newBindings;

        Settings.Apply ();

        // assert
        Assert.Equal (Key.Q, Application.GetDefaultKey (Command.Quit));
        Assert.Equal (Key.F, Application.GetDefaultKey (Command.NextTabGroup));
        Assert.Equal (Key.B, Application.GetDefaultKey (Command.PreviousTabGroup));

        Disable (true);
    }

    [Fact]
    public void CopyUpdatedPropertiesFrom_ShouldCopyChangedPropertiesOnly ()
    {
        Enable (ConfigLocations.HardCoded);

        // Set DefaultKeyBindings to a custom value
        Dictionary<Command, PlatformKeyBinding> customBindings = new ()
        {
            [Command.Quit] = Bind.All (Key.End),
        };
        Settings! ["Application.DefaultKeyBindings"].PropertyValue = customBindings;

        SettingsScope updatedSettings = new ();
        updatedSettings.LoadHardCodedDefaults ();

        // Mark DefaultKeyBindings as not having a value (should not overwrite)
        updatedSettings ["Application.DefaultKeyBindings"].HasValue = false;
        updatedSettings ["Application.DefaultKeyBindings"].PropertyValue = null;

        // Set a different property to verify it IS copied
        updatedSettings ["FileDialog.MaxSearchResults"].PropertyValue = 42;

        Settings.UpdateFrom (updatedSettings);

        // DefaultKeyBindings should still have our custom value
        Dictionary<Command, PlatformKeyBinding> result =
            (Dictionary<Command, PlatformKeyBinding>)Settings ["Application.DefaultKeyBindings"].PropertyValue!;
        Assert.Contains (Command.Quit, result.Keys);

        // MaxSearchResults should be updated
        Assert.Equal (42, Settings ["FileDialog.MaxSearchResults"].PropertyValue);

        Disable (true);
    }

    [Fact]
    public void ResetToHardCodedDefaults_Resets_Config_And_Applies ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);
            Load (ConfigLocations.LibraryResources);

            Assert.True (Settings! ["Application.DefaultKeyBindings"].PropertyValue is Dictionary<Command, PlatformKeyBinding>);
            Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));

            // Change to a custom value
            Dictionary<Command, PlatformKeyBinding> customBindings = new ()
            {
                [Command.Quit] = Bind.All (Key.Q),
            };
            Settings ["Application.DefaultKeyBindings"].PropertyValue = customBindings;
            Apply ();
            Assert.Equal (Key.Q, Application.GetDefaultKey (Command.Quit));

            // Act
            ResetToHardCodedDefaults ();
            Dictionary<Command, PlatformKeyBinding> resetBindings =
                (Dictionary<Command, PlatformKeyBinding>)Settings ["Application.DefaultKeyBindings"].PropertyValue!;
            Assert.Contains (Command.Quit, resetBindings.Keys);
            Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Themes_Property_Exists ()
    {
        var settingsScope = new SettingsScope ();

        Assert.NotEmpty (settingsScope);

        // Themes exists, but is not initialized
        Assert.Null (settingsScope ["Themes"].PropertyValue);

        //settingsScope.UpdateToCurrentValues ();

        //Assert.NotEmpty (settingsScope);
    }

    [Fact]
    public void LoadHardCodedDefaults_Resets ()
    {
        // Arrange
        Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));
        SettingsScope settingsScope = new ();
        settingsScope.LoadHardCodedDefaults ();

        // Act — change the quit key
        Dictionary<Command, PlatformKeyBinding> customBindings = new ()
        {
            [Command.Quit] = Bind.All (Key.Q),
        };
        settingsScope ["Application.DefaultKeyBindings"].PropertyValue = customBindings;
        settingsScope.Apply ();
        Assert.Equal (Key.Q, Application.GetDefaultKey (Command.Quit));

        settingsScope.LoadHardCodedDefaults ();
        settingsScope.Apply ();

        // Assert — should be back to default
        Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));

        Disable (true);
    }

    private class ConfigPropertyMock
    {
        public object? PropertyValue { get; init; }
        public bool Immutable { get; init; }
    }

    private class SettingsScopeMock : Dictionary<string, ConfigPropertyMock>
    {
        public string? Theme { get; set; }
    }

    [Fact]
    public void SettingsScopeMockWithKey_CreatesDeepCopy ()
    {
        SettingsScopeMock? source = new ()
        {
            Theme = "Dark",
            ["KeyBinding"] = new () { PropertyValue = new Key (KeyCode.A) { Handled = true } },
            ["Counts"] = new () { PropertyValue = new Dictionary<string, int> { { "X", 1 } } }
        };
        SettingsScopeMock? result = DeepCloner.DeepClone (source);

        Assert.NotNull (result);
        Assert.NotSame (source, result);
        Assert.Equal (source.Theme, result!.Theme);
        Assert.NotSame (source ["KeyBinding"], result ["KeyBinding"]);
        Assert.NotSame (source ["Counts"], result ["Counts"]);

        ConfigPropertyMock clonedKeyProp = result ["KeyBinding"];
        var clonedKey = (Key)clonedKeyProp.PropertyValue!;
        Assert.NotSame (source ["KeyBinding"].PropertyValue, clonedKey);
        Assert.Equal (((Key)source ["KeyBinding"].PropertyValue!).KeyCode, clonedKey.KeyCode);
        Assert.Equal (((Key)source ["KeyBinding"].PropertyValue!).Handled, clonedKey.Handled);

        Assert.Equal ((Dictionary<string, int>)source ["Counts"].PropertyValue!, (Dictionary<string, int>)result ["Counts"].PropertyValue!);

        // Modify result, ensure source unchanged
        result.Theme = "Light";
        clonedKey.Handled = false;
        ((Dictionary<string, int>)result ["Counts"].PropertyValue!).Add ("Y", 2);
        Assert.Equal ("Dark", source.Theme);
        Assert.True (((Key)source ["KeyBinding"].PropertyValue!).Handled);
        Assert.Single ((Dictionary<string, int>)source ["Counts"].PropertyValue!);
        Disable (true);
    }

    [Fact /*(Skip = "This test randomly fails due to a concurrent change to something. Needs to be moved to non-parallel tests.")*/]
    public void ThemeScopeList_WithThemes_ClonesSuccessfully ()
    {
        // Arrange: Create a ThemeScope and verify a property exists
        var defaultThemeScope = new ThemeScope ();
        defaultThemeScope.LoadHardCodedDefaults ();
        Assert.True (defaultThemeScope.ContainsKey ("Button.DefaultMouseHighlightStates"));

        var darkThemeScope = new ThemeScope ();
        darkThemeScope.LoadHardCodedDefaults ();
        Assert.True (darkThemeScope.ContainsKey ("Button.DefaultMouseHighlightStates"));

        // Create a Themes list with two themes
        List<Dictionary<string, ThemeScope>> themesList =
        [
            new () { { "Default", defaultThemeScope } },
            new () { { "Dark", darkThemeScope } }
        ];

        // Create a SettingsScope and set the Themes property
        var settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();
        Assert.True (settingsScope.ContainsKey ("Themes"));
        settingsScope ["Themes"].PropertyValue = themesList;

        // Act
        SettingsScope? result = DeepCloner.DeepClone (settingsScope);

        // Assert
        Assert.NotNull (result);
        Assert.IsType<SettingsScope> (result);
        var resultScope = result;
        Assert.True (resultScope.ContainsKey ("Themes"));

        Assert.NotNull (resultScope ["Themes"].PropertyValue);

        List<Dictionary<string, ThemeScope>> clonedThemes = (List<Dictionary<string, ThemeScope>>)resultScope ["Themes"].PropertyValue!;
        Assert.Equal (2, clonedThemes.Count);
        Disable (true);
    }

    [Fact]
    public void Empty_SettingsScope_ClonesSuccessfully ()
    {
        // Arrange: Create a SettingsScope 
        var settingsScope = new SettingsScope ();
        Assert.True (settingsScope.ContainsKey ("Themes"));

        // Act
        SettingsScope? result = DeepCloner.DeepClone (settingsScope);

        // Assert
        Assert.NotNull (result);
        Assert.IsType<SettingsScope> (result);

        Assert.True (result.ContainsKey ("Themes"));
        Disable (true);
    }

    [Fact]
    public void SettingsScope_With_Themes_Set_ClonesSuccessfully ()
    {
        // Arrange: Create a SettingsScope 
        var settingsScope = new SettingsScope ();
        Assert.True (settingsScope.ContainsKey ("Themes"));

        settingsScope ["Themes"].PropertyValue = new List<Dictionary<string, ThemeScope>>
        {
            new () { { "Default", new () } },
            new () { { "Dark", new () } }
        };

        // Act
        SettingsScope? result = DeepCloner.DeepClone (settingsScope);

        // Assert
        Assert.NotNull (result);
        Assert.IsType<SettingsScope> (result);
        Assert.True (result.ContainsKey ("Themes"));
        Assert.NotNull (result ["Themes"].PropertyValue);
        Disable (true);
    }
}
