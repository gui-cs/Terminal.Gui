
namespace Terminal.Gui.Drawing;

/// <summary>Defines the standard set of glyphs used to draw checkboxes, lines, borders, etc...</summary>
/// <remarks>
///     <para>
///         Access with <see cref="Glyphs"/> (which is a global using alias for
///         <see cref="Glyphs"/>).
///     </para>
///     <para>
///         The default glyphs can be changed per-<see cref="ThemeScope"/> in <see cref="ConfigurationManager"/>. Within a <c>config.json</c>
///         file the Json property name is the property name prefixed with "Glyphs.".
///     </para>
///     <para>
///         The Json property can be one of:
///         - unicode glyph in a string (e.g. "â˜‘")
///         - U+hex format in a string  (e.g. "U+2611")
///         - \u format in a string (e.g. "\\u2611")
///         - A decimal number (e.g. 97 for "a")
///     </para>
/// </remarks>
public class Glyphs
{
    // IMPORTANT: If you change these, make sure to update the ./Resources/config.json file as
    // IMPORTANT: it is the source of truth for the default glyphs at runtime.
    // IMPORTANT: Configuration Manager test SaveDefaults uses this class to generate the default config file
    // IMPORTANT: in ./UnitTests/bin/Debug/netX.0/config.json

    /// <summary>Unicode replacement character; used by Drivers when rendering in cases where a wide glyph can't
    /// be output because it would be clipped. Defaults to ' ' (Space).</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune WideGlyphReplacement
    {
        get => GlyphSettings.Defaults.WideGlyphReplacement;
        set => GlyphSettings.Defaults.WideGlyphReplacement = value;
    }

    /// <summary>File icon.  Defaults to â˜° (Trigram For Heaven)</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune File
    {
        get => GlyphSettings.Defaults.File;
        set => GlyphSettings.Defaults.File = value;
    }

    /// <summary>Folder icon.  Defaults to ê¤‰ (Kayah Li Digit Nine)</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Folder
    {
        get => GlyphSettings.Defaults.Folder;
        set => GlyphSettings.Defaults.Folder = value;
    }

    /// <summary>Horizontal Ellipsis - â€¦ U+2026</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HorizontalEllipsis
    {
        get => GlyphSettings.Defaults.HorizontalEllipsis;
        set => GlyphSettings.Defaults.HorizontalEllipsis = value;
    }

    /// <summary>Vertical Four Dots - âž U+205e</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VerticalFourDots
    {
        get => GlyphSettings.Defaults.VerticalFourDots;
        set => GlyphSettings.Defaults.VerticalFourDots = value;
    }

    #region ----------------- Single Glyphs -----------------

    /// <summary>Null symbol ('â€')</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Null
    {
        get => GlyphSettings.Defaults.Null;
        set => GlyphSettings.Defaults.Null = value;
    }

    /// <summary>Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CheckStateChecked // 'â˜‘' is colored
    {
        get => GlyphSettings.Defaults.CheckStateChecked;
        set => GlyphSettings.Defaults.CheckStateChecked = value;
    }

    /// <summary>Not Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CheckStateUnChecked
    {
        get => GlyphSettings.Defaults.CheckStateUnChecked;
        set => GlyphSettings.Defaults.CheckStateUnChecked = value;
    }

    /// <summary>Null Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CheckStateNone // TODO: Verify this works as broadly as possible
    {
        get => GlyphSettings.Defaults.CheckStateNone;
        set => GlyphSettings.Defaults.CheckStateNone = value;
    }

    /// <summary>Selected indicator  (e.g. for <see cref="ListView"/> and <see cref="OptionSelector"/>).</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Selected
    {
        get => GlyphSettings.Defaults.Selected;
        set => GlyphSettings.Defaults.Selected = value;
    }

    /// <summary>Not Selected indicator (e.g. for <see cref="ListView"/> and <see cref="OptionSelector"/>).</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune UnSelected
    {
        get => GlyphSettings.Defaults.UnSelected;
        set => GlyphSettings.Defaults.UnSelected = value;
    }

    /// <summary>Horizontal arrow.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightArrow
    {
        get => GlyphSettings.Defaults.RightArrow;
        set => GlyphSettings.Defaults.RightArrow = value;
    }

    /// <summary>Left arrow.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftArrow
    {
        get => GlyphSettings.Defaults.LeftArrow;
        set => GlyphSettings.Defaults.LeftArrow = value;
    }

    /// <summary>Down arrow.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune DownArrow
    {
        get => GlyphSettings.Defaults.DownArrow;
        set => GlyphSettings.Defaults.DownArrow = value;
    }

    /// <summary>Vertical arrow.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune UpArrow
    {
        get => GlyphSettings.Defaults.UpArrow;
        set => GlyphSettings.Defaults.UpArrow = value;
    }

    /// <summary>Left default indicator (e.g. for <see cref="Button"/>.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftDefaultIndicator
    {
        get => GlyphSettings.Defaults.LeftDefaultIndicator;
        set => GlyphSettings.Defaults.LeftDefaultIndicator = value;
    }

