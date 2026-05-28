namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Drawing.Glyphs"/> defaults (ThemeScope).
/// </summary>
public class GlyphSettings
{
    /// <summary>Unicode replacement character; used when a wide glyph can't be output because it would be clipped.</summary>
    public Rune WideGlyphReplacement { get; set; } = (Rune)' ';

    /// <summary>File icon.</summary>
    public Rune File { get; set; } = (Rune)'☰';

    /// <summary>Folder icon.</summary>
    public Rune Folder { get; set; } = (Rune)'꤉';

    /// <summary>Horizontal Ellipsis.</summary>
    public Rune HorizontalEllipsis { get; set; } = (Rune)'…';

    /// <summary>Vertical Four Dots.</summary>
    public Rune VerticalFourDots { get; set; } = (Rune)'⁞';

    /// <summary>Null symbol.</summary>
    public Rune Null { get; set; } = (Rune)'␀';

    /// <summary>Checked indicator.</summary>
    public Rune CheckStateChecked { get; set; } = (Rune)'☒';

    /// <summary>Not Checked indicator.</summary>
    public Rune CheckStateUnChecked { get; set; } = (Rune)'☐';

    /// <summary>Null Checked indicator.</summary>
    public Rune CheckStateNone { get; set; } = (Rune)'□';

    /// <summary>Selected indicator.</summary>
    public Rune Selected { get; set; } = (Rune)'◉';

    /// <summary>Not Selected indicator.</summary>
    public Rune UnSelected { get; set; } = (Rune)'○';

    /// <summary>Right arrow.</summary>
    public Rune RightArrow { get; set; } = (Rune)'►';

    /// <summary>Left arrow.</summary>
    public Rune LeftArrow { get; set; } = (Rune)'◄';

    /// <summary>Down arrow.</summary>
    public Rune DownArrow { get; set; } = (Rune)'▼';

    /// <summary>Up arrow.</summary>
    public Rune UpArrow { get; set; } = (Rune)'▲';

    /// <summary>Left default indicator.</summary>
    public Rune LeftDefaultIndicator { get; set; } = (Rune)'►';

    /// <summary>Right default indicator.</summary>
    public Rune RightDefaultIndicator { get; set; } = (Rune)'◄';

    /// <summary>Left Bracket.</summary>
    public Rune LeftBracket { get; set; } = (Rune)'⟦';

    /// <summary>Right Bracket.</summary>
    public Rune RightBracket { get; set; } = (Rune)'⟧';

    /// <summary>Half block meter segment.</summary>
    public Rune BlocksMeterSegment { get; set; } = (Rune)'▌';

    /// <summary>Continuous block meter segment.</summary>
    public Rune ContinuousMeterSegment { get; set; } = (Rune)'█';

    /// <summary>Stipple pattern.</summary>
    public Rune Stipple { get; set; } = (Rune)'░';

    /// <summary>Diamond.</summary>
    public Rune Diamond { get; set; } = (Rune)'◊';

    /// <summary>Close.</summary>
    public Rune Close { get; set; } = (Rune)'✘';

    /// <summary>Minimize.</summary>
    public Rune Minimize { get; set; } = (Rune)'❏';

    /// <summary>Maximize.</summary>
    public Rune Maximize { get; set; } = (Rune)'✽';

    /// <summary>Dot.</summary>
    public Rune Dot { get; set; } = (Rune)'∙';

    /// <summary>Dotted Square.</summary>
    public Rune DottedSquare { get; set; } = (Rune)'⬚';

    /// <summary>Black Circle.</summary>
    public Rune BlackCircle { get; set; } = (Rune)'●';

    /// <summary>Expand.</summary>
    public Rune Expand { get; set; } = (Rune)'+';

    /// <summary>Collapse.</summary>
    public Rune Collapse { get; set; } = (Rune)'-';

    /// <summary>Identical To.</summary>
    public Rune IdenticalTo { get; set; } = (Rune)'≡';

    /// <summary>Move indicator.</summary>
    public Rune Move { get; set; } = (Rune)'◊';

    /// <summary>Size Horizontally indicator.</summary>
    public Rune SizeHorizontal { get; set; } = (Rune)'↔';

    /// <summary>Size Vertical indicator.</summary>
    public Rune SizeVertical { get; set; } = (Rune)'↕';

