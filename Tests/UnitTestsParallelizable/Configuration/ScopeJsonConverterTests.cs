#nullable enable
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

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
}
