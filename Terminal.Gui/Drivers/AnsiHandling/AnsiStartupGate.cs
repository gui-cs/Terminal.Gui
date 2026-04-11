using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Default implementation of <see cref="IAnsiStartupGate"/>.
/// </summary>
public sealed class AnsiStartupGate : IAnsiStartupGate
{
    private readonly Lock _lock = new ();
    private readonly Dictionary<string, QueryState> _queries = [];

    private sealed class QueryState
    {
        public DateTime DeadlineUtc { get; init; }
        public bool Completed { get; set; }
        public bool TimedOut { get; set; }
    }

    /// <summary>
    ///     Creates a new startup gate.
    /// </summary>
    /// <param name="utcNow">Optional time provider for testability.</param>
    public AnsiStartupGate (Func<DateTime>? utcNow = null)
    {
        UtcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    internal Func<DateTime> UtcNow { get; }

    /// <inheritdoc/>
    public IDisposable RegisterQuery (string name, TimeSpan timeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace (name);

        DateTime nowUtc = UtcNow ();
        DateTime deadlineUtc = nowUtc + timeout;

        lock (_lock)
        {
            _queries [name] = new QueryState
            {
                DeadlineUtc = deadlineUtc
            };
        }

        Trace.Lifecycle (nameof (AnsiStartupGate), "RegisterQuery", $"'{name}' timeout={timeout.TotalMilliseconds}ms");

        return new CompletionHandle (this, name);
    }

    /// <inheritdoc/>
    public void MarkComplete (string name)
    {
        if (string.IsNullOrWhiteSpace (name))
        {
            return;
        }

        lock (_lock)
        {
            if (!_queries.TryGetValue (name, out QueryState? query) || query.Completed || query.TimedOut)
            {
                return;
            }

            query.Completed = true;
        }

        Trace.Lifecycle (nameof (AnsiStartupGate), "MarkComplete", $"'{name}' completed");
    }

    /// <inheritdoc/>
    public bool IsReady
    {
        get
        {
            SweepTimedOutQueries ();

            lock (_lock)
            {
                return _queries.Values.All (q => q.Completed || q.TimedOut);
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> PendingQueryNames
    {
        get
        {
            SweepTimedOutQueries ();

            lock (_lock)
            {
                return _queries.Where (kvp => !kvp.Value.Completed && !kvp.Value.TimedOut).Select (kvp => kvp.Key).ToArray ();
            }
        }
    }

    private void SweepTimedOutQueries ()
    {
        string [] timedOutNames;
        DateTime nowUtc = UtcNow ();

        lock (_lock)
        {
            timedOutNames = _queries.Where (kvp => !kvp.Value.Completed && !kvp.Value.TimedOut && nowUtc >= kvp.Value.DeadlineUtc)
                                    .Select (kvp => kvp.Key)
                                    .ToArray ();

            foreach (string name in timedOutNames)
            {
                _queries [name].TimedOut = true;
            }
        }

        foreach (string name in timedOutNames)
        {
            Trace.Lifecycle (nameof (AnsiStartupGate), "Timeout", $"'{name}' timed out");
        }
    }

    private sealed class CompletionHandle : IDisposable
    {
        private readonly AnsiStartupGate _gate;
        private readonly string _name;

        public CompletionHandle (AnsiStartupGate gate, string name)
        {
            _gate = gate;
            _name = name;
        }

        public void Dispose () => _gate.MarkComplete (_name);
    }
}
