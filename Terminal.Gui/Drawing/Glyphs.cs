using Rune = System.Rune;

namespace Terminal.Gui {
	/// <summary>
	/// Defines the standard set of glyphs used to draw checkboxes, lines, borders, etc...
	/// </summary>
	/// <remarks>
	/// <para>
	/// The default glyphs can be changed via the <see cref="ConfigurationManager"/>. Within a <c>config.json</c> file 
	/// tHe JSon property name is the <see cref="Glyphs"/> property prefixed with "Application.Glyphs.". 
	/// </para>
	/// <para>
	/// The JSon property can be either a decimal number or a string. The string may be one of:
	/// - A unicode char (e.g. "☑")
	/// - A hex value in U+ format (e.g. "U+2611")
	/// - A hex value in UTF-16 format (e.g. "\\u2611")
	/// </para>
	/// </remarks>
	public class Glyphs {
		/// <summary>
		/// Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).
		/// </summary>
		public Rune Checked { get; set; } = '☑';

		/// <summary>
		/// Not Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).
		/// </summary>
		public Rune UnChecked { get; set; } = '☐';

		/// <summary>
		/// Null Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).
		/// </summary>
		public Rune NullChecked { get; set; } = '☒';

		/// <summary>
		/// Selected indicator  (e.g. for <see cref="ListView"/> and <see cref="RadioGroup"/>).
		/// </summary>
		public Rune Selected { get; set; } = '◉';

		/// <summary>
		/// Not Selected indicator (e.g. for <see cref="ListView"/> and <see cref="RadioGroup"/>).
		/// </summary>
		public Rune UnSelected { get; set; } = '○';

		/// <summary>
		/// Right arrow.
		/// </summary>
		public Rune RightArrow { get; set; } = '►';

		/// <summary>
		/// Left arrow.
		/// </summary>
		public Rune LeftArrow { get; set; } = '◄';

		/// <summary>
		/// Down arrow.
		/// </summary>
		public Rune DownArrow { get; set; } = '▼';

		/// <summary>
		/// Up arrow.
		/// </summary>
		public Rune UpArrow { get; set; } = '▲';

		/// <summary>
		/// Left default indicator (e.g. for <see cref="Button"/>.
		/// </summary>
		public Rune LeftDefaultIndicator { get; set; } = '►';

		/// <summary>
		/// Right default indicator (e.g. for <see cref="Button"/>.
		/// </summary>
		public Rune RightDefaultIndicator { get; set; } = '◄';


		/// <summary>
		/// Left Bracket (e.g. for <see cref="Button"/>. Default is (U+005B) - [.
		/// </summary>
		public Rune LeftBracket { get; set; } = '⟦';


		/// <summary>
		/// Right Bracket (e.g. for <see cref="Button"/>. Default is (U+005D) - ].
		/// </summary>
		public Rune RightBracket { get; set; } = '⟧';


		/// <summary>
		/// Half block meter segment (e.g. for <see cref="ProgressBar"/>).
		/// </summary>
		public Rune BlocksMeterSegment { get; set; } = '▌';


		/// <summary>
		/// Continuous block meter segment (e.g. for <see cref="ProgressBar"/>).
		/// </summary>
		public Rune ContinuousMeterSegment { get; set; } = '█';


		/// <summary>
		/// Stipple pattern (e.g. for <see cref="ScrollBarView"/>). Default is Light Shade (U+2591) - ░.
		/// </summary>
		public Rune Stipple { get; set; } = '░';


		/// <summary>
		/// Diamond (e.g. for <see cref="ScrollBarView"/>. Default is Lozenge (U+25CA) - ◊.
		/// </summary>
		public Rune Diamond { get; set; } = '◊';


		/// <summary>
		/// Close. Default is Heavy Ballot X (U+2718) - ✘.
		/// </summary>
		public Rune Close { get; set; } = '✘';


		/// <summary>
		/// Minimize. Default is Lower Right Shadowed White Circle (U+274F) - ❏.
		/// </summary>
		public Rune Minimize { get; set; } = '❏';


		/// <summary>
		/// Maximize. Default is Upper Right Shadowed White Circle (U+273D) - ✽.
		/// </summary>
		public Rune Maximize { get; set; } = '✽';


		/// <summary>
		/// Horizontal Line (U+2500) - ─
		/// </summary>
		public Rune HLine { get; set; } = '─';


		/// <summary>
		/// Vertical Line (U+2502) - │
		/// </summary>
		public Rune VLine { get; set; } = '│';


		/// <summary>
		/// Box drawings light down and right (U+250C) - ┌
		/// </summary>
		public Rune ULCorner { get; set; } = '┌';


