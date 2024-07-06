![Terminal.Gui](https://socialify.git.ci/gui-cs/Terminal.Gui/image?description=1&font=Rokkitt&forks=1&language=1&logo=https%3A%2F%2Fraw.githubusercontent.com%2Fgui-cs%2FTerminal.Gui%2Fdevelop%2Fdocfx%2Fimages%2Flogo.png&name=1&owner=1&pattern=Circuit%20Board&stargazers=1&theme=Auto)
![.NET Core](https://github.com/gui-cs/Terminal.Gui/workflows/.NET%20Core/badge.svg?branch=develop)
![Code scanning - action](https://github.com/gui-cs/Terminal.Gui/workflows/Code%20scanning%20-%20action/badge.svg)
[![Version](https://img.shields.io/nuget/v/Terminal.Gui.svg)](https://www.nuget.org/packages/Terminal.Gui)
![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/migueldeicaza/90ef67a684cb71db1817921a970f8d27/raw/code-coverage.json)
[![Downloads](https://img.shields.io/nuget/dt/Terminal.Gui)](https://www.nuget.org/packages/Terminal.Gui)
[![License](https://img.shields.io/github/license/gui-cs/gui.cs.svg)](LICENSE)
![Bugs](https://img.shields.io/github/issues/gui-cs/gui.cs/bug)

***The current, stable, release of Terminal.Gui is [v1.x](https://www.nuget.org/packages/Terminal.Gui). It is stable, rich, and broadly used. The team is now focused on designing and building a significant upgrade we're referring to as `v2`. Therefore:***
 * *`v1` is now in maintenance mode, meaning we will accept PRs for v1.x (the `develop` branch) only for issues impacting existing functionality.*
 * *All new development happens on the `v2_develop` branch. See the V2 discussion [here](https://github.com/gui-cs/Terminal.GuiV2Docs/discussions/1940).*
 * *Developers are encouraged to continue building on [v1.x](https://www.nuget.org/packages/Terminal.Gui) until we announce `v2` is stable.*

**Terminal.Gui**: A toolkit for building rich console apps for .NET, .NET Core, and Mono that works on Windows, the Mac, and Linux/Unix.

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

* [Getting Started](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/getting-started.html)
* [What's new in v2](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/newinv2.html)
* [API Documentation](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.html)
* [Documentation Home](https://gui-cs.github.io/Terminal.GuiV2Docs)

## Showcase & Examples

* **[UI Catalog](https://github.com/gui-cs/Terminal.GuiV2Docs/tree/master/UICatalog)** - The UI Catalog project provides an easy to use and extend sample illustrating the capabilities of **Terminal.Gui**. Run `dotnet run --project UICatalog` to run the UI Catalog.
* **[C# Example](https://github.com/gui-cs/Terminal.GuiV2Docs/tree/master/Example)** - Run `dotnet run` in the `Example` directory to run the C# Example.
* **[F# Example](https://github.com/gui-cs/Terminal.GuiV2Docs/tree/master/FSharpExample)** - An example showing how to build a Terminal.Gui app using F#.
* **[Reactive Example](https://github.com/gui-cs/Terminal.GuiV2Docs/tree/master/ReactiveExample)** - A sample app that shows how to use `System.Reactive` and `ReactiveUI` with `Terminal.Gui`. The app uses the MVVM architecture that may seem familiar to folks coming from WPF, Xamarin Forms, UWP, Avalonia, or Windows Forms. In this app, we implement the data bindings using ReactiveUI `WhenAnyValue` syntax and [Pharmacist](https://github.com/reactiveui/pharmacist) — a tool that converts all events in a NuGet package into observable wrappers.
* **[CommunityToolkit Example](./CommunityToolkitExample/README.md)** - A example of using the `CommunityToolkit.MVVM` framework's `ObservableObject`, `ObservableProperty`, and `IRecipient<T>` in conjunction with `Microsoft.Extensions.DependencyInjection`.
* **[C# SelfContained](./SelfContained/README.md)** - An example showing how to publish a Terminal.Gui app using C# self-contained single file.
* **[PowerShell's `Out-ConsoleGridView`](https://github.com/PowerShell/GraphicalTools)** - `OCGV` sends the output from a command to an interactive table. 
* **[F7History](https://github.com/gui-cs/F7History)** - Graphical Command History for PowerShell (built on PowerShell's `Out-ConsoleGridView`).
* **[PoshRedisViewer](https://github.com/En3Tho/PoshRedisViewer)** - A compact Redis viewer module for PowerShell written in F#.
* **[PoshDotnetDumpAnalyzeViewer](https://github.com/En3Tho/PoshDotnetDumpAnalyzeViewer)** - dotnet-dump UI module for PowerShell.
* **[TerminalGuiDesigner](https://github.com/tznind/TerminalGuiDesigner)** - Cross platform view designer for building Terminal.Gui applications.

## Contributing

See [CONTRIBUTING.md](https://github.com/gui-cs/Terminal.GuiV2Docs/blob/master/CONTRIBUTING.md).

Debates on architecture and design can be found in Issues tagged with [design](https://github.com/gui-cs/Terminal.GuiV2Docs/issues?q=is%3Aopen+is%3Aissue+label%3Adesign).

## History

See [gui-cs](https://github.com/gui-cs/) for how this project came to be.
