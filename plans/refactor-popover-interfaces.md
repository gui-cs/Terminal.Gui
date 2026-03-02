# Refactor Popover Interfaces

## Context

`IPopover` has only `Owner` — callers of `GetActivePopover()` get a near-useless interface and must cast to `View`. `ApplicationPopover` is littered with `if (popover is View ...)` casts. Key operations like `MakeVisible`, `Visible`, and `Anchor` are only on concrete classes. There are no events for popover registration/deregistration. `IsOpen` on `Popover<>` duplicates `View.Visible` which already has CWP events (`VisibleChanging`/`VisibleChanged`).

**Goal:**
1. Create `IPopoverView : IPopover` with View-level popover operations
2. Remove `IsOpen` (redundant — `View.Visible` + its CWP events suffice)
3. Rename `PopoverBaseImpl` → `PopoverImpl`
4. Move common members from `Popover<>` down to `PopoverImpl`
5. Add registration events to `ApplicationPopover`
6. Eliminate View casts at caller sites where possible

---

## Step 1: Create `IPopoverView` interface

**New file:** `Terminal.Gui/App/IPopoverView.cs`

```csharp
public interface IPopoverView : IPopover
{
    // Visibility & enablement (satisfied by View base)
    bool Visible { get; set; }
    bool Enabled { get; set; }

    // Popover-specific
    Func<Rectangle?>? Anchor { get; set; }
    WeakReference<View>? Target { get; set; }

    // Operations
    void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null);

    // View operations needed by framework (eliminates casts at caller sites)
    void SetNeedsDraw ();
    void SetNeedsLayout ();
    bool AdvanceFocus (NavigationDirection direction, TabBehavior? behavior);
}
```

No `IsOpen` — `Visible` is the single source of truth. Consumers use `VisibleChanging` (cancellable) and `VisibleChanged` from View.

## Step 2: Rename `PopoverBaseImpl` → `PopoverImpl` and move members

**Rename file:** `Terminal.Gui/App/PopoverBaseImpl.cs` → `Terminal.Gui/App/PopoverImpl.cs`

Changes to `PopoverImpl`:
- Class declaration: `public abstract class PopoverImpl : View, IPopoverView`
- **Add from `Popover<>`:**
  - `Anchor` property (`Func<Rectangle?>?`)
  - `MakeVisible` as `virtual` method — base impl does `Layout ()` + `App!.Popovers?.Show (this)` (no `SetPosition` since that depends on `ContentView`)
- **Add `OnVisibleChanged` override** to call `App?.Popovers?.Hide (this)` when hiding (moved from `Popover<>`)
- `Visible`, `Enabled`, `SetNeedsDraw`, `SetNeedsLayout`, `AdvanceFocus` are inherited from `View` — satisfy `IPopoverView` implicitly
- `Target` is already on `PopoverBaseImpl` — just stays, now satisfies `IPopoverView.Target`
- Update all `#if DEBUG Id` references

## Step 3: Update `Popover<TView, TResult>`

**File:** `Terminal.Gui/App/Popover.cs`

- Change base: `Popover<TView, TResult> : PopoverImpl, IDesignable`
- **Remove entirely:** `_isOpen` field, `IsOpen` property, `IsOpenChanging`/`IsOpenChanged` events, `OnIsOpenChanging`/`OnIsOpenChanged` virtual methods
- **Remove** (moved to base): `Anchor`
- **Override** `MakeVisible` to insert `SetPosition` call:
  ```csharp
  public override void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null)
  {
      if (Visible)
      {
          return;
      }

      Layout ();
      SetPosition (idealScreenPosition, anchor);
      App!.Popovers?.Show (this);
  }
  ```
- **Simplify** `OnVisibleChanged` — remove all IsOpen sync, just handle ContentView + ExtractResult before calling base:
  ```csharp
  protected override void OnVisibleChanged ()
  {
      if (Visible)
      {
          ContentView?.Visible = true;
      }
      else
      {
          ContentView?.Visible = false;
          ExtractResult ();
      }

      base.OnVisibleChanged (); // PopoverImpl handles Hide
  }
  ```
