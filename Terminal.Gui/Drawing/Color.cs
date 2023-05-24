using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System;

namespace Terminal.Gui {
	/// <summary>
	/// Colors that can be used to set the foreground and background colors in console applications.
	/// </summary>
	/// <remarks>
	/// The <see cref="Attribute.HasValidColors"/> value indicates either no-color has been set or the color is invalid.
	/// </remarks>
	[JsonConverter (typeof (ColorJsonConverter))]
	public enum Color {
		/// <summary>
		/// The black color.
		/// </summary>
		Black,
		/// <summary>
		/// The blue color.
		/// </summary>
		Blue,
		/// <summary>
		/// The green color.
		/// </summary>
		Green,
		/// <summary>
		/// The cyan color.
		/// </summary>
		Cyan,
		/// <summary>
		/// The red color.
		/// </summary>
		Red,
		/// <summary>
		/// The magenta color.
		/// </summary>
		Magenta,
		/// <summary>
		/// The brown color.
		/// </summary>
		Brown,
		/// <summary>
		/// The gray color.
		/// </summary>
		Gray,
		/// <summary>
		/// The dark gray color.
		/// </summary>
		DarkGray,
		/// <summary>
		/// The bright bBlue color.
		/// </summary>
		BrightBlue,
		/// <summary>
		/// The bright green color.
		/// </summary>
		BrightGreen,
		/// <summary>
		/// The bright cyan color.
		/// </summary>
		BrightCyan,
		/// <summary>
		/// The bright red color.
		/// </summary>
		BrightRed,
		/// <summary>
		/// The bright magenta color.
		/// </summary>
		BrightMagenta,
		/// <summary>
		/// The bright yellow color.
		/// </summary>
		BrightYellow,
		/// <summary>
		/// The White color.
		/// </summary>
		White
	}

	/// <summary>
	/// Indicates the RGB for true colors.
	/// </summary>
	public class TrueColor {
		/// <summary>
		/// Red color component.
		/// </summary>
		public int Red { get; }
		/// <summary>
		/// Green color component.
		/// </summary>
		public int Green { get; }
		/// <summary>
		/// Blue color component.
		/// </summary>
		public int Blue { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TrueColor"/> struct.
		/// </summary>
		/// <param name="red"></param>
		/// <param name="green"></param>
		/// <param name="blue"></param>
		public TrueColor (int red, int green, int blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
		}

		/// <summary>
		/// Converts true color to console color.
		/// </summary>
		/// <returns></returns>
		public Color ToConsoleColor ()
		{
			var trueColorMap = new Dictionary<TrueColor, Color> () {
				{ new TrueColor (0,0,0),Color.Black},
				{ new TrueColor (0, 0, 0x80),Color.Blue},
				{ new TrueColor (0, 0x80, 0),Color.Green},
				{ new TrueColor (0, 0x80, 0x80),Color.Cyan},
				{ new TrueColor (0x80, 0, 0),Color.Red},
				{ new TrueColor (0x80, 0, 0x80),Color.Magenta},
				{ new TrueColor (0xC1, 0x9C, 0x00),Color.Brown},  // TODO confirm this
				{ new TrueColor (0xC0, 0xC0, 0xC0),Color.Gray},
				{ new TrueColor (0x80, 0x80, 0x80),Color.DarkGray},
				{ new TrueColor (0, 0, 0xFF),Color.BrightBlue},
				{ new TrueColor (0, 0xFF, 0),Color.BrightGreen},
				{ new TrueColor (0, 0xFF, 0xFF),Color.BrightCyan},
				{ new TrueColor (0xFF, 0, 0),Color.BrightRed},
				{ new TrueColor (0xFF, 0, 0xFF),Color.BrightMagenta },
				{ new TrueColor (0xFF, 0xFF, 0),Color.BrightYellow},
				{ new TrueColor (0xFF, 0xFF, 0xFF),Color.White},
				};
			// Iterate over all colors in the map
			var distances = trueColorMap.Select (
							k => Tuple.Create (
								// the candidate we are considering matching against (RGB)
								k.Key,

								CalculateDistance (k.Key, this)
							));

			// get the closest
			var match = distances.OrderBy (t => t.Item2).First ();
			return trueColorMap [match.Item1];
		}

		private float CalculateDistance (TrueColor color1, TrueColor color2)
		{
			// use RGB distance
			return
				Math.Abs (color1.Red - color2.Red) +
				Math.Abs (color1.Green - color2.Green) +
				Math.Abs (color1.Blue - color2.Blue);
		}
	}

