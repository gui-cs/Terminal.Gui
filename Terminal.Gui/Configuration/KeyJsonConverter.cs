using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Terminal.Gui;
class KeyJsonConverter : JsonConverter<Key> {
	
	public override Key Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.StartObject) {
			Key key = KeyCode.Unknown;
			while (reader.Read ()) {
				if (reader.TokenType == JsonTokenType.EndObject) {
					break;
				}

				if (reader.TokenType == JsonTokenType.PropertyName) {
					string propertyName = reader.GetString ();
					reader.Read ();

					switch (propertyName.ToLowerInvariant ()) {
					case "key":
						if (reader.TokenType == JsonTokenType.String) {
							string keyValue = reader.GetString ();
							if (Key.TryParse (keyValue, out key)) {
								break;
							}
							throw new JsonException ($"Error parsing Key: {keyValue}.");

						}
						break;
					default:
						throw new JsonException ($"Unexpected Key property \"{propertyName}\".");
					}
				}
			}
			return key;
		}
		throw new JsonException ($"Unexpected StartObject token when parsing Key: {reader.TokenType}.");
	}

	public override void Write (Utf8JsonWriter writer, Key value, JsonSerializerOptions options)
	{
		writer.WriteStartObject ();

		writer.WriteString ("Key", value.ToString ());
		writer.WriteEndObject ();
	}
}
