using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui;

namespace Terminal.Gui {
	/// <summary>
	/// Json converter fro the <see cref="Attribute"/> class.
	/// </summary>
	class AttributeJsonConverter : JsonConverter<Attribute> {
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

		public override Attribute Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject) {
				throw new JsonException ($"Unexpected StartObject token when parsing Attribute: {reader.TokenType}.");
			}

			Attribute attribute = new Attribute ();
			Color foreground = (Color)(-1);
			Color background = (Color)(-1);
			TrueColor? trueColorForeground = null;
			TrueColor? trueColorBackground = null;
			while (reader.Read ()) {
				if (reader.TokenType == JsonTokenType.EndObject) {
					if (!attribute.TrueColorForeground.HasValue || !attribute.TrueColorBackground.HasValue) {
						throw new JsonException ($"Both Foreground and Background colors must be provided.");
					}
					return attribute;
				}

				if (reader.TokenType != JsonTokenType.PropertyName) {
					throw new JsonException ($"Unexpected token when parsing Attribute: {reader.TokenType}.");
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
				case "truecolorforeground":
					trueColorForeground = JsonSerializer.Deserialize<TrueColor> (color, options);
					break;
				case "truecolorbackground":
					trueColorBackground = JsonSerializer.Deserialize<TrueColor> (color, options);
					break;
				//case "bright":
				//case "bold":
				//	attribute.Bright = reader.GetBoolean ();
				//	break;
				//case "dim":
				//	attribute.Dim = reader.GetBoolean ();
				//	break;
				//case "underline":
				//	attribute.Underline = reader.GetBoolean ();
				//	break;
				//case "blink":
				//	attribute.Blink = reader.GetBoolean ();
				//	break;
				//case "reverse":
				//	attribute.Reverse = reader.GetBoolean ();
				//	break;
				//case "hidden":
				//	attribute.Hidden = reader.GetBoolean ();
				//	break;
				//case "strike-through":
				//	attribute.StrikeThrough = reader.GetBoolean ();
				//	break;				
				default:
					throw new JsonException ($"Unknown Attribute property {propertyName}.");
				}

				if (foreground != (Color)(-1) && background != (Color)(-1)) {
					attribute = new Attribute (foreground, background);
				}
				if (trueColorForeground.HasValue && trueColorBackground.HasValue) {
					attribute = new Attribute (trueColorForeground, trueColorBackground);
				}
			}
			throw new JsonException ();
		}

		public override void Write (Utf8JsonWriter writer, Attribute value, JsonSerializerOptions options)
		{
			writer.WriteStartObject ();
			writer.WritePropertyName (nameof(Attribute.Foreground));
			ColorJsonConverter.Instance.Write (writer, value.Foreground, options);
			writer.WritePropertyName (nameof (Attribute.Background));
			ColorJsonConverter.Instance.Write (writer, value.Background, options);
			if (value.TrueColorForeground.HasValue && value.TrueColorBackground.HasValue) {
				writer.WritePropertyName (nameof (Attribute.TrueColorForeground));
				TrueColorJsonConverter.Instance.Write (writer, value.TrueColorForeground.Value, options);
				writer.WritePropertyName (nameof (Attribute.TrueColorBackground));
				TrueColorJsonConverter.Instance.Write (writer, value.TrueColorBackground.Value, options);
			}
			writer.WriteEndObject ();
		}
	}
}

