using System.Diagnostics;
using System.Runtime.CompilerServices;
using Terminal.Gui.Input;

namespace Terminal.Gui;

/// <summary>
///     Provides unified tracing for debugging and testing. Supports independent enable/disable
///     for Command, Mouse, and Keyboard tracing. Traces are only captured in DEBUG builds.
/// </summary>
/// <remarks>
///     <para>
///         By default, all tracing is disabled. Enable individual categories via properties:
///     </para>
///     <list type="bullet">
///         <item><see cref="CommandEnabled"/> - command routing traces</item>
///         <item><see cref="MouseEnabled"/> - mouse event traces</item>
///         <item><see cref="KeyboardEnabled"/> - keyboard event traces</item>
///     </list>
///     <para>
///         All trace methods are marked with <c>[Conditional("DEBUG")]</c>,
///         so they have zero overhead in Release builds.
///     </para>
/// </remarks>
public static class ViewTrace
{
    private static readonly AsyncLocal<ITraceBackend?> _asyncLocalBackend = new ();
    private static readonly NullBackend _defaultBackend = new ();

    /// <summary>
    ///     Gets or sets whether command tracing is enabled.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool CommandEnabled { get; set; }

    /// <summary>
    ///     Gets or sets whether mouse tracing is enabled.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool MouseEnabled { get; set; }

    /// <summary>
    ///     Gets or sets whether keyboard tracing is enabled.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool KeyboardEnabled { get; set; }

    /// <summary>
    ///     Gets or sets the trace backend for the current async context. Default is <see cref="NullBackend"/>.
    ///     Each async flow (test, thread) has its own backend instance.
    /// </summary>
    public static ITraceBackend Backend { get => _asyncLocalBackend.Value ?? _defaultBackend; set => _asyncLocalBackend.Value = value; }

    #region Command Tracing

