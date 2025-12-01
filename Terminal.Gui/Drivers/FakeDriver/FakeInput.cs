using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IInput{TInputRecord}"/> implementation that uses a fake input source for testing.
///     The <see cref="Peek"/> and <see cref="Read"/> methods are executed
///     on the input thread created by <see cref="MainLoopCoordinator{TInputRecord}.StartInputTaskAsync"/>.
/// </summary>
public class FakeInput : InputImpl<ConsoleKeyInfo>, ITestableInput<ConsoleKeyInfo>
{
    // Queue for storing injected input that will be returned by Peek/Read
    private readonly ConcurrentQueue<ConsoleKeyInfo> _testInput = new ();

    private int _peekCallCount;

    /// <summary>
    ///     Gets the number of times <see cref="Peek"/> has been called.
    ///     This is useful for verifying that the input loop throttling is working correctly.
    /// </summary>
    internal int PeekCallCount => _peekCallCount;

    /// <summary>
    ///     Creates a new FakeInput.
    /// </summary>
    public FakeInput () { }

    /// <inheritdoc/>
    public override bool Peek ()
    {
        // Will be called on the input thread.
        Interlocked.Increment (ref _peekCallCount);

        return !_testInput.IsEmpty;
    }

    /// <inheritdoc/>
    public override IEnumerable<ConsoleKeyInfo> Read ()
    {
        // Will be called on the input thread.
        while (_testInput.TryDequeue (out ConsoleKeyInfo input))
        {
            yield return input;
        }
    }

    /// <inheritdoc/>
    public void AddInput (ConsoleKeyInfo input)
    {
        //Logging.Trace ($"Enqueuing input: {input.Key}");

        // Will be called on the main loop thread.
        _testInput.Enqueue (input);
    }
}
