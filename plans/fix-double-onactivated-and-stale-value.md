# Fix: Double OnActivated When Originator Is Dispatch Target + Stale Value in Bridge

## Context

Two related bugs in `View.Command.cs` when a state-mutating view (e.g., CheckBox/ToggleView) is the dispatch target of a relay composite (Shortcut/MenuItem):

### Bug 1: Double-Fire (WithBinding path)

When activation starts on the dispatch target with a binding whose source is the composite:

1. toggleView.DefaultActivateHandler (Direct) → RaiseActivating → TryBubbleUp → composite
2. composite.RaiseActivating → TryDispatchToTarget → **DispatchDown to toggleView**
3. toggleView.DefaultActivateHandler (DispatchingDown) → RaiseActivated ← **toggle #1**
4. Returns to step 1's DefaultActivateHandler → falls to line 532 → RaiseActivated ← **toggle #2**

Proven by: `OnActivated_Fires_Once_When_Originator_Is_DispatchTarget`, `Bridge_..._WithBinding`

### Bug 2: Stale Value in Bridge (Direct path, no binding)

When `InvokeCommand(Command.Activate)` is called with no binding:
- The relay guard (line 943) prevents dispatch, so no double-fire
- But `RefreshValue` at line 528 only reads from `GetDispatchTarget(ctx)` — toggleView has none
- `RaiseActivated` at line 532 triggers `OnActivated` which mutates Value (0→1)
- `BubbleActivatedUp` at line 539 carries the **stale** ctx (Value=0) up to composite
- Bridge on composite's Activated forwards Value=0 to host — **wrong**

Proven by: `Bridge_Receives_Correct_Value_When_Originator_Is_DispatchTarget_Direct`

## Plan (Original)

All changes in `Terminal.Gui/ViewBase/View.Command.cs`.

### Step 1: Add `DispatchState` flags enum and replace `_lastDispatchOccurred`

```csharp
[Flags]
private enum DispatchState
{
    None = 0,
    DispatchOccurred = 1,       // was _lastDispatchOccurred
    ActivatedViaDispatch = 2,   // set when RaiseActivated fires during DispatchingDown
    AcceptedViaDispatch = 4     // set when RaiseAccepted fires during DispatchingDown
}

private DispatchState _dispatchState;
```

Replace all `_lastDispatchOccurred` references:
- `= false` → `_dispatchState = DispatchState.None` (lines 270, 475, 916)
- `= true` → `_dispatchState |= DispatchState.DispatchOccurred` (lines 960, 971)
- reads → `_dispatchState.HasFlag (DispatchState.DispatchOccurred)` (lines 278, 479)

### Step 2: Guard the double-fire in DefaultActivateHandler

In `DefaultActivateHandler`, at line 532 (the unconditional `RaiseActivated`), set the flag when entering via DispatchingDown and guard the outer call:

**Before line 532**, when `ctx?.Routing == CommandRouting.DispatchingDown`:
```csharp
_dispatchState |= DispatchState.ActivatedViaDispatch;
```

**At line 532**, guard:
```csharp
if (!_dispatchState.HasFlag (DispatchState.ActivatedViaDispatch))
{
    RaiseActivated (ctx);
}
```

**Why this works:** The inner call (DispatchingDown) runs first, sets the flag, returns. The outer call (Direct) then checks the flag — it's set, so it skips the second `RaiseActivated`. The reset at line 475 happened before the inner call, not between inner and outer.

`BubbleActivatedUp` at line 539 fires regardless — composite ancestors need notification.

### Step 3: Refresh value in BubbleActivatedUp

In `BubbleActivatedUp` (line 582), before calling `next.RaiseActivated(upCtx)`, re-read the value from the ancestor's dispatch target. This fixes the stale-value bug for both the direct and binding paths:

```csharp
// In BubbleActivatedUp, before RaiseActivated:
if (next.GetDispatchTarget (ctx) is IValue refreshedValue)
{
    upCtx = ((CommandContext)upCtx).WithValue (refreshedValue.GetValue ());
}
```

Also refresh `ctx` for the originator itself after `RaiseActivated` fires at line 532, so that `BubbleActivatedUp` gets the fresh value even when the originator IS the IValue:

After `RaiseActivated` at line 532 (or the guarded block), re-read `this` as IValue:
```csharp
if (this is IValue selfValue)
{
    ctx = (ctx as CommandContext)?.WithValue (selfValue.GetValue ()) ?? ctx;
}
```

### Step 4: Apply same pattern for Accept path

In `DefaultAcceptHandler`:
- Before `RaiseAccepted` at line 321 (when `ctx?.Routing == DispatchingDown`): set `AcceptedViaDispatch`
- At line 321: guard with `!_dispatchState.HasFlag(AcceptedViaDispatch)`

### Step 5: Add Trace logging

Add `Trace.Command` calls:
- When `ActivatedViaDispatch`/`AcceptedViaDispatch` flag is set
- When the flag causes a skip of `RaiseActivated`/`RaiseAccepted`

## Implementation (What Was Actually Done)

Implemented in commits `053f75c` (logic) and `5f11a59` (cleanup). All changes in `Terminal.Gui/ViewBase/View.Command.cs` except one test expectation change.

### What matched the plan

Steps 1–5 were implemented as designed:

1. **`DispatchState` flags enum** replaced `_lastDispatchOccurred` exactly as specified. All boolean references were migrated to `HasFlag` checks.

2. **Double-fire guard** in `DefaultActivateHandler` implemented as specified: `DispatchingDown` sets `ActivatedViaDispatch`, outer call checks the flag and skips.

3. **Value refresh in `BubbleActivatedUp`** implemented with a scoping refinement (see below).

4. **Accept path** got the same `AcceptedViaDispatch` pattern.

5. **Trace logging** added for flag-set and skip events.

### Additional discoveries from tracing (deviations from plan)

Two issues were discovered by adding `Trace.Command` calls and observing the actual command flow:

#### Discovery 1: BubblingUp-phase early firing on plain views

