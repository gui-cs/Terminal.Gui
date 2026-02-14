# Menus Refactor Plan

## Class Hierarchy

```
View
  └── Bar : View, IOrientation, IDesignable     (layout container for Shortcuts)
       └── Menu : Bar                            (vertical Bar of MenuItems)
            └── MenuBar : Menu                   (horizontal menu bar)

View
  └── Shortcut : View, IOrientation, IDesignable (composite: CommandView + HelpView + KeyView)
       └── MenuItem : Shortcut                   (adds SubMenu)
            └── MenuBarItem : MenuItem            (uses PopoverMenu instead of SubMenu)

PopoverBaseImpl
  └── PopoverMenu                                (popover container managing Menu hierarchy)
```

## Completed Work (Phases 1-5)

| Phase | What | Status |
|-------|------|--------|
| 1. Fix MenuItem.OnAccepted | Uncommented TargetView.InvokeCommand; deleted dead code | **DONE** |
| 2. Fix Menu.cs | Replaced AddCommand with Accepting event (collision fix); removed dead OnAccepting; simplified SelectedMenuItem | **DONE** |
| 3. Bar test bolstering | Added 5 new tests (mouse wheel, vertical layout, no bubbling, insert, remove) | **DONE** |
| 4. Migrate Command to Shortcut | Moved TargetView + Command from MenuItem to Shortcut; moved OnAccepted logic | **DONE** |
| 5. Update command.md | Added Shortcut/MenuItem/Menu/Bar rows to command table | **DONE** |
| — Shortcut Command tests | Added 9 tests for TargetView/Command on Shortcut | **DONE** |
| 6. Fix MenuBar / MenuBarItem / PopoverMenu | Fixed all 5 remaining failures: command bubbling, HotKey switching, menu closing | **DONE** |

### Current Test Counts

| File | Tests |
|------|-------|
| ShortcutTests (all partials) | 124 |
| MenuItemTests | 11 |
| MenuTests | 9 |
| BarTests | 16 |
| **MenuBarTests** | **18 pass, 0 fail** (2 skipped by design) |
| Full parallel suite | 13,928 pass, 0 fail |
| Full non-parallel suite | 1,002 pass, 0 fail |

---

## Next: MenuBar, MenuBarItem, PopoverMenu

### MenuBarTests — ALL FIXED

All 18 tests pass (was 9 pass, 11 fail). 2 tests skipped by design.

**Root causes found and fixed:**

1. **Activate/Accept command consumed by Menu** — Menu's DefaultActivateHandler/DefaultAcceptHandler returned true when commands bubbled up from MenuItems, preventing the originating MenuItem from calling RaiseActivated/RaiseAccepted → Action?.Invoke(). **Fix:** Custom Activate/Accept handlers in Menu that run the default handler (so events fire) but return false for IsBubblingUp.

2. **Menu not closing after HotKey activation** — The HotKey → Activate flow invoked Action but didn't trigger the Accept chain (needed for PopoverMenu to close). **Fix:** Subscribe to `menuItem.Activated` in Menu.OnSubViewAdded → `RaiseAccepted` to trigger PopoverMenu.MenuAccepted → close.

3. **MenuBarItem HotKey switch race condition** — DefaultHotKeyHandler called SetFocus before InvokeCommand(Activate), causing OnSelectedMenuItemChanged to open the popover, then OnActivating to toggle it closed. **Fix:** MenuBarItem overrides Command.HotKey to skip SetFocus (ShowItem handles focus).

4. **MenuBar command handlers** — MenuBar overrides Menu's Activate/Accept handlers with DefaultActivateHandler/DefaultAcceptHandler to preserve popover toggle behavior in OnActivating/OnAccepting.

### Key Source Files

| File | Lines | Role |
|------|-------|------|
| `Terminal.Gui/Views/Menu/MenuBar.cs` | ~600 | Horizontal menu bar; manages MenuBarItems and active/inactive states |
| `Terminal.Gui/Views/Menu/MenuBarItem.cs` | ~220 | MenuItem subclass; uses PopoverMenu instead of SubMenu |
| `Terminal.Gui/Views/Menu/PopoverMenu.cs` | ~700 | Popover container; manages Menu hierarchy, submenu show/hide, positioning |

