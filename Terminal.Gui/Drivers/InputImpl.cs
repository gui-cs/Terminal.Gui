#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Base class for reading console input in perpetual loop.
///     The <see cref="Peek"/> and <see cref="Read"/> methods are executed
///     on the input thread created by <see cref="MainLoopCoordinator{TInputRecord}.StartInputTask"/>.
/// </summary>
/// <typeparam name="TInputRecord"></typeparam>
public abstract class InputImpl<TInputRecord> : IInput<TInputRecord>
{
    private ConcurrentQueue<TInputRecord>? _inputQueue;

    /// <summary>
    ///     Determines how to get the current system type, adjust
    ///     in unit tests to simulate specific timings.
    /// </summary>
    public Func<DateTime> Now { get; set; } = () => DateTime.Now;

    /// <inheritdoc />
    public CancellationTokenSource? ExternalCancellationTokenSource { get; set; }

    /// <inheritdoc/>
    public void Initialize (ConcurrentQueue<TInputRecord> inputQueue) { _inputQueue = inputQueue; }

    /// <inheritdoc/>
    public void Run (CancellationToken runCancellationToken)
    {
        // Create a linked token source if we have an external one
        CancellationTokenSource? linkedCts = null;
        CancellationToken effectiveToken = runCancellationToken;

        if (ExternalCancellationTokenSource != null)
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource (runCancellationToken, ExternalCancellationTokenSource.Token);
            effectiveToken = linkedCts.Token;
        }

        try
        {
            if (_inputQueue == null)
            {
                throw new ("Cannot run input before Initialization");
            }

            do
            {
                DateTime dt = Now ();

                while (Peek ())
                {
                    foreach (TInputRecord r in Read ())
                    {
                        _inputQueue.Enqueue (r);
                    }
                }

                effectiveToken.ThrowIfCancellationRequested ();
            }
            while (!effectiveToken.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        { }
        finally
        {
            Logging.Trace($"Stopping input processing");
            linkedCts?.Dispose ();
        }
    }

    /// <summary>
    ///     When implemented in a derived class, returns true if there is data available
    ///     to read from console.
    /// </summary>
    /// <returns></returns>
    public abstract bool Peek ();

    /// <summary>
    ///     Returns the available data without blocking, called when <see cref="Peek"/>
    ///     returns <see langword="true"/>.
    /// </summary>
    /// <returns></returns>
    public abstract IEnumerable<TInputRecord> Read ();

    /// <inheritdoc/>
    public virtual void Dispose () { }
}