![.NET Core](https://github.com/gui-cs/Terminal.Gui/workflows/.NET%20Core/badge.svg?branch=master)
![Code scanning - action](https://github.com/gui-cs/Terminal.Gui/workflows/Code%20scanning%20-%20action/badge.svg)
[![Version](https://img.shields.io/nuget/v/Terminal.Gui.svg)](https://www.nuget.org/packages/Terminal.Gui)
![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/migueldeicaza/90ef67a684cb71db1817921a970f8d27/raw/code-coverage.json)
[![Downloads](https://img.shields.io/nuget/dt/Terminal.Gui)](https://www.nuget.org/packages/Terminal.Gui)
[![License](https://img.shields.io/github/license/gui-cs/gui.cs.svg)](LICENSE)
![Bugs](https://img.shields.io/github/issues/gui-cs/gui.cs/bug)

# Terminal.Gui - Cross Platform Terminal UI toolkit for .NET

A toolkit for building rich console apps for .NET, .NET Core, and Mono that works on Windows, the Mac, and Linux/Unix.

![Sample app](docfx/images/sample.gif)


## Quick Start

Paste these commands into your favorite terminal on Windows, Mac, or Linux. This will install the [Terminal.Gui.Templates](https://github.com/gui-cs/Terminal.Gui.templates), create a new "Hello World" TUI app, and run it.

(Press `CTRL-Q` to exit the app)

```powershell
dotnet new --install Terminal.Gui.templates
dotnet new tui -n myproj
cd myproj
dotnet run
```

## Documentation 

* [Documentation Home](https://gui-cs.github.io/Terminal.Gui/index.html)
* [Terminal.Gui Overview](https://gui-cs.github.io/Terminal.Gui/articles/overview.html)
* [List of Views/Controls](https://gui-cs.github.io/Terminal.Gui/articles/views.html)
* [Conceptual Documentation](https://gui-cs.github.io/Terminal.Gui/articles/index.html)
* [API Documentation](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.html)

_The Documentation matches the most recent Nuget release from the `main` branch ([![Version](https://img.shields.io/nuget/v/Terminal.Gui.svg)](https://www.nuget.org/packages/Terminal.Gui))_

## Features

* **Cross Platform** - Windows, Mac, and Linux. Terminal drivers for Curses, [Windows Console](https://github.com/gui-cs/Terminal.Gui/issues/27), and the .NET Console mean apps will work well on both color and monochrome terminals. 
* **Keyboard and Mouse Input** - Both keyboard and mouse input are supported, including support for drag & drop.
* **[Flexible Layout](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#layout)** - Supports both *Absolute layout* and an innovative *Computed Layout* system. *Computed Layout* makes it easy to layout controls relative to each other and enables dynamic terminal UIs.
* **Clipboard support** - Cut, Copy, and Paste of text provided through the [`Clipboard`](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.Clipboard.html) class.
* **[Arbitrary Views](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.View.html)** - All visible UI elements are subclasses of the `View` class, and these in turn can contain an arbitrary number of sub-views.
* **Advanced App Features** - The [Mainloop](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.MainLoop.html) supports processing events, idle handlers, timers, and monitoring file
descriptors. Most classes are safe for threading.
* **Reactive Extensions** - Use [reactive extensions](https://github.com/dotnet/reactive) and benefit from increased code readability, and the ability to apply the MVVM pattern and [ReactiveUI](https://www.reactiveui.net/) data bindings. See the [source code](https://github.com/gui-cs/Terminal.Gui/tree/master/ReactiveExample) of a sample app in order to learn how to achieve this.

## Showcase & Examples

* **[UI Catalog](https://github.com/gui-cs/Terminal.Gui/tree/master/UICatalog)** - The UI Catalog project provides an easy to use and extend sample illustrating the capabilities of **Terminal.Gui**. Run `dotnet run --project UICatalog` to run the UI Catalog.
* **[C# Example](https://github.com/gui-cs/Terminal.Gui/tree/master/Example)** - Run `dotnet run` in the `Example` directory to run the C# Example.
* **[F# Example](https://github.com/gui-cs/Terminal.Gui/tree/master/FSharpExample)** - An example showing how to build a Terminal.Gui app using F#.
* **[Reactive Example](https://github.com/gui-cs/Terminal.Gui/tree/master/ReactiveExample)** - A sample app that shows how to use `System.Reactive` and `ReactiveUI` with `Terminal.Gui`. The app uses the MVVM architecture that may seem familiar to folks coming from WPF, Xamarin Forms, UWP, Avalonia, or Windows Forms. In this app, we implement the data bindings using ReactiveUI `WhenAnyValue` syntax and [Pharmacist](https://github.com/reactiveui/pharmacist) — a tool that converts all events in a NuGet package into observable wrappers.
* **[PowerShell's `Out-ConsoleGridView`](https://github.com/PowerShell/GraphicalTools)** - `OCGV` sends the output from a command to  an interactive table. 
* **[PoshRedisViewer](https://github.com/En3Tho/PoshRedisViewer)** - A compact Redis viewer module for PowerShell written in F# and Gui.cs
* **[TerminalGuiDesigner](https://github.com/tznind/TerminalGuiDesigner)** - Cross platform view designer for building Terminal.Gui applications.

See the [`Terminal.Gui/` README](https://github.com/gui-cs/Terminal.Gui/tree/master/Terminal.Gui) for an overview of how the library is structured. The [Conceptual Documentation](https://gui-cs.github.io/Terminal.Gui/articles/index.html) provides insight into core concepts.

## Sample Usage in C#

```csharp
// A simple Terminal.Gui example in C# - using C# 9.0 Top-level statements

using Terminal.Gui;
using NStack;

Application.Init ();

// Creates the top-level window to show
var win = new Window ("Example App") {
	X = 0,
	Y = 1, // Leave one row for the toplevel menu

	// By using Dim.Fill(), this Window will automatically resize without manual intervention
	Width = Dim.Fill (),
	Height = Dim.Fill ()
};

Application.Top.Add (win);

// Creates a menubar, the item "New" has a help menu.
var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_New", "Creates a new file", null),
				new MenuItem ("_Close", "",null),
				new MenuItem ("_Quit", "", () => { if (Quit ()) Application.Top.Running = false; })
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", null),
				new MenuItem ("C_ut", "", null),
				new MenuItem ("_Paste", "", null)
			})
		});
Application.Top.Add (menu);

static bool Quit ()
{
	var n = MessageBox.Query (50, 7, "Quit Example", "Are you sure you want to quit this example?", "Yes", "No");
	return n == 0;
}

var login = new Label ("Login: ") { X = 3, Y = 2 };
var password = new Label ("Password: ") {
	X = Pos.Left (login),
	Y = Pos.Top (login) + 1
};
var loginText = new TextField ("") {
	X = Pos.Right (password),
	Y = Pos.Top (login),
	Width = 40
};
var passText = new TextField ("") {
	Secret = true,
	X = Pos.Left (loginText),
	Y = Pos.Top (password),
	Width = Dim.Width (loginText)
};

// Add the views to the main window, 
win.Add (
	// Using Computed Layout:
	login, password, loginText, passText,

	// Using Absolute Layout:
	new CheckBox (3, 6, "Remember me"),
	new RadioGroup (3, 8, new ustring [] { "_Personal", "_Company" }, 0),
	new Button (3, 14, "Ok"),
	new Button (10, 14, "Cancel"),
	new Label (3, 18, "Press F9 or ESC plus 9 to activate the menubar")
);

// Run blocks until the user quits the application
Application.Run ();

// Always bracket Application.Init with .Shutdown.
Application.Shutdown ();
```

The example above shows adding views using both styles of layout supported by **Terminal.Gui**: **Absolute layout** and **[Computed layout](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#layout)**.

Alternatively, you can encapsulate the app behavior in a new `Window`-derived class, say `App.cs` containing the code above, and simplify your `Main` method to:

```csharp
using Terminal.Gui;

class Demo {
	static void Main ()
	{
		Application.Run<App> ();
		Application.Shutdown ();
	}
}
```

## Installing

Use NuGet to install the `Terminal.Gui` NuGet package: https://www.nuget.org/packages/Terminal.Gui

### Installation in .NET Core Projects

To install Terminal.Gui into a .NET Core project, use the `dotnet` CLI tool with following command.

```
dotnet add package Terminal.Gui
```

Or, you can use the [Terminal.Gui.Templates](https://github.com/gui-cs/Terminal.Gui.templates).

## Building the Library and Running the Examples

* Windows, Mac, and Linux - Build and run using the .NET SDK command line tools (`dotnet build` in the root directory). Run `UICatalog` with `dotnet run --project UICatalog`.
* Windows - Open `Terminal.sln` with Visual Studio 2022.

See [CONTRIBUTING.md](CONTRIBUTING.md) for instructions for downloading and forking the source.

## Contributing

See [CONTRIBUTING.md](https://github.com/gui-cs/Terminal.Gui/blob/master/CONTRIBUTING.md).

Debates on architecture and design can be found in Issues tagged with [design](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Adesign).

## History

See [gui-cs](https://github.com/gui-cs/) for how this project came to be.