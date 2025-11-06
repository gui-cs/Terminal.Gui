#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Implements a fake console input for testing purposes that uses the same key input type as NetInput i.e. <see cref="ConsoleKeyInfo"/>.
/// </summary>
public class FakeInput : InputImpl<ConsoleKeyInfo>, ITestableInput<ConsoleKeyInfo>
{
    // Queue for storing injected input that will be returned by Peek/Read
    private readonly ConcurrentQueue<ConsoleKeyInfo> _testInput = new ();

    /// <summary>
    /// Creates a new FakeConsoleInput.
    /// </summary>
    public FakeInput ()
    { }

    /// <inheritdoc/>
    protected override bool Peek ()
    {
        return !_testInput.IsEmpty;
    }

    /// <inheritdoc/>
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        while (_testInput.TryDequeue (out ConsoleKeyInfo input))
        {
            yield return input;
        }
    }

    /// <inheritdoc />
    public void AddInput (ConsoleKeyInfo input)
    {
        //Logging.Trace ($"Enqueuing input: {input.Key}");

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