    /// <summary>Horizontal default indicator (e.g. for <see cref="Button"/>.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightDefaultIndicator
    {
        get => GlyphSettings.Defaults.RightDefaultIndicator;
        set => GlyphSettings.Defaults.RightDefaultIndicator = value;
    }

    /// <summary>Left Bracket (e.g. for <see cref="Button"/>. Default is (U+005B) - [.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftBracket
    {
        get => GlyphSettings.Defaults.LeftBracket;
        set => GlyphSettings.Defaults.LeftBracket = value;
    }

    /// <summary>Horizontal Bracket (e.g. for <see cref="Button"/>. Default is (U+005D) - ].</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightBracket
    {
        get => GlyphSettings.Defaults.RightBracket;
        set => GlyphSettings.Defaults.RightBracket = value;
    }

    /// <summary>Half block meter segment (e.g. for <see cref="ProgressBar"/>).</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BlocksMeterSegment
    {
        get => GlyphSettings.Defaults.BlocksMeterSegment;
        set => GlyphSettings.Defaults.BlocksMeterSegment = value;
    }

    /// <summary>Continuous block meter segment (e.g. for <see cref="ProgressBar"/>).</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ContinuousMeterSegment
    {
        get => GlyphSettings.Defaults.ContinuousMeterSegment;
        set => GlyphSettings.Defaults.ContinuousMeterSegment = value;
    }

    /// <summary>Stipple pattern (e.g. for <see cref="ScrollBar"/>). Default is Light Shade (U+2591) - â–‘.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Stipple
    {
        get => GlyphSettings.Defaults.Stipple;
        set => GlyphSettings.Defaults.Stipple = value;
    }

    /// <summary>Diamond. Default is Lozenge (U+25CA) - â—Š.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Diamond
    {
        get => GlyphSettings.Defaults.Diamond;
        set => GlyphSettings.Defaults.Diamond = value;
    }

    /// <summary>Close. Default is Heavy Ballot X (U+2718) - âœ˜.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Close
    {
        get => GlyphSettings.Defaults.Close;
        set => GlyphSettings.Defaults.Close = value;
    }

    /// <summary>Minimize. Default is Lower Horizontal Shadowed White Circle (U+274F) - â.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Minimize
    {
        get => GlyphSettings.Defaults.Minimize;
        set => GlyphSettings.Defaults.Minimize = value;
    }

    /// <summary>Maximize. Default is Upper Horizontal Shadowed White Circle (U+273D) - âœ½.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Maximize
    {
        get => GlyphSettings.Defaults.Maximize;
        set => GlyphSettings.Defaults.Maximize = value;
    }

    /// <summary>Dot. Default is (U+2219) - âˆ™.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Dot
    {
        get => GlyphSettings.Defaults.Dot;
        set => GlyphSettings.Defaults.Dot = value;
    }

    /// <summary>Dotted Square - â¬š U+02b1aâ”</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune DottedSquare
    {
        get => GlyphSettings.Defaults.DottedSquare;
        set => GlyphSettings.Defaults.DottedSquare = value;
    }

    /// <summary>Black Circle . Default is (U+025cf) - â—.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BlackCircle // Black Circle - â— U+025cf
    {
        get => GlyphSettings.Defaults.BlackCircle;
        set => GlyphSettings.Defaults.BlackCircle = value;
    }

    /// <summary>Expand (e.g. for <see cref="TreeView"/>.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Expand
    {
        get => GlyphSettings.Defaults.Expand;
        set => GlyphSettings.Defaults.Expand = value;
    }

    /// <summary>Expand (e.g. for <see cref="TreeView"/>.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Collapse
    {
        get => GlyphSettings.Defaults.Collapse;
        set => GlyphSettings.Defaults.Collapse = value;
    }

    /// <summary>Identical To (U+226)</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune IdenticalTo
    {
        get => GlyphSettings.Defaults.IdenticalTo;
        set => GlyphSettings.Defaults.IdenticalTo = value;
    }

    /// <summary>Move indicator. Default is Lozenge (U+25CA) - â—Š.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Move
    {
        get => GlyphSettings.Defaults.Move;
        set => GlyphSettings.Defaults.Move = value;
    }

    /// <summary>Size Horizontally indicator. Default is â”¥Left Right Arrow - â†” U+02194</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeHorizontal
    {
        get => GlyphSettings.Defaults.SizeHorizontal;
        set => GlyphSettings.Defaults.SizeHorizontal = value;
    }

    /// <summary>Size Vertical indicator. Default Up Down Arrow - â†• U+02195</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeVertical
    {
        get => GlyphSettings.Defaults.SizeVertical;
        set => GlyphSettings.Defaults.SizeVertical = value;
    }

    /// <summary>Size Top Left indicator. North West Arrow - â†– U+02196</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeTopLeft
    {
        get => GlyphSettings.Defaults.SizeTopLeft;
        set => GlyphSettings.Defaults.SizeTopLeft = value;
    }

