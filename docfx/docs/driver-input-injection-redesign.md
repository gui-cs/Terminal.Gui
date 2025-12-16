# Driver Input Injection - Redesign Specification

> **🎯 AI Agent Prompt**: This document is self-contained specification for redesigning Terminal.Gui's input injection architecture. It includes context, rationale, design decisions, code samples, and implementation guidance needed to execute the redesign without additional research.

## Context for AI Agents

### What You're Working On

You are redesigning the **input injection system** used for testing Terminal.Gui applications. The current system (documented in [driver-input-injection.md](driver-input-injection.md)) has several pain points:

1. **3-step injection process** - Tests must manually inject → simulate input thread → process queue
2. **Real-time delays** - Escape sequence processing requires 60ms `Thread.Sleep` calls
3. **Non-deterministic timing** - Uses `DateTime.Now` throughout, causing flaky tests
4. **Complex synchronization** - Integration tests need elaborate coordination between threads
5. **Dual injection paths** - Direct injection (preserves timestamps) vs Pipeline injection (tests encoding)

### Your Goal

Create a new architecture that:
- ✅ **Single-call injection** - `app.InjectKey(Key.A)` does everything
- ✅ **Virtual time** - Tests control time explicitly, no real delays
- ✅ **Deterministic** - Same input sequence produces same results every time
- ✅ **Preserves ANSI testing** - Can still test full encoding/parsing pipeline when needed
- ✅ **Simpler for test authors** - Cleaner API, fewer concepts to understand

### Key Constraints

1. **Follow Terminal.Gui coding standards** (see CONTRIBUTING.md):
   - **NO `var`** except for built-in types (`int`, `string`, `bool`, etc.)
   - **USE target-typed `new ()`** when type is already declared
   - **USE collection initializers** `[]` syntax when possible
   - **Explicit types everywhere** for clarity
   
2. **No backward compatibility required** - This is a clean redesign
3. **All drivers support testing** - No fake/mock drivers needed
4. **ANSI driver is preferred for tests** - Cross-platform, full encoding/parsing

## Current State (Problems to Solve)

### Problem 1: Three-Step Injection Dance

**Current pattern** (from `InputTestHelpers.cs`):
```csharp
// Step 1: Inject into test queue
app.Driver.InjectKeyEvent(Key.A);

// Step 2: Move from test queue to input buffer (simulates input thread)
app.SimulateInputThread();

// Step 3: Process input buffer and raise events
app.Driver.GetInputProcessor().ProcessQueue();
```

**Why this is bad:**
- Easy to forget steps
- Exposes internal implementation details
- Different from production flow
- Verbose and error-prone

### Problem 2: Real-Time Delays for Escape Sequences

**Current pattern** (from `InputTestHelpers.ProcessQueueWithEscapeHandling`):
```csharp
processor.ProcessQueue();  // First attempt
Thread.Sleep(60);          // Wait for 50ms escape timeout
processor.ProcessQueue();  // Process again to release held Esc key
```

**Why this is bad:**
- Tests take longer (60ms × number of Esc keys)
- Uses real time, can't be controlled
- Brittle - timing sensitive

### Problem 3: Non-Deterministic Timestamps

**Current code** (from `MouseButtonClickTracker.cs`):
```csharp
public void UpdateState (Mouse mouse, out int? numClicks)
{
    DateTime currentTime = mouse.Timestamp ?? DateTime.Now;  // ⚠️ Can't control this!
    TimeSpan elapsed = currentTime - At;
    
    if (elapsed >= _repeatClickThreshold || !isSamePosition)
    {
        _consecutiveClicks = 0;
    }
    // ...
}
```

**Why this is bad:**
- Can't test timing-dependent behavior reliably
- Different results on fast vs slow machines
- Race conditions in tests

### Problem 4: Dual Injection Paths

**Current approach:**
```csharp
// Path A: Direct injection (preserves timestamps, bypasses encoding)
app.InjectMouseEventDirectly(new Mouse { 
    Timestamp = baseTime.AddMilliseconds(600) 
});

// Path B: Pipeline injection (tests encoding, loses timestamps)
app.Driver.InjectMouseEvent(new Mouse { Position = new (5, 5) });
app.SimulateInputThread();
app.Driver.GetInputProcessor().ProcessQueue();
```

**Why this is bad:**
- Two different APIs for same goal
- Can't test full pipeline with precise timing
- Confusing for test authors

## Available Drivers (Important Context)

Terminal.Gui has **four production drivers** (no fake/mock drivers):

| Driver | Constant | Use Case |
|--------|----------|----------|
| Windows | `DriverRegistry.Names.WINDOWS` | Windows Console API |
| Unix | `DriverRegistry.Names.UNIX` | Unix/Linux/macOS terminal |
| Dotnet | `DriverRegistry.Names.DOTNET` | .NET System.Console cross-platform |
| ANSI | `DriverRegistry.Names.ANSI` | **Pure ANSI escape sequences** |

