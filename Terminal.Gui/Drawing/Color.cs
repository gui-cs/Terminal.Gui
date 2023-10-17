global using Attribute = Terminal.Gui.Attribute;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Terminal.Gui;
/// <summary>
/// Defines the 16 legacy color names and values that can be used to set the
/// foreground and background colors in Terminal.Gui apps. Used with <see cref="Color"/>.
/// </summary>
/// <remarks>
/// <para>
/// These colors match the 16 colors defined for ANSI escape sequences for 4-bit (16) colors.
/// </para>
/// <para>
/// For terminals that support 24-bit color (TrueColor), the RGB values for each of these colors can be configured using the
/// <see cref="Color.Colors"/> property.
/// </para>
/// </remarks>
public enum ColorName {
	/// <summary>
	/// The black color. ANSI escape sequence: <c>\u001b[30m</c>.
	/// </summary>
	Black,
	/// <summary>
	/// The blue color. ANSI escape sequence: <c>\u001b[34m</c>.
	/// </summary>
	Blue,
	/// <summary>
	/// The green color. ANSI escape sequence: <c>\u001b[32m</c>.
	/// </summary>
	Green,
	/// <summary>
	/// The cyan color. ANSI escape sequence: <c>\u001b[36m</c>.
	/// </summary>
	Cyan,
	/// <summary>
	/// The red color. ANSI escape sequence: <c>\u001b[31m</c>.
	/// </summary>
	Red,
	/// <summary>
	/// The magenta color. ANSI escape sequence: <c>\u001b[35m</c>.
	/// </summary>
	Magenta,
	/// <summary>
	/// The yellow color (also known as Brown). ANSI escape sequence: <c>\u001b[33m</c>.
	/// </summary>
	Yellow,
	/// <summary>
	/// The gray color (also known as White). ANSI escape sequence: <c>\u001b[37m</c>.
	/// </summary>
	Gray,
	/// <summary>
	/// The dark gray color (also known as Bright Black). ANSI escape sequence: <c>\u001b[30;1m</c>.
	/// </summary>
	DarkGray,
	/// <summary>
	/// The bright blue color. ANSI escape sequence: <c>\u001b[34;1m</c>.
	/// </summary>
	BrightBlue,
	/// <summary>
	/// The bright green color. ANSI escape sequence: <c>\u001b[32;1m</c>.
	/// </summary>
	BrightGreen,
	/// <summary>
	/// The bright cyan color. ANSI escape sequence: <c>\u001b[36;1m</c>.
	/// </summary>
	BrightCyan,
	/// <summary>
	/// The bright red color. ANSI escape sequence: <c>\u001b[31;1m</c>.
	/// </summary>
	BrightRed,
	/// <summary>
	/// The bright magenta color. ANSI escape sequence: <c>\u001b[35;1m</c>.
	/// </summary>
	BrightMagenta,
	/// <summary>
	/// The bright yellow color. ANSI escape sequence: <c>\u001b[33;1m</c>.
	/// </summary>
	BrightYellow,
	/// <summary>
	/// The White color (also known as Bright White). ANSI escape sequence: <c>\u001b[37;1m</c>.
	/// </summary>
	White
}

