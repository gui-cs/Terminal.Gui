#nullable enable
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.Drawing;

/// <summary>
/// Helper class for transforming to and from <see cref="StandardColor"/> enum.
/// </summary>
internal static class StandardColors
{
    private static readonly ImmutableArray<string> _names;
    private static readonly FrozenDictionary<uint, string> _argbNameMap;

    static StandardColors ()
    {
        // Populate based on names because enums with same numerical value
        // are not otherwise distinguishable from each other.
        string [] standardNames = Enum.GetNames<StandardColor> ().Order ().ToArray ();

        Dictionary<uint, string> map = new (standardNames.Length);
        foreach (string name in standardNames)
        {
            StandardColor standardColor = Enum.Parse<StandardColor> (name);
            uint argb = GetArgb (standardColor);
            // TODO: Collect aliases?
            _ = map.TryAdd (argb, name);
        }

        _names = ImmutableArray.Create (standardNames);
        _argbNameMap = map.ToFrozenDictionary ();
    }

    /// <summary>
    /// Gets read-only list of the W3C colors in alphabetical order.
    /// </summary>
    public static IReadOnlyList<string> GetColorNames ()
    {
        return _names;
    }

    /// <summary>
    /// Converts the given Standard (W3C+) color name to equivalent color value.
    /// </summary>
    /// <param name="name">Standard (W3C+) color name.</param>
    /// <param name="color">The successfully converted Standard (W3C+) color value.</param>
    /// <returns>True if the conversion succeeded; otherwise false.</returns>
    public static bool TryParseColor (ReadOnlySpan<char> name, out Color color)
    {
        if (!Enum.TryParse (name, ignoreCase: true, out StandardColor standardColor) ||
            // Any numerical value converts to undefined enum value.
            !Enum.IsDefined (standardColor))
        {
            color = default;
            return false;
        }

        uint argb = GetArgb (standardColor);
        color = new Color (argb);
        return true;
    }

    /// <summary>
    /// Converts the given color value to a Standard (W3C+) color name.
    /// </summary>
    /// <param name="color">Color value to match Standard (W3C+)color.</param>
    /// <param name="name">The successfully converted Standard (W3C+) color name.</param>
    /// <returns>True if conversion succeeded; otherwise false.</returns>
    public static bool TryNameColor (Color color, [NotNullWhen (true)] out string? name)
    {
        if (_argbNameMap.TryGetValue (color.Argb, out name))
        {
            return true;
        }

        name = null;
        return false;
    }

    internal static uint GetArgb (StandardColor standardColor)
    {
        const int ALPHA_SHIFT = 24;
        const uint ALPHA_MASK = 0xFFU << ALPHA_SHIFT;

        int rgb = (int)standardColor;

        uint argb = (uint)rgb | ALPHA_MASK;
        return argb;
    }
}
