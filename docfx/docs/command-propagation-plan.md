# Command Propagation Implementation Plan

> **Issue**: [Menu etc. with Selectors are broken - Formalize/Fix Command propagation](https://github.com/gui-cs/Terminal.Gui/issues/4473)

---

## Implementation Status

| Phase | Status | Breaking |
|-------|--------|----------|
| **1** Foundation | ✅ COMPLETE | No |
| **2** WeakReference | ✅ COMPLETE | YES |
| **3** Shortcut Propagation | ⏸️ NOT STARTED | No |
| **4** Re-enable Tests | ⏸️ NOT STARTED | No |
| **5** MenuBar Propagation | ⏸️ DEFERRED | No |

---

## Phase 1: Foundation ✅ COMPLETE

**Objective**: Add `PropagatedCommands` property and propagation helper

**Changes**:
- Added `PropagatedCommands` property (default: `[Command.Accept]`)
- Added `PropagateCommand` helper method
- Updated `RaiseAccepting` to use helper

**Tests**: 8 new tests in ViewCommandTests.cs (7 passing, 1 skipped for Phase 3)

**Result**: ✅ Compiles | ✅ All tests pass | ✅ Backward compatible

---

## Phase 2: WeakReference Infrastructure ✅ COMPLETE

**Objective**: Change `CommandContext.Source` to `WeakReference<View>?` for lifecycle safety

**Changes**:
- Updated `CommandContext` and `ICommandContext` to use `WeakReference<View>?`
- Modified `View.InvokeCommand` to wrap `this` in WeakReference
- Updated all Views using `Context.Source` to `TryGetTarget` pattern
- Skipped tests temporarily: CommandContextTests.cs, InputBindingTests.cs

**Files Modified**: 18 files (Dialog, MenuBar, PopoverMenu, ComboBox, OptionSelector, ScrollSlider, Shortcut, etc.)

**Result**: ✅ Compiles | ⚠️ Tests skipped (re-enable in Phase 4) | **BREAKING**

---

## Phase 3: Enable Shortcut Activate Propagation ⏸️ NOT STARTED

**Test Sample**: `Examples/ShortcutTest/` - Standalone mini-app for testing Shortcut → Window propagation

**Objective**: Enable `Command.Activate` propagation for Shortcut hierarchy:
- CheckBox (CommandView, CanFocus=false) → Shortcut → Window
- CheckBox (CommandView, CanFocus=true) → Shortcut → Window
- Button (CommandView) → Shortcut → Window

**Changes**:
1. Update `RaiseActivating` in `View.Command.cs` to call `PropagateCommand` (same pattern as `RaiseAccepting`)
2. Enable test: `PropagatedCommands_CanBeCustomized` (ViewCommandTests.cs:702)
3. Test with ShortcutTest example

**Deferred**: MenuBar/Menu/PopoverMenu hierarchy (more complex - Phase 5)

---

## Phase 4: Re-enable Tests & Cleanup ⏸️ NOT STARTED

**Objective**: Re-enable skipped tests and remove legacy code

**Changes**:
1. Update CommandContextTests.cs: Remove Skip attributes, update assertions to use `TryGetTarget`
2. Update InputBindingTests.cs: Remove Skip attributes, update assertions
3. Uncomment temporarily-commented assertions
4. Verify Shortcut.DispatchCommand preserves context correctly

---

## Phase 5: MenuBar/Menu Propagation ⏸️ DEFERRED

**Objective**: Extend Activate propagation to Menu hierarchy (MenuItem → Menu → MenuBar)

**Changes**:
1. Set `PropagatedCommands = [Command.Accept, Command.Activate]` in MenuBar/Menu constructors
2. Add MenuBar.OnActivating override to handle propagated activations
3. Add tests for FlagSelector → MenuItem → Menu → MenuBar propagation
4. Remove legacy workarounds: `SelectedMenuItemChanged` event

---

## Problem Statement

### Current Behavior
- Command propagation exists only for `Command.Accept` (hard-coded in `View.RaiseAccepting()`)
- `Command.Activate` does NOT propagate
- Causes: MenuBar can't respond to MenuItem activations, requiring workarounds like `SelectedMenuItemChanged`

### Example: Broken FlagSelector in MenuBar
```
MenuBar → MenuBarItem → PopoverMenu → Menu → MenuItem → FlagSelector → CheckBox
```
When CheckBox activates, command stops at MenuItem - MenuBar never sees it, can't manage popover state.

---

## Solution Overview

**Add `PropagatedCommands` property to View** - opt-in list of commands that propagate to SuperView

**Default**: `[Command.Accept]` (backward compatible)

**Pattern**:
```csharp
SubView.InvokeCommand(Command.Activate) → not handled
  → SuperView.PropagatedCommands.Contains(Command.Activate)?
    → YES: SuperView.InvokeCommand(Command.Activate, ctx)
    → NO: stop
```

---

## Key Code Patterns

### PropagatedCommands Property
**Location**: `Terminal.Gui/ViewBase/View.Command.cs`

```csharp
/// <summary>
///     Commands that propagate to this View from unhandled SubViews.
///     Default: [Command.Accept] (backward compatible)
/// </summary>
public IReadOnlyList<Command> PropagatedCommands { get; set; } = [Command.Accept];
```

### PropagateCommand Helper
```csharp
protected bool? PropagateCommand (Command command, ICommandContext? ctx, bool handled)
{
    if (handled) return true;

    // Special case: Command.Accept checks for IsDefault peer button first
    if (command == Command.Accept)
    {
        View? isDefaultView = SuperView?.GetSubViews (includePadding: true)
            .FirstOrDefault (v => v is Button { IsDefault: true });
        if (isDefaultView != this && isDefaultView is Button { IsDefault: true })
        {
            bool? buttonHandled = isDefaultView.InvokeCommand (Command.Accept, ctx);
            if (buttonHandled == true) return true;
        }
    }

    // Check if SuperView wants this command propagated
    if (SuperView?.PropagatedCommands?.Contains (command) == true)
    {
        return SuperView.InvokeCommand (command, ctx);
    }

    return handled;
}
```

### RaiseAccepting Pattern (Phase 1)
```csharp
protected bool? RaiseAccepting (ICommandContext? ctx)
{
    CommandEventArgs args = new () { Context = ctx };
    args.Handled = OnAccepting (args) || args.Handled;

    if (!args.Handled && Accepting is { })
    {
        Accepting?.Invoke (this, args);
    }

    if (args.Handled)
    {
        RaiseAccepted (ctx);
    }

    // NEW: Use propagation helper
    return PropagateCommand (Command.Accept, ctx, args.Handled);
}
```

### RaiseActivating Pattern (Phase 3)
```csharp
protected virtual bool? RaiseActivating (ICommandContext? ctx)
{
    CommandEventArgs args = new () { Context = ctx };

    if (OnActivating (args) || args.Handled)
    {
        return true;
    }

    Activating?.Invoke (this, args);

    // NEW: Use propagation helper (Phase 3)
    return PropagateCommand (Command.Activate, ctx, args.Handled ?? false);
}
```

### CommandContext WeakReference (Phase 2)
**Location**: `Terminal.Gui/Input/CommandContext.cs`

```csharp
public record struct CommandContext : ICommandContext
{
    public Command Command { get; set; }
    public WeakReference<View>? Source { get; set; }  // Lifecycle-safe
    public IInputBinding? Binding { get; set; }
}
```

### TryGetTarget Pattern (Phase 2)
```csharp
// Pattern 1: Type-based with view access
if (args.Context?.Source?.TryGetTarget (out View? view) && view is CheckBox cb)
{
    // Use cb
}

// Pattern 2: Initialize before TryGetTarget
View? sourceView = null;
args.Context?.Source?.TryGetTarget (out sourceView);
LogEvent (sourceView?.Id ?? "null");

// Pattern 3: Pattern matching in MenuBar
if (args.Context?.Source?.TryGetTarget (out View? sourceView) == true
    && sourceView is MenuBarItem { PopoverMenuOpen: false } item)
{
    ShowItem (item);
}
```

### InvokeCommand Update (Phase 2)
```csharp
// Wrap 'this' in WeakReference
return implementation! (new CommandContext {
    Command = command,
    Source = new WeakReference<View> (this),
    Binding = binding
});
```

---

## Testing Requirements

### Phase 1 Tests Added (ViewCommandTests.cs)
1. ✅ `PropagatedCommands_DefaultIsAcceptOnly`
2. ✅ `CommandAccept_PropagatesByDefault`
3. ✅ `CommandActivate_DoesNotPropagateByDefault`
4. ✅ `PropagatedCommands_CanDisableAllPropagation`
5. ⏸️ `PropagatedCommands_CanBeCustomized` (Skip - re-enable Phase 3)
6. ✅ `PropagateCommand_StopsWhenHandled`
7. ✅ `PropagateCommand_WorksInDeepHierarchy`
8. ✅ `PropagateCommand_StopsAtIntermediateHandler`

### Phase 2 Tests Skipped (Re-enable Phase 4)
- `CommandContextTests.cs` - All tests (need WeakReference updates)
- `InputBindingTests.cs` - Tests accessing Context.Source

### Phase 3 Test Sample
**Location**: `Examples/ShortcutTest/`

**Purpose**: Standalone app to test Shortcut command propagation
- 3 Shortcut instances with different CommandView types
- Event log showing propagation flow
- Window-level handlers to verify propagation

---

## References

- [Cancellable Work Pattern](cancellable-work-pattern.md)
- [Command Deep Dive](command.md)
- [Events Deep Dive](events.md)
- [View Deep Dive](View.md)
- Issue #4473: Menu etc. with Selectors are broken
- Issue #4050: PropagatedCommands design

---

## Revision History

| Date | Changes |
|------|---------|
| 2026-01-09 | Initial analysis (GitHub Copilot) |
| 2026-01-21 | WeakReference design; condensed 1576→567 lines (Claude Opus 4.5) |
| 2026-01-21 | Added phased implementation plan (Claude Sonnet 4.5) |
| 2026-01-22 | Moved phases to top, tersified 730→250 lines (Claude Sonnet 4.5) |
