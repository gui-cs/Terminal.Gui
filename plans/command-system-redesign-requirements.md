# Terminal.Gui v2 Command System — Clean Slate Redesign

## Part 1: Comprehensive Requirements (Derived from Current Implementation)

> This document captures every requirement the current command system satisfies, derived from the
> actual code in View.Command.cs, all View-derived classes, UICatalog scenarios, and unit tests.
> After review, Part 2 will propose a clean-slate redesign.

---

## 1. Core Abstractions

### 1.1 Command Enum
The system defines a small, closed set of semantic commands:

| Command | Semantic Meaning | Typical Triggers |
|---------|-----------------|-----------------|
| **Activate** | "Interact with / select this view" — changes state, toggles, selects | Space, mouse click/release, navigation keys |
| **Accept** | "Perform the view's primary action" — confirms, commits, executes | Enter, double-click |
| **HotKey** | "The view's hot key was pressed" — sets focus then invokes Activate | Alt+letter, Shortcut.Key |
| **NotBound** | Raised when an unregistered command is triggered | Programmatic dispatch to a view without a handler |

**Requirement R-CMD-1**: The system MUST support at least Activate, Accept, and HotKey as distinct, non-overlapping semantic commands.

**Requirement R-CMD-2**: Accept MUST NOT invoke Activate, and Activate MUST NOT invoke Accept. They are independent paths.

**Requirement R-CMD-3**: HotKey MUST set focus (when CanFocus is true), then invoke Activate as a secondary effect.

**Requirement R-CMD-4**: There MUST be a mechanism to handle unbound/unrecognized commands (currently CommandNotBound).

### 1.2 Command Context
Every command invocation carries context describing the origin and routing state:

| Property | Purpose |
|----------|---------|
| `Command` | Which command is being invoked |
| `Source` | WeakReference to the originating View (who started the chain) |
| `Binding` | The KeyBinding/MouseBinding/CommandBinding that triggered this (null for programmatic) |
| `IsBubblingUp` | True when the command is propagating upward through the SuperView chain |
| `IsBubblingDown` | True when a SuperView is dispatching downward to a specific SubView |

**Requirement R-CTX-1**: Every command invocation MUST carry a context that identifies the command, source view, and triggering binding.

**Requirement R-CTX-2**: The context MUST distinguish programmatic invocations (binding=null) from user-driven ones (binding present).

**Requirement R-CTX-3**: The context MUST indicate routing direction (bubbling up vs. bubbling down vs. direct) to prevent infinite loops.

**Requirement R-CTX-4**: The original source MUST be preserved as the command routes through the hierarchy — views at any level can inspect who originated the command.

### 1.3 Command Event Args
Commands follow the Cancellable Workflow Pattern (CWP):

| Phase | Virtual Method | Event | Cancellable? |
|-------|---------------|-------|-------------|
| Pre-execution | `OnActivating(args)` | `Activating` | Yes (return true or set Handled=true) |
| Post-execution | `OnActivated(ctx)` | `Activated` | No |
| Pre-execution | `OnAccepting(args)` | `Accepting` | Yes |
| Post-execution | `OnAccepted(ctx)` | `Accepted` | No |
| Pre-execution | `OnHandlingHotKey(args)` | `HandlingHotKey` | Yes |
| Post-execution | `OnHotKeyCommand(ctx)` | `HotKeyCommand` | No |

**Requirement R-CWP-1**: Each command MUST follow the CWP pattern: virtual method (subclass first) → event (external subscriber) → default behavior.

**Requirement R-CWP-2**: Pre-execution (Xxxing) methods/events MUST be cancellable via `Handled = true` or return value `true`.

**Requirement R-CWP-3**: Post-execution (Xxxed) events MUST NOT be cancellable.

**Requirement R-CWP-4**: If the pre-execution phase is handled/cancelled, the post-execution phase MUST NOT fire.

---

## 2. Command Routing

### 2.1 Key-to-Command Binding
Views have two binding collections:

| Collection | Scope | Example |
|-----------|-------|---------|
| `KeyBindings` | Only active when the view has focus | Space → Activate, Enter → Accept |
| `HotKeyBindings` | Active regardless of focus, within the SuperView tree | Alt+F → HotKey |

Plus application-level bindings:
| Collection | Scope |
|-----------|-------|
| `App.Keyboard.KeyBindings` | Global, active from any view |

**Requirement R-BIND-1**: Views MUST support focus-scoped key bindings (only fire when focused).

**Requirement R-BIND-2**: Views MUST support hot-key bindings (fire regardless of focus, scoped to SuperView tree).

**Requirement R-BIND-3**: The system MUST support application-global key bindings (fire from any view).

**Requirement R-BIND-4**: Mouse events MUST be bindable to commands (click → Activate, double-click → Accept, etc.).

**Requirement R-BIND-5**: HotKey bindings MUST recurse into unfocused SubViews (current `InvokeCommandsBoundToHotKey` behavior).

### 2.2 Command Dispatch Flow (NewKeyDownEvent)
When a key is pressed, the dispatch order is:

1. Recurse into Focused SubView (depth-first)
2. `OnKeyDown` / `KeyDown` event (pre-processing, cancellable)
3. `InvokeCommandsBoundToKey` (focus-scoped bindings)
4. `InvokeCommandsBoundToHotKey` (hot-key bindings, recurses into unfocused SubViews)
5. `OnKeyDownNotHandled` / `KeyDownNotHandled` event (post-processing)

**Requirement R-DISP-1**: Key dispatch MUST be depth-first through the focused SubView chain.

**Requirement R-DISP-2**: Focus-scoped bindings MUST be checked before hot-key bindings.

**Requirement R-DISP-3**: Views MUST have a pre-processing hook (`OnKeyDown`) before any bindings are invoked.

**Requirement R-DISP-4**: Views MUST have a post-processing hook (`OnKeyDownNotHandled`) for unhandled keys.

### 2.3 Upward Bubbling (TryBubbleToSuperView)

Bubbling is **opt-in** per SuperView via `CommandsToBubbleUp`:

