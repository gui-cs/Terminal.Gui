
namespace Terminal.Gui.Drivers;

/// <summary>
///     Interface for classes responsible for reporting the current
///     size of the terminal window.
/// </summary>
public interface ISizeMonitor
{
    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <summary>
    ///     Examines the current size of the terminal and raises <see cref="SizeChanged"/> if it is different
    ///     from last inspection.
    /// </summary>
    /// <returns></returns>
    bool Poll ();
}
