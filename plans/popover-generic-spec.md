# Spec: Refactor `PopoverMenu` into Generic `Popover<TView, TResult>`

> **Issue:** #2404 — Replace ComboBox with Popover\<TView\> and PopoverEdit\<TView, TResult\>
> **Related:** #4751 — Anchor-based positioning for PopoverMenu
> **Prototype:** PR #4412 — POC with Anchor, ElementSpacing, DropDownListExample
> **Goal:** Generalize the popover infrastructure so that *any* `View` can be hosted in a popover, with typed result extraction — enabling `PopoverEdit<TView, TResult>` as the ComboBox replacement.

---

## 1. Motivation

Today `PopoverMenu` is a concrete class tightly coupled to `Menu` / `MenuItem`. But the popover pattern — "show a transient, non-modal view anchored to an owner, extract a value when the user makes a selection" — is general-purpose. The same mechanics should power:

| Use Case | TView | TResult |
|---|---|---|
| Context / Drop-down menu | `Menu` | `MenuItem` |
| Color picker dropdown | `ColorPicker` | `Color?` |
| Date picker dropdown | `DatePicker` | `DateTime?` |
| Autocomplete / combo-box | `ListView` | `string?` |
| Enum selector dropdown | `OptionSelector<T>` | `int?` |
| Character map dropdown | `CharMap` | `Rune` |
| Any custom editor | Any `View` + `IValue<T>` | `T` |

This mirrors how `Prompt<TView, TResult>` generalizes `Dialog` — but for **non-modal, inline popovers** rather than modal dialogs.

---

## 2. Existing Work (PR #4412)

PR #4412 contains foundational changes this spec builds on:

- **`PopoverMenu.Anchor`** — `Func<Rectangle?>?` delegate for positioning. Priority: explicit anchor param → `Anchor` property → `idealScreenPosition` → mouse position.
- **`GetAnchoredPosition()`** — Prefers below-anchor, flips above when overflow, clamps X to screen.
- **`MenuBarItem.PopoverMenuAnchor`** — Delegates to `PopoverMenu.MakeVisible(anchor:)`. Falls back to `FrameToScreen()`.
- **`Shortcut.ElementSpacing`** — Replaces hardcoded `GetMarginThickness()`. Used in dropdown scenarios for compact toggle buttons (`ElementSpacing = 0`).
- **`PopoverBaseImpl` docs** — Fullscreen is a *default*, not a requirement. Fixed-size popovers explicitly supported.
- **`TextField`/`TextView`** — Now set `ContextMenu.Anchor = () => FrameToScreen()` for keyboard-activated context menus.
- **`DropDownListExample`** — Throwaway POC demonstrating TextField + MenuBarItem dropdown pattern.

---

## 3. Design Overview

### 3.1 The Core Insight: `MenuBarItem` Is Generic Popover Ownership

Today `MenuBarItem` is ~260 lines, of which **~80% is generic popover-ownership boilerplate**:

