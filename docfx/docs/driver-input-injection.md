# Driver Input Injection - Current Implementation

This document describes the current architecture for injecting input (keyboard and mouse events) into Terminal.Gui applications for testing purposes. This serves as a baseline for potential future redesign of the injection flow and related interfaces (`IInputProcessor`, `IInput`, `ITestableInput`).

## Overview

The input injection system allows tests to programmatically simulate user input without requiring actual keyboard/mouse hardware or terminal interaction. The current implementation has evolved to support:

1. **Deep injection** - Input injected at the lowest level (driver input queue) to test the full parsing/processing pipeline
2. **ANSI encoding/decoding** - For ANSI-based drivers, ensuring the full round-trip through escape sequence encoding
3. **Test helpers** - High-level utilities that abstract the injection complexity
4. **Time control** - Limited support for controlling event timestamps (added for mouse double-click testing)

## Architecture Layers

### 1. Driver Layer: IInput and ITestableInput

#### IInput<TInputRecord>
Located in: `Terminal.Gui/Drivers/IInput.cs`

The base interface for reading console input on a dedicated input thread.

**Key responsibilities:**
- Runs perpetual read loop on a background thread via `Run(CancellationToken)`
- Reads platform-specific input from the console
- Places input into a thread-safe `ConcurrentQueue<TInputRecord>` for processing
- Supports external cancellation via `ExternalCancellationTokenSource`

**Architecture flow:**
```
Input Thread (IInput.Run):           Main UI Thread (IInputProcessor):
???????????????????                  ????????????????????????
? IInput.Run()    ?                  ? IInputProcessor      ?
?  ?? Peek()      ?                  ?  ?? ProcessQueue()   ?
?  ?? Read()      ???Enqueue??????????  ?? Process()        ?
?  ?? Enqueue     ?                  ?  ?? ToKey()          ?
???????????????????                  ?  ?? Raise Events     ?
                                     ????????????????????????
```

**Platform implementations:**
- `WindowsInput` - Uses Windows Console API (`ReadConsoleInput`)
- `NetInput` - Uses .NET `System.Console` API
- `UnixInput` - Uses Unix terminal APIs  
- `AnsiInput` - For testing, implements `ITestableInput<char>`

#### ITestableInput<TInputRecord>
Located in: `Terminal.Gui/Drivers/ITestableInput.cs`

A marker interface extending `IInput<TInputRecord>` that adds test input injection capability.

```csharp
public interface ITestableInput<TInputRecord> : IInput<TInputRecord>
    where TInputRecord : struct
{
    /// <summary>
    /// Adds an input record that will be returned by Peek/Read for testing.
    /// </summary>
    void InjectInput (TInputRecord input);
}
```

**Design characteristics:**
- Single method: `InjectInput(TInputRecord)` - adds input to internal test queue
- Input flows through the same `Peek()`/`Read()` pipeline as real input
- No explicit time control - timestamps derived from `DateTime.Now` when input is processed
- Used by `AnsiInput` and `FakeInput` implementations

**Implementation in AnsiInput:**
```csharp
public class AnsiInput : InputImpl<char>, ITestableInput<char>
{
    private readonly ConcurrentQueue<char> _testInput = new ();
    
    public void InjectInput (char input) 
    {
        _testInput.Enqueue (input);
    }
    
    public override bool Peek() 
    {
        return !_testInput.IsEmpty; // Check test queue first
    }
    
    protected override IEnumerable<char> Read()
    {
        while (_testInput.TryDequeue(out char c))
        {
            yield return c;
        }
    }
}
```

### 2. Input Processor Layer: IInputProcessor

#### IInputProcessor
Located in: `Terminal.Gui/Drivers/InputProcessorImpl.cs`

Processes queued input on the main loop thread, translating driver-specific records into Terminal.Gui events.

**Key responsibilities:**
- Dequeues input from `InputQueue` (populated by `IInput` thread)
- Parses ANSI escape sequences via `AnsiResponseParser<TInputRecord>`
- Converts platform records to `Key` objects via `IKeyConverter`
- Generates synthetic mouse click events via `MouseInterpreter`
- Raises `KeyDown`, `KeyUp`, `MouseEventParsed`, `SyntheticMouseEvent`

