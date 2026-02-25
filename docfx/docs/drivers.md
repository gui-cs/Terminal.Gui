# Cross-Platform Driver Model

## Overview

The driver model is the mechanism by which Terminal.Gui supports multiple platforms. Windows, Mac, Linux, and unit test environments are all supported through a modular, component-based architecture.

Terminal.Gui v2 uses a sophisticated driver architecture that separates concerns and enables platform-specific optimizations while maintaining a consistent API. The architecture is based on the **Component Factory** pattern and uses **multi-threading** to ensure responsive input handling.

**Important:** View subclasses should not access `Application.Driver`. Use the View APIs instead:
- `View.Move(col, row)` for positioning
- `View.AddRune()` and `View.AddStr()` for drawing
- `View.App.Screen` for screen dimensions
 
## Available Drivers

Terminal.Gui provides four console driver implementations optimized for different platforms:


|  | **ansi** | **dotnet** | **unix** | **windows** |
|---|---|---|---|---|
| **Theme** | Showcase driver with pure ANSI implementation. Works on all platforms. Ideal for testing/CI. Deterministic behavior with virtual time support. | Cross-platform managed .NET driver. Simplest implementation using `System.Console` API. Works with .NET BCL only. | Optimized Unix/Linux/macOS driver. Direct syscall access. | High-performance Windows-only driver. Native Win32 Console API. Direct access to Windows-specific features. |
| **Input Model** | Reads raw ANSI sequences, parses to Terminal.Gui events | Reads `ConsoleKeyInfo` from `System.Console`, converts to Terminal.Gui events | Reads raw ANSI sequences, parses to Terminal.Gui events | Reads `INPUT_RECORD` structures directly, converts to Terminal.Gui events |
| **Unix Read APIs** | `poll(STDIN_FILENO, ...)`, `read(STDIN_FILENO, buffer, len)`, `tcgetattr()`/`tcsetattr()` for raw mode via `UnixRawModeHelper` | N/A (uses .NET `Console.ReadKey()` which internally delegates to platform APIs) reads `char` | `poll()`, `read()` syscalls on stdin (fd 0), `tcgetattr()`/`tcsetattr()` for termios raw mode | N/A (Windows-only) |
| **Windows Read APIs** | P/Invokes `ReadFile()` reads `char` | N/A (uses .NET `Console.ReadKey()` which internally delegates to platform APIs) | N/A (Unix-only) | P/Invokes `ReadConsoleInputW()` reads `INPUT_RECORD`, `GetConsoleMode()`/`SetConsoleMode()` enables mouse input and raw mode |
| **Output Model** | Pure ANSI escape sequences | Managed .NET + ANSI sequences (when VT mode enabled) | Pure ANSI escape sequences | Direct character output via Win32 API with double buffering |
| **Unix Write APIs** | `write()` syscall to stdout (fd 1) | N/A (uses .NET `Console.Write()` which internally delegates to platform APIs) | `write()` syscall to stdout, ANSI SGR sequences for colors (16-color and 24-bit RGB) | N/A (Windows-only) |
| **Windows Write APIs** | P/Invokes `WriteFile()` | N/A (uses .NET `Console.Write()` which internally delegates to platform APIs) | N/A (Unix-only) | P/Invokes `WriteConsoleW()`, `CreateConsoleScreenBuffer()`/`SetConsoleActiveScreenBuffer()` for double buffering, `SetConsoleTextAttribute()` |
| **Screen Model** | ANSI query-based resize. Throttled to 500ms. Falls back to polling. | Polling-based re-size: `Console.WindowWidth`/`Console.WindowHeight` queried periodically. Falls back to 80x25 on `IOException`. | Polling-based resize: `ioctl(TIOCGWINSZ)` syscall with platform-specific constants (Linux `0x5413`, macOS `0x40087468`). Queries `WinSize` struct. | Event-based re-size: `WINDOW_BUFFER_SIZE_EVENT` received in input stream via `ReadConsoleInputW()`. Immediate resize notification. `GetConsoleScreenBufferInfoEx()` queries dimensions. |
| **Cursor Handling** | ANSI sequences: DECTCEM (`CSI ? 25 h/l`) for show/hide, DECSCUSR (`CSI Ps SP q`) for style. Full `CursorStyle` support. | ANSI sequences (same as ansi driver). Falls back to `Console.SetCursorPosition()` on Windows. | ANSI sequences (same as ansi driver). Full `CursorStyle` support. | • Legacy mode: Win32 `CONSOLE_CURSOR_INFO` (size percentage only, no blinking control). <br>• Modern VT mode: ANSI sequences (same as ansi driver). Full `CursorStyle` support. |
| **Advantages** | • Cross-platform (all platforms)<br>• Pure, clean implementation<br>• Perfect for testing/CI<br>• Virtual time support<br>• Deterministic behavior<br> | • Cross-platform (all platforms)<br>• Maximum compatibility<br>• Simple implementation<br>• No P/Invoke; Works with .NET BCL<br> | • Immediate resize detection<br> | • Highest performance on Windows<br>• Immediate resize detection<br> |
| **Disadvantages** | • Requires proper ANSI support<br>• Throttled size detection (500ms) | • Lower performance (managed overhead)<br>• Limited feature set<br>• `System.ReadKey` has bugs on Windows<br>• Polling-based resize | • Unix-only<br>• Polling-based resize detection | • Windows-only<br>• More complex P/Invoke code |

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
// Use type-safe constants from DriverRegistry.Names
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
Application.ForceDriverChanged += (_, e) =>
{
    Logging.Information($"Driver changed: {e.OldValue} → {e.NewValue}");
};

