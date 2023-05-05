using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui {
	/// <summary>
	/// Json converter for the <see cref="Rune"/> class.
	/// </summary>
	public class RuneJsonConverter : JsonConverter<Rune> {
		/// <inheritdoc/>
		public override Rune Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String) {
				var value = reader.GetString ();
				return new Rune (value [0]);
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