**Injection methods:**
```csharp
public interface IInputProcessor
{
    // Keyboard injection
    void InjectKeyDownEvent(Key key);      // High-level key injection
    void RaiseKeyDownEvent(Key key);       // Directly raise event (bypass queue)
    
    // Mouse injection  
    void InjectMouseEvent(IApplication? app, Mouse mouse);  // High-level mouse injection
    void RaiseMouseEventParsed(Mouse mouse);                // Directly raise event (bypass queue)
}
```

#### InputProcessorImpl<TInputRecord>
Base implementation providing standard input processing logic.

**Processing flow:**
```csharp
public void ProcessQueue()
{
    // 1. Process all queued input
    while (InputQueue.TryDequeue(out TInputRecord input))
    {
        Process(input);  // Derived classes implement this
    }
    
    // 2. Release stale escape sequences (e.g., held Esc key)
    foreach (TInputRecord input in ReleaseParserHeldKeysIfStale())
    {
        ProcessAfterParsing(input);
    }
    
    // 3. Check for expired mouse clicks (deferred click detection)
    CheckForExpiredMouseClicks();
}
```

**Injection implementation:**
```csharp
public virtual void InjectKeyDownEvent(Key key)
{
    // Convert Key ? TInputRecord
    TInputRecord inputRecord = KeyConverter.ToKeyInfo(key);
    
    // If input supports testing, use Peek/Read pipeline
    if (InputImpl is ITestableInput<TInputRecord> testableInput)
    {
        testableInput.InjectInput(inputRecord);
    }
}
```

### 3. ANSI-Specific: AnsiInputProcessor

#### AnsiInputProcessor
Located in: `Terminal.Gui/Drivers/ANSIDriver/ANSIInputProcessor.cs`

Specialized processor for ANSI driver that handles character stream input and ANSI escape sequences.

**Key features:**
- Processes `char` stream input
- Uses `AnsiKeyboardEncoder` to convert `Key` ? ANSI escape sequence
- Uses `AnsiMouseEncoder` to convert `Mouse` ? ANSI SGR format
- Injects encoded sequences character-by-character into `AnsiInput`

**Keyboard injection with encoding:**
```csharp
public override void InjectKeyDownEvent(Key key)
{
    // Convert Key ? ANSI sequence (e.g., F1 ? "\x1b[OP")
    string sequence = AnsiKeyboardEncoder.Encode(key);
    
    if (InputImpl is not ITestableInput<char> testableInput)
        return;
    
    // Inject each character of the sequence
    foreach (char ch in sequence)
    {
        testableInput.InjectInput(ch);
    }
}
```

**Mouse injection with encoding:**
```csharp
public override void InjectMouseEvent(IApplication? app, Mouse mouse)
{
    base.InjectMouseEvent(app, mouse);  // Set timestamp
    
    // Convert Mouse to ANSI SGR format (e.g., "\x1b[<0;10;5M")
    string ansiSequence = AnsiMouseEncoder.Encode(mouse);
    
    if (InputImpl is not ITestableInput<char> testableInput)
        return;
    
    // Inject each character of the ANSI sequence
    foreach (char ch in ansiSequence)
    {
        testableInput.InjectInput(ch);
    }
}
```

**Why encoding is used:**
This ensures the full ANSI parsing pipeline is tested:
1. Encode: `Key.F1` ? `"\x1b[OP"` (4 characters)
2. Inject: Characters queued individually in `AnsiInput`
3. Parse: `AnsiResponseParser` detects escape sequence
4. Decode: Parser raises keyboard event with `Key.F1`

This tests the complete round-trip through ANSI encoding/decoding.

### 4. Driver Layer: IDriver.InjectKeyEvent/InjectMouseEvent

#### DriverImpl
Located in: `Terminal.Gui/Drivers/DriverImpl.cs`

The driver implementation provides public injection APIs that delegate to the input processor.

```csharp
public class DriverImpl : IDriver
{
    public void InjectKeyEvent(Key key) 
    { 
        GetInputProcessor().InjectKeyDownEvent(key); 
    }
    
    public void InjectMouseEvent(Mouse mouse) 
    { 
        GetInputProcessor().InjectMouseEvent(null, mouse); 
    }
}
```

These are the primary entry points used by test code via `IDriver`.

### 5. Test Helper Layer: InputTestHelpers

#### InputTestHelpers (UnitTests)
Located in: `Tests/UnitTests/InputTestHelpers.cs`

