# Terminal.Gui v2 Command System — Definitive Reference

## Status: Re-engineered for v2 Beta

The command system has been re-engineered across Phases A-E of the command system redesign plus
targeted bug fixes for MenuBar/PopoverMenu routing. All parallelizable and non-parallelizable
tests pass (14,045 + ~1,001). This document is the definitive reference for the system as shipped.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Core Types](#core-types)
3. [Command Lifecycle](#command-lifecycle)
4. [Default Handlers](#default-handlers)
5. [Dispatch Patterns (Composite Views)](#dispatch-patterns-composite-views)
6. [Command Bubbling](#command-bubbling)
7. [CommandBridge (Cross-Boundary Routing)](#commandbridge-cross-boundary-routing)
8. [Keyboard Integration](#keyboard-integration)
9. [Route Tracing](#route-tracing)
10. [View-Specific Behaviors](#view-specific-behaviors)
11. [Design Invariants](#design-invariants)
12. [Completed Work](#completed-work)
13. [Future Work](#future-work)

---

## Architecture Overview

The command system routes user interactions (keyboard, mouse, programmatic) through a hierarchy
of Views using a structured pipeline. Every View gets four default command handlers registered
at construction. Commands flow through a two-phase notification model (cancellable preview +
non-cancellable completion), with opt-in bubbling up the SuperView chain and declarative
dispatching down to composite SubViews.

### Key Principles

1. **Two-phase notifications** — Cancellable phase (`Activating`/`Accepting`/`HandlingHotKey`)
   followed by completion phase (`Activated`/`Accepted`/`HotKeyCommand`)
2. **Opt-in bubbling** — Commands only bubble when SuperView declares them in `CommandsToBubbleUp`
3. **Declarative dispatch** — Composite views override `GetDispatchTarget` + `ConsumeDispatch`
   instead of writing custom routing logic
4. **Immutable context** — `CommandContext` is a `readonly record struct`; views create modified
   copies via `WithCommand`/`WithRouting`
5. **Weak references** — Source tracking uses `WeakReference<View>` to prevent memory leaks
6. **Synchronous execution** — All command processing is synchronous; no async or deferred queueing

### Return Value Convention

All `InvokeCommand` and handler methods return `bool?`:

| Value | Meaning | Effect |
|-------|---------|--------|
| `null` | No handler found or not raised | Input processing continues |
| `false` | Event raised but not handled/cancelled | Processing continues |
| `true` | Event raised and handled/cancelled | Processing stops |

> **Future**: `CommandOutcome` enum (`NotHandled`, `HandledStop`, `HandledContinue`) exists with
> conversion shims (`ToBool()`/`ToOutcome()`) for incremental migration. See [Phase D](#phase-d-commandoutcome-migration).

---

## Core Types

### Command Enum (`Terminal.Gui/Input/Command.cs`)

| Command | Purpose |
|---------|---------|
| `NotBound` | No handler bound; invokes `RaiseCommandNotBound` |
| `Accept` | Accepts current view state (confirm, submit) |
| `Activate` | Activates a view/item (toggle, select, focus) |
| `HotKey` | Handles hot key press (focus + activate) |
| *(plus movement, editing, navigation, action commands)* | |

### CommandRouting Enum (`Terminal.Gui/Input/CommandRouting.cs`)

| Value | Meaning | Guards |
|-------|---------|--------|
| `Direct` | Programmatic invocation or from view's own bindings | Default |
| `BubblingUp` | Propagating upward through SuperView chain | Blocks SetFocus in Activate handler |
| `DispatchingDown` | SuperView dispatching to specific SubView | Blocks re-entry and bubbling on target |
| `Bridged` | Crossing non-containment boundary via CommandBridge | Blocks dispatch-down (bridge brings UP) |

### CommandContext (`Terminal.Gui/Input/CommandContext.cs`)

```csharp
public readonly record struct CommandContext : ICommandContext
{
    public required Command Command { get; init; }
    public required WeakReference<View>? Source { get; init; }
    public required ICommandBinding? Binding { get; init; }
    public CommandRouting Routing { get; init; }

    public CommandContext WithCommand (Command command) => this with { Command = command };
    public CommandContext WithRouting (CommandRouting routing) => this with { Routing = routing };
}
```

### CommandOutcome Enum (`Terminal.Gui/Input/CommandOutcome.cs`)

```csharp
public enum CommandOutcome
{
    NotHandled,      // Routing continues
    HandledStop,     // Routing stops
    HandledContinue, // Handled but routing may continue (notification semantics)
}
```

Conversion shims in `CommandOutcomeExtensions` bridge `bool?` ↔ `CommandOutcome`.

### CommandBridge (`Terminal.Gui/Input/CommandBridge.cs`)

See [CommandBridge section](#commandbridge-cross-boundary-routing).

---

## Command Lifecycle

### Registration (View Constructor)

Every View calls `SetupCommands()` at construction:

```csharp
private void SetupCommands ()
{
    AddCommand (Command.Activate, DefaultActivateHandler);
    AddCommand (Command.Accept, DefaultAcceptHandler);
    AddCommand (Command.HotKey, DefaultHotKeyHandler);
    AddCommand (Command.NotBound, DefaultCommandNotBoundHandler);
}
```

Views can override handlers via `AddCommand (Command.Xxx, myHandler)`.

### Invocation Flow

```
InvokeCommand (command, binding/context)
  │
  ├─ Look up handler in _commandImplementations dictionary
  │
  ├─ If found: call handler (e.g., DefaultActivateHandler)
  │  └─ Handler runs the CWP pipeline (see Default Handlers)
  │
  └─ If not found: return null (no handler)
```

### CWP (Cancellable Work Pattern) Pipeline

Each default handler follows this pattern:

```
1. Reset _lastDispatchOccurred = false
2. RaiseXxxing (cancellable phase):
   a. Call OnXxxing (args)          — virtual, subclass can cancel
   b. Fire Xxxing event             — subscriber can cancel
   c. TryDispatchToTarget (ctx)     — composite dispatch
   d. TryBubbleUp (ctx, handled)    — opt-in bubbling
3. If cancelled → return (no completion phase)
4. Completion work (SetFocus, etc.)
5. RaiseXxxed (ctx)                 — non-cancellable notification
6. Return result
```

---

## Default Handlers

### DefaultActivateHandler (`View.Command.cs`)

Handles `Command.Activate`:

1. Resets `_lastDispatchOccurred = false`
2. Calls `RaiseActivating (ctx)` — cancellable
3. If `RaiseActivating` returns `true` (handled/dispatched):
   - If `_lastDispatchOccurred` → calls `RaiseActivated (ctx)` (consume-dispatch completion)
   - Returns `true`
4. If routing is `BubblingUp`:
   - If no dispatch target (`GetDispatchTarget (ctx) is null`) → calls `RaiseActivated (ctx)`
   - Returns `false` (notification semantics — skips SetFocus)
5. If not `BubblingUp` (Direct):
   - Calls `SetFocus ()` if `CanFocus`
   - If `_lastDispatchOccurred` → skips `RaiseActivated` (relay-dispatch uses deferred completion)
   - Otherwise → calls `RaiseActivated (ctx)`
   - Returns `true`

**Key design decision**: For `BubblingUp`, plain views (no dispatch target) fire `RaiseActivated`,
but relay-dispatch views (like Shortcut) skip it — they use the deferred `CommandView_Activated`
callback path to ensure correct event ordering (CheckBox toggles before Shortcut.Action reads value).

### DefaultAcceptHandler (`View.Command.cs`)

Handles `Command.Accept`:

1. Resets `_lastDispatchOccurred = false`
2. Calls `RaiseAccepting (ctx)` — cancellable
3. If handled:
   - Calls `RaiseAccepted (ctx)` if dispatch occurred or routing is `Bridged`
   - Returns `true`
4. If not handled:
   - Checks `DefaultAcceptView` redirect (for Dialog default button behavior)
   - For `BubblingUp` with dispatch target → calls `RaiseAccepted (ctx)`
5. Calls `RaiseAccepted (ctx)` (final completion)
6. Returns `true` if redirected, bubbling, or is `IAcceptTarget`

### DefaultHotKeyHandler (`View.Command.cs`)

Handles `Command.HotKey`:

1. Calls `RaiseHandlingHotKey (ctx)` — cancellable
2. If not cancelled:
   - Calls `SetFocus ()` if `CanFocus`
   - Calls `RaiseHotKeyCommand (ctx)` — non-cancellable notification
   - Invokes `Command.Activate` with the original binding
3. Returns `true`

**Cascade**: HotKey → focus → notify → Activate. This is why pressing a HotKey both focuses
a view AND activates it.

---

## Dispatch Patterns (Composite Views)

### Overview

Composite views contain SubViews that are implementation details. The dispatch pattern lets the
container view declaratively route commands to the appropriate SubView.

### GetDispatchTarget Virtual

```csharp
protected virtual View? GetDispatchTarget (ICommandContext? ctx) => null;
```

Override to return the SubView that should receive dispatched commands. Called during
`RaiseActivating`/`RaiseAccepting` after `OnXxxing` and the `Xxxing` event have had
a chance to cancel.

### ConsumeDispatch Virtual

```csharp
protected virtual bool ConsumeDispatch => false;
```

- `true` = Dispatch consumes command; target doesn't complete its own activation (container
  owns state mutation). Used by selectors.
- `false` = Relay dispatch; target completes normally, container gets notified via deferred
  completion. Used by Shortcut.

### TryDispatchToTarget (`View.Command.cs`)

Guard conditions prevent re-entry and loops:

1. No target → return false
2. Already `DispatchingDown` → return false (prevents re-entry)
3. Routing is `Bridged` → return false (bridge brings commands UP, not down)
4. Relay (`ConsumeDispatch=false`) AND no binding → return false (programmatic guard)
5. For `ConsumeDispatch=true`:
   - Skip dispatch if routing is `BubblingUp` (prevents double-toggle)
   - Otherwise: `DispatchDown (target, ctx)`, set `_lastDispatchOccurred = true`, return `true`
6. For `ConsumeDispatch=false` (relay):
   - Skip if source is within target (prevents loops)
   - `DispatchDown (target, ctx)`, set `_lastDispatchOccurred = true`, return `false`

### Pattern Summary

| View | `GetDispatchTarget` | `ConsumeDispatch` | Behavior |
|------|--------------------|--------------------|----------|
| **Shortcut** | `=> CommandView` | `false` | Relay: CommandView completes, Shortcut notified via deferred callback |
| **OptionSelector** | `=> Focused` (or source CheckBox for BubblingUp) | `true` | Consume: Selector owns radio-select state |
| **FlagSelector** | `=> Focused` (or source CheckBox for BubblingUp) | `true` | Consume: Selector owns flag-toggle state |
| **MenuBar** | `=> Focused` | `true` | Consume: MenuBar owns activation state |
| **Bar** | not overridden | not overridden | Transparent container; bubbling only |
| **Menu** | not overridden (OnActivating dispatches manually) | not overridden | Dispatches to focused MenuItem via OnActivating override |
| **Plain View** | not overridden (`null`) | not overridden | No dispatch |

### Deviations from Original Plan

The original design assumed deferred completion could be purely synchronous (dispatch returns,
then fire completion). In practice, **relay-dispatch views (Shortcut) still use the
`CommandView_Activated` callback** because when a command arrives via `BubblingUp`, the originator
(CheckBox) hasn't called `RaiseActivated` yet. The callback ensures correct ordering: CheckBox
toggles state → Shortcut.Action reads updated value.

---

## Command Bubbling

### CommandsToBubbleUp Property

```csharp
public IReadOnlyList<Command> CommandsToBubbleUp { get; set; } = [];
```

Declares which commands should propagate from SubViews to this View's SuperView. Opt-in only.

### TryBubbleUp (`View.Command.cs`)

1. Returns early if already handled
2. Returns false if routing is `DispatchingDown` (no bubbling from dispatched commands)
3. Special handling for `Command.Accept`:
   - Checks `DefaultAcceptView` on this view and SuperView
   - If found and source is non-default `IAcceptTarget`: redirects Accept to default button
4. Checks if SuperView has command in `CommandsToBubbleUp`:
   - If yes: invokes command on SuperView with `BubblingUp` routing
5. Handles `Padding` wrapper scenarios (bubbles through Padding to its parent)

**Critical**: Bubbling is **notification, not consumption**. The SuperView's handler return
value is ignored. The method always returns `false` after a successful bubble. This ensures
the originating view completes its own processing.

### DefaultAcceptView / IAcceptTarget

```csharp
public View? DefaultAcceptView { get; set; }
```

Auto-discovers SubViews implementing `IAcceptTarget` with `IsDefault=true`. When a non-default
view accepts, the Accept command is redirected to the default button (Dialog's "OK" button pattern).

---

## CommandBridge (Cross-Boundary Routing)

### Purpose

Bridges command routing between views that are NOT in a SuperView/SubView containment
relationship. Primary use case: MenuBarItem (in the MenuBar) ↔ PopoverMenu (registered
with `Application.Popover`, outside the view hierarchy).

### Mechanism

```csharp
public sealed class CommandBridge : IDisposable
{
    public static CommandBridge Connect (View owner, View remote, Command[] commands);
}
```

1. Subscribes to remote view's `Accepted` and/or `Activated` events
2. When remote fires event: creates `CommandContext` with `Routing = CommandRouting.Bridged`
3. Invokes command on owner via `InvokeCommand` (full CWP pipeline re-enters)
4. Bridged commands can then bubble through the owner's SuperView hierarchy

### One-Way Routing

Remote fires event → Owner receives command. For bidirectional, create two bridges.

### Weak References

Both `_owner` and `_remote` are `WeakReference<View>`. Bridge becomes inert if either
is collected. Disposed automatically when either view disposes.

### Guard in TryDispatchToTarget

When a command arrives via `Bridged` routing, `TryDispatchToTarget` returns false. This prevents
dispatching back DOWN — the bridge is designed to bring commands UP from detached views into
the containment hierarchy.

### Example Flow

```
MenuItem (in PopoverMenu) activated
  → bubbles to Menu (in PopoverMenu)
  → Menu.Activated fires
  → PopoverMenu's callback fires
  → CommandBridge detects Activated on remote (PopoverMenu)
  → Creates {Routing=Bridged} context
  → Invokes Command.Activate on owner (MenuBarItem)
  → MenuBarItem.OnActivating sees Bridged → skips PopoverMenu toggle (notification only)
  → Bubbles to MenuBar.OnActivating with BubblingUp routing
```

---

## Keyboard Integration

### Key Binding Flow (`View.Keyboard.cs`)

```
NewKeyDownEvent (key)
  │
  ├─ If has Focused SubView:
  │  └─ Focused.NewKeyDownEvent (key)    — depth-first recursion
  │
  ├─ RaiseKeyDown (key)                  — OnKeyDown + KeyDown event
  │
  ├─ InvokeCommandsBoundToKey (key)      — KeyBindings lookup
  │
  ├─ InvokeCommandsBoundToHotKey (key)   — HotKeyBindings lookup (this + all SubViews)
  │
  └─ RaiseKeyDownNotHandled (key)        — OnKeyDownNotHandled + event
```

### Default Key Bindings (View Constructor)

```csharp
KeyBindings.Add (Key.Space, Command.Activate);
KeyBindings.Add (Key.Enter, Command.Accept);
```

Views like Button override these (both Space and Enter → `Command.Accept`).

### HotKey Processing

`InvokeCommandsBoundToHotKey` processes the current view's HotKeyBindings first, then recurses
through all SubViews' HotKeyBindings (skipping the already-processed Focused SubView).

---

## Route Tracing

### Infrastructure

Route tracing replaces the commented-out `Logging.Debug()` statements with a structured,
runtime-controllable tracing system.

**Key files**:
- `Terminal.Gui/App/Tracing/Trace.cs` — Static class with `CommandEnabled`, `MouseEnabled`,
  `KeyboardEnabled` properties
- UICatalog has a "Command Trace" toggle in the Logging menu

### Trace Points

Trace calls are placed at key routing points in `View.Command.cs`:
- Entry/exit of `InvokeCommand`
- Entry/exit of `RaiseActivating`/`RaiseAccepting`/`RaiseHandlingHotKey`
- Entry/exit of `TryDispatchToTarget`
- Entry/exit of `TryBubbleUp`
- Entry/exit of `DispatchDown`

### Usage

Enable tracing via configuration or the UICatalog menu toggle. Trace output shows the command
routing path with directional arrows (↑ BubblingUp, ↓ DispatchingDown, ↔ Bridged, • Direct).

---

## View-Specific Behaviors

### Button

- Does **NOT** raise `Activating`/`Activated` events
- Both Space and Enter → `Command.Accept` (not Activate)
- HotKey → focus + Accept (via `OnHotKeyCommand` invoking `Command.Accept`)
- Implements `IAcceptTarget` for Dialog default button behavior

### Shortcut

- `GetDispatchTarget => CommandView`, `ConsumeDispatch = false` (relay)
- `CommandsToBubbleUp = [Command.Activate, Command.Accept]`
- Uses `CommandView_Activated` callback for deferred completion (correct event ordering)
- `OnActivated` invokes `Action` and forwards to `TargetView` or Application

### OptionSelector / FlagSelector

- `GetDispatchTarget => Focused` (or source CheckBox for BubblingUp), `ConsumeDispatch = true`
- Selector owns state mutation (radio-select or flag-toggle)
- Context-dependent dispatch: uses `ctx.Source` for `BubblingUp`, `Focused` for direct
- `DispatchingDown` routing in `OnActivated` uses `Focused` as fallback target when source
  isn't a CheckBox (enables Menu → MenuItem → Selector dispatch chain)

### MenuBar

- `ConsumeDispatch = true`, `GetDispatchTarget => Focused`
- `OnActivating` handles:
  - `BubblingUp`: When MenuBarItem activation bubbles up, activates MenuBar + shows source item
  - Direct: Toggles Active state (on → off, off → activate first item with PopoverMenu)
- `OnMenuBarItemPopoverMenuOpenChanged`: Manages `_popoverBrowsingMode` flag
- Visibility/Enabled guard prevents activation when hidden or disabled

### MenuBarItem

- `OnActivating`: Toggles `PopoverMenuOpen` for Direct/BubblingUp commands
- `Bridged` guard: Commands arriving via bridge (from PopoverMenu internals) are notifications
  only — don't toggle PopoverMenu state
- `IsInitialized` guard on `MakeVisible` prevents crashes in design mode / tests without
  `Application.Init`

### Menu

- `OnActivating` override dispatches to focused MenuItem when routing is not `BubblingUp`
- Prevents loops: doesn't re-dispatch when a MenuItem's activation is bubbling up
- Same-tree containment wiring (not CommandBridge) for MenuItem events

---

## Design Invariants

These invariants must be preserved by any future changes:

1. **Bubbling is notification, not consumption.** SuperView's handler return value is ignored
   after bubble. The originating view always completes its own processing.

2. **Consume-dispatch blocks bubbling.** When `ConsumeDispatch=true` and
   `TryDispatchToTarget` consumes, `args.Handled=true` prevents `TryBubbleUp` from running.
   Selectors own their internal state; inner CheckBox activations are implementation details.

3. **`DispatchingDown` blocks re-entry.** `TryDispatchToTarget` returns false if routing is
   already `DispatchingDown`, preventing multi-level dispatch chains (e.g., Menu → MenuItem →
   FlagSelector → CheckBox would cause double-toggle).

4. **`Bridged` blocks dispatch-down.** Bridge brings commands UP into the containment hierarchy.
   Dispatching back down from a bridged command would create routing loops.

5. **Relay-dispatch programmatic guard.** For `ConsumeDispatch=false` views (Shortcut), dispatch
   is skipped when `ctx.Binding is null` (programmatic invocation). This prevents loops when
   `InvokeCommand (Command.Activate)` is called directly.

6. **`_lastDispatchOccurred` reset at handler entry.** Both `DefaultActivateHandler` and
   `DefaultAcceptHandler` reset this flag before calling `RaiseXxxing` to prevent stale state
   from a prior invocation causing spurious completion events.

7. **Deferred completion for relay-dispatch.** Shortcut's `RaiseActivated` fires AFTER
   CommandView's `RaiseActivated` (via `CommandView_Activated` callback), ensuring the
   CommandView's state change (e.g., CheckBox toggle) completes before Shortcut reads it.

---

## Completed Work

### Phase A — Foundation (Commit `be22792b2`)

- `CommandOutcome` enum with `ToBool()`/`ToOutcome()` conversion shims
- `CommandRouting` enum replacing ad-hoc boolean flags
- `CommandContext` as `readonly record struct` with `WithCommand`/`WithRouting`
- `ICommandBinding.Source` → `WeakReference<View>?`

### Phase B — Dispatch (Commit `bc48676af`)

- `GetDispatchTarget` / `ConsumeDispatch` virtuals on View
- Dispatch integration into `RaiseActivating` / `RaiseAccepting`
- Migrated Shortcut, OptionSelector, FlagSelector to declarative dispatch
- Six deviations from original plan (documented in detail below)

### Phase C — Bridge (Commit `9045e3050`)

- `CommandBridge` class with weak references and one-way routing
- MenuBarItem migrated to use `CommandBridge.Connect`
- `RaiseAccepted`/`RaiseActivated` changed to `internal protected` for bridge access

### Phase E — Cleanup (Commit `4262441ca`)

- Removed `IsBubblingUp`/`IsBubblingDown` compatibility properties
- Route tracing infrastructure replacing commented `Logging.Debug()` statements

### Bug Fixes — Issues #1-3 (Command Correctness)

| Issue | Fix | Files |
|-------|-----|-------|
| #1: `DefaultActivateHandler` didn't fire `RaiseActivated` on bubble-up | Added `RaiseActivated` for plain views (guarded by `GetDispatchTarget is null`) | `View.Command.cs` |
| #2: Stale `_lastDispatchOccurred` caused spurious completion events | Reset flag at top of both default handlers | `View.Command.cs` |
| #3: Shortcut `_activatedFiredThisCycle` stuck after programmatic invoke | Added `OnActivating` override to reset flag | `Shortcut.cs` |

### Bug Fixes — Architectural Gaps #1-3 (Menu Routing)

| Gap | Fix | Files |
|-----|-----|-------|
| #1: FlagSelector/OptionSelector external activation | `Focused` fallback in `OnActivated` for `DispatchingDown` routing | `FlagSelector.cs`, `OptionSelector.cs` |
| #2: CommandBridge called `RaiseXxxed` directly (fire-and-forget) | Changed to `InvokeCommand` (full CWP pipeline, enables bubbling) | `CommandBridge.cs`, `View.Command.cs` |
| #3: Menu.OnActivating was a no-op for dispatch | Added `OnActivating` override dispatching to focused MenuItem | `Menu.cs` |

### Bug Fixes — MenuBar/PopoverMenu Routing (12 tests fixed)

| Fix | Description | Tests Fixed |
|-----|-------------|-------------|
| MenuBar.OnActivating toggle logic | Explicit activation/deactivation for Direct + BubblingUp routing | 7 |
| MenuBarItem bridge guard | `Bridged` commands skip PopoverMenu toggle | 2 |
| PopoverMenuOpen MakeVisible guard | `IsInitialized` check prevents crash without Application | 2 |
| ConsumeDispatch design limitation | Skipped 1 test documenting intentional behavior | 1 |

### Phase B Deviations from Original Plan

1. **Shortcut still uses `CommandView_Activated` callback** — Synchronous deferred completion
   proved insufficient; callback ensures correct event ordering
2. **ConsumeDispatch=true does NOT DispatchDown for BubblingUp** — Prevents double-toggle
3. **Selectors dispatch Activate only, not Accept** — Accept must bubble through Menu hierarchy
4. **Selectors use context-dependent dispatch targets** — Source CheckBox for BubblingUp,
   Focused for direct
5. **FlagSelector retains `_suppressHotKeyActivate`** — Binding guard doesn't suppress dispatch
   because DefaultHotKeyHandler passes original binding
6. **Binding guard is ConsumeDispatch-dependent** — Only relay-dispatch (ConsumeDispatch=false)
   needs the programmatic guard
7. **Event ordering: Activating fires BEFORE dispatch** — Old code dispatched in OnActivating
   (step 2); new code dispatches after event (step 4)

### Final Test Results

| Suite | Passed | Failed | Skipped |
|-------|--------|--------|---------|
| UnitTestsParallelizable | 14,045 | 0 | 23 |
| UnitTests | ~1,001 | 0 | ~22 |
| **Total** | **~15,046** | **0** | **~45** |

---

## Future Work

### Phase D: CommandOutcome Migration (Deferred)

**Scope**: 267 `AddCommand` call sites across 28 files need `bool?` → `CommandOutcome` return
type migration.

**Approach**: Purely mechanical with no behavioral change. The `CommandOutcomeExtensions.ToBool()`
and `ToOutcome()` shims enable incremental migration. Recommended: migrate one file at a time,
verifying tests after each.

**Priority**: Low — the shims work correctly and there is no functional difference. This is a
code quality improvement for self-documenting return values.

### CommandManager / CommandRouter Extraction

**Concept**: Extract command routing logic from `View.Command.cs` (~1,043 lines) into a
dedicated `CommandRouter` or `CommandManager` class.

**Motivation**:
- `View.Command.cs` mixes command registration, invocation, event raising, routing, and
  default handlers — six distinct responsibilities
- Command routing tests require full View hierarchy setup, making it hard to isolate routing
  logic bugs from View lifecycle bugs
- A separated router could be tested with lightweight mocks

**Proposed approach** (from exploration):

```csharp
internal class CommandRouter
{
    private readonly View _owner;

    public bool? Route (Command command, ICommandContext ctx) { ... }
    public bool TryBubbleUp (ICommandContext ctx) { ... }
    public bool TryDispatchDown (View target, ICommandContext ctx) { ... }
}
```

**Challenges**:
- Router needs access to View state (SuperView, SubViews, CommandsToBubbleUp, Focused)
- Breaking change to View's protected API surface
- Risk of over-abstraction — the file is already well-organized with clear regions
- The tracing infrastructure provides sufficient debugging visibility for now

**Recommendation**: Defer until testing isolation becomes a concrete pain point. The route
tracing system (already implemented) addresses the primary debugging need.

### Command Pipeline / Middleware Pattern

**Concept**: Model command processing as a pipeline with discrete stages:

```csharp
internal class CommandPipeline
{
    void AddStage (ICommandStage stage);
    bool? Execute (Command command, ICommandContext ctx);
}

// Stages: Validation → Preview → Dispatch → Bubble → Execute
```

**Benefits**: Each stage independently testable, easy to add new stages (tracing, validation).

**Assessment**: Over-engineering for current needs. Performance overhead (foreach, interface
dispatch) and significant refactoring required. Only revisit if command flow becomes substantially
more complex.

### Additional Refinements

1. **Menu `ItemSelected` event** — Separate "close the menu" concern from command routing.
   Currently Menu conflates them via `menuItem.Activated → RaiseAccepted`. Works but is
   confusing. Low priority.

2. **CommandBridge Dispose symmetry** — Dispose unconditionally unsubscribes both handlers but
   constructor subscribes conditionally. Safe no-op but asymmetric. Trivial fix.

3. **Debug logging in mouse hot path** — String interpolation in `Logging.Debug` runs on every
   mouse event. Also active in MenuBar, PopoverMenu, MenuBarItem. Performance concern.

4. **Accept returns `false` for composite views on bubble-up** — May cause input events to not
   be consumed. Needs test coverage analysis before changing.

5. **Full Menu/PopoverMenu migration to CommandBridge** — Currently only MenuBarItem uses
   CommandBridge. Menu.OnSubViewAdded and PopoverMenu.Root setter use same-tree event wiring.
   These are containment relationships (not cross-boundary), so CommandBridge may not be
   appropriate, but the wiring could be simplified.

---

## File Reference

### Core Command System

| File | Lines | Purpose |
|------|-------|---------|
| `Terminal.Gui/ViewBase/View.Command.cs` | ~1,043 | Command registration, invocation, CWP pipeline, dispatch, bubbling |
| `Terminal.Gui/Input/Command.cs` | — | Command enum |
| `Terminal.Gui/Input/CommandContext.cs` | 68 | Immutable context struct |
| `Terminal.Gui/Input/CommandRouting.cs` | 30 | Routing enum |
| `Terminal.Gui/Input/CommandOutcome.cs` | 62 | Outcome enum + conversion shims |
| `Terminal.Gui/Input/CommandBridge.cs` | 122 | Cross-boundary routing |
| `Terminal.Gui/Input/CommandBinding.cs` | — | Generic command binding |
| `Terminal.Gui/Input/CommandEventArgs.cs` | — | Event args with ICommandContext |
| `Terminal.Gui/Input/ICommandContext.cs` | — | Read-only context interface |
| `Terminal.Gui/Input/ICommandBinding.cs` | — | Binding interface |

### View Implementations

| File | Dispatch Pattern | Notes |
|------|-----------------|-------|
| `Terminal.Gui/Views/Shortcut.cs` | Relay (`ConsumeDispatch=false`) | Deferred completion via callback |
| `Terminal.Gui/Views/Selectors/OptionSelector.cs` | Consume (`ConsumeDispatch=true`) | Radio-select semantics |
| `Terminal.Gui/Views/Selectors/FlagSelector.cs` | Consume (`ConsumeDispatch=true`) | Flag-toggle semantics |
| `Terminal.Gui/Views/Menu/MenuBar.cs` | Consume (`ConsumeDispatch=true`) | Activation toggle |
| `Terminal.Gui/Views/Menu/MenuBarItem.cs` | Bridge owner | PopoverMenu bridge |
| `Terminal.Gui/Views/Menu/Menu.cs` | Manual dispatch via OnActivating | Dispatches to focused MenuItem |
| `Terminal.Gui/Views/Button.cs` | None | Leaf view, Accept only |

### Tests

| File | Focus |
|------|-------|
| `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs` | Core command routing |
| `Tests/UnitTestsParallelizable/Views/ShortcutTests.Command.cs` | Shortcut relay dispatch |
| `Tests/UnitTestsParallelizable/Views/MenuBarTests.cs` | MenuBar activation/routing |
| `Tests/UnitTestsParallelizable/Views/MenuTests.cs` | Menu dispatch to MenuItem |
| `Tests/UnitTestsParallelizable/Views/PopoverMenuTests.cs` | Bridge + cross-boundary routing |
| `Tests/UnitTestsParallelizable/Views/MenuItemTests.cs` | MenuItem activation |

### Documentation

| File | Content |
|------|---------|
| `docfx/docs/command.md` | User-facing command system documentation |
| `docfx/docs/cancellable-work-pattern.md` | CWP pattern explanation |
