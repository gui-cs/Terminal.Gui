namespace Terminal.Gui;

/// <summary>
/// Represents the W3C color names with their RGB values.
/// </summary>
/// <remarks>
/// Based on https://www.w3schools.com/colors/color_tryit.asp page.
/// </remarks>
public enum W3cColor
{
    /// <summary>
    /// Alice blue RGB(240, 248, 255).
    /// </summary>
    AliceBlue = 0xF0F8FF,

    /// <summary>
    /// Antique white RGB(250, 235, 215).
    /// </summary>
    AntiqueWhite = 0xFAEBD7,

    /// <summary>
    /// Aqua RGB(0, 255, 255).
    /// </summary>
    Aqua = 0x00FFFF,

    /// <summary>
    /// Aquamarine RGB(127, 255, 212).
    /// </summary>
    Aquamarine = 0x7FFFD4,

    /// <summary>
    /// Azure RGB(240, 255, 255).
    /// </summary>
    Azure = 0xF0FFFF,

    /// <summary>
    /// Beige RGB(245, 245, 220).
    /// </summary>
    Beige = 0xF5F5DC,

    /// <summary>
    /// Bisque RGB(255, 228, 196).
    /// </summary>
    Bisque = 0xFFE4C4,

    /// <summary>
    /// Black RGB(0, 0, 0).
    /// </summary>
    Black = 0x000000,

    /// <summary>
    /// Blanched almond RGB(255, 235, 205).
    /// </summary>
    BlanchedAlmond = 0xFFEBCD,

    /// <summary>
    /// Blue RGB(0, 0, 255).
    /// </summary>
    Blue = 0x0000FF,

    /// <summary>
    /// Blue violet RGB(138, 43, 226).
    /// </summary>
    BlueViolet = 0x8A2BE2,

    /// <summary>
    /// Brown RGB(165, 42, 42).
    /// </summary>
    Brown = 0xA52A2A,

    /// <summary>
    /// Burly wood RGB(222, 184, 135).
    /// </summary>
    BurlyWood = 0xDEB887,

    /// <summary>
    /// Cadet blue RGB(95, 158, 160).
    /// </summary>
    CadetBlue = 0x5F9EA0,

    /// <summary>
    /// Chartreuse RGB(127, 255, 0).
    /// </summary>
    Chartreuse = 0x7FFF00,

    /// <summary>
    /// Chocolate RGB(210, 105, 30).
    /// </summary>
    Chocolate = 0xD2691E,

    /// <summary>
    /// Coral RGB(255, 127, 80).
    /// </summary>
    Coral = 0xFF7F50,

    /// <summary>
    /// Cornflower blue RGB(100, 149, 237).
    /// </summary>
    CornflowerBlue = 0x6495ED,

    /// <summary>
    /// Cornsilk RGB(255, 248, 220).
    /// </summary>
    Cornsilk = 0xFFF8DC,

    /// <summary>
    /// Crimson RGB(220, 20, 60).
    /// </summary>
    Crimson = 0xDC143C,

    /// <summary>
    /// Cyan RGB(0, 255, 255).
    /// </summary>
    /// <remarks>
    /// Same as <see cref="Aqua"/>.
    /// </remarks>
    Cyan = Aqua,

    /// <summary>
    /// Dark blue RGB(0, 0, 139).
    /// </summary>
    DarkBlue = 0x00008B,

    /// <summary>
    /// Dark cyan RGB(0, 139, 139).
    /// </summary>
    DarkCyan = 0x008B8B,

    /// <summary>
    /// Dark goldenrod RGB(184, 134, 11).
    /// </summary>
    DarkGoldenrod = 0xB8860B,

    /// <summary>
    /// Dark gray RGB(169, 169, 169).
    /// </summary>
    DarkGray = 0xA9A9A9,

    /// <summary>
    /// Dark green RGB(0, 100, 0).
    /// </summary>
    DarkGreen = 0x006400,

    /// <summary>
    /// Dark grey RGB(169, 169, 169).
    /// </summary>
    /// <remarks>
    /// Same as <see cref="DarkGray"/>.
    /// </remarks>
    DarkGrey = DarkGray,

    /// <summary>
    /// Dark khaki RGB(189, 183, 107).
    /// </summary>
    DarkKhaki = 0xBDB76B,

