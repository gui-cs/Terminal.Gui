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

PR #4412 contains foundational changes that this spec builds on. These changes are already implemented and tested:

### 2.1 `PopoverMenu.Anchor` Property

```csharp
/// Gets or sets a delegate that returns the screen-relative rectangle used to
/// anchor the menu when MakeVisible is called without an explicit position.
public Func<Rectangle?>? Anchor { get; set; }
```

**Positioning priority chain** (implemented in `SetPosition`):
1. Explicit `anchor` parameter to `MakeVisible(anchor:)`
2. `Anchor` property delegate
3. Explicit `idealScreenPosition` parameter
4. Last mouse position (fallback)

### 2.2 `PopoverMenu.GetAnchoredPosition()`

Private method that positions a menu relative to an anchor rectangle:
- **Below-anchor preferred** — places menu at `anchor.Bottom`
- **Flips above** when `anchor.Bottom + menuHeight > screenHeight` and space above exists
- **X clamped** to `[0, screenWidth - menuWidth]` to prevent overflow

### 2.3 `MenuBarItem.PopoverMenuAnchor`

```csharp
/// Gets or sets a delegate that returns the screen-relative anchor rectangle
/// used to position the PopoverMenu when this item opens.
public Func<Rectangle?>? PopoverMenuAnchor { get; set; }
```

When `PopoverMenuOpen` is set to `true`, the anchor is resolved:
```csharp
Rectangle anchor = PopoverMenuAnchor?.Invoke () ?? FrameToScreen ();
PopoverMenu.MakeVisible (anchor: anchor);
```

### 2.4 `Shortcut.ElementSpacing`

```csharp
/// Gets or sets the spacing between the CommandView, HelpView, and KeyView. Default is 1.
public int ElementSpacing { get; set; } = 1;
```

Replaces the former hardcoded `GetMarginThickness()`. Used in dropdown scenarios to set `ElementSpacing = 0` for compact toggle buttons.

### 2.5 `PopoverBaseImpl` Documentation Clarification

Fullscreen (`Dim.Fill()`) is now documented as a **default, not a requirement**. Fixed-size popovers (autocomplete, tooltips, dropdowns) are explicitly supported. `PopoverMenu` must stay fullscreen because cascading submenus are SubViews and `AddViewportToClip` would clip them.

### 2.6 `TextField` / `TextView` Anchor Integration

Both now set `ContextMenu.Anchor = () => FrameToScreen ()` so keyboard-activated context menus position correctly relative to the owning view.

### 2.7 `DropDownListExample` Scenario (POC)

Demonstrates the dropdown pattern: `TextField` + `MenuBarItem` with `ElementSpacing = 0`, `PopoverMenuAnchor` pointing at the `TextField`'s frame. This is the throwaway POC that `PopoverEdit` will replace.

---

## 3. Design Overview

### 3.1 Type Hierarchy (New)

```
PopoverBaseImpl (abstract, existing — Dim.Fill() default, not required)
├── Popover<TView, TResult> (NEW — generic popover host)
│   └── owns TView as its content view
│   └── supports IValue<TResult> for result extraction
│   └── inherits Anchor positioning from PopoverMenu refactor
│
├── PopoverMenu : Popover<Menu, MenuItem> (REFACTORED — was concrete, now specialization)
│   └── fullscreen (required for cascading submenu clipping)
│   └── menu-specific: cascading submenus, key binding propagation, arrow navigation
```

### 3.2 Companion View (New)

```
View
└── PopoverEdit<TView, TResult> (NEW — the ComboBox replacement)
    └── contains: TextField (display/edit)
    └── contains: MenuBarItem (toggle glyph, ▼) — reuses existing dropdown infrastructure
    └── owns: Popover<TView, TResult> via MenuBarItem.PopoverMenu or directly
    └── uses: Anchor property to position below/above the TextField
    └── implements: IValue<TResult>
```

### 3.3 Command Infrastructure Enhancement

```
ICommandContext (existing)
└── + object? Value { get; }        // NEW — carries the source view's IValue.GetValue()

CommandContext (existing record struct)
└── + object? Value { get; init; }  // NEW — populated when source implements IValue
```