**ANSI driver is preferred for tests** because:
- ✅ Cross-platform (works on all OSes)
- ✅ Full ANSI encoding/parsing pipeline to test
- ✅ Lightweight and fast
- ✅ Test infrastructure (`FakeDriverBase.CreateFakeDriver()`) uses it

**There is NO separate "FakeDriver" or "TestDriver"** - just use ANSI driver for tests.

## Goals

1. **Unified time control** - Single mechanism for controlling all time-dependent behavior (timestamps, delays, timers)
2. **Simplified injection** - Single method call that handles the full injection pipeline
3. **Deterministic testing** - Eliminate real-time delays and race conditions
4. **Preserve ANSI testing** - Still support full encoding/decoding pipeline testing when needed
5. **Better API** - Cleaner, more intuitive interface for test authors

## Non-Goals

- External application injection as a primary use case
- Backward compatibility with current `ITestableInput`/`InputTestHelpers` (clean break)

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
    Task Delay (TimeSpan duration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a timer. In tests, this can be controlled.
    /// </summary>
    ITimer CreateTimer (TimeSpan interval, Action callback);
}

/// <summary>
/// Real time provider using DateTime.Now and Task.Delay.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    /// <inheritdoc/>
    public DateTime Now => DateTime.Now;
    
    /// <inheritdoc/>
    public Task Delay (TimeSpan duration, CancellationToken ct) 
        => Task.Delay (duration, ct);
    
    /// <inheritdoc/>
    public ITimer CreateTimer (TimeSpan interval, Action callback)
        => new SystemTimer (interval, callback);
}

/// <summary>
/// Virtual time provider for testing - all time is controlled.
/// </summary>
public class VirtualTimeProvider : ITimeProvider
{
    private DateTime _currentTime = new (2025, 1, 1, 0, 0, 0);
    private readonly List<VirtualTimer> _timers = [];
    private readonly List<VirtualDelay> _delays = [];
    
    /// <inheritdoc/>
    public DateTime Now => _currentTime;
    
    /// <summary>
    /// Advance virtual time by the specified duration.
    /// This triggers any timers/delays that should fire.
    /// </summary>
    public void Advance (TimeSpan duration)
    {
        _currentTime += duration;
        
        // Fire any timers that should trigger
        foreach (VirtualTimer timer in _timers.Where (t => t.NextTrigger <= _currentTime))
        {
            timer.Trigger ();
        }
        
        // Complete any delays that should finish
        foreach (VirtualDelay delay in _delays.Where (d => d.CompletionTime <= _currentTime))
        {
            delay.Complete ();
        }
    }
    
    /// <summary>
    /// Set virtual time to a specific value.
    /// </summary>
    public void SetTime (DateTime time)
    {
        _currentTime = time;
    }
    
    /// <inheritdoc/>
    public Task Delay (TimeSpan duration, CancellationToken ct)
    {
        VirtualDelay delay = new (_currentTime + duration, ct);
        _delays.Add (delay);

        return delay.Task;
    }
    
    /// <inheritdoc/>
    public ITimer CreateTimer (TimeSpan interval, Action callback)
    {
        VirtualTimer timer = new (_currentTime, interval, callback);
        _timers.Add (timer);

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
    
    public MouseButtonClickTracker (ITimeProvider timeProvider, ...)
    {
        _timeProvider = timeProvider;
    }
    
    public void UpdateState (Mouse mouse, out int? numClicks)
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
    
    public bool IsEscapeSequenceStale ()
    {
        return State == AnsiResponseParserState.ExpectingEscapeSequence 
            && _timeProvider.Now - StateChangedAt > EscapeTimeout;
    }
}
```

**Test usage:**
```csharp
[Fact]
public void DoubleClick_WithinThreshold_DetectsDoubleClick ()
{
    // CoPilot - Create app with virtual time
    VirtualTimeProvider timeProvider = new ();
    timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));
    
    using IApplication app = Application.Create (timeProvider);
    app.Init (DriverRegistry.Names.ANSI);
    
    // Inject events with controlled timing
    app.InjectMouseEvent (new () { 
        Flags = MouseFlags.LeftButtonPressed,
        Position = new (5, 5)
    });
    
    timeProvider.Advance (TimeSpan.FromMilliseconds (50));
    
    app.InjectMouseEvent (new () { 
        Flags = MouseFlags.LeftButtonReleased,
        Position = new (5, 5)
    });
    
    // Second click 300ms later (within 500ms threshold)
    timeProvider.Advance (TimeSpan.FromMilliseconds (300));
    
    app.InjectMouseEvent (new () { 
        Flags = MouseFlags.LeftButtonPressed,
        Position = new (5, 5)
    });
    
    timeProvider.Advance (TimeSpan.FromMilliseconds (50));
    
    app.InjectMouseEvent (new () { 
        Flags = MouseFlags.LeftButtonReleased,
        Position = new (5, 5)
    });
    
    // Assert double-click detected
    Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked));
}
```

### 2. Unified Input Injection API

**Problem:** Current system requires 3 steps (inject → simulate thread → process queue) with different paths for testing full pipeline vs. testing with timestamps.

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
    /// Automatic mode - uses Direct for simple test scenarios, Pipeline for full driver testing.
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
    void InjectKey (Key key, InputInjectionOptions? options = null);
    
    /// <summary>
    /// Inject a mouse event. Handles encoding, queueing, and processing automatically.
    /// </summary>
    void InjectMouse (Mouse mouse, InputInjectionOptions? options = null);
    
    /// <summary>
    /// Inject a sequence of input events with delays between them.
    /// </summary>
    void InjectSequence (IEnumerable<InputEvent> events, InputInjectionOptions? options = null);
    
    /// <summary>
    /// Force processing of the input queue (usually automatic).
    /// </summary>
    void ProcessQueue ();
}

/// <summary>
/// Base class for input events in sequences.
/// </summary>
public abstract record InputEvent
{
    /// <summary>
    /// Optional delay before processing this event.
    /// </summary>
    public TimeSpan? Delay { get; init; }
}

/// <summary>
/// Keyboard event in a sequence.
/// </summary>
public record KeyEvent (Key Key) : InputEvent;

/// <summary>
/// Mouse event in a sequence.
/// </summary>
public record MouseEvent (Mouse Mouse) : InputEvent;
```