    /// <summary>
    /// Dark magenta RGB(139, 0, 139).
    /// </summary>
    DarkMagenta = 0x8B008B,

    /// <summary>
    /// Dark olive green RGB(85, 107, 47).
    /// </summary>
    DarkOliveGreen = 0x556B2F,

    /// <summary>
    /// Dark orange RGB(255, 140, 0).
    /// </summary>
    DarkOrange = 0xFF8C00,

    /// <summary>
    /// Dark orchid RGB(153, 50, 204).
    /// </summary>
    DarkOrchid = 0x9932CC,

    /// <summary>
    /// Dark red RGB(139, 0, 0).
    /// </summary>
    DarkRed = 0x8B0000,

    /// <summary>
    /// Dark salmon RGB(233, 150, 122).
    /// </summary>
    DarkSalmon = 0xE9967A,

    /// <summary>
    /// Dark sea green RGB(143, 188, 143).
    /// </summary>
    DarkSeaGreen = 0x8FBC8F,

    /// <summary>
    /// Dark slate blue RGB(72, 61, 139).
    /// </summary>
    DarkSlateBlue = 0x483D8B,

    /// <summary>
    /// Dark slate gray RGB(47, 79, 79).
    /// </summary>
    DarkSlateGray = 0x2F4F4F,

    /// <summary>
    /// Dark slate grey RGB(47, 79, 79).
    /// </summary>
    /// <remarks>
    /// Same as <see cref="DarkSlateGray"/>.
    /// </remarks>
    DarkSlateGrey = DarkSlateGray,

    /// <summary>
    /// Dark turquoise RGB(0, 206, 209).
    /// </summary>
    DarkTurquoise = 0x00CED1,

    /// <summary>
    /// Dark violet RGB(148, 0, 211).
    /// </summary>
    DarkViolet = 0x9400D3,

    /// <summary>
    /// Deep pink RGB(255, 20, 147).
    /// </summary>
    DeepPink = 0xFF1493,

    /// <summary>
    /// Deep sky blue RGB(0, 191, 255).
    /// </summary>
    DeepSkyBlue = 0x00BFFF,

    /// <summary>
    /// Dim gray RGB(105, 105, 105).
    /// </summary>
    DimGray = 0x696969,

    /// <summary>
    /// Dim grey RGB(105, 105, 105).
    /// </summary>
    /// <remarks>
    /// Same as <see cref="DimGray"/>.
    /// </remarks>
    DimGrey = DimGray,

    /// <summary>
    /// Dodger blue RGB(30, 144, 255).
    /// </summary>
    DodgerBlue = 0x1E90FF,

    /// <summary>
    /// Fire brick RGB(178, 34, 34).
    /// </summary>
    FireBrick = 0xB22222,

    /// <summary>
    /// Floral white RGB(255, 250, 240).
    /// </summary>
    FloralWhite = 0xFFFAF0,

    /// <summary>
    /// Forest green RGB(34, 139, 34).
    /// </summary>
    ForestGreen = 0x228B22,

    /// <summary>
    /// Fuchsia RGB(255, 0, 255).
    /// </summary>
    /// <remarks>
    /// Same as <see cref="Magenta"/>.
    /// </remarks>
    Fuchsia = Magenta,

    /// <summary>
    /// Gainsboro RGB(220, 220, 220).
    /// </summary>
    Gainsboro = 0xDCDCDC,

    /// <summary>
    /// Ghost white RGB(248, 248, 255).
    /// </summary>
    GhostWhite = 0xF8F8FF,

    /// <summary>
    /// Gold RGB(255, 215, 0).
    /// </summary>
    Gold = 0xFFD700,

    /// <summary>
    /// Goldenrod RGB(218, 165, 32).
    /// </summary>
    Goldenrod = 0xDAA520,

    /// <summary>
    /// Gray RGB(128, 128, 128).
    /// </summary>
    Gray = 0x808080,

    /// <summary>
    /// Green RGB(0, 128, 0).
    /// </summary>
    Green = 0x008000,

    /// <summary>
    /// Green yellow RGB(173, 255, 47).
    /// </summary>
    GreenYellow = 0xADFF2F,

