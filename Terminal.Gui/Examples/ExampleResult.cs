namespace Terminal.Gui.Examples;

/// <summary>
///     Contains the result of running an example application.
/// </summary>
public class ExampleResult
{
    /// <summary>
    ///     Gets or sets a value indicating whether the example completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Gets or sets the exit code of the example process (for out-of-process execution).
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the example timed out.
    /// </summary>
    public bool TimedOut { get; set; }

    /// <summary>
    ///     Gets or sets any error message that occurred during execution.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Gets or sets the performance metrics collected during execution.
    /// </summary>
    public ExampleMetrics? Metrics { get; set; }

    /// <summary>
    ///     Gets or sets the standard output captured during execution.
    /// </summary>
    public string? StandardOutput { get; set; }

    /// <summary>
    ///     Gets or sets the standard error captured during execution.
    /// </summary>
    public string? StandardError { get; set; }
}
