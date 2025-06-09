# Deep Dive into Command and View.Command in Terminal.Gui

## See Also

* [Lexicon & Taxonomy](lexicon.md)
* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Events](events.md)

## Overview

The `Command` system in Terminal.Gui provides a standardized framework for defining and executing actions that views can perform, such as selecting items, accepting input, or navigating content. Implemented primarily through the `View.Command` APIs, this system integrates tightly with input handling (e.g., keyboard and mouse events) and leverages the *Cancellable Work Pattern* to ensure extensibility, cancellation, and decoupling.

Central to this system are the `Activating` and `Accepting` events, which encapsulate common user interactions: `Activating` for changing a view's state or preparing it for interaction (e.g., toggling a checkbox, focusing a menu item), and `Accepting` for confirming an action or state (e.g., executing a menu command, submitting a dialog).

## Overview of the Command System

The `Command` system in Terminal.Gui defines a set of standard actions via the `Command` enum (e.g., `Command.Activate`, `Command.Accept`, `Command.HotKey`, `Command.StartOfPage`). These actions are triggered by user inputs (e.g., by default, `Key.Space` and `MouseFlags.Button1Clicked` are bound to `Command.Activate`, while `Key.Enter` is bound to `Command.Accept`).

### Key Components

- **Command Enum**: Defines actions like `Activate` (state change or interaction preparation), `Accept` (action confirmation), `HotKey` (hotkey activation), and others (e.g., `StartOfPage` for navigation).
- **Command Handlers**: Views register handlers using `View.AddCommand`, specifying a `CommandImplementation` delegate that returns `bool?` (`null`: no command executed; `false`: executed but not handled; `true`: handled or canceled).
- **Command Routing**: Commands are invoked via `View.InvokeCommand`, executing the handler or raising `CommandNotBound` if no handler exists.
- **Cancellable Work Pattern**: Command execution uses events (e.g., `Activating`, `Accepting`) and virtual methods (e.g., `OnActivating`, `OnAccepting`) for modification or cancellation, with `Cancel` indicating processing should stop.

### Role in Terminal.Gui

The `Command` system bridges user input and view behavior, enabling:
- **Consistency**: Standard commands ensure predictable interactions (e.g., `Enter` triggers `Accept` in buttons and menus).
- **Extensibility**: Custom handlers and events allow behavior customization.
- **Decoupling**: Events reduce reliance on sub-classing, though current propagation mechanisms may require subview-superview coordination.

## Default Command Implementations

How a View responds to a Command is up to that View. However, several have default implementations in the base `View` class to enable consistent behavior for the most common user actions. 

`View` binds the following keyboard and mouse actions:

```cs
        KeyBindings.Add (Key.Space, Command.Activate);
        KeyBindings.Add (Key.Enter, Command.Accept);

        MouseBindings.Add (MouseFlags.Button1Clicked, Command.Activate);
        MouseBindings.Add (MouseFlags.Button2Clicked, Command.Activate);
        MouseBindings.Add (MouseFlags.Button3Clicked, Command.Activate);
        MouseBindings.Add (MouseFlags.Button4Clicked, Command.Activate);
        MouseBindings.Add (MouseFlags.Button1Clicked | MouseFlags.ButtonCtrl, Command.Activate);
```

- @Terminal.Gui.Input.Command.NotBound - Default implementation raises @Terminal.Gui.ViewBase.CommandNotBound.
- @Terminal.Gui.Input.Command.Accept - Provides built-in support for a most common user action on Views: Accepting state. The efault implementation raises @Terminal.Gui.ViewBase.Accepting. If an override does not handle the command, the peer-Views are checked to see if any is a `Button` with `IsDefault` set. If so, `Accept` is invoked on that view. This enables "Default Button" support in forms. If there is no default button, or the default button does not handle the event, `Accept` is propagated up the SuperView hierarchy until handled.
- @Terminal.Gui.Input.Command.Activate - Provides built-in support for a common user action in Views: making the view, or something within the view active. The default implementation raises @Terminal.Gui.ViewBase.Activating and, if it wasn't handled and the view can be focused, sets focus to the View.
- @Terminal.Gui.Input.Command.HotKey - Provides support for accepting or activating a View with the keyboard. The default implementation raises @Terminal.Gui.ViewBase.HotKey and, if it wasn't handled, sets focus to the View.

Each of these default implementations uses CWP-style events to enable subclasses or external code to override or change the default behavior. For example, @Terminal.Gui.Views.Shortcut overrides the default `Accept` behavior of the SubViews it contains so that whenever the user causes an acceptance action on one of it's SubViews, `e.Handled` is set to `true` so that the `Accept` is ignored.

