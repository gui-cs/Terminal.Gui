namespace Terminal.Gui.Input;

/// <summary>
///     Interface for command trace backends. Implement this to capture command routing traces
///     for logging, testing, or debugging.
/// </summary>
public interface ICommandTraceBackend
{
    /// <summary>
    ///     Logs a route trace entry.
    /// </summary>
    /// <param name="entry">The trace entry to log.</param>
    void Log (RouteTraceEntry entry);

    /// <summary>
    ///     Clears any captured entries. For backends that don't capture, this is a no-op.
    /// </summary>
    void Clear ();
}
