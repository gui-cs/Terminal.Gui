#nullable enable

namespace Terminal.Gui;

public static partial class Application // Popover handling
{
    /// <summary>Gets the Application <see cref="Popover"/> manager.</summary>
    public static ApplicationPopover? Popover { get; internal set; }
}