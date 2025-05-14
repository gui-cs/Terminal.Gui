#nullable enable
using System.ComponentModel;
using Microsoft.CodeAnalysis.Operations;

namespace Terminal.Gui;

public partial class View
{
    // TODO: See https://github.com/gui-cs/Terminal.Gui/issues/4014

    /// <summary>
    ///     Gets the hard-coded set of <see cref="Scheme"/>s. Used for generating the built-in config.json and for
    ///     unit tests that don't depend on ConfigurationManager.
    /// </summary>
    /// <returns></returns>
    internal static Dictionary<string, Scheme?> GetHardCodedSchemes ()
    {
        return new (StringComparer.InvariantCultureIgnoreCase)
        {
            {
                "TopLevel", new Scheme (
                                        normal: new Attribute (new Color ("BrightGreen"), new Color ("#505050")),
                                        focus: new Attribute (new Color ("White"), new Color ("#696969")),
                                        hotNormal: new Attribute (new Color ("Yellow"), new Color ("#505050")),
                                        hotFocus: new Attribute (new Color ("Yellow"), new Color ("#505050")),
                                        disabled: new Attribute (new Color ("DarkGray"), new Color ("#505050"))
                                       )
            },
            {
                "Base", new Scheme (
                                    normal: new Attribute (new Color ("White"), new Color ("Blue")),
                                    focus: new Attribute (new Color ("DarkBlue"), new Color ("LightGray")),
                                    hotNormal: new Attribute (new Color ("BrightCyan"), new Color ("Blue")),
                                    hotFocus: new Attribute (new Color ("BrightBlue"), new Color ("LightGray")),
                                    disabled: new Attribute (new Color ("DarkGray"), new Color ("Blue"))
                                   )
            },
            {

                "Dialog", new Scheme (
                                      normal: new Attribute (new Color ("Black"), new Color ("LightGray")),
                                      focus: new Attribute (new Color ("DarkGray"), new Color ("LightGray")),
                                      hotNormal: new Attribute (new Color ("Blue"), new Color ("LightGray")),
                                      hotFocus: new Attribute (new Color ("BrightBlue"), new Color ("LightGray")),
                                      disabled: new Attribute (new Color ("Gray"), new Color ("DarkGray"))
                                     )
            },
            {
                "Menu", new Scheme (
                                    normal: new Attribute (new Color ("White"), new Color ("DarkBlue")),
                                    focus: new Attribute (new Color ("DarkBlue"), new Color ("White")),
                                    hotNormal: new Attribute (new Color ("Yellow"), new Color ("DarkBlue")),
                                    hotFocus: new Attribute (new Color ("Blue"), new Color ("White")),
                                    disabled: new Attribute (new Color ("Gray"), new Color ("DarkGray"))
                                   )
            },
            {
                "Error", new Scheme (
                                     normal: new Attribute (new Color ("Red"), new Color ("Pink")),
                                     focus: new Attribute (new Color ("White"), new Color ("BrightRed")),
                                     hotNormal: new Attribute (new Color ("Black"), new Color ("Pink")),
                                     hotFocus: new Attribute (new Color ("Pink"), new Color ("BrightRed")),
                                     disabled: new Attribute (new Color ("DarkGray"), new Color ("White"))
                                    )
            }
        };
    }

    // Both holds the set Scheme and is used to determine if a Scheme has been set or not
    private Scheme? _scheme;

    /// <summary>
    ///     Gets whether the Scheme has been explicitly set for this View.
    /// </summary>
    public bool HasScheme => _scheme is { };

    /// <summary>Gets or sets the Scheme for this view. If the Scheme has not been explicitly set (see <see cref="HasScheme"/>), gets <see cref="SuperView"/>'s Scheme.</summary>
    public Scheme? Scheme
    {
        get => GetScheme ();
        set => SetScheme (value);
    }


