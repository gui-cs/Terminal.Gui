#nullable enable

using System.Collections.Concurrent;

namespace Terminal.Gui;

/// <summary>
///     Mainloop intended to be used with the <see cref="WindowsDriver"/>, and can
///     only be used on Windows.
/// </summary>
/// <remarks>
///     This implementation is used for WindowsDriver.
/// </remarks>
internal class WindowsMainLoop : IMainLoopDriver
{
    /// <summary>
    ///     Invoked when the window is changed.
    /// </summary>
    public EventHandler<SizeChangedEventArgs>? WinChanged;

    private readonly IConsoleDriver _consoleDriver;
    private readonly ManualResetEventSlim _eventReady = new (false);

    // The records that we keep fetching
    private readonly ConcurrentQueue<WindowsConsole.InputRecord> _resultQueue = new ();
    private readonly ManualResetEventSlim _waitForProbe = new (false);
    private readonly WindowsConsole? _winConsole;
    private CancellationTokenSource _eventReadyTokenSource = new ();
    private readonly CancellationTokenSource _inputHandlerTokenSource = new ();
    private MainLoop? _mainLoop;

    public WindowsMainLoop (IConsoleDriver consoleDriver)
    {
        _consoleDriver = consoleDriver ?? throw new ArgumentNullException (nameof (consoleDriver));

        if (!ConsoleDriver.RunningUnitTests)
        {
            _winConsole = ((WindowsDriver)consoleDriver).WinConsole;
            _winConsole!._mainLoop = this;
        }
    }

    void IMainLoopDriver.Setup (MainLoop mainLoop)
    {
        _mainLoop = mainLoop;

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        Task.Run (WindowsInputHandler, _inputHandlerTokenSource.Token);
#if HACK_CHECK_WINCHANGED
        Task.Run (CheckWinChange);
#endif
    }

    void IMainLoopDriver.Wakeup () { _eventReady.Set (); }

    bool IMainLoopDriver.EventsPending ()
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return true;
        }

        _waitForProbe.Set ();
#if HACK_CHECK_WINCHANGED
        _winChange.Set ();
#endif
        if (_resultQueue.Count > 0 || _mainLoop!.TimedEvents.CheckTimersAndIdleHandlers (out int waitTimeout))
        {
            return true;
        }

        try
        {
            if (!_eventReadyTokenSource.IsCancellationRequested)
            {
                // Note: ManualResetEventSlim.Wait will wait indefinitely if the timeout is -1. The timeout is -1 when there
                // are no timers, but there IS an idle handler waiting.
                _eventReady.Wait (waitTimeout, _eventReadyTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            return true;
        }
        finally
        {
            if (!_eventReadyTokenSource.IsCancellationRequested)
            {
                _eventReady.Reset ();
            }
        }

        if (!_eventReadyTokenSource.IsCancellationRequested)
        {
#if HACK_CHECK_WINCHANGED
            return _resultQueue.Count > 0 || _mainLoop.TimedEvents.CheckTimersAndIdleHandlers (out _) || _winChanged;
#else
            return _resultQueue.Count > 0 || _mainLoop.TimedEvents.CheckTimersAndIdleHandlers (out _);
#endif
        }

        _eventReadyTokenSource.Dispose ();
        _eventReadyTokenSource = new CancellationTokenSource ();

        // If cancellation was requested then always return true
        return true;
    }

    void IMainLoopDriver.Iteration ()
    {
        foreach (var i in ((WindowsDriver)_consoleDriver).ShouldReleaseParserHeldKeys ())
        {
            ((WindowsDriver)_consoleDriver).ProcessInputAfterParsing (i);
        }

        while (!ConsoleDriver.RunningUnitTests && _resultQueue.TryDequeue (out WindowsConsole.InputRecord inputRecords))
        {
            ((WindowsDriver)_consoleDriver).ProcessInput (inputRecords);
        }
#if HACK_CHECK_WINCHANGED
        if (_winChanged)
        {
            _winChanged = false;
            WinChanged?.Invoke (this, new SizeChangedEventArgs (_windowSize));
        }
#endif
    }

    void IMainLoopDriver.TearDown ()
    {
        _inputHandlerTokenSource.Cancel ();
        _inputHandlerTokenSource.Dispose ();

        if (_winConsole is { })
        {
            var numOfEvents = _winConsole.GetNumberOfConsoleInputEvents ();

            if (numOfEvents > 0)
            {
                _winConsole.FlushConsoleInputBuffer ();
                //Debug.WriteLine ($"Flushed {numOfEvents} events.");
            }
        }

        _waitForProbe.Dispose ();

        _resultQueue.Clear ();

        _eventReadyTokenSource.Cancel ();
        _eventReadyTokenSource.Dispose ();
        _eventReady.Dispose ();

#if HACK_CHECK_WINCHANGED
        _winChange?.Dispose ();
#endif

        _mainLoop = null;
    }

    private void WindowsInputHandler ()
    {
        while (_mainLoop is { })
        {
            try
            {
                if (_inputHandlerTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        _waitForProbe.Wait (_inputHandlerTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException or ObjectDisposedException)
                        {
                            return;
                        }

                        throw;
                    }

                    _waitForProbe.Reset ();
                }

                ProcessInputQueue ();
            }
            catch (OperationCanceledException)
            {
                return;
            }

        }
    }

    private void ProcessInputQueue ()
    {
        if (_resultQueue?.Count == 0)
        {
            WindowsConsole.InputRecord? result = _winConsole!.DequeueInput ();

            if (result.HasValue)
            {
                _resultQueue!.Enqueue (result.Value);

                _eventReady.Set ();
            }
        }
    }

#if HACK_CHECK_WINCHANGED
    private readonly ManualResetEventSlim _winChange = new (false);
    private bool _winChanged;
    private Size _windowSize;
    private void CheckWinChange ()
    {
        while (_mainLoop is { })
        {
            _winChange.Wait ();
            _winChange.Reset ();

            // Check if the window size changed every half second.
            // We do this to minimize the weird tearing seen on Windows when resizing the console
            while (_mainLoop is { })
            {
                Task.Delay (500).Wait ();
                _windowSize = _winConsole.GetConsoleBufferWindow (out _);

                if (_windowSize != Size.Empty
                    && (_windowSize.Width != _consoleDriver.Cols
                        || _windowSize.Height != _consoleDriver.Rows))
                {
                    break;
                }
            }

            _winChanged = true;
            _eventReady.Set ();
        }
    }
#endif
}
