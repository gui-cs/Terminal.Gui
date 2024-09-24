using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Defines the standard set of glyphs used to draw checkboxes, lines, borders, etc...</summary>
/// <remarks>
///     <para>
///         Access with <see cref="CM.Glyphs"/> (which is a global using alias for
///         <see cref="ConfigurationManager.Glyphs"/>).
///     </para>
///     <para>
///         The default glyphs can be changed via the <see cref="ConfigurationManager"/>. Within a <c>config.json</c>
///         file The Json property name is the property name prefixed with "Glyphs.".
///     </para>
///     <para>
///         The JSon property can be either a decimal number or a string. The string may be one of: - A unicode char
///         (e.g. "☑") - A hex value in U+ format (e.g. "U+2611") - A hex value in UTF-16 format (e.g. "\\u2611")
///     </para>
/// </remarks>
public class GlyphDefinitions
{
    /// <summary>File icon.  Defaults to ☰ (Trigram For Heaven)</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune File { get; set; } = (Rune)'☰';

    /// <summary>Folder icon.  Defaults to ꤉ (Kayah Li Digit Nine)</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Folder { get; set; } = (Rune)'꤉';

    /// <summary>Horizontal Ellipsis - … U+2026</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HorizontalEllipsis { get; set; } = (Rune)'…';

    /// <summary>Vertical Four Dots - ⁞ U+205e</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VerticalFourDots { get; set; } = (Rune)'⁞';

    #region ----------------- Single Glyphs -----------------

    /// <summary>Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>) - Ballot Box With Check - ☑ U+02611.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune CheckStateChecked { get; set; } = (Rune)'☑';

    /// <summary>Not Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune CheckStateUnChecked { get; set; } = (Rune)'☐';

    /// <summary>Null Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>). - Ballot Box With X - ☒ U+02612.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune CheckStateNone { get; set; } = (Rune)'☒';

    /// <summary>Selected indicator  (e.g. for <see cref="ListView"/> and <see cref="RadioGroup"/>).</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Selected { get; set; } = (Rune)'◉';

    /// <summary>Not Selected indicator (e.g. for <see cref="ListView"/> and <see cref="RadioGroup"/>).</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune UnSelected { get; set; } = (Rune)'○';

    /// <summary>Horizontal arrow.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune RightArrow { get; set; } = (Rune)'►';

    /// <summary>Left arrow.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LeftArrow { get; set; } = (Rune)'◄';

    /// <summary>Down arrow.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune DownArrow { get; set; } = (Rune)'▼';

    /// <summary>Vertical arrow.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune UpArrow { get; set; } = (Rune)'▲';

    /// <summary>Left default indicator (e.g. for <see cref="Button"/>.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LeftDefaultIndicator { get; set; } = (Rune)'►';

    /// <summary>Horizontal default indicator (e.g. for <see cref="Button"/>.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune RightDefaultIndicator { get; set; } = (Rune)'◄';

    /// <summary>Left Bracket (e.g. for <see cref="Button"/>. Default is (U+005B) - [.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LeftBracket { get; set; } = (Rune)'⟦';

    /// <summary>Horizontal Bracket (e.g. for <see cref="Button"/>. Default is (U+005D) - ].</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune RightBracket { get; set; } = (Rune)'⟧';

    /// <summary>Half block meter segment (e.g. for <see cref="ProgressBar"/>).</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune BlocksMeterSegment { get; set; } = (Rune)'▌';

    /// <summary>Continuous block meter segment (e.g. for <see cref="ProgressBar"/>).</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ContinuousMeterSegment { get; set; } = (Rune)'█';

    /// <summary>Stipple pattern (e.g. for <see cref="ScrollBarView"/>). Default is Light Shade (U+2591) - ░.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Stipple { get; set; } = (Rune)'░';

    /// <summary>Diamond (e.g. for <see cref="ScrollBarView"/>. Default is Lozenge (U+25CA) - ◊.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Diamond { get; set; } = (Rune)'◊';

    /// <summary>Close. Default is Heavy Ballot X (U+2718) - ✘.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Close { get; set; } = (Rune)'✘';

