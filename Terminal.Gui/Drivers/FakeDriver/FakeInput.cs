using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Fake console input for testing that does not produce any input events.
/// </summary>
/// <typeparam name="T"></typeparam>
public class FakeInput<T> (CancellationToken hardStopToken) : IConsoleInput<T>
{
    private readonly CancellationTokenSource _timeoutCts = new (TimeSpan.FromSeconds (30));

    // Create a timeout-based cancellation token too to prevent tests ever fully hanging

    /// <inheritdoc/>
    public void Dispose () { }

    /// <inheritdoc/>
    public void Initialize (ConcurrentQueue<T> inputBuffer) { InputBuffer = inputBuffer; }

    /// <summary>
    ///     Gets or sets the input buffer.
    /// </summary>
    public ConcurrentQueue<T>? InputBuffer { get; set; }

    /// <inheritdoc/>
    public void Run (CancellationToken token)
    {
        // Blocks until either the token or the hardStopToken is cancelled.
        WaitHandle.WaitAny ([token.WaitHandle, hardStopToken.WaitHandle, _timeoutCts.Token.WaitHandle]);
    }
}