---

## 4. Detailed Design

### 4.1 `Popover<TView, TResult>` — Generic Popover Host

**File:** `Terminal.Gui/Views/Popover.cs`
**Namespace:** `Terminal.Gui.Views`

```csharp
public class Popover<TView, TResult> : PopoverBaseImpl, IDesignable
    where TView : View, new()
{
    // --- Content ---
    public TView? ContentView { get; set; }             // The hosted view (added as SubView)

    // --- Result Extraction (mirrors Prompt<TView, TResult>) ---
    public Func<TView, TResult?>? ResultExtractor { get; set; }
    public TResult? Result { get; protected set; }

    // --- Anchor-based Positioning (promoted from PopoverMenu) ---
    public Func<Rectangle?>? Anchor { get; set; }
    public void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null);
    internal void SetPosition (Point? idealScreenPosition = null, Rectangle? anchor = null);

    // --- Events ---
    public event EventHandler<ValueChangedEventArgs<TResult?>>? ResultChanged;
}
```

**Key behaviors:**

1. **Content management** — When `ContentView` is set, the previous content is removed and the new view is added as a SubView. A `CommandBridge` is established to relay `Command.Activate` from the content view to the popover.

2. **Result extraction** — When the content view fires `Activate` (user makes a selection), the popover extracts a result using the same priority chain as `Prompt<TView, TResult>`:
   1. Use `ResultExtractor` delegate if provided.
   2. If `ContentView` implements `IValue<TResult>`, use `.Value`.
   3. If `TResult` is `string`, use `ContentView.Text`.
   4. Otherwise, `Result` remains `null`.

3. **Visibility lifecycle** — Same as current `PopoverMenu`: `MakeVisible()` → `Application.Popover.Show()`, hiding restores focus. When the content fires `Activate`, the popover hides itself (the selection is complete).

4. **Anchor-based positioning** — The `Anchor` property and `GetAnchoredPosition()` logic currently in `PopoverMenu` is promoted to the base `Popover<TView, TResult>`. The positioning priority chain (anchor param > Anchor property > idealScreenPosition > mouse position) applies to all popover types. The `GetAnchoredPosition` method (prefer-below, flip-above, clamp-X) becomes the default positioning strategy.

5. **Size flexibility** — `PopoverBaseImpl` defaults to `Dim.Fill()` but this is overridable. For non-cascading popovers (ColorPicker, DatePicker, ListView), the popover can be sized to fit the content view rather than filling the screen. Only `PopoverMenu` requires fullscreen for cascading submenu clipping.

### 4.2 `PopoverMenu` — Refactored as `Popover<Menu, MenuItem>`

**File:** `Terminal.Gui/Views/Menu/PopoverMenu.cs` (same file, refactored)

```csharp
public class PopoverMenu : Popover<Menu, MenuItem>, IDesignable
{
    // Menu-specific: Root is an alias for ContentView
    public Menu? Root { get => ContentView; set => ContentView = value; }

    // Menu-specific properties (unchanged)
    public Key Key { get; set; }
    public static Key DefaultKey { get; set; }
    public MouseFlags MouseFlags { get; set; }

    // Menu-specific behaviors preserved:
    // - Cascading submenu management (ShowMenuItemSubMenu, GetMostVisibleLocationForSubMenu)
    // - Key binding propagation (UpdateKeyBindings)
    // - Left/Right arrow navigation between submenus (MoveLeft, MoveRight)
    // - SelectedMenuItemChanged subscription for submenu positioning
    // - QuitKey handling that propagates Accept
    // - OnKeyDownNotHandled for matching menu item keys
    // - Must remain fullscreen (cascading submenus are SubViews)
}
```

**What moves to the base class:**
- `Anchor` property → `Popover<TView, TResult>.Anchor`
- `MakeVisible(Point?, Rectangle?)` → `Popover<TView, TResult>.MakeVisible()`
- `SetPosition(Point?, Rectangle?)` → `Popover<TView, TResult>.SetPosition()`
- `GetAnchoredPosition(View, Rectangle)` → `Popover<TView, TResult>.GetAnchoredPosition()`
- `ContentView` management (`_rootCommandBridge`, add/remove SubView) → base class
- `OnVisibleChanged` show/hide content → base class (PopoverMenu overrides for `ShowMenu`/`HideMenu`)