- `SetPosition`, `GetAdjustedPosition`, `ContentView`, `Result`, `ResultExtractor`, `ResultChanged`, `ExtractResult` stay on `Popover<>`

## Step 4: Update `ApplicationPopover`

**File:** `Terminal.Gui/App/ApplicationPopover.cs`

### Type changes
| Before | After |
|--------|-------|
| `List<IPopover> _popovers` | `List<IPopoverView> _popovers` |
| `IPopover? _activePopover` | `IPopoverView? _activePopover` |
| `IPopover? GetActivePopover ()` | `IPopoverView? GetActivePopover ()` |
| `Register (IPopover?)` → `IPopover?` | `Register (IPopoverView?)` → `IPopoverView?` |
| `DeRegister (IPopover?)` | `DeRegister (IPopoverView?)` |
| `Show (IPopover?)` | `Show (IPopoverView?)` |
| `Hide (IPopover?)` | `Hide (IPopoverView?)` |
| `IsRegistered (IPopover?)` | `IsRegistered (IPopoverView?)` |
| `IReadOnlyCollection<IPopover> Popovers` | `IReadOnlyCollection<IPopoverView> Popovers` |

### New events
```csharp
public event EventHandler<EventArgs<IPopoverView>>? PopoverRegistered;
public event EventHandler<EventArgs<IPopoverView>>? PopoverDeRegistered;
```
- Raise `PopoverRegistered` at end of `Register()` after `_popovers.Add()`
- Raise `PopoverDeRegistered` at end of `DeRegister()` after `_popovers.Remove()`

(`EventArgs<T>` exists at `Terminal.Gui/App/CWP/EventArgs.cs`)

### Cast reductions
- `Dispose()`: cast to `IDisposable` instead of `View`
- `Hide()`: use `IPopoverView.Visible` directly; cast to View only for `App?.TopRunnableView?.SetNeedsDraw()`
- `Show()`: use `IPopoverView.Visible`/`.Enabled` directly; keep View cast for validation (`ViewportSettings`, `KeyBindings`, `BeginInit/EndInit`)
- `DispatchKeyDown()`: keep View cast — this is the app→view keyboard bridge (analogous to `ApplicationKeyboard`), so View coupling here is intentional

### Update `HideWithQuitCommand`
- Update references from `PopoverBaseImpl` → `PopoverImpl` if any

## Step 5: Update `IPopover` documentation

**File:** `Terminal.Gui/App/IPopover.cs`
- Add `<seealso cref="IPopoverView"/>` in remarks

## Step 6: Update caller sites

| File : Line | Current | After |
|---|---|---|
| `ApplicationImpl.Lifecycle.cs:254` | `is View popover` | `is { } popover` — **no cast** |
| `ApplicationImpl.Run.cs:306` | `as View is { Visible: true }` | Keep View cast (`HideWithQuitCommand` takes View) |
| `ApplicationImpl.Screen.cs:64` | `as View is { Visible: true }` | `IPopoverView` for `SetNeedsDraw`/`SetNeedsLayout`; View for `views.Insert` |
| `ApplicationNavigation.cs:117` | `as View is { Visible: true }` | `IPopoverView` directly — **fully eliminated** |
| `MouseImpl.cs:85,113` | `as View` | Keep View cast (`View.IsInHierarchy` takes View) |
| `View.Layout.cs:1195` | `is View { Visible: true }` | Keep View cast (layout needs View) |
| `Runnable.cs:177` | `is PopoverBaseImpl { Visible: true }` | `IPopoverView` directly — **fully eliminated** |

All `PopoverBaseImpl` references in these files → `PopoverImpl`.

## Step 7: Update `PopoverMenu`

**File:** `Terminal.Gui/Views/Menu/PopoverMenu.cs`

- Base class becomes `Popover<Menu, MenuItem>` (which now extends `PopoverImpl`) — no change to `PopoverMenu`'s declaration
- `PopoverMenu.SetPosition` `new` keyword: unchanged
- `PopoverMenu.OnVisibleChanged` call chain now: `PopoverMenu` → `Popover<>` → `PopoverImpl` → `View`
- Update doc references from `PopoverBaseImpl` → `PopoverImpl`

