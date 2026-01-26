# Popover System Analysis

> Internal architecture analysis of Terminal.Gui's Popover system.
> For user-facing documentation, see `docfx/docs/Popovers.md`.

## Architecture Overview

The Popover system in Terminal.Gui is a **layered architecture** for transient, non-modal UI elements that appear above other content. It consists of five key components:

```
Application.Popover (static accessor)
    └── ApplicationPopover (manager / singleton)
            └── IPopover (interface contract)
                    └── PopoverBaseImpl (abstract base)
                            └── PopoverMenu (concrete implementation)
```

### 1. `IPopover` (Interface) — `Terminal.Gui/App/IPopover.cs`

- **Minimal contract**: Only defines `IRunnable? Current { get; set; }`
- Associates a popover with a specific `IRunnable` for keyboard event scoping
- If `Current` is `null`, the popover receives all keyboard events globally
- If set, events only flow when the associated runnable is the active `TopRunnableView`

### 2. `PopoverBaseImpl` (Abstract Base) — `Terminal.Gui/App/PopoverBaseImpl.cs`

- Inherits from `View` and implements `IPopover`
- **Constructor sets critical defaults:**
  - `CanFocus = true`
  - `Width = Dim.Fill()`, `Height = Dim.Fill()` (fills entire screen)
  - `ViewportSettings = Transparent | TransparentMouse` (click-through behavior)
  - Binds `Application.QuitKey` → `Command.Quit` (hides popover)
- **`OnVisibleChanging` override**:
  - Becoming visible → calls `Layout(App.Screen.Size)` to resize
  - Becoming hidden → restores focus to `App.TopRunnableView.MostFocused`

### 3. `ApplicationPopover` (Manager) — `Terminal.Gui/App/ApplicationPopover.cs`

- **Singleton manager** held by `IApplication.Popover`
- Manages a list of registered `IPopover` instances plus one active popover
- **Key methods:**
  - `Register(popover)` — Adds to list, auto-sets `Current` to `TopRunnableView`
  - `DeRegister(popover)` — Removes from list, clears active if needed
  - `Show(popover)` — Validates requirements, hides previous active, initializes if needed, sets visible
  - `Hide(popover)` — Clears active, sets invisible, triggers redraw
  - `DispatchKeyDown(key)` — Routes keys: active popover gets ALL keys, inactive popovers also get dispatched
- **Show() validation**: Throws if not registered, missing Transparent flags, or missing Command.Quit binding
- **Dispose()**: Disposes all registered popovers on shutdown

### 4. `PopoverMenu` (Concrete) — `Terminal.Gui/Views/Menu/PopoverMenu.cs`

- Full cascading menu implementation built on `PopoverBaseImpl`
- **Constructor binds**: Left/Right arrow navigation, overrides Command.Quit
- **Key features:**
  - `Root` property: Sets the root `Menu` and wires up all event handlers recursively
  - `MakeVisible(Point?)`: Positions menu and calls `Show()`
  - `SetPosition(Point?)`: Calculates position, adjusts for screen edges
  - Automatic key binding discovery from `Command`-based `MenuItem`s
  - `OnKeyDownNotHandled`: Searches all menu items for matching key bindings
  - Cascading submenus via `ShowSubMenu()` / `HideAndRemoveSubMenu()`
- **Quit behavior**: Calls `RaiseAccepting` first (important for MenuBar integration), then hides
- **DefaultKey**: `Key.F10.WithShift` (configurable via `[ConfigurationProperty]`)

### 5. `Application.Popover` (Static Accessor) — `Terminal.Gui/App/Application.Popover.cs`

- Simple static partial class forwarding to `ApplicationImpl.Instance.Popover`
- Marked `[Obsolete]` as part of the legacy static API migration

---

## Key Design Patterns

### Transparency Model

Popovers fill the entire screen (`Dim.Fill()`) but use `Transparent` + `TransparentMouse` viewport settings. This means:

