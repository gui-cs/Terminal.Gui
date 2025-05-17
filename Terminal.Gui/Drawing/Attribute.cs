#nullable enable
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Attributes are a platform independent representation of colors and text styling.</summary>
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
    /// Indicates whether this attribute was explicitly set or is a default/derived value.
    /// Used internally by <see cref="Scheme"/> to determine which attributes should be inherited.
    /// </summary>
    [JsonIgnore (Condition = JsonIgnoreCondition.Always)]
    internal bool IsExplicitlySet { get; init; }

    // TODO: Once CursesDriver is dead, remove this property
    /// <summary>The <see cref="IConsoleDriver"/>-specific color value.</summary>
    [JsonIgnore (Condition = JsonIgnoreCondition.Always)]
    internal int PlatformColor { get; init; }

    /// <summary>The foreground color.</summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Foreground { get; init; }

    /// <summary>The background color.</summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Background { get; init; }

    // TODO: Add constructors which permit including a Style.
    /// <summary>The style (bold, italic, underlined, etc.).</summary>
    public TextStyle Style { get; init; } = TextStyle.None;

    /// <summary>Initializes a new instance with default values.</summary>
    public Attribute ()
    {
        this = Default with { PlatformColor = -1, IsExplicitlySet = false };
    }

    /// <summary>Initializes a new instance from an existing instance.</summary>
    public Attribute (in Attribute attr)
    {
        this = attr with { PlatformColor = -1, IsExplicitlySet = attr.IsExplicitlySet };
    }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
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

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="foreground">Foreground</param>
    /// <param name="background">Background</param>
    public Attribute (in Color foreground, in Color background)
    {
        Foreground = foreground;
        Background = background;
        IsExplicitlySet = true;

        // TODO: Once CursesDriver supports true color all the PlatformColor stuff goes away
        PlatformColor = Application.Driver?.MakeColor (in foreground, in background).PlatformColor ?? -1;
        Style = TextStyle.None;
    }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="foreground">Foreground</param>
    /// <param name="background">Background</param>
    /// <param name="style">Text Style</param>
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
    ///     Initializes a new instance with a <see cref="ColorName16"/> value. Both <see cref="Foreground"/> and
    ///     <see cref="Background"/> will be set to the specified color.
    /// </summary>
    /// <param name="colorName">Value.</param>
    internal Attribute (in ColorName16 colorName) : this (in colorName, in colorName) { }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="foregroundName">Foreground</param>
    /// <param name="backgroundName">Background</param>
    public Attribute (in ColorName16 foregroundName, in ColorName16 backgroundName)
        : this (new Color (in foregroundName), new Color (in backgroundName))
    { }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="foregroundName">Foreground</param>
    /// <param name="background">Background</param>
    public Attribute (in ColorName16 foregroundName, in Color background) : this (new Color (in foregroundName), in background) { }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="foreground">Foreground</param>
    /// <param name="backgroundName">Background</param>
    public Attribute (in Color foreground, in ColorName16 backgroundName) : this (in foreground, new Color (in backgroundName)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Attribute"/> struct with the same colors for the foreground and
    ///     background.
    /// </summary>
    /// <param name="color">The color.</param>
    public Attribute (in Color color) : this (color, color) { }

    /// <summary>
    /// Creates a version of this attribute marked as explicitly set.
    /// </summary>
    /// <returns>A copy of this attribute with IsExplicitlySet = true.</returns>
    internal Attribute AsExplicitlySet ()
    {
        return this with { IsExplicitlySet = true };
    }

    /// <summary>
    /// Creates a version of this attribute marked as not explicitly set (implicit/derived).
    /// </summary>
    /// <returns>A copy of this attribute with IsExplicitlySet = false.</returns>
    internal Attribute AsImplicit ()
    {
        return this with { IsExplicitlySet = false };
    }

    /// <inheritdoc />
    public bool Equals (Attribute other)
    {
        return PlatformColor == other.PlatformColor
               && Foreground.Equals (other.Foreground)
               && Background.Equals (other.Background)
               && Style == other.Style;
        // ❌ do not include IsExplicitlySet
    }

    /// <inheritdoc />
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
