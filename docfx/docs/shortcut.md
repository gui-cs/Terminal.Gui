# Deep Dive into Shortcut

## See Also

* [Command Deep Dive](command.md)
* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Events](events.md)
* [Mouse Deep Dive](mouse.md)

## From the User's Perspective

A `Shortcut` is a single, clickable row in a menu, toolbar, or status bar. It shows three things:

```
┌─────────────────────────────────────────────────┐
│ [CommandView]    [HelpView]         [KeyView]   │
│  _Open File      Opens a file       Ctrl+O      │
└─────────────────────────────────────────────────┘
```

**What the user expects:**

1. **Clicking anywhere on the Shortcut** activates it: toggles a checkbox, invokes the action, etc.
2. **Pressing the keyboard shortcut** (shown in KeyView, e.g., Ctrl+O) does the same thing, regardless of focus.
3. **Pressing the HotKey** (the underlined letter in CommandView, e.g., `O` in `_Open`) does the same thing.
4. **Pressing Space** while the Shortcut has focus activates it.
5. **Pressing Enter** while the Shortcut has focus accepts it (confirms/executes).
6. **Every interaction produces exactly one state change.** Clicking a Shortcut with a CheckBox toggles it once, not twice.

### CommandView Variants

The CommandView can be any View. Common configurations:

| CommandView Type | Activate Behavior | Accept Behavior |
|-----------------|-------------------|-----------------|
| **View** (default) | Invokes `Action` | Invokes `Action` |
| **CheckBox** | Toggles check state, invokes `Action` | Invokes `Action` (no toggle) |
| **Button** | Invokes `Action` | Invokes Button's Accept |
| **ColorPicker16** | Opens color dialog or cycles | Invokes `Action` |

### Key Principle: Single Responsibility

From the user's perspective, a Shortcut is **one control**. The fact that it contains three SubViews (CommandView, HelpView, KeyView) is an implementation detail. Whether the user clicks on the command text, the help text, the key text, or the gap between them, the result is the same.

## Design

### Commands and Their Semantics

Shortcut participates in the standard Command system with three commands:

| Command | Trigger | What It Does |
|---------|---------|-------------|
| **`Command.Activate`** | Space, click, `Shortcut.Key` press | Changes state (e.g., toggles CheckBox) and invokes `Action` |
| **`Command.Accept`** | Enter, double-click | Confirms/executes without state change; invokes `Action` |
| **`Command.HotKey`** | HotKey letter, `Shortcut.Key` | Sets focus, then invokes `Command.Activate` |

### CommandsToBubbleUp

`Shortcut` sets `CommandsToBubbleUp = [Command.Activate, Command.Accept]` in its constructor. This enables commands from SubViews (like CommandView) to bubble up to the Shortcut for centralized handling.

### The BubbleDown Pattern

Because Shortcut is a composite view, it must coordinate command flow between itself and its CommandView. The core pattern is:

1. **User interacts** with the Shortcut (clicks, presses key, etc.)
2. The command reaches `Shortcut.OnActivating` or `Shortcut.OnAccepting`
3. Shortcut **forwards the command down** to CommandView via `BubbleDown`
4. CommandView processes the command (e.g., CheckBox toggles)
5. `BubbleDown` suppresses re-bubbling (via `IsBubblingDown = true`), preventing infinite loops
6. Shortcut raises its own events and invokes `Action`

### When to BubbleDown (and When Not To)

The critical design decision is **when** Shortcut should forward a command to CommandView. The rule is:

```
BubbleDown to CommandView ONLY when:
  - The command has a Binding (i.e., it came from user interaction, not programmatic invoke)
  - AND the Binding.Source is NOT the CommandView (i.e., it didn't already come from CommandView)
```

This produces three paths:

| Origin | Has Binding? | Binding.Source | BubbleDown? | Reason |
|--------|-------------|---------------|-------------|--------|
| CommandView click/key | Yes | CommandView | **No** | CommandView already processed it; it bubbled up via `CommandsToBubbleUp` |
| Shortcut/HelpView/KeyView click, or Shortcut.Key press | Yes | Shortcut (or HelpView/KeyView) | **Yes** | CommandView hasn't seen this command yet |
| Programmatic `InvokeCommand` | No (null) | N/A | **No** | No user interaction to forward |

