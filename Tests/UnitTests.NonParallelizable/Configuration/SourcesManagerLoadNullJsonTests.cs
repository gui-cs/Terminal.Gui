using System.Text;
using System.Text.Json;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace UnitTests.NonParallelizable.ConfigurationTests;

public class SourcesManagerLoadNullJsonTests
{
    [Fact]
    public void Load_Stream_WithJsonNull_AndThrowOnJsonErrorsFalse_ReturnsFalse_AndAddsJsonError ()
    {
        // Copilot
        SourcesManager sourcesManager = new ();
        SettingsScope settingsScope = new ();
        string source = "Load_Stream_WithJsonNull_AndThrowOnJsonErrorsFalse_ReturnsFalse_AndAddsJsonError";
        ConfigLocations location = ConfigLocations.Runtime;
        Stream stream = new MemoryStream (Encoding.UTF8.GetBytes ("null"));

        bool? originalThrowOnJsonErrors = ThrowOnJsonErrors;
        string originalJsonErrors = _jsonErrors.ToString ();

        try
        {
            ThrowOnJsonErrors = false;
            _jsonErrors.Clear ();

            bool result = sourcesManager.Load (settingsScope, stream, source, location);

            Assert.False (result);
            Assert.Empty (sourcesManager.Sources);
            Assert.Contains ($"Error reading {source}:", _jsonErrors.ToString ());
        }
        finally
        {
            ThrowOnJsonErrors = originalThrowOnJsonErrors;
            _jsonErrors.Clear ();
            _jsonErrors.Append (originalJsonErrors);
        }
    }

    [Fact]
    public void Load_Stream_WithJsonNull_AndThrowOnJsonErrorsTrue_ThrowsJsonException ()
    {
        // Copilot
        SourcesManager sourcesManager = new ();
        SettingsScope settingsScope = new ();
        string source = "Load_Stream_WithJsonNull_AndThrowOnJsonErrorsTrue_ThrowsJsonException";
        ConfigLocations location = ConfigLocations.Runtime;
        Stream stream = new MemoryStream (Encoding.UTF8.GetBytes ("null"));

        bool? originalThrowOnJsonErrors = ThrowOnJsonErrors;
        string originalJsonErrors = _jsonErrors.ToString ();

        try
        {
            ThrowOnJsonErrors = true;
            _jsonErrors.Clear ();

            JsonException exception = Assert.Throws<JsonException> (() => sourcesManager.Load (settingsScope, stream, source, location));
            Assert.Contains ("null", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty (sourcesManager.Sources);
            Assert.Equal (0, _jsonErrors.Length);
        }
        finally
        {
            ThrowOnJsonErrors = originalThrowOnJsonErrors;
            _jsonErrors.Clear ();
            _jsonErrors.Append (originalJsonErrors);
        }
    }
}
