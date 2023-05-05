using System.Text.Json.Serialization;
using static Terminal.Gui.ConfigurationManager;
using Rune = System.Rune;

namespace Terminal.Gui {
	/// <summary>
	/// Defines the standard set of glyph characters that can be used to draw checkboxes, lines, borders, etc...
	/// </summary>
	public static class Glyphs {

		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HotKeySpecifier { get; set; } = '_';

		/// <summary>
		/// Horizontal line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HLine { get; set; } = '\u2500';

		/// <summary>
		/// Vertical line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune VLine { get; set; } = '\u2502';

		/// <summary>
		/// Stipple pattern
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune Stipple { get; set; } = '\u2591';

		/// <summary>
		/// Diamond character
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune Diamond { get; set; } = '\u25ca';

		/// <summary>
		/// Upper left corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ULCorner { get; set; } = '\u250C';

		/// <summary>
		/// Lower left corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LLCorner { get; set; } = '\u2514';

		/// <summary>
		/// Upper right corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune URCorner { get; set; } = '\u2510';

		/// <summary>
		/// Lower right corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LRCorner { get; set; } = '\u2518';

		/// <summary>
		/// Left tee
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LeftTee { get; set; } = '\u251c';

		/// <summary>
		/// Right tee
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune RightTee { get; set; } = '\u2524';

		/// <summary>
		/// Top tee
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune TopTee { get; set; } = '\u252c';

		/// <summary>
		/// The bottom tee.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune BottomTee { get; set; } = '\u2534';

		/// <summary>
		/// Checkmark.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune Checked { get; set; } = '\u221a';

		/// <summary>
		/// Un-checked checkmark.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune UnChecked { get; set; } = '\u2574';

		/// <summary>
		/// Null-checked checkmark.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune NullChecked { get; set; } = '\u2370';

		/// <summary>
		/// Selected mark.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune Selected { get; set; } = '\u25cf';

		/// <summary>
		/// Un-selected selected mark.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune UnSelected { get; set; } = '\u25cc';

		/// <summary>
		/// Right Arrow.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune RightArrow { get; set; } = '\u25ba';

		/// <summary>
		/// Left Arrow.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LeftArrow { get; set; } = '\u25c4';

		/// <summary>
		/// Down Arrow.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune DownArrow { get; set; } = '\u25bc';

		/// <summary>
		/// Up Arrow.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune UpArrow { get; set; } = '\u25b2';

		/// <summary>
		/// Left indicator for default action (e.g. for <see cref="Button"/>).
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LeftDefaultIndicator { get; set; } = '\u25e6';

		/// <summary>
		/// Right indicator for default action (e.g. for <see cref="Button"/>).
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune RightDefaultIndicator { get; set; } = '\u25e6';

		/// <summary>
		/// Left frame/bracket (e.g. '[' for <see cref="Button"/>).
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LeftBracket { get; set; } = '[';

		/// <summary>
		/// Right frame/bracket (e.g. ']' for <see cref="Button"/>).
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune RightBracket { get; set; } = ']';

		/// <summary>
		/// Blocks Segment indicator for meter views (e.g. <see cref="ProgressBar"/>.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune BlocksMeterSegment { get; set; } = '\u258c';

		/// <summary>
		/// Continuous Segment indicator for meter views (e.g. <see cref="ProgressBar"/>.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ContinuousMeterSegment { get; set; } = '\u2588';

		/// <summary>
		/// Horizontal double line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HDbLine { get; set; } = '\u2550';

		/// <summary>
		/// Vertical double line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune VDbLine { get; set; } = '\u2551';

		/// <summary>
		/// Upper left double corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ULDbCorner { get; set; } = '\u2554';

		/// <summary>
		/// Lower left double corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LLDbCorner { get; set; } = '\u255a';

		/// <summary>
		/// Upper right double corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune URDbCorner { get; set; } = '\u2557';

		/// <summary>
		/// Lower right double corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LRDbCorner { get; set; } = '\u255d';

		/// <summary>
		/// Upper left rounded corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ULRCorner { get; set; } = '\u256d';

		/// <summary>
		/// Lower left rounded corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LLRCorner { get; set; } = '\u2570';

		/// <summary>
		/// Upper right rounded corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune URRCorner { get; set; } = '\u256e';

		/// <summary>
		/// Lower right rounded corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LRRCorner { get; set; } = '\u256f';

		/// <summary>
		/// Horizontal double dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HDsLine { get; set; } = '\u254c';

		/// <summary>
		/// Vertical triple dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune VDsLine { get; set; } = '\u2506';

		/// <summary>
		/// Horizontal triple dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HDtLine { get; set; } = '\u2504';

		/// <summary>
		/// Horizontal quadruple dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HD4Line { get; set; } = '\u2508';

		/// <summary>
		/// Vertical double dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune VD2Line { get; set; } = '\u254e';

		/// <summary>
		/// Vertical quadruple dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune VDtLine { get; set; } = '\u250a';

		/// <summary>
		/// Horizontal heavy line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HThLine { get; set; } = '\u2501';

		/// <summary>
		/// Vertical heavy line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune VThLine { get; set; } = '\u2503';

		/// <summary>
		/// Upper left heavy corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ULThCorner { get; set; } = '\u250f';

		/// <summary>
		/// Lower left heavy corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LLThCorner { get; set; } = '\u2517';

		/// <summary>
		/// Upper right heavy corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune URThCorner { get; set; } = '\u2513';

		/// <summary>
		/// Lower right heavy corner
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune LRThCorner { get; set; } = '\u251b';

		/// <summary>
		/// Horizontal heavy double dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HThDsLine { get; set; } = '\u254d';

		/// <summary>
		/// Vertical heavy triple dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune VThDsLine { get; set; } = '\u2507';

		/// <summary>
		/// Horizontal heavy triple dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HThDtLine { get; set; } = '\u2505';

		/// <summary>
		/// Horizontal heavy quadruple dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HThD4Line { get; set; } = '\u2509';

		/// <summary>
		/// Vertical heavy double dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune VThD2Line { get; set; } = '\u254f';

		/// <summary>
		/// Vertical heavy quadruple dashed line character.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune VThDtLine { get; set; } = '\u250b';

		/// <summary>
		/// The left half line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HalfLeftLine { get; set; } = '\u2574';

		/// <summary>
		/// The up half line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HalfTopLine { get; set; } = '\u2575';

		/// <summary>
		/// The right half line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HalfRightLine { get; set; } = '\u2576';

		/// <summary>
		/// The down half line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune HalfBottomLine { get; set; } = '\u2577';

		/// <summary>
		/// The heavy left half line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ThHalfLeftLine { get; set; } = '\u2578';

		/// <summary>
		/// The heavy up half line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ThHalfTopLine { get; set; } = '\u2579';

		/// <summary>
		/// The heavy right half line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ThHalfRightLine { get; set; } = '\u257a';

		/// <summary>
		/// The heavy light down half line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ThHalfBottomLine { get; set; } = '\u257b';

		/// <summary>
		/// The light left and heavy right line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ThRightSideLine { get; set; } = '\u257c';

		/// <summary>
		/// The light up and heavy down line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ThBottomSideLine { get; set; } = '\u257d';

		/// <summary>
		/// The heavy left and light right line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ThLeftSideLine { get; set; } = '\u257e';

		/// <summary>
		/// The heavy up and light down line.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope)), JsonConverter (typeof (RuneJsonConverter))]
		public static Rune ThTopSideLine { get; set; } = '\u257f';
	}
}