### Implementation

```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    if (base.OnActivating (args))
    {
        return true;
    }

    // Only bubble down when binding exists and source is not CommandView
    if (args.Context?.Binding is { Source: { } source } && source != CommandView)
    {
        return BubbleDown (CommandView, args.Context) is null;
    }

    return false;
}
```

### OnAccepting Behavior

When `Command.Accept` is invoked on a Shortcut:

1. `OnAccepting` is called
2. If the command came from a user binding (not from CommandView), it forwards `Accept` to CommandView via `BubbleDown`
3. `Action` is invoked via `OnAccepted`

**Accept does NOT invoke Activate.** These are separate command paths. Accept is for confirmation/execution; Activate is for state change.

```csharp
protected override bool OnAccepting (CommandEventArgs args)
{
    if (base.OnAccepting (args))
    {
        return true;
    }

    // Same BubbleDown logic as OnActivating
    if (args.Context?.Binding is { Source: { } source } && source != CommandView)
    {
        return BubbleDown (CommandView, args.Context) is null;
    }

    return false;
}

protected override void OnAccepted (ICommandContext? ctx) => Action?.Invoke ();
```

### OnActivated Behavior

After activation completes successfully (not cancelled), `OnActivated` invokes `Action`:

```csharp
protected override void OnActivated (ICommandContext? ctx)
{
    base.OnActivated (ctx);
    Action?.Invoke ();
}
```

## Detailed Command Flows

### Flow 1: Click on CommandView

When the user clicks on the CommandView area:

```
User clicks CommandView
  → CommandView.InvokeCommand(Activate) [from mouse binding]
  → CommandView.RaiseActivating()
    → CommandView.Activating event fires
    → TryBubbleUpToSuperView (Shortcut has Activate in CommandsToBubbleUp)
      → Shortcut.InvokeCommand(Activate) [with IsBubblingUp=true]
        → Shortcut.OnActivating(args)
          → args.Context.Binding.Source == CommandView → skip BubbleDown
          → return false
        → Shortcut.Activating event fires
    → CommandView.RaiseActivated()
      → CommandView state changes here (e.g., CheckBox toggles)
  → Shortcut.RaiseActivated()
    → Action?.Invoke()
```

**Result:** CommandView activates once. Shortcut events fire. Action invoked.

### Flow 2: Click on HelpView/KeyView/Shortcut Background

When the user clicks outside of CommandView but within the Shortcut:

Because Shortcut has `MouseHighlightStates = MouseState.In`, it intercepts mouse events for its entire area. The click is attributed to the Shortcut itself.

```
User clicks on Shortcut (not CommandView)
  → Shortcut.InvokeCommand(Activate) [from mouse binding, Source=Shortcut]
  → Shortcut.RaiseActivating()
    → Shortcut.OnActivating(args)
      → args.Context.Binding.Source == Shortcut (not CommandView) → BubbleDown!
      → BubbleDown(CommandView, ctx)
        → CommandView.InvokeCommand(Activate) [IsBubblingDown=true]
          → CommandView.RaiseActivating()
            → TryBubbleUpToSuperView: IsBubblingDown=true → skip
          → CommandView.RaiseActivated()
            → State changes here (e.g., CheckBox toggles)
    → Shortcut.Activating event fires
  → Shortcut.RaiseActivated()
    → Action?.Invoke()
```

**Result:** CommandView activates once (via BubbleDown). Shortcut events fire. Action invoked.

### Flow 3: Shortcut.Key Press (e.g., Ctrl+O)

```
User presses Shortcut.Key
  → Shortcut.InvokeCommand(HotKey) [from HotKeyBinding, Binding.Source=Shortcut]
  → Shortcut.DefaultHotKeyHandler(ctx)
    → RaiseHandlingHotKey(ctx) → HandlingHotKey event
    → SetFocus() (if CanFocus)
    → RaiseHotKeyCommand(ctx) → HotKeyCommand event
    → InvokeCommand(Activate, ctx.Binding) [passes original binding through]
      → Shortcut.RaiseActivating()
        → Shortcut.OnActivating(args)
          → args.Context.Binding.Source == Shortcut → BubbleDown!
          → BubbleDown(CommandView, ctx)
            → CommandView activates (state change)
        → Shortcut.Activating event fires
      → Shortcut.RaiseActivated()
        → Action?.Invoke()
```

