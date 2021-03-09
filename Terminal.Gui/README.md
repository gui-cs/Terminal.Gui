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

To release a new version simply tag a commit

```powershell
git tag vmajor.minor.patch.build.height -a
git push upstream origin vmajor.minor.patch.build.height

```      

`patch` can indicate pre-release or not

e.g: 
       
```powershell
git tag v1.3.4-beta.5 -a
git push upstream v1.3.4-beta.5
```

    or
       
```powershell
git tag v2.3.4.5 -a
git push upstream v2.3.4.5
```       

Then rebuild the project and the version info will be updated.

## Nuget

https://www.nuget.org/packages/Terminal.Gui

When a new version tag is defined, and merged into master, a nuget package will be generated.

If the version is pre-release (includes a hyphen, e.g. `1.3.4-beta.5`) the Nuget package will be tagged as pre-release.

Miguel can hide defunct/old nuget packages.

## Contributing

See [CONTRIBUTING.md](https://github.com/migueldeicaza/gui.cs/blob/master/CONTRIBUTING.md).
