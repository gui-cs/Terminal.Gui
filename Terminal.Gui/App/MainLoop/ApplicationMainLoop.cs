using System.Collections.Concurrent;
using System.Diagnostics;
using Trace = Terminal.Gui.Tracing.Trace;

namespace Terminal.Gui.App;

/// <summary>
///     The main application loop that runs Terminal.Gui's UI rendering and event processing.
/// </summary>
/// <remarks>
///     This class coordinates the Terminal.Gui application lifecycle by:
///     <list type="bullet">
///         <item>Processing buffered input events and translating them to UI events</item>
///         <item>Executing user timeout callbacks at scheduled intervals</item>
///         <item>Detecting which views need redrawing or layout updates</item>
///         <item>Rendering UI changes to the console output buffer</item>
///         <item>Managing cursor position and visibility</item>
///         <item>Throttling iterations to respect <see cref="Application.MaximumIterationsPerSecond"/></item>
///     </list>
/// </remarks>
/// <typeparam name="TInputRecord">Type of raw input events, e.g. <see cref="ConsoleKeyInfo"/> for .NET driver</typeparam>
public class ApplicationMainLoop<TInputRecord> : IApplicationMainLoop<TInputRecord> where TInputRecord : struct
{
    private ITimedEvents? _timedEvents;
    private ConcurrentQueue<TInputRecord>? _inputQueue;
    private IInputProcessor? _inputProcessor;
    private IOutput? _output;
    private AnsiRequestScheduler? _ansiRequestScheduler;
    private ISizeMonitor? _sizeMonitor;

    /// <inheritdoc/>
    public IApplication? App { get; private set; }

    /// <inheritdoc/>
    public ITimedEvents TimedEvents
    {
        get => _timedEvents ?? throw new NotInitializedException (nameof (TimedEvents));
        private set => _timedEvents = value;
    }

    // TODO: follow above pattern for others too

    /// <summary>
    ///     The input events thread-safe collection. This is populated on separate
    ///     thread by a <see cref="IInput{T}"/>. Is drained as part of each
    ///     <see cref="Iteration"/> on the main loop thread.
    /// </summary>
    public ConcurrentQueue<TInputRecord> InputQueue
    {
        get => _inputQueue ?? throw new NotInitializedException (nameof (InputQueue));
        private set => _inputQueue = value;
    }

    /// <inheritdoc/>
    public IInputProcessor InputProcessor
    {
        get => _inputProcessor ?? throw new NotInitializedException (nameof (InputProcessor));
        private set => _inputProcessor = value;
    }

    /// <inheritdoc/>
    public IOutputBuffer OutputBuffer { get; } = new OutputBufferImpl ();

    /// <inheritdoc/>
    public IOutput Output
    {
        get => _output ?? throw new NotInitializedException (nameof (Output));
        private set => _output = value;
    }

    /// <inheritdoc/>
    public AnsiRequestScheduler AnsiRequestScheduler
    {
        get => _ansiRequestScheduler ?? throw new NotInitializedException (nameof (AnsiRequestScheduler));
        private set => _ansiRequestScheduler = value;
    }

    /// <inheritdoc/>
    public ISizeMonitor SizeMonitor
    {
        get => _sizeMonitor ?? throw new NotInitializedException (nameof (SizeMonitor));
        private set => _sizeMonitor = value;
    }

    /// <summary>
    ///     Initializes the class with the provided subcomponents
    /// </summary>
    /// <param name="timedEvents"></param>
    /// <param name="inputBuffer"></param>
    /// <param name="inputProcessor"></param>
    /// <param name="consoleOutput"></param>
    /// <param name="componentFactory"></param>
    /// <param name="app"></param>
    public void Initialize (
        ITimedEvents timedEvents,
        ConcurrentQueue<TInputRecord> inputBuffer,
        IInputProcessor inputProcessor,
        IOutput consoleOutput,
        IComponentFactory<TInputRecord> componentFactory,
        IApplication? app
    )
    {
        App = app;
        InputQueue = inputBuffer;
        Output = consoleOutput;
        InputProcessor = inputProcessor;

        TimedEvents = timedEvents;
        AnsiRequestScheduler = new AnsiRequestScheduler (InputProcessor.GetParser ());

        // In inline mode, cells must start clean so the first render only flushes
        // cells that views explicitly draw, leaving the rest of the terminal untouched.
        if (Application.AppModel == AppModel.Inline && OutputBuffer is OutputBufferImpl outputBufferImpl)
        {
            outputBufferImpl.InlineMode = true;
        }

        OutputBuffer.SetSize (consoleOutput.GetSize ().Width, consoleOutput.GetSize ().Height);
        SizeMonitor = componentFactory.CreateSizeMonitor (Output, OutputBuffer);
    }

