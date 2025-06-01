#nullable enable

namespace Terminal.Gui.Drawing;

/// <summary>Args for events that relate <see cref="VisualRole"/>.</summary>
public class VisualRoleEventArgs : CancelEventArgs<Attribute>
{
    /// <inheritdoc/>
    public VisualRoleEventArgs (in VisualRole role, ref readonly Attribute currentValue, ref Attribute newValue, bool cancel = false) : base (
                                                                                                                                              in currentValue,
                                                                                                                                              ref newValue,
                                                                                                                                              cancel)
    {
        Role = role;
    }

    /// <inheritdoc/>
    protected VisualRoleEventArgs (in VisualRole role, ref readonly Attribute currentValue, ref Attribute newValue) : base (currentValue, newValue)
    {
        Role = role;
    }

    /// <inheritdoc/>
    public VisualRoleEventArgs (in VisualRole role, ref Attribute newValue) : base (default (Attribute), newValue) { Role = role; }

    /// <summary>
    ///     The <see cref="VisualRole"/> that is being set.
    /// </summary>
    public VisualRole Role { get; set; }
}