    /// <summary>
    ///     Traces a command routing step.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="command">The command being routed.</param>
    /// <param name="routing">The current routing mode.</param>
    /// <param name="phase">The phase of routing (e.g., "Entry", "Exit").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Command (
        View view,
        Command command,
        CommandRouting routing,
        string phase,
        string? message = null,
        [CallerMemberName] string method = "")
    {
        if (!CommandEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (
                                     TraceCategory.Command,
                                     view.ToIdentifyingString (),
                                     phase,
                                     method,
                                     message,
                                     DateTime.UtcNow,
                                     (command, routing)));
    }

    /// <summary>
    ///     Traces a command routing step from a context.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="ctx">The command context (provides command and routing info).</param>
    /// <param name="phase">The phase of routing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Command (
        View view,
        ICommandContext? ctx,
        string phase,
        string? message = null,
        [CallerMemberName] string method = "")
    {
        if (!CommandEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (
                                     TraceCategory.Command,
                                     view.ToIdentifyingString (),
                                     phase,
                                     method,
                                     message,
                                     DateTime.UtcNow,
                                     (ctx?.Command ?? Input.Command.NotBound, ctx?.Routing ?? CommandRouting.Direct)));
    }

    #endregion

    #region Mouse Tracing

    /// <summary>
    ///     Traces a mouse event.
    /// </summary>
    /// <param name="view">The view processing the mouse event.</param>
    /// <param name="flags">The mouse flags.</param>
    /// <param name="position">The mouse position.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (
        View view,
        MouseFlags flags,
        Point position,
        string phase,
        string? message = null,
        [CallerMemberName] string method = "")
    {
        if (!MouseEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (
                                     TraceCategory.Mouse,
                                     view.ToIdentifyingString (),
                                     phase,
                                     method,
                                     message,
                                     DateTime.UtcNow,
                                     (flags, position)));
    }

    /// <summary>
    ///     Traces a mouse event with a Mouse object.
    /// </summary>
    /// <param name="view">The view processing the mouse event.</param>
    /// <param name="mouse">The mouse event data.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (
        View view,
        Mouse mouse,
        string phase,
        string? message = null,
        [CallerMemberName] string method = "")
    {
        if (!MouseEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (
                                     TraceCategory.Mouse,
                                     view.ToIdentifyingString (),
                                     phase,
                                     method,
                                     message,
                                     DateTime.UtcNow,
                                     mouse));
    }

    #endregion

    #region Keyboard Tracing

    /// <summary>
    ///     Traces a keyboard event.
    /// </summary>
    /// <param name="view">The view processing the keyboard event.</param>
    /// <param name="key">The key being processed.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Keyboard (
        View view,
        Key key,
        string phase,
        string? message = null,
        [CallerMemberName] string method = "")
    {
        if (!KeyboardEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (
                                     TraceCategory.Keyboard,
                                     view.ToIdentifyingString (),
                                     phase,
                                     method,
                                     message,
                                     DateTime.UtcNow,
                                     key));
    }

    #endregion

    #region Backends

    /// <summary>
    ///     A no-op backend that discards all trace entries. This is the default.
    /// </summary>
    public sealed class NullBackend : ITraceBackend
    {
        /// <inheritdoc/>
        public void Log (TraceEntry entry) { }

        /// <inheritdoc/>
        public void Clear () { }
    }

    /// <summary>
    ///     A backend that forwards trace entries to <see cref="Logging.Debug"/>.
    /// </summary>
    public sealed class LoggingBackend : ITraceBackend
    {
        /// <inheritdoc/>
        public void Log (TraceEntry entry)
        {
            string prefix = entry.Category switch
                            {
                                TraceCategory.Command => FormatCommand (entry),
                                TraceCategory.Mouse => FormatMouse (entry),
                                TraceCategory.Keyboard => FormatKeyboard (entry),
                                _ => $"[{entry.Category}]"
                            };

            string message = $"{prefix} @ {entry.ViewId} ({entry.Method})";

            if (!string.IsNullOrEmpty (entry.Message))
            {
                message += $" - {entry.Message}";
            }

            Logging.Debug (message);
        }

        private static string FormatCommand (TraceEntry entry)
        {
            if (entry.Data is (Command cmd, CommandRouting routing))
            {
                string arrow = routing switch
                               {
                                   CommandRouting.BubblingUp => "↑",
                                   CommandRouting.DispatchingDown => "↓",
                                   CommandRouting.Bridged => "↔",
                                   _ => "•"
                               };

                return $"[{entry.Phase}] {arrow} {cmd}";
            }

            return $"[Command:{entry.Phase}]";
        }

        private static string FormatMouse (TraceEntry entry)
        {
            if (entry.Data is (MouseFlags flags, Point pos))
            {
                return $"[Mouse:{entry.Phase}] {flags} @({pos.X},{pos.Y})";
            }

            if (entry.Data is Mouse mouse)
            {
                Point mousePos = mouse.Position ?? Point.Empty;

                return $"[Mouse:{entry.Phase}] {mouse.Flags} @({mousePos.X},{mousePos.Y})";
            }

            return $"[Mouse:{entry.Phase}]";
        }

        private static string FormatKeyboard (TraceEntry entry)
        {
            if (entry.Data is Key key)
            {
                return $"[Key:{entry.Phase}] {key}";
            }

            return $"[Key:{entry.Phase}]";
        }

        /// <inheritdoc/>
        public void Clear () { }
    }

    /// <summary>
    ///     A backend that captures trace entries to a list for testing and inspection.
    /// </summary>
    public sealed class ListBackend : ITraceBackend
    {
        private readonly List<TraceEntry> _entries = [];

        /// <summary>
        ///     Gets the captured trace entries.
        /// </summary>
        public IReadOnlyList<TraceEntry> Entries => _entries;

        /// <inheritdoc/>
        public void Log (TraceEntry entry) => _entries.Add (entry);

        /// <inheritdoc/>
        public void Clear () => _entries.Clear ();
    }

    #endregion
}
