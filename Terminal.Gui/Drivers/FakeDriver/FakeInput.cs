#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Implements a fake console input for testing purposes that uses the same key input type as NetInput i.e. <see cref="ConsoleKeyInfo"/>.
/// </summary>
public class FakeInput : InputImpl<ConsoleKeyInfo>, ITestableInput<ConsoleKeyInfo>
{
    // Queue for storing injected input that will be returned by Peek/Read
    private readonly ConcurrentQueue<ConsoleKeyInfo> _pendingInput = new ();

    /// <summary>
    /// Creates a new FakeConsoleInput.
    /// </summary>
    public FakeInput ()
    { }

    /// <inheritdoc/>
    protected override bool Peek () { return !_testInput.IsEmpty; }

    /// <inheritdoc/>
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        while (_testInput.TryDequeue (out var input))
        {
            yield return input;
        }
    }

    private readonly ConcurrentQueue<ConsoleKeyInfo> _testInput = new ();

    /// <inheritdoc />
    public void AddInput (ConsoleKeyInfo input) { _testInput.Enqueue (input); }
}
