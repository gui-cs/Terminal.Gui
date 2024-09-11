#nullable enable
namespace Terminal.Gui.ConsoleDrivers.Net;

using System.Diagnostics.CodeAnalysis;

[MustDisposeResource]
internal sealed class NetEvents : IDisposable
{
    private readonly ManualResetEventSlim _inputReady = new (false);
    private CancellationTokenSource? _inputReadyCancellationTokenSource;
    private readonly ManualResetEventSlim _waitForStart = new (false);

    //CancellationTokenSource _waitForStartCancellationTokenSource;
    private readonly ManualResetEventSlim _winChange = new (false);
    private readonly Queue<InputResult?> _inputQueue = new ();
    private readonly ConsoleDriver _consoleDriver;

    // TODO: Evaluate this for possible elimination of constant nulling/reallocation
    private ConsoleKeyInfo []? _cki;
    private bool _isEscSeq;
#if PROCESS_REQUEST
    bool _neededProcessRequest;
#endif
    public EscSeqRequests EscSeqRequests { get; } = new ();

    public NetEvents (ConsoleDriver consoleDriver)
    {
        _consoleDriver = consoleDriver ?? throw new ArgumentNullException (nameof (consoleDriver));
        _inputReadyCancellationTokenSource = new ();

        Task.Run (ProcessInputQueue, _inputReadyCancellationTokenSource.Token);

        Task.Run (CheckWindowSizeChange, _inputReadyCancellationTokenSource.Token);
    }