    /// <summary>
    /// Grey RGB(128, 128, 128).
    /// </summary>
    /// <remarks>
    /// Same as <see cref="Gray"/>.
    /// </remarks>
    Grey = Gray,

    /// <summary>
    /// Honey dew RGB(240, 255, 240).
    /// </summary>
    HoneyDew = 0xF0FFF0,

    /// <summary>
    /// Hot pink RGB(255, 105, 180).
    /// </summary>
    HotPink = 0xFF69B4,

    /// <summary>
    /// Indian red RGB(205, 92, 92).
    /// </summary>
    IndianRed = 0xCD5C5C,

    /// <summary>
    /// Indigo RGB(75, 0, 130).
    /// </summary>
    Indigo = 0x4B0082,

    /// <summary>
    /// Ivory RGB(255, 255, 240).
    /// </summary>
    Ivory = 0xFFFFF0,

    /// <summary>
    /// Khaki RGB(240, 230, 140).
    /// </summary>
    Khaki = 0xF0E68C,

    /// <summary>
    /// Lavender RGB(230, 230, 250).
    /// </summary>
    Lavender = 0xE6E6FA,

    /// <summary>
    /// Lavender blush RGB(255, 240, 245).
    /// </summary>
    LavenderBlush = 0xFFF0F5,

    /// <summary>
    /// Lawn green RGB(124, 252, 0).
    /// </summary>
    LawnGreen = 0x7CFC00,

    /// <summary>
    /// Lemon chiffon RGB(255, 250, 205).
    /// </summary>
    LemonChiffon = 0xFFFACD,

    /// <summary>
    /// Light blue RGB(173, 216, 230).
    /// </summary>
    LightBlue = 0xADD8E6,

    /// <summary>
    /// Light coral RGB(240, 128, 128).
    /// </summary>
    LightCoral = 0xF08080,

    /// <summary>
    /// Light cyan RGB(224, 255, 255).
    /// </summary>
    LightCyan = 0xE0FFFF,

    /// <summary>
    /// Light goldenrod yellow RGB(250, 250, 210).
    /// </summary>
    LightGoldenrodYellow = 0xFAFAD2,

    /// <summary>
    /// Light gray RGB(211, 211, 211).
    /// </summary>
    LightGray = 0xD3D3D3,

    /// <summary>
    /// Light green RGB(144, 238, 144).
    /// </summary>
    LightGreen = 0x90EE90,

    /// <summary>
    /// Light grey RGB(211, 211, 211).
    /// </summary>
    /// <remarks>
    /// Same as <see cref="LightGray"/>.
    /// </remarks>
    LightGrey = LightGray,

    /// <summary>
    /// Light pink RGB(255, 182, 193).
    /// </summary>
    LightPink = 0xFFB6C1,

    /// <summary>
    /// Light salmon RGB(255, 160, 122).
    /// </summary>
    LightSalmon = 0xFFA07A,

    /// <summary>
    /// Light sea green RGB(32, 178, 170).
    /// </summary>
    LightSeaGreen = 0x20B2AA,

    /// <summary>
    /// Light sky blue RGB(135, 206, 250).
    /// </summary>
    LightSkyBlue = 0x87CEFA,

    /// <summary>
    /// Light slate gray RGB(119, 136, 153).
    /// </summary>
    LightSlateGray = 0x778899,

    /// <summary>
    /// Light slate grey RGB(119, 136, 153).
    /// </summary>
    /// <remarks>
    /// Same as <see cref="LightSlateGray"/>.
    /// </remarks>
    LightSlateGrey = LightSlateGray,

    /// <summary>
    /// Light steel blue RGB(176, 196, 222).
    /// </summary>
    LightSteelBlue = 0xB0C4DE,

    /// <summary>
    /// Light yellow RGB(255, 255, 224).
    /// </summary>
    LightYellow = 0xFFFFE0,

    /// <summary>
    /// Lime RGB(0, 255, 0).
    /// </summary>
    Lime = 0x00FF00,

    /// <summary>
    /// Lime green RGB(50, 205, 50).
    /// </summary>
    LimeGreen = 0x32CD32,

    /// <summary>
    /// Linen RGB(250, 240, 230).
    /// </summary>
    Linen = 0xFAF0E6,

    /// <summary>
    /// Magenta RGB(255, 0, 255).
    /// </summary>
    Magenta = 0xFF00FF,

