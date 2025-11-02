#nullable enable
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Processes the queued input buffer contents - which must be of Type <typeparamref name="TKeyInfo"/>.
///     Is responsible for <see cref="ProcessQueue"/> and translating into common Terminal.Gui
///     events and data models.
/// </summary>
public abstract class InputProcessorImpl<TKeyInfo> : IInputProcessor
{
    /// <summary>
    ///     How long after Esc has been pressed before we give up on getting an Ansi escape sequence
    /// </summary>
    private readonly TimeSpan _escTimeout = TimeSpan.FromMilliseconds (50);

    internal AnsiResponseParser<TKeyInfo> Parser { get; } = new ();

    /// <summary>
    ///     Class responsible for translating the driver specific native input class <typeparamref name="TKeyInfo"/> e.g.
    ///     <see cref="ConsoleKeyInfo"/> into the Terminal.Gui <see cref="Key"/> class (used for all
    ///     internal library representations of Keys).
    /// </summary>
    public IKeyConverter<TKeyInfo> KeyConverter { get; }

    /// <summary>
    ///     Input buffer which will be drained from by this class.
    /// </summary>
    public ConcurrentQueue<TKeyInfo> InputBuffer { get; }

    /// <inheritdoc />
    public string? DriverName { get; init; }

    /// <inheritdoc/>
    public IAnsiResponseParser GetParser () { return Parser; }

    private readonly MouseInterpreter _mouseInterpreter = new ();

    /// <summary>Event fired when a key is pressed down. This is a precursor to <see cref="KeyUp"/>.</summary>
    public event EventHandler<Key>? KeyDown;

    /// <summary>Event fired when a terminal sequence read from input is not recognized and therefore ignored.</summary>
    public event EventHandler<string>? AnsiSequenceSwallowed;

    /// <summary>
    ///     Called when a key is pressed down. Fires the <see cref="KeyDown"/> event. This is a precursor to
    ///     <see cref="OnKeyUp"/>.
    /// </summary>
    /// <param name="a"></param>
    public void OnKeyDown (Key a)
    {
        Logging.Trace ($"{nameof (InputProcessorImpl<TKeyInfo>)} raised {a}");
        KeyDown?.Invoke (this, a);
    }

    /// <summary>Event fired when a key is released.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will fire this event after <see cref="KeyDown"/> processing is
    ///     complete.
    /// </remarks>
    public event EventHandler<Key>? KeyUp;

    /// <summary>Called when a key is released. Fires the <see cref="KeyUp"/> event.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will call this method after <see cref="OnKeyDown"/> processing
    ///     is complete.
    /// </remarks>
    /// <param name="a"></param>
    public void OnKeyUp (Key a) { KeyUp?.Invoke (this, a); }

    /// <summary>Event fired when a mouse event occurs.</summary>
    public event EventHandler<MouseEventArgs>? MouseEvent;

    /// <summary>Called when a mouse event occurs. Fires the <see cref="MouseEvent"/> event.</summary>
    /// <param name="a"></param>
    public void OnMouseEvent (MouseEventArgs a)
    {
        // Ensure ScreenPosition is set
        a.ScreenPosition = a.Position;

        foreach (MouseEventArgs e in _mouseInterpreter.Process (a))
        {
            // Logging.Trace ($"Mouse Interpreter raising {e.Flags}");

            // Pass on
            MouseEvent?.Invoke (this, e);
        }
    }

    /// <summary>
    ///     Constructs base instance including wiring all relevant
    ///     parser events and setting <see cref="InputBuffer"/> to
    ///     the provided thread safe input collection.
    /// </summary>
    /// <param name="inputBuffer">The collection that will be populated with new input (see <see cref="IConsoleInput{T}"/>)</param>
    /// <param name="keyConverter">
    ///     Key converter for translating driver specific
    ///     <typeparamref name="TKeyInfo"/> class into Terminal.Gui <see cref="Key"/>.
    /// </param>
    protected InputProcessorImpl (ConcurrentQueue<TKeyInfo> inputBuffer, IKeyConverter<TKeyInfo> keyConverter)
    {
        InputBuffer = inputBuffer;
        Parser.HandleMouse = true;
        Parser.Mouse += (s, e) => OnMouseEvent (e);

        Parser.HandleKeyboard = true;

        Parser.Keyboard += (s, k) =>
                           {
                               OnKeyDown (k);
                               OnKeyUp (k);
                           };

        // TODO: For now handle all other escape codes with ignore
        Parser.UnexpectedResponseHandler = str =>
                                           {
                                               var cur = new string (str.Select (k => k.Item1).ToArray ());
                                               Logging.Logger.LogInformation ($"{nameof (InputProcessorImpl<TKeyInfo>)} ignored unrecognized response '{cur}'");
                                               AnsiSequenceSwallowed?.Invoke (this, cur);

                                               return true;
                                           };
        KeyConverter = keyConverter;
    }

    /// <summary>
    ///     Drains the <see cref="InputBuffer"/> buffer, processing all available keystrokes
    /// </summary>
    public void ProcessQueue ()
    {
        while (InputBuffer.TryDequeue (out TKeyInfo? input))
        {
            Process (input);
        }

        foreach (TKeyInfo input in ReleaseParserHeldKeysIfStale ())
        {
            ProcessAfterParsing (input);
        }
    }

    private IEnumerable<TKeyInfo> ReleaseParserHeldKeysIfStale ()
    {
        if (Parser.State is AnsiResponseParserState.ExpectingEscapeSequence or AnsiResponseParserState.InResponse
            && DateTime.Now - Parser.StateChangedAt > _escTimeout)
        {
            return Parser.Release ().Select (o => o.Item2);
        }

        return [];
    }

    /// <summary>
    ///     Process the provided single input element <paramref name="input"/>. This method
    ///     is called sequentially for each value read from <see cref="InputBuffer"/>.
    /// </summary>
    /// <param name="input"></param>
    protected abstract void Process (TKeyInfo input);

    /// <summary>
    ///     Process the provided single input element - short-circuiting the <see cref="Parser"/>
    ///     stage of the processing.
    /// </summary>
    /// <param name="input"></param>
    protected abstract void ProcessAfterParsing (TKeyInfo input);

    private char _highSurrogate = '\0';

    /// <inheritdoc />
    public bool IsValidInput (Key key, out Key result)
    {
        result = key;

        if (char.IsHighSurrogate ((char)key))
        {
            _highSurrogate = (char)key;

            return false;
        }

        if (_highSurrogate > 0 && char.IsLowSurrogate ((char)key))
        {
            result = (KeyCode)new Rune (_highSurrogate, (char)key).Value;

            if (key.IsAlt)
            {
                result = result.WithAlt;
            }

            if (key.IsCtrl)
            {
                result = result.WithCtrl;
            }

            if (key.IsShift)
            {
                result = result.WithShift;
            }

            _highSurrogate = '\0';

            return true;
        }

        if (char.IsSurrogate ((char)key))
        {
            return false;
        }

        if (_highSurrogate > 0)
        {
            _highSurrogate = '\0';
        }

        if (key.KeyCode == 0)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void AddKeyEvent (Key key)
    {
        TKeyInfo keyInfo = KeyConverter.ToKeyInfo (key);
        InputBuffer.Enqueue (keyInfo);
    }
}
