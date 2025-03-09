using System.Text;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class RuneJsonConverterTests
{
    [Theory]
    [InlineData ("aa")]
    [InlineData ("☑☑")]
    [InlineData ("\\x2611")]
    [InlineData ("Z+2611")]
    [InlineData ("🍎🍎")]
    [InlineData ("U+FFF1F34E")]
    [InlineData ("\\UFFF1F34E")]
    [InlineData ("\\ud83d")] // not printable "high surrogate"
    [InlineData ("\\udc3d")] // not printable "low surrogate"
    [InlineData ("\\ud83d \\u1c69")] // bad surrogate pair
    [InlineData ("\\ud83ddc69")]

    // Emoji - Family Unit:
    // Woman (U+1F469, 👩)
    // Zero Width Joiner (U+200D)
    // Woman (U+1F469, 👩)
    // Zero Width Joiner (U+200D)
    // Girl (U+1F467, 👧)
    // Zero Width Joiner (U+200D)
    // Girl (U+1F467, 👧)
    [InlineData ("U+1F469 U+200D U+1F469 U+200D U+1F467 U+200D U+1F467")]
    [InlineData ("\\U0001F469\\u200D\\U0001F469\\u200D\\U0001F467\\u200D\\U0001F467")]
    public void RoundTripConversion_Negative (string rune)
    {
        // Act
        string json = JsonSerializer.Serialize (rune, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Throws<JsonException> (
                                      () => JsonSerializer.Deserialize<Rune> (
                                                                              json,
                                                                              ConfigurationManager.SerializerOptions
                                                                             )
                                     );
    }

    [Theory]
    [InlineData ("a", "a")]
    [InlineData ("☑", "☑")]
    [InlineData ("\\u2611", "☑")]
    [InlineData ("U+2611", "☑")]
    [InlineData ("🍎", "🍎")]
    [InlineData ("U+1F34E", "🍎")]
    [InlineData ("\\U0001F34E", "🍎")]
    [InlineData ("\\ud83d \\udc69", "👩")]
    [InlineData ("\\ud83d\\udc69", "👩")]
    [InlineData ("U+d83d U+dc69", "👩")]
    [InlineData ("U+1F469", "👩")]
    [InlineData ("\\U0001F469", "👩")]
    [InlineData ("\\u0065\\u0301", "é")]
    public void RoundTripConversion_Positive (string rune, string expected)
    {
        // Arrange

        // Act
        string json = JsonSerializer.Serialize (rune, ConfigurationManager.SerializerOptions);
        var deserialized = JsonSerializer.Deserialize<Rune> (json, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal (expected, deserialized.ToString ());
    }

    [Fact]
    public void Printable_Rune_Is_Serialized_As_Glyph ()
    {
        // Arrange

        // Act
        string json = JsonSerializer.Serialize ((Rune)'a', ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal ("\"a\"", json);
    }

    [Fact]
    public void Non_Printable_Rune_Is_Serialized_As_u_Encoded_Value ()
    {
        // Arrange

        // Act
        string json = JsonSerializer.Serialize ((Rune)0x01, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal ("\"\\u0001\"", json);
    }

    [Fact]
    public void Json_With_Glyph_Works ()
    {
        // Arrange
        var json = "\"a\"";

        // Act
        var deserialized = JsonSerializer.Deserialize<Rune> (json, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal ("a", deserialized.ToString ());
    }

    [Fact]
    public void Json_With_u_Encoded_Works ()
    {
        // Arrange
        var json = "\"\\u0061\"";

        // Act
        var deserialized = JsonSerializer.Deserialize<Rune> (json, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal ("a", deserialized.ToString ());
    }

    [Fact]
    public void Json_With_U_Encoded_Works ()
    {
        // Arrange
        var json = "\"U+0061\"";

        // Act
        var deserialized = JsonSerializer.Deserialize<Rune> (json, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal ("a", deserialized.ToString ());
    }

    [Fact]
    public void Json_With_Decimal_Works ()
    {
        // Arrange
        var json = "97";

        // Act
        var deserialized = JsonSerializer.Deserialize<Rune> (json, ConfigurationManager.SerializerOptions);

        // Assert
        Assert.Equal ("a", deserialized.ToString ());
    }
}
