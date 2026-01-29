---
name: csharp-type-design-performance
description: Design .NET types for performance. Seal classes, use readonly structs, prefer static pure functions, avoid premature enumeration. Critical for Terminal.Gui's AOT compatibility and UI performance.
---

# Type Design for Performance

> **Terminal.Gui Context:** Terminal.Gui is AOT-compatible (`IsAotCompatible=true`) and trimmable. Performance matters for a UI framework that redraws frequently.

## When to Use This Skill

Use this skill when:
- Designing new types and APIs
- Reviewing code for performance issues
- Choosing between class, struct, and record
- Working with collections and enumerables

---

## Core Principles

1. **Seal your types** - Unless explicitly designed for inheritance
2. **Prefer readonly structs** - For small, immutable value types
3. **Prefer static pure functions** - Better performance and testability
4. **Defer enumeration** - Don't materialize until you need to
5. **Return immutable collections** - From API boundaries

---

## Seal Classes by Default

Sealing classes enables JIT devirtualization and communicates API intent.

```csharp
// DO: Seal classes not designed for inheritance
public sealed class ColorScheme
{
    public Color Foreground { get; init; }
    public Color Background { get; init; }
}

// DO: Seal records (they're classes)
public sealed record ViewEventArgs(View Source, Rectangle Bounds);

// DON'T: Leave unsealed without reason
public class ColorScheme  // Can be subclassed - intentional?
{
    public virtual Color Foreground { get; }  // Virtual = slower
}
```

---

## Readonly Structs for Value Types

Structs should be `readonly` when immutable. This prevents defensive copies.

```csharp
// DO: Readonly struct for immutable value types
public readonly record struct Point(int X, int Y)
{
    public Point Offset(int dx, int dy) => new (X + dx, Y + dy);
}

// DO: Readonly struct for small, short-lived data
public readonly struct Rectangle
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

// DON'T: Mutable struct (causes defensive copies)
public struct Point  // Not readonly!
{
    public int X { get; set; }  // Mutable!
    public int Y { get; set; }
}
```

### When to Use Structs

| Use Struct When | Use Class When |
|-----------------|----------------|
| Small (<=16 bytes typically) | Larger objects |
| Short-lived | Long-lived |
| Frequently allocated | Shared references needed |
| Value semantics required | Identity semantics required |
| Immutable | Mutable state |

---

## Prefer Static Pure Functions

Static methods with no side effects are faster and more testable.

```csharp
// DO: Static pure function
public static class LayoutCalculator
{
    public static Rectangle CalculateBounds(Dim width, Dim height, Rectangle container)
    {
        int w = width.Calculate(container.Width);
        int h = height.Calculate(container.Height);
        return new Rectangle(container.X, container.Y, w, h);
    }
}
```

**Benefits:**
- No vtable lookup (faster)
- No hidden state
- Easier to test (pure input -> output)
- Thread-safe by design

---

## Defer Enumeration

Don't materialize enumerables until necessary.

```csharp
// BAD: Premature materialization
public IReadOnlyList<View> GetVisibleViews()
{
    return _subviews
        .Where(v => v.Visible)
        .ToList()  // Materialized!
        .OrderBy(v => v.TabIndex)  // Another iteration
        .ToList();  // Materialized again!
}

// GOOD: Defer until the end
public IReadOnlyList<View> GetVisibleViews()
{
    return _subviews
        .Where(v => v.Visible)
        .OrderBy(v => v.TabIndex)
        .ToList();  // Single materialization
}

// GOOD: Return IEnumerable if caller might not need all items
public IEnumerable<View> GetVisibleViews()
{
    return _subviews
        .Where(v => v.Visible)
        .OrderBy(v => v.TabIndex);
    // Caller decides when to materialize
}
```

---

## ValueTask vs Task

Use `ValueTask` for hot paths that often complete synchronously.

```csharp
// DO: ValueTask for cached/synchronous paths
public ValueTask<View?> GetCachedViewAsync(string id)
{
    if (_cache.TryGetValue(id, out View? view))
    {
        return ValueTask.FromResult<View?>(view);  // No allocation
    }

    return new ValueTask<View?>(FetchViewAsync(id));
}

// DO: Task for real I/O (simpler, no footguns)
public Task<ConfigurationManager> LoadConfigAsync()
{
    // This always hits the file system
    return ConfigurationManager.LoadAsync();
}
```

**ValueTask rules:**
- Never await a ValueTask more than once
- Never use `.Result` or `.GetAwaiter().GetResult()` before completion
- If in doubt, use Task

---

## Collection Return Types

### Return Immutable Collections from APIs

```csharp
// DO: Return immutable collection
public IReadOnlyList<View> SubViews => _subviews.AsReadOnly();

// DO: Use frozen collections for static data (.NET 8+)
private static readonly FrozenDictionary<Key, Action> _keyBindings =
    new Dictionary<Key, Action>
    {
        [Key.Enter] = HandleEnter,
        [Key.Escape] = HandleEscape,
    }.ToFrozenDictionary();

// DON'T: Return mutable collection
public List<View> SubViews => _subviews;  // Caller can modify!
```

### Collection Guidelines

| Scenario | Return Type |
|----------|-------------|
| API boundary | `IReadOnlyList<T>`, `IReadOnlyCollection<T>` |
| Static lookup data | `FrozenDictionary<K,V>`, `FrozenSet<T>` |
| Internal building | `List<T>`, then return as readonly |
| Single item or none | `T?` (nullable) |
| Zero or more, lazy | `IEnumerable<T>` |

---

## Span and Memory for Performance

```csharp
// DO: Accept Span for synchronous operations
public static int ParseInt(ReadOnlySpan<char> text)
{
    return int.Parse(text);
}

// DO: Use stackalloc for small buffers
public void RenderLine(int y)
{
    Span<Cell> buffer = stackalloc Cell[Width];
    // Fill buffer...
    Driver.WriteRow(y, buffer);
}

// DO: Use ArrayPool for larger buffers
public void RenderScreen()
{
    Cell[] buffer = ArrayPool<Cell>.Shared.Rent(Width * Height);
    try
    {
        // Use buffer...
    }
    finally
    {
        ArrayPool<Cell>.Shared.Return(buffer);
    }
}
```

---

## Quick Reference

| Pattern | Benefit |
|---------|---------|
| `sealed class` | Devirtualization, clear API |
| `readonly record struct` | No defensive copies, value semantics |
| Static pure functions | No vtable, testable, thread-safe |
| Defer `.ToList()` | Single materialization |
| `ValueTask` for hot paths | Avoid Task allocation |
| `Span<T>` for buffers | Stack allocation, no copying |
| `IReadOnlyList<T>` return | Immutable API contract |
| `FrozenDictionary` | Fastest lookup for static data |

---

## Anti-Patterns

```csharp
// DON'T: Unsealed class without reason
public class ViewStyle { }  // Seal it!

// DON'T: Mutable struct
public struct Point { public int X; public int Y; }  // Make readonly

// DON'T: Instance method that could be static
public int Add(int a, int b) => a + b;  // Make static

// DON'T: Multiple ToList() calls
items.Where(...).ToList().OrderBy(...).ToList();  // One ToList at end

// DON'T: Return List<T> from public API
public List<View> GetSubViews();  // Return IReadOnlyList<T>

// DON'T: ValueTask for always-async operations
public ValueTask<View> LoadViewAsync();  // Just use Task
```

---

## Attribution

This skill is adapted from [dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) by Aaron Stannard, licensed under Apache-2.0.
