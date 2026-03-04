# Plan: PR #4773 — Simplify MenuBarItem via Popover\<TView, TResult\> Base Class

**Issue:** #4765 — Simplify `MenuBarItem` (~80% code reduction)
**Branch:** `copilot/simplify-menubaritem-code` (reset to `copilot/refactor-popover-menu-structure`)
**Base:** `v2_develop`

## Goal

With `Popover<TView, TResult>` handling the generic popover-ownership lifecycle (registration, bridging, visibility sync, focus-loss close), `MenuBarItem` drops its boilerplate from ~338 lines to ~100 lines.

## Current State

### MenuBarItem.cs (338 lines) — Responsibilities

| Lines     | Responsibility                         | After Refactor          |
|-----------|----------------------------------------|-------------------------|
| 14–72     | Constructors (4 overloads)             | **SIMPLIFIED** — remove `RegisterPopover` calls |
| 77–103    | `SetupCommands` (HotKey handler)       | **KEPT** — menu-specific |
| 106–140   | `OnActivating` (toggle + bridged ignore) | **SIMPLIFIED** — use `IsOpen` instead of `PopoverMenuOpen` |
| 142–148   | `RegisterPopover()`                    | **REMOVED** — base auto-registers |
| 151–164   | `EndInit` override                     | **REMOVED** — base handles init |
| 166–170   | `SubMenu` override (throws)           | **KEPT** — menu-specific |
| 172–216   | `PopoverMenu` property + `_popoverBridge` | **SIMPLIFIED** — sets `Target` + `Anchor` only |
| 221–259   | `PopoverMenuOpen` CWP property         | **REMOVED** — delegates to `PopoverMenu.IsOpen` |
| 261–275   | `OnPopoverMenuOpenChanging/Changed` events | **REMOVED** — use base `IsOpenChanging/Changed` |
| 278–297   | `OnKeyDownNotHandled`                  | **KEPT** — menu-specific |
| 300–310   | `OnHasFocusChanged`                    | **REMOVED** — base Target focus tracking |
| 313–320   | `EnableForDesign`                      | **KEPT** — menu-specific |
| 323–337   | `Dispose`                              | **SIMPLIFIED** — just null PopoverMenu |

### Popover\<TView, TResult\>.cs (506 lines) — What it provides

- `ContentView` property with CommandBridge (ContentView → Popover)
- `IsOpen` CWP property synchronized with `Visible`
- `IsOpenChanging` / `IsOpenChanged` events
- `Anchor` property for positioning
- `MakeVisible` / `SetPosition` / `GetAdjustedPosition`
- `OnVisibleChanged` with IsOpen sync
- `Target` — currently a simple auto-property (weak reference only)
- `Dispose` — cleans up content bridge

### What's MISSING from the base class

1. **Target property with bridge + focus tracking** — Currently just `WeakReference<View?>? Target { get; set; }`. Needs:
   - `CommandBridge` from Popover → Target (relay Activate)
   - `HasFocusChanged` subscription for auto-close on focus loss
2. **Auto-registration** — `MakeVisible` currently requires pre-registration; MenuBarItem calls `RegisterPopover()` manually
3. **Virtual methods** — `MakeVisible` and `SetPosition` are non-virtual; PopoverMenu needs to override `SetPosition`
4. **Trace.Command calls** — No tracing in base class

---

## Implementation Plan

### Phase 1: Enhance `Popover<TView, TResult>` Base Class

**File:** `Terminal.Gui/App/Popover.cs`

#### 1.1 Add `using Terminal.Gui.Tracing;`
Already present (line 1 is blank, but the file starts in `Terminal.Gui.App` namespace). Need to add the using.

#### 1.2 Add `_targetCommandBridge` field
```csharp
private CommandBridge? _targetCommandBridge;
```

#### 1.3 Enhance `Target` property (replace simple auto-property)
Replace `public WeakReference<View?>? Target { get; set; }` with a full property that:
- Unsubscribes from old target's `HasFocusChanged`
- Disposes old `_targetCommandBridge`
- Subscribes to new target's `HasFocusChanged`
- Creates `CommandBridge.Connect (newTarget, this, Command.Activate)` — bridges Activate from Popover → Target
- Adds `Trace.Command` calls

