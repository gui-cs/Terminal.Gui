# Cross-Platform Driver Model

## Overview

The driver model is the mechanism by which Terminal.Gui supports multiple platforms. Windows, Mac, Linux, and unit test environments are all supported through a modular, component-based architecture.

Terminal.Gui v2 uses a sophisticated driver architecture that separates concerns and enables platform-specific optimizations while maintaining a consistent API. The architecture is based on the **Component Factory** pattern and uses **multi-threading** to ensure responsive input handling.

## Available Drivers

Terminal.Gui provides console driver implementations optimized for different platforms:

- **DotNetDriver (`dotnet`)** - A cross-platform driver that uses the .NET `System.Console` API. Works on all platforms (Windows, macOS, Linux). Best for maximum compatibility.
- **WindowsDriver (`windows`)** - A Windows-optimized driver that uses native Windows Console APIs for enhanced performance and platform-specific features.
- **UnixDriver (`unix`)** - A Unix/Linux/macOS-optimized driver that uses platform-specific APIs for better integration and performance.
- **FakeDriver (`fake`)** - A mock driver designed for unit testing. Simulates console behavior without requiring a real terminal.

### Automatic Driver Selection

The appropriate driver is automatically selected based on the platform when you call `Application.Init()`:

- **Windows** (Win32NT, Win32S, Win32Windows) → `WindowsDriver`
- **Unix/Linux/macOS** → `UnixDriver`

### Explicit Driver Selection

You can explicitly specify a driver in three ways:

```csharp
// Method 1: Set ForceDriver property before Init
Application.ForceDriver = "dotnet";
Application.Init();

// Method 2: Pass driver name to Init
Application.Init(driverName: "unix");

// Method 3: Pass a custom IDriver instance
var customDriver = new MyCustomDriver();
Application.Init(driver: customDriver);
```

Valid driver names: `"dotnet"`, `"windows"`, `"unix"`, `"fake"`

## Architecture

### Component Factory Pattern

The v2 driver architecture uses the **Component Factory** pattern to create platform-specific components. Each driver has a corresponding factory:

- `NetComponentFactory` - Creates components for DotNetDriver
- `WindowsComponentFactory` - Creates components for WindowsDriver  
- `UnixComponentFactory` - Creates components for UnixDriver
- `FakeComponentFactory` - Creates components for FakeDriver

### Core Components

Each driver is composed of specialized components, each with a single responsibility:

#### IInput&lt;T&gt;
Reads raw console input events from the terminal. The generic type `T` represents the platform-specific input type:
- `ConsoleKeyInfo` for DotNetDriver and FakeDriver
- `WindowsConsole.InputRecord` for WindowsDriver
- `char` for UnixDriver

Runs on a dedicated input thread to avoid blocking the UI.

#### IOutput
Renders the output buffer to the terminal. Handles:
- Writing text and ANSI escape sequences
- Setting cursor position
- Managing cursor visibility
- Detecting terminal window size

#### IInputProcessor
Translates raw console input into Terminal.Gui events:
- Converts raw input to `Key` events (handles keyboard input)
- Parses ANSI escape sequences (mouse events, special keys)
- Generates `MouseEventArgs` for mouse input
- Handles platform-specific key mappings

#### IOutputBuffer
Manages the screen buffer and drawing operations:
- Maintains the `Contents` array (what should be displayed)
- Provides methods like `AddRune()`, `AddStr()`, `Move()`, `FillRect()`
- Handles clipping regions
- Tracks dirty regions for efficient rendering

#### IWindowSizeMonitor
Detects terminal size changes and raises `SizeChanged` events when the terminal is resized.

#### DriverFacade&lt;T&gt;
A unified facade that implements `IDriver` and coordinates all the components. This is what gets assigned to `Application.Driver`.

### Threading Model

The driver architecture employs a **multi-threaded design** for optimal responsiveness:

```
┌─────────────────────────────────────────────┐
│         IApplication.Init()              │
│  Creates MainLoopCoordinator<T> with        │
│  ComponentFactory<T>                        │
└────────────────┬────────────────────────────┘
                 │
                 ├──────────────────┬───────────────────┐
                 │                  │                   │
        ┌────────▼────────┐ ┌──────▼─────────┐ ┌──────▼──────────┐
        │  Input Thread   │ │  Main UI Thread│ │ Driver          │
        │                 │ │                 │ │   Facade        │
        │ IInput   │ │ ApplicationMain│ │                 │
        │ reads console   │ │ Loop processes │ │ Coordinates all │
        │ input async     │ │ events, layout,│ │ components      │
        │ into queue      │ │ and rendering  │ │                 │
        └─────────────────┘ └────────────────┘ └─────────────────┘
```

