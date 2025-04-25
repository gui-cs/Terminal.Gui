# Cancellable Work Pattern in Terminal.Gui

The *Cancellable Work Pattern* is a core design pattern in Terminal.Gui, used to structure workflows that can be executed in a default manner, modified by external code or subclasses, or cancelled entirely. This pattern is prevalent across various components of Terminal.Gui, including the `View` class for rendering, keyboard input, and command execution, as well as application-level input handling and property changes. Unlike traditional inheritance-based approaches that rely on overriding virtual methods (which often require subclasses to understand base class implementation details), the *Cancellable Work Pattern* prioritizes events for loose coupling, supplemented by optional virtual methods for flexibility.

This deep dive defines the *Cancellable Work Pattern*, outlines its components and goals, and illustrates its implementation through examples in `View.Draw`, `View.Keyboard`, `View.Command`, `Application.Keyboard`, and `OrientationHelper`.

## Definition

The *Cancellable Work Pattern* is a design pattern for executing a structured workflow with one or more phases, where each phase can:
- Proceed in a default manner.
- Be modified by external code or subclasses.
- Be cancelled to halt further processing.

The pattern uses events as the primary mechanism for notification and customization, supplemented by virtual methods for subclassing when needed. It is a specialization of the **Observer Pattern**, extended with structured workflows, explicit cancellation mechanisms, and context-aware notifications. It also incorporates elements of the **Template Method Pattern** (via virtual methods) and **Pipeline Pattern** (via sequential phases).

## Goals

The *Cancellable Work Pattern* is designed to achieve the following:
1. **Default Execution**: Provide a standard process that executes unless interrupted, ensuring predictable behavior out of the box.
2. **Modification**: Allow external code or subclasses to customize specific phases without requiring deep knowledge of the implementation.
3. **Cancellation**: Enable halting of a phase or the entire workflow, giving consumers control over the process.
4. **Decoupling**: Use events to reduce reliance on inheritance, minimizing the need for subclasses to understand base class details.

## Components

The *Cancellable Work Pattern* consists of the following components:
- **Workflow**: A sequence of phases, which may be multi-phase (e.g., rendering in `View.Draw`), linear (e.g., key processing in `View.Keyboard`), per-unit (e.g., command execution in `View.Command`), or event-driven (e.g., key handling in `Application.Keyboard`, property changes in `OrientationHelper`).
- **Notifications**: Events (e.g., `DrawingText`, `KeyDown`, `Accepting`, `OrientationChanging`) and virtual methods (e.g., `OnDrawingText`, `OnKeyDown`, `OnAccepting`, `OnOrientationChanging`) raised at each phase to notify observers.
- **Cancellation**: Mechanisms to halt a phase or workflow, such as setting `Cancel`/`Handled` properties in event arguments or returning `bool` from virtual methods.
- **Context**: Data passed to observers for informed decision-making, such as `DrawContext` (drawing), `Key` (keyboard), `ICommandContext` (commands), or `CancelEventArgs<Orientation>` (orientation).
- **Default Behavior**: A standard implementation for each phase, such as `DrawText` (drawing), `InvokeCommands` (keyboard and application-level), `RaiseAccepting` (commands), or updating a property (`OrientationHelper`).

## Implementation in Terminal.Gui

The *Cancellable Work Pattern* is implemented consistently across several key areas of Terminal.Gui. Below are five primary examples, each illustrating the pattern in a different domain: rendering, keyboard input at the view level, command execution, application-level keyboard input, and property changes.

### 1. View.Draw: Rendering Workflow

The `View.Draw` method orchestrates the rendering of a view, including its adornments (margin, border, padding), viewport, text, content, subviews, and line canvas. It is a multi-phase workflow where each phase can be customized or cancelled.

#### Example: `DoDrawText`
The `DoDrawText` method, responsible for drawing a view’s text, exemplifies the pattern:
```csharp
private void DoDrawText(DrawContext? context = null)
{
    if (OnDrawingText(context)) // Virtual method for subclasses
    {
        return; // Cancel if true
    }
    if (OnDrawingText()) // Legacy virtual method
    {
        return; // Cancel if true
    }
    var dev = new DrawEventArgs(Viewport, Rectangle.Empty, context);
    DrawingText?.Invoke(this, dev); // Notify observers
    if (dev.Cancel) // Check for cancellation
    {
        return;
    }
    DrawText(context); // Default behavior
}
```