#### 1.4 Add `OnTargetHasFocusChanged` handler
Private method that sets `IsOpen = false` when target loses focus (`!e.NewValue`).

#### 1.5 Add auto-registration to `MakeVisible`
Before showing, check `App?.Popovers?.IsRegistered (this)` — if not registered, call `popovers.Register (this)`.
Change `App!.Popovers?.Show` to `App?.Popovers?.Show`.

#### 1.6 Make `MakeVisible` virtual
Change `public void MakeVisible` → `public virtual void MakeVisible`.

#### 1.7 Make `SetPosition` virtual
Change `public void SetPosition` → `public virtual void SetPosition`.

#### 1.8 Add Trace.Command calls throughout
Add tracing to: `ContentView` setter, `Target` setter, `IsOpen` setter, `MakeVisible`, `OnVisibleChanged`, `ContentViewOnVisibleChanged`.

#### 1.9 Update `Dispose` to clean up Target
In the disposing block, unsubscribe from target's `HasFocusChanged` and dispose `_targetCommandBridge`.

#### 1.10 Update XML docs
- `Target` property — document bridge + focus tracking behavior
- `MakeVisible` — document auto-registration
- `IsOpen` — note Visible sync details

---

### Phase 2: Update `PopoverMenu` for virtual methods

**File:** `Terminal.Gui/Views/Menu/PopoverMenu.cs`

#### 2.1 Change `SetPosition` from `new` to `override`
Change `public new void SetPosition` → `public override void SetPosition`.

#### 2.2 Add `#if DEBUG` Id assignment in `Root` setter
Add `Id = $"{value?.Id}PopoverMenu";` inside `#if DEBUG` block.

#### 2.3 Add Trace.Command to `Root` setter
Add trace when old/new root changes.

#### 2.4 Fix Root setter submenu cleanup
When setting a new Root, old root's submenus should be `Remove`d and `Dispose`d (not just unsubscribed from events).

---

### Phase 3: Simplify `MenuBarItem`

**File:** `Terminal.Gui/Views/Menu/MenuBarItem.cs`

#### 3.1 Simplify `PopoverMenu` property
Replace the full setter with:
```csharp
public PopoverMenu? PopoverMenu
{
    get;
    set
    {
        if (field == value)
        {
            return;
        }

        field = value;

        if (field is null)
        {
            return;
        }

        // Set Target for base class bridge + focus tracking
        field.Target = new WeakReference<View?> (this);

        // Set Anchor for positioning below MenuBarItem
        field.Anchor = () => FrameToScreen ();
    }
}
```
Remove: `_popoverBridge` field, `CommandBridge.Connect`, `OnPopoverVisibleChanged` handler, `RegisterPopover()` call, `VisibleChanged` subscription.

#### 3.2 Replace `PopoverMenuOpen` with delegation
```csharp
public bool PopoverMenuOpen
{
    get => PopoverMenu?.IsOpen ?? false;
    set
    {
        if (PopoverMenu is { })
        {
            PopoverMenu.IsOpen = value;
        }
    }
}
```
Remove: entire CWP block, `field` backing.

#### 3.3 Remove CWP events and virtuals
Remove:
- `OnPopoverMenuOpenChanging` virtual method
- `PopoverMenuOpenChanging` event
- `OnPopoverMenuOpenChanged` virtual method
- `PopoverMenuOpenChanged` event

Note: Consumers should use `PopoverMenu.IsOpenChanging` / `PopoverMenu.IsOpenChanged` instead.

#### 3.4 Remove `RegisterPopover()` method
Delete entirely — base class auto-registers in `MakeVisible`.

#### 3.5 Remove `EndInit` override
Delete entirely — base class handles ContentView initialization. If PopoverMenu registration is needed, base `MakeVisible` auto-registers.

#### 3.6 Remove `OnHasFocusChanged` override
Delete entirely — base `Target` property now tracks focus and auto-closes.

