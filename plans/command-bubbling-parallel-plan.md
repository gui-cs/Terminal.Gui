# Parallel Agent Plan: Command Bubbling Refactor (PR #4620)

**Status:** Ready for execution
**Created:** 2026-02-06
**Author:** Claude Opus 4.6
**Source:** `plans/command-bubbling-progress.md`, `docfx/docs/command-propagation-plan.md`,
           `plans/precious-leaping-flurry.md`, `plans/view-command-behavior-tests.md`
**PR:** #4620 (branch: `copilot/fix-command-propagation-issue-clean`)
**Issue:** #4473

---

## Current State

| Phase | Description | Status |
|-------|-------------|--------|
| **1** Foundation (`PropagatedCommands`, `TryBubbleToSuperView`) | COMPLETE |
| **2** WeakReference for `ICommandContext.Source` (BREAKING) | COMPLETE |
| **3** Shortcut `Command.Activate` propagation | COMPLETE |
| **4** Re-enable skipped tests | NOT STARTED |
| **5** MenuBar/Menu Activate propagation | NOT STARTED |

**CI:** All 14 checks FAILING.
**Coverage:** 61.66% patch (target 70%), 235 lines missing.
**Skipped tests:** 7 (6 for Phase 5 MenuBar bubbling, 1 broken Shortcut test).

---

## What Needs to Happen

1. **Stabilize the branch** - merge v2_develop, build, identify actual CI failures
2. **Phase 4** - Re-enable WeakReference-related skipped tests
3. **Phase 5** - MenuBar/Menu Activate propagation (un-skips 6 tests)
4. **CWP fix** - SetFocus ordering violation in Command handlers
5. **Fix broken Shortcut test** - `KeyDown_CheckBox_Raises_Accepted_Selected`
6. **Close test coverage gaps** - extensions, events, view-specific behaviors
7. **Documentation** - update command.md, events.md, migration guide

---

## Stream Architecture

```
Phase 0 (Sequential - must complete first):
  ██ Stream 0: Merge + Build + Identify Failures ██

Phase 1 (Parallel - no file conflicts):
  Agent 1: ██ Stream A: Phase 4 - Re-enable WeakRef tests     ██
  Agent 2: ██ Stream B: CWP Violation Fix                      ██
  Agent 3: ██ Stream C: Extension & Event tests (new files)    ██
  Agent 4: ██ Stream D: Documentation updates                  ██

Phase 2 (Parallel - after B completes):
  Agent 5: ██ Stream E: Phase 5 - MenuBar propagation          ██
  Agent 6: ██ Stream F: Fix Shortcut test + Shortcut tests     ██
  Agent 7: ██ Stream G: View behavior tests - Core views       ██
  Agent 8: ██ Stream H: View behavior tests - Composite views  ██

Phase 3 (Sequential - validation gate):
  Agent 9: ██ Stream I: Full build + test suite + coverage     ██
```

---

## Stream 0: Merge & Stabilize (Sequential Prerequisite)

**Must complete before all other streams.**

### Tasks

1. Merge latest `v2_develop` into `copilot/fix-command-propagation-issue-clean`
2. Resolve any merge conflicts
3. `dotnet restore && dotnet build --no-restore`
4. Run tests with verbose output to identify actual failures:
   ```bash
   dotnet test Tests/UnitTestsParallelizable --no-build --verbosity normal 2>&1 | tee /tmp/parallel-results.txt
   dotnet test Tests/UnitTests --no-build --verbosity normal 2>&1 | tee /tmp/unit-results.txt
   ```
5. Categorize failures into: (a) merge-related, (b) pre-existing in PR, (c) new regressions
6. Create `/tmp/failure-report.md` listing each failing test with its error

### Output

A failure report that all subsequent streams can reference to know what's broken.

---

## Stream A: Phase 4 - Re-enable WeakReference Tests

**Depends on:** Stream 0
**Files (exclusive):**
- `Tests/UnitTestsParallelizable/ViewBase/CommandContextTests.cs`
- `Tests/UnitTestsParallelizable/Input/InputBindingTests.cs`

### Tasks

1. **Remove Skip attributes** from all tests in `CommandContextTests.cs`
2. **Update assertions** to use `TryGetTarget` pattern:
   ```csharp
   // OLD:
   Assert.Equal (view, ctx.Source);

   // NEW:
   Assert.NotNull (ctx.Source);
   Assert.True (ctx.Source.TryGetTarget (out View? source));
   Assert.Equal (view, source);
   ```