		/// <summary>
		/// Lower Left Corner (U+2514) - └
		/// </summary>
		public Rune LLCorner { get; set; } = '└';


		/// <summary>
		/// Upper Right Corner (U+2510) - ┐
		/// </summary>
		public Rune URCorner { get; set; } = '┐';


		/// <summary>
		/// Lower Right Corner (U+2518) - ┘
		/// </summary>
		public Rune LRCorner { get; set; } = '┘';


		/// <summary>
		/// Box Drawings Left Tee - Single Vertical and Single Horizontal (U+251C) - ├
		/// </summary>
		public Rune LeftTee { get; set; } = '├';

		/// <summary>
		/// Box Drawings Left Tee - Single Vertical and Double Horizontal (U+255E) - ╞
		/// </summary>
		public Rune LeftTeeDblH { get; set; } = '╞';

		/// <summary>
		/// Box Drawings Left Tee - Double Vertical and Single Horizontal (U+255F) - ╟
		/// </summary>
		public Rune LeftTeeDblV { get; set; } = '╟';

		/// <summary>
		/// Box Drawings Left Tee - Double Vertical and Double Horizontal (U+2560) - ╠
		/// </summary>
		public Rune LeftTeeDbl { get; set; } = '╠';

		/// <summary>
		/// Box Drawings Left Tee - Heavy Horizontal and Light Vertical (U+2523) - ┝
		/// </summary>
		public Rune LeftTeeHvH { get; set; } = '┝';

		/// <summary>
		/// Box Drawings Left Tee - Light Horizontal and Heavy Vertical (U+252B) - ┠
		/// </summary>
		public Rune LeftTeeHvV { get; set; } = '┠';

		/// <summary>
		/// Box Drawings Left Tee - Heavy Vertical and Heavy Horizontal (U+2527) - ┣
		/// </summary>
		public Rune LeftTeeHvDblH { get; set; } = '┣';

		/// <summary>
		/// Box Drawings Right Tee - Single Vertical and Single Horizontal (U+2524) - ┤
		/// </summary>
		public Rune RightTee { get; set; } = '┤';

		/// <summary>
		/// Box Drawings Right Tee - Single Vertical and Double Horizontal (U+2561) - ╡
		/// </summary>
		public Rune RightTeeDblH { get; set; } = '╡';

		/// <summary>
		/// Box Drawings Right Tee - Double Vertical and Single Horizontal (U+2562) - ╢
		/// </summary>
		public Rune RightTeeDblV { get; set; } = '╢';

		/// <summary>
		/// Box Drawings Right Tee - Double Vertical and Double Horizontal (U+2563) - ╣
		/// </summary>
		public Rune RightTeeDbl { get; set; } = '╣';

		/// <summary>
		/// Box Drawings Right Tee - Heavy Horizontal and Light Vertical (U+2528) - ┥
		/// </summary>
		public Rune RightTeeHvH { get; set; } = '┥';

		/// <summary>
		/// Box Drawings Right Tee - Light Horizontal and Heavy Vertical (U+2530) - ┨
		/// </summary>
		public Rune RightTeeHvV { get; set; } = '┨';

		/// <summary>
		/// Box Drawings Right Tee - Heavy Vertical and Heavy Horizontal (U+252C) - ┫
		/// </summary>
		public Rune RightTeeHvDblH { get; set; } = '┫';


		/// <summary>
		/// Box Drawings Top Tee - Single Vertical and Single Horizontal (U+252C) - ┬
		/// </summary>
		public Rune TopTee { get; set; } = '┬';

		/// <summary>
		/// Box Drawings Top Tee - Single Vertical and Double Horizontal (U+2564) - ╤
		/// </summary>
		public Rune TopTeeDblH { get; set; } = '╤';

		/// <summary>
		/// Box Drawings Top Tee - Double Vertical and Single Horizontal  (U+2565) - ╥
		/// </summary>
		public Rune TopTeeDblV { get; set; } = '╥';

		/// <summary>
		/// Box Drawings Top Tee - Double Vertical and Double Horizontal (U+2566) - ╦
		/// </summary>
		public Rune TopTeeDbl { get; set; } = '╦';

		/// <summary>
		/// Box Drawings Top Tee - Heavy Horizontal and Light Vertical (U+252F) - ┯
		/// </summary>
		public Rune TopTeeHvH { get; set; } = '┯';

		/// <summary>
		/// Box Drawings Top Tee - Light Horizontal and Heavy Vertical (U+2537) - ┰
		/// </summary>
		public Rune TopTeeHvV { get; set; } = '┰';

