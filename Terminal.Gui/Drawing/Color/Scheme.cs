#nullable enable
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Defines a standard set of <see cref="Attribute"/>s for common visible elements in a <see cref="View"/>.</summary>
/// <remarks>
///     <para>
///         Scheme objects are immutable. Once constructed, the properties cannot be changed. To change a
///         Scheme, create a new one with the desired values, using the <see cref="Scheme(Scheme)"/>
///         constructor.
///     </para>
/// </remarks>
[JsonConverter (typeof (SchemeJsonConverter))]
public record Scheme : IEqualityOperators<Scheme, Scheme, bool>
{
    /// <summary>Creates a new instance set to the default colors (see <see cref="Attribute.Default"/>).</summary>
    public Scheme () : this (Attribute.Default) { }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="scheme"/>.</summary>
    /// <param name="scheme">The scheme to initialize the new instance with.</param>
    public Scheme (Scheme? scheme)
    {
        ArgumentNullException.ThrowIfNull (scheme);

        Normal = scheme.Normal;
        Focus = scheme.Focus;
        HotNormal = scheme.HotNormal;
        Disabled = scheme.Disabled;
        HotFocus = scheme.HotFocus;
    }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="attribute"/>.</summary>
    /// <param name="attribute">The attribute to initialize the new instance with.</param>
    public Scheme (Attribute attribute)
    {
        Normal = attribute;
        Focus = attribute;
        HotNormal = attribute;
        Disabled = attribute;
        HotFocus = attribute;
    }

    /// <summary>Creates a new instance, initialized with the values provided.</summary>
    public Scheme (
        Attribute normal,
        Attribute focus,
        Attribute hotNormal,
        Attribute disabled,
        Attribute hotFocus
    )
    {
        Normal = normal;
        Focus = focus;
        HotNormal = hotNormal;
        Disabled = disabled;
        HotFocus = hotFocus;
    }


    /// <summary>
    /// Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>.
    /// </summary>
    /// <param name="role">The semantic <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <returns>The corresponding <see cref="Attribute"/> from the <see cref="Scheme"/>.</returns>
    public Attribute GetAttributeForRole (VisualRole role)
    {
        return role switch
               {
                   VisualRole.Normal => Normal,
                   VisualRole.HotNormal => HotNormal,
                   VisualRole.Focus => Focus,
                   VisualRole.HotFocus => HotFocus,
                   //VisualRole.Active => Active,
                   //VisualRole.HotActive => HotActive,
                   VisualRole.Disabled => Disabled,
                   //VisualRole.ReadOnly => ReadOnly,
                   _ => Normal
               };
    }

    /// <summary>The default foreground and background color for text when the view is disabled.</summary>
    public Attribute Disabled { get; init; }

    /// <summary>The foreground and background color for text when the view has the focus.</summary>
    public Attribute Focus { get; init; }

    /// <summary>The foreground and background color for text in a focused view that indicates a <see cref="View.HotKey"/>.</summary>
    public Attribute HotFocus { get; init; }

    /// <summary>The foreground and background color for text in a non-focused view that indicates a <see cref="View.HotKey"/>.</summary>
    public Attribute HotNormal { get; init; }

    /// <summary>The foreground and background color for text when the view is not focused, hot, or disabled.</summary>
    public Attribute Normal { get; init; }

    /// <summary>
    ///     Gets a new <see cref="Scheme"/> with the same values as this instance, but with the foreground and background
    ///     colors adjusted to be more visible.
    /// </summary>
    /// <returns></returns>
    public Scheme GetHighlightScheme ()
    {
        return this with
        {
            Normal = new (Normal.Foreground.GetHighlightColor (), Normal.Background),
            HotNormal = new (HotNormal.Foreground.GetHighlightColor (), HotNormal.Background),
            Focus = new (Focus.Foreground.GetHighlightColor (), Focus.Background),
            HotFocus = new (HotFocus.Foreground.GetHighlightColor (), HotFocus.Background)
        };
    }

    /// <summary>Compares two <see cref="Scheme"/> objects for equality.</summary>
    /// <param name="other"></param>
    /// <returns>true if the two objects are equal</returns>
    public virtual bool Equals (Scheme? other)
    {
        return other is { }
               && EqualityComparer<Attribute>.Default.Equals (Normal, other.Normal)
               && EqualityComparer<Attribute>.Default.Equals (Focus, other.Focus)
               && EqualityComparer<Attribute>.Default.Equals (HotNormal, other.HotNormal)
               && EqualityComparer<Attribute>.Default.Equals (HotFocus, other.HotFocus)
               && EqualityComparer<Attribute>.Default.Equals (Disabled, other.Disabled);
    }

    /// <summary>Returns a hashcode for this instance.</summary>
    /// <returns>hashcode for this instance</returns>
    public override int GetHashCode () { return HashCode.Combine (Normal, Focus, HotNormal, HotFocus, Disabled); }

    /// <inheritdoc/>
    public override string ToString () { return $"Normal: {Normal}; Focus: {Focus}; HotNormal: {HotNormal}; HotFocus: {HotFocus}; Disabled: {Disabled}"; }
}
