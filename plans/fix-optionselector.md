# Fix OptionSelector (and FlagSelector) Failing Tests

## Problem

6 tests fail across OptionSelector and FlagSelector. The root cause is that `SelectorBase.OnActivating` now uses `BubbleDown` with a guard condition (`Binding is null`) that prevents programmatic `InvokeCommand` calls from bubbling down to checkboxes. The tests call `InvokeCommand(Command.Activate)` and `InvokeCommand(Command.HotKey)` directly (no binding), so the BubbleDown is skipped and checkboxes never update.

## Failing Tests

### OptionSelector (3 failures)

1. **`OptionSelector_Command_Activate_ForwardsToFocusedCheckBox`** - Calls `InvokeCommand(Command.Activate)` (no binding). Expects `true` (handled). Gets `false` because BubbleDown is skipped (no binding) and `Cycle()` runs but returns `false`.

2. **`OptionSelector_Command_HotKey_ForwardsToFocusedItem`** - Calls `InvokeCommand(Command.HotKey)`. Expects `true`. `DefaultHotKeyHandler` → `SelectorBase.OnHandlingHotKey` → `InvokeCommand(Command.Activate, args.Context)` → same issue as above.

3. **`HotKey_No_Value_Selects_First`** - `selector.NewKeyDownEvent(Key.G.WithAlt)` fires HotKey. `OptionSelector.OnHandlingHotKey` checks `!HasFocus && Value is null` → calls `RaiseActivating` → `SelectorBase.OnActivating` skips BubbleDown (context has binding but source is not `this`). Then sets `Value = Values[0]`. Test expects `MostFocused` to be the first checkbox, but focus isn't set correctly.

### FlagSelector (3 failures)

4. **`FlagSelector_Command_Activate_Changes_Value_And_Activates`** - Calls `InvokeCommand(Command.Activate)`. Expects `cbActivatingRaised == 1` and `selectorValueChanged == 1`. BubbleDown is skipped so checkbox never gets Activated.

5. **`FlagSelector_Command_HotKey_Changes_Value_And_Activates`** - Same as above via HotKey path.

6. **`HotKey_SetsFocus`** - `NewKeyDownEvent(Key.F.WithAlt)` fires HotKey. Expects `HasFocus == true`. Similar focus issue as OptionSelector #3.

## Architecture Context

### Command Flow: SelectorBase

```
SelectorBase.OnActivating(args)
  → Guard: Focused is null? → return false
  → Guard: args.Context?.Binding is null? → return false (PROBLEM: programmatic calls have no binding)
  → Guard: TryGetSource != this? → return false
  → BubbleDown(Focused, args.Context)
```

### Command Flow: OptionSelector

```
OptionSelector.OnActivating(args)
  → base.OnActivating(args)  // SelectorBase - may BubbleDown
  → If source is not a CheckBox → Cycle()
  → If source is a CheckBox → set Value, UpdateChecked
```

OptionSelector also subscribes to checkbox `Activating` events via `OnCheckboxOnActivating`, which calls `InvokeCommand(Command.Activate, args.Context)` back on the selector. This event handler was designed to work with the old save/restore pattern.

### BubbleDown Re-entry Prevention

The `Binding is null` guard prevents re-entry when:
1. `BubbleDown` invokes Activate on checkbox (Binding=null in BubbleDown context)
2. Checkbox fires `Activating` event
3. `OnCheckboxOnActivating` calls `InvokeCommand(Command.Activate, args.Context)` back on selector
4. Guard sees `Binding is null` → skips BubbleDown → no infinite loop

But this same guard blocks legitimate programmatic `InvokeCommand` calls in the tests.

### Key Files