3. **Remove Skip attributes** from `InputBindingTests.cs`
4. **Update any commented-out assertions** (search for `// Assert` or `/* Assert`)
5. **Verify `Shortcut.DispatchCommand` preserves context correctly** - write a test
   confirming the Source WeakReference survives the dispatch chain

### Verification

```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~CommandContext" --no-build
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~InputBinding" --no-build
```

---

## Stream B: CWP Violation Fix

**Depends on:** Stream 0
**Files (exclusive):**
- `Terminal.Gui/ViewBase/View.Command.cs` (lines 18-51 in `SetupCommands()`)
- `Terminal.Gui/Views/CheckBox.cs` (lines 44-58)
- `Terminal.Gui/Views/Label.cs` (lines 52-81)
- `Terminal.Gui/Views/Selectors/FlagSelector.cs` (lines 80-102)

**Shared file (coordinate with Stream F):**
- `Terminal.Gui/Views/Shortcut.cs` (lines 298-367)

### Problem

`Command.HotKey` and `Command.Activate` handlers call `SetFocus()` AFTER notifications
instead of BEFORE, violating CWP ("work happens before notifications").

### Tasks

1. **Fix `View.Command.cs` HotKey handler** - Move `SetFocus()` before `RaiseHandlingHotKey()`
2. **Fix `View.Command.cs` Activate handler** - Move `SetFocus()` before `RaiseActivating()`
3. **Remove redundant `SetFocus()` from `CheckBox.OnHandlingHotKey`**
4. **Remove redundant `SetFocus()` from `Shortcut.OnHandlingHotKey` and `Shortcut.OnActivating`**
5. **Remove redundant `SetFocus()` from `Label.InvokeHotKeyOnNextPeer`**
6. **Remove redundant `SetFocus()` from `FlagSelector` event handler**
7. **Add CWP-compliance test:**
   ```csharp
   // Claude - Opus 4.6
   [Fact]
   public void HotKey_SetsFocus_BeforeHandlingHotKeyEvent ()
   {
       View view = new () { CanFocus = true, HotKeySpecifier = (Rune)'^', Title = "^Test" };
       bool focusSetBeforeEvent = false;
       view.HandlingHotKey += (_, _) => { focusSetBeforeEvent = view.HasFocus; };
       view.InvokeCommand (Command.HotKey);
       Assert.True (focusSetBeforeEvent, "CWP: SetFocus must happen BEFORE event");
       view.Dispose ();
   }
   ```

### Verification

```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~HotKey" --no-build
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~ViewCommand" --no-build
```

---

## Stream C: Extension & Event Tests (New Files Only)

**Depends on:** Stream 0
**Files (exclusive - ALL NEW):**
- `Tests/UnitTestsParallelizable/ViewBase/ViewExtensionsTests.cs` (NEW)
- `Tests/UnitTestsParallelizable/ViewBase/WeakReferenceExtensionsTests.cs` (NEW)
- `Tests/UnitTestsParallelizable/ViewBase/AcceptedActivatedEventsTests.cs` (NEW)
- `Tests/UnitTestsParallelizable/ViewBase/WeakReferenceCommandTests.cs` (NEW)

### Tasks

1. **Create `ViewExtensionsTests.cs`** - Test `ToIdentifyingString()`:
   - View with Id set
   - View with Title set
   - View with Text set
   - View with none set (returns type name)

2. **Create `WeakReferenceExtensionsTests.cs`** - Test `ToIdentifyingString()`:
   - Null weak reference
   - Dead target (view disposed)
   - Live target with various view states

3. **Create `AcceptedActivatedEventsTests.cs`** - Test non-cancellable events:
   - `Accepted` event fires after `Accepting` succeeds
   - `Activated` event fires after `Activating` succeeds
   - Event ordering verification
   - Events NOT raised when cancellable event is cancelled

4. **Create `WeakReferenceCommandTests.cs`** - Test lifecycle safety:
   - Source view disposal during propagation
   - Dead weak reference handling in TryBubbleToSuperView
   - Memory leak prevention (view not held by context)

### Verification

