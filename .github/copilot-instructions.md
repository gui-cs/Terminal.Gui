# Terminal.Gui - Copilot Coding Agent Instructions

This file provides onboarding instructions for GitHub Copilot and other AI coding agents working with Terminal.Gui.

## Project Overview

**Terminal.Gui** is a cross-platform UI toolkit for creating console-based graphical user interfaces in .NET. It's a large codebase (~1,050 C# files, 333MB) providing a comprehensive framework for building interactive console applications with support for keyboard and mouse input, customizable views, and a robust event system.

**Key characteristics:**
- **Language**: C# (net8.0)
- **Size**: ~496 source files in core library, ~1,050 total C# files
- **Platform**: Cross-platform (Windows, macOS, Linux)
- **Architecture**: Console UI toolkit with driver-based architecture
- **Version**: v2 (Alpha), v1 (maintenance mode)
- **Branching**: GitFlow model (v2_develop is default/active development)

## Building and Testing

### Required Tools
- **.NET SDK**: 8.0.0 (see `global.json`)
- **Runtime**: .NET 8.x (latest GA)
- **Optional**: ReSharper/Rider for code formatting

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
   - Preferred for new tests

3. **Integration tests**:
   ```bash
   dotnet test Tests/IntegrationTests --no-build --verbosity normal
   ```

**Important**: Tests may take significant time. CI uses blame flags for crash detection:
```bash
--diag:logs/UnitTests/logs.txt --blame --blame-crash --blame-hang --blame-hang-timeout 60s --blame-crash-collect-always
```

### Running Examples

**UICatalog** (comprehensive demo app):
```bash
dotnet run --project Examples/UICatalog/UICatalog.csproj
```

## Repository Structure

### Root Directory Files
- `Terminal.sln` - Main solution file
- `Terminal.sln.DotSettings` - ReSharper code style settings
- `.editorconfig` - Code formatting rules (111KB, extensive)
- `global.json` - .NET SDK version pinning
- `Directory.Build.props` - Common MSBuild properties
- `Directory.Packages.props` - Central package version management
- `GitVersion.yml` - Version numbering configuration
- `AGENTS.md` - General AI agent instructions (also useful reference)
- `CONTRIBUTING.md` - Contribution guidelines
- `README.md` - Project documentation

### Main Directories