    /// <summary>Minimize. Default is Lower Horizontal Shadowed White Circle (U+274F) - ❏.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Minimize { get; set; } = (Rune)'❏';

    /// <summary>Maximize. Default is Upper Horizontal Shadowed White Circle (U+273D) - ✽.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Maximize { get; set; } = (Rune)'✽';

    /// <summary>Dot. Default is (U+2219) - ∙.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Dot { get; set; } = (Rune)'∙';

    /// <summary>Black Circle . Default is (U+025cf) - ●.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune BlackCircle { get; set; } = (Rune)'●'; // Black Circle - ● U+025cf

    /// <summary>Expand (e.g. for <see cref="TreeView"/>.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Expand { get; set; } = (Rune)'+';

    /// <summary>Expand (e.g. for <see cref="TreeView"/>.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Collapse { get; set; } = (Rune)'-';

    /// <summary>Identical To (U+226)</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune IdenticalTo { get; set; } = (Rune)'≡';

    /// <summary>Move indicator. Default is Lozenge (U+25CA) - ◊.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Move { get; set; } = (Rune)'◊';

    /// <summary>Size Horizontally indicator. Default is ┥Left Right Arrow - ↔ U+02194</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune SizeHorizontal { get; set; } = (Rune)'↔';

    /// <summary>Size Vertical indicator. Default Up Down Arrow - ↕ U+02195</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune SizeVertical { get; set; } = (Rune)'↕';

    /// <summary>Size Top Left indicator. North West Arrow - ↖ U+02196</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune SizeTopLeft { get; set; } = (Rune)'↖';

    /// <summary>Size Top Right indicator. North East Arrow - ↗ U+02197</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune SizeTopRight { get; set; } = (Rune)'↗';

    /// <summary>Size Bottom Right indicator. South East Arrow - ↘ U+02198</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune SizeBottomRight { get; set; } = (Rune)'↘';

    /// <summary>Size Bottom Left indicator. South West Arrow - ↙ U+02199</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune SizeBottomLLeft { get; set; } = (Rune)'↙';

    /// <summary>Apple (non-BMP). Because snek. And because it's an example of a non-BMP surrogate pair. See Issue #2610.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Apple { get; set; } = "🍎".ToRunes () [0]; // nonBMP

    /// <summary>Apple (BMP). Because snek. See Issue #2610.</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune AppleBMP { get; set; } = (Rune)'❦';

    ///// <summary>
    ///// A nonprintable (low surrogate) that should fail to ctor.
    ///// </summary>
    //public Rune InvalidGlyph { get; set; } = (Rune)'\ud83d';

    #endregion

    #region ----------------- Lines -----------------

    /// <summary>Box Drawings Horizontal Line - Light (U+2500) - ─</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HLine { get; set; } = (Rune)'─';

    /// <summary>Box Drawings Vertical Line - Light (U+2502) - │</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VLine { get; set; } = (Rune)'│';

    /// <summary>Box Drawings Double Horizontal (U+2550) - ═</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HLineDbl { get; set; } = (Rune)'═';

    /// <summary>Box Drawings Double Vertical (U+2551) - ║</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VLineDbl { get; set; } = (Rune)'║';

    /// <summary>Box Drawings Heavy Double Dash Horizontal (U+254D) - ╍</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HLineHvDa2 { get; set; } = (Rune)'╍';

    /// <summary>Box Drawings Heavy Triple Dash Vertical (U+2507) - ┇</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VLineHvDa3 { get; set; } = (Rune)'┇';

    /// <summary>Box Drawings Heavy Triple Dash Horizontal (U+2505) - ┅</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HLineHvDa3 { get; set; } = (Rune)'┅';

    /// <summary>Box Drawings Heavy Quadruple Dash Horizontal (U+2509) - ┉</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HLineHvDa4 { get; set; } = (Rune)'┉';

    /// <summary>Box Drawings Heavy Double Dash Vertical (U+254F) - ╏</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VLineHvDa2 { get; set; } = (Rune)'╏';

    /// <summary>Box Drawings Heavy Quadruple Dash Vertical (U+250B) - ┋</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VLineHvDa4 { get; set; } = (Rune)'┋';

