# Terminal.Gui v2 Command System — Clean Slate Redesign (Synthesized)

## Problem Statement

The current v2 command system works, but it is hard to reason about and hard to evolve
safely. Behavior is distributed across `View.Command` default handlers, view-specific
overrides, keyboard/mouse binding layers, and special-case routing (`CommandsToBubbleUp`,
`DefaultAcceptView`, `DispatchDown`, boundary bridging). The result is high coupling, subtle
ordering dependencies, and fragile composite-view behavior.

This redesign is clean-slate. v2 is alpha, so compatibility is negotiable. Correctness,
clarity, and architectural durability take priority.

This document synthesizes the rigorous requirements from the "Terminal.Gui v2 Command
System Clean Slate Redesign" plan (R-001 through R-172) with a pragmatic, small-surface-area
design approach that maximizes code deletion in view classes while minimizing new abstractions.

---

# Part 1: Requirements

> Requirements R-001 through R-172 are adopted verbatim from the committed
> "Terminal.Gui v2 Command System Clean Slate Redesign.md" plan. They are the
> authoritative behavioral spec. Refer to that document for the full text.
>
> Key requirements called out below are ones that directly drive design decisions.

## Requirements That Drive Design Decisions

| Requirement | Design Impact |
|-------------|--------------|
| **R-004**: Activate/Accept semantically distinct | Preserved. No change needed. |
| **R-006**: Replace `bool?` with typed outcome | **New**: `CommandOutcome` enum |
| **R-007**: Immutable context for full route | **New**: `CommandContext` becomes immutable; views cannot mutate `ctx.Command` |
| **R-009**: Preserve source/binding provenance end-to-end | Preserved + **fix**: `ICommandBinding.Source` → `WeakReference<View>` |
| **R-011**: First-class router with deterministic phases | **Refined**: Phases integrated into existing `RaiseXxx` methods, not a separate CommandRouter object |
| **R-013**: Structural recursion protection | **New**: `CommandRouting` enum replaces ad-hoc boolean flags |
| **R-015**: Explicit bridge nodes for out-of-hierarchy boundaries | **New**: `CommandBridge` class |
| **R-023**: Composite controls declare delegation declaratively | **New**: `GetDispatchTarget` + `ConsumeDispatch` virtuals |
| **R-024**: Single gesture → single state mutation without hacks | Framework-enforced via dispatch + consume machinery |
| **R-035/R-036**: Command tracing, duplicate/cycle detection | **New**: Built-in route tracing in `RaiseXxx` methods |
| **R-045–R-048**: Migration explicit and bounded | Phased migration with backward-compat shims |

---

# Part 2: Design

## The Unifying Observation

Every view that participates in non-trivial command routing is doing one of these jobs:

### Pattern A: "I am one control made of parts" (Dispatch-to-Primary)

**Shortcut** has CommandView, HelpView, KeyView. Any interaction anywhere dispatches to
CommandView. There is exactly **one** primary target, fixed at construction.

### Pattern B: "I am a group of interactive items" (Dispatch-to-Focused)

**OptionSelector** has N CheckBoxes, one active (focused) at a time. Radio semantics —
the selector consumes the bubble and applies the value change itself.

**FlagSelector** same structure, but multi-select. Each CheckBox contributes a bit to the
composite Value.

### Pattern C: "I am a transparent container" (Relay)

**Bar** has N Shortcuts. It doesn't transform commands — it just declares
`CommandsToBubbleUp = [Accept, Activate]` and lets everything pass through.

**Menu** is the same as Bar but vertical, with one extra behavior: it signals
"close" when a leaf item is selected.

### Pattern D: "I own a view that isn't my subview" (Bridge)

**MenuItem** owns a **SubMenu**. **MenuBarItem** owns a **PopoverMenu** that is
registered with Application.Popover, outside the SuperView hierarchy.

### Pattern Summary