- The popover itself is invisible — only its SubViews (like `Menu`) are drawn
- Mouse clicks that don't hit a SubView pass through to underlying views
- This creates the "click outside to dismiss" behavior naturally

This is a clever technique: instead of implementing complex hit-testing to determine if a click is "outside" the popover, the framework's transparency system handles it automatically. The popover acts as a full-screen invisible overlay, and the `TransparentMouse` flag ensures mouse events that don't land on actual content simply pass through.

### Registration-Before-Show Pattern

Popovers MUST be registered before being shown. This is validated at runtime with `InvalidOperationException`. Registration enables:

- Keyboard event routing even when the popover is hidden (global hotkeys)
- Lifecycle management (auto-disposal on `Application.Shutdown`)
- Proper `IRunnable` association for scoped event routing

The registration step (`Register()`) and showing step (`Show()`) are intentionally separate because a popover may need to be registered once and shown/hidden many times.

### Keyboard Event Dispatch Order

In `DispatchKeyDown`:

1. **Active (visible) popover** gets ALL key events first via `NewKeyDownEvent`
2. If not handled, **inactive popovers** each get the key event (filtered by `Current` runnable match)
3. Inactive popovers skip if their `Current` doesn't match `TopRunnableView`

This ensures that:
- A visible popover can intercept any key (like Escape to close)
- Hidden popovers can still respond to global hotkeys (like Shift+F10 to open)
- Popovers scoped to a specific runnable only get events when that runnable is active

### Visibility-Driven Lifecycle

The popover lifecycle is tied to `Visible`:

- **`PopoverBaseImpl.OnVisibleChanging()`**: Layout on show, focus restore on hide
- **`PopoverMenu.OnVisibleChanged()`**: Add/remove root menu SubView on show/hide
- Setting `Visible = false` triggers `ApplicationPopover.Hide()`

This pattern means there is no separate "Open/Close" API — visibility IS the state machine. Setting `Visible = false` cascades through:
1. `OnVisibleChanging()` in `PopoverBaseImpl` (restores focus)
2. `OnVisibleChanged()` in `PopoverMenu` (removes SubViews, calls `Hide()`)
3. `ApplicationPopover.Hide()` (clears active popover, triggers redraw)

### Command Pattern Integration

`PopoverMenu` deeply integrates with Terminal.Gui's command system:

- `MenuItem`s can specify a `Command` enum value
- `UpdateKeyBindings()` discovers key bindings from the command system automatically
- If a `MenuItem` has a `TargetView`, it looks up the key binding from that view's `HotKeyBindings`
- If no `TargetView`, it checks `App.Keyboard.KeyBindings` (application-level)
- `OnKeyDownNotHandled` acts as a fallback, searching all `MenuItem`s for matching keys

This means menu items automatically show the correct keyboard shortcut without manual specification.

---

## Component Relationships

