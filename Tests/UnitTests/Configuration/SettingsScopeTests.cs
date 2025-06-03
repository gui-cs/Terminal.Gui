#nullable enable
using System.Collections.Concurrent;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class SettingsScopeTests
{
    [Fact]
    public void Load_Overrides_Defaults ()
    {
        // arrange
        Enable (ConfigLocations.HardCoded);

        Assert.Equal (Key.Esc, (Key)Settings! ["Application.QuitKey"].PropertyValue!);

        ThrowOnJsonErrors = true;

        // act
        RuntimeConfig = """
                   
                           {
                                 "Application.QuitKey": "Ctrl-Q"
                           }
                   """;

        Load (ConfigLocations.Runtime);

        // assert
        Assert.Equal (Key.Q.WithCtrl, (Key)Settings ["Application.QuitKey"].PropertyValue!);

        // clean up
        Disable (resetToHardCodedDefaults: true);

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
        Assert.Equal (MouseState.In | MouseState.Pressed | MouseState.PressedOutside, scope ["Button.DefaultHighlightStates"].PropertyValue);


        RuntimeConfig = """
                        {
                            "Themes": [
                                {
                                  "Default": 
                                     {
                                         "Button.DefaultHighlightStates": "None"
                                     }
                                },
                                {
                                  "NewTheme":
                                    {
                                        "Button.DefaultHighlightStates": "In"
                                    }
                                }                        
                            ]
                        }
                        """;

        Load (ConfigLocations.Runtime);

        // assert
        Assert.Equal (2, ThemeManager.GetThemes ().Count);
        Assert.Equal (MouseState.None, (MouseState)ThemeManager.GetCurrentTheme () ["Button.DefaultHighlightStates"].PropertyValue!);
        Assert.Equal (MouseState.In, (MouseState)ThemeManager.GetThemes () ["NewTheme"] ["Button.DefaultHighlightStates"].PropertyValue!);

        RuntimeConfig = """
                        {
                            "Themes": [
                                {
                                  "Default": 
                                     {
                                         "Button.DefaultHighlightStates": "Pressed"
                                     }
                                }
                            ]
                        }
                        """;
        Load (ConfigLocations.Runtime);

        // assert
        Assert.Equal (2, ThemeManager.GetThemes ().Count);
        Assert.Equal (MouseState.Pressed, (MouseState)ThemeManager.Themes! [ThemeManager.DEFAULT_THEME_NAME] ["Button.DefaultHighlightStates"].PropertyValue!);
        Assert.Equal (MouseState.In, (MouseState)ThemeManager.Themes! ["NewTheme"] ["Button.DefaultHighlightStates"].PropertyValue!);

        // clean up
        Disable (resetToHardCodedDefaults: true);

    }

    [Fact]
    public void Apply_ShouldApplyProperties ()
    {
        Enable (ConfigLocations.HardCoded);
        Load (ConfigLocations.LibraryResources);

        // arrange
        Assert.Equal (Key.Esc, (Key)Settings!["Application.QuitKey"].PropertyValue!);

        Assert.Equal (
                      Key.F6,
                      (Key)Settings["Application.NextTabGroupKey"].PropertyValue!
                     );

        Assert.Equal (
                      Key.F6.WithShift,
                      (Key)Settings["Application.PrevTabGroupKey"].PropertyValue!
                     );

        // act
        Settings ["Application.QuitKey"].PropertyValue = Key.Q;
        Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;

        Settings.Apply ();

        // assert
        Assert.Equal (Key.Q, Application.QuitKey);
        Assert.Equal (Key.F, Application.NextTabGroupKey);
        Assert.Equal (Key.B, Application.PrevTabGroupKey);

        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void CopyUpdatedPropertiesFrom_ShouldCopyChangedPropertiesOnly ()
    {
        Enable (ConfigLocations.HardCoded);
        Settings ["Application.QuitKey"].PropertyValue = Key.End;

        var updatedSettings = new SettingsScope ();
        updatedSettings.LoadHardCodedDefaults ();

        // Don't set Quitkey
        updatedSettings ["Application.QuitKey"].HasValue = false;
        updatedSettings ["Application.QuitKey"].PropertyValue = null;
        updatedSettings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        updatedSettings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;

        Settings.UpdateFrom (updatedSettings);
        Assert.Equal (KeyCode.End, ((Key)Settings["Application.QuitKey"].PropertyValue!).KeyCode);
        Assert.Equal (KeyCode.F, ((Key)updatedSettings["Application.NextTabGroupKey"].PropertyValue!).KeyCode);
        Assert.Equal (KeyCode.B, ((Key)updatedSettings["Application.PrevTabGroupKey"].PropertyValue!).KeyCode);
        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void ResetToHardCodedDefaults_Resets_Config_And_Applies ()
    {
        Enable (ConfigLocations.HardCoded);
        Load (ConfigLocations.LibraryResources);

        Assert.True (Settings! ["Application.QuitKey"].PropertyValue is Key);
        Assert.Equal (Key.Esc, Settings ["Application.QuitKey"].PropertyValue as Key);
        Settings ["Application.QuitKey"].PropertyValue = Key.Q;
        Apply ();
        Assert.Equal (Key.Q, Application.QuitKey);

        // Act
        ResetToHardCodedDefaults ();
        Assert.Equal (Key.Esc, Settings ["Application.QuitKey"].PropertyValue as Key);
        Assert.Equal (Key.Esc, Application.QuitKey);

        Disable ();
    }


    [Fact]
    public void Themes_Property_Exists ()
    {
        var settingsScope = new SettingsScope ();

        Assert.NotEmpty (settingsScope);

        // Themes exists, but is not initialized
        Assert.Null (settingsScope ["Themes"].PropertyValue);

        settingsScope.LoadCurrentValues ();

        Assert.NotEmpty (settingsScope);
    }


    [Fact]
    public void LoadHardCodedDefaults_Resets ()
    {
        // Arrange
        Assert.Equal (Key.Esc, Application.QuitKey);
        var settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();

        // Act
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q;
        settingsScope.Apply ();
        Assert.Equal (Key.Q, Application.QuitKey);

        settingsScope.LoadHardCodedDefaults ();
        settingsScope.Apply ();

        // Assert
        Assert.Equal (Key.Esc, Application.QuitKey);

        Disable (resetToHardCodedDefaults: true);
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
        Disable (resetToHardCodedDefaults: true);
    }

    [Fact /*(Skip = "This test randomly fails due to a concurrent change to something. Needs to be moved to non-parallel tests.")*/]
    public void ThemeScopeList_WithThemes_ClonesSuccessfully ()
    {
        // Arrange: Create a ThemeScope and verify a property exists
        ThemeScope defaultThemeScope = new ThemeScope ();
        defaultThemeScope.LoadHardCodedDefaults ();
        Assert.True (defaultThemeScope.ContainsKey ("Button.DefaultHighlightStates"));

        ThemeScope darkThemeScope = new ThemeScope ();
        darkThemeScope.LoadHardCodedDefaults ();
        Assert.True (darkThemeScope.ContainsKey ("Button.DefaultHighlightStates"));

        // Create a Themes list with two themes
        List<Dictionary<string, ThemeScope>> themesList =
        [
            new () { { "Default", defaultThemeScope } },
            new () { { "Dark", darkThemeScope } }
        ];

        // Create a SettingsScope and set the Themes property
        SettingsScope settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();
        Assert.True (settingsScope.ContainsKey ("Themes"));
        settingsScope ["Themes"].PropertyValue = themesList;

        // Act
        SettingsScope? result = DeepCloner.DeepClone (settingsScope);

        // Assert
        Assert.NotNull (result);
        Assert.IsType<SettingsScope> (result);
        SettingsScope resultScope = (SettingsScope)result;
        Assert.True (resultScope.ContainsKey ("Themes"));

        Assert.NotNull (resultScope ["Themes"].PropertyValue);

        List<Dictionary<string, ThemeScope>> clonedThemes = (List<Dictionary<string, ThemeScope>>)resultScope ["Themes"].PropertyValue!;
        Assert.Equal (2, clonedThemes.Count);
        Disable (resetToHardCodedDefaults: true);
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
        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void SettingsScope_With_Themes_Set_ClonesSuccessfully ()
    {
        // Arrange: Create a SettingsScope 
        var settingsScope = new SettingsScope ();
        Assert.True (settingsScope.ContainsKey ("Themes"));

        settingsScope ["Themes"].PropertyValue = new List<Dictionary<string, ThemeScope>>
        {
            new() { { "Default", new () } },
            new() { { "Dark", new () } }
        };

        // Act
        SettingsScope? result = DeepCloner.DeepClone (settingsScope);

        // Assert
        Assert.NotNull (result);
        Assert.IsType<SettingsScope> (result);
        Assert.True (result.ContainsKey ("Themes"));
        Assert.NotNull (result ["Themes"].PropertyValue);
        Disable (resetToHardCodedDefaults: true);
    }
}