| | Target | Cardinality | Consume? | Boundary |
|-|--------|-------------|----------|----------|
| Shortcut | CommandView | 1, fixed | No (relay + dispatch) | Same tree |
| OptionSelector | Focused CheckBox | N, dynamic | Yes (prevents double-toggle) | Same tree |
| FlagSelector | Focused CheckBox | N, dynamic | Yes (toggles directly) | Same tree |
| Bar | (none — relay only) | N | No | Same tree |
| Menu | (relay + close signal) | N | No (but fires close) | Same tree |
| MenuItem→SubMenu | SubMenu | 1, set later | No | Detached |
| MenuBarItem→PopoverMenu | PopoverMenu | 1, set later | No | Cross-boundary |

---

## Design Changes

### Change 1: `CommandOutcome` Enum (replaces `bool?`)

**Satisfies**: R-006

```csharp
public enum CommandOutcome
{
    /// Command was not handled; routing continues.
    NotHandled,

    /// Command was handled; routing stops.
    HandledStop,

    /// Command was handled but routing may continue (notification semantics).
    HandledContinue,
}
```

Replaces the current three-valued `bool?` (`null` = not found, `false` = not handled,
`true` = handled). Every return site becomes self-documenting.

**Migration**: A `CommandOutcomeExtensions.ToBool()` shim bridges old code during transition.

**Files**: New `CommandOutcome.cs`, update `View.Command.cs` return types, update all
`AddCommand` handler signatures.

### Change 2: `CommandRouting` Enum (replaces two booleans)

**Satisfies**: R-013

```csharp
public enum CommandRouting
{
    /// Direct invocation (programmatic or from this view's own bindings).
    Direct,

    /// Command is propagating upward through the SuperView chain.
    BubblingUp,

    /// A SuperView is dispatching downward to a specific SubView.
    DispatchingDown,

    /// Command is crossing a non-containment boundary via CommandBridge.
    Bridged,
}
```

Single property on `ICommandContext` replaces `IsBubblingUp` and `IsBubblingDown`.
Four states (not three) — `Bridged` covers cross-boundary routing explicitly.

**Files**: `ICommandContext.cs`, `CommandContext.cs`, `View.Command.cs`

### Change 3: Immutable `CommandContext`

**Satisfies**: R-007, R-009, R-010

```csharp
public readonly record struct CommandContext : ICommandContext
{
    public required Command Command { get; init; }
    public required WeakReference<View>? Source { get; init; }
    public required ICommandBinding? Binding { get; init; }
    public CommandRouting Routing { get; init; }

    /// Creates a new context with a different command, preserving all other fields.
    public CommandContext WithCommand (Command command) => this with { Command = command };

    /// Creates a new context with different routing, preserving all other fields.
    public CommandContext WithRouting (CommandRouting routing) => this with { Routing = routing };
}
```

Context is a `readonly record struct` — no mutation after creation. Views that need
to change the command (e.g., Shortcut translating to TargetCommand) create a new
context via `WithCommand`.

**Fix**: `ICommandBinding.Source` changes from `View?` to `WeakReference<View>?` to
match `ICommandContext.Source` and prevent leaks. This is a known bug in the current system.

### Change 4: `GetDispatchTarget` + `ConsumeDispatch` (Composite Pattern)

**Satisfies**: R-023, R-024, R-025, R-027

```csharp
// On View:

/// Gets the subview to dispatch commands to. Return null to skip dispatch.
/// The framework calls this during RaiseActivating/RaiseAccepting after the
/// OnXxxing virtual and Xxxing event have had a chance to cancel.
protected virtual View? GetDispatchTarget (ICommandContext? ctx) => null;

/// If true, dispatching to the target consumes the command, preventing the
/// original subview from completing its own activation/acceptance.
/// If false (default), the dispatch is a relay and the original subview
/// completes normally.
protected virtual bool ConsumeDispatch => false;
```

**Framework behavior in `RaiseActivating` / `RaiseAccepting`**:

```
1. Create CommandEventArgs
2. Call OnActivating(args) — subclass can cancel
3. Fire Activating event — subscriber can cancel
4. If not handled:
   a. Call GetDispatchTarget(ctx)
   b. If target is non-null
      AND routing is not DispatchingDown (prevents re-entry)
      AND source is not within the target (prevents loops):
        - Dispatch to target with Routing = DispatchingDown
        - If ConsumeDispatch: mark as handled (target owns mutation)
5. TryBubbleUp (opt-in via CommandsToBubbleUp, unchanged)
6. If not handled: proceed to RaiseActivated/RaiseAccepted
   - If dispatch occurred: defer Xxxed until target's Xxxed fires
```