```
SubView raises Command
  → RaiseXxxing(ctx) → OnXxxing → Xxxing event
  → If not handled: TryBubbleToSuperView
    → Check: IsBubblingDown? → Skip (prevent loop)
    → Check: DefaultAcceptView? (Accept only) → BubbleDown to it
    → Check: SuperView.CommandsToBubbleUp contains this command? → Invoke on SuperView with IsBubblingUp=true
    → Check: Padding edge case → same check on Padding.Parent
  → If still not handled: continue to Xxxed phase
```

**Requirement R-BUB-1**: Upward bubbling MUST be opt-in — views MUST explicitly declare which commands their SubViews can bubble.

**Requirement R-BUB-2**: `CommandsToBubbleUp` MUST be empty by default (no implicit bubbling).

**Requirement R-BUB-3**: When bubbling up, the context MUST have `IsBubblingUp = true` to distinguish from direct invocations.

**Requirement R-BUB-4**: Handling at any level (`Handled = true`) MUST stop further upward propagation.

**Requirement R-BUB-5**: Bubbling MUST work through arbitrarily deep hierarchies (SubView → SuperView → GrandSuperView...).

**Requirement R-BUB-6**: The `DefaultAcceptView` pattern: when Accept reaches a SuperView that has a SubView implementing `IAcceptTarget { IsDefault = true }`, the Accept MUST be redirected to that SubView.

### 2.4 Downward Dispatching (BubbleDown)

SuperViews can dispatch commands downward to specific SubViews:

```
SuperView.BubbleDown(target, ctx)
  → Creates new context with IsBubblingDown=true
  → Invokes command on target
  → Target's TryBubbleToSuperView sees IsBubblingDown → skips re-bubbling
```

**Requirement R-DOWN-1**: SuperViews MUST be able to dispatch commands to specific SubViews.

**Requirement R-DOWN-2**: BubbleDown MUST set `IsBubblingDown=true` to prevent the target from re-bubbling the command.

**Requirement R-DOWN-3**: BubbleDown MUST preserve the original command, source, and binding from the context.

**Requirement R-DOWN-4**: After a BubbleDown completes, subsequent direct invocations on the same view MUST bubble normally (no permanent state change).

---

## 3. View-Specific Command Behaviors

### 3.1 Button
| Aspect | Behavior |
|--------|----------|
| Space/Enter/Click | All map to **Accept** (not Activate) |
| HotKey | Maps to Accept (via `OnHotKeyCommand`) |
| `IAcceptTarget` | Implements it; `IsDefault` controls visual decoration and DefaultAcceptView targeting |
| Bubbling | Does not set `CommandsToBubbleUp` — it is a leaf originator |

**Requirement R-BTN-1**: Button's primary action MUST be Accept (not Activate).

**Requirement R-BTN-2**: Button MUST implement `IAcceptTarget` to participate in `DefaultAcceptView` routing.

### 3.2 CheckBox
| Aspect | Behavior |
|--------|----------|
| Space/Click | Activate → `AdvanceCheckState()` (toggle) |
| Enter | Accept (does not toggle) |
| Double-click | Accept |
| State change | Happens in `OnActivated` (post-execution) |

**Requirement R-CHK-1**: CheckBox Activate MUST toggle state; Accept MUST NOT toggle state.

**Requirement R-CHK-2**: State change MUST happen in `OnActivated` (after the Activating phase completes without cancellation).

### 3.3 Shortcut (Composite View)
Shortcut is a composite of CommandView + HelpView + KeyView that presents as a single control.

| Aspect | Behavior |
|--------|----------|
| CommandsToBubbleUp | `[Activate, Accept]` — SubView commands propagate up |
| Space/Click anywhere | Activate — dispatched to CommandView via BubbleDown if not from CommandView |
| Enter | Accept — dispatched to CommandView via BubbleDown |
| HotKey (Shortcut.Key) | HotKey → SetFocus → Activate |
| CommandView HotKey | Activates CommandView directly, bubbles up to Shortcut |
| Programmatic InvokeCommand | No BubbleDown (binding is null) |
| Action delegate | Fires on both OnActivated and OnAccepted |
| TargetView/Command | InvokeOnTargetOrApp dispatches to TargetView or App key bindings |

**BubbleDown Decision Rule:**
```
BubbleDown to CommandView ONLY when:
  1. The command has a Binding (user interaction, not programmatic)
  2. AND Binding.Source is NOT the CommandView (or a descendant)
```

**Requirement R-SC-1**: Shortcut MUST present as a single control — any interaction anywhere MUST produce exactly one state change.

**Requirement R-SC-2**: Shortcut MUST BubbleDown to CommandView for user-driven interactions originating outside CommandView.

**Requirement R-SC-3**: Shortcut MUST NOT BubbleDown for programmatic InvokeCommand calls (no binding).

**Requirement R-SC-4**: Shortcut MUST NOT BubbleDown when the source is already CommandView (prevents loops).

**Requirement R-SC-5**: Shortcut MUST support deferred activation — when activation bubbles up from CommandView, Shortcut defers its own RaiseActivated until CommandView.Activated fires.

**Requirement R-SC-6**: Shortcut.Action MUST fire on both Activated and Accepted.

**Requirement R-SC-7**: Shortcut MUST support BindKeyToApplication for application-global key shortcuts.

### 3.4 Bar (Container)
| Aspect | Behavior |
|--------|----------|
| CommandsToBubbleUp | `[Accept, Activate]` |
| Own command handling | None — pure pass-through |
| Does BubbleDown? | No |

**Requirement R-BAR-1**: Bar MUST be a transparent relay that bubbles commands from its Shortcut SubViews to its own SuperView.

**Requirement R-BAR-2**: Bar MUST NOT intercept or modify commands passing through it.

### 3.5 Menu (Vertical Bar for dropdown menus)
| Aspect | Behavior |
|--------|----------|
| CommandsToBubbleUp | `[Accept, Activate]` |
| OnSubViewAdded wiring | `menuItem.Accepting → Menu.RaiseAccepted`; `menuItem.Activated → Menu.RaiseAccepted` |

