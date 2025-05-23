#nullable enable
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Represents the visual styling for a UI element, including foreground and background color and text style.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Attribute"/> is a lightweight, immutable struct used to define how visual elements are rendered
///         in a terminal UI. It wraps color and style information in a platform-independent way and is used
///         extensively in <see cref="Scheme"/>, <see cref="VisualRole"/>, and theming infrastructure.
///     </para>
///     <para>
///         The <see cref="IsExplicitlySet"/> flag is used internally to determine whether the attribute was set directly
///         or is derived through inheritance logic. This affects how <see cref="Scheme.GetAttributeForRole(VisualRole)"/>
///         resolves values for different roles.
///     </para>
/// </remarks>
/// <seealso cref="Color"/>
/// <seealso cref="TextStyle"/>
/// <seealso cref="VisualRole"/>
/// <seealso cref="Scheme"/>
[JsonConverter (typeof (AttributeJsonConverter))]
public readonly record struct Attribute : IEqualityOperators<Attribute, Attribute, bool>
{
    /// <summary>Default empty attribute.</summary>
    [JsonIgnore]
    public static Attribute Default => new (Color.White, Color.Black);

    /// <summary>
    ///     INTERNAL: Indicates whether this attribute was explicitly set or is a default/derived value.
    ///     Used internally by <see cref="Scheme"/> to determine which attributes should be inherited.
    /// </summary>
    [JsonIgnore (Condition = JsonIgnoreCondition.Always)]
    internal bool IsExplicitlySet { get; init; }

    // TODO: Once CursesDriver is dead, remove this property
    /// <summary>INTERNAL: The <see cref="IConsoleDriver"/>-specific color value.</summary>
    [JsonIgnore (Condition = JsonIgnoreCondition.Always)]
    internal int PlatformColor { get; init; }

    /// <summary>
    /// Gets the foreground <see cref="Color"/> used to render text.
    /// </summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Foreground { get; init; }

    /// <summary>
    /// Gets the background <see cref="Color"/> used behind text.
    /// </summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Background { get; init; }

    // TODO: Add constructors which permit including a Style.
    /// <summary>
    /// Gets the <see cref="TextStyle"/> (e.g., bold, underline, italic) applied to text.
    /// </summary>
    public TextStyle Style { get; init; } = TextStyle.None;

    /// <summary>
    /// Initializes a new instance of the <see cref="Attribute"/> struct with default values.
    /// </summary>
    public Attribute () { this = Default with { PlatformColor = -1, IsExplicitlySet = false }; }

    /// <summary>
    /// Initializes a new <see cref="Attribute"/> from an existing instance, preserving explicit state.
    /// </summary>
    public Attribute (in Attribute attr) { this = attr with { PlatformColor = -1, IsExplicitlySet = attr.IsExplicitlySet }; }

    /// <summary>INTERNAL: Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="platformColor">platform-dependent color value.</param>
    /// <param name="foreground">Foreground</param>
    /// <param name="background">Background</param>
    internal Attribute (int platformColor, Color foreground, Color background)
    {
        Foreground = foreground;
        Background = background;
        PlatformColor = platformColor;
        IsExplicitlySet = true;
        Style = TextStyle.None;
    }

    /// <summary>
    /// Initializes an instance using two named colors.
    /// </summary>
    public Attribute (in Color foreground, in Color background)
    {
        Foreground = foreground;
        Background = background;
        IsExplicitlySet = true;

        // TODO: Once CursesDriver supports true color all the PlatformColor stuff goes away
        PlatformColor = Application.Driver?.MakeColor (in foreground, in background).PlatformColor ?? -1;
        Style = TextStyle.None;
    }

    /// <summary>
    /// Initializes a new instance with foreground, background, and <see cref="TextStyle"/>.
    /// </summary>
    public Attribute (in Color foreground, in Color background, in TextStyle style)
    {
        Foreground = foreground;
        Background = background;
        Style = style;
        IsExplicitlySet = true;

        // TODO: Once CursesDriver supports true color all the PlatformColor stuff goes away
        PlatformColor = Application.Driver?.MakeColor (in foreground, in background).PlatformColor ?? -1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Attribute"/> struct from string representations of colors and style.
    /// </summary>
    /// <param name="foreground">Foreground color as a string (name, hex, or rgb).</param>
    /// <param name="background">Background color as a string (name, hex, or rgb).</param>
    /// <param name="style">Optional style as a string (e.g., "Bold,Underline").</param>
    /// <exception cref="ArgumentException">Thrown if color parsing fails.</exception>
    public Attribute (string foreground, string background, string? style = null)
    {
        Foreground = Color.Parse (foreground);
        Background = Color.Parse (background);
        Style = style is { } && Enum.TryParse<TextStyle> (style, true, out var parsedStyle)
                    ? parsedStyle
                    : TextStyle.None;
        IsExplicitlySet = true;
        PlatformColor = Application.Driver?.MakeColor (Foreground, Background).PlatformColor ?? -1;
    }


    /// <summary>
    ///     INTERNAL: Initializes a new instance with a <see cref="ColorName16"/> value. Both <see cref="Foreground"/> and
    ///     <see cref="Background"/> will be set to the specified color.
    /// </summary>
    /// <param name="colorName">Value.</param>
    internal Attribute (in ColorName16 colorName) : this (in colorName, in colorName) { }

    /// <summary>
    /// Initializes a new instance with foreground and background colors.
    /// </summary>
    public Attribute (in ColorName16 foregroundName, in ColorName16 backgroundName)
        : this (new Color (in foregroundName), new Color (in backgroundName))
    { }

    /// <summary>
    /// Initializes a new instance with foreground and background colors.
    /// </summary>
    public Attribute (in ColorName16 foregroundName, in Color background) : this (new Color (in foregroundName), in background) { }

    /// <summary>
    /// Initializes a new instance with foreground and background colors.
    /// </summary>
    public Attribute (in Color foreground, in ColorName16 backgroundName) : this (in foreground, new Color (in backgroundName)) { }

    /// <summary>
    /// Initializes an instance using a single color for both foreground and background.
    /// </summary>
    public Attribute (in Color color) : this (color, color) { }

    /// <summary>
    ///     INTERNAL: Creates a version of this attribute marked as explicitly set.
    /// </summary>
    /// <returns>A copy of this attribute with IsExplicitlySet = true.</returns>
    internal Attribute AsExplicitlySet () { return this with { IsExplicitlySet = true }; }

    /// <summary>
    ///     INTERNAL: Creates a version of this attribute marked as not explicitly set (implicit/derived).
    /// </summary>
    /// <returns>A copy of this attribute with IsExplicitlySet = false.</returns>
    internal Attribute AsImplicit () { return this with { IsExplicitlySet = false }; }

    /// <inheritdoc/>
    public bool Equals (Attribute other)
    {
        return PlatformColor == other.PlatformColor
               && Foreground.Equals (other.Foreground)
               && Background.Equals (other.Background)
               && Style == other.Style;

        // ❌ do not include IsExplicitlySet
    }

    /// <inheritdoc/>
    public override int GetHashCode ()
    {
        return HashCode.Combine (PlatformColor, Foreground, Background, Style);

        // ❌ do not include IsExplicitlySet
    }

    /// <inheritdoc/>
    public override string ToString ()
    {
        // Note: Unit tests are dependent on this format
        return $"[{Foreground},{Background},{Style}]";
    }
}