Step 6 is the key: the framework **defers** the container's post-event until the
dispatch target completes. This replaces Shortcut's `_activationBubbledUp` /
`CommandView_Activated` deferred-completion hack.

**View overrides**:

| View | `GetDispatchTarget` | `ConsumeDispatch` |
|------|--------------------|--------------------|
| Shortcut | `=> CommandView` | `false` (default) |
| OptionSelector | `=> Focused` | `true` |
| FlagSelector | `=> Focused` | `true` |
| Bar | not overridden (null) | not overridden |
| Menu | not overridden (null) | not overridden |
| Dialog | not overridden (null) | not overridden |

### Change 5: `CommandBridge` (Cross-Boundary Routing)

**Satisfies**: R-015, R-161

```csharp
public class CommandBridge : IDisposable
{
    // Both references are weak — the bridge must not prevent GC of either view.
    private readonly WeakReference<View> _owner;
    private readonly WeakReference<View> _remote;
    private readonly Command [] _commands;

    /// Connects owner to a remote view for the specified commands.
    /// Subscribes to remote's Activated/Accepted events and re-invokes
    /// commands on owner with fresh immutable CommandContexts preserving
    /// source + binding provenance. Routing is set to Bridged.
    public static CommandBridge Connect (
        View owner,
        View remote,
        params Command [] commands);

    /// Tears down subscriptions. Called automatically when either view disposes.
    public void Dispose ();
}
```

The bridge:
- Subscribes to the remote view's `Activated`/`Accepted` events
- On fire: creates a new `CommandContext` with `Routing = Bridged`, preserving source + binding
- Invokes the command on the owner via `InvokeCommand` (fires full CWP chain)
- Auto-disposes when either view is disposed or when remote is reassigned
- Becomes inert if either `WeakReference` target is collected

This replaces all manual event-subscription bridging in:
- `MenuBarItem` (OnPopoverMenuOnActivated, OnPopoverMenuOnAccepted)
- `PopoverMenu` (MenuActivating, MenuAccepted)
- `Menu.OnSubViewAdded` (dual Accepting+Activated wiring)

### Change 6: Explicit Menu Close Signal

**Satisfies**: R-136, R-158, R-159

```csharp
// On Menu:
public event EventHandler<CommandEventArgs>? ItemSelected;
```

Menu fires `ItemSelected` when any leaf MenuItem is activated or accepted.
PopoverMenu subscribes to `ItemSelected` → `Visible = false`.

This separates the "close the menu" concern from command routing. Currently
Menu conflates them by wiring `menuItem.Activated → RaiseAccepted`, which is
confusing (why does activation trigger acceptance?).

### Change 7: Route Tracing

**Satisfies**: R-035, R-036, R-037

```csharp
// On View:
[Conditional ("DEBUG")]
internal static void TraceRoute (
    View view,
    Command command,
    CommandRouting routing,
    CommandOutcome outcome,
    string phase);
```

Built into `RaiseActivating`, `RaiseAccepting`, `RaiseHandlingHotKey`, `DispatchDown`,
`TryBubbleUp`, and `CommandBridge`. Outputs structured log entries via `Logging.Trace`.

In DEBUG builds, also detects:
- Duplicate dispatch (same view receiving the same command twice in one route)
- Route cycles (A → B → A)

Traces are captured in unit tests via `Logging` infrastructure — no terminal rendering required.

### Change 8: `ICommandBinding.Source` → `WeakReference<View>?`

**Satisfies**: R-009

```csharp
// In ICommandBinding:
WeakReference<View>? Source { get; init; }  // was: View? Source
```

This fixes a known memory leak where bindings hold strong references to views.
The current system already uses `WeakReference<View>` for `ICommandContext.Source`
but inconsistently uses strong `View?` for `ICommandBinding.Source`.

Affects: `ICommandBinding`, `CommandBinding`, `KeyBinding`, `MouseBinding`.

---

## What Stays the Same