```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~ViewExtensions" --no-build
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~WeakReference" --no-build
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~AcceptedActivated" --no-build
```

---

## Stream D: Documentation Updates

**Depends on:** Stream 0
**Files (exclusive):**
- `docfx/docs/command.md`
- `docfx/docs/events.md`
- `docfx/docs/command-propagation-plan.md`
- `docfx/docs/migratingfromv1.md`

### Tasks

1. **Update `command.md`** View Command Behaviors table:
   - Button row: document that Activating is NOT raised, only Accepting
   - Add `PropagatedCommands` documentation with opt-in pattern
   - Update command pipeline to show bubbling flow
   - Document `DefaultAcceptView` behavior

2. **Update `events.md`**:
   - Document `Accepted` / `Activated` non-cancellable events
   - Add event ordering diagram: `Activating → [handled] → Activated`
   - Document WeakReference Source pattern with code examples

3. **Update `command-propagation-plan.md`**:
   - Mark Phases 1-3 as COMPLETE
   - Update Phase 4-5 descriptions with current understanding
   - Add section on CWP fix (cross-reference precious-leaping-flurry.md)

4. **Add migration section to `migratingfromv1.md`**:
   - `ICommandContext.Source` type change (`View?` → `WeakReference<View>?`)
   - `CommandContext` is now non-generic
   - Button no longer raises Activating
   - TextView Enter key change
   - Code migration examples for each breaking change

### Verification

Build docs and review for consistency:
```bash
grep -n "CommandContext<" docfx/docs/*.md  # Should find zero matches
grep -n "ctx\.Source\b" docfx/docs/*.md    # All should use TryGetTarget pattern
```

---

## Stream E: Phase 5 - MenuBar Activate Propagation

**Depends on:** Stream B (CWP fix must complete first - both touch View.Command.cs)
**Files (exclusive):**
- `Terminal.Gui/Views/Menu/MenuBar.cs`
- `Terminal.Gui/Views/Menu/Menu.cs`
- `Terminal.Gui/Views/Menu/PopoverMenu.cs`
- `Terminal.Gui/Views/Menu/MenuItem.cs`
- `Tests/UnitTestsParallelizable/ViewBase/CommandBubblingTests.cs`

### Tasks

1. **Set `PropagatedCommands` in MenuBar constructor:**
   ```csharp
   PropagatedCommands = [Command.Accept, Command.Activate];
   ```

2. **Set `PropagatedCommands` in Menu constructor:**
   ```csharp
   PropagatedCommands = [Command.Accept, Command.Activate];
   ```

3. **Add `MenuBar.OnActivating` override** to handle propagated Activate commands:
   - When a nested CheckBox/FlagSelector activates, MenuBar should know
   - Use `ctx.Source?.TryGetTarget()` to identify source view
   - Manage popover state as needed

4. **Un-skip 6 CommandBubblingTests:**
   - `Activate_Propagates_FromCheckBox_ToMenuBar`
   - `Accept_Propagates_FromCheckBox_ToMenuBar`
   - `Source_RemainsConstant_DuringActivateBubbling`
   - `Binding_IsPreserved_DuringActivateBubbling`
   - `Activate_HandledAtMenu_DoesNotReachMenuBar`
   - `Accept_HandledAtMenu_DoesNotReachMenuBar`

5. **Add new tests:**
   - FlagSelector → MenuItem → Menu → MenuBar propagation chain
   - Remove legacy `SelectedMenuItemChanged` workarounds if safe

### Verification

```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~CommandBubbling" --no-build
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~MenuBar" --no-build
```

---

## Stream F: Fix Shortcut Test + Shortcut Coverage

**Depends on:** Stream B (CWP fix touches Shortcut.cs)
**Files (exclusive):**
- `Tests/UnitTestsParallelizable/Views/ShortcutTests.cs`

**Shared file (coordinate with Stream B):**
- `Terminal.Gui/Views/Shortcut.cs` - Stream B removes `SetFocus()` calls;
  Stream F only adds tests. Stream B must complete its Shortcut.cs edits first.

### Tasks

1. **Investigate and fix `KeyDown_CheckBox_Raises_Accepted_Selected`** (line 462):
   - Currently skipped with "Broke somehow!"
   - Theory with 12 parameter combinations
   - Likely broken by Button event model change (Space/Enter → Accept instead of HotKey)
   - Or by Shortcut event forwarding changes
   - Fix the test or update expectations to match new behavior

