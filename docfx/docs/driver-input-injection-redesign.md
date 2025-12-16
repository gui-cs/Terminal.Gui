# Driver Input Injection - Redesign Specification

This document proposes a redesigned architecture for input injection in Terminal.Gui, addressing the pain points identified in [driver-input-injection.md](driver-input-injection.md).

## Goals

1. **Unified time control** - Single mechanism for controlling all time-dependent behavior (timestamps, delays, timers)
2. **Simplified injection** - Single method call that handles the full injection pipeline
3. **Deterministic testing** - Eliminate real-time delays and race conditions
4. **Preserve ANSI testing** - Still support full encoding/decoding pipeline testing when needed
5. **Better API** - Cleaner, more intuitive interface for test authors
6. **Maintain compatibility** - Keep external app hooks possible (low priority, but don't preclude it)

## Non-Goals

- External application injection as a primary use case (support it if possible, but don't design around it)
- Backward compatibility with current `ITestableInput`/`InputTestHelpers` (this is a redesign)
- Supporting production drivers (Windows/Unix/Net) with testability interfaces (keep test drivers separate)

## Core Design Principles

### 1. Virtual Time System

**Problem:** Current architecture uses `DateTime.Now` throughout, making timing tests non-deterministic and slow.

**Solution:** Introduce an injectable `ITimeProvider` that allows "faking" time in tests.

```csharp
/// <summary>
/// Abstraction for time-related operations, allowing virtual time in tests.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current date/time. In tests, this can be controlled.
    /// </summary>
    DateTime Now { get; }
    
    /// <summary>
    /// Creates a delay. In tests, this can be instant or controlled.
    /// </summary>
    Task Delay(TimeSpan duration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a timer. In tests, this can be controlled.
    /// </summary>
    ITimer CreateTimer(TimeSpan interval, Action callback);
}

/// <summary>
/// Real time provider using DateTime.Now and Task.Delay.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
    
    public Task Delay(TimeSpan duration, CancellationToken ct) 
        => Task.Delay(duration, ct);
    
    public ITimer CreateTimer(TimeSpan interval, Action callback)
        => new SystemTimer(interval, callback);
}

/// <summary>
/// Virtual time provider for testing - all time is controlled.
/// </summary>
public class VirtualTimeProvider : ITimeProvider
{
    private DateTime _currentTime = new(2025, 1, 1, 0, 0, 0);
    private readonly List<VirtualTimer> _timers = new();
    private readonly List<VirtualDelay> _delays = new();
    
    public DateTime Now => _currentTime;
    
    /// <summary>
    /// Advance virtual time by the specified duration.
    /// This triggers any timers/delays that should fire.
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        _currentTime += duration;
        
        // Fire any timers that should trigger
        foreach (var timer in _timers.Where(t => t.NextTrigger <= _currentTime))
        {
            timer.Trigger();
        }
        
        // Complete any delays that should finish
        foreach (var delay in _delays.Where(d => d.CompletionTime <= _currentTime))
        {
            delay.Complete();
        }
    }
    
    /// <summary>
    /// Set virtual time to a specific value.
    /// </summary>
    public void SetTime(DateTime time)
    {
        _currentTime = time;
    }
    
    public Task Delay(TimeSpan duration, CancellationToken ct)
    {
        var delay = new VirtualDelay(_currentTime + duration, ct);
        _delays.Add(delay);
        return delay.Task;
    }
    
    public ITimer CreateTimer(TimeSpan interval, Action callback)
    {
        var timer = new VirtualTimer(_currentTime, interval, callback);
        _timers.Add(timer);
        return timer;
    }
}
```

**Usage in components:**
```csharp
// MouseButtonClickTracker uses ITimeProvider
public class MouseButtonClickTracker
{
    private readonly ITimeProvider _timeProvider;
    
    public MouseButtonClickTracker(ITimeProvider timeProvider, ...)
    {
        _timeProvider = timeProvider;
    }
    
    public void UpdateState(Mouse mouse, out int? numClicks)
    {
        // Use virtual time instead of DateTime.Now
        DateTime currentTime = mouse.Timestamp ?? _timeProvider.Now;
        TimeSpan elapsed = currentTime - At;
        
        // Check threshold using virtual time
        if (elapsed >= _repeatClickThreshold || !isSamePosition)
        {
            _consecutiveClicks = 0;
        }
        // ...
    }
}

// AnsiResponseParser uses ITimeProvider for escape sequence timeouts
public class AnsiResponseParser<TInputRecord>
{
    private readonly ITimeProvider _timeProvider;
    
    public bool IsEscapeSequenceStale()
    {
        return State == AnsiResponseParserState.ExpectingEscapeSequence 
            && _timeProvider.Now - StateChangedAt > EscapeTimeout;
    }
}
```

**Test usage:**
```csharp
[Fact]
public void DoubleClick_WithinThreshold_DetectsDoubleClick()
{
    // Create app with virtual time
    var timeProvider = new VirtualTimeProvider();
    timeProvider.SetTime(new DateTime(2025, 1, 1, 12, 0, 0));
    
    using var app = Application.Create(timeProvider);
    app.Init(DriverRegistry.Names.Fake);
    
    // Inject events with controlled timing
    app.InjectMouseEvent(new() { 
        Flags = MouseFlags.LeftButtonPressed,
        Position = new(5, 5)
    });
    
    timeProvider.Advance(TimeSpan.FromMilliseconds(50));
    
    app.InjectMouseEvent(new() { 
        Flags = MouseFlags.LeftButtonReleased,
        Position = new(5, 5)
    });
    
    // Second click 300ms later (within 500ms threshold)
    timeProvider.Advance(TimeSpan.FromMilliseconds(300));
    
    app.InjectMouseEvent(new() { 
        Flags = MouseFlags.LeftButtonPressed,
        Position = new(5, 5)
    });
    
    timeProvider.Advance(TimeSpan.FromMilliseconds(50));
    
    app.InjectMouseEvent(new() { 
        Flags = MouseFlags.LeftButtonReleased,
        Position = new(5, 5)
    });
    
    // Assert double-click detected
    Assert.Contains(receivedEvents, e => e.Flags.HasFlag(MouseFlags.LeftButtonDoubleClicked));
}
```

### 2. Unified Input Injection API

**Problem:** Current system requires 3 steps (inject ? simulate thread ? process queue) with different paths for testing full pipeline vs. testing with timestamps.

**Solution:** Single injection API that handles everything, with mode control for testing strategy.

```csharp
/// <summary>
/// Mode for input injection, controlling how input flows through the system.
/// </summary>
public enum InputInjectionMode
{
    /// <summary>
    /// Direct event injection - bypasses encoding/parsing, preserves all properties.
    /// Use for testing View/Application logic with precise control.
    /// </summary>
    Direct,
    
    /// <summary>
    /// Pipeline injection - goes through full encoding/parsing pipeline.
    /// Use for testing ANSI encoding/decoding and parser behavior.
    /// </summary>
    Pipeline,
    
    /// <summary>
    /// Automatic mode - uses Direct for test drivers, Pipeline for ANSI drivers.
    /// Recommended default for most tests.
    /// </summary>
    Auto
}

/// <summary>
/// Configuration for input injection behavior.
/// </summary>
public class InputInjectionOptions
{
    /// <summary>
    /// Injection mode (Direct, Pipeline, or Auto).
    /// </summary>
    public InputInjectionMode Mode { get; set; } = InputInjectionMode.Auto;
    
    /// <summary>
    /// Whether to automatically process the input queue after injection.
    /// </summary>
    public bool AutoProcess { get; set; } = true;
    
    /// <summary>
    /// Time provider to use for timestamps and timing.
    /// </summary>
    public ITimeProvider? TimeProvider { get; set; }
}

/// <summary>
/// High-level input injection API - single entry point for all injection.
/// </summary>
public interface IInputInjector
{
    /// <summary>
    /// Inject a keyboard event. Handles encoding, queueing, and processing automatically.
    /// </summary>
    void InjectKey(Key key, InputInjectionOptions? options = null);
    
    /// <summary>
    /// Inject a mouse event. Handles encoding, queueing, and processing automatically.
    /// </summary>
    void InjectMouse(Mouse mouse, InputInjectionOptions? options = null);
    
    /// <summary>
    /// Inject a sequence of input events with delays between them.
    /// </summary>
    void InjectSequence(IEnumerable<InputEvent> events, InputInjectionOptions? options = null);
    
    /// <summary>
    /// Force processing of the input queue (usually automatic).
    /// </summary>
    void ProcessQueue();
}

/// <summary>
/// Base class for input events in sequences.
/// </summary>
public abstract record InputEvent
{
    public TimeSpan? Delay { get; init; }
}

/// <summary>
/// Keyboard event in a sequence.
/// </summary>
public record KeyEvent(Key Key) : InputEvent;

/// <summary>
/// Mouse event in a sequence.
/// </summary>
public record MouseEvent(Mouse Mouse) : InputEvent;
```

**Implementation:**
```csharp
public class InputInjector : IInputInjector
{
    private readonly IApplication _app;
    private readonly IDriver _driver;
    private readonly IInputProcessor _processor;
    private readonly ITimeProvider _timeProvider;
    
    public InputInjector(IApplication app, ITimeProvider timeProvider)
    {
        _app = app;
        _driver = app.Driver ?? throw new InvalidOperationException("Driver not initialized");
        _processor = _driver.GetInputProcessor();
        _timeProvider = timeProvider;
    }
    
    public void InjectKey(Key key, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };
        var mode = ResolveMode(options.Mode);
        
        if (mode == InputInjectionMode.Direct)
        {
            // Direct injection - bypass encoding, raise event directly
            _processor.RaiseKeyDownEvent(key);
            _processor.RaiseKeyUpEvent(key);
        }
        else // Pipeline
        {
            // Pipeline injection - encode ? queue ? parse ? event
            _processor.InjectKeyDownEvent(key);
        }
        
        if (options.AutoProcess)
        {
            ProcessQueue();
        }
    }
    
    public void InjectMouse(Mouse mouse, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };
        var mode = ResolveMode(options.Mode);
        
        // Set timestamp if not provided
        mouse.Timestamp ??= (options.TimeProvider ?? _timeProvider).Now;
        
        if (mode == InputInjectionMode.Direct)
        {
            // Direct injection - bypass encoding, preserve timestamp
            _processor.RaiseMouseEventParsed(mouse);
            _processor.RaiseSyntheticMouseEvent(mouse);
        }
        else // Pipeline
        {
            // Pipeline injection - encode ? queue ? parse ? event
            // Note: Encoding may lose timestamp information
            _processor.InjectMouseEvent(_app, mouse);
        }
        
        if (options.AutoProcess)
        {
            ProcessQueue();
        }
    }
    
    public void InjectSequence(IEnumerable<InputEvent> events, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };
        
        foreach (var evt in events)
        {
            // Advance time if delay specified
            if (evt.Delay.HasValue && options.TimeProvider is VirtualTimeProvider vtp)
            {
                vtp.Advance(evt.Delay.Value);
            }
            
            switch (evt)
            {
                case KeyEvent ke:
                    InjectKey(ke.Key, options with { AutoProcess = false });
                    break;
                case MouseEvent me:
                    InjectMouse(me.Mouse, options with { AutoProcess = false });
                    break;
            }
        }
        
        if (options.AutoProcess)
        {
            ProcessQueue();
        }
    }
    
    public void ProcessQueue()
    {
        _processor.ProcessQueue();
        
        // If using virtual time and parser has stale escape sequences, advance time and process again
        if (_timeProvider is VirtualTimeProvider vtp)
        {
            var parser = _processor.GetParser();
            if (parser.State is AnsiResponseParserState.ExpectingEscapeSequence)
            {
                vtp.Advance(TimeSpan.FromMilliseconds(60)); // Past 50ms escape timeout
                _processor.ProcessQueue();
            }
        }
    }
    
    private InputInjectionMode ResolveMode(InputInjectionMode mode)
    {
        if (mode != InputInjectionMode.Auto)
            return mode;
        
        // Auto mode: Use Direct for FakeDriver, Pipeline for ANSI drivers
        return _driver is FakeDriver ? InputInjectionMode.Direct : InputInjectionMode.Pipeline;
    }
}
```

**Extension methods for convenience:**
```csharp
public static class InputInjectionExtensions
{
    /// <summary>
    /// Get or create the input injector for this application.
    /// </summary>
    public static IInputInjector GetInputInjector(this IApplication app)
    {
        // Cache injector as application metadata
        if (app.Metadata.TryGetValue("InputInjector", out var cached))
            return (IInputInjector)cached;
        
        var timeProvider = app.GetTimeProvider();
        var injector = new InputInjector(app, timeProvider);
        app.Metadata["InputInjector"] = injector;
        return injector;
    }
    
    /// <summary>
    /// Inject a key event (convenience method).
    /// </summary>
    public static void InjectKey(this IApplication app, Key key)
    {
        app.GetInputInjector().InjectKey(key);
    }
    
    /// <summary>
    /// Inject a mouse event (convenience method).
    /// </summary>
    public static void InjectMouse(this IApplication app, Mouse mouse)
    {
        app.GetInputInjector().InjectMouse(mouse);
    }
    
    /// <summary>
    /// Inject a sequence of events (convenience method).
    /// </summary>
    public static void InjectSequence(this IApplication app, params InputEvent[] events)
    {
        app.GetInputInjector().InjectSequence(events);
    }
}
```

**Test usage - simplified:**
```csharp
[Fact]
public void Button_ClickWithMouse_RaisesAccepting()
{
    // Create app with virtual time
    var time = new VirtualTimeProvider();
    using var app = Application.Create(time);
    app.Init(DriverRegistry.Names.Fake);
    
    var button = new Button { Text = "Click Me" };
    var acceptingCalled = false;
    button.Accepting += (s, e) => acceptingCalled = true;
    
    // Single call - injection + processing handled automatically
    app.InjectMouse(new() {
        Flags = MouseFlags.LeftButtonPressed,
        Position = new(5, 5)
    });
    
    app.InjectMouse(new() {
        Flags = MouseFlags.LeftButtonReleased,
        Position = new(5, 5)
    });
    
    Assert.True(acceptingCalled);
}

[Fact]
public void TextField_TypeText_UpdatesContent()
{
    var time = new VirtualTimeProvider();
    using var app = Application.Create(time);
    app.Init(DriverRegistry.Names.Fake);
    
    var textField = new TextField();
    
    // Inject sequence with automatic processing
    app.InjectSequence(
        new KeyEvent(Key.H),
        new KeyEvent(Key.E),
        new KeyEvent(Key.L),
        new KeyEvent(Key.L),
        new KeyEvent(Key.O)
    );
    
    Assert.Equal("HELLO", textField.Text);
}

[Fact]
public void DoubleClick_TestPipelineEncoding()
{
    var time = new VirtualTimeProvider();
    using var app = Application.Create(time);
    app.Init(DriverRegistry.Names.ANSI); // ANSI driver for pipeline testing
    
    // Force pipeline mode to test ANSI encoding/decoding
    var options = new InputInjectionOptions 
    { 
        Mode = InputInjectionMode.Pipeline,
        TimeProvider = time 
    };
    
    // Events go through full ANSI encoding ? parsing ? event pipeline
    app.InjectMouse(new() { 
        Flags = MouseFlags.LeftButtonPressed,
        Position = new(5, 5)
    }, options);
    
    time.Advance(TimeSpan.FromMilliseconds(50));
    
    app.InjectMouse(new() { 
        Flags = MouseFlags.LeftButtonReleased,
        Position = new(5, 5)
    }, options);
    
    // Verify ANSI encoding worked correctly
    Assert.Contains(receivedEvents, e => e.Flags.HasFlag(MouseFlags.LeftButtonClicked));
}
```

### 3. Refactored Input Architecture

**Problem:** Current `IInput`/`ITestableInput` separation is awkward, and test helpers require manual queue simulation.

**Solution:** Redesign input interfaces to be testability-first, with clear separation between production and test implementations.

```csharp
/// <summary>
/// Source of input events. Production implementations read from console,
/// test implementations provide pre-programmed input.
/// </summary>
public interface IInputSource
{
    /// <summary>
    /// Time provider for timestamps and timing.
    /// </summary>
    ITimeProvider TimeProvider { get; }
    
    /// <summary>
    /// Check if input is available without consuming it.
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// Read all available input synchronously.
    /// Called by InputProcessor on the main loop thread.
    /// </summary>
    IEnumerable<InputRecord> ReadAvailable();
    
    /// <summary>
    /// Start background input reading (for production implementations).
    /// Test implementations typically don't need this.
    /// </summary>
    void Start(CancellationToken cancellationToken);
    
    /// <summary>
    /// Stop background input reading.
    /// </summary>
    void Stop();
}

/// <summary>
/// Platform-independent input record.
/// </summary>
public abstract record InputRecord
{
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Keyboard input record.
/// </summary>
public record KeyboardRecord(Key Key) : InputRecord;

/// <summary>
/// Mouse input record (raw, before click synthesis).
/// </summary>
public record MouseRecord(Mouse Mouse) : InputRecord;

/// <summary>
/// ANSI sequence record (for ANSI driver testing).
/// </summary>
public record AnsiRecord(string Sequence) : InputRecord;

/// <summary>
/// Test input source - provides pre-programmed input.
/// </summary>
public class TestInputSource : IInputSource
{
    private readonly Queue<InputRecord> _inputQueue = new();
    
    public ITimeProvider TimeProvider { get; }
    
    public TestInputSource(ITimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
    }
    
    public bool IsAvailable => _inputQueue.Count > 0;
    
    public IEnumerable<InputRecord> ReadAvailable()
    {
        while (_inputQueue.Count > 0)
        {
            yield return _inputQueue.Dequeue();
        }
    }
    
    /// <summary>
    /// Add input to the queue. Called by InputInjector.
    /// </summary>
    public void Enqueue(InputRecord record)
    {
        // Set timestamp if not already set
        if (record.Timestamp == default)
        {
            record = record with { Timestamp = TimeProvider.Now };
        }
        _inputQueue.Enqueue(record);
    }
    
    public void Start(CancellationToken cancellationToken) { /* No-op for test */ }
    public void Stop() { /* No-op for test */ }
}

/// <summary>
/// Console input source - reads from actual console (production).
/// </summary>
public abstract class ConsoleInputSource : IInputSource
{
    private Task? _readTask;
    private CancellationTokenSource? _cancellationTokenSource;
    protected readonly ConcurrentQueue<InputRecord> InputBuffer = new();
    
    public ITimeProvider TimeProvider { get; }
    
    protected ConsoleInputSource(ITimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
    }
    
    public bool IsAvailable => !InputBuffer.IsEmpty;
    
    public IEnumerable<InputRecord> ReadAvailable()
    {
        while (InputBuffer.TryDequeue(out var record))
        {
            yield return record;
        }
    }
    
    public void Start(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _readTask = Task.Run(() => ReadLoop(_cancellationTokenSource.Token), cancellationToken);
    }
    
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _readTask?.Wait(TimeSpan.FromSeconds(1));
    }
    
    /// <summary>
    /// Platform-specific implementation of reading input.
    /// Runs on background thread, enqueues to InputBuffer.
    /// </summary>
    protected abstract Task ReadLoop(CancellationToken cancellationToken);
}
```

**Processor redesign:**
```csharp
/// <summary>
/// Input processor - consumes input from IInputSource and raises events.
/// </summary>
public interface IInputProcessor
{
    /// <summary>
    /// Input source providing input records.
    /// </summary>
    IInputSource InputSource { get; }
    
    /// <summary>
    /// Time provider for timing-dependent operations.
    /// </summary>
    ITimeProvider TimeProvider { get; }
    
    /// <summary>
    /// Process all available input from the source.
    /// Called by main loop on each iteration.
    /// </summary>
    void ProcessInput();
    
    /// <summary>
    /// Keyboard event raised when key is pressed/released.
    /// </summary>
    event EventHandler<Key>? KeyDown;
    event EventHandler<Key>? KeyUp;
    
    /// <summary>
    /// Mouse event raised (includes synthesized clicks).
    /// </summary>
    event EventHandler<Mouse>? MouseEvent;
    
    /// <summary>
    /// ANSI sequence parser (for ANSI drivers only).
    /// </summary>
    IAnsiResponseParser? AnsiParser { get; }
}

public class InputProcessor : IInputProcessor
{
    private readonly MouseInterpreter _mouseInterpreter;
    private readonly IAnsiResponseParser? _ansiParser;
    
    public IInputSource InputSource { get; }
    public ITimeProvider TimeProvider { get; }
    public IAnsiResponseParser? AnsiParser => _ansiParser;
    
    public event EventHandler<Key>? KeyDown;
    public event EventHandler<Key>? KeyUp;
    public event EventHandler<Mouse>? MouseEvent;
    
    public InputProcessor(IInputSource inputSource, ITimeProvider timeProvider, bool enableAnsiParsing = false)
    {
        InputSource = inputSource;
        TimeProvider = timeProvider;
        _mouseInterpreter = new MouseInterpreter(timeProvider);
        
        if (enableAnsiParsing)
        {
            _ansiParser = new AnsiResponseParser<char>(timeProvider);
            _ansiParser.Mouse += (s, e) => RaiseMouse(e);
            _ansiParser.Keyboard += (s, e) => RaiseKeyboard(e);
        }
    }
    
    public void ProcessInput()
    {
        // Read all available input
        foreach (var record in InputSource.ReadAvailable())
        {
            ProcessRecord(record);
        }
        
        // Check for stale escape sequences (if using ANSI parser)
        if (_ansiParser != null && _ansiParser.IsEscapeSequenceStale())
        {
            foreach (var released in _ansiParser.Release())
            {
                ProcessRecord(new AnsiRecord(released.ToString()) { Timestamp = TimeProvider.Now });
            }
        }
    }
    
    private void ProcessRecord(InputRecord record)
    {
        switch (record)
        {
            case KeyboardRecord kr:
                RaiseKeyboard(kr.Key);
                break;
                
            case MouseRecord mr:
                RaiseMouse(mr.Mouse);
                break;
                
            case AnsiRecord ar:
                // Feed to ANSI parser
                _ansiParser?.ProcessInput(ar.Sequence);
                break;
        }
    }
    
    private void RaiseKeyboard(Key key)
    {
        KeyDown?.Invoke(this, key);
        KeyUp?.Invoke(this, key);
    }
    
    private void RaiseMouse(Mouse mouse)
    {
        // Process through MouseInterpreter to generate click events
        foreach (var processedMouse in _mouseInterpreter.Process(mouse))
        {
            MouseEvent?.Invoke(this, processedMouse);
        }
    }
}
```

**InputInjector now works directly with InputSource:**
```csharp
public class InputInjector : IInputInjector
{
    private readonly IInputProcessor _processor;
    private readonly TestInputSource? _testSource;
    
    public InputInjector(IInputProcessor processor)
    {
        _processor = processor;
        _testSource = processor.InputSource as TestInputSource;
        
        if (_testSource == null)
            throw new InvalidOperationException("InputInjector requires TestInputSource");
    }
    
    public void InjectKey(Key key, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions();
        var mode = ResolveMode(options.Mode);
        
        InputRecord record = mode == InputInjectionMode.Direct
            ? new KeyboardRecord(key) { Timestamp = _processor.TimeProvider.Now }
            : new AnsiRecord(AnsiKeyboardEncoder.Encode(key)) { Timestamp = _processor.TimeProvider.Now };
        
        _testSource.Enqueue(record);
        
        if (options.AutoProcess)
        {
            _processor.ProcessInput();
        }
    }
    
    public void InjectMouse(Mouse mouse, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions();
        var mode = ResolveMode(options.Mode);
        
        mouse.Timestamp ??= _processor.TimeProvider.Now;
        
        InputRecord record = mode == InputInjectionMode.Direct
            ? new MouseRecord(mouse) { Timestamp = mouse.Timestamp.Value }
            : new AnsiRecord(AnsiMouseEncoder.Encode(mouse)) { Timestamp = mouse.Timestamp.Value };
        
        _testSource.Enqueue(record);
        
        if (options.AutoProcess)
        {
            _processor.ProcessInput();
        }
    }
    
    public void ProcessQueue()
    {
        _processor.ProcessInput();
    }
    
    // ... rest of implementation
}
```

### 4. Application Integration

**Problem:** Test setup requires manual wiring of components.

**Solution:** Application factory methods that set up testability automatically.

```csharp
public static class Application
{
    /// <summary>
    /// Create an application instance.
    /// </summary>
    public static IApplication Create(ITimeProvider? timeProvider = null)
    {
        timeProvider ??= new SystemTimeProvider();
        return new ApplicationImpl(timeProvider);
    }
    
    /// <summary>
    /// Create a test application with virtual time and test input source.
    /// </summary>
    public static IApplication CreateForTesting(VirtualTimeProvider? timeProvider = null)
    {
        timeProvider ??= new VirtualTimeProvider();
        var app = new ApplicationImpl(timeProvider);
        
        // Configure for testing - will use TestInputSource
        app.Metadata["TestMode"] = true;
        
        return app;
    }
}

public class ApplicationImpl : IApplication
{
    private readonly ITimeProvider _timeProvider;
    
    public ITimeProvider GetTimeProvider() => _timeProvider;
    
    public ApplicationImpl(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void Init(string? driverName = null)
    {
        // Determine if test mode
        bool testMode = Metadata.ContainsKey("TestMode");
        
        // Create appropriate input source
        IInputSource inputSource = testMode
            ? new TestInputSource(_timeProvider)
            : CreateProductionInputSource(driverName, _timeProvider);
        
        // Create input processor
        bool enableAnsiParsing = driverName == DriverRegistry.Names.ANSI;
        var processor = new InputProcessor(inputSource, _timeProvider, enableAnsiParsing);
        
        // Create driver with processor
        Driver = CreateDriver(driverName, processor, _timeProvider);
        
        // Wire up events
        processor.KeyDown += (s, e) => Keyboard.RaiseKeyDownEvent(e);
        processor.KeyUp += (s, e) => Keyboard.RaiseKeyUpEvent(e);
        processor.MouseEvent += (s, e) => Mouse.RaiseMouseEvent(e);
    }
    
    private IInputSource CreateProductionInputSource(string? driverName, ITimeProvider timeProvider)
    {
        return driverName switch
        {
            DriverRegistry.Names.Windows => new WindowsInputSource(timeProvider),
            DriverRegistry.Names.Unix => new UnixInputSource(timeProvider),
            DriverRegistry.Names.Net => new NetInputSource(timeProvider),
            DriverRegistry.Names.ANSI => new AnsiInputSource(timeProvider),
            _ => new FakeInputSource(timeProvider)
        };
    }
}
```

### 5. Simplified Test Helpers

**Problem:** Current `InputTestHelpers` is complex with many manual steps.

**Solution:** Much simpler helpers that just wrap `IInputInjector`.

```csharp
public static class InputTestHelpers
{
    /// <summary>
    /// Inject a key and wait for it to be processed.
    /// </summary>
    public static void InjectAndProcessKey(this IApplication app, Key key)
    {
        app.InjectKey(key); // Auto-processes by default
    }
    
    /// <summary>
    /// Inject a mouse event and wait for it to be processed.
    /// </summary>
    public static void InjectAndProcessMouse(this IApplication app, Mouse mouse)
    {
        app.InjectMouse(mouse); // Auto-processes by default
    }
    
    /// <summary>
    /// Inject a click (press + release).
    /// </summary>
    public static void InjectClick(this IApplication app, Point position, MouseFlags button = MouseFlags.LeftButton)
    {
        var pressed = button switch
        {
            MouseFlags.LeftButton => MouseFlags.LeftButtonPressed,
            MouseFlags.RightButton => MouseFlags.RightButtonPressed,
            MouseFlags.MiddleButton => MouseFlags.MiddleButtonPressed,
            _ => throw new ArgumentException("Invalid button", nameof(button))
        };
        
        var released = button switch
        {
            MouseFlags.LeftButton => MouseFlags.LeftButtonReleased,
            MouseFlags.RightButton => MouseFlags.RightButtonReleased,
            MouseFlags.MiddleButton => MouseFlags.MiddleButtonReleased,
            _ => throw new ArgumentException("Invalid button", nameof(button))
        };
        
        app.InjectMouse(new() { Flags = pressed, Position = position });
        app.InjectMouse(new() { Flags = released, Position = position });
    }
    
    /// <summary>
    /// Inject a sequence of keys.
    /// </summary>
    public static void InjectKeys(this IApplication app, params Key[] keys)
    {
        app.InjectSequence(keys.Select(k => new KeyEvent(k)).ToArray());
    }
    
    /// <summary>
    /// Type a string as key events.
    /// </summary>
    public static void TypeText(this IApplication app, string text)
    {
        var keys = text.Select(c => Key.FromChar(c));
        app.InjectKeys(keys.ToArray());
    }
}
```

**Test comparison - before and after:**
```csharp
// BEFORE (current architecture)
[Fact]
public void Test_CurrentArchitecture()
{
    using var app = Application.Create();
    app.Init(DriverRegistry.Names.ANSI);
    
    // Step 1: Inject
    app.Driver.InjectKeyEvent(Key.A);
    
    // Step 2: Simulate input thread
    app.SimulateInputThread();
    
    // Step 3: Process queue
    app.Driver.GetInputProcessor().ProcessQueue();
    
    // Assert
    Assert.True(keyReceived);
}

// AFTER (redesigned architecture)
[Fact]
public void Test_NewArchitecture()
{
    using var app = Application.CreateForTesting();
    app.Init(DriverRegistry.Names.Fake);
    
    // Single step - everything handled automatically
    app.InjectKey(Key.A);
    
    // Assert
    Assert.True(keyReceived);
}
```

### 6. Integration Test Support

**Problem:** `GuiTestContext` has complex synchronization logic.

**Solution:** Leverage virtual time to eliminate most synchronization complexity.

```csharp
/// <summary>
/// Fluent test context for integration testing with full application lifecycle.
/// Redesigned to use virtual time for simpler synchronization.
/// </summary>
public class GuiTestContext : IDisposable
{
    private readonly IApplication _app;
    private readonly VirtualTimeProvider _time;
    private readonly Task? _runTask;
    private readonly CancellationTokenSource _cts;
    
    public IApplication App => _app;
    public VirtualTimeProvider Time => _time;
    
    private GuiTestContext(IRunnable runnable, int width, int height, string driverName, TimeSpan timeout)
    {
        _time = new VirtualTimeProvider();
        _app = Application.CreateForTesting(_time);
        _cts = new CancellationTokenSource(timeout);
        
        _app.Init(driverName);
        _app.Driver.SetScreenSize(width, height);
        
        // Start application on background thread
        _runTask = Task.Run(() =>
        {
            try
            {
                _app.Run(runnable);
            }
            catch (OperationCanceledException)
            {
                // Expected on timeout/stop
            }
        }, _cts.Token);
        
        // Wait for application to start (modal)
        WaitForModal(runnable, timeout);
    }
    
    public GuiTestContext InjectKey(Key key)
    {
        _app.InjectKey(key);
        
        // Advance time to let application process
        _time.Advance(TimeSpan.FromMilliseconds(10));
        
        return this;
    }
    
    public GuiTestContext InjectMouse(Mouse mouse)
    {
        _app.InjectMouse(mouse);
        
        // Advance time to let application process
        _time.Advance(TimeSpan.FromMilliseconds(10));
        
        return this;
    }
    
    public GuiTestContext LeftClick(int x, int y)
    {
        InjectMouse(new() { Flags = MouseFlags.LeftButtonPressed, Position = new(x, y) });
        InjectMouse(new() { Flags = MouseFlags.LeftButtonReleased, Position = new(x, y) });
        return this;
    }
    
    public GuiTestContext WaitForCondition(Func<bool> condition, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        var deadline = _time.Now + timeout.Value;
        
        while (!condition())
        {
            if (_time.Now >= deadline)
                throw new TimeoutException("Condition not met within timeout");
            
            // Advance time and let application process
            _time.Advance(TimeSpan.FromMilliseconds(10));
            _app.GetInputInjector().ProcessQueue();
        }
        
        return this;
    }
    
    public GuiTestContext AssertTrue(bool condition, string? message = null)
    {
        Assert.True(condition, message);
        return this;
    }
    
    public GuiTestContext AssertEqual<T>(T expected, T actual)
    {
        Assert.Equal(expected, actual);
        return this;
    }
    
    private void WaitForModal(IRunnable runnable, TimeSpan timeout)
    {
        var deadline = DateTime.Now + timeout;
        
        while (!runnable.IsModal)
        {
            if (DateTime.Now >= deadline)
                throw new TimeoutException("Runnable did not become modal");
            
            Thread.Sleep(10);
        }
    }
    
    public void Dispose()
    {
        _cts.Cancel();
        _runTask?.Wait(TimeSpan.FromSeconds(1));
        _app.Dispose();
        _cts.Dispose();
    }
}

/// <summary>
/// Fluent builder for GuiTestContext.
/// </summary>
public static class With
{
    public static GuiTestContextBuilder A<TRunnable>(int width, int height, string driverName = "Fake") 
        where TRunnable : IRunnable, new()
    {
        return new GuiTestContextBuilder(new TRunnable(), width, height, driverName);
    }
}

public class GuiTestContextBuilder
{
    private readonly IRunnable _runnable;
    private readonly int _width;
    private readonly int _height;
    private readonly string _driverName;
    private readonly List<View> _views = new();
    
    internal GuiTestContextBuilder(IRunnable runnable, int width, int height, string driverName)
    {
        _runnable = runnable;
        _width = width;
        _height = height;
        _driverName = driverName;
    }
    
    public GuiTestContextBuilder Add(View view)
    {
        _views.Add(view);
        return this;
    }
    
    public GuiTestContext Build(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        
        // Add views to runnable
        foreach (var view in _views)
        {
            if (_runnable is View container)
                container.Add(view);
        }
        
        return new GuiTestContext(_runnable, _width, _height, _driverName, timeout.Value);
    }
    
    // Fluent methods can also build implicitly
    public GuiTestContext InjectKey(Key key) => Build().InjectKey(key);
    public GuiTestContext LeftClick(int x, int y) => Build().LeftClick(x, y);
}
```

**Integration test comparison:**
```csharp
// BEFORE (complex synchronization)
[Fact]
public void IntegrationTest_Current()
{
    using var context = With.A<Window>(40, 10, TestDriver.ANSI)
        .Add(button);
    
    // Complex WaitIteration synchronization
    context.WaitIteration(app => /* setup */);
    context.LeftClick(5, 5);
    context.WaitIteration(); // Wait for processing
    context.AssertEqual(1, clickCount);
}

// AFTER (virtual time, simpler)
[Fact]
public void IntegrationTest_New()
{
    using var context = With.A<Window>(40, 10, "Fake")
        .Add(button)
        .LeftClick(5, 5) // Automatically advances virtual time
        .AssertEqual(1, clickCount);
}
```

## Migration Path

### Phase 1: Add New Infrastructure (Non-Breaking)
1. Add `ITimeProvider`, `SystemTimeProvider`, `VirtualTimeProvider`
2. Add `InputRecord` types, `IInputSource` interface
3. Add new `InputProcessor` implementation alongside old `InputProcessorImpl`
4. Add `IInputInjector` and `InputInjector`
5. Update tests to use new APIs (opt-in)

**Result:** New and old systems coexist. Tests can migrate incrementally.

### Phase 2: Update Core Components
1. Add `ITimeProvider` parameter to `MouseButtonClickTracker`, `MouseInterpreter`
2. Add `ITimeProvider` parameter to `AnsiResponseParser`
3. Update `Application.Create()` to accept `ITimeProvider`
4. Add `Application.CreateForTesting()` factory method

**Result:** Core components support virtual time. Old tests still work.

### Phase 3: Migrate Tests
1. Migrate unit tests from `InputTestHelpers` to `IInputInjector`
2. Migrate integration tests from old `GuiTestContext` to new
3. Update test documentation

**Result:** All tests using new architecture. Old APIs marked `[Obsolete]`.

### Phase 4: Remove Old Infrastructure
1. Remove `ITestableInput` interface
2. Remove old `InputTestHelpers`
3. Remove old `GuiTestContext`
4. Simplify `IInputProcessor` (no need for `InjectKeyDownEvent` etc.)

**Result:** Clean architecture with only new system.

## Benefits Summary

### For Test Authors

**Before:**
```csharp
// 3-step dance, easy to forget steps
app.Driver.InjectKeyEvent(Key.A);
app.SimulateInputThread();
app.Driver.GetInputProcessor().ProcessQueue();
```

**After:**
```csharp
// Single call, automatic processing
app.InjectKey(Key.A);
```

**Before:**
```csharp
// Real delays (60ms per escape key)
processor.ProcessQueue();
Thread.Sleep(60);
processor.ProcessQueue();
```

**After:**
```csharp
// Virtual time, instant
time.Advance(TimeSpan.FromMilliseconds(60));
app.InjectKey(Key.Esc);
```

**Before:**
```csharp
// Complex timestamp control
app.InjectMouseEventDirectly(new() { 
    Timestamp = baseTime.AddMilliseconds(600) 
});
```

**After:**
```csharp
// Natural time advancement
time.Advance(TimeSpan.FromMilliseconds(600));
app.InjectMouse(new() { Position = new(5, 5) });
```

### For Maintainability

1. **Cleaner architecture** - Single input path, clear responsibilities
2. **Fewer interfaces** - No more `IInput`/`ITestableInput` split
3. **Simpler tests** - Less boilerplate, more readable
4. **Deterministic** - No race conditions, no real delays
5. **Flexible** - Easy to test both direct events and full pipeline

### For External Applications (Low Priority)

The new architecture doesn't directly solve external app injection, but it doesn't preclude it either:

- `IInputInjector` could theoretically be exposed via remote API
- Virtual time could be controlled externally
- `Application.CreateForTesting()` could accept external input source

However, this remains a low-priority use case that we won't optimize for in the initial design.

## Open Questions

1. **Driver-specific encoders** - Should `AnsiKeyboardEncoder`/`AnsiMouseEncoder` be driver-owned or shared?
   - **Recommendation:** Keep as shared utilities. They're stateless and used by multiple components.

2. **Event timestamps** - Should `Key` gain a `Timestamp` property (like `Mouse` has)?
   - **Recommendation:** Yes, for consistency and future keyboard timing features (repeat, debouncing).

3. **Backward compatibility** - How long do we keep old APIs with `[Obsolete]`?
   - **Recommendation:** At least one major version (v3.x keeps old APIs, v4.x removes them).

4. **Performance** - Does virtual time add overhead for production?
   - **Recommendation:** Minimal. `SystemTimeProvider.Now` is just `DateTime.Now`. Profile if concerned.

5. **Thread safety** - Is `VirtualTimeProvider.Advance()` thread-safe?
   - **Recommendation:** No, assumes single-threaded test execution. Document this clearly.

## Success Criteria

The redesign is successful if:

1. ? Unit tests are **simpler** - fewer steps, more readable
2. ? Tests are **faster** - no real delays (60ms ? 0ms for escape sequences)
3. ? Tests are **deterministic** - no race conditions, consistent timing
4. ? Pipeline testing is still possible - can verify ANSI encoding when needed
5. ? Integration tests are simpler - less synchronization complexity
6. ? Migration is gradual - old and new APIs can coexist
7. ? Documentation is clear - easy for new contributors to understand
8. ? No breaking changes to production code - drivers still work the same

## Next Steps

1. **Prototype** - Implement `ITimeProvider` and `VirtualTimeProvider` in a branch
2. **Validate** - Port 2-3 tests to new architecture, verify benefits
3. **Review** - Get feedback from team on design decisions
4. **Implement** - Follow migration path (Phase 1 ? Phase 4)
5. **Document** - Update testing guides with new patterns

---

**Document Status:** Draft specification for discussion  
**Last Updated:** 2025-01-14  
**Authors:** @copilot  
**Reviewers:** (pending)
