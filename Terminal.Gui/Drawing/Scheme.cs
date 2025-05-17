#nullable enable
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     A Scheme is a mapping from <see cref="VisualRole"/>s (such as <see cref="VisualRole.Focus"/>) to
///     <see cref="Attribute"/>s.
///     A Scheme defines how a `View` should look based on its purpose (e.g. Menu or Dialog).
///     <para>
///         Use <see cref="SchemeManager"/> to manage the available schemes.
///     </para>
///     <para>
///         See <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/drawing.html"/> for more info.
///     </para>
/// </summary>
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
    /// <summary>
    ///     INTERNAL: Gets the hard-coded set of <see cref="Scheme"/>s. Used for generating the built-in config.json and for
    ///     unit tests that don't depend on ConfigurationManager.
    /// </summary>
    /// <returns></returns>
    internal static Dictionary<string, Scheme?> GetHardCodedSchemes ()
    {
        return new (StringComparer.InvariantCultureIgnoreCase)
        {
            {
                SchemeManager.SchemesToSchemeName (Schemes.Base)!,
                new()
                {
                    Normal = new (new ("LightGray"), new ("RaisinBlack"), TextStyle.None), // Material Theme: Panel Background
                    Focus = new (new ("White"), new ("DarkGray"), TextStyle.None), // Slightly lighter background for focus
                    HotNormal = new (new ("Silver"), new ("RaisinBlack"), TextStyle.Underline), // Lighter text for hotkeys
                    Disabled = new (new ("DarkGray"), new ("RaisinBlack"), TextStyle.Faint), // Dimmed text for disabled
                    HotFocus = new (new ("White"), new ("DarkGray"), TextStyle.Underline), // Underlined white text on focus
                    Active = new (new ("White"), new ("Charcoal"), TextStyle.Bold), // White text on active
                    HotActive = new (new ("White"), new ("Charcoal"), TextStyle.Underline | TextStyle.Bold), // Underlined white text on active
                    Highlight = new (new ("White"), new ("Onyx"), TextStyle.None), // Highlight with slightly lighter background
                    Editable = new (new ("LightYellow"), new ("RaisinBlack"), TextStyle.None), // Yellowish text for editable fields
                    ReadOnly = new (new ("Gray"), new ("RaisinBlack"), TextStyle.Italic) // Gray italic text for read-only
                }
            },
            {
                SchemeManager.SchemesToSchemeName (Schemes.Toplevel)!,
                new()
                {
                    Normal = new (new ("Gainsboro"), new ("OuterSpace"), TextStyle.None), // Slightly darker than base
                    Focus = new (new ("White"), new ("SlateGray"), TextStyle.None),
                    HotNormal = new (new ("LightGray"), new ("OuterSpace"), TextStyle.Underline),
                    Disabled = new (new ("DimGray"), new ("OuterSpace"), TextStyle.Faint),
                    HotFocus = new (new ("White"), new ("SlateGray"), TextStyle.Underline),
                    Active = new (new ("White"), new ("DarkSlateGray"), TextStyle.Bold),
                    HotActive = new (new ("White"), new ("DarkSlateGray"), TextStyle.Underline | TextStyle.Bold),
                    Highlight = new (new ("White"), new ("Onyx"), TextStyle.None),
                    Editable = new (new ("LemonChiffon"), new ("OuterSpace"), TextStyle.None),
                    ReadOnly = new (new ("Silver"), new ("OuterSpace"), TextStyle.Italic)
                }
            },
            {
                SchemeManager.SchemesToSchemeName (Schemes.Error)!,
                new Scheme
                {
                    Normal = new Attribute(new Color("IndianRed"), new Color("RaisinBlack"), TextStyle.None), // Red text
                    Focus = new Attribute(new Color("White"), new Color("IndianRed"), TextStyle.None), // Inverted red
                    HotNormal = new Attribute(new Color("LightCoral"), new Color("RaisinBlack"), TextStyle.Underline), // Lighter red underline
                    Disabled = new Attribute(new Color("DarkGray"), new Color("RaisinBlack"), TextStyle.Faint), // Grayed out
                    HotFocus = new Attribute(new Color("White"), new Color("IndianRed"), TextStyle.Underline), // Inverted red underline
                    Active = new Attribute(new Color("White"), new Color("LightCoral"), TextStyle.Bold), // White on red bold
                    HotActive = new Attribute(new Color("White"), new Color("LightCoral"), TextStyle.Underline | TextStyle.Bold), // White on red bold underline
                    Highlight = new Attribute(new Color("White"), new Color("IndianRed"), TextStyle.None), // Highlighted red
                    Editable = new Attribute(new Color("LemonChiffon"), new Color("RaisinBlack"), TextStyle.None), // Yellowish text for editable fields
                    ReadOnly = new Attribute(new Color("Silver"), new Color("RaisinBlack"), TextStyle.Italic)  // Gray italic text for read-only
                }
            },
            {
                SchemeManager.SchemesToSchemeName (Schemes.Dialog)!,
                new()
                {
                    Normal = new (new ("Gainsboro"), new ("SlateGray"), TextStyle.None), // Light text on dark
                    Focus = new (new ("Black"), new ("Gainsboro"), TextStyle.None), // Dark on light
                    HotNormal = new (new ("WhiteSmoke"), new ("SlateGray"), TextStyle.Underline), // Lighter underline
                    Disabled = new (new ("Gray"), new ("SlateGray"), TextStyle.Faint), // Grayed out
                    HotFocus = new (new ("Black"), new ("Gainsboro"), TextStyle.Underline), // Dark on light underline
                    Active = new (new ("Black"), new ("WhiteSmoke"), TextStyle.Bold), // Dark on light bold
                    HotActive = new (new ("Black"), new ("WhiteSmoke"), TextStyle.Underline | TextStyle.Bold), // Dark on light bold underline
                    Highlight = new (new ("Black"), new ("Gainsboro"), TextStyle.None), // Highlighted light
                    Editable = new (new ("Black"), new ("LemonChiffon"), TextStyle.None), // Yellowish text for editable fields
                    ReadOnly = new (new ("Silver"), new ("SlateGray"), TextStyle.Italic) // Gray italic text for read-only
                }
            },
            {
                SchemeManager.SchemesToSchemeName (Schemes.Menu)!,
                new()
                {
                    Normal = new (new ("Gainsboro"), new ("Charcoal"), TextStyle.Bold), // Lighter text
                    Focus = new (new ("White"), new ("DarkSlateGray"), TextStyle.Bold), // White on slightly lighter
                    HotNormal = new (new ("Gainsboro"), new ("Charcoal"), TextStyle.Underline | TextStyle.Bold), // Lighter underline
                    Disabled = new (new ("Gray"), new ("Charcoal"), TextStyle.Faint), // Grayed out
                    HotFocus = new (new ("White"), new ("DarkSlateGray"), TextStyle.Underline | TextStyle.Bold), // White on lighter underline
                    Active = new (new ("White"), new ("DimGray"), TextStyle.Bold), // White on even lighter
                    HotActive = new (new ("White"), new ("DimGray"), TextStyle.Underline | TextStyle.Bold), // White on even lighter underline
                    Highlight = new (new ("White"), new ("Onyx"), TextStyle.None), // Highlighted
                    Editable = new (new ("LemonChiffon"), new ("Charcoal"), TextStyle.None), // Yellowish text for editable fields
                    ReadOnly = new (new ("Silver"), new ("Charcoal"), TextStyle.Italic) // Gray italic text for read-only
                }
            }
        };
    }

    /// <summary>Creates a new instance set to the default attributes (see <see cref="Attribute.Default"/>).</summary>
    public Scheme () : this (Attribute.Default) { }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="scheme"/>.</summary>
    /// <param name="scheme">The scheme to initialize the new instance with.</param>
    public Scheme (Scheme? scheme)
    {
        ArgumentNullException.ThrowIfNull (scheme);

        Normal = scheme.Normal;
        HotNormal = scheme.HotNormal;
        Focus = scheme.Focus;
        HotFocus = scheme.HotFocus;
        Active = scheme.Active;
        HotActive = scheme.HotActive;
        Highlight = scheme.Highlight;
        Editable = scheme.Editable;
        ReadOnly = scheme.ReadOnly;
        Disabled = scheme.Disabled;
    }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="attribute"/>.</summary>
    /// <param name="attribute">The attribute to initialize the new instance with.</param>
    public Scheme (Attribute attribute)
    {
        Normal = attribute;
        HotNormal = attribute;
        Focus = attribute;
        HotFocus = attribute;
        Active = attribute;
        HotActive = attribute;
        Highlight = attribute;
        Editable = attribute;
        ReadOnly = attribute;
        Disabled = attribute;
    }

    /// <summary>
    ///     Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>.
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
                   VisualRole.Active => Active,
                   VisualRole.HotActive => HotActive,
                   VisualRole.Highlight => Highlight,
                   VisualRole.Editable => Editable,
                   VisualRole.ReadOnly => ReadOnly,
                   VisualRole.Disabled => Disabled,
                   _ => Normal
               };
    }

    /// <summary>
    ///     Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/> string.
    /// </summary>
    /// <param name="roleName">The name of the <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <returns>The corresponding <see cref="Attribute"/> from the <see cref="Scheme"/>.</returns>
    public Attribute GetAttributeForRole (string roleName)
    {
        if (Enum.TryParse<VisualRole> (roleName, true, out VisualRole role))
        {
            return GetAttributeForRole (role);
        }

        // If the string does not match any VisualRole, return the default Normal attribute
        return Normal;
    }

    /// <summary>The foreground and background attribute for text when the view is not focused, hot, or disabled.</summary>
    public Attribute Normal { get; init; }

    /// <summary>
    ///     The foreground and background attribute for text in a non-focused view that indicates a
    ///     <see cref="View.HotKey"/>.
    /// </summary>
    public Attribute HotNormal { get; init; }

    /// <summary>The foreground and background attribute for text when the view has the focus.</summary>
    public Attribute Focus { get; init; }

    /// <summary>The foreground and background attribute for text in a focused view that indicates a <see cref="View.HotKey"/>.</summary>
    public Attribute HotFocus { get; init; }

    /// <summary>The foreground and background attribute for text when the view is active or selected.</summary>
    public Attribute Active { get; init; }

    /// <summary>The foreground and background attribute for text when the view is active and has a HotKey indicator.</summary>
    public Attribute HotActive { get; init; }

    /// <summary>The foreground and background attribute for text when the view is highlighted.</summary>
    public Attribute Highlight { get; init; }

    /// <summary>The foreground and background attribute for text when the view is editable.</summary>
    public Attribute Editable { get; init; }

    /// <summary>The foreground and background attribute for text when the view is read-only.</summary>
    public Attribute ReadOnly { get; init; }

    /// <summary>The default foreground and background attribute for text when the view is disabled.</summary>
    public Attribute Disabled { get; init; }

    /// <inheritdoc/>
    public virtual bool Equals (Scheme? other)
    {
        return other is not null
               && EqualityComparer<Attribute>.Default.Equals (Normal, other.Normal)
               && EqualityComparer<Attribute>.Default.Equals (HotNormal, other.HotNormal)
               && EqualityComparer<Attribute>.Default.Equals (Focus, other.Focus)
               && EqualityComparer<Attribute>.Default.Equals (HotFocus, other.HotFocus)
               && EqualityComparer<Attribute>.Default.Equals (Active, other.Active)
               && EqualityComparer<Attribute>.Default.Equals (HotActive, other.HotActive)
               && EqualityComparer<Attribute>.Default.Equals (Highlight, other.Highlight)
               && EqualityComparer<Attribute>.Default.Equals (Editable, other.Editable)
               && EqualityComparer<Attribute>.Default.Equals (ReadOnly, other.ReadOnly)
               && EqualityComparer<Attribute>.Default.Equals (Disabled, other.Disabled);
    }

    /// <inheritdoc/>
    public override int GetHashCode ()
    {
        return HashCode.Combine (
                                 HashCode.Combine (Normal, HotNormal, Focus, HotFocus, Active, HotActive, Highlight, Editable),
                                 HashCode.Combine (ReadOnly, Disabled)
                                );
    }

    /// <inheritdoc/>
    public override string ToString ()
    {
        return $"Normal: {Normal}; HotNormal: {HotNormal}; Focus: {Focus}; HotFocus: {HotFocus}; "
               + $"Active: {Active}; HotActive: {HotActive}; Highlight: {Highlight}; Editable: {Editable}; "
               + $"ReadOnly: {ReadOnly}; Disabled: {Disabled}";
    }
}