    /// <summary>Box Drawings Light Double Dash Horizontal (U+254C) - ╌</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HLineDa2 { get; set; } = (Rune)'╌';

    /// <summary>Box Drawings Light Triple Dash Vertical (U+2506) - ┆</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VLineDa3 { get; set; } = (Rune)'┆';

    /// <summary>Box Drawings Light Triple Dash Horizontal (U+2504) - ┄</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HLineDa3 { get; set; } = (Rune)'┄';

    /// <summary>Box Drawings Light Quadruple Dash Horizontal (U+2508) - ┈</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HLineDa4 { get; set; } = (Rune)'┈';

    /// <summary>Box Drawings Light Double Dash Vertical (U+254E) - ╎</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VLineDa2 { get; set; } = (Rune)'╎';

    /// <summary>Box Drawings Light Quadruple Dash Vertical (U+250A) - ┊</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VLineDa4 { get; set; } = (Rune)'┊';

    /// <summary>Box Drawings Heavy Horizontal (U+2501) - ━</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HLineHv { get; set; } = (Rune)'━';

    /// <summary>Box Drawings Heavy Vertical (U+2503) - ┃</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune VLineHv { get; set; } = (Rune)'┃';

    /// <summary>Box Drawings Light Left (U+2574) - ╴</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HalfLeftLine { get; set; } = (Rune)'╴';

    /// <summary>Box Drawings Light Vertical (U+2575) - ╵</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HalfTopLine { get; set; } = (Rune)'╵';

    /// <summary>Box Drawings Light Horizontal (U+2576) - ╶</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HalfRightLine { get; set; } = (Rune)'╶';

    /// <summary>Box Drawings Light Down (U+2577) - ╷</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HalfBottomLine { get; set; } = (Rune)'╷';

    /// <summary>Box Drawings Heavy Left (U+2578) - ╸</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HalfLeftLineHv { get; set; } = (Rune)'╸';

    /// <summary>Box Drawings Heavy Vertical (U+2579) - ╹</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HalfTopLineHv { get; set; } = (Rune)'╹';

    /// <summary>Box Drawings Heavy Horizontal (U+257A) - ╺</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HalfRightLineHv { get; set; } = (Rune)'╺';

    /// <summary>Box Drawings Light Vertical and Horizontal (U+257B) - ╻</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune HalfBottomLineLt { get; set; } = (Rune)'╻';

    /// <summary>Box Drawings Light Horizontal and Heavy Horizontal (U+257C) - ╼</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune RightSideLineLtHv { get; set; } = (Rune)'╼';

    /// <summary>Box Drawings Light Vertical and Heavy Horizontal (U+257D) - ╽</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune BottomSideLineLtHv { get; set; } = (Rune)'╽';

    /// <summary>Box Drawings Heavy Left and Light Horizontal (U+257E) - ╾</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LeftSideLineHvLt { get; set; } = (Rune)'╾';

    /// <summary>Box Drawings Heavy Vertical and Light Horizontal (U+257F) - ╿</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune TopSideLineHvLt { get; set; } = (Rune)'╿';

    #endregion

    #region ----------------- Upper Left Corners -----------------

    /// <summary>Box Drawings Upper Left Corner - Light Vertical and Light Horizontal (U+250C) - ┌</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ULCorner { get; set; } = (Rune)'┌';

    /// <summary>Box Drawings Upper Left Corner -  Double (U+2554) - ╔</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ULCornerDbl { get; set; } = (Rune)'╔';

    /// <summary>Box Drawings Upper Left Corner - Light Arc Down and Horizontal (U+256D) - ╭</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ULCornerR { get; set; } = (Rune)'╭';

    /// <summary>Box Drawings Heavy Down and Horizontal (U+250F) - ┏</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ULCornerHv { get; set; } = (Rune)'┏';

    /// <summary>Box Drawings Down Heavy and Horizontal Light (U+251E) - ┎</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ULCornerHvLt { get; set; } = (Rune)'┎';

    /// <summary>Box Drawings Down Light and Horizontal Heavy (U+250D) - ┎</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ULCornerLtHv { get; set; } = (Rune)'┍';

    /// <summary>Box Drawings Double Down and Single Horizontal (U+2553) - ╓</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ULCornerDblSingle { get; set; } = (Rune)'╓';