**Implementation:**
```csharp
/// <summary>
/// Implementation of IInputInjector for testing.
/// </summary>
public class InputInjector : IInputInjector
{
    private readonly IApplication _app;
    private readonly IDriver _driver;
    private readonly IInputProcessor _processor;
    private readonly ITimeProvider _timeProvider;
    
    public InputInjector (IApplication app, ITimeProvider timeProvider)
    {
        _app = app;
        _driver = app.Driver ?? throw new InvalidOperationException ("Driver not initialized");
        _processor = _driver.GetInputProcessor ();
        _timeProvider = timeProvider;
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
            // Pipeline injection - encode → queue → parse → event
            _processor.InjectKeyDownEvent (key);
        }
        
        if (options.AutoProcess)
        {
            ProcessQueue ();
        }
    }
    
    /// <inheritdoc/>
    public void InjectMouse (Mouse mouse, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };
        InputInjectionMode mode = ResolveMode (options.Mode);
        
        // Set timestamp if not provided
        mouse.Timestamp ??= (options.TimeProvider ?? _timeProvider).Now;
        
        if (mode == InputInjectionMode.Direct)
        {
            // Direct injection - bypass encoding, preserve timestamp
            _processor.RaiseMouseEventParsed (mouse);
            _processor.RaiseSyntheticMouseEvent (mouse);
        }
        else // Pipeline
        {
            // Pipeline injection - encode → queue → parse → event
            // Note: Encoding may lose timestamp information
            _processor.InjectMouseEvent (_app, mouse);
        }
        
        if (options.AutoProcess)
        {
            ProcessQueue ();
        }
    }
    
    /// <inheritdoc/>
    public void InjectSequence (IEnumerable<InputEvent> events, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { Time Provider = _timeProvider };
        
        foreach (InputEvent evt in events)
        {
            // Advance time if delay specified
            if (evt.Delay.HasValue && options.TimeProvider is VirtualTimeProvider vtp)
            {
                vtp.Advance (evt.Delay.Value);
            }
            
            switch (evt)
            {
                case KeyEvent ke:
                    InjectKey (ke.Key, options with { AutoProcess = false });

                    break;
                case MouseEvent me:
                    InjectMouse (me.Mouse, options with { AutoProcess = false });

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
```

**Extension methods for convenience:**
```csharp
/// <summary>
/// Extension methods for input injection.
/// </summary>
public static class InputInjectionExtensions
{
    private static readonly ConditionalWeakTable<IApplication, IInputInjector> _injectorCache = new ();
    
    /// <summary>
    /// Get or create the input injector for this application.
    /// </summary>
    public static IInputInjector GetInputInjector (this IApplication app)
    {
        // Cache injector per application instance
        return _injectorCache.GetValue (app, _ => 
        {
            ITimeProvider timeProvider = app.GetTimeProvider ();

            return new InputInjector (app, timeProvider);
        });
    }
    
    /// <summary>
    /// Inject a key event (convenience method).
    /// </summary>
    public static void InjectKey (this IApplication app, Key key)
    {
        app.GetInputInjector ().InjectKey (key);
    }
    
    /// <summary>
    /// Inject a mouse event (convenience method).
    /// </summary>
    public static void InjectMouse (this IApplication app, Mouse mouse)
    {
        app.GetInputInjector ().InjectMouse (mouse);
    }
    
    /// <summary>
    /// Inject a sequence of events (convenience method).
    /// </summary>
    public static void InjectSequence (this IApplication app, params InputEvent [] events)
    {
        app.GetInputInjector ().InjectSequence (events);
    }
}
```