    /// <summary>
    /// Maroon RGB(128, 0, 0).
    /// </summary>
    Maroon = 0x800000,

    /// <summary>
    /// Medium aqua marine RGB(102, 205, 170).
    /// </summary>
    MediumAquaMarine = 0x66CDAA,

    /// <summary>
    /// Medium blue RGB(0, 0, 205).
    /// </summary>
    MediumBlue = 0x0000CD,

    /// <summary>
    /// Medium orchid RGB(186, 85, 211).
    /// </summary>
    MediumOrchid = 0xBA55D3,

    /// <summary>
    /// Medium purple RGB(147, 112, 219).
    /// </summary>
    MediumPurple = 0x9370DB,

    /// <summary>
    /// Medium sea green RGB(60, 179, 113).
    /// </summary>
    MediumSeaGreen = 0x3CB371,

    /// <summary>
    /// Medium slate blue RGB(123, 104, 238).
    /// </summary>
    MediumSlateBlue = 0x7B68EE,

    /// <summary>
    /// Medium spring green RGB(0, 250, 154).
    /// </summary>
    MediumSpringGreen = 0x00FA9A,

    /// <summary>
    /// Medium turquoise RGB(72, 209, 204).
    /// </summary>
    MediumTurquoise = 0x48D1CC,

    /// <summary>
    /// Medium violet red RGB(199, 21, 133).
    /// </summary>
    MediumVioletRed = 0xC71585,

    /// <summary>
    /// Midnight blue RGB(25, 25, 112).
    /// </summary>
    MidnightBlue = 0x191970,

    /// <summary>
    /// Mint cream RGB(245, 255, 250).
    /// </summary>
    MintCream = 0xF5FFFA,

    /// <summary>
    /// Misty rose RGB(255, 228, 225).
    /// </summary>
    MistyRose = 0xFFE4E1,

    /// <summary>
    /// Moccasin RGB(255, 228, 181).
    /// </summary>
    Moccasin = 0xFFE4B5,

    /// <summary>
    /// Navajo white RGB(255, 222, 173).
    /// </summary>
    NavajoWhite = 0xFFDEAD,

    /// <summary>
    /// Navy RGB(0, 0, 128).
    /// </summary>
    Navy = 0x000080,

    /// <summary>
    /// Old lace RGB(253, 245, 230).
    /// </summary>
    OldLace = 0xFDF5E6,

    /// <summary>
    /// Olive RGB(128, 128, 0).
    /// </summary>
    Olive = 0x808000,

    /// <summary>
    /// Olive drab RGB(107, 142, 35).
    /// </summary>
    OliveDrab = 0x6B8E23,

    /// <summary>
    /// Orange RGB(255, 165, 0).
    /// </summary>
    Orange = 0xFFA500,

    /// <summary>
    /// Orange red RGB(255, 69, 0).
    /// </summary>
    OrangeRed = 0xFF4500,

    /// <summary>
    /// Orchid RGB(218, 112, 214).
    /// </summary>
    Orchid = 0xDA70D6,

    /// <summary>
    /// Pale goldenrod RGB(238, 232, 170).
    /// </summary>
    PaleGoldenrod = 0xEEE8AA,

    /// <summary>
    /// Pale green RGB(152, 251, 152).
    /// </summary>
    PaleGreen = 0x98FB98,

    /// <summary>
    /// Pale turquoise RGB(175, 238, 238).
    /// </summary>
    PaleTurquoise = 0xAFEEEE,

    /// <summary>
    /// Pale violet red RGB(219, 112, 147).
    /// </summary>
    PaleVioletRed = 0xDB7093,

    /// <summary>
    /// Papaya whip RGB(255, 239, 213).
    /// </summary>
    PapayaWhip = 0xFFEFD5,

    /// <summary>
    /// Peach puff RGB(255, 218, 185).
    /// </summary>
    PeachPuff = 0xFFDAB9,

    /// <summary>
    /// Peru RGB(205, 133, 63).
    /// </summary>
    Peru = 0xCD853F,

    /// <summary>
    /// Pink RGB(255, 192, 203).
    /// </summary>
    Pink = 0xFFC0CB,

    /// <summary>
    /// Plum RGB(221, 160, 221).
    /// </summary>
    Plum = 0xDDA0DD,

