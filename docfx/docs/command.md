# Deep Dive into Command and View.Command in Terminal.Gui

## See Also

* [Lexicon & Taxonomy](lexicon.md)
* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Events](events.md)

## Overview

The `Command` system in Terminal.Gui provides a standardized framework for defining and executing actions that views can perform, such as selecting items, accepting input, or navigating content. Implemented primarily through the `View.Command` APIs, this system integrates tightly with input handling (e.g., keyboard and mouse events) and leverages the *Cancellable Work Pattern* to ensure extensibility, cancellation, and decoupling. Central to this system are the `Activating/Activated` and `Accepting/Accepted` events, which encapsulate common user interactions: `Activated` for changing a view's state or preparing it for interaction (e.g., toggling a checkbox, focusing a menu item), and `Accepted` for confirming an action or state (e.g., executing a menu command, accepting a ListView, submitting a dialog).

This deep dive explores the `Command` and `View.Command` APIs and the default implementations of standardized commands including `Command.Activate`, `Command.Accept`, and `Command.HotKey`.

This diagram shows the fundamental command invocation flow within a single view, demonstrating the Cancellable Work Pattern with pre-events (e.g., `Activating`, `Accepting`) and opt-in bubbling via `CommandsToBubbleUp`.

```mermaid
flowchart TD
    input["User input (key/mouse)"] --> invoke["View.InvokeCommand(command)"]
    invoke --> |Command.Activate| act_pre["RaiseActivating: OnActivating + Activating event"]
    invoke --> |Command.Accept| acc_pre["RaiseAccepting: OnAccepting + Accepting event"]
    invoke --> |Command.HotKey| hk_pre["RaiseHandlingHotKey: OnHandlingHotKey + HandlingHotKey event"]

    act_pre --> |handled| act_stop["Stop"]
    act_pre --> |not handled| act_bubble["TryBubbleToSuperView"]
    act_bubble --> |bubbled & handled| act_stop2["Stop"]
    act_bubble --> |not bubbled| act_handler["SetFocus + RaiseActivated"]
    act_handler --> act_done["Complete (returns false)"]

    acc_pre --> |handled| acc_stop["Stop"]
    acc_pre --> |not handled| acc_bubble["TryBubbleToSuperView (DefaultAcceptView + CommandsToBubbleUp)"]
    acc_bubble --> |bubbled & handled| acc_stop2["Stop"]
    acc_bubble --> |not bubbled| acc_handler["RaiseAccepted"]
    acc_handler --> acc_done["Complete (returns false)"]

    hk_pre --> |handled| hk_stop["Stop"]
    hk_pre --> |not handled| hk_bubble["TryBubbleToSuperView"]
    hk_bubble --> |bubbled & handled| hk_stop2["Stop"]
    hk_bubble --> |not bubbled| hk_handler["SetFocus + RaiseHotKeyCommand + InvokeCommand(Activate)"]
    hk_handler --> hk_done["Complete (returns false)"]
```

## Activate/Accept/HotKey System Summary

| Aspect | `Command.Activate` | `Command.Accept` | `Command.HotKey` |
|--------|-------------------|------------------|-------------------|
| **Semantic Meaning** | "Interact with this view / select an item" - changes view state or prepares for interaction | "Perform the view's primary action" - confirms action or accepts current state | "The view's HotKey was pressed" - sets focus and activates |
| **Typical Triggers** | Spacebar, single mouse click, navigation keys (arrows), mouse enter (menus) | Enter key, double-click | HotKey letter (e.g., Alt+F), `Shortcut.Key` |
| **Pre-Virtual Method** | `OnActivating` | `OnAccepting` | `OnHandlingHotKey` |
| **Pre-Event Name** | `Activating` | `Accepting` | `HandlingHotKey` |
| **Post-Virtual Method** | `OnActivated` | `OnAccepted` | `OnHotKeyCommand` |
| **Post-Event Name** | `Activated` | `Accepted` | `HotKeyCommand` |
| **Bubbling** | Opt-in via `CommandsToBubbleUp` | Opt-in via `CommandsToBubbleUp` + `DefaultAcceptView` | Opt-in via `CommandsToBubbleUp` |

