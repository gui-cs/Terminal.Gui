#nullable enable
using System.ComponentModel;
using Microsoft.CodeAnalysis.Operations;

namespace Terminal.Gui;

public partial class View
{
    // TODO: See https://github.com/gui-cs/Terminal.Gui/issues/4014
    // TODO: See https://github.com/gui-cs/Terminal.Gui/issues/4016
    // TODO: Enable ability to tell if Scheme was explicitly set; Scheme, as is, hides this.
    internal Scheme? _scheme;

    /// <summary>The color scheme for this view, if it is not defined, it returns the <see cref="SuperView"/>'s color scheme.</summary>
    public virtual Scheme? Scheme
    {
        // BUGBUG: This prevents the ability to know if Scheme was explicitly set or not.
        get => _scheme ?? SuperView?.Scheme;
        set
        {
            if (_scheme == value)
            {
                return;
            }

            _scheme = value;

            // BUGBUG: This should be in Border.cs somehow
            if (Border is { } && Border.LineStyle != LineStyle.None && Border.Scheme is { })
            {
                Border.Scheme = _scheme;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>Determines the current <see cref="Scheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="Scheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Scheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetFocusColor ()
    {
        return GetAttributeForRole (VisualRole.Focus);

        Attribute currAttribute = Scheme?.Normal ?? Attribute.Default;
        var newAttribute = new Attribute ();
        CancelEventArgs<Attribute> args = new (in currAttribute, ref newAttribute);
        GettingFocusColor?.Invoke (this, args);

        if (args.Cancel)
        {
            return args.NewValue;
        }

        Scheme? cs = Scheme ?? new ();

        return Enabled ? GetColor (cs.Focus) : cs.Disabled;
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

        Attribute currAttribute = Scheme?.Normal ?? Attribute.Default;
        var newAttribute = new Attribute ();
        CancelEventArgs<Attribute> args = new (in currAttribute, ref newAttribute);
        GettingHotFocusColor?.Invoke (this, args);

        if (args.Cancel)
        {
            return args.NewValue;
        }

        Scheme? cs = Scheme ?? new ();

        return Enabled ? GetColor (cs.HotFocus) : cs.Disabled;
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

        Attribute currAttribute = Scheme?.Normal ?? Attribute.Default;
        var newAttribute = new Attribute ();
        CancelEventArgs<Attribute> args = new (in currAttribute, ref newAttribute);
        GettingHotNormalColor?.Invoke (this, args);

        if (args.Cancel)
        {
            return args.NewValue;
        }

        Scheme? cs = Scheme ?? new ();

        return Enabled ? GetColor (cs.HotNormal) : cs.Disabled;
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

        Attribute currAttribute = Scheme?.Normal ?? Attribute.Default;
        var newAttribute = new Attribute ();
        CancelEventArgs<Attribute> args = new (in currAttribute, ref newAttribute);
        GettingNormalColor?.Invoke (this, args);

        if (args.Cancel)
        {
            return args.NewValue;
        }

        Scheme? cs = Scheme ?? new ();
        Attribute disabled = new (cs.Disabled.Foreground, cs.Disabled.Background);

        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Hover) && _hovering)
        {
            disabled = new (disabled.Foreground.GetDarkerColor (), disabled.Background.GetDarkerColor ());
        }

        return Enabled ? GetColor (cs.Normal) : disabled;
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
    protected virtual Attribute GetAttributeForRole (VisualRole role)
    {
        Scheme scheme = Scheme ?? ThemeManager.GetDefaultSchemes () ["Base"]!;
        Attribute currAttribute = GetAttributeForRole (scheme, role);

        var newAttribute = new Attribute ();
        VisualRoleEventArgs args = new (role, ref newAttribute);
        GettingAttributeForRole?.Invoke (this, args);

        if (args.Cancel)
        {
            return args.NewValue;
        }

        // BUGBUG: This broke ViewDiagnosticFlags.Hover

        return GetAttributeForRole (scheme, role);
    }

    /// <summary>
    ///     Raised the Attribute for a VisualRole is being retrieved, from <see cref="GetAttributeForRole(Terminal.Gui.VisualRole)"/>. Cancel the event and set the new
    ///     attribute in the event args to
    ///     a different value to change the attribute.
    /// </summary>
    public event EventHandler<VisualRoleEventArgs>? GettingAttributeForRole;

    /// <summary>
    /// Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>.
    /// </summary>
    /// <param name="scheme">The scheme to use.</param>
    /// <param name="role">The semantic <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <returns>The corresponding <see cref="Attribute"/> from the <see cref="Scheme"/>.</returns>
    protected Attribute GetAttributeForRole (Scheme scheme, VisualRole role)
    {
        return role switch
               {
                   VisualRole.Normal => scheme.Normal,
                   VisualRole.HotNormal => scheme.HotNormal,
                   VisualRole.Focus => scheme.Focus,
                   VisualRole.HotFocus => scheme.HotFocus,
                   //VisualRole.Active => scheme.Active,
                   //VisualRole.HotActive => scheme.HotActive,
                   VisualRole.Disabled => scheme.Disabled,
                   //VisualRole.ReadOnly => scheme.ReadOnly,
                   _ => scheme.Normal
               };
    }

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

    private Attribute GetColor (Attribute inputAttribute)
    {
        Attribute attr = inputAttribute;

        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Hover) && _hovering)
        {
            attr = new (attr.Foreground.GetDarkerColor (), attr.Background.GetDarkerColor ());
        }

        return attr;
    }
}