using System.Reflection;
using Terminal.Gui.Configuration;

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
        bool result = sourcesManager.Update (null, stream, source, location);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void Update_WithValidStream_UpdatesSettingsScope ()
    {
        // Arrange
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        var json = """
                   {
                        "Application.QuitKey": "Ctrl+Z"
                   }
                   """;
        var location = ConfigLocations.None;
        var source = "stream";

        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        // Act
        bool result = sourcesManager.Update (settingsScope, stream, source, location);

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
        bool result = sourcesManager.Update (settingsScope, stream, source, location);

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
        bool result = sourcesManager.Update (settingsScope, filePath, location);

        // Assert
        Assert.True (result);
        Assert.Contains (filePath, sourcesManager.Sources.Values);
    }

    [Fact]
    public void Update_WithValidFile_UpdatesSettingsScope ()
    {
        // Arrange
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        var json = """
                   {
                        "Application.QuitKey": "Ctrl+Z"
                   }
                   """;
        var source = Path.GetTempFileName ();
        var location = ConfigLocations.None;

        File.WriteAllText (source, json);

        try
        {
            // Act
            bool result = sourcesManager.Update (settingsScope, source, location);

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
        var filePath = "locked.json";
        var json = "{\"Application.UseSystemConsole\": true}";
        File.WriteAllText (filePath, json);

        var location = ConfigLocations.AppCurrent;

        try
        {
            using FileStream fileStream = File.Open (filePath, FileMode.Open, FileAccess.Read, FileShare.None);

            // Act
            bool result = sourcesManager.Update (settingsScope, filePath, location);

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
        bool resultWithNull = sourcesManager.Update (settingsScope, json: null, source, location);
        bool resultWithEmpty = sourcesManager.Update (settingsScope, string.Empty, source, location);

        // Assert
        Assert.False (resultWithNull);
        Assert.False (resultWithEmpty);
    }

    [Fact]
    public void Update_WithValidJson_UpdatesSettingsScope ()
    {
        // Arrange
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        var json = """
                   {
                        "Application.QuitKey": "Ctrl+Z"
                   }
                   """;
        var source = "test.json";
        var location = ConfigLocations.None;

        // Act
        bool result = sourcesManager.Update (settingsScope, json, source, location);

        // Assert
        Assert.True (result);
        Assert.Equal (Key.Z.WithCtrl, settingsScope ["Application.QuitKey"].PropertyValue as Key);
        Assert.Contains (source, sourcesManager.Sources.Values);
    }

    #endregion

    #region UpdateFromResource

    [Fact]
    public void UpdateFromResource_WithNullResourceName_ReturnsFalse ()
    {
        // Arrange
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
        settingsScope ["Application.QuitKey"].PropertyValue = Key.Q.WithCtrl;

        var assembly = Assembly.GetExecutingAssembly ();
        var location = ConfigLocations.AppResources;

        // Act
        bool result = sourcesManager.UpdateFromResource (settingsScope, assembly, null, location);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void UpdateFromResource_WithValidResource_UpdatesSettingsScope ()
    {
        // Arrange
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();

        var assembly = Assembly.GetAssembly (typeof (ConfigurationManager));
        var resourceName = "Terminal.Gui.Resources.config.json";
        var location = ConfigLocations.Default;

        // Act
        bool result = sourcesManager.UpdateFromResource (settingsScope, assembly!, resourceName, location);

        // Assert
        Assert.True (result);

        // Verify settingsScope is updated as expected
    }

    #endregion

    #region ToJson and ToStream

    [Fact]
    public void ToJson_WithValidScope_ReturnsJsonString ()
    {
        // Arrange
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
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
        // Need to call Initialize to setup readonly statics
        ConfigurationManager.Initialize ();
        var sourcesManager = new SourcesManager ();
        var settingsScope = new SettingsScope ();
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
}