**Requirement R-MENU-1**: Menu MUST propagate both Accepting and Activated from MenuItems to its own Accepted event (enables menu close-on-select).

**Requirement R-MENU-2**: Menu MUST fire `SelectedMenuItemChanged` when focus changes between MenuItems.

### 3.6 MenuItem (Shortcut + SubMenu)
| Aspect | Behavior |
|--------|----------|
| Extends | Shortcut (inherits all command routing) |
| SubMenu | Optional cascading Menu |
| Mouse hover | Auto-focuses (triggers SelectedMenuItemChanged) |

**Requirement R-MI-1**: MenuItem MUST inherit all Shortcut command behavior.

**Requirement R-MI-2**: MenuItem MUST support optional SubMenu for cascading.

### 3.7 MenuBar (Horizontal Menu)
| Aspect | Behavior |
|--------|----------|
| Extends | Menu |
| OnActivating | Toggle: if popover open → close; if closed → open |
| OnAccepting | Show popover for MenuBarItem |
| OnAccepted | Set Active=false when leaf MenuItem accepted |
| HotKey (F9) | Toggle bar activation |
| MenuBarItem HotKey | Skips SetFocus before InvokeCommand(Activate) to prevent race |

**Requirement R-MB-1**: MenuBar MUST toggle popover visibility on Activate (click/activate when open → close).

**Requirement R-MB-2**: MenuBar MUST deactivate (Active=false) when a leaf MenuItem is accepted.

**Requirement R-MB-3**: MenuBar HotKey MUST toggle the entire bar (open first item or close all).

**Requirement R-MB-4**: MenuBarItem HotKey MUST skip SetFocus to prevent a focus-change race with toggle logic.

### 3.8 MenuBarItem (MenuItem + PopoverMenu)
| Aspect | Behavior |
|--------|----------|
| Extends | MenuItem |
| PopoverMenu | Hosts a PopoverMenu instead of SubMenu |
| HotKey handler | Custom: skips SetFocus, invokes Activate directly |
| Bridge | PopoverMenu.Activated → MenuBarItem.InvokeCommand(Activate) (fresh context) |
| Bridge | PopoverMenu.Accepted → MenuBarItem.RaiseAccepted |

**Requirement R-MBI-1**: MenuBarItem MUST bridge events across the PopoverMenu boundary using fresh CommandContexts.

**Requirement R-MBI-2**: MenuBarItem MUST re-invoke commands on itself when the PopoverMenu fires Activated/Accepted, so they propagate to MenuBar.

### 3.9 PopoverMenu
| Aspect | Behavior |
|--------|----------|
| Hosts | A root Menu in a floating overlay |
| Close-on-Accept | MenuOnAccepting → Visible=false (closes popover) |
| Bridge Activate | MenuActivating → InvokeCommand(Activate) with fresh context |
| Bridge Accepted | MenuAccepted → RaiseAccepted |
| SubMenu routing | Accepted on parent MenuItem → ShowSubMenu; Accepted on leaf → HideAndRemoveSubMenu |
| QuitKey | Special: marks Handled if parent visible (prevents app quit) |

**Requirement R-PM-1**: PopoverMenu MUST close when a leaf MenuItem is accepted.

**Requirement R-PM-2**: PopoverMenu MUST bridge Activate/Accepted signals across its boundary to its owner (MenuBarItem).

**Requirement R-PM-3**: PopoverMenu MUST show/hide cascading submenus based on focus changes.

**Requirement R-PM-4**: QuitKey in a PopoverMenu MUST close the popover, not quit the application.

### 3.10 SelectorBase / OptionSelector / FlagSelector
| Aspect | OptionSelector | FlagSelector |
|--------|---------------|-------------|
| CommandsToBubbleUp | `[Activate, Accept]` | `[Activate, Accept]` |
| OnActivating (IsBubblingUp) | Consumes bubble, calls ApplyActivation, re-bubbles as notification | Consumes bubble, toggles CheckBox Value directly |
| OnActivating (direct/programmatic) | Does nothing (OnActivated handles it) | BubbleDown to focused CheckBox |
| Enter key (Accept) | SelectorBase.OnAccepting invokes Activate then lets Accept continue | Same |
| HotKey | Standard | Suppressed — only sets focus, no toggle |

**Requirement R-SEL-1**: Selectors MUST consume bubbled Activate from CheckBox SubViews to prevent double-toggle.

**Requirement R-SEL-2**: OptionSelector MUST enforce radio-button semantics — only one item selected at a time.

**Requirement R-SEL-3**: FlagSelector MUST support multi-select with bitwise OR semantics.

**Requirement R-SEL-4**: Enter key on a Selector MUST trigger both Activate (state change) and Accept (confirmation).

**Requirement R-SEL-5**: FlagSelector HotKey MUST only set focus, not toggle any flag.

### 3.11 Dialog / Dialog\<TResult\>
| Aspect | Behavior |
|--------|----------|
| CommandsToBubbleUp | `[Accept]` (Activate does not bubble out of Dialog) |
| DefaultAcceptView | The default Button (IAcceptTarget.IsDefault) |
| OnAccepting | Calls RequestStop(); non-default buttons consumed, default buttons not consumed |
| OnActivating (Dialog) | Sets Result to button index |
| Result | Set based on which button triggered the Accept |

**Requirement R-DLG-1**: Dialog MUST stop its modal run when Accept is triggered by any button.

**Requirement R-DLG-2**: Dialog MUST redirect Accept to DefaultAcceptView when Accept is invoked directly on the Dialog.

**Requirement R-DLG-3**: Dialog MUST capture the triggering button's index as the Result.

**Requirement R-DLG-4**: Non-default button Accept MUST be consumed (prevents DefaultAcceptView from also firing).

**Requirement R-DLG-5**: Dialog\<TResult\> MUST support typed results via OnAccepted override.

---

## 4. Cross-Boundary Routing

The Menu system requires commands to cross boundaries that are NOT SuperView/SubView relationships:

```
MenuItem → Menu → [boundary] → PopoverMenu → [boundary] → MenuBarItem → MenuBar
```

PopoverMenu is registered with `App.Popover` — it is NOT a SubView of MenuBar. Therefore normal bubbling cannot work.

**Requirement R-XBOUND-1**: The command system MUST support routing across non-containment boundaries (e.g., PopoverMenu to MenuBar).

**Requirement R-XBOUND-2**: Cross-boundary routing MUST preserve the original source and binding.

**Requirement R-XBOUND-3**: Cross-boundary routing MUST create fresh CommandContexts to re-fire the full CWP chain at each boundary.

---

## 5. Complexity & Pain Points (Current System)

These are observed issues with the current implementation that a redesign should address:

### 5.1 The Deferred Activation Pattern (Shortcut)
Shortcut uses `_activationBubbledUp` flag + `_deferredActivationContext` as a two-phase commit. This is fragile:
- Two independent code paths in `CommandView_Activated` (deferred vs. direct IsBubblingUp)
- Easy to get wrong — FlagSelector had bugs due to this

### 5.2 Fresh CommandContext at Each Boundary
PopoverMenu and MenuBarItem both create fresh `CommandContext` objects to re-invoke commands. This is manual plumbing that should be systematic.

### 5.3 Three-Way IsBubblingUp/IsBubblingDown/Direct
The context has two boolean flags creating three states. Views must check both flags in various combinations, leading to complex conditionals.

### 5.4 Menu.OnSubViewAdded Event Wiring
Menu subscribes to `menuItem.Accepting` AND `menuItem.Activated` and calls `RaiseAccepted` for both. This dual-wiring is necessary but non-obvious.

### 5.5 Inconsistent BubbleDown Semantics
- Shortcut: BubbleDown only with binding, only if source is outside CommandView
- FlagSelector: BubbleDown for programmatic (no binding) invocations
- OptionSelector: No BubbleDown at all (consumes in OnActivating)

### 5.6 Button Maps Everything to Accept
Button remaps Space to Accept (overriding View's default Space → Activate). This means Button has no Activate behavior, which is inconsistent with other views.

### 5.7 SelectorBase.OnAccepting Invokes Activate
When Enter is pressed on a CheckBox in a Selector, SelectorBase's OnAccepting creates a fresh Activate invocation. Accept triggering Activate violates R-CMD-2 at the framework level (even though it's intentional at the Selector level).

### 5.8 CommandsToBubbleUp Must Be Set at Every Container Level
The UICatalog scenarios show that every intermediate FrameView, Window, etc. must explicitly set `CommandsToBubbleUp`. Forgetting one level silently breaks the chain.

### 5.9 TryBubbleToSuperView Has Three Padding Edge Cases
The Padding check is replicated three times with nearly identical code, adding complexity for a rare edge case.

---

## 6. Invariants (Rules That Must Never Be Violated)

**INV-1**: A single user gesture (click, key press) MUST result in at most one state change per view.

**INV-2**: Command routing MUST NOT create infinite loops.

**INV-3**: The original source view MUST be traceable at any point in the routing chain.

**INV-4**: Handled=true at any level MUST stop further propagation.

**INV-5**: Views that don't opt into bubbling MUST be isolated from their SubViews' commands.

**INV-6**: Programmatic InvokeCommand MUST be distinguishable from user-driven invocations.

**INV-7**: Post-execution events (Xxxed) MUST fire only after the pre-execution phase completes without cancellation.

---

## 7. Test Coverage Matrix

| Area | Test File | # Active Tests | # Skipped |
|------|----------|---------------|-----------|
| Core routing | ViewCommandTests.cs | ~30 | 0 |
| Bubbling hierarchy | CommandBubblingTests.cs | 2 | 5 |
| CommandContext | CommandContextTests.cs | ~12 | 0 |
| Shortcut | ShortcutTests.Command.cs | ~18 | 0 |
| Bar | BarTests.cs | ~15 | 2 |
| Menu | MenuTests.cs | ~9 | 1 |
| MenuBar | MenuBarTests.cs | 1 | 0 |
| PopoverMenu | PopoverMenuTests.cs (both) | ~11 | 0 |
| Dialog | DialogTests.cs | ~8 | 0 |
| Selectors | SelectorBaseTests.cs | ~4 | 0 |

---

---
---

# Part 2: Clean Slate Redesign Proposal

## The Unifying Observation

Every view that participates in non-trivial command routing is doing one of these jobs:

### Pattern A: "I am one control made of parts" (Dispatch-to-Primary)

**Shortcut** has CommandView, HelpView, KeyView. Any interaction anywhere dispatches to
CommandView. There is exactly **one** primary target, fixed at construction.

### Pattern B: "I am a group of interactive items" (Dispatch-to-Focused)

**OptionSelector** has N CheckBoxes, one active (focused) at a time. Radio semantics —
the selector consumes the bubble and applies the value change itself.

**FlagSelector** same structure, but multi-select. Each CheckBox contributes a bit to the
composite Value. More complex interaction: must toggle the specific checkbox, not just
"apply the focused one."

### Pattern C: "I am a transparent container" (Relay)

**Bar** has N Shortcuts. It doesn't transform commands — it just declares
`CommandsToBubbleUp = [Accept, Activate]` and lets everything pass through.

**Menu** is the same as Bar but vertical, with one extra behavior: it wires
`menuItem.Accepting` and `menuItem.Activated` to `RaiseAccepted` (the close-on-select
signal).

### Pattern D: "I own a view that isn't my subview" (Bridge)

**MenuItem** owns a **SubMenu** (a Menu that appears as a cascading dropdown). SubMenu is
not always in the view tree — it gets added/removed by PopoverMenu.

**MenuBarItem** owns a **PopoverMenu** that is registered with Application.Popover, completely
outside the SuperView hierarchy. Commands must cross this boundary.

---

## What These Patterns Share