#### 3.7 Simplify `OnActivating`
Update to use `PopoverMenu.IsOpen` directly:
```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    if (base.OnActivating (args))
    {
        return true;
    }

    if (args.Context?.Routing == CommandRouting.Bridged)
    {
        return false;
    }

    PopoverMenuOpen = !PopoverMenuOpen;

    return false;
}
```

#### 3.8 Simplify `Dispose`
```csharp
protected override void Dispose (bool disposing)
{
    if (disposing)
    {
        PopoverMenu?.Dispose ();
        PopoverMenu = null;
    }

    base.Dispose (disposing);
}
```
Remove: `App.Popovers.DeRegister` call (base handles this), and the inverted `!IsRegistered` check (which was a bug anyway — should be `IsRegistered`).

---

### Phase 4: Update Tests

**File:** `Tests/UnitTestsParallelizable/Views/MenuBarItemTests.cs`

#### 4.1 Verify all existing tests pass
The following tests should pass unchanged because `PopoverMenuOpen` still exists as a delegating property:
- `Constructors_Defaults`
- `Command_Activate_Executes_Action`
- `Command_Activate_Activates_PopoverMenu`
- `Command_Accept_Does_Not_Execute_Action`
- `Command_HotKey_Executes_Action`
- `PopoverMenu_Is_Registered_By_Init` — may need update (registration timing changes with auto-register)
- `PopoverMenu_Is_Registered_By_Activate`
- `PopoverMenu_Is_Registered_By_Set` — may need update

#### 4.2 Add new tests
- `PopoverMenuOpen_Delegates_To_IsOpen` — verify `PopoverMenuOpen` reads from `PopoverMenu.IsOpen`
- `Target_Is_Set_When_PopoverMenu_Assigned` — verify `PopoverMenu.Target` points back to MenuBarItem
- `PopoverMenu_Closes_On_Focus_Loss` — verify base class Target focus tracking works for MenuBarItem
- `Anchor_Is_Set_When_PopoverMenu_Assigned` — verify `PopoverMenu.Anchor` is set to `FrameToScreen()`

---

## Verification Steps

1. `dotnet build --no-restore` — no warnings, no errors
2. `dotnet test Tests/UnitTestsParallelizable --no-build --filter MenuBarItem` — all MenuBarItem tests pass
3. `dotnet test Tests/UnitTestsParallelizable --no-build --filter MenuBar` — all MenuBar tests pass
4. `dotnet test Tests/UnitTestsParallelizable --no-build --filter Popover` — all Popover tests pass
5. `dotnet test Tests/UnitTestsParallelizable --no-build` — full parallel suite passes
6. `dotnet test Tests/UnitTests --no-build` — full non-parallel suite passes

## Expected Result

MenuBarItem reduces from ~338 lines to ~100-120 lines (~65-70% reduction). The remaining code is:
- Constructors (~30 lines)
- `SetupCommands` — HotKey handler (~25 lines)
- `OnActivating` — toggle with bridged-command ignore (~15 lines)
- `SubMenu` override (~3 lines)
- `PopoverMenu` property — thin wrapper setting Target + Anchor (~15 lines)
- `PopoverMenuOpen` — delegation to IsOpen (~10 lines)
- `OnKeyDownNotHandled` — hotkey close (~15 lines)
- `EnableForDesign` (~8 lines)
- `Dispose` — simplified (~8 lines)

## Risk Assessment

- **Breaking change risk:** `PopoverMenuOpenChanging`/`PopoverMenuOpenChanged` events are removed. Consumers must switch to `PopoverMenu.IsOpenChanging`/`IsOpenChanged`. This is acceptable for v2 alpha.
- **Registration timing:** Tests `PopoverMenu_Is_Registered_By_Init` and `PopoverMenu_Is_Registered_By_Set` may need updates since registration now happens lazily at `MakeVisible` time rather than eagerly at `EndInit`/property-set time.
- **Focus tracking:** Base class `OnTargetHasFocusChanged` replaces MenuBarItem's `OnHasFocusChanged`. Must verify the behavior is equivalent (both set `IsOpen = false` when focus is lost).