| File | Role |
|------|------|
| `Terminal.Gui/Views/Selectors/SelectorBase.cs` | Base class with `OnActivating`, `OnHandlingHotKey`, `BubbleDown` call |
| `Terminal.Gui/Views/Selectors/OptionSelector.cs` | `OnActivating` override, `OnCheckboxOnActivating` event handler, `Cycle()` |
| `Terminal.Gui/Views/Selectors/FlagSelector.cs` | Uses `ValueChanged`/`ValueChanging` events (no Activating handler) |
| `Terminal.Gui/ViewBase/View.Command.cs` | `BubbleDown`, `TryBubbleToSuperView`, `DefaultActivateHandler`, `DefaultHotKeyHandler` |
| `Tests/UnitTestsParallelizable/Views/OptionSelectorTests.cs` | Failing tests |
| `Tests/UnitTestsParallelizable/Views/FlagSelectorTests.cs` | Failing tests |

### BubbleDown Method (View.Command.cs:649)

```csharp
protected bool? BubbleDown (View target, ICommandContext? ctx)
{
    CommandContext downCtx = new (ctx?.Command ?? Command.NotBound, ctx?.Source, null) { IsBubblingDown = true };
    return target.InvokeCommand (downCtx.Command, downCtx);
}
```

Creates context with: same Command, same Source, **Binding=null**, `IsBubblingDown=true`.

### TryBubbleToSuperView Guard (View.Command.cs:673)

```csharp
if (ctx?.IsBubblingDown == true)
{
    return handled;
}
```

Prevents commands dispatched via `BubbleDown` from bubbling back up.

## Analysis

The core tension: we need to distinguish between:
- **Programmatic `InvokeCommand`** (tests, `OnCheckboxOnActivating` re-entry) → should NOT trigger BubbleDown re-entry, but SHOULD allow the selector to function
- **User interaction** (keypress/mouse with Binding) → should BubbleDown to checkbox
- **BubbleDown re-entry** (from `OnCheckboxOnActivating` calling back) → must NOT BubbleDown again

The current `Binding is null` check conflates case 1 and case 3. We need a way to distinguish them. The `IsBubblingDown` flag on the context can distinguish case 3, since `BubbleDown` always sets it to `true`.

## Discrepancy Analysis: Comments vs. command.md Table vs. Code

Three sources describe OptionSelector/FlagSelector behavior. They disagree in several places.

### Sources

**OptionSelector.cs comments (lines 5-17):**
```
// DoubleClick - Focus, Select, and Accept the item under the mouse.
// Click - Focus, Select, and do NOT Accept the item under the mouse.
// CanFocus - Not Focused:
//  HotKey - Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - Focus item. If item is not active, make Active. Do NOT Accept.
// !CanFocus - Not Focused:
//  HotKey - Do NOT Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - Do NOT Focus item. If item is not active, make Active. Do NOT Accept.
// Focused:
//  Space key - If focused item is Active, move focus to and Activate next. Else, Activate current. Do NOT Accept.
//  Enter key - Activate and Accept the focused item.
//  HotKey - Restore Focus. Advance Active. Do NOT Accept.
//  Item HotKey - If item is not active, make Active. Do NOT Accept.
```

**FlagSelector.cs comments (lines 3-12):**
```
// DoubleClick - Focus, Select (Toggle), and Accept the item under the mouse.
// Click - Focus, Select (Toggle), and do NOT Accept the item under the mouse.
// Not Focused:
//  HotKey - Restore Focus. Do NOT change Active.
//  Item HotKey - Focus item. Activate (Toggle) item. Do NOT Accept.
// Focused:
//  Space key - Activate (Toggle) focused item. Do NOT Accept.
//  Enter key - Activate (Toggle) and Accept the focused item.
//  HotKey - No-op.
//  Item HotKey - Focus item, Activate (Toggle), and do NOT Accept.
```

**command.md table row:**
```
| OptionSelector | Forwards to SubView | Command.Accept | Forwards to SubView HotKey | Handled by SubViews | ...
| FlagSelector   | Forwards to SubView | Command.Accept | Forwards to SubView HotKey | Handled by SubViews | ...
```

### Discrepancy 1: HotKey - "Forwards to SubView HotKey" vs actual code

