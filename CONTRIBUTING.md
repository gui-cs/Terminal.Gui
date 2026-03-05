# Contributing to Terminal.Gui

> **📘 This document is the single source of truth for all contributors (humans and AI agents) to Terminal.Gui.**

Welcome! This guide provides everything you need to know to contribute effectively to Terminal.Gui, including project structure, build instructions, coding conventions, testing requirements, and CI/CD workflows.

## Table of Contents

- [Project Overview](#project-overview)
- [Key Architecture Concepts](#key-architecture-concepts)
- [Coding Conventions](#coding-conventions)
- [Building and Testing](#building-and-testing)
- [Testing Requirements](#testing-requirements)
- [API Documentation Requirements](#api-documentation-requirements)
- [Pull Request Guidelines](#pull-request-guidelines)
- [CI/CD Workflows](#cicd-workflows)
- [Repository Structure](#repository-structure)
- [Branching Model](#branching-model)
- [What NOT to Do](#what-not-to-do)

## Project Overview

**Terminal.Gui** is a cross-platform UI toolkit for creating console-based graphical user interfaces in .NET. It's a large codebase (~1,050 C# files) providing a comprehensive framework for building interactive console applications with support for keyboard and mouse input, customizable views, and a robust event system.

**Key characteristics:**
- **Language**: C# (net8.0)
- **Platform**: Cross-platform (Windows, macOS, Linux)
- **Architecture**: Console UI toolkit with driver-based architecture
- **Version**: v2 (Beta), v1 (maintenance mode)
- **Branching**: GitFlow model (v2_develop is default/active development)

## Key Architecture Concepts

**⚠️ CRITICAL - AI Agents MUST understand these concepts before starting work.**

- **Application Lifecycle** - How `Application.Init`, `Application.Run`, and `Application.Shutdown` work - [Application Deep Dive](./docfx/docs/application.md)
- **Cancellable Workflow Patern** - [CWP Deep Dive](./docfx/docs/cancellable-work-pattern.md)
- **View Hierarchy** - Understanding `View`, `Runnable`, `Window`, and view containment - [View Deep Dive](./docfx/docs/View.md)
- **Layout System** - Pos, Dim, and automatic layout -  [Layout System](./docfx/docs/layout.md)
- **Event System** - How keyboard, mouse, and application events flow - [Events Deep Dive](./docfx/docs/events.md)
- **Driver Architecture** - How console drivers abstract platform differences - [Drivers](./docfx/docs/drivers.md)
- **Drawing Model** - How rendering works with Attributes, Colors, and Glyphs  - [Drawing Deep Dive](./docfx/docs/drivers.md)
 
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
   dotnet test --project Tests/UnitTests --no-build --verbosity normal
   ```
   - Uses `Application.Init` and static state
   - Cannot run in parallel
   - Includes `--diagnostic` flag for logging

2. **Parallel tests** (can run concurrently, ~10 min timeout):
   ```bash
   dotnet test --project Tests/UnitTestsParallelizable --no-build --verbosity normal
   ```
   - No dependencies on static state
   - **Preferred for new tests**

3. **Integration tests**:
   ```bash
   dotnet test --project Tests/IntegrationTests --no-build --verbosity normal
   ```

### Common Build Issues

#### Issue: NativeAot/SelfContained Build

- **Solution**: Restore these projects explicitly:
  ```bash
  dotnet restore ./Examples/NativeAot/NativeAot.csproj -f
  dotnet restore ./Examples/SelfContained/SelfContained.csproj -f
  ```

## Coding Conventions

**⚠️ CRITICAL - These rules MUST be followed in ALL new or modified code**

### Code Style Tenets

1. **Six-Year-Old Reading Level** - Readability over terseness
2. **Consistency, Consistency, Consistency** - Follow existing patterns ruthlessly
3. **Don't be Weird** - Follow Microsoft/.NET conventions
4. **Set and Forget** - Rely on automated tooling
5. **Documentation is the Spec** - API docs are source of truth

### Code Formatting

**⚠️ CRITICAL - These rules MUST be followed in ALL new or modified code:**

**AI or AI Agent Written or Modified Code MUST Follow these instructions**

- Read and study `.editorconfig` and `Terminal.sln.DotSettings` to determine code style and formatting.
- Format code with:
  1. ReSharper/Rider (`Ctrl-E-C`)
  2. JetBrains CleanupCode CLI tool (free)
  3. Visual Studio (`Ctrl-K-D`) as fallback
- Only format files you modify
- Follow `.editorconfig` settings
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
- **ALWAYS use target-typed `new ()`** - Use `new ()` instead of `new TypeName()` when the type is already declared
  ```csharp
  // ✅ CORRECT - Target-typed new
  View view = new () { Width = 10 };
  MouseEventArgs args = new ();
  
  // ❌ WRONG - Redundant type name
  View view = new View() { Width = 10 };
  MouseEventArgs args = new MouseEventArgs();
  ```
- **ALWAYS** use collection initializers if possible:
  ```csharp
  // ✅ CORRECT - Collection initializer
  List<View> views = [
      new Button("OK"),
      new Button("Cancel")
  ];
  
  // ❌ WRONG - Adding items separately
  List<View> views = new ();
  views.Add(new Button("OK"));
  views.Add(new Button("Cancel"));
  ```

**⚠️ CRITICAL - These conventions apply to ALL code - production code, test code, examples, documentation, and samples.**

## Testing Requirements

### Code Coverage

- **Never decrease code coverage** - PRs must maintain or increase coverage
- Target: 70%+ coverage for new code
- **Coverage collection**:
- Centralized in `TestResults/` directory at repository root
- Collected only on Linux (ubuntu-latest) runners in CI for performance
- Windows and macOS runners skip coverage collection to reduce execution time
- Coverage reports uploaded to Codecov automatically from Linux runner
- CI monitors coverage on each PR

### Test Patterns

- **AI Created Tests MUST follow these patterns exactly.**
- **Add comment indicating the test was AI generated** - e.g., `// CoPilot - ChatGPT v4`
- **Make tests granular** - Each test should cover smallest area possible
- Follow existing test patterns in respective test projects
- **Avoid adding new tests to the `UnitTests` Project** - Make them parallelizable and add them to `UnitTests.Parallelizable`
- **Avoid static dependencies** - DO NOT use the legacy/static `Application` API or `ConfigurationManager` in tests unless the tests explicitly test related functionality.
- **Don't use `[AutoInitShutdown]` or `[SetupFakeApplication]`** - Legacy pattern, being phased out

### Test Configuration

- `xunit.runner.json` - xUnit configuration
- `coverlet.runsettings` - Coverage settings (OpenCover format)

## API Documentation Requirements

**All public APIs MUST have XML documentation:**

- Clear, concise `<summary>` tags
- Use `<see cref=""/>` for cross-references
- Add `<remarks>` for context
- Include `<example>` for non-obvious usage
- Complex topics → `docfx/docs/*.md` files
- Proper English and grammar - Clear, concise, complete. Use imperative mood.

## Pull Request Guidelines

### PR Requirements

- **ALWAYS** include instructions for pulling down locally at end of Description

- **Title**: "Fixes #issue. Terse description". If multiple issues, list all, separated by commas (e.g. "Fixes #123, #456. Terse description")
- **Description**: 
  - Include "- Fixes #issue" for each issue near the top
  - Suggest user setup a remote named `copilot` pointing to your fork
  - Example:
    ```markdown
    # To pull down this PR locally:
    git remote add copilot <your-fork-url>
    git fetch copilot <branch-name>
    git checkout copilot/<branch-name>
    ```
- **Tests**: Add tests for new functionality (see [Testing Requirements](#testing-requirements))
- **Coverage**: Maintain or increase code coverage
- **Scenarios**: Update UICatalog scenarios when adding features
- **Warnings**: **CRITICAL - PRs must not introduce any new warnings**
  - Any file modified in a PR that currently generates warnings **MUST** be fixed to remove those warnings
  - Exception: Warnings caused by `[Obsolete]` attributes can remain
  - Action: Before submitting a PR, verify your changes don't add new warnings and fix any warnings in files you modify


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
- `App/` - Application lifecycle (`Application.cs` static class, `SessionToken`, `MainLoop`)
- `Configuration/` - `ConfigurationManager` for settings
- `Drivers/` - Console driver implementations (`dotnet`, `Windows`, `Unix`, `ansi`)
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


## Branching Model

### GitFlow Model

- `v2_develop` - Default branch, active development
- `v2_release` - Stable releases, matches NuGet
- `v1_develop`, `v1_release` - Legacy v1 (maintenance only)

## Release Process

### Automated Release Workflow

Releases are now automated using GitHub Actions to prevent manual errors. To create a release:

1. **Navigate to Actions tab** in the GitHub repository
2. **Select "Create Release" workflow** from the left sidebar
3. **Click "Run workflow"** button
4. **Configure release parameters:**
   - **Branch:** Ensure `v2_release` is selected
   - **Release type:** Choose from `prealpha`, `alpha`, `beta`, `rc`, or `stable`
   - **Version override:** (Optional) Specify exact version (e.g., `2.0.0`), otherwise GitVersion calculates it automatically
5. **Click "Run workflow"** to start the automated release process

The workflow will:
- Create an annotated git tag (e.g., `v2.0.0-prealpha` or `v2.0.0`)
- Create a release commit on `v2_release`
- Push the tag and commit to the repository
- Create a GitHub Release
- Automatically trigger the publish workflow to push the package to NuGet.org

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

**Thank you for contributing to Terminal.Gui!** 🎉