## View Command Behaviors

The following table documents how `View` and each View subclass binds or handles keyboard and mouse events. This provides a comprehensive reference for understanding which commands are bound to specific inputs or whether views handle events directly through method overrides.

| View | Space | Enter | HotKey | Pressed | Released | Clicked | DoubleClicked |
|------|-------|-------|--------|---------|----------|---------|---------------|
| **View** (base) | `Command.Activate` (default) | `Command.Accept` (default) | `Command.HotKey` (default) | Base OnMouseEvent (updates MouseState) | `Command.Activate` (default) | Not bound by default | Not bound by default |
| **Button** | `Command.Accept` | `Command.Accept` | `Command.HotKey` (calls RaiseAccepting) | OnMouseEvent (updates MouseState) | `Command.Activate` (inherited) | Not bound (overridden) | Not bound (overridden) |
| **CheckBox** | `Command.Activate` | `Command.Accept` | `Command.HotKey` | `Command.Activate` | Base OnMouseEvent | `Command.Activate` | `Command.Accept` |
| **ComboBox** | Handled by SubViews | Handled by SubViews | `Command.HotKey` | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **ListView** | Custom handler (selection) | `Command.Accept` | `Command.HotKey` | Base OnMouseEvent | Base OnMouseEvent | OnMouseEvent (selects item) | `Command.Accept` |
| **TableView** | Custom handler (toggle selection) | `Command.Accept` | `Command.HotKey` | OnMouseEvent (cell selection) | OnMouseEvent (end drag) | OnMouseEvent (cell selection) | `Command.Accept` |
| **TreeView** | `Command.Accept` | `Command.Accept` | `Command.HotKey` | Base OnMouseEvent | Base OnMouseEvent | OnMouseEvent (node selection) | `Command.Accept` |
| **TextField** | OnKeyDown (inserts space) | `Command.Accept` | `Command.HotKey` | OnMouseEvent (set cursor) | OnMouseEvent (end drag) | OnMouseEvent (position cursor) | OnMouseEvent (select word) |
| **TextView** | OnKeyDown (inserts space) | OnKeyDown (inserts newline) | `Command.HotKey` | OnMouseEvent (set cursor) | OnMouseEvent (end drag) | OnMouseEvent (position cursor) | OnMouseEvent (select word) |
| **OptionSelector** | Forwards to SubView | `Command.Accept` | Forwards to SubView HotKey | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **FlagSelector** | Forwards to SubView | `Command.Accept` | Forwards to SubView HotKey | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **Menu** | Handled by SubViews | `Command.Accept` | `Command.HotKey` | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **MenuBar** | Handled by SubViews | `Command.Accept` | `Command.HotKey` | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **MenuItem** | Base handler | `Command.Accept` | `Command.HotKey` | Base OnMouseEvent | Base OnMouseEvent | `Command.Activate` | `Command.Accept` |
| **Shortcut** | `Command.HotKey` | `Command.HotKey` | `Command.HotKey` | OnMouseEvent (updates MouseState) | OnMouseEvent (updates MouseState) | `Command.HotKey` | `Command.HotKey` |
| **Dialog** | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **Wizard** | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **FileDialog** | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **TabView** | Not bound | Not bound | `Command.HotKey` | Handled by SubViews | Handled by SubViews | Handled by SubViews | Not bound |
| **ScrollBar** | Not bound | Not bound | Not bound | OnMouseEvent (auto-repeat/jump) | OnMouseEvent (auto-repeat) | OnMouseEvent (jump position) | Not bound |
| **HexView** | OnKeyDown (custom) | Not bound | Not bound | OnMouseEvent (position cursor) | Base OnMouseEvent | OnMouseEvent (position cursor) | OnMouseEvent (toggle side) |
| **NumericUpDown** | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **DatePicker** | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **ColorPicker** | OnKeyDown (custom) | Not bound | Handled by SubViews | OnMouseEvent (adjust value) | Base OnMouseEvent | OnMouseEvent (adjust value) | `Command.Accept` |
| **ProgressBar** | N/A | N/A | N/A | N/A | N/A | N/A | N/A |
| **SpinnerView** | N/A | N/A | N/A | N/A | N/A | N/A | N/A |
| **Bar** | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews | Handled by SubViews |
| **Label** | Not bound | Not bound | Forwards to next focusable | Not bound | Not bound | Not bound | Not bound |

