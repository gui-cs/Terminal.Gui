using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Terminal.Gui.Input;

/// <summary>
///     Provides command routing tracing for debugging and testing. Traces are only captured
///     in DEBUG builds and when a non-null backend is configured.
/// </summary>
/// <remarks>
///     <para>
///         By default, tracing uses <see cref="NullBackend"/> which does nothing.
///         Set <see cref="Backend"/> to a different implementation to enable tracing:
///     </para>
///     <list type="bullet">
///         <item><see cref="LoggingBackend"/> - forwards traces to <see cref="Logging.Debug"/></item>
///         <item><see cref="ListBackend"/> - captures traces to a list for testing</item>
///     </list>
///     <para>
///         All <c>TraceRoute</c> methods are marked with <c>[Conditional("DEBUG")]</c>,
///         so they have zero overhead in Release builds.
///     </para>
/// </remarks>
public static class CommandTrace
{
    private static readonly AsyncLocal<ICommandTraceBackend?> _asyncLocalBackend = new ();
    private static readonly NullBackend _defaultBackend = new ();

    /// <summary>
    ///     Gets or sets the trace backend for the current async context. Default is <see cref="NullBackend"/>.
    ///     Each async flow (test, thread) has its own backend instance.
    /// </summary>
    public static ICommandTraceBackend Backend
    {
        get => _asyncLocalBackend.Value ?? _defaultBackend;
        set => _asyncLocalBackend.Value = value;
    }

    /// <summary>
    ///     Traces a command routing step.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="command">The command being routed.</param>
    /// <param name="routing">The current routing mode.</param>
    /// <param name="phase">The phase of routing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void TraceRoute (View view,
                                   Command command,
                                   CommandRouting routing,
                                   CommandTracePhase phase,
                                   string? message = null,
                                   [CallerMemberName] string method = "") =>
        Backend.Log (new RouteTraceEntry (view.ToIdentifyingString (), command, routing, phase, method, message, DateTime.UtcNow));

    /// <summary>
    ///     Traces a command routing step from a context.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="ctx">The command context (provides command and routing info).</param>
    /// <param name="phase">The phase of routing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void TraceRoute (View view, ICommandContext? ctx, CommandTracePhase phase, string? message = null, [CallerMemberName] string method = "") =>
        Backend.Log (new RouteTraceEntry (view.ToIdentifyingString (),
                                           ctx?.Command ?? Command.NotBound,
                                           ctx?.Routing ?? CommandRouting.Direct,
                                           phase,
                                           method,
                                           message,
                                           DateTime.UtcNow));

    /// <summary>
    ///     A no-op backend that discards all trace entries. This is the default.
    /// </summary>
    public sealed class NullBackend : ICommandTraceBackend
    {
        /// <inheritdoc/>
        public void Log (RouteTraceEntry entry) { }

        /// <inheritdoc/>
        public void Clear () { }
    }

    /// <summary>
    ///     A backend that forwards trace entries to <see cref="Logging.Debug"/>.
    /// </summary>
    public sealed class LoggingBackend : ICommandTraceBackend
    {
        /// <inheritdoc/>
        public void Log (RouteTraceEntry entry)
        {
            string arrow = entry.Routing switch
                           {
                               CommandRouting.BubblingUp => "↑",
                               CommandRouting.DispatchingDown => "↓",
                               CommandRouting.Bridged => "↔",
                               _ => "•"
                           };

            string message = $"[{entry.Phase}] {arrow} {entry.Command} @ {entry.ViewId} ({entry.Method})";

            if (!string.IsNullOrEmpty (entry.Message))
            {
                message += $" - {entry.Message}";
            }

            Logging.Debug (message);
        }

        /// <inheritdoc/>
        public void Clear () { }
    }

    /// <summary>
    ///     A backend that captures trace entries to a list for testing and inspection.
    /// </summary>
    public sealed class ListBackend : ICommandTraceBackend
    {
        private readonly List<RouteTraceEntry> _entries = [];

        /// <summary>
        ///     Gets the captured trace entries.
        /// </summary>
        public IReadOnlyList<RouteTraceEntry> Entries => _entries;

        /// <inheritdoc/>
        public void Log (RouteTraceEntry entry) => _entries.Add (entry);

        /// <inheritdoc/>
        public void Clear () => _entries.Clear ();
    }
}
