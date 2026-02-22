# MenuBar Reengineering Plan

## Context

The PR `copilot/fix-command-propagation-issue-clean` has fixed the core command routing infrastructure (CommandBridge, routing guards, composite selector activation). MenuBar, MenuBarItem, and PopoverMenu now have the infrastructure they need, but several bugs remain that prevent the old MenuBarTests from passing. The old tests (in `Tests/UnitTests/Views/MenuBarTests.cs`) use `[AutoInitShutdown]` and the static `Application.RaiseKeyDownEvent` API — a legacy pattern being phased out. The modern parallelizable tests and all 63 integration tests already pass.

**Goal:** Fix the remaining bugs in MenuBar/MenuBarItem/PopoverMenu, then replace the 18 old non-parallelizable tests with granular parallelizable tests, adding coverage for gaps.

---

## Phase 1: Bug Fixes in Source Code

### Bug 1: QuitKey doesn't fully deactivate MenuBar

**File:** `Terminal.Gui/Views/Menu/MenuBar.cs` lines 501-512

**Problem:** `OnMenuBarItemPopoverMenuOpenChanged` only handles the `true` case. When a PopoverMenu closes (via QuitKey, Escape, focus loss), `MenuBar._isOpen` stays `true` and `Active` stays `true`.

**Flow:** QuitKey → PopoverMenu.Quit handler closes PopoverMenu (`Visible=false`) → `MenuBarItem.PopoverMenuOpen=false` → fires `PopoverMenuOpenChanged` → MenuBar's handler ignores the `false` case.

**Fix:**
```csharp
private void OnMenuBarItemPopoverMenuOpenChanged (object? sender, ValueChangedEventArgs<bool> e)
{
    if (sender is not MenuBarItem mbi)
    {
        return;
    }

    if (e.NewValue)
    {
        _isOpen = true;
    }
    else
    {
        // When a PopoverMenu closes, check if any others are still open.
        // If not, deactivate the MenuBar entirely.
        if (!SubViews.OfType<MenuBarItem> ().Any (m => m.PopoverMenuOpen))
        {
            _isOpen = false;
            Active = false;
        }
    }
}
```

### Bug 2: Invisible MenuBar can be activated

**File:** `Terminal.Gui/Views/Menu/MenuBar.cs` lines 109-137

**Problem:** `OnActivating` doesn't check `Visible` before setting `Active=true`. When `menuBar.Visible=false`, pressing the HotKey still activates it.

**Fix:** Add visibility guard at the top of `OnActivating`:
```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    // Don't activate if not visible or not enabled
    if (!Visible || !Enabled)
    {
        return false;
    }

    // ... rest of existing logic
}
```

### Bug 3: MenuBarItem HotKey should activate MenuBar when not active

**Analysis:** When MenuBar is NOT active and user presses a MenuBarItem's HotKey (e.g., Alt+F for "_File"), the flow is:
1. HotKey reaches MenuBarItem's custom HotKey handler (`SetupCommands`)
2. Handler calls `InvokeCommand(Command.Activate)` on MenuBarItem
3. MenuBarItem.OnActivating sets `PopoverMenuOpen = true`
4. This bubbles to MenuBar.OnActivating, but with `BubblingUp` routing — which is ignored!

The bubbling guard in `MenuBar.OnActivating` correctly prevents re-dispatch when a SubView activation bubbles up. But when the activation comes from a HotKey on a specific MenuBarItem, the MenuBar needs to know to activate itself AND show that specific item.

**Fix approach:** The `MenuBarItem.OnActivating` returns `false` (allowing bubbling). When MenuBar sees the bubbling activation and is not yet active, it should activate itself and show the source MenuBarItem. Update `MenuBar.OnActivating`:

```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    if (!Visible || !Enabled)
    {
        return false;
    }

    // When a SubView's (MenuBarItem's) activation bubbles up:
    if (args.Context?.Routing == CommandRouting.BubblingUp)
    {
        // If not active, activate and show the source MenuBarItem
        if (!Active && args.Context?.Source?.TryGetTarget (out View? source) == true
            && source is MenuBarItem mbi)
        {
            Active = true;
            ShowItem (mbi);

            return true;
        }

        return false;
    }

    if (Active)
    {
        Active = false;

        return true;
    }

    if (SubViews.OfType<MenuBarItem> ().FirstOrDefault (mbi => mbi.PopoverMenu is { }) is { } first)
    {
        Active = true;
        ShowItem (first);

        return true;
    }

    return false;
}
```

This way, when Alt+F triggers MenuBarItem("_File").HotKey → Activate bubbles up → MenuBar sees it's not active → activates and shows that specific MenuBarItem.

### Bug 4: MenuBarItem HotKey toggle-off when menu is already open

**File:** `Terminal.Gui/Views/Menu/MenuBarItem.cs` lines 274-291

**Current:** `OnKeyDownNotHandled` checks if the PopoverMenu is visible and the key matches a HotKeyBinding, then calls `menuBar.HideActiveItem()`.

