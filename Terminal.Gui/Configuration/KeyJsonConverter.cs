﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui {
	class KeyJsonConverter : JsonConverter<ConsoleDriverKey> {
		public override ConsoleDriverKey Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.StartObject) {
				ConsoleDriverKey key = ConsoleDriverKey.Unknown;
				Dictionary<string, ConsoleDriverKey> modifierDict = new Dictionary<string, ConsoleDriverKey> (comparer: StringComparer.InvariantCultureIgnoreCase) {
					{ "Shift", ConsoleDriverKey.ShiftMask },
					{ "Ctrl", ConsoleDriverKey.CtrlMask },
					{ "Alt", ConsoleDriverKey.AltMask }
				};

				List<ConsoleDriverKey> modifiers = new List<ConsoleDriverKey> ();

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
								if (Enum.TryParse (reader.GetString (), false, out key)) {
									break;
								}
								
								// The enum uses "D0..D9" for the number keys
								if (Enum.TryParse (reader.GetString ().TrimStart ('D', 'd'), false, out key)) {
									break;
								}

								if (key == ConsoleDriverKey.Unknown || key == ConsoleDriverKey.Null) {
									throw new JsonException ($"The value \"{reader.GetString ()}\" is not a valid Key.");
								}

							} else if (reader.TokenType == JsonTokenType.Number) {
								try {
									key = (ConsoleDriverKey)reader.GetInt32 ();
								} catch (InvalidOperationException ioe) {
									throw new JsonException ($"Error parsing Key value: {ioe.Message}", ioe);
								} catch (FormatException ioe) {
									throw new JsonException ($"Error parsing Key value: {ioe.Message}", ioe);
								}
								break;
							}
							break;

						case "modifiers":
							if (reader.TokenType == JsonTokenType.StartArray) {
								while (reader.Read ()) {
									if (reader.TokenType == JsonTokenType.EndArray) {
										break;
									}
									var mod = reader.GetString ();
									try {
										modifiers.Add (modifierDict [mod]);
									} catch (KeyNotFoundException e) {
										throw new JsonException ($"The value \"{mod}\" is not a valid modifier.", e);
									}
								}
							} else {
								throw new JsonException ($"Expected an array of modifiers, but got \"{reader.TokenType}\".");
							}
							break;

						default:
							throw new JsonException ($"Unexpected Key property \"{propertyName}\".");
						}
					}
				}

				foreach (var modifier in modifiers) {
					key |= modifier;
				}

				return key;
			}
			throw new JsonException ($"Unexpected StartObject token when parsing Key: {reader.TokenType}.");
		}

		public override void Write (Utf8JsonWriter writer, ConsoleDriverKey value, JsonSerializerOptions options)
		{
			writer.WriteStartObject ();

			var keyName = (value & ~ConsoleDriverKey.CtrlMask & ~ConsoleDriverKey.ShiftMask & ~ConsoleDriverKey.AltMask).ToString ();
			if (keyName != null) {
				writer.WriteString ("Key", keyName);
			} else {
				writer.WriteNumber ("Key", (uint)(value & ~ConsoleDriverKey.CtrlMask & ~ConsoleDriverKey.ShiftMask & ~ConsoleDriverKey.AltMask));
			}

			Dictionary<string, ConsoleDriverKey> modifierDict = new Dictionary<string, ConsoleDriverKey>
			{
				{ "Shift", ConsoleDriverKey.ShiftMask },
				{ "Ctrl", ConsoleDriverKey.CtrlMask },
				{ "Alt", ConsoleDriverKey.AltMask }
			    };

			List<string> modifiers = new List<string> ();
			foreach (var pair in modifierDict) {
				if ((value & pair.Value) == pair.Value) {
					modifiers.Add (pair.Key);
				}
			}

			if (modifiers.Count > 0) {
				writer.WritePropertyName ("Modifiers");
				writer.WriteStartArray ();
				foreach (var modifier in modifiers) {
					writer.WriteStringValue (modifier);
				}
				writer.WriteEndArray ();
			}

			writer.WriteEndObject ();
		}
	}
}
