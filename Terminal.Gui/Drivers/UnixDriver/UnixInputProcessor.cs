using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Input processor for <see cref="UnixInput"/>, deals in <see cref="char"/> stream.
/// </summary>
internal class UnixInputProcessor : InputProcessorImpl<char>
{
    /// <inheritdoc />
    /// <param name="inputBuffer">The input buffer to process.</param>
    /// <param name="timeProvider">Time provider for timestamps and timing control.</param>
    public UnixInputProcessor (ConcurrentQueue<char> inputBuffer, ITimeProvider? timeProvider = null)
        : base (inputBuffer, new AnsiKeyConverter (), timeProvider)
    {
    }

    /// <inheritdoc />
    public override void InjectKeyDownEvent (Key key)
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
            testableInput.InjectInput (ch);
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
    public override void InjectMouseEvent (IApplication? app, Mouse mouse)
    {
        base.InjectMouseEvent (app, mouse);
        // Convert Mouse to ANSI SGR format escape sequence
        string ansiSequence = AnsiMouseEncoder.Encode (mouse);

        // Enqueue each character of the ANSI sequence
        if (InputImpl is not ITestableInput<char> testableInput)
        {
            return;
        }

        foreach (char ch in ansiSequence)
        {
            testableInput.InjectInput (ch);
        }
    }
}
