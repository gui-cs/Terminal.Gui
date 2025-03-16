#nullable enable
using static Unix.Terminal.Curses;

namespace Terminal.Gui;

public static partial class Application // Popover handling
{
    /// <summary>Gets the Application <see cref="PopoverHost"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         Any View added as a SubView will be a Popover.
    ///     </para>
    ///     <para>
    ///         To show or hide a Popover, set the <see cref="View.Visible"/> property of the PopoverHost.
    ///     </para>
    ///     <para>
    ///         If the user clicks anywhere not occulded by a SubView of the PopoverHost, the PopoverHost will be hidden.
    ///     </para>
    /// </remarks>
    public static PopoverHost? PopoverHost { get; set; }
}