### Notes on Command Behaviors

#### Table Notation

The table shows how each view handles keyboard and mouse input using one of these approaches:

- **`Command.X`** - Input is bound to a command via KeyBinding or MouseBinding (e.g., `Command.HotKey`, `Command.Activate`, `Command.Accept`)
- **OnKeyDown (custom)** - Input is handled directly by overriding `OnKeyDown` with view-specific logic
- **OnMouseEvent (description)** - Input is handled directly by overriding `OnMouseEvent` with view-specific behavior
- **Base OnMouseEvent** - Input uses the base `View.OnMouseEvent` implementation (updates MouseState)
- **Custom handler** - Input uses a view-specific handler method (not a command)
- **Handled by SubViews** - Composite views delegate input handling to their contained SubViews
- **Forwards to SubView** - Input is forwarded to a specific SubView (e.g., OptionSelector -> CheckBox)
- **Not bound** - Input is not handled or bound by this view

#### Key Points

1. **View Base Class**: The first row shows the default behavior provided by the base `View` class. Space and Enter are bound to `Command.Activate` and `Command.Accept` respectively in `SetupCommands()`. Mouse events use the base `OnMouseEvent` implementation which updates `MouseState`. Subclasses typically override these bindings or add MouseBindings for Clicked/DoubleClicked events.

2. **Composite Views** (Dialog, Wizard, FileDialog, DatePicker, NumericUpDown, Bar): These views delegate input handling to their SubViews. The SuperView may intercept commands to coordinate actions (e.g., Dialog intercepting `Accept` to set `Result`).

3. **Display-Only Views** (ProgressBar, SpinnerView, Label): These views typically have `CanFocus = false` and do not handle keyboard or mouse input directly.

4. **Command Bindings vs. Event Handlers**: Views with simple, standardized behaviors use **command bindings** (KeyBinding/MouseBinding -> Command). Views requiring custom logic (e.g., text editing, cursor positioning, drag selection) override **OnKeyDown** or **OnMouseEvent** directly.

5. **TreeView Special Case**: Both Space and Enter are bound to `Command.Accept`, which invokes the same handler (`ActivateSelectedObjectIfAny`).

6. **Shortcut and Button Unified Handling**: Space, Enter, Clicked, and DoubleClicked all map to `Command.HotKey`, providing consistent activation behavior.

7. **Selector Views** (OptionSelector, FlagSelector): These forward Space and HotKey inputs to the focused CheckBox's handlers, enabling keyboard-driven selection changes.

8. **Text Input Views** (TextField, TextView): These override OnKeyDown to handle Space (inserts space character) and OnMouseEvent for cursor positioning, text selection, and drag operations. Enter is bound to `Command.Accept` in TextField (submit), but handled directly in TextView (inserts newline).

9. **Mouse Event Columns**:
   - **Pressed**: `MouseFlags.LeftButtonPressed` - button initially pressed down
   - **Released**: `MouseFlags.LeftButtonReleased` - button released after press
   - **Clicked**: `MouseFlags.LeftButtonClicked` - synthesized from press+release in same location
   - **DoubleClicked**: `MouseFlags.LeftButtonDoubleClicked` - synthesized from timing of two clicks
   - For detailed information about the mouse event pipeline and how events are synthesized, see the [Mouse Deep Dive](mouse.md).

10. **Implementation Patterns**: To understand how bindings work, see:
    - `Terminal.Gui/ViewBase/Mouse/View.Mouse.cs` - Base mouse handling and MouseBindings
    - `Terminal.Gui/ViewBase/Keyboard/View.Keyboard.cs` - Base keyboard handling and KeyBindings
    - Individual view source files for view-specific overrides and custom handlers