**Test usage - simplified:**
```csharp
[Fact]
public void Button_ClickWithMouse_RaisesAccepting ()
{
    // CoPilot - Create app with virtual time
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create (time);
    app.Init (DriverRegistry.Names.ANSI); // Use ANSI driver for testing
    
    Button button = new () { Text = "Click Me" };
    bool acceptingCalled = false;
    button.Accepting += (s, e) => acceptingCalled = true;
    
    // Single call - injection + processing handled automatically
    app.InjectMouse (new () {
        Flags = MouseFlags.LeftButtonPressed,
        Position = new (5, 5)
    });
    
    app.InjectMouse (new () {
        Flags = MouseFlags.LeftButtonReleased,
        Position = new (5, 5)
    });
    
    Assert.True (acceptingCalled);
}

[Fact]
public void TextField_TypeText_UpdatesContent ()
{
    // CoPilot
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create (time);
    app.Init (DriverRegistry.Names.ANSI);
    
    TextField textField = new ();
    
    // Inject sequence with automatic processing
    app.InjectSequence (
        new KeyEvent (Key.H),
        new KeyEvent (Key.E),
        new KeyEvent (Key.L),
        new KeyEvent (Key.L),
        new KeyEvent (Key.O)
    );
    
    Assert.Equal ("HELLO", textField.Text);
}

[Fact]
public void DoubleClick_TestPipelineEncoding ()
{
    // CoPilot - Test ANSI encoding/decoding
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create (time);
    app.Init (DriverRegistry.Names.ANSI); // ANSI driver for pipeline testing
    
    // Force pipeline mode to test ANSI encoding/decoding
    InputInjectionOptions options = new () 
    { 
        Mode = InputInjectionMode.Pipeline,
        TimeProvider = time 
    };
    
    // Events go through full ANSI encoding → parsing → event pipeline
    app.InjectMouse (new () { 
        Flags = MouseFlags.LeftButtonPressed,
        Position = new (5, 5)
    }, options);
    
    time.Advance (TimeSpan.FromMilliseconds (50));
    
    app.InjectMouse (new () { 
        Flags = MouseFlags.LeftButtonReleased,
        Position = new (5, 5)
    }, options);
    
    // Verify ANSI encoding worked correctly
    Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonClicked));
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
    IEnumerable<InputRecord> ReadAvailable ();
    
    /// <summary>
    /// Start background input reading (for production implementations).
    /// Test implementations typically don't need this.
    /// </summary>
    void Start (CancellationToken cancellationToken);
    
    /// <summary>
    /// Stop background input reading.
    /// </summary>
    void Stop ();
}

/// <summary>
/// Platform-independent input record.
/// </summary>
public abstract record InputRecord
{
    /// <summary>
    /// When this input occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Keyboard input record.
/// </summary>
public record KeyboardRecord (Key Key) : InputRecord;

/// <summary>
/// Mouse input record (raw, before click synthesis).
/// </summary>
public record MouseRecord (Mouse Mouse) : InputRecord;

/// <summary>
/// ANSI sequence record (for ANSI driver testing).
/// </summary>
public record AnsiRecord (string Sequence) : InputRecord;

/// <summary>
/// Test input source - provides pre-programmed input.
/// </summary>
public class TestInputSource : IInputSource
{
    private readonly Queue<InputRecord> _inputQueue = new ();
    
    /// <inheritdoc/>
    public ITimeProvider TimeProvider { get; }
    
    public TestInputSource (ITimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
    }
    
    /// <inheritdoc/>
    public bool IsAvailable => _inputQueue.Count > 0;
    
    /// <inheritdoc/>
    public IEnumerable<InputRecord> ReadAvailable ()
    {
        while (_inputQueue.Count > 0)
        {
            yield return _inputQueue.Dequeue ();
        }
    }
    
    /// <summary>
    /// Add input to the queue. Called by InputInjector.
    /// </summary>
    public void Enqueue (InputRecord record)
    {
        // Set timestamp if not already set
        if (record.Timestamp == default)
        {
            record = record with { Timestamp = TimeProvider.Now };
        }

        _inputQueue.Enqueue (record);
    }
    
    /// <inheritdoc/>
    public void Start (CancellationToken cancellationToken) { /* No-op for test */ }

    /// <inheritdoc/>
    public void Stop () { /* No-op for test */ }
}

/// <summary>
/// Console input source - reads from actual console (production).
/// </summary>
public abstract class ConsoleInputSource : IInputSource
{
    private Task? _readTask;
    private CancellationTokenSource? _cancellationTokenSource;
    protected readonly ConcurrentQueue<InputRecord> InputBuffer = new ();
    
    /// <inheritdoc/>
    public ITimeProvider TimeProvider { get; }
    
    protected ConsoleInputSource (ITimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
    }
    
    /// <inheritdoc/>
    public bool IsAvailable => !InputBuffer.IsEmpty;
    
    /// <inheritdoc/>
    public IEnumerable<InputRecord> ReadAvailable ()
    {
        while (InputBuffer.TryDequeue (out InputRecord? record))
        {
            yield return record;
        }
    }
    
    /// <inheritdoc/>
    public void Start (CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
        _readTask = Task.Run (() => ReadLoop (_cancellationTokenSource.Token), cancellationToken);
    }
    
    /// <inheritdoc/>
    public void Stop ()
    {
        _cancellationTokenSource?.Cancel ();
        _readTask?.Wait (TimeSpan.FromSeconds (1));
    }
    
    /// <summary>
    /// Platform-specific implementation of reading input.
    /// Runs on background thread, enqueues to InputBuffer.
    /// </summary>
    protected abstract Task ReadLoop (CancellationToken cancellationToken);
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
    void ProcessInput ();
    
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

/// <summary>
/// Implementation of IInputProcessor.
/// </summary>
public class InputProcessor : IInputProcessor
{
    private readonly MouseInterpreter _mouseInterpreter;
    private readonly IAnsiResponseParser? _ansiParser;
    
    /// <inheritdoc/>
    public IInputSource InputSource { get; }

    /// <inheritdoc/>
    public ITimeProvider TimeProvider { get; }

    /// <inheritdoc/>
    public IAnsiResponseParser? AnsiParser => _ansiParser;
    
    /// <inheritdoc/>
    public event EventHandler<Key>? KeyDown;

    /// <inheritdoc/>
    public event EventHandler<Key>? KeyUp;

    /// <inheritdoc/>
    public event EventHandler<Mouse>? MouseEvent;
    
    public InputProcessor (IInputSource inputSource, ITimeProvider timeProvider, bool enableAnsiParsing = false)
    {
        InputSource = inputSource;
        TimeProvider = timeProvider;
        _mouseInterpreter = new MouseInterpreter (timeProvider);
        
        if (enableAnsiParsing)
        {
            _ansiParser = new AnsiResponseParser<char> (timeProvider);
            _ansiParser.Mouse += (s, e) => RaiseMouse (e);
            _ansiParser.Keyboard += (s, e) => RaiseKeyboard (e);
        }
    }
    
    /// <inheritdoc/>
    public void ProcessInput ()
    {
        // Read all available input
        foreach (InputRecord record in InputSource.ReadAvailable ())
        {
            ProcessRecord (record);
        }
        
        // Check for stale escape sequences (if using ANSI parser)
        if (_ansiParser != null && _ansiParser.IsEscapeSequenceStale ())
        {
            foreach (char released in _ansiParser.Release ())
            {
                ProcessRecord (new AnsiRecord (released.ToString ()) { Timestamp = TimeProvider.Now });
            }
        }
    }
    
    private void ProcessRecord (InputRecord record)
    {
        switch (record)
        {
            case KeyboardRecord kr:
                RaiseKeyboard (kr.Key);

                break;
                
            case MouseRecord mr:
                RaiseMouse (mr.Mouse);

                break;
                
            case AnsiRecord ar:
                // Feed to ANSI parser
                _ansiParser?.ProcessInput (ar.Sequence);

                break;
        }
    }
    
    private void RaiseKeyboard (Key key)
    {
        KeyDown?.Invoke (this, key);
        KeyUp?.Invoke (this, key);
    }
    
    private void RaiseMouse (Mouse mouse)
    {
        // Process through MouseInterpreter to generate click events
        foreach (Mouse processedMouse in _mouseInterpreter.Process (mouse))
        {
            MouseEvent?.Invoke (this, processedMouse);
        }
    }
}
```