		/// <summary>
		/// Box Drawings Top Tee - Heavy Vertical and Heavy Horizontal (U+2533) - ┳
		/// </summary>
		public Rune TopTeeHvDblH { get; set; } = '┳';

		/// <summary>
		/// Box Drawings Bottom Tee - Single Vertical and Single Horizontal (U+2534) - ┴
		/// </summary>
		public Rune BottomTee { get; set; } = '┴';

		/// <summary>
		/// Box Drawings Bottom Tee - Single Vertical and Double Horizontal (U+2567) - ╧
		/// </summary>
		public Rune BottomTeeDblH { get; set; } = '╧';

		/// <summary>
		/// Box Drawings Bottom Tee - Double Vertical and Single Horizontal (U+2568) - ╨
		/// </summary>
		public Rune BottomTeeDblV { get; set; } = '╨';

		/// <summary>
		/// Box Drawings Bottom Tee - Double Vertical and Double Horizontal (U+2569) - ╩
		/// </summary>
		public Rune BottomTeeDbl { get; set; } = '╩';

		/// <summary>
		/// Box Drawings Bottom Tee - Heavy Horizontal and Light Vertical (U+2535) - ┷
		/// </summary>
		public Rune BottomTeeHvH { get; set; } = '┷';

		/// <summary>
		/// Box Drawings Bottom Tee - Light Horizontal and Heavy Vertical (U+253D) - ┸
		/// </summary>
		public Rune BottomTeeHvV { get; set; } = '┸';

		/// <summary>
		/// Box Drawings Bottom Tee - Heavy Vertical and Heavy Horizontal (U+2539) - ┻
		/// </summary>
		public Rune BottomTeeHvDblH { get; set; } = '┻';

		/// <summary>
		/// Box Drawings Cross - Single Vertical and Single Horizontal (U+253C) - ┼
		/// </summary>
		public Rune Cross { get; set; } = '┼';

		/// <summary>
		/// Box Drawings Cross - Single Vertical and Double Horizontal (U+256A) - ╪
		/// </summary>
		public Rune CrossDblH { get; set; } = '╪';

		/// <summary>
		/// Box Drawings Cross - Double Vertical and Single Horizontal (U+256B) - ╫
		/// </summary>
		public Rune CrossDblV { get; set; } = '╫';

		/// <summary>
		/// Box Drawings Cross - Double Vertical and Double Horizontal (U+256C) - ╬
		/// </summary>
		public Rune CrossDbl { get; set; } = '╬';

		/// <summary>
		/// Box Drawings Cross - Heavy Horizontal and Light Vertical (U+253F) - ┿
		/// </summary>
		public Rune CrossHvH { get; set; } = '┿';

		/// <summary>
		/// Box Drawings Cross - Light Horizontal and Heavy Vertical (U+2541) - ╂
		/// </summary>
		public Rune CrossHvV { get; set; } = '╂';

		/// <summary>
		/// Box Drawings Cross - Heavy Vertical and Heavy Horizontal (U+254B) - ╋
		/// </summary>
		public Rune CrossHvDblH { get; set; } = '╋';

		/// <summary>
		/// Box Drawings Double Horizontal (U+2550) - ═
		/// </summary>
		public Rune HLineDb { get; set; } = '═';

		/// <summary>
		/// Box Drawings Double Vertical (U+2551) - ║
		/// </summary>
		public Rune VLineDb { get; set; } = '║';

		/// <summary>
		/// Box Drawings Double Down and Right (U+2554) - ╔
		/// </summary>
		public Rune ULCornerDb { get; set; } = '╔';

		/// <summary>
		/// Box Drawings Double Up and Right (U+255A) - ╚
		/// </summary>
		public Rune LLCornerDb { get; set; } = '╝';

		/// <summary>
		/// Box Drawings Double Down and Left (U+2557) - ╗
		/// </summary>
		public Rune URCornerDb { get; set; } = '╗';

		/// <summary>
		/// Box Drawings Double Up and Left (U+255D) - ╝
		/// </summary>
		public Rune LRCornerDb { get; set; } = '╝';

			/// <summary>
		/// Box Drawings Light Arc Down and Right (U+256D) - ╭
		/// </summary>
		public Rune ULCornerR { get; set; } = '╭';

		/// <summary>
		/// Box Drawings Light Arc Down and Left (U+2570) - ╰
		/// </summary>
		public Rune LLCornerR { get; set; } = '╰';

		/// <summary>
		/// Box Drawings Light Arc Up and Right (U+256E) - ╮
		/// </summary>
		public Rune URCornerR { get; set; } = '╮';

