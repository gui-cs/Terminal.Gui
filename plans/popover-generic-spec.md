# Spec: Refactor `PopoverMenu` into Generic `Popover<TView, TResult>`

> **Issue:** #2404 — Replace ComboBox with DropDown\<TView\>
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

## 2. Design Overview

### 2.1 Type Hierarchy (New)

```
PopoverBaseImpl (abstract, existing)
├── Popover<TView, TResult> (NEW — generic popover host)
│   └── owns TView as its content view
│   └── supports IValue<TResult> for result extraction
│
├── PopoverMenu : Popover<Menu, MenuItem> (REFACTORED — was concrete, now specialization)
│   └── menu-specific: cascading submenus, key binding propagation, right-arrow navigation
```

### 2.2 Companion View (New)

```
View
└── PopoverEdit<TView, TResult> (NEW — the ComboBox replacement)
    └── contains: TextField (display/edit)
    └── contains: Button (dropdown toggle glyph)
    └── owns: Popover<TView, TResult> (non-containment, registered with Application.Popover)
    └── implements: IValue<TResult>
```

### 2.3 Command Infrastructure Enhancement

```
ICommandContext (existing)
└── + object? Value { get; }        // NEW — carries the source view's IValue.GetValue()

CommandContext (existing record struct)
└── + object? Value { get; init; }  // NEW — populated when source implements IValue
```

---

## 3. Detailed Design

### 3.1 `Popover<TView, TResult>` — Generic Popover Host

**File:** `Terminal.Gui/Views/Popover.cs`
**Namespace:** `Terminal.Gui.Views`

```csharp
public class Popover<TView, TResult> : PopoverBaseImpl, IDesignable
    where TView : View, new()
{
    // --- Content ---
    public TView ContentView { get; set; }              // The hosted view (added as SubView)

    // --- Result Extraction (mirrors Prompt<TView, TResult>) ---
    public Func<TView, TResult?>? ResultExtractor { get; set; }
    public TResult? Result { get; protected set; }

    // --- Positioning ---
    public void MakeVisible (Point? idealScreenPosition = null);
    public void SetPosition (Point? idealScreenPosition = null);

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

4. **Positioning** — The `GetLocationEnsuringFullVisibility` helper (inherited from `PopoverBaseImpl`) ensures the content fits on screen. Subclasses like `PopoverMenu` add more specialized positioning for cascading submenus.

### 3.2 `PopoverMenu` — Refactored as `Popover<Menu, MenuItem>`

**File:** `Terminal.Gui/Views/Menu/PopoverMenu.cs` (same file, refactored)

```csharp
public class PopoverMenu : Popover<Menu, MenuItem>, IDesignable
{
    // Menu-specific: Root is an alias for ContentView
    public Menu? Root { get => ContentView; set => ContentView = value; }

