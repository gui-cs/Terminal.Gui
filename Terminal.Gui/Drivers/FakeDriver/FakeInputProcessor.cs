using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <para>
///         Input processor for <see cref="FakeInput"/>, processes a <see cref="char"/> stream
///         using pure ANSI escape sequence handling.
///     </para>
///     <para>
///         <b>ANSI Driver Architecture:</b>
///     </para>
///     <para>
///         This processor integrates with Terminal.Gui's ANSI infrastructure:
///         <list type="bullet">
///             <item>
///                 <b><see cref="AnsiResponseParser{TInputRecord}"/></b> - Automatically parses ANSI escape sequences
///                 from the input stream, extracting keyboard events, mouse events, and terminal responses.
///             </item>
///             <item>
///                 <b><see cref="AnsiRequestScheduler"/></b> - Manages outgoing ANSI requests (via <see cref="IDriver.QueueAnsiRequest"/>)
///                 and matches responses from the parser.
///             </item>
///             <item>
///                 <b><see cref="UnixKeyConverter"/></b> - Converts character input to <see cref="Key"/> events,
///                 shared with UnixDriver for consistent behavior.
///             </item>
///             <item>
///                 <b><see cref="AnsiKeyboardEncoder"/></b> and <see cref="AnsiMouseEncoder"/></b> - Convert
///                 <see cref="Key"/> and <see cref="Mouse"/> events into ANSI sequences for test injection.
///             </item>
///         </list>
///     </para>
///     <para>
///         The parser is configured in the base class <see cref="InputProcessorImpl{TInputRecord}"/> with
///         <c>HandleMouse = true</c> and <c>HandleKeyboard = true</c>, enabling automatic event raising.
///     </para>
/// </summary>
public class FakeInputProcessor : InputProcessorImpl<char>
{
    /// <inheritdoc/>
    public FakeInputProcessor (ConcurrentQueue<char> inputBuffer) : base (inputBuffer, new UnixKeyConverter ())
    {
        DriverName = "fake";
    }

    /// <inheritdoc/>
    protected override void Process (char input)
    {
        foreach (Tuple<char, char> released in Parser.ProcessInput (Tuple.Create (input, input)))
        {
            ProcessAfterParsing (released.Item2);
        }
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