**`/Terminal.Gui/`** - Core library (496 C# files):
- `App/` - Application lifecycle (`Application.cs` static class, `RunState`, `MainLoop`)
- `Configuration/` - `ConfigurationManager` for settings
- `Drivers/` - Console driver implementations (`IConsoleDriver`, `NetDriver`, `UnixDriver`, `WindowsDriver`)
- `Drawing/` - Rendering system (attributes, colors, glyphs)
- `Input/` - Keyboard and mouse input handling
- `ViewBase/` - Core `View` class hierarchy and layout
- `Views/` - Specific View subclasses (Window, Dialog, Button, ListView, etc.)
- `Text/` - Text manipulation and formatting

**`/Tests/`**:
- `UnitTests/` - Non-parallel tests (use `Application.Init`, static state)
- `UnitTestsParallelizable/` - Parallel tests (no static dependencies)
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

**`/.github/workflows/`** - CI/CD pipelines:
- `unit-tests.yml` - Main test workflow (Ubuntu, Windows, macOS)
- `build-release.yml` - Release build verification
- `integration-tests.yml` - Integration test workflow
- `publish.yml` - NuGet package publishing
- `api-docs.yml` - Documentation building and deployment
- `codeql-analysis.yml` - Security scanning

## Code Style and Quality

### Formatting
- **Do NOT add formatting tools** - Use existing `.editorconfig` and `Terminal.sln.DotSettings`
- Format code with:
  1. ReSharper/Rider (`Ctrl-E-C`)
  2. JetBrains CleanupCode CLI tool (free)
  3. Visual Studio (`Ctrl-K-D`) as fallback
- **Only format files you modify**

### Code Style Tenets
1. **Six-Year-Old Reading Level** - Readability over terseness
2. **Consistency, Consistency, Consistency** - Follow existing patterns ruthlessly
3. **Don't be Weird** - Follow Microsoft/.NET conventions
4. **Set and Forget** - Rely on automated tooling
5. **Documentation is the Spec** - API docs are source of truth

### Coding Conventions
- Use explicit types (avoid `var` except for basic types like `int`, `string`)
- Use target-typed `new()`
- Follow `.editorconfig` settings (e.g., braces on new lines, spaces after keywords)
- 4-space indentation
- See `CONTRIBUTING.md` for full guidelines

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

## API Documentation Requirements

**All public APIs MUST have XML documentation:**
- Clear, concise `<summary>` tags
- Use `<see cref=""/>` for cross-references
- Add `<remarks>` for context
- Include `<example>` for non-obvious usage
- Complex topics → `docfx/docs/*.md` files
- Proper English and grammar

## Common Build Issues

### Issue: Build Warnings
- **Expected**: ~326 warnings (nullable refs, unused vars, xUnit suggestions)
- **Action**: Don't add new warnings; fix warnings in code you modify

### Issue: Test Timeouts
- **Expected**: Tests can take 5-10 minutes
- **Action**: Use appropriate timeout values (60-120 seconds for test commands)

### Issue: Restore Failures
- **Solution**: Ensure `dotnet restore` completes before building
- **Note**: Takes 15-20 seconds on first run

### Issue: NativeAot/SelfContained Build
- **Solution**: Restore these projects explicitly:
  ```bash
  dotnet restore ./Examples/NativeAot/NativeAot.csproj -f
  dotnet restore ./Examples/SelfContained/SelfContained.csproj -f
  ```

## CI/CD Validation

The following checks run on PRs:

1. **Unit Tests** (`unit-tests.yml`):
   - Runs on Ubuntu, Windows, macOS
   - Both parallel and non-parallel test suites
   - Code coverage collection
   - 10-minute timeout per job

2. **Build Release** (`build-release.yml`):
   - Verifies Release configuration builds
   - Tests NativeAot and SelfContained builds
   - Packs NuGet package

3. **Integration Tests** (`integration-tests.yml`):
   - Cross-platform integration testing
   - 10-minute timeout

4. **CodeQL Analysis** (`codeql-analysis.yml`):
   - Security vulnerability scanning

To replicate CI locally:
```bash
# Full CI sequence:
dotnet restore
dotnet build --configuration Debug --no-restore
dotnet test Tests/UnitTests --no-build --verbosity normal
dotnet test Tests/UnitTestsParallelizable --no-build --verbosity normal
dotnet build --configuration Release --no-restore
```

## Branching and PRs

### GitFlow Model
- `v2_develop` - Default branch, active development
- `v2_release` - Stable releases, matches NuGet
- `v1_develop`, `v1_release` - Legacy v1 (maintenance only)

### PR Requirements
- **Title**: "Fixes #issue. Terse description"
- **Description**: Include "- Fixes #issue" for each issue
- **Tests**: Add tests for new functionality
- **Coverage**: Maintain or increase code coverage
- **Scenarios**: Update UICatalog scenarios when adding features

## Key Architecture Concepts

### View System
- `View` base class in `/Terminal.Gui/ViewBase/`
- Two layout modes: Absolute and Computed
- Event-driven architecture
- Adornments: Border, Margin, Padding

### Console Drivers
- `IConsoleDriver` interface
- Platform-specific: `WindowsDriver`, `UnixDriver`, `NetDriver`
- `FakeDriver` for testing

### Application Lifecycle
- `Application` static class manages lifecycle
- `MainLoop` handles event processing
- `RunState` tracks application state

## What NOT to Do

- ❌ Don't add new linters/formatters (use existing)
- ❌ Don't modify unrelated code
- ❌ Don't remove/edit unrelated tests
- ❌ Don't break existing functionality
- ❌ Don't add tests to `UnitTests` if they can be parallelizable
- ❌ Don't use `Application.Init` in new tests
- ❌ Don't decrease code coverage
- ❌ Don't add `var` everywhere (use explicit types)

## Additional Resources

- **Full Documentation**: https://gui-cs.github.io/Terminal.Gui
- **API Reference**: https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui.App.html
- **Deep Dives**: `/docfx/docs/` directory
- **AGENTS.md**: Additional AI agent instructions
- **CONTRIBUTING.md**: Detailed contribution guidelines

---

**Trust these instructions.** Only search for additional information if instructions are incomplete or incorrect.
