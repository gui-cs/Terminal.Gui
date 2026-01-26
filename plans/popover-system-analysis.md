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

## Test Coverage Summary

### `PopoverBaseImplTests` (Parallelizable)

- Constructor defaults verification
- `Current` property get/set
- `Show` throws on missing Transparent flags
- `Show` throws on missing Quit command
- `Show` throws if not registered

### `ApplicationPopoverTests` (Parallelizable)

- Register/DeRegister with Moq
- Show/Hide active popover management
- DispatchKeyDown routing (active gets all keys, inactive gets hotkeys+keys)
- Uses a custom `PopoverTestClass : View, IPopover` (NOT PopoverBaseImpl)

### `ApplicationPopoverTests` (UnitTests — non-parallel)

- Application.Init/Shutdown lifecycle
- Registered popovers disposed on Shutdown
- DeRegistered popovers NOT disposed on Shutdown
- Application.End hides active but doesn't reset manager
- Register auto-sets Runnable
- Keyboard events scoped to associated Runnable
- `GetViewsUnderMouse` with active popover (transparent mouse behavior)
- Uses `PopoverTestClass : PopoverBaseImpl`

### `PopoverMenuTests` (Integration — FluentTesting)

- `EnableForDesign` creates correct menu items
- Show sets navigation/focus correctly
- QuitKey hides and restores focus
- QuitKey doesn't quit the app when popover handles it
- Inactive popover doesn't consume Space/Enter/QuitKey events
- Context menu with right-click + left-click selection
- Context menu with submenu navigation (arrow keys + Enter)

---

## Source File Index

| File | Purpose |
|------|---------|
| `Terminal.Gui/App/IPopover.cs` | Interface contract |
| `Terminal.Gui/App/PopoverBaseImpl.cs` | Abstract base class |
| `Terminal.Gui/App/ApplicationPopover.cs` | Manager singleton |
| `Terminal.Gui/App/Application.Popover.cs` | Static accessor (legacy) |
| `Terminal.Gui/Views/Menu/PopoverMenu.cs` | Concrete cascading menu |
| `docfx/docs/Popovers.md` | User-facing documentation |
