# MenuBar Reengineering Plan

## Status: Phase 1 COMPLETE, Phase 2 COMPLETE, Phase 3 COMPLETE

All source code bugs fixed. Old non-parallelizable test file deleted. ~30 parallelizable tests
passing with 0 failures and 0 skipped.

---

## Context

The PR `copilot/fix-command-propagation-issue-clean` fixed the core command routing infrastructure
(CommandBridge, routing guards, composite selector activation). MenuBar, MenuBarItem, and
PopoverMenu now use the re-engineered command system. All MenuBar/PopoverMenu test failures
from the original PR have been resolved.

See `finalizing-command-system.md` for the definitive command system reference.

---

## Phase 1: Bug Fixes in Source Code — COMPLETE

All five bugs identified in the original plan have been resolved.

### Bug 1: QuitKey doesn't fully deactivate MenuBar — FIXED

**File:** `Terminal.Gui/Views/Menu/MenuBar.cs`

**Problem:** `OnMenuBarItemPopoverMenuOpenChanged` only handled the `true` case. When a PopoverMenu
closed, `MenuBar._isOpen` stayed `true` and `Active` stayed `true`.

**Resolution:** The deactivation is now handled through the `Active` setter and Command.Quit
handling rather than in `OnMenuBarItemPopoverMenuOpenChanged`. The `_popoverBrowsingMode` flag
is intentionally NOT reset on popover close (to support smooth MenuBarItem switching via arrow
keys/hotkeys). It resets when `Active` goes false or `Command.Quit` is handled.

### Bug 2: Invisible MenuBar can be activated — FIXED

**File:** `Terminal.Gui/Views/Menu/MenuBar.cs:OnActivating`

**Fix:** Added `!Visible || !Enabled` guard at top of `OnActivating` that returns `true`
(blocking activation).

### Bug 3: MenuBarItem HotKey should activate MenuBar when not active — FIXED

**File:** `Terminal.Gui/Views/Menu/MenuBar.cs:OnActivating`

**Fix:** `OnActivating` handles `BubblingUp` routing from MenuBarItem: uses
`FindMenuBarItemForSource` to identify the source MenuBarItem and either activates the MenuBar
+ shows the item, or deactivates if the item's PopoverMenu is already closing.

### Bug 4: MenuBarItem HotKey toggle-off when menu is already open — FIXED

**Resolution:** Bugs 1 + 3 together made toggle-off work correctly.
`MenuBarItem.OnActivating` toggles `PopoverMenuOpen`, the change bubbles to MenuBar which
updates `Active` state, and `OnMenuBarItemPopoverMenuOpenChanged` manages browsing mode.

### Bug 5: Switching between MenuBarItems via HotKey when menu is open — FIXED

**File:** `Terminal.Gui/Views/Menu/MenuBar.cs:OnActivating`

**Fix:** In the `BubblingUp` branch, when MenuBar is already active and a different MenuBarItem's
HotKey fires, `ShowItem` is called on the new item. Focus transfer from old to new MenuBarItem
triggers `OnHasFocusChanged` on the old item, which closes its PopoverMenu.

### Additional Fix: MenuBarItem Bridge Guard

**File:** `Terminal.Gui/Views/Menu/MenuBarItem.cs:OnActivating`

**Fix:** Added guard for `CommandRouting.Bridged` commands. When a MenuItem inside the PopoverMenu
is activated, the command bridges back to MenuBarItem. Without the guard, the bridged command
toggled `PopoverMenuOpen=true`, causing `MakeVisible` → `Debug.Assert(App is { })` crash.
Bridged commands are notifications, not toggle requests.

### Additional Fix: PopoverMenuOpen MakeVisible Guard

**File:** `Terminal.Gui/Views/Menu/MenuBarItem.cs`

**Fix:** Added `IsInitialized` guard around `PopoverMenu.MakeVisible` in the `PopoverMenuOpen`
setter. Prevents crash when `App` is not available (design mode, unit tests without
`Application.Init`).

---

## Phase 2: New Parallelizable Tests — COMPLETE

**File:** `Tests/UnitTestsParallelizable/Views/MenuBarTests.cs`

All tests use the modern pattern with `VirtualTimeProvider`, `Application.Create()`, and
explicit `IRunnable`. Approximately 30 test methods covering:

### Tests ported from old UnitTests:

- `Command_HotKey_Activates` — HotKey activates MenuBar
- `DefaultKey_Activates` / `DefaultKey_Deactivates` — Alt key toggle
- `Command_Activate_Activates` / `Command_Activate_Deactivates` — Programmatic activation
- `Command_Activate_Focuses_MenuBarItem_PopoverMenu_And_First_MenuItem` — Full activation chain
- `MenuItem_HotKey_Fires_Action_When_Open` — MenuItem action from hotkey
- `MenuBarItem_HotKey_Activates_And_Opens` — Individual MenuBarItem HotKey
- `MenuBar_EnableForDesign_*` tests — Design mode scenarios

### New gap coverage tests:

- `Command_Right_Navigates_Between_MenuBarItems` — Arrow key navigation
- `MenuBar_Active_False_Sets_CanFocus_False` — Active/CanFocus relationship
- `IsOpen_False_After_All_Popovers_Close` — IsOpen state management
- Various activation/deactivation edge cases

---

## Phase 3: Delete Old Tests — COMPLETE

**File deleted:** `Tests/UnitTests/Views/MenuBarTests.cs`

The old non-parallelizable test file using `[AutoInitShutdown]` and static `Application.RaiseKeyDownEvent`
has been removed. All MenuBar test coverage is now in `Tests/UnitTestsParallelizable/Views/MenuBarTests.cs`.

---

## Final Test Results

| Suite | MenuBar Tests | Status |
|-------|--------------|--------|
| UnitTestsParallelizable | ~30 | All passing, 0 skipped |
| UnitTests | 0 | Old file deleted |

All 14,045+ parallelizable tests and ~1,001 non-parallelizable tests pass with 0 failures.

---

## Remaining Work / Known Limitations

1. **ConsumeDispatch design limitation** — When a MenuItem contains a FlagSelector/OptionSelector
   as its CommandView, `ConsumeDispatch=true` on the selector stops command propagation at the
   selector level. Commands from inner CheckBoxes don't propagate through the full
   MenuBar hierarchy. This is by design (selectors own internal state). One test in
   PopoverMenuTests is skipped documenting this. See `finalizing-command-system.md` →
   Design Invariants #2.

2. **Dynamic MenuItem title/HotKey changes** — Changing a MenuItem's title at runtime should
   update its HotKey binding. This may need additional test coverage.

3. **Mouse interaction tests** — Mouse enter/click scenarios for MenuBar switching could use
   additional parallelizable test coverage.

4. **Menu `ItemSelected` event** — Future refinement to separate "close the menu" concern from
   command routing. Currently `menuItem.Activated → Menu.RaiseAccepted → PopoverMenu closes`.
   Works but conflates two concerns. See `finalizing-command-system.md` → Future Work.