- **Table says**: "Forwards to SubView HotKey"
- **OptionSelector comment says**: "Restore Focus. Advance Active."
- **FlagSelector comment says**: "Restore Focus. Do NOT change Active." (and "No-op" when focused)
- **Code does**: `SelectorBase.OnHandlingHotKey` calls `InvokeCommand(Command.Activate, args.Context)` on **itself** (not on a SubView). `OptionSelector.OnHandlingHotKey` has extra logic for `!CanFocus` and `!HasFocus && Value is null`.
- **Verdict**: Table is wrong. HotKey does NOT forward to a SubView. It invokes Activate on the selector itself.

### Discrepancy 2: Enter - "Activate and Accept" vs code only does Accept

- **OptionSelector comment says**: "Enter key - Activate and Accept the focused item."
- **FlagSelector comment says**: "Enter key - Activate (Toggle) and Accept the focused item."
- **Table says**: `Command.Accept`
- **Code does**: Enter → `Command.Accept` → `SelectorBase.OnAccepting` (only checks DoubleClick, no Activate). No `Command.Activate` is invoked by Enter.
- **Verdict**: Comments are wrong (or aspirational). Code only does Accept on Enter, no Activate/Toggle. Table is correct.

### Discrepancy 3: Double-Activate on HotKey path

- **Code issue**: `SelectorBase.OnHandlingHotKey` calls `InvokeCommand(Command.Activate, args.Context)`. If this returns `false`, `OnHandlingHotKey` returns `false`, and `DefaultHotKeyHandler` continues to call `InvokeCommand(Command.Activate, ctx?.Binding)` — a **second** Activate invocation.
- **Comments say**: HotKey should "Advance Active" (once).
- **Verdict**: Potential double-Activate bug when `SelectorBase.OnHandlingHotKey`'s Activate doesn't return `true`.

### Discrepancy 4: FlagSelector HotKey "No-op" when focused vs code

- **FlagSelector comment says**: "HotKey - No-op" (when focused)
- **Code does**: `SelectorBase.OnHandlingHotKey` calls `InvokeCommand(Command.Activate, args.Context)` unconditionally. FlagSelector has no `OnHandlingHotKey` override, so it inherits SelectorBase's behavior which invokes Activate.
- **Verdict**: Comment says no-op but code invokes Activate. Either the comment is aspirational or code needs a FlagSelector override.

### Discrepancy 5: Space — "Forwards to SubView" oversimplifies

- **Table says**: "Forwards to SubView"
- **OptionSelector comment says**: "If focused item is Active, move focus to and Activate next. Else, Activate current."
- **Code does**: Space → `Command.Activate` → `SelectorBase.OnActivating` → may BubbleDown to checkbox → `OptionSelector.OnActivating` checks if source is CheckBox, then either Cycles or sets Value.
- **Verdict**: Table is an oversimplification. The selector does forward to the SubView (via BubbleDown) but then also has complex logic in `OptionSelector.OnActivating` involving Cycle. The comment is the most accurate.

### Discrepancy 6: DoubleClick — "Handled by SubViews" vs SelectorBase intercepts

- **Table says**: "Handled by SubViews"
- **Comment says**: "Focus, Select, and Accept the item under the mouse."
- **Code does**: DoubleClick on checkbox → checkbox raises Accept → bubbles to SelectorBase via `CommandsToBubbleUp`. `SelectorBase.OnAccepting` checks for DoubleClick and returns `!DoubleClickAccepts`. SelectorBase **intercepts** the Accept from the SubView.
- **Verdict**: Table is misleading. The SubView initiates the event, but SelectorBase intercepts and may block it.

### Summary Table