    /// <summary>Box Drawings Single Down and Double Horizontal (U+2552) - ╒</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ULCornerSingleDbl { get; set; } = (Rune)'╒';

    #endregion

    #region ----------------- Lower Left Corners -----------------

    /// <summary>Box Drawings Lower Left Corner - Light Vertical and Light Horizontal (U+2514) - └</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LLCorner { get; set; } = (Rune)'└';

    /// <summary>Box Drawings Heavy Vertical and Horizontal (U+2517) - ┗</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LLCornerHv { get; set; } = (Rune)'┗';

    /// <summary>Box Drawings Heavy Vertical and Horizontal Light (U+2516) - ┖</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LLCornerHvLt { get; set; } = (Rune)'┖';

    /// <summary>Box Drawings Vertical Light and Horizontal Heavy (U+2511) - ┕</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LLCornerLtHv { get; set; } = (Rune)'┕';

    /// <summary>Box Drawings Double Vertical and Double Left (U+255A) - ╚</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LLCornerDbl { get; set; } = (Rune)'╚';

    /// <summary>Box Drawings Single Vertical and Double Left (U+2558) - ╘</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LLCornerSingleDbl { get; set; } = (Rune)'╘';

    /// <summary>Box Drawings Double Down and Single Left (U+2559) - ╙</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LLCornerDblSingle { get; set; } = (Rune)'╙';

    /// <summary>Box Drawings Upper Left Corner - Light Arc Down and Left (U+2570) - ╰</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LLCornerR { get; set; } = (Rune)'╰';

    #endregion

    #region ----------------- Upper Right Corners -----------------

    /// <summary>Box Drawings Upper Horizontal Corner - Light Vertical and Light Horizontal (U+2510) - ┐</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune URCorner { get; set; } = (Rune)'┐';

    /// <summary>Box Drawings Upper Horizontal Corner - Double Vertical and Double Horizontal (U+2557) - ╗</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune URCornerDbl { get; set; } = (Rune)'╗';

    /// <summary>Box Drawings Upper Horizontal Corner - Light Arc Vertical and Horizontal (U+256E) - ╮</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune URCornerR { get; set; } = (Rune)'╮';

    /// <summary>Box Drawings Heavy Down and Left (U+2513) - ┓</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune URCornerHv { get; set; } = (Rune)'┓';

    /// <summary>Box Drawings Heavy Vertical and Left Down Light (U+2511) - ┑</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune URCornerHvLt { get; set; } = (Rune)'┑';

    /// <summary>Box Drawings Down Light and Horizontal Heavy (U+2514) - ┒</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune URCornerLtHv { get; set; } = (Rune)'┒';

    /// <summary>Box Drawings Double Vertical and Single Left (U+2556) - ╖</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune URCornerDblSingle { get; set; } = (Rune)'╖';

    /// <summary>Box Drawings Single Vertical and Double Left (U+2555) - ╕</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune URCornerSingleDbl { get; set; } = (Rune)'╕';

    #endregion

    #region ----------------- Lower Right Corners -----------------

    /// <summary>Box Drawings Lower Right Corner - Light (U+2518) - ┘</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LRCorner { get; set; } = (Rune)'┘';

    /// <summary>Box Drawings Lower Right Corner - Double (U+255D) - ╝</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LRCornerDbl { get; set; } = (Rune)'╝';

    /// <summary>Box Drawings Lower Right Corner - Rounded (U+256F) - ╯</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LRCornerR { get; set; } = (Rune)'╯';

    /// <summary>Box Drawings Lower Right Corner - Heavy (U+251B) - ┛</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LRCornerHv { get; set; } = (Rune)'┛';

    /// <summary>Box Drawings Lower Right Corner - Double Vertical and Single Horizontal (U+255C) - ╜</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LRCornerDblSingle { get; set; } = (Rune)'╜';

    /// <summary>Box Drawings Lower Right Corner - Single Vertical and Double Horizontal (U+255B) - ╛</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LRCornerSingleDbl { get; set; } = (Rune)'╛';

