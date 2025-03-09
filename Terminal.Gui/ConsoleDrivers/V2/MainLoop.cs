#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Terminal.Gui;

/// <inheritdoc/>
public class MainLoop<T> : IMainLoop<T>
{
    private ITimedEvents? _timedEvents;
    private ConcurrentQueue<T>? _inputBuffer;
    private IInputProcessor? _inputProcessor;
    private IConsoleOutput? _out;
    private AnsiRequestScheduler? _ansiRequestScheduler;
    private IWindowSizeMonitor? _windowSizeMonitor;

    /// <inheritdoc/>
    public ITimedEvents TimedEvents
    {
        get => _timedEvents ?? throw new NotInitializedException (nameof (TimedEvents));
        private set => _timedEvents = value;
    }

    // TODO: follow above pattern for others too

    /// <summary>
    ///     The input events thread-safe collection. This is populated on separate
    ///     thread by a <see cref="IConsoleInput{T}"/>. Is drained as part of each
    ///     <see cref="Iteration"/>
    /// </summary>
    public ConcurrentQueue<T> InputBuffer
    {
        get => _inputBuffer ?? throw new NotInitializedException (nameof (InputBuffer));
        private set => _inputBuffer = value;
    }

    /// <inheritdoc/>
    public IInputProcessor InputProcessor
    {
        get => _inputProcessor ?? throw new NotInitializedException (nameof (InputProcessor));
        private set => _inputProcessor = value;
    }

    /// <inheritdoc/>
    public IOutputBuffer OutputBuffer { get; } = new OutputBuffer ();

    /// <inheritdoc/>
    public IConsoleOutput Out
    {
        get => _out ?? throw new NotInitializedException (nameof (Out));
        private set => _out = value;
    }

    /// <inheritdoc/>
    public AnsiRequestScheduler AnsiRequestScheduler
    {
        get => _ansiRequestScheduler ?? throw new NotInitializedException (nameof (AnsiRequestScheduler));
        private set => _ansiRequestScheduler = value;
    }

    /// <inheritdoc/>
    public IWindowSizeMonitor WindowSizeMonitor
    {
        get => _windowSizeMonitor ?? throw new NotInitializedException (nameof (WindowSizeMonitor));
        private set => _windowSizeMonitor = value;
    }

    /// <summary>
    ///     Handles raising events and setting required draw status etc when <see cref="Application.Top"/> changes
    /// </summary>
    public IToplevelTransitionManager ToplevelTransitionManager = new ToplevelTransitionManager ();

    /// <summary>
    ///     Determines how to get the current system type, adjust
    ///     in unit tests to simulate specific timings.
    /// </summary>
    public Func<DateTime> Now { get; set; } = () => DateTime.Now;

    /// <summary>
    ///     Initializes the class with the provided subcomponents
    /// </summary>
    /// <param name="timedEvents"></param>
    /// <param name="inputBuffer"></param>
    /// <param name="inputProcessor"></param>
    /// <param name="consoleOutput"></param>
    public void Initialize (ITimedEvents timedEvents, ConcurrentQueue<T> inputBuffer, IInputProcessor inputProcessor, IConsoleOutput consoleOutput)
    {
        InputBuffer = inputBuffer;
        Out = consoleOutput;
        InputProcessor = inputProcessor;

        TimedEvents = timedEvents;
        AnsiRequestScheduler = new (InputProcessor.GetParser ());

        WindowSizeMonitor = new WindowSizeMonitor (Out, OutputBuffer);
    }

    /// <inheritdoc/>
    public void Iteration ()
    {
        DateTime dt = Now ();

        IterationImpl ();

        TimeSpan took = Now () - dt;
        TimeSpan sleepFor = TimeSpan.FromMilliseconds (50) - took;

        Logging.TotalIterationMetric.Record (took.Milliseconds);

        if (sleepFor.Milliseconds > 0)
        {
            Task.Delay (sleepFor).Wait ();
        }
    }

    internal void IterationImpl ()
    {
        InputProcessor.ProcessQueue ();

        ToplevelTransitionManager.RaiseReadyEventIfNeeded ();
        ToplevelTransitionManager.HandleTopMaybeChanging ();

        if (Application.Top != null)
        {
            bool needsDrawOrLayout = AnySubviewsNeedDrawn (Application.Top);

            bool sizeChanged = WindowSizeMonitor.Poll ();

            if (needsDrawOrLayout || sizeChanged)
            {
                Logging.Redraws.Add (1);

                Application.LayoutAndDrawImpl (true);

                Out.Write (OutputBuffer);

                Out.SetCursorVisibility (CursorVisibility.Default);
            }

            SetCursor ();
        }

        var swCallbacks = Stopwatch.StartNew ();

        TimedEvents.LockAndRunTimers ();

        TimedEvents.LockAndRunIdles ();

        Logging.IterationInvokesAndTimeouts.Record (swCallbacks.Elapsed.Milliseconds);
    }

    private void SetCursor ()
    {
        View? mostFocused = Application.Top!.MostFocused;

        if (mostFocused == null)
        {
            return;
        }

        Point? to = mostFocused.PositionCursor ();

        if (to.HasValue)
        {
            // Translate to screen coordinates
            to = mostFocused.ViewportToScreen (to.Value);

            Out.SetCursorPosition (to.Value.X, to.Value.Y);
            Out.SetCursorVisibility (mostFocused.CursorVisibility);
        }
        else
        {
            Out.SetCursorVisibility (CursorVisibility.Invisible);
        }
    }

    private bool AnySubviewsNeedDrawn (View v)
    {
        if (v.NeedsDraw || v.NeedsLayout)
        {
            Logging.Trace ($"{v.GetType ().Name} triggered redraw (NeedsDraw={v.NeedsDraw} NeedsLayout={v.NeedsLayout}) ");

            return true;
        }

        foreach (View subview in v.Subviews)
        {
            if (AnySubviewsNeedDrawn (subview))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        // TODO release managed resources here
    }
}
