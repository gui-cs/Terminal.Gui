# Command Propagation Analysis and Design

> **Status**: Analysis Document (Step 1 of Issue Resolution)
>
> **Issue**: [Menu etc. with Selectors are broken - Formalize/Fix Command propagation](https://github.com/gui-cs/Terminal.Gui/issues/XXXXX)
>
> **Created**: 2026-01-09
>
> **Author**: GitHub Copilot

## Table of Contents

- [Executive Summary](#executive-summary)
- [Background](#background)
- [Current State Analysis](#current-state-analysis)
- [Problems Identified](#problems-identified)
- [Shortcut + CommandView Scenario Analysis](#shortcut--commandview-scenario-analysis)
- [Design Requirements](#design-requirements)
- [Proposed Solution](#proposed-solution)
- [Implementation Impact](#implementation-impact)
- [References](#references)

---

## Executive Summary

Terminal.Gui currently implements a **command propagation** mechanism for `Command.Accept` that allows events to bubble up the View hierarchy until handled. This mechanism is hard-coded in `View.RaiseAccepting()` and includes special-case logic for:

1. Invoking `Command.Accept` on peer Views with `IsDefault == true` (e.g., default `Button`)
2. Bubbling the command up to the `SuperView` if not handled

However, this approach has **three critical problems**:

1. **One-off Implementation**: The propagation logic is specific to `Command.Accept` only
2. **Incomplete Coverage**: `Command.Activate` does NOT propagate, breaking hierarchical components like `MenuBar` that need to respond to subview activations
3. **Tight Coupling**: Views like `PopoverMenu` resort to view-specific events (`SelectedMenuItemChanged`) that couple subviews to superview implementation details

This document analyzes the current state, identifies problems, establishes design requirements, and proposes a solution that:

- Generalizes propagation to support multiple commands
- Maintains backward compatibility
- Enables opt-in propagation for superviews
- Preserves the decoupling principle of the Cancellable Work Pattern (CWP)

---

## Background

### The Cancellable Work Pattern (CWP)

Terminal.Gui implements the **Cancellable Work Pattern** throughout the framework. CWP provides a structured workflow where:

1. **Virtual Methods** enable subclass customization
2. **Events** enable external subscribers to participate
3. **Cancellation** enables workflow interruption via `Handled` or `Cancel` flags

For commands, the pattern looks like:

```csharp
protected bool? RaiseAccepting(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };
    
    // 1. Virtual method (subclass priority)
    if (OnAccepting(args) || args.Handled)
    {
        return true;
    }
    
    // 2. Event (external subscribers)
    Accepting?.Invoke(this, args);
    
    // 3. Check handled flag
    return Accepting is null ? null : args.Handled;
}
```

### Current Command System

The `Command` enum defines standard actions that Views can perform:

- **`Command.Accept`**: Confirms actions (e.g., button press, dialog submission, menu command execution)
- **`Command.Activate`**: Changes state or prepares for interaction (e.g., checkbox toggle, list item selection, button focus, menu navigation)
- **`Command.HotKey`**: Handles hot key activation
- **Movement commands**: `Up`, `Down`, `Left`, `Right`, etc.

Commands are invoked via:
- **Key bindings**: `KeyBindings` map keys to commands
- **Mouse bindings**: `MouseBindings` map mouse actions to commands
- **Programmatic**: `InvokeCommand(command, context)`

### The Rename: Select → Activate

Recently (as noted in the issue), `Select` was renamed to `Activate` because:

- **Ambiguity**: "Select" conflated state changes (checkbox toggle) with preparatory actions (button focus)
- **Clarity**: "Activate" better describes preparing a view for interaction
- **Propagation**: The rename set the stage for targeted propagation with `PropagateActivating`

---

## Current State Analysis

### How `Command.Accept` Propagation Works Today

The special-case propagation logic is in `View.RaiseAccepting()` (View.Command.cs:127-182):

```csharp
protected bool? RaiseAccepting(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };

    // CWP: Virtual method first
    args.Handled = OnAccepting(args) || args.Handled;

    // CWP: Event second
    if (!args.Handled && Accepting is { })
    {
        Accepting?.Invoke(this, args);
    }

    // CWP: Raise non-cancellable "Accepted" event if handled
    if (args.Handled)
    {
        RaiseAccepted(ctx);
    }

    // **SPECIAL CASE**: Accept propagation logic
    if (!args.Handled)
    {
        // 1. Check for IsDefault peer button
        View? isDefaultView = SuperView?.GetSubViews(includePadding: true)
            .FirstOrDefault(v => v is Button { IsDefault: true });

        if (isDefaultView != this && isDefaultView is Button { IsDefault: true } button)
        {
            bool? handled = isDefaultView.InvokeCommand(Command.Accept, ctx);
            if (handled == true)
            {
                return true;
            }
        }

        // 2. Bubble to SuperView
        if (SuperView is { })
        {
            return SuperView?.InvokeCommand(Command.Accept, ctx);
        }
    }

    return args.Handled;
}
```

**Key Points**:
1. Follows CWP for `OnAccepting` and `Accepting` event
2. Adds **hard-coded** propagation logic after CWP phase
3. First tries peer `Button` with `IsDefault == true`
4. Then bubbles to `SuperView`
5. This logic is **ONLY** in `RaiseAccepting` - no other command has it

### How `Command.Activate` Works Today

The current implementation of `RaiseActivating()` (View.Command.cs:268-284):

```csharp
protected virtual bool? RaiseActivating(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };

    // CWP: Virtual method first
    if (OnActivating(args) || args.Handled)
    {
        return true;
    }

    // CWP: Event second
    Activating?.Invoke(this, args);

    // NO propagation logic
    return Activating is null ? null : args.Handled;
}
```

**Key Points**:
1. Follows CWP pattern correctly
2. **Does NOT propagate** - stops at the view that handles it
3. **This is the problem** for hierarchical views like `MenuBar`

### How MenuBar Currently Works Around This

Since `Command.Activate` doesn't propagate, `MenuBar` and `PopoverMenu` use a **workaround** via view-specific events:

#### In `Menu.cs`:
```csharp
internal void RaiseSelectedMenuItemChanged(MenuItem? selected)
{
    SelectedMenuItem = selected;
    OnSelectedMenuItemChanged(selected);
    SelectedMenuItemChanged?.Invoke(this, selected);
}

public event EventHandler<MenuItem?>? SelectedMenuItemChanged;
```

#### In `PopoverMenu.cs`:
```csharp
private void MenuOnSelectedMenuItemChanged(object? sender, MenuItem? e)
{
    ShowSubMenu(e);
}

// During initialization:
menu.SelectedMenuItemChanged += MenuOnSelectedMenuItemChanged;
```

#### In `MenuBar.cs`:
```csharp
protected override void OnSelectedMenuItemChanged(MenuItem? selected)
{
    if (IsOpen() && selected is MenuBarItem { PopoverMenuOpen: false } selectedMenuBarItem)
    {
        ShowItem(selectedMenuBarItem);
    }
}
```

**Problems with This Approach**:
1. **Tight Coupling**: Subviews (`Menu`) must know their superviews need notifications
2. **Not Generalizable**: Custom event per use case (`SelectedMenuItemChanged`)
3. **Violation of CWP**: Circumvents the standard command/event system
4. **Fragile**: Adding new hierarchical views requires new custom events

---

## Problems Identified

### Problem 1: Hard-Coded, Command-Specific Propagation

**Current State**: Only `Command.Accept` has propagation logic, hard-coded in `RaiseAccepting()`.

**Issues**:
- **Not generalizable**: Other commands can't propagate (e.g., `Command.Activate`)
- **Maintenance burden**: Adding propagation to other commands requires duplicating logic
- **Inconsistent**: Different commands behave differently with no clear pattern

**Evidence**: Lines 154-179 in View.Command.cs show the special-case logic only exists for `Accept`.

### Problem 2: Missing `Command.Activate` Propagation

**Current State**: `Command.Activate` does NOT propagate to superviews.

**Issues**:
- **MenuBar broken**: `MenuBar` can't respond to `MenuItem` activations to show popovers
- **Workarounds required**: Custom events like `SelectedMenuItemChanged` needed
- **Limits composability**: New hierarchical views can't leverage activation

**Evidence**: 
- View.Command.cs:268-284 shows `RaiseActivating` lacks propagation
- PopoverMenu.cs:670-674 shows the workaround event handler
- MenuBar.cs:303-311 shows `OnSelectedMenuItemChanged` override

### Problem 3: Tight Coupling via View-Specific Events

**Current State**: `Menu` raises `SelectedMenuItemChanged` that `MenuBar` and `PopoverMenu` subscribe to.

**Issues**:
- **Coupling**: Subviews know about superview needs (violates separation of concerns)
- **Not scalable**: Each hierarchical view pair needs custom events
- **Bypasses CWP**: Custom events circumvent the standard command system
- **Hard to discover**: Developers must know about these special events

**Evidence**:
- Menu.cs:168-173 defines and raises `SelectedMenuItemChanged`
- PopoverMenu.cs:342 subscribes to it
- MenuBar.cs:303 overrides to handle it

### Problem 4: Unclear Ownership of Propagation Behavior

**Current State**: Propagation logic is in the "raising" view (subview), not the "receiving" view (superview).

**Issues**:
- **Inverted responsibility**: Subviews shouldn't dictate how superviews handle commands
- **Inflexible**: Can't change propagation behavior per superview instance
- **Not opt-in**: Superviews can't choose which commands to receive

**Evidence**: The propagation code in `RaiseAccepting()` (View.Command.cs:154-179) is in the base `View` class, applying to all views universally.

---

## Shortcut + CommandView Scenario Analysis

### Overview

A critical use case for command propagation is when a `Shortcut` contains a custom `CommandView` such as a `CheckBox`. This section validates that the proposed `PropagatedCommands` design correctly supports this scenario.

### View Hierarchy

```
SuperView (e.g., StatusBar, Bar, or custom container)
  └── Shortcut
        └── CheckBox (as CommandView)
```

### How Shortcut Handles CommandView Events

`Shortcut` acts as a **command aggregator** for its `CommandView`. When setting the `CommandView` property, Shortcut attaches event handlers:

```csharp
_commandView.Activating += CommandViewOnActivating;
_commandView.Accepting += CommandViewOnAccepted;

void CommandViewOnAccepted (object? sender, CommandEventArgs e)
{
    e.Handled = true;  // Eat CommandView's Accept
}

void CommandViewOnActivating (object? sender, CommandEventArgs e)
{
    if (/* context indicates direct activation */)
    {
        InvokeCommand (Command.Activate, e);  // Forward to Shortcut
    }
    e.Handled = true;  // Eat CommandView's Activating
}
```

**Key Behavior**: Shortcut intercepts and "eats" all CommandView events, then raises its own events through `DispatchCommand`.

### Command Flow in DispatchCommand

When a command is dispatched on a Shortcut:

1. **Activate CommandView**: `CommandView.InvokeCommand(Command.Activate)` - toggles CheckBox
2. **Raise Activating**: `RaiseActivating(ctx)` - if handled, **stops here**
3. **Set Focus**: If focusable, sets focus
4. **Raise Accepting**: `RaiseAccepting(ctx)` - if handled, **stops here**
5. **Invoke Action**: If `Action` is set, invokes it

### Validation: PropagatedCommands Works for This Scenario

The proposed design works correctly:

1. **CheckBox state changes**: Step 1 invokes Activate on CommandView, toggling the checkbox
2. **Activating can propagate**: Step 2 calls `RaiseActivating` on Shortcut, which (with the proposed changes) will propagate to SuperView if in `PropagatedCommands`
3. **Accepting can propagate**: Step 4 calls `RaiseAccepting`, which already propagates by default

### Important Behavior: Handled Commands Stop the Chain

If `RaiseActivating` returns `true` (handled locally or via propagation), `DispatchCommand` returns early and:
- **`Accepting` is NOT raised**
- **`Action` is NOT invoked**

This is intentional CWP behavior but should be documented clearly.

---

## Design Requirements

### Functional Requirements

1. **Generalized Propagation**: Support propagation for multiple commands, not just `Accept`
2. **Opt-In Model**: Superviews explicitly declare which commands propagate to them
3. **Backward Compatibility**: Existing `Command.Accept` propagation behavior must be preserved
4. **CWP Compliance**: Maintain the Cancellable Work Pattern structure
5. **Decoupling**: Subviews remain ignorant of superview propagation needs

### Non-Functional Requirements

1. **Performance**: Minimal overhead (avoid reflection, keep checks simple)
2. **Discoverability**: Clear, documented API
3. **Consistency**: All commands follow the same propagation rules
4. **Extensibility**: Easy to add propagation to new commands

### Constraints

1. **No Breaking Changes**: Cannot alter existing public APIs
2. **.NET 8 Compatible**: Use available C# 12 features
3. **Follows Conventions**: Adhere to Terminal.Gui coding standards

---

## Proposed Solution

### Overview

Introduce a **propagation registry** that allows superviews to declare which commands should propagate to them from unhandled subviews. The mechanism:

1. **Property**: `IReadOnlyList<Command> PropagatedCommands { get; set; }`
2. **Default**: `new List<Command> { Command.Accept }` (preserves current behavior)
3. **Propagation Logic**: Moved to a **helper method** used by all `Raise*` methods
4. **Opt-In**: Superviews set `PropagatedCommands` to include `Command.Activate` (or others)

### Detailed Design

#### 1. Add `PropagatedCommands` Property to `View`

```csharp
/// <summary>
///     Gets or sets the list of commands that should propagate to this View from unhandled subviews.
///     When a subview raises a command that is not handled (e.g., via <see cref="Accepting"/> or <see cref="Activating"/>),
///     and the command is in the SuperView's <see cref="PropagatedCommands"/> list, the command will be invoked on the SuperView.
/// </summary>
/// <remarks>
///     <para>
///         By default, only <see cref="Command.Accept"/> propagates to maintain backward compatibility.
///         To enable <see cref="Command.Activate"/> propagation (e.g., for <see cref="MenuBar"/> managing popover visibility),
///         set this property to include <see cref="Command.Activate"/>:
///     </para>
///     <code>
///         menuBar.PropagatedCommands = new List&lt;Command&gt; { Command.Accept, Command.Activate };
///     </code>
///     <para>
///         This design decouples subviews from superviews, allowing superviews to opt-in to command propagation
///         without requiring subviews to know about superview needs.
///     </para>
/// </remarks>
public IReadOnlyList<Command> PropagatedCommands { get; set; } = new List<Command> { Command.Accept };
```

#### 2. Create a Propagation Helper Method

```csharp
/// <summary>
///     Propagates a command to the SuperView if the command is in the SuperView's <see cref="PropagatedCommands"/> list.
///     Handles the special case of invoking the command on a peer IsDefault button for <see cref="Command.Accept"/>.
/// </summary>
/// <param name="command">The command to propagate.</param>
/// <param name="ctx">The command context.</param>
/// <param name="handled">Whether the command was already handled by this view.</param>
/// <returns>
///     <see langword="true"/> if the command was handled by propagation; otherwise, <see langword="false"/>.
/// </returns>
protected bool? PropagateCommand(Command command, ICommandContext? ctx, bool handled)
{
    // Only propagate if not already handled
    if (handled)
    {
        return true;
    }

    // Special case for Command.Accept: check for IsDefault peer button first
    if (command == Command.Accept)
    {
        View? isDefaultView = SuperView?.GetSubViews(includePadding: true)
            .FirstOrDefault(v => v is Button { IsDefault: true });

        if (isDefaultView != this && isDefaultView is Button { IsDefault: true })
        {
            bool? buttonHandled = isDefaultView.InvokeCommand(Command.Accept, ctx);
            if (buttonHandled == true)
            {
                return true;
            }
        }
    }

    // Check if SuperView wants this command propagated
    if (SuperView?.PropagatedCommands?.Contains(command) == true)
    {
        return SuperView.InvokeCommand(command, ctx);
    }

    return handled;
}
```

#### 3. Update `RaiseAccepting` to Use Helper

```csharp
protected bool? RaiseAccepting(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };

    // CWP: Virtual method first
    args.Handled = OnAccepting(args) || args.Handled;

    // CWP: Event second
    if (!args.Handled && Accepting is { })
    {
        Accepting?.Invoke(this, args);
    }

    // CWP: Raise non-cancellable "Accepted" event if handled
    if (args.Handled)
    {
        RaiseAccepted(ctx);
    }

    // NEW: Use propagation helper (maintains backward compatibility)
    return PropagateCommand(Command.Accept, ctx, args.Handled);
}
```

#### 4. Update `RaiseActivating` to Use Helper

```csharp
protected virtual bool? RaiseActivating(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };

    // CWP: Virtual method first
    if (OnActivating(args) || args.Handled)
    {
        return true;
    }

    // CWP: Event second
    Activating?.Invoke(this, args);

    // NEW: Use propagation helper (enables opt-in propagation)
    return PropagateCommand(Command.Activate, ctx, args.Handled ?? false);
}
```

#### 5. Update `MenuBar` to Opt-In to `Command.Activate` Propagation

```csharp
public MenuBar(IEnumerable<MenuBarItem> menuBarItems) : base(menuBarItems)
{
    // ... existing initialization ...

    // NEW: Opt-in to Command.Activate propagation for popover management
    PropagatedCommands = new List<Command> { Command.Accept, Command.Activate };

    // ... rest of initialization ...
}
```

#### 6. Handle `Command.Activate` in `MenuBar`

Add a new override or update the existing `Command.Activate` handler in `MenuBar`:

```csharp
protected override bool OnActivating(CommandEventArgs args)
{
    // If a MenuItem is being activated, show its popover if applicable
    if (args.Context?.Source is MenuBarItem { PopoverMenuOpen: false } menuBarItem)
    {
        if (!CanFocus)
        {
            Active = true;
        }
        
        ShowItem(menuBarItem);
        
        if (!menuBarItem.HasFocus)
        {
            menuBarItem.SetFocus();
        }
        
        return true; // Handled
    }

    return base.OnActivating(args);
}
```

### Migration Path

#### Phase 1: Deprecate Custom Events (Optional)

Mark `SelectedMenuItemChanged` as `[Obsolete]` with guidance:

```csharp
/// <summary>
///     Event raised when the selected menu item changes.
/// </summary>
/// <remarks>
///     <para>
///         <b>Obsolete</b>: Use <see cref="Command.Activate"/> propagation instead.
///         Set <see cref="View.PropagatedCommands"/> to include <see cref="Command.Activate"/>
///         and override <see cref="View.OnActivating"/> in the superview.
///     </para>
/// </remarks>
[Obsolete("Use Command.Activate propagation via PropagatedCommands property instead.")]
public event EventHandler<MenuItem?>? SelectedMenuItemChanged;
```

#### Phase 2: Remove Custom Event Usage (Later Release)

Once `Command.Activate` propagation is proven stable:
1. Remove `SelectedMenuItemChanged` event
2. Remove event subscriptions in `PopoverMenu` and `MenuBar`
3. Update documentation

---

## Implementation Impact

### Files to Modify

1. **Terminal.Gui/ViewBase/View.Command.cs**:
   - Add `PropagatedCommands` property
   - Add `PropagateCommand()` helper method
   - Update `RaiseAccepting()` to use helper
   - Update `RaiseActivating()` to use helper
   - Update XML documentation

2. **Terminal.Gui/Views/Menu/MenuBar.cs**:
   - Set `PropagatedCommands` in constructor
   - Add/update `OnActivating()` override
   - (Optional) Mark `OnSelectedMenuItemChanged` as obsolete

3. **Terminal.Gui/Views/Menu/Menu.cs**:
   - (Optional) Mark `SelectedMenuItemChanged` as obsolete

4. **Terminal.Gui/Views/Menu/PopoverMenu.cs**:
   - (Optional) Remove `MenuOnSelectedMenuItemChanged` handler
   - (Optional) Remove event subscription

5. **Terminal.Gui/Views/Shortcut.cs** (Documentation only):
   - Update XML documentation to clarify command aggregation behavior
   - Document that handled `Activating` prevents `Accepting` from being raised

### New Tests Required

1. **PropagatedCommands Default Behavior**:
   - Verify `Command.Accept` propagates by default
   - Verify `Command.Activate` does NOT propagate by default

2. **PropagatedCommands Opt-In**:
   - Verify setting `PropagatedCommands` enables propagation
   - Verify clearing `PropagatedCommands` disables all propagation

3. **MenuBar Integration**:
   - Verify `MenuItem` activation propagates to `MenuBar`
   - Verify `MenuBar` shows popover on `Command.Activate`
   - Verify backward compatibility with `Command.Accept`

4. **Backward Compatibility**:
   - Verify existing `Button.IsDefault` behavior unchanged
   - Verify existing `Dialog` accept behavior unchanged

5. **Shortcut + CheckBox Propagation**:
   - Verify CheckBox toggles when Shortcut receives Activate
   - Verify Shortcut.Activating propagates when SuperView opts-in
   - Verify Shortcut.Accepting propagates when not handled
   - Verify that if SuperView handles Activating, Accepting is NOT raised

6. **Shortcut Command Chain Behavior**:
   - Verify handled Activating prevents Accepting from being raised
   - Verify handled Accepting prevents Action from being invoked

### Documentation Updates

1. **command.md**: Add section on command propagation
2. **events.md**: Update to reference `PropagatedCommands`
3. **View.md**: Document `PropagatedCommands` property
4. **API docs**: Ensure XML comments are complete

---

## Alternative Designs Considered

### Alternative 1: Per-Command Propagation Methods

**Design**: Add `ShouldPropagate(Command)` virtual method.

**Pros**:
- Flexible per-command control
- No property to set

**Cons**:
- Requires overriding in every view
- Less discoverable
- **Rejected**: Too verbose, violates "explicit is better than implicit"

### Alternative 2: Event-Based Propagation

**Design**: Raise a `CommandPropagating` event before propagating.

**Pros**:
- Consistent with CWP
- External subscribers can intercept

**Cons**:
- Performance overhead
- Complicates the workflow
- **Rejected**: Adds complexity without clear benefit

### Alternative 3: Attribute-Based Propagation

**Design**: Use `[Propagated]` attribute on command handlers.

**Pros**:
- Declarative
- Clear intent

**Cons**:
- Requires reflection (performance hit)
- Not idiomatic for Terminal.Gui
- **Rejected**: Performance concerns, complexity

---

## Open Questions

1. **Should `PropagatedCommands` be instance-specific or type-specific?**
   - **Decision**: Instance-specific (property, not static) for flexibility
   - **Rationale**: Different instances may need different propagation (e.g., `MenuBar` in different contexts)

2. **Should propagation be recursive or single-level?**
   - **Decision**: Recursive (current `Accept` behavior)
   - **Rationale**: Maintains backward compatibility; hierarchical views expect it

3. **Should there be a maximum propagation depth?**
   - **Decision**: No explicit limit; rely on `Handled` flag
   - **Rationale**: Avoids infinite loops via CWP; adding depth limit adds complexity

4. **Should `PropagatedCommands` be settable or collection-mutable?**
   - **Decision**: Settable (replace entire list)
   - **Rationale**: Simpler API; most use cases set once during initialization

5. **Should Activating handled by SuperView prevent Accepting on Shortcut?**
   - **Current Design**: Yes - if `RaiseActivating` returns `true`, `DispatchCommand` exits early
   - **Rationale**: Consistent with CWP "cancelable" semantics
   - **Alternative**: Could add `Activated` event (non-cancelable) like `Accepted`, allowing observation without cancellation

---

## Success Criteria

### Must Have

1. ✅ `Command.Accept` propagation behavior unchanged (backward compatibility)
2. ✅ `Command.Activate` can propagate when `PropagatedCommands` includes it
3. ✅ `MenuBar` can respond to `MenuItem` activations via `Command.Activate` propagation
4. ✅ All existing tests pass
5. ✅ New tests cover propagation scenarios

### Should Have

1. ⚠️ `SelectedMenuItemChanged` marked obsolete with migration guidance
2. ⚠️ Documentation updated across all relevant files
3. ⚠️ UICatalog scenario demonstrates propagation

### Could Have

1. ⏸️ Remove `SelectedMenuItemChanged` entirely (deferred to later release)
2. ⏸️ Apply propagation to other commands (e.g., `Command.HotKey`)

---

## References

### Code Files Analyzed

- `Terminal.Gui/ViewBase/View.Command.cs` (lines 1-400)
- `Terminal.Gui/Views/Shortcut.cs` (lines 1-300)
- `Terminal.Gui/Views/Menu/Menu.cs`
- `Terminal.Gui/Views/Menu/MenuBar.cs`
- `Terminal.Gui/Views/Menu/MenuItem.cs`
- `Terminal.Gui/Views/Menu/PopoverMenu.cs`
- `Terminal.Gui/Input/Command.cs`

### Documentation Reviewed

- `docfx/docs/View.md` (View Deep Dive)
- `docfx/docs/cancellable-work-pattern.md` (CWP definition)
- `docfx/docs/events.md` (Event patterns)
- `docfx/docs/command.md` (Command system overview)
- `CONTRIBUTING.md` (Contribution guidelines)

### Related Issues

- Issue #XXXXX: Menu etc. with Selectors are broken - Formalize/Fix Command propagation
- Issue #3925: (Referenced in events.md - propagation hacks)
- Issue #4050: (Referenced in events.md - PropagatedCommands proposal)

---

## Revision History

| Date       | Author          | Changes                          |
|------------|-----------------|----------------------------------|
| 2026-01-09 | GitHub Copilot  | Initial analysis document created |