    // Menu-specific behaviors preserved:
    // - Cascading submenu management (ShowMenuItemSubMenu, GetMostVisibleLocationForSubMenu)
    // - Key binding propagation (UpdateKeyBindings)
    // - Left/Right arrow navigation between submenus
    // - SelectedMenuItemChanged subscription for submenu positioning
    // - QuitKey handling that propagates Accept
}
```

**Migration notes:**
- `Root` becomes an alias for `Popover<Menu, MenuItem>.ContentView`.
- The `_rootCommandBridge` in current `PopoverMenu` is replaced by the bridge in the base class.
- All cascading-menu-specific code (`MoveLeft`, `MoveRight`, `ShowMenuItemSubMenu`, `MenuOnSelectedMenuItemChanged`, `UpdateKeyBindings`) stays in `PopoverMenu`.
- The `OnActivating` / `OnActivated` / `OnAccepting` overrides that handle menu-specific command routing stay in `PopoverMenu`.
- `ResultExtractor` defaults to extracting the selected `MenuItem` from the `Menu`.

**`Menu` must implement `IValue<MenuItem>`:**

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

### 3.3 `ICommandContext` Enhancement — Value Propagation

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

**Impact on `Popover<TView, TResult>`:**

When the content view fires `Activate` and the command is bridged to the popover, the `CommandContext.Value` carries the content view's value. The popover's `OnActivated` can use this to set `Result` directly, providing a second (complementary) path to result extraction alongside `ResultExtractor` and `IValue<TResult>`.

### 3.4 `PopoverEdit<TView, TResult>` — The ComboBox Replacement

**File:** `Terminal.Gui/Views/PopoverEdit.cs`
**Namespace:** `Terminal.Gui.Views`

```csharp
public class PopoverEdit<TView, TResult> : View, IValue<TResult?>, IDesignable
    where TView : View, new()
{
    // --- SubViews ---
    private TextField _textField;                      // Display/edit area
    private Button _toggleButton;                      // Dropdown toggle (▼ glyph)

    // --- Popover ---
    public Popover<TView, TResult> Popover { get; }   // The dropdown popover

    // --- Configuration ---
    public bool ReadOnly { get; set; }                 // If true, TextField is not editable (pure dropdown)
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

1. **Layout** — The `TextField` fills the width minus the toggle button. The `Button` is anchored to the right edge, showing `Glyphs.DownArrow` (▼) or similar.

2. **Toggle** — Clicking the button or pressing `F4` / `Alt+Down` opens/closes the popover. The popover is positioned below the `PopoverEdit` (or above if insufficient space below).

3. **ReadOnly mode** — When `ReadOnly = true`, the `TextField` is not editable and clicking anywhere on the control opens the popover (like a classic non-editable ComboBox). When `ReadOnly = false`, the `TextField` is editable (like an editable ComboBox).

4. **Value flow** — When the user selects an item in the popover:
   - `Popover.Result` is set via the standard extraction chain.
   - `PopoverEdit.Value` is updated from `Popover.Result`.
   - `TextField.Text` is updated via `DisplayFormatter(Value)` (or `Value?.ToString()` by default).
   - `ValueChanged` is raised.

5. **Registration** — The `Popover<TView, TResult>` is registered with `Application.Popover` during `EndInit()`, following the same pattern as `MenuBarItem.RegisterPopover()`.

6. **CommandBridge** — A bridge relays `Command.Activate` from the popover back to the `PopoverEdit`, enabling command propagation up the view hierarchy.

### 3.5 `MenuBarItem` — Refactored to Use `Popover<Menu, MenuItem>`

**File:** `Terminal.Gui/Views/Menu/MenuBarItem.cs` (same file, minimal changes)

`MenuBarItem.PopoverMenu` property type stays `PopoverMenu` (which is now `Popover<Menu, MenuItem>` under the hood). Since `PopoverMenu` inherits from the generic base, `MenuBarItem` benefits from the generic infrastructure without API-breaking changes.

---

## 4. `ICommandContext.Value` — Detailed Propagation Rules

### 4.1 Where Value Is Set

| Scenario | Where Value Is Populated |
|---|---|
| Direct command invocation (`InvokeCommand`) | If source implements `IValue`, set `Value = source.GetValue()` |
| Command bridging (`CommandBridge`) | Copy `Value` from the bridged context, OR re-extract from remote if remote implements `IValue` |
| Key/Mouse binding dispatch | Set `Value` from the focused view if it implements `IValue` |

### 4.2 How Consumers Use It

```csharp
// In a command handler up the chain:
protected override bool OnActivating (CommandEventArgs args)
{
    if (args.Context?.Value is MenuItem selectedItem)
    {
        // Handle the selected menu item
    }
    return false;
}
```

This is particularly useful for `MenuBarItem` — when a `MenuItem` is activated inside the `PopoverMenu`, the `Value` on the `CommandContext` carries the `MenuItem` reference (via `Menu`'s `IValue<MenuItem>` implementation), allowing `MenuBarItem` to know *which* item was selected without complex source-view inspection.

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

### Phase 1: Infrastructure
1. Add `Value` property to `ICommandContext` and `CommandContext`.
2. Wire `Value` population into command invocation pipeline.
3. Add `IValue<MenuItem?>` to `Menu`.

### Phase 2: Generic Popover
4. Extract common popover logic from `PopoverMenu` into `Popover<TView, TResult>`.
5. Refactor `PopoverMenu` to extend `Popover<Menu, MenuItem>`.
6. Ensure all existing `PopoverMenu` tests pass.

### Phase 3: PopoverEdit
7. Implement `PopoverEdit<TView, TResult>`.
8. Implement the non-generic `PopoverEdit` convenience alias.
9. Add unit tests to `UnitTestsParallelizable`.

### Phase 4: ComboBox Replacement
10. Replace all `ComboBox` usages with `PopoverEdit` (or `PopoverEdit<ListView, string?>`).
11. Remove `ComboBox` class.
12. Update UICatalog scenarios.

### Phase 5: Documentation
13. Update `docfx/docs/` with Popover and PopoverEdit documentation.
14. Add API documentation to all new public APIs.

---

## 7. Key Design Decisions

### Q: Why `Popover<TView, TResult>` instead of `Popover<TView>` with separate result handling?

**A:** Following the `Prompt<TView, TResult>` precedent. The two-type-parameter design enables:
- Automatic result extraction via `IValue<TResult>`.
- Type-safe `Result` property without casting.
- `ResultExtractor` delegate for custom extraction.

C# doesn't support partial generic specialization (`TResult Popover<TView>`), so the two-parameter form is necessary.

### Q: Why not make `PopoverBaseImpl` generic directly?

**A:** `PopoverBaseImpl` handles fundamental popover mechanics (screen-filling, transparency, focus restoration, `Command.Quit`). These are independent of content type. Keeping `PopoverBaseImpl` non-generic means it stays usable for popovers that don't need typed content (e.g., tooltip popovers, notification popovers). The generic layer is `Popover<TView, TResult>`.

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

The base `Popover<TView, TResult>` only handles: content hosting, result extraction, positioning, visibility lifecycle, and command bridging. `PopoverMenu` overrides and extends as needed.

---

## 8. Testing Strategy

All new tests go in `Tests/UnitTestsParallelizable/`.

### `Popover<TView, TResult>` Tests
- Content view is added/removed correctly as SubView.
- `ResultExtractor` is called on `Activate`.
- `IValue<TResult>` extraction works when `ResultExtractor` is null.
- String fallback to `Text` works when `TResult` is `string`.
- `Result` is null when popover is dismissed (Quit/Escape).
- `MakeVisible`/`SetPosition` position correctly.
- `CommandBridge` relays `Activate` from content to popover.

### `PopoverMenu` Tests
- All existing `PopoverMenu` tests pass unchanged.
- `Root` property works as alias for `ContentView`.
- `Menu` `IValue<MenuItem>` returns selected item.

### `ICommandContext.Value` Tests
- `Value` is populated from `IValue` source on command invocation.
- `Value` propagates through `CommandBridge`.
- `Value` is `null` when source doesn't implement `IValue`.
- `WithValue` creates new context preserving other fields.

### `PopoverEdit<TView, TResult>` Tests
- Opens/closes popover on button click and keyboard shortcuts.
- `Value` updates when item is selected in popover.
- `TextField.Text` updates via `DisplayFormatter`.
- `ReadOnly` mode prevents text editing.
- `IValue<TResult>` interface works correctly.
- Popover positions below by default, above when no space below.
- Registration with `Application.Popover` on `EndInit`.
- Focus returns to `PopoverEdit` when popover closes.

---

## 9. Breaking Changes

| Change | Impact | Migration |
|---|---|---|
| `PopoverMenu` base class changes from `PopoverBaseImpl` to `Popover<Menu, MenuItem>` | Low — `Popover<Menu, MenuItem>` extends `PopoverBaseImpl` | None for most consumers. Subclasses of `PopoverMenu` may need minor adjustments. |
| `Menu` implements `IValue<MenuItem?>` | None — additive | N/A |
| `ICommandContext` gains `Value` property | Low — interface addition with default `null` | N/A |
| `ComboBox` removed | High — all `ComboBox` usages must migrate | Replace with `PopoverEdit` or `PopoverEdit<ListView, string?>` |

---

## 10. Open Questions

1. **Naming:** Is `PopoverEdit` the right name? Alternatives: `DropDown<TView, TResult>`, `PopoverCombo`, `InlinePopover`. The issue title says "DropDown" but `PopoverEdit` better reflects the architecture.

2. **Non-generic alias:** Should the non-generic `PopoverEdit` use `ListView` or `Menu` as default `TView`? `ListView` is simpler for the common combo-box case; `Menu` provides richer item capabilities (icons, shortcuts, submenus).

3. **Editable mode text-to-value:** When `ReadOnly = false` and the user types text, how is the text converted back to `TResult`? Options: a `Func<string, TResult?>? ValueParser` property, or rely on `IValue<TResult>` if the `TextField` itself provides it.

4. **Filtering/autocomplete:** Should `PopoverEdit` natively support filtering the content view based on typed text? Or should this be left to the `TView` implementation (e.g., a filterable `ListView`)?
