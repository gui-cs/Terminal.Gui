// Claude - Opus 4.6

using System.Text.Json;
using Terminal.Gui;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

public class KeyBindingSchemaTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new ()
    {
        TypeInfoResolver = SourceGenerationContext.Default
    };

    [Fact]
    public void PlatformKeyBinding_RoundTrips_ThroughJson ()
    {
        // Arrange
        PlatformKeyBinding original = new ()
        {
            All = ["CursorLeft", "Home"],
            Windows = ["Alt+Left"],
            Linux = ["Ctrl+B"],
            Macos = ["Cmd+Left"]
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        PlatformKeyBinding? deserialized = JsonSerializer.Deserialize<PlatformKeyBinding> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Equal (original.All, deserialized.All);
        Assert.Equal (original.Windows, deserialized.Windows);
        Assert.Equal (original.Linux, deserialized.Linux);
        Assert.Equal (original.Macos, deserialized.Macos);
    }

    [Fact]
    public void KeyBindingDict_RoundTrips_ThroughJson ()
    {
        // Arrange
        Dictionary<string, PlatformKeyBinding> original = new ()
        {
            ["Left"] = new () { All = ["CursorLeft"] },
            ["Right"] = new () { All = ["CursorRight"], Linux = ["Ctrl+F"] }
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        Dictionary<string, PlatformKeyBinding>? deserialized = JsonSerializer.Deserialize<Dictionary<string, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Equal (2, deserialized.Count);
        Assert.Equal (["CursorLeft"], deserialized ["Left"].All!);
        Assert.Equal (["Ctrl+F"], deserialized ["Right"].Linux!);
    }

    [Fact]
    public void KeyBindingDict_Deserializes_FromUserConfigFormat ()
    {
        // Arrange
        string json = """{ "Left": { "All": ["CursorLeft"], "Linux": ["Ctrl+B"] } }""";

        // Act
        Dictionary<string, PlatformKeyBinding>? result = JsonSerializer.Deserialize<Dictionary<string, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (result);
        Assert.Single (result);
        Assert.True (result.ContainsKey ("Left"));
        Assert.Equal (["CursorLeft"], result ["Left"].All!);
        Assert.Equal (["Ctrl+B"], result ["Left"].Linux!);
        Assert.Null (result ["Left"].Windows);
        Assert.Null (result ["Left"].Macos);
    }

    [Fact]
    public void KeyBindingDict_EmptyDict_RoundTrips ()
    {
        // Arrange
        Dictionary<string, PlatformKeyBinding> original = [];

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        Dictionary<string, PlatformKeyBinding>? deserialized = JsonSerializer.Deserialize<Dictionary<string, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Empty (deserialized);
    }

    [Fact]
    public void ViewKeyBindings_RoundTrips_ThroughJson ()
    {
        // Arrange
        Dictionary<string, Dictionary<string, PlatformKeyBinding>> original = new ()
        {
            ["TextView"] = new ()
            {
                ["Left"] = new () { All = ["CursorLeft"], Macos = ["Cmd+Left"] },
                ["SelectAll"] = new () { All = ["Ctrl+A"] }
            },
            ["TextField"] = new ()
            {
                ["DeleteAll"] = new () { All = ["Ctrl+Shift+Delete"] }
            }
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        Dictionary<string, Dictionary<string, PlatformKeyBinding>>? deserialized =
            JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, PlatformKeyBinding>>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Equal (2, deserialized.Count);
        Assert.Equal (["CursorLeft"], deserialized ["TextView"] ["Left"].All!);
        Assert.Equal (["Cmd+Left"], deserialized ["TextView"] ["Left"].Macos!);
        Assert.Equal (["Ctrl+A"], deserialized ["TextView"] ["SelectAll"].All!);
        Assert.Equal (["Ctrl+Shift+Delete"], deserialized ["TextField"] ["DeleteAll"].All!);
    }
}