		/// <summary>
		/// Box Drawings Light Arc Up and Left (U+256F) - ╯
		/// </summary>
		public Rune LRCornerR { get; set; } = '╯';

		/// <summary>
		/// Box Drawings Light Double Dash Horizontal (U+254C) - ╌
		/// </summary>
		public Rune HLineDa2 { get; set; } = '╌';

		/// <summary>
		/// Box Drawings Light Triple Dash Vertical (U+2506) - ┆
		/// </summary>
		public Rune VLineDa3 { get; set; } = '┆';

		/// <summary>
		/// Box Drawings Light Triple Dash Horizontal (U+2504) - ┄
		/// </summary>
		public Rune HLineDa3 { get; set; } = '┄';

		/// <summary>
		/// Box Drawings Light Quadruple Dash Horizontal (U+2508) - ┈
		/// </summary>
		public Rune HLineDa4 { get; set; } = '┈';

		/// <summary>
		/// Box Drawings Light Double Dash Vertical (U+254E) - ╎
		/// </summary>
		public Rune VLineDa2 { get; set; } = '╎';

		/// <summary>
		/// Box Drawings Light Quadruple Dash Vertical (U+250A) - ┊
		/// </summary>
		public Rune VLineDa4 { get; set; } = '┊';

		/// <summary>
		/// Box Drawings Heavy Horizontal (U+2501) - ━
		/// </summary>
		public Rune HLineHv { get; set; } = '━';

		/// <summary>
		/// Box Drawings Heavy Vertical (U+2503) - ┃
		/// </summary>
		public Rune VLineHv { get; set; } = '┃';

		/// <summary>
		/// Box Drawings Heavy Down and Right (U+250F) - ┏
		/// </summary>
		public Rune ULCornerHv { get; set; } = '┏';

		/// <summary>
		/// Box Drawings Down Heavy and Right Light (U+251E) - ┎
		/// </summary>
		public Rune ULCornerHvLt { get; set; } = '┎';
		
		/// <summary>
		/// Box Drawings Down Light and Right Heavy (U+250D) - ┎
		/// </summary>
		public Rune ULCornerLtHv { get; set; } = '┎';

		/// <summary>
		/// Box Drawings Double Down and Double Right (U+2554) - ╔
		/// </summary>
		public Rune ULCornerDbl { get; set; } = '╔';

		/// <summary>
		/// Box Drawings Double Down and Single Right (U+2553) - ╓
		/// </summary>
		public Rune ULCornerDblSingle { get; set; } = '╓';

		/// <summary>
		/// Box Drawings Single Down and Double Right (U+2552) - ╒
		/// </summary>
		public Rune ULCornerSingleDbl { get; set; } = '╒';

		/// <summary>
		/// Box Drawings Heavy Up and Right (U+2517) - ┗
		/// </summary>
		public Rune LLCornerHv { get; set; } = '┗';

		/// <summary>
		/// Box Drawings Up Heavy and Right Light (U+2516) - ┖
		/// </summary>
		public Rune LLCornerHvLt { get; set; } = '┖';

		/// <summary>
		/// Box Drawings Up Light and Right Heavy (U+2511) - ┕
		/// </summary>
		public Rune LLCornerLtHv { get; set; } = '┕';

		/// <summary>
		/// Box Drawings Double Up and Double Left (U+255A) - ╚
		/// </summary>
		public Rune LLCornerDbl { get; set; } = '╚';

		/// <summary>
		/// Box Drawings Single Up and Double Left (U+2558) - ╘
		/// </summary>
		public Rune LLCornerSingleDbl { get; set; } = '╘';

		/// <summary>
		/// Box Drawings Double Down and Single Left (U+2559) - ╙
		/// </summary>
		public Rune LLCornerDblSingle { get; set; } = '╙';

		/// <summary>
		/// Box Drawings Heavy Down and Left (U+2513) - ┓
		/// </summary>
		public Rune URCornerHv { get; set; } = '┓';

		/// <summary>
		/// Box Drawings Up Heavy and Left Down Light (U+2511) - ┑
		/// </summary>
		public Rune URCornerHvLt { get; set; } = '┑';

		/// <summary>
		/// Box Drawings Down Light and Right Heavy (U+2514) - ┒
		/// </summary>
		public Rune URCornerLtHv { get; set; } = '┒';

		/// <summary>
		/// Box Drawings Double Up and Double Right (U+2557) - ╗
		/// </summary>
		public Rune URCornerDbl { get; set; } = '╗';

		/// <summary>
		/// Box Drawings Double Up and Single Left (U+2556) - ╖
		/// </summary>
		public Rune URCornerDblSingle { get; set; } = '╖';

