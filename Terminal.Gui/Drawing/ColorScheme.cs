#nullable enable
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Defines a standard set of <see cref="Attribute"/>s for common visible elements in a <see cref="View"/>.</summary>
/// <remarks>
///     <para>
///         ColorScheme objects are immutable. Once constructed, the properties cannot be changed. To change a
///         ColorScheme, create a new one with the desired values, using the <see cref="ColorScheme(ColorScheme)"/>
///         constructor.
///     </para>
/// </remarks>
[JsonConverter (typeof (ColorSchemeJsonConverter))]
public record ColorScheme : IEqualityOperators<ColorScheme, ColorScheme, bool>
{
    private readonly Attribute _disabled;
    private readonly Attribute _focus;
    private readonly Attribute _hotFocus;
    private readonly Attribute _hotNormal;
    private readonly Attribute _normal;

    /// <summary>Creates a new instance set to the default colors (see <see cref="Attribute.Default"/>).</summary>
    public ColorScheme () : this (Attribute.Default) { }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="scheme"/>.</summary>
    /// <param name="scheme">The scheme to initialize the new instance with.</param>
    public ColorScheme (ColorScheme? scheme)
    {
        ArgumentNullException.ThrowIfNull (scheme);

        _normal = scheme.Normal;
        _focus = scheme.Focus;
        _hotNormal = scheme.HotNormal;
        _disabled = scheme.Disabled;
        _hotFocus = scheme.HotFocus;
    }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="attribute"/>.</summary>
    /// <param name="attribute">The attribute to initialize the new instance with.</param>
    public ColorScheme (Attribute attribute)
    {
        _normal = attribute;
        _focus = attribute;
        _hotNormal = attribute;
        _disabled = attribute;
        _hotFocus = attribute;
    }

    /// <summary>Creates a new instance, initialized with the values provided.</summary>
    public ColorScheme (
        Attribute normal,
        Attribute focus,
        Attribute hotNormal,
        Attribute disabled,
        Attribute hotFocus)
    {
        _normal = normal;
        _focus = focus;
        _hotNormal = hotNormal;
        _disabled = disabled;
        _hotFocus = hotFocus;
    }

    /// <summary>The default foreground and background color for text when the view is disabled.</summary>
    public Attribute Disabled
    {
        get => _disabled;
        init => _disabled = value;
    }

    /// <summary>The foreground and background color for text when the view has the focus.</summary>
    public Attribute Focus
    {
        get => _focus;
        init => _focus = value;
    }

    /// <summary>The foreground and background color for text in a focused view that indicates a <see cref="View.HotKey"/>.</summary>
    public Attribute HotFocus
    {
        get => _hotFocus;
        init => _hotFocus = value;
    }

    /// <summary>The foreground and background color for text in a non-focused view that indicates a <see cref="View.HotKey"/>.</summary>
    public Attribute HotNormal
    {
        get => _hotNormal;
        init => _hotNormal = value;
    }

    /// <summary>The foreground and background color for text when the view is not focused, hot, or disabled.</summary>
    public Attribute Normal
    {
        get => _normal;
        init => _normal = value;
    }

    public ColorScheme GetHighlightColorScheme ()
    {
        return this with
        {
            Normal = new (Normal.Foreground.GetHighlightColor (), Normal.Background),
            HotNormal = new (HotNormal.Foreground.GetHighlightColor (), HotNormal.Background),
            Focus = new (Focus.Foreground.GetHighlightColor (), Focus.Background),
            HotFocus = new (HotFocus.Foreground.GetHighlightColor (), HotFocus.Background),
        };
    }

    /// <summary>Compares two <see cref="ColorScheme"/> objects for equality.</summary>
    /// <param name="other"></param>
    /// <returns>true if the two objects are equal</returns>
    public virtual bool Equals (ColorScheme? other)
    {
        return other is { }
               && EqualityComparer<Attribute>.Default.Equals (_normal, other._normal)
               && EqualityComparer<Attribute>.Default.Equals (_focus, other._focus)
               && EqualityComparer<Attribute>.Default.Equals (_hotNormal, other._hotNormal)
               && EqualityComparer<Attribute>.Default.Equals (_hotFocus, other._hotFocus)
               && EqualityComparer<Attribute>.Default.Equals (_disabled, other._disabled);
    }

    /// <summary>Returns a hashcode for this instance.</summary>
    /// <returns>hashcode for this instance</returns>
    public override int GetHashCode ()
    {
        return HashCode.Combine (_normal, _focus, _hotNormal, _hotFocus, _disabled);
    }

    /// <inheritdoc/>
    public override string ToString () { return $"Normal: {Normal}; Focus: {Focus}; HotNormal: {HotNormal}; HotFocus: {HotFocus}; Disabled: {Disabled}"; }
}