Views can also override the default behavior by simply registering a new command handler while ensuring the default implementation is still given first chance by calling `RaiseXXX` where `XXX` is the name of the command. @Terminal.Gui.Views.Label overrides the default for `HotKey` in this manner:

```cs
// On HoKey, pass it to the next peer view
AddCommand (Command.HotKey, InvokeHotKeyOnNextPeer);

// ...
private bool? InvokeHotKeyOnNextPeer (ICommandContext commandContext)
{
    if (RaiseHandlingHotKey () == true)
    {
        return true;
    }

    if (CanFocus)
    {
        SetFocus ();

        // Always return true on hotkey, even if SetFocus fails because
        // hotkeys are always handled by the View (unless RaiseHandlingHotKey cancels).
        // This is the same behavior as the base (View).
        return true;
    }

    if (HotKey.IsValid)
    {
        // If the Label has a hotkey, we need to find the next peer-view and pass the
        // command on to it.
        int me = SuperView?.SubViews.IndexOf (this) ?? -1;

        if (me != -1 && me < SuperView?.SubViews.Count - 1)
        {
            return SuperView?.SubViews.ElementAt (me + 1).InvokeCommand (Command.HotKey) == true;
        }
    }

    return false;
}

```

An illustrative case-study is to compare @Terminal.Gui.Views.Button and @Terminal.Gui.Views.Checkbox and how they handle the HotKey command differently.

When a user presses the HotKey for a Button, they expect the button to both gain focus and accept. However, for a Checkbox, the user does not want the Checkbox to gain focus, but does want the state of the Checkbox to advance. A Checkbox is only accepted if the user double-clicks on it or presses `Enter` while it has focus.

To enable this, @Terminal.Gui.Views.Checkbox replaces the built-in HotKey behavior to that if a user presses the Checkbox's hotkey, the @Terminal.Gui.Views.Checkbox.CheckState advances. 

```cs
// Activate (Space key and single-click) - Advance state and raise Accepting event
// - DO NOT raise Accept
// - DO NOT SetFocus
AddCommand (Command.Activate, AdvanceAndActivate);

// Accept (Enter key) - Raise Accept event
// - DO NOT advance state
// The default Accept handler does that.

// Enable double-clicking to Accept
MouseBindings.Add (MouseFlags.Button1DoubleClicked, Command.Accept);

// ...

protected override bool OnHandlingHotKey (CommandEventArgs args)
{
    // Invoke Activate on ourselves
    if (InvokeCommand (Command.Activate, args.Context) is true)
    {
        return true;
    }
    return base.OnHandlingHotKey (args);
}
```

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
Most commands route directly to the target view. `Command.Activate` and `Command.Accept` have special routing:
- `Command.Activate`: Handled locally, with no propagation to superviews, relying on view-specific events (e.g., `SelectedMenuItemChanged` in `Menuv2`) for hierarchical coordination.
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

## The Activating and Accepting Concepts

The `Activating` and `Accepting` events, along with their corresponding commands (`Command.Activate`, `Command.Accept`), are designed to handle the most common user interactions with views:
- **Activating**: Changing a view's state or preparing it for further interaction, such as highlighting an item in a list, toggling a checkbox, or focusing a menu item.
- **Accepting**: Confirming an action or state, such as submitting a form, activating a button, or finalizing a selection.

These concepts are opinionated, reflecting Terminal.Gui's view that most UI interactions can be modeled as either state changes/preparation (selecting) or action confirmations (accepting). Below, we explore each concept, their implementation, use cases, and propagation behavior, using `Cancel` to reflect the current implementation. Additionally, we document inconsistencies in their application across various `View` sub-classes as observed in the codebase.

### Activating
- **Definition**: `Activating` represents a user action that changes a view's state or prepares it for further interaction, such as selecting an item in a `ListView`, toggling a `CheckBox`, or focusing a `MenuItemv2`. It is associated with `Command.Activate`, typically triggered by a spacebar press, single mouse click, navigation keys (e.g., arrow keys), or mouse enter (e.g., in menus).
- **Event**: The `Activating` event is raised by `RaiseActivating`, allowing external code to modify or cancel the state change.
- **Virtual Method**: `OnActivating` enables subclasses to preprocess or cancel the action.
- **Implementation**:
  ```csharp
  protected bool? RaiseActivating(ICommandContext? ctx)
  {
      CommandEventArgs args = new() { Context = ctx };
      if (OnActivating(args) || args.Cancel)
      {
          return true;
      }
      Activating?.Invoke(this, args);
      return Activating is null ? null : args.Cancel;
  }
  ```
  - **Default Behavior**: Sets focus if `CanFocus` is true (via `SetupCommands`).
  - **Cancellation**: `args.Cancel` or `OnActivating` returning `true` halts the command.
  - **Context**: `ICommandContext` provides invocation details.

