#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IInput{TInputRecord}"/> implementation that uses a fake input source for testing.
///     The <see cref="Peek"/> and <see cref="Read"/> methods are executed
///     on the input thread created by <see cref="MainLoopCoordinator{TInputRecord}.StartInputTask"/>.
/// </summary>
public class FakeInput : InputImpl<ConsoleKeyInfo>, ITestableInput<ConsoleKeyInfo>
{
    // Queue for storing injected input that will be returned by Peek/Read
    private readonly ConcurrentQueue<ConsoleKeyInfo> _testInput = new ();

    /// <summary>
    ///     Creates a new FakeInput.
    /// </summary>
    public FakeInput ()
    { }

    /// <inheritdoc/>
    public override bool Peek ()
    {
        // Will be called on the input thread.
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

    /// <inheritdoc />
    public void AddInput (ConsoleKeyInfo input)
    {
        //Logging.Trace ($"Enqueuing input: {input.Key}");

        // Will be called on the main loop thread.
        _testInput.Enqueue (input);

        // Wait for the input thread to drain the queue (with timeout)
        var timeout = TimeSpan.FromMilliseconds (100);
        var sw = System.Diagnostics.Stopwatch.StartNew ();
        var spinWait = new SpinWait ();

        while (!_testInput.IsEmpty && sw.Elapsed < timeout)
        {
            spinWait.SpinOnce ();
        }

        if (!_testInput.IsEmpty)
        {
            Logging.Warning ($"Timeout waiting for input '{input.Key}' to be processed after {sw.ElapsedMilliseconds}ms");
        }
    }
}