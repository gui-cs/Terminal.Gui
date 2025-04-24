# Terminal.Gui Project

All files required to build the **Terminal.Gui** library (and NuGet package).

## Project Folder Structure

- `Terminal.Gui.sln` - The Visual Studio solution
- `Core/` - Source files for all types that comprise the core building blocks of **Terminal-Gui** 
    - `Application` - A `static` class that provides the base 'application driver'. Given it defines a **Terminal.Gui** application it is both logically and literally (because `static`) a singleton. It has direct dependencies on `MainLoop`, `Events.cs` `NetDriver`, `CursesDriver`, `WindowsDriver`, `Responder`, `View`, and `TopLevel` (and nothing else).
    - `MainLoop` - Defines `IMainLoopDriver` and implements the `MainLoop` class.
    - `ConsoleDriver` - Definition for the Console Driver API.
    - `Events.cs` - Defines keyboard and mouse-related structs & classes. 
    - `PosDim.cs` - Implements *Computed Layout* system. These classes have deep dependencies on `View`.
    - `Responder` - Base class for the windowing class hierarchy. Implements support for keyboard & mouse input.
    - `View` - Derived from `Responder`, the base class for non-modal visual elements such as controls.
    - `Toplevel` - Derived from `View`, the base class for modal visual elements such as top-level windows and dialogs. Supports the concept of `MenuBar` and `StatusBar`.
    - `Window` - Derived from `TopLevel`; implements toplevel views with a visible frame and Title.
- `Types/` - A folder (not namespace) containing implementations of `Point`, `Rect`, and `Size` which are ancient versions of the modern `System.Drawing.Point`, `System.Drawing.Size`, and `System.Drawning.Rectangle`.
- `ConsoleDrivers/` - Source files for the three `ConsoleDriver`-based drivers: .NET: `NetDriver`, Unix & Mac: `UnixDriver`, and Windows: `WindowsDriver`.
- `Views/` - A folder (not namespace) containing the source for all built-in classes that derive from `View` (non-modals). 
- `Windows/` - A folder (not namespace) containing the source of all built-in classes that derive from `Window`.

## Version numbers

Version info for Terminal.Gui is managed by [gitversion](https://gitversion.net).

Install `gitversion`:

```powershell
dotnet tool install --global GitVersion.Tool
dotnet-gitversion
```

The project version (the nuget package and in `Terminal.Gui.dll`) is determined from the latest `git tag`. 

The format of version numbers is `vmajor.minor.patch.build` and follows the [Semantic Versioning](https://semver.org/) rules.

To define a new version (e.g. with a higher `major`, `minor`, `patch`, or `build` value) tag a commit using `git tag`:

```powershell
git tag v1.2.3 -a -m "Release v1.2.3"
dotnet-gitversion /updateprojectfiles
dotnet build -c Release
```

**DO NOT COMMIT AFTER USING `/updateprojectfiles`!**

Doing so will update the `.csproj` files in your branch with version info, which we do not want.

## Automatic Nuget Publishing

The following actions will publish the Terminal.Gui package to Nuget:

* A new version tag is defined and pushed to `v1_release` - this is the normal release process.
* A push to the `v1_release` branch without a new version tag - this is a release-candidate build and will be of the form `1.2.3-rc.4`
* A push to the `v1_develop` branch - this is a pre-release build and will be of the form `1.2.3-pre.4`

## Publishing a Release of Terminal.Gui

First, use the [Semantic Versioning](https://semver.org/) rules to determine the new version number. 

Given a version number MAJOR.MINOR.PATCH, increment the:

* MAJOR version when you make incompatible API changes
* MINOR version when you add functionality in a backward-compatible manner
* PATCH version when you make backwards-compatible bug fixes

To release a new version (e.g. with a higher `major`, `minor`, or `patch` value) tag a commit using `git tag` and then push that tag directly to the `main` branch on `github.com/gui-cs/Terminal.Gui` (`upstream`).

The `tag` must be of the form `v<major>.<minor>.<patch>`, e.g. `v1.2.3`.

### 1) Verify the `develop` branch is ready for release

* Ensure everything is committed and pushed to the `v1_develop` branch
* Ensure your local `v1_develop` branch is up-to-date with `upstream/v1_develop`

### 2) Create a pull request for the release in the `v1_develop` branch

The PR title should be of the form "Release v1.2.3"

```powershell
git checkout v1_develop
git pull upstream v1_develop
git checkout -b v1_2_3
<touch a file>
git add .
git commit -m "Release v1.2.3"
git push
```

Go to the link printed by `git push` and fill out the Pull Request.

### 3) On github.com, verify the build action worked on your fork, then merge the PR to `v1_develop`

* Merging the PR will trigger the publish action on `upstream` (the main repo) and publish the Nuget package as a pre-release (e.g. `1.2.3-pre.1`).

### 4) Pull the merged `develop` from `upstream`

```powershell
git checkout v1_develop
git pull upstream v1_develop
```

### 5) Merge `develop` into `main`

```powershell
git checkout v1_release
git pull upstream v1_release
git merge v1_develop
```

Fix any merge errors.

At this point, to release a release candidate, push the `v1_release` branch `upstream` without a new tag.

```powershell
git push upstream v1_release
```

This will publish `1.2.3-rc.1` to Nuget.

### 6) Create a new annotated tag for the release on `v1_release`

```powershell
git tag v1.2.3 -a -m "Release v1.2.3"
```       

### 7) Push the new tag to `main` on `upstream`

```powershell
git push --atomic upstream main v1.2.3
```       

*See https://stackoverflow.com/a/3745250/297526*

This will publish `1.2.3` to Nuget.

### 8) Monitor Github Actions to ensure the Nuget publishing worked.

https://github.com/gui-cs/Terminal.Gui/actions

### 9) Check Nuget to see the new package version (wait a few minutes) 
https://www.nuget.org/packages/Terminal.Gui

### 10) Add a new Release in Github: https://github.com/gui-cs/Terminal.Gui/releases

Generate release notes with the list of PRs since the last release.

### 11) Update the `v1_develop` branch with the new version

```powershell
git checkout v1_develop
git pull upstream v1_develop
git merge v1_release
git push upstream v1_develop
```

## Nuget

https://www.nuget.org/packages/Terminal.Gui

Miguel & Tig can hide defunct/old Nuget packages.

## Contributing

See [CONTRIBUTING.md](https://github.com/gui-cs/Terminal.Gui/blob/master/CONTRIBUTING.md).
