# Plan: Implement InlineMenuBarItem

## Status

| Step | Status | Notes |
|---|---|---|
| 1. IMenuBarEntry interface | DONE | 3 members: `IsMenuOpen`, `MenuOpenChanged`, `RootMenu` |
| 2. SubMenuGlyph on MenuItem | DONE | Virtual property replaces hardcoded `Glyphs.RightArrow` |
| 3. IMenuBarEntry on MenuBarItem | DONE | Explicit interface impl delegates to PopoverMenu |
| 4. InlineMenuBarItem class | DONE | 3 constructors, SubMenu-based, DownArrow glyph |
| 5. MenuBar generalization | DONE | 12 sites updated, `ShowEntry` with dual-path dispatch |
| 6. Tests | DONE | 21 new tests pass, 63 existing tests pass (4 pre-existing skips) |
| 7. Build verification | DONE | 0 errors across library + all test projects |

### Test Results
- **21 new tests**: All pass (constructors, glyph, IMenuBarEntry contract, activation toggle, MenuBar integration, mixed mode, dispose)
- **63 existing MenuBar tests**: All pass (4 pre-existing skips)
- **0 compilation errors** across Terminal.Gui, UnitTestsParallelizable, UnitTests, UICatalog

### Key Design Decisions Made During Implementation

1. **Constructor signature change**: `MenuBar(IEnumerable<MenuBarItem>)` → `MenuBar(IEnumerable<MenuItem>)` — accepts both types since both are MenuItems
2. **SubMenu visibility wiring**: `InlineMenuBarItem` uses explicit `SubscribeToSubMenuVisibility()`/`UnsubscribeFromSubMenuVisibility()` methods called from `MenuBar.EndInit` (rather than auto-subscribing in the SubMenu setter, to avoid event leaks during construction)
3. **ShowEntry dual-path**: `ShowEntry` dispatches to `ShowPopoverItem` (existing logic) or `ShowInlineItem` (new) based on concrete type. This keeps each path clean without if-chains
4. **SubMenu positioning**: Uses same `FrameToScreen`/`ScreenToViewport` pattern as `Menu.OnSelectedMenuItemChanged`, but positions below (`FrameToScreen().Bottom`) instead of right (`FrameToScreen().Right`)

---

## Context

**Issue:** https://github.com/gui-cs/Terminal.Gui/issues/4643 — The Terminal.Gui Designer (TGD) needs menus that are non-modal. Currently, `MenuBarItem` uses `PopoverMenu`, which is modal (only one at a time, blocks all other mouse/keyboard). The Designer needs menus that can stay open for extended editing, allow simultaneous visibility, and support add/remove/reorder while open.

**Solution:** Create `InlineMenuBarItem` — a sibling of `MenuBarItem` that extends `MenuItem` and uses the inherited `SubMenu` mechanism (inline, non-modal, viewport-constrained) instead of `PopoverMenu`.

**Class hierarchy:**
```
View → Shortcut → MenuItem → MenuBarItem        (uses PopoverMenu — modal)
View → Shortcut → MenuItem → InlineMenuBarItem   (uses SubMenu — inline) ← NEW
View → Bar → Menu → MenuBar                      (contains either type)
```

---

## Implementation Steps

### Step 1: Create `IMenuBarEntry` interface

**New file:** `Terminal.Gui/Views/Menu/IMenuBarEntry.cs`

Defines the shared contract for both `MenuBarItem` and `InlineMenuBarItem`, enabling `MenuBar` to operate on either uniformly:

- `bool IsMenuOpen { get; set; }` — open/close the dropdown
- `event EventHandler<ValueChangedEventArgs<bool>>? MenuOpenChanged` — visibility notification
- `Menu? RootMenu { get; }` — the root Menu for item search/traversal

### Step 2: Add virtual `SubMenuGlyph` to `MenuItem`

**Modify:** `Terminal.Gui/Views/Menu/MenuItem.cs`

`MenuItem.cs` line 125 already has a TODO: *"This is a temporary hack - add a flag or something instead"*. Replace the hardcoded `Glyphs.RightArrow` with a virtual property:

- Add: `protected virtual Rune SubMenuGlyph => Glyphs.RightArrow;`
- Change line 126: `KeyView.Text = $"{SubMenuGlyph}";`

### Step 3: Implement `IMenuBarEntry` on `MenuBarItem`

**Modify:** `Terminal.Gui/Views/Menu/MenuBarItem.cs`

Add `IMenuBarEntry` to the class declaration. Use explicit interface implementation to avoid API collisions:

- `IMenuBarEntry.IsMenuOpen` → delegates to `PopoverMenuOpen`
- `IMenuBarEntry.MenuOpenChanged` → delegates to `PopoverMenuOpenChanged`
- `IMenuBarEntry.RootMenu` → returns `PopoverMenu?.Root`

### Step 4: Create `InlineMenuBarItem`

**New file:** `Terminal.Gui/Views/Menu/InlineMenuBarItem.cs`

Extends `MenuItem`, implements `IMenuBarEntry`, `IDesignable`. Mirrors `MenuBarItem` structure but uses SubMenu:

| Aspect | Implementation |
|---|---|
| Constructors | Mirror MenuBarItem's 4 constructors, pass SubMenu instead of PopoverMenu |
| Arrow glyph | Override `SubMenuGlyph => Glyphs.DownArrow` |
| SubMenu visibility events | Listen to `SubMenu.VisibleChanged`, relay to `MenuOpenChanged` |
| `IsMenuOpen` | Delegates to `SubMenu?.Visible`, calls `ShowMenu()`/`HideMenu()` |
| `RootMenu` | Returns `SubMenu` |
| `OnActivating` | Toggles `IsMenuOpen`; ignores `CommandRouting.Bridged` |
| `SetupCommands` | Custom HotKey handler: skip SetFocus, invoke Activate (same pattern as MenuBarItem) |
| `OnKeyDownNotHandled` | Close own menu on repeat hotkey press (same pattern as MenuBarItem) |
| `EndInit` | Initialize SubMenu if not yet initialized |
| `EnableForDesign` | Create demo SubMenu with sample items |
| `Dispose` | Unsubscribe from `VisibleChanged`; base handles SubMenu disposal |

### Step 5: Update `MenuBar` to support `IMenuBarEntry`

**Modify:** `Terminal.Gui/Views/Menu/MenuBar.cs`

Generalize 12+ sites from `MenuBarItem`-specific to `IMenuBarEntry`-aware:

| Location | Change |
|---|---|
| Constructor (line 57) | Add overload accepting `IEnumerable<MenuItem>` |
| `FindMenuBarItemForSource` (line 440) | Rename to `FindMenuBarEntryForSource`, return `IMenuBarEntry?`, check `is IMenuBarEntry` |
| `OnActivating` BubblingUp (line 176) | Use `entry.IsMenuOpen` instead of `sourceMbi.PopoverMenuOpen` |
| `OnActivating` FallbackToFirst (line 221) | `OfType<IMenuBarEntry>().FirstOrDefault(e => e.RootMenu is { })` |
| `EndInit` (line 305) | Only register popovers for `MenuBarItem` entries (skip `InlineMenuBarItem`) |
| `GetMenuItemsWith` (line 318) | Iterate `OfType<IMenuBarEntry>()`, use `entry.RootMenu` |
| `HideItem` (line 345) | Parameter → `IMenuBarEntry?`, use `IsMenuOpen` |
| `IsOpen` (line 379) | `OfType<IMenuBarEntry>().Any(e => e.IsMenuOpen)` |
| `GetActiveItem` (line 542) | Return `IMenuBarEntry?`, use `IsMenuOpen` |
| `OnSubViewAdded/Removed` (lines 510, 518) | Check `is IMenuBarEntry`, subscribe/unsubscribe `MenuOpenChanged` |
| `OnSelectedMenuItemChanged` (line 499) | Use `is IMenuBarEntry { IsMenuOpen: false }` |
| `OnMenuBarItemPopoverMenuOpenChanged` (line 546) | Rename to `OnEntryMenuOpenChanged`, use `OfType<IMenuBarEntry>()` and `IsMenuOpen` |
| `ShowItem` (line 594) | Rename to `ShowEntry`, branch on concrete type: `MenuBarItem` → existing popover logic; `InlineMenuBarItem` → new `ShowInlineItem` |

