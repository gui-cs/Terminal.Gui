# Terminal.Gui Project

**Terminal.Gui** is a cross-platform UI toolkit for creating console-based graphical user interfaces in .NET. This repository contains all files required to build the **Terminal.Gui** library and NuGet package, enabling developers to create rich terminal applications with ease.

## Project Overview

**Terminal.Gui** provides a comprehensive framework for building interactive console applications with support for keyboard and mouse input, customizable views, and a robust event system. It is designed to work across Windows, macOS, and Linux, leveraging platform-specific console capabilities where available.

## Project Folder Structure

- `\` - The root folder contains the core solution and project files for the library.
	- `Terminal.Gui.sln` - The Visual Studio solution file for the project.
	- `Terminal.Gui.csproj` - The project file defining build configurations and dependencies.
	- `App\` - Contains the core `Application` logic, including `Application.cs`, a `static` class that serves as the base 'application engine', managing `RunState` and `MainLoop`.

- `Configuration\` - Classes related to the `ConfigurationManager` for handling application settings.

- `Drivers\` - Contains the console driver implementations:
	- `IConsoleDriver.cs` - Defines the Console Driver API.
	- Driver implementations for .NET (`NetDriver`), Unix & macOS (`UnixDriver`), and Windows (`WindowsDriver`).

- `Drawing\` - Classes related to rendering graphical elements in the console.

- `FileServices\` - Utility classes for file operations and services.

- `Input\` - Classes handling keyboard and mouse input:
	- `Events.cs` - Defines structs and classes for keyboard and mouse events.

- `Resources\` - Assets and resources used by the library.

- `Text\` - Classes for text processing and formatting.

- `View\` - Core `View` class hierarchy (excluding specific sub-classes):
	- `View.cs` - The base class for non-modal visual elements such as controls.
	- Related subdirectories for layout and positioning logic.

- `ViewBase\` - Base classes and utilities for views.

- `Views\` - Specific sub-classes of `View`:
	- `Toplevel` - Base class for modal visual elements like top-level windows and dialogs, supporting `MenuBar` and `StatusBar`.
	- `Window` - Implements framed top-level views with titles.
	- `Dialog` - Specialized windows for user interaction.
	- Other specialized view classes.

## Showcase

See the [Showcase](docs/showcase.md) to find independent applications and examples built with Terminal.Gui.

## Getting Started

For instructions on how to start using **Terminal.Gui**, refer to the [Getting Started Guide](https://gui-cs.github.io/Terminal.Gui/docs/getting-started.html) in our documentation.

## Documentation

Comprehensive documentation for **Terminal.Gui** is available at [gui-cs.github.io/Terminal.Gui](https://gui-cs.github.io/Terminal.Gui). Key resources include:
- [Events Deep Dive](https://gui-cs.github.io/Terminal.Gui/docs/events.html) - Detailed guide on event handling and the Cancellable Work Pattern.
- [View Documentation](https://gui-cs.github.io/Terminal.Gui/docs/View.html) - Information on creating and customizing views.
- [Keyboard Handling](https://gui-cs.github.io/Terminal.Gui/docs/keyboard.html) - Guide to managing keyboard input.
- [Mouse Support](https://gui-cs.github.io/Terminal.Gui/docs/mouse.html) - Details on implementing mouse interactions.
- [Showcase](https://gui-cs.github.io/Terminal.Gui/docs/showcase.html) - A collection of applications and examples built with Terminal.Gui.

For information on generating and updating the API documentation locally, refer to the [DocFX README](../docfx/README.md) in the `docfx` folder.

## Versioning

Version information for Terminal.Gui is managed by [gitversion](https://gitversion.net). To install `gitversion`:

```powershell
dotnet tool install --global GitVersion.Tool
dotnet-gitversion
```

The project version (used in the NuGet package and `Terminal.Gui.dll`) is determined from the latest `git tag`. The format of version numbers is `major.minor.patch.build.height` and follows [Semantic Versioning](https://semver.org/) rules.

To define a new version, tag a commit using `git tag`:

```powershell
git tag v1.3.4-beta.5 -a -m "Release v1.3.4 Beta 5"
dotnet-gitversion /updateprojectfiles
dotnet build -c Release
```

**DO NOT COMMIT AFTER USING `/updateprojectfiles`!** Doing so will update the `.csproj` files in your branch with version info, which we do not want.

## Publishing a Release of Terminal.Gui

To release a new version, follow these steps based on [Semantic Versioning](https://semver.org/) rules:

- **MAJOR** version for incompatible API changes.
- **MINOR** version for backwards-compatible functionality additions.
- **PATCH** version for backwards-compatible bug fixes.

### Steps for Release:

1. **Verify the `develop` branch is ready for release**:
	- Ensure all changes are committed and pushed to the `develop` branch.
	- Ensure your local `develop` branch is up-to-date with `upstream/develop`.

2. **Create a pull request for the release in the `develop` branch**:
	- Title the PR as "Release vX.Y.Z".
	```powershell
	git checkout develop
	git pull upstream develop
	git checkout -b vX_Y_Z
	git add .
	git commit -m "Release vX.Y.Z"
	git push
	```
	- Go to the link printed by `git push` and fill out the Pull Request.

3. **On github.com, verify the build action worked on your fork, then merge the PR**.

4. **Pull the merged `develop` from `upstream`**:
	```powershell
	git checkout develop
	git pull upstream develop
	```

5. **Merge `develop` into `main`**:
	```powershell
	git checkout main
	git pull upstream main
	git merge develop
	```
	- Fix any merge errors.

6. **Create a new annotated tag for the release on `main`**:
	```powershell
	git tag vX.Y.Z -a -m "Release vX.Y.Z"
	```

7. **Push the new tag to `main` on `upstream`**:
	```powershell
	git push --atomic upstream main vX.Y.Z
	```

8. **Monitor Github Actions to ensure the NuGet publishing worked**:
	- Check [GitHub Actions](https://github.com/gui-cs/Terminal.Gui/actions).

9. **Check NuGet to see the new package version (wait a few minutes)**:
	- Visit [NuGet Package](https://www.nuget.org/packages/Terminal.Gui).

10. **Add a new Release in Github**:
	- Go to [GitHub Releases](https://github.com/gui-cs/Terminal.Gui/releases) and generate release notes with the list of PRs since the last release.

11. **Update the `develop` branch with the new version**:
	```powershell
	git checkout develop
	git pull upstream develop
	git merge main
	git push upstream develop
	```

## NuGet

The official NuGet package for Terminal.Gui is available at [https://www.nuget.org/packages/Terminal.Gui](https://www.nuget.org/packages/Terminal.Gui). When a new version tag is defined and merged into `main`, a NuGet package is automatically generated by a GitHub Action. Pre-release versions (e.g., `1.3.4-beta.5`) are tagged as pre-release on NuGet.

## Contributing

We welcome contributions from the community. For detailed guidelines on how to contribute, including coding style, unit tests, and pull request processes, please refer to [CONTRIBUTING.md](https://github.com/gui-cs/Terminal.Gui/blob/master/CONTRIBUTING.md).
