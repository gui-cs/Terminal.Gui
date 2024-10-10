#nullable enable
using System.Collections.Frozen;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ColorHelper;

namespace Terminal.Gui;

/// <summary>
///     Represents a 24-bit color encoded in ARGB32 format.
///     <para/>
/// </summary>
/// <seealso cref="Attribute"/>
/// <seealso cref="ColorExtensions"/>
/// <seealso cref="ColorName16"/>
[JsonConverter (typeof (ColorJsonConverter))]
[StructLayout (LayoutKind.Explicit)]
public readonly partial record struct Color : ISpanParsable<Color>, IUtf8SpanParsable<Color>, ISpanFormattable,
                                              IUtf8SpanFormattable, IMinMaxValue<Color>
{
    /// <summary>The value of the alpha channel component</summary>
    /// <remarks>
    ///     The alpha channel is not currently supported, so the value of the alpha channel bits will not affect
    ///     rendering.
    /// </remarks>
    [JsonIgnore]
    [field: FieldOffset (3)]
    public readonly byte A;

    /// <summary>The value of this <see cref="Color"/> as a <see langword="uint"/> in ARGB32 format.</summary>
    /// <remarks>
    ///     The alpha channel is not currently supported, so the value of the alpha channel bits will not affect
    ///     rendering.
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

    /// <summary>The value of this <see cref="Color"/> encoded as a signed 32-bit integer in ARGB32 format.</summary>
    [JsonIgnore]
    [field: FieldOffset (0)]
    public readonly int Rgba;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Color"/> <see langword="struct"/> using the supplied component
    ///     values.
    /// </summary>
    /// <param name="red">The red 8-bits.</param>
    /// <param name="green">The green 8-bits.</param>
    /// <param name="blue">The blue 8-bits.</param>
    /// <param name="alpha">Optional; defaults to 0xFF. The Alpha channel is not supported by Terminal.Gui.</param>
    /// <remarks>Alpha channel is not currently supported by Terminal.Gui.</remarks>
    /// <exception cref="OverflowException">If the value of any parameter is greater than <see cref="byte.MaxValue"/>.</exception>
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
    ///     Initializes a new instance of the <see cref="Color"/> class with an encoded signed 32-bit color value in
    ///     ARGB32 format.
    /// </summary>
    /// <param name="rgba">The encoded 32-bit color value (see <see cref="Rgba"/>).</param>
    /// <remarks>
    ///     The alpha channel is not currently supported, so the value of the alpha channel bits will not affect
    ///     rendering.
    /// </remarks>
    public Color (int rgba) { Rgba = rgba; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Color"/> class with an encoded unsigned 32-bit color value in
    ///     ARGB32 format.
    /// </summary>
    /// <param name="argb">The encoded unsigned 32-bit color value (see <see cref="Argb"/>).</param>
    /// <remarks>
    ///     The alpha channel is not currently supported, so the value of the alpha channel bits will not affect
    ///     rendering.
    /// </remarks>
    public Color (uint argb) { Argb = argb; }

    /// <summary>Initializes a new instance of the <see cref="Color"/> color from a legacy 16-color named value.</summary>
    /// <param name="colorName">The 16-color value.</param>
    public Color (in ColorName16 colorName) { this = ColorExtensions.ColorName16ToColorMap [colorName]; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Color"/> color from string. See
    ///     <see cref="TryParse(string, out Color?)"/> for details.
    /// </summary>
    /// <param name="colorString"></param>
    /// <exception cref="ArgumentNullException">If <paramref name="colorString"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    ///     If <paramref name="colorString"/> is an empty string or consists of only whitespace
    ///     characters.
    /// </exception>
    /// <exception cref="ColorParseException">If thrown by <see cref="Parse(string?,System.IFormatProvider?)"/></exception>
    public Color (string colorString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace (colorString, nameof (colorString));
        this = Parse (colorString, CultureInfo.InvariantCulture);
    }

    /// <summary>Initializes a new instance of the <see cref="Color"/> with all channels set to 0.</summary>
    public Color () { Argb = 0u; }

    // TODO: ColorName and AnsiColorCode are only needed when a driver is in Force16Color mode and we
    // TODO: should be able to remove these from any non-Driver-specific usages.
    /// <summary>Gets or sets the 3-byte/6-character hexadecimal value for each of the legacy 16-color values.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    public static Dictionary<ColorName16, string> Colors16
    {
        get =>

            // Transform _colorToNameMap into a Dictionary<ColorNames,string>
            ColorExtensions.ColorToName16Map.ToDictionary (static kvp => kvp.Value, static kvp => kvp.Key.ToString ("g"));
        set
        {
            // Transform Dictionary<ColorNames,string> into _colorToNameMap
            ColorExtensions.ColorToName16Map = value.ToFrozenDictionary (GetColorToNameMapKey, GetColorToNameMapValue);

            return;

            static Color GetColorToNameMapKey (KeyValuePair<ColorName16, string> kvp) { return new Color (kvp.Value); }

            static ColorName16 GetColorToNameMapValue (KeyValuePair<ColorName16, string> kvp)
            {
                return Enum.TryParse (kvp.Key.ToString (), true, out ColorName16 colorName)
                           ? colorName
                           : throw new ArgumentException ($"Invalid color name: {kvp.Key}");
            }
        }
    }

    /// <summary>
    ///     Gets the <see cref="Color"/> using a legacy 16-color <see cref="ColorName16"/> value. <see langword="get"/> will
    ///     return the closest 16 color match to the true color when no exact value is found.
    /// </summary>
    /// <remarks>
    ///     Get returns the <see cref="GetClosestNamedColor16(Color)"/> of the closest 24-bit color value. Set sets the RGB
    ///     value using a hard-coded map.
    /// </remarks>
    public AnsiColorCode GetAnsiColorCode () { return ColorExtensions.ColorName16ToAnsiColorMap [GetClosestNamedColor16 ()]; }

    /// <summary>
    ///     Gets the <see cref="Color"/> using a legacy 16-color <see cref="ColorName16"/> value. <see langword="get"/>
    ///     will return the closest 16 color match to the true color when no exact value is found.
    /// </summary>
    /// <remarks>
    ///     Get returns the <see cref="GetClosestNamedColor16(Terminal.Gui.Color)"/> of the closest 24-bit color value. Set sets the RGB
    ///     value using a hard-coded map.
    /// </remarks>
    public ColorName16 GetClosestNamedColor16 () { return GetClosestNamedColor16 (this); }

    /// <summary>
    ///     Determines if the closest named <see cref="Color"/> to <see langword="this"/> is the provided
    ///     <paramref name="namedColor"/>.
    /// </summary>
    /// <param name="namedColor">
    ///     The <see cref="GetClosestNamedColor16(Terminal.Gui.Color)"/> to check if this <see cref="Color"/> is closer
    ///     to than any other configured named color.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the closest named color is the provided value. <br/> <see langword="false"/> if any
    ///     other named color is closer to this <see cref="Color"/> than <paramref name="namedColor"/>.
    /// </returns>
    /// <remarks>
    ///     If <see langword="this"/> is equidistant from two named colors, the result of this method is not guaranteed to
    ///     be determinate.
    /// </remarks>
    [Pure]
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public bool IsClosestToNamedColor16 (in ColorName16 namedColor) { return GetClosestNamedColor16 () == namedColor; }

    /// <summary>
    ///     Determines if the closest named <see cref="Color"/> to <paramref name="color"/>/> is the provided
    ///     <paramref name="namedColor"/>.
    /// </summary>
    /// <param name="color">
    ///     The color to test against the <see cref="GetClosestNamedColor16(Terminal.Gui.Color)"/> value in
    ///     <paramref name="namedColor"/>.
    /// </param>
    /// <param name="namedColor">
    ///     The <see cref="GetClosestNamedColor16(Terminal.Gui.Color)"/> to check if this <see cref="Color"/> is closer
    ///     to than any other configured named color.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the closest named color to <paramref name="color"/> is the provided value. <br/>
    ///     <see langword="false"/> if any other named color is closer to <paramref name="color"/> than
    ///     <paramref name="namedColor"/>.
    /// </returns>
    /// <remarks>
    ///     If <paramref name="color"/> is equidistant from two named colors, the result of this method is not guaranteed
    ///     to be determinate.
    /// </remarks>
    [Pure]
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool IsColorClosestToNamedColor16 (in Color color, in ColorName16 namedColor) { return color.IsClosestToNamedColor16 (in namedColor); }

    /// <summary>Gets the "closest" named color to this <see cref="Color"/> value.</summary>
    /// <param name="inputColor"></param>
    /// <remarks>
    ///     Distance is defined here as the Euclidean distance between each color interpreted as a <see cref="Vector3"/>.
    /// </remarks>
    /// <returns></returns>
    [SkipLocalsInit]
    internal static ColorName16 GetClosestNamedColor16 (Color inputColor)
    {
        return ColorExtensions.ColorToName16Map.MinBy (pair => CalculateColorDistance (inputColor, pair.Key)).Value;
    }

    [SkipLocalsInit]
    private static float CalculateColorDistance (in Vector4 color1, in Vector4 color2) { return Vector4.Distance (color1, color2); }

    /// <summary>
    /// Gets a color that is the same hue as the current color, but with a different lightness.
    /// </summary>
    /// <returns></returns>
    public Color GetHighlightColor ()
    {
        // TODO: This is a temporary implementation; just enough to show how it could work. 
        var hsl = ColorHelper.ColorConverter.RgbToHsl (new RGB (R, G, B));

        var amount = .7;
        if (hsl.L <= 5)
        {
            return DarkGray;
        }
        hsl.L = (byte)(hsl.L * amount);

        var rgb = ColorHelper.ColorConverter.HslToRgb (hsl);
        return new (rgb.R, rgb.G, rgb.B);

    }

    /// <summary>
    /// Gets a color that is the same hue as the current color, but with a different lightness.
    /// </summary>
    /// <returns></returns>
    public Color GetDarkerColor ()
    {
        // TODO: This is a temporary implementation; just enough to show how it could work. 
        var hsl = ColorHelper.ColorConverter.RgbToHsl (new RGB (R, G, B));

        var amount = .3;
        if (hsl.L <= 5)
        {
            return DarkGray;
        }
        hsl.L = (byte)(hsl.L * amount);

        var rgb = ColorHelper.ColorConverter.HslToRgb (hsl);
        return new (rgb.R, rgb.G, rgb.B);

    }

    #region Legacy Color Names

    /// <summary>The black color.</summary>
    public const ColorName16 Black = ColorName16.Black;

    /// <summary>The blue color.</summary>
    public const ColorName16 Blue = ColorName16.Blue;

    /// <summary>The green color.</summary>
    public const ColorName16 Green = ColorName16.Green;

    /// <summary>The cyan color.</summary>
    public const ColorName16 Cyan = ColorName16.Cyan;

    /// <summary>The red color.</summary>
    public const ColorName16 Red = ColorName16.Red;

    /// <summary>The magenta color.</summary>
    public const ColorName16 Magenta = ColorName16.Magenta;

    /// <summary>The yellow color.</summary>
    public const ColorName16 Yellow = ColorName16.Yellow;

    /// <summary>The gray color.</summary>
    public const ColorName16 Gray = ColorName16.Gray;

    /// <summary>The dark gray color.</summary>
    public const ColorName16 DarkGray = ColorName16.DarkGray;

    /// <summary>The bright bBlue color.</summary>
    public const ColorName16 BrightBlue = ColorName16.BrightBlue;

    /// <summary>The bright green color.</summary>
    public const ColorName16 BrightGreen = ColorName16.BrightGreen;

    /// <summary>The bright cyan color.</summary>
    public const ColorName16 BrightCyan = ColorName16.BrightCyan;

    /// <summary>The bright red color.</summary>
    public const ColorName16 BrightRed = ColorName16.BrightRed;

    /// <summary>The bright magenta color.</summary>
    public const ColorName16 BrightMagenta = ColorName16.BrightMagenta;

    /// <summary>The bright yellow color.</summary>
    public const ColorName16 BrightYellow = ColorName16.BrightYellow;

    /// <summary>The White color.</summary>
    public const ColorName16 White = ColorName16.White;

    #endregion
}