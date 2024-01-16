using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
/// Defines a standard set of <see cref="Attribute"/>s for common visible elements in a <see cref="View"/>.
/// </summary>
/// <remarks>
/// <para>
/// ColorScheme objects are immutable. Once constructed, the properties cannot be changed.
/// To change a ColorScheme, create a new one with the desired values,
/// using the <see cref="ColorScheme(ColorScheme)"/> constructor.
/// </para>
/// <para>
/// See also: <see cref="Colors.ColorSchemes"/>.
/// </para>
/// </remarks>
[JsonConverter (typeof (ColorSchemeJsonConverter))]
public class ColorScheme : IEquatable<ColorScheme> {
	readonly Attribute _disabled = Attribute.Default;
	readonly Attribute _focus = Attribute.Default;
	readonly Attribute _hotFocus = Attribute.Default;
	readonly Attribute _hotNormal = Attribute.Default;
	readonly Attribute _normal = Attribute.Default;

	/// <summary>
	/// Creates a new instance set to the default colors (see <see cref="Attribute.Default"/>).
	/// </summary>
	public ColorScheme () : this (Attribute.Default) { }

	/// <summary>
	/// Creates a new instance, initialized with the values from <paramref name="scheme"/>.
	/// </summary>
	/// <param name="scheme">The scheme to initialize the new instance with.</param>
	public ColorScheme (ColorScheme scheme)
	{
		if (scheme == null) {
			throw new ArgumentNullException (nameof (scheme));
		}
		_normal = scheme.Normal;
		_focus = scheme.Focus;
		_hotNormal = scheme.HotNormal;
		_disabled = scheme.Disabled;
		_hotFocus = scheme.HotFocus;
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
		init => _normal = value;
	}

	/// <summary>
	/// The foreground and background color for text when the view has the focus.
	/// </summary>
	public Attribute Focus {
		get => _focus;
		init => _focus = value;
	}

	/// <summary>
	/// The foreground and background color for text in a non-focused view that indicates a <see cref="View.HotKey"/>.
	/// </summary>
	public Attribute HotNormal {
		get => _hotNormal;
		init => _hotNormal = value;
	}

	/// <summary>
	/// The foreground and background color for for text in a focused view that indicates a <see cref="View.HotKey"/>.
	/// </summary>
	public Attribute HotFocus {
		get => _hotFocus;
		init => _hotFocus = value;
	}

	/// <summary>
	/// The default foreground and background color for text when the view is disabled.
	/// </summary>
	public Attribute Disabled {
		get => _disabled;
		init => _disabled = value;
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
	public override bool Equals (object obj) => Equals (obj is ColorScheme ? (ColorScheme)obj : default);

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
/// Holds the <see cref="ColorScheme"/>s that define the <see cref="Attribute"/>s that are used by views to render themselves.
/// </summary>
public static class Colors {
	static Colors () => Reset ();
	/// <summary>
	/// Gets a dictionary of defined <see cref="ColorScheme"/> objects.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <see cref="ColorSchemes"/> dictionary includes the following keys, by default:
	/// <list type="table">
	/// <listheader>
	///         <term>Built-in Color Scheme</term>
	///         <description>Description</description>
	/// </listheader>
	/// <item>
	///         <term>
	///         Base
	///         </term>
	///         <description>
	///         The base color scheme used for most Views.
	///         </description>
	/// </item>
	/// <item>
	///         <term>
	///         TopLevel
	///         </term>
	///         <description>
	///         The application Toplevel color scheme; used for the <see cref="Toplevel"/> View.
	///         </description>
	/// </item>
	/// <item>
	///         <term>
	///         Dialog
	///         </term>
	///         <description>
	///         The dialog color scheme; used for <see cref="Dialog"/>, <see cref="MessageBox"/>, and other views dialog-like views.
	///         </description>
	/// </item> 
	/// <item>
	///         <term>
	///         Menu
	///         </term>
	///         <description>
	///         The menu color scheme; used for <see cref="MenuBar"/>, <see cref="ContextMenu"/>, and <see cref="StatusBar"/>. 
	///         </description>
	/// </item>
	/// <item>
	///         <term>
	///         Error
	///         </term>
	///         <description>
	///         The color scheme for showing errors, such as in <see cref="MessageBox.ErrorQuery(string, string, string[])"/>. 
	///         </description>
	/// </item>
	/// </list>
	/// </para>
	/// <para>
	/// Changing the values of an entry in this dictionary will affect all views that use the scheme.
	/// </para>
	/// <para>
	/// <see cref="ConfigurationManager"/> can be used to override the default values for these schemes and add additional schemes.
	/// See <see cref="ConfigurationManager.Themes"/>.
	/// </para>
	/// </remarks>
	[SerializableConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
	[JsonConverter (typeof (DictionaryJsonConverter<ColorScheme>))]
	public static Dictionary<string, ColorScheme> ColorSchemes { get; private set; } // Serialization requires this to have a setter (private set;)

	/// <summary>
	/// Resets the <see cref="ColorSchemes"/> dictionary to the default values.
	/// </summary>
	public static Dictionary<string, ColorScheme> Reset () =>
		ColorSchemes = new Dictionary<string, ColorScheme> (comparer: new SchemeNameComparerIgnoreCase ()) {
			{ "TopLevel", new ColorScheme () },
			{ "Base", new ColorScheme () },
			{ "Dialog", new ColorScheme () },
			{ "Menu", new ColorScheme () },
			{ "Error", new ColorScheme () },
		};

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