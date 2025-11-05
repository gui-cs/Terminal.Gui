using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Input processor for <see cref="NetInput"/>, deals in <see cref="ConsoleKeyInfo"/> stream
/// </summary>
public class FakeInputProcessor : InputProcessorImpl<ConsoleKeyInfo>
{
    /// <inheritdoc/>
    public FakeInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer) : base (inputBuffer, new NetKeyConverter ()) { DriverName = "fake"; }

    /// <inheritdoc/>
    protected override void Process (ConsoleKeyInfo input)
    {
        foreach (Tuple<char, ConsoleKeyInfo> released in Parser.ProcessInput (Tuple.Create (input.KeyChar, input)))
        {
            ProcessAfterParsing (released.Item2);
        }
    }
}
