using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Input processor for <see cref="UnixInput"/>, deals in <see cref="char"/> stream.
/// </summary>
internal class UnixInputProcessor : InputProcessorImpl<char>
{
    /// <inheritdoc />
    public UnixInputProcessor (ConcurrentQueue<char> inputBuffer) : base (inputBuffer, new UnixKeyConverter ())
    {
        DriverName = "unix";
    }

    /// <inheritdoc />
    protected override void Process (char input)
    {
        foreach (Tuple<char, char> released in Parser.ProcessInput (Tuple.Create (input, input)))
        {
            ProcessAfterParsing (released.Item2);
        }

    }
}
