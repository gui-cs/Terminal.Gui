#nullable enable
namespace Terminal.Gui;

/// <summary>
///     A helper class used by <see cref="Shortcut"/> to display the key. Reverses the Normal and HotNormal colors.
/// </summary>
public class ShortcutKeyView : View
{
    /// <inheritdoc />
    protected override bool OnGettingAttributeForRole (VisualRole role, ref Attribute currentAttribute)
    {
        if (role == VisualRole.Normal && SuperView is { HasFocus: true })
        {
            currentAttribute = GetAttributeForRole (VisualRole.HotFocus);

            return true;
        }
        return base.OnGettingAttributeForRole (role, ref currentAttribute);
    }
}
