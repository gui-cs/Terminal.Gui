#nullable enable
using System.Collections.Immutable;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Drawing;

/// <summary>
///     Represents a theme definition that maps each <see cref="VisualRole"/> (such as <see cref="VisualRole.Focus"/>,
///     <see cref="VisualRole.Disabled"/>, etc.)
///     to an <see cref="Attribute"/> describing its foreground color, background color, and text style.
///     <para>
///         A <see cref="Scheme"/> enables consistent, semantic theming of UI elements by associating each visual state
///         with a specific style.
///         Each property (e.g., <see cref="Normal"/>, <see cref="Focus"/>, <see cref="Disabled"/>) is an
///         <see cref="Attribute"/>.
///         If a property is not explicitly set, its value is derived from other roles (typically <see cref="Normal"/>)
///         using well-defined inheritance rules.
///     </para>
///     <para>
///         <see cref="Scheme"/> objects are immutable. To update a scheme, create a new instance with the desired values.
///         Use <see cref="SchemeManager"/> to manage available schemes and apply them to views.
///     </para>
///     <para>
///         See <see href="https://gui-cs.github.io/Terminal.Gui/docs/drawing.html"/> for more information.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         <b>Immutability:</b> Scheme objects are immutable. Once constructed, their properties cannot be changed. To
///         modify a Scheme,
///         create a new instance with the desired values (e.g., using the <see cref="Scheme(Scheme)"/> constructor).
///     </para>
///     <para>
///         <b>Attribute Resolution Algorithm:</b>
///         <br/>
///         Each <see cref="Scheme"/> property corresponds to a <see cref="VisualRole"/> and is an <see cref="Attribute"/>.
///         The <see cref="Normal"/> attribute must always be set.
///         All other attributes are optional. If an attribute for a given <see cref="VisualRole"/> is not explicitly set,
///         its value is derived using the following rules:
///         <list type="number">
///             <item>
///                 <description><b>Normal:</b> Must always be explicitly set.</description>
///             </item>
///             <item>
///                 <description>
///                     <b>Focus:</b> If not set, derived from <see cref="Normal"/> by swapping foreground and background
///                     colors.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Active:</b> If not set, derived from <see cref="Focus"/> by:
///                     <list type="bullet">
///                         <item>
///                             <description>
///                                 Setting <c>Foreground</c> to <see cref="Focus"/>'s foreground with
///                                 <c>GetHighlightColor()</c>.
///                             </description>
///                         </item>
///                         <item>
///                             <description>
///                                 Setting <c>Background</c> to <see cref="Focus"/>'s background with
///                                 <c>GetDimColor()</c>.
///                             </description>
///                         </item>
///                         <item>
///                             <description>Adding <see cref="TextStyle.Bold"/> to the style.</description>
///                         </item>
///                     </list>
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Highlight:</b> If not set, derived from <see cref="Normal"/> by:
///                     <list type="bullet">
///                         <item>
///                             <description>
///                                 Setting <c>Foreground</c> to <see cref="Normal"/>'s background with
///                                 <c>GetHighlightColor()</c>.
///                             </description>
///                         </item>
///                         <item>
///                             <description>Setting <c>Background</c> to <see cref="Normal"/>'s background.</description>
///                         </item>
///                         <item>
///                             <description>
///                                 Setting <c>Style</c> to <see cref="Editable"/>'s style with
///                                 <see cref="TextStyle.Italic"/> added.
///                             </description>
///                         </item>
///                     </list>
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Editable:</b> If not set, derived from <see cref="Normal"/> by:
///                     <list type="bullet">
///                         <item>
///                             <description>
///                                 Setting <c>Foreground</c> to <see cref="Normal"/>'s background with
///                                 <c>GetHighlightColor()</c>.
///                             </description>
///                         </item>
///                         <item>
///                             <description>
///                                 Setting <c>Background</c> to <see cref="Normal"/>'s background with
///                                 <c>GetDimColor()</c>.
///                             </description>
///                         </item>
///                     </list>
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>ReadOnly:</b> If not set, derived from <see cref="Editable"/> by adding
///                     <see cref="TextStyle.Faint"/> to the style.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Disabled:</b> If not set, derived from <see cref="Normal"/> by adding
///                     <see cref="TextStyle.Faint"/> to the style.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>HotNormal:</b> If not set, derived from <see cref="Normal"/> by adding
///                     <see cref="TextStyle.Underline"/> to the style.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>HotFocus:</b> If not set, derived from <see cref="Focus"/> by adding
///                     <see cref="TextStyle.Underline"/> to the style.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>HotActive:</b> If not set, derived from <see cref="Active"/> by adding
///                     <see cref="TextStyle.Underline"/> to the style.
///                 </description>
///             </item>
///         </list>
///         This algorithm ensures that every <see cref="VisualRole"/> always resolves to a valid <see cref="Attribute"/>,
///         either explicitly set or derived.
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
    internal static ImmutableSortedDictionary<string, Scheme> GetHardCodedSchemes ()
    {
        return ImmutableSortedDictionary.CreateRange (
                                                      StringComparer.InvariantCultureIgnoreCase,
                                                      [
                                                          new KeyValuePair<string, Scheme> (SchemeManager.SchemesToSchemeName (Schemes.Base)!, CreateBase ()),
                                                          new (SchemeManager.SchemesToSchemeName (Schemes.Dialog)!, CreateDialog ()),
                                                          new (SchemeManager.SchemesToSchemeName (Schemes.Error)!, CreateError ()),
                                                          new (SchemeManager.SchemesToSchemeName (Schemes.Menu)!, CreateMenu ()),
                                                          new (SchemeManager.SchemesToSchemeName (Schemes.Toplevel)!, CreateToplevel ()),
                                                      ]
                                                     );

        Scheme CreateBase ()
        {
            return new ()
            {
                Normal = new (StandardColor.LightBlue, StandardColor.RaisinBlack)
            };
        }

        Scheme CreateError ()
        {
            return new ()
            {
                Normal = new (StandardColor.IndianRed, StandardColor.RaisinBlack)
            };
        }

        Scheme CreateDialog ()
        {
            return new ()
            {
                Normal = new (StandardColor.LightSkyBlue, StandardColor.OuterSpace)
            };
        }

        Scheme CreateMenu ()
        {
            return new ()
            {
                Normal = new (StandardColor.Charcoal, StandardColor.LightBlue, TextStyle.Bold)
            };
        }

        Scheme CreateToplevel ()
        {
            return new ()
            {
                Normal = new (StandardColor.CadetBlue, StandardColor.Charcoal)
            };
        }

    }

    /// <summary>Creates a new instance set to the default attributes (see <see cref="Attribute.Default"/>).</summary>
    public Scheme () : this (Attribute.Default) { }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="scheme"/>.</summary>
    /// <param name="scheme">The scheme to initialize the new instance with.</param>
    public Scheme (Scheme? scheme)
    {
        ArgumentNullException.ThrowIfNull (scheme);

        Normal = scheme.Normal;

        _hotNormal = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotNormal, out Attribute? hotNormal) ? hotNormal : null;
        _focus = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Focus, out Attribute? focus) ? focus : null;
        _hotFocus = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotFocus, out Attribute? hotFocus) ? hotFocus : null;
        _active = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out Attribute? active) ? active : null;
        _hotActive = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotActive, out Attribute? hotActive) ? hotActive : null;
        _highlight = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Highlight, out Attribute? highlight) ? highlight : null;
        _editable = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Editable, out Attribute? editable) ? editable : null;
        _readOnly = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.ReadOnly, out Attribute? readOnly) ? readOnly : null;
        _disabled = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Disabled, out Attribute? disabled) ? disabled : null;
    }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="attribute"/>.</summary>
    /// <param name="attribute">The attribute to initialize the new instance with.</param>
    public Scheme (Attribute attribute)
    {
        // Only set Normal as explicitly set
        Normal = attribute;
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
        // Use a HashSet to guard against recursion cycles
        return GetAttributeForRoleCore (role, []);
    }

    /// <summary>
    ///     Attempts to get the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>. If the
    ///     role is not explicitly set, it will return false and the out parameter will be null.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="attribute"></param>
    /// <returns></returns>
    public bool TryGetExplicitlySetAttributeForRole (VisualRole role, out Attribute? attribute)
    {
        // Use a HashSet to guard against recursion cycles
        attribute = role switch
                    {
                        VisualRole.Normal => _normal,
                        VisualRole.HotNormal => _hotNormal,
                        VisualRole.Focus => _focus,
                        VisualRole.HotFocus => _hotFocus,
                        VisualRole.Active => _active,
                        VisualRole.HotActive => _hotActive,
                        VisualRole.Highlight => _highlight,
                        VisualRole.Editable => _editable,
                        VisualRole.ReadOnly => _readOnly,
                        VisualRole.Disabled => _disabled,
                        _ => null
                    };

        return attribute is { };
    }

    // TODO: Provide a CWP-based API that lets devs override this algo?

    private Attribute GetAttributeForRoleCore (VisualRole role, HashSet<VisualRole> stack)
    {
        // Prevent infinite recursion
        if (!stack.Add (role))
        {
            return Normal; // fallback
        }

        Attribute? attr = Normal;
        if (role == VisualRole.Normal || TryGetExplicitlySetAttributeForRole (role, out attr))
        {
            return attr!.Value;
        }

        // TODO: Provide an API that lets devs override this algo?

        // Derivation algorithm as documented
        Attribute result = role switch
                           {
                               VisualRole.Focus =>
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Normal, stack).Background,
                                       Background = GetAttributeForRoleCore (VisualRole.Normal, stack).Foreground
                                   },

                               VisualRole.Active =>
                                   GetAttributeForRoleCore (VisualRole.Focus, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Focus, stack).Foreground.GetBrighterColor (),
                                       Background = GetAttributeForRoleCore (VisualRole.Focus, stack).Background.GetDimColor (),
                                       Style = GetAttributeForRoleCore (VisualRole.Focus, stack).Style | TextStyle.Bold
                                   },

                               VisualRole.Highlight =>
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Normal, stack).Background.GetBrighterColor (),
                                       Background = GetAttributeForRoleCore (VisualRole.Normal, stack).Background,
                                       Style = GetAttributeForRoleCore (VisualRole.Editable, stack).Style | TextStyle.Italic
                                   },

                               VisualRole.Editable =>
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Normal, stack).Foreground,
                                       Background = GetAttributeForRoleCore (VisualRole.Normal, stack).Foreground.GetDimColor (0.5)
                                   },

                               VisualRole.ReadOnly =>
                                   GetAttributeForRoleCore (VisualRole.Editable, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Editable, stack).Foreground.GetDimColor (0.05),
                                   },

                               VisualRole.Disabled =>
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Normal, stack).Foreground.GetDimColor (0.05),
                                   },

                               VisualRole.HotNormal =>
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Style = GetAttributeForRoleCore (VisualRole.Normal, stack).Style | TextStyle.Underline
                                   },

                               VisualRole.HotFocus =>
                                   GetAttributeForRoleCore (VisualRole.Focus, stack) with
                                   {
                                       Style = GetAttributeForRoleCore (VisualRole.Focus, stack).Style | TextStyle.Underline
                                   },

                               VisualRole.HotActive =>
                                   GetAttributeForRoleCore (VisualRole.Active, stack) with
                                   {
                                       Style = GetAttributeForRoleCore (VisualRole.Active, stack).Style | TextStyle.Underline
                                   },

                               _ => GetAttributeForRoleCore (VisualRole.Normal, stack)
                           };

        stack.Remove (role);

        return result;
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

    // Helper method for property _get implementation
    private Attribute GetAttributeForRoleProperty (Attribute? explicitValue, VisualRole role)
    {
        if (explicitValue is { })
        {
            return explicitValue.Value;
        }

        return GetAttributeForRoleCore (role, []);
    }

    // Helper method for property _set implementation
    private Attribute? SetAttributeForRoleProperty (Attribute value, VisualRole role)
    {
        // If value is the same as the algorithm value, use null
        if (GetAttributeForRoleCore (role, []) == value)
        {
            return null;
        }
        return value;
    }

    private readonly Attribute? _normal;

    /// <summary>
    ///     The default visual role for unfocused, unselected, enabled elements.
    ///     The Normal attribute must always be set. All other attributes are optional, and if not explicitly
    ///     set, will be automatically generated. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Normal
    {
        get => _normal!.Value;
        init => _normal = value;
    }

    private readonly Attribute? _hotNormal;

    /// <summary>
    ///     The visual role for <see cref="Normal"/> elements with a <see cref="View.HotKey"/> indicator.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute HotNormal
    {
        get => GetAttributeForRoleProperty (_hotNormal, VisualRole.HotNormal);
        init => _hotNormal = SetAttributeForRoleProperty (value, VisualRole.HotNormal);
    }

    private readonly Attribute? _focus;

    /// <summary>
    ///     The visual role when the element is focused.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Focus
    {
        get => GetAttributeForRoleProperty (_focus, VisualRole.Focus);
        init => _focus = SetAttributeForRoleProperty (value, VisualRole.Focus);
    }

    private readonly Attribute? _hotFocus;

    /// <summary>
    ///     The visual role for <see cref="Focus"/> elements with a <see cref="View.HotKey"/> indicator.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute HotFocus
    {
        get => GetAttributeForRoleProperty (_hotFocus, VisualRole.HotFocus);
        init => _hotFocus = SetAttributeForRoleProperty (value, VisualRole.HotFocus);
    }

    private readonly Attribute? _active;

    /// <summary>
    ///     The visual role for elements that are active or selected (e.g., selected item in a <see cref="ListView"/>). Also
    ///     used
    ///     for headers in, <see cref="HexView"/>, <see cref="CharMap"/> and  <see cref="TabView"/>.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Active
    {
        get => GetAttributeForRoleProperty (_active, VisualRole.Active);
        init => _active = SetAttributeForRoleProperty (value, VisualRole.Active);
    }

    private readonly Attribute? _hotActive;

    /// <summary>
    ///     The visual role for <see cref="Active"/> elements with a <see cref="View.HotKey"/> indicator.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute HotActive
    {
        get => GetAttributeForRoleProperty (_hotActive, VisualRole.HotActive);
        init => _hotActive = SetAttributeForRoleProperty (value, VisualRole.HotActive);
    }

    private readonly Attribute? _highlight;

    /// <summary>
    ///     The visual role for elements that are highlighted (e.g., when the mouse is inside a <see cref="Button"/>).
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Highlight
    {
        get => GetAttributeForRoleProperty (_highlight, VisualRole.Highlight);
        init => _highlight = SetAttributeForRoleProperty (value, VisualRole.Highlight);
    }

    private readonly Attribute? _editable;

    /// <summary>
    ///     The visual role for elements that are editable (e.g., <see cref="TextField"/> and <see cref="TextView"/>).
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Editable
    {
        get => GetAttributeForRoleProperty (_editable, VisualRole.Editable);
        init => _editable = SetAttributeForRoleProperty (value, VisualRole.Editable);
    }

    private readonly Attribute? _readOnly;

    /// <summary>
    ///     The visual role for elements that are normally editable but currently read-only.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute ReadOnly
    {
        get => GetAttributeForRoleProperty (_readOnly, VisualRole.ReadOnly);
        init => _readOnly = SetAttributeForRoleProperty (value, VisualRole.ReadOnly);
    }

    private readonly Attribute? _disabled;

    /// <summary>
    ///     The visual role for elements that are disabled and not interactable.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Disabled
    {
        get => GetAttributeForRoleProperty (_disabled, VisualRole.Disabled);
        init => _disabled = SetAttributeForRoleProperty (value, VisualRole.Disabled);
    }

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
