# Plan: Fix Command Bubbling Through Bar and Across PopoverMenu Boundary

## Context

Command bubbling in pr-4620 works for Shortcut, Dialog, Menu, and SelectorBase, but `Bar` (the base class of `Menu` and `MenuBar`) has `CommandsToBubbleUp` commented out (Bar.cs:38-39, BUGBUG). This prevents commands from Shortcuts inside a Bar from propagating upward. Additionally, the PopoverMenu overlay breaks the SuperView chain, blocking `Command.Activate` from reaching MenuBar's host view (Accept already bridges this gap via event subscriptions).

The BarTests BUGBUG assertions currently test the *wrong* (broken) values. The goal is to enable correct bubbling without breaking any existing tests.

## Approach: Incremental, Test-Driven

Each phase is verified before moving to the next. No phase depends on later phases being correct.

---

### Phase 1: Bolster Bar Tests + Diagnose Breakage

**Goal:** Write comprehensive Bar command-bubbling tests (matching the depth of ShortcutTests.Command.cs and SelectorBase tests), then enable `CommandsToBubbleUp` and see what breaks.

#### 1a. Write new Bar tests FIRST (before any source changes)

Add tests to `Tests/UnitTestsParallelizable/Views/BarTests.cs` covering command bubbling scenarios. Model after `ShortcutTests.Command.cs` patterns. These tests should initially assert the **desired** behavior (they will fail until Phase 2 fixes Bar).

New tests to add:

**Activate bubbling:**
- `Shortcut_Activate_Bubbles_To_Bar` — `shortcut.InvokeCommand (Command.Activate)` → `bar.Activating` fires once
- `Shortcut_Activate_Bubbles_To_Bar_And_SuperView` — Bar inside a SuperView with `CommandsToBubbleUp` → Activate reaches SuperView
- `CheckBox_CommandView_Activate_Bubbles_To_Bar` — CheckBox click inside Shortcut → bubbles through Shortcut → reaches Bar
- `CheckBox_CommandView_Toggles_Exactly_Once` — Same scenario but verifies CheckBox.Value changes exactly once (critical: no double-toggle)
- `Shortcut_Activate_Handled_Does_Not_Bubble_To_Bar` — `Activating` handler sets `Handled = true` → Bar.Activating does NOT fire
- `Bar_Activate_Does_Not_BubbleDown_To_Shortcuts` — `bar.InvokeCommand (Command.Activate)` → Shortcut.Activating does NOT fire (Bar fires its own events, not BubbleDown)

**Accept bubbling:**
- `Shortcut_Accept_Bubbles_To_Bar` — `shortcut.InvokeCommand (Command.Accept)` → `bar.Accepting` fires once
- `Shortcut_Accept_Handled_Does_Not_Bubble_To_Bar` — Handled Accept stops at Shortcut

**Event ordering:**
- `Shortcut_Activate_Event_Order` — Verify: Shortcut.Activating → Bar.Activating → Shortcut.Activated → Bar.Activated (Bar's events fire during Shortcut's RaiseActivating via TryBubbleUp)

**Context preservation:**
- `Activate_Source_Preserved_Through_Bubble` — `args.Context.Source` at Bar level still points to original source
- `Activate_Binding_Preserved_Through_Bubble` — Binding info preserved

#### 1b. Uncomment and run

1. In `Terminal.Gui/Views/Bar.cs` line 38-39, uncomment:
   ```csharp
   CommandsToBubbleUp = [Command.Accept, Command.Activate];
   ```
2. Build and run the full test suite:
   ```bash
   dotnet build --no-restore
   dotnet test Tests/UnitTestsParallelizable --no-build
   dotnet test Tests/UnitTests --no-build
   ```
3. Catalog every failure: file, test name, assertion that failed, actual vs expected.
4. Categorize failures into:
   - **Bar-only:** Tests that fail because Bar now bubbles when it didn't before
   - **Menu/MenuBar cascade:** Tests that fail because Menu inherits from Bar and behavior changed
   - **Shortcut interaction:** Tests where Shortcut's deferred activation breaks
   - **New tests:** Which of the Phase 1a tests now pass, which still fail (needing Phase 2 custom handlers)
   - **Other:** Unexpected failures

**Deliverable:** A categorized failure list that drives Phase 2, plus comprehensive test coverage for Bar.

---

### Phase 2: Fix Bar — Add Custom Handlers

**Goal:** Bar bubbles correctly without consuming commands from SubViews.

The key problem: without custom handlers, `DefaultActivateHandler` returns `true` (consuming the command), which prevents the originating Shortcut/CheckBox from completing its own activation.

**Fix:** Add custom command handlers to Bar matching Menu's existing pattern (`Menu.cs:41-65`):

```csharp
// In Bar constructor, after CommandsToBubbleUp line:
AddCommand (Command.Activate,
            ctx =>
            {
                if (ctx?.IsBubblingUp != true)
                {
                    return DefaultActivateHandler (ctx);
                }

                DefaultActivateHandler (ctx);

                return false;
            });

AddCommand (Command.Accept,
            ctx =>
            {
                if (ctx?.IsBubblingUp != true)
                {
                    return DefaultAcceptHandler (ctx);
                }

                DefaultAcceptHandler (ctx);

                return false;
            });
```

**Why `return false` when `IsBubblingUp`?** The originating SubView (e.g., CheckBox via Shortcut) must complete its own activation (toggle state). Returning `true` would consume the command and prevent that. This is exactly what `Menu` already does.

