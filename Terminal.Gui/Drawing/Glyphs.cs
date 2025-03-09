#nullable enable
namespace Terminal.Gui;

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
///         - unicode glyph in a string (e.g. "☑")
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

    /// <summary>File icon.  Defaults to ☰ (Trigram For Heaven)</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune File { get; set; } = (Rune)'☰';

    /// <summary>Folder icon.  Defaults to ꤉ (Kayah Li Digit Nine)</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Folder { get; set; } = (Rune)'꤉';

    /// <summary>Horizontal Ellipsis - … U+2026</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HorizontalEllipsis { get; set; } = (Rune)'…';

    /// <summary>Vertical Four Dots - ⁞ U+205e</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VerticalFourDots { get; set; } = (Rune)'⁞';

    #region ----------------- Single Glyphs -----------------

    /// <summary>Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CheckStateChecked { get; set; } = (Rune)'☑';

    /// <summary>Not Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CheckStateUnChecked { get; set; } = (Rune)'☐';

    /// <summary>Null Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune CheckStateNone { get; set; } = (Rune)'☒';

    /// <summary>Selected indicator  (e.g. for <see cref="ListView"/> and <see cref="RadioGroup"/>).</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Selected { get; set; } = (Rune)'◉';

    /// <summary>Not Selected indicator (e.g. for <see cref="ListView"/> and <see cref="RadioGroup"/>).</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune UnSelected { get; set; } = (Rune)'○';

    /// <summary>Horizontal arrow.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightArrow { get; set; } = (Rune)'►';

    /// <summary>Left arrow.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftArrow { get; set; } = (Rune)'◄';

    /// <summary>Down arrow.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune DownArrow { get; set; } = (Rune)'▼';

    /// <summary>Vertical arrow.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune UpArrow { get; set; } = (Rune)'▲';

    /// <summary>Left default indicator (e.g. for <see cref="Button"/>.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftDefaultIndicator { get; set; } = (Rune)'►';

    /// <summary>Horizontal default indicator (e.g. for <see cref="Button"/>.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightDefaultIndicator { get; set; } = (Rune)'◄';

    /// <summary>Left Bracket (e.g. for <see cref="Button"/>. Default is (U+005B) - [.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune LeftBracket { get; set; } = (Rune)'⟦';

    /// <summary>Horizontal Bracket (e.g. for <see cref="Button"/>. Default is (U+005D) - ].</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune RightBracket { get; set; } = (Rune)'⟧';

    /// <summary>Half block meter segment (e.g. for <see cref="ProgressBar"/>).</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BlocksMeterSegment { get; set; } = (Rune)'▌';

    /// <summary>Continuous block meter segment (e.g. for <see cref="ProgressBar"/>).</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune ContinuousMeterSegment { get; set; } = (Rune)'█';

    /// <summary>Stipple pattern (e.g. for <see cref="ScrollBar"/>). Default is Light Shade (U+2591) - ░.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Stipple { get; set; } = (Rune)'░';

    /// <summary>Diamond. Default is Lozenge (U+25CA) - ◊.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Diamond { get; set; } = (Rune)'◊';

    /// <summary>Close. Default is Heavy Ballot X (U+2718) - ✘.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Close { get; set; } = (Rune)'✘';

    /// <summary>Minimize. Default is Lower Horizontal Shadowed White Circle (U+274F) - ❏.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Minimize { get; set; } = (Rune)'❏';

    /// <summary>Maximize. Default is Upper Horizontal Shadowed White Circle (U+273D) - ✽.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Maximize { get; set; } = (Rune)'✽';

    /// <summary>Dot. Default is (U+2219) - ∙.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Dot { get; set; } = (Rune)'∙';

    /// <summary>Dotted Square - ⬚ U+02b1a┝</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune DottedSquare { get; set; } = (Rune)'⬚';

    /// <summary>Black Circle . Default is (U+025cf) - ●.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune BlackCircle { get; set; } = (Rune)'●'; // Black Circle - ● U+025cf

    /// <summary>Expand (e.g. for <see cref="TreeView"/>.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Expand { get; set; } = (Rune)'+';

    /// <summary>Expand (e.g. for <see cref="TreeView"/>.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Collapse { get; set; } = (Rune)'-';

    /// <summary>Identical To (U+226)</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune IdenticalTo { get; set; } = (Rune)'≡';

    /// <summary>Move indicator. Default is Lozenge (U+25CA) - ◊.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Move { get; set; } = (Rune)'◊';

    /// <summary>Size Horizontally indicator. Default is ┥Left Right Arrow - ↔ U+02194</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeHorizontal { get; set; } = (Rune)'↔';

    /// <summary>Size Vertical indicator. Default Up Down Arrow - ↕ U+02195</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeVertical { get; set; } = (Rune)'↕';

    /// <summary>Size Top Left indicator. North West Arrow - ↖ U+02196</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeTopLeft { get; set; } = (Rune)'↖';

    /// <summary>Size Top Right indicator. North East Arrow - ↗ U+02197</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeTopRight { get; set; } = (Rune)'↗';

    /// <summary>Size Bottom Right indicator. South East Arrow - ↘ U+02198</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeBottomRight { get; set; } = (Rune)'↘';

    /// <summary>Size Bottom Left indicator. South West Arrow - ↙ U+02199</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune SizeBottomLeft { get; set; } = (Rune)'↙';

    /// <summary>Apple (non-BMP). Because snek. And because it's an example of a non-BMP surrogate pair. See Issue #2610.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune Apple { get; set; } = "🍎".ToRunes () [0]; // nonBMP

    /// <summary>Apple (BMP). Because snek. See Issue #2610.</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune AppleBMP { get; set; } = (Rune)'❦';

    #endregion

    #region ----------------- Lines -----------------

    /// <summary>Box Drawings Horizontal Line - Light (U+2500) - ─</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLine { get; set; } = (Rune)'─';

    /// <summary>Box Drawings Vertical Line - Light (U+2502) - │</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLine { get; set; } = (Rune)'│';

    /// <summary>Box Drawings Double Horizontal (U+2550) - ═</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune HLineDbl { get; set; } = (Rune)'═';

    /// <summary>Box Drawings Double Vertical (U+2551) - ║</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune VLineDbl { get; set; } = (Rune)'║';

    /// <summary>Box Drawings Heavy Double Dash Horizontal (U+254D) - ╍</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HLineHvDa2 { get; set; } = (Rune)'╍';

    /// <summary>Box Drawings Heavy Triple Dash Vertical (U+2507) - ┇</summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Rune VLineHvDa3 { get; set; } = (Rune)'┇';

    /// <summary>Box Drawings Heavy Triple Dash Horizontal (U+2505) - ┅</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HLineHvDa3 { get; set; } = (Rune)'┅';

    /// <summary>Box Drawings Heavy Quadruple Dash Horizontal (U+2509) - ┉</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HLineHvDa4 { get; set; } = (Rune)'┉';

    /// <summary>Box Drawings Heavy Double Dash Vertical (U+254F) - ╏</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune VLineHvDa2 { get; set; } = (Rune)'╏';

    /// <summary>Box Drawings Heavy Quadruple Dash Vertical (U+250B) - ┋</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune VLineHvDa4 { get; set; } = (Rune)'┋';

    /// <summary>Box Drawings Light Double Dash Horizontal (U+254C) - ╌</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HLineDa2 { get; set; } = (Rune)'╌';

    /// <summary>Box Drawings Light Triple Dash Vertical (U+2506) - ┆</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune VLineDa3 { get; set; } = (Rune)'┆';

    /// <summary>Box Drawings Light Triple Dash Horizontal (U+2504) - ┄</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HLineDa3 { get; set; } = (Rune)'┄';

    /// <summary>Box Drawings Light Quadruple Dash Horizontal (U+2508) - ┈</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HLineDa4 { get; set; } = (Rune)'┈';

    /// <summary>Box Drawings Light Double Dash Vertical (U+254E) - ╎</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune VLineDa2 { get; set; } = (Rune)'╎';

    /// <summary>Box Drawings Light Quadruple Dash Vertical (U+250A) - ┊</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune VLineDa4 { get; set; } = (Rune)'┊';

    /// <summary>Box Drawings Heavy Horizontal (U+2501) - ━</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HLineHv { get; set; } = (Rune)'━';

    /// <summary>Box Drawings Heavy Vertical (U+2503) - ┃</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune VLineHv { get; set; } = (Rune)'┃';

    /// <summary>Box Drawings Light Left (U+2574) - ╴</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HalfLeftLine { get; set; } = (Rune)'╴';

    /// <summary>Box Drawings Light Vertical (U+2575) - ╵</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HalfTopLine { get; set; } = (Rune)'╵';

    /// <summary>Box Drawings Light Horizontal (U+2576) - ╶</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HalfRightLine { get; set; } = (Rune)'╶';

    /// <summary>Box Drawings Light Down (U+2577) - ╷</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HalfBottomLine { get; set; } = (Rune)'╷';

    /// <summary>Box Drawings Heavy Left (U+2578) - ╸</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HalfLeftLineHv { get; set; } = (Rune)'╸';

    /// <summary>Box Drawings Heavy Vertical (U+2579) - ╹</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HalfTopLineHv { get; set; } = (Rune)'╹';

    /// <summary>Box Drawings Heavy Horizontal (U+257A) - ╺</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HalfRightLineHv { get; set; } = (Rune)'╺';

    /// <summary>Box Drawings Light Vertical and Horizontal (U+257B) - ╻</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune HalfBottomLineLt { get; set; } = (Rune)'╻';

    /// <summary>Box Drawings Light Horizontal and Heavy Horizontal (U+257C) - ╼</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune RightSideLineLtHv { get; set; } = (Rune)'╼';

    /// <summary>Box Drawings Light Vertical and Heavy Horizontal (U+257D) - ╽</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune BottomSideLineLtHv { get; set; } = (Rune)'╽';

    /// <summary>Box Drawings Heavy Left and Light Horizontal (U+257E) - ╾</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LeftSideLineHvLt { get; set; } = (Rune)'╾';

    /// <summary>Box Drawings Heavy Vertical and Light Horizontal (U+257F) - ╿</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune TopSideLineHvLt { get; set; } = (Rune)'╿';

    #endregion

    #region ----------------- Upper Left Corners -----------------

    /// <summary>Box Drawings Upper Left Corner - Light Vertical and Light Horizontal (U+250C) - ┌</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ULCorner { get; set; } = (Rune)'┌';

    /// <summary>Box Drawings Upper Left Corner -  Double (U+2554) - ╔</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ULCornerDbl { get; set; } = (Rune)'╔';

    /// <summary>Box Drawings Upper Left Corner - Light Arc Down and Horizontal (U+256D) - ╭</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ULCornerR { get; set; } = (Rune)'╭';

    /// <summary>Box Drawings Heavy Down and Horizontal (U+250F) - ┏</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ULCornerHv { get; set; } = (Rune)'┏';

    /// <summary>Box Drawings Down Heavy and Horizontal Light (U+251E) - ┎</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ULCornerHvLt { get; set; } = (Rune)'┎';

    /// <summary>Box Drawings Down Light and Horizontal Heavy (U+250D) - ┎</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ULCornerLtHv { get; set; } = (Rune)'┍';

    /// <summary>Box Drawings Double Down and Single Horizontal (U+2553) - ╓</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ULCornerDblSingle { get; set; } = (Rune)'╓';

    /// <summary>Box Drawings Single Down and Double Horizontal (U+2552) - ╒</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ULCornerSingleDbl { get; set; } = (Rune)'╒';

    #endregion

    #region ----------------- Lower Left Corners -----------------

    /// <summary>Box Drawings Lower Left Corner - Light Vertical and Light Horizontal (U+2514) - └</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LLCorner { get; set; } = (Rune)'└';

    /// <summary>Box Drawings Heavy Vertical and Horizontal (U+2517) - ┗</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LLCornerHv { get; set; } = (Rune)'┗';

    /// <summary>Box Drawings Heavy Vertical and Horizontal Light (U+2516) - ┖</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LLCornerHvLt { get; set; } = (Rune)'┖';

    /// <summary>Box Drawings Vertical Light and Horizontal Heavy (U+2511) - ┕</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LLCornerLtHv { get; set; } = (Rune)'┕';

    /// <summary>Box Drawings Double Vertical and Double Left (U+255A) - ╚</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LLCornerDbl { get; set; } = (Rune)'╚';

    /// <summary>Box Drawings Single Vertical and Double Left (U+2558) - ╘</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LLCornerSingleDbl { get; set; } = (Rune)'╘';

    /// <summary>Box Drawings Double Down and Single Left (U+2559) - ╙</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LLCornerDblSingle { get; set; } = (Rune)'╙';

    /// <summary>Box Drawings Upper Left Corner - Light Arc Down and Left (U+2570) - ╰</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LLCornerR { get; set; } = (Rune)'╰';

    #endregion

    #region ----------------- Upper Right Corners -----------------

    /// <summary>Box Drawings Upper Horizontal Corner - Light Vertical and Light Horizontal (U+2510) - ┐</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune URCorner { get; set; } = (Rune)'┐';

    /// <summary>Box Drawings Upper Horizontal Corner - Double Vertical and Double Horizontal (U+2557) - ╗</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune URCornerDbl { get; set; } = (Rune)'╗';

    /// <summary>Box Drawings Upper Horizontal Corner - Light Arc Vertical and Horizontal (U+256E) - ╮</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune URCornerR { get; set; } = (Rune)'╮';

    /// <summary>Box Drawings Heavy Down and Left (U+2513) - ┓</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune URCornerHv { get; set; } = (Rune)'┓';

    /// <summary>Box Drawings Heavy Vertical and Left Down Light (U+2511) - ┑</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune URCornerHvLt { get; set; } = (Rune)'┑';

    /// <summary>Box Drawings Down Light and Horizontal Heavy (U+2514) - ┒</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune URCornerLtHv { get; set; } = (Rune)'┒';

    /// <summary>Box Drawings Double Vertical and Single Left (U+2556) - ╖</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune URCornerDblSingle { get; set; } = (Rune)'╖';

    /// <summary>Box Drawings Single Vertical and Double Left (U+2555) - ╕</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune URCornerSingleDbl { get; set; } = (Rune)'╕';

    #endregion

    #region ----------------- Lower Right Corners -----------------

    /// <summary>Box Drawings Lower Right Corner - Light (U+2518) - ┘</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LRCorner { get; set; } = (Rune)'┘';

    /// <summary>Box Drawings Lower Right Corner - Double (U+255D) - ╝</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LRCornerDbl { get; set; } = (Rune)'╝';

    /// <summary>Box Drawings Lower Right Corner - Rounded (U+256F) - ╯</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LRCornerR { get; set; } = (Rune)'╯';

    /// <summary>Box Drawings Lower Right Corner - Heavy (U+251B) - ┛</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LRCornerHv { get; set; } = (Rune)'┛';

    /// <summary>Box Drawings Lower Right Corner - Double Vertical and Single Horizontal (U+255C) - ╜</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LRCornerDblSingle { get; set; } = (Rune)'╜';

    /// <summary>Box Drawings Lower Right Corner - Single Vertical and Double Horizontal (U+255B) - ╛</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LRCornerSingleDbl { get; set; } = (Rune)'╛';

    /// <summary>Box Drawings Lower Right Corner - Light Vertical and Heavy Horizontal (U+2519) - ┙</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LRCornerLtHv { get; set; } = (Rune)'┙';

    /// <summary>Box Drawings Lower Right Corner - Heavy Vertical and Light Horizontal (U+251A) - ┚</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LRCornerHvLt { get; set; } = (Rune)'┚';

    #endregion

    #region ----------------- Tees -----------------

    /// <summary>Box Drawings Left Tee - Single Vertical and Single Horizontal (U+251C) - ├</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LeftTee { get; set; } = (Rune)'├';

    /// <summary>Box Drawings Left Tee - Single Vertical and Double Horizontal (U+255E) - ╞</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LeftTeeDblH { get; set; } = (Rune)'╞';

    /// <summary>Box Drawings Left Tee - Double Vertical and Single Horizontal (U+255F) - ╟</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LeftTeeDblV { get; set; } = (Rune)'╟';

    /// <summary>Box Drawings Left Tee - Double Vertical and Double Horizontal (U+2560) - ╠</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LeftTeeDbl { get; set; } = (Rune)'╠';

    /// <summary>Box Drawings Left Tee - Heavy Horizontal and Light Vertical (U+2523) - ┝</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LeftTeeHvH { get; set; } = (Rune)'┝';

    /// <summary>Box Drawings Left Tee - Light Horizontal and Heavy Vertical (U+252B) - ┠</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LeftTeeHvV { get; set; } = (Rune)'┠';

    /// <summary>Box Drawings Left Tee - Heavy Vertical and Heavy Horizontal (U+2527) - ┣</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune LeftTeeHvDblH { get; set; } = (Rune)'┣';

    /// <summary>Box Drawings Right Tee - Single Vertical and Single Horizontal (U+2524) - ┤</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune RightTee { get; set; } = (Rune)'┤';

    /// <summary>Box Drawings Right Tee - Single Vertical and Double Horizontal (U+2561) - ╡</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune RightTeeDblH { get; set; } = (Rune)'╡';

    /// <summary>Box Drawings Right Tee - Double Vertical and Single Horizontal (U+2562) - ╢</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune RightTeeDblV { get; set; } = (Rune)'╢';

    /// <summary>Box Drawings Right Tee - Double Vertical and Double Horizontal (U+2563) - ╣</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune RightTeeDbl { get; set; } = (Rune)'╣';

    /// <summary>Box Drawings Right Tee - Heavy Horizontal and Light Vertical (U+2528) - ┥</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune RightTeeHvH { get; set; } = (Rune)'┥';

    /// <summary>Box Drawings Right Tee - Light Horizontal and Heavy Vertical (U+2530) - ┨</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune RightTeeHvV { get; set; } = (Rune)'┨';

    /// <summary>Box Drawings Right Tee - Heavy Vertical and Heavy Horizontal (U+252C) - ┫</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune RightTeeHvDblH { get; set; } = (Rune)'┫';

    /// <summary>Box Drawings Top Tee - Single Vertical and Single Horizontal (U+252C) - ┬</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune TopTee { get; set; } = (Rune)'┬';

    /// <summary>Box Drawings Top Tee - Single Vertical and Double Horizontal (U+2564) - ╤</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune TopTeeDblH { get; set; } = (Rune)'╤';

    /// <summary>Box Drawings Top Tee - Double Vertical and Single Horizontal  (U+2565) - ╥</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune TopTeeDblV { get; set; } = (Rune)'╥';

    /// <summary>Box Drawings Top Tee - Double Vertical and Double Horizontal (U+2566) - ╦</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune TopTeeDbl { get; set; } = (Rune)'╦';

    /// <summary>Box Drawings Top Tee - Heavy Horizontal and Light Vertical (U+252F) - ┯</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune TopTeeHvH { get; set; } = (Rune)'┯';

    /// <summary>Box Drawings Top Tee - Light Horizontal and Heavy Vertical (U+2537) - ┰</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune TopTeeHvV { get; set; } = (Rune)'┰';

    /// <summary>Box Drawings Top Tee - Heavy Vertical and Heavy Horizontal (U+2533) - ┳</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune TopTeeHvDblH { get; set; } = (Rune)'┳';

    /// <summary>Box Drawings Bottom Tee - Single Vertical and Single Horizontal (U+2534) - ┴</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune BottomTee { get; set; } = (Rune)'┴';

    /// <summary>Box Drawings Bottom Tee - Single Vertical and Double Horizontal (U+2567) - ╧</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune BottomTeeDblH { get; set; } = (Rune)'╧';

    /// <summary>Box Drawings Bottom Tee - Double Vertical and Single Horizontal (U+2568) - ╨</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune BottomTeeDblV { get; set; } = (Rune)'╨';

    /// <summary>Box Drawings Bottom Tee - Double Vertical and Double Horizontal (U+2569) - ╩</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune BottomTeeDbl { get; set; } = (Rune)'╩';

    /// <summary>Box Drawings Bottom Tee - Heavy Horizontal and Light Vertical (U+2535) - ┷</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune BottomTeeHvH { get; set; } = (Rune)'┷';

    /// <summary>Box Drawings Bottom Tee - Light Horizontal and Heavy Vertical (U+253D) - ┸</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune BottomTeeHvV { get; set; } = (Rune)'┸';

    /// <summary>Box Drawings Bottom Tee - Heavy Vertical and Heavy Horizontal (U+2539) - ┻</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune BottomTeeHvDblH { get; set; } = (Rune)'┻';

    #endregion

    #region ----------------- Crosses -----------------

    /// <summary>Box Drawings Cross - Single Vertical and Single Horizontal (U+253C) - ┼</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune Cross { get; set; } = (Rune)'┼';

    /// <summary>Box Drawings Cross - Single Vertical and Double Horizontal (U+256A) - ╪</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune CrossDblH { get; set; } = (Rune)'╪';

    /// <summary>Box Drawings Cross - Double Vertical and Single Horizontal (U+256B) - ╫</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune CrossDblV { get; set; } = (Rune)'╫';

    /// <summary>Box Drawings Cross - Double Vertical and Double Horizontal (U+256C) - ╬</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune CrossDbl { get; set; } = (Rune)'╬';

    /// <summary>Box Drawings Cross - Heavy Horizontal and Light Vertical (U+253F) - ┿</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune CrossHvH { get; set; } = (Rune)'┿';

    /// <summary>Box Drawings Cross - Light Horizontal and Heavy Vertical (U+2541) - ╂</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune CrossHvV { get; set; } = (Rune)'╂';

    /// <summary>Box Drawings Cross - Heavy Vertical and Heavy Horizontal (U+254B) - ╋</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune CrossHv { get; set; } = (Rune)'╋';

    #endregion

    #region ----------------- ShadowStyle -----------------

    /// <summary>Shadow - Vertical Start - Left Half Block - ▌ U+0258c</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ShadowVerticalStart { get; set; } = (Rune)'▖'; // Half: '\u2596'  ▖;

    /// <summary>Shadow - Vertical - Left Half Block - ▌ U+0258c</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ShadowVertical { get; set; } = (Rune)'▌';

    /// <summary>Shadow - Horizontal Start - Upper Half Block - ▀ U+02580</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ShadowHorizontalStart { get; set; } = (Rune)'▝'; // Half: ▝ U+0259d;

    /// <summary>Shadow - Horizontal - Upper Half Block - ▀ U+02580</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ShadowHorizontal { get; set; } = (Rune)'▀';

    /// <summary>Shadow - Horizontal End - Quadrant Upper Left - ▘ U+02598</summary>
    [SerializableConfigurationProperty(Scope = typeof(ThemeScope))] public static Rune ShadowHorizontalEnd { get; set; } = (Rune)'▘';

    #endregion
}
