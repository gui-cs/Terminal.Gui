# Terminal.Gui Project

Contains all files required to build the **Terminal.Gui** library (and nuget package).

## Project Folder Structure

- `Terminal.Gui.sln` - The Visual Studio 2019 solution
- `Core/` - Source files for all types that comprise the core building blocks of **Terminal-Gui** 
    - `Application` - A `static` class that provides the base 'application driver'. Given it defines a **Terminal.Gui** application it is both logically and literally (because `static`) a singleton. It has direct dependencies on `MainLoop`, `Events.cs` `NetDriver`, `CursesDriver`, `WindowsDriver`, `Responder`, `View`, and `TopLevel` (and nothing else).
    - `MainLoop` - Defines `IMainLoopDriver` and implements the and `MainLoop` class.
    - `ConsoleDriver` - Definition for the Console Driver API.
    - `Events.cs` - Defines keyboard and mouse related structs & classes. 
    - `PosDim.cs` - Implements **Terminal-Gui's** *Computed Layout* system. These classes have deep dependencies on `View`.
    - `Responder` - Base class for the windowing class hierarchy. Implements support for keyboard & mouse input.
    - `View` - Derived from `Responder`, the base class for non-modal visual elements such as controls.
    - `Toplevel` - Drived from `View`, the base class for modal visual elements such as top-level windows and dialogs. Supports the concept of `MenuBar` and `StatusBar`.
    - `Window` - Drived from `TopLevel`, implements Toplevel views with a visible frame and Title.
- `Types/` - A folder (not namespace) containing implementations of `Point`, `Rect`, and `Size` which are ancient versions of the modern `System.Drawing.Point`, `System.Drawing.Size`, and `System.Drawning.Rectangle`.
- `ConsoleDrivers/` - Source files for the three `ConsoleDriver`-based drivers: .NET: `NetDriver`, Unix & Mac: `UnixDriver`, and Windows: `WindowsDriver`.
- `Views/` - A folder (not namespace) containing the source for all built-in classes that drive from `View` (non-modals). 
- `Windows/` - A folder (not namespace) containing the source all built-in classes that derive from `Window`.

## Version numbers

Version info for Terminal.Gui is managed by MinVer (https://github.com/adamralph/minver).

The project version (the nuget package and in `Terminal.Gui.dlls`) is determined from the latest `git tag`. 

The format of version numbers is `vmajor.minor.patch.build.height` and follows the [Semantic Versioning](https://semver.org/) rules.

To define a new version (e.g. with a higher `major`, `minor`, `patch`, or `build` value) tag a commit using `git tag`:

```powershell
git tag v1.3.4-beta.5 -a -m "v1.3.4 Beta 5"
dotnet build -c Release
```

If the current commit does not have a version tag, another number is added to the pre-release identifiers. This is the number of commits since the latest commit with a version tag or, if no commits have a version tag, since the root commit. This is known as "height". For example, if the latest version tag found is 1.0.0-beta.1, at a height of 42 commits, the calculated version is 1.0.0-beta.1.42.

You can see the version in the `UICatalog` about box or by viewing the "Details" page of the file properties of `/Terminal.Gui/bin/Release/net5.0/Terminal.Gui.dll.

![About Box](https://raw.githubusercontent.com/migueldeicaza/gui.cs/master/docfx/aboutbox.png)

## Publishing a Release of Terminal.Gui

To release a new version (e.g. with a higher `major`, `minor`, or `patch` value) tag a commit using `git tag` and then push that tag directly to the upstream repo:

```powershell
git tag vmajor.minor.patch.build -a -m "Descriptive comment about release"
git push upstream vmajor.minor.patch.build

```      

`patch` can indicate pre-release or not (e.g. `pre`, `beta`, `rc`, etc...). 

For example, to launch v1.3.4-beta.5 as a Pre-Release nuget package, do the following:
       
```powershell
git tag v1.3.4-beta.5 -a -m "v1.3.4 Beta 5"
git push upstream v1.3.4-beta.5
```

## To launch version 2.3.4 as a Release nuget package do this:

1) Create a new tag

```powershell
git tag v2.3.4 -a -m "v2.3.4 Release"
```       

2) Update `./Terminal.Gui/Terminal.Gui.csproj` with latest release notes and submit a PR with a commit of `v2.3.4 Release`

* Use `gh` to get list with just titles to make it easy to paste into release notes: `gh pr list --limit 500 --search "is:pr is:closed is:merged closed:>=2021-05-18"` 
* PR title should be "v2.3.4 Release"

```powershell
git add .
git commit -m "v2.3.4 Release"
git push
```

3) Pull upstream after PR has been merged

```powershell
git pull upstream main
```

4) Push new tag to `main`

```powershell
git tag v2.3.4 -a -m "v2.3.4 Release"
git push upstream v2.3.4
```       

5) Monitor Github actions to ensure it worked.

6) Check nuget to see new package (wait a few minutes)

https://www.nuget.org/packages/Terminal.Gui

7) Add a new Release in Github: https://github.com/migueldeicaza/gui.cs/releases

## Nuget

https://www.nuget.org/packages/Terminal.Gui

When a new version tag is defined, and merged into master, a nuget package will be generated.

If the version is pre-release (includes a hyphen, e.g. `1.3.4-beta.5`) the Nuget package will be tagged as pre-release.

Miguel can hide defunct/old nuget packages.

## Contributing

See [CONTRIBUTING.md](https://github.com/migueldeicaza/gui.cs/blob/master/CONTRIBUTING.md).
