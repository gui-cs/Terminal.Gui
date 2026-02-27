using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Terminal.Gui.Tracing;

/// <summary>
///     Provides unified tracing for debugging and testing. Supports independent enable/disable
///     for Command, Mouse, and Keyboard tracing. Traces are only captured in DEBUG builds.
/// </summary>
/// <remarks>
///     <para>
///         By default, all tracing is disabled. Enable individual categories via properties:
///     </para>
///     <list type="bullet">
///         <item><see cref="LifecycleEnabled"/> - command routing traces</item>
///         <item><see cref="CommandEnabled"/> - command routing traces</item>
///         <item><see cref="MouseEnabled"/> - mouse event traces</item>
///         <item><see cref="KeyboardEnabled"/> - keyboard event traces</item>
///         <item><see cref="NavigationEnabled"/> - keyboard event traces</item>
///     </list>
///     <para>
///         All trace methods are marked with <c>[Conditional("DEBUG")]</c>,
///         so they have zero overhead in Release builds.
///     </para>
///     <para>
///         When a category is enabled, if no backend has been set, <see cref="LoggingBackend"/>
///         is automatically used so traces appear in the log output.
///     </para>
/// </remarks>
public static class Trace
{
    private static readonly AsyncLocal<ITraceBackend?> _asyncLocalBackend = new ();
    private static readonly AsyncLocal<TraceCategory> _asyncLocalEnabledCategories = new ();
    private static readonly NullBackend _nullBackend = new ();
    private static readonly LoggingBackend _loggingBackend = new ();

    /// <summary>
    ///     Gets or sets the enabled trace categories for the current async context.
    ///     This property is thread-safe and isolated per async flow.
    /// </summary>
    public static TraceCategory EnabledCategories
    {
        get => _asyncLocalEnabledCategories.Value;
        set
        {
            _asyncLocalEnabledCategories.Value = value;
            EnsureBackendIfEnabled ();
        }
    }

