![Terminal.Gui](https://socialify.git.ci/gui-cs/Terminal.Gui/image?description=1&descriptionEditable=Cross%20Platform%20Terminal%20UI%20Toolkit&font=KoHo&forks=1&logo=https%3A%2F%2Fgithub.com%2Fgui-cs%2FTerminal.Gui%2Fblob%2Fv2_develop%2Fdocfx%2Fimages%2Flogo.png%3Fraw%3Dtrue&pattern=Circuit%20Board&stargazers=1&theme=Dark)
![.NET Core](https://github.com/gui-cs/Terminal.Gui/workflows/.NET%20Core/badge.svg?branch=develop)
[![Version](https://img.shields.io/nuget/v/Terminal.Gui.svg)](https://www.nuget.org/packages/Terminal.Gui)
![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/migueldeicaza/90ef67a684cb71db1817921a970f8d27/raw/code-coverage.json)
[![Downloads](https://img.shields.io/nuget/dt/Terminal.Gui)](https://www.nuget.org/packages/Terminal.Gui)
[![License](https://img.shields.io/github/license/gui-cs/gui.cs.svg)](LICENSE)
![Bugs](https://img.shields.io/github/issues/gui-cs/gui.cs/bug)

* The current, stable, release of Terminal.Gui v1 is [![Version](https://img.shields.io/nuget/v/Terminal.Gui.svg)](https://www.nuget.org/packages/Terminal.Gui).
* The current `prealpha` release of Terminal.Gui v2 can be found on [Nuget](https://www.nuget.org/packages/Terminal.Gui).
* Developers starting new TUI projects are encouraged to target `v2`. The API is significantly changed, and significantly improved. There will be breaking changes in the API before Beta, but the core API is stable.
* `v1` is in maintenance mode and we will only accept PRs for issues impacting existing functionality.
 
**Terminal.Gui**: A toolkit for building rich console apps for Windows, the Mac, and Linux/Unix.

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

There is also a [visual designer](https://github.com/gui-cs/TerminalGuiDesigner) (uses Terminal.Gui itself).

## Documentation 

* [Getting Started](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/getting-started.html)
* [What's new in v2](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/newinv2.html)
* [API Documentation](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.html)
* [Documentation Home](https://gui-cs.github.io/Terminal.GuiV2Docs)

The above documentation matches the most recent Nuget release from the `v2_develop` branch. Get the [v1 documentation here](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui.html).

See the [`Terminal.Gui/`README](https://github.com/gui-cs/Terminal.Gui/tree/master/Terminal.Gui) for an overview of how the library is structured. 

## Showcase & Examples

**Terminal.Gui** can be used with any .Net language to create feature rich and robust applications.  
[Showcase](https://github.com/gui-cs/Terminal.Gui/blob/develop/Showcase.md) is a place where you can find all kind of projects from simple examples to advanced real world apps that fully utilize capabilities of the toolkit.  
The team is looking forward to seeing new amazing projects made by the community to be added there!

## Sample Usage in C#

The following example shows a basic Terminal.Gui application in C#:  
[Example (source)](./Example/Example.cs)

When run the application looks as follows:

![Simple Usage app](./docfx/images/Example.png)

## Sample usage in F#  
F# examples are located [here](./FSharpExample/Program.fs)

## Installing

Use NuGet to install the `Terminal.Gui` NuGet package: https://www.nuget.org/packages/Terminal.Gui

### Installation in .NET Core Projects

To install Terminal.Gui into a .NET Core project, use the `dotnet` CLI tool with this command.

```
dotnet add package Terminal.Gui
```

Or, you can use the [Terminal.Gui.Templates](https://github.com/gui-cs/Terminal.Gui.templates).

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md).

Debates on architecture and design can be found in Issues tagged with [design](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Av2+label%3Adesign).

## History

See [gui-cs](https://github.com/gui-cs/) for how this project came to be.