    public InputResult? DequeueInput ()
    {
        while (_inputReadyCancellationTokenSource != null
            && !_inputReadyCancellationTokenSource.Token.IsCancellationRequested)
        {
            _waitForStart.Set ();
            _winChange.Set ();

            try
            {
                if (!_inputReadyCancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (_inputQueue.Count == 0)
                    {
                        _inputReady.Wait (_inputReadyCancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                _inputReady.Reset ();
            }

#if PROCESS_REQUEST
            _neededProcessRequest = false;
#endif
            if (_inputQueue.Count > 0)
            {
                return _inputQueue.Dequeue ();
            }
        }

        return null;
    }

    private static ConsoleKeyInfo ReadConsoleKeyInfo (CancellationToken cancellationToken, bool intercept = true)
    {
        // if there is a key available, return it without waiting
        //  (or dispatching work to the thread queue)
        if (Console.KeyAvailable)
        {
            return Console.ReadKey (intercept);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            Task.Delay (100, cancellationToken).Wait (cancellationToken);

            if (Console.KeyAvailable)
            {
                return Console.ReadKey (intercept);
            }
        }

        cancellationToken.ThrowIfCancellationRequested ();

        return default;
    }

    private void ProcessInputQueue ()
    {
        while (_inputReadyCancellationTokenSource is { IsCancellationRequested: false })
        {
            try
            {
                _waitForStart.Wait (_inputReadyCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _waitForStart.Reset ();

            if (_inputQueue.Count == 0)
            {
                ConsoleKey key = 0;
                ConsoleModifiers mod = 0;
                ConsoleKeyInfo newConsoleKeyInfo = default;

                while (_inputReadyCancellationTokenSource is { IsCancellationRequested: false })
                {
                    ConsoleKeyInfo consoleKeyInfo;

                    try
                    {
                        consoleKeyInfo = ReadConsoleKeyInfo (_inputReadyCancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    if (consoleKeyInfo.KeyChar == (char)KeyCode.Esc && !_isEscSeq
                     || consoleKeyInfo.KeyChar != (char)KeyCode.Esc && _isEscSeq)
                    {
                        if (_cki is null && consoleKeyInfo.KeyChar != (char)KeyCode.Esc && _isEscSeq)
                        {
                            _cki = EscSeqUtils.ResizeArray (
                                                            new (
                                                                 (char)KeyCode.Esc,
                                                                 0,
                                                                 false,
                                                                 false,
                                                                 false
                                                                ),
                                                            _cki
                                                           );
                        }

                        _isEscSeq = true;
                        newConsoleKeyInfo = consoleKeyInfo;
                        _cki = EscSeqUtils.ResizeArray (consoleKeyInfo, _cki);

                        if (Console.KeyAvailable)
                        {
                            continue;
                        }

                        ProcessRequestResponse (ref newConsoleKeyInfo, ref key, _cki, ref mod);
                        _cki = null;
                        _isEscSeq = false;

                        break;
                    }

                    if (consoleKeyInfo.KeyChar == (char)KeyCode.Esc && _isEscSeq && _cki is { })
                    {
                        ProcessRequestResponse (ref newConsoleKeyInfo, ref key, _cki, ref mod);
                        _cki = null;

                        if (Console.KeyAvailable)
                        {
                            _cki = EscSeqUtils.ResizeArray (consoleKeyInfo, _cki);
                        }
                        else
                        {
                            ProcessMapConsoleKeyInfo (consoleKeyInfo);
                        }

                        break;
                    }

                    ProcessMapConsoleKeyInfo (consoleKeyInfo);

                    break;
                }
            }

            _inputReady.Set ();
        }

        void ProcessMapConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
        {
            _inputQueue.Enqueue (
                                 new InputResult
                                 {
                                     EventType = EventType.Key, ConsoleKeyInfo = EscSeqUtils.MapConsoleKeyInfo (consoleKeyInfo)
                                 }
                                );
            _isEscSeq = false;
        }
    }

    private void CheckWindowSizeChange ()
    {
        void RequestWindowSize (CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for a while then check if screen has changed sizes
                Task.Delay (500, cancellationToken).Wait (cancellationToken);

                int buffHeight, buffWidth;

                if (((NetDriver)_consoleDriver).IsWinPlatform)
                {
                    buffHeight = Math.Max (Console.BufferHeight, 0);
                    buffWidth = Math.Max (Console.BufferWidth, 0);
                }
                else
                {
                    buffHeight = _consoleDriver.Rows;
                    buffWidth = _consoleDriver.Cols;
                }

                if (EnqueueWindowSizeEvent (
                                            Math.Max (Console.WindowHeight, 0),
                                            Math.Max (Console.WindowWidth, 0),
                                            buffHeight,
                                            buffWidth
                                           ))
                {
                    return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested ();
        }

        while (_inputReadyCancellationTokenSource is { IsCancellationRequested: false })
        {
            try
            {
                _winChange.Wait (_inputReadyCancellationTokenSource.Token);
                _winChange.Reset ();

                RequestWindowSize (_inputReadyCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _inputReady.Set ();
        }
    }

    /// <summary>Enqueue a window size event if the window size has changed.</summary>
    /// <param name="winHeight"></param>
    /// <param name="winWidth"></param>
    /// <param name="buffHeight"></param>
    /// <param name="buffWidth"></param>
    /// <returns></returns>
    private bool EnqueueWindowSizeEvent (int winHeight, int winWidth, int buffHeight, int buffWidth)
    {
        if (winWidth == _consoleDriver.Cols && winHeight == _consoleDriver.Rows)
        {
            return false;
        }

        int w = Math.Max (winWidth, 0);
        int h = Math.Max (winHeight, 0);

        _inputQueue.Enqueue (
                             new InputResult
                             {
                                 EventType = EventType.WindowSize, WindowSizeEvent = new () { Size = new (w, h) }
                             }
                            );

        return true;
    }

    // Process a CSI sequence received by the driver (key pressed, mouse event, or request/response event)
    private void ProcessRequestResponse (
        ref ConsoleKeyInfo newConsoleKeyInfo,
        ref ConsoleKey key,
        ConsoleKeyInfo []? cki,
        ref ConsoleModifiers mod
    )
    {
        // isMouse is true if it's CSI<, false otherwise
        EscSeqUtils.DecodeEscSeq (
                                  EscSeqRequests,
                                  ref newConsoleKeyInfo,
                                  ref key,
                                  cki,
                                  ref mod,
                                  out string c1Control,
                                  out string code,
                                  out string [] values,
                                  out string terminating,
                                  out bool isMouse,
                                  out List<MouseFlags> mouseFlags,
                                  out Point pos,
                                  out bool isReq,
                                  (f, p) => HandleMouseEvent (MapMouseFlags (f), p)
                                 );

        if (isMouse)
        {
            foreach (MouseFlags mf in mouseFlags)
            {
                HandleMouseEvent (MapMouseFlags (mf), pos);
            }

            return;
        }

        if (isReq)
        {
            HandleRequestResponseEvent (c1Control, code, values, terminating);

            return;
        }

        HandleKeyboardEvent (newConsoleKeyInfo);
    }

    [UnconditionalSuppressMessage ("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static MouseButtonState MapMouseFlags (MouseFlags mouseFlags)
    {
        MouseButtonState mbs = default;

        foreach (object flag in Enum.GetValues (mouseFlags.GetType ()))
        {
            if (mouseFlags.HasFlag ((MouseFlags)flag))
            {
                switch (flag)
                {
                    case MouseFlags.Button1Pressed:
                        mbs |= MouseButtonState.Button1Pressed;

                        break;
                    case MouseFlags.Button1Released:
                        mbs |= MouseButtonState.Button1Released;

                        break;
                    case MouseFlags.Button1Clicked:
                        mbs |= MouseButtonState.Button1Clicked;

                        break;
                    case MouseFlags.Button1DoubleClicked:
                        mbs |= MouseButtonState.Button1DoubleClicked;

                        break;
                    case MouseFlags.Button1TripleClicked:
                        mbs |= MouseButtonState.Button1TripleClicked;

                        break;
                    case MouseFlags.Button2Pressed:
                        mbs |= MouseButtonState.Button2Pressed;

                        break;
                    case MouseFlags.Button2Released:
                        mbs |= MouseButtonState.Button2Released;

                        break;
                    case MouseFlags.Button2Clicked:
                        mbs |= MouseButtonState.Button2Clicked;

                        break;
                    case MouseFlags.Button2DoubleClicked:
                        mbs |= MouseButtonState.Button2DoubleClicked;

                        break;
                    case MouseFlags.Button2TripleClicked:
                        mbs |= MouseButtonState.Button2TripleClicked;

                        break;
                    case MouseFlags.Button3Pressed:
                        mbs |= MouseButtonState.Button3Pressed;

                        break;
                    case MouseFlags.Button3Released:
                        mbs |= MouseButtonState.Button3Released;

                        break;
                    case MouseFlags.Button3Clicked:
                        mbs |= MouseButtonState.Button3Clicked;

                        break;
                    case MouseFlags.Button3DoubleClicked:
                        mbs |= MouseButtonState.Button3DoubleClicked;

                        break;
                    case MouseFlags.Button3TripleClicked:
                        mbs |= MouseButtonState.Button3TripleClicked;

                        break;
                    case MouseFlags.WheeledUp:
                        mbs |= MouseButtonState.ButtonWheeledUp;

                        break;
                    case MouseFlags.WheeledDown:
                        mbs |= MouseButtonState.ButtonWheeledDown;

                        break;
                    case MouseFlags.WheeledLeft:
                        mbs |= MouseButtonState.ButtonWheeledLeft;

                        break;
                    case MouseFlags.WheeledRight:
                        mbs |= MouseButtonState.ButtonWheeledRight;

                        break;
                    case MouseFlags.Button4Pressed:
                        mbs |= MouseButtonState.Button4Pressed;

                        break;
                    case MouseFlags.Button4Released:
                        mbs |= MouseButtonState.Button4Released;

                        break;
                    case MouseFlags.Button4Clicked:
                        mbs |= MouseButtonState.Button4Clicked;

                        break;
                    case MouseFlags.Button4DoubleClicked:
                        mbs |= MouseButtonState.Button4DoubleClicked;

                        break;
                    case MouseFlags.Button4TripleClicked:
                        mbs |= MouseButtonState.Button4TripleClicked;

                        break;
                    case MouseFlags.ButtonShift:
                        mbs |= MouseButtonState.ButtonShift;

                        break;
                    case MouseFlags.ButtonCtrl:
                        mbs |= MouseButtonState.ButtonCtrl;

                        break;
                    case MouseFlags.ButtonAlt:
                        mbs |= MouseButtonState.ButtonAlt;

                        break;
                    case MouseFlags.ReportMousePosition:
                        mbs |= MouseButtonState.ReportMousePosition;

                        break;
                    case MouseFlags.AllEvents:
                        mbs |= MouseButtonState.AllEvents;

                        break;
                }
            }
        }

        return mbs;
    }

    private Point _lastCursorPosition;

    private void HandleRequestResponseEvent (string c1Control, string code, string [] values, string terminating)
    {
        switch (terminating)
        {
            // BUGBUG: I can't find where we send a request for cursor position (ESC[?6n), so I'm not sure if this is needed.
            case EscSeqUtils.CSI_RequestCursorPositionReport_Terminator:
                Point point = new() { X = int.Parse (values [1]) - 1, Y = int.Parse (values [0]) - 1 };

                if (_lastCursorPosition.Y != point.Y)
                {
                    _lastCursorPosition = point;
                    EventType eventType = EventType.WindowPosition;
                    WindowPositionEvent winPositionEv = new() { CursorPosition = point };

                    _inputQueue.Enqueue (
                                         new InputResult { EventType = eventType, WindowPositionEvent = winPositionEv }
                                        );
                }
                else
                {
                    return;
                }

                break;

            case EscSeqUtils.CSI_ReportTerminalSizeInChars_Terminator:
                switch (values [0])
                {
                    case EscSeqUtils.CSI_ReportTerminalSizeInChars_ResponseValue:
                        EnqueueWindowSizeEvent (
                                                Math.Max (int.Parse (values [1]), 0),
                                                Math.Max (int.Parse (values [2]), 0),
                                                Math.Max (int.Parse (values [1]), 0),
                                                Math.Max (int.Parse (values [2]), 0)
                                               );

                        break;
                    default:
                        EnqueueRequestResponseEvent (c1Control, code, values, terminating);

                        break;
                }

                break;
            default:
                EnqueueRequestResponseEvent (c1Control, code, values, terminating);

                break;
        }

        _inputReady.Set ();
    }

    private void EnqueueRequestResponseEvent (string c1Control, string code, string [] values, string terminating)
    {
        EventType eventType = EventType.RequestResponse;
        RequestResponseEvent requestRespEv = new() { ResultTuple = (c1Control, code, values, terminating) };

        _inputQueue.Enqueue (
                             new InputResult { EventType = eventType, RequestResponseEvent = requestRespEv }
                            );
    }

    private void HandleMouseEvent (MouseButtonState buttonState, Point pos)
    {
        MouseEvent mouseEvent = new() { Position = pos, ButtonState = buttonState };

        _inputQueue.Enqueue (
                             new InputResult { EventType = EventType.Mouse, MouseEvent = mouseEvent }
                            );

        _inputReady.Set ();
    }

    public enum EventType
    {
        Key = 1,
        Mouse = 2,
        WindowSize = 3,
        WindowPosition = 4,
        RequestResponse = 5
    }

    [Flags]
    public enum MouseButtonState
    {
        Button1Pressed = 0x1,
        Button1Released = 0x2,
        Button1Clicked = 0x4,
        Button1DoubleClicked = 0x8,
        Button1TripleClicked = 0x10,
        Button2Pressed = 0x20,
        Button2Released = 0x40,
        Button2Clicked = 0x80,
        Button2DoubleClicked = 0x100,
        Button2TripleClicked = 0x200,
        Button3Pressed = 0x400,
        Button3Released = 0x800,
        Button3Clicked = 0x1000,
        Button3DoubleClicked = 0x2000,
        Button3TripleClicked = 0x4000,
        ButtonWheeledUp = 0x8000,
        ButtonWheeledDown = 0x10000,
        ButtonWheeledLeft = 0x20000,
        ButtonWheeledRight = 0x40000,
        Button4Pressed = 0x80000,
        Button4Released = 0x100000,
        Button4Clicked = 0x200000,
        Button4DoubleClicked = 0x400000,
        Button4TripleClicked = 0x800000,
        ButtonShift = 0x1000000,
        ButtonCtrl = 0x2000000,
        ButtonAlt = 0x4000000,
        ReportMousePosition = 0x8000000,
        AllEvents = -1
    }

    public struct MouseEvent
    {
        public Point Position;
        public MouseButtonState ButtonState;
    }

    public struct WindowSizeEvent
    {
        public Size Size;
    }

    public struct WindowPositionEvent
    {
        public int Top;
        public int Left;
        public Point CursorPosition;
    }

    public struct RequestResponseEvent
    {
        public (string c1Control, string code, string [] values, string terminating) ResultTuple;
    }

    public struct InputResult
    {
        public EventType EventType;
        public ConsoleKeyInfo ConsoleKeyInfo;
        public MouseEvent MouseEvent;
        public WindowSizeEvent WindowSizeEvent;
        public WindowPositionEvent WindowPositionEvent;
        public RequestResponseEvent RequestResponseEvent;

        public readonly override string? ToString ()
        {
            return EventType switch
                   {
                       EventType.Key   => ToString (ConsoleKeyInfo),
                       EventType.Mouse => MouseEvent.ToString (),

                       //EventType.WindowSize => WindowSize.ToString (),
                       //EventType.RequestResponse => RequestResponse.ToString (),
                       _ => "Unknown event type: " + EventType
                   };
        }

        /// <summary>Prints a ConsoleKeyInfoEx structure</summary>
        /// <param name="cki"></param>
        /// <returns></returns>
        public static string ToString (ConsoleKeyInfo cki)
        {
            Key ke = new ((KeyCode)cki.KeyChar);
            StringBuilder sb = new ();
            sb.Append ($"Key: {(KeyCode)cki.Key} ({cki.Key})");
            sb.Append ((cki.Modifiers & ConsoleModifiers.Shift) != 0 ? " | Shift" : string.Empty);
            sb.Append ((cki.Modifiers & ConsoleModifiers.Control) != 0 ? " | Control" : string.Empty);
            sb.Append ((cki.Modifiers & ConsoleModifiers.Alt) != 0 ? " | Alt" : string.Empty);
            sb.Append ($", KeyChar: {ke.AsRune.MakePrintable ()} ({(uint)cki.KeyChar}) ");
            string s = sb.ToString ().TrimEnd (',').TrimEnd (' ');

            return $"[ConsoleKeyInfo({s})]";
        }
    }

    private void HandleKeyboardEvent (ConsoleKeyInfo cki)
    {
        InputResult inputResult = new() { EventType = EventType.Key, ConsoleKeyInfo = cki };

        _inputQueue.Enqueue (inputResult);
    }

    public void Dispose ()
    {
        _inputReadyCancellationTokenSource?.Cancel ();
        _inputReadyCancellationTokenSource?.Dispose ();
        _inputReadyCancellationTokenSource = null;

        try
        {
            // throws away any typeahead that has been typed by
            // the user and has not yet been read by the program.
            while (Console.KeyAvailable)
            {
                Console.ReadKey (true);
            }
        }
        catch (InvalidOperationException)
        {
            // Ignore - Console input has already been closed
        }
    }
}