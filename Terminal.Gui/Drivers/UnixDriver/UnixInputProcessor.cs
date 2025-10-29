using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Input processor for <see cref="UnixInput"/>, deals in <see cref="char"/> stream.
/// </summary>
internal class UnixInputProcessor : InputProcessor<char>
{
    /// <inheritdoc />
    public UnixInputProcessor (ConcurrentQueue<char> inputBuffer) : base (inputBuffer, new UnixKeyConverter ())
    {
        DriverName = "Unix";
    }

    /// <inheritdoc />
    protected override void Process (char input)
    {
        foreach (Tuple<char, char> released in Parser.ProcessInput (Tuple.Create (input, input)))
        {
            ProcessAfterParsing (released.Item2);
        }

    }

    /// <inheritdoc />
    protected override void ProcessAfterParsing (char input)
    {
        var key = KeyConverter.ToKey (input);

        // If the key is not valid, we don't want to raise any events.
        if (IsValidInput (key, out key))
        {
            OnKeyDown (key);
            OnKeyUp (key);
        }
    }
}
