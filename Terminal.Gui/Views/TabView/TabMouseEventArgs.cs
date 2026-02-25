
using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>Describes a mouse event over a specific <see cref="Tab"/> in a <see cref="TabView"/>.</summary>
public class TabMouseEventArgs : HandledEventArgs
{
    /// <summary>Creates a new instance of the <see cref="TabMouseEventArgs"/> class.</summary>
    /// <param name="tab"><see cref="Tab"/> that the mouse was over when the event occurred.</param>
    /// <param name="mouse">The mouse activity being reported</param>
    public TabMouseEventArgs (Tab? tab, Mouse mouse)
    {
        Tab = tab;
        MouseEvent = mouse;
    }

    /// <summary>
    ///     Gets the actual mouse event.  Use <see cref="HandledEventArgs.Handled"/> to cancel this event and perform custom
    ///     behavior (e.g. show a context menu).
    /// </summary>
    public Mouse MouseEvent { get; }

    /// <summary>Gets the <see cref="Tab"/> (if any) that the mouse was over when the <see cref="MouseEvent"/> occurred.</summary>
    /// <remarks>This will be null if the click is after last tab or before first.</remarks>
    public Tab? Tab { get; }
}
