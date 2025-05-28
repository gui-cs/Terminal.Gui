#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

public partial class View
{
    #region Get

    /// <summary>Gets the current <see cref="Attribute"/> used by <see cref="AddRune(System.Text.Rune)"/>.</summary>
    /// <returns>The current attribute.</returns>
    public Attribute GetCurrentAttribute () { return Driver?.GetAttribute () ?? Attribute.Default; }

    /// <summary>
    ///     Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>
    ///     from the <see cref="Scheme"/>.
    ///     <para>
    ///         Raises <see cref="OnGettingAttributeForRole"/>/<see cref="GettingAttributeForRole"/>
    ///         which can cancel the default behavior, and optionally change the attribute in the event args.
    ///     </para>
    ///     <para>
    ///         If the View is disabled (<see cref="Enabled"/> is <see langword="false"/>), <see cref="VisualRole.Disabled"/>
    ///         will be used instead of <paramref name="role"/>. Cancel the event to override this behavior.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     If the View is disabled (<see cref="Enabled"/> is <see langword="false"/>), <see cref="VisualRole.Disabled"/>
    ///     will be used instead of <paramref name="role"></paramref>.
    ///     To override this behavior use  <see cref="OnGettingAttributeForRole"/>/<see cref="GettingAttributeForRole"/>
    ///     to cancel the method, and return a different attribute.
    /// </remarks>
    /// <param name="role">The semantic <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <returns>The corresponding <see cref="Attribute"/> from the <see cref="Scheme"/>.</returns>
    public Attribute GetAttributeForRole (VisualRole role)
    {
        Attribute schemeAttribute = GetScheme ()!.GetAttributeForRole (role);

        if (OnGettingAttributeForRole (role, ref schemeAttribute))
        {
            // The implementation may have changed the attribute
            return schemeAttribute;
        }

        VisualRoleEventArgs args = new (role, newValue: ref schemeAttribute, currentValue: ref schemeAttribute);
        GettingAttributeForRole?.Invoke (this, args);

        if (args.Cancel)
        {
            // A handler may have changed the attribute
            return args.NewValue;
        }

        if (HighlightStyle != HighlightStyle.None)
        {
            if (MouseHovering && HighlightStyle.HasFlag (HighlightStyle.Hover) && role != VisualRole.Highlight && role != VisualRole.Disabled)
            {
                schemeAttribute = GetAttributeForRole (VisualRole.Highlight);
            }
        }

        return Enabled || role == VisualRole.Disabled ? schemeAttribute : GetAttributeForRole (VisualRole.Disabled);
    }

    /// <summary>
    ///     Called when the Attribute for a <see cref="GetAttributeForRole(Terminal.Gui.VisualRole)"/> is being retrieved.
    ///     Implementations can
    ///     return <see langword="true"/> to stop further processing and optionally set the <see cref="Attribute"/> in the
    ///     event args to a different value.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="currentAttribute">The current value of the Attribute for the VisualRole. This by-ref value can be changed</param>
    /// <returns></returns>
    protected virtual bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute) { return false; }

    /// <summary>
    ///     Raised when the Attribute for a <see cref="GetAttributeForRole(Terminal.Gui.VisualRole)"/> is being retrieved.
    ///     Handlers should check if <see cref="CancelEventArgs.Cancel"/>
    ///     has been set to <see langword="true"/> and do nothing if so. If Cancel is <see langword="false"/>
    ///     a handler can set it to <see langword="true"/> to stop further processing optionally change the
    ///     `CurrentValue` in the event args to a different value.
    /// </summary>
    public event EventHandler<VisualRoleEventArgs>? GettingAttributeForRole;

    #endregion Get

    #region Set

    /// <summary>
    ///     Selects the specified Attribute
    ///     as the Attribute to use for subsequent calls to <see cref="AddRune(System.Text.Rune)"/> and <see cref="AddStr"/>.
    /// </summary>
    /// <param name="attribute">THe Attribute to set.</param>
    /// <returns>The previously set Attribute.</returns>
    public Attribute SetAttribute (Attribute attribute) { return Driver?.SetAttribute (attribute) ?? Attribute.Default; }

    /// <summary>
    ///     Selects the Attribute associated with the specified <see cref="VisualRole"/>
    ///     as the Attribute to use for subsequent calls to <see cref="AddRune(System.Text.Rune)"/> and <see cref="AddStr"/>.
    ///     <para>
    ///         Calls <see cref="GetAttributeForRole"/> to get the Attribute associated with the specified role, which will
    ///         raise <see cref="OnGettingAttributeForRole"/>/<see cref="GettingAttributeForRole"/>.
    ///     </para>
    ///     <para>
    ///         Then, raises <see cref="OnSettingAttributeForRole"/>/<see cref="SettingAttributeForRole"/> and checks for
    ///         cancellation
    ///         before setting the Attribute via <see cref="SetAttribute"/>.
    ///     </para>
    /// </summary>
    /// <param name="role">The semantic <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <returns>The previously set Attribute.</returns>
    public Attribute? SetAttributeForRole (VisualRole role)
    {
        Attribute schemeAttribute = GetAttributeForRole (role);
        Attribute currentAttribute = GetCurrentAttribute ();

        if (OnSettingAttributeForRole (in role, in currentAttribute, ref schemeAttribute))
        {
            return currentAttribute;
        }

        var args = new VisualRoleEventArgs (role, in currentAttribute, ref schemeAttribute);
        SettingAttributeForRole?.Invoke (this, args);

        if (args.Cancel)
        {
            return currentAttribute;
        }

        return SetAttribute (schemeAttribute);
    }

    /// <summary>
    ///     Raised when the Attribute associated with the specified <see cref="VisualRole"/> for the View being set.
    ///     This is raised by <see cref="SetAttributeForRole"/>.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> to cancel the setting of the attribute.
    /// </returns>
    protected virtual bool OnSettingAttributeForRole (in VisualRole role, in Attribute currentAttribute, ref Attribute newAttribute) { return false; }

    /// <summary>
    ///     Raised when the Attribute associated with the specified <see cref="VisualRole"/> for the View being set.
    ///     This is raised by <see cref="SetAttributeForRole"/>.
    /// </summary>
    /// <para>
    ///     Set `Cancel` to <see langword="true"/> to cancel the setting of the attribute.
    /// </para>
    public event EventHandler<VisualRoleEventArgs>? SettingAttributeForRole;

    #endregion Set
}
