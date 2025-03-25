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
    private static readonly FrozenDictionary<uint, string> ArgbNameMap;

    static W3cColors ()
    {
        // Populate based on names because enums with same numerical value
        // are not otherwise distinguishable from each other.
        string[] w3cNames = Enum.GetNames<W3cColor> ().Order().ToArray();

        Dictionary<uint, string> map = new(w3cNames.Length);
        foreach (string name in w3cNames)
        {
            W3cColor w3c = Enum.Parse<W3cColor>(name);
            uint argb = GetArgb(w3c);
            // TODO: Collect aliases?
            _ = map.TryAdd (argb, name);
        }

        Names = ImmutableArray.Create (w3cNames);
        ArgbNameMap = map.ToFrozenDictionary ();
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

        uint argb = GetArgb (w3cColor);
        color = new Color (argb);
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
        if (ArgbNameMap.TryGetValue (color.Argb, out name))
        {
            return true;
        }

        name = null;
        return false;
    }

    private static uint GetArgb (W3cColor w3cColor)
    {
        const int alphaShift = 24;
        const uint alphaMask = 0xFFU << alphaShift;

        int rgb = (int)w3cColor;

        uint argb = (uint)rgb | alphaMask;
        return argb;
    }
}