    /// <summary>
    ///     Gets or sets whether Application and Driver lifecycle tracing is enabled.
    ///     When enabled, automatically uses <see cref="LoggingBackend"/> if no backend is set.
    ///     This property is thread-safe and isolated per async flow.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool LifecycleEnabled
    {
        get => EnabledCategories.HasFlag (TraceCategory.Lifecycle);
        set
        {
            if (value)
            {
                EnabledCategories |= TraceCategory.Lifecycle;
            }
            else
            {
                EnabledCategories &= ~TraceCategory.Lifecycle;
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether command tracing is enabled.
    ///     When enabled, automatically uses <see cref="LoggingBackend"/> if no backend is set.
    ///     This property is thread-safe and isolated per async flow.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool CommandEnabled
    {
        get => EnabledCategories.HasFlag (TraceCategory.Command);
        set
        {
            if (value)
            {
                EnabledCategories |= TraceCategory.Command;
            }
            else
            {
                EnabledCategories &= ~TraceCategory.Command;
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether mouse tracing is enabled.
    ///     When enabled, automatically uses <see cref="LoggingBackend"/> if no backend is set.
    ///     This property is thread-safe and isolated per async flow.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool MouseEnabled
    {
        get => EnabledCategories.HasFlag (TraceCategory.Mouse);
        set
        {
            if (value)
            {
                EnabledCategories |= TraceCategory.Mouse;
            }
            else
            {
                EnabledCategories &= ~TraceCategory.Mouse;
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether keyboard tracing is enabled.
    ///     When enabled, automatically uses <see cref="LoggingBackend"/> if no backend is set.
    ///     This property is thread-safe and isolated per async flow.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool KeyboardEnabled
    {
        get => EnabledCategories.HasFlag (TraceCategory.Keyboard);
        set
        {
            if (value)
            {
                EnabledCategories |= TraceCategory.Keyboard;
            }
            else
            {
                EnabledCategories &= ~TraceCategory.Keyboard;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether Navigation (Focus and TabBehavior) tracing is enabled.
    /// When enabled, automatically uses
    /// <see cref="LoggingBackend"/>
    /// if no backend is set.
    /// This property is thread-safe and isolated per async flow.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool NavigationEnabled
    {
        get => EnabledCategories.HasFlag (TraceCategory.Navigation);
        set
        {
            if (value)
            {
                EnabledCategories |= TraceCategory.Navigation;
            }
            else
            {
                EnabledCategories &= ~TraceCategory.Navigation;
            }
        }
    }

    /// <summary>
    ///     Gets or sets the trace backend for the current async context. Default is <see cref="NullBackend"/>.
    ///     Each async flow (test, thread) has its own backend instance.
    /// </summary>
    /// <remarks>
    ///     When any trace category is enabled and no backend has been explicitly set,
    ///     <see cref="LoggingBackend"/> is automatically used.
    /// </remarks>
    public static ITraceBackend Backend { get => _asyncLocalBackend.Value ?? _nullBackend; set => _asyncLocalBackend.Value = value; }

    private static void EnsureBackendIfEnabled ()
    {
        // Auto-switch to LoggingBackend if:
        // 1. Any category is enabled, AND
        // 2. No backend has been set (null) OR the backend is NullBackend
        if (EnabledCategories != TraceCategory.None && (_asyncLocalBackend.Value is null || _asyncLocalBackend.Value is NullBackend))
        {
            _asyncLocalBackend.Value = _loggingBackend;
        }
    }

    /// <summary>
    ///     Pushes a trace scope that enables the specified categories and optionally sets a backend.
    ///     Returns an <see cref="IDisposable"/> that restores the previous state when disposed.
    ///     This is thread-safe and works correctly with parallel tests.
    /// </summary>
    /// <param name="categories">The trace categories to enable.</param>
    /// <param name="backend">Optional backend to use. If null, uses default backend selection.</param>
    /// <returns>An <see cref="IDisposable"/> scope that restores previous tracing state.</returns>
    /// <example>
    ///     <code>
    ///     using (Trace.PushScope (TraceCategory.Command | TraceCategory.Mouse))
    ///     {
    ///         // Command and Mouse tracing enabled in this scope
    ///         CheckBox checkbox = new () { Id = "test" };
    ///         checkbox.InvokeCommand (Command.Activate);
    ///     }
    ///     // Previous tracing state restored
    ///     </code>
    /// </example>
    public static IDisposable PushScope (TraceCategory categories, ITraceBackend? backend = null)
    {
        return new TraceScope (categories, backend);
    }

    private sealed class TraceScope : IDisposable
    {
        private readonly TraceCategory _previousCategories;
        private readonly ITraceBackend? _previousBackend;

        public TraceScope (TraceCategory categories, ITraceBackend? backend)
        {
            _previousCategories = EnabledCategories;
            _previousBackend = _asyncLocalBackend.Value;

            EnabledCategories = categories;

            if (backend is { })
            {
                Backend = backend;
            }
        }

        public void Dispose ()
        {
            EnabledCategories = _previousCategories;
            _asyncLocalBackend.Value = _previousBackend;
        }
    }

    #region Lifecycle Tracing

    /// <summary>
    ///     Traces a command routing step.
    /// </summary>
    /// <param name="id">An identifying string for the trace. E.g. <c></c></param>
    /// <param name="phase">The phase of routing (e.g., "Init", "Run", "Iteration").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Lifecycle (string? id, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!LifecycleEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Lifecycle, id ?? string.Empty, phase, method, message, DateTime.UtcNow, null));
    }

    #endregion

    #region Command Tracing

    /// <summary>
    ///     Traces a command routing step.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="command">The command being routed.</param>
    /// <param name="routing">The current routing mode.</param>
    /// <param name="phase">The phase of routing (e.g., "Entry", "Exit", "Handler", "Event", "Routing").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Command (View view, Command command, CommandRouting routing, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!CommandEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Command, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, (command, routing)));
    }

    /// <summary>
    ///     Traces a command routing step from a context.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="ctx">The command context (provides command and routing info).</param>
    /// <param name="phase">The phase of routing (e.g., "Entry", "Exit", "Handler", "Event", "Routing").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Command (View view, ICommandContext? ctx, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!CommandEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Command,
                                     view.ToIdentifyingString (),
                                     phase,
                                     method,
                                     message,
                                     DateTime.UtcNow,
                                     (ctx?.Command ?? Input.Command.NotBound, ctx?.Routing ?? CommandRouting.Direct)));
    }

    /// <summary>
    ///     Traces a command routing step from a context.
    /// </summary>
    /// <param name="view">The view where routing is occurring.</param>
    /// <param name="phase">The phase of routing (e.g., "Entry", "Exit", "Handler", "Event", "Routing").</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Command (View view, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!CommandEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Command, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, null));
    }

    #endregion

    #region Mouse Tracing

    /// <summary>
    ///     Traces a mouse event at the Driver or Application level (no view context).
    /// </summary>
    /// <param name="source">Identifying string for the source (e.g., "Driver", "Application").</param>
    /// <param name="flags">The mouse flags.</param>
    /// <param name="position">The mouse position.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (string source, MouseFlags flags, Point position, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!MouseEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Mouse, source, phase, method, message, DateTime.UtcNow, (flags, position)));
    }

    /// <summary>
    ///     Traces a mouse event at the View level.
    /// </summary>
    /// <param name="view">The view processing the mouse event.</param>
    /// <param name="flags">The mouse flags.</param>
    /// <param name="position">The mouse position.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (View view, MouseFlags flags, Point position, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!MouseEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Mouse, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, (flags, position)));
    }

    /// <summary>
    ///     Traces a mouse event with a Mouse object at the View level.
    /// </summary>
    /// <param name="view">The view processing the mouse event.</param>
    /// <param name="mouse">The mouse event data.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (View view, Mouse mouse, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!MouseEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Mouse, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, mouse));
    }

    /// <summary>
    ///     Traces a mouse event with a Mouse object at the Driver or Application level.
    /// </summary>
    /// <param name="source">Identifying string for the source (e.g., "Driver", "Application").</param>
    /// <param name="mouse">The mouse event data.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Mouse (string source, Mouse mouse, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!MouseEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Mouse, source, phase, method, message, DateTime.UtcNow, mouse));
    }

    #endregion

    #region Keyboard Tracing

    /// <summary>
    ///     Traces a keyboard event at the Driver or Application level (no view context).
    /// </summary>
    /// <param name="source">Identifying string for the source (e.g., "Driver", "Application").</param>
    /// <param name="key">The key being processed.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Keyboard (string source, Key key, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!KeyboardEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Keyboard, source, phase, method, message, DateTime.UtcNow, key));
    }

    /// <summary>
    ///     Traces a keyboard event at the View level.
    /// </summary>
    /// <param name="view">The view processing the keyboard event.</param>
    /// <param name="key">The key being processed.</param>
    /// <param name="phase">The phase of processing.</param>
    /// <param name="message">Optional additional context.</param>
    /// <param name="method">Automatically captured caller method name.</param>
    [Conditional ("DEBUG")]
    public static void Keyboard (View view, Key key, string phase, string? message = null, [CallerMemberName] string method = "")
    {
        if (!KeyboardEnabled)
        {
            return;
        }

        Backend.Log (new TraceEntry (TraceCategory.Keyboard, view.ToIdentifyingString (), phase, method, message, DateTime.UtcNow, key));
    }

    #endregion
}