```
┌─────────────────────────────────────────────┐
│             IApplication                     │
│  ┌────────────────────────────────────────┐  │
│  │     ApplicationPopover (Manager)       │  │
│  │                                        │  │
│  │  _popovers: List<IPopover>             │  │
│  │  _activePopover: IPopover?             │  │
│  │                                        │  │
│  │  Register() / DeRegister()             │  │
│  │  Show() / Hide()                       │  │
│  │  DispatchKeyDown()                     │  │
│  └────────────────────────────────────────┘  │
│                    │                          │
│                    │ manages                  │
│                    ▼                          │
│  ┌────────────────────────────────────────┐  │
│  │     PopoverBaseImpl : View, IPopover   │  │
│  │                                        │  │
│  │  Width/Height = Dim.Fill()             │  │
│  │  ViewportSettings = Transparent        │  │
│  │  Command.Quit → Hide                  │  │
│  │  Current: IRunnable? (scoping)         │  │
│  └────────────────────────────────────────┘  │
│                    │                          │
│                    │ extends                  │
│                    ▼                          │
│  ┌────────────────────────────────────────┐  │
│  │     PopoverMenu : PopoverBaseImpl      │  │
│  │                                        │  │
│  │  Root: Menu (menu tree)                │  │
│  │  MakeVisible() / SetPosition()         │  │
│  │  Arrow key navigation                  │  │
│  │  Cascading submenus                    │  │
│  └────────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

---

## Integration Points

### Keyboard Pipeline

The keyboard dispatch in `KeyboardImpl.RaiseKeyDownEvent()` (`Terminal.Gui/App/Keyboard/KeyboardImpl.cs:148-206`) routes keys through the popover system **before** sending them to the `TopRunnableView`:

1. `KeyDown` event is raised (line 160)
2. **`App?.Popover?.DispatchKeyDown(key)`** is called (line 167) — popovers get first priority
3. If no popover handled it, `TopRunnableView.NewKeyDownEvent()` is called (line 192)
4. Finally, application-level command bindings are checked (line 198)

### Mouse Pipeline

The mouse dismiss logic in `MouseImpl.RaiseMouseEvent()` (`Terminal.Gui/App/Mouse/MouseImpl.cs:42-93`):

1. Gets views under the mouse location (line 60)
2. If a mouse press occurs outside the active popover's hierarchy (lines 83-85), calls `HideWithQuitCommand()` to dismiss the popover
3. Recursively re-raises the mouse event so the underlying view receives the click (line 90)
4. A secondary check at line 109-113 ensures mouse events only go to views inside the `TopRunnableView` or active popover hierarchy

### Application Lifecycle

- **Initialization**: `ApplicationImpl.Popover` is lazy-initialized (`ApplicationImpl.cs:251-260`)
- **Session End**: `ApplicationImpl.Run.End()` hides the active popover (`ApplicationImpl.Run.cs:300-305`)
- **Shutdown**: `ApplicationImpl.ResetState()` hides active, disposes `ApplicationPopover`, sets to null (`ApplicationImpl.Lifecycle.cs:248-258`)

### MenuBar Integration

- `MenuBar` creates/manages `PopoverMenu` instances for each `MenuBarItem`
- `MenuBarItem.PopoverMenu` property subscribes to `VisibleChanged` and `Accepted` events
- `MenuBar.ShowItem()` calls `PopoverMenu.MakeVisible()` and subscribes to `Accepting`
- `MenuBar.HideItem()` / `HideActiveItem()` manages visibility state

### View Integration

- `TextField` creates a context menu (`TextField.cs:106-127`) and registers it with `App.Popover`
- `CharMap` similarly uses `PopoverMenu.DefaultKey` for context menu binding

---

## Code Review — Issues Found

### CRITICAL — Bugs That Can Cause Crashes or Memory Leaks

#### Issue 1: Event Handlers Not Unsubscribed When `PopoverMenu.Root` Changes

**File:** `PopoverMenu.cs:305-336`

When the `Root` property setter is called with a new value, it subscribes event handlers (`MenuOnAccepting`, `MenuAccepted`, `MenuOnSelectedMenuItemChanged`) to every menu in the *new* hierarchy — but never unsubscribes the same handlers from the *old* hierarchy:

```csharp
// Line 312: old root is hidden but handlers remain attached
HideAndRemoveSubMenu (_root);
_root = value;
// ...
// Line 328-335: only subscribes to new menus
foreach (Menu menu in allMenus)
{
    menu.Accepting += MenuOnAccepting;       // old menu still has this handler!
    menu.Accepted += MenuAccepted;
    menu.SelectedMenuItemChanged += MenuOnSelectedMenuItemChanged;
}
```

Contrast this with the `Dispose` method (`PopoverMenu.cs:682-700`) which *does* unsubscribe — proving the intent exists but was missed in the setter. If `Root` is set multiple times, handlers accumulate on old menu objects, preventing GC and causing ghost event firings.

**Fix:** Add unsubscription before `HideAndRemoveSubMenu(_root)`:

```csharp
if (_root is { })
{
    IEnumerable<Menu> oldMenus = GetAllSubMenus ();
    foreach (Menu menu in oldMenus)
    {
        menu.Accepting -= MenuOnAccepting;
        menu.Accepted -= MenuAccepted;
        menu.SelectedMenuItemChanged -= MenuOnSelectedMenuItemChanged;
    }
}
```

---

#### Issue 2: `DispatchKeyDown` Can Crash If a Popover Is DeRegistered During Iteration

**File:** `ApplicationPopover.cs:213-231`

The `foreach (IPopover popover in _popovers)` loop iterates the live `_popovers` list. If `NewKeyDownEvent()` on line 225 triggers user code that calls `DeRegister()`, the list is modified during enumeration, throwing `InvalidOperationException`:

```csharp
foreach (IPopover popover in _popovers)   // <-- iterating live list
{
    // ...
    hotKeyHandled = popoverView.NewKeyDownEvent (key);  // could trigger DeRegister()
}
```

**Fix:** Iterate a snapshot: `foreach (IPopover popover in _popovers.ToList())`.

---

#### Issue 3: `MenuBar.ShowItem` Subscribes to Wrong Event for Unsubscription

**File:** `MenuBar.cs:425-442`

This is a clear bug — the handler subscribes to `menuBarItem.Accepting` but tries to unsubscribe from `menuBarItem.PopoverMenu.VisibleChanged`:

```csharp
menuBarItem.Accepting += OnMenuItemAccepted;          // subscribes to Accepting

