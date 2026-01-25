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


    // NOTE: This test causes the static CM._jsonErrors to be modified; can't use in a parallel test
    [Fact]
    public void Load_WithInvalidJson_AddsJsonError ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();

        var settingsScope = new SettingsScope ();
        var invalidJson = "{ invalid json }";
        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write (invalidJson);
        writer.Flush ();
        stream.Position = 0;

        var source = "Load_WithInvalidJson_AddsJsonError";
        var location = ConfigLocations.AppCurrent;

        // Act
        bool result = sourcesManager.Load (settingsScope, stream, source, location);

        // Assert
        Assert.False (result);

        // Assuming AddJsonError logs errors, verify the error was logged (mock or inspect logs if possible).
    }
}