- **Use Cases**:
  - **ListView**: Activating an item (e.g., via arrow keys or mouse click) raises `Activating` to update the highlighted item.
  - **CheckBox**: Toggling the checked state (e.g., via spacebar) raises `Activating` to change the state, as seen in the `AdvanceAndSelect` method.
  - **RadioGroup**: Activating a radio button raises `Activating` to update the selected option.
  - **Menuv2** and **MenuBarv2**: Activating a `MenuItemv2` (e.g., via mouse enter or arrow keys) sets focus, tracked by `SelectedMenuItem` and raising `SelectedMenuItemChanged`.
  - **FlagSelector**: Activating a `CheckBox` subview toggles a flag, updating the `Value` property and raising `ValueChanged`, though it incorrectly triggers `Accepting`.
  - **Views without State**: For views like `Button`, `Activating` typically sets focus but does not change state, making it less relevant.

- **Propagation**: `Command.Activate` is handled locally by the target view. If the command is unhandled (`null` or `false`), processing stops without propagating to the superview or other views. This is evident in `Menuv2`, where `SelectedMenuItemChanged` is used for hierarchical coordination, and in `CheckBox` and `FlagSelector`, where state changes are internal.

### Accepting
- **Definition**: `Accepting` represents a user action that confirms or finalizes a view's state or triggers an action, such as submitting a dialog, activating a button, or confirming a selection in a list. It is associated with `Command.Accept`, typically triggered by the Enter key or double-click.
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
  - **Menuv2** and **MenuBarv2**: Pressing Enter on a `MenuItemv2` raises `Accepting` to execute a command or open a submenu, followed by the `Accepted` event to hide the menu or deactivate the menu bar.
  - **CheckBox**: Pressing Enter raises `Accepting` to confirm the current `CheckedState` without modifying it.
  - **FlagSelector**: Pressing Enter raises `Accepting` to confirm the current `Value`, though its subview `Activating` handler incorrectly triggers `Accepting`, which should be reserved for parent-level confirmation.
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
| Aspect | Activating | Accepting |
|--------|-----------|-----------|
| **Purpose** | Change view state or prepare for interaction (e.g., focus menu item, toggle checkbox, select list item) | Confirm action or state (e.g., execute menu command, submit, activate) |
| **Trigger** | Spacebar, single click, navigation keys, mouse enter | Enter, double-click |
| **Event** | `Activating` | `Accepting` |
| **Virtual Method** | `OnActivating` | `OnAccepting` |
| **Propagation** | Local to the view | Propagates to default button, superview, or SuperMenuItem (in menus) |
| **Use Cases** | `Menuv2`, `MenuBarv2`, `CheckBox`, `FlagSelector`, `ListView`, `Button` | `Menuv2`, `MenuBarv2`, `CheckBox`, `FlagSelector`, `Button`, `ListView`, `Dialog` |
| **State Dependency** | Often stateful, but includes focus for stateless views | May be stateless (triggers action) |

## Inconsistencies in Command Usage Across View Sub-classes

Analysis of the Terminal.Gui codebase reveals several inconsistencies in the application of `Command.Activate` and `Command.Accept` across various `View` sub-classes. These discrepancies deviate from the intended sequential interaction model (initiation via `Activate` followed by confirmation via `Accept`) and can lead to varied user experiences and developer confusion. Below, we document these inconsistencies as tasks to be addressed to ensure a uniform command handling approach.

### 1. TableView (TableView.cs)
- **Inconsistency in Command Purpose**: `Command.Accept` triggers `OnCellActivated`, suggesting a final action, but is bound to `CellActivationKey` and other custom keys, implying activation rather than acceptance. `Command.Activate` is associated with toggling selection, but `Key.Space` is bound to `Command.Activate` for toggling in related classes, blurring the distinction between activation and state change.
- **Key Binding Discrepancy**: Unlike default `View` bindings (`Space` for `Activate`, `Enter` for `Accept`), custom keys are used for `Accept`, disrupting the expected user interaction model.
- **Task**: **Task 1 - Standardize Command Purpose and Bindings in TableView**: Review and revise `TableView` to align `Command.Accept` with confirmation actions (e.g., finalizing cell selection) and `Command.Activate` with initial interaction (e.g., toggling or focusing). Ensure key bindings match default `View` expectations (`Space` for `Activate`, `Enter` for `Accept`) unless a specific use case justifies deviation.

### 2. TreeView (TreeView.cs)
- **Inconsistency in Command Handling**: Both `Command.Accept` and `Command.Activate` are bound to the same method `ActivateSelectedObjectIfAny`, eliminating the distinction between initiating and finalizing an action, contrary to the `View` model.
- **Key Binding Discrepancy**: Only `ObjectActivationKey` is bound to `Command.Activate`, with no explicit binding for `Command.Accept`, suggesting `Accept` might not be directly accessible via a standard key.
- **Task**: **Task 2 - Differentiate Command Handling in TreeView**: Modify `TreeView` to separate `Command.Activate` for initial selection or focus and `Command.Accept` for confirmation of the selected node. Add standard key binding for `Command.Accept` (e.g., `Enter`) to ensure accessibility.

