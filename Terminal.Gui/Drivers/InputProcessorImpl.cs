using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Base implementation for processing queued input of type <typeparamref name="TInputRecord"/>.
///     Translates driver-specific input into Terminal.Gui events and data models on the main loop thread.
/// </summary>
/// <typeparam name="TInputRecord">The driver-specific input record type (e.g., <see cref="ConsoleKeyInfo"/>).</typeparam>
public abstract class InputProcessorImpl<TInputRecord> : IInputProcessor, IDisposable where TInputRecord : struct
{
    #region Fields and Configuration

    /// <summary>
    ///     Timeout for detecting stale escape sequences. After this duration, held Esc sequences are released.
    /// </summary>
    private readonly TimeSpan _escTimeout = TimeSpan.FromMilliseconds (50);

    /// <summary>
    ///     ANSI response parser for handling escape sequences from the input stream.
    /// </summary>
    internal AnsiResponseParser<TInputRecord> Parser { get; } = new ();

    /// <summary>
    ///     Thread-safe input queue populated by <see cref="IInput{TInputRecord}"/> on the input thread.
    ///     Dequeued by <see cref="ProcessQueue"/> on the main loop thread.
    /// </summary>
    public ConcurrentQueue<TInputRecord> InputQueue { get; }

    /// <summary>
    ///     Input implementation instance. Set by <see cref="IMainLoopCoordinator"/>.
    /// </summary>
    public IInput<TInputRecord>? InputImpl { get; set; }

    /// <summary>
    ///     External cancellation token source for cooperative cancellation.
    /// </summary>
    public CancellationTokenSource? ExternalCancellationTokenSource { get; set; }

    /// <inheritdoc />
    public string? DriverName { get; init; }

    #endregion

    #region Constructor

    /// <summary>
    ///     Initializes a new instance, wiring parser events and configuring the input queue.
    /// </summary>
    /// <param name="inputBuffer">Thread-safe queue to be populated with input by <see cref="IInput{TInputRecord}"/>.</param>
    /// <param name="keyConverter">Converter for translating <typeparamref name="TInputRecord"/> to <see cref="Key"/>.</param>
    protected InputProcessorImpl (ConcurrentQueue<TInputRecord> inputBuffer, IKeyConverter<TInputRecord> keyConverter)
    {
        InputQueue = inputBuffer;
        KeyConverter = keyConverter;

        // Enable mouse handling
        Parser.HandleMouse = true;
        Parser.Mouse += (_, mouseEvent) => RaiseMouseEventParsed (mouseEvent);

        // Enable keyboard handling
        Parser.HandleKeyboard = true;
        Parser.Keyboard += (_, keyEvent) =>
                           {
                               RaiseKeyDownEvent (keyEvent);
                               RaiseKeyUpEvent (keyEvent);
                           };

        // Configure unexpected response handler
        Parser.UnexpectedResponseHandler = str =>
                                           {
                                               var cur = new string (str.Select (k => k.Item1).ToArray ());
                                               Logging.Information ($"{nameof (InputProcessorImpl<TInputRecord>)} ignored unrecognized response '{cur}'");
                                               AnsiSequenceSwallowed?.Invoke (this, cur);

                                               return true;
                                           };
    }

    #endregion

    #region Core Processing

    /// <inheritdoc />
    public IAnsiResponseParser GetParser () { return Parser; }

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

    /// <summary>
    ///     Releases held escape sequences that have exceeded the timeout threshold.
    /// </summary>
    /// <returns>Enumerable of input records that were held and are now released.</returns>
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
    ///     Processes a single input element dequeued from <see cref="InputQueue"/>.
    ///     Called sequentially for each dequeued value.
    /// </summary>
    /// <param name="input">The input record to process.</param>
    protected abstract void Process (TInputRecord input);

    /// <summary>
    ///     Processes input that bypasses the <see cref="Parser"/> (e.g., stale escape sequences).
    /// </summary>
    /// <param name="input">The input record to process.</param>
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

    #endregion

    #region Keyboard Events

    /// <summary>
    ///     Translates driver-specific <typeparamref name="TInputRecord"/> to Terminal.Gui <see cref="Key"/>.
    /// </summary>
    public IKeyConverter<TInputRecord> KeyConverter { get; }

    /// <inheritdoc />
    public event EventHandler<Key>? KeyDown;

    /// <inheritdoc />
    public void RaiseKeyDownEvent (Key a)
    {
        KeyDown?.Invoke (this, a);
    }

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
    public event EventHandler<Key>? KeyUp;

    /// <inheritdoc />
    public void RaiseKeyUpEvent (Key a) { KeyUp?.Invoke (this, a); }

    /// <inheritdoc />
    public void EnqueueKeyUpEvent (Key key)
    {
        // TODO: Determine if we can still support this on Windows
        throw new NotImplementedException ();
    }

    #endregion

    #region Mouse Events

    private readonly MouseInterpreter _mouseInterpreter = new ();

    /// <inheritdoc />
    public event EventHandler<MouseEventArgs>? MouseEventParsed;

    /// <inheritdoc />
    public void RaiseMouseEventParsed (MouseEventArgs mouseEvent)
    {
        Logging.Trace ($"{mouseEvent}");
        MouseEventParsed?.Invoke (this, mouseEvent);
        RaiseSyntheticMouseEvent (mouseEvent);
    }

    /// <inheritdoc />
    public event EventHandler<MouseEventArgs>? SyntheticMouseEvent;

    /// <inheritdoc />
    public void RaiseSyntheticMouseEvent (MouseEventArgs mouseEvent)
    {
        // Process through MouseInterpreter to generate clicks
        // The interpreter yields the original event first, then any synthetic click events
        foreach (MouseEventArgs e in _mouseInterpreter.Process (mouseEvent))
        {
            Logging.Trace ($"{e}");

            // Raise all events: original + synthetic clicks
            SyntheticMouseEvent?.Invoke (this, e);
        }
    }

    /// <inheritdoc />
    public virtual void EnqueueMouseEvent (IApplication? app, MouseEventArgs mouseEvent)
    {
        // Base implementation: For drivers where TInputRecord cannot represent mouse events
        // (e.g., ConsoleKeyInfo), derived classes should override this method.
        // See WindowsInputProcessor for an example implementation that converts MouseEventArgs
        // to InputRecord and enqueues it.
        Logging.Logger.LogWarning (
                                   $"{DriverName ?? "Unknown"} driver's InputProcessor does not support EnqueueMouseEvent. " +
                                   "Override this method to enable mouse event enqueueing for testing.");
    }

    #endregion

    #region ANSI Sequence Handling

    /// <inheritdoc />
    public event EventHandler<string>? AnsiSequenceSwallowed;

    #endregion

    #region Disposal

    /// <inheritdoc />
    public void Dispose () { ExternalCancellationTokenSource?.Dispose (); }

    #endregion
}