### Key Takeaways

1. **`Activate` = Interaction/Selection** (immediate, local by default)
   - Changes view state or sets focus
   - Views that implement `IValue` will emit `ValueChanging`/`ValueChanged` events.
   - Views can emit view-specific events for notification (e.g., `CheckedStateChanged`, `SelectedMenuItemChanged`)
   - Bubbles to SuperView only if `SuperView.CommandsToBubbleUp` includes `Command.Activate`

2. **`Accept` = Confirmation/Action** (final, hierarchical)
   - Confirms current state or executes primary action
   - `View.DefaultAcceptView` is the SubView that has `Command.Accept` invoked on it if no other SubView handles `Accept`.
   - Bubbles to SuperView if `SuperView.CommandsToBubbleUp` includes `Command.Accept`
   - Enables dialog/menu close scenarios

3. **`HotKey` = Focus + Activate** (delegated)
   - Sets focus to the view and then invokes `Command.Activate`
   - Bubbles to SuperView only if `SuperView.CommandsToBubbleUp` includes `Command.HotKey`

## Overview of the Command System

The `Command` system in Terminal.Gui defines a set of standard actions via the `Command` enum (e.g., `Command.Activate`, `Command.Accept`, `Command.HotKey`, `Command.StartOfPage`). These actions are triggered by user inputs (e.g., key presses, mouse clicks) or programmatically, enabling consistent view interactions.

### Key Components
- **Command Enum**: Defines actions like `Activate` (state change or interaction preparation), `Accept` (action confirmation), `HotKey` (hotkey activation), and others (e.g., `StartOfPage` for navigation).
- **Command Handlers**: Views register handlers using `View.AddCommand`, specifying a `CommandImplementation` delegate that returns `bool?` (`null`: no command executed; `false`: executed but not handled; `true`: handled or canceled).
- **Command Routing**: Commands are invoked via `View.InvokeCommand`, executing the handler or raising `CommandNotBound` if no handler exists.
- **Cancellable Work Pattern**: Command execution uses events (e.g., `Activating`, `Accepting`) and virtual methods (e.g., `OnActivating`, `OnAccepting`) for modification or cancellation, with `Handled` indicating processing should stop.

### Role in Terminal.Gui
The `Command` system bridges user input and view behavior, enabling:
- **Consistency**: Standard commands ensure predictable interactions (e.g., `Enter` and `Double-click` trigger `Accept` in buttons, menus, checkboxes).
- **Extensibility**: Custom handlers and events allow behavior customization.
- **Decoupling**: Events reduce reliance on sub-classing, and `CommandsToBubbleUp` provides structured command propagation up the view hierarchy.

## Implementation in View.Command

The `View.Command` APIs in the `View` class provide infrastructure for registering, invoking, and routing commands, adhering to the *Cancellable Work Pattern*. `View` provides default implementations of four commands:

* `Command.Activate` - Bound to `Key.Space` and `MouseFlags.LeftButtonReleased`. The default handler (`DefaultActivateHandler`) calls `RaiseActivating`; if not handled, sets focus and calls `RaiseActivated`.
* `Command.Accept` - Bound to `Key.Enter`. The default handler (`DefaultAcceptHandler`) calls `RaiseAccepting`; if not handled, calls `RaiseAccepted`.
* `Command.HotKey` - Bound to `View.HotKey`. The default handler (`DefaultHotKeyHandler`) calls `RaiseHandlingHotKey`; if not handled, sets focus, calls `RaiseHotKeyCommand`, then invokes `Command.Activate`.
* `Command.NotBound` - Invoked when an unregistered command is triggered. Raises the `CommandNotBound` event.

### Command Registration
Views register commands using `View.AddCommand`, associating a `Command` with a `CommandImplementation` delegate. The delegate's `bool?` return controls processing flow.

### Command Invocation
Commands are invoked via `View.InvokeCommand` or `View.InvokeCommands`, passing an `ICommandContext` for context (e.g., source view, binding details). Unhandled commands trigger `CommandNotBound`.

