![Terminal.Gui](https://socialify.git.ci/gui-cs/Terminal.Gui/image?description=1&font=Rokkitt&forks=1&language=1&logo=https%3A%2F%2Fraw.githubusercontent.com%2Fgui-cs%2FTerminal.Gui%2Fdevelop%2Fdocfx%2Fimages%2Flogo.png&name=1&owner=1&pattern=Circuit%20Board&stargazers=1&theme=Auto)
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

The above documentation matches the most recent Nuget release from the `v2_develop` branch. Get the [v1 documentation here](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui.html).

See the [`Terminal.Gui/`README](https://github.com/gui-cs/Terminal.Gui/tree/master/Terminal.Gui) for an overview of how the library is structured. 

## Showcase & Examples

**Terminal.Gui** can be used with any .Net language to create feature rich and robust applications.  
[Showcase](https://github.com/gui-cs/Terminal.Gui/blob/develop/Showcase.md) is a place where you can find all kind of projects from simple examples to advanced real world apps that fully utilize capabilities of the toolkit.  
The team is looking forward to seeing new amazing projects made by the community to be added there!

## Sample Usage in C#

The following example shows a basic Terminal.Gui application in C#:

```csharp
// This is a simple example application.  For the full range of functionality
// see the UICatalog project

// A simple Terminal.Gui example in C# - using C# 9.0 Top-level statements

using System;
using Terminal.Gui;

Application.Run<ExampleWindow> ().Dispose ();

// Before the application exits, reset Terminal.Gui for clean shutdown
Application.Shutdown ();

// To see this output on the screen it must be done after shutdown,
// which restores the previous screen.
Console.WriteLine ($@"Username: {ExampleWindow.UserName}");

// Defines a top-level window with border and title
public class ExampleWindow : Window
{
    public static string UserName;

    public ExampleWindow ()
    {
        Title = $"Example App ({Application.QuitKey} to quit)";

        // Create input components and labels
        var usernameLabel = new Label { Text = "Username:" };

        var userNameText = new TextField
        {
            // Position text field adjacent to the label
            X = Pos.Right (usernameLabel) + 1,

            // Fill remaining horizontal space
            Width = Dim.Fill ()
        };

        var passwordLabel = new Label
        {
            Text = "Password:", X = Pos.Left (usernameLabel), Y = Pos.Bottom (usernameLabel) + 1
        };

        var passwordText = new TextField
        {
            Secret = true,

            // align with the text box above
            X = Pos.Left (userNameText),
            Y = Pos.Top (passwordLabel),
            Width = Dim.Fill ()
        };

        // Create login button
        var btnLogin = new Button
        {
            Text = "Login",
            Y = Pos.Bottom (passwordLabel) + 1,

            // center the login button horizontally
            X = Pos.Center (),
            IsDefault = true
        };

        // When login button is clicked display a message popup
        btnLogin.Accept += (s, e) =>
                           {
                               if (userNameText.Text == "admin" && passwordText.Text == "password")
                               {
                                   MessageBox.Query ("Logging In", "Login Successful", "Ok");
                                   UserName = userNameText.Text;
                                   Application.RequestStop ();
                               }
                               else
                               {
                                   MessageBox.ErrorQuery ("Logging In", "Incorrect username or password", "Ok");
                               }
                           };

        // Add the views to the Window
        Add (usernameLabel, userNameText, passwordLabel, passwordText, btnLogin);
    }
}
```

When run the application looks as follows:

![Simple Usage app](./docfx/images/Example.png)

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
