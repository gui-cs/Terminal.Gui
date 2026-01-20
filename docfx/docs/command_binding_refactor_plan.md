# Command and Binding Refactor Plan

> **Status**: Design In Progress
>
> **Branch**: `copilot/fix-command-propagation-issue`
>
> **Related**: [command-propagation-analysis.md](./command-propagation-analysis.md)
>
> **Last Updated**: 2026-01-09

## Table of Contents

- [Executive Summary](#executive-summary)
- [Current State](#current-state)
- [Problems to Solve](#problems-to-solve)
- [Design Decisions](#design-decisions)
- [Proposed Type System](#proposed-type-system)
- [Implementation Plan](#implementation-plan)
- [Progress Tracking](#progress-tracking)

---

## Executive Summary

This document tracks the refactoring of Terminal.Gui's command and binding system to:

1. **Simplify `CommandContext`** - Remove generics, make non-generic
2. **Unify binding source tracking** - Rationalize `KeyBinding.Target` vs `Mouse.View`
3. **Add `InputBinding`** - Generic/programmatic binding type
4. **Expose `Binding` on `ICommandContext`** - Enable polymorphic access
5. **Prepare for command propagation** - Infrastructure for `PropagatedCommands`

---

## Relationship to Command Propagation

This refactor prepares the infrastructure for the command propagation design documented in [command-propagation-analysis.md](./command-propagation-analysis.md). Key dependencies:

| Refactor Item | Propagation Impact |
|---------------|-------------------|
| **`ctx.Source` clarity** | Propagation relies on `Source` remaining constant as commands bubble up the view hierarchy |
| **Non-generic `CommandContext`** | Simplifies propagation code that pattern-matches on binding types |
| **`IInputBinding.Source`** | Provides consistent origin tracking across binding types during propagation |
| **`InputBinding`** | Provides binding type for programmatically-propagated commands (though propagation typically forwards the original binding) |

### Dependency Note

**Propagation implementation should wait for this refactor to complete.** Terminal.Gui v2 is in alpha, and this binding refactor plus command propagation are the last major items before beta. Since breaking changes are acceptable during alpha, we should complete the type system cleanup first to avoid implementing propagation against deprecated types.

**Recommended order:**
1. Complete Phases 2-6 of this refactor (non-generic `CommandContext`, `InputBinding`, etc.)
2. Implement `PropagatedCommands` using the clean type system
3. Remove deprecated types before beta

### How Propagation Uses These Types

During command propagation:

1. The original `ICommandContext` (including its `Source` and `Binding`) is passed unchanged to `SuperView.InvokeCommand()`
2. `sender` changes at each propagation step, but `ctx.Source` remains the original invoker
3. Pattern matching on `ctx.Binding` works at any level: `if (ctx.Binding is KeyBinding kb)`

See the "Identifying the Originating View in Handlers" section in [command-propagation-analysis.md](./command-propagation-analysis.md) for detailed guidance.

---

## Current State

### Type Hierarchy (Before)

```
IInputBinding (interface)
├── Commands: Command[]
├── Data: object?
└── (no Source property)

KeyBinding : IInputBinding (record struct)
├── Commands, Data
├── Key: Key?
└── Target: View?  ← Used for app-level hotkeys

MouseBinding : IInputBinding (record struct)
├── Commands, Data
└── MouseEventArgs: Mouse?  ← Mouse.View tracks source

ICommandContext (interface)
├── Command: Command
└── Source: View?

CommandContext<TBindingType> : ICommandContext (record struct)
├── Command, Source
└── Binding: TBindingType?  ← Generic, causes variance issues
```

### Current Issues

| Issue | Description |
|-------|-------------|
| Generic variance | `CommandContext<MouseBinding>` is NOT a subtype of `CommandContext<IInputBinding>` |
| No polymorphic binding access | `ICommandContext` doesn't expose `Binding` |
| Inconsistent source tracking | `KeyBinding.Target` vs `Mouse.View` in `MouseBinding.MouseEventArgs` |
| Redundant `Source` | Both `ICommandContext.Source` and binding track origin |
| Naming inconsistency | `MouseEventArgs` should be `MouseEvent` |

---

## Problems to Solve

### Problem 1: Generic Variance Blocks Pattern Matching

```csharp
// This fails - generics are invariant
if (args.Context is CommandContext<IInputBinding> ctx)  // FALSE for MouseBinding!
```

**Solution**: Non-generic `CommandContext` with `IInputBinding Binding` property.

### Problem 2: Can't Access Binding Polymorphically

```csharp
// ICommandContext doesn't have Binding
if (args.Context is ICommandContext ctx)
{
    var binding = ctx.Binding;  // ERROR: no such property
}
```

**Solution**: Add `IInputBinding Binding { get; }` to `ICommandContext`.

### Problem 3: Inconsistent Source Tracking

| Binding Type | Where Source Is Tracked |
|--------------|------------------------|
| `KeyBinding` | `Target` property |
| `MouseBinding` | `MouseEventArgs.View` (nested in `Mouse`) |
| Programmatic | `ICommandContext.Source` (no binding) |

**Solution**: Add `Source` to `IInputBinding` interface. Keep `KeyBinding.Target` for backward compatibility (it serves a specific purpose for app-level hotkeys).

### Problem 4: Programmatic Invocations Have No Binding

```csharp
view.InvokeCommand(Command.Accept);  // No binding, Source comes from context
```

**Solution**: `InputBinding` type for programmatic invocations. All invocations have a binding.

---

## Design Decisions

### Decision 1: Non-Generic CommandContext

**Choice**: Remove `CommandContext<T>`, use non-generic `CommandContext`.

**Rationale**:
- Generic adds complexity without significant value
- Consumers pattern-match on binding type anyway
- Eliminates variance issues

### Decision 2: Keep `ICommandContext.Source`

**Choice**: Keep `Source` on context, NOT just on binding.

**Rationale**:
- During propagation, `sender` changes but we need original invoker
- Programmatic invocations need origin tracking
- `Source` = "who first invoked this command" (fixed during propagation)
- `sender` = "who is handling this now" (changes during propagation)

### Decision 3: Add `IInputBinding.Source`

**Choice**: Add `Source: View?` to `IInputBinding` interface.

**Rationale**:
- All bindings should track their originating view
- Provides consistent access pattern
- `KeyBinding.Target` remains for its specific use case (app-level hotkey target)

### Decision 4: Keep `KeyBinding.Target`

**Choice**: Don't rename `Target` to `Source` in `KeyBinding`.

**Rationale**:
- `Target` has specific meaning: the view that should receive app-level hotkeys
- This is different from "source" (where binding was created)
- A key binding might be created by View A but target View B
- Keep both: `Source` (from interface) + `Target` (KeyBinding-specific)

### Decision 5: `InputBinding` for Programmatic Invocations

**Choice**: Create `InputBinding` as generic/base binding type.

**Rationale**:
- All invocations should have a binding (simplifies model)
- `InputBinding` serves as "programmatic" or "generic" binding
- Pattern matching cleanly discriminates: `KeyBinding`, `MouseBinding`, `InputBinding`

### Decision 6: Rename `MouseEventArgs` to `MouseEvent`

**Choice**: Rename `MouseBinding.MouseEventArgs` to `MouseBinding.MouseEvent`.

**Rationale**:
- More accurate name (it's not event args, it's the mouse event data)
- Consistent with naming conventions

---

## Proposed Type System

### IInputBinding (Updated)

```csharp
public interface IInputBinding
{
    /// <summary>The commands this binding will invoke.</summary>
    Command[] Commands { get; set; }

    /// <summary>Arbitrary context data.</summary>
    object? Data { get; set; }

    /// <summary>
    ///     The View that is the origin of this binding.
    ///     For key bindings, this is where the binding was added.
    ///     For mouse bindings, this is the view that received the mouse event.
    ///     For programmatic invocations, this is the view that called InvokeCommand.
    /// </summary>
    View? Source { get; set; }
}
```

### InputBinding (New)

```csharp
/// <summary>
///     A generic input binding used for programmatic command invocations
///     or when a specific binding type is not needed.
/// </summary>
public record struct InputBinding : IInputBinding
{
    public InputBinding(Command[] commands, View? source = null, object? data = null)
    {
        Commands = commands;
        Source = source;
        Data = data;
    }

    public Command[] Commands { get; set; }
    public object? Data { get; set; }
    public View? Source { get; set; }
}
```

### KeyBinding (Updated)

```csharp
public record struct KeyBinding : IInputBinding
{
    public Command[] Commands { get; set; }
    public object? Data { get; set; }
    public View? Source { get; set; }  // NEW: from interface
    public Key? Key { get; set; }
    public View? Target { get; set; }  // KEEP: app-level hotkey target
}
```

### MouseBinding (Updated)

```csharp
public record struct MouseBinding : IInputBinding
{
    public Command[] Commands { get; set; }
    public object? Data { get; set; }
    public View? Source { get; set; }  // NEW: from interface (replaces MouseEvent.View usage)
    public Mouse? MouseEvent { get; set; }  // RENAMED from MouseEventArgs
}
```

### ICommandContext (Updated)

```csharp
public interface ICommandContext
{
    /// <summary>The command being invoked.</summary>
    Command Command { get; }

    /// <summary>
    ///     The View that first invoked this command.
    ///     This remains constant during command propagation.
    /// </summary>
    View? Source { get; set; }

    /// <summary>
    ///     The binding that triggered the command.
    ///     Use pattern matching to access specific binding types.
    /// </summary>
    IInputBinding Binding { get; }
}
```

### CommandContext (Non-Generic)

```csharp
public record struct CommandContext : ICommandContext
{
    public CommandContext(Command command, View? source, IInputBinding binding)
    {
        Command = command;
        Source = source;
        Binding = binding;
    }

    public Command Command { get; init; }
    public View? Source { get; set; }
    public IInputBinding Binding { get; init; }
}
```

---

## Source vs Target vs Sender Clarification

| Property | Location | Meaning | Changes During Propagation? |
|----------|----------|---------|----------------------------|
| `sender` | Event handler parameter | View currently raising event | **Yes** |
| `this` | Virtual method override | View currently processing | **Yes** |
| `ctx.Source` | `ICommandContext` | View that first invoked command | **No** |
| `ctx.Binding.Source` | `IInputBinding` | View where binding originated | **No** |
| `keyBinding.Target` | `KeyBinding` only | Target view for app-level hotkey | **No** |

### Example: Propagation Scenario

```
User presses key bound in CheckBox
    ↓
CheckBox.InvokeCommand(Activate, keyBinding)
    ctx.Source = CheckBox
    ctx.Binding = KeyBinding { Source = CheckBox, Key = F5 }
    ↓
Shortcut intercepts, re-invokes
    ctx.Source = CheckBox (unchanged)
    sender = Shortcut
    ↓
StatusBar receives via propagation
    ctx.Source = CheckBox (unchanged)
    sender = StatusBar
```

---

## Implementation Plan

### Phase 1: Interface Updates (Non-Breaking Prep) ✅ COMPLETED

- [x] Add `Source` property to `IInputBinding`
- [x] Add `Source` to `KeyBinding` (separate from `Target`)
- [x] Add `Source` to `MouseBinding`
- [x] Rename `MouseBinding.MouseEventArgs` → `MouseEvent`
- [x] Update all call sites using `MouseEventArgs` pattern
- [x] Update documentation (mouse.md, events.md)

### Phase 2: New Types ✅ COMPLETED

- [x] Create `InputBinding` record struct
- [x] Add `Binding` property to `ICommandContext`
- [x] Rename `CommandContext<T>.Binding` → `TypedBinding` (strongly-typed access)
- [x] Add computed `Binding` property to `CommandContext<T>` for interface compliance
- [x] Update all call sites from `.Binding` to `.TypedBinding`

### Phase 3: CommandContext Simplification

- [ ] Create non-generic `CommandContext`
- [ ] Update `InvokeCommand` overloads
- [ ] Deprecate `CommandContext<T>`

### Phase 4: Update Call Sites

- [ ] Update `View.Command.cs` to use new types
- [ ] Update `KeyBindings` to populate `Source`
- [ ] Update `MouseBindings` to populate `Source`
- [ ] Update scenarios and examples

### Phase 5: Testing

- [ ] Unit tests for new binding types
- [ ] Integration tests for propagation scenarios
- [ ] Verify backward compatibility

### Phase 6: Documentation

- [ ] Update command.md
- [ ] Update events.md
- [ ] Add migration guide

---

## Progress Tracking

### Completed

- [x] Initial design discussion
- [x] Identified generic variance issue
- [x] Decided on non-generic `CommandContext`
- [x] Clarified `Source` vs `Target` semantics
- [x] Created this planning document
- [x] **Phase 1: Interface updates** (2026-01-09)
  - Added `Source` property to `IInputBinding`
  - Added `Source` to `KeyBinding` and `MouseBinding`
  - Renamed `MouseBinding.MouseEventArgs` → `MouseEvent`
  - Updated 15+ files with call site changes
  - Updated documentation (mouse.md, events.md)
  - All tests pass
- [x] **Added Unit Tests for Bindings** (2026-01-09)
  - `MouseBindingTests.cs` - 14 tests covering constructor, properties, Source, MouseEvent, pattern matching
  - `KeyBindingTests.cs` - 17 tests covering constructor, properties, Source, Target, Key, pattern matching  
  - `CommandContextTests.cs` - 13 tests covering ICommandContext, pattern matching, Source propagation
- [x] **Phase 2: New Types** (2026-01-21)
  - Created `InputBinding` record struct in `Terminal.Gui\Input\InputBinding.cs`
  - Added `IInputBinding? Binding { get; }` property to `ICommandContext` interface
  - Renamed `CommandContext<T>.Binding` to `TypedBinding` for strongly-typed access
  - Added computed `Binding` property: `IInputBinding? Binding => TypedBinding`
  - Updated 20+ files to use `.TypedBinding` instead of `.Binding`
  - Created `InputBindingTests.cs` - 19 tests covering constructor, properties, IInputBinding, pattern matching
  - Updated `CommandContextTests.cs` - added 6 tests for new `Binding` property
  - All 34 binding-related tests pass

### In Progress

- [ ] Phase 3: CommandContext Simplification

### Blocked

- None

### Open Questions

1. **Should `InputBinding` require a non-null `Source`?**
   - Leaning: No, keep optional for flexibility

2. **Should we deprecate or remove `CommandContext<T>`?**
   - **Resolved: Remove** - We can and will break things in alpha

3. **How to handle existing code using `CommandContext<KeyBinding>` pattern?**
   - Answer: Pattern match on `ctx.Binding is KeyBinding kb` instead.

4. ~~**Should `CommandEventArgs` include a `Sender` property?**~~
   - **Resolved: No** - In virtual method overrides, `this` is the View currently processing (equivalent to `sender` in event handlers). No additional property needed.
      - `this` / `sender` = View currently handling the command (changes during propagation)
      - `args.Context?.Source` = View that originally invoked the command (constant during propagation)

   ---

   ## Revision History

   | Date | Author | Changes |
   |------|--------|---------|
   | 2026-01-09 | GitHub Copilot | Initial document created from design discussion |
   | 2026-01-09 | GitHub Copilot | Phase 1 completed: Added Source to IInputBinding, renamed MouseEventArgs to MouseEvent |
   | 2026-01-20 | Claude Opus 4.5 | Added "Relationship to Command Propagation" section; added Open Question #4 about CommandEventArgs.Sender; updated to note propagation depends on this refactor completing (alpha status) |
   | 2026-01-21 | GitHub Copilot | Phase 2 completed: Created InputBinding, added Binding to ICommandContext, renamed Binding to TypedBinding in CommandContext<T> |