- **Workflow**: Single phase (text drawing) within the broader `Draw` workflow.
- **Default Behavior**: Calls `DrawText` to render the view’s text using the `TextFormatter`.
- **Modification**:
  - **Virtual Method**: `OnDrawingText` allows subclasses to preprocess or replace the default behavior.
  - **Event**: `DrawingText` enables external code to perform custom drawing or modify the process.
- **Cancellation**: Returning `true` from `OnDrawingText` or setting `dev.Cancel = true` in `DrawingText` halts text drawing.
- **Context**: `DrawContext` provides information about drawn regions, enabling precise customization.
- **Decoupling**: The `DrawingText` event allows external code to customize without subclassing.

**Usage Example**:
```csharp
// Modify default text drawing
view.DrawingText += (sender, args) =>
{
    // Custom text rendering
    args.Cancel = true; // Skip default DrawText
};

// Cancel text drawing
view.DrawingText += (sender, args) => { args.Cancel = true; };
view.Draw();
```

#### Other Phases
Similar patterns appear in other phases of `View.Draw`, such as `DoClearViewport` (`ClearingViewport` event, `OnClearingViewport` method), `DoDrawContent` (`DrawingContent`, `OnDrawingContent`), and `DoDrawSubViews` (`DrawingSubViews`, `OnDrawingSubViews`). Each phase follows the same structure, ensuring consistency.

### 2. View.Keyboard: Keyboard Input Workflow

The `View.Keyboard` code, particularly `NewKeyDownEvent`, processes keyboard input through a linear workflow that handles focused subviews, pre-processing, command invocation, and post-processing.

#### Example: `NewKeyDownEvent`
The `NewKeyDownEvent` method illustrates the pattern:
```csharp
public bool NewKeyDownEvent(Key key)
{
    if (!Enabled)
    {
        return false;
    }
    if (Focused?.NewKeyDownEvent(key) == true)
    {
        return true;
    }
    if (RaiseKeyDown(key) || key.Handled)
    {
        return true;
    }
    if (InvokeCommands(key) is true || key.Handled)
    {
        return true;
    }
    bool? handled = InvokeCommandsBoundToHotKey(key);
    if (handled is true)
    {
        return true;
    }
    if (RaiseKeyDownNotHandled(key) || key.Handled)
    {
        return true;
    }
    return key.Handled;
}
```

- **Workflow**: Linear sequence (subview processing, pre-processing, command invocation, post-processing).
- **Default Behavior**: Invokes commands bound to the key (`InvokeCommands`) or hotkey (`InvokeCommandsBoundToHotKey`).
- **Modification**:
  - **Virtual Methods**: `OnKeyDown` and `OnKeyDownNotHandled` allow subclasses to preprocess or post-process the key.
  - **Events**: `KeyDown` and `KeyDownNotHandled` enable external code to customize key handling.
- **Cancellation**: Setting `key.Handled = true` in `KeyDown`/`KeyDownNotHandled` or returning `true` from `OnKeyDown`/`OnKeyDownNotHandled` halts processing.
- **Context**: The `Key` object provides key state and handling status.
- **Decoupling**: Events allow external customization without subclassing.

**Usage Example**:
```csharp
// Modify key handling
view.KeyDown += (sender, k) =>
{
    if (k.KeyCode == KeyCode.Enter)
    {
        // Custom Enter key logic
        k.Handled = true;
    }
};

// Cancel key processing
view.KeyDown += (sender, k) => { k.Handled = true; };
view.NewKeyDownEvent(new Key(KeyCode.Enter));
```

### 3. View.Command: Command Execution Workflow

The `View.Command` code handles command execution, with methods like `RaiseAccepting` implementing the pattern for specific commands (e.g., `Command.Accept`).

#### Example: `RaiseAccepting`
The `RaiseAccepting` method, invoked for `Command.Accept`, demonstrates the pattern:
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

- **Workflow**: Per-command sequence (pre-processing, event raising, propagation to default button/superview).
- **Default Behavior**: Raises the `Accepting` event and propagates the command to a default button or superview.
- **Modification**:
  - **Virtual Method**: `OnAccepting` allows subclasses to preprocess or cancel the command.
  - **Event**: `Accepting` enables external code to customize or replace the behavior.
