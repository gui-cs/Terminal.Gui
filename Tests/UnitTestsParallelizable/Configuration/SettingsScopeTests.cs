
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
    public void Themes_Property_Exists ()
    {
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();

        var settingsScope = new SettingsScope ();

        Assert.NotEmpty (settingsScope);

        // Themes exists, but is not initialized
        Assert.Null (settingsScope ["Themes"].PropertyValue);

        settingsScope.RetrieveValues ();

        Assert.NotEmpty (settingsScope);
    }
}
