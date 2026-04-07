Terminal.Gui is a cross-platform UI toolkit for building sophisticated terminal UI (TUI) applications on Windows, macOS, and Linux/Unix.

![Sample app](~/images/sample.gif)

## Quick Start

Install the [Terminal.Gui.Templates](https://github.com/gui-cs/Terminal.Gui.templates), create a new TUI app, and run it:

```powershell
dotnet new install Terminal.Gui.Templates
dotnet new tui-simple -n myproj
cd myproj
dotnet run
```

## Simple Example

```csharp
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using IApplication app = Application.Create ();
app.Init ();

using Window window = new () { Title = "Hello World (Esc to quit)" };
Label label = new ()
{
    Text = "Hello, Terminal.Gui v2!",
    X = Pos.Center (),
    Y = Pos.Center ()
};
window.Add (label);

app.Run (window);
```

See the [Examples](https://github.com/gui-cs/Terminal.Gui/tree/develop/Examples) directory for more.

## Build Powerful Terminal Applications

Terminal.Gui enables building sophisticated console applications with modern UIs:

- **Rich Forms and Dialogs** - Text fields, buttons, checkboxes, radio buttons, and data validation
- **Interactive Data Views** - Tables, lists, and trees with sorting, filtering, and in-place editing  
- **Visualizations** - Charts, graphs, progress indicators, and color pickers with TrueColor support
- **Text Editors** - Full-featured text editing with clipboard, undo/redo, and Unicode support
- **File Management** - File and directory browsers with search and filtering
- **Wizards and Multi-Step Processes** - Guided workflows with navigation and validation
- **System Monitoring Tools** - Real-time dashboards with scrollable, resizable views
- **Configuration UIs** - Settings editors with persistent themes and user preferences
- **Cross-Platform CLI Tools** - Consistent experience on Windows, macOS, and Linux
- **Server Management Interfaces** - SSH-compatible UIs for remote administration

## Key Features

* **[Dozens of Built-in Views](~/docs/views.md)** - Rich set of controls for building complex user interfaces

* **[Cross Platform](~/docs/drivers.md)** - Windows, Mac, and Linux with terminal drivers that work on color and monochrome terminals, including over SSH

* **[Powerful Layout Engine](~/docs/layout.md)** - Relative positioning, automatic sizing, and dynamic terminal UIs

* **[Keyboard](~/docs/keyboard.md) and [Mouse](~/docs/mouse.md) Input** - Complete input handling with simple event-based API

* **[Configuration System](~/docs/config.md)** - Machine, user, and app-level settings with themes and key bindings

* **[Clipboard Support](~/api/Terminal.Gui.App.Clipboard.yml)** - Cut, Copy, and Paste across platforms

* **[Multi-tasking](~/docs/multitasking.md)** - Event processing, idle handlers, timers, and thread-safe classes

* **[Reactive Extensions](https://github.com/dotnet/reactive)** - MVVM pattern support with ReactiveUI data bindings

## Installing

### v2 Alpha (Recommended for new projects)

```powershell
dotnet add package Terminal.Gui --version "2.0.0-alpha.*"
```

### v2 Develop (Latest)

```powershell
dotnet add package Terminal.Gui --version "2.0.0-develop.*"
```

Or use the [Terminal.Gui.Templates](https://github.com/gui-cs/Terminal.Gui.templates):

```powershell
dotnet new install Terminal.Gui.Templates
```
