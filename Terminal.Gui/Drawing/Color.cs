#nullable enable
using System.Collections.Frozen;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Represents a 24-bit color encoded in ARGB32 format.
///   <para />
/// </summary>
/// <seealso cref="Attribute" />
/// <seealso cref="ColorExtensions" />
/// <seealso cref="ColorName" />
[JsonConverter (typeof (ColorJsonConverter))]
[StructLayout (LayoutKind.Explicit)]
public readonly partial record struct Color : ISpanParsable<Color>, IUtf8SpanParsable<Color>, ISpanFormattable, IUtf8SpanFormattable, IMinMaxValue<Color> {

	/// <summary>The value of the alpha channel component</summary>
	/// <remarks>
	///   The alpha channel is not currently supported, so the value of the alpha channel bits will not affect rendering.
	/// </remarks>
	[JsonIgnore]
	[field: FieldOffset (3)]
	public readonly byte A;

	/// <summary>
	///   The value of this <see cref="Color" /> as a <see langword="uint" /> in ARGB32 format.
	/// </summary>
	/// <remarks>
	///   The alpha channel is not currently supported, so the value of the alpha channel bits will not affect rendering.
	/// </remarks>
	[JsonIgnore]
	[field: FieldOffset (0)]
	public readonly uint Argb;

	/// <summary>The value of the blue color component.</summary>
	[JsonIgnore]
	[field: FieldOffset (0)]
	public readonly byte B;

	/// <summary>The value of the green color component.</summary>
	[JsonIgnore]
	[field: FieldOffset (1)]
	public readonly byte G;

	/// <summary>The value of the red color component.</summary>
	[JsonIgnore]
	[field: FieldOffset (2)]
	public readonly byte R;

	/// <summary>
	///   The value of this <see cref="Color" /> encoded as a signed 32-bit integer in ARGB32 format.
	/// </summary>
	[JsonIgnore]
	[field: FieldOffset (0)]
	public readonly int Rgba;
	/// <summary>
	///   Initializes a new instance of the <see cref="Color" /> <see langword="struct" /> using the supplied component values.
	/// </summary>
	/// <param name="red">The red 8-bits.</param>
	/// <param name="green">The green 8-bits.</param>
	/// <param name="blue">The blue 8-bits.</param>
	/// <param name="alpha">Optional; defaults to 0xFF. The Alpha channel is not supported by Terminal.Gui.</param>
	/// <remarks>Alpha channel is not currently supported by Terminal.Gui.</remarks>
	/// <exception cref="OverflowException">If the value of any parameter is greater than <see cref="byte.MaxValue" />.</exception>
	/// <exception cref="ArgumentOutOfRangeException">If the value of any parameter is negative.</exception>
	public Color (int red = 0, int green = 0, int blue = 0, int alpha = byte.MaxValue)
	{
		ArgumentOutOfRangeException.ThrowIfNegative (red, nameof (red));
		ArgumentOutOfRangeException.ThrowIfNegative (green, nameof (green));
		ArgumentOutOfRangeException.ThrowIfNegative (blue, nameof (blue));
		ArgumentOutOfRangeException.ThrowIfNegative (alpha, nameof (alpha));

		A = Convert.ToByte (alpha);
		R = Convert.ToByte (red);
		G = Convert.ToByte (green);
		B = Convert.ToByte (blue);
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="Color" /> class with an encoded signed 32-bit color value in ARGB32 format.
	/// </summary>
	/// <param name="rgba">The encoded 32-bit color value (see <see cref="Rgba" />).</param>
	/// <remarks>
	///   The alpha channel is not currently supported, so the value of the alpha channel bits will not affect rendering.
	/// </remarks>
	public Color (int rgba)
	{
		Rgba = rgba;
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="Color" /> class with an encoded unsigned 32-bit color value in ARGB32 format.
	/// </summary>
	/// <param name="argb">The encoded unsigned 32-bit color value (see <see cref="Argb" />).</param>
	/// <remarks>
	///   The alpha channel is not currently supported, so the value of the alpha channel bits will not affect rendering.
	/// </remarks>
	public Color (uint argb)
	{
		Argb = argb;
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="Color" /> color from a legacy 16-color named value.
	/// </summary>
	/// <param name="colorName">The 16-color value.</param>
	public Color (in ColorName colorName)
	{
		this = ColorExtensions.ColorNameToColorMap [colorName];
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="Color" /> color from string. See <see cref="TryParse(string, out Color?)" /> for details.
	/// </summary>
	/// <param name="colorString"></param>
	/// <exception cref="ArgumentNullException">If <paramref name="colorString" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">
	///   If <paramref name="colorString" /> is an empty string or consists of only whitespace characters.
	/// </exception>
	/// <exception cref="ColorParseException">If thrown by <see cref="Parse(string?,System.IFormatProvider?)" /></exception>
	public Color (string colorString)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace (colorString, nameof (colorString));
		this = Parse (colorString, CultureInfo.InvariantCulture);
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="Color" /> with all channels set to 0.
	/// </summary>
	public Color ()
	{
		Argb = 0u;
	}

	/// <summary>
	///   Gets or sets the 3-byte/6-character hexadecimal value for each of the legacy 16-color values.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
	public static Dictionary<ColorName, string> Colors {
		get =>
			// Transform _colorToNameMap into a Dictionary<ColorNames,string>
			ColorExtensions.ColorToNameMap.ToDictionary (static kvp => kvp.Value, static kvp => kvp.Key.ToString ("g"));
		set {
			// Transform Dictionary<ColorNames,string> into _colorToNameMap
			ColorExtensions.ColorToNameMap = value.ToFrozenDictionary (GetColorToNameMapKey, GetColorToNameMapValue);
			return;

			static Color GetColorToNameMapKey (KeyValuePair<ColorName, string> kvp) => new Color (kvp.Value);
			static ColorName GetColorToNameMapValue (KeyValuePair<ColorName, string> kvp) => Enum.TryParse<ColorName> (kvp.Key.ToString (), true, out var colorName) ? colorName : throw new ArgumentException ($"Invalid color name: {kvp.Key}");
		}
	}

	/// <summary>
	///   Gets the <see cref="Color" /> using a legacy 16-color <see cref="ColorName" /> value. <see langword="get" /> will return the closest 16
	///   color match to the true color when no exact value is found.
	/// </summary>
	/// <remarks>
	///   Get returns the <see cref="GetClosestNamedColor" /> of the closest 24-bit color value. Set sets the RGB value using a hard-coded map.
	/// </remarks>
	public AnsiColorCode GetAnsiColorCode () => ColorExtensions.ColorNameToAnsiColorMap [GetClosestNamedColor ()];

	/// <summary>
	///   Gets the <see cref="Color" /> using a legacy 16-color <see cref="Gui.ColorName" /> value. <see langword="get" /> will return the closest
	///   16 color match to the true color when no exact value is found.
	/// </summary>
	/// <remarks>
	///   Get returns the <see cref="GetClosestNamedColor" /> of the closest 24-bit color value. Set sets the RGB value using a hard-coded map.
	/// </remarks>
	public ColorName GetClosestNamedColor () => GetClosestNamedColor (this);

	/// <summary>
	///   Determines if the closest named <see cref="Color" /> to <see langword="this" /> is the provided <paramref name="namedColor" />.
	/// </summary>
	/// <param name="namedColor">
	///   The <see cref="GetClosestNamedColor" /> to check if this <see cref="Color" /> is closer to than any other configured named color.
	/// </param>
	/// <returns>
	///   <see langword="true" /> if the closest named color is the provided value.
	///   <br />
	///   <see langword="false" /> if any other named color is closer to this <see cref="Color" /> than <paramref name="namedColor" />.
	/// </returns>
	/// <remarks>
	///   If <see langword="this" /> is equidistant from two named colors, the result of this method is not guaranteed to be determinate.
	/// </remarks>
	[Pure]
	[MethodImpl (MethodImplOptions.AggressiveInlining)]
	public bool IsClosestToNamedColor (in ColorName namedColor) => GetClosestNamedColor () == namedColor;


	/// <summary>
	///   Determines if the closest named <see cref="Color" /> to <paramref name="color" />/> is the provided <paramref name="namedColor" />.
	/// </summary>
	/// <param name="color">
	///   The color to test against the <see cref="GetClosestNamedColor" /> value in <paramref name="namedColor" />.
	/// </param>
	/// <param name="namedColor">
	///   The <see cref="GetClosestNamedColor" /> to check if this <see cref="Color" /> is closer to than any other configured named color.
	/// </param>
	/// <returns>
	///   <see langword="true" /> if the closest named color to <paramref name="color" /> is the provided value.
	///   <br />
	///   <see langword="false" /> if any other named color is closer to <paramref name="color" /> than <paramref name="namedColor" />.
	/// </returns>
	/// <remarks>
	///   If <paramref name="color" /> is equidistant from two named colors, the result of this method is not guaranteed to be determinate.
	/// </remarks>
	[Pure]
	[MethodImpl (MethodImplOptions.AggressiveInlining)]
	public static bool IsColorClosestToNamedColor (in Color color, in ColorName namedColor)
	{
		return color.IsClosestToNamedColor (in namedColor);
	}

	/// <summary>Gets the "closest" named color to this <see cref="Color" /> value.</summary>
	/// <param name="inputColor"></param>
	/// <remarks>
	///   Distance is defined here as the Euclidean distance between each color interpreted as a <see cref="Vector3" />.
	///   <para />
	///   The order of the values in the passed Vector3 must be
	/// </remarks>
	/// <returns></returns>
	[SkipLocalsInit]
	internal static ColorName GetClosestNamedColor (Color inputColor) => ColorExtensions.ColorToNameMap.MinBy (pair => CalculateColorDistance (inputColor, pair.Key)).Value;

	[SkipLocalsInit]
	static float CalculateColorDistance (in Vector4 color1, in Vector4 color2) => Vector4.Distance (color1, color2);

	#region Legacy Color Names
	/// <summary>The black color.</summary>
	public const ColorName Black = ColorName.Black;

	/// <summary>The blue color.</summary>
	public const ColorName Blue = ColorName.Blue;

	/// <summary>The green color.</summary>
	public const ColorName Green = ColorName.Green;

	/// <summary>The cyan color.</summary>
	public const ColorName Cyan = ColorName.Cyan;

	/// <summary>The red color.</summary>
	public const ColorName Red = ColorName.Red;

	/// <summary>The magenta color.</summary>
	public const ColorName Magenta = ColorName.Magenta;

	/// <summary>The yellow color.</summary>
	public const ColorName Yellow = ColorName.Yellow;

	/// <summary>The gray color.</summary>
	public const ColorName Gray = ColorName.Gray;

	/// <summary>The dark gray color.</summary>
	public const ColorName DarkGray = ColorName.DarkGray;

	/// <summary>The bright bBlue color.</summary>
	public const ColorName BrightBlue = ColorName.BrightBlue;

	/// <summary>The bright green color.</summary>
	public const ColorName BrightGreen = ColorName.BrightGreen;

	/// <summary>The bright cyan color.</summary>
	public const ColorName BrightCyan = ColorName.BrightCyan;

	/// <summary>The bright red color.</summary>
	public const ColorName BrightRed = ColorName.BrightRed;

	/// <summary>The bright magenta color.</summary>
	public const ColorName BrightMagenta = ColorName.BrightMagenta;

	/// <summary>The bright yellow color.</summary>
	public const ColorName BrightYellow = ColorName.BrightYellow;

	/// <summary>The White color.</summary>
	public const ColorName White = ColorName.White;
	#endregion
}