		/// <summary>
		/// Box Drawings Single Up and Double Left (U+2555) - ╕
		/// </summary>
		public Rune URCornerSingleDbl { get; set; } = '╕';

		/// <summary>
		/// Box Drawings Heavy Up and Left (U+251B) - ┛
		/// </summary>
		public Rune LRCornerHv { get; set; } = '┛';

		/// <summary>
		/// Box Drawings Double Up and Double Left (U+255D) - ╝
		/// </summary>
		public Rune LRCornerDbl { get; set; } = '╝';

		/// <summary>
		/// Box Drawings Double Up and Single Right (U+255C) - ╜
		/// </summary>
		public Rune LRCornerDblSingle { get; set; } = '╜';

		/// <summary>
		/// Box Drawings Single Up and Double Right (U+255B) - ╛
		/// </summary>
		public Rune LRCornerSingleDbl { get; set; } = '╛';

		/// <summary>
		/// Box Drawings Up Heavy and Right Down Light (U+2518) - ┘
		/// </summary>
		public Rune LRCornerLtHv { get; set; } = '┘';

		/// <summary>
		/// Box Drawings Up Heavy and Right Down Light (U+251A) - ┚
		/// </summary>
		public Rune LRCornerHvLt { get; set; } = '┚';

		/// <summary>
		/// Box Drawings Heavy Double Dash Horizontal (U+254D) - ╍
		/// </summary>
		public Rune HLineHvDa2 { get; set; } = '╍';

		/// <summary>
		/// Box Drawings Heavy Triple Dash Vertical (U+2507) - ┇
		/// </summary>
		public Rune VLineHvDa3 { get; set; } = '┇';

		/// <summary>
		/// Box Drawings Heavy Triple Dash Horizontal (U+2505) - ┅
		/// </summary>
		public Rune HLineHvDa3 { get; set; } = '┅';

		/// <summary>
		/// Box Drawings Heavy Quadruple Dash Horizontal (U+2509) - ┉
		/// </summary>
		public Rune HLineHvDa4 { get; set; } = '┉';

		/// <summary>
		/// Box Drawings Heavy Double Dash Vertical (U+254F) - ╏
		/// </summary>
		public Rune VLineHvDa2 { get; set; } = '╏';

		/// <summary>
		/// Box Drawings Heavy Quadruple Dash Vertical (U+250B) - ┋
		/// </summary>
		public Rune VLineHvDa4 { get; set; } = '┋';

		/// <summary>
		/// Box Drawings Light Left (U+2574) - ╴
		/// </summary>
		public Rune HalfLeftLine { get; set; } = '╴';

		/// <summary>
		/// Box Drawings Light Up (U+2575) - ╵
		/// </summary>
		public Rune HalfTopLine { get; set; } = '╵';

		/// <summary>
		/// Box Drawings Light Right (U+2576) - ╶
		/// </summary>
		public Rune HalfRightLine { get; set; } = '╶';

		/// <summary>
		/// Box Drawings Light Down (U+2577) - ╷
		/// </summary>
		public Rune HalfBottomLine { get; set; } = '╷';

		/// <summary>
		/// Box Drawings Heavy Left (U+2578) - ╸
		/// </summary>
		public Rune HalfLeftLineHv { get; set; } = '╸';

		/// <summary>
		/// Box Drawings Heavy Up (U+2579) - ╹
		/// </summary>
		public Rune HalfTopLineHv { get; set; } = '╹';

		/// <summary>
		/// Box Drawings Heavy Right (U+257A) - ╺
		/// </summary>
		public Rune HalfRightLineHv { get; set; } = '╺';

		/// <summary>
		/// Box Drawings Light Up and Right (U+257B) - ╻
		/// </summary>
		public Rune HalfBottomLineLt { get; set; } = '╻';

		/// <summary>
		/// Box Drawings Light Horizontal and Heavy Right (U+257C) - ╼
		/// </summary>
		public Rune RightSideLineLtHv { get; set; } = '╼';

		/// <summary>
		/// Box Drawings Light Vertical and Heavy Right (U+257D) - ╽
		/// </summary>
		public Rune BottomSideLineLtHv { get; set; } = '╽';

		/// <summary>
		/// Box Drawings Heavy Left and Light Horizontal (U+257E) - ╾
		/// </summary>
		public Rune LeftSideLineHvLt { get; set; } = '╾';

		/// <summary>
		/// Box Drawings Heavy Up and Light Horizontal (U+257F) - ╿
		/// </summary>
		public Rune TopSideLineHvLt { get; set; } = '╿';
	}
}
