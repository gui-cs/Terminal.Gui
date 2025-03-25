#nullable enable

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;

/// <summary>
/// W3C color name resolver.
/// </summary>
public class W3cColorNameResolver : IColorNameResolver
{
    /// <inheritdoc/>
    public IEnumerable<string> GetColorNames () =>
        W3cColors.GetColorNames ();

    /// <inheritdoc/>
    public bool TryParseColor (ReadOnlySpan<char> name, out Color color) =>
        W3cColors.TryParseColor (name, out color);

    /// <inheritdoc/>
    public bool TryNameColor (Color color, [NotNullWhen (true)] out string? name) =>
        W3cColors.TryNameColor (color, out name);
}

/// <summary>
/// Helper class for transforming to and from <see cref="W3cColor"/> enum.
/// </summary>
internal static class W3cColors
{
    private static readonly ImmutableArray<string> Names;
    private static readonly FrozenDictionary<int, string> RgbNameMap;

    static W3cColors ()
    {
        // Populate based on names because enums with same numerical value
        // are not otherwise distinguishable from each other.
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
    /// Converts the given W3C color name to equivalent color value.
    /// </summary>
    /// <param name="name">W3C color name.</param>
    /// <param name="color">The successfully converted W3C color value.</param>
    /// <returns>True if the conversion succeeded; otherwise false.</returns>
    public static bool TryParseColor (ReadOnlySpan<char> name, out Color color)
    {
        if (!Enum.TryParse (name, ignoreCase: true, out W3cColor w3cColor) ||
            // Any numerical value converts to undefined enum value.
            !Enum.IsDefined (w3cColor))
        {
            color = default;
            return false;
        }

        (byte red, byte green, byte blue) = GetRgbComponents (w3cColor);
        color = new Color (red, green, blue);
        return true;
    }

    /// <summary>
    /// Converts the given color value to a W3C color name.
    /// </summary>
    /// <param name="color">Color value to match W3C color.</param>
    /// <param name="name">The successfully converted W3C color name.</param>
    /// <returns>True if conversion succeeded; otherwise false.</returns>
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
