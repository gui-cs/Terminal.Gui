# Bug: Bridge-based Activating Cancellation Cannot Prevent Originator State Change

**Source:** `PopoverMenus.cs` line 192 BUGBUG comment
**Tests:** `ViewCommandTests.Bridge_Ancestor_Cancel_OnActivating_Does_Not_Prevent_Originator_State_Change`
**Contrast:** `ViewCommandTests.Direct_Ancestor_Cancel_OnActivating_Prevents_Originator_State_Change`

## Summary

When a `CommandBridge` relays an `Activated` event from a remote view to a bridge owner, and the owner (or its ancestor) cancels via `OnActivating` / `Activating`, the originator's state has already changed. The cancellation is too late because the bridge fires from the **post-event** (`Activated`), not the **pre-event** (`Activating`).

In contrast, direct containment bubbling (via `TryBubbleUp`) fires during `RaiseActivating` — the pre-event phase — so cancellation at an ancestor **does** prevent the originator's `OnActivated` from firing.

## Topology

```
BRIDGE PATH (cancellation broken):

  ancestor (Activating → cancel)
    └── owner  ← Bridge ←  container (CommandsToBubbleUp=[Activate])
                               └── toggleView (IValue, mutates in OnActivated)

DIRECT PATH (cancellation works):

  ancestor (Activating → cancel)
    └── toggleView (IValue, mutates in OnActivated)
```

## Root Cause Analysis (from trace logs)

### Bridge Path — Cancellation Too Late

```
toggleView.InvokeCommand(Activate)
  DefaultActivateHandler @toggleView
    RaiseActivating @toggleView
      TryBubbleUp → BubblingUp to container
        DefaultActivateHandler @container (BubblingUp → returns false)
    ← RaiseActivating returns false (not handled)
  ← Direct path: SetFocus + RaiseActivated
    RaiseActivated @toggleView → OnActivated → Value++ ← STATE CHANGES HERE (Value=0→1)
    BubbleActivatedUp @toggleView
      RaiseActivated @container                              ← container.Activated fires
        Bridge catches → owner.InvokeCommand(Activate, Bridged)
          DefaultActivateHandler @owner
            RaiseActivating @owner
              TryBubbleUp → BubblingUp to ancestor
                DefaultActivateHandler @ancestor
                  RaiseActivating @ancestor
                    Activating event → CANCEL               ← TOO LATE: Value already 1
```

**Key trace lines confirming the bug:**

```
[RaiseActivated] @toggleView        - Value=0     ← OnActivated fires, Value changes to 1
[RaiseActivated] @container         - Value=1     ← Bridge fires from here
[Bridge]         @owner             - Value=1     ← Value already 1 crossing the bridge
[RaiseActivating] @ancestor         - Value=1     ← Ancestor sees Value=1, cancellation is too late
```

### Direct Path — Cancellation Works

```
toggleView.InvokeCommand(Activate)
  DefaultActivateHandler @toggleView
    RaiseActivating @toggleView
      TryBubbleUp → BubblingUp to ancestor
        DefaultActivateHandler @ancestor (BubblingUp)
          RaiseActivating @ancestor
            Activating event → CANCEL                       ← WORKS: Value still 0
    ← RaiseActivating returns true (handled)
  ← Dispatch not occurred → return true (NO RaiseActivated, NO OnActivated)
```

**Key trace lines:**

```
[RaiseActivating] @ancestor         - Value=0     ← Ancestor cancels while Value is still 0
                                                    ← toggleView.OnActivated NEVER fires
```

## Why This Happens

The asymmetry comes from **where** in the pipeline each mechanism fires:

| Mechanism | Fires During | Phase | Can Cancel? |
|-----------|-------------|-------|-------------|
| `TryBubbleUp` (direct containment) | `RaiseActivating` | **Pre-event** | Yes — cancellation prevents `OnActivated` |
| `CommandBridge` | `Activated` event handler | **Post-event** | No — `OnActivated` already fired |
| `BubbleActivatedUp` | After `RaiseActivated` | **Post-event** | No — only fires `RaiseActivated` (no `RaiseActivating`) |