### 3. MenuItemv2 and MenuBarv2 (Menuv2.cs, Menuv1/MenuItem.cs, Menuv1/MenuBar.cs)
- **Inconsistency in Command Purpose**: In `Menuv2.cs`, `Command.Accept` propagates to `SuperMenuItem`, aligning with hierarchical acceptance. In `MenuItem.cs` (v1), `Command.Activate` is used for selection with no clear `Accept` handling. In `MenuBar.cs` (v1), `Command.Accept` is bound to `Key.CursorDown`, which is atypical for a confirmation action.
- **Key Binding Discrepancy**: Binding `Accept` to navigation keys deviates from the standard confirmation role of `Accept`. `Activate` often triggers selection without a separate `Accept` step.
- **Task**: **Task 3 - Harmonize Command Usage in Menu Classes**: Ensure `Command.Activate` is used for focusing or selecting menu items and `Command.Accept` for executing the selected action across all menu-related classes. Standardize key bindings to use `Enter` for `Accept` and navigation keys or `Space` for `Activate`.

### 4. PopoverMenu (PopoverMenu.cs)
- **Inconsistency in Command Handling**: Focuses on `Command.Accept` for menu closure without a clear `Command.Activate` phase. No explicit binding or use of `Command.Activate` is present, missing the initial interaction step.
- **Key Binding Discrepancy**: Lacks bindings for `Command.Activate`, unlike the default `Space` binding in `View`.
- **Task**: **Task 4 - Implement Dual-Command Model in PopoverMenu**: Introduce `Command.Activate` for initial menu item selection or focus, and ensure `Command.Accept` is used for final action execution or menu closure. Add appropriate key bindings (`Space` for `Activate`, `Enter` for `Accept`).

### 5. Shortcut (Shortcut.cs)
- **Inconsistency in Command Handling**: Both `Command.Accept` and `Command.Activate` are bound to `DispatchCommand`, erasing the distinction between initiating and finalizing an action.
- **Invocation Discrepancy**: Multiple direct invocations of `Command.Activate` exist, but `Accept` is bound to the same action, indicating redundancy.
- **Task**: **Task 5 - Separate Command Roles in Shortcut**: Redefine `Command.Activate` to handle initial interaction (e.g., focus or preparation) and `Command.Accept` to execute the shortcut action, ensuring distinct roles and reducing redundancy in command invocation.

### 6. Bar (Bar.cs)
- **Inconsistency in Command Handling**: No explicit binding or handling of `Command.Accept` or `Command.Activate`. Relies on contained `Shortcut` objects, which themselves handle commands inconsistently.
- **Key Binding Discrepancy**: No direct key bindings for commands, delegating to sub-views.
- **Task**: **Task 6 - Add Direct Command Handling in Bar**: Implement direct handling of `Command.Activate` and `Command.Accept` in `Bar` for focus or selection of contained items and confirmation actions, respectively. Ensure standard key bindings are supported or delegate consistently to sub-views with clear documentation.

### 7. ListView (ListView.cs)
- **Inconsistency in Command Purpose**: `Command.Activate` is used for toggling or selecting, but combined with navigation (`Down`), which is atypical for `Activate`. `Command.Accept` finalizes selection, aligning with `View`.
- **Event Handling Discrepancy**: Dual-purpose binding of `Activate` with navigation suggests mixed responsibility.
- **Task**: **Task 7 - Clarify Command Purpose in ListView**: Restrict `Command.Activate` to selection or state change without navigation elements, and ensure `Command.Accept` remains focused on confirmation. Separate navigation bindings from activation to maintain clear command roles.

### Summary of Inconsistencies
- **Purpose Confusion**: Several sub-classes (`TreeView`, `Shortcut`) bind both commands to the same method, losing the initiation-confirmation distinction. `TableView` uses `Accept` for activation-like behavior. `PopoverMenu` focuses on `Accept` without `Activate`. `Bar` omits direct handling.
- **Key Binding Variations**: Default bindings are often overridden or ignored, leading to inconsistent user interaction.
- **Handling and Propagation**: Some maintain `Accept` propagation, while others handle commands identically without distinction or propagation.
- **Missing Implementations**: `Bar` and `PopoverMenu` lack complete command models, either delegating or focusing on one command.
- **Task**: **Task 8 - Develop Uniform Command Guidelines**: Create and enforce guidelines for `Command.Activate` and `Command.Accept` usage across all `View` sub-classes, ensuring distinct roles (initiation vs. confirmation), standard key bindings, and consistent propagation behavior. Update documentation and codebase accordingly.

