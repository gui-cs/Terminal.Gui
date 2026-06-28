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
    // A2.4: dropped "Dialog.DefaultButtonAlignment" InlineData row — Dialog.DefaultButtonAlignment lost
    // [ConfigurationProperty] in A2.4 when view-facade theme setters were removed; ScopeJsonConverter now
    // rejects it. CM and this test are removed in step D.
    public void RoundTripConversion_Property_Positive (string configPropertyJson)
    {
        // Arrange
        string scopeJson = $"{{{configPropertyJson}}}";

        // Act
        SettingsScope? deserialized = JsonSerializer.Deserialize<SettingsScope> (scopeJson, TuiSerializerContext.Instance.Options);

        string? json = JsonSerializer.Serialize<SettingsScope> (deserialized!, TuiSerializerContext.Instance.Options);

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
        string json = JsonSerializer.Serialize (settingsScope, TuiSerializerContext.Instance.SettingsScope);
        JsonDocument document = JsonDocument.Parse (json);

        // Assert
        Assert.True (document.RootElement.TryGetProperty ("$schema", out JsonElement schema));
        Assert.Equal (JsonValueKind.Null, schema.ValueKind);
    }
}
