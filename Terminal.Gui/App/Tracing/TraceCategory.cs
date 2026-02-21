namespace Terminal.Gui;

/// <summary>
///     Categories of trace events that can be independently enabled or disabled.
/// </summary>
[Flags]
public enum TraceCategory
{
    /// <summary>No tracing enabled.</summary>
    None = 0,

    /// <summary>Command routing traces (InvokeCommand, bubbling, dispatch).</summary>
    Command = 1,

    /// <summary>Mouse event traces (clicks, drags, wheel).</summary>
    Mouse = 2,

    /// <summary>Keyboard event traces (key down, key up).</summary>
    Keyboard = 4,

    /// <summary>Menu interaction traces.</summary>
    Menu = 8,

    /// <summary>Collection navigation traces (ListView, TreeView search).</summary>
    Navigation = 16,

    /// <summary>All trace categories enabled.</summary>
    All = Command | Mouse | Keyboard | Menu | Navigation
}
