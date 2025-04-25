#nullable enable
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;


// TODO: Add support for other attributes (bold, underline, etc.) once the platform drivers support them.
// TODO: See https://github.com/gui-cs/Terminal.Gui/issues/457


/// <summary>Attributes represent how text is styled when displayed in the terminal.</summary>
/// <remarks>
///     <see cref="Attribute"/> provides a platform independent representation of colors (and someday other forms of
///     text styling). They encode both the foreground and the background color and are used in the
///     <see cref="ColorScheme"/> class to define color schemes that can be used in an application.
/// </remarks>
[JsonConverter (typeof (AttributeJsonConverter))]
public readonly record struct Attribute : IEqualityOperators<Attribute, Attribute, bool>
{
    /// <summary>Default empty attribute.</summary>
    [JsonIgnore]
    public static Attribute Default => new (Color.White, Color.Black);

    /// <summary>The <see cref="IConsoleDriver"/>-specific color value.</summary>
    [JsonIgnore (Condition = JsonIgnoreCondition.Always)]
    internal int PlatformColor { get; init; }

    /// <summary>The foreground color.</summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Foreground { get; }

    /// <summary>The background color.</summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Background { get; }

    /// <summary>Initializes a new instance with default values.</summary>
    public Attribute ()
    {
        this = Default with { PlatformColor = -1 };
    }

    /// <summary>Initializes a new instance from an existing instance.</summary>
    public Attribute (in Attribute attr)
    {
        this = attr with { PlatformColor = -1 };
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
    }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="foreground">Foreground</param>
    /// <param name="background">Background</param>
    public Attribute (in Color foreground, in Color background)
    {
        Foreground = foreground;
        Background = background;

        // TODO: Once CursesDriver supports true color all the PlatformColor stuff goes away
        PlatformColor = Application.Driver?.MakeColor(in foreground, in background).PlatformColor ?? -1;
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

    /// <inheritdoc/>
    public override int GetHashCode () { return HashCode.Combine (PlatformColor, Foreground, Background); }

    /// <inheritdoc/>
    public override string ToString ()
    {
        // Note: Unit tests are dependent on this format
        return $"[{Foreground},{Background}]";
    }
}