    /// <summary>
    ///     Gets the Scheme for the View. If the Scheme has not been explicitly set (see <see cref="HasScheme"/>), gets <see cref="SuperView"/>'s Scheme.
    /// </summary>
    /// <returns></returns>
    public Scheme? GetScheme ()
    {
        if (OnGettingScheme (out Scheme? newScheme))
        {
            return newScheme;
        }

        var args = new SchemeEventArgs (in _scheme, ref newScheme);
        GettingScheme?.Invoke (this, args);
        if (args.Cancel)
        {
            return args.NewScheme;
        }

        if (!HasScheme)
        {
            return SuperView?.Scheme ?? SchemeManager.GetCurrentSchemes () ["Base"]!;
        }

        return _scheme;
    }


    /// <summary>
    ///     Called when the <see cref="Scheme"/> for the View is being retrieved. Overrides can return <see langword="true"/> to
    ///     stop further processing and optionally set <paramref name="scheme"/> to a different value.
    /// </summary>
    /// <returns><see langword="true"/> to stop default behavior.</returns>
    protected virtual bool OnGettingScheme (out Scheme? scheme)
    {
        scheme = null;
        return false;
    }

    /// <summary>
    ///     Raised when the <see cref="Scheme"/> for the View is being retrieved. Overrides can return <see langword="true"/> to
    ///     stop further processing and optionally set the <see cref="Scheme"/> in the event args to a different value.
    /// </summary>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop default behavior.
    /// </returns>
    public event EventHandler<CancelEventArgs>? GettingScheme;

    /// <summary>
    ///     Sets the Scheme for the View. Raises <see cref="SettingScheme"/> event before setting the scheme. 
    /// </summary>
    /// <param name="scheme">The scheme to set. If <see langword="null"/> <see cref="HasScheme"/> will be <see langword="false"/>.</param>
    /// <returns><see langword="true"/> if the scheme was set.</returns>
    public bool SetScheme (Scheme? scheme)
    {
        if (_scheme == scheme)
        {
            return false;
        }

        if (OnSettingScheme (in scheme))
        {
            return false;
        }

        var args = new CancelEventArgs ();
        SettingScheme?.Invoke (this, args);
        if (args.Cancel)
        {
            return false;
        }

        _scheme = scheme;

        // BUGBUG: This should be in Border.cs somehow
        if (Border is { } && Border.LineStyle != LineStyle.None && Border.HasScheme)
        {
            Border.Scheme = _scheme;
        }

        SetNeedsDraw ();
        return true;
    }

    /// <summary>
    ///     Called when the <see cref="Scheme"/> for the View is to be set. 
    /// </summary>
    /// <param name="scheme"></param>
    /// <returns><see langword="true"/> to stop default behavior.</returns>
    protected virtual bool OnSettingScheme (in Scheme? scheme) { return false; }


    #region VisualRole

    /// <summary>Raised when the <see cref="Scheme"/> for the View is to be set.</summary>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop default behavior.
    /// </returns>
    public event EventHandler<CancelEventArgs>? SettingScheme;

    /// <summary>Determines the current <see cref="Scheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="Scheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Scheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetFocusColor ()
    {
        return GetAttributeForRole (VisualRole.Focus);
    }

    /// <summary>
    ///     Raised the Focus Color is being retrieved, from <see cref="GetFocusColor"/>. Cancel the event and set the new
    ///     attribute in the event args to
    ///     a different value to change the focus color.
    /// </summary>
    public event EventHandler<CancelEventArgs<Attribute>>? GettingFocusColor;

    /// <summary>Determines the current <see cref="Scheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="Scheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Scheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetHotFocusColor ()
    {
        return GetAttributeForRole (VisualRole.HotFocus);
    }

    /// <summary>
    ///     Raised the HotFocus Color is being retrieved, from <see cref="GetHotFocusColor"/>. Cancel the event and set the new
    ///     attribute in the event args to
    ///     a different value to change the focus color.
    /// </summary>
    public event EventHandler<CancelEventArgs<Attribute>>? GettingHotFocusColor;

    /// <summary>Determines the current <see cref="Scheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="Scheme.HotNormal"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Scheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetHotNormalColor ()
    {
        return GetAttributeForRole (VisualRole.HotNormal);
    }