These parts of the current design work well and are preserved:

- **CWP pattern** (OnXxxing → Xxxing → OnXxxed → Xxxed) — maps to Preview → Execute → Notify phases
- **Opt-in bubbling** via `CommandsToBubbleUp` — no hidden defaults (R-016)
- **Command enum** (Activate, Accept, HotKey, NotBound) — stable, well-tested
- **DefaultAcceptView / IAcceptTarget** for Dialog redirect — works correctly
- **KeyBindings / HotKeyBindings split** — correct scoping model
- **InvokeCommand overloads** — public API preserved
- **CommandEventArgs with Handled property** — CWP cancellation mechanism
- **WeakReference source tracking** — already correct on context, now extended to bindings
- **InvokeCommand remains synchronous** (R-049) — no async, no deferred queueing

---

## Impact Assessment

| View | Changes Required |
|------|-----------------|
| **View.Command.cs** | Add `CommandOutcome`, `CommandRouting`, `GetDispatchTarget`, `ConsumeDispatch`, integrate dispatch into `RaiseActivating`/`RaiseAccepting`, add route tracing, refactor `TryBubbleUp` Padding checks |
| **ICommandContext / CommandContext** | Immutable `readonly record struct`, `Routing` enum, `WithCommand`/`WithRouting` methods |
| **ICommandBinding / KeyBinding / MouseBinding** | `Source` → `WeakReference<View>?` |
| **CommandBridge** | New class |
| **Shortcut** | Override `GetDispatchTarget → CommandView`. Delete ~120 lines (HandleActivate, deferred flags, IsWithinCommandView, CommandView_Activated, OnActivating, OnAccepting) |
| **OptionSelector** | Override `GetDispatchTarget → Focused`, `ConsumeDispatch → true`. Delete OnActivating (~23 lines). Simplify OnActivated. |
| **FlagSelector** | Override `GetDispatchTarget → Focused`, `ConsumeDispatch → true`. Delete OnActivating (~45 lines), `_suppressHotKeyActivate` flag. Simplify OnHandlingHotKey and OnActivated. |
| **Bar** | No change |
| **Menu** | Add `ItemSelected` event. Remove `OnSubViewAdded` dual event wiring. |
| **MenuItem** | No command changes |
| **MenuBarItem** | Replace manual event handlers with `CommandBridge.Connect` |
| **PopoverMenu** | Replace manual event subscriptions with `CommandBridge` + `ItemSelected` |
| **Dialog / Dialog\<TResult\>** | No change (already clean) |
| **Button** | No change |
| **CheckBox** | No change |
| **SelectorBase** | Update return types to `CommandOutcome`. Move Enter→Activate from `OnAccepting` to `OnAccepted`. |
| **All `AddCommand` handlers** | Return `CommandOutcome` instead of `bool?` |

---

## Code Samples

### A. CheckBox.cs (Redesigned)

CheckBox is a leaf view — no dispatch, no bridge, no relay. Barely changes.

```csharp
public class CheckBox : View, IValue<CheckState>
{
    public CheckBox ()
    {
        Width = Dim.Auto (DimAutoStyle.Text);
        Height = Dim.Auto (DimAutoStyle.Text, 1);
        CanFocus = true;

        // Single-click → Activate (toggle state)
        // Double-click → Accept (confirm without toggle)
        MouseBindings.Remove (MouseFlags.LeftButtonReleased);
        MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Activate);
        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);

        // Space → Activate and Enter → Accept inherited from View
        TitleChanged += Checkbox_TitleChanged;
        MouseHighlightStates = DefaultMouseHighlightStates;
    }

    // No GetDispatchTarget override (null — no subviews to dispatch to)
    // No ConsumeDispatch override (irrelevant — no dispatch)
    // No OnActivating/OnAccepting overrides (base View behavior is correct)

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);
        AdvanceCheckState ();
    }

    // State management (Value, AdvanceCheckState, ValueChanging/ValueChanged,
    // AllowCheckStateNone, RadioStyle, drawing) — all unchanged.
}
```

**What changed**: Only `AddCommand` handler return types change from `bool?` to
`CommandOutcome` (framework-wide migration). Zero command-routing logic changes.

