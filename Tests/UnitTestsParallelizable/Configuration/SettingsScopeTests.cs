
namespace ConfigurationTests;

public class SettingsScopeTests
{
    [Fact]
    public void Schema_Is_Correct ()
    {
        // Arrange
        var settingsScope = new SettingsScope ();
        // Act

        // Assert
        Assert.Equal ("https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json", settingsScope.Schema);
    }
}
