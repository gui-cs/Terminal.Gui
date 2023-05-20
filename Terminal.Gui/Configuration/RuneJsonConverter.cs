using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui {
	/// <summary>
	/// Json converter for <see cref="Rune"/>. Supports
	/// A string as one of:
	/// - unicode char (e.g. "☑")
	/// - U+hex format (e.g. "U+2611")
	/// - \u format (e.g. "\\u2611")
	/// A number
	/// - The unicode code in decimal
	/// </summary>
	internal class RuneJsonConverter : JsonConverter<Rune> {
		public override Rune Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String) {
				var value = reader.GetString ();
				if (value.StartsWith ("U+", StringComparison.OrdinalIgnoreCase) || value.StartsWith ("\\u")) {
					try {
						uint result = uint.Parse (value [2..^0], System.Globalization.NumberStyles.HexNumber);
						return new Rune (result);
					} catch (FormatException e) {
						throw new JsonException ($"Invalid Rune format: {value}.", e);
					}
				} else {
					return new Rune (value [0]);
				}
				throw new JsonException ($"Invalid Rune format: {value}.");
			} else if (reader.TokenType == JsonTokenType.Number) {
				return new Rune (reader.GetUInt32 ());
			}
			throw new JsonException ($"Unexpected StartObject token when parsing Rune: {reader.TokenType}.");
		}

		public override void Write (Utf8JsonWriter writer, Rune value, JsonSerializerOptions options)
		{
			// HACK: Writes a JSON comment in addition to the glyph to ease debugging.
			// Technically, JSON comments are not valid, but we use relaxed decoding
			// (ReadCommentHandling = JsonCommentHandling.Skip)
			writer.WriteCommentValue ($"(U+{value.Value:X4})");
			writer.WriteRawValue ($"\"{value}\"");
		}
	}
#pragma warning restore 1591
}