---

### B. Shortcut.cs (Redesigned)

The entire `#region Accept/Activate/HotKey Command Handling` collapses.

```csharp
public class Shortcut : View, IOrientation, IDesignable
{
    public Shortcut (Key key, string? commandText, Action? action, string? helpText = null)
    {
        MouseHighlightStates = MouseState.In;
        CanFocus = true;
        Border?.Settings &= ~BorderSettings.Title;
        Width = GetWidthDimAuto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        _orientationHelper = new OrientationHelper (this);
        _orientationHelper.OrientationChanging += (_, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (_, e) => OrientationChanged?.Invoke (this, e);

        CommandsToBubbleUp = [Command.Activate, Command.Accept];

        // NOTE: No AddCommand (Command.Activate, HandleActivate).
        // The framework calls GetDispatchTarget and handles dispatch/deferred-completion
        // automatically via the default handlers.

        TitleChanged += Shortcut_TitleChanged;
        CommandView = new View { Width = Dim.Auto (), Height = Dim.Fill () };
        Title = commandText ?? string.Empty;
        HelpView.Text = helpText ?? string.Empty;
        // KeyView setup, GettingAttributeForRole wiring — unchanged ...

        Key = key;
        Action = action;
        ShowHide ();
    }

    // ──── Command Coordination (THE ENTIRE DISPATCH LOGIC) ────

    /// <summary>
    ///     Shortcut dispatches all commands to CommandView. The framework handles:
    ///     - Source guard (skip if source is already within CommandView)
    ///     - Programmatic guard (skip if no binding)
    ///     - Deferred completion (Shortcut.Activated fires after CommandView.Activated)
    /// </summary>
    protected override View? GetDispatchTarget (ICommandContext? ctx) => CommandView;

    // ConsumeDispatch defaults to false — CommandView completes its own activation
    // (e.g., CheckBox.OnActivated calls AdvanceCheckState).

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);
        Action?.Invoke ();
        InvokeOnTargetOrApp (ctx);
    }

    /// <inheritdoc/>
    protected override void OnAccepted (ICommandContext? ctx)
    {
        base.OnAccepted (ctx);
        Action?.Invoke ();
        InvokeOnTargetOrApp (ctx);
    }

    private void InvokeOnTargetOrApp (ICommandContext? ctx)
    {
        View? target = TargetView ?? GetTopSuperView ();

        if (target is { } && Command != Command.NotBound)
        {
            // Create new immutable context with the target command
            CommandContext targetCtx = ((CommandContext)ctx!).WithCommand (Command);
            target.InvokeCommand (Command, targetCtx);

            return;
        }

        if (!Key.IsValid || Command == Command.NotBound)
        {
            return;
        }

        App?.Keyboard.InvokeCommandsBoundToKey (Key);
    }

    // ──── DELETED (now handled by framework via GetDispatchTarget) ────
    //
    // - HandleActivate (~40 lines)
    // - IsWithinCommandView (~15 lines)
    // - _activationBubbledUp + _deferredActivationContext (flags)
    // - OnActivating override (~20 lines)
    // - OnAccepting override (~20 lines)
    // - CommandView_Activated (~18 lines)
    //
    // Total: ~120 lines of nuanced routing replaced by 1 override.

    // ──── Properties (unchanged) ────
    public View? TargetView { get; set; }
    public Command Command { get; set; } = Command.NotBound;
    public Action? Action { get; set; }
    public bool BindKeyToApplication { get; set; }

    // CommandView, HelpView, KeyView, Key, ShowHide, layout — all unchanged.
}
```

---

### C. OptionSelector.cs (Redesigned)

