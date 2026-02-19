using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApplicationTests;

[Collection ("Logging Tests")]
public class LoggingTests : IDisposable
{
    private readonly ILogger _originalLogger;

    public LoggingTests ()
    {
        _originalLogger = Logging.Logger;
        Logging.Logger = NullLogger.Instance;
    }

    public void Dispose ()
    {
        Logging.Logger = _originalLogger;
    }

    [Fact]
    public void Uses_Global_Logger_When_No_Scope ()
    {
        CollectingLogger globalLogger = new ();
        Logging.Logger = globalLogger;

        Logging.Information ("global-message");

        Assert.True (globalLogger.Contains ("global-message"));
    }

    [Fact]
    public void PushLogger_Overrides_Global_And_Restores_After_Dispose ()
    {
        CollectingLogger globalLogger = new ();
        CollectingLogger scopedLogger = new ();
        Logging.Logger = globalLogger;

        using (Logging.PushLogger (scopedLogger))
        {
            Logging.Warning ("scoped-message");
        }

        Logging.Warning ("global-message");

        Assert.True (scopedLogger.Contains ("scoped-message"));
        Assert.False (globalLogger.Contains ("scoped-message"));
        Assert.True (globalLogger.Contains ("global-message"));
    }

    [Fact]
    public void PushLogger_Nested_Scopes_Restore_Correctly ()
    {
        CollectingLogger outerLogger = new ();
        CollectingLogger innerLogger = new ();

        using (Logging.PushLogger (outerLogger))
        {
            Logging.Debug ("outer-before-inner");

            using (Logging.PushLogger (innerLogger))
            {
                Logging.Debug ("inner-message");
            }

            Logging.Debug ("outer-after-inner");
        }

        Assert.True (outerLogger.Contains ("outer-before-inner"));
        Assert.True (outerLogger.Contains ("outer-after-inner"));
        Assert.False (outerLogger.Contains ("inner-message"));

        Assert.True (innerLogger.Contains ("inner-message"));
        Assert.False (innerLogger.Contains ("outer-before-inner"));
        Assert.False (innerLogger.Contains ("outer-after-inner"));
    }

    [Fact]
    public async Task PushLogger_Flows_Across_Async_Boundary ()
    {
        CollectingLogger scopedLogger = new ();

        using (Logging.PushLogger (scopedLogger))
        {
            await Task.Run (() => Logging.Trace ("async-message"));
        }

        Assert.True (scopedLogger.Contains ("async-message"));
    }

    private sealed class CollectingLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _messages = new ();

        public IDisposable BeginScope<TState> (TState state) where TState : notnull
        {
            return NoopScope.Instance;
        }

        public bool IsEnabled (LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState> (
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? ex,
            Func<TState, Exception?, string> formatter
        )
        {
            _messages.Enqueue (formatter (state, ex));
        }

        public bool Contains (string text)
        {
            foreach (string message in _messages)
            {
                if (message.Contains (text, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new ();

        public void Dispose ()
        {
        }
    }
}
