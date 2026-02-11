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

1. Implement `IsBubblingDown` in `ICommandContext` and `CommandContext` and build and run tests to verify no breakage.
2. Implement `BubbleDown` method in `View` and modify `TryBubbleToSuperView` to check the flag. Build and run tests to verify no breakage.
3. Refactor `Shortcut` to use `BubbleDown` and remove dispatch methods. Build and run tests to verify no breakage.
4. Stop here. Do not proceed to refactor `SelectorBase` yet. We want to verify the new pattern in Shortcut first, which has more complex bubbling logic. Once verified, we can apply the same pattern to SelectorBase with confidence.

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