    /// <summary>Box Drawings Lower Right Corner - Light Vertical and Heavy Horizontal (U+2519) - ┙</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LRCornerLtHv { get; set; } = (Rune)'┙';

    /// <summary>Box Drawings Lower Right Corner - Heavy Vertical and Light Horizontal (U+251A) - ┚</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune LRCornerHvLt { get; set; } = (Rune)'┚';

    #endregion

    #region ----------------- Tees -----------------

    /// <summary>Box Drawings Left Tee - Single Vertical and Single Horizontal (U+251C) - ├</summary>
    public Rune LeftTee { get; set; } = (Rune)'├';

    /// <summary>Box Drawings Left Tee - Single Vertical and Double Horizontal (U+255E) - ╞</summary>
    public Rune LeftTeeDblH { get; set; } = (Rune)'╞';

    /// <summary>Box Drawings Left Tee - Double Vertical and Single Horizontal (U+255F) - ╟</summary>
    public Rune LeftTeeDblV { get; set; } = (Rune)'╟';

    /// <summary>Box Drawings Left Tee - Double Vertical and Double Horizontal (U+2560) - ╠</summary>
    public Rune LeftTeeDbl { get; set; } = (Rune)'╠';

    /// <summary>Box Drawings Left Tee - Heavy Horizontal and Light Vertical (U+2523) - ┝</summary>
    public Rune LeftTeeHvH { get; set; } = (Rune)'┝';

    /// <summary>Box Drawings Left Tee - Light Horizontal and Heavy Vertical (U+252B) - ┠</summary>
    public Rune LeftTeeHvV { get; set; } = (Rune)'┠';

    /// <summary>Box Drawings Left Tee - Heavy Vertical and Heavy Horizontal (U+2527) - ┣</summary>
    public Rune LeftTeeHvDblH { get; set; } = (Rune)'┣';

    /// <summary>Box Drawings Right Tee - Single Vertical and Single Horizontal (U+2524) - ┤</summary>
    public Rune RightTee { get; set; } = (Rune)'┤';

    /// <summary>Box Drawings Right Tee - Single Vertical and Double Horizontal (U+2561) - ╡</summary>
    public Rune RightTeeDblH { get; set; } = (Rune)'╡';

    /// <summary>Box Drawings Right Tee - Double Vertical and Single Horizontal (U+2562) - ╢</summary>
    public Rune RightTeeDblV { get; set; } = (Rune)'╢';

    /// <summary>Box Drawings Right Tee - Double Vertical and Double Horizontal (U+2563) - ╣</summary>
    public Rune RightTeeDbl { get; set; } = (Rune)'╣';

    /// <summary>Box Drawings Right Tee - Heavy Horizontal and Light Vertical (U+2528) - ┥</summary>
    public Rune RightTeeHvH { get; set; } = (Rune)'┥';

    /// <summary>Box Drawings Right Tee - Light Horizontal and Heavy Vertical (U+2530) - ┨</summary>
    public Rune RightTeeHvV { get; set; } = (Rune)'┨';

    /// <summary>Box Drawings Right Tee - Heavy Vertical and Heavy Horizontal (U+252C) - ┫</summary>
    public Rune RightTeeHvDblH { get; set; } = (Rune)'┫';

    /// <summary>Box Drawings Top Tee - Single Vertical and Single Horizontal (U+252C) - ┬</summary>
    public Rune TopTee { get; set; } = (Rune)'┬';

    /// <summary>Box Drawings Top Tee - Single Vertical and Double Horizontal (U+2564) - ╤</summary>
    public Rune TopTeeDblH { get; set; } = (Rune)'╤';

    /// <summary>Box Drawings Top Tee - Double Vertical and Single Horizontal  (U+2565) - ╥</summary>
    public Rune TopTeeDblV { get; set; } = (Rune)'╥';

    /// <summary>Box Drawings Top Tee - Double Vertical and Double Horizontal (U+2566) - ╦</summary>
    public Rune TopTeeDbl { get; set; } = (Rune)'╦';

    /// <summary>Box Drawings Top Tee - Heavy Horizontal and Light Vertical (U+252F) - ┯</summary>
    public Rune TopTeeHvH { get; set; } = (Rune)'┯';