/// <summary>
/// Represents a 24-bit color. Provides automatic mapping between the legacy 4-bit (16 color) system and 24-bit colors (see <see cref="ColorName"/>).
/// Used with <see cref="Attribute"/>. 
/// </summary>
[JsonConverter (typeof (ColorJsonConverter))]
public class Color : IEquatable<Color> {

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> class.
	/// </summary>
	/// <param name="red">The red 8-bits.</param>
	/// <param name="green">The green 8-bits.</param>
	/// <param name="blue">The blue 8-bits.</param>
	/// <param name="alpha">Optional; defaults to 0xFF. The Alpha channel is not supported by Terminal.Gui.</param>
	public Color (int red, int green, int blue, int alpha = 0xFF)
	{
		R = red;
		G = green;
		B = blue;
		A = alpha;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> class with an encoded 24-bit color value.
	/// </summary>
	/// <param name="rgba">The encoded 24-bit color value (see <see cref="Rgba"/>).</param>
	public Color (int rgba)
	{
		Rgba = rgba;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> color from a legacy 16-color value.
	/// </summary>
	/// <param name="colorName">The 16-color value.</param>
	public Color (ColorName colorName)
	{
		var c = Color.FromColorName (colorName);
		R = c.R;
		G = c.G;
		B = c.B;
		A = c.A;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> color from string. See <see cref="TryParse(string, out Color)"/> for details.
	/// </summary>
	/// <param name="colorString"></param>
	/// <exception cref="Exception"></exception>
	public Color (string colorString)
	{
		if (!TryParse (colorString, out var c)) {
			throw new ArgumentOutOfRangeException (nameof (colorString));
		}
		R = c.R;
		G = c.G;
		B = c.B;
		A = c.A;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/>.
	/// </summary>
	public Color ()
	{
		R = 0;
		G = 0;
		B = 0;
		A = 0xFF;
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
	/// The Alpha channel is not supported by Terminal.Gui.
	/// </remarks>
	public int A { get; set; } = 0xFF; // Not currently supported; here for completeness.

	/// <summary>
	/// Gets or sets the color value encoded as ARGB32.
	/// <code>
	/// (&lt;see cref="A"/&gt; &lt;&lt; 24) | (&lt;see cref="R"/&gt; &lt;&lt; 16) | (&lt;see cref="G"/&gt; &lt;&lt; 8) | &lt;see cref="B"/&gt;
	/// </code>
	/// </summary>
	public int Rgba {
		get => (A << 24) | (R << 16) | (G << 8) | B;
		set {
			A = (byte)((value >> 24) & 0xFF);
			R = (byte)((value >> 16) & 0xFF);
			G = (byte)((value >> 8) & 0xFF);
			B = (byte)(value & 0xFF);
		}
	}

	// TODO: Make this map configurable via ConfigurationManager
	// TODO: This does not need to be a Dictionary, but can be an 16 element array.
	/// <summary>
	/// Maps legacy 16-color values to the corresponding 24-bit RGB value.
	/// </summary>
	internal static ImmutableDictionary<Color, ColorName> _colorToNameMap = new Dictionary<Color, ColorName> () {
			// using "Windows 10 Console/PowerShell 6" here: https://i.stack.imgur.com/9UVnC.png
			// See also: https://en.wikipedia.org/wiki/ANSI_escape_code
			{ new Color (12, 12, 12),Gui.ColorName.Black },
			{ new Color (0, 55, 218),Gui.ColorName.Blue },
			{ new Color (19, 161, 14),Gui.ColorName.Green},
			{ new Color (58, 150, 221),Gui.ColorName.Cyan},
			{ new Color (197, 15, 31),Gui.ColorName.Red},
			{ new Color (136, 23, 152),Gui.ColorName.Magenta},
			{ new Color (128, 64, 32),Gui.ColorName.Yellow},
			{ new Color (204, 204, 204),Gui.ColorName.Gray},
			{ new Color (118, 118, 118),Gui.ColorName.DarkGray},
			{ new Color (59, 120, 255),Gui.ColorName.BrightBlue},
			{ new Color (22, 198, 12),Gui.ColorName.BrightGreen},
			{ new Color (97, 214, 214),Gui.ColorName.BrightCyan},
			{ new Color (231, 72, 86),Gui.ColorName.BrightRed},
			{ new Color (180, 0, 158),Gui.ColorName.BrightMagenta },
			{ new Color (249, 241, 165),Gui.ColorName.BrightYellow},
			{ new Color (242, 242, 242),Gui.ColorName.White},
		}.ToImmutableDictionary ();

	/// <summary>
	/// Gets or sets the 24-bit color value for each of the legacy 16-color values.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
	public static Dictionary<ColorName, string> Colors {
		get {
			// Transform _colorToNameMap into a Dictionary<ColorNames,string>
			return _colorToNameMap.ToDictionary (kvp => kvp.Value, kvp => $"#{kvp.Key.R:X2}{kvp.Key.G:X2}{kvp.Key.B:X2}");
		}
		set {
			// Transform Dictionary<ColorNames,string> into _colorToNameMap
			var newMap = value.ToDictionary (kvp => new Color (kvp.Value), kvp => {
				if (Enum.TryParse<ColorName> (kvp.Key.ToString (), ignoreCase: true, out ColorName colorName)) {
					return colorName;
				}
				throw new ArgumentException ($"Invalid color name: {kvp.Key}");
			});
			_colorToNameMap = newMap.ToImmutableDictionary ();
		}
	}

	/// <summary>
	/// Converts a legacy <see cref="Gui.ColorName"/> to a 24-bit <see cref="Color"/>.
	/// </summary>
	/// <param name="consoleColor">The <see cref="Color"/> to convert.</param>
	/// <returns></returns>
	private static Color FromColorName (ColorName consoleColor) => _colorToNameMap.FirstOrDefault (x => x.Value == consoleColor).Key;

	// Iterates through the entries in the _colorNames dictionary, calculates the
	// Euclidean distance between the input color and each dictionary color in RGB space,
	// and keeps track of the closest entry found so far. The function returns a KeyValuePair
	// representing the closest color entry and its associated color name.
	internal static ColorName FindClosestColor (Color inputColor)
	{
		ColorName closestColor = Gui.ColorName.Black; // Default to Black
		double closestDistance = double.MaxValue;

		foreach (var colorEntry in _colorToNameMap) {
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

	/// <summary>
	/// Gets or sets the <see cref="Color"/> using a legacy 16-color <see cref="Gui.ColorName"/> value.
	/// <see langword="get"/> will return the closest 16 color match to the true color when no exact value is found.
	/// </summary>
	/// <remarks>
	/// Get returns the <see cref="ColorName"/> of the closest 24-bit color value. Set sets the RGB value using a hard-coded map.
	/// </remarks>
	public ColorName ColorName {
		get => FindClosestColor (this);
		set {

			var c = FromColorName (value);
			R = c.R;
			G = c.G;
			B = c.B;
			A = c.A;
		}
	}

	#region Legacy Color Names
	/// <summary>
	/// The black color.
	/// </summary>
	public const ColorName Black = ColorName.Black;

	/// <summary>
	/// The blue color.
	/// </summary>
	public const ColorName Blue = ColorName.Blue;
	/// <summary>
	/// The green color.
	/// </summary>
	public const ColorName Green = ColorName.Green;
	/// <summary>
	/// The cyan color.
	/// </summary>
	public const ColorName Cyan = ColorName.Cyan;
	/// <summary>
	/// The red color.
	/// </summary>
	public const ColorName Red = ColorName.Red;
	/// <summary>
	/// The magenta color.
	/// </summary>
	public const ColorName Magenta = ColorName.Magenta;
	/// <summary>
	/// The yellow color.
	/// </summary>
	public const ColorName Yellow = ColorName.Yellow;
	/// <summary>
	/// The gray color.
	/// </summary>
	public const ColorName Gray = ColorName.Gray;
	/// <summary>
	/// The dark gray color.
	/// </summary>
	public const ColorName DarkGray = ColorName.DarkGray;
	/// <summary>
	/// The bright bBlue color.
	/// </summary>
	public const ColorName BrightBlue = ColorName.BrightBlue;
	/// <summary>
	/// The bright green color.
	/// </summary>
	public const ColorName BrightGreen = ColorName.BrightGreen;
	/// <summary>
	/// The bright cyan color.
	/// </summary>
	public const ColorName BrightCyan = ColorName.BrightCyan;
	/// <summary>
	/// The bright red color.
	/// </summary>
	public const ColorName BrightRed = ColorName.BrightRed;
	/// <summary>
	/// The bright magenta color.
	/// </summary>
	public const ColorName BrightMagenta = ColorName.BrightMagenta;
	/// <summary>
	/// The bright yellow color.
	/// </summary>
	public const ColorName BrightYellow = ColorName.BrightYellow;
	/// <summary>
	/// The White color.
	/// </summary>
	public const ColorName White = ColorName.White;
	#endregion

	/// <summary>
	/// Converts the provided string to a new <see cref="Color"/> instance.
	/// </summary>
	/// <param name="text">The text to analyze. Formats supported are
	/// "#RGB", "#RRGGBB", "#RGBA", "#RRGGBBAA", "rgb(r,g,b)", "rgb(r,g,b,a)", and any of the
	/// <see cref="Gui.ColorName"/>.</param>
	/// <param name="color">The parsed value.</param>
	/// <returns>A boolean value indicating whether parsing was successful.</returns>
	/// <remarks>
	/// While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.
	/// </remarks>
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

		// #RRGGBB, #RGBA
		if ((text [0] == '#') && text.Length is 8 or 5) {
			if (text.Length == 7) {
				var r = Convert.ToInt32 (text.Substring (1, 2), 16);
				var g = Convert.ToInt32 (text.Substring (3, 2), 16);
				var b = Convert.ToInt32 (text.Substring (5, 2), 16);
				var a = Convert.ToInt32 (text.Substring (7, 2), 16);
				color = new Color (a, r, g, b);
			} else {
				var rText = char.ToString (text [1]);
				var gText = char.ToString (text [2]);
				var bText = char.ToString (text [3]);
				var aText = char.ToString (text [4]);

				var r = Convert.ToInt32 (aText + aText, 16);
				var g = Convert.ToInt32 (rText + rText, 16);
				var b = Convert.ToInt32 (gText + gText, 16);
				var a = Convert.ToInt32 (bText + bText, 16);
				color = new Color (r, g, b, a);
			}
			return true;
		}

		// rgb(r,g,b)
		var match = Regex.Match (text, @"rgb\((\d+),(\d+),(\d+)\)");
		if (match.Success) {
			var r = int.Parse (match.Groups [1].Value);
			var g = int.Parse (match.Groups [2].Value);
			var b = int.Parse (match.Groups [3].Value);
			color = new Color (r, g, b);
			return true;
		}

		// rgb(r,g,b,a)
		match = Regex.Match (text, @"rgb\((\d+),(\d+),(\d+),(\d+)\)");
		if (match.Success) {
			var r = int.Parse (match.Groups [1].Value);
			var g = int.Parse (match.Groups [2].Value);
			var b = int.Parse (match.Groups [3].Value);
			var a = int.Parse (match.Groups [4].Value);
			color = new Color (r, g, b, a);
			return true;
		}

		if (Enum.TryParse<ColorName> (text, ignoreCase: true, out ColorName colorName)) {
			color = new Color (colorName);
			return true;
		}

		color = null;
		return false;
	}

	#region Operators
	/// <summary>
	/// Cast from int.
	/// </summary>
	/// <param name="rgba"></param>
	public static implicit operator Color (int rgba)
	{
		return new Color (rgba);
	}

	/// <summary>
	/// Cast to int.
	/// </summary>
	/// <param name="color"></param>
	public static explicit operator int (Color color)
	{
		return color.Rgba;
	}

	/// <summary>
	/// Cast from <see cref="Gui.ColorName"/>.
	/// </summary>
	/// <param name="colorName"></param>
	public static explicit operator Color (ColorName colorName)
	{
		return new Color (colorName);
	}

	/// <summary>
	/// Cast to <see cref="Gui.ColorName"/>.
	/// </summary>
	/// <param name="color"></param>
	public static explicit operator ColorName (Color color)
	{
		return color.ColorName;
	}


	/// <summary>
	/// Equality operator for two <see cref="Color"/> objects..
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator == (Color left, Color right)
	{
		if (left is null && right is null)
			return true;

		if (left is null || right is null)
			return false;

		return left.Equals (right);
	}


	/// <summary>
	/// Inequality operator for two <see cref="Color"/> objects.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator != (Color left, Color right)
	{
		if (left is null && right is null)
			return false;

		if (left is null || right is null)
			return true;

		return !left.Equals (right);
	}

	/// <summary>
	/// Equality operator for <see cref="Color"/> and <see cref="Gui.ColorName"/> objects.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator == (ColorName left, Color right)
	{
		return left == right.ColorName;
	}

	/// <summary>
	/// Inequality operator for <see cref="Color"/> and <see cref="Gui.ColorName"/> objects.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator != (ColorName left, Color right)
	{
		return left != right.ColorName;
	}

	/// <summary>
	/// Equality operator for <see cref="Color"/> and <see cref="Gui.ColorName"/> objects.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator == (Color left, ColorName right)
	{
		return left.ColorName == right;
	}

	/// <summary>
	/// Inequality operator for <see cref="Color"/> and <see cref="Gui.ColorName"/> objects.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator != (Color left, ColorName right)
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
			R == other.R &&
			G == other.G &&
			B == other.B &&
			A == other.A;
	}

	/// <inheritdoc/>
	public override int GetHashCode ()
	{
		return HashCode.Combine (R, G, B, A);
	}
	#endregion

	/// <summary>
	/// Converts the color to a string representation.
	/// </summary>
	/// <remarks>
	/// <para>
	/// If the color is a named color, the name is returned. Otherwise, the color is returned as a hex string.
	/// </para>
	/// <para>
	/// <see cref="A"/> (Alpha channel) is ignored and the returned string will not include it.
	/// </para>
	/// </remarks>
	/// <returns></returns>
	public override string ToString ()
	{
		// If Values has an exact match with a named color (in _colorNames), use that.
		if (_colorToNameMap.TryGetValue (this, out ColorName colorName)) {
			return Enum.GetName (typeof (ColorName), colorName);
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
public readonly struct Attribute : IEquatable<Attribute> {

	/// <summary>
	/// Default empty attribute.
	/// </summary>
	public static readonly Attribute Default = new Attribute (Color.White, Color.Black);

	/// <summary>
	/// The <see cref="ConsoleDriver"/>-specific color value.
	/// </summary>
	[JsonIgnore (Condition = JsonIgnoreCondition.Always)]
	internal int PlatformColor { get; }

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
		PlatformColor = -1;
		Foreground = d.Foreground;
		Background = d.Background;
	}

	/// <summary>
	/// Initializes a new instance with platform specific color value.
	/// </summary>
	/// <param name="platformColor">Value.</param>
	internal Attribute (int platformColor) : this (platformColor, Default.Foreground, Default.Background) { }

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
		PlatformColor = platformColor;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Attribute"/> struct.
	/// </summary>
	/// <param name="platformColor">platform-dependent color value.</param>
	/// <param name="foreground">Foreground</param>
	/// <param name="background">Background</param>
	internal Attribute (int platformColor, ColorName foreground, ColorName background) : this (platformColor, new Color (foreground), new Color (background)) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="Attribute"/> struct.
	/// </summary>
	/// <param name="foreground">Foreground</param>
	/// <param name="background">Background</param>
	public Attribute (Color foreground, Color background)
	{
		Foreground = foreground;
		Background = background;

		if (Application.Driver == null) {
			PlatformColor = -1;
			return;
		}

		var make = Application.Driver.MakeAttribute (foreground, background);
		PlatformColor = make.PlatformColor;
	}

	/// <summary>
	/// Initializes a new instance with a <see cref="ColorName"/> value. Both <see cref="Foreground"/> and
	/// <see cref="Background"/> will be set to the specified color.
	/// </summary>
	/// <param name="colorName">Value.</param>
	internal Attribute (ColorName colorName) : this (colorName, colorName) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="Attribute"/> struct.
	/// </summary>
	/// <param name="foregroundName">Foreground</param>
	/// <param name="backgroundName">Background</param>
	public Attribute (ColorName foregroundName, ColorName backgroundName) : this (new Color (foregroundName), new Color (backgroundName)) { }


	/// <summary>
	/// Initializes a new instance of the <see cref="Attribute"/> struct.
	/// </summary>
	/// <param name="foregroundName">Foreground</param>
	/// <param name="background">Background</param>
	public Attribute (ColorName foregroundName, Color background) : this (new Color (foregroundName), background) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="Attribute"/> struct.
	/// </summary>
	/// <param name="foreground">Foreground</param>
	/// <param name="backgroundName">Background</param>
	public Attribute (Color foreground, ColorName backgroundName) : this (foreground, new Color (backgroundName)) { }

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
		return PlatformColor == other.PlatformColor &&
			Foreground == other.Foreground &&
			Background == other.Background;
	}

	/// <inheritdoc />
	public override int GetHashCode () => HashCode.Combine (PlatformColor, Foreground, Background);

	/// <inheritdoc />
	public override string ToString ()
	{
		// Note, Unit tests are dependent on this format
		return $"{Foreground},{Background}";
	}
}

/// <summary>
/// Defines the <see cref="Attribute"/>s for common visible elements in a <see cref="View"/>. 
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
	internal string _schemeBeingSet = "";

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
		get => _normal;
		set => _normal = value;
	}

	/// <summary>
	/// The foreground and background color for text when the view has the focus.
	/// </summary>
	public Attribute Focus {
		get => _focus;
		set => _focus = value;
	}

	/// <summary>
	/// The foreground and background color for text when the view is highlighted (hot).
	/// </summary>
	public Attribute HotNormal {
		get => _hotNormal;
		set => _hotNormal = value;
	}

	/// <summary>
	/// The foreground and background color for text when the view is highlighted (hot) and has focus.
	/// </summary>
	public Attribute HotFocus {
		get => _hotFocus;
		set => _hotFocus = value;
	}

	/// <summary>
	/// The default foreground and background color for text, when the view is disabled.
	/// </summary>
	public Attribute Disabled {
		get => _disabled;
		set => _disabled = value;
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
		colorScheme._schemeBeingSet = schemeBeingSet;
	}

	/// <summary>
	/// Provides the defined <see cref="ColorScheme"/>s.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
	[JsonConverter (typeof (DictionaryJsonConverter<ColorScheme>))]
	public static Dictionary<string, ColorScheme> ColorSchemes { get; private set; }
}