// Change driver
Application.ForceDriver = DriverRegistry.Names.ANSI;
```

### Discovering Available Drivers

Terminal.Gui provides several methods to discover available drivers at runtime through the **Driver Registry**:

```csharp
// Get driver names (AOT-friendly, no reflection)
IEnumerable<string> driverNames = Application.GetRegisteredDriverNames();

Logging.Information("Available drivers:");
foreach (string name in driverNames)
{
    Logging.Information($"  - {name}");
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
foreach (DriverRegistry.DriverDescriptor descriptor in Application.GetRegisteredDrivers())
{
    Logging.Information($"{descriptor.DisplayName}");
    Logging.Information($"  Name: {descriptor.Name}");
    Logging.Information($"  Description: {descriptor.Description}");
    Logging.Information($"  Platforms: {string.Join(", ", descriptor.SupportedPlatforms)}");
    Logging.Information("");
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
    Logging.Information($"Invalid driver: {userInput}");
    Logging.Information($"Valid options: {string.Join(", ", Application.GetRegisteredDriverNames())}");
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
if (DriverRegistry.TryGetDriver(DriverRegistry.Names.WINDOWS, out DriverRegistry.DriverDescriptor descriptor))
{
    Logging.Information($"Found: {descriptor.DisplayName}");
    Logging.Information($"Description: {descriptor.Description}");
    
    // Check if supported on current platform
    bool isSupported = descriptor.SupportedPlatforms.Contains(Environment.OSVersion.Platform);
}

// Get drivers supported on current platform
foreach (DriverRegistry.DriverDescriptor driver in DriverRegistry.GetSupportedDrivers())
{
    Logging.Information($"{driver.Name} - {driver.DisplayName}");
}

// Get the default driver for current platform
DriverRegistry.DriverDescriptor defaultDriver = DriverRegistry.GetDefaultDriver();
Logging.Information($"Default driver: {defaultDriver.Name}");
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
Reads raw console input events from the terminal on a dedicated input thread. The generic type `T` represents the platform-specific input record type:
- `ConsoleKeyInfo` for DotNetDriver (from `Console.ReadKey()`)
- `WindowsConsole.InputRecord` for WindowsDriver (from `ReadConsoleInputW()`)
- `char` for UnixDriver and AnsiDriver (raw bytes from `read()` syscall or `ReadFile()`)

Input runs on a separate thread managed by `MainLoopCoordinator`, continuously reading from the console and queueing events into a thread-safe `ConcurrentQueue<T>` to avoid blocking the UI thread.

#### IOutput
Renders the output buffer to the terminal. Platform-specific implementations:
- **WindowsOutput**: Uses `WriteConsoleW()` for direct character output
- **UnixOutput**: Writes ANSI sequences to stdout via `write()` syscall
- **NetOutput**: Uses `Console.Write()` with ANSI sequences (VT mode on Windows)
- **AnsiOutput**: Pure ANSI escape sequences via `WriteFile()` (Windows) or `write()` (Unix)

Responsibilities include:
- Writing characters, strings, and ANSI escape sequences
- Cursor positioning and visibility control
- Querying terminal window size
- Managing the active screen buffer

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
- `DefaultAttribute` - The terminal's actual default foreground/background colors, detected at startup via OSC 10/11 queries. Used by `Scheme` to resolve `Color.None` during role derivation. `null` if the terminal didn't respond (e.g., legacy console).
- `ColorCapabilities` - The terminal's color capability level (`NoColor`, `Colors16`, `Colors256`, `TrueColor`), detected from `$TERM`, `$COLORTERM`, and other environment variables

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

Drivers implement cursor control through `IDriver` which delegates to `IOutput`:
- `SetCursor(Cursor cursor)` - Set cursor position and style atomically
- `GetCursor()` - Get current cursor state
- `SetCursorNeedsUpdate(bool)` / `GetCursorNeedsUpdate()` - Optimization flag for cursor updates

> [!NOTE]
> The cursor system is managed by `ApplicationNavigation`. Drivers implement the low-level cursor control; views use the `View.Cursor` property.
> See [Cursor Management](cursor.md) for complete details.

#### Input Events
- `KeyDown`, `MouseEvent` - Input events raised by the driver when input is processed

> [!NOTE]
> For testing, use the input injection API. See [Input Injection](input-injection.md) for details.

#### ANSI Escape Sequences
- `QueueAnsiRequest()` - ANSI request handling

**Note:** The driver is internal to Terminal.Gui. View classes should not access `Driver` directly. Instead:
- Use <xref:Terminal.Gui.Application.Screen> to get screen dimensions
- Use <xref:Terminal.Gui.View.Move> for positioning (with viewport-relative coordinates)
- Use <xref:Terminal.Gui.View.AddRune> and <xref:Terminal.Gui.View.AddStr> for drawing
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
    MainLoopCoordinator<TInputRecord> coordinator = new (
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

## Testing and Input Injection

For comprehensive documentation on testing Terminal.Gui applications with input injection, virtual time control, and deterministic testing, see [Input Injection](input-injection.md).

**Quick Summary:**

- Use the **ANSI driver** for testing - it's cross-platform and deterministic
- Use **Virtual Time** (`VirtualTimeProvider`) for timing control
- The default **Direct Mode** is fastest for most tests
- Use **Pipeline Mode** only when testing ANSI encoding/parsing

**Example:**

```csharp
VirtualTimeProvider time = new ();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

// Inject input
app.InjectKey(Key.Enter);
app.InjectMouse(new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (5, 5) });

// Verify behavior
Assert.True(eventFired);
```

See [Input Injection](input-injection.md) for:
- Complete API documentation
- Testing patterns and best practices
- Virtual time control details
- Injection modes (Direct vs Pipeline)
- Troubleshooting guide
