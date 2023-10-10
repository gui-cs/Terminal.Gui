global using Attribute = Terminal.Gui.Attribute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Defines the 16 legacy color names and values that can be used to set the foreground and background colors in Terminal.Gui apps. Used with <see cref="Color"/>.
	/// </summary>
	/// <remarks>
	/// 
	/// </remarks>
	public enum ColorNames {
		/// <summary>
		/// The black color.
		/// </summary>
		Black,
		/// <summary>
		/// The blue color.
		/// </summary>
		Blue,
		/// <summary>
		/// The green color.
		/// </summary>
		Green,
		/// <summary>
		/// The cyan color.
		/// </summary>
		Cyan,
		/// <summary>
		/// The red color.
		/// </summary>
		Red,
		/// <summary>
		/// The magenta color.
		/// </summary>
		Magenta,
		/// <summary>
		/// The brown color.
		/// </summary>
		Brown,
		/// <summary>
		/// The gray color.
		/// </summary>
		Gray,
		/// <summary>
		/// The dark gray color.
		/// </summary>
		DarkGray,
		/// <summary>
		/// The bright bBlue color.
		/// </summary>
		BrightBlue,
		/// <summary>
		/// The bright green color.
		/// </summary>
		BrightGreen,
		/// <summary>
		/// The bright cyan color.
		/// </summary>
		BrightCyan,
		/// <summary>
		/// The bright red color.
		/// </summary>
		BrightRed,
		/// <summary>
		/// The bright magenta color.
		/// </summary>
		BrightMagenta,
		/// <summary>
		/// The bright yellow color.
		/// </summary>
		BrightYellow,
		/// <summary>
		/// The White color.
		/// </summary>
		White
	}

	/// <summary>
	/// Represents a color in the console. This is used with <see cref="Attribute"/>. 
	/// </summary>
	[JsonConverter (typeof (ColorJsonConverter))]
	public class Color : IEquatable<Color> {

		/// <summary>
		/// Initializes a new instance of the <see cref="Color"/> class.
		/// </summary>
		/// <param name="red"></param>
		/// <param name="green"></param>
		/// <param name="blue"></param>
		public Color (int red, int green, int blue)
		{
			A = 0xFF;
			R = red;
			G = green;
			B = blue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Color"/> class.
		/// </summary>
		/// <param name="alpha"></param>
		/// <param name="red"></param>
		/// <param name="green"></param>
		/// <param name="blue"></param>
		public Color (int alpha, int red, int green, int blue)
		{
			A = alpha;
			R = red;
			G = green;
			B = blue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Color"/> class with an encoded 24-bit color value.
		/// </summary>
		/// <param name="argb">The encoded 24-bit color value.</param>
		public Color (int argb)
		{
			Value = argb;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Color"/> color from a legacy 16-color value.
		/// </summary>
		/// <param name="colorName">The 16-color value.</param>
		public Color (ColorNames colorName)
		{
			var c = Color.FromColorName (colorName);
			A = c.A;
			R = c.R;
			G = c.G;
			B = c.B;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Color"/>.
		/// </summary>
		public Color ()
		{
			A = 0xFF;
			R = 0;
			G = 0;
			B = 0;
		}

		/// <summary>
		/// Red color component.
		/// </summary>
		public int R { get; set; }
		/// <summary>
		/// Green color component.
		/// </summary>
		public int G { get; set; }
		/// <summary>
		/// Blue color component.
		/// </summary>
		public int B { get; set; }

		/// <summary>
		/// Alpha color component.
		/// </summary>
		/// <remarks>
		/// Not currently supported; here for completeness. 
		/// </remarks>
		public int A { get; set; }

		/// <summary>
		/// Gets or sets the color value encoded using the following code:
		/// <code>
		/// (&lt;see cref="A"/&gt; &lt;&lt; 24) | (&lt;see cref="R"/&gt; &lt;&lt; 16) | (&lt;see cref="G"/&gt; &lt;&lt; 8) | &lt;see cref="B"/&gt;
		/// </code>
		/// </summary>
		public int Value {
			get => (A << 24) | (R << 16) | (G << 8) | B;
			set {
				A = (byte)((value >> 24) & 0xFF);
				R = (byte)((value >> 16) & 0xFF);
				G = (byte)((value >> 8) & 0xFF);
				B = (byte)(value & 0xFF);
			}
		}

		// TODO: Make this map configurable via ConfigurationManager
		/// <summary>
		/// Maps legacy 16-color values to the corresponding 24-bit RGB value.
		/// </summary>
		internal static readonly ImmutableDictionary<Color, ColorNames> _colorNames = new Dictionary<Color, ColorNames> () {
			// using "Windows 10 Console/PowerShell 6" here: https://i.stack.imgur.com/9UVnC.png
			{ new Color (12, 12, 12),ColorNames.Black },
			{ new Color (0, 55, 218),ColorNames.Blue },
			{ new Color (19, 161, 14),ColorNames.Green},
			{ new Color (58, 150, 221),ColorNames.Cyan},
			{ new Color (197, 15, 31),ColorNames.Red},
			{ new Color (136, 23, 152),ColorNames.Magenta},
			{ new Color (128, 64, 32),ColorNames.Brown},
			{ new Color (204, 204, 204),ColorNames.Gray},
			{ new Color (118, 118, 118),ColorNames.DarkGray},
			{ new Color (59, 120, 255),ColorNames.BrightBlue},
			{ new Color (22, 198, 12),ColorNames.BrightGreen},
			{ new Color (97, 214, 214),ColorNames.BrightCyan},
			{ new Color (231, 72, 86),ColorNames.BrightRed},
			{ new Color (180, 0, 158),ColorNames.BrightMagenta },
			{ new Color (249, 241, 165),ColorNames.BrightYellow},
			{ new Color (242, 242, 242),ColorNames.White},
		}.ToImmutableDictionary ();

		/// <summary>
		/// Converts a legacy <see cref="ColorNames"/> to a 24-bit <see cref="Color"/>.
		/// </summary>
		/// <param name="consoleColor">The <see cref="Color"/> to convert.</param>
		/// <returns></returns>
		private static Color FromColorName (ColorNames consoleColor) => _colorNames.FirstOrDefault (x => x.Value == consoleColor).Key;

		/// <summary>
		/// Converts a legacy <see cref="ColorNames"/> index to a 24-bit <see cref="Color"/>.
		/// </summary>
		/// <param name="colorNameId">The index into the <see cref="ColorNames"/> enum to convert.</param>
		/// <returns></returns>
		private static Color FromColorName (int colorNameId)
		{
			return FromColorName ((ColorNames)colorNameId);
		}

		// This function iterates through the entries in the _colorNames dictionary, calculates the
		// Euclidean distance between the input color and each dictionary color in RGB space,
		// and keeps track of the closest entry found so far. The function returns a KeyValuePair
		// representing the closest color entry and its associated color name.
		internal static ColorNames FindClosestColor (Color inputColor)
		{
			ColorNames closestColor = ColorNames.Black; // Default to Black
			double closestDistance = double.MaxValue;

			foreach (var colorEntry in _colorNames) {
				var distance = CalculateColorDistance (inputColor, colorEntry.Key);
				if (distance < closestDistance) {
					closestDistance = distance;
					closestColor = colorEntry.Value;
				}
			}

			return closestColor;
		}

		private static double CalculateColorDistance (Color color1, Color color2)
		{
			// Calculate the Euclidean distance between two colors
			var deltaR = (double)color1.R - (double)color2.R;
			var deltaG = (double)color1.G - (double)color2.G;
			var deltaB = (double)color1.B - (double)color2.B;

			return Math.Sqrt (deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);
		}

		//private static KeyValuePair<Color, ColorNames> FindClosestColor (Color inputColor)
		//{
		//	KeyValuePair<Color, ColorNames> closestEntry = default;
		//	double closestDistance = double.MaxValue;

		//	foreach (var entry in _colorNames) {
		//		Color dictionaryColor = entry.Key;
		//		double distance = Math.Sqrt (
		//			Math.Pow (inputColor.R - dictionaryColor.R, 2) +
		//			Math.Pow (inputColor.G - dictionaryColor.G, 2) +
		//			Math.Pow (inputColor.B - dictionaryColor.B, 2)
		//		);

		//		if (distance < closestDistance) {
		//			closestDistance = distance;
		//			closestEntry = entry;
		//		}
		//	}

		//	return closestEntry;
		//}

		/// <summary>
		/// Gets or sets the <see cref="Color"/> using a legacy 16-color <see cref="ColorNames"/> value.
		/// </summary>
		/// <remarks>
		/// Get returns the closest 24-bit color value. Set sets the RGB value using a hard-coded map.
		/// </remarks>
		public ColorNames ColorName {
			get {
				return FindClosestColor (this.Value);
			}
			set {

				var c = FromColorName (value);
				A = c.A;
				R = c.R;
				G = c.G;
				B = c.B;
			}
		}

		/// <summary>
		/// 
		/// The black color.
		/// </summary>
		public const ColorNames Black = ColorNames.Black;

		/// <summary>
		/// The blue color.
		/// </summary>
		public const ColorNames Blue = ColorNames.Blue;
		/// <summary>
		/// The green color.
		/// </summary>
		public const ColorNames Green = ColorNames.Green;
		/// <summary>
		/// The cyan color.
		/// </summary>
		public const ColorNames Cyan = ColorNames.Cyan;
		/// <summary>
		/// The red color.
		/// </summary>
		public const ColorNames Red = ColorNames.Red;
		/// <summary>
		/// The magenta color.
		/// </summary>
		public const ColorNames Magenta = ColorNames.Magenta;
		/// <summary>
		/// The brown color.
		/// </summary>
		public const ColorNames Brown = ColorNames.Brown;
		/// <summary>
		/// The gray color.
		/// </summary>
		public const ColorNames Gray = ColorNames.Gray;
		/// <summary>
		/// The dark gray color.
		/// </summary>
		public const ColorNames DarkGray = ColorNames.DarkGray;
		/// <summary>
		/// The bright bBlue color.
		/// </summary>
		public const ColorNames BrightBlue = ColorNames.BrightBlue;
		/// <summary>
		/// The bright green color.
		/// </summary>
		public const ColorNames BrightGreen = ColorNames.BrightGreen;
		/// <summary>
		/// The bright cyan color.
		/// </summary>
		public const ColorNames BrightCyan = ColorNames.BrightCyan;
		/// <summary>
		/// The bright red color.
		/// </summary>
		public const ColorNames BrightRed = ColorNames.BrightRed;
		/// <summary>
		/// The bright magenta color.
		/// </summary>
		public const ColorNames BrightMagenta = ColorNames.BrightMagenta;
		/// <summary>
		/// The bright yellow color.
		/// </summary>
		public const ColorNames BrightYellow = ColorNames.BrightYellow;
		/// <summary>
		/// The White color.
		/// </summary>
		public const ColorNames White = ColorNames.White;

		/// <summary>
		/// Converts the provided text to a new <see cref="Color"/> instance.
		/// </summary>
		/// <param name="text">The text to analyze.</param>
		/// <param name="color">The parsed value.</param>
		/// <returns>A boolean value indicating whether it was successful.</returns>
		public static bool TryParse (string text, [NotNullWhen (true)] out Color color)
		{
			// empty color
			if ((text == null) || (text.Length == 0)) {
				color = null;
				return false;
			}

			// #RRGGBB, #RGB
			if ((text [0] == '#') && text.Length is 7 or 4) {
				if (text.Length == 7) {
					var r = Convert.ToInt32 (text.Substring (1, 2), 16);
					var g = Convert.ToInt32 (text.Substring (3, 2), 16);
					var b = Convert.ToInt32 (text.Substring (5, 2), 16);
					color = new Color (r, g, b);
				} else {
					var rText = char.ToString (text [1]);
					var gText = char.ToString (text [2]);
					var bText = char.ToString (text [3]);

					var r = Convert.ToInt32 (rText + rText, 16);
					var g = Convert.ToInt32 (gText + gText, 16);
					var b = Convert.ToInt32 (bText + bText, 16);
					color = new Color (r, g, b);
				}
				return true;
			}

			// #AARRGGBB, #ARGB
			if ((text [0] == '#') && text.Length is 8 or 5) {
				if (text.Length == 7) {
					var a = Convert.ToInt32 (text.Substring (1, 2), 16);
					var r = Convert.ToInt32 (text.Substring (3, 2), 16);
					var g = Convert.ToInt32 (text.Substring (5, 2), 16);
					var b = Convert.ToInt32 (text.Substring (7, 2), 16);
					color = new Color (a, r, g, b);
				} else {
					var aText = char.ToString (text [1]);
					var rText = char.ToString (text [2]);
					var gText = char.ToString (text [3]);
					var bText = char.ToString (text [4]);

					var a = Convert.ToInt32 (aText + aText, 16);
					var r = Convert.ToInt32 (rText + rText, 16);
					var g = Convert.ToInt32 (gText + gText, 16);
					var b = Convert.ToInt32 (bText + bText, 16);
					color = new Color (a, r, g, b);
				}
				return true;
			}

			// rgb(XX,YY,ZZ)
			var match = Regex.Match (text, @"rgb\((\d+),(\d+),(\d+)\)");
			if (match.Success) {
				var r = int.Parse (match.Groups [1].Value);
				var g = int.Parse (match.Groups [2].Value);
				var b = int.Parse (match.Groups [3].Value);
				color = new Color (r, g, b);
				return true;
			}

			// rgb(AA,XX,YY,ZZ)
			match = Regex.Match (text, @"rgb\((\d+),(\d+),(\d+),(\d+)\)");
			if (match.Success) {
				var a = int.Parse (match.Groups [1].Value);
				var r = int.Parse (match.Groups [2].Value);
				var g = int.Parse (match.Groups [3].Value);
				var b = int.Parse (match.Groups [4].Value);
				color = new Color (a, r, g, b);
				return true;
			}

			color = null;
			return false;
		}


		/// <summary>
		/// Cast from int.
		/// </summary>
		/// <param name="argb"></param>
		public static implicit operator Color (int argb)
		{
			return new Color (argb);
		}

		/// <summary>
		/// Cast to int.
		/// </summary>
		/// <param name="color"></param>
		public static explicit operator int (Color color)
		{
			return color.Value;
		}

		/// <summary>
		/// Cast from <see cref="ColorNames"/>.
		/// </summary>
		/// <param name="colorName"></param>
		public static explicit operator Color (ColorNames colorName)
		{
			return new Color (colorName);
		}

		/// <summary>
		/// Cast to <see cref="ColorNames"/>.
		/// </summary>
		/// <param name="color"></param>
		public static explicit operator ColorNames (Color color)
		{
			return color.ColorName;
		}


		/// <inheritdoc/>
		public static bool operator == (Color left, Color right)
		{
			return left.Equals (right);
		}

		/// <inheritdoc/>
		public static bool operator != (Color left, Color right)
		{
			return !left.Equals (right);
		}

		/// <inheritdoc/>
		public static bool operator == (ColorNames left, Color right)
		{
			return left == right.ColorName;
		}

		/// <inheritdoc/>
		public static bool operator != (ColorNames left, Color right)
		{
			return left != right.ColorName;
		}

		/// <inheritdoc/>
		public static bool operator == (Color left, ColorNames right)
		{
			return left.ColorName == right;
		}

		/// <inheritdoc/>
		public static bool operator != (Color left, ColorNames right)
		{
			return left.ColorName != right;
		}


		/// <inheritdoc/>
		public override bool Equals (object obj)
		{
			return obj is Color other && Equals (other);
		}

		/// <inheritdoc/>
		public bool Equals (Color other)
		{
			return
				A == other.A &&
				R == other.R &&
				G == other.G &&
				B == other.B;
		}

		/// <inheritdoc/>
		public override int GetHashCode ()
		{
			return HashCode.Combine (A, R, G, B);
		}

		/// <summary>
		/// Converts the color to a string representation.
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <returns></returns>
		public override string ToString ()
		{
			// If Values has an exact match with a named color (in _colorNames), use that.
			if (_colorNames.TryGetValue (this, out ColorNames colorName)) {
				return Enum.GetName (typeof (ColorNames), colorName);
			}
			// Otherwise return as an RGB hex value.
			return $"#{R:X2}{G:X2}{B:X2}";
		}
	}

	/// <summary>
	/// Attributes represent how text is styled when displayed in the terminal. 
	/// </summary>
	/// <remarks>
	///   <see cref="Attribute"/> provides a platform independent representation of colors (and someday other forms of text styling).
	///   They encode both the foreground and the background color and are used in the <see cref="ColorScheme"/>
	///   class to define color schemes that can be used in an application.
	/// </remarks>
	[JsonConverter (typeof (AttributeJsonConverter))]
	public struct Attribute : IEquatable<Attribute> {

		/// <summary>
		/// Default empty attribute.
		/// </summary>
		public static readonly Attribute Default = new Attribute (Color.White, Color.Black);

		/// <summary>
		/// The <see cref="ConsoleDriver"/>-specific color value. If <see cref="Initialized"/> is <see langword="false"/> 
		/// the value of this property is invalid (typically because the Attribute was created before a driver was loaded)
		/// and the attribute should be re-made (see <see cref="Make(Color, Color)"/>) before it is used.
		/// </summary>
		[JsonIgnore (Condition = JsonIgnoreCondition.Always)]
		internal int Value { get; }

		/// <summary>
		/// The foreground color.
		/// </summary>
		[JsonConverter (typeof (ColorJsonConverter))]
		public Color Foreground { get; private init; }

		/// <summary>
		/// The background color.
		/// </summary>
		[JsonConverter (typeof (ColorJsonConverter))]
		public Color Background { get; private init; }

		/// <summary>
		///  Initializes a new instance with default values.
		/// </summary>
		public Attribute ()
		{
			var d = Default;
			Value = -1;
			Foreground = d.Foreground;
			Background = d.Background;
		}

		/// <summary>
		/// Initializes a new instance with platform specific color value.
		/// </summary>
		/// <param name="platformColor">Value.</param>
		internal Attribute (int platformColor)
		{
			ColorNames foreground = Default.Foreground.ColorName;
			ColorNames background = Default.Background.ColorName;

			Initialized = false;
			if (Application.Driver != null) {
				Application.Driver.GetColors (platformColor, out foreground, out background);
				Initialized = true;
			}
			Value = platformColor;
			Foreground = (Color)foreground;
			Background = (Color)background;
		}

		/// <summary>
		/// Initializes a new instance with a <see cref="ColorNames"/> value.
		/// </summary>
		/// <param name="colorName">Value.</param>
		internal Attribute (ColorNames colorName)
		{
			ColorNames foreground = colorName;
			ColorNames background = colorName;

			Initialized = false;
			if (Application.Driver != null) {
				Application.Driver.GetColors ((int)colorName, out foreground, out background);
				Initialized = true;
			}
			Value = ((Color)colorName).Value;
			Foreground = (Color)foreground;
			Background = (Color)background;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="platformColor">platform-dependent color value.</param>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		internal Attribute (int platformColor, Color foreground, Color background)
		{
			Foreground = foreground;
			Background = background;
			Value = platformColor;
			Initialized = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="platformColor">platform-dependent color value.</param>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		internal Attribute (int platformColor, ColorNames foreground, ColorNames background)
		{
			Foreground = (Color)foreground;
			Background = (Color)background;
			Value = platformColor;
			Initialized = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (Color foreground, Color background)
		{
			Foreground = foreground;
			Background = background;

			var make = Make (foreground, background);
			Initialized = make.Initialized;
			Value = make.Value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (ColorNames foreground, ColorNames background)
		{
			Foreground = new Color (foreground);
			Background = new Color (background);

			var make = Make (foreground, background);
			Initialized = make.Initialized;
			Value = make.Value;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (ColorNames foreground, Color background)
		{
			Foreground = new Color (foreground);
			Background = background;

			var make = Make (foreground, background);
			Initialized = make.Initialized;
			Value = make.Value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (Color foreground, ColorNames background)
		{
			Foreground = foreground;
			Background = new Color (background);

			var make = Make (foreground, background);
			Initialized = make.Initialized;
			Value = make.Value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct
		///  with the same colors for the foreground and background.
		/// </summary>
		/// <param name="color">The color.</param>
		public Attribute (Color color) : this (color, color) { }


		/// <summary>
		/// Compares two attributes for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator == (Attribute left, Attribute right) => left.Equals (right);

		/// <summary>
		/// Compares two attributes for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator != (Attribute left, Attribute right) => !(left == right);

		/// <inheritdoc />
		public override bool Equals (object obj)
		{
			return obj is Attribute other && Equals (other);
		}

		/// <inheritdoc />
		public bool Equals (Attribute other)
		{
			return Value == other.Value &&
				Foreground == other.Foreground &&
				Background == other.Background;
		}

		/// <inheritdoc />
		public override int GetHashCode () => HashCode.Combine (Value, Foreground, Background);

		/// <summary>
		/// Creates an <see cref="Attribute"/> from the specified foreground and background colors.
		/// </summary>
		/// <remarks>
		/// If a <see cref="ConsoleDriver"/> has not been loaded (<c>Application.Driver == null</c>) this
		/// method will return an attribute with <see cref="Initialized"/> set to  <see langword="false"/>.
		/// </remarks>
		/// <returns>The new attribute.</returns>
		/// <param name="foreground">Foreground color to use.</param>
		/// <param name="background">Background color to use.</param>
		public static Attribute Make (Color foreground, Color background)
		{
			if (Application.Driver == null) {
				// Create the attribute, but show it's not been initialized
				return new Attribute () {
					Initialized = false,
					Foreground = foreground,
					Background = background
				};
			}
			return Application.Driver.MakeAttribute (foreground, background);
		}


		/// <summary>
		/// Creates an <see cref="Attribute"/> from the specified foreground and background colors.
		/// </summary>
		/// <remarks>
		/// If a <see cref="ConsoleDriver"/> has not been loaded (<c>Application.Driver == null</c>) this
		/// method will return an attribute with <see cref="Initialized"/> set to  <see langword="false"/>.
		/// </remarks>
		/// <returns>The new attribute.</returns>
		/// <param name="foreground">Foreground color to use.</param>
		/// <param name="background">Background color to use.</param>
		public static Attribute Make (ColorNames foreground, ColorNames background)
		{
			if (Application.Driver == null) {
				// Create the attribute, but show it's not been initialized
				return new Attribute () {
					Initialized = false,
					Foreground = new Color (foreground),
					Background = new Color (background)
				};
			}
			return Application.Driver.MakeAttribute (foreground, background);
		}

		/// <summary>
		/// Creates an <see cref="Attribute"/> from the specified foreground and background colors.
		/// </summary>
		/// <remarks>
		/// If a <see cref="ConsoleDriver"/> has not been loaded (<c>Application.Driver == null</c>) this
		/// method will return an attribute with <see cref="Initialized"/> set to  <see langword="false"/>.
		/// </remarks>
		/// <returns>The new attribute.</returns>
		/// <param name="foreground">Foreground color to use.</param>
		/// <param name="background">Background color to use.</param>
		public static Attribute Make (ColorNames foreground, Color background)
		{
			if (Application.Driver == null) {
				// Create the attribute, but show it's not been initialized
				return new Attribute () {
					Initialized = false,
					Foreground = new Color (foreground),
					Background = background
				};
			}
			return Application.Driver.MakeAttribute (new Color (foreground), background);
		}

		/// <summary>
		/// Creates an <see cref="Attribute"/> from the specified foreground and background colors.
		/// </summary>
		/// <remarks>
		/// If a <see cref="ConsoleDriver"/> has not been loaded (<c>Application.Driver == null</c>) this
		/// method will return an attribute with <see cref="Initialized"/> set to  <see langword="false"/>.
		/// </remarks>
		/// <returns>The new attribute.</returns>
		/// <param name="foreground">Foreground color to use.</param>
		/// <param name="background">Background color to use.</param>
		public static Attribute Make (Color foreground, ColorNames background)
		{
			if (Application.Driver == null) {
				// Create the attribute, but show it's not been initialized
				return new Attribute () {
					Initialized = false,
					Foreground = foreground,
					Background = new Color (background)
				};
			}
			return Application.Driver.MakeAttribute (foreground, new Color (background));
		}

		/// <summary>
		/// Gets the current <see cref="Attribute"/> from the driver.
		/// </summary>
		/// <returns>The current attribute.</returns>
		public static Attribute Get ()
		{
			if (Application.Driver == null) {
				throw new InvalidOperationException ("The Application has not been initialized");
			}
			return Application.Driver.GetAttribute ();
		}

		/// <summary>
		/// If <see langword="true"/> the attribute has been initialized by a <see cref="ConsoleDriver"/> and 
		/// thus has <see cref="Value"/> that is valid for that driver. If <see langword="false"/> the <see cref="Foreground"/>
		/// and <see cref="Background"/> colors may have been set '-1' but
		/// the attribute has not been mapped to a <see cref="ConsoleDriver"/> specific color value.
		/// </summary>
		/// <remarks>
		/// Attributes that have not been initialized must eventually be initialized before being passed to a driver.
		/// </remarks>
		[JsonIgnore]
		public bool Initialized { get; internal set; }

		/// <summary>
		/// Returns <see langword="true"/> if the Attribute is valid (both foreground and background have valid color values).
		/// </summary>
		/// <returns></returns>
		[JsonIgnore]
		public bool HasValidColors => (int)Foreground.ColorName > -1 && (int)Background.ColorName > -1;

		/// <inheritdoc />
		public override string ToString ()
		{
			// Note, Unit tests are dependent on this format
			return $"{Foreground},{Background}";
		}
	}

	/// <summary>
	/// Defines the color <see cref="Attribute"/>s for common visible elements in a <see cref="View"/>. 
	/// Containers such as <see cref="Window"/> and <see cref="FrameView"/> use <see cref="ColorScheme"/> to determine
	/// the colors used by sub-views.
	/// </summary>
	/// <remarks>
	/// See also: <see cref="Colors.ColorSchemes"/>.
	/// </remarks>
	[JsonConverter (typeof (ColorSchemeJsonConverter))]
	public class ColorScheme : IEquatable<ColorScheme> {
		Attribute _normal = Attribute.Default;
		Attribute _focus = Attribute.Default;
		Attribute _hotNormal = Attribute.Default;
		Attribute _hotFocus = Attribute.Default;
		Attribute _disabled = Attribute.Default;

		/// <summary>
		/// Used by <see cref="Colors.SetColorScheme(ColorScheme, string)"/> and <see cref="Colors.GetColorScheme(string)"/> to track which ColorScheme 
		/// is being accessed.
		/// </summary>
		internal string schemeBeingSet = "";

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ColorScheme () : this (Attribute.Default) { }

		/// <summary>
		/// Creates a new instance, initialized with the values from <paramref name="scheme"/>.
		/// </summary>
		/// <param name="scheme">The scheme to initialize the new instance with.</param>
		public ColorScheme (ColorScheme scheme) : base ()
		{
			if (scheme != null) {
				_normal = scheme.Normal;
				_focus = scheme.Focus;
				_hotNormal = scheme.HotNormal;
				_disabled = scheme.Disabled;
				_hotFocus = scheme.HotFocus;
			}
		}

		/// <summary>
		/// Creates a new instance, initialized with the values from <paramref name="attribute"/>.
		/// </summary>
		/// <param name="attribute">The attribute to initialize the new instance with.</param>
		public ColorScheme (Attribute attribute)
		{
			_normal = attribute;
			_focus = attribute;
			_hotNormal = attribute;
			_disabled = attribute;
			_hotFocus = attribute;
		}

		/// <summary>
		/// The foreground and background color for text when the view is not focused, hot, or disabled.
		/// </summary>
		public Attribute Normal {
			get { return _normal; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_normal = value;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view has the focus.
		/// </summary>
		public Attribute Focus {
			get { return _focus; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_focus = value;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view is highlighted (hot).
		/// </summary>
		public Attribute HotNormal {
			get { return _hotNormal; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_hotNormal = value;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view is highlighted (hot) and has focus.
		/// </summary>
		public Attribute HotFocus {
			get { return _hotFocus; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_hotFocus = value;
			}
		}

		/// <summary>
		/// The default foreground and background color for text, when the view is disabled.
		/// </summary>
		public Attribute Disabled {
			get { return _disabled; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_disabled = value;
			}
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for equality.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>true if the two objects are equal</returns>
		public override bool Equals (object obj)
		{
			return Equals (obj as ColorScheme);
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for equality.
		/// </summary>
		/// <param name="other"></param>
		/// <returns>true if the two objects are equal</returns>
		public bool Equals (ColorScheme other)
		{
			return other != null &&
		       EqualityComparer<Attribute>.Default.Equals (_normal, other._normal) &&
		       EqualityComparer<Attribute>.Default.Equals (_focus, other._focus) &&
		       EqualityComparer<Attribute>.Default.Equals (_hotNormal, other._hotNormal) &&
		       EqualityComparer<Attribute>.Default.Equals (_hotFocus, other._hotFocus) &&
		       EqualityComparer<Attribute>.Default.Equals (_disabled, other._disabled);
		}

		/// <summary>
		/// Returns a hashcode for this instance.
		/// </summary>
		/// <returns>hashcode for this instance</returns>
		public override int GetHashCode ()
		{
			int hashCode = -1242460230;
			hashCode = hashCode * -1521134295 + _normal.GetHashCode ();
			hashCode = hashCode * -1521134295 + _focus.GetHashCode ();
			hashCode = hashCode * -1521134295 + _hotNormal.GetHashCode ();
			hashCode = hashCode * -1521134295 + _hotFocus.GetHashCode ();
			hashCode = hashCode * -1521134295 + _disabled.GetHashCode ();
			return hashCode;
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><c>true</c> if the two objects are equivalent</returns>
		public static bool operator == (ColorScheme left, ColorScheme right)
		{
			return EqualityComparer<ColorScheme>.Default.Equals (left, right);
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><c>true</c> if the two objects are not equivalent</returns>
		public static bool operator != (ColorScheme left, ColorScheme right)
		{
			return !(left == right);
		}

		internal void Initialize ()
		{
			// If the new scheme was created before a driver was loaded, we need to re-make
			// the attributes
			if (!_normal.Initialized) {
				_normal = new Attribute (_normal.Foreground, _normal.Background);
			}
			if (!_focus.Initialized) {
				_focus = new Attribute (_focus.Foreground, _focus.Background);
			}
			if (!_hotNormal.Initialized) {
				_hotNormal = new Attribute (_hotNormal.Foreground, _hotNormal.Background);
			}
			if (!_hotFocus.Initialized) {
				_hotFocus = new Attribute (_hotFocus.Foreground, _hotFocus.Background);
			}
			if (!_disabled.Initialized) {
				_disabled = new Attribute (_disabled.Foreground, _disabled.Background);
			}
		}
	}

	/// <summary>
	/// The default <see cref="ColorScheme"/>s for the application.
	/// </summary>
	/// <remarks>
	/// This property can be set in a Theme to change the default <see cref="Colors"/> for the application.
	/// </remarks>
	public static class Colors {
		private class SchemeNameComparerIgnoreCase : IEqualityComparer<string> {
			public bool Equals (string x, string y)
			{
				if (x != null && y != null) {
					return string.Equals (x, y, StringComparison.InvariantCultureIgnoreCase);
				}
				return false;
			}

			public int GetHashCode (string obj)
			{
				return obj.ToLowerInvariant ().GetHashCode ();
			}
		}

		static Colors ()
		{
			ColorSchemes = Create ();
		}

		/// <summary>
		/// Creates a new dictionary of new <see cref="ColorScheme"/> objects.
		/// </summary>
		public static Dictionary<string, ColorScheme> Create ()
		{
			// Use reflection to dynamically create the default set of ColorSchemes from the list defined 
			// by the class. 
			return typeof (Colors).GetProperties ()
				.Where (p => p.PropertyType == typeof (ColorScheme))
				.Select (p => new KeyValuePair<string, ColorScheme> (p.Name, new ColorScheme ()))
				.ToDictionary (t => t.Key, t => t.Value, comparer: new SchemeNameComparerIgnoreCase ());
		}

		/// <summary>
		/// The application Toplevel color scheme, for the default Toplevel views.
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["TopLevel"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme TopLevel { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The base color scheme, for the default Toplevel views.
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Base"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Base { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The dialog color scheme, for standard popup dialog boxes
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Dialog"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Dialog { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The menu bar color
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Menu"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Menu { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The color scheme for showing errors.
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Error"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Error { get => GetColorScheme (); set => SetColorScheme (value); }

		static ColorScheme GetColorScheme ([CallerMemberName] string schemeBeingSet = null)
		{
			return ColorSchemes [schemeBeingSet];
		}

		static void SetColorScheme (ColorScheme colorScheme, [CallerMemberName] string schemeBeingSet = null)
		{
			ColorSchemes [schemeBeingSet] = colorScheme;
			colorScheme.schemeBeingSet = schemeBeingSet;
		}

		/// <summary>
		/// Provides the defined <see cref="ColorScheme"/>s.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
		[JsonConverter (typeof (DictionaryJsonConverter<ColorScheme>))]
		public static Dictionary<string, ColorScheme> ColorSchemes { get; private set; }
	}

}
