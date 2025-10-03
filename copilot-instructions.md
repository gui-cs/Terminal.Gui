# Terminal.Gui - GitHub Copilot Instructions

This file provides instructions for GitHub Copilot when working with the Terminal.Gui project.

## Project Overview

**Terminal.Gui** is a cross-platform UI toolkit for creating console-based graphical user interfaces in .NET. It provides a comprehensive framework for building interactive console applications with support for keyboard and mouse input, customizable views, and a robust event system. The toolkit works across Windows, macOS, and Linux, leveraging platform-specific console capabilities where available.

**Key characteristics:**
- Cross-platform terminal/console UI framework for .NET
- Supports Windows, macOS, and Linux
- Rich GUI controls (buttons, dialogs, menus, text boxes, etc.)
- Keyboard-first design with full mouse support
- Follows Microsoft .NET Framework Design Guidelines, with some tweaks.
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
- `/Examples/UICatalog/` - Comprehensive demo app for manual testing
- `/Tests/` - Unit and integration tests
- `/docfx/` - Documentation source files (Deep Dive Articles and API docs)
- `/Scripts/` - Build and utility scripts

## Branching Model

**Terminal.Gui uses GitFlow:**
- `v2_develop` - Default branch for v2 development (active development)
- `v2_release` - Stable release branches matching NuGet packages

## Code Style and Standards

### Code Style Tenets

1. **Six-Year-Old Reading Level** - Prioritize readability over terseness. Use clear variable names and comments.
2. **Consistency, Consistency, Consistency** - Follow established patterns ruthlessly.
3. **Don't be Weird** - Follow Microsoft and .NET community conventions.
4. **Set and Forget** - Use ReSharper/Rider for automated formatting.
5. **Documentation is the Spec** - API documentation is the source of truth.

### Coding Conventions

- Based on [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Project settings defined and enforced via `./Terminal.sln.DotSettings` and `./.editorconfig`
- Use `var` only for the most basic dotnet types - prefer explicit types for clarity
- Use target-typed new

## API Design Guidelines

### Public API Tenets

1. **Stand on the shoulders of giants** - Follow [Microsoft .NET Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
2. **Don't Break Existing Stuff** - Avoid breaking changes; find compatible ways to add features
3. **Fail-fast** - Prefer early failure to expose bugs sooner
4. **Standards Reduce Complexity** - Use standard .NET idioms, tweaked to match Terminal.Gui. 

### API Documentation Requirements

**All public APIs must have XML documentation:**
- Clear, concise, and complete `<summary>` tags
- Use `<see cref=""/>` liberally for cross-references
- Add `<remarks>` for context and detailed explanations
- Document complex topics in `docfx/articles/*.md` files
- Use proper English and correct grammar
- Provide sample code via `<example>` in cases where a sample is needed (not for very obvious things)

### Events

- Follow the [Events Deep Dive](https://gui-cs.github.io/Terminal.Gui/docs/events.html) documentation
- Use the Cancellable Work Pattern for user-initiated actions
- Use the CWPHelpers if possible
- Name event handlers consistently (e.g., `On[EventName]`), following dotnet guidelines.

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
- Many existing unit tests are obtuse and not really unit tests. Anytime new tests are added or updated, strive to refactor the tests into more granular tests where each test covers the smallest area possible. 
- Many existing unit tests in the `./Tests/UnitTests` project incorrectly require `Application.Init` and use `[AutoInitShutdown]`. Anytime new tests are added or updated, strive to remove these dependencies and make the tests parallelizable. This means not taking any dependency on static objects like `Application` and `ConfigurationManager`. 

## Pull Request Guidelines

- Titles should be of the form "Fixes #issue. Terse description." 
- If the PR addresses multiple issues, use "Fixes #issue1, #issue2. Terse description."
- First comment should include "- Fixes #issue" for each issue addressed. If an issue is only partially addressed, use "Partially addresses #issue".
- First comment should include a thorough description of the change and any impact. 
- Put temporary .md files in `/docfx/docs/drafts/` and remove before merging.

## Building and Running

### Build the Solution
```powershell
dotnet build
```

### Run Tests
```powershell
dotnet test
```

## Key Concepts

`./docfx/docs` contains a set of architectural and key-concept deep-dives. 

## Additional Guidelines
1. Maintain existing code structure and organization unless explicitly told
2. View sub-classes must not use private APIs
3. Suggest changes to the `./docfx/docs/` folder when appropriate