    /// <summary>Size Top Right indicator. North East Arrow - â†— U+02197</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeTopRight
    {
        get => GlyphSettings.Defaults.SizeTopRight;
        set => GlyphSettings.Defaults.SizeTopRight = value;
    }

    /// <summary>Size Bottom Right indicator. South East Arrow - â†˜ U+02198</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeBottomRight
    {
        get => GlyphSettings.Defaults.SizeBottomRight;
        set => GlyphSettings.Defaults.SizeBottomRight = value;
    }

    /// <summary>Size Bottom Left indicator. South West Arrow - â†™ U+02199</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeBottomLeft
    {
        get => GlyphSettings.Defaults.SizeBottomLeft;
        set => GlyphSettings.Defaults.SizeBottomLeft = value;
    }

    /// <summary>Apple (non-BMP). Because snek. And because it's an example of a non-BMP surrogate pair. See Issue #2610.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Apple // nonBMP
    {
        get => GlyphSettings.Defaults.Apple;
        set => GlyphSettings.Defaults.Apple = value;
    }

    /// <summary>Apple (BMP). Because snek. See Issue #2610.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune AppleBMP
    {
        get => GlyphSettings.Defaults.AppleBMP;
        set => GlyphSettings.Defaults.AppleBMP = value;
    }

    /// <summary>Copy indicator. Two Joined Squares - â§‰ U+29C9. Used for code block copy buttons.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Copy
    {
        get => GlyphSettings.Defaults.Copy;
        set => GlyphSettings.Defaults.Copy = value;
    }

    #endregion

    #region ----------------- Lines -----------------

    /// <summary>Box Drawings Horizontal Line - Light (U+2500) - â”€</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLine
    {
        get => GlyphSettings.Defaults.HLine;
        set => GlyphSettings.Defaults.HLine = value;
    }

    /// <summary>Box Drawings Vertical Line - Light (U+2502) - â”‚</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLine
    {
        get => GlyphSettings.Defaults.VLine;
        set => GlyphSettings.Defaults.VLine = value;
    }

    /// <summary>Box Drawings Double Horizontal (U+2550) - â•</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLineDbl
    {
        get => GlyphSettings.Defaults.HLineDbl;
        set => GlyphSettings.Defaults.HLineDbl = value;
    }

    /// <summary>Box Drawings Double Vertical (U+2551) - â•‘</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLineDbl

    {

        get => GlyphSettings.Defaults.VLineDbl;

        set => GlyphSettings.Defaults.VLineDbl = value;

    }

    /// <summary>Box Drawings Heavy Double Dash Horizontal (U+254D) - â•</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLineHvDa2

    {

        get => GlyphSettings.Defaults.HLineHvDa2;

        set => GlyphSettings.Defaults.HLineHvDa2 = value;

    }

    /// <summary>Box Drawings Heavy Triple Dash Vertical (U+2507) - â”‡</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLineHvDa3
    {
        get => GlyphSettings.Defaults.VLineHvDa3;
        set => GlyphSettings.Defaults.VLineHvDa3 = value;
    }

    /// <summary>Box Drawings Heavy Triple Dash Horizontal (U+2505) - â”…</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLineHvDa3

    {

        get => GlyphSettings.Defaults.HLineHvDa3;

        set => GlyphSettings.Defaults.HLineHvDa3 = value;

    }

    /// <summary>Box Drawings Heavy Quadruple Dash Horizontal (U+2509) - â”‰</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLineHvDa4

    {

        get => GlyphSettings.Defaults.HLineHvDa4;

        set => GlyphSettings.Defaults.HLineHvDa4 = value;

    }

    /// <summary>Box Drawings Heavy Double Dash Vertical (U+254F) - â•</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLineHvDa2

    {

        get => GlyphSettings.Defaults.VLineHvDa2;

        set => GlyphSettings.Defaults.VLineHvDa2 = value;

    }

    /// <summary>Box Drawings Heavy Quadruple Dash Vertical (U+250B) - â”‹</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLineHvDa4

    {

        get => GlyphSettings.Defaults.VLineHvDa4;

        set => GlyphSettings.Defaults.VLineHvDa4 = value;

    }

    /// <summary>Box Drawings Light Double Dash Horizontal (U+254C) - â•Œ</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLineDa2

    {

        get => GlyphSettings.Defaults.HLineDa2;

        set => GlyphSettings.Defaults.HLineDa2 = value;

    }

    /// <summary>Box Drawings Light Triple Dash Vertical (U+2506) - â”†</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLineDa3

    {

        get => GlyphSettings.Defaults.VLineDa3;

        set => GlyphSettings.Defaults.VLineDa3 = value;

    }

    /// <summary>Box Drawings Light Triple Dash Horizontal (U+2504) - â”„</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLineDa3

    {

        get => GlyphSettings.Defaults.HLineDa3;

        set => GlyphSettings.Defaults.HLineDa3 = value;

    }

    /// <summary>Box Drawings Light Quadruple Dash Horizontal (U+2508) - â”ˆ</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLineDa4

    {

        get => GlyphSettings.Defaults.HLineDa4;

        set => GlyphSettings.Defaults.HLineDa4 = value;

    }

