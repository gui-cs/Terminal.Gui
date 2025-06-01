# Deep Dive into Command and View.Command in Terminal.Gui

## See Also

* [Lexicon & Taxonomy](lexicon.md)
* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Events](events.md)

## Overview

The `Command` system in Terminal.Gui provides a standardized framework for defining and executing actions that views can perform, such as selecting items, accepting input, or navigating content. Implemented primarily through the `View.Command` APIs, this system integrates tightly with input handling (e.g., keyboard and mouse events) and leverages the *Cancellable Work Pattern* to ensure extensibility, cancellation, and decoupling. Central to this system are the `Selecting` and `Accepting` events, which encapsulate common user interactions: `Selecting` for changing a view’s state or preparing it for interaction (e.g., toggling a checkbox, focusing a menu item), and `Accepting` for confirming an action or state (e.g., executing a menu command, submitting a dialog).

This deep dive explores the `Command` and `View.Command` APIs, focusing on the `Selecting` and `Accepting` concepts, their implementation, and their propagation behavior. It critically evaluates the need for additional events (`Selected`/`Accepted`) and the propagation of `Selecting` events, drawing on insights from `Menuv2`, `MenuItemv2`, `MenuBarv2`, `CheckBox`, and `FlagSelector`. These implementations highlight the system’s application in hierarchical (menus) and stateful (checkboxes, flag selectors) contexts. The document reflects the current implementation, including the `Cancel` property in `CommandEventArgs` and local handling of `Command.Select`. An appendix briefly summarizes proposed changes from a filed issue to rename `Command.Select` to `Command.Activate`, replace `Cancel` with `Handled`, and introduce a propagation mechanism, addressing limitations in the current system.

## Overview of the Command System

The `Command` system in Terminal.Gui defines a set of standard actions via the `Command` enum (e.g., `Command.Select`, `Command.Accept`, `Command.HotKey`, `Command.StartOfPage`). These actions are triggered by user inputs (e.g., key presses, mouse clicks) or programmatically, enabling consistent view interactions.

### Key Components
- **Command Enum**: Defines actions like `Select` (state change or interaction preparation), `Accept` (action confirmation), `HotKey` (hotkey activation), and others (e.g., `StartOfPage` for navigation).
- **Command Handlers**: Views register handlers using `View.AddCommand`, specifying a `CommandImplementation` delegate that returns `bool?` (`null`: no command executed; `false`: executed but not handled; `true`: handled or canceled).
- **Command Routing**: Commands are invoked via `View.InvokeCommand`, executing the handler or raising `CommandNotBound` if no handler exists.
- **Cancellable Work Pattern**: Command execution uses events (e.g., `Selecting`, `Accepting`) and virtual methods (e.g., `OnSelecting`, `OnAccepting`) for modification or cancellation, with `Cancel` indicating processing should stop.

### Role in Terminal.Gui
The `Command` system bridges user input and view behavior, enabling:
- **Consistency**: Standard commands ensure predictable interactions (e.g., `Enter` triggers `Accept` in buttons, menus, checkboxes).
- **Extensibility**: Custom handlers and events allow behavior customization.
- **Decoupling**: Events reduce reliance on sub-classing, though current propagation mechanisms may require subview-superview coordination.

### Note on `Cancel` Property
The `CommandEventArgs` class uses a `Cancel` property to indicate that a command event (e.g., `Accepting`) should stop processing. This is misleading, as it implies action negation rather than completion. A filed issue proposes replacing `Cancel` with `Handled` to align with input events (e.g., `Key.Handled`). This document uses `Cancel` to reflect the current implementation, with the appendix summarizing the proposed change.

## Implementation in View.Command

The `View.Command` APIs in the `View` class provide infrastructure for registering, invoking, and routing commands, adhering to the *Cancellable Work Pattern*.

### Command Registration
Views register commands using `View.AddCommand`, associating a `Command` with a `CommandImplementation` delegate. The delegate’s `bool?` return controls processing flow.

**Example**: Default commands in `View.SetupCommands`:
```csharp
private void SetupCommands()
{
    AddCommand(Command.Accept, RaiseAccepting);
    AddCommand(Command.Select, ctx =>
    {
        if (RaiseSelecting(ctx) is true)
        {
            return true;
        }
        if (CanFocus)
        {
            SetFocus();
            return true;
        }
        return false;
    });
    AddCommand(Command.HotKey, () =>
    {
        if (RaiseHandlingHotKey() is true)
        {
            return true;
        }
        SetFocus();
        return true;
    });
    AddCommand(Command.NotBound, RaiseCommandNotBound);
}
```

- **Default Commands**: `Accept`, `Select`, `HotKey`, `NotBound`.
- **Customization**: Views override or add commands (e.g., `CheckBox` for state toggling, `MenuItemv2` for menu actions).

### Command Invocation
Commands are invoked via `View.InvokeCommand` or `View.InvokeCommands`, passing an `ICommandContext` for context (e.g., source view, binding details). Unhandled commands trigger `CommandNotBound`.

**Example**:
```csharp
public bool? InvokeCommand(Command command, ICommandContext? ctx)
{
    if (!_commandImplementations.TryGetValue(command, out CommandImplementation? implementation))
    {
        _commandImplementations.TryGetValue(Command.NotBound, out implementation);
    }
    return implementation!(ctx);
}
```

### Command Routing
Most commands route directly to the target view. `Command.Select` and `Command.Accept` have special routing:
- `Command.Select`: Handled locally, with no propagation to superviews, relying on view-specific events (e.g., `SelectedMenuItemChanged` in `Menuv2`) for hierarchical coordination.
- `Command.Accept`: Propagates to a default button (if `IsDefault = true`), superview, or `SuperMenuItem` (in menus).

