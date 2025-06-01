---
uid: Terminal.Gui.Drivers
summary: The `Drivers` namespace provides cross-platform terminal abstraction and console driver implementations.
---

@Terminal.Gui.Drivers contains the platform abstraction layer that enables Terminal.Gui to run consistently across Windows, macOS, and Linux/Unix systems. This namespace includes console drivers, terminal capability detection, and platform-specific optimizations.

The driver system handles low-level terminal operations including cursor management, color support detection, input processing, and screen buffer management. It provides a unified API while accommodating the unique characteristics of different terminal environments.

## Key Components

- **ConsoleDriver**: Base class for platform-specific terminal implementations
- **WindowsDriver**: Windows Console API implementation
- **CursesDriver**: Unix/Linux ncurses-based implementation
- **NetDriver**: Cross-platform .NET Console implementation

## Example Usage

```csharp
// Driver selection is typically automatic
Application.Init();

// Access current driver capabilities
var driver = Application.Driver;
bool supportsColors = driver.SupportsTrueColor;
```

## Deep Dive

See the [Cross-Platform Driver Model](~/docs/drivers.md) for comprehensive details. 