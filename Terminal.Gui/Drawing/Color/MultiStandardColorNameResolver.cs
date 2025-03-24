using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Terminal.Gui;

/// <summary>
/// Backwards compatible(-ish) color name resolver prioritizing ANSI 4-bit (16) colors with fallback to W3C colors.
/// </summary>
public class MultiStandardColorNameResolver : IColorNameResolver
{
    private static readonly AnsiColorNameResolver Ansi = new();
    private static readonly W3cColorNameResolver W3c = new();
    private static readonly FrozenDictionary<int, string> W3cBlockedColorNameHashMap;
    private static readonly FrozenSet<Color> W3cBlockedColors;
    private static readonly ImmutableArray<string> CombinedColorNames;

    static MultiStandardColorNameResolver ()
    {
        HashSet<string> combinedNames = new(Ansi.GetColorNames());

        HashSet<string> w3cInconsistentColorNames = new(StringComparer.OrdinalIgnoreCase);
        HashSet<Color> w3cInconsistentColors = new();

        IEnumerable<string> enumerableW3cNames = W3c.GetColorNames ();
        IReadOnlyList<string> w3cNames =  enumerableW3cNames is IReadOnlyList<string> alreadyReadOnlyList
            ? alreadyReadOnlyList
            : [.. enumerableW3cNames];

        Dictionary<Color, HashSet<string>> w3cColorsWithAlternativeNames = w3cNames
            .GroupBy(w3cName =>
            {
                if (!W3c.TryParseColor(w3cName, out Color w3cColor))
                {
                    throw new InvalidOperationException ($"W3C color name '{w3cName}' does not resolve to any W3C color.");
                }
                return w3cColor;
            })
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.ToHashSet());

        // Gather inconsistencies between ANSI and W3C, filter out problematic W3C names and
        // create additional blocklists for W3C names and colors.
        // Blocking and filtering is only applied to W3C because this resolver prioritizes ANSI for backwards compatibility.
        // It would be a lot simpler to just prioritize W3C colors and names.
        foreach (string w3cName in w3cNames)
        {
            if (w3cInconsistentColorNames.Contains (w3cName))
            {
                // Already blocked, most likely through alternative name.
                continue;
            }

            if (!W3c.TryParseColor (w3cName, out Color w3cColor))
            {
                // This condition is just inverted to reduce indentation.
                // This should practically never happen if the W3C color name resolver is properly implemented.
                throw new InvalidOperationException ($"W3C color name '{w3cName}' does not resolve to any color.");
            }

            if (w3cColorsWithAlternativeNames.TryGetValue (w3cColor, out var names))
            {
                bool blocked = false;
                // Alternative names cause issues with ColorPicker etc. when combined with ANSI and prioritizing ANSI resolver.
                // For example Aqua is not in ColorName16 but the actual color value resolves to ANSI Cyan
                // so autocomplete for Aqua suddenly changes to Cyan because they happen to have same color value in both color scheme.
                // Also DarkGrey would cause inconsistencies because the alternative DarkGray exists in ANSI and has different color value.
                foreach (string name in names)
                {
                    if (Ansi.TryParseColor (name, out _))
                    {
                        w3cInconsistentColors.Add (w3cColor);
                        // Block all if one is inconsistent.
                        foreach (string inconsistentName in names)
                        {
                            w3cInconsistentColorNames.Add (inconsistentName);
                        }
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                {
                    // Already blocked continue to next W3C color name.
                    continue;
                }
            }

            // Just in case check.
            // Same name, different ANSI value.
            // For example both #767676 (ColorName16) and #A9A9A9 (W3C) resolve to DarkGray,
            // although a bad example because it is already filtered due to also having alternative names.
            if (Ansi.TryParseColor (w3cName, out Color ansiColor) && w3cColor != ansiColor)
            {
                w3cInconsistentColorNames.Add (w3cName);
                w3cInconsistentColors.Add (w3cColor);
                continue;
            }

            combinedNames.Add (w3cName);
        }

        // TODO: Utilize .NET 9 and later alternative lookup for matching ReadOnlySpan<char> with string.
        W3cBlockedColorNameHashMap = w3cInconsistentColorNames.ToFrozenDictionary (
            // Workaround for alternative lookup not being available in .NET 8 by matching ReadOnlySpan<char> hash code to string hash code.
            keySelector: x => string.GetHashCode (x, StringComparison.OrdinalIgnoreCase),
            // String element is just for verifying hash code collision, e.g. same hash code but the name was different.
            // Quite unlikely due to small data set, but still possible.
            elementSelector: x => x);
        W3cBlockedColors = w3cInconsistentColors.ToFrozenSet ();
        CombinedColorNames = combinedNames.Order ().ToImmutableArray ();
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetColorNames ()
    {
        return CombinedColorNames;
    }

    /// <inheritdoc/>
    public bool TryNameColor (Color color, [NotNullWhen (true)] out string? name)
    {
        if (Ansi.TryNameColor (color, out string? ansiName))
        {
            name = ansiName;
            return true;
        }

        if (!IsBlockedW3cColor (color) &&
            W3c.TryNameColor (color, out string? w3cName) &&
            !IsBlockedW3cName (w3cName))
        {
            name = w3cName;
            return true;
        }

        name = null;
        return false;
    }

    /// <inheritdoc/>
    public bool TryParseColor (ReadOnlySpan<char> name, out Color color)
    {
        if (Ansi.TryParseColor (name, out color))
        {
            return true;
        }

        if (!IsBlockedW3cName (name) &&
            W3c.TryParseColor (name, out color) &&
            !IsBlockedW3cColor (color))
        {
            return true;
        }

        color = default;
        return false;
    }

    private bool IsBlockedW3cName (ReadOnlySpan<char> name)
    {
        int nameHashCode = string.GetHashCode(name, StringComparison.OrdinalIgnoreCase);
        return W3cBlockedColorNameHashMap.TryGetValue (nameHashCode, out string? inconsistentColorName) &&
            name.Equals (inconsistentColorName, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsBlockedW3cColor (Color color)
    {
        return W3cBlockedColors.Contains (color);
    }
}