- **Cancellation**: Setting `args.Cancel = true` in `Accepting` or returning `true` from `OnAccepting` halts processing.
- **Context**: `ICommandContext` provides command source and binding information.
- **Decoupling**: The `Accepting` event allows external customization without subclassing.

**Usage Example**:
```csharp
// Modify command behavior
view.Accepting += (sender, args) =>
{
    // Custom accept logic
    args.Cancel = true; // Cancel the command
};

// Cancel command
view.Accepting += (sender, args) => { args.Cancel = true; };
view.InvokeCommand(Command.Accept);
```

#### Other Commands
Similar patterns appear in `RaiseSelecting` (`Selecting` event, `OnSelecting` method), `RaiseHandlingHotKey` (`HandlingHotKey`, `OnHandlingHotKey`), and `RaiseCommandNotBound` (`CommandNotBound`, `OnCommandNotBound`), ensuring all commands follow the pattern.

### 4. Application.Keyboard: Application-Level Keyboard Input Workflow

The `Application` class handles application-level keyboard input through methods like `RaiseKeyDownEvent`, which processes key presses across top-level views and application-scoped key bindings. This workflow exemplifies the *Cancellable Work Pattern* by allowing cancellation and modification at multiple stages.

#### Example: `RaiseKeyDownEvent`
The `RaiseKeyDownEvent` method orchestrates the processing of a key press at the application level:
```csharp
public static bool RaiseKeyDownEvent(Key key)
{
    KeyDown?.Invoke(null, key);
    if (key.Handled)
    {
        return true;
    }
    if (Popover?.DispatchKeyDown(key) is true)
    {
        return true;
    }
    if (Top is null)
    {
        foreach (Toplevel topLevel in TopLevels.ToList())
        {
            if (topLevel.NewKeyDownEvent(key))
            {
                return true;
            }
            if (topLevel.Modal)
            {
                break;
            }
        }
    }
    else
    {
        if (Top.NewKeyDownEvent(key))
        {
            return true;
        }
    }
    bool? commandHandled = InvokeCommandsBoundToKey(key);
    if (commandHandled is true)
    {
        return true;
    }
    return false;
}
```

- **Workflow**: Linear sequence (application-level event, popover dispatch, top-level view processing, application-scoped command invocation).
- **Default Behavior**: Dispatches the key to the popover (if any), top-level views via `NewKeyDownEvent`, and application-scoped commands via `InvokeCommandsBoundToKey`.
- **Modification**:
  - **Event**: `KeyDown` allows external code to preprocess the key at the application level.
  - **View Processing**: Top-level views can modify the key handling through their own `NewKeyDownEvent` (which itself follows the pattern).
- **Cancellation**: Setting `key.Handled = true` in `KeyDown` or returning `true` from `DispatchKeyDown`, `NewKeyDownEvent`, or `InvokeCommandsBoundToKey` halts processing.
- **Context**: The `Key` object provides key state and handling status.
- **Decoupling**: The `KeyDown` event enables external customization without modifying the `Application` class or view hierarchy.

**Usage Example**:
```csharp
// Modify application-level key handling
Application.KeyDown += (sender, k) =>
{
    if (k.KeyCode == KeyCode.Esc)
    {
        // Custom Esc key logic
        k.Handled = true;
    }
};

// Cancel key processing
Application.KeyDown += (sender, k) => { k.Handled = true; };
Application.RaiseKeyDownEvent(new Key(KeyCode.Esc));
```

#### Other Methods
The `RaiseKeyUpEvent` method follows a similar pattern, raising the `KeyUp` event and dispatching to top-level views, with cancellation via `key.Handled`. The `InvokeCommandsBoundToKey` method further supports the pattern by executing application-scoped commands, which can be cancelled or modified through their implementations.

### 5. OrientationHelper: Property Change Workflow

The `OrientationHelper` class, implementing the `IOrientation` interface, manages the `Orientation` property for views that support horizontal or vertical orientation. The `Orientation` property’s setter follows the *Cancellable Work Pattern* to handle changes to the orientation value.

