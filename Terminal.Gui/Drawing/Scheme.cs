#nullable enable
using System.Collections.Immutable;
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
///     <para>
///         The Normal attribute must always be set. All other attributes are optional.
///         If a Scheme does not have a value set for a specific <see cref="VisualRole"/>, then a derived value is used.
///         The algorithm for deriving the value is as follows:
///             - For roles that have a "Hot" variant (e.g., <see cref="VisualRole.Normal"/> has <see cref="VisualRole.HotNormal"/> as it's "Hot" variant):
///                 - If the "Hot" variant is not set, it will be derived from the Normal attribute by adding the underline style.
///                 - If the non-"Hot" variant is not set, it will be derived from the "Hot" variant, removing the underline style.
///             - For roles that have a "Focus" variant (e.g., <see cref="VisualRole.Normal"/> has <see cref="VisualRole.Focus"/> as it's "Focus" variant):
///                 - If the "Focus" variant is not set, it will be derived from the Normal attribute by changing the background color to the highlight color.
///             - If "Highlight" is not set, it will be derived from the Normal attribute by using the GetHighlightColor method.
///             - If "Editable" is not set, the Base "Editable" attribute will be used.
///             - If "ReadOnly" is not set, it will be derived from the "Editable" attribute by adding the italic and faint style.
///                 
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
    internal static ImmutableDictionary<string, Scheme> GetHardCodedSchemes ()
    {
        return ImmutableDictionary.CreateRange (
                                                StringComparer.InvariantCultureIgnoreCase,
                                                [
                                                    new KeyValuePair<string, Scheme> (SchemeManager.SchemesToSchemeName (Schemes.Base)!, CreateBase ()),
                                                    new (SchemeManager.SchemesToSchemeName (Schemes.Toplevel)!, CreateToplevel ()),
                                                    new (SchemeManager.SchemesToSchemeName (Schemes.Error)!, CreateError ()),
                                                    new (SchemeManager.SchemesToSchemeName (Schemes.Dialog)!, CreateDialog ()),
                                                    new (SchemeManager.SchemesToSchemeName (Schemes.Menu)!, CreateMenu ())
                                                ]
                                               );

        Scheme CreateBase ()
        {
            Color highlight = new Color ("LightGray");
            highlight = highlight.GetHighlightColor ();
            return new ()
            {
                Normal = new ("LightGray", "RaisinBlack"),
                Focus = new ("White", "DarkGray", "Bold"),
                HotNormal = new ("Silver", "RaisinBlack", "Underline"),
                Disabled = new ("DarkGray", "RaisinBlack", "Faint"),
                HotFocus = new ("White", "DarkGray", "Underline,Bold"),
                Active = new ("White", "Charcoal"),
                HotActive = new ("White", "Charcoal", "Underline"),
                Highlight = new (highlight, new Color("RaisinBlack")),
                Editable = new ("LightYellow", "OuterSpace"),
                //use algo: ReadOnly = new ("Gray", "RaisinBlack", "Italic")
            };
        }

        Scheme CreateToplevel ()
        {
            return new ()
            {
                Normal = new ("Gainsboro", "OuterSpace"),
                Focus = new ("White", "SlateGray"),
                HotNormal = new ("LightGray", "OuterSpace", "Underline"),
                Disabled = new ("DimGray", "OuterSpace", "Faint"),
                HotFocus = new ("White", "SlateGray", "Underline"),
                Active = new ("White", "DarkSlateGray", "Bold"),
                HotActive = new ("White", "DarkSlateGray", "Underline,Bold"),
                Highlight = new ("White", "Onyx"),
                Editable = new ("LemonChiffon", "RaisinBlack"),
                ReadOnly = new ("Silver", "OuterSpace", "Italic")
            };
        }

        Scheme CreateError ()
        {
            return new ()
            {
                Normal = new ("IndianRed", "RaisinBlack"),
                Focus = new ("White", "IndianRed"),
                HotNormal = new ("LightCoral", "RaisinBlack", "Underline"),
                Disabled = new ("DarkGray", "RaisinBlack", "Faint"),
                HotFocus = new ("White", "IndianRed", "Underline"),
                Active = new ("White", "LightCoral", "Bold"),
                HotActive = new ("White", "LightCoral", "Underline,Bold"),
                Highlight = new ("White", "IndianRed"),
                Editable = new ("LemonChiffon", "RaisinBlack"),
                ReadOnly = new ("Silver", "RaisinBlack", "Italic")
            };
        }

        Scheme CreateDialog ()
        {
            return new ()
            {
                Normal = new ("Gainsboro", "SlateGray"),
                Focus = new ("Black", "Gainsboro"),
                HotNormal = new ("WhiteSmoke", "SlateGray", "Underline"),
                Disabled = new ("Gray", "SlateGray", "Faint"),
                HotFocus = new ("Black", "Gainsboro", "Underline"),
                Active = new ("Black", "WhiteSmoke", "Bold"),
                HotActive = new ("Black", "WhiteSmoke", "Underline,Bold"),
                Highlight = new ("Black", "Gainsboro"),
                Editable = new ("Black", "LemonChiffon"),
                ReadOnly = new ("Silver", "SlateGray", "Italic")
            };
        }

        Scheme CreateMenu ()
        {
            return new ()
            {
                Normal = new ("Charcoal", "WhiteSmoke", "Bold"),
                Focus = new ("Black", "SlateGray", "Bold"),
                HotNormal = new ("Charcoal", "WhiteSmoke", "Underline,Bold"),
                Disabled = new ("Gray", "Gainsboro", "Faint"),
                HotFocus = new ("Black", "SlateGray", "Underline,Bold"),
                Active = new ("White", "LightGray", "Bold"),
                HotActive = new ("White", "LightGray", "Underline,Bold"),
                Highlight = new ("White", "SlateGray"),
                Editable = new ("Charcoal", "WhiteSmoke"),
                ReadOnly = new ("Silver", "WhiteSmoke", "Italic")
            };
        }
    }

    /// <summary>Creates a new instance set to the default attributes (see <see cref="Attribute.Default"/>).</summary>
    public Scheme () : this (Attribute.Default.AsExplicitlySet ()) { }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="scheme"/>.</summary>
    /// <param name="scheme">The scheme to initialize the new instance with.</param>
    public Scheme (Scheme? scheme)
    {
        ArgumentNullException.ThrowIfNull (scheme);

        // Copy attributes preserving their IsExplicitlySet status
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
        // Only set Normal as explicitly set
        Normal = attribute.AsExplicitlySet ();

        // All others are implicit and will inherit from Normal
        HotNormal = attribute.AsImplicit ();
        Focus = attribute.AsImplicit ();
        HotFocus = attribute.AsImplicit ();
        Active = attribute.AsImplicit ();
        HotActive = attribute.AsImplicit ();
        Highlight = attribute.AsImplicit ();
        Editable = attribute.AsImplicit ();
        ReadOnly = attribute.AsImplicit ();
        Disabled = attribute.AsImplicit ();
    }

    /// <summary>
    ///     Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>,
    ///     applying inheritance rules for attributes not explicitly set.
    /// </summary>
    /// <param name="role">The semantic <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <returns>
    ///     The corresponding <see cref="Attribute"/> from the <see cref="Scheme"/>, possibly derived if not explicitly
    ///     set.
    /// </returns>
    public Attribute GetAttributeForRole (VisualRole role)
    {
        // Get the base attribute for the role
        Attribute attr = role switch
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

        // If explicitly set or it's the Normal role (which must always be set), return as is
        if (attr.IsExplicitlySet || role == VisualRole.Normal)
        {
            return attr;
        }

        // Otherwise apply inheritance rules
        return DeriveAttributeForRole (role);
    }

    /// <summary>
    ///     PRIVATE: Derives an attribute for a visual role based on inheritance rules.
    /// </summary>
    private Attribute DeriveAttributeForRole (VisualRole role)
    {
        return role switch
               {
                   VisualRole.HotNormal => Normal with { Style = Normal.Style | TextStyle.Underline, IsExplicitlySet = false },
                   VisualRole.Focus => Normal with { Background = Normal.Background.GetHighlightColor (), IsExplicitlySet = false },
                   VisualRole.HotFocus => GetDerivedAttribute (VisualRole.Focus) with
                   {
                       Style = GetDerivedAttribute (VisualRole.Focus).Style | TextStyle.Underline,
                       IsExplicitlySet = false
                   },
                   VisualRole.Active => GetDerivedAttribute (VisualRole.Focus) with
                   {
                       Style = GetDerivedAttribute (VisualRole.Focus).Style | TextStyle.Bold,
                       IsExplicitlySet = false
                   },
                   VisualRole.HotActive => GetDerivedAttribute (VisualRole.Active) with
                   {
                       Style = GetDerivedAttribute (VisualRole.Active).Style | TextStyle.Underline,
                       IsExplicitlySet = false
                   },
                   VisualRole.Highlight => Normal with { Background = Normal.Background.GetHighlightColor (), IsExplicitlySet = false },
                   VisualRole.Editable => Normal with { Foreground = new ("LightYellow"), IsExplicitlySet = false },
                   VisualRole.ReadOnly => GetDerivedAttribute (VisualRole.Editable) with
                   {
                       Style = GetDerivedAttribute (VisualRole.Editable).Style | TextStyle.Italic,
                       IsExplicitlySet = false
                   },
                   VisualRole.Disabled => Normal with { Style = Normal.Style | TextStyle.Faint, IsExplicitlySet = false },
                   _ => Normal
               };
    }

    /// <summary>
    ///     PRIVATE: Helper method to get an attribute (explicitly set or derived) for a role.
    ///     Avoids potential infinite recursion by handling each role directly.
    /// </summary>
    private Attribute GetDerivedAttribute (VisualRole role)
    {
        Attribute attr = role switch
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

        if (attr.IsExplicitlySet || role == VisualRole.Normal)
        {
            return attr;
        }

        // Direct derivation for each role to avoid recursion issues
        return role switch
               {
                   VisualRole.HotNormal => Normal with { Style = Normal.Style | TextStyle.Underline, IsExplicitlySet = false },
                   VisualRole.Focus => Normal with { Background = Normal.Background.GetHighlightColor (), IsExplicitlySet = false },
                   VisualRole.HotFocus => Normal with
                   {
                       Background = Normal.Background.GetHighlightColor (),
                       Style = Normal.Style | TextStyle.Underline,
                       IsExplicitlySet = false
                   },
                   VisualRole.Active => Normal with
                   {
                       Background = Normal.Background.GetHighlightColor (),
                       Style = Normal.Style | TextStyle.Bold,
                       IsExplicitlySet = false
                   },
                   VisualRole.HotActive => Normal with
                   {
                       Background = Normal.Background.GetHighlightColor (),
                       Style = Normal.Style | TextStyle.Bold | TextStyle.Underline,
                       IsExplicitlySet = false
                   },
                   VisualRole.Highlight => Normal with { Background = Normal.Background.GetHighlightColor (), IsExplicitlySet = false },
                   VisualRole.Editable => Normal with { Foreground = new ("LightYellow"), IsExplicitlySet = false },
                   VisualRole.ReadOnly => Normal with { Foreground = new ("LightYellow"), Style = Normal.Style | TextStyle.Italic, IsExplicitlySet = false },
                   VisualRole.Disabled => Normal with { Style = Normal.Style | TextStyle.Faint, IsExplicitlySet = false },
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
        if (Enum.TryParse (roleName, true, out VisualRole role))
        {
            return GetAttributeForRole (role);
        }

        // If the string does not match any VisualRole, return the default Normal attribute
        return Normal;
    }

    /// <summary>
    ///     The default visual role for unfocused, unselected, enabled elements.
    /// </summary>
    public Attribute Normal { get; init; }

    /// <summary>
    ///     The visual role for <see cref="Normal"/> elements with a <see cref="View.HotKey"/> indicator.
    /// </summary>
    public Attribute HotNormal { get; init; }

    /// <summary>
    ///     The visual role when the element is focused.
    /// </summary>
    public Attribute Focus { get; init; }

    /// <summary>
    ///     The visual role for <see cref="Focus"/> elements with a <see cref="View.HotKey"/> indicator.
    /// </summary>
    public Attribute HotFocus { get; init; }

    /// <summary>
    ///     The visual role for elements that are active or selected (e.g., selected item in a <see cref="ListView"/>). Also
    ///     used
    ///     for headers in, <see cref="HexView"/>, <see cref="CharMap"/> and  <see cref="TabView"/>.
    /// </summary>
    public Attribute Active { get; init; }

    /// <summary>
    ///     The visual role for <see cref="Active"/> elements with a <see cref="View.HotKey"/> indicator.
    /// </summary>
    public Attribute HotActive { get; init; }

    /// <summary>
    ///     The visual role for elements that are highlighted (e.g., when the mouse is hovering over a <see cref="Button"/>).
    /// </summary>
    public Attribute Highlight { get; init; }

    /// <summary>
    ///     The visual role for elements that are editable (e.g., <see cref="TextField"/> and <see cref="TextView"/>).
    /// </summary>
    public Attribute Editable { get; init; }

    /// <summary>
    ///     The visual role for elements that are normally editable but currently read-only.
    /// </summary>
    public Attribute ReadOnly { get; init; }

    /// <summary>
    ///     The visual role for elements that are disabled and not interactable.
    /// </summary>
    public Attribute Disabled { get; init; }

    /// <inheritdoc/>
    public virtual bool Equals (Scheme? other)
    {
        return other is { }
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