**Example**: `Command.Accept` in `RaiseAccepting`:
```csharp
protected bool? RaiseAccepting(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };
    args.Cancel = OnAccepting(args) || args.Cancel;
    if (!args.Cancel && Accepting is {})
    {
        Accepting?.Invoke(this, args);
    }
    if (!args.Cancel)
    {
        var isDefaultView = SuperView?.InternalSubViews.FirstOrDefault(v => v is Button { IsDefault: true });
        if (isDefaultView != this && isDefaultView is Button { IsDefault: true } button)
        {
            bool? handled = isDefaultView.InvokeCommand(Command.Accept, ctx);
            if (handled == true)
            {
                return true;
            }
        }
        if (SuperView is {})
        {
            return SuperView?.InvokeCommand(Command.Accept, ctx);
        }
    }
    return args.Cancel;
}
```

## The Selecting and Accepting Concepts

The `Selecting` and `Accepting` events, along with their corresponding commands (`Command.Select`, `Command.Accept`), are designed to handle the most common user interactions with views:
- **Selecting**: Changing a view’s state or preparing it for further interaction, such as highlighting an item in a list, toggling a checkbox, or focusing a menu item.
- **Accepting**: Confirming an action or state, such as submitting a form, activating a button, or finalizing a selection.

These concepts are opinionated, reflecting Terminal.Gui’s view that most UI interactions can be modeled as either state changes/preparation (selecting) or action confirmations (accepting). Below, we explore each concept, their implementation, use cases, and propagation behavior, using `Cancel` to reflect the current implementation.

### Selecting
- **Definition**: `Selecting` represents a user action that changes a view’s state or prepares it for further interaction, such as selecting an item in a `ListView`, toggling a `CheckBox`, or focusing a `MenuItemv2`. It is associated with `Command.Select`, typically triggered by a spacebar press, single mouse click, navigation keys (e.g., arrow keys), or mouse enter (e.g., in menus).
- **Event**: The `Selecting` event is raised by `RaiseSelecting`, allowing external code to modify or cancel the state change.
- **Virtual Method**: `OnSelecting` enables subclasses to preprocess or cancel the action.
- **Implementation**:
  ```csharp
  protected bool? RaiseSelecting(ICommandContext? ctx)
  {
      CommandEventArgs args = new() { Context = ctx };
      if (OnSelecting(args) || args.Cancel)
      {
          return true;
      }
      Selecting?.Invoke(this, args);
      return Selecting is null ? null : args.Cancel;
  }
  ```
  - **Default Behavior**: Sets focus if `CanFocus` is true (via `SetupCommands`).
  - **Cancellation**: `args.Cancel` or `OnSelecting` returning `true` halts the command.
  - **Context**: `ICommandContext` provides invocation details.

- **Use Cases**:
  - **ListView**: Selecting an item (e.g., via arrow keys or mouse click) raises `Selecting` to update the highlighted item.
  - **CheckBox**: Toggling the checked state (e.g., via spacebar) raises `Selecting` to change the state, as seen in the `AdvanceAndSelect` method:
    ```csharp
    private bool? AdvanceAndSelect(ICommandContext? commandContext)
    {
        bool? cancelled = AdvanceCheckState();
        if (cancelled is true)
        {
            return true;
        }
        if (RaiseSelecting(commandContext) is true)
        {
            return true;
        }
        return commandContext?.Command == Command.HotKey ? cancelled : cancelled is false;
    }
    ```
  - **RadioGroup**: Selecting a radio button raises `Selecting` to update the selected option.
  - **Menuv2** and **MenuBarv2**: Selecting a `MenuItemv2` (e.g., via mouse enter or arrow keys) sets focus, tracked by `SelectedMenuItem` and raising `SelectedMenuItemChanged`:
    ```csharp
    protected override void OnFocusedChanged(View? previousFocused, View? focused)
    {
        base.OnFocusedChanged(previousFocused, focused);
        SelectedMenuItem = focused as MenuItemv2;
        RaiseSelectedMenuItemChanged(SelectedMenuItem);
    }
    ```
  - **FlagSelector**: Selecting a `CheckBox` subview toggles a flag, updating the `Value` property and raising `ValueChanged`, though it incorrectly triggers `Accepting`:
    ```csharp
    checkbox.Selecting += (sender, args) =>
    {
        if (RaiseSelecting(args.Context) is true)
        {
            args.Cancel = true;
            return;
        }
        if (RaiseAccepting(args.Context) is true)
        {
            args.Cancel = true;
        }
    };
    ```
  - **Views without State**: For views like `Button`, `Selecting` typically sets focus but does not change state, making it less relevant.

- **Propagation**: `Command.Select` is handled locally by the target view. If the command is unhandled (`null` or `false`), processing stops without propagating to the superview or other views. This is evident in `Menuv2`, where `SelectedMenuItemChanged` is used for hierarchical coordination, and in `CheckBox` and `FlagSelector`, where state changes are internal.

### Accepting
- **Definition**: `Accepting` represents a user action that confirms or finalizes a view’s state or triggers an action, such as submitting a dialog, activating a button, or confirming a selection in a list. It is associated with `Command.Accept`, typically triggered by the Enter key or double-click.
- **Event**: The `Accepting` event is raised by `RaiseAccepting`, allowing external code to modify or cancel the action.
- **Virtual Method**: `OnAccepting` enables subclasses to preprocess or cancel the action.
- **Implementation**: As shown above in `RaiseAccepting`.
  - **Default Behavior**: Raises `Accepting` and propagates to a default button (if present in the superview with `IsDefault = true`) or the superview if not canceled.
  - **Cancellation**: `args.Cancel` or `OnAccepting` returning `true` halts the command.
  - **Context**: `ICommandContext` provides invocation details.

