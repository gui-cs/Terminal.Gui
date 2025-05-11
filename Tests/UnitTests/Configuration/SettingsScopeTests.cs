using UnitTests;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class SettingsScopeTests
{
    [Fact]
    public void Update_Overrides_Defaults ()
    {
        // arrange
        Enable();
        Load (ConfigLocations.LibraryResources);

        Assert.Equal (Key.Esc, (Key)Settings ["Application.QuitKey"].PropertyValue);

        ThrowOnJsonErrors = true;

        // act
        var json = """
                   
                           {
                                 "Application.QuitKey": "Ctrl-Q"
                           }
                   """;

        CM.SourcesManager?.Load(Settings!, json, "test", ConfigLocations.Runtime);

        // assert
        Assert.Equal (Key.Q.WithCtrl, (Key)Settings ["Application.QuitKey"].PropertyValue);

        // clean up
        Disable ();
        ResetToHardCodedDefaults ();

    }

    [Fact]
    public void Apply_ShouldApplyProperties ()
    {
        Enable ();
        Load (ConfigLocations.LibraryResources);

        // arrange
        Assert.Equal (Key.Esc, (Key)Settings ["Application.QuitKey"].PropertyValue);

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

        Disable ();
        ResetToHardCodedDefaults ();


    }

    [Fact]
    [AutoInitShutdown]
    public void CopyUpdatedPropertiesFrom_ShouldCopyChangedPropertiesOnly ()
    {
        Settings ["Application.QuitKey"].PropertyValue = Key.End;

        var updatedSettings = new SettingsScope ();

        ///Don't set Quitkey
        updatedSettings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        updatedSettings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;

        Settings.DeepCloneFrom (updatedSettings);
        Assert.Equal (KeyCode.End, ((Key)Settings ["Application.QuitKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.F, ((Key)updatedSettings ["Application.NextTabGroupKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.B, ((Key)updatedSettings ["Application.PrevTabGroupKey"].PropertyValue).KeyCode);
    }

    [Fact]
    public void ResetToHardCodedDefaults_Resets_Config_Does_Not_Apply ()
    {
        Enable ();
        Load (ConfigLocations.LibraryResources);

        Assert.True (Settings! ["Application.QuitKey"].PropertyValue is Key);
        Assert.Equal (Key.Esc, Settings ["Application.QuitKey"].PropertyValue as Key);
        Settings ["Application.QuitKey"].PropertyValue = Key.Q;
        Apply ();
        Assert.Equal (Key.Q, Application.QuitKey);

        // Act
        ResetToHardCodedDefaults ();
        Assert.Equal (Key.Esc, Settings ["Application.QuitKey"].PropertyValue as Key);
        Assert.Equal (Key.Q, Application.QuitKey);

        Disable ();
    }


    [Fact]
    [AutoInitShutdown]
    public void Themes_Property_Exists ()
    {
        var settingsScope = new SettingsScope ();

        Assert.NotEmpty (settingsScope);

        // Themes exists, but is not initialized
        Assert.Null (settingsScope ["Themes"].PropertyValue);

        settingsScope.UpdateToCurrentValues ();

        Assert.NotEmpty (settingsScope);
    }


    [Fact]
    public void ResetToHardCodedDefaults_Resets ()
    {
        // Arrange
        CM.Enable ();
        Assert.Equal (Key.Esc, Application.QuitKey);
        var settingsScope = new SettingsScope ();

        // Act
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q;
        settingsScope.Apply ();
        Assert.Equal (Key.Q, Application.QuitKey);
        settingsScope.UpdateToHardCodedDefaults ();
        settingsScope.Apply ();

        // Assert
        Assert.Equal (Key.Esc, Application.QuitKey);
    }
}
