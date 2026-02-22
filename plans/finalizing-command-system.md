# Finalizing Command System - Bug Fixes

## Status: Issues #1-3 COMPLETED, Gaps #1-3 COMPLETED

Issues #1-3 have been implemented and verified. See [Implementation Results](#implementation-results) at the bottom.
Architectural Gaps #1-3 are all completed.
Issues #4-6 remain deferred.

---

## Context

The command system redesign (Phases A-E from PR #62) introduced `GetDispatchTarget`/`ConsumeDispatch`, `CommandRouting` enum, and `CommandBridge`. Several issues remain, identified through a failing test (`CommandView_Command_Activate_Bubbles_To_Shortcut_SuperView`) and the Cursor Bugbot review of PR #62.

**Caution**: The command system has subtle invariants. Pushing in one place (e.g., making Activated fire on bubble-up) can break something else nuanced (e.g., double-firing in Shortcut's deferred completion, or breaking consume-dispatch semantics in selectors). Each fix must be verified against ALL tests before proceeding to the next.

---

## Architectural Background: Two Dispatch Patterns

Understanding these patterns is critical before making any changes.

### Pattern 1: Relay Dispatch (Shortcut, `ConsumeDispatch=false`)

```
SuperView (CommandsToBubbleUp=[Activate])
  └─ Shortcut (CommandsToBubbleUp=[Activate,Accept], ConsumeDispatch=false)
       └─ CommandView (e.g., CheckBox)
```

- `TryDispatchToTarget` returns **false** (relay, doesn't consume)
- `TryBubbleUp` **CAN run** → command propagates further up the hierarchy
- CommandView completes its own activation (fires `RaiseActivated`)
- Shortcut gets Activated via deferred completion (`CommandView_Activated` callback)
- **SuperView CAN receive the command** (if it has Activate in `CommandsToBubbleUp`)

### Pattern 2: Consume Dispatch (OptionSelector/FlagSelector, `ConsumeDispatch=true`)

```
SuperView (CommandsToBubbleUp=[Activate])
  └─ FlagSelector (CommandsToBubbleUp=[Activate,Accept], ConsumeDispatch=true)
       └─ CheckBox (internal)
```

- `TryDispatchToTarget` returns **true** (consumed) → `args.Handled = true` (line 545)
- `TryBubbleUp` is **SKIPPED** (line 548: `if (!args.Handled)` fails)
- Selector fires `RaiseActivated` itself (via `_lastDispatchOccurred=true`, line 450)
- **SuperView NEVER receives the command** — this is intentional
- The selector owns its internal state; inner CheckBox activations are implementation details

### Key Implication

**Issue #1's fix only helps relay-dispatch views (Shortcut) and plain views.** The skipped `CommandBubblingTests` (which involve FlagSelector inside MenuItem/MenuBar hierarchy) require a separate design decision about whether consume-dispatch views should also propagate after consuming. That is out of scope for this plan.

---

## Priority 1: Correctness Fixes (Issues #1-3)

### Issue #1: `DefaultActivateHandler` doesn't fire `RaiseActivated` on bubble-up

**File**: `Terminal.Gui/ViewBase/View.Command.cs:464`
**Failing Test**: `ShortcutTests.CommandView_Command_Activate_Bubbles_To_Shortcut_SuperView`

#### Root Cause

Asymmetry between Activate and Accept handlers. When a command arrives via `BubblingUp`:

| Handler | Behavior on BubblingUp | Calls Raise*d? |
|---------|----------------------|----------------|
| `DefaultAcceptHandler` (line 285-304) | Falls through to `RaiseAccepted` (line 294) | **Yes** ✓ |
| `DefaultActivateHandler` (line 464-467) | Early-returns `false` | **No** ✗ |

The current code at line 464:
```csharp
// When a SubView's activation bubbles up, the default behavior is notification:
// Activating fires (above), but Activated and side effects (SetFocus) are skipped.
if (ctx?.Routing == CommandRouting.BubblingUp)
{
    return false;
}
```

The comment's intent (skip SetFocus) is correct, but skipping RaiseActivated breaks the two-phase notification model.

#### Detailed Trace of the Failing Test

```
testCommandView.InvokeCommand(Activate) — ctx: {Routing=Direct, Binding=null}
│
├─ DefaultActivateHandler (testCommandView)
│  └─ RaiseActivating (testCommandView)
│     ├─ OnActivating → false (base View, no override)
│     ├─ Activating?.Invoke → commandViewActivatingRaised=1 ✓
│     ├─ TryDispatchToTarget → null (no dispatch target) → false
│     └─ TryBubbleUp:
│        SuperView is Shortcut, CommandsToBubbleUp has Activate → YES
│        upCtx = {Routing=BubblingUp, Binding=null}
│        │
│        └─ Shortcut.InvokeCommand(Activate, upCtx)
│           └─ DefaultActivateHandler (Shortcut) — ctx: {Routing=BubblingUp}
│              └─ RaiseActivating (Shortcut)
│                 ├─ OnActivating → false
│                 ├─ Activating?.Invoke → shortcutActivatingRaised=1 ✓
│                 ├─ TryDispatchToTarget:
│                 │  target = CommandView (testCommandView)
│                 │  !ConsumeDispatch && ctx?.Binding is null → TRUE → return false
│                 │  (Relay guard blocks dispatch for programmatic invocations)
│                 └─ TryBubbleUp:
│                    SuperView is superView, CommandsToBubbleUp has Activate → YES
│                    │
│                    └─ superView.InvokeCommand(Activate, upCtx2)
│                       └─ DefaultActivateHandler (superView) — ctx: {Routing=BubblingUp}
│                          └─ RaiseActivating (superView)
│                             ├─ Activating?.Invoke → superViewActivatingCount=1 ✓
│                             └─ returns false (not handled)
│                          ★ LINE 464: BubblingUp → return false
│                          ★ RaiseActivated NEVER CALLED → superViewActivatedCount=0 ✗
│
│              ★ LINE 464: BubblingUp → return false
│              ★ Shortcut's RaiseActivated NOT called here (deferred path handles it)
│
│  RaiseActivating returned false
│  Not BubblingUp (Direct) → continues
│  !_lastDispatchOccurred → RaiseActivated (testCommandView)
│  └─ testCommandView.Activated fires → commandViewActivatedRaised=1 ✓
│     └─ Shortcut.CommandView_Activated:
│        _activatedFiredThisCycle=false → calls RaiseActivated(ctx)
│        → shortcutActivatedRaised=1 ✓
│        → BUT nothing propagates to superView
│
│ RESULT: superViewActivatedCount=0 ✗ (expected 1)
```

#### Fix

Change `DefaultActivateHandler` to call `RaiseActivated` for BubblingUp (matching Accept's behavior) while still skipping `SetFocus`:

```csharp
// Line 464 — CHANGE FROM:
if (ctx?.Routing == CommandRouting.BubblingUp)
{
    return false;
}

// TO:
if (ctx?.Routing == CommandRouting.BubblingUp)
{
    if (!_lastDispatchOccurred)
    {
        RaiseActivated (ctx);
    }

    return false;
}
```

#### Why the `_lastDispatchOccurred` guard is needed

For consume-dispatch views (OptionSelector/FlagSelector) receiving BubblingUp:
- `RaiseActivating` returns `true` (consumed) → we enter the `if` block at line 445
- `_lastDispatchOccurred=true` → `RaiseActivated` already fires at line 450
- We never reach line 464 → the fix doesn't apply → no double-fire

For relay-dispatch views (Shortcut) receiving BubblingUp:
- `RaiseActivating` returns `false` → skip the `if` block at line 445
- Reach line 464 (BubblingUp check)
- `_lastDispatchOccurred=false` (relay guard prevented dispatch) → call `RaiseActivated`
- Shortcut's `OnActivated` sets `_activatedFiredThisCycle=true`
- Later: `CommandView_Activated` fires → sees flag is true → skips duplicate → resets flag ✓

For plain Views receiving BubblingUp:
- No dispatch target → `_lastDispatchOccurred=false`
- Reach line 464 → `RaiseActivated` fires → superView.Activated event ✓

#### Focused Unit Tests to Verify

**Test A** — Simplest possible case (new test to add to ViewCommandTests.cs):
```csharp
[Fact]
public void Activate_BubblingUp_Fires_Activated_On_SuperView ()
{
    View superView = new () { CommandsToBubbleUp = [Command.Activate] };
    View subView = new ();
    superView.Add (subView);

    var activatedCount = 0;
    superView.Activated += (_, _) => activatedCount++;

    subView.InvokeCommand (Command.Activate);

    Assert.Equal (1, activatedCount); // FAILS with current code
}
```

**Test B** — The user's failing test (already exists):
`ShortcutTests.CommandView_Command_Activate_Bubbles_To_Shortcut_SuperView`

**Test C** — Deep hierarchy (new test for ViewCommandTests.cs):
```csharp
[Fact]
public void Activate_BubblingUp_Fires_Activated_In_Deep_Hierarchy ()
{
    View grandSuperView = new () { CommandsToBubbleUp = [Command.Activate] };
    View superView = new () { CommandsToBubbleUp = [Command.Activate] };
    View subView = new ();
    grandSuperView.Add (superView);
    superView.Add (subView);

    var grandActivatedCount = 0;
    grandSuperView.Activated += (_, _) => grandActivatedCount++;

    subView.InvokeCommand (Command.Activate);

    Assert.Equal (1, grandActivatedCount); // FAILS with current code
}
```

**Test D** — Verify consume-dispatch blocks bubbling (document current behavior):
```csharp
[Fact]
public void ConsumeDispatch_Blocks_Further_Bubbling ()
{
    // OptionSelector uses ConsumeDispatch=true — activation should NOT
    // propagate from its inner CheckBox to OptionSelector's SuperView
    View superView = new () { CommandsToBubbleUp = [Command.Activate] };
    OptionSelector<int> selector = new () { /* ... setup ... */ };
    superView.Add (selector);

    var superViewActivatingCount = 0;
    superView.Activating += (_, _) => superViewActivatingCount++;

    // Activate an inner CheckBox
    CheckBox innerCb = selector.SubViews.OfType<CheckBox> ().First ();
    innerCb.InvokeCommand (Command.Activate);

    Assert.Equal (0, superViewActivatingCount); // Consume blocks propagation
}
```

#### Existing tests that MUST NOT break

- `CommandView_Command_Activate_Bubbles_To_Shortcut` (ShortcutTests.Command.cs:162) — Shortcut.Activating fires
- `CommandsToBubbleUp_CanBeCustomized` (ViewCommandTests.cs:411) — plain View bubbling
- `CommandsToBubbleUp_StopsWhenHandled` (ViewCommandTests.cs:427) — handled stops bubbling
- `CommandsToBubbleUp_WorksInDeepHierarchy` (ViewCommandTests.cs:447) — Accept deep chain
- `FlagSelector_CommandView_SubView_Activate_Does_Not_Duplicate` (ShortcutTests.Command.cs:866) — no duplicate events
- `OptionSelector_CommandView_Enter_Activates_And_Accepts` (ShortcutTests.Command.cs:924)
- All existing `Activate_*` and `Activated_*` tests in ViewCommandTests.cs
- All FlagSelectorTests and OptionSelectorTests

---

### Issue #2: Stale `_lastDispatchOccurred` causes spurious completion events (HIGH)

**File**: `Terminal.Gui/ViewBase/View.Command.cs:765`
**Source**: Cursor Bugbot review #3 on PR #62

#### The Problem

`_lastDispatchOccurred` is only reset inside `TryDispatchToTarget` (line 777), but `RaiseActivating`/`RaiseAccepting` can return `true` via early-exit paths that skip `TryDispatchToTarget`:
1. `OnActivating` returns true (line 533)
2. `Activating` event handler sets `args.Handled = true` (line 540)

In those cases, the flag retains its value from a **prior** invocation.

#### Failure Scenario

```
1. User clicks CheckBox inside Shortcut → dispatch occurs → _lastDispatchOccurred=true
2. User subscribes to Activating, sets Handled=true (cancel)
3. Click again → DefaultActivateHandler calls RaiseActivating
4. RaiseActivating: OnActivating→false, Activating→Handled=true → returns true
5. DefaultActivateHandler line 448: _lastDispatchOccurred is STALE true
6. RaiseActivated fires DESPITE activation being cancelled ✗
```

#### Fix

Reset at the top of both handlers:

```csharp
internal bool? DefaultActivateHandler (ICommandContext? ctx)
{
    _lastDispatchOccurred = false;  // ADD THIS LINE

    if (RaiseActivating (ctx) is true)
    ...
```

Same for `DefaultAcceptHandler` (line 250).

#### Focused Unit Test

```csharp
[Fact]
public void Activate_Cancelled_After_Dispatch_Does_Not_Fire_Activated ()
{
    Shortcut shortcut = new () { Key = Key.T, CommandView = new CheckBox () };
    var activatedCount = 0;
    shortcut.Activated += (_, _) => activatedCount++;

    // First: normal activation with binding (triggers dispatch)
    KeyBinding kb = new ([Command.Activate]) { Key = Key.Space, Source = new WeakReference<View> (shortcut) };
    shortcut.InvokeCommand (Command.Activate, kb);
    // activatedCount is now 1 (or more depending on deferred path)
    int afterFirst = activatedCount;

    // Second: cancel activation
    shortcut.Activating += (_, args) => args.Handled = true;
    shortcut.InvokeCommand (Command.Activate, kb);

    // Activated should NOT have fired again (cancelled)
    Assert.Equal (afterFirst, activatedCount);
}
```

---

### Issue #3: Shortcut `_activatedFiredThisCycle` stuck after programmatic invoke (MEDIUM)

**File**: `Terminal.Gui/Views/Shortcut.cs:562`
**Source**: Cursor Bugbot review #4 on PR #62

#### The Problem

```
1. shortcut.InvokeCommand(Command.Activate) — programmatic, no binding
2. DefaultActivateHandler → RaiseActivating:
   - TryDispatchToTarget: !ConsumeDispatch && ctx?.Binding is null → SKIP (line 796)
3. Not BubblingUp → RaiseActivated fires on Shortcut
4. Shortcut.OnActivated: _activatedFiredThisCycle = true
5. CommandView_Activated NEVER fires (CommandView was never activated)
6. _activatedFiredThisCycle stays stuck at true

7. LATER: User clicks CheckBox (CommandView)
8. CheckBox activates → CommandView_Activated fires on Shortcut
9. _activatedFiredThisCycle is true → SKIPS RaiseActivated ✗
10. Shortcut's Action silently doesn't run
11. Flag resets to false (too late)
```

#### Fix

Reset `_activatedFiredThisCycle` at the start of each activation cycle:

```csharp
// Add to Shortcut.cs
protected override bool OnActivating (CommandEventArgs args)
{
    _activatedFiredThisCycle = false;

    return base.OnActivating (args);
}
```

**Why `OnActivating`?** It fires at the beginning of every activation cycle, before any `RaiseActivated` or `CommandView_Activated` can run. This ensures the flag starts clean.

#### Focused Unit Test

```csharp
[Fact]
public void Shortcut_Programmatic_Activate_Then_User_Click_Both_Fire_Action ()
{
    var actionCount = 0;
    CheckBox cb = new () { Title = "Test" };
    Shortcut shortcut = new () { Key = Key.T, CommandView = cb, Action = () => actionCount++ };

    // Programmatic invoke (no binding → dispatch skipped)
    shortcut.InvokeCommand (Command.Activate);
    Assert.Equal (1, actionCount);

    // Simulate user click (with binding → dispatch runs)
    KeyBinding kb = new ([Command.Activate]) { Key = Key.Space, Source = new WeakReference<View> (cb) };
    cb.InvokeCommand (Command.Activate, kb);

    Assert.Equal (2, actionCount); // FAILS with current code: stuck flag skips Action
}
```

---

## Priority 2: Deferred Fixes (Issues #4-6)

### Issue #4: Accept returns `false` for composite views on bubble-up (MEDIUM)

**File**: `Terminal.Gui/ViewBase/View.Command.cs:286-291`
**Source**: Cursor Bugbot review #1

Returns `false` for composite views receiving Accept via BubblingUp. May cause input events to not be consumed. Needs test coverage analysis before changing.

### Issue #5: CommandBridge Dispose asymmetric unsubscription (LOW)

**File**: `Terminal.Gui/Input/CommandBridge.cs`
**Source**: Cursor Bugbot review #2

Dispose unconditionally unsubscribes both handlers but constructor subscribes conditionally. Safe no-op but asymmetric.

### Issue #6: Debug logging in mouse hot path (LOW)

**File**: `Terminal.Gui/ViewBase/Mouse/View.Mouse.cs`
**Source**: Cursor Bugbot review #6

String interpolation in `Logging.Debug` runs on every mouse event. Also active in MenuBar, PopoverMenu, MenuBarItem.

---

## Out of Scope: Consume-Dispatch Deep Hierarchy Bubbling

The skipped `CommandBubblingTests` (e.g., `Activate_Propagates_FromCheckBox_ToMenuBar`) involve FlagSelector inside MenuItem/MenuBar. Because FlagSelector uses `ConsumeDispatch=true`, `TryDispatchToTarget` sets `args.Handled=true` in `RaiseActivating` (line 545), which causes `TryBubbleUp` to be **skipped** (line 548). The command stops at the selector.

This is a design decision: should consume-dispatch views propagate after consuming? The current behavior is intentional (selectors own internal state), but it means the MenuBar hierarchy tests require a different approach (possibly through `CommandBridge` or the deferred `Activated` event chain). This is separate from Issues #1-3.

---

## Critical Files to Modify

| File | Issues | Risk |
|------|--------|------|
| `Terminal.Gui/ViewBase/View.Command.cs` | #1, #2 | HIGH — shared by all views |
| `Terminal.Gui/Views/Shortcut.cs` | #3 | MEDIUM — only affects Shortcut |
| `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs` | New tests | LOW |
| `Tests/UnitTestsParallelizable/Views/ShortcutTests.Command.cs` | New tests | LOW |

## Implementation Steps (Methodical)

### Step 0: Establish Baseline
```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build
dotnet test Tests/UnitTests --no-build
```
Record exact test counts and known failures (MenuBar/PopoverMenu). Every subsequent run must match or improve this baseline.

### Step 1: Add Failing Tests First (RED)
Add Test A (`Activate_BubblingUp_Fires_Activated_On_SuperView`) to ViewCommandTests.cs.
Run tests. Confirm it fails and ONLY it fails (no other regressions).

### Step 2: Fix Issue #1 (GREEN)
Change `DefaultActivateHandler` line 464 in View.Command.cs.
Run ALL tests. Verify:
- Test A passes ✓
- Test B (user's Shortcut test) passes ✓
- No other tests broke ✗ → investigate before proceeding

### Step 3: Add Test C (deep hierarchy) and Test D (consume-dispatch blocks)
Verify both pass without any further code changes.

### Step 4: Fix Issue #2 (stale `_lastDispatchOccurred`)
Add `_lastDispatchOccurred = false` at top of both handlers.
Run ALL tests. This is a safety fix — existing tests should still pass.

### Step 5: Fix Issue #3 (stuck `_activatedFiredThisCycle`)
Add `OnActivating` override to Shortcut.cs.
Run ALL tests. Verify the focused test passes.

### Step 6: Final Verification
Run the complete test suite one final time. Compare test counts to Step 0 baseline.

## Verification Commands

After **every** step:
```bash
dotnet build --no-restore
dotnet test Tests/UnitTestsParallelizable --no-build
dotnet test Tests/UnitTests --no-build
```

**Known failures to exclude**: MenuBar/PopoverMenu tests (in-progress refactoring).
**Zero tolerance**: Any new failure not in the known list → stop and investigate.

---

## Implementation Results

### Issue #1: COMPLETED — Deviation from Plan

The plan proposed adding `RaiseActivated` unconditionally (guarded only by `_lastDispatchOccurred`):
```csharp
if (!_lastDispatchOccurred)
{
    RaiseActivated (ctx);
}
```

**Problem discovered during implementation**: This caused two new test failures:
- `Action_Sees_Updated_CheckBox_Value_On_BubbleUp_Activate` — Shortcut's `OnActivated` called `Action` before CheckBox had toggled state
- `BubbleUp_Activate_Event_Ordering_CommandView_Completes_Before_Shortcut_Activated` — Wrong event ordering

**Root cause**: Relay-dispatch views (Shortcut) need deferred completion — their `RaiseActivated` must fire AFTER the CommandView (e.g., CheckBox) has completed its own activation (state toggle happens in `CheckBox.OnActivated`). Calling `RaiseActivated` during BubblingUp is too early.

**Actual fix**: Added a `GetDispatchTarget (ctx) is null` guard:
```csharp
if (!_lastDispatchOccurred && GetDispatchTarget (ctx) is null)
{
    RaiseActivated (ctx);
}
```

This fires `RaiseActivated` only for **plain views** (no dispatch target). Relay-dispatch views (Shortcut) continue to use the deferred `CommandView_Activated` callback path. Consume-dispatch views (Selectors) are already handled by the `RaiseActivating` return path (line 445–453).

### Issue #2: COMPLETED — As Planned

Added `_lastDispatchOccurred = false;` at the top of both `DefaultActivateHandler` and `DefaultAcceptHandler`, before `RaiseActivating`/`RaiseAccepting` is called.

### Issue #3: COMPLETED — As Planned

Added `OnActivating` override to `Shortcut.cs` that resets `_activatedFiredThisCycle = false` at the start of each activation cycle.

### Test Results

| Metric | Baseline | Final | Delta |
|--------|----------|-------|-------|
| **UnitTestsParallelizable** | | | |
| Total | 14022 | 14027 | +5 (new tests) |
| Passed | 13989 | 13995 | +6 (+5 new + 1 fixed) |
| Failed | 11 | 10 | -1 (Shortcut SuperView test fixed) |
| Skipped | 22 | 22 | 0 |
| **UnitTests** | | | |
| Total | 1022 | 1022 | 0 |
| Passed | 989 | 989 | 0 |
| Failed | 10 | 10 | 0 (all MenuBar — known) |
| Skipped | 23 | 23 | 0 |

### New Tests Added (5)

**ViewCommandTests.cs:**
1. `Activate_BubblingUp_Fires_Activated_On_SuperView` — Plain view Activated fires on bubble-up
2. `Activate_BubblingUp_Fires_Activated_In_Deep_Hierarchy` — Deep chain propagation
3. `ConsumeDispatch_Blocks_Further_Bubbling` — OptionSelector blocks propagation to SuperView

**ShortcutTests.Command.cs:**
4. `Activate_Cancelled_After_Dispatch_Does_Not_Fire_Activated` — Stale flag regression test
5. `Shortcut_Programmatic_Activate_Then_User_Click_Both_Fire_Action` — Stuck flag regression test

### Files Modified

| File | Change |
|------|--------|
| `Terminal.Gui/ViewBase/View.Command.cs` | Issues #1, #2: BubblingUp RaiseActivated + flag reset |
| `Terminal.Gui/Views/Shortcut.cs` | Issue #3: OnActivating override resets deferred flag |
| `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs` | 3 new tests |
| `Tests/UnitTestsParallelizable/Views/ShortcutTests.Command.cs` | 2 new tests |

---

## Architectural Gaps (Menu/MenuItem Focus)

Three architectural gaps were discovered while adding CommandView propagation tests for Menu→MenuItem:

### Gap 1: FlagSelector/OptionSelector External Activation Enhancement — COMPLETED

**Problem**: When Menu.OnActivating dispatches `Command.Activate` to a MenuItem that has a FlagSelector as its CommandView, the FlagSelector's value doesn't change. The `DispatchingDown` guard in `TryDispatchToTarget` prevents multi-level dispatch chains (Menu → MenuItem → FlagSelector → CheckBox).

**Analysis**: The guard IS important for correctness. Removing it would cause FlagSelector.OnActivated to receive a command with the MenuItem as the source (not a CheckBox), leading to state desync between inner CheckBoxes and FlagSelector.Value. The FlagSelector is a consume-dispatch composite view that owns its internal state; its inner CheckBox activations are implementation details.

**Test documenting this**: `Menu_InvokeActivate_With_Focus_FlagSelector_Value_Unchanged` in MenuTests.cs.

**Resolution**: This is by design. Consume-dispatch views should not have their internal state manipulated from external dispatch chains. If users need to change FlagSelector state from a menu, they should use the FlagSelector's public API (e.g., set `Value` directly) rather than attempting to dispatch through the command pipeline.

### Gap 2: CommandBridge → InvokeCommand — COMPLETED

**Problem**: CommandBridge called `owner.RaiseAccepted()`/`owner.RaiseActivated()` directly, which are fire-and-forget events. Bridged commands stopped at the parentMenuItem and never bubbled further to rootMenu or its SuperView.

**Fix** (`CommandBridge.cs`):
- Changed `OnRemoteAccepted`: `owner.RaiseAccepted(ctx)` → `owner.InvokeCommand(Command.Accept, ctx)`
- Changed `OnRemoteActivated`: `owner.RaiseActivated(ctx)` → `owner.InvokeCommand(Command.Activate, ctx)`
- Both create a `CommandContext` with `Routing = CommandRouting.Bridged`

**Fix** (`View.Command.cs`):
- Added `Bridged` routing guard in `TryDispatchToTarget` — prevents dispatching down when a command arrives via bridge (bridge brings commands *up*, not *down*)
- Added `ctx?.Routing == CommandRouting.Bridged` check in `DefaultAcceptHandler`'s early-exit path — ensures `RaiseAccepted` fires for bridged Accept commands (compensates for Accept/Activate asymmetry: Accept bubbling returns `true` while Activate returns `false`)

**Tests updated**:
- `SubMenu_ChildActivate_Fires_Activating_On_ParentMenuItem` (was "Does_Not_Fire", assert 0→1)
- `SubMenu_ChildActivate_Bridges_Through_ParentMenuItem_To_RootMenu` (was "Does_Not_Reach", assert 0→1)
- `SubMenu_Bridge_Propagates_Through_ParentMenuItem_To_RootMenu_And_SuperView` (was "Does_Not_Propagate", assert 0→1)

### Gap 3: Menu.OnActivating Dispatches to Focused MenuItem — COMPLETED

**Problem**: `menu.InvokeCommand(Command.Activate)` fired Menu's own Activating event but did NOT dispatch to any MenuItem or its CommandView. There was no `GetDispatchTarget` or equivalent dispatch mechanism.

**Fix** (`Menu.cs`): Added `OnActivating` override that:
1. Calls `base.OnActivating(args)` — lets the base class handle default behavior
2. Checks for `BubblingUp` routing — if a MenuItem's activation is bubbling up, don't re-dispatch (prevents loops)
3. If `Focused` is a `MenuItem`, creates a synthetic context and calls `menuItem.InvokeCommand(Command.Activate, ctx)`

**Tests added**:
- `Menu_InvokeActivate_With_Focus_Dispatches_To_MenuItem_CheckBox_RoundTrip` — Full round-trip: menu.InvokeCommand(Activate) → MenuItem → CheckBox value toggles
- `Menu_InvokeActivate_With_Focus_FlagSelector_Value_Unchanged` — Documents DispatchingDown guard prevents FlagSelector inner dispatch

### Gap Implementation Test Results

| Metric | Before Gaps | After Gaps | Delta |
|--------|-------------|------------|-------|
| **UnitTestsParallelizable** | | | |
| Total | 14027 | 14075 | +48 (new tests from multiple sessions) |
| Passed | 13995 | 14041 | +46 |
| Failed | 10 | 12 | +2 (pre-existing MenuBar/PopoverMenu) |
| Skipped | 22 | 22 | 0 |

### Files Modified (Gaps #2-3)

| File | Change |
|------|--------|
| `Terminal.Gui/Input/CommandBridge.cs` | Gap 2: InvokeCommand instead of RaiseAccepted/RaiseActivated |
| `Terminal.Gui/ViewBase/View.Command.cs` | Gap 2: Bridged guard in TryDispatchToTarget + DefaultAcceptHandler |
| `Terminal.Gui/Views/Menu/Menu.cs` | Gap 3: OnActivating override dispatches to focused MenuItem |
| `Tests/UnitTestsParallelizable/Views/MenuItemTests.cs` | 2 tests renamed/updated |
| `Tests/UnitTestsParallelizable/Views/MenuTests.cs` | 2 tests renamed, 2 new tests added |
| `docfx/docs/command.md` | Updated CommandBridge, TryDispatchToTarget, Menu behavior docs |

---

## Gap 1: FlagSelector/OptionSelector External Activation Enhancement

### Problem Statement

When `Menu.OnActivating` dispatches `Command.Activate` to a `MenuItem` whose `CommandView` is a `FlagSelector`, the FlagSelector's `Value` property did not change. The `DispatchingDown` guard in `TryDispatchToTarget` correctly prevents multi-level dispatch chains (Menu → MenuItem → FlagSelector → CheckBox), but `FlagSelector.OnActivated` silently did nothing when the source wasn't a CheckBox. OptionSelector had the same issue (documented in an existing TODO).

### Solution

Both `FlagSelector.OnActivated` and `OptionSelector.ApplyActivation` now use `Focused` (the currently focused inner CheckBox) as the toggle/selection target when `ctx.Routing == CommandRouting.DispatchingDown`. This is safe because `SetFocus()` is called before `OnActivated` in `DefaultActivateHandler`, so `Focused` reliably points to the correct inner CheckBox.

The `DispatchingDown` routing check prevents double-toggle: in the BubblingUp path (user clicks inner CheckBox), the source IS the CheckBox. In the Direct path (programmatic), `DispatchDown` already activated the CheckBox. Only in the `DispatchingDown` path was the inner CheckBox never reached.

### Files Modified

| File | Change |
|------|--------|
| `Terminal.Gui/Views/Selectors/FlagSelector.cs` | `OnActivated`: Focused fallback for DispatchingDown |
| `Terminal.Gui/Views/Selectors/OptionSelector.cs` | `ApplyActivation`: Focused fallback, removed TODO |
| `Tests/UnitTestsParallelizable/Views/MenuTests.cs` | Renamed 1 test, added 1 new test |

### Tests

- `Menu_InvokeActivate_With_Focus_FlagSelector_Toggles_Focused_CheckBox` (renamed from `_Value_Unchanged`, assertion flipped)
- `Menu_InvokeActivate_With_Focus_OptionSelector_Selects_Focused_Item` (new)