- **Use Cases**:
  - **Button**: Pressing Enter raises `Accepting` to activate the button (e.g., submit a dialog).
  - **ListView**: Double-clicking or pressing Enter raises `Accepting` to confirm the selected item(s).
  - **TextField**: Pressing Enter raises `Accepting` to submit the input.
  - **Menuv2** and **MenuBarv2**: Pressing Enter on a `MenuItemv2` raises `Accepting` to execute a command or open a submenu, followed by the `Accepted` event to hide the menu or deactivate the menu bar:
    ```csharp
    protected void RaiseAccepted(ICommandContext? ctx)
    {
        CommandEventArgs args = new() { Context = ctx };
        OnAccepted(args);
        Accepted?.Invoke(this, args);
    }
    ```
  - **CheckBox**: Pressing Enter raises `Accepting` to confirm the current `CheckedState` without modifying it, as seen in its command setup:
    ```csharp
    AddCommand(Command.Accept, RaiseAccepting);
    ```
  - **FlagSelector**: Pressing Enter raises `Accepting` to confirm the current `Value`, though its subview `Selecting` handler incorrectly triggers `Accepting`, which should be reserved for parent-level confirmation.
  - **Dialog**: `Accepting` on a default button closes the dialog or triggers an action.

- **Propagation**: `Command.Accept` propagates to:
  - A default button (if present in the superview with `IsDefault = true`).
  - The superview, enabling hierarchical handling (e.g., a dialog processes `Accept` if no button handles it).
  - In `Menuv2`, propagation extends to the `SuperMenuItem` for submenus in popovers, as seen in `OnAccepting`:
    ```csharp
    protected override bool OnAccepting(CommandEventArgs args)
    {
        if (args.Context is CommandContext<KeyBinding> keyCommandContext && keyCommandContext.Binding.Key == Application.QuitKey)
        {
            return true;
        }
        if (SuperView is null && SuperMenuItem is {})
        {
            return SuperMenuItem?.InvokeCommand(Command.Accept, args.Context) is true;
        }
        return false;
    }
    ```
  - Similarly, `MenuBarv2` customizes propagation to show popovers:
    ```csharp
    protected override bool OnAccepting(CommandEventArgs args)
    {
        if (Visible && Enabled && args.Context?.Source is MenuBarItemv2 { PopoverMenuOpen: false } sourceMenuBarItem)
        {
            if (!CanFocus)
            {
                Active = true;
                ShowItem(sourceMenuBarItem);
                if (!sourceMenuBarItem.HasFocus)
                {
                    sourceMenuBarItem.SetFocus();
                }
            }
            else
            {
                ShowItem(sourceMenuBarItem);
            }
            return true;
        }
        return false;
    }
    ```

### Key Differences
| Aspect | Selecting | Accepting |
|--------|-----------|-----------|
| **Purpose** | Change view state or prepare for interaction (e.g., focus menu item, toggle checkbox, select list item) | Confirm action or state (e.g., execute menu command, submit, activate) |
| **Trigger** | Spacebar, single click, navigation keys, mouse enter | Enter, double-click |
| **Event** | `Selecting` | `Accepting` |
| **Virtual Method** | `OnSelecting` | `OnAccepting` |
| **Propagation** | Local to the view | Propagates to default button, superview, or SuperMenuItem (in menus) |
| **Use Cases** | `Menuv2`, `MenuBarv2`, `CheckBox`, `FlagSelector`, `ListView`, `Button` | `Menuv2`, `MenuBarv2`, `CheckBox`, `FlagSelector`, `Button`, `ListView`, `Dialog` |
| **State Dependency** | Often stateful, but includes focus for stateless views | May be stateless (triggers action) |

### Critical Evaluation: Selecting vs. Accepting
The distinction between `Selecting` and `Accepting` is clear in theory:
- `Selecting` is about state changes or preparatory actions, such as choosing an item in a `ListView` or toggling a `CheckBox`.
- `Accepting` is about finalizing an action, such as submitting a selection or activating a button.

However, practical challenges arise:
- **Overlapping Triggers**: In `ListView`, pressing Enter might both select an item (`Selecting`) and confirm it (`Accepting`), depending on the interaction model, potentially confusing developers. Similarly, in `Menuv2`, navigation (e.g., arrow keys) triggers `Selecting`, while Enter triggers `Accepting`, but the overlap in user intent can blur the lines.
- **Stateless Views**: For views like `Button` or `MenuItemv2`, `Selecting` is limited to setting focus, which dilutes its purpose as a state-changing action and may confuse developers expecting a more substantial state change.
- **Propagation Limitations**: The local handling of `Command.Select` restricts hierarchical coordination. For example, `MenuBarv2` relies on `SelectedMenuItemChanged` to manage `PopoverMenu` visibility, which is view-specific and not generalizable. This highlights a need for a propagation mechanism that maintains subview-superview decoupling.
- **FlagSelector Design Flaw**: In `FlagSelector`, the `CheckBox.Selecting` handler incorrectly triggers both `Selecting` and `Accepting`, conflating state changes (toggling flags) with action confirmation (submitting the flag set). This violates the intended separation and requires a design fix to ensure `Selecting` is limited to subview state changes and `Accepting` is reserved for parent-level confirmation.

**Recommendation**: Enhance documentation to clarify the `Selecting`/`Accepting` model:
- Define `Selecting` as state changes or interaction preparation (e.g., item selection, toggling, focusing) and `Accepting` as action confirmations (e.g., submission, activation).
- Explicitly note that `Command.Select` may set focus in stateless views (e.g., `Button`, `MenuItemv2`) but is primarily for state changes.
- Address `FlagSelector`’s conflation by refactoring its `Selecting` handler to separate state changes from confirmation.

## Evaluating Selected/Accepted Events

The need for `Selected` and `Accepted` events is under consideration, with `Accepted` showing utility in specific views (`Menuv2`, `MenuBarv2`) but not universally required across all views. These events would serve as post-events, notifying that a `Selecting` or `Accepting` action has completed, similar to other *Cancellable Work Pattern* post-events like `ClearedViewport` in `View.Draw` or `OrientationChanged` in `OrientationHelper`.