### Key Known Issues

1. **BUGBUG in Menus.cs scenario (lines 189-191):** App-level key bindings must be set up _before_ creating the MenuBar, due to PopoverMenu not updating key bindings after construction.

2. **PopoverMenu has multiple Layout BUGBUGs** — Manual `Layout()` calls as workarounds for issue #4522.

3. **MenuItem.OnAccepted commented-out code** was restored in Phase 1 — this may have changed the flow for MenuBar scenarios that previously worked around the missing invocation.

4. **MenuBarItem.SubMenu throws** (`InvalidOperationException`) — by design, MenuBarItem uses PopoverMenu, not SubMenu.

---

## Phase 6: Fix MenuBar / MenuBarItem / PopoverMenu

### 6a: Study and Diagnose

Read and understand the full command flow for a typical MenuBar interaction:

1. User presses HotKey (e.g., Alt+F for "File") → How does it reach MenuBar?
2. MenuBar activates → How does it show the PopoverMenu?
3. PopoverMenu shows Menu with MenuItems → How is focus managed?
4. User presses Enter on a MenuItem → How does Accept propagate back?
5. PopoverMenu hides → How does MenuBar deactivate?

Trace through the 11 failing tests to identify the exact breakpoint in each flow.

### 6b: Fix HotKey Activation Flow

The most common failure: HotKey doesn't make PopoverMenu visible. Investigate:

- `MenuBar.OnActivating` / `OnAccepting` — How do they handle HotKey commands?
- `MenuBarItem` HotKey bindings — Are they correctly registered?
- `PopoverMenu.Show()` / `AddAndShowSubMenu()` — Is the show path reachable?
- `MenuBar.ShowItem()` — Does it correctly toggle PopoverMenu visibility?

### 6c: Fix Mouse Click Activation

Test 7 (`Mouse_Click_Activates_And_Opens`) — Mouse click should activate and open. Check:

- Mouse binding on MenuBarItem (LeftButtonReleased → Activate?)
- Does activation trigger PopoverMenu show?

### 6d: Fix MenuItem Click Action Invocation

**Observed:** Clicking on a MenuItem in an open PopoverMenu does nothing — the action is not invoked and the menu does not close.

Tests 3, 8, 11 — MenuItem actions not firing. Check:

- Does MenuItem.OnAccepted correctly invoke Action?
- Does the Accept command propagate from PopoverMenu → Menu → MenuItem → Action?
- Same source-identity issue as mouse click fix? (source is CommandView, not MenuItem — `OnAccepting` may need `FindMenuBarItemForSource`-style fix)
- Does PopoverMenu hide after a MenuItem action fires?
- Has the Phase 1 OnAccepted fix changed the invocation order?
- Write parallelizable test: click on MenuItem in open PopoverMenu → verify action invoked and menu closed

### 6e: Fix Dynamic MenuItem Updates

Test 8 (`Dynamic_Change_MenuItem_Title`) and Test 10 (`Update_MenuBarItem_HotKey_Works`):

- PopoverMenu doesn't update key bindings after construction (known BUGBUG)
- Need to implement dynamic binding updates or rebuild on change

### 6f: Port MenuBarTests to Parallelizable

All existing MenuBar tests live in `Tests/UnitTests/Views/MenuBarTests.cs` (non-parallel). Port them:

1. Create `Tests/UnitTestsParallelizable/Views/MenuBarTests.cs`
2. Port each test, removing `Application.Init`/`Shutdown` static dependencies
3. Use `VirtualTimeProvider` + `Application.Create` pattern (same as other parallelizable tests)
4. Fix all 11 currently-failing tests as part of the port
5. Delete the old non-parallel test file once all tests pass in parallel
6. Add new tests for:
   - PopoverMenu show/hide lifecycle
   - MenuBar active/inactive state transitions
   - MenuBarItem HotKey registration and dynamic updates

---

## Phase 7: Update UICatalog Scenarios

### 7a: Shortcuts.cs Scenario

**Goal:** Modernize to demonstrate `Shortcut.Command` and `Shortcut.TargetView`.

**Current state:** All Shortcut examples use `Action` lambdas and event handlers. None demonstrate the new `Command`/`TargetView` pattern.

**Changes:**

