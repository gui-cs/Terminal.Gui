#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Interface identifying a View as being capable of being a Popover.
/// </summary>
public interface IPopover
{
    /// <summary>
    ///     Gets or sets the <see cref="Toplevel"/> that this Popover is associated with. If null, it is not associated with
    ///     any Toplevel and will receive all keyboard
    ///     events from the <see cref="Application"/>. If set, it will only receive keyboard events the Toplevel would normally
    ///     receive.
    ///     When <see cref="ApplicationPopover.Register"/> is called, the <see cref="Toplevel"/> is set to the current
    ///     <see cref="Application.Top"/> if not already set.
    /// </summary>
    public Toplevel? Toplevel { get; set; }
}