### Need for Selected/Accepted Events
- **Selected Event**:
  - **Purpose**: A `Selected` event would notify that a `Selecting` action has completed, indicating that a state change or preparatory action (e.g., a new item highlighted, a checkbox toggled) has taken effect.
  - **Use Cases**:
    - **Menuv2** and **MenuBarv2**: Notify when a new `MenuItemv2` is focused, currently handled by the `SelectedMenuItemChanged` event, which tracks focus changes:
      ```csharp
      protected override void OnFocusedChanged(View? previousFocused, View? focused)
      {
          base.OnFocusedChanged(previousFocused, focused);
          SelectedMenuItem = focused as MenuItemv2;
          RaiseSelectedMenuItemChanged(SelectedMenuItem);
      }
      ```
    - **CheckBox**: Notify when the `CheckedState` changes, handled by the `CheckedStateChanged` event, which is raised after a state toggle:
      ```csharp
      private bool? ChangeCheckedState(CheckState value)
      {
          if (_checkedState == value || (value is CheckState.None && !AllowCheckStateNone))
          {
              return null;
          }
          CancelEventArgs<CheckState> e = new(in _checkedState, ref value);
          if (OnCheckedStateChanging(e))
          {
              return true;
          }
          CheckedStateChanging?.Invoke(this, e);
          if (e.Cancel)
          {
              return e.Cancel;
          }
          _checkedState = value;
          UpdateTextFormatterText();
          SetNeedsLayout();
          EventArgs<CheckState> args = new(in _checkedState);
          OnCheckedStateChanged(args);
          CheckedStateChanged?.Invoke(this, args);
          return false;
      }
      ```
    - **FlagSelector**: Notify when the `Value` changes due to a flag toggle, handled by the `ValueChanged` event, which is raised after a `CheckBox` state change:
      ```csharp
      checkbox.CheckedStateChanged += (sender, args) =>
      {
          uint? newValue = Value;
          if (checkbox.CheckedState == CheckState.Checked)
          {
              if (flag == default!)
              {
                  newValue = 0;
              }
              else
              {
                  newValue = newValue | flag;
              }
          }
          else
          {
              newValue = newValue & ~flag;
          }
          Value = newValue;
      };
      ```
    - **ListView**: Notify when a new item is selected, typically handled by `SelectedItemChanged` or similar custom events.
    - **Button**: Less relevant, as `Selecting` typically only sets focus, and no state change occurs to warrant a `Selected` notification.
  - **Current Approach**: Views like `Menuv2`, `CheckBox`, and `FlagSelector` use custom events (`SelectedMenuItemChanged`, `CheckedStateChanged`, `ValueChanged`) to signal state changes, bypassing a generic `Selected` event. These view-specific events provide context (e.g., the selected `MenuItemv2`, the new `CheckedState`, or the updated `Value`) that a generic `Selected` event would struggle to convey without additional complexity.
  - **Pros**:
    - A standardized `Selected` event could unify state change notifications across views, reducing the need for custom events in some cases.
    - Aligns with the *Cancellable Work Pattern*’s post-event phase, providing a consistent way to react to completed `Selecting` actions.
    - Could simplify scenarios where external code needs to monitor state changes without subscribing to view-specific events.
  - **Cons**:
    - Overlaps with existing view-specific events, which are more contextually rich (e.g., `CheckedStateChanged` provides the new `CheckState`, whereas `Selected` would need additional data).
    - Less relevant for stateless views like `Button`, where `Selecting` only sets focus, leading to inconsistent usage across view types.
    - Adds complexity to the base `View` class, potentially bloating the API for a feature not universally needed.
    - Requires developers to handle generic `Selected` events with less specific information, which could lead to more complex event handling logic compared to targeted view-specific events.
  - **Context Insight**: The use of `SelectedMenuItemChanged` in `Menuv2` and `MenuBarv2`, `CheckedStateChanged` in `CheckBox`, and `ValueChanged` in `FlagSelector` suggests that view-specific events are preferred for their specificity and context. These events are tailored to the view’s state (e.g., `MenuItemv2` instance, `CheckState`, or `Value`), making them more intuitive for developers than a generic `Selected` event. The absence of a `Selected` event in the current implementation indicates that it hasn’t been necessary for most use cases, as view-specific events adequately cover state change notifications.
  - **Verdict**: A generic `Selected` event could provide a standardized way to notify state changes, but its benefits are outweighed by the effectiveness of view-specific events like `SelectedMenuItemChanged`, `CheckedStateChanged`, and `ValueChanged`. These events offer richer context and are sufficient for current use cases across `Menuv2`, `CheckBox`, `FlagSelector`, and other views. Adding `Selected` to the base `View` class is not justified at this time, as it would add complexity without significant advantages over existing mechanisms.

