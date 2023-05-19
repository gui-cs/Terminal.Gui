using System.Text;
using Xunit;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;
public class RunJsonConverterTests {

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
		var options = new JsonSerializerOptions ();
		options.Converters.Add (new RuneJsonConverter ());

		// Act
		var json = JsonSerializer.Serialize (rune, options);
		var deserialized = JsonSerializer.Deserialize<Rune> (json, options);

		// Assert
		Assert.Equal (expected, deserialized.ToString ());
	}

	[Theory]
	[InlineData ("aa")]
	[InlineData ("☑☑")]
	[InlineData ("\\x2611")]
	[InlineData ("Z+2611")]
	[InlineData ("🍎🍎")]
	[InlineData ("U+FFF1F34E")]
	[InlineData ("\\UFFF1F34E")]
	[InlineData ("\\ud83d")] // not printable
	[InlineData ("\\ud83d \\u1c69")]  // bad surrogate pair
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
		// Arrange
		var options = new JsonSerializerOptions ();
		options.Converters.Add (new RuneJsonConverter ());

		// Act
		var json = JsonSerializer.Serialize (rune, options);

		// Assert
		Assert.Throws<JsonException> (() => JsonSerializer.Deserialize<Rune> (json, options));
	}

}