```csharp
public class OptionSelector : SelectorBase, IDesignable
{
    public OptionSelector () => base.Value = 0;

    // ──── Command Coordination ────

    /// Dispatch to whichever CheckBox has focus.
    protected override View? GetDispatchTarget (ICommandContext? ctx) => Focused;

    /// Consume: OptionSelector owns selection state, not the individual CheckBoxes.
    protected override bool ConsumeDispatch => true;

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);

        // Apply the value change. Runs for ALL activation paths uniformly.
        // No routing-direction check needed — the framework handled dispatch/consumption.
        ApplyActivation (ctx);
    }

    // ──── DELETED ────
    // - OnActivating override (~23 lines) — replaced by GetDispatchTarget + ConsumeDispatch

    private void ApplyActivation (ICommandContext? ctx)
    {
        // Unchanged from current implementation
        if (ctx?.Source?.TryGetTarget (out View? sourceView) != true
            || sourceView is not CheckBox checkBox)
        {
            Cycle ();

            return;
        }

        if (ctx.Binding is KeyBinding keyBinding
            && (int)checkBox.Data! == Value
            && keyBinding.Key is { }
            && keyBinding.Key == Key.Space)
        {
            Cycle ();
        }
        else
        {
            if (Value == (int)checkBox.Data!)
            {
                return;
            }

            Value = (int)checkBox.Data!;
        }
    }

    // OnSubViewAdded (RadioStyle=true), Cycle, UpdateChecked, FocusedItem — all unchanged.
}
```

---

### D. FlagSelector.cs (Redesigned)

```csharp
public class FlagSelector : SelectorBase, IDesignable
{
    public FlagSelector ()
    {
        KeyBindings.Remove (Key.Space);
        KeyBindings.Remove (Key.Enter);
        MouseBindings.Clear ();
    }

    // ──── Command Coordination ────

    /// Dispatch to whichever CheckBox has focus.
    protected override View? GetDispatchTarget (ICommandContext? ctx) => Focused;

    /// Consume: FlagSelector owns toggle semantics.
    protected override bool ConsumeDispatch => true;

    /// <inheritdoc/>
    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        if (base.OnHandlingHotKey (args))
        {
            return true;
        }

        // When focused, HotKey is a no-op
        if (HasFocus)
        {
            return true;
        }

        // Not focused: restore focus only. No _suppressHotKeyActivate flag needed —
        // DefaultHotKeyHandler calls InvokeCommand(Activate) without a binding,
        // so GetDispatchTarget dispatch is skipped by the framework's
        // programmatic-invoke guard (binding is null → no dispatch).
        if (CanFocus)
        {
            SetFocus ();
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);

        // Toggle the source CheckBox's value directly.
        // ConsumeDispatch=true means CheckBox.OnActivated/AdvanceCheckState was suppressed.
        if (ctx?.Source?.TryGetTarget (out View? source) == true && source is CheckBox checkBox)
        {
            checkBox.Value = checkBox.Value == CheckState.Checked
                                 ? CheckState.UnChecked
                                 : CheckState.Checked;
        }

        // CheckboxOnValueChanged handler updates FlagSelector.Value bitmask
    }

    // ──── DELETED ────
    // - _suppressHotKeyActivate flag
    // - OnActivating override (~45 lines, 4 code paths)
    // Total: ~50 lines replaced by GetDispatchTarget + ConsumeDispatch

    // OnSubViewAdded, OnCheckboxOnValueChanging, CheckboxOnValueChanged,
    // Value, UpdateChecked, CreateSubViews — all unchanged.
}
```

---

### E. Lines Changed Summary

| View | `GetDispatchTarget` | `ConsumeDispatch` | Lines Deleted | Lines Added |
|------|--------------------|--------------------|---------------|-------------|
| CheckBox | — | — | 0 | 0 |
| Shortcut | `=> CommandView` | `false` (default) | ~120 | 1 |
| OptionSelector | `=> Focused` | `true` | ~23 | 2 |
| FlagSelector | `=> Focused` | `true` | ~50 | 2 |
| MenuBarItem | — | — | ~30 | 1 (`CommandBridge.Connect`) |
| PopoverMenu | — | — | ~40 | 1 (`CommandBridge.Connect`) + `ItemSelected` sub |

**Leaf views don't change. Composite views replace N lines of hand-written routing
with 1–2 declarative overrides. Bridge views replace manual event wiring with
one `CommandBridge.Connect` call.**

---

## Migration Path