**Problem:** This approach is fragile. The HotKey handler in `SetupCommands` calls `InvokeCommand(Command.Activate)`, which toggles `PopoverMenuOpen`. If `PopoverMenuOpen` is already true, `OnActivating` sets it to false. Then the activation bubbles to MenuBar, which sees `BubblingUp` and now (with Bug 3 fix) would try to activate if not already active. But at this point Active was already true, so this should work correctly since the MenuBarItem already toggled PopoverMenuOpen to false.

**Verification needed:** Test whether toggle-off works with Bug 3 fix. If the MenuBarItem.OnActivating toggles `PopoverMenuOpen=false` and returns false, the bubble to MenuBar with `BubblingUp` routing should be handled by the `!Active` check — but Active IS true at that point, so MenuBar won't re-activate, and the `OnMenuBarItemPopoverMenuOpenChanged` handler (with Bug 1 fix) will deactivate MenuBar.

**Likely no additional fix needed** — Bugs 1 and 3 together should make toggle-off work. But the `OnKeyDownNotHandled` approach might cause double-toggle. Need to verify and potentially simplify.

### Bug 5: Switching between MenuBarItems via HotKey when menu is open

**Scenario:** File menu open → press Alt+E → Edit menu should open, File should close.

**Flow with fixes:** Alt+E → MenuBarItem("_Edit").HotKey handler → `InvokeCommand(Activate)` → `OnActivating` toggles `PopoverMenuOpen=true` → bubbles to MenuBar.OnActivating with `BubblingUp` → MenuBar is already Active → should show the Edit item.

**Fix needed:** In the `BubblingUp` branch of `MenuBar.OnActivating`, if already active, we need to show the new item:

```csharp
if (args.Context?.Routing == CommandRouting.BubblingUp)
{
    if (args.Context?.Source?.TryGetTarget (out View? source) == true
        && source is MenuBarItem mbi)
    {
        if (!Active)
        {
            Active = true;
        }
        ShowItem (mbi);

        return true;
    }

    return false;
}
```

Wait — but `ShowItem` already handles hiding the old popover via `OnSelectedMenuItemChanged`. Actually, ShowItem doesn't hide the old one. The `OnSelectedMenuItemChanged` only fires when focus changes. And when the HotKey handler skips `SetFocus` to avoid double-toggle...

Need to ensure that when switching via HotKey, the old item's popover closes. This should happen naturally: MenuBarItem("_Edit").OnActivating sets PopoverMenuOpen=true. But the old item ("_File") still has PopoverMenuOpen=true. We need ShowItem to hide the currently active item first.

Looking at `ShowItem`:
```csharp
private void ShowItem (MenuBarItem? menuBarItem)
{
    if (!Active || !Visible) return;
    // ... init logic
    Active = true;
    menuBarItem.SetFocus ();
    // ...
    menuBarItem.PopoverMenuOpen = true;
}
```

It calls `SetFocus()` on the new item. When focus moves from File to Edit, `MenuBarItem.OnHasFocusChanged` fires on File with `newHasFocus=false`, which sets `PopoverMenuOpen=false`. So the old popover should close.

But the HotKey handler in `SetupCommands` calls `InvokeCommand(Command.Activate)` BEFORE focus changes. So PopoverMenuOpen gets set to true on Edit, then the bubbling to MenuBar calls ShowItem which calls SetFocus. At that point both Edit and File might have PopoverMenuOpen=true momentarily.

Actually, let me reconsider. The HotKey handler calls `InvokeCommand(Command.Activate)` which triggers `MenuBarItem.OnActivating`. This sets `PopoverMenuOpen = true`. Then it returns false, allowing bubbling. MenuBar.OnActivating sees BubblingUp + source is MenuBarItem Edit. It calls ShowItem(Edit) which calls Edit.SetFocus(). This causes File's OnHasFocusChanged to fire, setting File.PopoverMenuOpen = false. Then ShowItem sets Edit.PopoverMenuOpen = true again (it was already true).

There's a potential issue: the HotKey handler already set PopoverMenuOpen=true AND called InvokeCommand. Then ShowItem also sets PopoverMenuOpen=true. But since PopoverMenuOpen has a guard `if (field == value) return`, the second set is a no-op. This should work.

**Potential issue:** The HotKey handler in `SetupCommands` calls `RaiseHotKeyCommand(ctx)` then `InvokeCommand(Command.Activate)`. When MenuBar is already active and another item is open, `RaiseHotKeyCommand` might set focus or do something unwanted. Need to verify.

Actually, `RaiseHotKeyCommand` is a method on View that raises the HotKeyCommand event. For MenuBarItem, this is the standard notification. It shouldn't change focus.

### Summary of Source Code Changes

| File | Change | Bug |
|------|--------|-----|
| MenuBar.cs:OnMenuBarItemPopoverMenuOpenChanged | Handle false case — deactivate when all closed | 1 |
| MenuBar.cs:OnActivating | Add Visible/Enabled guard | 2 |
| MenuBar.cs:OnActivating | Handle BubblingUp from MenuBarItem — activate + show source | 3, 5 |

---

## Phase 2: New Parallelizable Tests