    /// <summary>Size Top Left indicator.</summary>
    public Rune SizeTopLeft { get; set; } = (Rune)'↖';

    /// <summary>Size Top Right indicator.</summary>
    public Rune SizeTopRight { get; set; } = (Rune)'↗';

    /// <summary>Size Bottom Right indicator.</summary>
    public Rune SizeBottomRight { get; set; } = (Rune)'↘';

    /// <summary>Size Bottom Left indicator.</summary>
    public Rune SizeBottomLeft { get; set; } = (Rune)'↙';

    /// <summary>Apple (non-BMP).</summary>
    public Rune Apple { get; set; } = "🍎".ToRunes () [0];

    /// <summary>Apple (BMP).</summary>
    public Rune AppleBMP { get; set; } = (Rune)'❦';

    /// <summary>Copy indicator.</summary>
    public Rune Copy { get; set; } = (Rune)'⧉';

    /// <summary>Box Drawings Horizontal Line - Light.</summary>
    public Rune HLine { get; set; } = (Rune)'─';

    /// <summary>Box Drawings Vertical Line - Light.</summary>
    public Rune VLine { get; set; } = (Rune)'│';

    /// <summary>Box Drawings Double Horizontal.</summary>
    public Rune HLineDbl { get; set; } = (Rune)'═';

    /// <summary>Box Drawings Double Vertical.</summary>
    public Rune VLineDbl { get; set; } = (Rune)'║';

    /// <summary>Box Drawings Heavy Double Dash Horizontal.</summary>
    public Rune HLineHvDa2 { get; set; } = (Rune)'╍';

    /// <summary>Box Drawings Heavy Triple Dash Vertical.</summary>
    public Rune VLineHvDa3 { get; set; } = (Rune)'┇';

    /// <summary>Box Drawings Heavy Triple Dash Horizontal.</summary>
    public Rune HLineHvDa3 { get; set; } = (Rune)'┅';

    /// <summary>Box Drawings Heavy Quadruple Dash Horizontal.</summary>
    public Rune HLineHvDa4 { get; set; } = (Rune)'┉';

    /// <summary>Box Drawings Heavy Double Dash Vertical.</summary>
    public Rune VLineHvDa2 { get; set; } = (Rune)'╏';

    /// <summary>Box Drawings Heavy Quadruple Dash Vertical.</summary>
    public Rune VLineHvDa4 { get; set; } = (Rune)'┋';

    /// <summary>Box Drawings Light Double Dash Horizontal.</summary>
    public Rune HLineDa2 { get; set; } = (Rune)'╌';

    /// <summary>Box Drawings Light Triple Dash Vertical.</summary>
    public Rune VLineDa3 { get; set; } = (Rune)'┆';

    /// <summary>Box Drawings Light Triple Dash Horizontal.</summary>
    public Rune HLineDa3 { get; set; } = (Rune)'┄';

    /// <summary>Box Drawings Light Quadruple Dash Horizontal.</summary>
    public Rune HLineDa4 { get; set; } = (Rune)'┈';

    /// <summary>Box Drawings Light Double Dash Vertical.</summary>
    public Rune VLineDa2 { get; set; } = (Rune)'╎';

    /// <summary>Box Drawings Light Quadruple Dash Vertical.</summary>
    public Rune VLineDa4 { get; set; } = (Rune)'┊';

    /// <summary>Box Drawings Heavy Horizontal.</summary>
    public Rune HLineHv { get; set; } = (Rune)'━';

    /// <summary>Box Drawings Heavy Vertical.</summary>
    public Rune VLineHv { get; set; } = (Rune)'┃';

    /// <summary>Box Drawings Light Left.</summary>
    public Rune HalfLeftLine { get; set; } = (Rune)'╴';

    /// <summary>Box Drawings Light Up.</summary>
    public Rune HalfTopLine { get; set; } = (Rune)'╵';

    /// <summary>Box Drawings Light Right.</summary>
    public Rune HalfRightLine { get; set; } = (Rune)'╶';

    /// <summary>Box Drawings Light Down.</summary>
    public Rune HalfBottomLine { get; set; } = (Rune)'╷';

    /// <summary>Box Drawings Heavy Left.</summary>
    public Rune HalfLeftLineHv { get; set; } = (Rune)'╸';