    /// <summary>
    ///     Raised the HotNormal Color is being retrieved, from <see cref="GetHotNormalColor"/>. Cancel the event and set the
    ///     new attribute in the event args to
    ///     a different value to change the focus color.
    /// </summary>
    public event EventHandler<CancelEventArgs<Attribute>>? GettingHotNormalColor;

    /// <summary>Determines the current <see cref="Scheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="Scheme.Normal"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Scheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetNormalColor ()
    {
        return GetAttributeForRole (VisualRole.Normal);
    }

    /// <summary>
    ///     Raised the Normal Color is being retrieved, from <see cref="GetNormalColor"/>. Cancel the event and set the new
    ///     attribute in the event args to
    ///     a different value to change the focus color.
    /// </summary>
    public event EventHandler<CancelEventArgs<Attribute>>? GettingNormalColor;

    /// <summary>
    /// Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>.
    /// </summary>
    /// <param name="role">The semantic <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <returns>The corresponding <see cref="Attribute"/> from the <see cref="Scheme"/>.</returns>
    public Attribute GetAttributeForRole (VisualRole role)
    {
        Attribute curAttribute = GetScheme ()!.GetAttributeForRole (role);

        if (OnGettingAttributeForRole (role, ref curAttribute))
        {
            // The implementation may have changed the attribute
            return curAttribute;
        }

        VisualRoleEventArgs args = new (role, newValue: ref curAttribute, currentValue: ref curAttribute);
        GettingAttributeForRole?.Invoke (this, args);

        if (args.Cancel)
        {
            // A handler may have changed the attribute
            return args.NewValue;
        }

        return Enabled || role == VisualRole.Disabled ? curAttribute : GetAttributeForRole (VisualRole.Disabled);
    }

    /// <summary>
    ///     Called when the Attribute for a <see cref="GetAttributeForRole(Terminal.Gui.VisualRole)"/> is being retrieved. Implementations can
    ///     return <see langword="true"/> to stop further processing and optionally set the <see cref="Attribute"/> in the event args to a different value.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="currentAttribute">The current value of the Attribute for the VisualRole. This by-ref value can be changed</param>
    /// <returns></returns>
    protected virtual bool OnGettingAttributeForRole (VisualRole role, ref Attribute currentAttribute)
    {
        return false;
    }

    /// <summary>
    ///     Raised when the Attribute for a <see cref="GetAttributeForRole(Terminal.Gui.VisualRole)"/> is being retrieved. Handelers should check if <see cref="CancelEventArgs.Cancel"/>
    ///     has been set to <see langword="true"/> and do nothing if so. If Cancel is <see langword="false"/>
    ///     a handler can set it to <see langword="true"/> to stop further processing optionally change the <see cref="VisualRoleEventArgs.CurrentValue"/> in the event args to a different value.
    /// </summary>
    public event EventHandler<VisualRoleEventArgs>? GettingAttributeForRole;


    /// <summary>
    ///     Sets the Normal attribute if the setting process is not canceled. It triggers an event and checks for
    ///     cancellation before proceeding.
    /// </summary>
    public void SetNormalAttribute ()
    {
        if (OnSettingNormalAttribute ())
        {
            return;
        }

        var args = new CancelEventArgs ();
        SettingNormalAttribute?.Invoke (this, args);

        if (args.Cancel)
        {
            return;
        }

        if (Scheme is { })
        {
            SetAttribute (GetNormalColor ());
        }
    }

    /// <summary>
    ///     Called when the normal attribute for the View is to be set. This is called before the View is drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop default behavior.</returns>
    protected virtual bool OnSettingNormalAttribute () { return false; }

    /// <summary>Raised  when the normal attribute for the View is to be set. This is raised before the View is drawn.</summary>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop default behavior.
    /// </returns>
    public event EventHandler<CancelEventArgs>? SettingNormalAttribute;

    private Attribute GetDiagnosticsColor (Attribute inputAttribute)
    {
        Attribute attr = inputAttribute;

        //if (Diagnostics.HasFlag (ViewDiagnosticFlags.Hover) && _hovering)
        //{
        //    attr = new (attr.Foreground.GetDarkerColor (), attr.Background.GetDarkerColor ());
        //}

        return attr;
    }

    #endregion VisualRole

}