| Interaction | Comment | Table | Code | Who's Right? |
|-------------|---------|-------|------|--------------|
| Space | Cycle/Activate | Forwards to SubView | BubbleDown + Cycle logic | Comment most accurate |
| Enter | Activate + Accept | `Command.Accept` | Accept only (no Activate) | Table + Code agree; Comment wrong |
| HotKey | Restore Focus, Advance | Forwards to SubView HotKey | Invokes Activate on self | Comment most accurate; Table wrong |
| DoubleClick | Focus, Select, Accept | Handled by SubViews | SubView initiates, SelectorBase intercepts | Comment most accurate; Table misleading |
| Click | Focus, Select | Handled by SubViews | Checkbox handles click | All roughly agree |
| FlagSelector HotKey (focused) | No-op | Forwards to SubView | Invokes Activate on self | Code disagrees with Comment |

### Implications for Fixing

These discrepancies suggest the comments represent the **intended design** and should be treated as the spec. The code and table should be fixed to match. Key actions:

1. **Fix the command.md table** for OptionSelector/FlagSelector HotKey column — it should NOT say "Forwards to SubView HotKey"
2. **Decide**: Should Enter also Activate (as comments say) or just Accept (as code does)?
3. **Fix the double-Activate bug** in the HotKey path
4. **FlagSelector** needs either a "no-op HotKey when focused" override or the comment should be updated

## Proposed Fix

Replace the `Binding is null` guard with an `IsBubblingDown` check in `SelectorBase.OnActivating`:

```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    if (base.OnActivating (args))
    {
        return true;
    }

    if (args.Context?.IsBubblingDown == true || Focused is null || args.Context?.TryGetSource (out View? ctxSource) is not true || ctxSource != this)
    {
        return false;
    }

    BubbleDown (Focused, args.Context);

    return false;
}
```

This works because:
- **Programmatic `InvokeCommand(Command.Activate)`**: `IsBubblingDown` is false (default). Guard passes if source is `this`. But wait - programmatic calls have no context, so `TryGetSource` returns false → returns false (skips BubbleDown). The OptionSelector override then calls `Cycle()`. This matches the expected test behavior for `OptionSelector_Command_Activate_ForwardsToFocusedCheckBox` only if `Cycle()` returns true (handled). Need to verify.
- **User interaction**: `IsBubblingDown` is false. Source is `this`. BubbleDown fires.
- **Re-entry from `OnCheckboxOnActivating`**: The context passed was the BubbleDown context with `IsBubblingDown=true`. Guard catches it → returns false.

## Implementation Results

### Changes Made

1. **SelectorBase.OnActivating** (`SelectorBase.cs`):
   - Changed `IsBubblingDown` guard (replaced `Binding is null`)
   - Return `BubbleDown (...) is true` instead of always `false` — prevents OptionSelector from double-Cycling

2. **SelectorBase.OnHandlingHotKey** removed (was causing double-Activate bug)

3. **FlagSelector** (`FlagSelector.cs`):
   - Added constructor with `AddCommand(Command.HotKey, ...)` to replace `DefaultHotKeyHandler`
   - Custom handler calls `RaiseHandlingHotKey` (event fires), does `SetFocus` if not focused, skips Activate
   - Uses `AddCommand` instead of `OnHandlingHotKey` override so the `HandlingHotKey` event fires (AllViews test requires this)

4. **command.md table** updated:
   - OptionSelector HotKey: "Restores focus, advances Active" (was "Forwards to SubView HotKey")
   - FlagSelector HotKey: "Restores focus (no-op if focused)" (was "Forwards to SubView HotKey")
   - Section 7 note updated to describe BubbleDown and differing HotKey behavior

5. **Tests rewritten**:
   - `OptionSelector_Command_Activate_ForwardsToFocusedCheckBox` — verifies Value cycles from 0 to 1
   - `OptionSelector_Command_HotKey_ForwardsToFocusedItem` — verifies focus restored and Value cycles
   - `FlagSelector_Command_HotKey_WhenFocused_IsNoOp` (renamed) — verifies no value change, HandlingHotKey event fires, no Activate

### Test Results

- 13,869 of 13,870 parallelizable tests pass
- 1 pre-existing failure: `Menu_Command_HotKey_ActivatesMatchingItem` (unrelated to this work)