## Step 8: Rename in `Application.Popover.cs`

**File:** `Terminal.Gui/App/Application.Popover.cs`
- No structural changes needed (uses `ApplicationPopover` type, not `PopoverBaseImpl`)

## Step 9: Update tests

### Update type references
- `Tests/UnitTests/Application/ApplicationPopoverTests.cs` — `IPopover` → `IPopoverView`
- `Tests/UnitTestsParallelizable/Application/Popover/PopoverBaseImplTests.cs` — rename to `PopoverImplTests.cs`, update class refs
- `Tests/UnitTestsParallelizable/Application/Popover/PopoverTests.cs` — remove `IsOpen` tests
- `Tests/UnitTestsParallelizable/Application/Popover/Application.PopoverTests.cs` — update type refs

### Remove obsolete `IsOpen` tests
- `PopoverTests.cs:74` `IsOpen_Set_True_CallsMakeVisible` — **delete**
- `PopoverTests.cs:93` `IsOpen_Set_False_HidesPopover` — **delete**
- `PopoverTests.cs:113` `IsOpenChanging_CanCancel` — **delete**

### Add new tests
- `PopoverRegistered` / `PopoverDeRegistered` events fire correctly
- `IPopoverView` contract satisfied by `PopoverImpl`
- `Anchor`, `MakeVisible` work at `PopoverImpl` level

## Step 10: Update Popovers scenario

**File:** `Examples/UICatalog/Scenarios/Popovers.cs`
- Remove `OnPopoverIsOpenChanged` handler (line 181)
- Remove any `IsOpen` references
- Update `PopoverBaseImpl` → `PopoverImpl`

---

## `OnVisibleChanged` call chain

```
PopoverMenu.OnVisibleChanged:    ShowMenu/HideMenu
  → Popover<>.OnVisibleChanged:  ContentView.Visible, ExtractResult
    → PopoverImpl.OnVisibleChanged:  Hide (when becoming invisible)
      → View.OnVisibleChanged:   base behavior
```

## Risk: `PopoverMenu.SetPosition` with `new`

`new` does not participate in virtual dispatch. `Popover<>.MakeVisible` calls `this.SetPosition(...)` which resolves to `Popover<>.SetPosition`, not `PopoverMenu.SetPosition`. This is existing behavior, preserved.

---

## Files modified (summary)

| File | Action |
|---|---|
| `Terminal.Gui/App/IPopoverView.cs` | **NEW** |
| `Terminal.Gui/App/PopoverBaseImpl.cs` → `PopoverImpl.cs` | Rename + add `IPopoverView`, `Anchor`, `MakeVisible`, `OnVisibleChanged` |
| `Terminal.Gui/App/Popover.cs` | Remove `IsOpen`/events, remove `Anchor`, simplify `OnVisibleChanged` |
| `Terminal.Gui/App/ApplicationPopover.cs` | `IPopover` → `IPopoverView`, add events |
| `Terminal.Gui/App/IPopover.cs` | Doc update only |
| `Terminal.Gui/App/Application.Popover.cs` | No change |
| `Terminal.Gui/Views/Menu/PopoverMenu.cs` | Doc refs only |
| `Terminal.Gui/App/ApplicationImpl.Lifecycle.cs` | Remove View cast |
| `Terminal.Gui/App/ApplicationImpl.Screen.cs` | Partial cast elimination |
| `Terminal.Gui/App/ApplicationNavigation.cs` | Full cast elimination |
| `Terminal.Gui/Views/Runnable/Runnable.cs` | Full cast elimination |
| `Examples/UICatalog/Scenarios/Popovers.cs` | Remove `IsOpen` usage |
| Tests (multiple files) | Type renames, remove `IsOpen` tests, add new tests |

## Verification

```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build
dotnet test Tests/UnitTests --no-build
```

Run UICatalog: "Popovers" and "PopoverMenus" scenarios.