    /// <summary>Box Drawings Heavy Up.</summary>
    public Rune HalfTopLineHv { get; set; } = (Rune)'╹';

    /// <summary>Box Drawings Heavy Right.</summary>
    public Rune HalfRightLineHv { get; set; } = (Rune)'╺';

    /// <summary>Box Drawings Light Down Heavy.</summary>
    public Rune HalfBottomLineLt { get; set; } = (Rune)'╻';

    /// <summary>Box Drawings Light Horizontal and Heavy Horizontal.</summary>
    public Rune RightSideLineLtHv { get; set; } = (Rune)'╼';

    /// <summary>Box Drawings Light Vertical and Heavy Horizontal.</summary>
    public Rune BottomSideLineLtHv { get; set; } = (Rune)'╽';

    /// <summary>Box Drawings Heavy Left and Light Horizontal.</summary>
    public Rune LeftSideLineHvLt { get; set; } = (Rune)'╾';

    /// <summary>Box Drawings Heavy Vertical and Light Horizontal.</summary>
    public Rune TopSideLineHvLt { get; set; } = (Rune)'╿';

    /// <summary>Box Drawings Upper Left Corner - Light.</summary>
    public Rune ULCorner { get; set; } = (Rune)'┌';

    /// <summary>Box Drawings Upper Left Corner - Double.</summary>
    public Rune ULCornerDbl { get; set; } = (Rune)'╔';

    /// <summary>Box Drawings Upper Left Corner - Rounded.</summary>
    public Rune ULCornerR { get; set; } = (Rune)'╭';

    /// <summary>Box Drawings Upper Left Corner - Heavy.</summary>
    public Rune ULCornerHv { get; set; } = (Rune)'┏';

    /// <summary>Box Drawings Upper Left Corner - Heavy Vertical Light Horizontal.</summary>
    public Rune ULCornerHvLt { get; set; } = (Rune)'┎';

    /// <summary>Box Drawings Upper Left Corner - Light Vertical Heavy Horizontal.</summary>
    public Rune ULCornerLtHv { get; set; } = (Rune)'┍';

    /// <summary>Box Drawings Upper Left Corner - Double Down Single Horizontal.</summary>
    public Rune ULCornerDblSingle { get; set; } = (Rune)'╓';

    /// <summary>Box Drawings Upper Left Corner - Single Down Double Horizontal.</summary>
    public Rune ULCornerSingleDbl { get; set; } = (Rune)'╒';

    /// <summary>Box Drawings Lower Left Corner - Light.</summary>
    public Rune LLCorner { get; set; } = (Rune)'└';

    /// <summary>Box Drawings Lower Left Corner - Heavy.</summary>
    public Rune LLCornerHv { get; set; } = (Rune)'┗';

    /// <summary>Box Drawings Lower Left Corner - Heavy Vertical Light Horizontal.</summary>
    public Rune LLCornerHvLt { get; set; } = (Rune)'┖';

    /// <summary>Box Drawings Lower Left Corner - Light Vertical Heavy Horizontal.</summary>
    public Rune LLCornerLtHv { get; set; } = (Rune)'┕';

    /// <summary>Box Drawings Lower Left Corner - Double.</summary>
    public Rune LLCornerDbl { get; set; } = (Rune)'╚';

    /// <summary>Box Drawings Lower Left Corner - Single Vertical Double Horizontal.</summary>
    public Rune LLCornerSingleDbl { get; set; } = (Rune)'╘';

    /// <summary>Box Drawings Lower Left Corner - Double Vertical Single Horizontal.</summary>
    public Rune LLCornerDblSingle { get; set; } = (Rune)'╙';

    /// <summary>Box Drawings Lower Left Corner - Rounded.</summary>
    public Rune LLCornerR { get; set; } = (Rune)'╰';

    /// <summary>Box Drawings Upper Right Corner - Light.</summary>
    public Rune URCorner { get; set; } = (Rune)'┐';

    /// <summary>Box Drawings Upper Right Corner - Double.</summary>
    public Rune URCornerDbl { get; set; } = (Rune)'╗';

    /// <summary>Box Drawings Upper Right Corner - Rounded.</summary>
    public Rune URCornerR { get; set; } = (Rune)'╮';

