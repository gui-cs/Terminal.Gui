#nullable enable
using System.Collections;
using System.Globalization;
using System.Resources;
using Terminal.Gui.Resources;

namespace Terminal.Gui;

/// <summary>
///     Provides a mapping between <see cref="Color"/> and the W3C standard color name strings.
/// </summary>
public static class ColorStrings
{
    // PERFORMANCE: See https://stackoverflow.com/a/15521524/297526 for why GlobalResources.GetString is fast.

    /// <summary>
    ///     Gets the W3C standard string for <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns><see langword="null"/> if there is no standard color name for the specified color.</returns>
    public static string? GetW3CColorName (Color color)
    {
        return GlobalResources.GetString ($"#{color.R:X2}{color.G:X2}{color.B:X2}", CultureInfo.CurrentUICulture);
    }

    /// <summary>
    ///     Returns the list of W3C standard color names.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> GetW3CColorNames ()
    {
        ResourceSet? resourceSet = GlobalResources.GetResourceSet (CultureInfo.CurrentUICulture, true, true);
        if (resourceSet == null)
        {
            yield break;
        }

        foreach (DictionaryEntry entry in resourceSet)
        {
            if (entry is { Value: string colorName, Key: string key } && key.StartsWith ('#'))
            {
                yield return colorName;
            }
        }
    }

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is a W3C standard named color.
    /// </summary>
    /// <param name="name">The name to parse.</param>
    /// <param name="color">If successful, the color.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was parsed successfully.</returns>
    public static bool TryParseW3CColorName (string name, out Color color)
    {
        foreach (DictionaryEntry entry in GlobalResources.GetResourceSet (CultureInfo.CurrentUICulture, true, true)!)
        {
            if (entry.Value is string colorName && colorName.Equals (name, StringComparison.OrdinalIgnoreCase))
            {
                return TryParseColorKey (entry.Key.ToString (), out color);
            }
        }

        return TryParseColorKey (name, out color);

        bool TryParseColorKey (string? key, out Color color)
        {
            if (key != null && key.StartsWith ('#') && key.Length == 7)
            {
                if (int.TryParse (key.AsSpan (1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) &&
                    int.TryParse (key.AsSpan (3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) &&
                    int.TryParse (key.AsSpan (5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
                {
                    color = new Color (r, g, b);
                    return true;
                }
            }

            color = default (Color);
            return false;
        }
    }
}
