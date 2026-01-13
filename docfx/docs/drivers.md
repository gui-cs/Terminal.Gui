# Cross-Platform Driver Model

## Overview

The driver model is the mechanism by which Terminal.Gui supports multiple platforms. Windows, Mac, Linux, and unit test environments are all supported through a modular, component-based architecture.

Terminal.Gui v2 uses a sophisticated driver architecture that separates concerns and enables platform-specific optimizations while maintaining a consistent API. The architecture is based on the **Component Factory** pattern and uses **multi-threading** to ensure responsive input handling.

**Important:** View subclasses should not access `Application.Driver`. Use the View APIs instead:
- `View.Move(col, row)` for positioning
- `View.AddRune()` and `View.AddStr()` for drawing
- `View.App.Screen` for screen dimensions
 
## Available Drivers

Terminal.Gui provides console driver implementations optimized for different platforms:

- **DotNetDriver (`dotnet`)** - A cross-platform driver that uses the .NET `System.Console` API. Works on all platforms (Windows, macOS, Linux). Best for maximum compatibility.
- **WindowsDriver (`windows`)** - A Windows-optimized driver that uses native Windows Console APIs for enhanced performance and platform-specific features.
- **UnixDriver (`unix`)** - A Unix/Linux/macOS-optimized driver that uses platform-specific APIs for better integration and performance.
- **AnsiDriver (`ansi`)** - A pure ANSI escape sequence driver for unit testing and headless environments. Simulates console behavior without requiring a real terminal.

### Automatic Driver Selection

The appropriate driver is automatically selected based on the platform when `Application.Init()` is called:

- **Windows** (Win32NT, Win32S, Win32Windows) → `WindowsDriver`
- **Unix/Linux/macOS** → `UnixDriver`

### Explicit Driver Selection

Explicitly specify a driver in several ways:

Method 1: Set ForceDriver using Configuration Manager

```json
{
  "Application.ForceDriver": "ansi"
}
```

Method 2: Pass driver name to Init

```csharp
// Using string directly
Application.Init(driverName: "unix");

// Or using type-safe constant
Application.Init(driverName: DriverRegistry.Names.UNIX);
```

Method 3: Set ForceDriver on instance

```csharp
using Terminal.Gui.Drivers;
using (IApplication app = Application.Create())
{
    app.ForceDriver = DriverRegistry.Names.ANSI;
    app.Init();
}
```

### ForceDriver as Configuration Property

The `ForceDriver` property is a configuration property marked with `[ConfigurationProperty]`, which means:

- It can be set through the configuration system (e.g., `config.json`)
- Changes raise the `ForceDriverChanged` event
- It persists across application instances when using the static `Application` class

```csharp
// Subscribe to driver changes
Application.ForceDriverChanged += (sender, e) =>
{
    Console.WriteLine($"Driver changed: {e.OldValue} → {e.NewValue}");
};

// Change driver
Application.ForceDriver = DriverRegistry.Names.ANSI;
```

### Discovering Available Drivers

Terminal.Gui provides several methods to discover available drivers at runtime through the **Driver Registry**:

```csharp
// Get driver names (AOT-friendly, no reflection)
IEnumerable<string> driverNames = Application.GetRegisteredDriverNames();

Console.WriteLine("Available drivers:");
foreach (string name in driverNames)
{
    Console.WriteLine($"  - {name}");
}

// Output:
// Available drivers:
//   - dotnet
//   - windows
//   - unix
//   - ansi
```

For more detailed information about each driver:

```csharp
// Get driver metadata
foreach (var descriptor in Application.GetRegisteredDrivers())
{
    Console.WriteLine($"{descriptor.DisplayName}");
    Console.WriteLine($"  Name: {descriptor.Name}");
    Console.WriteLine($"  Description: {descriptor.Description}");
    Console.WriteLine($"  Platforms: {string.Join(", ", descriptor.SupportedPlatforms)}");
    Console.WriteLine();
}

// Output:
// Windows Console Driver
//   Name: windows
//   Description: Optimized Windows Console API driver with native input handling
//   Platforms: Win32NT, Win32S, Win32Windows
//
// .NET Cross-Platform Driver
//   Name: dotnet
//   Description: Cross-platform driver using System.Console API
//   Platforms: Win32NT, Unix, MacOSX
// ...
```

