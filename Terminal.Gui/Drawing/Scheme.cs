#nullable enable
using System.Collections.Immutable;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

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
///         See <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/drawing.html"/> for more information.
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
///                     colors, or by applying <see cref="Color.GetHighlightColor"/> to the background.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Highlight:</b> If not set, derived from <see cref="Normal"/> by applying
///                     <see cref="Color.GetHighlightColor"/> to the background color.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Editable:</b> If not set, derived from <see cref="Normal"/> by setting the foreground
///                     to <c>LightYellow</c>.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>ReadOnly:</b> If not set, derived from <see cref="Editable"/> by adding
///                     <see cref="TextStyle.Italic"/> to the style.
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
///                     <b>Active:</b> If not set, derived from <see cref="Focus"/> by adding
///                     <see cref="TextStyle.Bold"/> to the style.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Hot* variants (e.g., HotNormal, HotFocus, HotActive):</b>
///                     <list type="bullet">
///                         <item>
///                             <description>
///                                 If the corresponding non-hot variant (e.g., <see cref="Normal"/>, <see cref="Focus"/>,
///                                 <see cref="Active"/>) is not set, it is first derived as above.
///                             </description>
///                         </item>
///                         <item>
///                             <description>
///                                 If the hot variant is not set, it is derived from its non-hot variant by adding
///                                 <see cref="TextStyle.Underline"/> to the style.
///                             </description>
///                         </item>
///                     </list>
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
                                                          new (SchemeManager.SchemesToSchemeName (Schemes.Toplevel)!, CreateToplevel ()),
                                                          new (SchemeManager.SchemesToSchemeName (Schemes.Error)!, CreateError ()),
                                                          new (SchemeManager.SchemesToSchemeName (Schemes.Dialog)!, CreateDialog ()),
                                                          new (SchemeManager.SchemesToSchemeName (Schemes.Menu)!, CreateMenu ())
                                                      ]
                                                     );

        Scheme CreateBase ()
        {
            var highlight = new Color ("LightGray");
            highlight = highlight.GetHighlightColor ();

            return new ()
            {
                Normal = new ("LightGray", "RaisinBlack"),
                //Focus = new ("White", "DarkGray", "Bold"),
                //HotNormal = new ("Silver", "RaisinBlack", "Underline"),
                //Disabled = new ("DarkGray", "RaisinBlack", "Faint"),
                //HotFocus = new ("White", "DarkGray", "Underline,Bold"),
                //Active = new ("White", "Charcoal"),
                //HotActive = new ("White", "Charcoal", "Underline"),
                //Highlight = new (highlight, new Color ("RaisinBlack")),
                //Editable = new ("LightYellow", "OuterSpace")

                //use algo: ReadOnly = new ("Gray", "RaisinBlack", "Italic")
            };
        }

        Scheme CreateToplevel ()
        {
            return new ()
            {
                Normal = new ("SlateGray", "OuterSpace"),
                //Focus = new ("White", "SlateGray"),
                //HotNormal = new ("LightGray", "OuterSpace", "Underline"),
                //Disabled = new ("DimGray", "OuterSpace", "Faint"),
                //HotFocus = new ("White", "SlateGray", "Underline"),
                //Active = new ("White", "DarkSlateGray", "Bold"),
                //HotActive = new ("White", "DarkSlateGray", "Underline,Bold"),
                //Highlight = new ("White", "Onyx"),
                //Editable = new ("LemonChiffon", "RaisinBlack"),
                //ReadOnly = new ("Silver", "OuterSpace", "Italic")
            };
        }

        Scheme CreateError ()
        {
            return new ()
            {
                Normal = new ("IndianRed", "RaisinBlack"),
                //Focus = new ("White", "IndianRed"),
                //HotNormal = new ("LightCoral", "RaisinBlack", "Underline"),
                //Disabled = new ("DarkGray", "RaisinBlack", "Faint"),
                //HotFocus = new ("White", "IndianRed", "Underline"),
                //Active = new ("White", "LightCoral", "Bold"),
                //HotActive = new ("White", "LightCoral", "Underline,Bold"),
                //Highlight = new ("White", "IndianRed"),
                //Editable = new ("LemonChiffon", "RaisinBlack"),
                //ReadOnly = new ("Silver", "RaisinBlack", "Italic")
            };
        }

        Scheme CreateDialog ()
        {
            return new ()
            {
                Normal = new ("Gainsboro", "SlateGray"),
                //Focus = new ("Black", "Gainsboro"),
                //HotNormal = new ("WhiteSmoke", "SlateGray", "Underline"),
                //Disabled = new ("Gray", "SlateGray", "Faint"),
                //HotFocus = new ("Black", "Gainsboro", "Underline"),
                //Active = new ("Black", "WhiteSmoke", "Bold"),
                //HotActive = new ("Black", "WhiteSmoke", "Underline,Bold"),
                //Highlight = new ("Black", "Gainsboro"),
                //Editable = new ("Black", "LemonChiffon"),
                //ReadOnly = new ("Silver", "SlateGray", "Italic")
            };
        }

        Scheme CreateMenu ()
        {
            return new ()
            {
                Normal = new ("Charcoal", "WhiteSmoke", "Bold"),
                //Focus = new ("Black", "SlateGray", "Bold"),
                //HotNormal = new ("Charcoal", "WhiteSmoke", "Underline,Bold"),
                //Disabled = new ("Gray", "Gainsboro", "Faint"),
                //HotFocus = new ("Black", "SlateGray", "Underline,Bold"),
                //Active = new ("White", "LightGray", "Bold"),
                //HotActive = new ("White", "LightGray", "Underline,Bold"),
                //Highlight = new ("White", "SlateGray"),
                //Editable = new ("Charcoal", "WhiteSmoke"),
                //ReadOnly = new ("Silver", "WhiteSmoke", "Italic")
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

        if (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotNormal, out Attribute? hotNormal))
        {
            _hotNormal = hotNormal;
        }
        else
        {
            _hotNormal = null;
        }

        if (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Focus, out Attribute? focus))
        {
            _focus = focus;
        }
        else
        {
            _focus = null;
        }

        if (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotFocus, out Attribute? hotFocus))
        {
            _hotFocus = hotFocus;
        }
        else
        {
            _hotFocus = null;
        }

        if (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out Attribute? active))
        {
            _active = active;
        }
        else
        {
            _active = null;
        }

        if (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotActive, out Attribute? hotActive))
        {
            _hotActive = hotActive;
        }
        else
        {
            _hotActive = null;
        }


        if (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Highlight, out Attribute? highlight))
        {
            _highlight = highlight;
        }
        else
        {
            _highlight = null;
        }


        if (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Editable, out Attribute? editable))
        {
            _editable = editable;
        }
        else
        {
            _editable = null;
        }

        if (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.ReadOnly, out Attribute? readOnly))
        {
            _readOnly = readOnly;
        }
        else
        {
            _readOnly = null;
        }


        if (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Disabled, out Attribute? disabled))
        {
            _disabled = disabled;
        }
        else
        {
            _disabled = null;
        }
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
        return attribute is {};
    }

    private Attribute GetAttributeForRoleCore (VisualRole role, HashSet<VisualRole> stack)
    {
        // Prevent infinite recursion
        if (!stack.Add (role))
        {
            return Normal; // fallback
        }

        Attribute? attr = role switch
        {
            VisualRole.Normal => Normal,
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

        if (attr is {} || role == VisualRole.Normal)
        {
            return attr!.Value;
        }

        // Derivation algorithm as documented
        Attribute result = role switch
                           {
                               VisualRole.Focus =>

                                   // Derived from Normal by swapping fg/bg or by applying highlight to background
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Normal, stack).Background,
                                       Background = GetAttributeForRoleCore (VisualRole.Normal, stack).Foreground,
                                   },


                               VisualRole.Active =>

                                   // Derived from Focus by adding Bold and by applying highlight to foreground and dim to background
                                   GetAttributeForRoleCore (VisualRole.Focus, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Focus, stack).Foreground.GetHighlightColor (),
                                       Background = GetAttributeForRoleCore (VisualRole.Focus, stack).Background.GetDimColor (),
                                       Style = GetAttributeForRoleCore (VisualRole.Focus, stack).Style | TextStyle.Bold,
                                   },

                               VisualRole.Highlight =>

                                   // Derived from Normal by applying highlight to foreground and dim to background
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Normal, stack).Background.GetHighlightColor (),
                                       Background = GetAttributeForRoleCore (VisualRole.Normal, stack).Background.GetDimColor(),
                                   },

                               VisualRole.Editable =>

                                   // Derived from Normal by applying highlight to foreground and highlight/dim to background
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Foreground = GetAttributeForRoleCore (VisualRole.Normal, stack).Background.GetHighlightColor(),
                                       Background = GetAttributeForRoleCore (VisualRole.Normal, stack).Background.GetHighlightColor ().GetDimColor (),
                                   },

                               VisualRole.ReadOnly =>

                                   // Derived from Editable by adding Faint
                                   GetAttributeForRoleCore (VisualRole.Editable, stack) with
                                   {
                                       Style = GetAttributeForRoleCore (VisualRole.Editable, stack).Style | TextStyle.Faint,
                                   },

                               VisualRole.Disabled =>

                                   // Derived from Normal by adding Faint
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Style = GetAttributeForRoleCore (VisualRole.Normal, stack).Style | TextStyle.Faint,
                                   },


                               VisualRole.HotNormal =>

                                   // Derived from Normal by adding Underline
                                   GetAttributeForRoleCore (VisualRole.Normal, stack) with
                                   {
                                       Style = GetAttributeForRoleCore (VisualRole.Normal, stack).Style | TextStyle.Underline,
                                   },

                               VisualRole.HotFocus =>

                                   // Derived from Focus by adding Underline
                                   GetAttributeForRoleCore (VisualRole.Focus, stack) with
                                   {
                                       Style = GetAttributeForRoleCore (VisualRole.Focus, stack).Style | TextStyle.Underline,
                                   },

                               VisualRole.HotActive =>

                                   // Derived from Active by adding Underline
                                   GetAttributeForRoleCore (VisualRole.Active, stack) with
                                   {
                                       Style = GetAttributeForRoleCore (VisualRole.Active, stack).Style | TextStyle.Underline,
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
    /// </summary>
    public Attribute HotNormal
    {
        get
        {
            if (_hotNormal is { })
            {
                return _hotNormal.Value;
            }
            return GetAttributeForRoleCore (VisualRole.HotNormal, []);
        }
        init => _hotNormal = value;
    }

    private readonly Attribute? _focus;

    /// <summary>
    ///     The visual role when the element is focused.
    /// </summary>
    public Attribute Focus
    {
        get
        {
            if (_focus is { })
            {
                return _focus.Value;
            }
            return GetAttributeForRoleCore (VisualRole.Focus, []);
        }
        init => _focus = value;
    }

    private readonly Attribute? _hotFocus;

    /// <summary>
    ///     The visual role for <see cref="Focus"/> elements with a <see cref="View.HotKey"/> indicator.
    /// </summary>
    public Attribute HotFocus
    {
        get
        {
            if (_hotFocus is { })
            {
                return _hotFocus.Value;
            }
            return GetAttributeForRoleCore (VisualRole.HotFocus, []);
        }
        init => _hotFocus = value;
    }

    private readonly Attribute? _active = null;

    /// <summary>
    ///     The visual role for elements that are active or selected (e.g., selected item in a <see cref="ListView"/>). Also
    ///     used
    ///     for headers in, <see cref="HexView"/>, <see cref="CharMap"/> and  <see cref="TabView"/>.
    /// </summary>
    public Attribute Active
    {
        get
        {
            if (_active is { })
            {
                return _active.Value;
            }

            return GetAttributeForRoleCore (VisualRole.Active, []);
        }
        init => _active = value;
    }

    private readonly Attribute? _hotActive;

    /// <summary>
    ///     The visual role for <see cref="Active"/> elements with a <see cref="View.HotKey"/> indicator.
    /// </summary>
    public Attribute HotActive
    {
        get
        {
            if (_hotActive is { })
            {
                return _hotActive.Value;
            }
            return GetAttributeForRoleCore (VisualRole.HotActive, []);
        }
        init => _hotActive = value;
    }

    private readonly Attribute? _highlight;

    /// <summary>
    ///     The visual role for elements that are highlighted (e.g., when the mouse is hovering over a <see cref="Button"/>).
    /// </summary>
    public Attribute Highlight
    {
        get
        {
            if (_highlight is { })
            {
                return _highlight.Value;
            }
            return GetAttributeForRoleCore (VisualRole.Highlight, []);
        }
        init => _highlight = value;
    }

    private readonly Attribute? _editable;

    /// <summary>
    ///     The visual role for elements that are editable (e.g., <see cref="TextField"/> and <see cref="TextView"/>).
    /// </summary>
    public Attribute Editable
    {
        get
        {
            if (_editable is { })
            {
                return _editable.Value;
            }
            return GetAttributeForRoleCore (VisualRole.Editable, []);
        }
        init => _editable = value;
    }

    private readonly Attribute? _readOnly;

    /// <summary>
    ///     The visual role for elements that are normally editable but currently read-only.
    /// </summary>
    public Attribute ReadOnly
    {
        get
        {
            if (_readOnly is { })
            {
                return _readOnly.Value;
            }
            return GetAttributeForRoleCore (VisualRole.ReadOnly, []);
        }
        init => _readOnly = value;
    }

    private readonly Attribute? _disabled;

    /// <summary>
    ///     The visual role for elements that are disabled and not interactable.
    /// </summary>
    public Attribute Disabled
    {
        get
        {
            if (_disabled is { })
            {
                return _disabled.Value;
            }
            return GetAttributeForRoleCore (VisualRole.Disabled, []);
        }
        init => _disabled = value;
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
