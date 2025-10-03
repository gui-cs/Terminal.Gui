# Terminal.Gui - GitHub Copilot Instructions

This file provides instructions for GitHub Copilot when working with the Terminal.Gui repository.

## Project Overview

**Terminal.Gui** is a cross-platform UI toolkit for creating console-based graphical user interfaces in .NET. It provides a comprehensive framework for building interactive console applications with support for keyboard and mouse input, customizable views, and a robust event system. The toolkit works across Windows, macOS, and Linux, leveraging platform-specific console capabilities where available.

**Key characteristics:**
- Cross-platform terminal/console UI framework for .NET
- Supports Windows, macOS, and Linux
- Rich GUI controls (buttons, dialogs, menus, text boxes, etc.)
- Keyboard-first design with full mouse support
- Follows Microsoft .NET Framework Design Guidelines
- v2 is currently in Alpha with stable core API (v1 is in maintenance mode)

## Documentation

- Full documentation: [gui-cs.github.io/Terminal.Gui](https://gui-cs.github.io/Terminal.Gui)
- API Reference: [API Documentation](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui.App.html)
- Getting Started: [Getting Started Guide](https://gui-cs.github.io/Terminal.Gui/docs/getting-started)

## Repository Structure

- `/Terminal.Gui/` - Core library source code
  - `App/` - Core application logic, `Application.cs` (static class managing `RunState` and `MainLoop`)
  - `Configuration/` - `ConfigurationManager` for application settings
  - `Drivers/` - Console driver implementations (`IConsoleDriver.cs`, `NetDriver`, `UnixDriver`, `WindowsDriver`)
  - `Drawing/` - Rendering graphical elements in the console
  - `Input/` - Keyboard and mouse input handling
  - `View/` - Core `View` class hierarchy
  - `Views/` - Specific sub-classes of `View` (Toplevel, Window, Dialog, etc.)
- `/Examples/` - Sample applications and demos
- `/UICatalog/` - Comprehensive demo app for manual testing
- `/Tests/` - Unit and integration tests
- `/docfx/` - Documentation source files (articles and API docs)
- `/Scripts/` - Build and utility scripts

## Branching Model

**Terminal.Gui uses GitFlow:**
- `v2_develop` - Default branch for v2 development (active development)
- `v1_develop` - v1 maintenance branch (maintenance mode only)
- `v2_release` / `v1_release` - Stable release branches matching NuGet packages
- `main` - Production branch for releases

**Workflow:**
1. Fork the repository
2. Create feature branches from `v2_develop` (or `v1_develop` for v1 fixes)
3. Submit Pull Requests back to `v2_develop`
4. Releases are merged from `develop` to `main` and tagged

## Code Style and Standards

### Code Style Tenets

1. **Six-Year-Old Reading Level** - Prioritize readability over terseness. Use clear variable names and comments.
2. **Consistency, Consistency, Consistency** - Follow established patterns ruthlessly.
3. **Don't be Weird** - Follow Microsoft and .NET community conventions.
4. **Set and Forget** - Use ReSharper/Rider for automated formatting.
5. **Documentation is the Spec** - API documentation is the source of truth.

### Coding Conventions

- Based on [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Rules defined in `.editorconfig` and `.dotsettings` files
- 4 spaces for indentation (not 8)
- Space between method name and opening parenthesis: `MyMethod (args)`
- Opening braces on new lines (Allman style)
- Use `var` sparingly - prefer explicit types for clarity

### Formatting Code

Before committing, format your code (in order of preference):
1. `Ctrl-E-C` in ReSharper or Rider
2. Run `cleanupcode.exe relative/path/to/your/file.cs` (JetBrains CleanupCode tool)
3. `Ctrl-K-D` in Visual Studio (last resort)

**Important:** Only format files you have modified, not the entire codebase.

## API Design Guidelines

### Public API Tenets

1. **Stand on the shoulders of giants** - Follow [Microsoft .NET Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
2. **Don't Break Existing Stuff** - Avoid breaking changes; find compatible ways to add features
3. **Fail-fast** - Prefer early failure to expose bugs sooner
4. **Standards Reduce Complexity** - Use standard .NET idioms

### API Documentation Requirements

**All public APIs must have XML documentation:**
- Clear, concise, and complete `<summary>` tags
- Use `<see cref=""/>` liberally for cross-references
- Add `<remarks>` for context and detailed explanations
- Document complex topics in `docfx/articles/*.md` files
- Use proper English and correct grammar

Example:
```csharp
/// <summary>Renders the current <see cref="Model"/> on the specified line <paramref name="y"/>.</summary>
/// <param name="y">The line number to render on.</param>
/// <param name="availableWidth">The available width for rendering.</param>
/// <remarks>
/// This method handles scrolling and text truncation automatically.
/// </remarks>
public virtual void Draw (int y, int availableWidth)
{
    // Implementation
}
```

### Defining New View Classes

- Support parameterless constructors (allow `new Foo()`)
- Avoid initialization in constructors; use properties for object initialization (`var foo = new Foo() { a = b };`)
- Ensure `UICatalog` demo illustrates both Absolute Layout and Computed Layout
- Follow the existing `View` hierarchy patterns

### Events

- Follow the [Events Deep Dive](https://gui-cs.github.io/Terminal.Gui/docs/events.html) documentation
- Use the Cancellable Work Pattern for user-initiated actions
- Name event handlers consistently (e.g., `On[EventName]`)

### Breaking Changes

- Tag PRs with breaking changes using the `breaking-change` label
- Document breaking changes in XML `<remark>` tags
- Avoid breaking changes whenever possible

## User Experience Tenets

1. **Honor What's Come Before** - Follow established Mac/Windows GUI idioms (e.g., `Ctrl-C` for copy)
2. **Consistency Matters** - Common UI patterns should be consistent (e.g., `Ctrl-Q` quits modals)
3. **Honor the OS, but Work Everywhere** - Take advantage of platform capabilities while maintaining cross-platform support
4. **Keyboard first, Mouse also** - Optimize for keyboard, but ensure everything also works with mouse

## Testing

### Unit Test Requirements

- **Never decrease code coverage** - Aim for 70%+ coverage on new code
- Write unit tests for all new functionality
- Follow existing test patterns in `/Tests/`
- Run tests before committing: `dotnet test --no-restore --verbosity normal`

### Code Coverage

- Current coverage is shown in the README badge
- Generate coverage report: `dotnet test --no-restore --verbosity normal --collect:"XPlat Code Coverage" --settings UnitTests/coverlet.runsettings`
- Project targets continuous improvement towards 100% coverage

### Manual Testing

- Add new `Scenario` to UICatalog or update existing ones
- UICatalog is the primary manual testing application
- Run UICatalog: `dotnet run --project Examples/UICatalog/UICatalog.csproj`

## Pull Request Checklist

Before submitting a PR, ensure:
- [ ] PR title: "Fixes #issue. Terse description."
- [ ] Code follows style guidelines (`.editorconfig`)
- [ ] Code follows design guidelines (`CONTRIBUTING.md`)
- [ ] Ran `dotnet test` and all tests pass
- [ ] Added/updated XML API documentation (`///` comments)
- [ ] No new warnings generated
- [ ] Checked for grammar/spelling errors
- [ ] Conducted basic QA testing
- [ ] Added/updated UICatalog scenario if applicable

## Building and Running

### Build the Solution
```powershell
dotnet build
```

### Run Tests
```powershell
dotnet test
```

### Run UICatalog Demo
```powershell
dotnet run --project Examples/UICatalog/UICatalog.csproj
```

### Quick Start Template
```powershell
dotnet new --install Terminal.Gui.templates
dotnet new tui -n myproj
cd myproj
dotnet run
```

## Common Commands

```powershell
# Clean build
dotnet clean
dotnet build -c Release

# Run specific test
dotnet test --filter "TestName~MyTest"

# Format a file
cleanupcode.exe Terminal.Gui/Views/MyView.cs

# Check version
dotnet-gitversion
```

## Versioning and Releases

- Uses [GitVersion](https://gitversion.net) for versioning
- Follows [Semantic Versioning](https://semver.org/): `major.minor.patch.build.height`
- Versions determined by git tags
- NuGet packages automatically generated on release

## Key Concepts

### View Hierarchy
- **View** - Base class for visual/interactive elements
- **SubView** - A View contained in another view (added via `Add()`)
- **SuperView** - The container View (parent)
- **Toplevel** - Base class for modal elements (top-level windows, dialogs)
- **Window** - Framed top-level view with title
- **Dialog** - Specialized modal window for user interaction

### Layout System
- **Absolute Layout** - Fixed positioning with absolute coordinates
- **Computed Layout** - Dynamic positioning based on expressions (Dim/Pos)
- **Dim** - Dimension specification (width/height)
- **Pos** - Position specification (x/y)

### Application Lifecycle
- `Application.Init()` - Initialize the application
- `Application.Run()` - Run modal view
- `Application.RequestStop()` - Stop modal execution
- `Application.Shutdown()` - Clean shutdown

### Console Drivers
- **NetDriver** - Cross-platform .NET driver
- **WindowsDriver** - Windows Console API driver
- **UnixDriver** - Unix/macOS terminfo driver
- **CursesDriver** - ncurses-based driver

## Additional Resources

- **CONTRIBUTING.md** - Detailed contribution guidelines
- **Terminal.Gui/README.md** - Project-specific README
- **docfx/docs/** - Comprehensive conceptual documentation
- **Examples/** - Sample applications demonstrating features
- **UICatalog** - Interactive demo showcasing all controls

## Tips for Contributors

1. **Start Small** - Look for `good first issue` or `up for grabs` labels
2. **Read the Docs** - Familiarize yourself with existing documentation
3. **Ask Questions** - Use GitHub Issues with the `design` label for architecture discussions
4. **Test Thoroughly** - Run UICatalog and add test scenarios
5. **Follow Patterns** - Study existing code for established patterns
6. **Document Well** - Clear documentation prevents future confusion
7. **Be Patient** - Reviews may take time; respond to feedback constructively

## Current State (v2)

- **Status:** Alpha (stable core API, may have breaking changes before Beta)
- **Recommendation:** New projects should target v2 Alpha
- **v1 Status:** Maintenance mode only (PRs for critical bugs only)

## License

Licensed under MIT License. See LICENSE file in repository root.
