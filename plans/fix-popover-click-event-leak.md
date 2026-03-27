# Fix: Popover Click Event Leaks to Views Below After Item Selection

## Problem

When a user clicks on a list item in a DropDownList popover (or any popover that closes on `Released`), the synthesized `Clicked` event leaks through to views underneath.

### Root Cause

The mouse event cycle is `Pressed -> Released -> Clicked` (where `Clicked` is synthesized from `Press+Release`). The bug occurs because:

1. **Pressed** at (x,y) -> dispatched to ListView inside visible popover -> not consumed (no Press binding)
2. **Released** at (x,y) -> dispatched to ListView -> fires `Command.Activate` via mouse binding -> `CommandBridge` routes to `DropDownList.OnActivated` -> **popover closes** (`_listPopover.Visible = false`)
3. **Clicked** (synthesized) at (x,y) -> popover is now **hidden** -> `GetViewsUnderLocation` no longer finds the popover -> event falls through to Runnable and its SubViews below

The dismiss-by-clicking-outside mechanism (`ApplicationMouse.cs:108-129`) handles the case where a click **outside** a popover dismisses it and intentionally recurses the event to views below. But when a popover closes **internally** (via `Visible = false` during `Released` processing), there is no equivalent guard to suppress the subsequent `Clicked` event.

### Evidence

Trace output from the failing test `ClickListItem_DoesNotActivateViewBelow`:

```
[Mouse:Entry] LeftButtonReleased @(2,3)  -> hits ListView
[Command:Handler] Activate              -> bridges to DropDownList
[Command:Entry] OnVisibleChanged         -> popover hides
[Mouse:Exit]  LeftButtonReleased         -> handled=True

[Mouse:Entry] LeftButtonClicked @(2,3)   -> popover is GONE
[Mouse:Dispatch] LeftButtonClicked       -> dispatched to Runnable (!)
[Mouse:Entry] NewMouseEvent @Runnable    -> LEAKED
```

## Fix Options

### Option A: Track "popover was just hidden" in ApplicationMouse (Recommended)

Extend the existing `_dismissedByMousePress` guard to also cover **programmatic popover hides** during event processing.

**Where:** `ApplicationMouse.cs`

**How:**

1. Add a new field `_popoverHiddenDuringEvent` (or reuse `_dismissedByMousePress`) that tracks when a popover becomes invisible during any `RaiseMouseEvent` call.
2. In `RaiseMouseEvent`, at the top (before dispatch), check if a popover was hidden during a prior event in this Press->Release->Click cycle. If so, suppress the `Clicked` event.
3. The `ApplicationPopover.Hide` method (or `PopoverImpl.OnVisibleChanged`) would set this flag on `ApplicationMouse` when hiding occurs during mouse event processing.

**Pseudo-code:**

```csharp
// In ApplicationMouse:
private IPopoverView? _popoverHiddenDuringMouseEvent;

// In RaiseMouseEvent, before dispatch:
if (mouseEvent.IsSingleDoubleOrTripleClicked && _popoverHiddenDuringMouseEvent is { })
{
    _popoverHiddenDuringMouseEvent = null;
    return; // Suppress the Clicked event
}

// Clear on new press cycle:
if (mouseEvent.IsPressed)
{
    _popoverHiddenDuringMouseEvent = null;
}
```

The flag would be set by `ApplicationPopover.Hide()` or `PopoverImpl.OnVisibleChanged()` when the hide occurs during mouse event processing.

**Pros:**
- Centralized fix in `ApplicationMouse` where all mouse event routing lives
- Follows the existing pattern of `_dismissedByMousePress` guard
- Works for ALL popover types, not just DropDownList

**Cons:**
- Need to detect "during mouse event processing" (could use a flag or stack depth)

### Option B: Suppress in ApplicationMouse by tracking the popover visible state

**Where:** `ApplicationMouse.RaiseMouseEvent`

**How:**

Before dispatching `Clicked` events, check if the active popover changed between `Pressed/Released` and `Clicked`. If a popover was active during `Pressed` but is gone during `Clicked`, suppress the `Clicked` dispatch.

```csharp
// In RaiseMouseEvent, new field:
private IPopoverView? _activePopoverAtPress;

// On Pressed:
if (mouseEvent.IsPressed)
{
    _activePopoverAtPress = App?.Popovers?.GetActivePopover();
}

// On Clicked:
if (mouseEvent.IsSingleDoubleOrTripleClicked
    && _activePopoverAtPress is { }
    && App?.Popovers?.GetActivePopover() is null)
{
    _activePopoverAtPress = null;
    return; // Popover was dismissed between press and click
}
```

**Pros:**
- Simple state tracking, no coordination with `ApplicationPopover`
- No changes needed outside `ApplicationMouse`

**Cons:**
- Does not distinguish between "popover closed by user selection" (should suppress Click) and "popover closed externally" (ambiguous)
- The existing dismiss-by-click-outside already handles its own case, so this mainly catches the internal-close case

### Option C: Handle in DropDownList specifically

**Where:** `DropDownList.cs`

**How:** When `OnActivated` hides the popover, mark the mouse event as handled so the `Clicked` event knows to stop.

**Cons:**
- Only fixes DropDownList, not other popovers (e.g., PopoverMenu)
- The `Clicked` event is a separate `RaiseMouseEvent` call, so marking handled on `Released` doesn't help `Clicked`
- NOT recommended

## Recommended Approach: Option B

Option B is the simplest and most self-contained. It tracks the active popover state at `Pressed` time and suppresses the `Clicked` event if the popover disappeared between `Pressed` and `Clicked`. This:

- Is entirely within `ApplicationMouse.RaiseMouseEvent`
- Requires no coordination with `ApplicationPopover` or `PopoverImpl`
- Works for all popover types (DropDownList, PopoverMenu, etc.)
- Follows the existing guard pattern (`_dismissedByMousePress`)
- Has minimal risk of side effects

## Implementation Steps

1. **Add field to `ApplicationMouse`:** `private IPopoverView? _activePopoverAtPress;`
2. **Record on Pressed:** In `RaiseMouseEvent`, when `mouseEvent.IsPressed`, record the current active popover.
3. **Guard on Clicked:** Before dispatch of `Clicked` events, check if a popover was active at press time but is no longer active. If so, clear the field and return (suppress).
4. **Clear on new press cycle:** Ensure the field is cleared on each new `Pressed` event.
5. **Update the test:** Verify `ClickListItem_DoesNotActivateViewBelow` passes.
6. **Add additional test:** Verify that the existing dismiss-by-click-outside behavior still works correctly (clicking outside a popover still recurses to views below).
7. **Run full test suite:** Ensure no regressions.

## Files to Modify

| File | Change |
|------|--------|
| `Terminal.Gui/App/Mouse/ApplicationMouse.cs` | Add `_activePopoverAtPress` field and guard logic in `RaiseMouseEvent` |
| `Tests/.../DropDownListTests.cs` | Failing test already added (just needs to pass) |

## Verification

- `ClickListItem_DoesNotActivateViewBelow` passes
- All existing `PopoverMouseDismissTests` pass (especially `MousePress_OutsidePopover_EventRecursesToViewsBelow`)
- All existing `DropDownListTests` pass
- Full `UnitTestsParallelizable` suite passes
