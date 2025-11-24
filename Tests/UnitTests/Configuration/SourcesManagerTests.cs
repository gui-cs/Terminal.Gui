using System.Reflection;
using System.Text.Json;

namespace UnitTests.ConfigurationTests;

public class SourcesManagerTests
{
    [Fact]
    public void Sources_StaysConsistentWhenUpdateFails ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();

        // Add one successful source
        var validSource = "valid.json";
        var validLocation = ConfigLocations.Runtime;
        sourcesManager.Load (settingsScope, """{"Application.QuitKey": "Ctrl+Z"}""", validSource, validLocation);

        try
        {
            // Configure to throw on errors
            ConfigurationManager.ThrowOnJsonErrors = true;

            // Act & Assert - attempt to update with invalid JSON
            var invalidSource = "invalid.json";
            var invalidLocation = ConfigLocations.AppCurrent;
            var invalidJson = "{ invalid json }";

            Assert.Throws<JsonException> (
                                          () =>
                                              sourcesManager.Load (settingsScope, invalidJson, invalidSource, invalidLocation));

            // The valid source should still be there
            Assert.Single (sourcesManager.Sources);
            Assert.Equal (validSource, sourcesManager.Sources [validLocation]);

            // The invalid source should not have been added
            Assert.DoesNotContain (invalidLocation, sourcesManager.Sources.Keys);
        }
        finally
        {
            // Reset for other tests
            ConfigurationManager.ThrowOnJsonErrors = false;
        }
    }
}