    /// <summary>
    /// Powder blue RGB(176, 224, 230).
    /// </summary>
    PowderBlue = 0xB0E0E6,

    /// <summary>
    /// Purple RGB(128, 0, 128).
    /// </summary>
    Purple = 0x800080,

    /// <summary>
    /// Rebecca purple RGB(102, 51, 153).
    /// </summary>
    RebeccaPurple = 0x663399,

    /// <summary>
    /// Red RGB(255, 0, 0).
    /// </summary>
    Red = 0xFF0000,

    /// <summary>
    /// Rosy brown RGB(188, 143, 143).
    /// </summary>
    RosyBrown = 0xBC8F8F,

    /// <summary>
    /// Royal blue RGB(65, 105, 225).
    /// </summary>
    RoyalBlue = 0x4169E1,

    /// <summary>
    /// Saddle brown RGB(139, 69, 19).
    /// </summary>
    SaddleBrown = 0x8B4513,

    /// <summary>
    /// Salmon RGB(250, 128, 114).
    /// </summary>
    Salmon = 0xFA8072,

    /// <summary>
    /// Sandy brown RGB(244, 164, 96).
    /// </summary>
    SandyBrown = 0xF4A460,

    /// <summary>
    /// Sea green RGB(46, 139, 87).
    /// </summary>
    SeaGreen = 0x2E8B57,

    /// <summary>
    /// Sea shell RGB(255, 245, 238).
    /// </summary>
    SeaShell = 0xFFF5EE,

    /// <summary>
    /// Sienna RGB(160, 82, 45).
    /// </summary>
    Sienna = 0xA0522D,

    /// <summary>
    /// Silver RGB(192, 192, 192).
    /// </summary>
    Silver = 0xC0C0C0,

    /// <summary>
    /// Sky blue RGB(135, 206, 235).
    /// </summary>
    SkyBlue = 0x87CEEB,

    /// <summary>
    /// Slate blue RGB(106, 90, 205).
    /// </summary>
    SlateBlue = 0x6A5ACD,

    /// <summary>
    /// Slate gray RGB(112, 128, 144).
    /// </summary>
    SlateGray = 0x708090,

    /// <summary>
    /// Slate grey RGB(112, 128, 144).
    /// </summary>
    /// <remarks>
    /// Same as <see cref="SlateGray"/>.
    /// </remarks>
    SlateGrey = SlateGray,

    /// <summary>
    /// Snow RGB(255, 250, 250).
    /// </summary>
    Snow = 0xFFFAFA,

    /// <summary>
    /// Spring green RGB(0, 255, 127).
    /// </summary>
    SpringGreen = 0x00FF7F,

    /// <summary>
    /// Steel blue RGB(70, 130, 180).
    /// </summary>
    SteelBlue = 0x4682B4,

    /// <summary>
    /// Tan RGB(210, 180, 140).
    /// </summary>
    Tan = 0xD2B48C,

    /// <summary>
    /// Teal RGB(0, 128, 128).
    /// </summary>
    Teal = 0x008080,

    /// <summary>
    /// Thistle RGB(216, 191, 216).
    /// </summary>
    Thistle = 0xD8BFD8,

    /// <summary>
    /// Tomato RGB(255, 99, 71).
    /// </summary>
    Tomato = 0xFF6347,

    /// <summary>
    /// Turquoise RGB(64, 224, 208).
    /// </summary>
    Turquoise = 0x40E0D0,

    /// <summary>
    /// Violet RGB(238, 130, 238).
    /// </summary>
    Violet = 0xEE82EE,

    /// <summary>
    /// Wheat RGB(245, 222, 179).
    /// </summary>
    Wheat = 0xF5DEB3,

    /// <summary>
    /// White RGB(255, 255, 255).
    /// </summary>
    White = 0xFFFFFF,

    /// <summary>
    /// White smoke RGB(245, 245, 245).
    /// </summary>
    WhiteSmoke = 0xF5F5F5,

    /// <summary>
    /// Yellow RGB(255, 255, 0).
    /// </summary>
    Yellow = 0xFFFF00,

    /// <summary>
    /// Yellow green RGB(154, 205, 50).
    /// </summary>
    YellowGreen = 0x9ACD32
}