    /// <summary>Box Drawings Upper Right Corner - Heavy.</summary>
    public Rune URCornerHv { get; set; } = (Rune)'┓';

    /// <summary>Box Drawings Upper Right Corner - Heavy Vertical Light Horizontal.</summary>
    public Rune URCornerHvLt { get; set; } = (Rune)'┑';

    /// <summary>Box Drawings Upper Right Corner - Light Vertical Heavy Horizontal.</summary>
    public Rune URCornerLtHv { get; set; } = (Rune)'┒';

    /// <summary>Box Drawings Upper Right Corner - Double Vertical Single Horizontal.</summary>
    public Rune URCornerDblSingle { get; set; } = (Rune)'╖';

    /// <summary>Box Drawings Upper Right Corner - Single Vertical Double Horizontal.</summary>
    public Rune URCornerSingleDbl { get; set; } = (Rune)'╕';

    /// <summary>Box Drawings Lower Right Corner - Light.</summary>
    public Rune LRCorner { get; set; } = (Rune)'┘';

    /// <summary>Box Drawings Lower Right Corner - Double.</summary>
    public Rune LRCornerDbl { get; set; } = (Rune)'╝';

    /// <summary>Box Drawings Lower Right Corner - Rounded.</summary>
    public Rune LRCornerR { get; set; } = (Rune)'╯';

    /// <summary>Box Drawings Lower Right Corner - Heavy.</summary>
    public Rune LRCornerHv { get; set; } = (Rune)'┛';

    /// <summary>Box Drawings Lower Right Corner - Double Vertical Single Horizontal.</summary>
    public Rune LRCornerDblSingle { get; set; } = (Rune)'╜';

    /// <summary>Box Drawings Lower Right Corner - Single Vertical Double Horizontal.</summary>
    public Rune LRCornerSingleDbl { get; set; } = (Rune)'╛';

    /// <summary>Box Drawings Lower Right Corner - Light Vertical Heavy Horizontal.</summary>
    public Rune LRCornerLtHv { get; set; } = (Rune)'┙';

    /// <summary>Box Drawings Lower Right Corner - Heavy Vertical Light Horizontal.</summary>
    public Rune LRCornerHvLt { get; set; } = (Rune)'┚';

    /// <summary>Box Drawings Left Tee - Light.</summary>
    public Rune LeftTee { get; set; } = (Rune)'├';

    /// <summary>Box Drawings Left Tee - Single Vertical Double Horizontal.</summary>
    public Rune LeftTeeDblH { get; set; } = (Rune)'╞';

    /// <summary>Box Drawings Left Tee - Double Vertical Single Horizontal.</summary>
    public Rune LeftTeeDblV { get; set; } = (Rune)'╟';

    /// <summary>Box Drawings Left Tee - Double.</summary>
    public Rune LeftTeeDbl { get; set; } = (Rune)'╠';

    /// <summary>Box Drawings Left Tee - Heavy Horizontal Light Vertical.</summary>
    public Rune LeftTeeHvH { get; set; } = (Rune)'┝';

    /// <summary>Box Drawings Left Tee - Light Horizontal Heavy Vertical.</summary>
    public Rune LeftTeeHvV { get; set; } = (Rune)'┠';

    /// <summary>Box Drawings Left Tee - Heavy.</summary>
    public Rune LeftTeeHvDblH { get; set; } = (Rune)'┣';

    /// <summary>Box Drawings Right Tee - Light.</summary>
    public Rune RightTee { get; set; } = (Rune)'┤';

    /// <summary>Box Drawings Right Tee - Single Vertical Double Horizontal.</summary>
    public Rune RightTeeDblH { get; set; } = (Rune)'╡';

    /// <summary>Box Drawings Right Tee - Double Vertical Single Horizontal.</summary>
    public Rune RightTeeDblV { get; set; } = (Rune)'╢';

    /// <summary>Box Drawings Right Tee - Double.</summary>
    public Rune RightTeeDbl { get; set; } = (Rune)'╣';

    /// <summary>Box Drawings Right Tee - Heavy Horizontal Light Vertical.</summary>
    public Rune RightTeeHvH { get; set; } = (Rune)'┥';

    /// <summary>Box Drawings Right Tee - Light Horizontal Heavy Vertical.</summary>
    public Rune RightTeeHvV { get; set; } = (Rune)'┨';

