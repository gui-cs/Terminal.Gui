# Terminal.Gui Project

Contains all files required to build the **Terminal.Gui** library (and NuGet package).

## Project Folder Structure

- `Terminal.Gui.sln` - The Visual Studio solution
- `Core/` - Source files for all types that comprise the core building blocks of **Terminal-Gui** 
    - `Application` - A `static` class that provides the base 'application driver'. Given it defines a **Terminal.Gui** application it is both logically and literally (because `static`) a singleton. It has direct dependencies on `MainLoop`, `Events.cs` `NetDriver`, `CursesDriver`, `WindowsDriver`, `Responder`, `View`, and `TopLevel` (and nothing else).
    - `MainLoop` - Defines `IMainLoopDriver` and implements the and `MainLoop` class.
    - `ConsoleDriver` - Definition for the Console Driver API.
    - `Events.cs` - Defines keyboard and mouse related structs & classes. 
    - `PosDim.cs` - Implements *Computed Layout* system. These classes have deep dependencies on `View`.
    - `Responder` - Base class for the windowing class hierarchy. Implements support for keyboard & mouse input.
    - `View` - Derived from `Responder`, the base class for non-modal visual elements such as controls.
    - `Toplevel` - Derived from `View`, the base class for modal visual elements such as top-level windows and dialogs. Supports the concept of `MenuBar` and `StatusBar`.
    - `Window` - Derived from `TopLevel`; implements top level views with a visible frame and Title.
- `Types/` - A folder (not namespace) containing implementations of `Point`, `Rect`, and `Size` which are ancient versions of the modern `System.Drawing.Point`, `System.Drawing.Size`, and `System.Drawning.Rectangle`.
- `ConsoleDrivers/` - Source files for the three `ConsoleDriver`-based drivers: .NET: `NetDriver`, Unix & Mac: `UnixDriver`, and Windows: `WindowsDriver`.
- `Views/` - A folder (not namespace) containing the source for all built-in classes that drive from `View` (non-modals). 
- `Windows/` - A folder (not namespace) containing the source all built-in classes that derive from `Window`.

## Version numbers

Version info for Terminal.Gui is managed by [gitversion](https://gitversion.net).

Install `gitversion`:

```powershell
dotnet tool install --global GitVersion.Tool
dotnet-gitversion
```

The project version (the nuget package and in `Terminal.Gui.dll`) is determined from the latest `git tag`. 

The format of version numbers is `vmajor.minor.patch.build.height` and follows the [Semantic Versioning](https://semver.org/) rules.

To define a new version (e.g. with a higher `major`, `minor`, `patch`, or `build` value) tag a commit using `git tag`:

```powershell
git tag v1.3.4-beta.5 -a -m "Release v1.3.4 Beta 5"
dotnet-gitversion /updateprojectfiles
dotnet build -c Release
```

You can see the version in the `UICatalog` about box.

![About Box](https://raw.githubusercontent.com/migueldeicaza/gui.cs/master/docfx/aboutbox.png)

## Publishing a Release of Terminal.Gui

To release a new version (e.g. with a higher `major`, `minor`, or `patch` value) tag a commit using `git tag` and then push that tag directly to the upstream repo.

The `tag` must be of the form `v<major>.<minor>.<patch>`, e.g. `v1.2.3`.

```powershell
git tag vmajor.minor.patch -a -m "Release vmajor.minor.patch"
git push upstream vmajor.minor.patch
```      

`patch` can indicate pre-release or not (e.g. `pre`, `beta`, `rc`, etc...). 

For example, to launch v1.3.4-beta.5 as a Pre-Release nuget package, do the following:
       
```powershell
git tag v1.3.4-beta.5 -a -m "v1.3.4 Beta 5"
git push upstream v1.3.4-beta.5
```

## To launch version 2.3.4 as a Release nuget package do this:

1) Generate release notes with the list of PRs since the last release

Use `gh` to get list with just titles to make it easy to paste into release notes: 

```powershell
gh pr list --limit 500 --search "is:pr is:closed is:merged closed:>=2021-05-18"
```

Use the output to update `./Terminal.Gui/Terminal.Gui.csproj` with latest release notes

2) Update the API documentation

See `./docfx/README.md`.

3) Create a PR for the release

The PR title should be "Release v2.3.4"

```powershell
git add .
git commit -m "Release v2.3.4"
git push
```

4) On github.co, verify the build action worked on your fork, then merge the PR

5) Pull the merged main

```powershell
git pull upstream main
```

6) Create a new tag for the release

```powershell
git tag v2.3.4 -a -m "Release v2.3.4"
```       

7) Push new tag to `main`

```powershell
git push upstream v2.3.4
```       

8) Monitor Github actions to ensure it worked.

9) Check nuget to see new package (wait a few minutes)

https://www.nuget.org/packages/Terminal.Gui

10) Add a new Release in Github: https://github.com/gui-cs/Terminal.Gui/releases

## Nuget

https://www.nuget.org/packages/Terminal.Gui

When a new version tag is defined, and merged into main, a nuget package will be generated.

If the version is pre-release (includes a hyphen, e.g. `1.3.4-beta.5`) the Nuget package will be tagged as pre-release.

Miguel can hide defunct/old nuget packages.

## Contributing

See [CONTRIBUTING.md](https://github.com/gui-cs/Terminal.Gui/blob/master/CONTRIBUTING.md).
