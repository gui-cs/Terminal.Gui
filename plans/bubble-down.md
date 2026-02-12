# Design: Move "Bubble Down" from Shortcut into View

## Context

Shortcut and SelectorBase both implement a "bubble down" pattern - dispatching a command to a SubView with bubbling suppressed. Currently this involves manually saving/restoring `CommandsToBubbleUp` and constructing contexts, duplicated across views.

This is the inverse of "bubbling up" (`TryBubbleToSuperView` + `CommandsToBubbleUp`) and should be a first-class concept in View.

## Key Insight: Use `ICommandContext` to Prevent Re-entry

Instead of saving/restoring `CommandsToBubbleUp`, the context itself carries a flag indicating the command is "bubbling down." `TryBubbleToSuperView` checks this flag and skips bubbling. This eliminates the save/restore pattern entirely.

## Proposed Design

### 1. Add `IsBubblingDown` to `ICommandContext`

**File:** `Terminal.Gui/Input/ICommandContext.cs`

```csharp
public interface ICommandContext
{
    Command Command { get; set; }
    WeakReference<View>? Source { get; set; }
    ICommandBinding? Binding { get; }
    bool IsBubblingDown { get; }  // NEW
}
```

**File:** `Terminal.Gui/Input/CommandContext.cs`

```csharp
public record struct CommandContext : ICommandContext
{
    // ... existing members ...
    public bool IsBubblingDown { get; init; }  // NEW - default false
}
```

### 2. Add `BubbleDown` helper to `View`

**File:** `Terminal.Gui/ViewBase/View.Command.cs`

```csharp
protected bool? BubbleDown (View target, ICommandContext? ctx)
{
    CommandContext downCtx = new (ctx?.Command ?? Command.NotBound, ctx?.Source, null)
    {
        IsBubblingDown = true
    };

    return target.InvokeCommand (downCtx.Command, downCtx);
}
```

This method:
- Creates a new context with `IsBubblingDown = true` and no binding
- Invokes the command on the target
- Because `IsBubblingDown` is true, `TryBubbleToSuperView` in the target's Raise method will skip bubbling

### 3. Modify `TryBubbleToSuperView` to check the flag

**File:** `Terminal.Gui/ViewBase/View.Command.cs`

At the top of `TryBubbleToSuperView`, after the `handled` check:

```csharp
if (ctx?.IsBubblingDown == true)
{
    return handled;
}
```

This replaces the need to save/restore `CommandsToBubbleUp`.

### 4. Simplify Shortcut

**File:** `Terminal.Gui/Views/Shortcut.cs`

**Remove:**
- `DispatchCommandFromSubview` method
- `DispatchCommandFromSelf` method

**Simplify `OnActivating` and `OnAccepting`:**

```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    if (base.OnActivating (args))
    {
        return true;
    }

    // If command didn't originate from CommandView, bubble down so it can update state
    if (!IsFromCommandView (args.Context!))
    {
        BubbleDown (CommandView, args.Context);
    }

    return false;
}
```

Same pattern for `OnAccepting`.

**Keep:** `IsFromCommandView` - still needed to avoid double-processing when a command bubbles up FROM CommandView. The CommandView already processed the command; bubbling it down again would cause double-processing (e.g., CheckBox toggling twice).

**Can remove:** `IsBindingFromSelf`, `IsBindingFromKeyView`, `IsBindingFromHelpView` - the simplified check only needs "is this from CommandView?" (skip bubble-down) vs "anything else" (bubble down).

### 5. Simplify SelectorBase

**File:** `Terminal.Gui/Views/Selectors/SelectorBase.cs`

Replace the manual save/restore in `OnActivating`:

```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    if (base.OnActivating (args))
    {
        return true;
    }

    if (Focused is null || args.Context?.TryGetSource (out View? ctxSource) is not true || ctxSource != this)
    {
        return false;
    }

    // Bubble DOWN to the focused checkbox
    BubbleDown (Focused, args.Context);

    return false;
}
```

## Flow Comparison

### Before (Shortcut.DispatchCommandFromSelf)

```
1. Save CommandsToBubbleUp
2. Set CommandsToBubbleUp = []
3. Create new CommandContext(command, null, null)
4. target.InvokeCommand(command, context)
5. Restore CommandsToBubbleUp
```

### After (View.BubbleDown)

```
1. Create new CommandContext(command, source, null) { IsBubblingDown = true }
2. target.InvokeCommand(command, context)
```

## Phases

1. [DONE] Implement `IsBubblingDown` in `ICommandContext` and `CommandContext` and build and run tests to verify no breakage.
2. [DONE] Implement `BubbleDown` method in `View` and modify `TryBubbleToSuperView` to check the flag. Build and run tests to verify no breakage.
3. [DONE] Refactor `Shortcut` to use `BubbleDown` and remove dispatch methods. Build and run tests to verify no breakage.
4. Stop here. Do not proceed to refactor `SelectorBase` yet. We want to verify the new pattern in Shortcut first, which has more complex bubbling logic. Once verified, we can apply the same pattern to SelectorBase with confidence.

## Implementation Notes

### Shortcut Simplification Details

