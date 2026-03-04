# Plan: Add `UsePopoverMenu` Behavior Switch to MenuBarItem

## Background

**Issue:** https://github.com/gui-cs/Terminal.Gui/issues/4643

The Terminal.Gui Designer (TGD) needs menus that are non-modal. The previous approach
created a separate `InlineMenuBarItem` class. That approach was rejected because:

- The Designer user is designing apps that use `MenuBarItem` — requiring a different type
  inside the Designer is a leaky abstraction
- `MenuBarItem` and `InlineMenuBarItem` should be a single type with a behavior switch
- Both modes should look and operate identically except for clipping/modality

The `InlineMenuBarItem` PoC (committed on branch `fix/4789-menu-edit`) proved the concept
works and identified all the focus/reentrant bugs. This plan folds that behavior back into
`MenuBarItem` via a property.

## Lessons Learned from InlineMenuBarItem PoC

These bugs were found and fixed. The same fixes must be preserved:

1. **Reentrant toggle-close**: When toggling IsMenuOpen from true→false, `SubMenu.HideMenu()`
   triggers focus changes that cause `OnSelectedMenuItemChanged` to reopen the menu.
   **Fix**: Set `IsSwitchingItem=true` during the close toggle.

2. **Close-other-entries on switch**: `ShowEntry` must close any currently-open entry before
   opening the new one. Popover→popover handles this via `SetFocus()`, but popover→inline
   and inline→popover do not.
   **Fix**: Explicit `GetActiveItem()` check in `ShowEntry`.

3. **Focus must move to SubMenu**: After opening the inline SubMenu, focus must move there
   so Enter/arrow keys reach the MenuItems inside.
   **Fix**: `FocusInlineSubMenuIfNeeded()` called after `ShowEntry` completes.

4. **MenuBar deactivation guard**: When focus moves from MenuBar to inline SubMenu, MenuBar
   must not deactivate.
   **Fix**: `IsInlineSubMenu()` check in `OnHasFocusChanged`.

5. **SubMenu key forwarding**: Left/Right/Escape in the inline SubMenu must forward to the
   parent MenuBar for entry switching and menu closing.
   **Fix**: `KeyDown` event handler on SubMenu.

---

## Design

### New Property: `MenuBarItem.UsePopoverMenu` (Init-Only)

```csharp
/// <summary>
///     Gets whether this entry uses a modal <see cref="PopoverMenu"/> (true, default)
///     or an inline <see cref="MenuItem.SubMenu"/> (false) for its dropdown.
///     This property must be set at construction time and cannot be changed after initialization.
/// </summary>
public bool UsePopoverMenu { get; init; } = true;
```

**Init-only semantics**: `UsePopoverMenu` is set once at construction and cannot be changed
afterward. This avoids stale-state bugs from runtime switching (e.g., orphaned PopoverMenu
registrations, double-parented Menu trees, inconsistent SubMenu wiring). The `init` accessor
enforces this at the language level.

When `true` (default): Existing behavior — PopoverMenu is registered with the Application
popover system, renders above all views, modal input capture.

When `false`: Uses the inherited `MenuItem.SubMenu` — rendered as a sibling view of MenuBar,
non-modal, positioned below the MenuBarItem. The same Menu/MenuItem tree is used; only the
container changes.

### What Stays the Same

- `IMenuBarEntry` interface — still useful for MenuBar's uniform handling
- `MenuBar` generalization — already works with `IMenuBarEntry`, keeps working
- Constructor API — MenuBarItem already accepts MenuItems; SubMenu is already a MenuItem property
- `SubMenuGlyph` virtual property — already on MenuItem

### What Changes

| Component | Change |
|---|---|
| `MenuBarItem` | Add `UsePopoverMenu` property; route `IMenuBarEntry` impl based on it |
| `MenuBarItem` | Remove `SubMenu` override that throws; allow SubMenu when `!UsePopoverMenu` |
| `MenuBarItem.OnActivating` | Add inline toggle-close guard (from InlineMenuBarItem PoC) |
| `MenuBarItem` | Add SubMenu key forwarding (Left/Right/Escape) when `!UsePopoverMenu` |
| `MenuBarItem` | Override `SubMenuGlyph` to return `default` (no glyph) when `!UsePopoverMenu` |
| `MenuBar.EndInit` | Conditionally register popover or subscribe to SubMenu visibility |
| `MenuBar.ShowEntry` | Unify `ShowPopoverItem`/`ShowInlineItem` dispatch using `UsePopoverMenu` |
| `InlineMenuBarItem` | DELETE — folded into MenuBarItem |
| Tests | Adapt InlineMenuBarItem tests to use `MenuBarItem { UsePopoverMenu = false }` |
| MenuBars scenario | Add checkbox that rebuilds MenuBar with popover/inline mode |