Every pattern involves:
1. **Target selection** — which subview(s) should receive the command?
2. **Loop prevention** — if the target raises an event that bubbles back, don't re-dispatch
3. **Source preservation** — the original source must be traceable at every level
4. **Completion coordination** — the container's post-events (Activated/Accepted) should
   fire after the target's processing completes

The differences are:

| | Target | Cardinality | Consume? | Boundary |
|-|--------|-------------|----------|----------|
| Shortcut | CommandView | 1, fixed | No (relay + dispatch) | Same tree |
| OptionSelector | Focused CheckBox | N, dynamic | Yes (prevents double-toggle) | Same tree |
| FlagSelector | Focused CheckBox | N, dynamic | Yes (toggles directly) | Same tree |
| Bar | (none — relay only) | N | No | Same tree |
| Menu | (relay + close signal) | N | No (but wires close) | Same tree |
| MenuItem→SubMenu | SubMenu | 1, set later | No | Detached |
| MenuBarItem→PopoverMenu | PopoverMenu | 1, set later | No | Cross-boundary |

---

## Design Proposal

### Change 1: CommandRouting Enum (replaces two booleans)

**Problem**: `IsBubblingUp` and `IsBubblingDown` create three implicit states that every
view must check independently, leading to complex conditionals.

**Proposal**: Single enum on the context:

```csharp
public enum CommandRouting { Direct, BubblingUp, DispatchingDown }
```

Single property on `ICommandContext` replaces `IsBubblingUp` and `IsBubblingDown`.
One field to check, one `switch`. No illegal fourth state.

**Files**: `ICommandContext.cs`, `CommandContext.cs`, `View.Command.cs`

### Change 2: Command Coordination Policies

Instead of one `PrimaryCommandTarget` property, provide a small set of coordination
building blocks on View that cover all four patterns:

#### 2a. `GetDispatchTarget` — for Patterns A and B

```csharp
/// Gets the subview to dispatch commands to. Return null to skip dispatch.
/// Called by the framework during RaiseActivating/RaiseAccepting when
/// routing is Direct or BubblingUp.
protected virtual View? GetDispatchTarget (ICommandContext? ctx) => null;
```

- **Shortcut** overrides: `return CommandView;` (fixed target)
- **OptionSelector** overrides: `return Focused;` (dynamic — whichever CheckBox has focus)
- **FlagSelector** overrides: `return Focused;` (dynamic — whichever CheckBox has focus)
- **Bar/Menu**: Don't override (return null = no dispatch, pure relay)

The framework handles:
1. During `RaiseActivating` / `RaiseAccepting`, after OnXxx and Xxxing event:
   - Call `GetDispatchTarget(ctx)`
   - If non-null AND routing is not `DispatchingDown` AND source is not within the target:
     - Dispatch to target with `Routing = DispatchingDown`
2. Defer the container's Xxxed event until the target's processing completes

This replaces:
- Shortcut's `HandleActivate`, `_activationBubbledUp`, `IsWithinCommandView`,
  `CommandView_Activated` (~100 lines)
- OptionSelector's `OnActivating` IsBubblingUp branch
- FlagSelector's `OnActivating` IsBubblingUp branch + `_suppressHotKeyActivate`

#### 2b. `ConsumeDispatch` — for Pattern B (Selectors)

```csharp
/// If true, dispatching to the target consumes the command (returns true from
/// RaiseActivating), preventing the original subview from completing its own
/// activation. If false, the dispatch is a notification and the original
/// subview completes normally.
protected virtual bool ConsumeDispatch => false;
```

- **OptionSelector**: `return true;` (prevents CheckBox.AdvanceCheckState double-fire)
- **FlagSelector**: `return true;` (toggles value directly in OnActivating)
- **Shortcut**: `return false;` (CommandView completes its own activation normally)

#### 2c. Transparent Relay — Pattern C (Bar, Menu)

No new API needed. `CommandsToBubbleUp` already handles relay perfectly.
Bar and Menu just set it in their constructors and don't override `GetDispatchTarget`.

Simplification: extract the 3x Padding edge-case checks from `TryBubbleUp` into a
single `GetBubbleTarget()` helper.

#### 2d. `CommandBridge` — for Pattern D (cross-boundary)

```csharp
public class CommandBridge : IDisposable
{
    // Both references are weak — the bridge must not prevent GC of either view.
    // If either view is collected, the bridge becomes inert.
    private readonly WeakReference<View> _owner;
    private readonly WeakReference<View> _remote;

    /// Connects owner to a remote view for the specified commands.
    /// Subscribes to remote's Activated/Accepted events and re-invokes
    /// commands on owner with fresh CommandContexts preserving source+binding.
    /// Disposes automatically when either view is disposed.
    public static CommandBridge Connect (
        View owner,           // e.g., MenuBarItem
        View remote,          // e.g., PopoverMenu
        params Command[] commands);  // e.g., Command.Activate, Command.Accept
}
```

**WeakReference note**: Both `owner` and `remote` MUST be held as `WeakReference<View>`.
The current system already uses `WeakReference<View>` for `ICommandContext.Source` to
prevent leaks during command propagation. However, `ICommandBinding.Source` is currently
a strong `View?` reference — this is a **known bug** that should be fixed as part of
this redesign (change `ICommandBinding.Source` to `WeakReference<View>?`).

The bridge:
- Subscribes to the remote view's `Activated`/`Accepted` events
- On fire: creates a fresh `CommandContext` preserving source + binding
- Invokes the command on the owner via `InvokeCommand` (fires full CWP chain)
- Auto-unsubscribes on `Dispose()` or when remote is reassigned
- Becomes inert if either WeakReference target is collected

This replaces:
- `MenuBarItem.OnPopoverMenuOnActivated` + `OnPopoverMenuOnAccepted`
- `PopoverMenu.MenuActivating` + `MenuAccepted` manual bridge code
- `Menu.OnSubViewAdded` dual event subscription wiring
- Any future cross-boundary scenarios

### Change 3: Explicit Menu Close Signal

