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

You can explicitly specify a driver in several ways:

Method 1: Set ForceDriver using Configuration Manager

```json
{
  "ForceDriver": "fake"
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
    app.ForceDriver = DriverRegistry.Names.FAKE;
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
Application.ForceDriver = DriverRegistry.Names.FAKE;
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
//   - fake
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
string driverName = DriverRegistry.Names.FAKE;  // "fake"
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
string fakeDriver = DriverRegistry.Names.FAKE;        // "fake"

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
- `FakeComponentFactory` - Creates components for FakeDriver

Each factory is responsible for:
- Creating driver-specific components (`IInput<T>`, `IOutput`, `IInputProcessor`, etc.)
- Providing the driver name via `GetDriverName()` (single source of truth for driver identity)
- Being registered in the `DriverRegistry` with metadata

The factory pattern ensures proper component creation and initialization while maintaining clean separation of concerns.

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
- Uses `IKeyConverter<T>` to translate `TInputRecord` to `Key`:
  - `AnsiKeyConverter` - For `char` input (UnixDriver, FakeDriver)
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
- `UpdateCursor()` - Position cursor
- `GetCursorVisibility()`, `SetCursorVisibility()` - Visibility management

#### Input Events
- `KeyDown`, `KeyUp`, `MouseEvent` - Input events
- `EnqueueKeyEvent()` - Test support

#### ANSI Escape Sequences
- `QueueAnsiRequest()` - ANSI request handling

**Note:** The driver is internal to Terminal.Gui. View classes should not access `Driver` directly. Instead:
- Use @Terminal.Gui.App.Application.Screen to get screen dimensions
- Use @Terminal.Gui.ViewBase.View.Move for positioning (with viewport-relative coordinates)
- Use @Terminal.Gui.ViewBase.View.AddRune and @Terminal.Gui.ViewBase.View.AddStr for drawing
- ViewBase infrastructure classes (in `Terminal.Gui/ViewBase/`) can access Driver when needed for framework implementation

### Driver Creation and Selection

The driver selection logic in `ApplicationImpl.Driver.cs` uses the **Driver Registry** to select and instantiate drivers:

**Selection Priority Order:**

1. **Provided Component Factory**: If an `IComponentFactory` is explicitly provided to `ApplicationImpl`, it determines the driver via `factory.GetDriverName()`
2. **Driver Name Parameter**: The `driverName` parameter passed to `Init()` is looked up in the registry
3. **ForceDriver Configuration**: The `ForceDriver` property is checked and looked up in the registry
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

## Extending the Driver System

### Registering Custom Drivers

The Driver Registry is extensible, allowing you to register custom drivers:

```csharp
// Create a custom driver descriptor
var customDescriptor = new DriverRegistry.DriverDescriptor(
    Name: "custom",
    DisplayName: "Custom Terminal Driver",
    Description: "My custom driver implementation",
    SupportedPlatforms: new[] { PlatformID.Unix, PlatformID.Win32NT },
    CreateFactory: () => new MyCustomComponentFactory()
);

// Register it before calling Application.Init()
DriverRegistry.Register(customDescriptor);

// Now you can use it
Application.Init(driverName: "custom");
```

Your custom factory must implement `IComponentFactory<T>`:

```csharp
public class MyCustomComponentFactory : ComponentFactoryImpl<char>
{
    public override string GetDriverName() => "custom";
    
    public override IInput<char> CreateInput()
    {
        return new MyCustomInput();
    }
    
    public override IInputProcessor CreateInputProcessor(ConcurrentQueue<char> inputBuffer)
    {
        return new MyCustomInputProcessor(inputBuffer);
    }
    
    public override IOutput CreateOutput()
    {
        return new MyCustomOutput();
    }
}
```

### Driver Development Guidelines

When creating custom drivers:

1. **Choose the right input type** for your `TInputRecord`:
   - `char` for character-based ANSI terminals (like UnixDriver, FakeDriver)
   - `ConsoleKeyInfo` for .NET Console API (like DotNetDriver)
   - `WindowsConsole.InputRecord` for Windows Console API (like WindowsDriver)
   - Custom struct for specialized input handling

2. **Implement all required components**:
   - `IInput<T>` - Reads from your terminal/console
   - `IOutput` - Writes to your terminal/console
   - `IInputProcessor` - Converts `T` to Terminal.Gui `Key` and `Mouse` events
   - `IKeyConverter<T>` - Translates your `TInputRecord` to/from `Key`
   - `ISizeMonitor` - Detects terminal size changes (or use `SizeMonitorImpl`)

3. **Implement a key converter**:
   ```csharp
   public class MyKeyConverter : IKeyConverter<char>
   {
       public Key ToKey(char input)
       {
           // Convert char to Key
           ConsoleKeyInfo keyInfo = EscSeqUtils.MapChar(input);
           return EscSeqUtils.MapKey(keyInfo);
       }
       
       public char ToKeyInfo(Key key)
       {
           // Convert Key back to char (for test injection)
           ConsoleKeyInfo keyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode(key.KeyCode);
           return keyInfo.KeyChar;
       }
   }
   ```
   
   For ANSI-based terminals, reuse `AnsiKeyConverter`. For .NET Console, reuse `NetKeyConverter`.

4. **Follow the threading model**:
   - `IInput.Run()` runs on a dedicated input thread
   - `IInputProcessor` processes on the main UI thread
   - Use `ConcurrentQueue<T>` for thread-safe communication

5. **Handle CI/CD environments gracefully**:
   - Detect when running without a real terminal (e.g., GitHub Actions)
   - Provide fallback behavior (return empty input, default size, etc.)
   - Log warnings but don't throw exceptions

6. **Register with meaningful metadata**:
   - Provide clear `DisplayName` and `Description`
   - List actual `SupportedPlatforms`
   - Use lowercase name consistent with built-in drivers


## See Also

- @Terminal.Gui.Drivers - API Reference
- @Terminal.Gui.App.IApplication - Application interface
- @Terminal.Gui.App.MainLoopCoordinator`1 - Main loop coordination