**InputInjector integration with InputSource:**
```csharp
// InputInjector works directly with TestInputSource
public class InputInjector : IInputInjector
{
    private readonly IInputProcessor _processor;
    private readonly TestInputSource? _testSource;
    
    public InputInjector (IInputProcessor processor)
    {
        _processor = processor;
        _testSource = processor.InputSource as TestInputSource;
        
        if (_testSource == null)
        {
            throw new InvalidOperationException ("InputInjector requires TestInputSource");
        }
    }
    
    public void InjectKey (Key key, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions ();
        InputInjectionMode mode = ResolveMode (options.Mode);
        
        InputRecord record = mode == InputInjectionMode.Direct
            ? new KeyboardRecord (key) { Timestamp = _processor.TimeProvider.Now }
            : new AnsiRecord (AnsiKeyboardEncoder.Encode (key)) { Timestamp = _processor.TimeProvider.Now };
        
        _testSource.Enqueue (record);
        
        if (options.AutoProcess)
        {
            _processor.ProcessInput ();
        }
    }
    
    public void InjectMouse (Mouse mouse, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions ();
        InputInjectionMode mode = ResolveMode (options.Mode);
        
        mouse.Timestamp ??= _processor.TimeProvider.Now;
        
        InputRecord record = mode == InputInjectionMode.Direct
            ? new MouseRecord (mouse) { Timestamp = mouse.Timestamp.Value }
            : new AnsiRecord (AnsiMouseEncoder.Encode (mouse)) { Timestamp = mouse.Timestamp.Value };
        
        _testSource.Enqueue (record);
        
        if (options.AutoProcess)
        {
            _processor.ProcessInput ();
        }
    }
    
    public void ProcessQueue ()
    {
        _processor.ProcessInput ();
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

```

