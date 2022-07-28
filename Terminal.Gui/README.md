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

## Publishing a Release of Terminal.Gui

To release a new version (e.g. with a higher `major`, `minor`, or `patch` value) tag a commit using `git tag` and then push that tag directly to the `main` branch on `github.com/gui-cs/Terminal.Gui`.

The `tag` must be of the form `v<major>.<minor>.<patch>`, e.g. `v1.2.3`.

```powershell
git checkout main
git tag vmajor.minor.patch -a -m "Release vmajor.minor.patch"
git push origin vmajor.minor.patch
```      

`patch` can indicate pre-release or not (e.g. `pre`, `beta`, `rc`, etc...). 

For example, to launch v1.3.4-beta.5 as a Pre-Release nuget package, do the following:
       
```powershell
git tag v1.3.4-beta.5 -a -m "v1.3.4 Beta 5"
git push upstream v1.3.4-beta.5
```

## To launch version v2.3.4 as a new Nuget package do this

### 1) Generate release notes with the list of PRs since the last release 

Use `gh` to get a list with just titles to make it easy to paste into release notes: 

```powershell
gh pr list --limit 500 --search "is:pr is:closed is:merged closed:>=2021-05-18"
```

Use the output to update `./Terminal.Gui/Terminal.Gui.csproj` with latest release notes

### 2) Update the API documentation

See `./docfx/README.md`.

### 3) Create a PR for the release in the `develop` branch

The PR title should be "Release v2.3.4"

```powershell
git checkout develop
git pull -all
git checkout -b v_2_3_4
git add .
git commit -m "Release v2.3.4"
git push
```

### 4) On github.com, verify the build action worked on your fork, then merge the PR

### 5) Pull the merged `develop` from `origin`

```powershell
git pull origin `develop`
```

### 6) Merge `develop` into `main`

```powershell
git checkout main
git merge develop
```

Fix any merge errors.

### 7) Create a new annotated tag for the release

```powershell
git tag v2.3.4 -a -m "Release v2.3.4"
```       

### 8) Push the new tag to `main` on `origin`

```powershell
git push --atomic origin main v2.3.4
```       

*See https://stackoverflow.com/a/3745250/297526*

### 9) Monitor Github actions to ensure the Nuget publishing worked.

### 10) Check Nuget to see the new package version (wait a few minutes): 
https://www.nuget.org/packages/Terminal.Gui

### 11) Add a new Release in Github: https://github.com/gui-cs/Terminal.Gui/releases

### 12) Tweet about it

## Nuget

https://www.nuget.org/packages/Terminal.Gui

When a new version tag is defined and merged into `main`, a Nuget package will be generated by a Github Action.

If the version is pre-release (includes a hyphen, e.g. `1.3.4-beta.5`) the Nuget package will be tagged as pre-release.

Miguel & Tig can hide defunct/old Nuget packages.

## Contributing

See [CONTRIBUTING.md](https://github.com/gui-cs/Terminal.Gui/blob/master/CONTRIBUTING.md).
