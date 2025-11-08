#nullable enable
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Processes the queued input queue contents - which must be of Type <typeparamref name="TInputRecord"/>.
///     Is responsible for <see cref="ProcessQueue"/> and translating into common Terminal.Gui
///     events and data models. Runs on the main loop thread.
/// </summary>
public abstract class InputProcessorImpl<TInputRecord> : IInputProcessor, IDisposable where TInputRecord : struct
{
    /// <summary>
    ///     Constructs base instance including wiring all relevant
    ///     parser events and setting <see cref="InputQueue"/> to
    ///     the provided thread safe input collection.
    /// </summary>
    /// <param name="inputBuffer">The collection that will be populated with new input (see <see cref="IInput{T}"/>)</param>
    /// <param name="keyConverter">
    ///     Key converter for translating driver specific
    ///     <typeparamref name="TInputRecord"/> class into Terminal.Gui <see cref="Key"/>.
    /// </param>
    protected InputProcessorImpl (ConcurrentQueue<TInputRecord> inputBuffer, IKeyConverter<TInputRecord> keyConverter)
    {
        InputQueue = inputBuffer;
        Parser.HandleMouse = true;
        Parser.Mouse += (s, e) => RaiseMouseEvent (e);

        Parser.HandleKeyboard = true;

        Parser.Keyboard += (s, k) =>
                           {
                               RaiseKeyDownEvent (k);
                               RaiseKeyUpEvent (k);
                           };

        // TODO: For now handle all other escape codes with ignore
        Parser.UnexpectedResponseHandler = str =>
                                           {
                                               var cur = new string (str.Select (k => k.Item1).ToArray ());
                                               Logging.Logger.LogInformation ($"{nameof (InputProcessorImpl<TInputRecord>)} ignored unrecognized response '{cur}'");
                                               AnsiSequenceSwallowed?.Invoke (this, cur);

                                               return true;
                                           };
        KeyConverter = keyConverter;
    }

    /// <summary>
    ///     How long after Esc has been pressed before we give up on getting an Ansi escape sequence
    /// </summary>
    private readonly TimeSpan _escTimeout = TimeSpan.FromMilliseconds (50);

    internal AnsiResponseParser<TInputRecord> Parser { get; } = new ();

    /// <summary>
    ///     Class responsible for translating the driver specific native input class <typeparamref name="TInputRecord"/> e.g.
    ///     <see cref="ConsoleKeyInfo"/> into the Terminal.Gui <see cref="Key"/> class (used for all
    ///     internal library representations of Keys).
    /// </summary>
    public IKeyConverter<TInputRecord> KeyConverter { get; }

    /// <summary>
    ///     The input queue which is filled by <see cref="IInput{TInputRecord}"/> implementations running on the input thread.
    ///     Implementations of this class should dequeue from this queue in <see cref="ProcessQueue"/> on the main loop thread.
    /// </summary>
    public ConcurrentQueue<TInputRecord> InputQueue { get; }

    /// <inheritdoc />
    public string? DriverName { get; init; }

    /// <inheritdoc/>
    public IAnsiResponseParser GetParser () { return Parser; }

    private readonly MouseInterpreter _mouseInterpreter = new ();

    /// <inheritdoc />
    public event EventHandler<Key>? KeyDown;

    /// <inheritdoc />
    public event EventHandler<string>? AnsiSequenceSwallowed;

    /// <inheritdoc />
    public void RaiseKeyDownEvent (Key a)
    {
        KeyDown?.Invoke (this, a);
    }

    /// <inheritdoc />
    public event EventHandler<Key>? KeyUp;

    /// <inheritdoc />
    public void RaiseKeyUpEvent (Key a) { KeyUp?.Invoke (this, a); }

    /// <summary>
    /// 
    /// </summary>
    public IInput<TInputRecord>? InputImpl { get; set; }  // Set by MainLoopCoordinator

    /// <inheritdoc />
    public void EnqueueKeyDownEvent (Key key)
    {
        // Convert Key → TInputRecord
        TInputRecord inputRecord = KeyConverter.ToKeyInfo (key);

        // If input supports testing, use InputImplPeek/Read pipeline
        // which runs on the input thread.
        if (InputImpl is ITestableInput<TInputRecord> testableInput)
        {
            testableInput.AddInput (inputRecord);
        }
    }

    /// <inheritdoc />
    public void EnqueueKeyUpEvent (Key key)
    {
        // TODO: Determine if we can still support this on Windows
        throw new NotImplementedException ();
    }

    /// <inheritdoc />
    public event EventHandler<MouseEventArgs>? MouseEvent;

    /// <inheritdoc />
    public virtual void EnqueueMouseEvent (MouseEventArgs mouseEvent)
    {
        // Base implementation: For drivers where TInputRecord cannot represent mouse events
        // (e.g., ConsoleKeyInfo), derived classes should override this method.
        // See WindowsInputProcessor for an example implementation that converts MouseEventArgs
        // to InputRecord and enqueues it.
        Logging.Logger.LogWarning (
            $"{DriverName ?? "Unknown"} driver's InputProcessor does not support EnqueueMouseEvent. " +
            "Override this method to enable mouse event enqueueing for testing.");
    }

    /// <inheritdoc />
    public void RaiseMouseEvent (MouseEventArgs a)
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

    /// <inheritdoc />
    public void ProcessQueue ()
    {
        while (InputQueue.TryDequeue (out TInputRecord input))
        {
            Process (input);
        }

        foreach (TInputRecord input in ReleaseParserHeldKeysIfStale ())
        {
            ProcessAfterParsing (input);
        }
    }

    private IEnumerable<TInputRecord> ReleaseParserHeldKeysIfStale ()
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
    ///     is called sequentially for each value read from <see cref="InputQueue"/>.
    /// </summary>
    /// <param name="input"></param>
    protected abstract void Process (TInputRecord input);

    /// <summary>
    ///     Process the provided single input element - short-circuiting the <see cref="Parser"/>
    ///     stage of the processing.
    /// </summary>
    /// <param name="input"></param>
    protected virtual void ProcessAfterParsing (TInputRecord input)
    {
        var key = KeyConverter.ToKey (input);

        // If the key is not valid, we don't want to raise any events.
        if (IsValidInput (key, out key))
        {
            RaiseKeyDownEvent (key);
            RaiseKeyUpEvent (key);
        }
    }

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

    /// <inheritdoc/>
    public CancellationTokenSource? ExternalCancellationTokenSource { get; set; }

    /// <inheritdoc />
    public void Dispose () { ExternalCancellationTokenSource?.Dispose (); }
}