High-level helper methods that abstract the injection complexity for unit tests. These methods handle:
- Simulating the input thread (moving items from test queue to input buffer)
- Processing the input queue
- Handling special timing (e.g., escape sequence delays)
- Waiting for events to complete

**Key methods:**

##### SimulateInputThread
```csharp
public static void SimulateInputThread<TInputRecord>(
    this InputImpl<TInputRecord> input,
    ConcurrentQueue<TInputRecord> inputBuffer)
{
    // Drain the test input queue and move to InputBuffer
    while (input.Peek())
    {
        foreach (TInputRecord item in input.Read())
        {
            inputBuffer.Enqueue(item);
        }
    }
}
```

Purpose: Tests don't start the actual input thread via `Run()`. This method manually drains the internal test queue (`ITestableInput._testInput`) and moves items to the `InputBuffer` queue that `IInputProcessor.ProcessQueue()` reads from.

##### InjectAndProcessKey
```csharp
public static void InjectAndProcessKey(this IApplication app, Key key)
{
    app.Driver!.InjectKeyEvent(key);
    
    // Simulate the input thread
    app.SimulateInputThread();
    
    // Process the queue (with special handling for Esc key)
    if (key.KeyCode == KeyCode.Esc || key.IsAlt)
    {
        app.ProcessQueueWithEscapeHandling();
    }
    else
    {
        app.Driver.GetInputProcessor().ProcessQueue();
    }
}
```

Purpose: Complete injection + processing flow for keyboard input, handling escape sequence timing.

##### InjectAndProcessMouse  
```csharp
public static void InjectAndProcessMouse(this IApplication app, Mouse mouse)
{
    app.Driver!.InjectMouseEvent(mouse);
    
    // Simulate the input thread
    app.SimulateInputThread();
    
    // Process the queue
    app.Driver.GetInputProcessor().ProcessQueue();
}
```

Purpose: Complete injection + processing flow for mouse input.

##### InjectMouseEventDirectly
```csharp
public static void InjectMouseEventDirectly(this IApplication app, Mouse mouse)
{
    IInputProcessor processor = app.Driver!.GetInputProcessor();
    
    // Set timestamp if not provided
    mouse.Timestamp ??= DateTime.Now;
    
    // Directly raise the event through the processor, bypassing ANSI encoding
    processor.RaiseMouseEventParsed(mouse);
}
```

Purpose: **Bypass ANSI encoding** to preserve timestamps and other properties that ANSI encoding cannot carry. Used for testing timestamp-based multi-click detection where precise timing control is required.

**Why this exists:** ANSI mouse encoding (SGR format) doesn't carry timestamp information. When testing timestamp-based logic (e.g., double-click thresholds), we need to inject `Mouse` events directly with controlled timestamps, bypassing the encoding/decoding round-trip.

##### ProcessQueueWithEscapeHandling
```csharp
public static void ProcessQueueWithEscapeHandling(
    this IInputProcessor processor, 
    int maxAttempts = 3)
{
    // First attempt - process immediately
    processor.ProcessQueue();
    
    // Wait and process again to release held escape keys
    for (var attempt = 1; attempt < maxAttempts; attempt++)
    {
        Thread.Sleep(60);  // Wait longer than the 50ms escape timeout
        processor.ProcessQueue();
    }
}
```

Purpose: Handle escape sequence timing. The `AnsiResponseParser` holds `Esc` for 50ms waiting to see if it's part of an escape sequence. This method waits and processes again to ensure held keys are released.

### 6. Integration Test Layer: GuiTestContext

#### GuiTestContext (TerminalGuiFluentTesting)
Located in: `Tests/TerminalGuiFluentTesting/GuiTestContext.cs`

Fluent API for integration tests that runs a complete Terminal.Gui application in a background thread with proper lifecycle management.

**Key features:**
- Runs application in background thread via `Task.Run(() => app.Run(runnable))`
- Uses `AnsiInput` with `ExternalCancellationTokenSource` for timeout control
- Provides fluent methods for injecting input and asserting state
- Handles synchronization between test thread and application thread