### Constructor Behavior

The Menu/MenuItem tree should be the same regardless of `UsePopoverMenu`. Since the property
is init-only, `EndInit` can branch once and set up either the PopoverMenu or SubMenu path
without needing cleanup/switching logic.

**Construction examples:**
```csharp
// Popover mode (default — existing behavior, no breaking change)
MenuBarItem fileMenu = new ("_File", [new MenuItem { Title = "_New" }]);

// Inline mode
MenuBarItem fileMenu = new ("_File", [new MenuItem { Title = "_New" }])
{
    UsePopoverMenu = false
};
```

**Recommended approach**: Always build the Menu from the MenuItem list. In `EndInit`, check
`UsePopoverMenu` once:
- If `true`: wrap the Menu in a PopoverMenu and register it (existing path).
- If `false`: assign the Menu as `SubMenu`, wire up visibility events and key forwarding.

No runtime switching means no need for teardown/rebuild logic in a setter.

---

## Implementation Steps

### Step 1: Allow SubMenu on MenuBarItem

**Modify:** `MenuBarItem.cs`

Currently MenuBarItem overrides `SubMenu` with `new` to throw:
```csharp
public new Menu? SubMenu { get => null; set => throw ... }
```

Remove this override. The inherited `MenuItem.SubMenu` property should be usable when
`UsePopoverMenu = false`.

Add `UsePopoverMenu` property (init-only):
```csharp
public bool UsePopoverMenu { get; init; } = true;
```

Override `SubMenuGlyph` — suppress the glyph entirely in inline/MenuBar mode:
```csharp
protected override Rune SubMenuGlyph => UsePopoverMenu ? Glyphs.RightArrow : default;
```

**Also modify `MenuItem.SubMenu` setter** (line 132 of `MenuItem.cs`):
```csharp
// Before:
KeyView.Text = $"{SubMenuGlyph}";

// After:
Rune glyph = SubMenuGlyph;
KeyView.Text = glyph == default ? string.Empty : $"{glyph}";
```

This is needed because `Shortcut` only hides `KeyView` when `KeyView.Text == string.Empty`
(see `Shortcut.cs` line 286). A `default` Rune would produce `"\0"` which is not empty and
would still render as a visible (but blank) slot. Setting `string.Empty` causes the KeyView
to be removed from the Shortcut layout entirely.

### Step 2: Route IMenuBarEntry Based on UsePopoverMenu

**Modify:** `MenuBarItem.cs`

Update the explicit `IMenuBarEntry` implementation:

```csharp
bool IMenuBarEntry.IsMenuOpen
{
    get => UsePopoverMenu ? PopoverMenuOpen : SubMenu?.Visible ?? false;
    set
    {
        if (UsePopoverMenu)
        {
            PopoverMenuOpen = value;
        }
        else if (SubMenu is { })
        {
            if (value) SubMenu.ShowMenu ();
            else SubMenu.HideMenu ();
        }
    }
}

Menu? IMenuBarEntry.RootMenu => UsePopoverMenu ? PopoverMenu?.Root : SubMenu;
```

Wire up `MenuOpenChanged` to fire from SubMenu visibility changes when in inline mode.

### Step 3: Add Inline Toggle-Close Guard to OnActivating

**Modify:** `MenuBarItem.cs`

Port the reentrant toggle-close guard from InlineMenuBarItem:

```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    if (base.OnActivating (args)) return true;
    if (args.Context?.Routing == CommandRouting.Bridged) return false;

    if (UsePopoverMenu)
    {
        PopoverMenuOpen = !PopoverMenuOpen;
    }
    else
    {
        IMenuBarEntry entry = this;
        if (entry.IsMenuOpen && SuperView is MenuBar menuBar)
        {
            menuBar.IsSwitchingItem = true;
            try { entry.IsMenuOpen = false; }
            finally { menuBar.IsSwitchingItem = false; }
        }
        else
        {
            entry.IsMenuOpen = !entry.IsMenuOpen;
        }
    }

    return false;
}
```

### Step 4: Add SubMenu Key Forwarding

**Modify:** `MenuBarItem.cs`

When `!UsePopoverMenu`, subscribe to `SubMenu.KeyDown` to forward Left/Right/Escape to the
parent MenuBar. Same pattern as InlineMenuBarItem's `OnSubMenuKeyDown`.

Wire this up in `EndInit` (with guard to prevent duplicate subscriptions).
Unsubscribe in `Dispose`.

### Step 5: Update MenuBar for Unified Handling

**Modify:** `MenuBar.cs`

Most of MenuBar is already generalized to `IMenuBarEntry`. The remaining changes:

- `EndInit`: Check `mbi.UsePopoverMenu` to decide whether to register popover or subscribe
  to SubMenu visibility
- `ShowEntry`: Dispatch to `ShowPopoverItem` or `ShowInlineItem` based on `UsePopoverMenu`
  (instead of checking concrete type)
- `ShowInlineItem`: Accept `MenuBarItem` parameter instead of `InlineMenuBarItem`
- Keep `FocusInlineSubMenuIfNeeded`, `IsInlineSubMenu` — update to check
  `MenuBarItem { UsePopoverMenu: false }` instead of `InlineMenuBarItem`

### Step 6: Delete InlineMenuBarItem

**Delete:** `Terminal.Gui/Views/Menu/InlineMenuBarItem.cs`

All its logic is now in `MenuBarItem`. Remove references from:
- `MenuBar.cs` (type checks, `ShowInlineItem` parameter)
- Test files
- Any `using` statements

### Step 7: Update Tests

**Modify:** `InlineMenuBarItemTests.cs` → rename to reflect new pattern

All tests that created `InlineMenuBarItem` now create:
```csharp
MenuBarItem item = new ("_File", [...]) { UsePopoverMenu = false };
```

Add new tests:
- Init-only enforcement: verify `UsePopoverMenu` cannot be changed after construction
- Mixed mode: some entries popover, some inline, in same MenuBar
- Default behavior unchanged (`UsePopoverMenu = true`)

### Step 8: Update MenuBars Scenario

**Modify:** `Examples/UICatalog/Scenarios/MenuBars.cs`

Since `UsePopoverMenu` is init-only, toggling between modes requires rebuilding the MenuBar.
Add a `CheckBox` that rebuilds the MenuBar with the selected mode:

```csharp
CheckBox usePopoverCb = new ()
{
    Title = "Use _Popover Menus",
    Value = CheckState.Checked
};

usePopoverCb.CheckedStateChanging += (_, args) =>
{
    bool usePopover = args.NewValue == CheckState.Checked;
    RebuildMenuBar (usePopover);  // Helper that creates new MenuBarItems with UsePopoverMenu set
};
```

The `RebuildMenuBar` helper recreates the MenuBarItems with `UsePopoverMenu` set in the
object initializer, removes the old MenuBar, and adds the new one.

---

## File Summary

| File | Action |
|---|---|
| `Terminal.Gui/Views/Menu/MenuBarItem.cs` | MODIFY — add `UsePopoverMenu`, route IMenuBarEntry, inline guards |
| `Terminal.Gui/Views/Menu/MenuBar.cs` | MODIFY — dispatch on `UsePopoverMenu` instead of concrete type |
| `Terminal.Gui/Views/Menu/InlineMenuBarItem.cs` | DELETE |
| `Terminal.Gui/Views/Menu/IMenuBarEntry.cs` | KEEP (no changes) |
| `Terminal.Gui/Views/Menu/MenuItem.cs` | MODIFY — SubMenu setter: skip KeyView glyph when SubMenuGlyph is `default` |
| `Tests/.../InlineMenuBarItemTests.cs` | MODIFY — use `MenuBarItem { UsePopoverMenu = false }` |
| `Tests/IntegrationTests/.../MenuBarTests.cs` | MODIFY — update integration tests |
| `Examples/UICatalog/Scenarios/MenuBars.cs` | MODIFY — add toggle |

## Implementation Order

```
1. MenuBarItem.cs (UsePopoverMenu, remove SubMenu override)  ─┐
2. MenuBarItem.cs (route IMenuBarEntry, OnActivating)          │ sequential
3. MenuBarItem.cs (SubMenu key forwarding)                     │
4. MenuBar.cs (dispatch on UsePopoverMenu)                    ─┘
5. Delete InlineMenuBarItem.cs
6. Update tests
7. Update MenuBars scenario
```

## Verification

1. `dotnet build --no-restore` — zero errors
2. `dotnet test Tests/UnitTestsParallelizable --no-build --filter "MenuBar"` — all pass
3. `dotnet test Tests/IntegrationTests --no-build --filter "MenuBar"` — all pass
4. `dotnet test Tests/UnitTestsParallelizable --no-build` — full suite, no regressions
5. Manual: Run UICatalog MenuBars scenario, rebuild with UsePopoverMenu=false, verify identical behavior

## Risks

| Risk | Mitigation |
|---|---|
| `init`-only may surprise users expecting runtime toggle | Document clearly; provide `RebuildMenuBar` pattern in scenario as example |
| PopoverMenu and SubMenu share the same MenuItem tree | Ensure Menu is not double-parented; only one container active |
| Existing MenuBarItem API surface changes | `SubMenu` override removal could break code that catches the throw |
| Focus edge cases in inline mode | All identified in PoC and have proven fixes |
