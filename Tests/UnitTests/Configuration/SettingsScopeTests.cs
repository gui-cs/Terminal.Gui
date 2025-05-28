using System.Collections.Concurrent;
using UnitTests;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class SettingsScopeTests
{
    [Fact]
    public void Load_Overrides_Defaults ()
    {
        // arrange
        Enable (ConfigLocations.HardCoded);

        Assert.Equal (Key.Esc, (Key)Settings! ["Application.QuitKey"].PropertyValue);

        ThrowOnJsonErrors = true;

        // act
        RuntimeConfig = """
                   
                           {
                                 "Application.QuitKey": "Ctrl-Q"
                           }
                   """;

        Load (ConfigLocations.Runtime);

        // assert
        Assert.Equal (Key.Q.WithCtrl, (Key)Settings ["Application.QuitKey"].PropertyValue);

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
        ConcurrentDictionary<string, ThemeScope> dict = themesConfigProperty.PropertyValue as ConcurrentDictionary<string, ThemeScope>;

        Assert.NotNull (dict);
        Assert.Single (dict);
        Assert.NotEmpty ((ConcurrentDictionary<string, ThemeScope>)themesConfigProperty.PropertyValue);

        ThemeScope scope = dict [ThemeManager.DEFAULT_THEME_NAME];
        Assert.NotNull (scope);
        Assert.Equal (HighlightStyle.Hover | HighlightStyle.Pressed, scope ["Button.DefaultHighlightStyle"].PropertyValue);


        RuntimeConfig = """
                        {
                            "Themes": [
                                {
                                  "Default": 
                                     {
                                         "Button.DefaultHighlightStyle": "None"
                                     }
                                },
                                {
                                  "NewTheme":
                                    {
                                        "Button.DefaultHighlightStyle": "Hover"
                                    }
                                }                        
                            ]
                        }
                        """;

        Load (ConfigLocations.Runtime);

        // assert
        Assert.Equal (2, ThemeManager.GetThemes ().Count);
        Assert.Equal (HighlightStyle.None, (HighlightStyle)ThemeManager.GetCurrentTheme () ["Button.DefaultHighlightStyle"].PropertyValue!);
        Assert.Equal (HighlightStyle.Hover, (HighlightStyle)ThemeManager.GetThemes () ["NewTheme"] ["Button.DefaultHighlightStyle"].PropertyValue!);

        RuntimeConfig = """
                        {
                            "Themes": [
                                {
                                  "Default": 
                                     {
                                         "Button.DefaultHighlightStyle": "Pressed"
                                     }
                                }
                            ]
                        }
                        """;
        Load (ConfigLocations.Runtime);

        // assert
        Assert.Equal (2, ThemeManager.GetThemes ().Count);
        Assert.Equal (HighlightStyle.Pressed, (HighlightStyle)ThemeManager.Themes! [ThemeManager.DEFAULT_THEME_NAME] ["Button.DefaultHighlightStyle"].PropertyValue!);
        Assert.Equal (HighlightStyle.Hover, (HighlightStyle)ThemeManager.Themes! ["NewTheme"] ["Button.DefaultHighlightStyle"].PropertyValue!);

        // clean up
        Disable (resetToHardCodedDefaults: true);

    }

    [Fact]
    public void Apply_ShouldApplyProperties ()
    {
        Enable (ConfigLocations.HardCoded);
        Load (ConfigLocations.LibraryResources);

        // arrange
        Assert.Equal (Key.Esc, (Key)Settings! ["Application.QuitKey"].PropertyValue);

        Assert.Equal (
                      Key.F6,
                      (Key)Settings ["Application.NextTabGroupKey"].PropertyValue
                     );

        Assert.Equal (
                      Key.F6.WithShift,
                      (Key)Settings ["Application.PrevTabGroupKey"].PropertyValue
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
        Assert.Equal (KeyCode.End, ((Key)Settings ["Application.QuitKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.F, ((Key)updatedSettings ["Application.NextTabGroupKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.B, ((Key)updatedSettings ["Application.PrevTabGroupKey"].PropertyValue).KeyCode);
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
}
