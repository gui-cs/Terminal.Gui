using System.Collections.Concurrent;
using Terminal.Gui.Drivers;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.App;

/// <summary>
///     <para>
///         Coordinates the creation and startup of the main UI loop and input thread.
///     </para>
///     <para>
///         This class bootstraps the <see cref="ApplicationMainLoop{T}"/> that handles
///         UI layout, drawing, and event processing while also managing a separate thread
///         for reading console input asynchronously.
///     </para>
///     <para>This class is designed to be managed by <see cref="ApplicationImpl"/></para>
/// </summary>
/// <typeparam name="T">Type of raw input events, e.g. <see cref="ConsoleKeyInfo"/> for .NET driver</typeparam>
internal class MainLoopCoordinator<T> : IMainLoopCoordinator
{
    private readonly ConcurrentQueue<T> _inputBuffer;
    private readonly IInputProcessor _inputProcessor;
    private readonly IApplicationMainLoop<T> _loop;
    private readonly IComponentFactory<T> _componentFactory;
    private readonly CancellationTokenSource _tokenSource = new ();
    private IConsoleInput<T> _input;
    private IConsoleOutput _output;
    private readonly object _oLockInitialization = new ();
    private ConsoleDriverFacade<T> _facade;
    private Task _inputTask;
    private readonly ITimedEvents _timedEvents;

    private readonly SemaphoreSlim _startupSemaphore = new (0, 1);

    /// <summary>
    ///     Creates a new coordinator that will manage the main UI loop and input thread.
    /// </summary>
    /// <param name="timedEvents">Handles scheduling and execution of user timeout callbacks</param>
    /// <param name="inputBuffer">Thread-safe queue for buffering raw console input</param>
    /// <param name="loop">The main application loop instance</param>
    /// <param name="componentFactory">Factory for creating driver-specific components (input, output, etc.)</param>
    public MainLoopCoordinator (
        ITimedEvents timedEvents,
        ConcurrentQueue<T> inputBuffer,
        IApplicationMainLoop<T> loop,
        IComponentFactory<T> componentFactory
    )
    {
        _timedEvents = timedEvents;
        _inputBuffer = inputBuffer;
        _inputProcessor = componentFactory.CreateInputProcessor (_inputBuffer);
        _loop = loop;
        _componentFactory = componentFactory;
    }

    /// <summary>
    ///     Starts the input loop thread in separate task (returning immediately).
    /// </summary>
    public async Task StartAsync ()
    {
        Logging.Logger.LogInformation ("Main Loop Coordinator booting...");

        _inputTask = Task.Run (RunInput);

        // Main loop is now booted on same thread as rest of users application
        BootMainLoop ();

        // Wait asynchronously for the semaphore or task failure.
        Task waitForSemaphore = _startupSemaphore.WaitAsync ();

        // Wait for either the semaphore to be released or the input task to crash.
        // ReSharper disable once UseConfigureAwaitFalse
        Task completedTask = await Task.WhenAny (waitForSemaphore, _inputTask);

        // Check if the task was the input task and if it has failed.
        if (completedTask == _inputTask)
        {
            if (_inputTask.IsFaulted)
            {
                throw _inputTask.Exception;
            }

            Logging.Logger.LogCritical("Input loop exited during startup instead of entering read loop properly (i.e. and blocking)");
        }

        Logging.Logger.LogInformation ("Main Loop Coordinator booting complete");
    }

    private void RunInput ()
    {
        try
        {
            lock (_oLockInitialization)
            {
                // Instance must be constructed on the thread in which it is used.
                _input = _componentFactory.CreateInput ();
                _input.Initialize (_inputBuffer);

                BuildFacadeIfPossible ();
            }

            try
            {
                _input.Run (_tokenSource.Token);
            }
            catch (OperationCanceledException)
            { }

            _input.Dispose ();
        }
        catch (Exception e)
        {
            Logging.Logger.LogCritical (e, "Input loop crashed");

            throw;
        }

        if (_stopCalled)
        {
            Logging.Logger.LogInformation ("Input loop exited cleanly");
        }
        else
        {
            Logging.Logger.LogCritical ("Input loop exited early (stop not called)");
        }
    }

    /// <inheritdoc/>
    public void RunIteration () { _loop.Iteration (); }

    private void BootMainLoop ()
    {
        lock (_oLockInitialization)
        {
            // Instance must be constructed on the thread in which it is used.
            _output = _componentFactory.CreateOutput ();
            _loop.Initialize (_timedEvents, _inputBuffer, _inputProcessor, _output,_componentFactory);

            BuildFacadeIfPossible ();
        }
    }

    private void BuildFacadeIfPossible ()
    {
        if (_input != null && _output != null)
        {
            _facade = new (
                           _inputProcessor,
                           _loop.OutputBuffer,
                           _output,
                           _loop.AnsiRequestScheduler,
                           _loop.WindowSizeMonitor);

            Application.Driver = _facade;

            _startupSemaphore.Release ();
        }
    }

    private bool _stopCalled;

    /// <inheritdoc/>
    public void Stop ()
    {
        // Ignore repeated calls to Stop - happens if user spams Application.Shutdown().
        if (_stopCalled)
        {
            return;
        }

        _stopCalled = true;

        _tokenSource.Cancel ();
        _output.Dispose ();

        // Wait for input infinite loop to exit
        _inputTask.Wait ();
    }
}