1. **Phase A — Foundation**: Add `CommandOutcome` enum alongside `bool?`. Add
   `CommandRouting` enum with backward-compat `IsBubblingUp`/`IsBubblingDown`
   computed properties. Make `CommandContext` a `readonly record struct` with
   `WithCommand`/`WithRouting` methods. Fix `ICommandBinding.Source` → `WeakReference<View>?`.
   *All existing tests continue to pass.*

2. **Phase B — Dispatch**: Add `GetDispatchTarget` / `ConsumeDispatch` to View.
   Integrate dispatch logic into `RaiseActivating`/`RaiseAccepting`. Migrate
   Shortcut first (most complex, best test coverage), then OptionSelector,
   then FlagSelector.
   *Shortcut/Selector tests validate correctness.*

3. **Phase C — Bridge**: Implement `CommandBridge`. Migrate MenuBarItem and
   PopoverMenu. Add `ItemSelected` to Menu. Simplify PopoverMenu close logic.
   *Menu/PopoverMenu tests validate correctness. Skipped CommandBubblingTests unblocked.*

4. **Phase D — Outcome**: Migrate all `AddCommand` handlers from `bool?` to
   `CommandOutcome`. Remove backward-compat `bool?` shims.
   *Full test suite validates.*

5. **Phase E — Cleanup**: Remove `IsBubblingUp`/`IsBubblingDown` compat properties.
   Add route tracing. Delete dead code. Update `docfx/docs/command.md`.

Each phase is independently shippable and testable.

---

## Verification

- All tests in ViewCommandTests, ShortcutTests.Command, BarTests, MenuTests,
  MenuBarTests, PopoverMenuTests, DialogTests, SelectorBaseTests must pass
- Skipped CommandBubblingTests (Phase 5 targets) must be unblocked by Phase C
- UICatalog scenarios (Shortcuts, Menus, Selectors, Dialogs, Bars) must work identically
- Route tracing (Phase E) must detect any duplicate dispatches in the test suite
- `dotnet test Tests/UnitTestsParallelizable --no-build`
- `dotnet test Tests/UnitTests --no-build`

---

## Resolved Decisions

1. **`GetDispatchTarget` is framework-driven.** Called from within `RaiseActivating`/
   `RaiseAccepting` (step 4 in the flow), not from view overrides. Views that need
   to skip dispatch return `null`. This keeps the dispatch logic in one place.

2. **Deferred completion is synchronous.** `DispatchDown` is already synchronous and
   returns after the target completes, so the framework fires `RaiseActivated`
   immediately after `DispatchDown` returns. No event-subscription machinery needed.

3. **`CommandBridge` is one-way.** Remote fires event → owner receives command.
   If bidirectional is needed, create two bridges.

---

## Implementation Status (Updated 2026-02-19)

### Completed

| Phase | Status | Commit | Notes |
|-------|--------|--------|-------|
| **A — Foundation** | Done | `be22792b2` | As planned. All types added, WeakRef migration complete. |
| **B — Dispatch** | Done | `bc48676af` | Significant deviations from plan — see below. |
| **C — Bridge** | Done | `9045e3050` | CommandBridge implemented. Only MenuBarItem migrated (pragmatic scope). |
| **D — Outcome** | **Deferred** | — | 267 `AddCommand` call sites across 28 files. Conversion shims exist for incremental migration. |
| **E — Cleanup** | Done | `4262441ca` | IsBubblingUp/IsBubblingDown removed. Route tracing not yet added. |

### Deviations from Plan

#### Phase B — Dispatch Deviations

1. **Shortcut still uses `CommandView_Activated` callback.**
   The plan said "No event-subscription machinery needed" for deferred completion, but this
   proved incorrect. When `IsBubblingUp`, the originator (CheckBox) hasn't called `RaiseActivated`
   yet when Shortcut's `DefaultActivateHandler` runs. `CommandView_Activated` fires AFTER the
   originator completes, ensuring correct ordering (CheckBox toggles before Shortcut.Action reads
   the value). The callback is simplified from the old version (no `_activationBubbledUp` flag).

2. **ConsumeDispatch=true does NOT DispatchDown for IsBubblingUp.**
   The plan's framework behavior said "Dispatch to target with Routing = DispatchingDown"
   unconditionally. In practice, for consume views (OptionSelector, FlagSelector), dispatching
   back to the originating CheckBox would cause double-toggle. The actual behavior:
   - IsBubblingUp → mark as consumed, NO DispatchDown (OnActivated handles state mutation)
   - Direct/programmatic → DispatchDown to target (forwards the command)