**Example**:
```csharp
public bool? InvokeCommand (Command command, ICommandContext? ctx)
{
    if (!_commandImplementations.TryGetValue (command, out CommandImplementation? implementation))
    {
        _commandImplementations.TryGetValue (Command.NotBound, out implementation);
    }

    return implementation! (ctx);
}
```

### Command Routing and Bubbling

By default, commands route directly to the target view and processing stops after the view's handler returns. Command **bubbling** - where an unhandled command propagates up to the SuperView - is **opt-in** and controlled by `View.CommandsToBubbleUp`.

#### `CommandsToBubbleUp`

`CommandsToBubbleUp` is a property on `View` that specifies which commands should bubble up from unhandled SubViews to the SuperView. When a SubView raises a command that is not handled, and that command is in the SuperView's `CommandsToBubbleUp` list, the command will be invoked on the SuperView.

```csharp
public IReadOnlyList<Command> CommandsToBubbleUp { get; set; } = [];
```

By default, `CommandsToBubbleUp` is empty (no bubbling). Views that need hierarchical command propagation opt in explicitly:

| View | `CommandsToBubbleUp` |
|------|---------------------|
| **Shortcut** | `[Command.Activate, Command.Accept]` |
| **Dialog** | `[Command.Accept]` |
| **Menu** | `[Command.Accept, Command.Activate]` |
| **SelectorBase** (OptionSelector, FlagSelector) | `[Command.Activate, Command.Accept]` |

#### `TryBubbleToSuperView`

All three `Raise` methods (`RaiseAccepting`, `RaiseActivating`, `RaiseHandlingHotKey`) call the unified `TryBubbleToSuperView` helper when the command is not handled. This method:

1. **Checks for `DefaultAcceptView`** (only for `Command.Accept`): If the SuperView has a `DefaultAcceptView` (e.g., a `Button` with `IsDefault = true`), the command is first invoked on that view.
2. **Checks `SuperView.CommandsToBubbleUp`**: If the current command is in the SuperView's `CommandsToBubbleUp` list, the command is invoked on the SuperView.
3. **Handles the Padding edge case**: If the SuperView is a `Padding` adornment, checks the Padding's parent View's `CommandsToBubbleUp` instead.

```mermaid
flowchart TD
    start["TryBubbleToSuperView(ctx, handled)"] --> check_handled{handled?}
    check_handled --> |yes| return_true["return true"]
    check_handled --> |no| check_accept{Command == Accept?}
    check_accept --> |yes| check_default{"SuperView has DefaultAcceptView?"}
    check_default --> |yes| invoke_default["Invoke Accept on DefaultAcceptView"]
    invoke_default --> default_handled{handled?}
    default_handled --> |yes| return_true2["return true"]
    default_handled --> |no| check_bubble{"Command in SuperView.CommandsToBubbleUp?"}
    check_default --> |no| check_bubble
    check_accept --> |no| check_bubble
    check_bubble --> |yes| invoke_super["Invoke command on SuperView"]
    check_bubble --> |no| check_padding{"SuperView is Padding?"}
    check_padding --> |yes| check_parent{"Command in Padding.Parent.CommandsToBubbleUp?"}
    check_parent --> |yes| invoke_parent["Invoke command on Padding.Parent"]
    check_parent --> |no| return_false["return false"]
    check_padding --> |no| return_false
```

#### `BubbleDown`

`BubbleDown` is the inverse of `TryBubbleToSuperView`. Where bubbling up propagates an unhandled command from a SubView to its SuperView, `BubbleDown` dispatches a command from a SuperView down to a specific SubView with bubbling suppressed.

```csharp
protected bool? BubbleDown (View target, ICommandContext? ctx)
```

This method:
1. Creates a new `CommandContext` with `IsBubblingDown = true` and no binding
2. Invokes the command on the target
3. Because `IsBubblingDown` is true, `TryBubbleToSuperView` in the target's Raise method skips bubbling, preventing infinite recursion

