#nullable enable

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;

/// <summary>
///     Helper class that resolves w3c color names to their hex values
///     Based on https://www.w3schools.com/colors/color_tryit.asp
/// </summary>
[Obsolete ("Superseded by W3cColors")]
public class W3CColors : IColorNameResolver
{
    /// <inheritdoc/>
    [Obsolete ("Prefer W3cColors.GetColorNames()")]
    public IEnumerable<string> GetColorNames () { return ColorStrings.GetW3CColorNames (); }

    /// <inheritdoc/>
    public bool TryParseColor (string name, out Color color) { return ColorStrings.TryParseW3CColorName (name, out color); }

    /// <inheritdoc/>
    public bool TryNameColor (Color color, out string name)
    {
        string? answer = ColorStrings.GetW3CColorName (color);

        name = answer ?? string.Empty;

        return answer != null;
    }
}

/// <summary>
/// Helper class for transforming to and from <see cref="W3cColor"/> enum.
/// </summary>
public static class W3cColors
{
    private static readonly ImmutableArray<string> Names;
    private static readonly FrozenDictionary<int, string> RgbNameMap;

    static W3cColors ()
    {
        // Populate based on names because enums with same name are not otherwise distinguishable from each other.
        string[] w3cNames = Enum.GetNames<W3cColor> ().Order().ToArray();

        Dictionary<int, string> map = new(w3cNames.Length);
        foreach (string name in w3cNames)
        {
            W3cColor w3c = Enum.Parse<W3cColor>(name);
            int rgb = (int)w3c;
            // TODO: Collect aliases?
            _ = map.TryAdd (rgb, name);
        }

        Names = ImmutableArray.Create (w3cNames);
        RgbNameMap = map.ToFrozenDictionary ();
    }

    /// <summary>
    /// Gets read-only list of the W3C colors in alphabetical order.
    /// </summary>
    public static IReadOnlyList<string> GetColorNames ()
    {
        return Names;
    }

    /// <summary>
    /// Tries to parse W3C color from the given name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="color">Contains the successfully parsed <see cref="W3cColor"/> value.</param>
    /// <returns>True if parsed successfully; otherwise false.</returns>
    public static bool TryParseColor (ReadOnlySpan<char> name, out Color color)
    {
        if (!Enum.TryParse (name, ignoreCase: true, out W3cColor w3cColor))
        {
            color = default;
            return false;
        }

        (byte red, byte green, byte blue) = GetRgbComponents (w3cColor);
        color = new Color (red, green, blue);
        return true;
    }

    /// <summary>
    /// Tries to match the given color RGB value to a W3C color and returns the name.
    /// </summary>
    /// <param name="color">Color to match W3C RGB value.</param>
    /// <param name="name">Contains name of matching W3C color.</param>
    /// <returns>True if match; otherwise false.</returns>
    public static bool TryNameColor (Color color, [NotNullWhen (true)] out string? name)
    {
        int rgb = GetRgb (color.R, color.G, color.B);
        if (RgbNameMap.TryGetValue (rgb, out name))
        {
            return true;
        }

        name = null;
        return false;
    }

    private const int RgbRedShift = 16;
    private const int RgbGreenShift = 8;
    private const int RgbBlueShift = 0;
    private const int RgbRedMask = 0xFF << RgbRedShift;
    private const int RgbGreenMask = 0xFF << RgbGreenShift;
    private const int RgbBlueMask = 0xFF << RgbBlueShift;

    private static (byte Red, byte Green, byte Blue) GetRgbComponents (W3cColor w3cColor)
    {
        int rgb = (int)w3cColor;
        byte red = (byte)((rgb & RgbRedMask) >> RgbRedShift);
        byte green = (byte)((rgb & RgbGreenMask) >> RgbGreenShift);
        byte blue = (byte)((rgb & RgbBlueMask) >> RgbBlueShift);
        return (red, green, blue);
    }

    private static int GetRgb (byte red, byte green, byte blue) =>
        red << RgbRedShift |
        green << RgbGreenShift |
        blue << RgbBlueShift;
}
