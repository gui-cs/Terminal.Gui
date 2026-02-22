using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Terminal.Gui.Tests;

/// <summary>
///     Helper for binding Terminal.Gui logging to xUnit test output.
///     By default, only Warning and above are logged. Use <see cref="Verbose"/> for full logging.
/// </summary>
public static class TestLogging
{
    /// <summary>
    ///     Binds Terminal.Gui logging to xUnit output with default filtering (Warning and above).
    ///     Dispose the returned scope to restore the previous logger.
    /// </summary>
    /// <param name="output">The xUnit test output helper.</param>
    /// <returns>An <see cref="IDisposable"/> scope.</returns>
    /// <example>
    ///     <code>
    ///     using (TestLogging.BindTo(_output))
    ///     {
    ///         // Only warnings and errors appear in test output
    ///         Application.Init();
    ///         // ... test code
    ///         Application.Shutdown();
    ///     }
    ///     </code>
    /// </example>
    public static IDisposable BindTo (ITestOutputHelper output) => BindTo (output, LogLevel.Warning);

    /// <summary>
    ///     Binds Terminal.Gui logging to xUnit output with the specified minimum log level.
    /// </summary>
    /// <param name="output">The xUnit test output helper.</param>
    /// <param name="minLevel">The minimum log level to output.</param>
    /// <returns>An <see cref="IDisposable"/> scope.</returns>
    public static IDisposable BindTo (ITestOutputHelper output, LogLevel minLevel) => Logging.PushLogger (new TestOutputLogger (output, minLevel));

    /// <summary>
    ///     Binds Terminal.Gui logging to xUnit output with verbose logging (Trace and above).
    ///     Use this when debugging a specific test.
    /// </summary>
    /// <param name="output">The xUnit test output helper.</param>
    /// <returns>An <see cref="IDisposable"/> scope.</returns>
    /// <example>
    ///     <code>
    ///     using (TestLogging.Verbose(_output))
    ///     {
    ///         // All log levels appear in test output
    ///         Application.Init();
    ///         // ... test code
    ///         Application.Shutdown();
    ///     }
    ///     </code>
    /// </example>
    public static IDisposable Verbose (ITestOutputHelper output) => BindTo (output, LogLevel.Trace);

    private sealed class TestOutputLogger (ITestOutputHelper output, LogLevel minLevel) : ILogger
    {
        public IDisposable? BeginScope<TState> (TState state) where TState : notnull => null;

        public bool IsEnabled (LogLevel logLevel) => logLevel >= minLevel;

        public void Log<TState> (
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (!IsEnabled (logLevel))
            {
                return;
            }

            string levelTag = logLevel switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "???"
            };

            try
            {
                output.WriteLine ($"[{levelTag}] {formatter (state, exception)}");
            }
            catch (InvalidOperationException)
            {
                // Test already completed - ignore late writes
            }
        }
    }
}
