namespace Terminal.Gui.Input;

/// <summary>
///     Describes a phase of command routing for tracing purposes.
/// </summary>
public enum CommandTracePhase
{
    /// <summary>Entry to a method or handler.</summary>
    Entry,

    /// <summary>Exit from a method or handler.</summary>
    Exit,

    /// <summary>Routing decision point (e.g., bubble, dispatch).</summary>
    Routing,

    /// <summary>Event raised (e.g., Accepting, Activating).</summary>
    Event,

    /// <summary>Handler invocation.</summary>
    Handler
}