    /// <summary>Box Drawings Light Double Dash Vertical (U+254E) - â•Ž</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLineDa2

    {

        get => GlyphSettings.Defaults.VLineDa2;

        set => GlyphSettings.Defaults.VLineDa2 = value;

    }

    /// <summary>Box Drawings Light Quadruple Dash Vertical (U+250A) - â”Š</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLineDa4

    {

        get => GlyphSettings.Defaults.VLineDa4;

        set => GlyphSettings.Defaults.VLineDa4 = value;

    }

    /// <summary>Box Drawings Heavy Horizontal (U+2501) - â”</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLineHv

    {

        get => GlyphSettings.Defaults.HLineHv;

        set => GlyphSettings.Defaults.HLineHv = value;

    }

    /// <summary>Box Drawings Heavy Vertical (U+2503) - â”ƒ</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLineHv

    {

        get => GlyphSettings.Defaults.VLineHv;

        set => GlyphSettings.Defaults.VLineHv = value;

    }

    /// <summary>Box Drawings Light Left (U+2574) - â•´</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HalfLeftLine

    {

        get => GlyphSettings.Defaults.HalfLeftLine;

        set => GlyphSettings.Defaults.HalfLeftLine = value;

    }

    /// <summary>Box Drawings Light Vertical (U+2575) - â•µ</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HalfTopLine

    {

        get => GlyphSettings.Defaults.HalfTopLine;

        set => GlyphSettings.Defaults.HalfTopLine = value;

    }

    /// <summary>Box Drawings Light Horizontal (U+2576) - â•¶</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HalfRightLine

    {

        get => GlyphSettings.Defaults.HalfRightLine;

        set => GlyphSettings.Defaults.HalfRightLine = value;

    }

    /// <summary>Box Drawings Light Down (U+2577) - â•·</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HalfBottomLine

    {

        get => GlyphSettings.Defaults.HalfBottomLine;

        set => GlyphSettings.Defaults.HalfBottomLine = value;

    }

    /// <summary>Box Drawings Heavy Left (U+2578) - â•¸</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HalfLeftLineHv

    {

        get => GlyphSettings.Defaults.HalfLeftLineHv;

        set => GlyphSettings.Defaults.HalfLeftLineHv = value;

    }

    /// <summary>Box Drawings Heavy Vertical (U+2579) - â•¹</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HalfTopLineHv

    {

        get => GlyphSettings.Defaults.HalfTopLineHv;

        set => GlyphSettings.Defaults.HalfTopLineHv = value;

    }

    /// <summary>Box Drawings Heavy Horizontal (U+257A) - â•º</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HalfRightLineHv

    {

        get => GlyphSettings.Defaults.HalfRightLineHv;

        set => GlyphSettings.Defaults.HalfRightLineHv = value;

    }

    /// <summary>Box Drawings Light Vertical and Horizontal (U+257B) - â•»</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HalfBottomLineLt

    {

        get => GlyphSettings.Defaults.HalfBottomLineLt;

        set => GlyphSettings.Defaults.HalfBottomLineLt = value;

    }

    /// <summary>Box Drawings Light Horizontal and Heavy Horizontal (U+257C) - â•¼</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightSideLineLtHv

    {

        get => GlyphSettings.Defaults.RightSideLineLtHv;

        set => GlyphSettings.Defaults.RightSideLineLtHv = value;

    }

    /// <summary>Box Drawings Light Vertical and Heavy Horizontal (U+257D) - â•½</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BottomSideLineLtHv

    {

        get => GlyphSettings.Defaults.BottomSideLineLtHv;

        set => GlyphSettings.Defaults.BottomSideLineLtHv = value;

    }

    /// <summary>Box Drawings Heavy Left and Light Horizontal (U+257E) - â•¾</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftSideLineHvLt

    {

        get => GlyphSettings.Defaults.LeftSideLineHvLt;

        set => GlyphSettings.Defaults.LeftSideLineHvLt = value;

    }

    /// <summary>Box Drawings Heavy Vertical and Light Horizontal (U+257F) - â•¿</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune TopSideLineHvLt

    {

        get => GlyphSettings.Defaults.TopSideLineHvLt;

        set => GlyphSettings.Defaults.TopSideLineHvLt = value;

    }

    #endregion

    #region ----------------- Upper Left Corners -----------------

    /// <summary>Box Drawings Upper Left Corner - Light Vertical and Light Horizontal (U+250C) - â”Œ</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ULCorner

    {

        get => GlyphSettings.Defaults.ULCorner;

        set => GlyphSettings.Defaults.ULCorner = value;

    }

    /// <summary>Box Drawings Upper Left Corner -  Double (U+2554) - â•”</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ULCornerDbl

    {

        get => GlyphSettings.Defaults.ULCornerDbl;

        set => GlyphSettings.Defaults.ULCornerDbl = value;

    }

    /// <summary>Box Drawings Upper Left Corner - Light Arc Down and Horizontal (U+256D) - â•­</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ULCornerR

