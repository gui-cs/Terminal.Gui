using System.Collections.Concurrent;

namespace TerminalGuiFluentTesting;

internal class FakeInput<T> : IConsoleInput<T>
{
    private readonly CancellationToken _hardStopToken;

    private readonly CancellationTokenSource _timeoutCts;

    public FakeInput (CancellationToken hardStopToken)
    {
        _hardStopToken = hardStopToken;

        // Create a timeout-based cancellation token too to prevent tests ever fully hanging
        _timeoutCts = new (With.Timeout);
    }

    /// <inheritdoc/>
    public void Dispose () { }

    /// <inheritdoc/>
    public void Initialize (ConcurrentQueue<T> inputBuffer) { InputBuffer = inputBuffer; }

    public ConcurrentQueue<T>? InputBuffer { get; set; }

    /// <inheritdoc/>
    public void Run (CancellationToken token)
    {
        // Blocks until either the token or the hardStopToken is cancelled.
        WaitHandle.WaitAny ([token.WaitHandle, _hardStopToken.WaitHandle, _timeoutCts.Token.WaitHandle]);
    }
}