## Critical Evaluation: Activating vs. Accepting

The distinction between `Activating` and `Accepting` is clear in theory:
- `Activating` is about state changes or preparatory actions, such as choosing an item in a `ListView` or toggling a `CheckBox`.
- `Accepting` is about finalizing an action, such as submitting a selection or activating a button.

However, practical challenges arise due to the inconsistencies listed above:
- **Overlapping Triggers**: In some views like `ListView`, actions might blur the lines between selection and confirmation.
- **Stateless Views**: For views like `Button`, `Activating` is limited to setting focus, diluting its purpose.
- **Propagation Limitations**: The local handling of `Command.Activate` restricts hierarchical coordination, as seen in `MenuBarv2`.
- **Design Flaws**: Incorrect usage, such as in `FlagSelector`, conflates state changes with action confirmation.

**Recommendation**: Address the documented tasks to standardize command usage, enhance documentation to clarify the `Activating`/`Accepting` model, and ensure each `View` sub-class adheres to the intended interaction flow.

## Evaluating Selected/Accepted Events

The need for `Selected` and `Accepted` events is under consideration, with `Accepted` showing utility in specific views (`Menuv2`, `MenuBarv2`) but not universally required across all views. These events would serve as post-events, notifying that a `Activating` or `Accepting` action has completed, similar to other *Cancellable Work Pattern* post-events like `ClearedViewport` in `View.Draw` or `OrientationChanged` in `OrientationHelper`.

### Need for Selected/Accepted Events
- **Selected Event**:
  - **Purpose**: A `Selected` event would notify that a `Activating` action has completed, indicating that a state change or preparatory action (e.g., a new item highlighted, a checkbox toggled) has taken effect.
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
    - **Button**: Less relevant, as `Activating` typically only sets focus, and no state change occurs to warrant a `Selected` notification.
  - **Current Approach**: Views like `Menuv2`, `CheckBox`, and `FlagSelector` use custom events (`SelectedMenuItemChanged`, `CheckedStateChanged`, `ValueChanged`) to signal state changes, bypassing a generic `Selected` event. These view-specific events provide context (e.g., the selected `MenuItemv2`, the new `CheckedState`, or the updated `Value`) that a generic `Selected` event would struggle to convey without additional complexity.
  - **Pros**:
    - A standardized `Selected` event could unify state change notifications across views, reducing the need for custom events in some cases.
    - Aligns with the *Cancellable Work Pattern*'s post-event phase, providing a consistent way to react to completed `Activating` actions.
    - Could simplify scenarios where external code needs to monitor state changes without subscribing to view-specific events.
  - **Cons**:
    - Overlaps with existing view-specific events, which are more contextually rich (e.g., `CheckedStateChanged` provides the new `CheckState`, whereas `Selected` would need additional data).
    - Less relevant for stateless views like `Button`, where `Activating` only sets focus, leading to inconsistent usage across view types.
    - Adds complexity to the base `View` class, potentially bloating the API for a feature not universally needed.
    - Requires developers to handle generic `Selected` events with less specific information, which could lead to more complex event handling logic compared to targeted view-specific events.
  - **Context Insight**: The use of `SelectedMenuItemChanged` in `Menuv2` and `MenuBarv2`, `CheckedStateChanged` in `CheckBox`, and `ValueChanged` in `FlagSelector` suggests that view-specific events are preferred for their specificity and context. These events are tailored to the view's state (e.g., `MenuItemv2` instance, `CheckState`, or `Value`), making them more intuitive for developers than a generic `Selected` event. The absence of a `Selected` event in the current implementation indicates that it hasn't been necessary for most use cases, as view-specific events adequately cover state change notifications.
  - **Verdict**: A generic `Selected` event could provide a standardized way to notify state changes, but its benefits are outweighed by the effectiveness of view-specific events like `SelectedMenuItemChanged`, `CheckedStateChanged`, and `ValueChanged`. These events offer richer context and are sufficient for current use cases across `Menuv2`, `CheckBox`, `FlagSelector`, and other views. Adding `Selected` to the base `View` class is not justified at this time, as it would add complexity without significant advantages over existing mechanisms.