    /// <summary>Box Drawings Top Tee - Light Horizontal and Heavy Vertical (U+2537) - ┰</summary>
    public Rune TopTeeHvV { get; set; } = (Rune)'┰';

    /// <summary>Box Drawings Top Tee - Heavy Vertical and Heavy Horizontal (U+2533) - ┳</summary>
    public Rune TopTeeHvDblH { get; set; } = (Rune)'┳';

    /// <summary>Box Drawings Bottom Tee - Single Vertical and Single Horizontal (U+2534) - ┴</summary>
    public Rune BottomTee { get; set; } = (Rune)'┴';

    /// <summary>Box Drawings Bottom Tee - Single Vertical and Double Horizontal (U+2567) - ╧</summary>
    public Rune BottomTeeDblH { get; set; } = (Rune)'╧';

    /// <summary>Box Drawings Bottom Tee - Double Vertical and Single Horizontal (U+2568) - ╨</summary>
    public Rune BottomTeeDblV { get; set; } = (Rune)'╨';

    /// <summary>Box Drawings Bottom Tee - Double Vertical and Double Horizontal (U+2569) - ╩</summary>
    public Rune BottomTeeDbl { get; set; } = (Rune)'╩';

    /// <summary>Box Drawings Bottom Tee - Heavy Horizontal and Light Vertical (U+2535) - ┷</summary>
    public Rune BottomTeeHvH { get; set; } = (Rune)'┷';

    /// <summary>Box Drawings Bottom Tee - Light Horizontal and Heavy Vertical (U+253D) - ┸</summary>
    public Rune BottomTeeHvV { get; set; } = (Rune)'┸';

    /// <summary>Box Drawings Bottom Tee - Heavy Vertical and Heavy Horizontal (U+2539) - ┻</summary>
    public Rune BottomTeeHvDblH { get; set; } = (Rune)'┻';

    #endregion

    #region ----------------- Crosses -----------------

    /// <summary>Box Drawings Cross - Single Vertical and Single Horizontal (U+253C) - ┼</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune Cross { get; set; } = (Rune)'┼';

    /// <summary>Box Drawings Cross - Single Vertical and Double Horizontal (U+256A) - ╪</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune CrossDblH { get; set; } = (Rune)'╪';

    /// <summary>Box Drawings Cross - Double Vertical and Single Horizontal (U+256B) - ╫</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune CrossDblV { get; set; } = (Rune)'╫';

    /// <summary>Box Drawings Cross - Double Vertical and Double Horizontal (U+256C) - ╬</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune CrossDbl { get; set; } = (Rune)'╬';

    /// <summary>Box Drawings Cross - Heavy Horizontal and Light Vertical (U+253F) - ┿</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune CrossHvH { get; set; } = (Rune)'┿';

    /// <summary>Box Drawings Cross - Light Horizontal and Heavy Vertical (U+2541) - ╂</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune CrossHvV { get; set; } = (Rune)'╂';

    /// <summary>Box Drawings Cross - Heavy Vertical and Heavy Horizontal (U+254B) - ╋</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune CrossHv { get; set; } = (Rune)'╋';

    #endregion

    #region ----------------- ShadowStyle -----------------


    /// <summary>Shadow - Vertical Start - Left Half Block - ▌ U+0258c</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ShadowVerticalStart { get; set; } =  (Rune)'▖'; // Half: '\u2596'  ▖;

    /// <summary>Shadow - Vertical - Left Half Block - ▌ U+0258c</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ShadowVertical { get; set; } = (Rune)'▌';

    /// <summary>Shadow - Horizontal Start - Upper Half Block - ▀ U+02580</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ShadowHorizontalStart { get; set; } = (Rune)'▝'; // Half: ▝ U+0259d;

    /// <summary>Shadow - Horizontal - Upper Half Block - ▀ U+02580</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ShadowHorizontal { get; set; } = (Rune)'▀';

    /// <summary>Shadow - Horizontal End - Quadrant Upper Left - ▘ U+02598</summary>
    [JsonConverter (typeof (RuneJsonConverter))]
    public Rune ShadowHorizontalEnd { get; set; } = (Rune)'▘';

    #endregion
}