    {

        get => GlyphSettings.Defaults.ULCornerR;

        set => GlyphSettings.Defaults.ULCornerR = value;

    }

    /// <summary>Box Drawings Heavy Down and Horizontal (U+250F) - â”</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ULCornerHv

    {

        get => GlyphSettings.Defaults.ULCornerHv;

        set => GlyphSettings.Defaults.ULCornerHv = value;

    }

    /// <summary>Box Drawings Down Heavy and Horizontal Light (U+251E) - â”Ž</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ULCornerHvLt

    {

        get => GlyphSettings.Defaults.ULCornerHvLt;

        set => GlyphSettings.Defaults.ULCornerHvLt = value;

    }

    /// <summary>Box Drawings Down Light and Horizontal Heavy (U+250D) - â”Ž</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ULCornerLtHv

    {

        get => GlyphSettings.Defaults.ULCornerLtHv;

        set => GlyphSettings.Defaults.ULCornerLtHv = value;

    }

    /// <summary>Box Drawings Double Down and Single Horizontal (U+2553) - â•“</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ULCornerDblSingle

    {

        get => GlyphSettings.Defaults.ULCornerDblSingle;

        set => GlyphSettings.Defaults.ULCornerDblSingle = value;

    }

    /// <summary>Box Drawings Single Down and Double Horizontal (U+2552) - â•’</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ULCornerSingleDbl

    {

        get => GlyphSettings.Defaults.ULCornerSingleDbl;

        set => GlyphSettings.Defaults.ULCornerSingleDbl = value;

    }

    #endregion

    #region ----------------- Lower Left Corners -----------------

    /// <summary>Box Drawings Lower Left Corner - Light Vertical and Light Horizontal (U+2514) - â””</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LLCorner

    {

        get => GlyphSettings.Defaults.LLCorner;

        set => GlyphSettings.Defaults.LLCorner = value;

    }

    /// <summary>Box Drawings Heavy Vertical and Horizontal (U+2517) - â”—</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LLCornerHv

    {

        get => GlyphSettings.Defaults.LLCornerHv;

        set => GlyphSettings.Defaults.LLCornerHv = value;

    }

    /// <summary>Box Drawings Heavy Vertical and Horizontal Light (U+2516) - â”–</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LLCornerHvLt

    {

        get => GlyphSettings.Defaults.LLCornerHvLt;

        set => GlyphSettings.Defaults.LLCornerHvLt = value;

    }

    /// <summary>Box Drawings Vertical Light and Horizontal Heavy (U+2511) - â”•</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LLCornerLtHv

    {

        get => GlyphSettings.Defaults.LLCornerLtHv;

        set => GlyphSettings.Defaults.LLCornerLtHv = value;

    }

    /// <summary>Box Drawings Double Vertical and Double Left (U+255A) - â•š</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LLCornerDbl

    {

        get => GlyphSettings.Defaults.LLCornerDbl;

        set => GlyphSettings.Defaults.LLCornerDbl = value;

    }

    /// <summary>Box Drawings Single Vertical and Double Left (U+2558) - â•˜</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LLCornerSingleDbl

    {

        get => GlyphSettings.Defaults.LLCornerSingleDbl;

        set => GlyphSettings.Defaults.LLCornerSingleDbl = value;

    }

    /// <summary>Box Drawings Double Down and Single Left (U+2559) - â•™</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LLCornerDblSingle

    {

        get => GlyphSettings.Defaults.LLCornerDblSingle;

        set => GlyphSettings.Defaults.LLCornerDblSingle = value;

    }

    /// <summary>Box Drawings Upper Left Corner - Light Arc Down and Left (U+2570) - â•°</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LLCornerR

    {

        get => GlyphSettings.Defaults.LLCornerR;

        set => GlyphSettings.Defaults.LLCornerR = value;

    }

    #endregion

    #region ----------------- Upper Right Corners -----------------

    /// <summary>Box Drawings Upper Horizontal Corner - Light Vertical and Light Horizontal (U+2510) - â”</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune URCorner

    {

        get => GlyphSettings.Defaults.URCorner;

        set => GlyphSettings.Defaults.URCorner = value;

    }

    /// <summary>Box Drawings Upper Horizontal Corner - Double Vertical and Double Horizontal (U+2557) - â•—</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune URCornerDbl

    {

        get => GlyphSettings.Defaults.URCornerDbl;

        set => GlyphSettings.Defaults.URCornerDbl = value;

    }

    /// <summary>Box Drawings Upper Horizontal Corner - Light Arc Vertical and Horizontal (U+256E) - â•®</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune URCornerR

    {

        get => GlyphSettings.Defaults.URCornerR;

        set => GlyphSettings.Defaults.URCornerR = value;

    }

    /// <summary>Box Drawings Heavy Down and Left (U+2513) - â”“</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune URCornerHv

    {

        get => GlyphSettings.Defaults.URCornerHv;

        set => GlyphSettings.Defaults.URCornerHv = value;

    }

    /// <summary>Box Drawings Heavy Vertical and Left Down Light (U+2511) - â”‘</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune URCornerHvLt