- **Input Thread**: Started by `MainLoopCoordinator`, runs `IInput.Run()` which continuously reads console input and queues it into a thread-safe `ConcurrentQueue<T>`.

- **Main UI Thread**: Runs `ApplicationMainLoop.Iteration()` which:
  1. Processes input from the queue via `IInputProcessor`
  2. Executes timeout callbacks
  3. Checks for UI changes (layout/drawing)
  4. Renders updates via `IOutput`

This separation ensures that input is never lost and the UI remains responsive during intensive operations.

### Initialization Flow

When you call `Application.Init()`:

1. **IApplication.Init()** is invoked
2. Creates a `MainLoopCoordinator<T>` with the appropriate `ComponentFactory<T>`
3. **MainLoopCoordinator.StartAsync()** begins:
   - Starts the input thread which creates `IInput<T>`
   - Initializes the main UI loop which creates `IOutput`
   - Creates `DriverFacade<T>` and assigns to `IApplication.Driver`
   - Waits for both threads to be ready
4. Returns control to the application

### Shutdown Flow

When `IApplication.Shutdown()` is called:

1. Cancellation token is triggered
2. Input thread exits its read loop
3. `IOutput` is disposed
4. Main thread waits for input thread to complete
5. All resources are cleaned up

## Component Interfaces

### IDriver

The main driver interface that the framework uses internally. Provides:

- **Screen Management**: `Screen`, `Cols`, `Rows`, `Contents`
- **Drawing Operations**: `AddRune()`, `AddStr()`, `Move()`, `FillRect()`
- **Cursor Management**: `SetCursorVisibility()`, `UpdateCursor()`
- **Attribute Management**: `CurrentAttribute`, `SetAttribute()`, `MakeColor()`
- **Clipping**: `Clip` property
- **Events**: `KeyDown`, `KeyUp`, `MouseEvent`, `SizeChanged`
- **Platform Features**: `SupportsTrueColor`, `Force16Colors`, `Clipboard`

**Note:** The driver is internal to Terminal.Gui. View classes should not access `Driver` directly. Instead:
- Use @Terminal.Gui.App.Application.Screen to get screen dimensions
- Use @Terminal.Gui.ViewBase.View.Move for positioning (with viewport-relative coordinates)
- Use @Terminal.Gui.ViewBase.View.AddRune and @Terminal.Gui.ViewBase.View.AddStr for drawing
- ViewBase infrastructure classes (in `Terminal.Gui/ViewBase/`) can access Driver when needed for framework implementation

## Platform-Specific Details

### DotNetDriver (NetComponentFactory)

- Uses `System.Console` for all I/O operations
- Input: Reads `ConsoleKeyInfo` via `Console.ReadKey()`
- Output: Uses `Console.Write()` and ANSI escape sequences
- Works on all platforms but may have limited features
- Best for maximum compatibility and simple applications

### WindowsDriver (WindowsComponentFactory)

- Uses Windows Console API via P/Invoke
- Input: Reads `InputRecord` structs via `ReadConsoleInput`
- Output: Uses Windows Console API for optimal performance
- Supports Windows-specific features and better performance
- Automatically selected on Windows platforms

#### Visual Studio Debug Console Support

When running in Visual Studio's debug console (`VSDebugConsole.exe`), WindowsDriver detects the `VSAPPIDNAME` environment variable and automatically adjusts its behavior:

- Disables the alternative screen buffer (which is not supported in VS debug console)
- Preserves the original console colors on startup
- Restores the original colors and clears the screen on shutdown

This ensures Terminal.Gui applications can be debugged directly in Visual Studio without rendering issues.

### UnixDriver (UnixComponentFactory)

- Uses Unix/Linux terminal APIs
- Input: Reads raw `char` data from terminal
- Output: Uses ANSI escape sequences
- Supports Unix-specific features
- Automatically selected on Unix/Linux/macOS platforms

### FakeDriver (FakeComponentFactory)

- Simulates console behavior for unit testing
- Uses `FakeConsole` for all operations
- Allows injection of predefined input
- Captures output for verification
- Always used when `IApplication.ForceDriver` is `fake`

**Important:** View subclasses should not access `Application.Driver`. Use the View APIs instead:
- `View.Move(col, row)` for positioning
- `View.AddRune()` and `View.AddStr()` for drawing
- `View.App.Screen` for screen dimensions


## See Also

- @Terminal.Gui.Drivers - API Reference
- @Terminal.Gui.App.IApplication - Application interface
- @Terminal.Gui.App.MainLoopCoordinator`1 - Main loop coordination