- **Accepted Event**:
  - **Purpose**: An `Accepted` event would notify that an `Accepting` action has completed (i.e., was not canceled via `args.Cancel`), indicating that the action has taken effect, aligning with the *Cancellable Work Pattern*'s post-event phase.
  - **Use Cases**:
    - **Menuv2** and **MenuBarv2**: The `Accepted` event is critical for signaling that a menu command has been executed or a submenu action has completed, triggering actions like hiding the menu or deactivating the menu bar. In `Menuv2`, it's raised by `RaiseAccepted` and used hierarchically:
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
    - **FlagSelector**: Could notify that the current `Value` was confirmed, but this is not implemented, and the incorrect triggering of `Accepting` by subview `Activating` complicates its use.
    - **Button**: Could notify that the button was activated, typically handled by a custom event like `Clicked`.
    - **ListView**: Could notify that a selection was confirmed (e.g., Enter pressed), often handled by custom events.
    - **Dialog**: Could notify that an action was completed (e.g., OK button clicked), useful for hierarchical scenarios.
  - **Current Approach**: `Menuv2` and `MenuItemv2` implement `Accepted` to signal action completion, with hierarchical handling via subscriptions (e.g., `MenuItemv2.Accepted` triggers `Menuv2.RaiseAccepted`, which triggers `MenuBarv2.OnAccepted`). Other views like `CheckBox` and `FlagSelector` rely on the completion of the `Accepting` event (i.e., not canceled) or custom events (e.g., `Button.Clicked`) to indicate action completion, without a generic `Accepted` event.
  - **Pros**:
    - Provides a standardized way to react to confirmed actions, particularly valuable in composite or hierarchical views like `Menuv2`, `MenuBarv2`, and `Dialog`, where superviews need to respond to action completion (e.g., closing a menu or dialog).
    - Aligns with the *Cancellable Work Pattern*'s post-event phase, offering a consistent mechanism for post-action notifications.
    - Simplifies hierarchical scenarios by providing a unified event for action completion, reducing reliance on view-specific events in some cases.
  - **Cons**:
    - May duplicate existing view-specific events (e.g., `Button.Clicked`, `Menuv2.Accepted`), leading to redundancy in views where custom events are already established.
    - Adds complexity to the base `View` class, especially for views like `CheckBox` or `FlagSelector` where `Accepting`'s completion is often sufficient without a post-event.
    - Requires clear documentation to distinguish `Accepted` from `Accepting` and to clarify when it should be used over view-specific events.
  - **Context Insight**: The implementation of `Accepted` in `Menuv2` and `MenuBarv2` demonstrates its utility in hierarchical contexts, where it facilitates actions like menu closure or menu bar deactivation. For example, `MenuItemv2` raises `Accepted` to trigger `Menuv2`'s `RaiseAccepted`, which propagates to `MenuBarv2`:
      ```csharp
      protected void RaiseAccepted(ICommandContext? ctx)
      {
          CommandEventArgs args = new() { Context = ctx };
          OnAccepted(args);
          Accepted?.Invoke(this, args);
      }
      ```
    In contrast, `CheckBox` and `FlagSelector` do not use `Accepted`, relying on `Accepting`'s completion or view-specific events like `CheckedStateChanged` or `ValueChanged`. This suggests that `Accepted` is particularly valuable in composite views with hierarchical interactions but not universally needed across all views. The absence of `Accepted` in `CheckBox` and `FlagSelector` indicates that `Accepting` is often sufficient for simple confirmation scenarios, but the hierarchical use in menus and potential dialog applications highlight its potential for broader adoption in specific contexts.
  - **Verdict**: The `Accepted` event is highly valuable in composite and hierarchical views like `Menuv2`, `MenuBarv2`, and potentially `Dialog`, where it supports coordinated action completion (e.g., closing menus or dialogs). However, adding it to the base `View` class is premature without broader validation across more view types, as many views (e.g., `CheckBox`, `FlagSelector`) function effectively without it, using `Accepting` or custom events. Implementing `Accepted` in specific views or base classes like `Bar` or `Toplevel` (e.g., for menus and dialogs) and reassessing its necessity for the base `View` class later is a prudent approach. This balances the demonstrated utility in hierarchical scenarios with the need to avoid unnecessary complexity in simpler views.

**Recommendation**: Avoid adding `Selected` or `Accepted` events to the base `View` class for now. Instead:
- Continue using view-specific events (e.g., `Menuv2.SelectedMenuItemChanged`, `CheckBox.CheckedStateChanged`, `FlagSelector.ValueChanged`, `ListView.SelectedItemChanged`, `Button.Clicked`) for their contextual specificity and clarity.
- Maintain and potentially formalize the use of `Accepted` in views like `Menuv2`, `MenuBarv2`, and `Dialog`, tracking its utility to determine if broader adoption in a base class like `Bar` or `Toplevel` is warranted.
- If `Selected` or `Accepted` events are added in the future, ensure they fire only when their respective events (`Activating`, `Accepting`) are not canceled (i.e., `args.Cancel` is `false`), maintaining consistency with the *Cancellable Work Pattern*'s post-event phase.

## Propagation of Activating