## Migration Path

### Phase 1: Add New Infrastructure (Non-Breaking)
1. ✅ Add `ITimeProvider`, `SystemTimeProvider`, `VirtualTimeProvider` interfaces and implementations
2. ✅ Add `InputRecord` types (`KeyboardRecord`, `MouseRecord`, `AnsiRecord`)
3. ✅ Add `IInputSource` interface with `TestInputSource` and `ConsoleInputSource` implementations
4. ✅ Add new `InputProcessor` implementation alongside existing code
5. ✅ Add `IInputInjector` interface and `InputInjector` implementation
6. ✅ Add extension methods in `InputInjectionExtensions`

**Result:** New and old systems coexist. Tests can migrate incrementally.

### Phase 2: Update Core Components
1. ✅ Add `ITimeProvider` parameter to `MouseButtonClickTracker` constructor
2. ✅ Add `ITimeProvider` parameter to `MouseInterpreter` constructor
3. ✅ Add `ITimeProvider` parameter to `AnsiResponseParser` constructor
4. ✅ Update `Application.Create()` to accept `ITimeProvider` parameter
5. ✅ Add `Application.CreateForTesting()` factory method
6. ✅ Update all uses of `DateTime.Now` to use `ITimeProvider.Now`

**Result:** Core components support virtual time. Old tests still work.

### Phase 3: Migrate Tests
1. ✅ Create new `InputTestHelpers` class with simplified methods
2. ✅ Migrate unit tests from old `InputTestHelpers` to new `IInputInjector` API
3. ✅ Update integration tests to use `VirtualTimeProvider`
4. ✅ Update test documentation and examples
5. ✅ Mark old APIs with `[Obsolete]` attribute

**Note: Disabled tests** - Tests marked with `Skip = "Broken in #4474"` were disabled as part of other refactoring in this massive PR. All tests currently pass; but only because these were disabled. Refactor and re-enable if they are relevant to this redesign.

**Result:** All tests using new architecture. Old APIs marked for removal.

### Phase 4: Remove Old Infrastructure
1. ✅ Remove `ITestableInput` interface
2. ✅ Remove old `InputTestHelpers` methods (SimulateInputThread, ProcessQueueWithEscapeHandling)
3. ✅ Remove old injection methods from `IInputProcessor` (`InjectKeyDownEvent`, etc.)
4. ✅ Clean up any remaining references to obsolete APIs

**Result:** Clean architecture with only new system.

## Implementation Checklist

> **🎯 For AI Agents**: Use this checklist to implement the redesign systematically.

### Step 1: Create Core Time Abstraction
- [ ] Create file: `Terminal.Gui/Time/ITimeProvider.cs`
  - [ ] Add `ITimeProvider` interface
  - [ ] Add `SystemTimeProvider` class
  - [ ] Add `VirtualTimeProvider` class
  - [ ] Add `VirtualTimer` helper class
  - [ ] Add `VirtualDelay` helper class
- [ ] Follow Terminal.Gui coding standards:
  - [ ] NO `var` except for built-in types
  - [ ] USE target-typed `new ()`
  - [ ] USE collection initializers `[]`
  - [ ] Add XML docs to all public members

### Step 2: Create Input Source Abstraction
- [ ] Create file: `Terminal.Gui/Drivers/Input/IInputSource.cs`
  - [ ] Add `IInputSource` interface
  - [ ] Add `InputRecord` base record
  - [ ] Add `KeyboardRecord` record
  - [ ] Add `MouseRecord` record
  - [ ] Add `AnsiRecord` record
- [ ] Create file: `Terminal.Gui/Drivers/Input/TestInputSource.cs`
  - [ ] Implement `TestInputSource` class
  - [ ] Add `Enqueue` method for test injection