Validate driver names (useful for CLI argument validation):

```csharp
string userInput = args[0];

if (Application.IsDriverNameValid(userInput))
{
    Application.Init(driverName: userInput);
}
else
{
    Console.WriteLine($"Invalid driver: {userInput}");
    Console.WriteLine($"Valid options: {string.Join(", ", Application.GetRegisteredDriverNames())}");
}
```

Use type-safe constants in code:

```csharp
using Terminal.Gui.Drivers;

// Type-safe driver names from DriverRegistry.Names
string driverName = DriverRegistry.Names.ANSI;  // "ansi"
app.Init(driverName);
```

**Note**: The legacy `GetDriverTypes()` method is now obsolete. Use `GetRegisteredDriverNames()` or `GetRegisteredDrivers()` instead for AOT-friendly, reflection-free driver discovery.

## Architecture

### Driver Registry

Terminal.Gui v2 uses a **Driver Registry** pattern for managing available drivers without reflection. The registry provides:

- **Type-safe driver names** via `DriverRegistry.Names` constants
- **Driver metadata** including display names, descriptions, and supported platforms
- **AOT compatibility** - no reflection, fully ahead-of-time compilation friendly
- **Extensibility** - custom drivers can be registered via `DriverRegistry.Register()`

```csharp
// Access well-known driver name constants
string windowsDriver = DriverRegistry.Names.WINDOWS;  // "windows"
string unixDriver = DriverRegistry.Names.UNIX;        // "unix"
string dotnetDriver = DriverRegistry.Names.DOTNET;    // "dotnet"
string ansiDriver = DriverRegistry.Names.ANSI;        // "ansi"

// Get detailed driver information
if (DriverRegistry.TryGetDriver("windows", out var descriptor))
{
    Console.WriteLine($"Found: {descriptor.DisplayName}");
    Console.WriteLine($"Description: {descriptor.Description}");
    
    // Check if supported on current platform
    bool isSupported = descriptor.SupportedPlatforms.Contains(Environment.OSVersion.Platform);
}

// Get drivers supported on current platform
foreach (var driver in DriverRegistry.GetSupportedDrivers())
{
    Console.WriteLine($"{driver.Name} - {driver.DisplayName}");
}

// Get the default driver for current platform
var defaultDriver = DriverRegistry.GetDefaultDriver();
Console.WriteLine($"Default driver: {defaultDriver.Name}");
```

### Component Factory Pattern

The v2 driver architecture uses the **Component Factory** pattern to create platform-specific components. Each driver has a corresponding factory that implements `IComponentFactory<T>`:

- `NetComponentFactory` - Creates components for DotNetDriver
- `WindowsComponentFactory` - Creates components for WindowsDriver  
- `UnixComponentFactory` - Creates components for UnixDriver
- `AnsiComponentFactory` - Creates components for AnsiDriver

Each factory is responsible for:
- Creating driver-specific components (`IInput<T>`, `IOutput`, `IInputProcessor`, etc.)
- Providing the driver name via `GetDriverName()` (single source of truth for driver identity)
- Being registered in the `DriverRegistry` with metadata

The factory pattern ensures proper component creation and initialization while maintaining clean separation of concerns.

### Core Components

Each driver is composed of specialized components, each with a single responsibility:

#### IInput&lt;T&gt;
Reads raw console input events from the terminal. The generic type `T` represents the platform-specific input type:
- `ConsoleKeyInfo` for DotNetDriver
- `WindowsConsole.InputRecord` for WindowsDriver
- `char` for UnixDriver and AnsiDriver

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
- Uses `IKeyConverter<T>` to translate `TInputRecord` to `Key`:
- `AnsiKeyConverter` - For `char` input (UnixDriver, AnsiDriver)
- `NetKeyConverter` - For `ConsoleKeyInfo` input (DotNetDriver)
- `WindowsKeyConverter` - For `WindowsConsole.InputRecord` input (WindowsDriver)

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

When `Application.Init()` is called:

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

The main driver interface that the framework uses internally. `IDriver` is organized into logical regions:

