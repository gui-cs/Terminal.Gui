using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
/// Defines the <see cref="Attribute"/>s for common visible elements in a <see cref="View"/>.
/// Containers such as <see cref="Window"/> and <see cref="FrameView"/> use <see cref="ColorScheme"/> to determine
/// the colors used by sub-views.
/// </summary>
/// <remarks>
/// See also: <see cref="Colors.ColorSchemes"/>.
/// </remarks>
[JsonConverter (typeof (ColorSchemeJsonConverter))]
public class ColorScheme : IEquatable<ColorScheme> {
	Attribute _disabled = Attribute.Default;
	Attribute _focus = Attribute.Default;
	Attribute _hotFocus = Attribute.Default;
	Attribute _hotNormal = Attribute.Default;
	Attribute _normal = Attribute.Default;

	/// <summary>
	/// Used by <see cref="Colors.SetColorScheme(ColorScheme, string)"/> and <see cref="Colors.GetColorScheme(string)"/> to
	/// track which ColorScheme
	/// is being accessed.
	/// </summary>
	internal string _schemeBeingSet = "";

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	public ColorScheme () : this (Attribute.Default) { }

	/// <summary>
	/// Creates a new instance, initialized with the values from <paramref name="scheme"/>.
	/// </summary>
	/// <param name="scheme">The scheme to initialize the new instance with.</param>
	public ColorScheme (ColorScheme scheme)
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
	/// Creates a new instance, initialized with the values from <paramref name="attribute"/>.
	/// </summary>
	/// <param name="attribute">The attribute to initialize the new instance with.</param>
	public ColorScheme (Attribute attribute)
	{
		_normal = attribute;
		_focus = attribute;
		_hotNormal = attribute;
		_disabled = attribute;
		_hotFocus = attribute;
	}

	/// <summary>
	/// The foreground and background color for text when the view is not focused, hot, or disabled.
	/// </summary>
	public Attribute Normal {
		get => _normal;
		set => _normal = value;
	}

	/// <summary>
	/// The foreground and background color for text when the view has the focus.
	/// </summary>
	public Attribute Focus {
		get => _focus;
		set => _focus = value;
	}

	/// <summary>
	/// The foreground and background color for text in a non-focused view that indicates a <see cref="View.HotKey"/>.
	/// </summary>
	public Attribute HotNormal {
		get => _hotNormal;
		set => _hotNormal = value;
	}

	/// <summary>
	/// The foreground and background color for for text in a focused view that indicates a <see cref="View.HotKey"/>.
	/// </summary>
	public Attribute HotFocus {
		get => _hotFocus;
		set => _hotFocus = value;
	}

	/// <summary>
	/// The default foreground and background color for text when the view is disabled.
	/// </summary>
	public Attribute Disabled {
		get => _disabled;
		set => _disabled = value;
	}

	/// <summary>
	/// Compares two <see cref="ColorScheme"/> objects for equality.
	/// </summary>
	/// <param name="other"></param>
	/// <returns>true if the two objects are equal</returns>
	public bool Equals (ColorScheme other) => other != null &&
	                                          EqualityComparer<Attribute>.Default.Equals (_normal, other._normal) &&
	                                          EqualityComparer<Attribute>.Default.Equals (_focus, other._focus) &&
	                                          EqualityComparer<Attribute>.Default.Equals (_hotNormal, other._hotNormal) &&
	                                          EqualityComparer<Attribute>.Default.Equals (_hotFocus, other._hotFocus) &&
	                                          EqualityComparer<Attribute>.Default.Equals (_disabled, other._disabled);

	/// <summary>
	/// Compares two <see cref="ColorScheme"/> objects for equality.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns>true if the two objects are equal</returns>
	public override bool Equals (object obj) => Equals (obj as ColorScheme);

	/// <summary>
	/// Returns a hashcode for this instance.
	/// </summary>
	/// <returns>hashcode for this instance</returns>
	public override int GetHashCode ()
	{
		var hashCode = -1242460230;
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
	public static bool operator == (ColorScheme left, ColorScheme right) => EqualityComparer<ColorScheme>.Default.Equals (left, right);

	/// <summary>
	/// Compares two <see cref="ColorScheme"/> objects for inequality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns><c>true</c> if the two objects are not equivalent</returns>
	public static bool operator != (ColorScheme left, ColorScheme right) => !(left == right);
}

/// <summary>
/// The default <see cref="ColorScheme"/>s for the application.
/// </summary>
/// <remarks>
/// This property can be set in a Theme to change the default <see cref="Colors"/> for the application.
/// </remarks>
public static class Colors {

