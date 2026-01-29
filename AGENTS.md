# Terminal.Gui - AI Agent Instructions

> **📘 Source of Truth: [CONTRIBUTING.md](CONTRIBUTING.md)**
> 
> This file provides quick-reference coding conventions for AI agents.
> For complete guidelines, architecture concepts, and workflows, see [CONTRIBUTING.md](CONTRIBUTING.md).

## Project Essentials

**Terminal.Gui** - Cross-platform console UI toolkit for .NET (C# 12, net8.0)

**Build:** `dotnet restore && dotnet build --no-restore`  
**Test:** `dotnet test --no-build`  
**Details:** [Build & Test Workflow](.claude/workflows/build-test-workflow.md)

## Quick Rules

**⚠️ READ THIS BEFORE MODIFYING ANY FILE - These are Terminal.Gui-specific conventions:**

1. **No `var`** - Use explicit types except for: `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`
2. **Use `new ()`** - Target-typed new when type is on left side (not `new TypeName()`)
3. **Use `[...]`** - Collection expressions, not `new () { ... }`
4. **SubView/SuperView** - Never say "child", "parent", or "container"
5. **Unused lambda params** - Use `_` discard: `(_, _) => { }`
6. **Local functions** - Use camelCase: `void myLocalFunc ()`
7. **Backing fields** - Place immediately before their property

## Detailed Coding Rules

Consult these files in `.claude/rules/` before editing code:

- [Type Declarations](/.claude/rules/type-declarations.md) - `var` vs explicit types
- [Target-Typed New](/.claude/rules/target-typed-new.md) - `new()` syntax
- [Collection Expressions](/.claude/rules/collection-expressions.md) - `[...]` syntax
- [Terminology](/.claude/rules/terminology.md) - SubView/SuperView terms
- [Event Patterns](/.claude/rules/event-patterns.md) - Lambdas, handlers, closures
- [CWP Pattern](/.claude/rules/cwp-pattern.md) - Cancellable Workflow Pattern
- [Code Layout](/.claude/rules/code-layout.md) - Member ordering, backing fields
- [Testing Patterns](/.claude/rules/testing-patterns.md) - Test writing conventions
- [API Documentation](/.claude/rules/api-documentation.md) - XML doc requirements

## Workflows

Process guides in `.claude/workflows/`:

- [Build & Test Workflow](/.claude/workflows/build-test-workflow.md) - Build, test, and troubleshooting
- [PR Workflow](/.claude/workflows/pr-workflow.md) - Submitting pull requests

## Task-Specific Guides

Check `.claude/tasks/` for specialized task checklists before starting work.

---

## API Quick Reference

> **Full Reference:** [.claude/api-reference.md](.claude/api-reference.md)
> **Deep Dives:** `docfx/docs/*.md`

### Architecture

```csharp
// Instance-based (v2 - recommended)
using (IApplication app = Application.Create ().Init ())
{
    app.Run<MyDialog> ();
    MyResult? result = app.GetResult<MyResult> ();
}
```

- **`View.App`** - Access application context (not static `Application`)
- **`IApplication.TopRunnable`** - Current modal (top of SessionStack)
- **`IRunnable<TResult>`** - Views with typed results

### View Hierarchy (CRITICAL TERMINOLOGY)

| Term | Meaning | Usage |
|------|---------|-------|
| **SuperView** | Container (via `Add()`) | `view.SuperView` |
| **SubView** | Contained view | `superView.Add(subView)` |
| **Parent/Child** | Non-containment refs | Rare - avoid for containment |

### View Composition

```
Frame → Margin → Border → Padding → Viewport → Content Area
```

- **Frame** - SuperView-relative location/size
- **Viewport** - Visible content window
- **Content Area** - Total drawable area (larger = scrolling)

### Layout (Pos/Dim)

```csharp
view.X = Pos.Right (other) + 1;   // Relative positioning
view.Width = Dim.Fill ();         // Fill available space
view.Height = Dim.Auto ();        // Size to content
```

### Commands & Events

| Command | Trigger | Purpose |
|---------|---------|---------|
| `Activate` | Space, Click | State change (toggle, select) |
| `Accept` | Enter | Confirm action (submit) |
| `HotKey` | Alt+Key | Direct access |

```csharp
AddCommand (Command.Accept, ctx => { /* handle */ return true; });
KeyBindings.Add (Key.Enter, Command.Accept);
```

### Cancellable Work Pattern (CWP)

```csharp
// Structure: Work → Virtual → Event
internal void RaiseXxx ()
{
    DoWork ();           // 1. Work BEFORE notifications
    OnXxx (args);        // 2. Virtual method (override in subclass)
    Xxx?.Invoke (args);  // 3. Event for external subscribers
}
```

### Navigation & Focus

- **`CanFocus`** - View can receive focus
- **`TabStop`** - Keyboard nav behavior (`NoStop`, `TabStop`, `TabGroup`)
- **`SetFocus()`** - Request focus
- **Tab/Shift+Tab** - Navigate TabStop views
- **F6/Shift+F6** - Navigate TabGroup containers

### Drawing

```csharp
protected override bool OnDrawingContent ()
{
    Move (0, 0);                              // Position draw cursor
    SetAttributeForRole (VisualRole.Normal);  // Set colors
    AddStr ("Hello");                         // Draw text
    return true;
}
```

### Testing

```csharp
VirtualTimeProvider time = new ();
using IApplication app = Application.Create (time);
app.Init (DriverRegistry.Names.ANSI);
app.InjectKey (Key.Enter);  // Input injection
```

**Avoid** `Application.Init()` in tests unless testing Application-specific functionality.
