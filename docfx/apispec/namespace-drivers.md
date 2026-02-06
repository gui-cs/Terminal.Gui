---
uid: Terminal.Gui.Drivers
summary: The `Drivers` namespace provides cross-platform terminal abstraction and console driver implementations.
---

@Terminal.Gui.Drivers contains the platform abstraction layer that enables Terminal.Gui to run consistently across Windows, macOS, and Linux/Unix systems. This namespace includes console drivers, component factories, input/output processors, and platform-specific optimizations.

The driver system handles low-level terminal operations including cursor management, color support detection, input processing, screen buffer management, and terminal size monitoring. It provides a unified API through `IConsoleDriver` while accommodating the unique characteristics of different terminal environments.

## Architecture Overview

Terminal.Gui v2 uses a modular driver architecture based on the **Component Factory** pattern:

- **IComponentFactory<T>**: Factory interface that creates driver-specific components
- **ConsoleDriverFacade<T>**: Unified facade implementing `IConsoleDriver` and `IConsoleDriverFacade`
- **IConsoleInput<T>**: Reads raw console input events on a separate thread
- **IConsoleOutput**: Renders the output buffer to the terminal
- **IInputProcessor**: Translates raw input into Terminal.Gui events (`Key`, `MouseEventArgs`)
- **IOutputBuffer**: Manages the screen buffer and drawing operations
- **IWindowSizeMonitor**: Detects and reports terminal size changes

## Available Drivers

Terminal.Gui provides three console driver implementations optimized for different platforms:

- **DotNetDriver** (`dotnet`, `NetComponentFactory`) - Cross-platform driver using .NET `System.Console` API. Works on all platforms.
- **WindowsDriver** (`windows`, `WindowsComponentFactory`) - Windows-optimized driver using Windows Console APIs for enhanced performance and features.
- **UnixDriver** (`unix`, `UnixComponentFactory`) - Unix/Linux/macOS-optimized driver using platform-specific APIs.
- **FakeDriver** (`fake`, `FakeComponentFactory`) - Mock driver for unit testing.

The appropriate driver is automatically selected based on the platform. Windows defaults to `WindowsDriver`, while Unix-based systems default to `UnixDriver`.

## Example Usage

```csharp
// Driver selection is typically automatic
Application.Init();

// Access current driver
IConsoleDriver driver = Application.Driver;
bool supportsColors = driver.SupportsTrueColor;

// Explicitly specify a driver
Application.ForceDriver = "dotnet";
Application.Init();

// Or pass driver name to Init
Application.Init(driverName: "unix");
```

## Threading Model

The driver architecture uses a multi-threaded design:

- **Input Thread**: Asynchronously reads console input without blocking the UI
- **Main UI Thread**: Processes events, performs layout, and renders output

This separation ensures responsive input handling even during intensive UI operations.

## Deep Dive

See the [Cross-Platform Driver Model](~/docs/drivers.md) for comprehensive details. 