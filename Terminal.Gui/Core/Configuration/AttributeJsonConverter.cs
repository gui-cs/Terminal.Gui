using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui;

namespace Terminal.Gui.Core {
	/// <summary>
	/// 
	/// </summary>
	public class AttributeJsonConverter : JsonConverter<Attribute> {
		private static AttributeJsonConverter instance;

		/// <summary>
		/// 
		/// </summary>
		public static AttributeJsonConverter Instance {
			get {
				if (instance == null) {
					instance = new AttributeJsonConverter ();
				}

				return instance;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="typeToConvert"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		/// <exception cref="JsonException"></exception>
		public override Attribute Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject) {
				throw new JsonException ();
			}

			Attribute attribute = new Attribute ();
			Color foreground = Color.Invalid;
			Color background = Color.Invalid;
			while (reader.Read ()) {
				if (reader.TokenType == JsonTokenType.EndObject) {
					if (foreground == Color.Invalid || background == Color.Invalid) {
						throw new JsonException ("Both Foreground and Background colors must be provided.");
					}
					return attribute;
				}

				if (reader.TokenType != JsonTokenType.PropertyName) {
					throw new JsonException ();
				}

				string propertyName = reader.GetString ();
				reader.Read ();
				string color = $"\"{reader.GetString ()}\"";

				switch (propertyName.ToLower ()) {
				case "foreground":
					foreground = JsonSerializer.Deserialize<Color> (color, options);
					break;
				case "background":
					background = JsonSerializer.Deserialize<Color> (color, options);
					break;
					//case "Bright":
					//	attribute.Bright = reader.GetBoolean ();
					//	break;
					//case "Underline":
					//	attribute.Underline = reader.GetBoolean ();
					//	break;
					//case "Reverse":
					//	attribute.Reverse = reader.GetBoolean ();
					//	break;
				}

				if (Application.Driver != null) {
					attribute = new Attribute (foreground, background);
				}
			}
			throw new JsonException ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="options"></param>
		public override void Write (Utf8JsonWriter writer, Attribute value, JsonSerializerOptions options)
		{
			writer.WriteStartObject ();
			writer.WritePropertyName ("Foreground");
			ColorJsonConverter.Instance.Write (writer, value.Foreground, options);
			writer.WritePropertyName ("Background");
			ColorJsonConverter.Instance.Write (writer, value.Background, options);
			writer.WriteEndObject ();
		}
	}
}