**Key detail:** `DefaultHotKeyHandler` passes `ctx.Binding` when invoking `Command.Activate`, preserving the binding source so `OnActivating` can detect it was user-initiated and BubbleDown to CommandView.

### Flow 4: CommandView HotKey Press (e.g., Alt+O for "_Open")

```
User presses CommandView's HotKey letter
  → CommandView.InvokeCommand(HotKey) [from HotKeyBinding]
  → CommandView.DefaultHotKeyHandler(ctx)
    → RaiseHandlingHotKey → HandlingHotKey event on CommandView
    → SetFocus() (if CanFocus)
    → RaiseHotKeyCommand
    → InvokeCommand(Activate, ctx.Binding) [Source=CommandView]
      → CommandView.RaiseActivating()
        → Bubbles up to Shortcut (Activate in CommandsToBubbleUp)
          → Shortcut.OnActivating: Binding.Source == CommandView → skip BubbleDown
      → CommandView.RaiseActivated() → state changes
  → Shortcut.RaiseActivated() → Action?.Invoke()
```

### Flow 5: Space Key (Shortcut Focused)

```
User presses Space (Shortcut has focus)
  → Shortcut.InvokeCommand(Activate) [from KeyBinding, Source=Shortcut]
  → Same as Flow 2 (BubbleDown to CommandView)
```

### Flow 6: Enter Key (Shortcut Focused)

```
User presses Enter (Shortcut has focus)
  → Shortcut.InvokeCommand(Accept) [from KeyBinding, Source=Shortcut]
  → Shortcut.RaiseAccepting()
    → Shortcut.OnAccepting(args)
      → Binding.Source == Shortcut → BubbleDown(CommandView, Accept)
        → CommandView processes Accept
    → Shortcut.Accepting event fires
  → Shortcut.RaiseAccepted()
    → Action?.Invoke()
```

### Flow 7: Programmatic InvokeCommand

```
Code calls shortcut.InvokeCommand(Command.Activate)
  → Shortcut.RaiseActivating()
    → Shortcut.OnActivating(args)
      → args.Context.Binding == null → skip BubbleDown
      → return false
    → Shortcut.Activating event fires
  → Shortcut.RaiseActivated()
    → Action?.Invoke()
```

**Result:** Action invokes, but CommandView does NOT change state. This is by design: programmatic invocations should use `commandView.InvokeCommand(Command.Activate)` directly if they want to change CommandView state.

## MouseHighlightStates and Event Routing

`Shortcut` defaults to `MouseHighlightStates = MouseState.In`, which causes it to highlight on mouse hover and intercept mouse events for its entire area.

### With MouseHighlightStates = MouseState.In (Default)

- Clicks **anywhere** on the Shortcut are attributed to the **Shortcut** itself
- `Binding.Source` is the Shortcut
- Path: BubbleDown to CommandView (Flow 2)

### With MouseHighlightStates = MouseState.None

- Clicks on CommandView are attributed to **CommandView**
- `Binding.Source` is CommandView
- Path: Bubbles up from CommandView, skip BubbleDown (Flow 1)
- Clicks on HelpView/KeyView are attributed to those views, which bubble up to Shortcut

**Both paths produce the same result:** CommandView activates once, Shortcut events fire, Action invokes.

## Event Summary

### Events on Shortcut (for SuperView subscribers)

| Event | When Fired | Can Cancel? |
|-------|-----------|-------------|
| `HandlingHotKey` | When `Shortcut.Key` is pressed | Yes |
| `Activating` | During activation flow | Yes |
| `Activated` | After successful activation; `Action` invoked | No |
| `Accepting` | When `Command.Accept` invoked | Yes |
| `Accepted` | After successful accept; `Action` invoked | No |

### Events on CommandView (if subscribed directly)