| Responsibility | Generic? |
|---|---|
| Owns a `PopoverMenu` property | Yes |
| `CommandBridge` to relay Activate | Yes |
| `RegisterPopover()` with `Application.Popover` | Yes |
| `PopoverMenuOpen` CWP toggle | Yes |
| `VisibleChanged` sync | Yes |
| Close on focus loss (`OnHasFocusChanged`) | Yes |
| `EndInit` initialization | Yes |
| `Dispose` cleanup | Yes |
| `PopoverMenuAnchor` (PR #4412) | Yes |
| Custom `HotKey` handler (skip SetFocus) | **Menu-specific** |
| `OnKeyDownNotHandled` close on same hotkey | **Menu-specific** |
| `SubMenu` → throws | **Menu-specific** |

The same boilerplate would be duplicated in `PopoverEdit`. It must be extracted.

`IPopover` already has a TODO: `// TODO: Add WeakReference<View?>? Target {get; set;} - The view that commands will get bubbled up to` — this is exactly the "owner view" concept that `MenuBarItem` hand-rolls.

### 3.2 Type Hierarchy

```
PopoverBaseImpl (abstract, existing — Dim.Fill() default, not required)
│
├── Popover<TView, TResult> (NEW — generic popover host WITH owner lifecycle)
│   └── owns TView ContentView (SubView + CommandBridge)
│   └── Target: WeakReference<View?> — the owner view (implements IPopover TODO)
│   └── IsOpen CWP property — toggle visibility
│   └── Anchor positioning — promoted from PopoverMenu
│   └── Result extraction — mirrors Prompt<TView, TResult>
│   └── Auto: register, bridge, visibility sync, close-on-owner-focus-loss
│
├── PopoverMenu : Popover<Menu, MenuItem> (REFACTORED)
│   └── Root alias for ContentView
│   └── Fullscreen (required for cascading submenu clipping)
│   └── Menu-specific only: cascading submenus, key propagation, arrow nav
│
MenuBarItem : MenuItem (SIMPLIFIED — drops ~80% of boilerplate)
│   └── Has PopoverMenu (typed as Popover<Menu, MenuItem>)
│   └── Sets PopoverMenu.Target = this
│   └── Lifecycle delegated to PopoverMenu base class
│   └── Keeps only: HotKey skip-focus, hotkey-deactivation, SubMenu throwing
│
PopoverEdit<TView, TResult> : View, IValue<TResult?> (NEW — ComboBox replacement)
│   └── Has Popover<TView, TResult>
│   └── Sets Popover.Target = _textField (or this)
│   └── Lifecycle is automatic via base class
│   └── Just wires: TextField + toggle button + DisplayFormatter
```

### 3.3 Command Infrastructure Enhancement

```
ICommandContext (existing)
└── + object? Value { get; }        // NEW — carries source IValue.GetValue()

CommandContext (existing record struct)
└── + object? Value { get; init; }  // NEW — populated when source implements IValue
```

---

## 4. Detailed Design

### 4.1 `Popover<TView, TResult>` — Generic Popover Host with Owner Lifecycle

**File:** `Terminal.Gui/Views/Popover.cs`
**Namespace:** `Terminal.Gui.Views`

```csharp
public class Popover<TView, TResult> : PopoverBaseImpl, IDesignable
    where TView : View, new()
{
    // --- Content ---
    public TView? ContentView { get; set; }             // Hosted view (SubView + CommandBridge)

    // --- Owner / Target ---
    // Implements the IPopover TODO: "Add WeakReference<View?>? Target - view commands bubble up to"
    public WeakReference<View?>? Target { get; set; }   // The view that owns this popover

    // --- Open/Close (CWP — replaces MenuBarItem.PopoverMenuOpen) ---
    public bool IsOpen { get; set; }                    // CWP property: toggles Visible + MakeVisible
    protected virtual bool OnIsOpenChanging (ValueChangingEventArgs<bool> args);
    public event EventHandler<ValueChangingEventArgs<bool>>? IsOpenChanging;
    protected virtual void OnIsOpenChanged (ValueChangedEventArgs<bool> args);
    public event EventHandler<ValueChangedEventArgs<bool>>? IsOpenChanged;

    // --- Anchor-based Positioning (promoted from PopoverMenu) ---
    public Func<Rectangle?>? Anchor { get; set; }
    public void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null);

    // --- Result Extraction (mirrors Prompt<TView, TResult>) ---
    public Func<TView, TResult?>? ResultExtractor { get; set; }
    public TResult? Result { get; protected set; }
    public event EventHandler<ValueChangedEventArgs<TResult?>>? ResultChanged;

    // --- Automatic Lifecycle (replaces MenuBarItem boilerplate) ---
    // When Target is set:
    //   - Auto-registers with Application.Popover on EndInit
    //   - Auto-bridges Command.Activate from ContentView
    //   - Syncs IsOpen ↔ Visible
    //   - Closes (IsOpen = false) when Target loses focus
    //   - Disposes bridge + deregisters on Dispose
}
```

**Key behaviors:**

1. **Content management** — When `ContentView` is set, the previous content is removed and the new view is added as a SubView. A `CommandBridge` is established to relay `Command.Activate` from the content view to the popover.

2. **Target / Owner lifecycle** — When `Target` is set, the popover subscribes to the target view's `HasFocusChanged` event and auto-closes when the target loses focus. This replaces the manual `OnHasFocusChanged` override in `MenuBarItem`. The `Target` also implements the existing TODO on `IPopover` line 72.

3. **IsOpen CWP property** — Follows the Cancellable Workflow Pattern. Setting `IsOpen = true` calls `MakeVisible()`. Setting `IsOpen = false` sets `Visible = false`. `VisibleChanged` syncs back to `IsOpen`. This replaces `MenuBarItem.PopoverMenuOpen` — the exact same logic, generalized.

4. **Result extraction** — When the content view fires `Activate`, the popover extracts a result using the same priority chain as `Prompt<TView, TResult>`:
   1. Use `ResultExtractor` delegate if provided.
   2. If `ContentView` implements `IValue<TResult>`, use `.Value`.
   3. If `TResult` is `string`, use `ContentView.Text`.
   4. Otherwise, `Result` remains `null`.

5. **Anchor-based positioning** — `Anchor` property and `GetAnchoredPosition()` promoted from `PopoverMenu`. Positioning priority: explicit `anchor` param → `Anchor` property → `idealScreenPosition` → mouse position. `GetAnchoredPosition`: prefer below, flip above, clamp X.

6. **Auto-registration** — During `EndInit`, if `Target` is set and `Application.Popover` is available, the popover auto-registers. This replaces `MenuBarItem.RegisterPopover()`.

7. **Size flexibility** — `PopoverBaseImpl` defaults to `Dim.Fill()` but this is overridable. Non-cascading popovers can be fixed-size. Only `PopoverMenu` requires fullscreen for cascading submenu clipping.

### 4.2 `PopoverMenu` — Refactored as `Popover<Menu, MenuItem>`

**File:** `Terminal.Gui/Views/Menu/PopoverMenu.cs` (same file, much thinner)

```csharp
public class PopoverMenu : Popover<Menu, MenuItem>, IDesignable
{
    // Menu-specific: Root is an alias for ContentView
    public Menu? Root { get => ContentView; set => ContentView = value; }

    // Menu-specific properties (unchanged)
    public Key Key { get; set; }
    public static Key DefaultKey { get; set; }
    public MouseFlags MouseFlags { get; set; }

    // Menu-specific overrides only:
    // - OnSubViewAdded: setup SelectedMenuItemChanged, add all submenus
    // - OnActivating/OnActivated/OnAccepting: menu command routing
    // - OnKeyDownNotHandled: menu item key matching
    // - OnVisibleChanged: ShowMenu/HideMenu on Root
    // - MoveLeft/MoveRight: cursor key navigation
    // - ShowMenuItemSubMenu, GetMostVisibleLocationForSubMenu: cascading
    // - UpdateKeyBindings: key propagation
    //
    // DROPS (handled by base):
    // - Anchor, MakeVisible, SetPosition, GetAnchoredPosition → base
    // - _rootCommandBridge → base manages the ContentView bridge
    // - RootOnVisibleChanged → base IsOpen sync
    // - Dispose of bridge → base
    // - EnableForDesign stays
    //
    // Must remain fullscreen (cascading submenus are SubViews that would be clipped)
}
```

**`Menu` must implement `IValue<MenuItem?>`** — enables automatic result extraction:

```csharp
public class Menu : Bar, IDesignable, IValue<MenuItem?>
{
    public MenuItem? Value { get; set; }  // Currently selected/activated MenuItem
    public event EventHandler<ValueChangingEventArgs<MenuItem?>>? ValueChanging;
    public event EventHandler<ValueChangedEventArgs<MenuItem?>>? ValueChanged;
}
```

### 4.3 `MenuBarItem` — Dramatically Simplified

**File:** `Terminal.Gui/Views/Menu/MenuBarItem.cs` (same file, much smaller)

Today `MenuBarItem` has ~260 lines. After the refactor, the generic popover-ownership boilerplate is gone:

```csharp
public class MenuBarItem : MenuItem, IDesignable
{
    // --- PopoverMenu property (now thin) ---
    public PopoverMenu? PopoverMenu
    {
        get;
        set
        {
            // Set Target, Anchor, handle PopoverMenuAnchor
            // Delegates registration, bridging, visibility sync to Popover base class
            field = value;
            if (field is not null)
            {
                field.Target = new WeakReference<View?> (this);
                field.Anchor = PopoverMenuAnchor ?? (() => FrameToScreen ());
            }
        }
    }

    // --- PopoverMenuAnchor (from PR #4412, unchanged) ---
    public Func<Rectangle?>? PopoverMenuAnchor { get; set; }

    // --- Convenience: delegates to PopoverMenu.IsOpen ---
    public bool PopoverMenuOpen
    {
        get => PopoverMenu?.IsOpen ?? false;
        set { if (PopoverMenu is { }) PopoverMenu.IsOpen = value; }
    }

    // --- Menu-specific only ---
    // SetupCommands: HotKey handler that skips SetFocus before Activate
    // OnActivating: toggle PopoverMenuOpen (bridged commands ignored)
    // OnKeyDownNotHandled: close on same hotkey press
    // SubMenu: throws InvalidOperationException
    // OnHasFocusChanged: REMOVED — handled by Popover.Target focus tracking
    // RegisterPopover: REMOVED — handled by Popover base class
    // EndInit popover init: REMOVED — handled by Popover base class
    // CommandBridge: REMOVED — handled by Popover base class
    // VisibleChanged sync: REMOVED — handled by Popover.IsOpen sync
    // Dispose cleanup: SIMPLIFIED — just null out PopoverMenu
}
```

**What's removed from `MenuBarItem`:**
- `_popoverBridge` and `CommandBridge.Connect` (~10 lines) → base class
- `RegisterPopover()` (~5 lines) → base class
- `EndInit` popover initialization (~12 lines) → base class
- `OnHasFocusChanged` close-on-blur (~8 lines) → base class Target tracking
- `PopoverMenuOpen` CWP property with full `CWPPropertyHelper.ChangeProperty` (~35 lines) → delegates to `PopoverMenu.IsOpen`
- `OnPopoverMenuOpenChanging/Changed` virtual + events (~8 lines) → delegates to `PopoverMenu.IsOpenChanging/Changed`
- `VisibleChanged` sync handler (~3 lines) → base class `IsOpen` sync
- `Dispose` bridge cleanup (~10 lines) → base class

**What stays:**
- `SetupCommands` — custom HotKey handler (menu-specific focus race)
- `OnActivating` — toggle logic + bridged-command-ignore (menu-specific)
- `OnKeyDownNotHandled` — close on same hotkey (menu-specific)
- `SubMenu` → throws (menu-specific)
- Constructors (simplified)
- `EnableForDesign`

### 4.4 `ICommandContext` Enhancement — Value Propagation

**Files:** `Terminal.Gui/Input/ICommandContext.cs`, `Terminal.Gui/Input/CommandContext.cs`

```csharp
public interface ICommandContext
{
    // ... existing members ...
    public object? Value { get; }
}

public readonly record struct CommandContext : ICommandContext
{
    // ... existing members ...
    public object? Value { get; init; }
    public CommandContext WithValue (object? value) => this with { Value = value };
}
```

Populated from `IValue.GetValue()` at command invocation time. The `IValue` doc comments already reference `CommandContext.Value` — this implements that stated intent.

**Example: `MenuBar.Activated` with `ctx.Value`**

With `Menu : IValue<MenuItem?>`, when a `MenuItem` is activated inside a `PopoverMenu`, the value propagates through the bridge chain: `Menu` → `PopoverMenu` → `MenuBarItem` → `MenuBar`. At each step, `ctx.Value` carries the selected `MenuItem`. This enables clean handlers:

```csharp
// Clean — ctx.Value carries the selected MenuItem
menuBar.Activated += (sender, args) =>
{
    if (args.Context?.Value is MenuItem selected)
    {
        textField.Text = selected.Title;
    }
};
```

Compare to the current POC in `DropDownListExample` which must manually search SubViews to find the selection — the `ctx.Value` approach eliminates that boilerplate entirely.

### 4.5 `PopoverEdit<TView, TResult>` — The ComboBox Replacement

**File:** `Terminal.Gui/Views/PopoverEdit.cs`
**Namespace:** `Terminal.Gui.Views`

Replaces the throwaway `DropDownListExample` POC from PR #4412. Because `Popover<TView, TResult>` now handles all the ownership lifecycle, `PopoverEdit` is clean:

```csharp
public class PopoverEdit<TView, TResult> : View, IValue<TResult?>, IDesignable
    where TView : View, new()
{
    private TextField _textField;
    private Button _toggleButton;                       // ▼ glyph (simpler than MenuBarItem now)

    // --- Popover ---
    public Popover<TView, TResult> Popover { get; }    // Target = _textField, Anchor = textfield frame
                                                        // All lifecycle automatic

    // --- Configuration ---
    public bool ReadOnly { get; set; }
    public Func<TView, TResult?>? ResultExtractor { get; set; }
    public Func<TResult?, string>? DisplayFormatter { get; set; }

    // --- IValue<TResult?> ---
    public TResult? Value { get; set; }
    public event EventHandler<ValueChangingEventArgs<TResult?>>? ValueChanging;
    public event EventHandler<ValueChangedEventArgs<TResult?>>? ValueChanged;

    // --- Convenience ---
    public bool IsOpen => Popover.IsOpen;
    public void Open () => Popover.IsOpen = true;
    public void Close () => Popover.IsOpen = false;
}
```

**Key behaviors:**
- **Anchor** — `Popover.Anchor = () => _textField.FrameToScreen()`.
- **Target** — `Popover.Target = new WeakReference<View?>(this)` (or `_textField`).
- **Toggle** — Button click, `F4`, `Alt+Down` set `Popover.IsOpen`. All lifecycle automatic.
- **ReadOnly** — `true` = non-editable dropdown; `false` = editable combo-box.
- **Value flow** — Selection → `Popover.Result` → `PopoverEdit.Value` → `TextField.Text` via `DisplayFormatter`.
- **No boilerplate** — no manual bridge, registration, visibility sync, focus tracking. All handled by `Popover<TView, TResult>` base.

Note: `PopoverEdit` can use a simple `Button` for the toggle now (instead of `MenuBarItem` as in the POC) because the popover lifecycle is handled by `Popover<TView, TResult>`. The `MenuBarItem` was only needed in the POC because it was the only thing that knew how to manage a popover.

### 4.6 Non-Generic Convenience Alias

```csharp
public class PopoverEdit : PopoverEdit<ListView, string?>
{
    public IListDataSource? Source { get; set; }  // Delegates to ListView.Source
}
```

### 4.7 `IPopover` Enhancement

```csharp
public interface IPopover
{
    IRunnable? Owner { get; set; }

    // Implements the existing TODO on line 72:
    /// <summary>
    ///     Gets or sets the view that commands will be bubbled up to
    ///     and that is tracked for focus-loss auto-close behavior.
    /// </summary>
    WeakReference<View?>? Target { get; set; }
}
```

---

## 5. Migration Plan

### Phase 1: Infrastructure (Command Context)
1. Add `Value` property to `ICommandContext` and `CommandContext`.
2. Wire `Value` population into command invocation pipeline.

### Phase 2: IPopover.Target
3. Add `Target` property to `IPopover` (implements existing TODO).
4. Add `Target` to `PopoverBaseImpl`.

### Phase 3: Menu IValue
5. Add `IValue<MenuItem?>` to `Menu`.
6. Verify existing tests pass.

### Phase 4: Generic Popover
7. Create `Popover<TView, TResult>` base class:
   - `ContentView` management + `CommandBridge`
   - `IsOpen` CWP property with visibility sync
   - `Anchor` property + `GetAnchoredPosition`
   - `MakeVisible` / `SetPosition` with anchor priority chain
   - Result extraction chain (ResultExtractor → IValue → Text)
   - Auto-registration on `EndInit`
   - Target-based focus-loss auto-close
8. Refactor `PopoverMenu` to extend `Popover<Menu, MenuItem>`.
   - `Root` becomes alias for `ContentView`
   - Remove promoted code (Anchor, positioning, bridge, registration, visibility sync)
   - Keep menu-specific overrides
9. Simplify `MenuBarItem`:
   - `PopoverMenuOpen` delegates to `PopoverMenu.IsOpen`
   - Remove: bridge, registration, visibility sync, focus-loss handler, EndInit init
   - Keep: HotKey handler, OnActivating toggle, OnKeyDownNotHandled, SubMenu throws
10. Merge PR #4412 changes (Anchor, GetAnchoredPosition, ElementSpacing) into refactored code.
11. All existing `PopoverMenu`, `MenuBar`, and `MenuBarItem` tests pass.

### Phase 5: PopoverEdit
12. Implement `PopoverEdit<TView, TResult>`.
13. Implement non-generic `PopoverEdit` convenience alias.
14. Add unit tests to `UnitTestsParallelizable`.

### Phase 6: ComboBox Replacement
15. Replace all `ComboBox` usages with `PopoverEdit`.
16. Remove `ComboBox` class.
17. Replace `DropDownListExample` POC with proper `PopoverEdit` scenarios.

### Phase 7: Documentation
18. Update `docfx/docs/` with Popover and PopoverEdit documentation.
19. API documentation on all new public APIs.
20. Close #4751 (Anchor positioning fully resolved).

---

## 6. Key Design Decisions

### Q: Why `Popover<TView, TResult>` instead of `Popover<TView>` with separate result handling?

**A:** Following the `Prompt<TView, TResult>` precedent. C# doesn't support partial generic specialization (`TResult Popover<TView>`), so the two-parameter form is necessary for type-safe `Result`.

### Q: Why not make `PopoverBaseImpl` generic directly?

**A:** `PopoverBaseImpl` handles fundamentals (transparency, focus restoration, `Command.Quit`). Keeping it non-generic means it's usable for popovers without typed content (tooltips, notifications). The generic layer is `Popover<TView, TResult>`.

### Q: Why add `Value` to `ICommandContext`?

**A:** The `IValue` doc comments already reference `CommandContext.Value` but it doesn't exist. This implements that intent. Captures value at invocation time, available throughout the propagation chain without weak-ref chasing.

### Q: Why does `Popover<TView, TResult>` handle owner lifecycle instead of a helper?

**A:** Terminal.Gui uses helpers like `OrientationHelper` when multiple unrelated classes need the same behavior. Here, every popover owner needs the same lifecycle. Since `Popover<TView, TResult>` is the common base of all typed popovers, it's the natural place. No separate helper needed — the popover *is* the helper.

### Q: Why must PopoverMenu stay fullscreen?

**A:** `View.AddViewportToClip` clips SubViews to the parent viewport. Cascading submenus are SubViews, so a non-fullscreen `PopoverMenu` would clip cascades. `MakeVisible`/`SetPosition` position `Root`'s X/Y within the fullscreen overlay. Other `Popover<TView, TResult>` subclasses without cascading SubViews can be fixed-size.

### Q: Why can `PopoverEdit` use a simple `Button` instead of `MenuBarItem`?

**A:** The POC in PR #4412 used `MenuBarItem` because it was the only thing that knew how to manage a popover (register, bridge, toggle, position). Now that `Popover<TView, TResult>` handles all of that, a simple `Button` that sets `Popover.IsOpen = !Popover.IsOpen` is sufficient. `MenuBarItem`'s value was its popover infrastructure, which is now generic.

---

## 7. Testing Strategy

All new tests go in `Tests/UnitTestsParallelizable/`.

### `Popover<TView, TResult>` Tests
- Content view add/remove as SubView.
- `ResultExtractor` called on Activate.
- `IValue<TResult>` extraction when `ResultExtractor` is null.
- String fallback to `Text` when `TResult` is `string`.
- `Result` is null on dismiss (Quit/Escape).
- `IsOpen` CWP property: set true → MakeVisible, set false → Visible = false.
- `IsOpen` syncs from Visible changes.
- `Target` focus-loss auto-close.
- Auto-registration on EndInit.
- Anchor priority chain (anchor param → Anchor prop → idealScreenPosition → mouse).
- Anchor prefer-below / flip-above / clamp-X (ports PR #4412 tests).
- `CommandBridge` relays Activate from content to popover.

### `PopoverMenu` Regression Tests
- All existing `PopoverMenu` tests pass unchanged.
- All existing PR #4412 anchor tests pass unchanged.
- `Root` works as alias for `ContentView`.
- `Menu` `IValue<MenuItem>` returns selected item.

### `MenuBarItem` Regression Tests
- All existing `MenuBarItem` tests pass unchanged.
- `PopoverMenuOpen` delegates to `PopoverMenu.IsOpen`.
- Lifecycle (register, bridge, close-on-blur) works via base class.

### `ICommandContext.Value` Tests
- `Value` populated from `IValue` source on command invocation.
- `Value` propagates through `CommandBridge`.
- `Value` is `null` when source doesn't implement `IValue`.
- `WithValue` creates new context preserving other fields.

### `PopoverEdit<TView, TResult>` Tests
- Opens/closes on button click, `F4`, `Alt+Down`.
- `Value` updates on selection.
- `TextField.Text` updates via `DisplayFormatter`.
- `ReadOnly` mode prevents text editing.
- `IValue<TResult>` interface works.
- Anchor positioning (below default, above when no space).
- Auto-registration on `EndInit`.
- Focus returns on close.
- Pre-selection of current value on open.

---

## 8. Breaking Changes

| Change | Impact | Migration |
|---|---|---|
| `PopoverMenu` base: `PopoverBaseImpl` → `Popover<Menu, MenuItem>` | Low | None for most consumers |
| `PopoverMenu.SetPosition` → `internal` | Low (already in PR #4412) | Use `MakeVisible` |
| `MenuBarItem.PopoverMenuOpen` becomes delegate to `PopoverMenu.IsOpen` | Low | Events move to `PopoverMenu.IsOpenChanging`/`IsOpenChanged` |
| `IPopover` gains `Target` property | Low — additive | N/A |
| `Menu` implements `IValue<MenuItem?>` | None — additive | N/A |
| `ICommandContext` gains `Value` | Low — additive | N/A |
| `ComboBox` removed | High | Replace with `PopoverEdit` |

---

## 9. Open Questions

1. **Naming:** Is `PopoverEdit` the right name? Alternatives: `DropDown`, `PopoverCombo`. `PopoverEdit` reflects the architecture.

2. **Non-generic alias TView:** `ListView` (simpler for combo-box) vs `Menu` (richer items)?

3. **Editable mode parsing:** When `ReadOnly = false`, how is typed text converted to `TResult`? `Func<string, TResult?>? ValueParser`?

4. **Filtering/autocomplete:** Native in `PopoverEdit`, or delegate to `TView`?

5. **`Target` vs `Owner`:** `IPopover` already has `Owner` (typed as `IRunnable?`). Should `Target` (`WeakReference<View?>`) replace `Owner`, or coexist? `Owner` is for keyboard event scoping; `Target` is for command bubbling and focus tracking. They serve different purposes, but the naming needs clarity.

6. **`PopoverMenuOpen` events:** Today `MenuBarItem` has `PopoverMenuOpenChanging`/`PopoverMenuOpenChanged` events. After the refactor, these are `PopoverMenu.IsOpenChanging`/`IsOpenChanged`. Should `MenuBarItem` forward them for backward compatibility, or is this an acceptable breaking change for V2 Beta?
