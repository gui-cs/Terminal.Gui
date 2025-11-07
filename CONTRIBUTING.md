# Contributing to Terminal.Gui

> **📘 This document is the single source of truth for all contributors (humans and AI agents) to Terminal.Gui.**

Welcome! This guide provides everything you need to know to contribute effectively to Terminal.Gui, including project structure, build instructions, coding conventions, testing requirements, and CI/CD workflows.

## Table of Contents

- [Project Overview](#project-overview)
- [Building and Testing](#building-and-testing)
- [Coding Conventions](#coding-conventions)
- [Testing Requirements](#testing-requirements)
- [API Documentation Requirements](#api-documentation-requirements)
- [Pull Request Guidelines](#pull-request-guidelines)
- [CI/CD Workflows](#cicd-workflows)
- [Repository Structure](#repository-structure)
- [Branching Model](#branching-model)
- [Key Architecture Concepts](#key-architecture-concepts)
- [What NOT to Do](#what-not-to-do)
- [Additional Resources](#additional-resources)

---

## Project Overview

**Terminal.Gui** is a cross-platform UI toolkit for creating console-based graphical user interfaces in .NET. It's a large codebase (~1,050 C# files, 333MB) providing a comprehensive framework for building interactive console applications with support for keyboard and mouse input, customizable views, and a robust event system.

**Key characteristics:**
- **Language**: C# (net8.0)
- **Size**: ~496 source files in core library, ~1,050 total C# files
- **Platform**: Cross-platform (Windows, macOS, Linux)
- **Architecture**: Console UI toolkit with driver-based architecture
- **Version**: v2 (Alpha), v1 (maintenance mode)
- **Branching**: GitFlow model (v2_develop is default/active development)

---

## Building and Testing

### Required Tools

- **.NET SDK**: 8.0.0 (see `global.json`)
- **Runtime**: .NET 8.x (latest GA)
- **Optional**: ReSharper/Rider for code formatting (honor `.editorconfig` and `Terminal.sln.DotSettings`)

### Build Commands (In Order)

**ALWAYS run these commands from the repository root:**

1. **Restore packages** (required first, ~15-20 seconds):
   ```bash
   dotnet restore
   ```

2. **Build solution** (Debug, ~50 seconds):
   ```bash
   dotnet build --configuration Debug --no-restore
   ```
   - Expect ~326 warnings (nullable reference warnings, unused variables, etc.) - these are normal
   - 0 errors expected

3. **Build Release** (for packaging):
   ```bash
   dotnet build --configuration Release --no-restore
   ```

### Test Commands

**Two test projects exist:**

1. **Non-parallel tests** (depend on static state, ~10 min timeout):
   ```bash
   dotnet test Tests/UnitTests --no-build --verbosity normal
   ```
   - Uses `Application.Init` and static state
   - Cannot run in parallel
   - Includes `--blame` flags for crash diagnostics

2. **Parallel tests** (can run concurrently, ~10 min timeout):
   ```bash
   dotnet test Tests/UnitTestsParallelizable --no-build --verbosity normal
   ```
   - No dependencies on static state
   - **Preferred for new tests**

3. **Integration tests**:
   ```bash
   dotnet test Tests/IntegrationTests --no-build --verbosity normal
   ```

**Important**: Tests may take significant time. CI uses blame flags for crash detection:
```bash
--diag:logs/UnitTests/logs.txt --blame --blame-crash --blame-hang --blame-hang-timeout 60s --blame-crash-collect-always
```

### Common Build Issues

#### Issue: Build Warnings
- **Expected**: ~326 warnings (nullable refs, unused vars, xUnit suggestions)
- **Action**: Don't add new warnings; fix warnings in code you modify

#### Issue: Test Timeouts
- **Expected**: Tests can take 5-10 minutes
- **Action**: Use appropriate timeout values (60-120 seconds for test commands)

#### Issue: Restore Failures
- **Solution**: Ensure `dotnet restore` completes before building
- **Note**: Takes 15-20 seconds on first run

#### Issue: NativeAot/SelfContained Build
- **Solution**: Restore these projects explicitly:
  ```bash
  dotnet restore ./Examples/NativeAot/NativeAot.csproj -f
  dotnet restore ./Examples/SelfContained/SelfContained.csproj -f
  ```

### Running Examples

**UICatalog** (comprehensive demo app):
```bash
dotnet run --project Examples/UICatalog/UICatalog.csproj
```

---

## Coding Conventions

### Code Style Tenets

1. **Six-Year-Old Reading Level** - Readability over terseness
2. **Consistency, Consistency, Consistency** - Follow existing patterns ruthlessly
3. **Don't be Weird** - Follow Microsoft/.NET conventions
4. **Set and Forget** - Rely on automated tooling
5. **Documentation is the Spec** - API docs are source of truth

### Code Formatting

- **Do NOT add formatting tools** - Use existing `.editorconfig` and `Terminal.sln.DotSettings`
- Format code with:
  1. ReSharper/Rider (`Ctrl-E-C`)
  2. JetBrains CleanupCode CLI tool (free)
  3. Visual Studio (`Ctrl-K-D`) as fallback
- **Only format files you modify**

### Critical Coding Rules

**⚠️ CRITICAL - These rules MUST be followed in ALL new or modified code:**

#### Type Declarations and Object Creation

- **ALWAYS use explicit types** - Never use `var` except for built-in simple types (`int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`)
  ```csharp
  // ✅ CORRECT - Explicit types
  View view = new () { Width = 10 };
  MouseEventArgs args = new () { Position = new Point(5, 5) };
  List<View?> views = new ();
  var count = 0;  // OK - int is a built-in type
  var name = "test";  // OK - string is a built-in type
  
  // ❌ WRONG - Using var for non-built-in types
  var view = new View { Width = 10 };
  var args = new MouseEventArgs { Position = new Point(5, 5) };
  var views = new List<View?>();
  ```

- **ALWAYS use target-typed `new()`** - Use `new ()` instead of `new TypeName()` when the type is already declared
  ```csharp
  // ✅ CORRECT - Target-typed new
  View view = new () { Width = 10 };
  MouseEventArgs args = new ();
  
  // ❌ WRONG - Redundant type name
  View view = new View() { Width = 10 };
  MouseEventArgs args = new MouseEventArgs();
  ```

#### Other Conventions

- Follow `.editorconfig` settings (e.g., braces on new lines, spaces after keywords)
- 4-space indentation
- No trailing whitespace
- File-scoped namespaces

**⚠️ CRITICAL - These conventions apply to ALL code - production code, test code, examples, and samples.**

---

## Testing Requirements

### Code Coverage

- **Never decrease code coverage** - PRs must maintain or increase coverage
- Target: 70%+ coverage for new code
- CI monitors coverage on each PR

### Test Patterns

- **Parallelizable tests preferred** - Add new tests to `UnitTestsParallelizable` when possible
- **Avoid static dependencies** - Don't use `Application.Init`, `ConfigurationManager` in tests
- **Don't use `[AutoInitShutdown]`** - Legacy pattern, being phased out
- **Make tests granular** - Each test should cover smallest area possible
- Follow existing test patterns in respective test projects

### Test Configuration

- `xunit.runner.json` - xUnit configuration
- `coverlet.runsettings` - Coverage settings (OpenCover format)

---

## API Documentation Requirements

**All public APIs MUST have XML documentation:**

- Clear, concise `<summary>` tags
- Use `<see cref=""/>` for cross-references
- Add `<remarks>` for context
- Include `<example>` for non-obvious usage
- Complex topics → `docfx/docs/*.md` files
- Proper English and grammar - Clear, concise, complete. Use imperative mood.

---

## Pull Request Guidelines

### PR Requirements

- **Title**: "Fixes #issue. Terse description". If multiple issues, list all, separated by commas (e.g. "Fixes #123, #456. Terse description")
- **Description**: 
  - Include "- Fixes #issue" for each issue near the top
  - **ALWAYS** include instructions for pulling down locally at end of Description
  - Suggest user setup a remote named `copilot` pointing to your fork
  - Example:
    ```markdown
    # To pull down this PR locally:
    git remote add copilot <your-fork-url>
    git fetch copilot <branch-name>
    git checkout copilot/<branch-name>
    ```
- **Coding Style**: Follow all coding conventions in this document for new and modified code
- **Tests**: Add tests for new functionality (see [Testing Requirements](#testing-requirements))
- **Coverage**: Maintain or increase code coverage
- **Scenarios**: Update UICatalog scenarios when adding features
- **Warnings**: **CRITICAL - PRs must not introduce any new warnings**
  - Any file modified in a PR that currently generates warnings **MUST** be fixed to remove those warnings
  - Exception: Warnings caused by `[Obsolete]` attributes can remain
  - Expected baseline: ~326 warnings (mostly nullable reference warnings, unused variables, xUnit suggestions)
  - Action: Before submitting a PR, verify your changes don't add new warnings and fix any warnings in files you modify

---

## CI/CD Workflows

The repository uses multiple GitHub Actions workflows. What runs and when:

### 1) Build Solution (`.github/workflows/build.yml`)

- **Triggers**: push and pull_request to `v2_release`, `v2_develop` (ignores `**.md`); supports `workflow_call`
- **Runner/timeout**: `ubuntu-latest`, 10 minutes
- **Steps**:
  - Checkout and setup .NET 8.x GA
  - `dotnet restore`
  - Build Debug: `dotnet build --configuration Debug --no-restore -property:NoWarn=0618%3B0612`
  - Build Release (library): `dotnet build Terminal.Gui/Terminal.Gui.csproj --configuration Release --no-incremental --force -property:NoWarn=0618%3B0612`
  - Pack Release: `dotnet pack Terminal.Gui/Terminal.Gui.csproj --configuration Release --output ./local_packages -property:NoWarn=0618%3B0612`
  - Restore NativeAot/SelfContained examples, then restore solution again
  - Build Release for `Examples/NativeAot` and `Examples/SelfContained`
  - Build Release solution
  - Upload artifacts named `build-artifacts`, retention 1 day

### 2) Build & Run Unit Tests (`.github/workflows/unit-tests.yml`)

- **Triggers**: push and pull_request to `v2_release`, `v2_develop` (ignores `**.md`)
- **Process**: Calls build workflow, then runs:
  - Non-parallel UnitTests on Ubuntu/Windows/macOS matrix with coverage and blame/diag flags; `xunit.stopOnFail=false`
  - Parallel UnitTestsParallelizable similarly with coverage; `xunit.stopOnFail=false`
  - Uploads logs per-OS

### 3) Build & Run Integration Tests (`.github/workflows/integration-tests.yml`)

- **Triggers**: push and pull_request to `v2_release`, `v2_develop` (ignores `**.md`)
- **Process**: Calls build workflow, then runs IntegrationTests on matrix with blame/diag; `xunit.stopOnFail=true`
- Uploads logs per-OS

### 4) Publish to NuGet (`.github/workflows/publish.yml`)

- **Triggers**: push to `v2_release`, `v2_develop`, and tags `v*` (ignores `**.md`)
- Uses GitVersion to compute SemVer, builds Release, packs with symbols, and pushes to NuGet.org using `NUGET_API_KEY`

### 5) Build and publish API docs (`.github/workflows/api-docs.yml`)

- **Triggers**: push to `v1_release` and `v2_develop`
- Builds DocFX site on Windows and deploys to GitHub Pages when `ref_name` is `v2_release` or `v2_develop`

### Replicating CI Locally

```bash
# Full CI sequence:
dotnet restore
dotnet build --configuration Debug --no-restore
dotnet test Tests/UnitTests --no-build --verbosity normal
dotnet test Tests/UnitTestsParallelizable --no-build --verbosity normal
dotnet build --configuration Release --no-restore
```

---

## Repository Structure

### Root Directory Files

- `Terminal.sln` - Main solution file
- `Terminal.sln.DotSettings` - ReSharper code style settings
- `.editorconfig` - Code formatting rules (111KB, extensive)
- `global.json` - .NET SDK version pinning
- `Directory.Build.props` - Common MSBuild properties
- `Directory.Packages.props` - Central package version management
- `GitVersion.yml` - Version numbering configuration
- `CONTRIBUTING.md` - This file - contribution guidelines (source of truth)
- `AGENTS.md` - Pointer to this file for AI agents
- `README.md` - Project documentation

### Main Directories

**`/Terminal.Gui/`** - Core library (496 C# files):
- `App/` - Application lifecycle (`Application.cs` static class, `RunState`, `MainLoop`)
- `Configuration/` - `ConfigurationManager` for settings
- `Drivers/` - Console driver implementations (`Dotnet`, `Windows`, `Unix`, `Fake`)
- `Drawing/` - Rendering system (attributes, colors, glyphs)
- `Input/` - Keyboard and mouse input handling
- `ViewBase/` - Core `View` class hierarchy and layout
- `Views/` - Specific View subclasses (Window, Dialog, Button, ListView, etc.)
- `Text/` - Text manipulation and formatting
- `FileServices/` - File operations and services

**`/Tests/`**:
- `UnitTests/` - Non-parallel tests (use `Application.Init`, static state)
- `UnitTestsParallelizable/` - Parallel tests (no static dependencies) - **Preferred**
- `IntegrationTests/` - Integration tests
- `StressTests/` - Long-running stress tests (scheduled daily)
- `coverlet.runsettings` - Code coverage configuration

**`/Examples/`**:
- `UICatalog/` - Comprehensive demo app for manual testing
- `Example/` - Basic example
- `NativeAot/`, `SelfContained/` - Deployment examples
- `ReactiveExample/`, `CommunityToolkitExample/` - Integration examples

**`/docfx/`** - Documentation source:
- `docs/` - Conceptual documentation (deep dives)
- `api/` - Generated API docs (gitignored)
- `docfx.json` - DocFX configuration

**`/Scripts/`** - PowerShell build utilities (requires PowerShell 7.4+)

**`/.github/workflows/`** - CI/CD pipelines (see [CI/CD Workflows](#cicd-workflows))

---

## Branching Model

### GitFlow Model

- `v2_develop` - Default branch, active development
- `v2_release` - Stable releases, matches NuGet
- `v1_develop`, `v1_release` - Legacy v1 (maintenance only)

---

## Key Architecture Concepts

**⚠️ CRITICAL - Contributors should understand these concepts before starting work.**

See `/docfx/docs/` for deep dives on:

- **Application Lifecycle** - How `Application.Init`, `Application.Run`, and `Application.Shutdown` work
- **View Hierarchy** - Understanding `View`, `Toplevel`, `Window`, and view containment
- **Layout System** - Pos, Dim, and automatic layout
- **Event System** - How keyboard, mouse, and application events flow
- **Driver Architecture** - How console drivers abstract platform differences
- **Drawing Model** - How rendering works with Attributes, Colors, and Glyphs

Key documentation:
- [View Documentation](https://gui-cs.github.io/Terminal.Gui/docs/View.html)
- [Events Deep Dive](https://gui-cs.github.io/Terminal.Gui/docs/events.html)
- [Layout System](https://gui-cs.github.io/Terminal.Gui/docs/layout.html)
- [Keyboard Handling](https://gui-cs.github.io/Terminal.Gui/docs/keyboard.html)
- [Mouse Support](https://gui-cs.github.io/Terminal.Gui/docs/mouse.html)
- [Drivers](https://gui-cs.github.io/Terminal.Gui/docs/drivers.html)

---

## What NOT to Do

- ❌ Don't add new linters/formatters (use existing)
- ❌ Don't modify unrelated code
- ❌ Don't remove/edit unrelated tests
- ❌ Don't break existing functionality
- ❌ Don't add tests to `UnitTests` if they can be parallelizable
- ❌ Don't use `Application.Init` in new tests
- ❌ Don't decrease code coverage
- ❌ **Don't use `var` for anything but built-in simple types** (use explicit types)
- ❌ **Don't use redundant type names with `new`** (**ALWAYS PREFER** target-typed `new ()`)
- ❌ **Don't introduce new warnings** (fix warnings in files you modify; exception: `[Obsolete]` warnings)

---

## Additional Resources

- **Full Documentation**: https://gui-cs.github.io/Terminal.Gui
- **API Reference**: https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui.App.html
- **Deep Dives**: `/docfx/docs/` directory
- **Getting Started**: https://gui-cs.github.io/Terminal.Gui/docs/getting-started.html
- **Migrating from v1 to v2**: https://gui-cs.github.io/Terminal.Gui/docs/migratingfromv1.html
- **Showcase**: https://gui-cs.github.io/Terminal.Gui/docs/showcase.html

---

**Thank you for contributing to Terminal.Gui!** 🎉
