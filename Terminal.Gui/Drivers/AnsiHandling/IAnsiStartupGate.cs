namespace Terminal.Gui.Drivers;

/// <summary>
///     Tracks ANSI startup queries that must complete before startup rendering is considered ready.
/// </summary>
public interface IAnsiStartupGate
{
    /// <summary>
    ///     Registers a startup query that must complete before readiness.
    /// </summary>
    /// <param name="name">Stable query name used for diagnostics and completion.</param>
    /// <param name="timeout">Maximum time to wait before marking the query as timed out.</param>
    /// <returns>An <see cref="IDisposable"/> that marks the query complete when disposed.</returns>
    IDisposable RegisterQuery (string name, TimeSpan timeout);

    /// <summary>
    ///     Marks the named startup query as complete.
    /// </summary>
    /// <param name="name">The query name passed to <see cref="RegisterQuery"/>.</param>
    void MarkComplete (string name);

    /// <summary>
    ///     Gets whether all registered startup queries are complete or timed out.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    ///     Gets the names of startup queries that are still pending.
    /// </summary>
    IReadOnlyCollection<string> PendingQueryNames { get; }
}