**Mouse injection:**
```csharp
public GuiTestContext LeftClick(int screenX, int screenY)
{
    InjectMouseEvent(new()
    {
        Flags = MouseFlags.LeftButtonPressed,
        ScreenPosition = new(screenX, screenY),
        Position = new(screenX, screenY)
    });
    
    return InjectMouseEvent(new()
    {
        Flags = MouseFlags.LeftButtonReleased,
        ScreenPosition = new(screenX, screenY),
        Position = new(screenX, screenY)
    });
}

private GuiTestContext InjectMouseEvent(Mouse mouse)
{
    WaitIteration((app) =>
    {
        if (app.Driver is { })
        {
            mouse.Timestamp = DateTime.Now;
            mouse.Position = mouse.ScreenPosition;
            
            // Inject through the driver
            app.Driver.GetInputProcessor().InjectMouseEvent(app, mouse);
        }
    });
    
    // Wait for the event to be processed
    return WaitIteration();
}
```

**Keyboard injection:**
```csharp
public GuiTestContext InjectKeyEvent(Key key)
{
    bool keyReceived = false;
    
    if (App?.Driver is { })
    {
        App.Driver.KeyDown += (s, e) => keyReceived = true;
        App.Driver.InjectKeyEvent(key);
        
        // Wait until key is processed
        WaitUntil(() => keyReceived);
    }
    
    return this;
}
```

**Synchronization mechanism:**
`GuiTestContext` uses `WaitIteration()` to synchronize between test thread and application thread:
1. Enqueues an action to run on the next main loop iteration
2. Blocks test thread until action completes
3. Handles timeouts and exceptions from background thread

## Time Control Limitations

### Current State: Limited Timestamp Support

The current implementation has **very limited** time control capabilities:

#### What Works:
1. **Direct injection with timestamps** - `InjectMouseEventDirectly()` allows setting `Mouse.Timestamp` directly
2. **MouseInterpreter uses timestamps** - The `MouseButtonClickTracker` respects `Mouse.Timestamp ?? DateTime.Now` for multi-click detection
3. **Tests can control click spacing** - By setting timestamps on injected events

#### What Doesn't Work:
1. **ANSI encoding loses timestamps** - ANSI escape sequences don't carry timestamp info, so round-trip through encoding/decoding uses `DateTime.Now`
2. **No global time control** - Can't "fake" `DateTime.Now` to control timing without direct injection
3. **No timer control** - Delays like `Thread.Sleep(60)` in `ProcessQueueWithEscapeHandling` use real time
4. **KeyboardInterpreter doesn't use timestamps** - Keyboard events don't have timestamp support yet

### Example: Timestamp-Based Mouse Testing

```csharp
[Fact]
public void InjectMouseEventDirectly_WithTimestamps_PreventsDoubleClickWhenSpaced()
{
    using IApplication app = Application.Create();
    app.Init(DriverRegistry.Names.ANSI);
    
    List<MouseFlags> receivedFlags = [];
    app.Mouse.MouseEvent += (s, e) => receivedFlags.Add(e.Flags);
    
    DateTime baseTime = new(2025, 1, 1, 12, 0, 0);
    
    // First click at T+0
    app.InjectMouseEventDirectly(new() { 
        ScreenPosition = new(5, 5), 
        Flags = MouseFlags.LeftButtonPressed, 
        Timestamp = baseTime 
    });
    app.InjectMouseEventDirectly(new() { 
        ScreenPosition = new(5, 5), 
        Flags = MouseFlags.LeftButtonReleased, 
        Timestamp = baseTime.AddMilliseconds(100) 
    });
    
    // Second click at T+600 (more than 500ms threshold)
    app.InjectMouseEventDirectly(new() { 
        ScreenPosition = new(5, 5), 
        Flags = MouseFlags.LeftButtonPressed, 
        Timestamp = baseTime.AddMilliseconds(600) 
    });
    app.InjectMouseEventDirectly(new() { 
        ScreenPosition = new(5, 5), 
        Flags = MouseFlags.LeftButtonReleased, 
        Timestamp = baseTime.AddMilliseconds(700) 
    });
    
    // Should get two single clicks (not a double-click)
    Assert.Equal(2, receivedFlags.Count(f => f.HasFlag(MouseFlags.LeftButtonClicked)));
}
```

This test uses `InjectMouseEventDirectly()` to **bypass ANSI encoding** and preserve timestamps for precise timing control.

## Use Case: External Test Infrastructure (PR #4427)

PR #4427 demonstrates a use case for injecting input into **arbitrary Terminal.Gui applications** (not just tests):

