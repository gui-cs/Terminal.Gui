# FakeDriver Testing Guide

## Overview

Terminal.Gui provides testing infrastructure through two complementary driver implementations:

1. **FakeDriver** (this directory) - Legacy ConsoleDriver-based fake driver for backward compatibility
2. **FakeConsoleDriver** (in TerminalGuiFluentTesting) - Modern component-based fake driver for fluent testing

## FakeDriver Architecture

`FakeDriver` extends the abstract `ConsoleDriver` base class and is designed for:
- Backward compatibility with existing tests
- Use with `AutoInitShutdownAttribute` in traditional unit tests
- Direct driver manipulation and state inspection

### Key Components

- **FakeDriver.cs** - Main driver implementation
- **FakeConsole.cs** - Static mock console for input/output simulation
- **FakeComponentFactory.cs** - Component factory for modern initialization path
- **FakeConsoleInput.cs** - Input simulation
- **FakeConsoleOutput.cs** - Output capture and validation
- **FakeWindowSizeMonitor.cs** - Window resize simulation

## Usage Patterns

### Basic Test Setup

```csharp
[Fact]
[AutoInitShutdown]  // Automatically initializes and shuts down Application
public void My_Test()
{
    // Application is already initialized with FakeDriver
    Assert.NotNull(Application.Driver);
    Assert.True(Application.Initialized);
}
```

### Simulating Screen Resizes

```csharp
[Fact]
[AutoInitShutdown]
public void Test_Resize_Behavior()
{
    // Start with default size (80x25)
    Assert.Equal(80, Application.Driver.Cols);
    Assert.Equal(25, Application.Driver.Rows);
    
    // Simulate a terminal resize
    AutoInitShutdownAttribute.FakeResize(new Size(120, 40));
    
    // Verify the resize took effect
    Assert.Equal(120, Application.Driver.Cols);
    Assert.Equal(40, Application.Driver.Rows);
    Assert.Equal(new Rectangle(0, 0, 120, 40), Application.Screen);
}
```

### Subscribing to Resize Events

```csharp
[Fact]
[AutoInitShutdown]
public void Test_Resize_Events()
{
    bool eventFired = false;
    Size? newSize = null;
    
    Application.Driver.SizeChanged += (sender, args) =>
    {
        eventFired = true;
        newSize = args.Size;
    };
    
    AutoInitShutdownAttribute.FakeResize(new Size(100, 30));
    
    Assert.True(eventFired);
    Assert.Equal(100, newSize.Value.Width);
    Assert.Equal(30, newSize.Value.Height);
}
```

### Simulating Keyboard Input

```csharp
[Fact]
[AutoInitShutdown]
public void Test_Keyboard_Input()
{
    // Queue keyboard input before it's processed
    FakeConsole.PushMockKeyPress(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
    FakeConsole.PushMockKeyPress(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
    
    // Your test logic that processes the input
    // ...
}
```

### Verifying Screen Output

```csharp
[Fact]
[AutoInitShutdown]
public void Test_Screen_Output()
{
    Application.Top = new Toplevel();
    var label = new Label { Text = "Hello", X = 0, Y = 0 };
    Application.Top.Add(label);
    
    Application.Begin(Application.Top);
    AutoInitShutdownAttribute.RunIteration();
    
    // Access driver contents to verify output
    var contents = Application.Driver.Contents;
    Assert.NotNull(contents);
    
    // Verify specific characters were drawn
    // contents[row, col] gives you access to individual cells
}
```

## Relationship Between Driver Properties

Understanding the relationship between key driver properties:

- **Screen** - `Rectangle` representing the full console area. Always starts at (0,0) with size (Cols, Rows).
- **Cols** - Number of columns (width) in the console.
- **Rows** - Number of rows (height) in the console.
- **Contents** - 2D array `[rows, cols]` containing the actual screen buffer cells.

When you resize:
1. `SetBufferSize(width, height)` or `FakeResize(Size)` is called
2. `Cols` and `Rows` are updated
3. `Contents` buffer is reallocated to match new dimensions
4. `Screen` property returns updated rectangle
5. `SizeChanged` event fires with new size
6. Application propagates resize to top-level views

## Thread Safety

⚠️ **Important**: FakeDriver is **not thread-safe**. Tests that use FakeDriver should:
- Not run in parallel with other tests that access the driver
- Not share driver state between test methods
- Use `AutoInitShutdownAttribute` to ensure clean initialization/shutdown per test

For parallel-safe tests, use the UnitTestsParallelizable project with its own test infrastructure.

## Differences from Production Drivers

FakeDriver differs from production drivers (WindowsDriver, UnixDriver, DotNetDriver) in several ways:

| Aspect | FakeDriver | Production Drivers |
|--------|------------|-------------------|
| Screen Size | Programmatically set via `SetBufferSize` | Determined by actual terminal size |
| Input | Queued via `FakeConsole.PushMockKeyPress` | Reads from actual stdin |
| Output | Captured in memory buffer | Written to actual terminal |
| Resize | Triggered by test code | Triggered by OS (SIGWINCH, WINDOW_BUFFER_SIZE_EVENT) |
| Buffer vs Window | Always equal (no scrollback) | Can differ (scrollback support) |

## Advanced: Direct Driver Access

For tests that need more control, you can access the driver directly:

```csharp
[Fact]
public void Test_With_Direct_Driver_Access()
{
    // Create and initialize driver manually
    Application.ResetState(true);
    var driver = new FakeDriver();
    Application.Driver = driver;
    Application.SubscribeDriverEvents();
    
    // Use driver directly
    driver.SetBufferSize(100, 50);
    
    Assert.Equal(100, driver.Cols);
    Assert.Equal(50, driver.Rows);
    
    // Cleanup
    Application.ResetState(true);
}
```

## Modern Component-Based Architecture

For new test infrastructure, consider using the modern component factory approach via `FakeComponentFactory`:

```csharp
var factory = new FakeComponentFactory();
// Modern driver initialization through component factory pattern
// This is used internally by the fluent testing infrastructure
```

The fluent testing project (`TerminalGuiFluentTesting`) provides a higher-level API built on this architecture.

## See Also

- **AutoInitShutdownAttribute** - Attribute for automatic test setup/teardown
- **TerminalGuiFluentTesting** - Modern fluent testing infrastructure
- **FakeConsole** - Static mock console used by FakeDriver
- **ConsoleDriver** - Base class documentation for all drivers