| Event | When Fired | Notes |
|-------|-----------|-------|
| `Activating` | When CommandView activates | Fires once per interaction |
| `Activated` | After CommandView activates | State changes here for CheckBox |

### CheckBox-Specific Events

| Event | When Fired |
|-------|-----------|
| `CheckedStateChanging` | Before state toggle (cancellable) |
| `CheckedStateChanged` | After state toggle |

## Action Property

The `Action` property is invoked in two places:

1. **`OnActivated`**: After `Command.Activate` completes successfully
2. **`OnAccepted`**: After `Command.Accept` completes successfully

This means `Action` fires regardless of whether the Shortcut was activated (Space/click) or accepted (Enter).

## How To

### Handle Activation Differently Based on Source

Use `args.Context.TryGetSource()` in the `Activating` event handler to determine whether the user interacted with the CommandView directly or with the Shortcut:

```csharp
Shortcut shortcut = new ()
{
    Key = Key.F9,
    HelpText = "Cycles BG Color",
    CommandView = bgColor
};

shortcut.Activating += (_, args) =>
{
    if (args.Context.TryGetSource (out View? source) && source == shortcut.CommandView)
    {
        // User clicked directly on the CommandView
        args.Handled = true; // Let CommandView handle it normally
    }
    else
    {
        // User pressed F9 or clicked elsewhere on the Shortcut
        bgColor.SelectedColor++; // Custom logic
    }
};
```

### Use Shortcut with a CheckBox

```csharp
Shortcut shortcut = new ()
{
    Key = Key.F6,
    CommandView = new CheckBox { Text = "Force 16 Colors" }
};

// Subscribe to the CheckBox state changes
((CheckBox)shortcut.CommandView).CheckedStateChanged += (_, args) =>
{
    bool isChecked = args.CurrentValue == CheckState.Checked;
    // React to state change
};

// Or subscribe to the Shortcut's Action for simple callbacks
shortcut.Action = () => DoSomething ();
```

## Design Rationale

### Why BubbleDown?

Without BubbleDown, clicking on the HelpView or KeyView area would not toggle a CheckBox CommandView. BubbleDown ensures that **all** user interactions with the Shortcut reach the CommandView, maintaining the "single control" illusion.

### Why Check Binding.Source?

The three-way check (has binding? source is CommandView? programmatic?) prevents:

1. **Double-processing**: When CommandView raises Activate and it bubbles up to Shortcut, Shortcut should not BubbleDown back to CommandView (infinite loop / double toggle).
2. **Unwanted side effects**: Programmatic `InvokeCommand` on the Shortcut should not automatically change CommandView state - the caller should be explicit.

### Why Accept Does Not Invoke Activate?

Accept and Activate are distinct semantic actions:

- **Activate** = "interact with this control" (toggle, select, change state)
- **Accept** = "confirm/execute" (submit, close menu, run command)

Conflating them causes confusion in composite views like Menu, where Accept on a MenuItem should execute the command and close the menu, but Activate should just highlight/focus the item.

### Comparison with SelectorBase/FlagSelector

`FlagSelector` is another composite view that uses `BubbleDown`, but with intentionally different semantics:

| | Shortcut | FlagSelector |
|--|---------|-------------|
| **Check** | `Binding.Source` | `Context.Source` (via `TryGetSource`) |
| **Programmatic invoke** | Skip BubbleDown | BubbleDown to focused checkbox |
| **From SubView** | Skip (already processed) | Skip (already processed) |
| **From self** | BubbleDown to CommandView | BubbleDown to focused checkbox |

**Why the difference?** FlagSelector is a container for N equivalent checkboxes; programmatic `InvokeCommand(Activate)` naturally means "toggle the focused item." Shortcut is a composite with one CommandView; programmatic invoke should raise Shortcut's own events/Action without implicitly changing CommandView state. Callers who want to change CommandView state should call `commandView.InvokeCommand(Activate)` directly.

`OptionSelector` takes a different approach entirely: it subscribes to checkbox `Activating` events and manually calls `InvokeCommand(Command.Activate, args.Context)` on itself, bypassing the BubbleDown pattern. This works but has a TODO noting it shouldn't be needed.
