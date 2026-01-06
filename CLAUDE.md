# CLAUDE.md

> **This file provides guidance for Claude (AI) when working with the Terminal.Gui codebase.**
> For full details, see [CONTRIBUTING.md](./CONTRIBUTING.md).

## Project Overview

**Terminal.Gui** is a cross-platform UI toolkit for creating console-based graphical user interfaces in .NET.

- **Language**: C# (net8.0)
- **Platform**: Cross-platform (Windows, macOS, Linux)
- **Version**: v2 (Alpha), v1 (maintenance mode)
- **Default Branch**: `v2_develop`

## Build Commands

**Run all commands from the repository root:**

```bash
# Restore packages (required first)
dotnet restore

# Build solution (Debug)
dotnet build --configuration Debug --no-restore

# Build Release
dotnet build --configuration Release --no-restore
```

Expect ~326 warnings (nullable references, unused variables) - these are normal. 0 errors expected.

## Test Commands

```bash
# Non-parallel tests (depend on static state)
dotnet test Tests/UnitTests --no-build --verbosity normal

# Parallel tests (preferred for new tests)
dotnet test Tests/UnitTestsParallelizable --no-build --verbosity normal

# Integration tests
dotnet test Tests/IntegrationTests --no-build --verbosity normal
```

## Key Architecture Concepts

Before modifying code, understand these concepts:

- **Application Lifecycle** - `Application.Init`, `Application.Run`, `Application.Shutdown` - see `docfx/docs/application.md`
- **Cancellable Workflow Pattern (CWP)** - see `docfx/docs/cancellable-work-pattern.md`
- **View Hierarchy** - `View`, `Runnable`, `Window`, SuperView/SubViews - see `docfx/docs/View.md`
- **Layout System** - Pos, Dim, automatic layout - see `docfx/docs/layout.md`
- **Event System** - keyboard, mouse, application events - see `docfx/docs/events.md`
- **Driver Architecture** - console drivers abstract platform differences - see `docfx/docs/drivers.md`
- **Lexicon & Taxonomy** - standard terminology used in the codebase - see `docfx/docs/lexicon.md`

## Terminology

**ALWAYS use these terms consistently:**

- **SubView** - A View contained within another View. NOT "child" or "children".
- **SuperView** - The View that contains a SubView. NOT "parent" or "container".
- **Add/Remove** - Methods to add/remove SubViews. NOT "append", "insert", "attach".

```csharp
// CORRECT
View superView = new ();
View subView = new () { Title = "SubView" };
superView.Add (subView);  // subView.SuperView == superView

// WRONG - Don't use "parent", "child", or "container"
View parent = new ();      // Should be: superView
View child = new ();       // Should be: subView
View container = new ();   // Should be: superView
```

## Cancellable Workflow Pattern (CWP)

When implementing CWP events:

- **Virtual `OnXXX` methods** should be empty in base class - for subclasses to override
- **Work happens BEFORE** the `OnXXX`/Event notification, not after
- **Events** are raised after the virtual method call

```csharp
// CORRECT CWP pattern
internal void RaiseSubViewAdded (View view)
{
    // Do work BEFORE notifications
    if (AssignHotKeys)
    {
        AssignHotKeyToView (view);
    }

    OnSubViewAdded (view);  // Virtual method (empty in base)
    SubViewAdded?.Invoke (this, new (this, view));  // Event
}
```

## Critical Coding Conventions

### Type Declarations

**ALWAYS use explicit types** - Never use `var` except for built-in simple types (`int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`):

```csharp
// CORRECT
View view = new () { Width = 10 };
List<View?> views = new ();
var count = 0;  // OK - int is built-in

// WRONG
var view = new View { Width = 10 };
var views = new List<View?>();
```

**ALWAYS use target-typed `new ()`**:

```csharp
// CORRECT
View view = new () { Width = 10 };

// WRONG
View view = new View() { Width = 10 };
```

**ALWAYS use collection initializers**:

```csharp
// CORRECT
List<View> views = [
    new Button("OK"),
    new Button("Cancel")
];

// WRONG
List<View> views = new ();
views.Add(new Button("OK"));
```

### Code Style

- Follow `.editorconfig` and `Terminal.sln.DotSettings`
- Only format files you modify
- All public APIs must have XML documentation

## Testing Requirements

- **Never decrease code coverage** - maintain or increase
- **Target 70%+ coverage** for new code
- **Add AI-generated comment** to tests: `// Claude - Opus 4.5`
- **Prefer `UnitTestsParallelizable`** - avoid adding to `UnitTests`
- **Avoid static dependencies** - don't use `Application.Init` or `ConfigurationManager` unless testing that functionality
- **Don't use `[AutoInitShutdown]` or `[SetupFakeApplication]`** - legacy patterns

## Repository Structure

- `/Terminal.Gui/` - Core library (496 C# files)
- `/Tests/UnitTests/` - Non-parallel tests
- `/Tests/UnitTestsParallelizable/` - Parallel tests (preferred)
- `/Examples/UICatalog/` - Demo app for manual testing
- `/docfx/docs/` - Conceptual documentation

## What NOT to Do

- Don't modify unrelated code
- Don't remove/edit unrelated tests
- Don't add tests to `UnitTests` if they can be parallelizable
- Don't use `Application.Init` in new tests
- Don't decrease code coverage
- Don't use `var` for non-built-in types
- Don't use redundant type names with `new`
- Don't introduce new warnings (fix warnings in files you modify)
