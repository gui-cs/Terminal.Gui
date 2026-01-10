# Scenario Modernization Guide

This guide provides instructions for upgrading UICatalog scenarios to follow modern Terminal.Gui coding standards.

## Pre-requisites

1. Ensure `#nullable enable` is at the top of the file
2. Review CLAUDE.md for coding conventions

## Checklist for Each Scenario

### 1. Application Initialization Pattern

**Required pattern:**
```csharp
public override void Main ()
{
    // Init
    Application.Init ();

    // Prepping for modern app model
    using IApplication app = Application.Instance;

    // ... setup code ...

    // Run
    app.Run (mainView);
    mainView.Dispose ();
}
```

**Issues to check:**
- [ ] Any field initializers that access `Application.*` must be moved to after `Application.Init()`
- [ ] Use `using IApplication app = Application.Instance;` pattern
- [ ] Dispose views after `app.Run()` completes
- [ ] The main Runnable (e.g. `mainView`) should be created with `using` unless it's a class field. It should be named `appWindow`, `runnable`, or something that clearly indicates its purpose (many existing Scenarios inproperly name it `app`).

### 2. Type Declarations (from CLAUDE.md)

**Never use `var` except for built-in types:** `int`, `string`, `bool`, `double`, `float`, `decimal`, `char`, `byte`

```csharp
// CORRECT
Label label = new () { Text = "Hello" };
List<View> views = [];
var count = 0;  // OK - int is built-in
var text = "hello";  // OK - string is built-in

// WRONG
var label = new Label { Text = "Hello" };
var views = new List<View>();
```

**Use target-typed `new ()` when type can be inferred:**
```csharp
// CORRECT
Label label = new () { Text = "Hello" };
Window window = new () { Title = "Test" };

// WRONG
Label label = new Label() { Text = "Hello" };
```

**Exception:** In collection initializers where the element type cannot be inferred, use explicit type:
```csharp
// MenuItem must be explicit here because the array type inference needs it
new PopoverMenu ([
    new MenuItem { Title = "Item 1" },  // Explicit type required
    new Line (),
    new MenuItem { Title = "Item 2" }
]);
```

### 3. Pattern Matching

Use explicit types in pattern matching for non-built-in types:
```csharp
// CORRECT
if (e.Context is CommandContext<KeyBinding> { Binding.Key: Key key })

// WRONG (var for non-built-in type)
if (e.Context is CommandContext<KeyBinding> { Binding.Key: var key })
```

### 4. Code Simplification

**Return collections directly:**
```csharp
// CORRECT
return new (items.ToArray ());

// WRONG
Menu menu = new (items.ToArray ());
return menu;
```

**Return collection initializers directly:**
```csharp
// CORRECT
public override List<Key> GetDemoKeyStrokes (IApplication? app)
{
    return
    [
        Key.F10.WithShift,
        Key.Esc
    ];
}

// WRONG
public override List<Key> GetDemoKeyStrokes (IApplication? app)
{
    List<Key> keys =
    [
        Key.F10.WithShift,
        Key.Esc
    ];
    return keys;
}
```

### 5. Trailing Whitespace

Remove all trailing whitespace, including from comment lines:
```csharp
// CORRECT
//
// Next line of comment

// WRONG (space after //)
//
// Next line of comment
```

### 6. Field Initialization

Move any field initializers that depend on `Application` to after `Application.Init()`:

```csharp
// WRONG - Application not initialized yet
private readonly List<CultureInfo>? _cultures = Application.SupportedCultures;

// CORRECT
private List<CultureInfo>? _cultures;

public override void Main ()
{
    Application.Init ();
    using IApplication app = Application.Instance;
    _cultures = Application.SupportedCultures;
    // ...
}
```

## Quick Reference Commands

Check for trailing whitespace:
```bash
grep -n ' $' "Examples/UICatalog/Scenarios/YourScenario.cs"
```

Check for `var` usage (review each for built-in types):
```bash
grep -n '\bvar\b' "Examples/UICatalog/Scenarios/YourScenario.cs"
```

## Testing

After upgrading, ensure:
1. Build succeeds with no new errors
2. Integration tests pass: `dotnet test Tests/IntegrationTests --no-build`
3. Manually test the scenario in UICatalog if behavior changed
