#nullable enable
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Attributes represent how text is styled when displayed in the terminal.</summary>
/// <remarks>
///     <see cref="Attribute"/> provides a platform independent representation of colors (and someday other forms of
///     text styling). They encode both the foreground and the background color and are used in the
///     <see cref="ColorScheme"/> class to define color schemes that can be used in an application.
/// </remarks>
[JsonConverter (typeof (AttributeJsonConverter))]
public readonly struct Attribute : IEquatable<Attribute>, IEqualityOperators<Attribute, Attribute, bool>
{
    /// <summary>Default empty attribute.</summary>
    public static readonly Attribute Default = new (Color.White, ColorName.Black);

    /// <summary>The <see cref="ConsoleDriver"/>-specific color value.</summary>
    [JsonIgnore (Condition = JsonIgnoreCondition.Always)]
    internal int PlatformColor { get; }

    /// <summary>The foreground color.</summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Foreground { get; }

    /// <summary>The background color.</summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Background { get; }

    /// <summary>Initializes a new instance with default values.</summary>
    public Attribute ()
    {
        PlatformColor = -1;
        Foreground = Default.Foreground;
        Background = Default.Background;
    }

    /// <summary>Initializes a new instance from an existing instance.</summary>
    public Attribute (in Attribute attr)
    {
        PlatformColor = -1;
        Foreground = attr.Foreground;
        Background = attr.Background;
    }

    /// <summary>Initializes a new instance with platform specific color value.</summary>
    /// <param name="platformColor">Value.</param>
    internal Attribute (int platformColor)
    {
        PlatformColor = platformColor;
        Foreground = Default.Foreground;
        Background = Default.Background;
    }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="platformColor">platform-dependent color value.</param>
    /// <param name="foreground">Foreground</param>
    /// <param name="background">Background</param>
    internal Attribute (int platformColor, in Color foreground, in Color background)
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

        // TODO: Once CursesDriver supports truecolor all the PlatformColor stuff goes away
        if (Application.Driver is null)
        {
            PlatformColor = -1;

            return;
        }

        Attribute make = Application.Driver.MakeColor (foreground, background);
        PlatformColor = make.PlatformColor;
    }

    /// <summary>
    ///     Initializes a new instance with a <see cref="ColorName"/> value. Both <see cref="Foreground"/> and
    ///     <see cref="Background"/> will be set to the specified color.
    /// </summary>
    /// <param name="colorName">Value.</param>
    internal Attribute (ColorName colorName) : this (colorName, colorName) { }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="foregroundName">Foreground</param>
    /// <param name="backgroundName">Background</param>
    public Attribute (in ColorName foregroundName, in ColorName backgroundName)
        : this (new Color (in foregroundName), new Color (in backgroundName))
    { }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="foregroundName">Foreground</param>
    /// <param name="background">Background</param>
    public Attribute (ColorName foregroundName, Color background) : this (new Color (foregroundName), background) { }

    /// <summary>Initializes a new instance of the <see cref="Attribute"/> struct.</summary>
    /// <param name="foreground">Foreground</param>
    /// <param name="backgroundName">Background</param>
    public Attribute (Color foreground, ColorName backgroundName) : this (foreground, new Color (backgroundName)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Attribute"/> struct with the same colors for the foreground and
    ///     background.
    /// </summary>
    /// <param name="color">The color.</param>
    public Attribute (Color color) : this (color, color) { }

    /// <summary>Compares two attributes for equality.</summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator == (Attribute left, Attribute right) { return left.Equals (right); }

    /// <summary>Compares two attributes for inequality.</summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator != (Attribute left, Attribute right) { return !(left == right); }

    /// <inheritdoc/>
    public override bool Equals (object? obj) { return obj is Attribute other && Equals (other); }

    /// <inheritdoc/>
    public bool Equals (Attribute other) { return PlatformColor == other.PlatformColor && Foreground == other.Foreground && Background == other.Background; }

    /// <inheritdoc/>
    public override int GetHashCode () { return HashCode.Combine (PlatformColor, Foreground, Background); }

    /// <inheritdoc/>
    public override string ToString ()
    {
        // Note: Unit tests are dependent on this format
        return $"[{Foreground},{Background}]";
    }
}
