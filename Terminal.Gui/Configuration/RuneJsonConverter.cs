using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui {
	/// <summary>
	/// Json converter for the <see cref="Rune"/> class. Supports
	/// A string as one of:
	/// - unicode char (e.g. "☑")
	/// - U+hex format (e.g. "U+2611")
	/// - \u format (e.g. "\\u2611")
	/// A number
	/// - The unicode code in decimal
	/// </summary>
	public class RuneJsonConverter : JsonConverter<Rune> {
		/// <inheritdoc/>
		public override Rune Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String) {
				var value = reader.GetString ();
				if (value.ToUpper ().StartsWith ("U+") || value.StartsWith ("\\u")) {
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

		/// <inheritdoc/>
		public override void Write (Utf8JsonWriter writer, Rune value, JsonSerializerOptions options)
		{
			writer.WriteStringValue (value.ToString ());
		}
	}
}
