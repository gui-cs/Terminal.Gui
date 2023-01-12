using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Terminal.Gui.Configuration {
	/// <summary>
	/// 
	/// </summary>
	public class ColorJsonConverter : JsonConverter<Color> {
		private static ColorJsonConverter instance;

		/// <summary>
		/// 
		/// </summary>
		public ColorJsonConverter ()
		{
			Debug.Assert (colorMap.Count == Enum.GetNames (typeof (Color)).Length);
		}

		/// <summary>
		/// 
		/// </summary>
		public static ColorJsonConverter Instance {
			get {
				if (instance == null) {
					instance = new ColorJsonConverter ();
				}

				return instance;
			}
		}

		private static readonly Dictionary<Color, string> colorMap = new Dictionary<Color, string>
		{
			{ Color.Black, "Black" },
			{ Color.Blue, "Blue" },
			{ Color.Green, "Green" },
			{ Color.Cyan, "Cyan" },
			{ Color.Red, "Red" },
			{ Color.Magenta, "Magenta" },
			{ Color.Brown, "Brown" },
			{ Color.Gray, "Gray" },
			{ Color.DarkGray, "DarkGray" },
			{ Color.BrightBlue, "BrightBlue" },
			{ Color.BrightGreen, "BrightGreen" },
			{ Color.BrightCyan, "BrightCyan" },
			{ Color.BrightRed, "BrightRed" },
			{ Color.BrightMagenta, "BrightMagenta" },
			{ Color.BrightYellow, "BrightYellow" },
			{ Color.White, "White" },
			{ Color.Invalid, "Invalid" }
		 };

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="typeToConvert"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		/// <exception cref="JsonException"></exception>
		public override Color Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			// Check if the value is a string
			if (reader.TokenType == JsonTokenType.String) {
				// Get the color string
				var colorString = reader.GetString ();

				// Check if the color string is a color name
				if (Enum.TryParse (colorString, ignoreCase: true, out Color color)) {
					// Return the parsed color
					return color;
				} else {
					// Parse the color string as an RGB value
					var match = Regex.Match (colorString, @"rgb\((\d+),(\d+),(\d+)\)");
					if (match.Success) {
						var r = int.Parse (match.Groups [1].Value);
						var g = int.Parse (match.Groups [2].Value);
						var b = int.Parse (match.Groups [3].Value);
						return new TrueColor (r, g, b).ToConsoleColor ();
					} else {
						throw new JsonException ($"Invalid color string: '{colorString}'");
					}
				}
			} else {
				throw new JsonException ($"Unexpected token when parsing color: {reader.TokenType}");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="options"></param>
		/// <exception cref="JsonException"></exception>
		public override void Write (Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
		{
			// Try to get the human readable color name from the map
			var name = Enum.GetName (typeof (Color), value);
			if (name != null) {
				// Write the color name to the JSON
				writer.WriteStringValue (name);
			} else {
				//// If the color is not in the map, look up its RGB values in the consoleDriver.colors array
				//ConsoleColor consoleColor = (ConsoleDriver [(int)value]);
				//int r = consoleColor.R;
				//int g = consoleColor.G;
				//int b = consoleColor.B;

				//// Write the RGB values as a string to the JSON
				//writer.WriteStringValue ($"rgb({r},{g},{b})");
				throw new JsonException ($"Unkown color value");
			}
		}

	}



}
