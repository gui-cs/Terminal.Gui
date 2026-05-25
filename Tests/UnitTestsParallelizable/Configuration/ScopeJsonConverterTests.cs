#nullable enable
using System.Text.Json;

namespace ConfigurationTests;

public class ScopeJsonConverterTests
{
    [Theory]
    [InlineData ("\"ConfigurationManager.ThrowOnJsonErrors\":true")]
    [InlineData ("\"Key.Separator\":\"@\"")]
    [InlineData ("\"Themes\":[]")]
    [InlineData ("\"Themes\":[{\"themeName\":{}}]")]
    [InlineData ("\"Themes\":[{\"themeName\":{\"Dialog.DefaultButtonAlignment\":\"End\"}}]")]
    public void RoundTripConversion_Property_Positive (string configPropertyJson)
    {
        // Arrange
        string scopeJson = $"{{{configPropertyJson}}}";

        // Act
        SettingsScope? deserialized = JsonSerializer.Deserialize<SettingsScope> (scopeJson, ConfigurationManager.SerializerContext.Options);

        string? json = JsonSerializer.Serialize<SettingsScope> (deserialized!, ConfigurationManager.SerializerContext.Options);

        // Strip all whitespace
        json = json.Replace (" ", string.Empty);
        json = json.Replace ("\n", string.Empty);
        json = json.Replace ("\r", string.Empty);
        json = json.Replace ("\t", string.Empty);

        // Assert
        Assert.Contains (configPropertyJson, json);
    }

    [Fact]
    public void RoundTripConversion_JsonIncludeProperty_WithNullValue_WritesNull ()
    {
        // Copilot
        // Arrange
        SettingsScope settingsScope = new () { Schema = null! };

        // Act
        string json = JsonSerializer.Serialize (settingsScope, ConfigurationManager.SerializerContext.SettingsScope);
        JsonDocument document = JsonDocument.Parse (json);

        // Assert
        Assert.True (document.RootElement.TryGetProperty ("$schema", out JsonElement schema));
        Assert.Equal (JsonValueKind.Null, schema.ValueKind);
    }
}