**What stays in `PopoverMenu`:**
- `Root` as alias for `ContentView`
- Cascading submenu management (`ShowMenuItemSubMenu`, `GetMostVisibleLocationForSubMenu`)
- `MoveLeft`/`MoveRight` keyboard handlers
- `UpdateKeyBindings`
- `MenuOnSelectedMenuItemChanged`
- `OnActivating`/`OnActivated`/`OnAccepting` overrides for menu-specific command routing
- `OnKeyDownNotHandled` for menu item key matching
- `Key`/`DefaultKey`/`MouseFlags` properties
- `OnSubViewAdded` handling for Menu subview setup

**`Menu` must implement `IValue<MenuItem?>`:**

```csharp
public class Menu : Bar, IDesignable, IValue<MenuItem?>
{
    // Value is the currently selected/activated MenuItem
    public MenuItem? Value { get; set; }
    public event EventHandler<ValueChangingEventArgs<MenuItem?>>? ValueChanging;
    public event EventHandler<ValueChangedEventArgs<MenuItem?>>? ValueChanged;
}
```

This enables `Popover<Menu, MenuItem>` to extract the selected `MenuItem` via `IValue<MenuItem>` without a custom `ResultExtractor`.

### 4.3 `ICommandContext` Enhancement — Value Propagation

**Files:** `Terminal.Gui/Input/ICommandContext.cs`, `Terminal.Gui/Input/CommandContext.cs`

```csharp
public interface ICommandContext
{
    // ... existing members ...

    /// <summary>
    ///     Gets the value from the source view at the time the command was invoked,
    ///     if the source implements <see cref="IValue"/>.
    /// </summary>
    public object? Value { get; }
}

public readonly record struct CommandContext : ICommandContext
{
    // ... existing members ...

    public object? Value { get; init; }

    public CommandContext WithValue (object? value) => this with { Value = value };
}
```

**When Value is populated:**

When a command is invoked and the source `View` implements `IValue`, the `CommandContext.Value` is populated with `IValue.GetValue()`. This happens in the command invocation pipeline (likely in `View.InvokeCommand` or `CommandBridge.Connect`). This allows handlers up the command chain to access the source view's value without needing a direct reference to the source view or knowledge of its generic type.

The `IValue` doc comments already reference `CommandContext.Value` — this implements that stated intent.

**Impact on `Popover<TView, TResult>`:**

When the content view fires `Activate` and the command is bridged to the popover, the `CommandContext.Value` carries the content view's value. The popover's `OnActivated` can use this to set `Result` directly, providing a second (complementary) path to result extraction alongside `ResultExtractor` and `IValue<TResult>`.

### 4.4 `PopoverEdit<TView, TResult>` — The ComboBox Replacement

**File:** `Terminal.Gui/Views/PopoverEdit.cs`
**Namespace:** `Terminal.Gui.Views`

This replaces the hacky POC `DropDownListExample` in PR #4412 with a proper reusable control.

```csharp
public class PopoverEdit<TView, TResult> : View, IValue<TResult?>, IDesignable
    where TView : View, new()
{
    // --- SubViews ---
    private TextField _textField;                      // Display/edit area
    private MenuBarItem _toggleButton;                 // Dropdown toggle (▼ glyph)
                                                       // Uses MenuBarItem for existing popover infra
                                                       // ElementSpacing = 0 for compact display

    // --- Popover ---
    public Popover<TView, TResult> Popover { get; }   // The dropdown popover

    // --- Configuration ---
    public bool ReadOnly { get; set; }                 // If true, TextField is not editable
    public Func<TView, TResult?>? ResultExtractor      // Delegates to Popover.ResultExtractor
    { get; set; }
    public Func<TResult?, string>? DisplayFormatter    // Converts TResult to display text
    { get; set; }

    // --- IValue<TResult?> ---
    public TResult? Value { get; set; }
    public event EventHandler<ValueChangingEventArgs<TResult?>>? ValueChanging;
    public event EventHandler<ValueChangedEventArgs<TResult?>>? ValueChanged;

    // --- Popover Open/Close ---
    public bool IsOpen { get; }
    public void Open ();
    public void Close ();
    public event EventHandler<ValueChangingEventArgs<bool>>? IsOpenChanging;
    public event EventHandler<ValueChangedEventArgs<bool>>? IsOpenChanged;
}
```

