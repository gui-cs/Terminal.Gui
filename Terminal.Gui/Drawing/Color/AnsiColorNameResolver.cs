#nullable enable

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.Drawing;

/// <summary>
/// Color name resolver for <see cref="ColorName16"/>.
/// </summary>
public class AnsiColorNameResolver : IColorNameResolver
{
    private static readonly ImmutableArray<string> _ansiColorNames = ImmutableArray.Create (Enum.GetNames<ColorName16> ());

    /// <inheritdoc/>
    public IEnumerable<string> GetColorNames ()
    {
        return _ansiColorNames;
    }

    /// <inheritdoc/>
    public bool TryNameColor (Color color, [NotNullWhen (true)] out string? name)
    {
        if (Color.TryGetExactNamedColor16 (color, out ColorName16 colorName16))
        {
            name = Color16Name (colorName16);
            return true;
        }
        name = null;
        return false;
    }

    /// <inheritdoc/>
    public bool TryParseColor (ReadOnlySpan<char> name, out Color color)
    {
        if (Enum.TryParse (name, ignoreCase: true, out ColorName16 colorName16) &&
            // Any numerical value converts to undefined enum value.
            Enum.IsDefined (colorName16))
        {
            color = new Color (colorName16);
            return true;
        }
        color = default;
        return false;
    }

    private static string Color16Name (ColorName16 color16)
    {
        return color16 switch
        {
            ColorName16.Black => nameof (ColorName16.Black),
            ColorName16.Blue => nameof (ColorName16.Blue),
            ColorName16.Green => nameof (ColorName16.Green),
            ColorName16.Cyan => nameof (ColorName16.Cyan),
            ColorName16.Red => nameof (ColorName16.Red),
            ColorName16.Magenta => nameof (ColorName16.Magenta),
            ColorName16.Yellow => nameof (ColorName16.Yellow),
            ColorName16.Gray => nameof (ColorName16.Gray),
            ColorName16.DarkGray => nameof (ColorName16.DarkGray),
            ColorName16.BrightBlue => nameof (ColorName16.BrightBlue),
            ColorName16.BrightGreen => nameof (ColorName16.BrightGreen),
            ColorName16.BrightCyan => nameof (ColorName16.BrightCyan),
            ColorName16.BrightRed => nameof (ColorName16.BrightRed),
            ColorName16.BrightMagenta => nameof (ColorName16.BrightMagenta),
            ColorName16.BrightYellow => nameof (ColorName16.BrightYellow),
            ColorName16.White => nameof (ColorName16.White),
            _ => throw new NotSupportedException ($"ColorName16 '{color16}' is not supported.")
        };
    }
}
