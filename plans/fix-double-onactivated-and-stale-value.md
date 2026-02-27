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

## Fix

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

## Files to Modify

| File | Change |
|------|--------|
| `Terminal.Gui/ViewBase/View.Command.cs` | Add `DispatchState` flags enum; replace `_lastDispatchOccurred`; guard double-fire in both handlers; refresh value in `BubbleActivatedUp` and after `RaiseActivated` |

## Verification

1. **3 new ViewCommandTests pass:**
   - `OnActivated_Fires_Once_When_Originator_Is_DispatchTarget` — ActivatedCount=1, Value=1
   - `OnActivated_Fires_Once_When_Originator_Is_DispatchTarget_Direct` — ActivatedCount=1, Value=1 (already passes, keep passing)
   - `Bridge_Receives_Correct_Value_When_Originator_Is_DispatchTarget_Direct` — hostActivatedCount=1, capturedValue=1
   - `Bridge_Receives_Correct_Value_When_Originator_Is_DispatchTarget_WithBinding` — hostActivatedCount=1, capturedValue=1, valueChangeCount=1
2. **PopoverMenu tests pass:**
   - `Target_CheckBox_CommandView_Activate_Direct_Source_Reaches_Target_And_Value_Is_Correct`
   - `Target_CheckBox_CommandView_Activate_With_KeyBinding_Source_Reaches_Target_And_Value_Is_Correct` — update expected `valueChangeCount` from 2 to 1
3. **All ~14,300 parallel + ~985 non-parallel tests still pass** (run in both Debug and Release)
4. **ShortcutTests event ordering**: `BubbleUp_Activate_Event_Ordering` and `BubbleDown_Activate_Event_Ordering` must keep correct 4-event ordering