The plan's suggested `!IsFromCommandView` check was too broad. The actual condition for bubbling down is:
- Must have a `Binding` (user interaction, not programmatic `InvokeCommand`)
- `Binding.Source` must NOT be the `CommandView` (to avoid double-processing when CommandView's event bubbles up)

This condition naturally handles all cases:
- User clicks Shortcut surface → Binding.Source = Shortcut → bubble down ✓
- User clicks HelpView/KeyView → Binding.Source = HelpView/KeyView → bubble down ✓
- CommandView mouse click bubbles up → Binding.Source = CommandView → skip ✓
- CommandView HotKey triggers Activate → Binding = null → skip ✓
- Direct `shortcut.InvokeCommand()` → Binding = null → skip ✓

This eliminated the need for `IsFromCommandView`, `IsBindingFromSelf`, `IsBindingFromKeyView`, and `IsBindingFromHelpView` entirely.

## Files to Modify

| File | Change |
|------|--------|
| `Terminal.Gui/Input/ICommandContext.cs` | Add `IsBubblingDown` property |
| `Terminal.Gui/Input/CommandContext.cs` | Add `IsBubblingDown` property |
| `Terminal.Gui/ViewBase/View.Command.cs` | Add `BubbleDown` method; modify `TryBubbleToSuperView` |
| `Terminal.Gui/Views/Shortcut.cs` | Remove dispatch methods; simplify `OnActivating`/`OnAccepting` |
| `Terminal.Gui/Views/Selectors/SelectorBase.cs` | Simplify `OnActivating` |

## Verification

1. `dotnet build --no-restore`
2. `dotnet test Tests/UnitTestsParallelizable --no-build` - especially Shortcut and Selector tests
3. `dotnet test Tests/UnitTests --no-build`
4. Verify in UICatalog: Shortcut with CheckBox CommandView toggles correctly on click
5. Verify OptionSelector/FlagSelector respond to clicks and keyboard

## Next Steps

### 5. [DONE] Add BubbleDown Tests to ViewCommandTests (Phase 5)

Added 11 View-level tests for `BubbleDown` in `ViewCommandTests.cs` using a
`BubbleDownTestView` helper class. All 70 ViewCommandTests pass (59 existing + 11 new).

**File:** `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs`

Add a new `#region BubbleDown Tests` section with the following tests. Each test
uses plain `View` instances (and a simple `BubbleDownTestView` subclass that
exposes `BubbleDown` since it is `protected`), avoiding any dependency on
Shortcut, CheckBox, or other concrete views.

#### Test helper

```csharp
private class BubbleDownTestView : View
{
    public bool? TestBubbleDown (View target, ICommandContext? ctx) => BubbleDown (target, ctx);
}
```

#### Tests to add

| Test Name | What it verifies |
|-----------|-----------------|
| `BubbleDown_InvokesCommandOnTarget` | Calling `BubbleDown(subView, ctx)` invokes the command on the target and returns the target's result. |
| `BubbleDown_SetsIsBubblingDown_True` | The `ICommandContext` received by the target's command handler has `IsBubblingDown == true`. |
| `BubbleDown_ClearsBinding` | The context received by the target has `Binding == null` (no binding carried from the original). |
| `BubbleDown_PreservesSource` | The context received by the target has the same `Source` as the original context. |
| `BubbleDown_PreservesCommand` | The context received by the target has the same `Command` as the original context. |
| `BubbleDown_UsesNotBound_WhenCtxIsNull` | When `ctx` is `null`, `BubbleDown` uses `Command.NotBound`. |
| `BubbleDown_Target_DoesNotBubbleUp` | When the target is a SubView of a SuperView with `CommandsToBubbleUp` set, the command does NOT bubble to the SuperView because `IsBubblingDown` is true. This is the core invariant. |
| `BubbleDown_Target_DoesNotBubbleUp_Accept` | Same as above but for `Command.Accept` specifically, verifying that `DefaultAcceptView` is also skipped. |
| `BubbleDown_Target_DoesNotBubbleUp_DeepHierarchy` | The bubble-down flag prevents bubbling at every level of a 3-level hierarchy. |
| `TryBubbleToSuperView_SkipsWhenIsBubblingDown` | Directly verifies that `TryBubbleToSuperView` returns `handled` (not `true`) when `IsBubblingDown` is true, even when `CommandsToBubbleUp` contains the command. |
| `BubbleDown_Then_NormalInvoke_BubblesNormally` | After a `BubbleDown` call completes, a subsequent normal `InvokeCommand` on the same target bubbles normally (no "sticky" state). |

#### Shortcut tests to keep as-is

The existing Shortcut tests in `ShortcutTests.cs` that exercise BubbleDown indirectly
(e.g., `CheckBox_CanFocus_False_Direct_InvokeCommand_Does_Not_Change_State`,
`CommandView_Command_Activate_Bubbles_To_Shortcut`, etc.) should remain as integration-level
tests that verify the Shortcut-specific policy of *when* to call `BubbleDown`. The new
ViewCommandTests verify the *mechanism* itself.

### 6. Refactor SelectorBase to use BubbleDown (Phase 6)

Once Shortcut is verified working in UICatalog, apply the same pattern to SelectorBase.

### 7. Update Deep Dive Documentation

Update the conceptual documentation to describe `BubbleDown` and `IsBubblingDown`:

**File:** `docfx/docs/command.md`
- Document `BubbleDown` as the inverse of `TryBubbleToSuperView`
- Explain `IsBubblingDown` flag and how it prevents re-entry
- Add flow diagrams showing bubble-up vs bubble-down patterns
- Document when to use `BubbleDown` (SuperView dispatching to SubView with bubbling suppressed)

**File:** `docfx/docs/View.md`
- Update the command propagation section to mention `BubbleDown`
- Add `BubbleDown` to the API reference table alongside `CommandsToBubbleUp` and `TryBubbleToSuperView`

**File:** `docfx/docs/shortcut.md` (if exists, or relevant scenario docs)
- Update Shortcut architecture docs to reflect the simplified dispatch pattern
- Remove references to `DispatchCommandFromSelf`/`DispatchCommandFromSubview`