`CommandBridge` subscribes to `remote.Activated` (line 48 of `CommandBridge.cs`). By the time `Activated` fires, the full lifecycle on the remote side is complete:

1. `RaiseActivating` (pre-event) ✓
2. `OnActivated` (state change) ✓  ← Already happened
3. `Activated` event ← Bridge fires from here
4. Bridge → `owner.InvokeCommand` → `owner.RaiseActivating` ← Cancellation attempt, too late

## Possible Fix Approaches

### Option A: Bridge fires from `Activating` too (two-phase bridge)

Add a second bridge subscription to `remote.Activating`. When the remote's `Activating` fires, the bridge would call the owner's `InvokeCommand` in a "tentative" mode. If the owner cancels, the bridge would set `args.Handled = true` on the remote's `Activating`, preventing `OnActivated` from firing on the remote side.

**Pros:** Correct cancellation semantics across the bridge.
**Cons:** Complex — requires `Activating` to support external cancellation feedback, and the bridge would need to manage two-phase coordination. `Activating` currently uses `CommandEventArgs` (not `EventArgs<ICommandContext?>`), so the bridge subscription shape would differ. Risk of re-entry loops.

### Option B: Accept the limitation, document it

The bridge is designed for **notification**, not cancellation. The `Activating` pre-event is fundamentally a local concern — the originator and its direct containment ancestors can veto. But once the command crosses a non-containment boundary (bridge), the remote side has already committed.

**Pros:** No code changes, simple mental model.
**Cons:** The BUGBUG in PopoverMenus.cs remains; developers may be surprised that `OnActivating` on the bridge owner can't prevent state changes on the remote side.

### Option C: Bridge fires from `Activating` with cancel-back

Similar to Option A, but more targeted:

1. Bridge subscribes to `remote.Activating` in addition to `remote.Activated`.
2. On `remote.Activating`, bridge calls `owner.RaiseActivating(bridgedCtx)`.
3. If owner's `RaiseActivating` returns `true` (cancelled), bridge sets `args.Cancel = true` on the remote's `Activating` args.
4. Remote's `OnActivated` never fires → state change prevented.
5. Bridge still subscribes to `remote.Activated` for the post-event notification (unchanged).

**Pros:** Clean semantics — cancellation propagates across the bridge boundary.
**Cons:** Requires `CommandEventArgs` to support a `Cancel` property (or reuse `Handled`). The bridge would need to avoid double-invoking the owner (once from `Activating`, once from `Activated`).

### Option D: Revert-on-cancel pattern

Instead of preventing the state change, allow it to happen but revert it if cancelled:

1. Bridge fires normally from `Activated`.
2. Owner's `OnActivating` returns `true` (cancel).
3. The bridge (or framework) detects the cancellation and fires a "revert" command on the remote side.

**Pros:** No changes to the pre-event pipeline.
**Cons:** Not all state changes are easily reversible. Adds complexity.

## Recommendation

**Option C** provides the cleanest fix if the design intent is that `OnActivating` should be a universal veto point. However, it requires careful handling to avoid re-entry and double-fire issues.

**Option B** is acceptable if the bridge is documented as a notification-only mechanism where cancellation is not supported. The BUGBUG comment in `PopoverMenus.cs` should be updated to explain this limitation.

## Files

| File | Role |
|------|------|
| `Terminal.Gui/Input/CommandBridge.cs` | Bridge implementation — subscribes to `Activated` only |
| `Terminal.Gui/ViewBase/View.Command.cs` | `DefaultActivateHandler`, `BubbleActivatedUp`, `RaiseActivating` |
| `Examples/UICatalog/Scenarios/PopoverMenus.cs:192` | Original BUGBUG comment |
| `Tests/.../ViewCommandTests.cs` | Two new tests reproducing and contrasting the bug |
