
namespace Terminal.Gui.ConfigurationTests;

public class SettingsScopeTests
{
    [Fact]
    public void Schema_Is_Correct ()
    {
        // Arrange
        var settingsScope = new SettingsScope ();
        // Act

        // Assert
        Assert.Equal ("https://gui-cs.github.io/Terminal.GuiV2Docs/schemas/tui-config-schema.json", settingsScope.Schema);
    }

    [Fact]
    public void ResetToHardCodedDefaults_Resets ()
    {
        // TODO: Move this test to non-parallelizable tests
        // Arrange
        CM.Enable();
        Assert.Equal (Key.Esc, Application.QuitKey);
        var settingsScope = new SettingsScope ();

        // Act
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q;
        settingsScope.Apply ();
        Assert.Equal (Key.Q, Application.QuitKey);
        settingsScope.LoadHardCodedDefaults();
        settingsScope.Apply ();

        // Assert
        Assert.Equal (Key.Esc, Application.QuitKey);
    }

}
