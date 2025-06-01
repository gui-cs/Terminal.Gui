#nullable enable

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Terminal.Gui.Drawing;

/// <summary>
/// Color name resolver prioritizing Standard (W3C+) colors with fallback to ANSI 4-bit (16) colors.
/// </summary>
public class MultiStandardColorNameResolver : IColorNameResolver
{
    private static readonly AnsiColorNameResolver _ansi = new ();
    private static readonly StandardColorsNameResolver _standard = new ();

    private static readonly ImmutableArray<string> _combinedColorNames;
    private static readonly FrozenDictionary<string, Color> _nameToColorMap;
    private static readonly FrozenDictionary<uint, string> _colorToNameMap;

    static MultiStandardColorNameResolver ()
    {
        Dictionary<string, Color> nameToColor = new (StringComparer.OrdinalIgnoreCase);
        Dictionary<uint, string> colorToName = new ();

        foreach (string name in _standard.GetColorNames ())
        {
            if (_standard.TryParseColor (name, out Color color))
            {
                if (nameToColor.TryAdd (name, color))
                {
                    _ = colorToName.TryAdd (color.Argb, name);
                }
            }
        }

        foreach (string name in _ansi.GetColorNames ())
        {
            if (_ansi.TryParseColor (name, out Color color))
            {
                nameToColor.TryAdd (name, color);
                colorToName.TryAdd (color.Argb, name);
            }
        }

        _nameToColorMap = nameToColor.ToFrozenDictionary ();
        _colorToNameMap = colorToName.ToFrozenDictionary ();
        _combinedColorNames = nameToColor.Keys.Order ().ToImmutableArray ();
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetColorNames () => _combinedColorNames;

    /// <inheritdoc/>
    public bool TryParseColor (ReadOnlySpan<char> name, out Color color)
    {
        if (name.StartsWith ("#") || name.StartsWith ("0x", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                color = Color.Parse (name.ToString (), CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                color = default;
                return false;
            }
        }

        if (_ansi.TryParseColor (name, out color)) return true;
        if (_standard.TryParseColor (name, out color)) return true;

        color = default;
        return false;
    }


    /// <inheritdoc/>
    public bool TryNameColor (Color color, [NotNullWhen (true)] out string? name)
    {
        return _colorToNameMap.TryGetValue (color.Argb, out name);
    }
}