**Problem**: Menu currently wires `menuItem.Accepting → RaiseAccepted` and
`menuItem.Activated → RaiseAccepted` in `OnSubViewAdded`. This conflates
"command bubbling" with "close the menu." The Activated→Accepted mapping
is particularly confusing (why does activation of a MenuItem close the menu?).

**Proposal**: Make the close signal explicit and separate from command routing:

```csharp
// On Menu or PopoverMenu:
public event EventHandler<CommandEventArgs>? ItemSelected;
```

- Menu fires `ItemSelected` when any leaf MenuItem is activated or accepted
- PopoverMenu subscribes to `ItemSelected` → `Visible = false`
- Command routing (Activate/Accept bubbling) continues normally via
  `CommandsToBubbleUp` and `CommandBridge`
- The "close" concern is separated from the "routing" concern

---

## What Stays the Same

These parts of the current design work well and should be preserved:

- **CWP pattern** (OnXxxing → Xxxing → OnXxxed → Xxxed)
- **Opt-in bubbling** via `CommandsToBubbleUp`
- **Command enum** (Activate, Accept, HotKey, NotBound)
- **DefaultAcceptView / IAcceptTarget** for Dialog redirect
- **Three-valued bool? returns** (null/false/true)
- **WeakReference source tracking**
- **KeyBindings / HotKeyBindings split**
- **InvokeCommand overloads**
- **CommandEventArgs with Handled property**

---

## Impact Assessment

| View | Changes Required |
|------|-----------------|
| **View.Command.cs** | Add CommandRouting enum, `GetDispatchTarget` virtual, `ConsumeDispatch` virtual, refactor `TryBubbleUp` Padding checks, integrate dispatch logic into `RaiseActivating`/`RaiseAccepting` |
| **CommandContext** | Replace `IsBubblingUp`/`IsBubblingDown` with `Routing` enum |
| **Shortcut** | Override `GetDispatchTarget` → CommandView. Delete ~100 lines (HandleActivate, deferred flags, CommandView_Activated complexity) |
| **OptionSelector** | Override `GetDispatchTarget` → Focused, `ConsumeDispatch` → true. Simplify OnActivating to just ApplyActivation |
| **FlagSelector** | Override `GetDispatchTarget` → Focused, `ConsumeDispatch` → true. Delete `_suppressHotKeyActivate` |
| **Bar** | No change (already just sets `CommandsToBubbleUp`) |
| **Menu** | Replace `OnSubViewAdded` event wiring with `ItemSelected` event |
| **MenuItem** | No command changes (inherits Shortcut behavior) |
| **MenuBarItem** | Replace manual event handlers with `CommandBridge.Connect` |
| **PopoverMenu** | Replace manual event subscriptions with `CommandBridge` + `ItemSelected` |
| **Dialog** | No change |
| **Button** | No change |
| **CheckBox** | No change |
| **SelectorBase** | Move Enter→Activate from `OnAccepting` to `OnAccepted` (preserves R-CMD-2) |

---

## Migration Path

1. **Phase A**: Add `CommandRouting` enum alongside booleans. Add `IsBubblingUp`/`IsBubblingDown`
   computed properties that map to/from the enum for backward compat.
2. **Phase B**: Add `GetDispatchTarget` / `ConsumeDispatch` to View. Migrate Shortcut first
   (most complex, best test coverage).
3. **Phase C**: Implement `CommandBridge`. Migrate MenuBarItem/PopoverMenu.
4. **Phase D**: Add `ItemSelected` to Menu. Simplify PopoverMenu close logic.
5. **Phase E**: Remove boolean flags, delete dead code, update docs.

Each phase is independently testable with the existing test suite.

---

## Verification

- All existing tests in ViewCommandTests, ShortcutTests.Command, BarTests, MenuTests,
  MenuBarTests, PopoverMenuTests, DialogTests, SelectorBaseTests must continue passing
- The skipped CommandBubblingTests should be unblocked by the bridge work
- UICatalog scenarios (Shortcuts, Menus, Selectors, Dialogs, Bars) must function identically
- Run: `dotnet test Tests/UnitTestsParallelizable --no-build`
- Run: `dotnet test Tests/UnitTests --no-build`

---

## Open Questions

1. Should `GetDispatchTarget` be called from within `RaiseActivating`/`RaiseAccepting`
   (framework-driven) or should it remain in the virtual `OnActivating`/`OnAccepting`
   overrides (view-driven)? Framework-driven is cleaner but less flexible.

2. Should `CommandBridge` also handle the "close" signal, or should that remain a
   separate mechanism (`ItemSelected`)?

3. Should Bar gain `GetDispatchTarget` semantics? Currently Bar is pure relay, but if
   someone invokes Activate directly on a Bar, should it dispatch to the focused Shortcut?

---

## Appendix: Code Samples Under the New Design

### A. CheckBox.cs (Redesigned)

