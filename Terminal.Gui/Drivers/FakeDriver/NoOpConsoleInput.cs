#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///    Implements a no-operation console input that waits until the provided cancellation token is triggered.
/// </summary>
/// <typeparam name="T"></typeparam>
public class NoOpConsoleInput<T> (CancellationToken hardStopToken) : IConsoleInput<T>
{
    private readonly CancellationTokenSource _timeoutCts = new (TimeSpan.FromSeconds (30));

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