- [ ] Create file: `Terminal.Gui/Drivers/Input/ConsoleInputSource.cs`
  - [ ] Add abstract `ConsoleInputSource` base class
  - [ ] Add background thread support
  - [ ] Add `ReadLoop` abstract method

### Step 3: Create Input Injector
- [ ] Create file: `Terminal.Gui/Testing/IInputInjector.cs`
  - [ ] Add `IInputInjector` interface
  - [ ] Add `InputInjectionMode` enum
  - [ ] Add `InputInjectionOptions` class
  - [ ] Add `InputEvent` base record
  - [ ] Add `KeyEvent` record
  - [ ] Add `MouseEvent` record
- [ ] Create file: `Terminal.Gui/Testing/InputInjector.cs`
  - [ ] Implement `InputInjector` class
  - [ ] Implement `InjectKey` method
  - [ ] Implement `InjectMouse` method
  - [ ] Implement `InjectSequence` method
  - [ ] Implement `ProcessQueue` method
  - [ ] Implement `ResolveMode` method
- [ ] Create file: `Terminal.Gui/Testing/InputInjectionExtensions.cs`
  - [ ] Add extension methods for `IApplication`
  - [ ] Use `ConditionalWeakTable` for caching

### Step 4: Update Core Components
- [ ] Update `Terminal.Gui/Drivers/MouseButtonClickTracker.cs`
  - [ ] Add `ITimeProvider` constructor parameter
  - [ ] Replace `DateTime.Now` with `_timeProvider.Now`
- [ ] Update `Terminal.Gui/Drivers/MouseInterpreter.cs`
  - [ ] Add `ITimeProvider` constructor parameter
  - [ ] Pass to `MouseButtonClickTracker`
- [ ] Update `Terminal.Gui/Drivers/ANSIDriver/AnsiResponseParser.cs`
  - [ ] Add `ITimeProvider` constructor parameter
  - [ ] Replace `DateTime.Now` with `_timeProvider.Now` for timeout checks

### Step 5: Update Application Layer
- [ ] Update `Terminal.Gui/Application/Application.cs`
  - [ ] Add `ITimeProvider` parameter to `Create` method (optional, defaults to `SystemTimeProvider`)
  - [ ] Add `CreateForTesting` factory method (returns app with `VirtualTimeProvider` and `testMode: true`)
- [ ] Update `Terminal.Gui/Application/ApplicationImpl.cs`
  - [ ] Add `ITimeProvider` and `bool testMode` constructor parameters
  - [ ] Add `GetTimeProvider()` method
  - [ ] Update `Init()` to create `TestInputSource` when `testMode == true`
  - [ ] Update `Init()` to pass `ITimeProvider` to all components

### Step 6: Create New Test Helpers
- [ ] Create file: `Tests/UnitTests/InputTestHelpers.cs` (new version)
  - [ ] Add simplified `InjectAndProcessKey` method
  - [ ] Add simplified `InjectAndProcessMouse` method
  - [ ] Add `InjectClick` helper method
  - [ ] Add `InjectKeys` helper method
  - [ ] Add `TypeText` helper method
  - [ ] Mark all methods with `// CoPilot` comment

### Step 7: Write Validation Tests
- [ ] Create file: `Tests/UnitTestsParallelizable/Testing/VirtualTimeProviderTests.cs`
  - [ ] Test `Advance` method
  - [ ] Test `SetTime` method
  - [ ] Test timer triggering
  - [ ] Test delay completion
- [ ] Create file: `Tests/UnitTestsParallelizable/Testing/InputInjectorTests.cs`
  - [ ] Test Direct mode keyboard injection
  - [ ] Test Direct mode mouse injection
  - [ ] Test Pipeline mode ANSI encoding
  - [ ] Test sequence injection with delays
  - [ ] Test auto-processing vs manual processing

### Step 8: Migrate Example Tests
- [ ] Pick 3-5 existing tests from `Tests/UnitTestsParallelizable/Application/Mouse/ApplicationMouseTests.cs`
  - [ ] Rewrite using `Application.CreateForTesting()`
  - [ ] Replace manual 3-step injection with `app.InjectMouse()`
  - [ ] Use `VirtualTimeProvider.Advance()` instead of real delays
  - [ ] Verify tests still pass
  - [ ] Compare lines of code (should be ~50% reduction)

### Step 9: Update Documentation
- [ ] Update `docfx/docs/testing.md`
  - [ ] Add section on virtual time
  - [ ] Add section on input injection
  - [ ] Include code examples
- [ ] Add inline code comments to all new classes explaining usage

### Step 10: Phase Out Old Code (Later)
- [ ] Mark old `ITestableInput` with `[Obsolete]`
- [ ] Mark old `InputTestHelpers.SimulateInputThread` with `[Obsolete]`
- [ ] Mark old `InjectKeyDownEvent` methods with `[Obsolete]`
- [ ] Schedule removal for next major version

## Success Criteria

