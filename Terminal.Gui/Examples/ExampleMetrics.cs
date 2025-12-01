namespace Terminal.Gui.Examples;

/// <summary>
///     Contains performance and execution metrics collected during an example's execution.
/// </summary>
public class ExampleMetrics
{
    /// <summary>
    ///     Gets or sets the time when the example started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    ///     Gets or sets the time when initialization completed.
    /// </summary>
    public DateTime? InitializedAt { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether initialization completed successfully.
    /// </summary>
    public bool InitializedSuccessfully { get; set; }

    /// <summary>
    ///     Gets or sets the number of iterations executed.
    /// </summary>
    public int IterationCount { get; set; }

    /// <summary>
    ///     Gets or sets the time when shutdown began.
    /// </summary>
    public DateTime? ShutdownAt { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether shutdown completed gracefully.
    /// </summary>
    public bool ShutdownGracefully { get; set; }

    /// <summary>
    ///     Gets or sets the number of times the screen was cleared.
    /// </summary>
    public int ClearedContentCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of times views were drawn.
    /// </summary>
    public int DrawCompleteCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of times views were laid out.
    /// </summary>
    public int LaidOutCount { get; set; }
}