    {

        get => GlyphSettings.Defaults.URCornerHvLt;

        set => GlyphSettings.Defaults.URCornerHvLt = value;

    }

    /// <summary>Box Drawings Down Light and Horizontal Heavy (U+2514) - â”’</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune URCornerLtHv

    {

        get => GlyphSettings.Defaults.URCornerLtHv;

        set => GlyphSettings.Defaults.URCornerLtHv = value;

    }

    /// <summary>Box Drawings Double Vertical and Single Left (U+2556) - â•–</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune URCornerDblSingle

    {

        get => GlyphSettings.Defaults.URCornerDblSingle;

        set => GlyphSettings.Defaults.URCornerDblSingle = value;

    }

    /// <summary>Box Drawings Single Vertical and Double Left (U+2555) - â••</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune URCornerSingleDbl

    {

        get => GlyphSettings.Defaults.URCornerSingleDbl;

        set => GlyphSettings.Defaults.URCornerSingleDbl = value;

    }

    #endregion

    #region ----------------- Lower Right Corners -----------------

    /// <summary>Box Drawings Lower Right Corner - Light (U+2518) - â”˜</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LRCorner

    {

        get => GlyphSettings.Defaults.LRCorner;

        set => GlyphSettings.Defaults.LRCorner = value;

    }

    /// <summary>Box Drawings Lower Right Corner - Double (U+255D) - â•</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LRCornerDbl

    {

        get => GlyphSettings.Defaults.LRCornerDbl;

        set => GlyphSettings.Defaults.LRCornerDbl = value;

    }

    /// <summary>Box Drawings Lower Right Corner - Rounded (U+256F) - â•¯</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LRCornerR

    {

        get => GlyphSettings.Defaults.LRCornerR;

        set => GlyphSettings.Defaults.LRCornerR = value;

    }

    /// <summary>Box Drawings Lower Right Corner - Heavy (U+251B) - â”›</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LRCornerHv

    {

        get => GlyphSettings.Defaults.LRCornerHv;

        set => GlyphSettings.Defaults.LRCornerHv = value;

    }

    /// <summary>Box Drawings Lower Right Corner - Double Vertical and Single Horizontal (U+255C) - â•œ</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LRCornerDblSingle

    {

        get => GlyphSettings.Defaults.LRCornerDblSingle;

        set => GlyphSettings.Defaults.LRCornerDblSingle = value;

    }

    /// <summary>Box Drawings Lower Right Corner - Single Vertical and Double Horizontal (U+255B) - â•›</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LRCornerSingleDbl

    {

        get => GlyphSettings.Defaults.LRCornerSingleDbl;

        set => GlyphSettings.Defaults.LRCornerSingleDbl = value;

    }

    /// <summary>Box Drawings Lower Right Corner - Light Vertical and Heavy Horizontal (U+2519) - â”™</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LRCornerLtHv

    {

        get => GlyphSettings.Defaults.LRCornerLtHv;

        set => GlyphSettings.Defaults.LRCornerLtHv = value;

    }

    /// <summary>Box Drawings Lower Right Corner - Heavy Vertical and Light Horizontal (U+251A) - â”š</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LRCornerHvLt

    {

        get => GlyphSettings.Defaults.LRCornerHvLt;

        set => GlyphSettings.Defaults.LRCornerHvLt = value;

    }

    #endregion

    #region ----------------- Tees -----------------

    /// <summary>Box Drawings Left Tee - Single Vertical and Single Horizontal (U+251C) - â”œ</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftTee

    {

        get => GlyphSettings.Defaults.LeftTee;

        set => GlyphSettings.Defaults.LeftTee = value;

    }

    /// <summary>Box Drawings Left Tee - Single Vertical and Double Horizontal (U+255E) - â•ž</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftTeeDblH

    {

        get => GlyphSettings.Defaults.LeftTeeDblH;

        set => GlyphSettings.Defaults.LeftTeeDblH = value;

    }

    /// <summary>Box Drawings Left Tee - Double Vertical and Single Horizontal (U+255F) - â•Ÿ</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftTeeDblV

    {

        get => GlyphSettings.Defaults.LeftTeeDblV;

        set => GlyphSettings.Defaults.LeftTeeDblV = value;

    }

    /// <summary>Box Drawings Left Tee - Double Vertical and Double Horizontal (U+2560) - â• </summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftTeeDbl

    {

        get => GlyphSettings.Defaults.LeftTeeDbl;

        set => GlyphSettings.Defaults.LeftTeeDbl = value;

    }

    /// <summary>Box Drawings Left Tee - Heavy Horizontal and Light Vertical (U+2523) - â”</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftTeeHvH

    {

        get => GlyphSettings.Defaults.LeftTeeHvH;

        set => GlyphSettings.Defaults.LeftTeeHvH = value;

    }

    /// <summary>Box Drawings Left Tee - Light Horizontal and Heavy Vertical (U+252B) - â” </summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftTeeHvV

