#nullable enable
namespace Terminal.Gui;

/// <summary>
///     A helper class used by <see cref="Shortcut"/> to display the key with <see cref="VisualRole.HotFocus"/>.
/// </summary>
public class ShortcutKeyView : View
{
    /// <inheritdoc />
    protected override bool OnGettingAttributeForRole (VisualRole role, ref Attribute currentAttribute)
    {
        if (role != VisualRole.Normal)
        {
            return base.OnGettingAttributeForRole (role, ref currentAttribute);
        }

        currentAttribute = SuperView?.GetAttributeForRole (HasFocus ? VisualRole.HotFocus : VisualRole.HotNormal) ?? Attribute.Default;

        return true;
    }

    /// <inheritdoc />
    protected override bool OnClearingViewport ()
    {
        // No need to clear. If we do need to clear, then we need to strip off Underline...
        return true;
    }
}