void OnMenuItemAccepted (object? sender, EventArgs args)
{
    menuBarItem.PopoverMenu.VisibleChanged -= OnMenuItemAccepted;  // wrong event!
    // ...
}
```

This means:
- The `Accepting` handler is **never** unsubscribed (memory leak, handler accumulation)
- Each call to `ShowItem()` adds *another* `OnMenuItemAccepted` handler since the local function captures the `menuBarItem` parameter (a new closure per call)
- Over time, repeated open/close cycles cause `OnMenuItemAccepted` to fire multiple times

---

### HIGH — Significant Design Issues

#### Issue 4: `HideWithQuitCommand` Has Confusing / Possibly Inverted Logic

**File:** `ApplicationPopover.cs:177-185`

```csharp
if (visiblePopover.Visible
    && (!visiblePopover.GetSupportedCommands ().Contains (Command.Quit)
    || (visiblePopover.InvokeCommand (Command.Quit) is true && visiblePopover.Visible)))
{
    visiblePopover.Visible = false;
}
```

This reads: "If the popover is visible AND (doesn't support Quit **OR** (Quit returned true AND still visible)), then force hide."

- If the popover **doesn't support** `Command.Quit`, it gets force-hidden anyway — this seems inverted. Why force-hide a popover that hasn't opted into Quit behavior?
- If `InvokeCommand(Command.Quit)` returns `true` (handled), the handler should have already hidden the popover. The `visiblePopover.Visible` re-check implies distrust of the handler.
- Called from `MouseImpl.RaiseMouseEvent` (line 87) and `ApplicationImpl.Run.End` (line 302).

---

#### Issue 5: Recursive `OnVisibleChanged` ↔ `HideAndRemoveSubMenu` Interaction

**File:** `PopoverMenu.cs:272-286` and `548-570`

`HideAndRemoveSubMenu` sets `Visible = false` on Root (line 567), which triggers `OnVisibleChanged`, which calls `HideAndRemoveSubMenu(_root)` again. Currently this doesn't infinite-loop because the second call finds `menu.Visible` already `false` and exits — but this is a fragile invariant:

```csharp
private void HideAndRemoveSubMenu (Menu? menu)
{
    if (menu is { Visible: true })       // guard saves us, but fragile
    {
        // ...
        menu.Visible = false;
        if (menu == Root) { Visible = false; }  // triggers OnVisibleChanged → HideAndRemoveSubMenu again
    }
}
```

**Fix:** Add a `_isHiding` guard flag to make this robust.

---

#### Issue 6: `Dispose()` Doesn't Clear `_activePopover`

**File:** `ApplicationPopover.cs:237-248`

After disposing all registered popovers and clearing the list, `_activePopover` is never set to `null`. If `GetActivePopover()` is called after `Dispose()`, it returns a reference to a disposed `View`, potentially causing `ObjectDisposedException`.

---

### MEDIUM — Design Smells and Edge Cases

#### Issue 7: `PopoverBaseImpl.OnVisibleChanging` — Counter-Intuitive Logic

**File:** `PopoverBaseImpl.cs:111-139`

The method checks `!Visible` to mean "about to become visible," which is correct but reads backwards:

```csharp
if (!Visible)   // Comment says: "When visible is changing to true"
{
    Layout (App.Screen.Size);
}
else            // Comment says: "When visible is changing to false"
{
    // restore focus
}
```

This works because `OnVisibleChanging` fires *before* the property value changes. The comments are correct but the code reads opposite to the comments. This is a maintenance trap.

---

#### Issue 8: `ForceFocusColors` Not Reset When Popover Is Closed

**File:** `PopoverMenu.cs:483, 497, 558`

When `ShowSubMenu()` is called, `menuItem.ForceFocusColors = true` (line 497). But when the entire popover is hidden (not just when switching to a peer submenu), `ForceFocusColors` is never reset to `false`. The `HideAndRemoveSubMenu` method only resets it for *peer* items (line 484), not for the item whose submenu is being hidden.

---

#### Issue 9: `Hide()` Silently No-Ops for Non-Active Popovers

**File:** `ApplicationPopover.cs:161-170`

If you call `Hide()` with a popover that isn't the active one, nothing happens — no exception, no return value, no logging:

```csharp
public void Hide (IPopover? popover)
{
    if (_activePopover is View popoverView && popoverView == popover)
    {
        // only hides if it's the active popover
    }
    // else: silently does nothing
}
```

---

#### Issue 10: `Show()` Skips All Validation If Popover Is Not a `View`

**File:** `ApplicationPopover.cs:130-152`

The entire validation and activation block is wrapped in `if (popover is View newPopover)`. If someone implements `IPopover` on a non-`View` class, `Show()` would pass the registration check but then silently skip all initialization.

---

#### Issue 11: Commented-Out Filter in `OnAccepting`

**File:** `PopoverMenu.cs:645-649`

```csharp
// Only raise Accepted if the command came from one of our MenuItems
//if (GetMenuItemsOfAllSubMenus ().Contains (args.Context?.Source))
{
    RaiseAccepted (args.Context);  // always runs — ignores the contract in the comment
}
```

`RaiseAccepted` fires unconditionally, even for commands that didn't originate from a `MenuItem`. The comment indicates intent to filter, but the filter is commented out.

---

#### Issue 12: Unused Variable in `GetMostVisibleLocationForSubMenu`

**File:** `PopoverMenu.cs:512`

```csharp
var pos = Point.Empty;  // created but never used
```

---

#### Issue 13: Indentation Error in `MenuOnAccepting`

**File:** `PopoverMenu.cs:584`

The `if` block starting at line 584 has incorrect indentation (extra indent level), suggesting a refactoring leftover.

---

#### Issue 14: Dynamic Menu Support Not Implemented (TODOs)

**File:** `PopoverMenu.cs:321-325`

Two TODO comments indicate that key bindings and event subscriptions should be updated whenever `MenuItem`s change in the tree, but this isn't implemented. Adding/removing menu items dynamically after `Root` is set results in stale bindings and missing event handlers.

---

## Test Coverage Summary

### Existing Test Files (8 files, ~80+ tests)

| Test File | Project | Tests | Coverage Area |
|-----------|---------|-------|---------------|
| `UnitTests/Application/ApplicationPopoverTests.cs` | UnitTests | 11 | App lifecycle, disposal, runnable scoping, GetViewsUnderMouse |
| `UnitTestsParallelizable/Application/Popover/Application.PopoverTests.cs` | Parallelizable | 8 | Register/DeRegister, Show/Hide, DispatchKeyDown routing |
| `UnitTestsParallelizable/Application/Popover/PopoverBaseImplTests.cs` | Parallelizable | 5 | Constructor defaults, Show validation |
| `IntegrationTests/FluentTests/PopverMenuTests.cs` | Integration | 9 | EnableForDesign, QuitKey, focus, context menus, submenus |
| `IntegrationTests/FluentTests/MenuBarvTests.cs` | Integration | 15 | MenuBar activation, navigation, show/hide popovers |
| `UnitTests/Views/MenuBarTests.cs` | UnitTests | 17 | MenuBar hotkeys, mouse, dynamic updates, disabled state |
| `UnitTestsParallelizable/Views/MenuBarItemTests.cs` | Parallelizable | 1 | Constructor defaults |
| `TerminalGuiFluentTesting/TestContext.ContextMenu.cs` | Test Helper | — | `WithContextMenu()` helper method |

### Test Coverage Gaps

#### Gap 1: No Test for Setting `Root` Multiple Times
**Covers bug:** Issue 1 (event handler leak)

No test verifies that setting `PopoverMenu.Root` to a new value properly unsubscribes from the old menu hierarchy. A test should:
1. Create a `PopoverMenu`, set `Root` to Menu A
2. Set `Root` to Menu B
3. Trigger events on Menu A and verify they do NOT reach the `PopoverMenu`
4. Verify Menu A's event subscribers are empty

---

#### Gap 2: No Test for `DeRegister` During `DispatchKeyDown`
**Covers bug:** Issue 2 (iteration crash)

No test verifies that calling `DeRegister()` from within a key handler doesn't crash. A test should:
1. Register two popovers
2. Have the first popover's key handler call `DeRegister()` on the second
3. Verify no `InvalidOperationException`

---

#### Gap 3: No Test for `MenuBar.ShowItem` Handler Accumulation
**Covers bug:** Issue 3 (wrong event unsubscription)

No test verifies that calling `ShowItem()` multiple times doesn't accumulate `Accepting` handlers. Note: `MenuBarTests.cs` has a **skipped test** (`Mouse_Click_Deactivates`) commenting about a "known issue with popover mouse handling" — this may be related.

---

#### Gap 4: No Test for `HideWithQuitCommand` Edge Cases
**Covers issue:** Issue 4

No test verifies behavior when `HideWithQuitCommand` is called on a popover that doesn't support `Command.Quit`. The current behavior (force-hide) is untested and possibly wrong.

---

#### Gap 5: No Test for `Hide()` With Non-Active Popover
**Covers issue:** Issue 9

No test verifies behavior when `Hide()` is called with a registered-but-not-active popover.

---

#### Gap 6: No Test for `Dispose()` Cleanup of `_activePopover`
**Covers issue:** Issue 6

Tests verify that registered popovers are disposed on shutdown, but no test verifies that `GetActivePopover()` returns `null` after disposal.

---

#### Gap 7: No Test for Showing the Same Popover Twice

No test verifies what happens when `Show()` is called with the already-active popover. Currently it hides and re-shows, which is wasteful but not harmful.

---

#### Gap 8: No Test for `OnVisibleChanging` Focus Restoration Path

`PopoverBaseImpl.OnVisibleChanging` restores focus when hiding (line 132-135). Integration tests verify focus restoration via `QuitKey_Restores_Focus_Correctly`, but no unit test directly tests the `OnVisibleChanging` path — especially the `ApplicationNavigation.IsInHierarchy` check.

---

#### Gap 9: No Test for `ForceFocusColors` State After Popover Close
**Covers issue:** Issue 8

No test verifies that `MenuItem.ForceFocusColors` is properly reset when the entire popover is closed.

---

#### Gap 10: No Test for Mouse Dismiss → Recurse Behavior

`MouseImpl.cs:86-92` hides the popover on outside click and then **recursively calls** `RaiseMouseEvent` so the click passes through. No test verifies:
- That the recursion doesn't infinite-loop
- That the underlying view actually receives the click
- That the recursion happens exactly once

---

## Issues Summary Table

| # | Issue | Severity | File:Line | Has Test? |
|---|-------|----------|-----------|-----------|
| 1 | Event handlers not unsubscribed on `Root` change | **CRITICAL** | PopoverMenu.cs:305 | No |
| 2 | `DispatchKeyDown` iteration crash on `DeRegister` | **CRITICAL** | ApplicationPopover.cs:213 | No |
| 3 | `ShowItem` subscribes/unsubscribes wrong events | **CRITICAL** | MenuBar.cs:425-442 | No |
| 4 | `HideWithQuitCommand` confusing/inverted logic | HIGH | ApplicationPopover.cs:177 | No |
| 5 | Recursive `OnVisibleChanged` ↔ `HideAndRemoveSubMenu` | HIGH | PopoverMenu.cs:272,548 | No |
| 6 | `Dispose()` doesn't clear `_activePopover` | HIGH | ApplicationPopover.cs:237 | No |
| 7 | `OnVisibleChanging` counter-intuitive `!Visible` check | MEDIUM | PopoverBaseImpl.cs:120 | Partial |
| 8 | `ForceFocusColors` not reset on popover close | MEDIUM | PopoverMenu.cs:497 | No |
| 9 | `Hide()` silent no-op for non-active popover | MEDIUM | ApplicationPopover.cs:161 | No |
| 10 | `Show()` skips validation for non-View IPopover | MEDIUM | ApplicationPopover.cs:130 | No |
| 11 | Commented-out filter in `OnAccepting` | MEDIUM | PopoverMenu.cs:645 | No |
| 12 | Unused variable `pos` | LOW | PopoverMenu.cs:512 | — |
| 13 | Indentation error | LOW | PopoverMenu.cs:584 | — |
| 14 | Dynamic menu updates not supported | MEDIUM | PopoverMenu.cs:321 | — |

---

## Source File Index

| File | Purpose |
|------|---------|
| `Terminal.Gui/App/IPopover.cs` | Interface contract |
| `Terminal.Gui/App/PopoverBaseImpl.cs` | Abstract base class |
| `Terminal.Gui/App/ApplicationPopover.cs` | Manager singleton |
| `Terminal.Gui/App/Application.Popover.cs` | Static accessor (legacy) |
| `Terminal.Gui/Views/Menu/PopoverMenu.cs` | Concrete cascading menu |
| `Terminal.Gui/Views/Menu/MenuBar.cs` | MenuBar integration |
| `Terminal.Gui/Views/Menu/MenuBarItem.cs` | MenuBarItem integration |
| `Terminal.Gui/App/Keyboard/KeyboardImpl.cs` | Keyboard dispatch pipeline |
| `Terminal.Gui/App/Mouse/MouseImpl.cs` | Mouse dismiss pipeline |
| `Terminal.Gui/App/ApplicationImpl.cs` | Popover lazy init |
| `Terminal.Gui/App/ApplicationImpl.Lifecycle.cs` | Shutdown cleanup |
| `Terminal.Gui/App/ApplicationImpl.Run.cs` | Session end cleanup |
| `docfx/docs/Popovers.md` | User-facing documentation |
