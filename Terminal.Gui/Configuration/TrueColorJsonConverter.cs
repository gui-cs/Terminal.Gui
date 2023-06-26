using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="JsonConverter{T}"/> for <see cref="TrueColor"/>.
	/// </summary>
	internal class TrueColorJsonConverter : JsonConverter<TrueColor> {
		private static TrueColorJsonConverter instance;

		/// <summary>
		/// Singleton
		/// </summary>
		public static TrueColorJsonConverter Instance {
			get {
				if (instance == null) {
					instance = new TrueColorJsonConverter ();
				}

				return instance;
			}
		}

		public override TrueColor Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			// Check if the value is a string
			if (reader.TokenType == JsonTokenType.String) {
				// Get the color string
				var colorString = reader.GetString ();

				if (!TrueColor.TryParse (colorString, out TrueColor? trueColor)) {
					throw new JsonException ($"Invalid TrueColor: '{colorString}'");
				}

				return trueColor.Value;
			} else {
				throw new JsonException ($"Unexpected token when parsing TrueColor: {reader.TokenType}");
			}
		}

		public override void Write (Utf8JsonWriter writer, TrueColor value, JsonSerializerOptions options)
		{
			writer.WriteStringValue (value.ToString ());
		}
	}
}