The current implementation of `Command.Activate` is local, but `MenuBarv2` requires propagation to manage `PopoverMenu` visibility, highlighting a limitation in the system's ability to support hierarchical coordination without view-specific mechanisms.

### Current Behavior
- **Activating**: `Command.Activate` is handled locally by the target view, with no propagation to the superview or other views. If the command is unhandled (returns `null` or `false`), processing stops without further routing.
  - **Rationale**: `Activating` is typically view-specific, as state changes (e.g., highlighting a `ListView` item, toggling a `CheckBox`) or preparatory actions (e.g., focusing a `MenuItemv2`) are internal to the view. This is evident in `CheckBox`, where state toggling is self-contained:
    ```csharp
    private bool? AdvanceAndSelect(ICommandContext? commandContext)
    {
        bool? cancelled = AdvanceCheckState();
        if (cancelled is true)
        {
            return true;
        }
        if (RaiseActivating(commandContext) is true)
        {
            return true;
        }
        return commandContext?.Command == Command.HotKey ? cancelled : cancelled is false;
    }
    ```
  - **Context Across Views**: 
    - In `Menuv2`, `Activating` sets focus and raises `SelectedMenuItemChanged` to track changes, but this is a view-specific mechanism:
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
    - In `CheckBox` and `FlagSelector`, `Activating` is local, with state changes (e.g., `CheckedState`, `Value`) handled internally or via view-specific events (`CheckedStateChanged`, `ValueChanged`), requiring no superview involvement.
    - In `ListView`, `Activating` updates the highlighted item locally, with no need for propagation in typical use cases.
    - In `Button`, `Activating` sets focus, which is inherently local.

- **Accepting**: `Command.Accept` propagates to a default button (if present), the superview, or a `SuperMenuItem` (in menus), enabling hierarchical handling.
  - **Rationale**: `Accepting` often involves actions that affect the broader UI context (e.g., closing a dialog, executing a menu command), requiring coordination with parent views. This is evident in `Menuv2`'s propagation to `SuperMenuItem` and `MenuBarv2`'s handling of `Accepted`:
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

### Should Activating Propagate?
The local handling of `Command.Activate` is sufficient for many views, but `MenuBarv2`'s need to manage `PopoverMenu` visibility highlights a gap in the current design, where hierarchical coordination relies on view-specific events like `SelectedMenuItemChanged`.

- **Arguments For Propagation**:
  - **Hierarchical Coordination**: In `MenuBarv2`, propagation would allow the menu bar to react to `MenuItemv2` selections (e.g., focusing a menu item via arrow keys or mouse enter) to show or hide popovers, streamlining the interaction model. Without propagation, `MenuBarv2` depends on `SelectedMenuItemChanged`, which is specific to `Menuv2` and not reusable for other hierarchical components.
  - **Consistency with Accepting**: `Command.Accept`'s propagation model supports hierarchical actions (e.g., dialog submission, menu command execution), suggesting that `Command.Activate` could benefit from a similar approach to enable broader UI coordination, particularly in complex views like menus or dialogs.
  - **Future-Proofing**: Propagation could support other hierarchical components, such as `TabView` (coordinating tab selection) or nested dialogs (tracking subview state changes), enhancing the `Command` system's flexibility for future use cases.

- **Arguments Against Propagation**:
  - **Locality of State Changes**: `Activating` is inherently view-specific in most cases, as state changes (e.g., `CheckBox` toggling, `ListView` item highlighting) or preparatory actions (e.g., `Button` focus) are internal to the view. Propagating `Activating` events could flood superviews with irrelevant events, requiring complex filtering logic. For example, `CheckBox` and `FlagSelector` operate effectively without propagation:
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
  - **Performance and Complexity**: Propagation increases event handling overhead and complicates the API, as superviews must process or ignore `Activating` events. This could lead to performance issues in deeply nested view hierarchies or views with frequent state changes.
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
  - **Semantics of `Cancel`**: Propagation would occur only if `args.Cancel` is `false`, implying an unhandled selection, which is counterintuitive since `Activating` typically completes its action (e.g., setting focus or toggling a state) within the view. This could confuse developers expecting propagation to occur for all `Activating` events.

- **Context Insight**: The `MenuBarv2` implementation demonstrates a clear need for propagation to manage `PopoverMenu` visibility, as it must react to `MenuItemv2` selections (e.g., focus changes) across its submenu hierarchy. The reliance on `SelectedMenuItemChanged` works but is specific to `Menuv2`, limiting its applicability to other hierarchical components. In contrast, `CheckBox` and `FlagSelector` show that local handling is adequate for most stateful views, where state changes are self-contained or communicated via view-specific events. `ListView` similarly operates locally, with `SelectedItemChanged` or similar events handling external notifications. `Button`'s focus-based `Activating` is inherently local, requiring no propagation. This dichotomy suggests that while propagation is critical for certain hierarchical scenarios (e.g., menus), it's unnecessary for many views, and any propagation mechanism must avoid coupling subviews to superviews to maintain encapsulation.