	/// <summary>
	/// Attributes represent how text is styled when displayed in the terminal. 
	/// </summary>
	/// <remarks>
	///   <see cref="Attribute"/> provides a platform independent representation of colors (and someday other forms of text styling).
	///   They encode both the foreground and the background color and are used in the <see cref="ColorScheme"/>
	///   class to define color schemes that can be used in an application.
	/// </remarks>
	[JsonConverter (typeof (AttributeJsonConverter))]
	public struct Attribute {
		/// <summary>
		/// The <see cref="ConsoleDriver"/>-specific color value. If <see cref="Initialized"/> is <see langword="false"/> 
		/// the value of this property is invalid (typically because the Attribute was created before a driver was loaded)
		/// and the attribute should be re-made (see <see cref="Make(Color, Color)"/>) before it is used.
		/// </summary>
		[JsonIgnore (Condition = JsonIgnoreCondition.Always)]
		internal int Value { get; }

		/// <summary>
		/// The foreground color.
		/// </summary>
		[JsonConverter (typeof (ColorJsonConverter))]
		public Color Foreground { get; private init; }

		/// <summary>
		/// The background color.
		/// </summary>
		[JsonConverter (typeof (ColorJsonConverter))]
		public Color Background { get; private init; }

		/// <summary>
		/// Initializes a new instance with a platform-specific color value.
		/// </summary>
		/// <param name="value">Value.</param>
		internal Attribute (int value)
		{
			Color foreground = default;
			Color background = default;

			Initialized = false;
			if (Application.Driver != null) {
				Application.Driver.GetColors (value, out foreground, out background);
				Initialized = true;
			}
			Value = value;
			Foreground = foreground;
			Background = background;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="value">platform-dependent color value.</param>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (int value, Color foreground, Color background)
		{
			Value = value;
			Foreground = foreground;
			Background = background;
			Initialized = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (Color foreground = new Color (), Color background = new Color ())
		{
			var make = Make (foreground, background);
			Initialized = make.Initialized;
			Value = make.Value;
			Foreground = foreground;
			Background = background;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct
		///  with the same colors for the foreground and background.
		/// </summary>
		/// <param name="color">The color.</param>
		public Attribute (Color color) : this (color, color) { }
		
		/// <inheritdoc />
		public override bool Equals (object obj)
		{
			if (obj is Attribute other) {
				return this.Value == other.Value;
			}

			return false;
		}

		/// <summary>
		/// Compares two attributes for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator == (Attribute left, Attribute right) => left.Equals (right);

		/// <summary>
		/// Compares two attributes for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator != (Attribute left, Attribute right) => !(left == right);

		/// <inheritdoc />
		public override int GetHashCode () => this.Value.GetHashCode ();

		/// <summary>
		/// Creates an <see cref="Attribute"/> from the specified foreground and background colors.
		/// </summary>
		/// <remarks>
		/// If a <see cref="ConsoleDriver"/> has not been loaded (<c>Application.Driver == null</c>) this
		/// method will return an attribute with <see cref="Initialized"/> set to  <see langword="false"/>.
		/// </remarks>
		/// <returns>The new attribute.</returns>
		/// <param name="foreground">Foreground color to use.</param>
		/// <param name="background">Background color to use.</param>
		public static Attribute Make (Color foreground, Color background)
		{
			if (Application.Driver == null) {
				// Create the attribute, but show it's not been initialized
				return new Attribute () {
					Initialized = false,
					Foreground = foreground, 
					Background = background
				};
			}
			return Application.Driver.MakeAttribute (foreground, background);
		}

		/// <summary>
		/// Gets the current <see cref="Attribute"/> from the driver.
		/// </summary>
		/// <returns>The current attribute.</returns>
		public static Attribute Get ()
		{
			if (Application.Driver == null) {
				throw new InvalidOperationException ("The Application has not been initialized");
			}
			return Application.Driver.GetAttribute ();
		}

		/// <summary>
		/// If <see langword="true"/> the attribute has been initialized by a <see cref="ConsoleDriver"/> and 
		/// thus has <see cref="Value"/> that is valid for that driver. If <see langword="false"/> the <see cref="Foreground"/>
		/// and <see cref="Background"/> colors may have been set '-1' but
		/// the attribute has not been mapped to a <see cref="ConsoleDriver"/> specific color value.
		/// </summary>
		/// <remarks>
		/// Attributes that have not been initialized must eventually be initialized before being passed to a driver.
		/// </remarks>
		[JsonIgnore]
		public bool Initialized { get; internal set; }

		/// <summary>
		/// Returns <see langword="true"/> if the Attribute is valid (both foreground and background have valid color values).
		/// </summary>
		/// <returns></returns>
		[JsonIgnore]
		public bool HasValidColors => (int)Foreground > -1 && (int)Background > -1;

		/// <inheritdoc />
		public override string ToString ()
		{
			// Note, Unit tests are dependent on this format
			return $"{Foreground},{Background}";
		}
	}

	/// <summary>
	/// Defines the color <see cref="Attribute"/>s for common visible elements in a <see cref="View"/>. 
	/// Containers such as <see cref="Window"/> and <see cref="FrameView"/> use <see cref="ColorScheme"/> to determine
	/// the colors used by sub-views.
	/// </summary>
	/// <remarks>
	/// See also: <see cref="Colors.ColorSchemes"/>.
	/// </remarks>
	[JsonConverter (typeof (ColorSchemeJsonConverter))]
	public class ColorScheme : IEquatable<ColorScheme> {
		Attribute _normal = new Attribute (Color.White, Color.Black);
		Attribute _focus = new Attribute (Color.White, Color.Black);
		Attribute _hotNormal = new Attribute (Color.White, Color.Black);
		Attribute _hotFocus = new Attribute (Color.White, Color.Black);
		Attribute _disabled = new Attribute (Color.White, Color.Black);

		/// <summary>
		/// Used by <see cref="Colors.SetColorScheme(ColorScheme, string)"/> and <see cref="Colors.GetColorScheme(string)"/> to track which ColorScheme 
		/// is being accessed.
		/// </summary>
		internal string schemeBeingSet = "";

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ColorScheme () { }

		/// <summary>
		/// Creates a new instance, initialized with the values from <paramref name="scheme"/>.
		/// </summary>
		/// <param name="scheme">The scheme to initlize the new instance with.</param>
		public ColorScheme (ColorScheme scheme) : base ()
		{
			if (scheme != null) {
				_normal = scheme.Normal;
				_focus = scheme.Focus;
				_hotNormal = scheme.HotNormal;
				_disabled = scheme.Disabled;
				_hotFocus = scheme.HotFocus;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view is not focused, hot, or disabled.
		/// </summary>
		public Attribute Normal {
			get { return _normal; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_normal = value;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view has the focus.
		/// </summary>
		public Attribute Focus {
			get { return _focus; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_focus = value;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view is highlighted (hot).
		/// </summary>
		public Attribute HotNormal {
			get { return _hotNormal; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_hotNormal = value;
			}
		}

		/// <summary>
		/// The foreground and background color for text when the view is highlighted (hot) and has focus.
		/// </summary>
		public Attribute HotFocus {
			get { return _hotFocus; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_hotFocus = value;
			}
		}

		/// <summary>
		/// The default foreground and background color for text, when the view is disabled.
		/// </summary>
		public Attribute Disabled {
			get { return _disabled; }
			set {
				if (!value.HasValidColors) {
					return;
				}
				_disabled = value;
			}
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for equality.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>true if the two objects are equal</returns>
		public override bool Equals (object obj)
		{
			return Equals (obj as ColorScheme);
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for equality.
		/// </summary>
		/// <param name="other"></param>
		/// <returns>true if the two objects are equal</returns>
		public bool Equals (ColorScheme other)
		{
			return other != null &&
			       EqualityComparer<Attribute>.Default.Equals (_normal, other._normal) &&
			       EqualityComparer<Attribute>.Default.Equals (_focus, other._focus) &&
			       EqualityComparer<Attribute>.Default.Equals (_hotNormal, other._hotNormal) &&
			       EqualityComparer<Attribute>.Default.Equals (_hotFocus, other._hotFocus) &&
			       EqualityComparer<Attribute>.Default.Equals (_disabled, other._disabled);
		}

		/// <summary>
		/// Returns a hashcode for this instance.
		/// </summary>
		/// <returns>hashcode for this instance</returns>
		public override int GetHashCode ()
		{
			int hashCode = -1242460230;
			hashCode = hashCode * -1521134295 + _normal.GetHashCode ();
			hashCode = hashCode * -1521134295 + _focus.GetHashCode ();
			hashCode = hashCode * -1521134295 + _hotNormal.GetHashCode ();
			hashCode = hashCode * -1521134295 + _hotFocus.GetHashCode ();
			hashCode = hashCode * -1521134295 + _disabled.GetHashCode ();
			return hashCode;
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><c>true</c> if the two objects are equivalent</returns>
		public static bool operator == (ColorScheme left, ColorScheme right)
		{
			return EqualityComparer<ColorScheme>.Default.Equals (left, right);
		}

		/// <summary>
		/// Compares two <see cref="ColorScheme"/> objects for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><c>true</c> if the two objects are not equivalent</returns>
		public static bool operator != (ColorScheme left, ColorScheme right)
		{
			return !(left == right);
		}

		internal void Initialize ()
		{
			// If the new scheme was created before a driver was loaded, we need to re-make
			// the attributes
			if (!_normal.Initialized) {
				_normal = new Attribute (_normal.Foreground, _normal.Background);
			}
			if (!_focus.Initialized) {
				_focus = new Attribute (_focus.Foreground, _focus.Background);
			}
			if (!_hotNormal.Initialized) {
				_hotNormal = new Attribute (_hotNormal.Foreground, _hotNormal.Background);
			}
			if (!_hotFocus.Initialized) {
				_hotFocus = new Attribute (_hotFocus.Foreground, _hotFocus.Background);
			}
			if (!_disabled.Initialized) {
				_disabled = new Attribute (_disabled.Foreground, _disabled.Background);
			}
		}
	}

	/// <summary>
	/// The default <see cref="ColorScheme"/>s for the application.
	/// </summary>
	/// <remarks>
	/// This property can be set in a Theme to change the default <see cref="Colors"/> for the application.
	/// </remarks>
	public static class Colors {
		private class SchemeNameComparerIgnoreCase : IEqualityComparer<string> {
			public bool Equals (string x, string y)
			{
				if (x != null && y != null) {
					return string.Equals (x, y, StringComparison.InvariantCultureIgnoreCase);
				}
				return false;
			}

			public int GetHashCode (string obj)
			{
				return obj.ToLowerInvariant ().GetHashCode ();
			}
		}

		static Colors ()
		{
			ColorSchemes = Create ();
		}

		/// <summary>
		/// Creates a new dictionary of new <see cref="ColorScheme"/> objects.
		/// </summary>
		public static Dictionary<string, ColorScheme> Create ()
		{
			// Use reflection to dynamically create the default set of ColorSchemes from the list defined 
			// by the class. 
			return typeof (Colors).GetProperties ()
				.Where (p => p.PropertyType == typeof (ColorScheme))
				.Select (p => new KeyValuePair<string, ColorScheme> (p.Name, new ColorScheme ()))
				.ToDictionary (t => t.Key, t => t.Value, comparer: new SchemeNameComparerIgnoreCase ());
		}

		/// <summary>
		/// The application Toplevel color scheme, for the default Toplevel views.
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["TopLevel"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme TopLevel { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The base color scheme, for the default Toplevel views.
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Base"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Base { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The dialog color scheme, for standard popup dialog boxes
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Dialog"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Dialog { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The menu bar color
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Menu"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Menu { get => GetColorScheme (); set => SetColorScheme (value); }

		/// <summary>
		/// The color scheme for showing errors.
		/// </summary>
		/// <remarks>
		/// <para>
		///	This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g. <c>edit.ColorScheme = Colors.ColorSchemes["Error"];</c>
		/// </para>
		/// </remarks>
		public static ColorScheme Error { get => GetColorScheme (); set => SetColorScheme (value); }

		static ColorScheme GetColorScheme ([CallerMemberName] string schemeBeingSet = null)
		{
			return ColorSchemes [schemeBeingSet];
		}

		static void SetColorScheme (ColorScheme colorScheme, [CallerMemberName] string schemeBeingSet = null)
		{
			ColorSchemes [schemeBeingSet] = colorScheme;
			colorScheme.schemeBeingSet = schemeBeingSet;
		}

		/// <summary>
		/// Provides the defined <see cref="ColorScheme"/>s.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
		[JsonConverter (typeof (DictionaryJsonConverter<ColorScheme>))]
		public static Dictionary<string, ColorScheme> ColorSchemes { get; private set; }
	}

}