    /// <summary>Box Drawings Right Tee - Heavy.</summary>
    public Rune RightTeeHvDblH { get; set; } = (Rune)'┫';

    /// <summary>Box Drawings Top Tee - Light.</summary>
    public Rune TopTee { get; set; } = (Rune)'┬';

    /// <summary>Box Drawings Top Tee - Single Vertical Double Horizontal.</summary>
    public Rune TopTeeDblH { get; set; } = (Rune)'╤';

    /// <summary>Box Drawings Top Tee - Double Vertical Single Horizontal.</summary>
    public Rune TopTeeDblV { get; set; } = (Rune)'╥';

    /// <summary>Box Drawings Top Tee - Double.</summary>
    public Rune TopTeeDbl { get; set; } = (Rune)'╦';

    /// <summary>Box Drawings Top Tee - Heavy Horizontal Light Vertical.</summary>
    public Rune TopTeeHvH { get; set; } = (Rune)'┯';

    /// <summary>Box Drawings Top Tee - Light Horizontal Heavy Vertical.</summary>
    public Rune TopTeeHvV { get; set; } = (Rune)'┰';

    /// <summary>Box Drawings Top Tee - Heavy.</summary>
    public Rune TopTeeHvDblH { get; set; } = (Rune)'┳';

    /// <summary>Box Drawings Bottom Tee - Light.</summary>
    public Rune BottomTee { get; set; } = (Rune)'┴';

    /// <summary>Box Drawings Bottom Tee - Single Vertical Double Horizontal.</summary>
    public Rune BottomTeeDblH { get; set; } = (Rune)'╧';

    /// <summary>Box Drawings Bottom Tee - Double Vertical Single Horizontal.</summary>
    public Rune BottomTeeDblV { get; set; } = (Rune)'╨';

    /// <summary>Box Drawings Bottom Tee - Double.</summary>
    public Rune BottomTeeDbl { get; set; } = (Rune)'╩';

    /// <summary>Box Drawings Bottom Tee - Heavy Horizontal Light Vertical.</summary>
    public Rune BottomTeeHvH { get; set; } = (Rune)'┷';

    /// <summary>Box Drawings Bottom Tee - Light Horizontal Heavy Vertical.</summary>
    public Rune BottomTeeHvV { get; set; } = (Rune)'┸';

    /// <summary>Box Drawings Bottom Tee - Heavy.</summary>
    public Rune BottomTeeHvDblH { get; set; } = (Rune)'┻';

    /// <summary>Box Drawings Cross - Light.</summary>
    public Rune Cross { get; set; } = (Rune)'┼';

    /// <summary>Box Drawings Cross - Single Vertical Double Horizontal.</summary>
    public Rune CrossDblH { get; set; } = (Rune)'╪';

    /// <summary>Box Drawings Cross - Double Vertical Single Horizontal.</summary>
    public Rune CrossDblV { get; set; } = (Rune)'╫';

    /// <summary>Box Drawings Cross - Double.</summary>
    public Rune CrossDbl { get; set; } = (Rune)'╬';

    /// <summary>Box Drawings Cross - Heavy Horizontal Light Vertical.</summary>
    public Rune CrossHvH { get; set; } = (Rune)'┿';

    /// <summary>Box Drawings Cross - Light Horizontal Heavy Vertical.</summary>
    public Rune CrossHvV { get; set; } = (Rune)'╂';

    /// <summary>Box Drawings Cross - Heavy.</summary>
    public Rune CrossHv { get; set; } = (Rune)'╋';

    /// <summary>Shadow - Vertical Start.</summary>
    public Rune ShadowVerticalStart { get; set; } = (Rune)'▖';

    /// <summary>Shadow - Vertical.</summary>
    public Rune ShadowVertical { get; set; } = (Rune)'▌';

    /// <summary>Shadow - Horizontal Start.</summary>
    public Rune ShadowHorizontalStart { get; set; } = (Rune)'▝';

    /// <summary>Shadow - Horizontal.</summary>
    public Rune ShadowHorizontal { get; set; } = (Rune)'▀';

    /// <summary>Shadow - Horizontal End.</summary>
    public Rune ShadowHorizontalEnd { get; set; } = (Rune)'▘';

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static GlyphSettings Defaults { get; set; } = new ();
}
