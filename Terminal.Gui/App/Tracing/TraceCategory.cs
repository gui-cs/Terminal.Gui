namespace Terminal.Gui;

/// <summary>
///     Categories of trace events that can be independently enabled or disabled.
/// </summary>
[Flags]
public enum TraceCategory
{
    /// <summary>No tracing enabled.</summary>
    None = 0,

    /// <summary>
    ///     Application and Driver lifecycle tracing.
    /// </summary>
    Lifecycle = 1,

    /// <summary>Command routing traces (InvokeCommand, bubbling, dispatch).</summary>
    Command = 2,

    /// <summary>Mouse event traces (clicks, drags, wheel).</summary>
    Mouse = 4,

    /// <summary>Keyboard event traces (key down, key up).</summary>
    Keyboard = 8,

    /// <summary>Navigation traces (Focus, TabBehavior, etc...).</summary>
    Navigation = 16,

    /// <summary>All trace categories enabled.</summary>
    All = Lifecycle | Command | Mouse | Keyboard | Navigation
}