**Key behaviors:**

1. **Layout** — The `TextField` fills the width minus the toggle button. The toggle button uses a `MenuBarItem` with `ElementSpacing = 0` and `Glyphs.DownArrow` as title, anchored to the right edge (matching the `DropDownListExample` POC pattern).

2. **Anchor integration** — The popover's `Anchor` is set to `() => _textField.FrameToScreen ()` so the dropdown aligns to the text field's left edge and drops below (or above if no space below), using the `GetAnchoredPosition` logic from PR #4412/#4751.

3. **Toggle** — Clicking the toggle button, pressing `F4`, or pressing `Alt+Down` opens/closes the popover. The `MenuBarItem.PopoverMenuAnchor` delegates to the `Popover.Anchor`.

4. **ReadOnly mode** — When `ReadOnly = true`, the `TextField` is not editable and clicking anywhere on the control opens the popover (like a classic non-editable ComboBox). When `ReadOnly = false`, the `TextField` is editable (like an editable ComboBox).

5. **Value flow** — When the user selects an item in the popover:
   - `Popover.Result` is set via the standard extraction chain.
   - `PopoverEdit.Value` is updated from `Popover.Result`.
   - `TextField.Text` is updated via `DisplayFormatter(Value)` (or `Value?.ToString()` by default).
   - `ValueChanged` is raised.

6. **Pre-selection** — When the popover opens, the current `Value` is used to pre-select the corresponding item in the content view (matching the `PopoverMenuOpenChanged` handler in the POC).

7. **Registration** — The popover is registered with `Application.Popover` during `EndInit()`, following the same pattern as `MenuBarItem.RegisterPopover()`.

8. **CommandBridge** — A bridge relays `Command.Activate` from the popover back to the `PopoverEdit`, enabling command propagation up the view hierarchy.

### 4.5 `MenuBarItem` — Minimal Changes

`MenuBarItem.PopoverMenu` property type stays `PopoverMenu` (which is now `Popover<Menu, MenuItem>` under the hood). Since `PopoverMenu` inherits from the generic base, `MenuBarItem` benefits from the generic infrastructure without API-breaking changes.

