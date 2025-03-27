#nullable enable

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;

/// <summary>
/// Backwards compatible(-ish) color name resolver prioritizing ANSI 4-bit (16) colors with fallback to W3C colors.
/// </summary>
public class MultiStandardColorNameResolver : IColorNameResolver
{
    private static readonly AnsiColorNameResolver Ansi = new();
    private static readonly W3cColorNameResolver W3c = new();
    private static readonly FrozenSet<Color> W3cBlockedColors;
    private static readonly ImmutableArray<string> CombinedColorNames;
    private static readonly FrozenDictionary<int, (string Name, Color Color)> W3cSubstituteColors;

    static MultiStandardColorNameResolver ()
    {
        HashSet<string> combinedNames = new(Ansi.GetColorNames());

        HashSet<Color> w3cInconsistentColors = new();
        Dictionary<string, Color> w3cSubstituteColors = new(StringComparer.OrdinalIgnoreCase);

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

        // Gather inconsistencies between ANSI and W3C, filter out or substitute problematic W3C colors and names,
        // and create additional blocklist for W3C colors.
        // Blocking and filtering is only applied to W3C because this resolver prioritizes ANSI for backwards compatibility.
        // It would be a lot simpler to just prioritize W3C colors and names.
        foreach (string w3cName in w3cNames)
        {
            if (w3cSubstituteColors.ContainsKey (w3cName))
            {
                // Already dealt with alternative name.
                continue;
            }

            if (!W3c.TryParseColor (w3cName, out Color w3cColor))
            {
                // This condition is just inverted to reduce indentation.
                // Also it should practically never happen if the W3C color name resolver is properly implemented.
                throw new InvalidOperationException ($"W3C color name '{w3cName}' does not resolve to any color.");
            }

            if (w3cColorsWithAlternativeNames.TryGetValue (w3cColor, out var names))
            {
                bool substituted = false;
                // Alternative names cause issues with ColorPicker etc. when combined with ANSI and prioritizing ANSI resolver.
                // For example Aqua is not in ColorName16 but the actual color value resolves to ANSI Cyan
                // so autocomplete for Aqua suddenly changes to Cyan because they happen to have same color value in both color scheme.
                // Also DarkGrey would cause inconsistencies because the alternative DarkGray exists in ANSI and has different color value.
                foreach (string name in names)
                {
                    if (Ansi.TryParseColor (name, out Color substituteColor))
                    {
                        // Block the W3C color when it is inconsistent with the substitute color
                        // so there is no situation where W3C color -> color name -> ANSI color.
                        if (w3cColor != substituteColor)
                        {
                            w3cInconsistentColors.Add (w3cColor);
                        }

                        // Substitute all W3C alternatives to match with the ANSI color to keep colors consistent.
                        foreach (string alternativeName in names)
                        {
                            w3cSubstituteColors.Add (alternativeName, substituteColor);
                            combinedNames.Add (alternativeName);
                        }
                        substituted = true;
                        break;
                    }
                }

                if (substituted)
                {
                    // Already dealt with, continue to next W3C color name.
                    continue;
                }
            }

            // Same name, different ANSI value.
            // For example both #767676 (ColorName16) and #A9A9A9 (W3C) resolve to DarkGray,
            // although a bad example because it is already substituted due to also having alternative names.
            if (Ansi.TryParseColor (w3cName, out Color ansiColor) && w3cColor != ansiColor)
            {
                w3cInconsistentColors.Add (w3cColor);
                continue;
            }

            combinedNames.Add (w3cName);
        }

        // TODO: Utilize .NET 9 and later alternative lookup for matching ReadOnlySpan<char> with string.
        W3cSubstituteColors = w3cSubstituteColors.ToFrozenDictionary (
            // Workaround for alternative lookup not being available in .NET 8 by matching ReadOnlySpan<char> hash code to string hash code.
            keySelector: kvp => string.GetHashCode (kvp.Key, StringComparison.OrdinalIgnoreCase),
            // The string element is for detecting hash collision.
            elementSelector: kvp => (kvp.Key, kvp.Value));
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
            W3c.TryNameColor (color, out string? w3cName))
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

        if (GetSubstituteW3cColor (name, out color))
        {
            return true;
        }

        if (W3c.TryParseColor (name, out color) &&
            !IsBlockedW3cColor (color))
        {
            return true;
        }

        color = default;
        return false;
    }

    private static bool GetSubstituteW3cColor (ReadOnlySpan<char> name, out Color substituteColor)
    {
        int nameHashCode = string.GetHashCode(name, StringComparison.OrdinalIgnoreCase);
        if (W3cSubstituteColors.TryGetValue (nameHashCode, out var match) &&
            match is (string matchName, Color matchColor) &&
            name.Equals (matchName, StringComparison.OrdinalIgnoreCase))
        {
            substituteColor = matchColor;
            return true;
        }
        substituteColor = default;
        return false;
    }

    private static bool IsBlockedW3cColor (Color color)
    {
        return W3cBlockedColors.Contains (color);
    }
}