	static Colors () => ColorSchemes = Create ();

	/// <summary>
	/// The application Toplevel color scheme, for the default Toplevel views.
	/// </summary>
	/// <remarks>
	///         <para>
	///         This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g.
	///         <c>edit.ColorScheme = Colors.ColorSchemes["TopLevel"];</c>
	///         </para>
	/// </remarks>
	public static ColorScheme TopLevel { get => GetColorScheme (); set => SetColorScheme (value); }

	/// <summary>
	/// The base color scheme, for the default Toplevel views.
	/// </summary>
	/// <remarks>
	///         <para>
	///         This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g.
	///         <c>edit.ColorScheme = Colors.ColorSchemes["Base"];</c>
	///         </para>
	/// </remarks>
	public static ColorScheme Base { get => GetColorScheme (); set => SetColorScheme (value); }

	/// <summary>
	/// The dialog color scheme, for standard popup dialog boxes
	/// </summary>
	/// <remarks>
	///         <para>
	///         This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g.
	///         <c>edit.ColorScheme = Colors.ColorSchemes["Dialog"];</c>
	///         </para>
	/// </remarks>
	public static ColorScheme Dialog { get => GetColorScheme (); set => SetColorScheme (value); }

	/// <summary>
	/// The menu bar color
	/// </summary>
	/// <remarks>
	///         <para>
	///         This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g.
	///         <c>edit.ColorScheme = Colors.ColorSchemes["Menu"];</c>
	///         </para>
	/// </remarks>
	public static ColorScheme Menu { get => GetColorScheme (); set => SetColorScheme (value); }

	/// <summary>
	/// The color scheme for showing errors.
	/// </summary>
	/// <remarks>
	///         <para>
	///         This API will be deprecated in the future. Use <see cref="Colors.ColorSchemes"/> instead (e.g.
	///         <c>edit.ColorScheme = Colors.ColorSchemes["Error"];</c>
	///         </para>
	/// </remarks>
	public static ColorScheme Error { get => GetColorScheme (); set => SetColorScheme (value); }

	/// <summary>
	/// Provides the defined <see cref="ColorScheme"/>s.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
	[JsonConverter (typeof (DictionaryJsonConverter<ColorScheme>))]
	public static Dictionary<string, ColorScheme> ColorSchemes { get; private set; } // Serialization requires this to have a setter (private set;)

	/// <summary>
	/// Creates a new dictionary of new <see cref="ColorScheme"/> objects.
	/// </summary>
	public static Dictionary<string, ColorScheme> Create () =>
		// Use reflection to dynamically create the default set of ColorScheme names (e.g. "TopLevel", "Base", etc.)
		// from the list defined by the class. 
		typeof (Colors).GetProperties ()
			.Where (p => p.PropertyType == typeof (ColorScheme))
			.Select (p => new KeyValuePair<string, ColorScheme> (p.Name, new ColorScheme ()))
			.ToDictionary (t => t.Key, t => t.Value, new SchemeNameComparerIgnoreCase ());

	static ColorScheme GetColorScheme ([CallerMemberName] string schemeBeingSet = null) => ColorSchemes [schemeBeingSet];

	static void SetColorScheme (ColorScheme colorScheme, [CallerMemberName] string schemeBeingSet = null)
	{
		ColorSchemes [schemeBeingSet] = colorScheme;
		colorScheme._schemeBeingSet = schemeBeingSet;
	}

	class SchemeNameComparerIgnoreCase : IEqualityComparer<string> {
		public bool Equals (string x, string y)
		{
			if (x != null && y != null) {
				return string.Equals (x, y, StringComparison.InvariantCultureIgnoreCase);
			}
			return false;
		}

		public int GetHashCode (string obj) => obj.ToLowerInvariant ().GetHashCode ();
	}
}