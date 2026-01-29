---
name: csharp-api-design
description: Design stable, compatible public APIs using extend-only design principles. Manage API compatibility, wire compatibility, and versioning for NuGet packages. Critical for Terminal.Gui as a public library.
---

# Public API Design and Compatibility

## When to Use This Skill

Use this skill when:
- Designing public APIs for Terminal.Gui
- Making changes to existing public APIs
- Reviewing pull requests for breaking changes
- Planning versioning strategies

---

## The Three Types of Compatibility

| Type | Definition | Scope |
|------|------------|-------|
| **API/Source** | Code compiles against newer version | Public method signatures, types |
| **Binary** | Compiled code runs against newer version | Assembly layout, method tokens |
| **Wire** | Serialized data readable by other versions | Persistence formats (e.g., config.json) |

Breaking any of these creates upgrade friction for users.

---

## Extend-Only Design

The foundation of stable APIs: **never remove or modify, only extend**.

### Three Pillars

1. **Previous functionality is immutable** - Once released, behavior and signatures are locked
2. **New functionality through new constructs** - Add overloads, new types, opt-in features
3. **Removal only after deprecation period** - Years, not releases

---

## API Change Guidelines

### Safe Changes (Any Release)

```csharp
// ADD new overloads with default parameters
public void Draw(Rectangle bounds, CancellationToken ct = default);

// ADD new optional parameters to existing methods
public void Add(View view, bool assignHotKey = true);

// ADD new types, interfaces, enums
public interface IViewFilter { }
public enum BorderStyle { None, Single, Double, Rounded }

// ADD new members to existing types
public class View
{
    public Thickness? Margin { get; init; }  // NEW
}
```

### Unsafe Changes (Never or Major Version Only)

```csharp
// REMOVE or RENAME public members
public void DrawView(Rectangle bounds);  // Was: Draw()

// CHANGE parameter types or order
public void Add(bool assignHotKey, View view);  // Was: Add(View view)

// CHANGE return types
public View? GetFocused();  // Was: public View GetFocused()

// CHANGE access modifiers
internal class Application { }  // Was: public

// ADD required parameters without defaults
public void Add(View view, ILogger logger);  // Breaks callers!
```

### Deprecation Pattern

```csharp
// Step 1: Mark as obsolete with version
[Obsolete("Obsolete since v2.1.0. Use DrawContentAsync instead.")]
public void DrawContent() { }

// Step 2: Add new recommended API (same release)
public Task DrawContentAsync(CancellationToken ct = default);

// Step 3: Remove in next major version (v3.0+)
```

---

## Encapsulation Patterns

### Sealing Classes

```csharp
// DO: Seal classes not designed for inheritance
public sealed class ColorScheme { }

// DON'T: Leave unsealed by accident
public class ColorScheme { }  // Users might inherit, blocking changes
```

### Interface Segregation

```csharp
// DO: Small, focused interfaces
public interface IViewDrawable
{
    void Draw();
}

public interface IViewLayout
{
    void Layout();
}

// DON'T: Monolithic interfaces (can't add methods without breaking)
public interface IView
{
    void Draw();
    void Layout();
    // Adding new methods breaks all implementations!
}
```

---

## Versioning Strategy

### Semantic Versioning (Practical)

| Version | Changes Allowed |
|---------|----------------|
| **Patch** (2.0.x) | Bug fixes, security patches |
| **Minor** (2.x.0) | New features, deprecations |
| **Major** (x.0.0) | Breaking changes, old API removal |

### Key Principles

1. **No surprise breaks** - Even major versions should be announced
2. **Extensions anytime** - New APIs can ship in any release
3. **Deprecate before remove** - `[Obsolete]` for at least one minor version
4. **Communicate timelines** - Users need to plan upgrades

---

## Pull Request Checklist

When reviewing PRs that touch public APIs:

- [ ] **No removed public members** (use `[Obsolete]` instead)
- [ ] **No changed signatures** (add overloads instead)
- [ ] **No new required parameters** (use defaults)
- [ ] **Breaking changes documented** (release notes, migration guide)
- [ ] **XML documentation updated** for new/changed APIs

---

## Anti-Patterns

### Breaking Changes Disguised as Fixes

```csharp
// "Bug fix" that breaks users
public async Task<View> GetViewAsync(string id)  // Was sync!
{
    // "Fixed" to be async - but breaks all callers
}

// Correct: Add new method, deprecate old
[Obsolete("Use GetViewAsync instead")]
public View GetView(string id) => GetViewAsync(id).Result;

public async Task<View> GetViewAsync(string id, CancellationToken ct = default) { }
```

### Silent Behavior Changes

```csharp
// Changing defaults breaks users who relied on old behavior
public void Configure(bool enableBorder = true)  // Was: false!

// Correct: New parameter with new name
public void Configure(
    bool enableBorder = false,  // Original default preserved
    bool enableNewBorder = true)  // New behavior opt-in
```

---

## Resources

- [Extend-Only Design](https://aaronstannard.com/extend-only-design/)
- [OSS Compatibility Standards](https://aaronstannard.com/oss-compatibility-standards/)
- [Semantic Versioning](https://semver.org/)

---

## Attribution

This skill is adapted from [dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) by Aaron Stannard, licensed under Apache-2.0.
