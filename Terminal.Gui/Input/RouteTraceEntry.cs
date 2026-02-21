namespace Terminal.Gui.Input;

/// <summary>
///     Captures information about a command routing step for debugging and testing.
/// </summary>
/// <param name="ViewId">Identifying string of the view (e.g., from <c>ToIdentifyingString()</c>).</param>
/// <param name="Command">The command being routed.</param>
/// <param name="Routing">The routing mode at this point.</param>
/// <param name="Phase">The phase of routing (Entry, Exit, etc.).</param>
/// <param name="Method">The method name where the trace occurred.</param>
/// <param name="Message">Optional additional message.</param>
/// <param name="Timestamp">When the trace was captured.</param>
public readonly record struct RouteTraceEntry (string ViewId,
                                               Command Command,
                                               CommandRouting Routing,
                                               CommandTracePhase Phase,
                                               string Method,
                                               string? Message,
                                               DateTime Timestamp);
