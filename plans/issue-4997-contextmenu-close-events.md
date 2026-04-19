# Fix: Context Menu Closing Events Not Firing on Click-Outside (#4997)

## Problem

When a `PopoverMenu` is dismissed by clicking outside of it, none of the closing-related events fire (`Activating`, `Activated`, `Accepting`, `Accepted`, `PopoverDeRegistered`, `Disposing`). These events only fire when a menu item is explicitly selected.

This prevents consumers from detecting when a context menu has been dismissed, e.g. to unlock selection or clean up state.

## Root Cause Analysis

There are two paths that close a popover:

### Path A: Menu item selected (events fire correctly)
1. User clicks a `MenuItem` → `Command.Activate` → `PopoverMenu.OnActivating()`
2. Fires `Activating` → sets `Visible = false` → fires `Activated`
3. `OnAccepting` fires → `RaiseAccepted` → `Accepting` + `Accepted` events

### Path B: Click outside (events DO NOT fire — the bug)
1. `ApplicationMouse.RaiseMouseEvent` detects click is outside the popover
2. Calls `ApplicationPopover.HideWithQuitCommand()` → invokes `Command.Quit`
3. `Command.Quit` handler in `PopoverImpl.cs` **only sets `Visible = false`** — bypasses the Activate/Accept command chain entirely

The `Command.Quit` handler (PopoverImpl.cs ~line 93-103) is:
```csharp
bool? Quit (ICommandContext? ctx)
{
    if (!Visible) return false;
    Visible = false;    // ← just sets Visible, no events
    return true;
}
```

## Key Files

| File | Role |
|---|---|
| `Terminal.Gui/App/Popovers/PopoverImpl.cs` | Abstract base — `Command.Quit` handler |
| `Terminal.Gui/App/Popovers/Popover.cs` | Generic `Popover<TView, TResult>` |
| `Terminal.Gui/Views/Menu/PopoverMenu.cs` | `PopoverMenu : Popover<Menu, MenuItem>` |
| `Terminal.Gui/App/ApplicationPopover.cs` | Registration, Show/Hide, `HideWithQuitCommand` |
| `Terminal.Gui/App/Mouse/ApplicationMouse.cs` | Click-outside detection (line ~152) |
| `Tests/.../Popover/PopoverMouseDismissTests.cs` | Existing click-outside tests |

## Proposed Fix

The best approach is to add a dedicated **`Closing`/`Closed`** event pair on `PopoverImpl` that fires on **all** hide paths. This is cleaner than forcing Activate/Accept events to fire when no menu item was actually selected (those events semantically mean "an item was chosen").

### Step 1: Add `Closing` and `Closed` events to `PopoverImpl`

Add a cancellable `Closing` event (using `CommandEventArgs`) and a `Closed` event that fire whenever the popover transitions from visible to hidden, regardless of the trigger (click-outside, Escape key, menu item selection, programmatic hide).

The natural place is in `OnVisibleChanging` / `OnVisibleChanged` in `PopoverImpl.cs`, since all hide paths converge there.

### Step 2: Fire events in the visibility change path

In `PopoverImpl.OnVisibleChanging()` (when transitioning to `Visible = false`):
- Raise `Closing` event (cancellable — if cancelled, prevent the visibility change)

In `PopoverImpl.OnVisibleChanged()` (after `Visible` becomes `false`):
- Raise `Closed` event

### Step 3: Update `PopoverMenu` to participate

`PopoverMenu` already overrides `OnVisibleChanged`. Ensure the new events integrate cleanly with the existing menu hide logic.

### Step 4: Add tests

Add tests in `Tests/.../Popover/PopoverMouseDismissTests.cs` (or a new test file) that verify:
- `Closing` fires when clicking outside a popover
- `Closed` fires when clicking outside a popover  
- `Closing` fires when pressing Escape to dismiss
- `Closed` fires when pressing Escape to dismiss
- `Closing` can cancel the dismiss (popover stays visible)
- `Closing`/`Closed` also fire when a menu item is selected (all paths covered)

### Step 5: Update documentation

Update XML doc comments on the new events. If there are relevant docs in `docfx/`, update those as well.

## Alternative Considered (and rejected)

**Fire `Accepting`/`Accepted` on click-outside dismiss**: This was rejected because those events semantically indicate "a value/item was accepted." Clicking outside is a cancellation, not an acceptance. Firing acceptance events would be misleading to consumers.

## Notes

- `PopoverDeRegistered` not firing is **by design** — popovers stay registered for reuse. The issue reporter likely expected it to fire, but the real fix is providing a `Closing`/`Closed` event they can use instead.
- The `VisibleChanged` event already fires on all paths but is too generic — consumers need a popover-specific closing event with cancellation support.
