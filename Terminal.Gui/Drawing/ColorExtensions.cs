using System.Collections.Frozen;

namespace Terminal.Gui;

static class ColorExtensions {
    static ColorExtensions () {
        Dictionary<ColorName, AnsiColorCode> nameToCodeMap = new () {
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

        ColorToNameMap = new Dictionary<Color, ColorName> {
            // using "Windows 10 Console/PowerShell 6" here: https://i.stack.imgur.com/9UVnC.png
            // See also: https://en.wikipedia.org/wiki/ANSI_escape_code
            { new Color (12, 12, 12), ColorName.Black },
            { new Color (0, 55, 218), ColorName.Blue },
            { new Color (19, 161, 14), ColorName.Green },
            { new Color (58, 150, 221), ColorName.Cyan },
            { new Color (197, 15, 31), ColorName.Red },
            { new Color (136, 23, 152), ColorName.Magenta },
            { new Color (128, 64, 32), ColorName.Yellow },
            { new Color (204, 204, 204), ColorName.Gray },
            { new Color (118, 118, 118), ColorName.DarkGray },
            { new Color (59, 120, 255), ColorName.BrightBlue },
            { new Color (22, 198, 12), ColorName.BrightGreen },
            { new Color (97, 214, 214), ColorName.BrightCyan },
            { new Color (231, 72, 86), ColorName.BrightRed },
            { new Color (180, 0, 158), ColorName.BrightMagenta },
            { new Color (249, 241, 165), ColorName.BrightYellow },
            { new Color (242, 242, 242), ColorName.White }
        }.ToFrozenDictionary ();
    }

    private static FrozenDictionary<Color, ColorName> colorToNameMap;

    /// <summary>
    ///     Gets or sets a <see cref="FrozenDictionary{TKey,TValue}"/> that maps legacy 16-color values to the
    ///     corresponding <see cref="ColorName"/>.
    /// </summary>
    /// <remarks>
    ///     Setter should be called as infrequently as possible, as <see cref="FrozenDictionary{TKey,TValue}"/> is
    ///     expensive to create.
    /// </remarks>
    internal static FrozenDictionary<Color, ColorName> ColorToNameMap {
        get => colorToNameMap;
        set {
            colorToNameMap = value;

            //Also be sure to set the reverse mapping
            ColorNameToColorMap = value.ToFrozenDictionary (static kvp => kvp.Value, static kvp => kvp.Key);
        }
    }

    /// <summary>Defines the 16 legacy color names and their corresponding ANSI color codes.</summary>
    internal static FrozenDictionary<ColorName, AnsiColorCode> ColorNameToAnsiColorMap { get; }

    /// <summary>Reverse mapping for <see cref="ColorToNameMap"/>.</summary>
    internal static FrozenDictionary<ColorName, Color> ColorNameToColorMap { get; private set; }
}