CheckBox is a leaf view — no dispatch, no bridge, no relay. It barely changes.
The only difference is the `CommandRouting` enum check replacing boolean checks
(though CheckBox doesn't even check routing today). Included for completeness.

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

        // Space → Activate and Enter → Accept are inherited from View

        TitleChanged += Checkbox_TitleChanged;
        MouseHighlightStates = DefaultMouseHighlightStates;
    }

    // ──── Command handling ────
    // CheckBox does NOT override:
    //   - GetDispatchTarget (returns null — no subviews to dispatch to)
    //   - ConsumeDispatch (irrelevant — no dispatch)
    //   - OnActivating (base View behavior is fine)
    //   - OnAccepting (base View behavior is fine)

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);
        AdvanceCheckState ();
    }

    // ──── State management ────
    // Value, AdvanceCheckState, ValueChanging/ValueChanged, AllowCheckStateNone,
    // RadioStyle, drawing, etc. — all unchanged from current implementation.
    // ...
}
```

**What changed**: Nothing in the command code. CheckBox is already clean.
The only project-wide change that touches CheckBox is the `CommandRouting` enum
on `ICommandContext`, but CheckBox never inspects routing direction so it's
transparent.

---

### B. Shortcut.cs (Redesigned)

This is the big payoff. The entire `#region Accept/Activate/HotKey Command Handling`
shrinks dramatically. No more `HandleActivate`, `_activationBubbledUp`,
`_deferredActivationContext`, `IsWithinCommandView`, or `CommandView_Activated`.

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

        // NOTE: No AddCommand (Command.Activate, HandleActivate) — the framework
        // calls GetDispatchTarget and handles the dispatch/deferred-completion
        // automatically via the default Activate handler.

        TitleChanged += Shortcut_TitleChanged;

        CommandView = new View
        {
            Width = Dim.Auto (),
            Height = Dim.Fill ()
        };
        Title = commandText ?? string.Empty;
        HelpView.Text = helpText ?? string.Empty;
        // ... KeyView setup, GettingAttributeForRole wiring (unchanged) ...

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
    // (e.g., CheckBox.OnActivated calls AdvanceCheckState). We don't override it.

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);
        Action?.Invoke ();

        if (Command != Command.NotBound && ctx is { })
        {
            ctx.Command = Command;
        }

        InvokeOnTargetOrApp (ctx);
    }

    /// <inheritdoc/>
    protected override void OnAccepted (ICommandContext? ctx)
    {
        base.OnAccepted (ctx);
        Action?.Invoke ();

        if (Command != Command.NotBound && ctx is { })
        {
            ctx.Command = Command;
        }

        InvokeOnTargetOrApp (ctx);
    }

    private void InvokeOnTargetOrApp (ICommandContext? ctx)
    {
        View? target = TargetView ?? GetTopSuperView ();

        if (target is { })
        {
            target.InvokeCommand (Command, ctx);

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
    // - private bool? HandleActivate (ICommandContext? ctx)         — ~40 lines
    // - private bool IsWithinCommandView (View source)             — ~15 lines
    // - private bool _activationBubbledUp;                         — flag
    // - private ICommandContext? _deferredActivationContext;        — flag
    // - protected override bool OnActivating (CommandEventArgs)    — ~20 lines
    // - protected override bool OnAccepting (CommandEventArgs)     — ~20 lines
    // - private void CommandView_Activated (...)                    — ~18 lines
    //
    // Total: ~120 lines of nuanced command routing logic removed.
    // The framework's dispatch machinery in RaiseActivating/RaiseAccepting
    // handles all of this via GetDispatchTarget + source guards + deferred
    // completion.

    // ──── Properties (unchanged) ────

    public View? TargetView { get; set; }
    public Command Command { get; set; } = Command.NotBound;
    public Action? Action { get; set; }
    public bool BindKeyToApplication { get; set; }

    // ──── CommandView, HelpView, KeyView, Key, ShowHide, layout ────
    // All unchanged from current implementation.
    // ...
}
```

**What changed**:
- `HandleActivate` (40 lines) → **deleted**. Default handler + framework dispatch.
- `IsWithinCommandView` (15 lines) → **deleted**. Framework does source-guard check.
- `_activationBubbledUp` + `_deferredActivationContext` → **deleted**. Framework defers.
- `CommandView_Activated` event handler (18 lines) → **deleted**. Framework coordinates.
- `OnActivating` override (20 lines) → **deleted**. Framework calls `GetDispatchTarget`.
- `OnAccepting` override (20 lines) → **deleted**. Framework calls `GetDispatchTarget`.
- **Added**: One 1-line override: `GetDispatchTarget => CommandView`
- **Kept**: `OnActivated`, `OnAccepted`, `InvokeOnTargetOrApp`, `Action`, all properties.

Net: ~120 lines of the most complex, bug-prone code in the class replaced by a
single virtual method override.

---

### C. OptionSelector.cs (Redesigned)

OptionSelector currently has an `OnActivating` override with two branches
(IsBubblingUp consumption vs. fallthrough) and an `OnActivated` that must
check IsBubblingUp to avoid double-applying. With the new design, the
framework's dispatch machinery handles the bubble consumption via
`ConsumeDispatch = true`, and `ApplyActivation` moves entirely to `OnActivated`.

```csharp
public class OptionSelector : SelectorBase, IDesignable
{
    public OptionSelector () => base.Value = 0;

    // ──── Command Coordination ────

    /// <summary>
    ///     Dispatch to whichever CheckBox has focus. The framework will:
    ///     - Skip dispatch if the command already came from a CheckBox (source guard)
    ///     - Skip dispatch if no binding (programmatic invoke)
    ///     - Consume the command (ConsumeDispatch = true) so the originating
    ///       CheckBox does NOT call AdvanceCheckState independently
    /// </summary>
    protected override View? GetDispatchTarget (ICommandContext? ctx) => Focused;

    /// <summary>
    ///     Consume: OptionSelector owns the selection state, not the individual
    ///     CheckBoxes. When a CheckBox activation dispatches here, the framework
    ///     returns true (handled) so CheckBox.OnActivated/AdvanceCheckState is
    ///     suppressed.
    /// </summary>
    protected override bool ConsumeDispatch => true;

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);

        // Apply the value change in the completion phase. This runs for ALL
        // activation paths: direct invocation, dispatch from framework, or
        // programmatic. No need to check routing direction — the framework
        // already handled dispatch/consumption.
        ApplyActivation (ctx);
    }

    // ──── DELETED (now handled by framework) ────
    //
    // - protected override bool OnActivating (CommandEventArgs args)  — ~23 lines
    //   The IsBubblingUp check, ApplyActivation call, RaiseActivated call,
    //   and return true (consumption) are all replaced by:
    //     GetDispatchTarget => Focused
    //     ConsumeDispatch => true
    //
    // The two-branch structure (IsBubblingUp vs. direct) collapses because
    // the framework dispatches uniformly and OnActivated always applies.

    /// <summary>
    ///     Applies the value change based on the activation source.
    /// </summary>
    private void ApplyActivation (ICommandContext? ctx)
    {
        // Unchanged from current implementation
        if (ctx?.Source?.TryGetTarget (out View? sourceView) != true || sourceView is not CheckBox checkBox)
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

    // ──── Everything below is unchanged ────

    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is not CheckBox checkbox)
        {
            return;
        }

        checkbox.RadioStyle = true;
    }

    private void Cycle () { /* unchanged */ }
    public override void UpdateChecked () { /* unchanged */ }
    public int FocusedItem { get; set; }  // unchanged
    public bool EnableForDesign () { /* unchanged */ }
}
```

**What changed**:
- `OnActivating` override (23 lines) → **deleted**. Replaced by `GetDispatchTarget`
  + `ConsumeDispatch`.
- `OnActivated` override → **simplified**. No longer needs `IsBubblingUp` check.
  Always calls `ApplyActivation`.
- `ApplyActivation` → **unchanged**. Same logic, just called from one place now.
- **Added**: 2 one-line overrides (`GetDispatchTarget`, `ConsumeDispatch`)

Net: ~20 lines of routing logic replaced by 2 declarative overrides. The
remaining business logic (`ApplyActivation`, `Cycle`, `UpdateChecked`) is
identical.

---

### D. FlagSelector.cs (Redesigned)

FlagSelector is the most complex selector. Currently it has:
- `_suppressHotKeyActivate` flag + `OnHandlingHotKey` override to prevent toggle on focus
- `OnActivating` with 4 code paths (base handled, suppress, bubble consumption, BubbleDown)
- Direct CheckBox.Value manipulation in the bubble path

With the new design, `GetDispatchTarget` + `ConsumeDispatch` replace the bubble
consumption and BubbleDown paths. The HotKey suppress flag is eliminated because
the framework's dispatch guard (no dispatch for programmatic/no-binding invocations)
naturally handles it — `DefaultHotKeyHandler` calls `InvokeCommand(Activate)` without
a binding after `SetFocus`, so `GetDispatchTarget` dispatch is skipped.

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

    /// <summary>
    ///     Dispatch to whichever CheckBox has focus.
    /// </summary>
    protected override View? GetDispatchTarget (ICommandContext? ctx) => Focused;

    /// <summary>
    ///     Consume: FlagSelector owns the toggle semantics. When a CheckBox
    ///     activation dispatches here, the framework returns true (handled)
    ///     so CheckBox.OnActivated/AdvanceCheckState is suppressed.
    ///     FlagSelector toggles the checkbox value directly in OnActivated.
    /// </summary>
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

        // Not focused: restore focus. No need for _suppressHotKeyActivate flag —
        // DefaultHotKeyHandler calls InvokeCommand(Activate) without a binding,
        // so GetDispatchTarget dispatch is skipped by the framework's
        // programmatic-invoke guard.
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
        // This runs for all activation paths. For dispatch from framework
        // (user clicked a CheckBox, dispatched here via GetDispatchTarget),
        // the CheckBox's own AdvanceCheckState was suppressed by ConsumeDispatch.
        // For programmatic invocations (no dispatch), this handles it too.
        if (ctx?.Source?.TryGetTarget (out View? source) == true && source is CheckBox checkBox)
        {
            checkBox.Value = checkBox.Value == CheckState.Checked
                                 ? CheckState.UnChecked
                                 : CheckState.Checked;
        }

        // CheckboxOnValueChanged handler updates FlagSelector.Value bitmask
    }

    // ──── DELETED (now handled by framework) ────
    //
    // - private bool _suppressHotKeyActivate                          — flag
    // - protected override bool OnActivating (CommandEventArgs args)  — ~45 lines
    //   The 4 code paths (base, suppress, IsBubblingUp consumption,
    //   BubbleDown to focused) are all replaced by:
    //     GetDispatchTarget => Focused
    //     ConsumeDispatch => true
    //   The programmatic BubbleDown path (source == this, dispatch to Focused)
    //   is now handled by framework dispatch when binding is present.
    //   The _suppressHotKeyActivate is eliminated because DefaultHotKeyHandler's
    //   InvokeCommand(Activate) has no binding → framework skips dispatch.

    // ──── Everything below is unchanged ────

    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is not CheckBox checkbox)
        {
            return;
        }

        checkbox.RadioStyle = false;
        checkbox.ValueChanging += OnCheckboxOnValueChanging;
        checkbox.ValueChanged += CheckboxOnValueChanged;
    }

    private void OnCheckboxOnValueChanging (...) { /* unchanged */ }
    private void CheckboxOnValueChanged (...) { /* unchanged */ }

    private bool _updatingChecked;
    public override int? Value { get; set; }  // unchanged (with RaiseValueChanging etc.)
    public override void UpdateChecked () { /* unchanged */ }
    public override void CreateSubViews () { /* unchanged */ }
    public bool EnableForDesign () { /* unchanged */ }
}
```

