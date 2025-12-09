using System.Collections.Concurrent;

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
/// <typeparam name="TInputRecord">Type of raw input events, e.g. <see cref="ConsoleKeyInfo"/> for .NET driver</typeparam>
internal class MainLoopCoordinator<TInputRecord> : IMainLoopCoordinator where TInputRecord : struct
{
    /// <summary>
    ///     Creates a new coordinator that will manage the main UI loop and input thread.
    /// </summary>
    /// <param name="timedEvents">Handles scheduling and execution of user timeout callbacks</param>
    /// <param name="inputQueue">Thread-safe queue for buffering raw console input</param>
    /// <param name="loop">The main application loop instance</param>
    /// <param name="componentFactory">Factory for creating driver-specific components (input, output, etc.)</param>
    public MainLoopCoordinator (
        ITimedEvents timedEvents,
        ConcurrentQueue<TInputRecord> inputQueue,
        IApplicationMainLoop<TInputRecord> loop,
        IComponentFactory<TInputRecord> componentFactory
    )
    {
        _timedEvents = timedEvents;
        _inputQueue = inputQueue;
        _inputProcessor = componentFactory.CreateInputProcessor (_inputQueue);
        _loop = loop;
        _componentFactory = componentFactory;
    }

    private readonly IApplicationMainLoop<TInputRecord> _loop;
    private readonly IComponentFactory<TInputRecord> _componentFactory;
    private readonly CancellationTokenSource _runCancellationTokenSource = new ();
    private readonly ConcurrentQueue<TInputRecord> _inputQueue;
    private readonly IInputProcessor _inputProcessor;
    private readonly object _oLockInitialization = new ();
    private readonly ITimedEvents _timedEvents;

    private readonly SemaphoreSlim _startupSemaphore = new (0, 1);
    private IInput<TInputRecord>? _input;
    private Task? _inputTask;
    private IOutput? _output;
    private DriverImpl? _driver;

    private bool _stopCalled;

    /// <summary>
    ///     Starts the input loop thread in separate task (returning immediately).
    /// </summary>
    /// <param name="app">The <see cref="IApplication"/> instance that is running the input loop.</param>
    public async Task StartInputTaskAsync (IApplication? app)
    {
        Logging.Trace ("Booting... ()");

        _inputTask = Task.Run (() => RunInput (app));

        // Main loop is now booted on same thread as rest of users application
        BootMainLoop (app);

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

            Logging.Critical ("Input loop exited during startup instead of entering read loop properly (i.e. and blocking)");
        }

        Logging.Trace ("Booting complete");
    }

    /// <inheritdoc/>
    public void RunIteration ()
    {
        lock (_oLockInitialization)
        {
            _loop.Iteration ();
        }
    }

    /// <inheritdoc/>
    public void Stop ()
    {
        // Ignore repeated calls to Stop - happens if user spams Application.Shutdown().
        if (_stopCalled)
        {
            return;
        }

        _stopCalled = true;

        _runCancellationTokenSource.Cancel ();
        _output?.Dispose ();

        // Wait for input infinite loop to exit
        _inputTask?.Wait ();
    }

    private void BootMainLoop (IApplication? app)
    {
        //Logging.Trace ($"_inputProcessor: {_inputProcessor}, _output: {_output}, _componentFactory: {_componentFactory}");

        lock (_oLockInitialization)
        {
            // Instance must be constructed on the thread in which it is used.
            _output = _componentFactory.CreateOutput ();
            _loop.Initialize (_timedEvents, _inputQueue, _inputProcessor, _output, _componentFactory, app);

            BuildDriverIfPossible (app);
        }
    }

    private void BuildDriverIfPossible (IApplication? app)
    {

        if (_input != null && _output != null)
        {
            _driver = new (
                           _inputProcessor,
                           _loop.OutputBuffer,
                           _output,
                           _loop.AnsiRequestScheduler,
                           _loop.SizeMonitor);

            app!.Driver = _driver;

            _startupSemaphore.Release ();
            Logging.Trace ($"Driver: _input: {_input}, _output: {_output}");
        }
    }

    /// <summary>
    ///     INTERNAL: Runs the IInput read loop on a new thread called the "Input Thread".
    /// </summary>
    /// <param name="app"></param>
    private void RunInput (IApplication? app)
    {
        try
        {
            lock (_oLockInitialization)
            {
                // Instance must be constructed on the thread in which it is used.
                _input = _componentFactory.CreateInput ();
                _input.Initialize (_inputQueue);

                // Wire up InputImpl reference for ITestableInput support
                if (_inputProcessor is InputProcessorImpl<TInputRecord> impl)
                {
                    impl.InputImpl = _input;
                }

                BuildDriverIfPossible (app);
            }

            try
            {
                _input.Run (_runCancellationTokenSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                Logging.Debug ($"Input loop canceled: {ex.Message}");
            }

            _input.Dispose ();
        }
        catch (Exception e)
        {
            Logging.Critical ($"Input loop crashed: {e}");

            throw;
        }

        if (_stopCalled)
        {
            Logging.Information ("Input loop exited cleanly");
        }
        else
        {
            Logging.Critical ("Input loop exited early (stop not called)");
        }
    }
}
