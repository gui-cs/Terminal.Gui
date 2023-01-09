using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Terminal.Gui.Core {
	/// <summary>
	/// 
	/// </summary>
	public class ColorJsonConverter : JsonConverter<Color> {
		/// <summary>
		/// 
		/// </summary>
		public ColorJsonConverter ()
		{
			Debug.Assert (colorMap.Count == Enum.GetNames (typeof (Color)).Length);
		}

		private static readonly Dictionary<Color, string> colorMap = new Dictionary<Color, string>
		{
			{ Color.Black, "Black" },
			{ Color.Blue, "Blue" },
			{ Color.Green, "Green" },
			{ Color.Cyan, "Cyan" },
			{ Color.Gray, "Gray" },
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
			{ Color.White, "White" }
		 };

		private class TrueColor {
			/// <summary>
			/// Red color component.
			/// </summary>
			public int Red { get; }
			/// <summary>
			/// Green color component.
			/// </summary>
			public int Green { get; }
			/// <summary>
			/// Blue color component.
			/// </summary>
			public int Blue { get; }

			/// <summary>
			/// Initializes a new instance of the <see cref="TrueColor"/> struct.
			/// </summary>
			/// <param name="red"></param>
			/// <param name="green"></param>
			/// <param name="blue"></param>
			public TrueColor (int red, int green, int blue)
			{
				Red = red;
				Green = green;
				Blue = blue;
			}

			public Color ToConsoleColor ()
			{
				var trueColorMap = new Dictionary<TrueColor, Color> () {
				{ new TrueColor (0,0,0),Color.Black},
				{ new TrueColor (0, 0, 0x80),Color.Blue},
				{ new TrueColor (0, 0x80, 0),Color.Green},
				{ new TrueColor (0, 0x80, 0x80),Color.Cyan},
				{ new TrueColor (0x80, 0, 0),Color.Red},
				{ new TrueColor (0x80, 0, 0x80),Color.Magenta},
				{ new TrueColor (0xC1, 0x9C, 0x00),Color.Brown},  // TODO confirm this
				{ new TrueColor (0xC0, 0xC0, 0xC0),Color.Gray},
				{ new TrueColor (0x80, 0x80, 0x80),Color.DarkGray},
				{ new TrueColor (0, 0, 0xFF),Color.BrightBlue},
				{ new TrueColor (0, 0xFF, 0),Color.BrightGreen},
				{ new TrueColor (0, 0xFF, 0xFF),Color.BrightCyan},
				{ new TrueColor (0xFF, 0, 0),Color.BrightRed},
				{ new TrueColor (0xFF, 0, 0xFF),Color.BrightMagenta },
				{ new TrueColor (0xFF, 0xFF, 0),Color.BrightYellow},
				{ new TrueColor (0xFF, 0xFF, 0xFF),Color.White},
				};
				// Initialize the minimum distance to the maximum possible value
				int minDistance = int.MaxValue;
				Color nearestColor = Color.Black;

				// Iterate over all colors in the map
				var distances = trueColorMap.Select (
								k => Tuple.Create (
									// the candidate we are considering matching against (RGB)
									k.Key,

									CalculateDistance (k.Key, this)
								));

				// get the closest
				var match = distances.OrderBy (t => t.Item2).First ();
				return nearestColor;
			}

			private float CalculateDistance (TrueColor color1, TrueColor color2)
			{
				// use RGB distance
				return
					Math.Abs (color1.Red - color2.Red) +
					Math.Abs (color1.Green - color2.Green) +
					Math.Abs (color1.Blue - color2.Blue);
			}
		}

		private static readonly Dictionary<string, Color> reverseColorMap = colorMap.ToDictionary (x => x.Value, x => x.Key);

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
				if (Enum.TryParse (colorString, out Color color)) {
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
			if (colorMap.TryGetValue (value, out string colorName)) {
				// Write the color name to the JSON
				writer.WriteStringValue (colorName);
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