**Problem:** The original code fired `RaiseActivated` on plain views (no dispatch target) during the BubblingUp phase of `RaiseActivating`. This happened *before* the originator completed its state change (e.g., before ToggleView's `OnActivated` incremented Value). When these plain ancestors had bridges (e.g., Menu → PopoverMenu), the bridge forwarded the stale pre-change value.

**Trace evidence:** The trace showed `RaiseActivated` firing on Menu during BubblingUp with Value=0, then the bridge forwarding that to PopoverMenu → host, all before the originator's `RaiseActivated` at line 532 which triggers the actual state mutation.

**Fix:** Removed the early `RaiseActivated` during BubblingUp for ALL views (not just composite ones). The BubblingUp handler now always returns `false` without firing `RaiseActivated`. Instead, ALL ancestors (both plain and composite) receive `Activated` later via `BubbleActivatedUp`, which runs after the originator completes its state change. This is the key difference from the plan, which only addressed composite views.

**Code change:**
```csharp
// BubblingUp handler — before:
if (ctx?.Routing == CommandRouting.BubblingUp)
{
    if (GetDispatchTarget (ctx) is null)
    {
        RaiseActivated (ctx);  // ← fired with stale value
    }
    return false;
}

// After:
if (ctx?.Routing == CommandRouting.BubblingUp)
{
    return false;  // No early firing — BubbleActivatedUp handles ALL ancestors
}
```

**Corresponding change:** `BubbleActivatedUp` was changed from `compositeOnly: true` to no filter, notifying all ancestors:
```csharp
// Before:
BubbleActivatedUp (ctx, compositeOnly: true);
// After:
BubbleActivatedUp (ctx);
```

#### Discovery 2: Self-value refresh scope

**Problem:** After `RaiseActivated`, the code re-reads `IValue.GetValue()` to capture post-change values. The plan suggested reading from `this` unconditionally. But tracing showed this was wrong for intermediate views in bridge chains: MenuBarItem (which implements IValue via MenuItem) would overwrite the original MenuItem value with its own Title ("Test" instead of "TestItem").

**Trace evidence:** When MenuItem "TestItem" activated, the command propagated through the bridge chain: MenuItem → Menu → PopoverMenu → MenuBarItem → MenuBar. Without scoping, MenuBarItem's self-value refresh would read `MenuBarItem.GetValue()` (returns Title="Test"), overwriting the original value ("TestItem").

**Fix:** The self-value refresh after `RaiseActivated` is guarded by a source-identity check:
```csharp
// Only refresh when this view IS the command source
if (this is IValue selfValue
    && ctx is CommandContext cc
    && ctx.Source?.TryGetTarget (out View? src) == true
    && ReferenceEquals (src, this))
{
    ctx = cc.WithValue (selfValue.GetValue ());
}
```

The same scoping applies in `BubbleActivatedUp` when refreshing from ancestors' dispatch targets:
```csharp
// Only refresh from dispatch target when it matches the command source
View? dispatchTarget = next.GetDispatchTarget (ctx);
if (dispatchTarget is IValue refreshedValue
    && ctx.Source?.TryGetTarget (out View? source) == true
    && ReferenceEquals (source, dispatchTarget))
{
    upCtx = upCtx.WithValue (refreshedValue.GetValue ());
}
```

### Additional fix: DefaultActivateHandler return value

**Problem:** `DefaultActivateHandler` previously returned `true` unconditionally. This prevented key propagation from continuing when Activate originated from a local key binding on a plain view (no dispatch target, no bubble config). This was inconsistent with `DefaultAcceptHandler` which returns `false` in that case to allow HotKey dispatch.

**Fix:** Changed to return a computed value:
```csharp
bool activateWillBubble = CommandWillBubbleToAncestor (Command.Activate);
return _dispatchState.HasFlag (DispatchState.DispatchOccurred) || activateWillBubble;
```

This mirrors `DefaultAcceptHandler`'s existing pattern and fixes #4759 (Space HotKeyBinding swallowed by focused view).

### Test changes

| Test | Change |
|------|--------|
| `InvokeCommands_Returns_True_If_No_Command_Handled` | Renamed to `InvokeCommands_Returns_False_If_No_Command_Handled` — plain views with no dispatch target and no bubble config now correctly return `false` |
| `Target_CheckBox_CommandView_Activate_With_KeyBinding_Source_Reaches_Target_And_Value_Is_Correct` | Updated expected `valueChangeCount` from 2 to 1 — the double-fire fix means CheckBox only toggles once |

## Files Modified

| File | Change |
|------|--------|
| `Terminal.Gui/ViewBase/View.Command.cs` | Add `DispatchState` flags enum; replace `_lastDispatchOccurred`; guard double-fire in both Activate and Accept handlers; defer all ancestor notification to `BubbleActivatedUp`; scope self-value refresh to command source only; fix return value for key propagation |
| `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs` | Rename test; update assertion for `false` return from plain view |
| `Tests/UnitTestsParallelizable/Views/PopoverMenuTests.cs` | Update `valueChangeCount` from 2 to 1 |

## Verification (Actual Results)

1. **4 ViewCommandTests pass:**
   - `OnActivated_Fires_Once_When_Originator_Is_DispatchTarget` — ActivatedCount=1, Value=1 ✅
   - `OnActivated_Fires_Once_When_Originator_Is_DispatchTarget_Direct` — ActivatedCount=1, Value=1 ✅
   - `Bridge_Receives_Correct_Value_When_Originator_Is_DispatchTarget_Direct` — hostActivatedCount=1, capturedValue=1 ✅
   - `Bridge_Receives_Correct_Value_When_Originator_Is_DispatchTarget_WithBinding` — hostActivatedCount=1, capturedValue=1, valueChangeCount=1 ✅
2. **PopoverMenu tests pass:**
   - `Target_CheckBox_CommandView_Activate_Direct_Source_Reaches_Target_And_Value_Is_Correct` ✅
   - `Target_CheckBox_CommandView_Activate_With_KeyBinding_Source_Reaches_Target_And_Value_Is_Correct` — `valueChangeCount` now 1 ✅
3. **MenuBar tests pass:**
   - `MenuBar_Activated_ContextValue_ContainsMenuItem` ✅
   - `MenuBar_Activated_ContextValue_WithFocusedMenuItem` ✅
4. **ShortcutTests event ordering preserved:**
   - `BubbleUp_Activate_Event_Ordering` ✅
   - `BubbleDown_Activate_Event_Ordering` ✅
5. **Full test suite: 14,344 parallel + 985 non-parallel — 0 failures** ✅
