namespace Terminal.Gui.Tracing;

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

        var message = $"{prefix} @ {entry.ViewId} ({entry.Method})";

        if (!string.IsNullOrEmpty (entry.Message))
        {
            message += $" - {entry.Message}";
        }

        Logging.Trace (message);
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
