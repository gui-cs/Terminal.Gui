﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui;

namespace Terminal.Gui.Configuration {
	/// <summary>
	/// 
	/// </summary>
	public class ColorSchemeJsonConverter : JsonConverter<ColorScheme> {
		private static ColorSchemeJsonConverter instance;

		/// <summary>
		/// 
		/// </summary>
		public static ColorSchemeJsonConverter Instance {
			get {
				if (instance == null) {
					instance = new ColorSchemeJsonConverter ();
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
		public override ColorScheme Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject) {
				throw new JsonException ();
			}

			var colorScheme = new ColorScheme ();

			while (reader.Read ()) {
				if (reader.TokenType == JsonTokenType.EndObject) {
					return colorScheme;
				}

				if (reader.TokenType != JsonTokenType.PropertyName) {
					throw new JsonException ();
				}

				var propertyName = reader.GetString ();
				reader.Read ();
				var attribute = JsonSerializer.Deserialize<Attribute> (ref reader, options);

				switch (propertyName.ToLower()) {
				case "normal":
					colorScheme.Normal = attribute;
					break;
				case "focus":
					colorScheme.Focus = attribute;
					break;
				case "hotnormal":
					colorScheme.HotNormal = attribute;
					break;
				case "hotfocus":
					colorScheme.HotFocus = attribute;
					break;
				case "disabled":
					colorScheme.Disabled = attribute;
					break;
				default:
					throw new JsonException ($"Unrecognized property name: {propertyName}");
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
		public override void Write (Utf8JsonWriter writer, ColorScheme value, JsonSerializerOptions options)
		{
			writer.WriteStartObject ();

			writer.WritePropertyName ("Normal");
			AttributeJsonConverter.Instance.Write (writer, value.Normal, options);
			writer.WritePropertyName ("Focus");
			AttributeJsonConverter.Instance.Write (writer, value.Focus, options);
			writer.WritePropertyName ("HotNormal");
			AttributeJsonConverter.Instance.Write (writer, value.HotNormal, options);
			writer.WritePropertyName ("HotFocus");
			AttributeJsonConverter.Instance.Write (writer, value.HotFocus, options);
			writer.WritePropertyName ("Disabled");
			AttributeJsonConverter.Instance.Write (writer, value.Disabled, options);

			writer.WriteEndObject ();
		}


	}
}