**New method — `ShowInlineItem`:**
- Initialize SubMenu if needed
- Set focus to the `InlineMenuBarItem`
- Add SubMenu to MenuBar's SuperView if not already added
- Set `IsMenuOpen = true`
- Position SubMenu below (not right): `X = FrameToScreen().Left`, `Y = FrameToScreen().Bottom`

### Step 6: Add tests

**New file:** `Tests/UnitTestsParallelizable/Views/InlineMenuBarItemTests.cs`

Test categories:
1. Constructor — defaults, with text, with menu items
2. SubMenu glyph — DownArrow displayed (not RightArrow)
3. `IMenuBarEntry` contract — `IsMenuOpen`, `MenuOpenChanged`, `RootMenu`
4. Activation — toggle on/off, bridged commands ignored
5. MenuBar integration — add to MenuBar, show/hide, `IsOpen()`, arrow key navigation
6. Mixed mode — `MenuBarItem` + `InlineMenuBarItem` in same MenuBar
7. Positioning — SubMenu appears below, not to the right
8. `EnableForDesign` — creates valid demo SubMenu
9. Dispose — cleanup, no leaks

**Modify:** `Tests/UnitTestsParallelizable/Views/MenuBarTests.cs` — update any tests broken by API signature changes (e.g., `HideItem` parameter type)

---

## File Summary

| File | Action |
|---|---|
| `Terminal.Gui/Views/Menu/IMenuBarEntry.cs` | NEW |
| `Terminal.Gui/Views/Menu/InlineMenuBarItem.cs` | NEW |
| `Terminal.Gui/Views/Menu/MenuItem.cs` | MODIFY — add `SubMenuGlyph` virtual property |
| `Terminal.Gui/Views/Menu/MenuBarItem.cs` | MODIFY — add `IMenuBarEntry` implementation |
| `Terminal.Gui/Views/Menu/MenuBar.cs` | MODIFY — generalize to `IMenuBarEntry` (~12 sites) |
| `Tests/UnitTestsParallelizable/Views/InlineMenuBarItemTests.cs` | NEW |
| `Tests/UnitTestsParallelizable/Views/MenuBarTests.cs` | MODIFY — update for API changes |

## Implementation Order

```
1. IMenuBarEntry.cs          ─┐
2. MenuItem.cs (SubMenuGlyph) ─┼─ independent, can be parallel
                               │
3. MenuBarItem.cs (IMenuBarEntry) ─── depends on 1
4. InlineMenuBarItem.cs       ─────── depends on 1, 2
5. MenuBar.cs                 ─────── depends on 1, 3, 4
6. Tests                      ─────── depends on all above
```

## Verification

1. `dotnet build --no-restore` — ensure no compilation errors
2. `dotnet test Tests/UnitTestsParallelizable --no-build --filter "MenuBar"` — existing tests pass
3. `dotnet test Tests/UnitTestsParallelizable --no-build --filter "InlineMenuBarItem"` — new tests pass
4. `dotnet test Tests/UnitTests --no-build --filter "MenuBar"` — non-parallel tests pass
5. Manual: Run UICatalog Menus scenario to verify no regression in normal MenuBar behavior

## Risks

| Risk | Mitigation |
|---|---|
| MenuBar regression (12+ sites changed) | Each change is mechanical `MenuBarItem` → `IMenuBarEntry`. Existing tests catch regressions. |
| SubMenu positioning in constrained viewports | Uses same `FrameToScreen`/`ScreenToViewport` pattern as existing SubMenu positioning in `Menu.cs` |
| Focus conflicts (non-modal SubMenus) | `_isSwitchingItem` guard and browsing-mode logic extend naturally |
| Multiple SubMenus visible simultaneously | Desired for Designer; MenuBar's `ShowEntry` hides previous entry's menu during browsing |