**Impact on Menu/MenuBar:** Menu's constructor calls `AddCommand` after `base(shortcuts)` (which runs Bar's constructor), so Menu's handlers replace Bar's. Same for MenuBar. Only plain `Bar` usage is affected.

**Verification:** Run Bar, Shortcut, and CheckBox tests. Fix any BUGBUG assertions.

---

### Phase 3: Fix BarTests BUGBUG Assertions

**Goal:** Tests assert correct behavior.

**File:** `Tests/UnitTestsParallelizable/Views/BarTests.cs`

- `No_CommandsToBubbleUp` → rename to `Has_CommandsToBubbleUp`, assert `Contains (Command.Accept)` and `Contains (Command.Activate)`
- `Command_Activate_On_Shortcut_BubblesDownToShortcuts` → change `Assert.Equal (0, barActivatingFired)` to `Assert.Equal (1, barActivatingFired)`, remove BUGBUG comments
- `Command_Activate_BubblesDownToShortcuts` → review/update BUGBUG comments (Bar doesn't BubbleDown to shortcuts; it fires its own events)
- Add new tests for:
  - Bar Accept bubbling from Shortcut
  - Bar with CheckBox CommandView: Activate bubbles, CheckBox toggles exactly once
  - Bar inside a SuperView with `CommandsToBubbleUp`: full chain propagation

---

### Phase 4: Fix Cascade Failures

**Goal:** Address any Menu/MenuBar/Shortcut test failures from Phase 1.

Based on the categorized failure list from Phase 1, fix each category:
- If Menu tests break: verify Menu's `AddCommand` overrides still take precedence
- If Shortcut tests break: verify `HandleActivate` deferred mechanism is intact
- If Dialog tests break: verify `CommandsToBubbleUp = [Command.Accept]` on Dialog still works

---

### Phase 5: Bridge Activate Across PopoverMenu

**Goal:** `Command.Activate` crosses the PopoverMenu boundary (like Accept already does).

This follows the existing Accept bridging pattern exactly:

1. **PopoverMenu** (`Terminal.Gui/Views/Menu/PopoverMenu.cs`): In the `Root` setter subscribe loop (line 342-349), add `menu.Activated += MenuActivated`. Add unsubscribe. Add handler:
   ```csharp
   private void MenuActivated (object? sender, CommandEventArgs e)
   {
       RaiseActivated (e.Context);
   }
   ```

2. **MenuBarItem** (`Terminal.Gui/Views/Menu/MenuBarItem.cs`): Line 156 — uncomment `RaiseActivated (e.Value)` in `OnPopoverMenuOnActivated`. Line 118-122 — add `_popoverMenu.Activated -= OnPopoverMenuOnActivated` to unsubscribe block. (Line 136 already subscribes.)

3. **MenuBar** (`Terminal.Gui/Views/Menu/MenuBar.cs`): In `OnSubViewAdded` (line 449) add `mbi.Activated += OnMenuBarItemActivated`. In `OnSubViewRemoved` (line 461) add unsubscribe. Add handler:
   ```csharp
   private void OnMenuBarItemActivated (object? sender, EventArgs<ICommandContext?> e) =>
       RaiseActivated (e.Value);
   ```

**Verification:** Unskip relevant tests in `CommandBubblingTests.cs`, run MenuBar/Menu tests.

---

### Phase 6: Unskip CommandBubblingTests

**File:** `Tests/UnitTestsParallelizable/ViewBase/CommandBubblingTests.cs`

Evaluate each skipped test. Priority unskips:
- `Activate_Propagates_FromCheckBox_ToFlagSelector`
- `Activate_Propagates_FromCheckBox_ToMenuBar`
- `Source_RemainsConstant_DuringActivateBubbling`
- `Binding_IsPreserved_DuringActivateBubbling`
- `Activate_HandledAtMenu_DoesNotReachMenuBar`

Accept tests may need separate investigation.

---

## Key Files

| File | Role |
|------|------|
| `Terminal.Gui/Views/Bar.cs` | Enable CommandsToBubbleUp + custom handlers |
| `Terminal.Gui/Views/Menu/Menu.cs` | Reference pattern (already correct) |
| `Terminal.Gui/Views/Menu/PopoverMenu.cs` | Bridge Activate via event subscription |
| `Terminal.Gui/Views/Menu/MenuBarItem.cs` | Uncomment RaiseActivated, fix unsubscribe |
| `Terminal.Gui/Views/Menu/MenuBar.cs` | Subscribe to MenuBarItem.Activated |
| `Terminal.Gui/Views/Shortcut.cs` | Do NOT modify (deferred activation must be preserved) |
| `Terminal.Gui/ViewBase/View.Command.cs` | Do NOT modify (core infrastructure) |
| `Tests/.../BarTests.cs` | Fix BUGBUG assertions, add new tests |
| `Tests/.../CommandBubblingTests.cs` | Unskip tests |

## Coding Style Reminders

- Space before `()` and `[]`: `Method ()`, `array [i]`
- Allman braces (next line)
- Blank line before `return`/`break`/`continue`/`throw`
- Blank line after control blocks
- No `var` except built-in types
- Use `new ()` not `new TypeName ()`
- Use `[...]` for collections
- Test comment: `// Claude - Opus 4.6`
- Tests go in `UnitTestsParallelizable`
