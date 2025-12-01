namespace Terminal.Gui.Examples;

/// <summary>
///     Defines the execution mode for running an example application.
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    ///     Run the example in a separate process.
    ///     This provides full isolation but makes debugging more difficult.
    /// </summary>
    OutOfProcess,

    /// <summary>
    ///     Run the example in the same process by loading its assembly and invoking its entry point.
    ///     This allows for easier debugging but may have side effects from shared process state.
    /// </summary>
    InProcess
}