The existing `PopoverMenuAnchor` property (from PR #4412) continues to work unchanged.

---

## 5. Non-Generic Convenience Aliases

For the most common use cases, provide non-generic aliases:

```csharp
// Simple dropdown with string values (ListView-based)
public class PopoverEdit : PopoverEdit<ListView, string?>
{
    // Sets up a ListView as the content
    // ResultExtractor defaults to getting the selected item's text
    public IListDataSource? Source { get; set; }  // Delegates to ListView.Source
}
```

This makes the common case simple:

```csharp
// Equivalent to old ComboBox
PopoverEdit dropdown = new ()
{
    Source = new ListWrapper<string> (["Option A", "Option B", "Option C"]),
    ReadOnly = true
};
```

---

## 6. Migration Plan

### Phase 1: Infrastructure (Command Context)
1. Add `Value` property to `ICommandContext` and `CommandContext`.
2. Wire `Value` population into command invocation pipeline.

### Phase 2: Menu IValue
3. Add `IValue<MenuItem?>` to `Menu`.
4. Ensure existing menu tests still pass.

### Phase 3: Generic Popover
5. Extract common popover logic from `PopoverMenu` into `Popover<TView, TResult>`:
   - `Anchor` property
   - `MakeVisible` / `SetPosition` with anchor priority chain
   - `GetAnchoredPosition` (prefer-below, flip-above, clamp-X)
   - Content view management and `CommandBridge`
   - Result extraction chain (ResultExtractor → IValue → Text)
6. Refactor `PopoverMenu` to extend `Popover<Menu, MenuItem>`.
7. `Root` becomes alias for `ContentView`.
8. Merge PR #4412 changes (Anchor, GetAnchoredPosition, ElementSpacing) into the refactored code.
9. Ensure all existing `PopoverMenu` and `MenuBar` tests pass.

### Phase 4: PopoverEdit
10. Implement `PopoverEdit<TView, TResult>`.
11. Implement the non-generic `PopoverEdit` convenience alias.
12. Add unit tests to `UnitTestsParallelizable`.

### Phase 5: ComboBox Replacement
13. Replace all `ComboBox` usages with `PopoverEdit` (or `PopoverEdit<ListView, string?>`).
14. Remove `ComboBox` class.
15. Replace `DropDownListExample` POC scenario with proper `PopoverEdit` scenarios.

### Phase 6: Documentation
16. Update `docfx/docs/` with Popover and PopoverEdit documentation.
17. Add API documentation to all new public APIs.
18. Close #4751 (Anchor positioning fully resolved).

---

## 7. Key Design Decisions

### Q: Why `Popover<TView, TResult>` instead of `Popover<TView>` with separate result handling?

**A:** Following the `Prompt<TView, TResult>` precedent. The two-type-parameter design enables:
- Automatic result extraction via `IValue<TResult>`.
- Type-safe `Result` property without casting.
- `ResultExtractor` delegate for custom extraction.

C# doesn't support partial generic specialization (`TResult Popover<TView>`), so the two-parameter form is necessary.

### Q: Why not make `PopoverBaseImpl` generic directly?

**A:** `PopoverBaseImpl` handles fundamental popover mechanics (screen-filling default, transparency, focus restoration, `Command.Quit`). These are independent of content type. Keeping `PopoverBaseImpl` non-generic means it stays usable for popovers that don't need typed content (e.g., tooltip popovers, notification popovers). The generic layer is `Popover<TView, TResult>`.

### Q: Why add `Value` to `ICommandContext`?

**A:** Today, when a command bubbles up through the hierarchy, handlers must:
1. Get the source view from `ctx.Source` (a `WeakReference<View>`).
2. Check if it implements `IValue`.
3. Cast and extract.

This is boilerplate-heavy and breaks when the source is collected. Adding `Value` to `ICommandContext` captures the value at invocation time, making it available throughout the propagation chain. The `IValue` doc comments already reference `CommandContext.Value` — this implements that intent.

### Q: What about `PopoverMenu`'s menu-specific behaviors?

**A:** All menu-specific behaviors stay in `PopoverMenu`:
- Cascading submenu management (`ShowMenuItemSubMenu`, `GetMostVisibleLocationForSubMenu`)
- Keyboard navigation (`MoveLeft`, `MoveRight`)
- Key binding propagation (`UpdateKeyBindings`)
- `SelectedMenuItemChanged` event handling
- `OnKeyDownNotHandled` for matching menu item keys
- Fullscreen requirement (for cascading submenu clipping)

The base `Popover<TView, TResult>` only handles: content hosting, result extraction, anchor-based positioning, visibility lifecycle, and command bridging. `PopoverMenu` overrides and extends as needed.

### Q: Why does PopoverEdit use a MenuBarItem internally?

**A:** PR #4412's `DropDownListExample` already proved this pattern works: a `MenuBarItem` with `ElementSpacing = 0` and `PopoverMenuAnchor` provides the toggle button, popover registration, anchor positioning, and open/close lifecycle — all for free. `PopoverEdit` formalizes this into a proper control with `IValue<TResult>` support, rather than requiring developers to manually wire up the composite view shown in the POC.

### Q: Why must PopoverMenu stay fullscreen?

**A:** As documented in the PR #4412 changes: `View.AddViewportToClip` clips SubViews to the parent viewport before drawing. Since `PopoverMenu` adds cascading submenus as SubViews, a non-fullscreen `PopoverMenu` would clip any cascade extending beyond its frame. `MakeVisible` and `SetPosition` position `Root`'s `X`/`Y` within the fullscreen overlay, not the overlay itself. Other `Popover<TView, TResult>` subclasses that don't have cascading SubViews may use fixed sizing.

---

## 8. Testing Strategy

All new tests go in `Tests/UnitTestsParallelizable/`.

### `Popover<TView, TResult>` Tests
- Content view is added/removed correctly as SubView.
- `ResultExtractor` is called on `Activate`.
- `IValue<TResult>` extraction works when `ResultExtractor` is null.
- String fallback to `Text` works when `TResult` is `string`.
- `Result` is null when popover is dismissed (Quit/Escape).
- `MakeVisible`/`SetPosition` position correctly with anchor priority chain.
- Anchor prefer-below / flip-above / clamp-X behavior (ports existing PR #4412 tests).
- `CommandBridge` relays `Activate` from content to popover.

### `PopoverMenu` Tests
- All existing `PopoverMenu` tests pass unchanged (regression).
- All existing PR #4412 anchor tests pass unchanged.
- `Root` property works as alias for `ContentView`.
- `Menu` `IValue<MenuItem>` returns selected item.

### `ICommandContext.Value` Tests
- `Value` is populated from `IValue` source on command invocation.
- `Value` propagates through `CommandBridge`.
- `Value` is `null` when source doesn't implement `IValue`.
- `WithValue` creates new context preserving other fields.

### `PopoverEdit<TView, TResult>` Tests
- Opens/closes popover on button click and keyboard shortcuts (`F4`, `Alt+Down`).
- `Value` updates when item is selected in popover.
- `TextField.Text` updates via `DisplayFormatter`.
- `ReadOnly` mode prevents text editing.
- Non-`ReadOnly` mode allows text editing.
- `IValue<TResult>` interface works correctly.
- Popover positions below by default, above when no space below (anchor behavior).
- Registration with `Application.Popover` on `EndInit`.
- Focus returns to `PopoverEdit` when popover closes.
- Pre-selection of current value when popover opens.

---

## 9. Breaking Changes

| Change | Impact | Migration |
|---|---|---|
| `PopoverMenu` base class changes from `PopoverBaseImpl` to `Popover<Menu, MenuItem>` | Low — `Popover<Menu, MenuItem>` extends `PopoverBaseImpl` | None for most consumers. Subclasses of `PopoverMenu` may need minor adjustments. |
| `PopoverMenu.SetPosition` changes from `public` to `internal` | Low — already changed in PR #4412 | Use `MakeVisible` instead |
| `Menu` implements `IValue<MenuItem?>` | None — additive | N/A |
| `ICommandContext` gains `Value` property | Low — interface addition with default `null` | N/A |
| `ComboBox` removed | High — all `ComboBox` usages must migrate | Replace with `PopoverEdit` or `PopoverEdit<ListView, string?>` |

---

## 10. Open Questions

1. **Naming:** Is `PopoverEdit` the right name? Alternatives: `DropDown<TView, TResult>`, `PopoverCombo`, `InlinePopover`. The issue title says "DropDown" but `PopoverEdit` better reflects the architecture.

2. **Non-generic alias:** Should the non-generic `PopoverEdit` use `ListView` or `Menu` as default `TView`? `ListView` is simpler for the common combo-box case; `Menu` provides richer item capabilities (icons, shortcuts, submenus).

3. **Editable mode text-to-value:** When `ReadOnly = false` and the user types text, how is the text converted back to `TResult`? Options: a `Func<string, TResult?>? ValueParser` property, or rely on `IValue<TResult>` if the `TextField` itself provides it.

4. **Filtering/autocomplete:** Should `PopoverEdit` natively support filtering the content view based on typed text? Or should this be left to the `TView` implementation (e.g., a filterable `ListView`)?

5. **PopoverEdit internal toggle:** Should the toggle button be a `MenuBarItem` (reusing existing infrastructure) or a simpler `Button` with direct popover management? The `MenuBarItem` approach reuses proven code but adds conceptual weight; a `Button` is simpler but requires reimplementing popover lifecycle.
