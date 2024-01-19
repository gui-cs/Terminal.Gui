#nullable enable
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
/// Represents a 24-bit color. Provides automatic mapping between the legacy 4-bit (16 color) system and 24-bit colors (see
/// <see cref="ColorName"/>). Used with <see cref="Attribute"/>.
/// </summary>
[JsonConverter ( typeof ( ColorJsonConverter ) )]
[StructLayout(LayoutKind.Explicit)]
public readonly record struct Color : ISpanParsable<Color>, IUtf8SpanParsable<Color>, ISpanFormattable, IUtf8SpanFormattable,IMinMaxValue<Color> {
	// TODO: Make this map configurable via ConfigurationManager
	// TODO: This does not need to be a Dictionary, but can be an 16 element array.
	/// <summary>
	/// Maps legacy 16-color values to the corresponding 24-bit RGB value.
	/// </summary>
	internal static ImmutableDictionary<Color, ColorName> _colorToNameMap = new Dictionary<Color, ColorName> {
		// using "Windows 10 Console/PowerShell 6" here: https://i.stack.imgur.com/9UVnC.png
		// See also: https://en.wikipedia.org/wiki/ANSI_escape_code
		{ new Color (12, 12, 12), ColorName.Black },
		{ new Color (0, 55, 218), ColorName.Blue },
		{ new Color (19, 161, 14), ColorName.Green },
		{ new Color (58, 150, 221), ColorName.Cyan },
		{ new Color (197, 15, 31), ColorName.Red },
		{ new Color (136, 23, 152), ColorName.Magenta },
		{ new Color (128, 64, 32), ColorName.Yellow },
		{ new Color (204, 204, 204), ColorName.Gray },
		{ new Color (118, 118, 118), ColorName.DarkGray },
		{ new Color (59, 120, 255), ColorName.BrightBlue },
		{ new Color (22, 198, 12), ColorName.BrightGreen },
		{ new Color (97, 214, 214), ColorName.BrightCyan },
		{ new Color (231, 72, 86), ColorName.BrightRed },
		{ new Color (180, 0, 158), ColorName.BrightMagenta },
		{ new Color (249, 241, 165), ColorName.BrightYellow },
		{ new Color (242, 242, 242), ColorName.White }
	}.ToImmutableDictionary ();


	/// <summary>
	/// Defines the 16 legacy color names and values that can be used to set them
	/// </summary>
	internal static ImmutableDictionary<ColorName, AnsiColorCode> _colorNameToAnsiColorMap = new Dictionary<ColorName, AnsiColorCode> {
		{ ColorName.Black, AnsiColorCode.BLACK },
		{ ColorName.Blue, AnsiColorCode.BLUE },
		{ ColorName.Green, AnsiColorCode.GREEN },
		{ ColorName.Cyan, AnsiColorCode.CYAN },
		{ ColorName.Red, AnsiColorCode.RED },
		{ ColorName.Magenta, AnsiColorCode.MAGENTA },
		{ ColorName.Yellow, AnsiColorCode.YELLOW },
		{ ColorName.Gray, AnsiColorCode.WHITE },
		{ ColorName.DarkGray, AnsiColorCode.BRIGHT_BLACK },
		{ ColorName.BrightBlue, AnsiColorCode.BRIGHT_BLUE },
		{ ColorName.BrightGreen, AnsiColorCode.BRIGHT_GREEN },
		{ ColorName.BrightCyan, AnsiColorCode.BRIGHT_CYAN },
		{ ColorName.BrightRed, AnsiColorCode.BRIGHT_RED },
		{ ColorName.BrightMagenta, AnsiColorCode.BRIGHT_MAGENTA },
		{ ColorName.BrightYellow, AnsiColorCode.BRIGHT_YELLOW },
		{ ColorName.White, AnsiColorCode.BRIGHT_WHITE }
	}.ToImmutableDictionary ();

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> <see langword="struct"/> using the supplied component values.
	/// </summary>
	/// <param name="red">The red 8-bits.</param>
	/// <param name="green">The green 8-bits.</param>
	/// <param name="blue">The blue 8-bits.</param>
	/// <param name="alpha">Optional; defaults to 0xFF. The Alpha channel is not supported by Terminal.Gui.</param>
	/// <remarks>Alpha channel is not currently supported by Terminal.Gui.</remarks>
	/// <exception cref="OverflowException">If the value of any parameter is greater than <see cref="byte.MaxValue"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">If the value of any parameter is negative.</exception>
	public Color ( int red = 0, int green = 0, int blue = 0, int alpha = byte.MaxValue )
	{
		ArgumentOutOfRangeException.ThrowIfNegative ( red, nameof ( red ) );
		ArgumentOutOfRangeException.ThrowIfNegative ( green, nameof ( green ) );
		ArgumentOutOfRangeException.ThrowIfNegative ( blue, nameof ( blue ) );
		ArgumentOutOfRangeException.ThrowIfNegative ( alpha, nameof ( alpha ) );

		A = Convert.ToByte ( alpha );
		R = Convert.ToByte ( red );
		G = Convert.ToByte ( green );
		B = Convert.ToByte ( blue );
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> class with an encoded signed 32-bit color value in ARGB32 format.
	/// </summary>
	/// <param name="rgba">The encoded 32-bit color value (see <see cref="Rgba"/>).</param>
	/// <remarks>The alpha channel is not currently supported, so the value of the alpha channel bits will not affect rendering.</remarks>
	public Color ( int rgba )
	{
		Rgba = rgba;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> class with an encoded unsigned 32-bit color value in ARGB32 format.
	/// </summary>
	/// <param name="argb">The encoded unsigned 32-bit color value (see <see cref="Argb"/>).</param>
	/// <remarks>The alpha channel is not currently supported, so the value of the alpha channel bits will not affect rendering.</remarks>
	public Color ( uint argb )
	{
		Argb = argb;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> color from a legacy 16-color named value.
	/// </summary>
	/// <param name="colorName">The 16-color value.</param>
	public Color ( ColorName colorName )
	{
		this = FromColorName ( colorName );
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> color from string. See <see cref="TryParse(string, out Color?)"/>
	/// for details.
	/// </summary>
	/// <param name="colorString"></param>
	/// <exception cref="ArgumentNullException">If <paramref name="colorString"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">If <paramref name="colorString"/> is an empty string or consists of only whitespace characters.</exception>
	/// <exception cref="ColorParseException">If thrown by <see cref="Parse(string?,System.IFormatProvider?)"/></exception>
	public Color ( string colorString )
	{
		ArgumentException.ThrowIfNullOrWhiteSpace ( colorString, nameof ( colorString ) );
		this = Parse ( colorString, CultureInfo.InvariantCulture );
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Color"/> with all channels set to 0.
	/// </summary>
	public Color ( )
	{
		Argb = 0u;
	}

    /// <summary>
    /// The value of this <see cref="Color"/> as a <see langword="uint"/> in ARGB32 format.
    /// </summary>
	/// <remarks>The alpha channel is not currently supported, so the value of the alpha channel bits will not affect rendering.</remarks>
	[JsonIgnore]
	[field: FieldOffset ( 0 )]
	public readonly uint Argb;


	/// <summary>
	/// The value of the red color component.
	/// </summary>
	[JsonIgnore]
	[field: FieldOffset ( 2 )]
	public readonly byte R;

	/// <summary>
	/// The value of the green color component.
	/// </summary>
	[JsonIgnore]
	[field: FieldOffset ( 1 )]
	public readonly byte G;

	/// <summary>
	/// The value of the blue color component.
	/// </summary>
	[JsonIgnore]
	[field: FieldOffset ( 0 )]
	public readonly byte B;

	/// <summary>
	/// The value of the alpha channel component
	/// </summary>
	/// <remarks>The alpha channel is not currently supported, so the value of the alpha channel bits will not affect rendering.</remarks>
	[JsonIgnore]
	[field: FieldOffset ( 3 )]
	public readonly byte A;

	/// <summary>
	/// The value of this <see cref="Color"/> encoded as a signed 32-bit integer in ARGB32 format.
	/// </summary>
	[JsonIgnore]
	[field: FieldOffset ( 0 )]
	public readonly int Rgba;

	/// <summary>
	/// Gets or sets the 3-byte/6-character hexadecimal value for each of the legacy 16-color values.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
	public static Dictionary<ColorName, string> Colors {
		get =>
			// Transform _colorToNameMap into a Dictionary<ColorNames,string>
			_colorToNameMap.ToDictionary (kvp => kvp.Value, kvp => $"#{kvp.Key.R:X2}{kvp.Key.G:X2}{kvp.Key.B:X2}");
		set {
			// Transform Dictionary<ColorNames,string> into _colorToNameMap
			var newMap = value.ToDictionary (kvp => new Color (kvp.Value), kvp => {
				if (Enum.TryParse<ColorName> (kvp.Key.ToString (), true, out var colorName)) {
					return colorName;
				}
				throw new ArgumentException ($"Invalid color name: {kvp.Key}");
			});
			_colorToNameMap = newMap.ToImmutableDictionary ();
		}
	}

	/// <summary>
	/// Gets the <see cref="Color"/> using a legacy 16-color <see cref="Gui.ColorName"/> value.
	/// <see langword="get"/> will return the closest 16 color match to the true color when no exact value is found.
	/// </summary>
	/// <remarks>
	/// Get returns the <see cref="ColorName"/> of the closest 24-bit color value. Set sets the RGB value using a hard-coded
	/// map.
	/// </remarks>
	[JsonIgnore]
	public ColorName ColorName => FindClosestColor (this);

	/// <summary>
	/// Gets the <see cref="Color"/> using a legacy 16-color <see cref="Gui.ColorName"/> value.
	/// <see langword="get"/> will return the closest 16 color match to the true color when no exact value is found.
	/// </summary>
	/// <remarks>
	/// Get returns the <see cref="ColorName"/> of the closest 24-bit color value. Set sets the RGB value using a hard-coded
	/// map.
	/// </remarks>
	[JsonIgnore]
	public AnsiColorCode AnsiColorCode => _colorNameToAnsiColorMap [ColorName];

	/// <summary>
	/// Converts a legacy <see cref="Gui.ColorName"/> to a 24-bit <see cref="Color"/>.
	/// </summary>
	/// <param name="colorName">The <see cref="Color"/> to convert.</param>
	/// <returns></returns>
	static Color FromColorName (ColorName colorName) => _colorToNameMap.FirstOrDefault (x => x.Value == colorName).Key;

	// Iterates through the entries in the _colorNames dictionary, calculates the
	// Euclidean distance between the input color and each dictionary color in RGB space,
	// and keeps track of the closest entry found so far. The function returns a KeyValuePair
	// representing the closest color entry and its associated color name.
	[SkipLocalsInit]
	internal static ColorName FindClosestColor ( Color inputColor ) => _colorToNameMap.MinBy ( pair => CalculateColorDistance ( inputColor, pair.Key ) ).Value;

	[SkipLocalsInit]
	static float CalculateColorDistance ( in Vector3 color1, in Vector3 color2 ) => Vector3.Distance ( color1, color2 );

	/// <summary>
	/// Converts the provided string to a new <see cref="Color"/> instance.
	/// </summary>
	/// <param name="text">
	/// The text to analyze. Formats supported are
	/// "#RGB", "#RRGGBB", "#RGBA", "#RRGGBBAA", "rgb(r,g,b)", "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)",
	/// and any of the <see cref="Gui.ColorName"/> string values.
	/// </param>
	/// <param name="color">The parsed value.</param>
	/// <returns>A boolean value indicating whether parsing was successful.</returns>
	/// <remarks>
	/// While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.
	/// </remarks>
	public static bool TryParse (string? text, [NotNullWhen (true)] out Color? color)
	{
		if ( TryParse ( text.AsSpan ( ), null, out Color c ) ) {
			color = c;
			return true;
		}
		color = null;
		return false;
	}

	/// <summary>
	/// Converts the provided <see langword="string"/> to a new <see cref="Color"/> value.
	/// </summary>
	/// <param name="text">
	/// The text to analyze. Formats supported are
	/// "#RGB", "#RRGGBB", "#RGBA", "#RRGGBBAA", "rgb(r,g,b)", "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)",
	/// and any of the <see cref="Gui.ColorName"/> string values.
	/// </param>
	/// <param name="formatProvider">
	///   Implemented for compatibility with <see cref="IParsable{TSelf}" />. Will be ignored.
	/// </param>
	/// <returns>A <see cref="Color"/> value equivalent to <paramref name="text"/>, if parsing was successful.</returns>
	/// <remarks>
	/// While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.
	/// </remarks>
	/// <exception cref="ArgumentNullException">If <paramref name="text"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">If <paramref name="text"/> is an empty string or consists of only whitespace characters.</exception>
	/// <exception cref="ColorParseException">If thrown by <see cref="Parse(System.ReadOnlySpan{char},System.IFormatProvider?)"/>.</exception>
	[Pure]
    [SkipLocalsInit]
	public static Color Parse ( string? text, IFormatProvider? formatProvider = null )
	{
		ArgumentException.ThrowIfNullOrWhiteSpace ( text, nameof ( text ) );
		if ( text is { Length: < 3 } && formatProvider is null ) {
			throw new ColorParseException ( colorString: text, reason: "Provided text is too short to be any known color format.", badValue: text );
		}
		return Parse ( text.AsSpan ( ), formatProvider ?? CultureInfo.InvariantCulture );
	}

	/// <summary>
	/// Converts the provided <see langword="string"/> to a new <see cref="Color"/> value.
	/// </summary>
	/// <param name="text">
	/// The text to analyze. Formats supported are
	/// "#RGB", "#RRGGBB", "#RGBA", "#RRGGBBAA", "rgb(r,g,b)", "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)",
	/// and any of the <see cref="ColorName"/> string values.
	/// </param>
	/// <param name="formatProvider">
	///   Optional <see cref="IFormatProvider"/> to provide formatting services for the input text.<br/>
	/// Defaults to <see cref="CultureInfo.InvariantCulture"/> if <see langword="null"/>.
	/// </param>
	/// <param name="result">The parsed value, if successful, or <see langword="default"/>(<see cref="Color"/>), if unsuccessful.</param>
	/// <returns>A <see langword="bool"/> value indicating whether parsing was successful.</returns>
	/// <remarks>
	/// While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.
	/// </remarks>
	[Pure]
    [SkipLocalsInit]
	public static bool TryParse ( string? text, IFormatProvider? formatProvider, out Color result )
	{
		return TryParse ( text.AsSpan ( ), formatProvider ?? CultureInfo.InvariantCulture, out result );
	}

	/// <summary>
	///   Converts the provided <see cref="ReadOnlySpan{T}" /> of <see langword="char" /> to a new <see cref="Color" /> value.
	/// </summary>
	/// <param name="text">
	///   The text to analyze. Formats supported are "#RGB", "#RRGGBB", "#RGBA", "#RRGGBBAA", "rgb(r,g,b)", "rgb(r,g,b,a)", "rgba(r,g,b)",
	///   "rgba(r,g,b,a)", and any of the <see cref="Gui.ColorName" /> string values.
	/// </param>
	/// <param name="formatProvider">
	///   Optional <see cref="IFormatProvider" /> to provide parsing services for the input text.<br /> Defaults to
	///   <see cref="CultureInfo.InvariantCulture" /> if <see langword="null" />.<br /> If not null, must implement
	///   <see cref="ICustomColorFormatter" /> or will be ignored and <see cref="CultureInfo.InvariantCulture" /> will be used.
	/// </param>
	/// <returns>
	///   A <see cref="Color" /> value equivalent to <paramref name="text" />, if parsing was successful.
	/// </returns>
	/// <remarks>
	///   While <see cref="Color" /> supports the alpha channel <see cref="A" />, Terminal.Gui does not.
	/// </remarks>
	/// <exception cref="ArgumentException">
	///   with an inner <see cref="FormatException" /> if <paramref name="text" /> was unable to be successfully parsed as a <see cref="Color" />,
	///   for any reason.
	/// </exception>
	[Pure]
    [SkipLocalsInit]
	public static Color Parse ( ReadOnlySpan<char> text, IFormatProvider? formatProvider = null )
	{
		return text switch {
			// Null string or empty span provided
			{ IsEmpty: true } when formatProvider is null => throw new ColorParseException ( in text, reason: "The text provided was null or empty.", badValue: in text ),
			// A valid ICustomColorFormatter was specified and the text wasn't null or empty
			{ IsEmpty: false } when formatProvider is ICustomColorFormatter f => f.Parse ( text ),
			// Input string is only whitespace
			{ Length: > 0 } when text.IsWhiteSpace ( ) => throw new ColorParseException ( in text, reason: "The text provided consisted of only whitespace characters.", badValue: in text ),
			// Any string too short to possibly be any supported format.
			{ Length: > 0 and < 4 } => throw new ColorParseException ( in text, reason: "Text was too short to be any possible supported format.", badValue: in text ),
			// The various hexadecimal cases
			['#', ..] hexString => hexString switch {
				// #RGB
				['#', var rChar, var gChar, var bChar] chars when chars [ 1.. ].IsAllAsciiHexDigits ( ) =>
					new Color ( byte.Parse ( [rChar, rChar], NumberStyles.HexNumber ), byte.Parse ( [gChar, gChar], NumberStyles.HexNumber ), byte.Parse ( [bChar, bChar], NumberStyles.HexNumber ) ),
				// #ARGB
				['#', var aChar, var rChar, var gChar, var bChar] chars when chars [ 1.. ].IsAllAsciiHexDigits ( ) =>
					new Color ( byte.Parse ( [rChar, rChar], NumberStyles.HexNumber ), byte.Parse ( [gChar, gChar], NumberStyles.HexNumber ), byte.Parse ( [bChar, bChar], NumberStyles.HexNumber ), byte.Parse ( [aChar, aChar], NumberStyles.HexNumber ) ),
				// #RRGGBB
				['#', var r1Char, var r2Char, var g1Char, var g2Char, var b1Char, var b2Char] chars when chars [ 1.. ].IsAllAsciiHexDigits ( ) =>
					new Color ( byte.Parse ( [r1Char, r2Char], NumberStyles.HexNumber ), byte.Parse ( [g1Char, g2Char], NumberStyles.HexNumber ), byte.Parse ( [b1Char, b2Char], NumberStyles.HexNumber ), byte.MaxValue ),
				// #AARRGGBB
				['#', var a1Char, var a2Char, var r1Char, var r2Char, var g1Char, var g2Char, var b1Char, var b2Char] chars when chars [ 1.. ].IsAllAsciiHexDigits ( ) =>
					new Color ( byte.Parse ( [r1Char, r2Char], NumberStyles.HexNumber ), byte.Parse ( [g1Char, g2Char], NumberStyles.HexNumber ), byte.Parse ( [b1Char, b2Char], NumberStyles.HexNumber ), byte.Parse ( [a1Char, a2Char], NumberStyles.HexNumber ) ),
				_ => throw new ColorParseException ( in hexString, reason: $"Color hex string {hexString} was not in a supported format", in hexString )
			},
			// rgb(r,g,b) or rgb(r,g,b,a)
			['r', 'g', 'b', '(', .., ')'] => ParseRgbaFormat ( in text, 4 ),
			// rgba(r,g,b,a) or rgba(r,g,b)
			['r', 'g', 'b', 'a', '(', .., ')'] => ParseRgbaFormat ( in text, 5 ),
			// Attempt to parse as a named color from the ColorName enum
			{ } when char.IsLetter ( text [ 0 ] ) && Enum.TryParse ( text, true, out ColorName colorName ) => new Color ( colorName ),
			// Any other input
			_ => throw new ColorParseException ( in text, reason: "Text did not match any expected format.", badValue: in text, [] )
		};

		[Pure]
		[SkipLocalsInit]
		static Color ParseRgbaFormat ( in ReadOnlySpan<char> originalString, in int startIndex )
		{
			ReadOnlySpan<char> valuesSubstring = originalString [ startIndex..^1 ];
			Span<Range> valueRanges = stackalloc Range [4];
			int rangeCount = valuesSubstring.Split ( valueRanges, ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );

			switch ( rangeCount ) {
			case 3:
			{
				// rgba(r,g,b)
				ParseRgbValues ( in valuesSubstring, in valueRanges, in originalString, out ReadOnlySpan<char> rSpan, out ReadOnlySpan<char> gSpan, out ReadOnlySpan<char> bSpan );
				return new Color ( int.Parse ( rSpan ), int.Parse ( gSpan ), int.Parse ( bSpan ) );
			}
			case 4:
			{
				// rgba(r,g,b,a)
				ParseRgbValues ( in valuesSubstring, in valueRanges, in originalString, out ReadOnlySpan<char> rSpan, out ReadOnlySpan<char> gSpan, out ReadOnlySpan<char> bSpan );
				ReadOnlySpan<char> aSpan = valuesSubstring [ valueRanges [ 3 ] ];
				if ( !aSpan.IsAllAsciiDigits ( ) ) {
					throw new ColorParseException ( in originalString, reason: "Value was not composed entirely of decimal digits.", badValue: in aSpan, badValueName: nameof ( A ) );
				}
				return new Color ( int.Parse ( rSpan ), int.Parse ( gSpan ), int.Parse ( bSpan ), int.Parse ( aSpan ) );
			}
			default:
				throw new ColorParseException ( in originalString, reason: $"Wrong number of values. Expected 3 or 4 decimal integers. Got {rangeCount}.", in originalString );
			}

			[Pure]
			[SkipLocalsInit]
			static void ParseRgbValues ( in ReadOnlySpan<char> valuesString, in Span<Range> valueComponentRanges, in ReadOnlySpan<char> originalString, out ReadOnlySpan<char> rSpan, out ReadOnlySpan<char> gSpan, out ReadOnlySpan<char> bSpan )
			{

				rSpan = valuesString [ valueComponentRanges [ 0 ] ];
				if ( !rSpan.IsAllAsciiDigits ( ) ) {
					throw new ColorParseException ( in originalString, reason: "Value was not composed entirely of decimal digits.", badValue: in rSpan, badValueName: nameof ( R ) );
				}
				gSpan = valuesString [ valueComponentRanges [ 1 ] ];
				if ( !gSpan.IsAllAsciiDigits ( ) ) {
					throw new ColorParseException ( in originalString, reason: "Value was not composed entirely of decimal digits.", badValue: in gSpan, badValueName: nameof ( G ) );
				}
				bSpan = valuesString [ valueComponentRanges [ 2 ] ];
				if ( !bSpan.IsAllAsciiDigits ( ) ) {
					throw new ColorParseException ( in originalString, reason: "Value was not composed entirely of decimal digits.", badValue: in bSpan, badValueName: nameof ( B ) );
				}
			}
		}
	}

	/// <summary>
	/// Converts the provided <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to a new <see cref="Color"/> value.
	/// </summary>
	/// <param name="text">
	/// The text to analyze. Formats supported are
	/// "#RGB", "#RRGGBB", "#ARGB", "#AARRGGBB", "rgb(r,g,b)", "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)",
	/// and any of the <see cref="ColorName"/> string values.
	/// </param>
	/// <param name="formatProvider">
	///   Implemented for compatibility with <see cref="IParsable{TSelf}" />. Will be ignored. Just pass <see langword="null"/>.
	/// </param>
	/// <param name="color">The parsed value, if successful, or <see langword="default"/>(<see cref="Color"/>), if unsuccessful.</param>
	/// <returns>A <see langword="bool"/> value indicating whether parsing was successful.</returns>
	/// <remarks>
	/// While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.
	/// </remarks>
	[Pure]
	[SkipLocalsInit]
	public static bool TryParse ( ReadOnlySpan<char> text, IFormatProvider? formatProvider, out Color color )
	{
		try {
			Color c = Parse ( text, formatProvider );
			color = c;
			return true;
		}
		catch ( ColorParseException ) {
			color = default;
			return false;
		}
	}

	/// <summary>
	/// Converts the color to a string representation.
	/// </summary>
	/// <remarks>
	///         <para>
	///         If the color is a named color, the name is returned. Otherwise, the color is returned as a hex string.
	///         </para>
	///         <para>
	///         <see cref="A"/> (Alpha channel) is ignored and the returned string will not include it.
	///         </para>
	/// </remarks>
	/// <returns></returns>
	[Pure]
	[SkipLocalsInit]
	public override string ToString ( )
	{
		// If Values has an exact match with a named color (in _colorNames), use that.
		if ( _colorToNameMap.TryGetValue ( this, out var colorName ) ) {
			return Enum.GetName ( typeof ( ColorName ), colorName );
		}
		// Otherwise return as an RGB hex value.
		return $"#{R:X2}{G:X2}{B:X2}";
	}
	/// <inheritdoc />
	public bool TryFormat ( Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider )
	{
		//TODO: Finish this
		throw new NotImplementedException ( );
	}


	/// <inheritdoc cref="object.ToString" />
	/// <summary>
	///   Returns a <see langword="string" /> representation of the current <see cref="Color" /> value, according to the provided
	///   <paramref name="formatString" /> and optional <paramref name="formatProvider" />.
	/// </summary>
	/// <param name="formatString">
	///   A format string that will be passed to <see cref="string.Format(System.IFormatProvider?,string,object?[])" />. See remarks for parameters
	///   passed to
	/// </param>
	/// <param name="formatProvider">
	///   An optional <see cref="IFormatProvider" /> to use when formatting the <see cref="Color" /> using custom format strings not specified for
	///   this method. Provides this instance as <see cref="Argb" />.<br /> If this parameter is not null, the specified
	///   <see cref="IFormatProvider" /> will be used instead of the custom formatting provided by the <see cref="Color" /> type.See remarks.
	/// </param>
	/// <remarks>
	///   Pre-defined format strings for this method, if a custom <paramref name="formatProvider" /> is not supplied are: <list type="bullet">
	///     <listheader>
	///       <term>Value</term> <description>Result</description>
	///     </listheader> <item>
	///       <term>g or null or empty string</term> <description>
	///         General/default format - Returns a named <see cref="Color" /> if there is a match, or a 24-bit/3-byte/6-hex digit string in
	///         "#RRGGBB" format.
	///       </description>
	///     </item> <item>
	///       <term>G</term> <description>
	///         Extended general format - Returns a named <see cref="Color" /> if there is a match, or a 32-bit/4-byte/8-hex digit string in
	///         "#AARRGGBB" format.
	///       </description>
	///     </item> <item>
	///       <term>d</term> <description>
	///         Decimal format - Returns a 3-component decimal representation of the <see cref="Color" /> in "rgb(R,G,B)" format.
	///       </description>
	///     </item> <item>
	///       <term>D</term> <description>
	///         Extended decimal format - Returns a 4-component decimal representation of the <see cref="Color" /> in "rgba(R,G,B,A)" format.
	///       </description>
	///     </item>
	///   </list>
	///   <para>
	///     If <paramref name="formatProvider" /> is provided and is a non-null <see cref="ICustomColorFormatter"/>, the following behaviors are available, for the
	///     specified values of <paramref name="formatString" />:
	/// <list type="bullet">
	///       <listheader>
	///         <term>Value</term> <description>Result</description>
	///       </listheader>
	/// <item>
	///         <term>null or empty string</term> <description>
	///           Calls <see cref="ICustomColorFormatter.Format(string?,byte,byte,byte,byte)}" /> on the provided
	///           <paramref name="formatProvider" /> with the null string, and <see cref="R" />, <see cref="G" />, <see cref="B" />, and <see cref="A" /> as typed
	///           arguments of type <see cref="Byte" />.
	///         </description>
	///       </item>
	/// <item>
	///         <term>All other values</term> <description>
	///           Calls <see cref="string.Format{TArg0}" /> with the provided <paramref name="formatProvider" /> and
	///           <paramref name="formatString" /> (parsed as a <see cref="CompositeFormat" />), with the value of <see cref="Argb" /> as the sole
	///           <see langword="uint" />-typed argument.
	///         </description>
	///       </item>
	///     </list>
	///   </para>
	/// </remarks>
	public string ToString ([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? formatString, IFormatProvider? formatProvider = null )
	{
		return ( formatString, formatProvider ) switch {
			// Null or empty string and null formatProvider - Revert to 'g' case behavior
			(null or { Length: 0 }, null) => ToString ( ),
			// Null or empty string and formatProvider is an ICustomColorFormatter - Output according to the given ICustomColorFormatted, with R, G, B, and A as typed arguments
			(null or { Length: 0 }, ICustomColorFormatter ccf) => ccf.Format ( null, R, G, B, A ),
			// Null or empty string and formatProvider is otherwise non-null but not the invariant culture - Output according to string.Format with the given IFormatProvider and R, G, B, and A as boxed arguments, with string.Empty as the format string
			(null or { Length: 0 }, { }) when !Equals ( formatProvider, CultureInfo.InvariantCulture ) => string.Format ( formatProvider, formatString ?? string.Empty, R, G, B, A ),
			// Null or empty string and formatProvider is the invariant culture - Output according to string.Format with the given IFormatProvider and R, G, B, and A as boxed arguments, with string.Empty as the format string
			(null or { Length: 0 }, { }) when Equals ( formatProvider, CultureInfo.InvariantCulture ) => $"#{R:X2}{G:X2}{B:X2}",
			// Non-null string and non-null formatProvider - let formatProvider handle it and give it R, G, B, and A
			({ }, { }) => string.Format ( formatProvider, CompositeFormat.Parse ( formatString ), R, G, B, A ),
			// g format string and null formatProvider - Output as 24-bit hex according to invariant culture rules from R, G, and B
			(['g'], null) => ToString ( ),
			// G format string and null formatProvider - Output as 32-bit hex according to invariant culture rules from Argb
			(['G'], null) => $"#{A:X2}{R:X2}{G:X2}{B:X2}",
			// d format string and null formatProvider - Output as 24-bit decimal rgb(r,g,b) according to invariant culture rules from R, G, and B
			(['d'], null) => $"rgb({R:D},{G:D},{B:D})",
			// D format string and null formatProvider - Output as 32-bit decimal rgba(r,g,b,a) according to invariant culture rules from R, G, B, and A. Non-standard: a is a decimal byte value.
			(['D'], null) => $"rgba({R:D},{G:D},{B:D},{A:D})",
			// All other cases (formatString is not null here) - Delegate to formatProvider, first, and otherwise to invariant culture, and try to format the provided string from Argb
			({ }, _) => string.Format ( formatProvider ?? CultureInfo.InvariantCulture, CompositeFormat.Parse ( formatString ), R, G, B, A ),
		};
	}

	/// <inheritdoc />
	/// <remarks>
	///   <para>
	///     This method should be used only when absolutely necessary, because it <b>always</b> has more overhead than <see cref="ToString(string?,System.IFormatProvider?)" />, as this method results
	///     in an intermediate allocation of one or more instances of <see langword="string" /> and a copy of that string to <paramref name="destination"/> if formatting was successful.<br /> When possible, use
	///     <see cref="ToString(string?,System.IFormatProvider?)" />, which attempts to avoid intermediate allocations.
	///   </para>
	///   <para>
	///     This method only returns <see langword="true" /> and with its output written to <paramref name="destination" /> if the formatted
	///     string, <i>in its entirety</i>, will fit in <paramref name="destination" />. If the resulting formatted string is too large to fit in
	///     <paramref name="destination" />, the result will be false and <paramref name="destination" /> will be unaltered.
	///   </para>
	/// <para>
	/// The resulting formatted string may be <b>shorter</b> than <paramref name="destination"/>. When this method returns <see langword="true"/>, use <paramref name="charsWritten"/> when handling the value of <paramref name="destination"/>.</para>
	/// </remarks>
	[Pure]
	public bool TryFormat ( Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider )
	{
		try {
			string formattedString = ToString ( format.ToString (  ), provider );
			if ( formattedString.Length <= destination.Length ) {
				formattedString.CopyTo ( destination );
				charsWritten = formattedString.Length;
				return true;
			}
		}
		catch {
			charsWritten = 0;
			return false;
		}
		charsWritten = 0;
		return false;
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

	// TODO: Verify implict/explicit are correct for below
	#region Operators

	/// <summary>
	/// Cast from int.
	/// </summary>
	/// <param name="rgba"></param>
	[Pure]
	public static implicit operator Color (int rgba) => new (rgba);

	/// <summary>
	/// Cast to int. 
	/// </summary>
	/// <param name="color"></param>
	[Pure]
	public static implicit operator int (Color color) => color.Rgba;

	/// <summary>
	/// Cast to int.
	/// </summary>
	/// <param name="color"></param>
	[Pure]
	public static implicit operator uint (Color color) => color.Argb;

	/// <summary>
	/// Implicit conversion from <see langword="uint"/> to <see cref="Color"/>.
	/// </summary>
	/// <param name="u"></param>
	[Pure]
	public static implicit operator Color ( uint u ) => new ( u );

	/// <summary>
	/// Cast from <see cref="Gui.ColorName"/>.
	/// </summary>
	/// <param name="colorName"></param>
	public static explicit operator Color (ColorName colorName) => new (colorName);

	/// <summary>
	/// Cast to <see cref="Gui.ColorName"/>. May fail if the color is not a named color.
	/// </summary>
	/// <param name="color"></param>
	public static explicit operator ColorName (Color color) => color.ColorName;

	/// <summary>
	/// Implicit conversion to <see cref="Vector3"/>, where <see cref="Vector3.X"/> = <see cref="R"/>, <see cref="Vector3.Y"/> = <see cref="G"/>, and <see cref="Vector3.Z"/> = <see cref="B"/>.
	/// </summary>
	/// <param name="color"></param>
	public static implicit operator Vector3 ( Color color ) => new ( color.R, color.G, color.B );

	/// <summary>
	/// Implicit conversion from <see cref="Vector3"/>, where <see cref="Vector3.X"/> = <see cref="R"/>, <see cref="Vector3.Y"/> = <see cref="G"/>, and <see cref="Vector3.Z"/> = <see cref="B"/>.
	/// </summary>
	/// <param name="v"></param>
	public static implicit operator Color ( Vector3 v ) => new ( (byte)v.X, (byte)v.Y, (byte)v.Z );

	/// <inheritdoc />
	public override int GetHashCode ( ) => Rgba.GetHashCode ( );
	#endregion

	/// <inheritdoc />
	/// <returns>A <see cref="Color"/> <see langword="struct"/> with all values set to <see cref="byte.MaxValue"/>, meaning white.</returns>
	public static Color MaxValue => new ( uint.MaxValue );

	/// <inheritdoc />
	/// <returns>A <see cref="Color"/> <see langword="struct"/> with all values set to zero.</returns>
	/// <remarks>Though this returns a <see cref="Color"/> with <see cref="A"/>, <see cref="R"/>, <see cref="G"/>, and <see cref="B"/> all set to zero, Terminal.Gui will treat it as black, because the alpha channel is not supported.</remarks>
	public static Color MinValue => new ( uint.MinValue );

	/// <inheritdoc />
	public static Color Parse ( ReadOnlySpan<byte> utf8Text, IFormatProvider? provider )
	{
		return Parse ( Encoding.UTF8.GetString ( utf8Text ), provider );
	}
	/// <inheritdoc />
	public static bool TryParse ( ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Color result )
	{
		return TryParse ( Encoding.UTF8.GetString ( utf8Text ), provider, out result );
	}

	/// <summary>
	/// Determines if the closest named <see cref="Color"/> to <see langword="this"/> is the provided <paramref name="namedColor"/>.
	/// </summary>
	/// <param name="namedColor">The <see cref="ColorName"/> to check if this <see cref="Color"/> is closer to than any other configured named color.</param>
	/// <returns><see langword="true"/> if the closest named color is the provided value.<br/>
	/// <see langword="false"/> if any other named color is closer to this <see cref="Color"/> than <paramref name="namedColor"/>.
	/// </returns>
	/// <remarks>If <see langword="this"/> is equidistant from two named colors, the result of this method is not guaranteed to be determinate.</remarks>
	public bool IsClosestToNamedColor ( in ColorName namedColor) => ColorName == namedColor;


	/// <summary>
	/// Determines if the closest named <see cref="Color"/> to <paramref name="color"/>/> is the provided <paramref name="namedColor"/>.
	/// </summary>
	/// <param name="color">The color to test against the <see cref="ColorName"/> value in <paramref name="namedColor"/>.</param>
	/// <param name="namedColor">The <see cref="ColorName"/> to check if this <see cref="Color"/> is closer to than any other configured named color.</param>
	/// <returns><see langword="true"/> if the closest named color to <paramref name="color"/> is the provided value.<br/>
	/// <see langword="false"/> if any other named color is closer to <paramref name="color"/> than <paramref name="namedColor"/>.
	/// </returns>
	/// <remarks>If <paramref name="color"/> is equidistant from two named colors, the result of this method is not guaranteed to be determinate.</remarks>
	public static bool IsColorClosestToNamedColor ( in Color color, in ColorName namedColor )
	{
		return color.IsClosestToNamedColor ( in namedColor );
	}
}