3. **Selectors dispatch Activate only, not Accept.**
   The plan showed `GetDispatchTarget` returning unconditionally. In practice, Accept must bubble
   normally through the Menu hierarchy (MenuItem → Menu → PopoverMenu → MenuBarItem → MenuBar).
   Selectors check `ctx?.Command != Command.Activate` and return null for non-Activate commands.

4. **Selectors use context-dependent dispatch targets.**
   The plan showed `GetDispatchTarget => Focused`. In practice, `Focused` is null in tests without
   Application.Init. Selectors now return the source CheckBox (from `ctx.Source`) for IsBubblingUp,
   and `Focused` for direct invocations.

5. **FlagSelector retains `_suppressHotKeyActivate`.**
   The plan said this flag was unnecessary because "DefaultHotKeyHandler calls InvokeCommand(Activate)
   without a binding." In practice, DefaultHotKeyHandler passes the original binding through
   (`ctx?.Binding`), so the binding guard doesn't suppress dispatch. The flag is checked in
   `GetDispatchTarget` and returns null to skip dispatch when set.

6. **Event ordering changed: Activating fires BEFORE dispatch.**
   The plan placed dispatch at step 4 (after OnActivating + Activating event). The old code
   dispatched in OnActivating (step 2, before the event). This means subscribers now get
   notified BEFORE dispatch occurs — they can cancel before the target is activated. Tests
   were updated to reflect the new ordering.

7. **Binding guard is ConsumeDispatch-dependent.**
   The plan said "skip dispatch for programmatic invocations (no binding)." This is only needed
   for relay dispatch (ConsumeDispatch=false, e.g., Shortcut) to prevent loops. Consume dispatch
   views (selectors) need programmatic invocations to dispatch so `InvokeCommand(Activate)`
   forwards to the focused item.

#### Phase C — Bridge Deviations

1. **Only MenuBarItem migrated, not Menu or PopoverMenu.**
   The plan called for replacing all manual event wiring in Menu.OnSubViewAdded and
   PopoverMenu.Root setter. These are same-tree (containment) wiring, not cross-boundary.
   CommandBridge is designed for cross-boundary routing (non-containment). The existing
   same-tree wiring works correctly and is lower risk to leave in place.

2. **ItemSelected event on Menu not added.**
   The plan proposed separating "close the menu" concern from command routing. The current
   pattern (menuItem.Activated → Menu.RaiseAccepted → PopoverMenu closes) works correctly.
   This can be added later as a separate improvement.

3. **`RaiseAccepted`/`RaiseActivated` made `internal protected`.**
   CommandBridge needs to call these from outside the View class hierarchy. The plan didn't
   address this access issue. Changed from `protected` to `internal protected`.

#### Phase D — Deferred

267 `AddCommand` handler signatures across 28 files need `bool?` → `CommandOutcome` migration.
This is purely mechanical with no behavioral change. The `CommandOutcomeExtensions.ToBool()`
and `ToOutcome()` shims enable incremental migration. Recommended approach: migrate one file
at a time with a sub-agent, verifying tests after each file.

#### Phase E — Partial

Route tracing (`TraceRoute` with `[Conditional("DEBUG")]`) not yet implemented. The
`IsBubblingUp`/`IsBubblingDown` removal is complete. `docfx/docs/command.md` not yet updated.

### Test Results (After Phase A-E)

- **UnitTestsParallelizable**: 13,980 passed, 0 failed, 22 skipped
- **UnitTests**: 1,001 passed, 0 failed, 22 skipped
- **Total**: 14,981 passed, 0 failed

### Test Results (After MenuBar/PopoverMenu Routing Fixes)

- **UnitTestsParallelizable**: 14,045 passed, 0 failed, 23 skipped
- All 12 pre-existing MenuBar/PopoverMenu test failures resolved
- See `finalizing-command-system.md` → [MenuBar/PopoverMenu Routing Fixes] for details