`BubbleDown` is used by composite views (like `Shortcut` and `SelectorBase`) that need to forward a command to a SubView without the SubView's command bubbling back up to the SuperView that dispatched it.

```mermaid
flowchart LR
    super["SuperView receives command"] --> bubble_down["BubbleDown(subView, ctx)"]
    bubble_down --> new_ctx["Create CommandContext with IsBubblingDown=true"]
    new_ctx --> invoke["subView.InvokeCommand(command, ctx)"]
    invoke --> raise["SubView raises Activating/Accepting"]
    raise --> try_bubble["TryBubbleToSuperView checks IsBubblingDown"]
    try_bubble --> skip["IsBubblingDown=true → skip bubbling"]
```

#### `DefaultAcceptView`

`DefaultAcceptView` is a special property on `View` that identifies the SubView that should receive `Command.Accept` when no other SubView handles it. By default, it returns the first `Button` SubView with `IsDefault = true`, but can be set explicitly.

```csharp
public View? DefaultAcceptView
{
    get
    {
        if (field is null)
        {
            return GetSubViews (includePadding: true).FirstOrDefault (v => v is Button { IsDefault: true });
        }

        return field;
    }
    set => field = value;
}
```

This enables the common pattern where pressing Enter in a `TextField` within a `Dialog` activates the default "OK" button.

## The Activating and Accepting Concepts

The `Activating` and `Accepting` events, along with their corresponding commands (`Command.Activate`, `Command.Accept`), are designed to handle the most common user interactions with views:
- **Activating**: Changing a view's state or preparing it for further interaction, such as highlighting an item in a list, toggling a checkbox, or focusing a menu item.
- **Accepting**: Confirming an action or state, such as submitting a form, activating a button, or finalizing a selection.

These concepts are opinionated, reflecting Terminal.Gui's view that most UI interactions can be modeled as either state changes/preparation (selecting) or action confirmations (accepting). Below, we explore each concept, their implementation, use cases, and propagation behavior, using `Handled` to reflect the current implementation.

### Activating

- **Definition**: `Activating` represents a user action that changes a view's state or prepares it for further interaction, such as selecting an item in a `ListView`, toggling a `CheckBox`, or focusing a `MenuItem`. It is associated with `Command.Activate`, typically triggered by a spacebar press, single mouse click, navigation keys (e.g., arrow keys), or mouse enter (e.g., in menus).
- **Event**: The `Activating` event is raised by `RaiseActivating`, allowing external code to modify or cancel the state change.
- **Virtual Method**: `OnActivating` enables subclasses to preprocess or cancel the action.
- **Implementation**:
  ```csharp
  protected bool? RaiseActivating (ICommandContext? ctx)
  {
      CommandEventArgs args = new () { Context = ctx };

      if (OnActivating (args) || args.Handled)
      {
          return true;
      }

      Activating?.Invoke (this, args);

      if (!args.Handled)
      {
          args.Handled = TryBubbleToSuperView (ctx, args.Handled) is true;
      }

      return args.Handled;
  }
  ```
  - **Default Behavior**: If not handled, the default handler sets focus (if `CanFocus` is true) and raises `Activated`.
  - **Cancellation**: `args.Handled` or `OnActivating` returning `true` halts the command.
  - **Bubbling**: If not handled by the view or its event subscribers, `TryBubbleToSuperView` checks if the SuperView's `CommandsToBubbleUp` includes `Command.Activate`.
  - **Context**: `ICommandContext` provides invocation details (source view, binding).

- **Use Cases**:
  - **ListView**: Activating an item (e.g., via arrow keys or mouse click) raises `Activating` to update the highlighted item.
  - **CheckBox**: Toggling the checked state (e.g., via spacebar) raises `Activating` to change the state, as seen in the `AdvanceAndSelect` method:
    ```csharp
    private bool? AdvanceAndSelect (ICommandContext? commandContext)
    {
        bool? cancelled = AdvanceCheckState ();

        if (cancelled is true)
        {
            return true;
        }

        if (RaiseActivating (commandContext) is true)
        {
            return true;
        }

        return commandContext?.Command == Command.HotKey ? cancelled : cancelled is false;
    }
    ```
  - **OptionSelector**: Activating an OptionSelector option raises `Activating` to update the selected option.
  - **Menu** and **MenuBar**: Activating a `MenuItem` (e.g., via mouse enter or arrow keys) sets focus, tracked by `SelectedMenuItem` and raising `SelectedMenuItemChanged`.
  - **Views without State**: For views like `Button`, `Activating` typically sets focus but does not change state, making it less relevant.