**File:** `Tests/UnitTestsParallelizable/Views/MenuBarTests.cs`

All new tests use the modern pattern:
```csharp
VirtualTimeProvider time = new ();
using IApplication app = Application.Create (time);
app.Init (DriverRegistry.Names.ANSI);
IRunnable runnable = new Runnable ();
// ... setup with explicit types (no var) ...
app.Begin (runnable);
```

### Tests ported from old UnitTests (scenarios the old tests intended to validate):

1. **QuitKey_Deactivates** — Open via DefaultKey, press QuitKey, verify Active=false, IsOpen=false, PopoverMenu not visible, CanFocus=false, HasFocus=false

2. **MenuBarItem_HotKey_Activates_And_Opens** — Build MenuBar with MenuBarItem("_New", popover), press Alt+N, verify Active=true, IsOpen=true, PopoverMenu.Visible=true

3. **MenuBarItem_HotKey_Deactivates** — Open via Alt+N, press Alt+N again, verify deactivated

4. **MenuItem_HotKey_Fires_Action_When_Open** — Open menu, press MenuItem's hotkey letter, verify Action executed

5. **WhenOpen_Switch_MenuBarItem_Via_HotKey** — Open File (Alt+N), press Alt+E, verify File closed, Edit opened

6. **Update_MenuBarItem_HotKey_Works** — Open via Alt+N, change HotKey to Alt+E, old key no-ops, new key toggles

7. **Mouse_Enter_Activates_But_Does_Not_Open** — Mouse position report over MenuBar, verify Active=true but IsOpen=false

8. **Disabled_MenuBar_Is_Not_Activated** — menuBar.Enabled=false, press HotKey, verify not activated

9. **Disabled_MenuBarItem_HotKey_Does_Not_Open** — menuBarItem.Enabled=false, press HotKey, verify not activated

10. **Disabled_PopoverMenu_MenuBar_Still_Activates** — popoverMenu.Enabled=false, press HotKey, verify MenuBar activates but popover still shows (Enabled doesn't prevent Visible)

11. **Invisible_MenuBar_HotKey_Does_Not_Activate** — menuBar.Visible=false, press HotKey, verify not activated

12. **Invisible_MenuBar_MenuItem_Key_Still_Works** — menuBar.Visible=false, MenuItem.Key=F1 fires Action

13. **HotKey_Fires_VisibleChanged_Only_Once** — Subscribe to VisibleChanged, open via HotKey, verify count=1

14. **Dynamic_MenuItem_Title_Change_Updates_HotKey** (was SKIPPED) — Change MenuItem title, verify old hotkey gone, new works

### New tests (missing coverage):

15. **Arrow_Right_Navigates_Between_MenuBarItems** — Open File, press Right, verify Edit opens, File closes

16. **Arrow_Left_Wraps_To_Last_MenuBarItem** — Focus on first item, press Left, verify last item focused

17. **Escape_Closes_Submenu_Then_Root** — Open menu with submenu visible, Escape closes submenu first, second Escape closes root

18. **Focus_Restoration_After_Close** — Focus on a hostView, open menu, close, verify focus returns to hostView

19. **Menus_Property_Setter_Replaces_Items** — Set Menus property, verify old items removed, new items added

20. **GetMenuItemsWith_Returns_Matching_Items** — Call GetMenuItemsWith with predicate, verify results

21. **Mouse_Click_On_Different_MenuBarItem_Switches** — Click File to open, click Edit, verify File closes and Edit opens

22. **MenuBar_Active_False_Sets_CanFocus_False** — Verify Active=false implies CanFocus=false

23. **IsOpen_False_After_All_Popovers_Close** — Open and close, verify IsOpen() returns false

---

## Phase 3: Delete Old Tests

**File to delete:** `Tests/UnitTests/Views/MenuBarTests.cs`

After all new parallelizable tests pass, delete the entire old test file. Verify no other files reference it.

---

## Verification

1. Build: `dotnet build --no-restore`
2. Run new tests: `dotnet test Tests/UnitTestsParallelizable --no-build --filter "FullyQualifiedName~MenuBarTests"`
3. Run all parallelizable: `dotnet test Tests/UnitTestsParallelizable --no-build`
4. Run integration tests: `dotnet test Tests/IntegrationTests --no-build --filter "FullyQualifiedName~MenuBarTests"`
5. Run full test suite to check for regressions: `dotnet test Tests/UnitTestsParallelizable --no-build && dotnet test Tests/UnitTests --no-build && dotnet test Tests/IntegrationTests --no-build`

## Order of Operations

1. Fix Bug 1 (OnMenuBarItemPopoverMenuOpenChanged) — smallest, most isolated fix
2. Fix Bug 2 (Visible/Enabled guard in OnActivating) — simple guard
3. Fix Bug 3/5 (BubblingUp handling in OnActivating) — most complex, addresses HotKey activation
4. Build and run existing parallelizable tests to verify no regressions
5. Write new parallelizable tests (ported scenarios first, then gap tests)
6. Run all tests
7. Delete old test file
8. Final full test run
