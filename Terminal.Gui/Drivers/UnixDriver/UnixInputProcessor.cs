using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Input processor for <see cref="UnixInput"/>, deals in <see cref="char"/> stream.
/// </summary>
internal class UnixInputProcessor : InputProcessorImpl<char>
{
    /// <inheritdoc />
    public UnixInputProcessor (ConcurrentQueue<char> inputBuffer) : base (inputBuffer, new AnsiKeyConverter ())
    {
    }

    /// <inheritdoc />
    public override void EnqueueKeyDownEvent (Key key)
    {
        // Convert Key → ANSI sequence (if needed) or char
        string sequence = AnsiKeyboardEncoder.Encode (key);

        // If input supports testing, use it
        if (InputImpl is not ITestableInput<char> testableInput)
        {
            return;
        }

        foreach (char ch in sequence)
        {
            testableInput.AddInput (ch);
        }
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
    public override void EnqueueMouseEvent (IApplication? app, Mouse mouse)
    {
        base.EnqueueMouseEvent (app, mouse);
        // Convert Mouse to ANSI SGR format escape sequence
        string ansiSequence = AnsiMouseEncoder.Encode (mouse);

        // Enqueue each character of the ANSI sequence
        if (InputImpl is not ITestableInput<char> testableInput)
        {
            return;
        }

        foreach (char ch in ansiSequence)
        {
            testableInput.AddInput (ch);
        }
    }
}
