// Claude - Opus 4.6

using System.Text.Json;
using Terminal.Gui;
using Terminal.Gui.Configuration;

namespace InputTests;

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
        Dictionary<Command, PlatformKeyBinding> original = new ()
        {
            [Command.Left] = new () { All = ["CursorLeft"] },
            [Command.Right] = new () { All = ["CursorRight"], Linux = ["Ctrl+F"] }
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        Dictionary<Command, PlatformKeyBinding>? deserialized = JsonSerializer.Deserialize<Dictionary<Command, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Equal (2, deserialized.Count);
        Assert.Equal (["CursorLeft"], deserialized [Command.Left].All!);
        Assert.Equal (["Ctrl+F"], deserialized [Command.Right].Linux!);
    }

    [Fact]
    public void KeyBindingDict_Deserializes_FromUserConfigFormat ()
    {
        // Arrange
        string json = """{ "Left": { "All": ["CursorLeft"], "Linux": ["Ctrl+B"] } }""";

        // Act
        Dictionary<Command, PlatformKeyBinding>? result = JsonSerializer.Deserialize<Dictionary<Command, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (result);
        Assert.Single (result);
        Assert.True (result.ContainsKey (Command.Left));
        Assert.Equal (["CursorLeft"], result [Command.Left].All!);
        Assert.Equal (["Ctrl+B"], result [Command.Left].Linux!);
        Assert.Null (result [Command.Left].Windows);
        Assert.Null (result [Command.Left].Macos);
    }

    [Fact]
    public void KeyBindingDict_EmptyDict_RoundTrips ()
    {
        // Arrange
        Dictionary<Command, PlatformKeyBinding> original = [];

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        Dictionary<Command, PlatformKeyBinding>? deserialized = JsonSerializer.Deserialize<Dictionary<Command, PlatformKeyBinding>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Empty (deserialized);
    }

    [Fact]
    public void ViewKeyBindings_RoundTrips_ThroughJson ()
    {
        // Arrange
        Dictionary<string, Dictionary<Command, PlatformKeyBinding>> original = new ()
        {
            ["TextView"] = new ()
            {
                [Command.Left] = new () { All = ["CursorLeft"], Macos = ["Cmd+Left"] },
                [Command.SelectAll] = new () { All = ["Ctrl+A"] }
            },
            ["TextField"] = new ()
            {
                [Command.DeleteAll] = new () { All = ["Ctrl+Shift+Delete"] }
            }
        };

        // Act
        string json = JsonSerializer.Serialize (original, _jsonOptions);
        Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? deserialized =
            JsonSerializer.Deserialize<Dictionary<string, Dictionary<Command, PlatformKeyBinding>>> (json, _jsonOptions);

        // Assert
        Assert.NotNull (deserialized);
        Assert.Equal (2, deserialized.Count);
        Assert.Equal (["CursorLeft"], deserialized ["TextView"] [Command.Left].All!);
        Assert.Equal (["Cmd+Left"], deserialized ["TextView"] [Command.Left].Macos!);
        Assert.Equal (["Ctrl+A"], deserialized ["TextView"] [Command.SelectAll].All!);
        Assert.Equal (["Ctrl+Shift+Delete"], deserialized ["TextField"] [Command.DeleteAll].All!);
    }
}
