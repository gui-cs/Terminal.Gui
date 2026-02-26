namespace Terminal.Gui.Tracing;

/// <summary>
///     A backend that forwards trace entries to <see cref="Logging.Trace"/>.
/// </summary>
public sealed class LoggingBackend : ITraceBackend
{
    /// <inheritdoc/>
    public void Log (TraceEntry entry)
    {
        string prefix = entry.Category switch
                        {
                            TraceCategory.Lifecycle => FormatLifecycle (entry),
                            TraceCategory.Command => FormatCommand (entry),
                            TraceCategory.Mouse => FormatMouse (entry),
                            TraceCategory.Keyboard => FormatKeyboard (entry),
                            TraceCategory.Navigation => FormatNavigation (entry),
                            _ => $"[{entry.Category}]"
                        };

        var message = $"{prefix}@\"{entry.Id}\"";

        if (!string.IsNullOrEmpty (entry.Message))
        {
            message += $" - {entry.Message}";
        }

        if (entry.Data is not null)
        {
            message += $" - {entry.Data}";
        }

        // ReSharper disable once ExplicitCallerInfoArgument
        Logging.Trace (message, entry.Method, $"{entry.Category}:{entry.Phase}");
    }

    private string FormatNavigation (TraceEntry entry) => string.Empty;

    private string FormatLifecycle (TraceEntry entry) => string.Empty;

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

            return $"{arrow} {cmd}";
        }

        return string.Empty;
    }

    private static string FormatMouse (TraceEntry entry)
    {
        if (entry.Data is (MouseFlags flags, Point pos))
        {
            return $"{flags} @({pos.X},{pos.Y})";
        }

        if (entry.Data is Mouse mouse)
        {
            Point mousePos = mouse.Position ?? Point.Empty;

            return $"{mouse.Flags} @({mousePos.X},{mousePos.Y})";
        }

        return string.Empty;
    }

    private static string FormatKeyboard (TraceEntry entry)
    {
        if (entry.Data is Key key)
        {
            return $"{key}";
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public void Clear () { }
}
