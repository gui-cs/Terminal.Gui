using System.Reflection;
using System.Text.Json;

public class SourcesManagerTests
{
    #region Update (Stream)

    [Fact]
    public void Update_WithNullSettingsScope_ReturnsFalse ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var stream = new MemoryStream ();
        var source = "test.json";
        var location = ConfigLocations.AppCurrent;

        // Act
        bool result = sourcesManager.Load (null, stream, source, location);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void Update_WithValidStream_UpdatesSettingsScope ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        var json = """
                   {
                        "Application.QuitKey": "Ctrl+Z"
                   }
                   """;
        var location = ConfigLocations.HardCoded;
        var source = "stream";

        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        // Act
        bool result = sourcesManager.Load (settingsScope, stream, source, location);

        // Assert
        // Assert
        Assert.True (result);
        Assert.Equal (Key.Z.WithCtrl, settingsScope ["Application.QuitKey"].PropertyValue as Key);
        Assert.Contains (source, sourcesManager.Sources.Values);
    }

    [Fact]
    public void Update_WithInvalidJson_AddsJsonError ()
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

        var source = "test.json";
        var location = ConfigLocations.AppCurrent;

        // Act
        bool result = sourcesManager.Load (settingsScope, stream, source, location);

        // Assert
        Assert.False (result);

        // Assuming AddJsonError logs errors, verify the error was logged (mock or inspect logs if possible).
    }

    #endregion

    #region Update (FilePath)

    [Fact]
    public void Update_WithNonExistentFile_AddsToSourcesAndReturnsTrue ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();

        var settingsScope = new SettingsScope ();
        var filePath = "nonexistent.json";
        var location = ConfigLocations.AppCurrent;

        // Act
        bool result = sourcesManager.Load (settingsScope, filePath, location);

        // Assert
        Assert.True (result);
        Assert.Contains (filePath, sourcesManager.Sources.Values);
    }

    [Fact]
    public void Update_WithValidFile_UpdatesSettingsScope ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        var json = """
                   {
                        "Application.QuitKey": "Ctrl+Z"
                   }
                   """;
        var source = Path.GetTempFileName ();
        var location = ConfigLocations.HardCoded;

        File.WriteAllText (source, json);

        try
        {
            // Act
            bool result = sourcesManager.Load (settingsScope, source, location);

            // Assert
            Assert.True (result);
            Assert.Equal (Key.Z.WithCtrl, settingsScope ["Application.QuitKey"].PropertyValue as Key);
            Assert.Contains (source, sourcesManager.Sources.Values);
        }
        finally
        {
            // Cleanup
            File.Delete (source);
        }
    }

    [Fact]
    public void Update_WithIOException_RetriesAndFailsGracefully ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();
        var filePath = "locked.json";
        var json = "{\"Application.UseSystemConsole\": true}";
        File.WriteAllText (filePath, json);

        var location = ConfigLocations.AppCurrent;

        try
        {
            using FileStream fileStream = File.Open (filePath, FileMode.Open, FileAccess.Read, FileShare.None);

            // Act
            bool result = sourcesManager.Load (settingsScope, filePath, location);

            // Assert
            Assert.False (result);
        }
        finally
        {
            // Cleanup
            File.Delete (filePath);
        }
    }

    #endregion

    #region Update (Json String)

    [Fact]
    public void Update_WithNullOrEmptyJson_ReturnsFalse ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();

        var settingsScope = new SettingsScope ();
        var source = "test.json";
        var location = ConfigLocations.AppCurrent;

        // Act
        bool resultWithNull = sourcesManager.Load (settingsScope, json: null, source, location);
        bool resultWithEmpty = sourcesManager.Load (settingsScope, string.Empty, source, location);

        // Assert
        Assert.False (resultWithNull);
        Assert.False (resultWithEmpty);
    }

    [Fact]
    public void Update_WithValidJson_UpdatesSettingsScope ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        var json = """
                   {
                        "Application.QuitKey": "Ctrl+Z"
                   }
                   """;
        var source = "test.json";
        var location = ConfigLocations.HardCoded;

        // Act
        bool result = sourcesManager.Load (settingsScope, json, source, location);

        // Assert
        Assert.True (result);
        Assert.Equal (Key.Z.WithCtrl, settingsScope ["Application.QuitKey"].PropertyValue as Key);
        Assert.Contains (source, sourcesManager.Sources.Values);
    }

    #endregion

    #region Load

    [Fact]
    public void Load_WithNullResourceName_ReturnsFalse ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        var assembly = Assembly.GetExecutingAssembly ();
        var location = ConfigLocations.AppResources;

        // Act
        bool result = sourcesManager.Load (settingsScope, assembly, null, location);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void Load_WithValidResource_UpdatesSettingsScope ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();

        var assembly = Assembly.GetAssembly (typeof (ConfigurationManager));
        var resourceName = "Terminal.Gui.Resources.config.json";
        var location = ConfigLocations.LibraryResources;

        // Act
        bool result = sourcesManager.Load (settingsScope, assembly!, resourceName, location);

        // Assert
        Assert.True (result);

        // Verify settingsScope is updated as expected
    }

    [Fact]
    public void Load_Runtime_Overrides ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();

        var assembly = Assembly.GetAssembly (typeof (ConfigurationManager));
        var resourceName = "Terminal.Gui.Resources.config.json";
        var location = ConfigLocations.LibraryResources;
        sourcesManager.Load (settingsScope, assembly!, resourceName, location);

        Assert.Equal (Key.Esc, settingsScope ["Application.QuitKey"].PropertyValue);

        var runtimeJson = """
                          {
                               "Application.QuitKey": "Ctrl+Z"
                          }
                          """;
        var runtimeSource = "runtime.json";
        var runtimeLocation = ConfigLocations.Runtime;
        var runtimeStream = new MemoryStream ();
        var writer = new StreamWriter (runtimeStream);
        writer.Write (runtimeJson);
        writer.Flush ();
        runtimeStream.Position = 0;

        // Act
        bool result = sourcesManager.Load (settingsScope, runtimeStream, runtimeSource, runtimeLocation);

        // Assert
        Assert.True (result);

        // Verify settingsScope is updated as expected
        Assert.Equal (Key.Z.WithCtrl, settingsScope ["Application.QuitKey"].PropertyValue);
    }

    #endregion

    #region ToJson and ToStream

    [Fact]
    public void ToJson_WithValidScope_ReturnsJsonString ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        // Act
        string json = sourcesManager.ToJson (settingsScope);

        // Assert
        Assert.Contains ("""Application.QuitKey": "Ctrl+Q""", json);
    }

    [Fact]
    public void ToStream_WithValidScope_ReturnsStream ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope.LoadHardCodedDefaults ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        // Act
        var stream = sourcesManager.ToStream (settingsScope);

        // Assert
        Assert.NotNull (stream);
        stream.Position = 0;
        var reader = new StreamReader (stream);
        string json = reader.ReadToEnd ();
        Assert.Contains ("""Application.QuitKey": "Ctrl+Q""", json);
    }

    #endregion

    #region Sources Dictionary Tests

    [Fact]
    public void Sources_Dictionary_IsInitializedEmpty ()
    {
        // Arrange & Act
        var sourcesManager = new SourcesManager ();

        // Assert
        Assert.NotNull (sourcesManager.Sources);
        Assert.Empty (sourcesManager.Sources);
    }

    [Fact]
    public void Update_WhenCalledMultipleTimes_MaintainsLastSourceForLocation ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();

        // Act - Update with first source for location
        var firstSource = "first.json";
        sourcesManager.Load (settingsScope, """{"Application.QuitKey": "Ctrl+A"}""", firstSource, ConfigLocations.Runtime);

        // Update with second source for same location
        var secondSource = "second.json";
        sourcesManager.Load (settingsScope, """{"Application.QuitKey": "Ctrl+B"}""", secondSource, ConfigLocations.Runtime);

        // Assert - Only the last source should be stored for the location
        Assert.Single (sourcesManager.Sources);
        Assert.Equal (secondSource, sourcesManager.Sources [ConfigLocations.Runtime]);
    }

    [Fact]
    public void Update_WithDifferentLocations_AddsAllSourcesToCollection ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();

        ConfigLocations [] locations =
        [
            ConfigLocations.LibraryResources,
            ConfigLocations.Runtime,
            ConfigLocations.AppCurrent,
            ConfigLocations.GlobalHome
        ];

        // Act - Update with different sources for different locations
        foreach (var location in locations)
        {
            var source = $"config-{location}.json";
            sourcesManager.Load (settingsScope, """{"Application.QuitKey": "Ctrl+Z"}""", source, location);
        }

        // Assert
        Assert.Equal (locations.Length, sourcesManager.Sources.Count);
        foreach (var location in locations)
        {
            Assert.Contains (location, sourcesManager.Sources.Keys);
            Assert.Equal ($"config-{location}.json", sourcesManager.Sources [location]);
        }
    }

    [Fact]
    public void Load_AddsResourceSourceToCollection ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();

        var assembly = Assembly.GetAssembly (typeof (ConfigurationManager));
        var resourceName = "Terminal.Gui.Resources.config.json";
        var location = ConfigLocations.LibraryResources;

        // Act
        bool result = sourcesManager.Load (settingsScope, assembly!, resourceName, location);

        // Assert
        Assert.True (result);
        Assert.Contains (location, sourcesManager.Sources.Keys);
        Assert.Equal ($"resource://[{assembly!.GetName ().Name}]/{resourceName}", sourcesManager.Sources [location]);
    }

    [Fact]
    public void Update_WithNonExistentFileAndDifferentLocations_TracksAllSources ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();

        // Define multiple files and locations
        var fileLocations = new Dictionary<string, ConfigLocations> (StringComparer.InvariantCultureIgnoreCase)
    {
        { "file1.json", ConfigLocations.AppCurrent },
        { "file2.json", ConfigLocations.GlobalHome },
        { "file3.json", ConfigLocations.AppHome }
    };

        // Act
        foreach (var pair in fileLocations)
        {
            sourcesManager.Load (settingsScope, pair.Key, pair.Value);
        }

        // Assert
        Assert.Equal (fileLocations.Count, sourcesManager.Sources.Count);
        foreach (var pair in fileLocations)
        {
            Assert.Contains (pair.Value, sourcesManager.Sources.Keys);
            Assert.Equal (pair.Key, sourcesManager.Sources [pair.Value]);
        }
    }

    [Fact]
    public void Sources_IsPreservedAcrossOperations ()
    {
        // Arrange
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();

        // First operation - file update
        var filePath = "testfile.json";
        var location1 = ConfigLocations.AppCurrent;
        sourcesManager.Load (settingsScope, filePath, location1);

        // Second operation - json string update
        var jsonSource = "jsonstring";
        var location2 = ConfigLocations.Runtime;
        sourcesManager.Load (settingsScope, """{"Application.QuitKey": "Ctrl+Z"}""", jsonSource, location2);

        // Perform a stream operation
        var streamSource = "streamdata";
        var location3 = ConfigLocations.GlobalCurrent;
        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write ("""{"Application.QuitKey": "Ctrl+Z"}""");
        writer.Flush ();
        stream.Position = 0;
        sourcesManager.Load (settingsScope, stream, streamSource, location3);

        // Assert - all sources should be preserved
        Assert.Equal (3, sourcesManager.Sources.Count);
        Assert.Equal (filePath, sourcesManager.Sources [location1]);
        Assert.Equal (jsonSource, sourcesManager.Sources [location2]);
        Assert.Equal (streamSource, sourcesManager.Sources [location3]);
    }

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

    #endregion

}