    {

        get => GlyphSettings.Defaults.LeftTeeHvV;

        set => GlyphSettings.Defaults.LeftTeeHvV = value;

    }

    /// <summary>Box Drawings Left Tee - Heavy Vertical and Heavy Horizontal (U+2527) - â”£</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftTeeHvDblH

    {

        get => GlyphSettings.Defaults.LeftTeeHvDblH;

        set => GlyphSettings.Defaults.LeftTeeHvDblH = value;

    }

    /// <summary>Box Drawings Right Tee - Single Vertical and Single Horizontal (U+2524) - â”¤</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightTee

    {

        get => GlyphSettings.Defaults.RightTee;

        set => GlyphSettings.Defaults.RightTee = value;

    }

    /// <summary>Box Drawings Right Tee - Single Vertical and Double Horizontal (U+2561) - â•¡</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightTeeDblH

    {

        get => GlyphSettings.Defaults.RightTeeDblH;

        set => GlyphSettings.Defaults.RightTeeDblH = value;

    }

    /// <summary>Box Drawings Right Tee - Double Vertical and Single Horizontal (U+2562) - â•¢</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightTeeDblV

    {

        get => GlyphSettings.Defaults.RightTeeDblV;

        set => GlyphSettings.Defaults.RightTeeDblV = value;

    }

    /// <summary>Box Drawings Right Tee - Double Vertical and Double Horizontal (U+2563) - â•£</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightTeeDbl

    {

        get => GlyphSettings.Defaults.RightTeeDbl;

        set => GlyphSettings.Defaults.RightTeeDbl = value;

    }

    /// <summary>Box Drawings Right Tee - Heavy Horizontal and Light Vertical (U+2528) - â”¥</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightTeeHvH

    {

        get => GlyphSettings.Defaults.RightTeeHvH;

        set => GlyphSettings.Defaults.RightTeeHvH = value;

    }

    /// <summary>Box Drawings Right Tee - Light Horizontal and Heavy Vertical (U+2530) - â”¨</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightTeeHvV

    {

        get => GlyphSettings.Defaults.RightTeeHvV;

        set => GlyphSettings.Defaults.RightTeeHvV = value;

    }

    /// <summary>Box Drawings Right Tee - Heavy Vertical and Heavy Horizontal (U+252C) - â”«</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightTeeHvDblH

    {

        get => GlyphSettings.Defaults.RightTeeHvDblH;

        set => GlyphSettings.Defaults.RightTeeHvDblH = value;

    }

    /// <summary>Box Drawings Top Tee - Single Vertical and Single Horizontal (U+252C) - â”¬</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune TopTee

    {

        get => GlyphSettings.Defaults.TopTee;

        set => GlyphSettings.Defaults.TopTee = value;

    }

    /// <summary>Box Drawings Top Tee - Single Vertical and Double Horizontal (U+2564) - â•¤</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune TopTeeDblH

    {

        get => GlyphSettings.Defaults.TopTeeDblH;

        set => GlyphSettings.Defaults.TopTeeDblH = value;

    }

    /// <summary>Box Drawings Top Tee - Double Vertical and Single Horizontal  (U+2565) - â•¥</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune TopTeeDblV

    {

        get => GlyphSettings.Defaults.TopTeeDblV;

        set => GlyphSettings.Defaults.TopTeeDblV = value;

    }

    /// <summary>Box Drawings Top Tee - Double Vertical and Double Horizontal (U+2566) - â•¦</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune TopTeeDbl

    {

        get => GlyphSettings.Defaults.TopTeeDbl;

        set => GlyphSettings.Defaults.TopTeeDbl = value;

    }

    /// <summary>Box Drawings Top Tee - Heavy Horizontal and Light Vertical (U+252F) - â”¯</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune TopTeeHvH

    {

        get => GlyphSettings.Defaults.TopTeeHvH;

        set => GlyphSettings.Defaults.TopTeeHvH = value;

    }

    /// <summary>Box Drawings Top Tee - Light Horizontal and Heavy Vertical (U+2537) - â”°</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune TopTeeHvV

    {

        get => GlyphSettings.Defaults.TopTeeHvV;

        set => GlyphSettings.Defaults.TopTeeHvV = value;

    }

    /// <summary>Box Drawings Top Tee - Heavy Vertical and Heavy Horizontal (U+2533) - â”³</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune TopTeeHvDblH

    {

        get => GlyphSettings.Defaults.TopTeeHvDblH;

        set => GlyphSettings.Defaults.TopTeeHvDblH = value;

    }

    /// <summary>Box Drawings Bottom Tee - Single Vertical and Single Horizontal (U+2534) - â”´</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BottomTee

    {

        get => GlyphSettings.Defaults.BottomTee;

        set => GlyphSettings.Defaults.BottomTee = value;

    }

    /// <summary>Box Drawings Bottom Tee - Single Vertical and Double Horizontal (U+2567) - â•§</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BottomTeeDblH

    {

        get => GlyphSettings.Defaults.BottomTeeDblH;

        set => GlyphSettings.Defaults.BottomTeeDblH = value;

    }