#### Example: `Orientation` Property
The `Orientation` property in `OrientationHelper` implements the pattern:
```csharp
public Orientation Orientation
{
    get => _orientation;
    set
    {
        if (_orientation == value)
        {
            return;
        }
        if (_owner?.OnOrientationChanging(value, _orientation) ?? false)
        {
            return;
        }
        CancelEventArgs<Orientation> args = new(in _orientation, ref value);
        OrientationChanging?.Invoke(_owner, args);
        if (args.Cancel)
        {
            return;
        }
        Orientation old = _orientation;
        if (_orientation != value)
        {
            _orientation = value;
            if (_owner is {})
            {
                _owner.Orientation = value;
            }
        }
        _owner?.OnOrientationChanged(_orientation);
        OrientationChanged?.Invoke(_owner, new(in _orientation));
    }
}
```

- **Workflow**: Single phase (changing the orientation property).
- **Default Behavior**: Updates the `_orientation` field and notifies the owning view.
- **Modification**:
  - **Virtual Method**: `OnOrientationChanging` allows the owning view to preprocess or cancel the change.
  - **Event**: `OrientationChanging` enables external code to modify or cancel the change.
  - **Post-Event**: `OrientationChanged` and `OnOrientationChanged` notify completion, allowing further customization.
- **Cancellation**: Returning `true` from `OnOrientationChanging` or setting `args.Cancel = true` in `OrientationChanging` prevents the property update.
- **Context**: `CancelEventArgs<Orientation>` provides the current and new orientation values, enabling informed decisions.
- **Decoupling**: The `OrientationChanging` and `OrientationChanged` events allow external customization without modifying the `OrientationHelper` or owning view.

**Usage Example**:
```csharp
// Modify orientation change
view.OrientationChanging += (sender, args) =>
{
    if (args.NewValue == Orientation.Vertical)
    {
        // Custom logic for vertical orientation
        args.Cancel = true; // Cancel the change
    }
};

// Cancel orientation change
view.OrientationChanging += (sender, args) => { args.Cancel = true; };
view.Orientation = Orientation.Vertical;

// Post-change notification
view.OrientationChanged += (sender, args) =>
{
    // Handle orientation change
};
```

## Standard Implementation Pattern

The *Cancellable Work Pattern* is implemented using a consistent structure across Terminal.Gui, typically via a method or property setter that raises events and executes default behavior. The standard pattern is as follows:

1. **Check Pre-Conditions**: Verify if the work should proceed (e.g., view is enabled, value has changed).
2. **Call Virtual Method**: Invoke a `protected virtual` method (e.g., `OnXxx`) to allow subclasses to preprocess or cancel.
   - If the method returns `true`, halt processing.
3. **Raise Cancelable Event**: Invoke a cancelable event (e.g., `Xxx` with `Cancel`/`Handled` property) to notify external subscribers.
   - If the event sets `Cancel`/`Handled` to `true`, halt processing.
4. **Execute Default Behavior**: Perform the default work (e.g., draw text, invoke command, update property) if not cancelled.
5. **Raise Post-Event (Optional)**: In some cases, raise a non-cancelable event (e.g., `ClearedViewport`, `OrientationChanged`) to notify completion.

### Example Implementation: `RaiseCommandNotBound`
```csharp
protected bool? RaiseCommandNotBound(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };
    if (OnCommandNotBound(args) || args.Cancel)
    {
        return true;
    }
    CommandNotBound?.Invoke(this, args);
    return CommandNotBound is null ? null : args.Cancel;
}
```

- **Return Type**: `bool?` to indicate:
  - `null`: No event was raised (no subscribers).
  - `false`: Event was raised but not cancelled.
  - `true`: Event was cancelled or handled.
- **Virtual Method**: `OnCommandNotBound` allows subclass customization.
- **Event**: `CommandNotBound` enables external customization.
- **Cancellation**: `args.Cancel` halts processing.

This structure is mirrored in `View.Draw` (e.g., `DoDrawText`), `View.Keyboard` (e.g., `RaiseKeyDown`), `View.Command` (e.g., `RaiseAccepting`), `Application.Keyboard` (e.g., `RaiseKeyDownEvent`), and `OrientationHelper` (e.g., `Orientation` setter).

## Comparison to Other Patterns