#### Driver Lifecycle
- `Init()`, `Refresh()`, `End()` - Core lifecycle methods
- `GetName()`, `GetVersionInfo()` - Driver identification
- `Suspend()` - Platform-specific suspend support

#### Driver Components
- `InputProcessor` - Processes input into Terminal.Gui events
- `OutputBuffer` - Manages screen buffer state
- `SizeMonitor` - Detects terminal size changes
- `Clipboard` - OS clipboard integration

#### Screen and Display
- `Screen`, `Cols`, `Rows`, `Left`, `Top` - Screen dimensions
- `SetScreenSize()`, `SizeChanged` - Size management

#### Color Support
- `SupportsTrueColor` - 24-bit color capability
- `Force16Colors` - Force 16-color mode

#### Content Buffer
- `Contents` - Screen buffer array
- `Clip` - Clipping region
- `ClearContents()`, `ClearedContents` - Buffer management

#### Drawing and Rendering
- `Col`, `Row`, `CurrentAttribute` - Drawing state
- `Move()`, `AddRune()`, `AddStr()`, `FillRect()` - Drawing operations
- `SetAttribute()`, `GetAttribute()` - Attribute management
- `WriteRaw()`, `GetSixels()` - Raw output and graphics
- `Refresh()`, `ToString()`, `ToAnsi()` - Output rendering

#### Cursor
- `SetCursorPosition(int col, int row)` - Set cursor position in screen coordinates
- `SetCursorVisibility(CursorStyle style)` - Set cursor style/visibility (ANSI DECSCUSR-based)
- `SetCursorNeedsUpdate(bool needsUpdate)` - Signal cursor position needs update without redraw

> [!NOTE]
> The cursor system is managed by `ApplicationNavigation`. Drivers should not directly manage cursor state.
> See [Cursor Management](cursor.md) for details.

#### Input Events
- `KeyDown`, `MouseEvent` - Input events
- `InjectKeyEvent()` - Test support
- `InjectMouseEvent()` - Test support

#### ANSI Escape Sequences
- `QueueAnsiRequest()` - ANSI request handling

**Note:** The driver is internal to Terminal.Gui. View classes should not access `Driver` directly. Instead:
- Use @Terminal.Gui.Application.Screen to get screen dimensions
- Use @Terminal.Gui.View.Move for positioning (with viewport-relative coordinates)
- Use @Terminal.Gui.View.AddRune and @Terminal.Gui.View.AddStr for drawing
- ViewBase infrastructure classes (in `Terminal.Gui/ViewBase/`) can access Driver when needed for framework implementation

### Driver Creation and Selection

The driver selection logic in `ApplicationImpl.Driver.cs` uses the **Driver Registry** to select and instantiate drivers:

**Selection Priority Order:**

1. **Provided Component Factory**: If an `IComponentFactory` is explicitly provided to `ApplicationImpl`, it determines the driver via `factory.GetDriverName()`
2. **Driver Name Parameter**: The `driverName` parameter passed to `Init()` is looked up in the registry
3. **Application.ForceDriver Configuration**: The `Application.ForceDriver` property is checked and looked up in the registry
4. **Platform Default**: `DriverRegistry.GetDefaultDriver()` selects based on current platform:
   - Windows (Win32NT, Win32S, Win32Windows) → `WindowsDriver`
   - Unix/Linux/macOS → `UnixDriver`
   - Other platforms → `DotNetDriver` (fallback)

**Driver Creation Process:**

```csharp
// Example of how driver creation works internally
DriverRegistry.DriverDescriptor descriptor;

if (DriverRegistry.TryGetDriver(driverName, out descriptor))
{
    // Create factory using descriptor's factory function
    IComponentFactory factory = descriptor.CreateFactory();
    
    // Factory creates all driver components
    var coordinator = new MainLoopCoordinator<TInputRecord>(
        timedEvents,
        inputQueue,
        mainLoop,
        factory  // Factory knows its driver name via GetDriverName()
    );
}
```

This architecture provides:
- **Deterministic behavior** - clear priority order for driver selection
- **Flexibility** - multiple ways to specify a driver
- **Type safety** - use `DriverRegistry.Names` constants instead of strings
- **Extensibility** - custom drivers can register themselves
- **AOT compatibility** - no reflection required

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