    /// <summary>Box Drawings Bottom Tee - Double Vertical and Single Horizontal (U+2568) - â•¨</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BottomTeeDblV

    {

        get => GlyphSettings.Defaults.BottomTeeDblV;

        set => GlyphSettings.Defaults.BottomTeeDblV = value;

    }

    /// <summary>Box Drawings Bottom Tee - Double Vertical and Double Horizontal (U+2569) - â•©</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BottomTeeDbl

    {

        get => GlyphSettings.Defaults.BottomTeeDbl;

        set => GlyphSettings.Defaults.BottomTeeDbl = value;

    }

    /// <summary>Box Drawings Bottom Tee - Heavy Horizontal and Light Vertical (U+2535) - â”·</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BottomTeeHvH

    {

        get => GlyphSettings.Defaults.BottomTeeHvH;

        set => GlyphSettings.Defaults.BottomTeeHvH = value;

    }

    /// <summary>Box Drawings Bottom Tee - Light Horizontal and Heavy Vertical (U+253D) - â”¸</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BottomTeeHvV

    {

        get => GlyphSettings.Defaults.BottomTeeHvV;

        set => GlyphSettings.Defaults.BottomTeeHvV = value;

    }

    /// <summary>Box Drawings Bottom Tee - Heavy Vertical and Heavy Horizontal (U+2539) - â”»</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BottomTeeHvDblH

    {

        get => GlyphSettings.Defaults.BottomTeeHvDblH;

        set => GlyphSettings.Defaults.BottomTeeHvDblH = value;

    }

    #endregion

    #region ----------------- Crosses -----------------

    /// <summary>Box Drawings Cross - Single Vertical and Single Horizontal (U+253C) - â”¼</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Cross

    {

        get => GlyphSettings.Defaults.Cross;

        set => GlyphSettings.Defaults.Cross = value;

    }

    /// <summary>Box Drawings Cross - Single Vertical and Double Horizontal (U+256A) - â•ª</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CrossDblH

    {

        get => GlyphSettings.Defaults.CrossDblH;

        set => GlyphSettings.Defaults.CrossDblH = value;

    }

    /// <summary>Box Drawings Cross - Double Vertical and Single Horizontal (U+256B) - â•«</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CrossDblV

    {

        get => GlyphSettings.Defaults.CrossDblV;

        set => GlyphSettings.Defaults.CrossDblV = value;

    }

    /// <summary>Box Drawings Cross - Double Vertical and Double Horizontal (U+256C) - â•¬</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CrossDbl

    {

        get => GlyphSettings.Defaults.CrossDbl;

        set => GlyphSettings.Defaults.CrossDbl = value;

    }

    /// <summary>Box Drawings Cross - Heavy Horizontal and Light Vertical (U+253F) - â”¿</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CrossHvH

    {

        get => GlyphSettings.Defaults.CrossHvH;

        set => GlyphSettings.Defaults.CrossHvH = value;

    }

    /// <summary>Box Drawings Cross - Light Horizontal and Heavy Vertical (U+2541) - â•‚</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CrossHvV

    {

        get => GlyphSettings.Defaults.CrossHvV;

        set => GlyphSettings.Defaults.CrossHvV = value;

    }

    /// <summary>Box Drawings Cross - Heavy Vertical and Heavy Horizontal (U+254B) - â•‹</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CrossHv

    {

        get => GlyphSettings.Defaults.CrossHv;

        set => GlyphSettings.Defaults.CrossHv = value;

    }

    #endregion

    #region ----------------- ShadowStyle -----------------

    /// <summary>Shadow - Vertical Start - Left Half Block - â–Œ U+0258c</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ShadowVerticalStart // Half: '\u2596'  â––;

    {

        get => GlyphSettings.Defaults.ShadowVerticalStart;

        set => GlyphSettings.Defaults.ShadowVerticalStart = value;

    }

    /// <summary>Shadow - Vertical - Left Half Block - â–Œ U+0258c</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ShadowVertical

    {

        get => GlyphSettings.Defaults.ShadowVertical;

        set => GlyphSettings.Defaults.ShadowVertical = value;

    }

    /// <summary>Shadow - Horizontal Start - Upper Half Block - â–€ U+02580</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ShadowHorizontalStart // Half: â– U+0259d;

    {

        get => GlyphSettings.Defaults.ShadowHorizontalStart;

        set => GlyphSettings.Defaults.ShadowHorizontalStart = value;

    }

    /// <summary>Shadow - Horizontal - Upper Half Block - â–€ U+02580</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ShadowHorizontal

    {

        get => GlyphSettings.Defaults.ShadowHorizontal;

        set => GlyphSettings.Defaults.ShadowHorizontal = value;

    }

    /// <summary>Shadow - Horizontal End - Quadrant Upper Left - â–˜ U+02598</summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ShadowHorizontalEnd

    {

        get => GlyphSettings.Defaults.ShadowHorizontalEnd;

        set => GlyphSettings.Defaults.ShadowHorizontalEnd = value;

    }

    #endregion
}