- **Verdict**: The local handling of `Command.Activate` is sufficient for most views, including `CheckBox`, `FlagSelector`, `ListView`, and `Button`, where state changes or preparatory actions are internal or communicated via view-specific events. However, `MenuBarv2`'s requirement for hierarchical coordination to manage `PopoverMenu` visibility highlights a gap in the current design, where view-specific events like `SelectedMenuItemChanged` are used as a workaround. A generic propagation model would enhance flexibility for hierarchical components, but it must ensure that subviews (e.g., `MenuItemv2`) remain decoupled from superviews (e.g., `MenuBarv2`) to avoid implementation-specific dependencies. The current lack of propagation is a limitation, particularly for menus, but adding it requires careful design to avoid overcomplicating the API or impacting performance for views that don't need it.

**Recommendation**: Maintain the local handling of `Command.Activate` for now, as it meets the needs of most views like `CheckBox`, `FlagSelector`, and `ListView`. For `MenuBarv2`, continue using `SelectedMenuItemChanged` as a temporary solution, but prioritize developing a generic propagation mechanism that supports hierarchical coordination without coupling subviews to superviews. This mechanism should allow superviews to opt-in to receiving `Activating` events from subviews, ensuring encapsulation (see appendix for a proposed solution).

## Recommendations for Refining the Design

Based on the analysis of the current `Command` and `View.Command` system, as implemented across various `View` sub-classes, the following recommendations aim to refine the system's clarity, consistency, and flexibility while addressing identified limitations and inconsistencies:

1. **Clarify Activating/Accepting in Documentation**:
   - Explicitly define `Activating` as state changes or interaction preparation and `Accepting` as action confirmations.
   - Emphasize that `Command.Activate` may set focus in stateless views but is primarily for state changes.
   - Provide examples for each view type, illustrating their distinct roles and addressing observed inconsistencies.

2. **Address Specific Inconsistencies**:
   - Implement the tasks outlined in the 'Inconsistencies in Command Usage Across View Sub-classes' section to ensure uniform application of `Command.Activate` and `Command.Accept` across all sub-classes.
   - Focus on separating command purposes, standardizing key bindings, and ensuring proper propagation.

3. **Enhance ICommandContext with View-Specific State**:
   - Enrich `ICommandContext` with a `State` property to include view-specific data, enabling more informed event handlers.

4. **Monitor Use Cases for Propagation Needs**:
   - Track usage of `Activating` and `Accepting` in real-world applications to identify scenarios where propagation of `Activating` events could simplify hierarchical coordination, addressing limitations seen in `MenuBarv2`.

5. **Improve Propagation for Hierarchical Views**:
   - Develop a propagation mechanism for `Command.Activate` that allows superviews to opt-in to receiving events from subviews, ensuring encapsulation and addressing needs in hierarchical components.

6. **Standardize Hierarchical Handling for Accepting**:
   - Refine the propagation model for `Command.Accept` to reduce reliance on view-specific logic, streamlining propagation and improving encapsulation.

## Conclusion

The `Command` and `View.Command` system in Terminal.Gui provides a robust framework for handling view actions, with `Activating` and `Accepting` serving as mechanisms for state changes/preparation and action confirmations. However, significant inconsistencies in their application across `View` sub-classes, as documented, highlight areas for improvement in command purpose, key bindings, handling, and propagation. By addressing these tasks, clarifying terminology, fixing implementation flaws, and developing a decoupled propagation model, Terminal.Gui can enhance the `Command` system's clarity and flexibility. The appendix summarizes proposed changes to address broader limitations, aligning with a filed issue to guide future improvements.

## Appendix: Summary of Proposed Changes to Command System

A filed issue proposes enhancements to the `Command` system to address limitations in terminology, cancellation semantics, and propagation, informed by `Menuv2`, `MenuBarv2`, `CheckBox`, and `FlagSelector`. These changes are not yet implemented but aim to improve clarity, consistency, and flexibility.

### Proposed Changes

1. **Introduce `PropagateActivating` Event**:
   - Add `event EventHandler<CancelEventArgs>? PropagateActivating` to `View`, allowing superviews (e.g., `MenuBarv2`) to subscribe to subview propagation requests.
   - Rationale: Enables hierarchical coordination (e.g., `MenuBarv2` managing `PopoverMenu` visibility) without coupling subviews to superviews, addressing the current reliance on view-specific events like `SelectedMenuItemChanged`.
   - Impact: Enhances flexibility for hierarchical views, requires subscription management in superviews like `MenuBarv2`.
