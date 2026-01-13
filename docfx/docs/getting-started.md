# Getting Started

Paste these commands into your favorite terminal on Windows, Mac, or Linux. This will install the [Terminal.Gui.Templates](https://github.com/gui-cs/Terminal.Gui.templates), create a new "Hello World" TUI app, and run it.

(Press `Esc` to exit the app)

```ps1
dotnet new install Terminal.Gui.Templates
dotnet new tui-simple -n myproj
cd myproj
dotnet run
```

## Adding Terminal.Gui to a Project

To install Terminal.Gui from [Nuget](https://www.nuget.org/packages/Terminal.Gui) into a .NET Core project, use the `dotnet` CLI tool with this command.

```ps1
dotnet add package Terminal.Gui
```

## Using the Templates

Use the [Terminal.Gui.Templates](https://github.com/gui-cs/Terminal.Gui.templates):

```ps1
dotnet new install Terminal.Gui.Templates
```

## Sample Usage in C#

The following example shows a basic Terminal.Gui application using the modern instance-based model (this is `./Example/Example.cs`):

[!code-csharp[Program.cs](../../Examples/Example/Example.cs)]

### Key aspects of the modern model:

- Use `Application.Create()` to create an `IApplication` instance
- The application initializes automatically when you call `Run<T>()`  
- Use `app.Run<ExampleWindow>()` to run a window that implements `IRunnable`
- Call `app.Dispose()` to clean up resources and restore the terminal
- Event handling uses `Accepting` event instead of legacy `Accept` event
- Set `e.Handled = true` in event handlers to prevent further processing

When run the application looks as follows:

![Simple Usage app](../images/Example.png)

## Building the Library and Running the Examples

* Windows, Mac, and Linux - Build and run using the .NET SDK command line tools (`dotnet build` in the root directory). Run `UICatalog` with `dotnet run --project UICatalog`.
* Windows - Open `Terminal.sln` with Visual Studio 202x.