- **Propagation**: `Command.Activate` bubbling is opt-in. If the command is unhandled and the SuperView's `CommandsToBubbleUp` includes `Command.Activate`, the command is invoked on the SuperView. Views that enable this include `Shortcut`, `Menu`, and `SelectorBase`.

### Accepting

- **Definition**: `Accepting` represents a user action that confirms or finalizes a view's state or triggers an action, such as submitting a dialog, activating a button, or confirming a selection in a list. It is associated with `Command.Accept`, typically triggered by the Enter key or double-click.
- **Event**: The `Accepting` event is raised by `RaiseAccepting`, allowing external code to modify or cancel the action.
- **Virtual Method**: `OnAccepting` enables subclasses to preprocess or cancel the action.
- **Implementation**:
  ```csharp
  protected bool? RaiseAccepting (ICommandContext? ctx)
  {
      CommandEventArgs args = new () { Context = ctx };

      args.Handled = OnAccepting (args) || args.Handled;

      if (!args.Handled && Accepting is { })
      {
          Accepting?.Invoke (this, args);
      }

      if (!args.Handled)
      {
          args.Handled = TryBubbleToSuperView (ctx, args.Handled) is true;
      }

      return args.Handled;
  }
  ```
  - **Default Behavior**: If not handled, the default handler calls `RaiseAccepted`.
  - **Cancellation**: `args.Handled` or `OnAccepting` returning `true` halts the command.
  - **Bubbling**: If not handled, `TryBubbleToSuperView` first checks for a `DefaultAcceptView` (e.g., an `IsDefault` button), then checks `SuperView.CommandsToBubbleUp`.
  - **Context**: `ICommandContext` provides invocation details.

- **Use Cases**:
  - **Button**: Pressing Enter raises `Accepting` to activate the button (e.g., submit a dialog).
  - **ListView**: Double-clicking or pressing Enter raises `Accepting` to confirm the selected item(s).
  - **TextField**: Pressing Enter raises `Accepting` to submit the input.
  - **Menu** and **MenuBar**: Pressing Enter on a `MenuItem` raises `Accepting` to execute a command or open a submenu, followed by the `Accepted` event to hide the menu or deactivate the menu bar.
  - **CheckBox**: Pressing Enter raises `Accepting` to confirm the current `CheckedState` without modifying it.
  - **Dialog**: `Accepting` on a default button closes the dialog or triggers an action.

- **Propagation**: `Command.Accept` bubbling is opt-in via `CommandsToBubbleUp`, with the added special case that `DefaultAcceptView` is checked first. This enables the pattern where pressing Enter in a `TextField` activates the dialog's default button. Views that enable Accept bubbling include `Dialog` (`[Command.Accept]`), `Shortcut` (`[Command.Activate, Command.Accept]`), and `Menu` (`[Command.Accept, Command.Activate]`).

### HotKey

- **Definition**: `HotKey` represents the user pressing a view's designated hot key. It is associated with `Command.HotKey`, typically triggered by the view's `HotKey` property or a `Shortcut.Key`.
- **Event**: The `HandlingHotKey` event is raised by `RaiseHandlingHotKey`, allowing external code to cancel the hot key handling.
- **Virtual Method**: `OnHandlingHotKey` enables subclasses to preprocess or cancel the action.
- **Implementation**:
  The default handler (`DefaultHotKeyHandler`) follows this sequence:
  1. Calls `RaiseHandlingHotKey` (which calls `OnHandlingHotKey`, raises `HandlingHotKey`, and attempts bubbling if unhandled)
  2. If not handled, calls `SetFocus ()` (if `CanFocus`)
  3. Calls `RaiseHotKeyCommand` (calls `OnHotKeyCommand` and raises `HotKeyCommand`)
  4. Invokes `Command.Activate` on the view

  ```csharp
  internal bool? DefaultHotKeyHandler (ICommandContext? ctx)
  {
      if (RaiseHandlingHotKey (ctx) is true)
      {
          return true;
      }

      if (CanFocus)
      {
          SetFocus ();
      }

      RaiseHotKeyCommand (ctx);

      InvokeCommand (Command.Activate);

      return false;
  }
  ```

