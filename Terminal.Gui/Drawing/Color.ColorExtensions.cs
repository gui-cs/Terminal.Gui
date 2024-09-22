using System.Collections.Frozen;

namespace Terminal.Gui;

internal static class ColorExtensions
{
    private static FrozenDictionary<Color, ColorName> colorToNameMap;

    static ColorExtensions ()
    {
        Dictionary<ColorName, AnsiColorCode> nameToCodeMap = new ()
        {
            { ColorName.Black, AnsiColorCode.BLACK },
            { ColorName.Blue, AnsiColorCode.BLUE },
            { ColorName.Green, AnsiColorCode.GREEN },
            { ColorName.Cyan, AnsiColorCode.CYAN },
            { ColorName.Red, AnsiColorCode.RED },
            { ColorName.Magenta, AnsiColorCode.MAGENTA },
            { ColorName.Yellow, AnsiColorCode.YELLOW },
            { ColorName.Gray, AnsiColorCode.WHITE },
            { ColorName.DarkGray, AnsiColorCode.BRIGHT_BLACK },
            { ColorName.BrightBlue, AnsiColorCode.BRIGHT_BLUE },
            { ColorName.BrightGreen, AnsiColorCode.BRIGHT_GREEN },
            { ColorName.BrightCyan, AnsiColorCode.BRIGHT_CYAN },
            { ColorName.BrightRed, AnsiColorCode.BRIGHT_RED },
            { ColorName.BrightMagenta, AnsiColorCode.BRIGHT_MAGENTA },
            { ColorName.BrightYellow, AnsiColorCode.BRIGHT_YELLOW },
            { ColorName.White, AnsiColorCode.BRIGHT_WHITE }
        };
        ColorNameToAnsiColorMap = nameToCodeMap.ToFrozenDictionary ();

        var colorToNameDict = new Dictionary<Color, ColorName> ();

        foreach (ColorName colorName in Enum.GetValues<ColorName> ())
        {
            if (ColorStrings.TryParseW3CColorName (Enum.GetName<ColorName> (colorName), out Color color))
            {
                colorToNameDict [color] = colorName;
            }
        }

        ColorToNameMap = colorToNameDict.ToFrozenDictionary ();
    }

    /// <summary>Defines the 16 legacy color names and their corresponding ANSI color codes.</summary>
    internal static FrozenDictionary<ColorName, AnsiColorCode> ColorNameToAnsiColorMap { get; }

    /// <summary>Reverse mapping for <see cref="ColorToNameMap"/>.</summary>
    internal static FrozenDictionary<ColorName, Color> ColorNameToColorMap { get; private set; }

    /// <summary>
    ///     Gets or sets a <see cref="FrozenDictionary{TKey,TValue}"/> that maps legacy 16-color values to the
    ///     corresponding <see cref="ColorName"/>.
    /// </summary>
    /// <remarks>
    ///     Setter should be called as infrequently as possible, as <see cref="FrozenDictionary{TKey,TValue}"/> is
    ///     expensive to create.
    /// </remarks>
    internal static FrozenDictionary<Color, ColorName> ColorToNameMap
    {
        get => colorToNameMap;
        set
        {
            colorToNameMap = value;

            //Also be sure to set the reverse mapping
            ColorNameToColorMap = value.ToFrozenDictionary (static kvp => kvp.Value, static kvp => kvp.Key);
        }
    }
}