- **Accepted Event**:
  - **Purpose**: An `Accepted` event would notify that an `Accepting` action has completed (i.e., was not canceled via `args.Cancel`), indicating that the action has taken effect, aligning with the *Cancellable Work Pattern*’s post-event phase.
  - **Use Cases**:
    - **Menuv2** and **MenuBarv2**: The `Accepted` event is critical for signaling that a menu command has been executed or a submenu action has completed, triggering actions like hiding the menu or deactivating the menu bar. In `Menuv2`, it’s raised by `RaiseAccepted` and used hierarchically:
      ```csharp
      protected void RaiseAccepted(ICommandContext? ctx)
      {
          CommandEventArgs args = new() { Context = ctx };
          OnAccepted(args);
          Accepted?.Invoke(this, args);
      }
      ```
      In `MenuBarv2`, it deactivates the menu bar:
      ```csharp
      protected override void OnAccepted(CommandEventArgs args)
      {
          base.OnAccepted(args);
          if (SubViews.OfType<MenuBarItemv2>().Contains(args.Context?.Source))
          {
              return;
          }
          Active = false;
      }
      ```
    - **CheckBox**: Could notify that the current `CheckedState` was confirmed (e.g., in a dialog context), though this is not currently implemented, as `Accepting` suffices for confirmation without a post-event.
    - **FlagSelector**: Could notify that the current `Value` was confirmed, but this is not implemented, and the incorrect triggering of `Accepting` by subview `Selecting` complicates its use.
    - **Button**: Could notify that the button was activated, typically handled by a custom event like `Clicked`.
    - **ListView**: Could notify that a selection was confirmed (e.g., Enter pressed), often handled by custom events.
    - **Dialog**: Could notify that an action was completed (e.g., OK button clicked), useful for hierarchical scenarios.
  - **Current Approach**: `Menuv2` and `MenuItemv2` implement `Accepted` to signal action completion, with hierarchical handling via subscriptions (e.g., `MenuItemv2.Accepted` triggers `Menuv2.RaiseAccepted`, which triggers `MenuBarv2.OnAccepted`). Other views like `CheckBox` and `FlagSelector` rely on the completion of the `Accepting` event (i.e., not canceled) or custom events (e.g., `Button.Clicked`) to indicate action completion, without a generic `Accepted` event.
  - **Pros**:
    - Provides a standardized way to react to confirmed actions, particularly valuable in composite or hierarchical views like `Menuv2`, `MenuBarv2`, and `Dialog`, where superviews need to respond to action completion (e.g., closing a menu or dialog).
    - Aligns with the *Cancellable Work Pattern*’s post-event phase, offering a consistent mechanism for post-action notifications.
    - Simplifies hierarchical scenarios by providing a unified event for action completion, reducing reliance on view-specific events in some cases.
  - **Cons**:
    - May duplicate existing view-specific events (e.g., `Button.Clicked`, `Menuv2.Accepted`), leading to redundancy in views where custom events are already established.
    - Adds complexity to the base `View` class, especially for views like `CheckBox` or `FlagSelector` where `Accepting`’s completion is often sufficient without a post-event.
    - Requires clear documentation to distinguish `Accepted` from `Accepting` and to clarify when it should be used over view-specific events.
  - **Context Insight**: The implementation of `Accepted` in `Menuv2` and `MenuBarv2` demonstrates its utility in hierarchical contexts, where it facilitates actions like menu closure or menu bar deactivation. For example, `MenuItemv2` raises `Accepted` to trigger `Menuv2`’s `RaiseAccepted`, which propagates to `MenuBarv2`:
    ```csharp
    protected void RaiseAccepted(ICommandContext? ctx)
    {
        CommandEventArgs args = new() { Context = ctx };
        OnAccepted(args);
        Accepted?.Invoke(this, args);
    }
    ```
    In contrast, `CheckBox` and `FlagSelector` do not use `Accepted`, relying on `Accepting`’s completion or view-specific events like `CheckedStateChanged` or `ValueChanged`. This suggests that `Accepted` is particularly valuable in composite views with hierarchical interactions but not universally needed across all views. The absence of `Accepted` in `CheckBox` and `FlagSelector` indicates that `Accepting` is often sufficient for simple confirmation scenarios, but the hierarchical use in menus and potential dialog applications highlight its potential for broader adoption in specific contexts.
  - **Verdict**: The `Accepted` event is highly valuable in composite and hierarchical views like `Menuv2`, `MenuBarv2`, and potentially `Dialog`, where it supports coordinated action completion (e.g., closing menus or dialogs). However, adding it to the base `View` class is premature without broader validation across more view types, as many views (e.g., `CheckBox`, `FlagSelector`) function effectively without it, using `Accepting` or custom events. Implementing `Accepted` in specific views or base classes like `Bar` or `Toplevel` (e.g., for menus and dialogs) and reassessing its necessity for the base `View` class later is a prudent approach. This balances the demonstrated utility in hierarchical scenarios with the need to avoid unnecessary complexity in simpler views.

**Recommendation**: Avoid adding `Selected` or `Accepted` events to the base `View` class for now. Instead:
- Continue using view-specific events (e.g., `Menuv2.SelectedMenuItemChanged`, `CheckBox.CheckedStateChanged`, `FlagSelector.ValueChanged`, `ListView.SelectedItemChanged`, `Button.Clicked`) for their contextual specificity and clarity.
- Maintain and potentially formalize the use of `Accepted` in views like `Menuv2`, `MenuBarv2`, and `Dialog`, tracking its utility to determine if broader adoption in a base class like `Bar` or `Toplevel` is warranted.
- If `Selected` or `Accepted` events are added in the future, ensure they fire only when their respective events (`Selecting`, `Accepting`) are not canceled (i.e., `args.Cancel` is `false`), maintaining consistency with the *Cancellable Work Pattern*’s post-event phase.

## Propagation of Selecting

The current implementation of `Command.Select` is local, but `MenuBarv2` requires propagation to manage `PopoverMenu` visibility, highlighting a limitation in the system’s ability to support hierarchical coordination without view-specific mechanisms.

### Current Behavior
- **Selecting**: `Command.Select` is handled locally by the target view, with no propagation to the superview or other views. If the command is unhandled (returns `null` or `false`), processing stops without further routing.
  - **Rationale**: `Selecting` is typically view-specific, as state changes (e.g., highlighting a `ListView` item, toggling a `CheckBox`) or preparatory actions (e.g., focusing a `MenuItemv2`) are internal to the view. This is evident in `CheckBox`, where state toggling is self-contained:
    ```csharp
    private bool? AdvanceAndSelect(ICommandContext? commandContext)
    {
        bool? cancelled = AdvanceCheckState();
        if (cancelled is true)
        {
            return true;
        }
        if (RaiseSelecting(commandContext) is true)
        {
            return true;
        }
        return commandContext?.Command == Command.HotKey ? cancelled : cancelled is false;
    }
    ```
  - **Context Across Views**: 
    - In `Menuv2`, `Selecting` sets focus and raises `SelectedMenuItemChanged` to track changes, but this is a view-specific mechanism:
      ```csharp
      protected override void OnFocusedChanged(View? previousFocused, View? focused)
      {
          base.OnFocusedChanged(previousFocused, focused);
          SelectedMenuItem = focused as MenuItemv2;
          RaiseSelectedMenuItemChanged(SelectedMenuItem);
      }
      ```
    - In `MenuBarv2`, `SelectedMenuItemChanged` is used to manage `PopoverMenu` visibility, but this relies on custom event handling rather than a generic propagation model:
      ```csharp
      protected override void OnSelectedMenuItemChanged(MenuItemv2? selected)
      {
          if (IsOpen() && selected is MenuBarItemv2 { PopoverMenuOpen: false } selectedMenuBarItem)
          {
              ShowItem(selectedMenuBarItem);
          }
      }
      ```
    - In `CheckBox` and `FlagSelector`, `Selecting` is local, with state changes (e.g., `CheckedState`, `Value`) handled internally or via view-specific events (`CheckedStateChanged`, `ValueChanged`), requiring no superview involvement.
    - In `ListView`, `Selecting` updates the highlighted item locally, with no need for propagation in typical use cases.
    - In `Button`, `Selecting` sets focus, which is inherently local.