**What changed**:
- `_suppressHotKeyActivate` flag → **deleted**. Framework's programmatic-invoke guard
  handles it.
- `OnActivating` override (45 lines, 4 code paths) → **deleted**. Replaced by
  `GetDispatchTarget` + `ConsumeDispatch`.
- `OnHandlingHotKey` → **simplified**. No flag to set; just SetFocus and return.
- `OnActivated` → **simplified**. Always toggles the source CheckBox directly.
  No routing-direction checks needed.
- **Added**: 2 one-line overrides (`GetDispatchTarget`, `ConsumeDispatch`)

Net: ~50 lines of the most complex routing logic (4 code paths with
IsBubblingUp/IsBubblingDown/suppress checks) replaced by 2 declarative
overrides. The toggle logic and value management are identical.

---

### Key Observation Across All Four Samples

| View | `GetDispatchTarget` | `ConsumeDispatch` | Lines Deleted | Lines Added |
|------|--------------------|--------------------|---------------|-------------|
| CheckBox | (not overridden) | (not overridden) | 0 | 0 |
| Shortcut | `=> CommandView` | `false` (default) | ~120 | 1 |
| OptionSelector | `=> Focused` | `true` | ~23 | 2 |
| FlagSelector | `=> Focused` | `true` | ~50 | 2 |

The pattern: **leaf views don't change. Composite views replace N lines of
hand-written routing with 1-2 declarative overrides.**
