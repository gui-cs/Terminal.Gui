using Microsoft.Extensions.Logging;

namespace TerminalGuiFluentTesting;

internal class TextWriterLogger (TextWriter writer) : ILogger
{
    public IDisposable? BeginScope<TState> (TState state) { return null; }

    public bool IsEnabled (LogLevel logLevel) { return true; }

    public void Log<TState> (
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? ex,
        Func<TState, Exception?, string> formatter
    )
    {
        writer.WriteLine (formatter (state, ex));
    }
}