1. Add a new section demonstrating `Shortcut.Command` + `Shortcut.TargetView`:
   - Create a target View with a command bound to it (e.g., Command.Save)
   - Create a Shortcut with `TargetView = targetView, Command = Command.Save`
   - Show that Accept on the Shortcut invokes the command on the target
2. Add an example showing `Shortcut.Command` with app-level key binding (no TargetView)
3. Verify all existing examples still work correctly after the refactor
4. Clean up any commented-out code

### 7b: Bars.cs Scenario

**Goal:** Ensure it works and demonstrates modern patterns.

**Current state:** Large chunks of commented-out code (lines 82-90, 276-414). StatusBar-like and MenuBar-like examples use `Action` lambdas only.

**Changes:**

1. Remove commented-out dead code (~140 lines)
2. Add a Bar example using `Shortcut.Command` + `TargetView`
3. Verify all existing Bar configurations (horizontal, vertical, popover) still work
4. Ensure focus navigation (Tab, arrow keys, mouse wheel) works correctly within Bars

### 7c: Menus.cs Scenario

**Goal:** Verify and fix focus behavior; demonstrate `MenuItem.Command`/`TargetView` (inherited from Shortcut).

**Current state:** Uses 3 MenuItem patterns (no-Command, View-level Command, App-level Command). Has BUGBUG about key binding ordering.

**Changes:**

1. Verify the 3 MenuItem binding patterns still work after Phase 4 migration
2. Fix or document the key binding ordering BUGBUG (lines 189-191) — this may be resolved by Phase 6
3. Verify focus appearance: Does the focused MenuItem look correct? Do focus colors highlight properly?
4. Verify submenu focus transitions: Opening/closing submenus should transfer focus correctly

### 7d: Menu Focus Verification

**Goal:** Ensure focus visuals and behavior are correct across all Menu scenarios.

**Checklist:**

- [ ] MenuBar item gets focus highlight when activated (F9 or click)
- [ ] PopoverMenu items show focus highlight when navigated (arrow keys)
- [ ] SubMenu items show focus highlight
- [ ] Focus returns to the previously focused view when menu is dismissed
- [ ] `ForceFocusColors` on MenuItems with open submenus displays correctly
- [ ] Mouse hover over MenuItems correctly moves focus (OnMouseEnter)
- [ ] Keyboard navigation (Up/Down/Left/Right) works in all menu contexts

---

## Phase 8: Fix Integration Tests

### 8a: MenuBar / PopoverMenu Integration Tests

Integration tests in `Tests/IntegrationTests/` that exercise MenuBar and PopoverMenu are failing. These should be fixed after Phase 6 (since the unit-level fixes will likely resolve most issues).

- Run `dotnet test Tests/IntegrationTests --no-build --filter "FullyQualifiedName~MenuBar|FullyQualifiedName~PopoverMenu"` to identify failures
- Fix any remaining issues not resolved by Phase 6

### 8b: FileDialog Integration Tests

FileDialog tests are also failing. These may be fixed as a side-effect of Menu fixes (FileDialog uses menus internally) or may be independent issues.

- Run `dotnet test Tests/IntegrationTests --no-build --filter "FullyQualifiedName~FileDialog"` to identify failures
- Determine if failures are menu-related or independent
- Fix as needed

---

## Execution Order

1. ~~Phase 1-5~~ **DONE** — Bar, MenuItem, Menu fixes + Shortcut.Command migration + docs
2. ~~Phase 6~~ **DONE** — MenuBar/MenuBarItem/PopoverMenu: All tests pass
3. **Phase 7** (Scenarios) — Update UICatalog scenarios; verify focus behavior
4. **Phase 8** (Integration Tests) — Fix MenuBar/PopoverMenu and FileDialog integration test failures (last step)

## Verification

After each phase:
1. `dotnet build --no-restore` — Zero new errors
2. `dotnet test Tests/UnitTestsParallelizable --no-build` — Full parallel suite, no regressions
3. `dotnet test Tests/UnitTests --no-build --filter "FullyQualifiedName~MenuBar"` — Track/fix MenuBar failures
4. `dotnet test Tests/IntegrationTests --no-build` — Track integration test failures
5. Manual verification of UICatalog scenarios (Shortcuts, Bars, Menus, ContextMenus)