- **Accepting**: `Command.Accept` propagates to a default button (if present), the superview, or a `SuperMenuItem` (in menus), enabling hierarchical handling.
  - **Rationale**: `Accepting` often involves actions that affect the broader UI context (e.g., closing a dialog, executing a menu command), requiring coordination with parent views. This is evident in `Menuv2`’s propagation to `SuperMenuItem` and `MenuBarv2`’s handling of `Accepted`:
    ```csharp
    protected override void OnAccepting(CommandEventArgs args)
    {
        if (args.Context is CommandContext<KeyBinding> keyCommandContext && keyCommandContext.Binding.Key == Application.QuitKey)
        {
            return true;
        }
        if (SuperView is null && SuperMenuItem is {})
        {
            return SuperMenuItem?.InvokeCommand(Command.Accept, args.Context) is true;
        }
        return false;
    }
    ```

### Should Selecting Propagate?
The local handling of `Command.Select` is sufficient for many views, but `MenuBarv2`’s need to manage `PopoverMenu` visibility highlights a gap in the current design, where hierarchical coordination relies on view-specific events like `SelectedMenuItemChanged`.

- **Arguments For Propagation**:
  - **Hierarchical Coordination**: In `MenuBarv2`, propagation would allow the menu bar to react to `MenuItemv2` selections (e.g., focusing a menu item via arrow keys or mouse enter) to show or hide popovers, streamlining the interaction model. Without propagation, `MenuBarv2` depends on `SelectedMenuItemChanged`, which is specific to `Menuv2` and not reusable for other hierarchical components.
  - **Consistency with Accepting**: `Command.Accept`’s propagation model supports hierarchical actions (e.g., dialog submission, menu command execution), suggesting that `Command.Select` could benefit from a similar approach to enable broader UI coordination, particularly in complex views like menus or dialogs.
  - **Future-Proofing**: Propagation could support other hierarchical components, such as `TabView` (coordinating tab selection) or nested dialogs (tracking subview state changes), enhancing the `Command` system’s flexibility for future use cases.

- **Arguments Against Propagation**:
  - **Locality of State Changes**: `Selecting` is inherently view-specific in most cases, as state changes (e.g., `CheckBox` toggling, `ListView` item highlighting) or preparatory actions (e.g., `Button` focus) are internal to the view. Propagating `Selecting` events could flood superviews with irrelevant events, requiring complex filtering logic. For example, `CheckBox` and `FlagSelector` operate effectively without propagation:
    ```csharp
    checkbox.CheckedStateChanged += (sender, args) =>
    {
        uint? newValue = Value;
        if (checkbox.CheckedState == CheckState.Checked)
        {
            if (flag == default!)
            {
                newValue = 0;
            }
            else
            {
                newValue = newValue | flag;
            }
        }
        else
        {
            newValue = newValue & ~flag;
        }
        Value = newValue;
    };
    ```
  - **Performance and Complexity**: Propagation increases event handling overhead and complicates the API, as superviews must process or ignore `Selecting` events. This could lead to performance issues in deeply nested view hierarchies or views with frequent state changes.
  - **Existing Alternatives**: View-specific events like `SelectedMenuItemChanged`, `CheckedStateChanged`, and `ValueChanged` already provide mechanisms for superview coordination, negating the need for generic propagation in many cases. For instance, `MenuBarv2` uses `SelectedMenuItemChanged` to manage popovers, albeit in a view-specific way:
    ```csharp
    protected override void OnSelectedMenuItemChanged(MenuItemv2? selected)
    {
        if (IsOpen() && selected is MenuBarItemv2 { PopoverMenuOpen: false } selectedMenuBarItem)
        {
            ShowItem(selectedMenuBarItem);
        }
    }
    ```
    Similarly, `CheckBox` and `FlagSelector` use `CheckedStateChanged` and `ValueChanged` to notify superviews or external code of state changes, which is sufficient for most scenarios.
  - **Semantics of `Cancel`**: Propagation would occur only if `args.Cancel` is `false`, implying an unhandled selection, which is counterintuitive since `Selecting` typically completes its action (e.g., setting focus or toggling a state) within the view. This could confuse developers expecting propagation to occur for all `Selecting` events.

- **Context Insight**: The `MenuBarv2` implementation demonstrates a clear need for propagation to manage `PopoverMenu` visibility, as it must react to `MenuItemv2` selections (e.g., focus changes) across its submenu hierarchy. The reliance on `SelectedMenuItemChanged` works but is specific to `Menuv2`, limiting its applicability to other hierarchical components. In contrast, `CheckBox` and `FlagSelector` show that local handling is adequate for most stateful views, where state changes are self-contained or communicated via view-specific events. `ListView` similarly operates locally, with `SelectedItemChanged` or similar events handling external notifications. `Button`’s focus-based `Selecting` is inherently local, requiring no propagation. This dichotomy suggests that while propagation is critical for certain hierarchical scenarios (e.g., menus), it’s unnecessary for many views, and any propagation mechanism must avoid coupling subviews to superviews to maintain encapsulation.

