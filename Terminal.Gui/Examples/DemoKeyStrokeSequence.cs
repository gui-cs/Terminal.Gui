namespace Terminal.Gui.Examples;

/// <summary>
///     Represents a sequence of keystrokes to inject during example demonstration or testing.
/// </summary>
public class DemoKeyStrokeSequence
{
    /// <summary>
    ///     Gets or sets the array of keystroke names to inject.
    /// </summary>
    public string [] KeyStrokes { get; set; } = [];

    /// <summary>
    ///     Gets or sets the delay in milliseconds before injecting these keystrokes.
    /// </summary>
    public int DelayMs { get; set; } = 0;

    /// <summary>
    ///     Gets or sets the order in which this sequence should be executed.
    /// </summary>
    public int Order { get; set; } = 0;
}