2. **Add missing Shortcut tests:**
   - `CommandsToBubbleUp` behavior (both Activate and Accept)
   - Event forwarding from CommandView to Shortcut
   - Verify Activating raised when CommandView activates
   - Verify Accepting raised when CommandView accepts
   - `MouseHighlightStates` change (now `MouseState.In`)
   - Hierarchical: Shortcut in MenuItem in Menu

### Verification

```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~Shortcut" --no-build
```

---

## Stream G: View Behavior Tests - Core Views

**Depends on:** Stream 0
**Files (exclusive):**
- `Tests/UnitTestsParallelizable/Views/ButtonTests.cs` (add tests only)
- `Tests/UnitTestsParallelizable/Views/TextFieldTests.cs` (add tests only)
- `Tests/UnitTestsParallelizable/Views/ListViewTests.cs` (add tests only)
- `Tests/UnitTestsParallelizable/Views/TableViewTests.cs` (add tests only)
- `Tests/UnitTestsParallelizable/Views/TextViewTests.cs` (add tests only)
- `Tests/UnitTestsParallelizable/Views/TreeViewTests.cs` (add tests only)

### Tasks

Per `plans/view-command-behavior-tests.md`, add command behavior tests for:

1. **Button** (HIGH PRIORITY):
   - Verify Button does NOT raise Activating (new behavior)
   - Verify Space/Enter trigger Accept (was HotKey)
   - Verify mouse clicks trigger Accept
   - Test as `DefaultAcceptView` in Dialog/Window

2. **TextField**:
   - Enter triggers `Command.Accept` → raises Accepting
   - Accept bubbling from TextField to SuperView

3. **ListView**:
   - `Command.Accept` fires `RowActivated`
   - `Command.Activate` changes selection

4. **TableView**:
   - Space toggles cell selection
   - Enter fires `CellActivated`

5. **TextView**:
   - Enter inserts newline (NOT Accept command)
   - Accept returns `null` when unhandled (enables bubbling)
   - Integration: TextView in Dialog - Enter should NOT close dialog

6. **TreeView**:
   - Both Activate and Accept invoke same handler

All tests must include:
```csharp
// Claude - Opus 4.6
// Behavior documented in docfx/docs/command.md - View Command Behaviors table
```

### Verification

```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~Button_Command" --no-build
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~TextField_Command" --no-build
# etc.
```

---

## Stream H: View Behavior Tests - Composite & Selector Views

**Depends on:** Stream 0
**Files (exclusive):**
- `Tests/UnitTestsParallelizable/Views/DialogTests.cs` (add tests only)
- `Tests/UnitTestsParallelizable/Views/CheckBoxTests.cs` (add tests only)
- `Tests/UnitTestsParallelizable/Views/OptionSelectorTests.cs` (add tests only)
- `Tests/UnitTestsParallelizable/Views/FlagSelectorTests.cs` (add tests only)
- `Tests/UnitTests/Views/ComboBoxTests.cs` (add tests only)

### Tasks

1. **Dialog** (HIGH PRIORITY):
   - `OnActivating` with non-Default button (verify Result set)
   - `OnAccepting` with Default button (verify Result set + RequestStop)
   - WeakReference Source handling in command context
   - Integration: full Accept flow from key press to RequestStop

2. **CheckBox**:
   - `Command.Activate` toggles state
   - `Command.Accept` confirms state (no toggle)
   - `Command.HotKey` invokes Activate (toggles + focus)

3. **OptionSelector**:
   - Forwards Activate to focused CheckBox
   - Accept raises Accepting

4. **FlagSelector**:
   - Forwards Activate to focused CheckBox
   - Accept raises Accepting

5. **ComboBox**:
   - Activate toggles dropdown
   - Accept selects highlighted item

### Verification

```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~Dialog_Command" --no-build
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~CheckBox_Command" --no-build
```

---

## Stream I: Full Validation Gate

**Depends on:** ALL other streams completing.

### Tasks

1. **Full build:**
   ```bash
   dotnet restore && dotnet build --no-restore
   ```
   Must succeed with zero new warnings.

2. **Run all parallelizable tests:**
   ```bash
   dotnet test Tests/UnitTestsParallelizable --no-build --verbosity normal
   ```