- **Propagation**: Like `Activate` and `Accept`, `HotKey` bubbling is opt-in via `CommandsToBubbleUp`. `RaiseHandlingHotKey` calls `TryBubbleToSuperView` when unhandled.

## Shortcut Command Dispatching

`Shortcut` is a composite view that contains three SubViews: `CommandView`, `HelpView`, and `KeyView`. It uses `CommandsToBubbleUp = [Command.Activate, Command.Accept]` to receive commands from its SubViews.

### The BubbleDown Pattern

Because `Shortcut` is a composite view, commands can originate from different SubViews (e.g., clicking on the `CommandView`, clicking on the `KeyView`, or pressing a hotkey on the `Shortcut` itself). The `Shortcut` uses `BubbleDown` to coordinate command flow.

When a command arrives at `Shortcut` via `OnActivating` or `OnAccepting`, the Shortcut checks the command's binding source:

- **From `CommandView`** (binding source is the `CommandView`): The `CommandView` already processed the command (e.g., a `CheckBox` toggled itself). The Shortcut skips `BubbleDown` to avoid double-processing.
- **From Shortcut itself, `HelpView`, or `KeyView`** (binding source exists but is not `CommandView`): The Shortcut calls `BubbleDown (CommandView, args.Context)` to forward the command to `CommandView` with bubbling suppressed, allowing `CommandView` to update its state.
- **No binding** (programmatic `InvokeCommand`): The Shortcut skips `BubbleDown` since there is no user interaction to forward.

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
        BubbleDown (CommandView, args.Context);
    }

    return false;
}
```

#### Flow Diagram

```mermaid
flowchart TD
    input["User action on Shortcut"] --> check_binding{"Has Binding with Source?"}

    check_binding --> |No binding| skip["Skip BubbleDown (programmatic invoke)"]
    skip --> raise_events1["Shortcut raises Activating/Accepting normally"]

    check_binding --> |Yes| check_source{"Binding.Source == CommandView?"}

    check_source --> |Yes - from CommandView| skip2["Skip BubbleDown (CommandView already processed)"]
    skip2 --> raise_events2["Shortcut raises Activating/Accepting normally"]

    check_source --> |No - from Shortcut/HelpView/KeyView| bubble["BubbleDown(CommandView, ctx)"]
    bubble --> invoke["CommandView.InvokeCommand with IsBubblingDown=true"]
    invoke --> cv_update["CommandView updates state (e.g., CheckBox toggles)"]
    cv_update --> no_rebubble["TryBubbleToSuperView skips (IsBubblingDown=true)"]
    no_rebubble --> raise_events3["Shortcut raises Activating/Accepting normally"]
```

### SelectorBase Command Dispatching

`SelectorBase` (used by `OptionSelector` and `FlagSelector`) follows the same `BubbleDown` pattern. When `SelectorBase` receives a `Command.Activate`, it forwards it to the focused `CheckBox` SubView using `BubbleDown`:

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

    BubbleDown (Focused, args.Context);

    return false;
}
```

### Shortcut's `Action` Property

`Shortcut` has an `Action` property that is invoked in `OnActivated` and `OnAccepted`. This provides a simple callback mechanism for handling shortcut activation:

```csharp
protected override void OnActivated (ICommandContext? ctx)
{
    base.OnActivated (ctx);
    Action?.Invoke ();
}

protected override void OnAccepted (CommandEventArgs args) => Action?.Invoke ();
```