### Goal: Example Discovery and Testing
Create infrastructure to:
1. Discover Terminal.Gui example applications via assembly attributes
2. Run them in-process or out-of-process with automatic input injection
3. Test that examples start, respond to input, and shut down correctly
4. Make examples "copy/paste ready" with no test-specific code

### Proposed Approach (from PR):

#### 1. Assembly Attributes for Metadata
```csharp
[assembly: ExampleMetadata("Character Map", "Unicode viewer")]
[assembly: ExampleCategory("Text and Formatting")]
[assembly: ExampleDemoKeyStrokes(KeyStrokes = ["SetDelay:500", "CursorDown", "Esc"])]

// Pure example code - no test cruft
IApplication app = Application.Create(example: true);
app.Init();
app.Run<CharMap>();
app.Dispose();
```

#### 2. Example Mode in Application
When `Application.Create(example: true)`:
- Application subscribes to `SessionBegun` event
- When first `TopRunnable` becomes modal, demo keys from attributes are injected
- Keys sent via `Keyboard.RaiseKeyDownEvent()` with configurable delays
- External systems can subscribe to `Application.Apps` observable collection

#### 3. ExampleRunner
Console app that:
- Discovers examples from assembly attributes
- Runs each example with FakeDriver
- Injects demo keystrokes automatically
- Reports success/failure
- Returns exit code 0 if all pass, 1 if any fail

### Limitations with Current Architecture:

1. **No environment variable-based injection** - Original PR tried using `TERMGUI_TEST_CONTEXT` env var to pass test context, but this tightly couples driver layer to testing infrastructure

2. **Key injection timing** - Getting keys to fire at the right time (after UI is ready) requires event coordination:
   ```csharp
   // Subscribe to IsModalChanged to know when UI is ready
   topRunnable.IsModalChanged += (s, e) => 
   {
       if (e.Value) // Became modal
       {
           Task.Run(async () => 
           {
               foreach (string keyStr in demoKeys)
               {
                   await Task.Delay(delayMs);
                   app.Keyboard.RaiseKeyDownEvent(Key.Parse(keyStr));
               }
           });
       }
   };
   ```

3. **No isolation** - In-process execution shares global state (Configuration, Application statics, etc.)

4. **Out-of-process coordination** - Separate process can't easily subscribe to events or inject input after startup

## Issues and Pain Points

### 1. Timestamp Control
**Problem:** Very limited ability to control time for testing timing-dependent behavior.

**Current workarounds:**
- `InjectMouseEventDirectly()` for mouse timestamps
- Real `Thread.Sleep()` delays for escape sequence handling
- No keyboard timestamp support

**Impact:** Can't test timing-dependent keyboard behavior (e.g., key repeat timing, debouncing)

### 2. ANSI Encoding Round-Trip Required
**Problem:** Testing the full pipeline requires encoding?queue?parsing?decoding, which loses information (timestamps) and adds complexity.

**Current approach:**
```csharp
// For testing full pipeline:
app.Driver.InjectKeyEvent(Key.F1);  // Encodes to "\x1b[OP", queues, parses back

// For testing with timestamps:
app.InjectMouseEventDirectly(mouse);  // Bypasses encoding entirely
```

**Impact:** Two different code paths, can't test full pipeline with time control

### 3. Input Thread Simulation
**Problem:** Tests don't start the real input thread, so `InputTestHelpers.SimulateInputThread()` must manually drain queues.

**Current approach:**
```csharp
app.Driver.InjectKeyEvent(key);        // Adds to _testInput
app.SimulateInputThread();             // Moves _testInput ? InputQueue  
processor.ProcessQueue();              // Processes InputQueue
```

**Impact:** Three-step dance required, easy to forget steps, doesn't match real flow

### 4. Escape Sequence Timing
**Problem:** ANSI parser holds `Esc` for 50ms to detect escape sequences. Tests must wait and re-process.

**Current workaround:**
```csharp
processor.ProcessQueue();              // Process immediately
Thread.Sleep(60);                      // Wait for timeout
processor.ProcessQueue();              // Process again to release held keys
```

**Impact:** Tests take longer (60ms per escape key), uses real time

### 5. External Application Injection
**Problem:** No clean way to inject input into a standalone application that wasn't designed for testing.

**Current approaches:**
- Modify application to call `Application.Create(example: true)`
- Use environment variables (couples driver to test infrastructure)
- Run in-process (shares global state)
- Run out-of-process (hard to coordinate injection timing)

