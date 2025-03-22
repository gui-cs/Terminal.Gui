#nullable enable
using System.Globalization;

namespace Terminal.Gui;

/// <summary>
///     Provides a mapping between <see cref="Color"/> and the W3C standard color name strings.
/// </summary>
public static class ColorStrings
{
    /// <summary>
    ///     Gets the W3C standard string for <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns><see langword="null"/> if there is no standard color name for the specified color.</returns>
    public static string? GetW3CColorName (Color color)
    {
        if (W3cColors.TryNameColor (color, out string? name))
        {
            return name;
        }
        return null;
    }

    /// <summary>
    ///     Gets the ANSI 4-bit (16) color name for <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns><see langword="null"/> if there is no standard color name for the specified color.</returns>
    public static string? GetANSIColor16Name (Color color)
    {
        if (Color.TryGetExactNamedColor16 (color, out ColorName16 color16))
        {
            return Color16Name (color16);
        }
        return null;
    }

    private static string Color16Name (ColorName16 color16)
    {
        return color16 switch
        {
            ColorName16.Black => nameof (ColorName16.Black),
            ColorName16.Blue => nameof (ColorName16.Blue),
            ColorName16.Green => nameof (ColorName16.Green),
            ColorName16.Cyan => nameof (ColorName16.Cyan),
            ColorName16.Red => nameof (ColorName16.Red),
            ColorName16.Magenta => nameof (ColorName16.Magenta),
            ColorName16.Yellow => nameof (ColorName16.Yellow),
            ColorName16.Gray => nameof (ColorName16.Gray),
            ColorName16.DarkGray => nameof (ColorName16.DarkGray),
            ColorName16.BrightBlue => nameof (ColorName16.BrightBlue),
            ColorName16.BrightGreen => nameof (ColorName16.BrightGreen),
            ColorName16.BrightCyan => nameof (ColorName16.BrightCyan),
            ColorName16.BrightRed => nameof (ColorName16.BrightRed),
            ColorName16.BrightMagenta => nameof (ColorName16.BrightMagenta),
            ColorName16.BrightYellow => nameof (ColorName16.BrightYellow),
            ColorName16.White => nameof (ColorName16.White),
            _ => throw new NotSupportedException ($"ColorName16 '{color16}' is not supported.")
        };
    }

    /// <summary>
    ///     Returns the list of W3C standard color names.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> GetW3CColorNames ()
    {
        return W3cColors.GetColorNames ();
    }

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is a W3C standard named color.
    /// </summary>
    /// <param name="name">The name to parse.</param>
    /// <param name="color">If successful, the color.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was parsed successfully.</returns>
    public static bool TryParseW3CColorName (ReadOnlySpan<char> name, out Color color)
    {
        if (W3cColors.TryParseColor (name, out color))
        {
            return true;
        }

        return TryParseColorKey (name, out color);

        static bool TryParseColorKey (ReadOnlySpan<char> key, out Color color)
        {
            if (!key.IsEmpty && key [0] == '#' && key.Length == 7)
            {
                if (int.TryParse (key.Slice (1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) &&
                    int.TryParse (key.Slice (3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) &&
                    int.TryParse (key.Slice (5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
                {
                    color = new Color (r, g, b);
                    return true;
                }
            }

            color = default (Color);
            return false;
        }
    }

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is a ANSI 4-bit standard named color.
    /// </summary>
    /// <param name="name">The name to parse.</param>
    /// <param name="color">If successful, the color.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was parsed successfully.</returns>
    public static bool TryParseColor16 (ReadOnlySpan<char> name, out Color color)
    {
        if (Enum.TryParse (name, ignoreCase: true, out ColorName16 color16))
        {
            color = new Color (color16);
            return true;
        }

        color = default;
        return false;
    }
}