The *Cancellable Work Pattern* is distinct from other design patterns but shares some characteristics:

- **Observer Pattern**:
  - **Similarity**: Uses events to notify observers, enabling loose coupling.
  - **Difference**: Adds structured workflows, explicit cancellation, and context-aware notifications.
  - **Example**: `KeyDown` in `Application.Keyboard` notifies observers like the Observer Pattern but allows cancellation via `key.Handled`.

- **Template Method Pattern**:
  - **Similarity**: Defines a workflow with customizable steps (via virtual methods).
  - **Difference**: Prioritizes events over inheritance, reducing coupling to base class details.
  - **Example**: `OnOrientationChanging` in `OrientationHelper` allows subclass customization, but `OrientationChanging` events are the primary mechanism.

- **Pipeline Pattern**:
  - **Similarity**: Executes sequential phases, like a pipeline.
  - **Difference**: Emphasizes cancellation and observer notifications at each phase.
  - **Example**: `View.Draw`’s multi-phase workflow resembles a pipeline but includes cancelable events like `ClearingViewport`.

## Benefits and Trade-offs

### Benefits
- **Extensibility**: Developers can customize specific phases without modifying the core implementation.
- **Decoupling**: Events allow external code to participate without subclassing, reducing tight coupling.
- **Flexibility**: Supports both simple (e.g., `OrientationHelper`) and complex (e.g., `View.Draw`) workflows.
- **Consistency**: Uniform structure across rendering, input, commands, and property changes ensures predictability.

### Trade-offs
- **Complexity**: Multiple notification points (events and virtual methods) can be overwhelming for simple use cases.
- **Performance**: Raising events and checking cancellation adds overhead, though typically negligible in UI contexts.
- **Incomplete Event Coverage**: Some areas (e.g., `RenderLineCanvas` in `View.Draw`) lack events, relying on virtual methods, which slightly deviates from the pattern’s ideal decoupling.

## Recommendations for Developers

When using or extending the *Cancellable Work Pattern* in Terminal.Gui:
- **Prefer Events**: Use events (e.g., `DrawingText`, `KeyDown`, `Accepting`, `OrientationChanging`) for customization to maintain loose coupling.
- **Use Virtual Methods Sparingly**: Override virtual methods (`OnDrawingText`, `OnKeyDown`, `OnAccepting`, `OnOrientationChanging`) only when subclassing is necessary.
- **Leverage Context**: Utilize context objects (`DrawContext`, `Key`, `ICommandContext`, `CancelEventArgs<Orientation>`) to make informed customization decisions.
- **Handle Cancellation**: Check `Cancel`/`Handled` properties in event handlers to respect the workflow’s state.
- **Contribute Events**: For areas lacking events (e.g., `RenderLineCanvas`), consider adding them to enhance extensibility.

## Future Improvements

To strengthen the *Cancellable Work Pattern* in Terminal.Gui:
- **Complete Event Coverage**: Add events for all phases (e.g., `RenderLineCanvas` in `View.Draw`) to eliminate reliance on virtual methods.
- **Standardize Propagation**: Refine command propagation in `View.Command` (e.g., default button handling) and key dispatch in `Application.Keyboard` to use a generic mechanism.
- **Add Pre-Focus Event**: In `View.Keyboard` and `Application.Keyboard`, introduce a `KeyDownPreFocus` event to allow superviews to intercept keys before subviews.
- **Optimize Performance**: Minimize event overhead in performance-critical paths, though this is rarely an issue in UI rendering.

## Conclusion

The *Cancellable Work Pattern* is a powerful and flexible design pattern in Terminal.Gui, enabling extensible, decoupled, and cancellable workflows for rendering (`View.Draw`), keyboard input (`View.Keyboard`, `Application.Keyboard`), command execution (`View.Command`), and property changes (`OrientationHelper`). By prioritizing events over inheritance, it aligns with the Observer Pattern while adding structured workflows and explicit cancellation. Its consistent implementation across diverse domains makes it a cornerstone of Terminal.Gui’s extensibility, empowering developers to customize UI behavior with minimal coupling.

For further details, refer to the Terminal.Gui source code and documentation:
- [View.Draw](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/drawing.html)
- [Keyboard APIs](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/keyboard.html)
- [Command APIs](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/commands.html)