    /// <inheritdoc/>
    public void Iteration ()
    {
        App?.RaiseIteration ();

        DateTime dt = DateTime.Now;
        int timeAllowed = 1000 / Math.Max (1, (int)Application.MaximumIterationsPerSecond);

        IterationImpl ();

        TimeSpan took = DateTime.Now - dt;
        TimeSpan sleepFor = TimeSpan.FromMilliseconds (timeAllowed) - took;

        Logging.TotalIterationMetric.Record (took.Milliseconds);

        if (sleepFor.Milliseconds > 0)
        {
            Task.Delay (sleepFor).Wait ();
        }
    }

    /// <summary>
    ///     Tracks whether the initial terminal size has been confirmed for inline mode.
    ///     Set to <see langword="true"/> once <see cref="ISizeMonitor.InitialSizeReceived"/> is
    ///     <see langword="true"/> or the wait timeout expires.
    /// </summary>
    private bool _inlineSizeConfirmed;

    private DateTime? _inlineWaitStarted;

    /// <summary>
    ///     Maximum time to wait for the ANSI size response before rendering with the fallback size.
    ///     Most terminals respond to CSI 18t within a few milliseconds; 500 ms is a generous upper bound.
    /// </summary>
    private static readonly TimeSpan _inlineSizeWaitTimeout = TimeSpan.FromMilliseconds (500);

    internal void IterationImpl ()
    {
        // Pull any input events from the input queue and process them
        InputProcessor.ProcessQueue ();

        // Run any queued ANSI requests that previously could not be sent
        // (e.g. throttled duplicate request sent too soon after an earlier one).
        AnsiRequestScheduler.RunSchedule (App?.Driver);

        // Check for any size changes; this will cause SizeChanged events
        SizeMonitor.Poll ();

        bool shouldDraw = true;

        // For inline mode, defer the first render until the ANSI size monitor confirms
        // the terminal's real dimensions via a CSI 18t response. Without this, the first
        // frame would render using the default 80×25 fallback, placing the inline region
        // at the wrong terminal row.
        if (Application.AppModel == AppModel.Inline && !_inlineSizeConfirmed)
        {
            if (SizeMonitor.InitialSizeReceived)
            {
                _inlineSizeConfirmed = true;

                // Pass the cursor row from the ANSI CPR response to ApplicationImpl
                // so LayoutAndDraw can resize Screen and set the rendering row offset.
                if (App is ApplicationImpl appImpl)
                {
                    appImpl.InlineCursorRow = SizeMonitor.InitialCursorRow;
                }

                Trace.Lifecycle (nameof (ApplicationMainLoop<TInputRecord>), "InlineSizeConfirmed", $"Screen={App?.Screen}, CursorRow={SizeMonitor.InitialCursorRow}");
            }
            else
            {
                _inlineWaitStarted ??= DateTime.Now;

                if (DateTime.Now - _inlineWaitStarted.Value < _inlineSizeWaitTimeout)
                {
                    shouldDraw = false;
                    Trace.Lifecycle (nameof (ApplicationMainLoop<TInputRecord>), "InlineSizeDeferred", "Waiting for ANSI size response");
                }
                else
                {
                    // Timeout — render with whatever size we have
                    _inlineSizeConfirmed = true;
                    Trace.Lifecycle (nameof (ApplicationMainLoop<TInputRecord>), "InlineSizeTimeout", $"Rendering with fallback. Screen={App?.Screen}");
                }
            }
        }

        if (shouldDraw)
        {
            Trace.Draw (nameof (ApplicationMainLoop<TInputRecord>), "IterationDraw", $"Screen={App?.Screen}");

            // Layout and draw any views that need it
            App?.LayoutAndDraw (false);

            // Update the cursor
            App?.Navigation?.UpdateCursor ();
        }

        var swCallbacks = Stopwatch.StartNew ();

        // Run any timeout callbacks that are due
        TimedEvents.RunTimers ();

        Logging.IterationInvokesAndTimeouts.Record (swCallbacks.Elapsed.Milliseconds);
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        // TODO release managed resources here
    }
}