- Uses Unix/Linux terminal APIs via P/Invoke to libc
- Input: Reads raw `char` data from stdin using `poll()` and `read()` syscalls
- Output: Writes ANSI escape sequences to stdout using `write()` syscall
- Terminal control: Uses termios for raw mode (via `UnixRawModeHelper`)
- Size detection: Uses `ioctl(TIOCGWINSZ)` to get terminal dimensions
- Automatically selected on Unix/Linux/macOS platforms

### AnsiDriver (AnsiComponentFactory)

- Pure ANSI escape sequence cross-platform driver
- **Windows**: Uses Virtual Terminal Input mode (`ReadFile` API)
- **Unix/Linux/macOS**: Uses the same low-level syscalls as UnixDriver (`poll()`, `read()`) via shared `UnixIOHelper`
- Shares code with UnixDriver:
  - `UnixRawModeHelper` - Terminal raw mode configuration (termios)
  - `UnixIOHelper` - Shared Unix syscall wrappers (poll, read, write, ioctl)
- Best for unit testing, headless environments, and maximum portability
- Specify with `IApplication.ForceDriver = "ansi"` or `DriverRegistry.Names.ANSI`

## Testing and Input Injection

Terminal.Gui provides a sophisticated input injection system for testing applications without requiring actual keyboard/mouse hardware or terminal interaction. The ANSI driver is the **recommended driver for testing** because:

- ✅ **Cross-platform** - Works identically on all platforms
- ✅ **Full pipeline testing** - Tests the complete ANSI encoding/parsing pipeline
- ✅ **Deterministic** - Virtual time control eliminates timing-related test flakiness
- ✅ **Fast** - No real delays needed for escape sequence handling

### Simple Test Example

```csharp
// Create app with virtual time for testing
VirtualTimeProvider time = new ();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);  // Use ANSI driver

Button button = new () { Text = "Click Me" };
bool acceptingCalled = false;
button.Accepting += (s, e) => acceptingCalled = true;

// Single-call injection - handles everything automatically
app.InjectKey(Key.Enter);

Assert.True(acceptingCalled);
```

### Virtual Time Control

The input injection system supports **virtual time** via `VirtualTimeProvider`, enabling deterministic testing of timing-dependent behavior like double-clicks:

```csharp
// Test double-click with precise timing
VirtualTimeProvider time = new ();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

// First click at T+0
app.InjectMouse(new () { 
    Flags = MouseFlags.LeftButtonPressed,
    Position = new (5, 5)
});
app.InjectMouse(new () { 
    Flags = MouseFlags.LeftButtonReleased,
    Position = new (5, 5)
});

// Advance virtual time by 300ms (within double-click threshold)
time.Advance(TimeSpan.FromMilliseconds(300));

// Second click at T+300
app.InjectMouse(new () { 
    Flags = MouseFlags.LeftButtonPressed,
    Position = new (5, 5)
});
app.InjectMouse(new () { 
    Flags = MouseFlags.LeftButtonReleased,
    Position = new (5, 5)
});

// Verify double-click was detected
Assert.Contains(receivedEvents, e => e.Flags.HasFlag(MouseFlags.LeftButtonDoubleClicked));
```

### Input Injection Modes

The input injection system supports two modes:

- **Direct Mode** (default) - Bypasses ANSI encoding/decoding for faster, simpler tests. Input events are raised directly.
- **Pipeline Mode** - Goes through full ANSI encoding → parsing → decoding pipeline. Use when testing ANSI escape sequence handling.

```csharp
// Test ANSI encoding/decoding pipeline
VirtualTimeProvider time = new ();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

InputInjectionOptions options = new () 
{ 
    Mode = InputInjectionMode.Pipeline,
    TimeProvider = time 
};

// This will encode to ANSI, parse back, and raise events
app.InjectKey(Key.F1, options);
```

### Key Concepts

- **Single-call injection** - `app.InjectKey(key)` and `app.InjectMouse(mouse)` handle injection, processing, and event raising in one call
- **Virtual time** - Control time explicitly via `VirtualTimeProvider.Advance()` for deterministic tests
- **No manual queue management** - The old 3-step dance (inject → simulate thread → process queue) is handled automatically
- **Automatic escape handling** - Escape sequences are processed without manual `Thread.Sleep()` delays