The redesign is successful when:

1. ✅ **Tests are simpler** - Compare before/after line counts for 5 migrated tests (expect 40-60% reduction)
2. ✅ **Tests are faster** - No more `Thread.Sleep(60)` for escape sequences (measure before/after test execution time)
3. ✅ **Tests are deterministic** - Run same test 100 times, verify identical results every time
4. ✅ **Pipeline testing works** - Verify ANSI encoding/decoding tests still pass in Pipeline mode
5. ✅ **Code coverage maintained** - Verify coverage doesn't drop after migration
6. ✅ **All tests pass** - Both old (unmigrated) and new (migrated) tests pass
7. ✅ **Documentation complete** - New contributors can understand and use the system
8. ✅ **No production code breakage** - Drivers work identically to before

## Coding Standards Reminder

⚠️ **CRITICAL**: All code MUST follow these Terminal.Gui standards (from CONTRIBUTING.md):

### Never Use `var` (Except Built-in Types)
```csharp
// ✅ CORRECT
VirtualTimeProvider time = new ();
InputInjector injector = new (app, time);
List<InputRecord> records = [];
var count = 0;  // OK - int is built-in

// ❌ WRONG
var time = new VirtualTimeProvider();
var injector = new InputInjector(app, time);
var records = new List<InputRecord>();
```

### Always Use Target-Typed `new ()`
```csharp
// ✅ CORRECT
VirtualTimeProvider time = new ();
Mouse mouse = new () { Position = new (5, 5) };

// ❌ WRONG
VirtualTimeProvider time = new VirtualTimeProvider();
Mouse mouse = new Mouse() { Position = new Point(5, 5) };
```

### Always Use Collection Initializers
```csharp
// ✅ CORRECT
List<VirtualTimer> timers = [];
InputEvent [] events = [
    new KeyEvent (Key.A),
    new KeyEvent (Key.B)
];

// ❌ WRONG
List<VirtualTimer> timers = new List<VirtualTimer>();
List<InputEvent> events = new ();
events.Add(new KeyEvent(Key.A));
events.Add(new KeyEvent(Key.B));
```

### Always Add XML Documentation
```csharp
/// <summary>
/// Advances virtual time by the specified duration.
/// </summary>
/// <param name="duration">The time span to advance.</param>
public void Advance (TimeSpan duration)
{
    // ...
}
```

### Add "CoPilot" Comment to All AI-Generated Code
```csharp
[Fact]
public void MyTest ()
{
    // CoPilot - Test virtual time advancement
    VirtualTimeProvider time = new ();
    time.Advance (TimeSpan.FromSeconds (1));
    // ...
}
```

## Key Files to Reference

While implementing, refer to these existing files for context:

| File | Purpose |
|------|---------|
| `CONTRIBUTING.md` | Coding standards, build instructions, test patterns |
| `./docfx/docs/drivers.md` | Driver deep dive |
| `./docfx/docs/mouse.md` | Mouse deep dive |
| `./docfx/docs/mouse-behavior-specification.md ` | New Mouse behavior details |
| `./docfx/docs/application.md` | Application deep dive |
| `Terminal.Gui/Drivers/MouseButtonClickTracker.cs` | Example of component needing `ITimeProvider` |
| `Terminal.Gui/Drivers/MouseInterpreter.cs` | Example of component using timestamps |
| `Tests/UnitTests/InputTestHelpers.cs` | Current helpers to be replaced |
| `Tests/UnitTestsParallelizable/Application/Mouse/ApplicationMouseTests.cs` | Example tests to migrate |
| `docfx/docs/driver-input-injection.md` | Current architecture documentation |

## Questions/Issues During Implementation

If you encounter issues:

1. **Can't find where to add `ITimeProvider` parameter** - Look for constructors that currently use `DateTime.Now` directly
2. **Tests fail after migration** - Check that `VirtualTimeProvider` is being passed correctly through all components
3. **ANSI encoding tests fail** - Verify `InputInjectionMode.Pipeline` is being used correctly
4. **Code style violations** - Run ReSharper/Rider cleanup, verify `.editorconfig` rules
5. **Coverage drops** - Add tests for new `VirtualTimeProvider` and `InputInjector` classes

## Expected Outcomes

After completing this implementation:

- **~15 new files** created for time abstraction and input injection
- **~10 existing files** updated to accept `ITimeProvider`
- **~50 test files** migrated to use new API (can be done incrementally)
- **~40-60% reduction** in test code verbosity
- **~2-3x faster** test execution (no real delays)
- **100% deterministic** timing-dependent tests

---

**Document Status:** Implementation-Ready Specification  
**Last Updated:** 2025-01-14  
**Authors:** @copilot  
**For AI Agents:** This document contains everything needed to implement the redesign. Follow the checklist systematically, refer to coding standards religiously, and validate at each step.