**Impact:** Examples need test-awareness, can't test arbitrary applications

### 6. Synchronization in Integration Tests
**Problem:** Integration tests run application on background thread, need to synchronize input injection with UI readiness.

**Current approach:** `GuiTestContext.WaitIteration()` blocks until action completes on main loop iteration

**Impact:** Complex synchronization logic, timeouts needed everywhere

## Comparison: Testing Patterns

### Unit Tests (Parallelizable)
```csharp
// Simple, synchronous, no threading
using IApplication app = Application.Create();
app.Init(DriverRegistry.Names.ANSI);

var view = new View { CanFocus = true };
app.Mouse.MouseEvent += (s, e) => /* ... */;

// Inject and process synchronously
app.InjectAndProcessMouse(new() { 
    ScreenPosition = new(5, 5), 
    Flags = MouseFlags.LeftButtonPressed 
});

// Assertions run immediately
Assert.True(mouseReceived);
```

**Pros:** Simple, fast, deterministic
**Cons:** Doesn't test full application lifecycle (no `Run()`)

### Integration Tests (GuiTestContext)
```csharp
// Complex, async, background thread
using GuiTestContext context = With.A<Window>(40, 10, TestDriver.ANSI)
    .Add(button)
    .LeftClick(6, 6)
    .AssertEqual(1, clickCount);

// Under the hood:
// - Application.Run() on background thread
// - InjectMouseEvent() queues action for next iteration
// - WaitIteration() blocks until processed
// - Background thread processes event and updates clickCount
// - Test thread continues after synchronization
```

**Pros:** Tests complete application lifecycle
**Cons:** Complex synchronization, timeouts, background thread issues

### External Application (PR #4427 approach)
```csharp
// Example: Clean, no test code
[assembly: ExampleDemoKeyStrokes(KeyStrokes = ["Esc"])]

IApplication app = Application.Create(example: true);
app.Init();
app.Run<CharMap>();

// Runner: Discovers and runs
var examples = ExampleDiscovery.DiscoverFromDirectory("Examples");
foreach (var example in examples)
{
    var result = ExampleRunner.Run(example, new() { 
        DriverName = "FakeDriver",
        TimeoutMs = 5000 
    });
}
```

**Pros:** Examples stay clean, can test arbitrary applications
**Cons:** Timing coordination, out-of-process challenges, key injection after startup

## Summary: Current State

### What Works Well:
1. ? **Deep injection** - Can inject at driver level to test full pipeline
2. ? **ANSI encoding test** - Can verify escape sequence encoding/decoding
3. ? **Unit test helpers** - Simple API for common test scenarios
4. ? **Integration test framework** - GuiTestContext provides fluent API with lifecycle management
5. ? **Mouse timestamps** - Direct injection preserves timestamps for testing

### What Needs Improvement:
1. ? **Time control** - Very limited, can't "fake time" globally
2. ? **Simplified injection** - Too many steps (inject ? simulate thread ? process queue)
3. ? **External app injection** - No clean way to inject into arbitrary applications
4. ? **Keyboard timestamps** - Not supported yet
5. ? **Escape timing** - Requires real delays and multiple processing passes
6. ? **Synchronization complexity** - Integration tests need complex coordination

## Future Considerations

Based on the pain points above and the use case from PR #4427, future redesign should consider:

1. **Virtual Time System**
   - Allow "faking" `DateTime.Now` for testing
   - Control all time-dependent behavior (timers, delays, timestamps)
   - Enable deterministic testing of timing behavior

2. **Simplified Injection API**
   - Single method call that handles injection ? threading ? processing
   - Hide the complexity of queue management
   - Match real input flow more closely

3. **Event-Based Injection**
   - Inject events directly into processor without encoding round-trip
   - Still support ANSI encoding test mode when needed
   - Preserve all event properties (timestamps, etc.)

4. **External Application Hooks**
   - Clean way to inject input into running applications
   - No application code changes required
   - Support for remote/out-of-process injection

5. **Declarative Input Sequences**
   - Define input sequences in data (JSON, attributes, etc.)
   - Support delays, conditions, assertions
   - Reusable across unit/integration/example tests

6. **Better Synchronization Primitives**
   - Wait for specific UI states (modal, focus, etc.)
   - Automatic retry with timeout
   - Cleaner than current `WaitIteration()` approach

This document provides the baseline for understanding the current architecture before proposing redesigns.