3. **Run all non-parallel tests:**
   ```bash
   dotnet test Tests/UnitTests --no-build --verbosity normal
   ```

4. **Run integration tests:**
   ```bash
   dotnet test Tests/IntegrationTests --no-build --verbosity normal
   ```

5. **Verify no remaining Skip attributes** (except intentional ones):
   ```bash
   grep -rn "Skip\s*=" Tests/ | grep -v "node_modules"
   ```
   Review each remaining Skip - document why it's still skipped.

6. **Verify code coverage** improved from 61.66% baseline.

7. **Verify no stale references:**
   ```bash
   # No direct Source access (should all use TryGetTarget)
   grep -rn "\.Source\b" Terminal.Gui/ --include="*.cs" | grep -v "WeakReference" | grep -v "//"
   ```

### Success Criteria

- All tests pass (zero failures)
- No new warnings
- Coverage >= 70% on patch
- Zero remaining unintentional Skip attributes
- All 7 previously-skipped tests now pass

---

## File Ownership Map (Conflict Prevention)

| File | Owner | Notes |
|------|-------|-------|
| **Library Source** | | |
| `View.Command.cs` | **B** | E waits for B |
| `CheckBox.cs` | **B** | |
| `Label.cs` | **B** | |
| `FlagSelector.cs` | **B** | |
| `Shortcut.cs` | **B then F** | B edits, F only tests after |
| `MenuBar.cs` | **E** | |
| `Menu.cs` | **E** | |
| `PopoverMenu.cs` | **E** | |
| `MenuItem.cs` | **E** | |
| **Test Files** | | |
| `CommandContextTests.cs` | **A** | |
| `InputBindingTests.cs` | **A** | |
| `CommandBubblingTests.cs` | **E** | |
| `ShortcutTests.cs` | **F** | |
| `ButtonTests.cs` | **G** | |
| `TextFieldTests.cs` | **G** | |
| `ListViewTests.cs` | **G** | |
| `TableViewTests.cs` | **G** | |
| `TextViewTests.cs` | **G** | |
| `TreeViewTests.cs` | **G** | |
| `DialogTests.cs` | **H** | |
| `CheckBoxTests.cs` | **H** | |
| `OptionSelectorTests.cs` | **H** | |
| `FlagSelectorTests.cs` | **H** | |
| `ComboBoxTests.cs` | **H** | |
| `ViewExtensionsTests.cs` | **C** | NEW |
| `WeakReferenceExtensionsTests.cs` | **C** | NEW |
| `AcceptedActivatedEventsTests.cs` | **C** | NEW |
| `WeakReferenceCommandTests.cs` | **C** | NEW |
| **Documentation** | | |
| `command.md` | **D** | |
| `events.md` | **D** | |
| `command-propagation-plan.md` | **D** | |
| `migratingfromv1.md` | **D** | |

---

## Dependency Graph

```
Stream 0 (Merge & Stabilize)
  │
  ├──► Stream A (Phase 4: WeakRef tests)
  ├──► Stream B (CWP fix) ──────────────────► Stream E (Phase 5: MenuBar)
  ├──► Stream C (Extension & Event tests)     Stream F (Shortcut fix)
  ├──► Stream D (Documentation)
  ├──► Stream G (Core view tests)
  └──► Stream H (Composite view tests)
                                               │
                                               ▼
                                         Stream I (Validation)
```

**Maximum parallelism:** 6 agents (A, B, C, D, G, H) after Stream 0.
**Second wave:** 2 agents (E, F) after Stream B completes.
**Final:** 1 agent (I) for validation.

---

## Commit Convention

Each stream should commit with:
```
feat(commands): [stream-X] <description>

Part of #4473, PR #4620
```

---

## Risk Register

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Merge conflicts between v2_develop and PR branch | High | Stream 0 handles this first |
| Stream B (CWP fix) introduces regressions | Medium | Comprehensive tests, Stream I validates |
| Stream E (MenuBar) more complex than expected | Medium | Can be split further if needed |
| View behavior tests discover new bugs | High | Document and fix in-stream or create follow-up |
| Test coverage still below 70% after all streams | Medium | Streams C, G, H specifically target gaps |

---

## Sign-off

**Plan Author:** Claude Opus 4.6
**Date:** 2026-02-06
**Status:** Ready for parallel execution
