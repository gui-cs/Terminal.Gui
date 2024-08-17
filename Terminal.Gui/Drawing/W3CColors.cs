using System.Collections;
using System.Globalization;
using System.Resources;
using System.Xml.Linq;

namespace Terminal.Gui;

/// <summary>
/// Helper class that resolves w3c color names to their hex values
/// Based on https://www.w3schools.com/colors/color_tryit.asp
/// </summary>
public class W3CColors : IColorNameResolver
{
    public IEnumerable<string> GetColorNames ()
    {
        return ColorStrings.GetW3CColorNames ();
    }

    public bool TryParseColor (string name, out Color color)
    {
        return ColorStrings.TryParseW3CColorName (name, out color);
    }

    /// <inheritdoc />
    public bool TryNameColor (Color color, out string name)
    {
        var answer = ColorStrings.GetW3CColorName (color);

        name = answer ?? string.Empty;
        return answer != null;
    }
}