namespace Terminal.Gui.Testing;

/// <summary>
///     Base class for input events in sequences.
/// </summary>
public abstract record InputEvent
{
    /// <summary>
    ///     Optional delay before processing this event.
    /// </summary>
    public TimeSpan? Delay { get; init; }
}