- **Verdict**: The local handling of `Command.Select` is sufficient for most views, including `CheckBox`, `FlagSelector`, `ListView`, and `Button`, where state changes or preparatory actions are internal or communicated via view-specific events. However, `MenuBarv2`’s requirement for hierarchical coordination to manage `PopoverMenu` visibility highlights a gap in the current design, where view-specific events like `SelectedMenuItemChanged` are used as a workaround. A generic propagation model would enhance flexibility for hierarchical components, but it must ensure that subviews (e.g., `MenuItemv2`) remain decoupled from superviews (e.g., `MenuBarv2`) to avoid implementation-specific dependencies. The current lack of propagation is a limitation, particularly for menus, but adding it requires careful design to avoid overcomplicating the API or impacting performance for views that don’t need it.

**Recommendation**: Maintain the local handling of `Command.Select` for now, as it meets the needs of most views like `CheckBox`, `FlagSelector`, and `ListView`. For `MenuBarv2`, continue using `SelectedMenuItemChanged` as a temporary solution, but prioritize developing a generic propagation mechanism that supports hierarchical coordination without coupling subviews to superviews. This mechanism should allow superviews to opt-in to receiving `Selecting` events from subviews, ensuring encapsulation (see appendix for a proposed solution).

## Recommendations for Refining the Design

Based on the analysis of the current `Command` and `View.Command` system, as implemented in `Menuv2`, `MenuBarv2`, `CheckBox`, and `FlagSelector`, the following recommendations aim to refine the system’s clarity, consistency, and flexibility while addressing identified limitations:

1. **Clarify Selecting/Accepting in Documentation**:
   - Explicitly define `Selecting` as state changes or interaction preparation (e.g., toggling a `CheckBox`, focusing a `MenuItemv2`, selecting a `ListView` item) and `Accepting` as action confirmations (e.g., executing a menu command, submitting a dialog).
   - Emphasize that `Command.Select` may set focus in stateless views (e.g., `Button`, `MenuItemv2`) but is primarily intended for state changes, to reduce confusion for developers.
   - Provide examples for each view type (e.g., `Menuv2`, `CheckBox`, `FlagSelector`, `ListView`, `Button`) to illustrate their distinct roles. For instance:
     - `Menuv2`: “`Selecting` focuses a `MenuItemv2` via arrow keys, while `Accepting` executes the selected command.”
     - `CheckBox`: “`Selecting` toggles the `CheckedState`, while `Accepting` confirms the current state.”
     - `FlagSelector`: “`Selecting` toggles a subview flag, while `Accepting` confirms the entire flag set.”
   - Document the `Cancel` property’s role in `CommandEventArgs`, noting its current limitation (implying negation rather than completion) and the planned replacement with `Handled` to align with input events like `Key.Handled`.

2. **Address FlagSelector Design Flaw**:
   - Refactor `FlagSelector`’s `CheckBox.Selecting` handler to separate `Selecting` and `Accepting` actions, ensuring `Selecting` is limited to subview state changes (toggling flags) and `Accepting` is reserved for parent-level confirmation of the `Value`. This resolves the conflation issue where subview `Selecting` incorrectly triggers `Accepting`.
   - Proposed fix:
     ```csharp
     checkbox.Selecting += (sender, args) =>
     {
         if (RaiseSelecting(args.Context) is true)
         {
             args.Cancel = true;
         }
     };
     ```
   - This ensures `Selecting` only propagates state changes to the parent `FlagSelector` via `RaiseSelecting`, and `Accepting` is triggered separately (e.g., via Enter on the `FlagSelector` itself) to confirm the `Value`.

3. **Enhance ICommandContext with View-Specific State**:
   - Enrich `ICommandContext` with a `State` property to include view-specific data (e.g., the selected `MenuItemv2` in `Menuv2`, the new `CheckedState` in `CheckBox`, the updated `Value` in `FlagSelector`). This enables more informed event handlers without requiring view-specific subscriptions.
   - Proposed interface update:
     ```csharp
     public interface ICommandContext
     {
         Command Command { get; }
         View? Source { get; }
         object? Binding { get; }
         object? State { get; } // View-specific state (e.g., selected item, CheckState)
     }
     ```
   - Example: In `Menuv2`, include the `SelectedMenuItem` in `ICommandContext.State` for `Selecting` handlers:
     ```csharp
     protected bool? RaiseSelecting(ICommandContext? ctx)
     {
         ctx.State = SelectedMenuItem; // Provide selected MenuItemv2
         CommandEventArgs args = new() { Context = ctx };
         if (OnSelecting(args) || args.Cancel)
         {
             return true;
         }
         Selecting?.Invoke(this, args);
         return Selecting is null ? null : args.Cancel;
     }
     ```
   - This enhances the flexibility of event handlers, allowing external code to react to state changes without subscribing to view-specific events like `SelectedMenuItemChanged` or `CheckedStateChanged`.

4. **Monitor Use Cases for Propagation Needs**:
   - Track the usage of `Selecting` and `Accepting` in real-world applications, particularly in `Menuv2`, `MenuBarv2`, `CheckBox`, and `FlagSelector`, to identify scenarios where propagation of `Selecting` events could simplify hierarchical coordination.
   - Collect feedback on whether the reliance on view-specific events (e.g., `SelectedMenuItemChanged` in `Menuv2`) is sufficient or if a generic propagation model would reduce complexity for hierarchical components like `MenuBarv2`. This will inform the design of a propagation mechanism that maintains subview-superview decoupling (see appendix).
   - Example focus areas:
     - `MenuBarv2`: Assess whether `SelectedMenuItemChanged` adequately handles `PopoverMenu` visibility or if propagation would streamline the interaction model.
     - `Dialog`: Evaluate whether `Selecting` propagation could enhance subview coordination (e.g., tracking checkbox toggles within a dialog).
     - `TabView`: Consider potential needs for tab selection coordination if implemented in the future.

