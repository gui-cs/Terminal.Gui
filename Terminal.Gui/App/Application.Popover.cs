
namespace Terminal.Gui.App;

public static partial class Application // Popover handling
{
    /// <summary>Gets the Application <see cref="Popover"/> manager.</summary>
    [Obsolete ("The legacy static Application object is going away.")]
    public static ApplicationPopover? Popover
    {
        get => ApplicationImpl.Instance.Popover;
        internal set => ApplicationImpl.Instance.Popover = value;
    }
}
