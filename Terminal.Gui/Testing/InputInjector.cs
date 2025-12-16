using Terminal.Gui.Drivers;
using Terminal.Gui.Input;

namespace Terminal.Gui;

/// <summary>
/// Implementation of IInputInjector for testing.
/// Provides a simplified API for injecting input into Terminal.Gui applications.
/// </summary>
public class InputInjector : IInputInjector
{
    private readonly IInputProcessor _processor;
    private readonly ITimeProvider _timeProvider;
    private readonly TestInputSource? _testSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputInjector"/> class.
    /// </summary>
    /// <param name="processor">The input processor to inject events into.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <param name="testSource">Optional test input source for queue-based injection.</param>
    public InputInjector (IInputProcessor processor, ITimeProvider timeProvider, TestInputSource? testSource = null)
    {
        _processor = processor;
        _timeProvider = timeProvider;
        _testSource = testSource;
    }

    /// <inheritdoc/>
    public void InjectKey (Key key, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };
        InputInjectionMode mode = ResolveMode (options.Mode);

        if (mode == InputInjectionMode.Direct)
        {
            // Direct injection - bypass encoding, raise event directly
            _processor.RaiseKeyDownEvent (key);
            _processor.RaiseKeyUpEvent (key);
        }
        else // Pipeline
        {
            // Pipeline injection - use enqueue to go through processing pipeline
            _processor.EnqueueKeyDownEvent (key);

            if (_testSource != null)
            {
                // If we have a test source, enqueue the record for full pipeline processing
                KeyboardEventRecord record = new (key) { Timestamp = (_timeProvider ?? options.TimeProvider ?? new SystemTimeProvider ()).Now };
                _testSource.Enqueue (record);
            }
        }

        if (options.AutoProcess)
        {
            ProcessQueue ();
        }
    }

    /// <inheritdoc/>
    public void InjectMouse (MouseEventArgs mouseEvent, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };
        InputInjectionMode mode = ResolveMode (options.Mode);

        // Set timestamp if not provided (MouseEventArgs doesn't have Timestamp property in current implementation)
        // For now, we'll work with the existing system

        if (mode == InputInjectionMode.Direct)
        {
            // Direct injection - bypass encoding, raise event directly
            _processor.RaiseMouseEvent (mouseEvent);
        }
        else // Pipeline
        {
            // Pipeline injection - use enqueue to go through processing pipeline
            _processor.EnqueueMouseEvent (null, mouseEvent);

            if (_testSource != null)
            {
                // If we have a test source, enqueue the record for full pipeline processing
                MouseEventRecord record = new (mouseEvent) { Timestamp = (_timeProvider ?? options.TimeProvider ?? new SystemTimeProvider ()).Now };
                _testSource.Enqueue (record);
            }
        }

        if (options.AutoProcess)
        {
            ProcessQueue ();
        }
    }

    /// <inheritdoc/>
    public void InjectSequence (IEnumerable<InputEvent> events, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };

        foreach (InputEvent evt in events)
        {
            // Advance time if delay specified
            if (evt.Delay.HasValue && _timeProvider is VirtualTimeProvider vtp)
            {
                vtp.Advance (evt.Delay.Value);
            }

            InputInjectionOptions eventOptions = new ()
            {
                Mode = options.Mode,
                AutoProcess = false,
                TimeProvider = options.TimeProvider
            };

            switch (evt)
            {
                case KeyEvent ke:
                    InjectKey (ke.Key, eventOptions);

                    break;

                case MouseEvent me:
                    InjectMouse (me.Mouse, eventOptions);

                    break;
            }
        }

        if (options.AutoProcess)
        {
            ProcessQueue ();
        }
    }

    /// <inheritdoc/>
    public void ProcessQueue ()
    {
        _processor.ProcessQueue ();

        // If using virtual time and parser has stale escape sequences, advance time and process again
        if (_timeProvider is VirtualTimeProvider vtp)
        {
            IAnsiResponseParser? parser = _processor.GetParser ();

            if (parser?.State is AnsiResponseParserState.ExpectingEscapeSequence)
            {
                vtp.Advance (TimeSpan.FromMilliseconds (60)); // Past 50ms escape timeout
                _processor.ProcessQueue ();
            }
        }
    }

    private InputInjectionMode ResolveMode (InputInjectionMode mode)
    {
        if (mode != InputInjectionMode.Auto)
        {
            return mode;
        }

        // Auto mode: Default to Direct for faster, simpler tests.
        // Use Pipeline explicitly when you need to test encoding/parsing.
        return InputInjectionMode.Direct;
    }
}