5. **Improve Propagation for Hierarchical Views**:
   - Recognize the limitation in `Command.Select`’s local handling for hierarchical components like `MenuBarv2`, where superviews need to react to subview selections (e.g., focusing a `MenuItemv2` to manage popovers). The current reliance on `SelectedMenuItemChanged` is effective but view-specific, limiting reusability.
   - Develop a propagation mechanism that allows superviews to opt-in to receiving `Selecting` events from subviews without requiring subviews to know superview details, ensuring encapsulation. This could involve a new event or property in `View` to enable propagation while maintaining decoupling (see appendix for a proposed solution).
   - Example: For `MenuBarv2`, a propagation mechanism could allow it to handle `Selecting` events from `MenuItemv2` subviews to show or hide popovers, replacing the need for `SelectedMenuItemChanged`:
     ```csharp
     // Current workaround in MenuBarv2
     protected override void OnSelectedMenuItemChanged(MenuItemv2? selected)
     {
         if (IsOpen() && selected is MenuBarItemv2 { PopoverMenuOpen: false } selectedMenuBarItem)
         {
             ShowItem(selectedMenuBarItem);
         }
     }
     ```

6. **Standardize Hierarchical Handling for Accepting**:
   - Refine the propagation model for `Command.Accept` to reduce reliance on view-specific logic, such as `Menuv2`’s use of `SuperMenuItem` for submenu propagation. The current approach, while functional, introduces coupling:
    ```csharp
    if (SuperView is null && SuperMenuItem is {})
    {
        return SuperMenuItem?.InvokeCommand(Command.Accept, args.Context) is true;
    }
    ```
   - Explore a more generic mechanism, such as allowing superviews to subscribe to `Accepting` events from subviews, to streamline propagation and improve encapsulation. This could be addressed in conjunction with `Selecting` propagation (see appendix).
   - Example: In `Menuv2`, a subscription-based model could replace `SuperMenuItem` logic:
     ```csharp
     // Hypothetical subscription in Menuv2
     SubViewAdded += (sender, args) =>
     {
         if (args.View is MenuItemv2 menuItem)
         {
             menuItem.Accepting += (s, e) => RaiseAccepting(e.Context);
         }
     };
     ```

## Conclusion

The `Command` and `View.Command` system in Terminal.Gui provides a robust framework for handling view actions, with `Selecting` and `Accepting` serving as opinionated mechanisms for state changes/preparation and action confirmations. The system is effectively implemented across `Menuv2`, `MenuBarv2`, `CheckBox`, and `FlagSelector`, supporting a range of stateful and stateless interactions. However, limitations in terminology (`Select`’s ambiguity), cancellation semantics (`Cancel`’s misleading implication), and propagation (local `Selecting` handling) highlight areas for improvement.

The `Selecting`/`Accepting` distinction is clear in principle but requires careful documentation to avoid confusion, particularly for stateless views where `Selecting` is focus-driven and for views like `FlagSelector` where implementation flaws conflate the two concepts. View-specific events like `SelectedMenuItemChanged`, `CheckedStateChanged`, and `ValueChanged` are sufficient for post-selection notifications, negating the need for a generic `Selected` event. The `Accepted` event is valuable in hierarchical views like `Menuv2` and `MenuBarv2` but not universally required, suggesting inclusion in `Bar` or `Toplevel` rather than `View`.

By clarifying terminology, fixing implementation flaws (e.g., `FlagSelector`), enhancing `ICommandContext`, and developing a decoupled propagation model, Terminal.Gui can enhance the `Command` system’s clarity and flexibility, particularly for hierarchical components like `MenuBarv2`. The appendix summarizes proposed changes to address these limitations, aligning with a filed issue to guide future improvements.

## Appendix: Summary of Proposed Changes to Command System

A filed issue proposes enhancements to the `Command` system to address limitations in terminology, cancellation semantics, and propagation, informed by `Menuv2`, `MenuBarv2`, `CheckBox`, and `FlagSelector`. These changes are not yet implemented but aim to improve clarity, consistency, and flexibility.

### Proposed Changes
1. **Rename `Command.Select` to `Command.Activate`**:
   - Replace `Command.Select`, `Selecting` event, `OnSelecting`, and `RaiseSelecting` with `Command.Activate`, `Activating`, `OnActivating`, and `RaiseActivating`.
   - Rationale: “Select” is ambiguous for stateless views (e.g., `Button` focus) and imprecise for non-list state changes (e.g., `CheckBox` toggling). “Activate” better captures state changes and preparation.
   - Impact: Breaking change requiring codebase updates and migration guidance.

2. **Replace `Cancel` with `Handled` in `CommandEventArgs`**:
   - Replace `Cancel` with `Handled` to indicate command completion, aligning with `Key.Handled` (issue #3913).
   - Rationale: `Cancel` implies negation, not completion.
   - Impact: Clarifies semantics, requires updating event handlers.

3. **Introduce `PropagateActivating` Event**:
   - Add `event EventHandler<CancelEventArgs>? PropagateActivating` to `View`, allowing superviews (e.g., `MenuBarv2`) to subscribe to subview propagation requests.
   - Rationale: Enables hierarchical coordination (e.g., `MenuBarv2` managing `PopoverMenu` visibility) without coupling subviews to superviews, addressing the current reliance on view-specific events like `SelectedMenuItemChanged`.
   - Impact: Enhances flexibility for hierarchical views, requires subscription management in superviews like `MenuBarv2`.

### Benefits
- **Clarity**: `Activate` improves terminology for all views.
- **Consistency**: `Handled` aligns with input events.
- **Decoupling**: `PropagateActivating` supports hierarchical needs without subview-superview dependencies.
- **Extensibility**: Applicable to other hierarchies (e.g., dialogs, `TabView`).

### Implementation Notes
- Update `Command` enum, `View`, and derived classes for the rename.
- Modify `CommandEventArgs` for `Handled`.
- Implement `PropagateActivating` and test in `MenuBarv2`.
- Revise documentation to reflect changes.

For details, refer to the filed issue in the Terminal.Gui repository.