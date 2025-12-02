namespace Terminal.Gui.Examples;

/// <summary>
///     Represents a sequence of keystrokes to inject during example demonstration or testing.
/// </summary>
public class DemoKeyStrokeSequence
{
    /// <summary>
    ///     Gets or sets the array of keystroke names to inject.
    ///     Can include special "SetDelay:nnn" commands to change the delay between keys.
    /// </summary>
    public string [] KeyStrokes { get; set; } = [];

    /// <summary>
    ///     Gets or sets the order in which this sequence should be executed.
    /// </summary>
    public int Order { get; set; } = 0;
}
