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
    private static readonly ResourceManager _resourceManager = new ResourceManager (typeof (Strings));

    /// <summary>
    ///     Gets the W3C standard string for <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns><see langword="null"/> if there is no standard color name for the specified color.</returns>
    public static string? GetW3CColorName (Color color)
    {
        // Fetch the color name from the resource file
        return _resourceManager.GetString ($"#{color.R:X2}{color.G:X2}{color.B:X2}", CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is a W3C standard named color.
    /// </summary>
    /// <param name="name">The name to parse.</param>
    /// <param name="color">If successful, the color.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was parsed successfully.</returns>
    public static bool TryParseW3CColorName (string name, out Color color)
    {
        // Iterate through all resource entries to find the matching color name
        foreach (DictionaryEntry entry in _resourceManager.GetResourceSet (CultureInfo.CurrentCulture, true, true)!)
        {
            if (entry.Value is string colorName && colorName.Equals (name, StringComparison.OrdinalIgnoreCase))
            {
                // Parse the key to extract the color components
                string key = entry.Key.ToString () ?? string.Empty;
                if (key.StartsWith ("#") && key.Length == 7)
                {
                    if (int.TryParse (key.Substring (1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) &&
                        int.TryParse (key.Substring (3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) &&
                        int.TryParse (key.Substring (5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
                    {
                        color = new Color (r, g, b);
                        return true;
                    }
                }
            }
        }

        color = default;
        return false;
    }

    public static IEnumerable<string> GetW3CColorNames ()
    {
        foreach (DictionaryEntry entry in _resourceManager.GetResourceSet (CultureInfo.CurrentCulture, true, true)!)
        {
            if (entry.Value is string colorName)
            {
                yield return colorName;
            }
        }
    }
}
