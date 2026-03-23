# Terminal.Gui — Copilot Instructions

Cross-platform .NET console UI toolkit. C# 12 targeting net8.0.
Full contribution guide: [CONTRIBUTING.md](../CONTRIBUTING.md).
Architecture deep dives: `docfx/docs/`.

## Build & Test

Run all commands from repository root.

```bash
# Restore + build
dotnet restore
dotnet build --no-restore

# Run all tests (two separate projects)
dotnet test --project Tests/UnitTestsParallelizable --no-build
dotnet test --project Tests/UnitTests --no-build

# Run a single test by fully-qualified name
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter "FullyQualifiedName~MyTestClass.MyTestMethod"

# Run tests matching a trait or pattern
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter "ClassName~ButtonTests"
```

New tests go in `Tests/UnitTestsParallelizable` (no static state dependencies). Only use `Tests/UnitTests` when testing `Application.Init`/`Shutdown` or other static state.

## Architecture Overview

### Application lifecycle

`Application.Init` → `Application.Run` → `Application.Shutdown`. The instance-based `IApplication` is replacing the static `Application` facade. Tests should avoid `Application.Init` unless explicitly testing that path.

### View system

`View` is the base class for all UI elements. Views form a tree via `Add()`/`Remove()`. Every View has three adornment layers: `Margin` → `Border` → `Padding` → content area. Layout uses `Pos` (position) and `Dim` (dimension) objects for declarative relative layout.

### Driver architecture

Platform-specific terminal I/O is abstracted behind `IDriver`. Implementations: `WindowsDriver`, `UnixDriver` (curses-free), `AnsiDriver`, `NetDriver` (pure .NET `System.Console`). Drivers are registered via `DriverRegistry` and selected automatically by platform.

### Cancellable Workflow Pattern (CWP)

The standard event pattern throughout the codebase. Order: **do work → call virtual `OnXxx` → raise event**. The virtual method is empty in the base class (for subclass override). Work happens *before* notifications, not after.

```csharp
internal void RaiseSubViewAdded (View view)
{
    // 1. Work first
    if (AssignHotKeys) { AssignHotKeyToView (view); }

    // 2. Virtual method (empty in base)
    OnSubViewAdded (view);

    // 3. Event
    SubViewAdded?.Invoke (this, new (this, view));
}
```

### Command/input system

Input flows: Driver → `IInputProcessor` → `KeyBindings`/`MouseBindings` → `Command` enum → handler. Views bind keys and mouse actions to `Command` values via `KeyBindings.Add` and `MouseBindings.Add`.

## Code Style (Non-Obvious Conventions)

### Spacing before parentheses and brackets — the #1 mistake

This codebase requires a space *before* every `()` and `[]`:

```csharp
// ✅ Correct
void MyMethod ()
int result = Calculate (x, y);
List<int> items = GetItems ();
int val = array [index];
if (condition) { }

// ❌ Wrong
void MyMethod()
int result = Calculate(x, y);
var items = GetItems();
int val = array[index];
```

### No `var` except for built-in numeric/string types

Use explicit types. `var` is only acceptable for: `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`.

```csharp
// ✅
View view = new () { Width = 10 };
List<View?> views = new ();
var count = 0;          // OK — int

// ❌
var view = new View () { Width = 10 };
var views = new List<View?> ();
```

### Target-typed `new ()`

When the type is on the left side, use `new ()` not `new TypeName()`:

```csharp
// ✅
Button btn = new () { Text = "OK" };

// ❌
Button btn = new Button () { Text = "OK" };
```

### Collection expressions

Use `[...]` syntax:

```csharp
// ✅
List<View> views = [new Button ("OK"), new Button ("Cancel")];

// ❌
List<View> views = new () { new Button ("OK"), new Button ("Cancel") };
```

### Early return

Prefer early return / guard clauses over nested `if`/`else`. Less nesting, clearer code:

```csharp
// ✅
if (view is null)
{
    return;
}

DoWork (view);

// ❌
if (view is not null)
{
    DoWork (view);
}
```

### One type per file

Public and internal types each get their own file. The filename must match the type name (e.g., `Button.cs` for `class Button`). Private nested types are fine inside their containing type's file.

### Allman brace style

All opening braces go on the next line. No exceptions.

### Blank lines

- 1 blank line *before* `return`, `break`, `continue`, `throw`
- 1 blank line *after* `if`/`for`/`while`/`foreach` blocks

### Unused lambda parameters → discard `_`

```csharp
textField.TextChanged += (_, _) => { /* ... */ };
```

### Local functions use camelCase

```csharp
void myLocalFunc () { }
```

### Backing fields directly above their property

```csharp
private string _name;
public string Name
{
    get => _name;
    set => _name = value;
}
```

## Terminology

| Use | Don't use | Meaning |
|-----|-----------|---------|
| **SuperView** | parent, container | The view that contains others via `Add()` |
| **SubView** | child, element | A view added to a SuperView via `Add()` |

"Parent/Child" is reserved for rare non-containment reference relationships.

## Testing Conventions

- Add a comment identifying AI-generated tests: `// Copilot`
- Each test covers the smallest unit possible
- Don't use `[AutoInitShutdown]` or `[SetupFakeApplication]` (legacy, being phased out)
- Avoid `Application.Init` in tests unless testing that specific functionality
- Never decrease code coverage
- Do not use Console.Error.WriteLine or Console.WriteLine for debug output in Terminal.Gui code. Use project's Logging infrastructure instead: `Terminal.Gui.App.Logging`, `Terminal.Gui.Tests.TestLogging` and `Terminal.Gui.Tracing.Tracing.Trace`.
- `Tracing.Trace` is only available in DEBUG builds; do not use it to validate test results as all tests must pass in RELEASE builds.
 
## Unicode & Grapheme Handling

- Measure display width with `string.GetColumns ()`, never `EnumerateRunes().Sum(r => r.GetColumns())`
- Render text by iterating graphemes via `GraphemeHelper.GetGraphemes ()` and `AddStr`, not rune-by-rune with `AddRune`

## PR Requirements

- PRs must not introduce new compiler warnings (fix warnings in files you modify)
- Title format: `Fixes #issue. Terse description`
- Update `Examples/UICatalog` scenarios when adding user-visible features 