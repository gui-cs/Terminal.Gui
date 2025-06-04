# Cancellable Work Pattern in Terminal.Gui

The *Cancellable Work Pattern* is a core design pattern in Terminal.Gui, used to structure workflows that can be executed in a default manner, modified by external code or subclasses, or cancelled entirely. This pattern is prevalent across various components of Terminal.Gui, including the `View` class for rendering, keyboard input, and command execution, as well as application-level input handling and property changes. Unlike traditional inheritance-based approaches that rely on overriding virtual methods (which often require subclasses to understand base class implementation details), the *Cancellable Work Pattern* prioritizes events for loose coupling, supplemented by optional virtual methods for flexibility.

This document is a conceptual definition of *Cancellable Work Pattern* and outlines its components and goals, and illustrates its implementation through examples in `View.Draw`, `View.Keyboard`, `View.Command`, `Application.Keyboard`, and `OrientationHelper`.

See the [Events Deep Dive](events.md) for a concrete deep dive and tutorial.

> [!NOTE]
> Some terms in this document are based on a yet-to-be addressed Issue: https://github.com/gui-cs/Terminal.Gui/issues/4050

## Definition

The *Cancellable Work Pattern* is a design pattern for executing a structured workflow with one or more phases, where each phase can:

- Proceed in a default manner.
- Be modified by external code or subclasses.
- Be cancelled to halt further processing.

The pattern uses events as the primary mechanism for notification and customization, supplemented by virtual methods for subclassing when needed. It is a specialization of the **Observer Pattern**, extended with structured workflows, explicit cancellation mechanisms, and context-aware notifications. It also incorporates elements of the **Template Method Pattern** (via virtual methods) and **Pipeline Pattern** (via sequential phases).

## Lexicon and Taxonomy

[!INCLUDE [Events Lexicon](~/includes/events-lexicon.md)]

## Core Concept

At its core, CWP defines a workflow as a sequence of one or more distinct phases, each representing a unit of work within a larger operation. For each phase, the pattern provides mechanisms to:

- **Execute Default Behavior**: A predefined implementation that executes if no external intervention occurs, ensuring the system remains functional out of the box.
- **Allow Customization**: Through event subscriptions or method overrides, external code or subclasses can inject custom logic to alter the phase's behavior without needing to replace the entire workflow.
- **Support Cancellation**: A explicit mechanism to halt the execution of a phase or the entire workflow, preventing further processing when certain conditions are met (e.g., user intervention, error states, or logical constraints).

This triadic structure—default execution, customization, and cancellation—distinguishes CWP from simpler event-driven or inheritance-based approaches. It ensures that workflows are both robust (via defaults) and flexible (via customization and cancellation), making it ideal for complex systems like terminal user interfaces where multiple stakeholders (e.g., framework developers, application developers, and end-users) interact with the same processes.

### Structural Components

The Cancellable Work Pattern typically comprises the following components:

1. **Workflow Container**: The entity (often a class or object) that encapsulates the overall workflow, defining the sequence of phases and orchestrating their execution. In Terminal.Gui, this might be a `View` object managing rendering or input handling.

2. **Phases**: Individual steps within the workflow, each representing a discrete unit of work. Each phase has a default implementation and points for intervention. For example, rendering text in a view could be a single phase within a broader drawing workflow.

3. **Notification Mechanisms**: Events or callbacks that notify external observers of a phase's impending execution, allowing them to intervene. These are typically implemented as delegate-based events (e.g., `DrawingText` event in Terminal.Gui) or virtual methods (e.g., `OnDrawingText`).

4. **Cancellation Flags**: Boolean indicators or properties within event arguments that signal whether a phase or workflow should be halted. In Terminal.Gui, this is often seen as `Handled` or `Cancel` properties in event args objects.

5. **Context Objects**: Data structures passed to notification handlers, providing relevant state or parameters about the phase (e.g., `DrawContext` or `Key` objects in Terminal.Gui), enabling informed decision-making by custom logic.

### Operational Flow

The operational flow of CWP follows a consistent pattern for each phase within a workflow:

1. **Pre-Phase Notification**: Before executing a phase's default behavior, the workflow container raises an event or calls a virtual method to notify potential observers or subclasses. This step allows for preemptive customization or cancellation.

2. **Cancellation Check**: If the notification mechanism indicates cancellation (e.g., a return value of `true` from a virtual method or a `Cancel` flag set in event args), the phase is aborted, and control may return or move to the next phase, depending on the workflow design.

3. **Default Execution**: If no cancellation occurs, the phase's default behavior is executed. This ensures the workflow progresses as intended in the absence of external intervention.

4. **Post-Phase Notification (Optional)**: In some implementations, a secondary notification occurs after the phase completes, informing observers of the outcome or updated state (e.g., `OrientationChanged` event after a property update in Terminal.Gui).

This flow repeats for each phase, allowing granular control over complex operations. Importantly, CWP decouples the workflow's structure from its customization, as external code can subscribe to events without needing to subclass or understand the container's internal logic.

## Advantages

- **Flexibility**: Developers can modify specific phases without altering the entire workflow, supporting a wide range of use cases from minor tweaks to complete overrides.

- **Decoupling**: By prioritizing events over inheritance, CWP reduces tight coupling between base and derived classes, adhering to principles of loose coupling in software design.

- **Robustness**: Default behaviors ensure the system remains operational even if no customization is provided, reducing the risk of incomplete implementations.

- **Control**: Cancellation mechanisms provide precise control over workflow execution, critical in interactive systems where user input or state changes may necessitate halting operations.

## Limitations

- **Complexity**: Multi-phase workflows can become intricate, especially when numerous events and cancellation points are involved, potentially leading to debugging challenges.

- **Performance Overhead**: Raising events and checking cancellation flags for each phase introduces minor performance costs, which may accumulate in high-frequency operations like rendering.

- **Learning Curve**: Understanding the pattern's structure and knowing when to use events versus overrides requires familiarity, which may pose a barrier to novice developers.

## Applicability

CWP is particularly suited to domains where workflows must balance standardization with adaptability, such as user interface frameworks (e.g., Terminal.Gui), game engines, or workflow automation systems. It excels in scenarios where operations are inherently interruptible—such as responding to user input, rendering dynamic content, or managing state transitions—and where multiple components or developers need to collaborate on the same process without tight dependencies.

In the context of Terminal.Gui, CWP underpins critical functionalities like view rendering, keyboard input processing, command execution, and property change handling, ensuring that these operations are both predictable by default and customizable as needed by application developers.

## Implementation in Terminal.Gui

The *Cancellable Work Pattern* is implemented consistently across several key areas of Terminal.Gui's `v2_develop` branch. Below are five primary examples, each illustrating the pattern in a different domain: rendering, keyboard input at the view level, command execution, application-level keyboard input, and property changes.

### 1. View.Draw: Rendering Workflow

The `View.Draw` method orchestrates the rendering of a view, including its adornments (margin, border, padding), viewport, text, content, subviews, and line canvas. It is a multi-phase workflow where each phase can be customized or cancelled.

#### Example: `DoDrawText`

The `DoDrawText` method, responsible for drawing a view's text, exemplifies the pattern:
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

- **Workflow**: Single phase for text drawing within the broader `Draw` workflow.
- **Notifications**: `OnDrawingText` (virtual), `DrawingText` (event).
- **Cancellation**: `OnDrawingText` returning `true` or `dev.Cancel = true`.
- **Context**: `DrawContext` and `DrawEventArgs` provide rendering details.
- **Default Behavior**: `DrawText` renders the view's text.
- **Use Case**: Allows customization of text rendering (e.g., custom formatting) or cancellation (e.g., skipping text for performance).

### 2. View.Keyboard: View-Level Keyboard Input

The `View.NewKeyDownEvent` method processes keyboard input for a view, mapping keys to commands or handling them directly. It is a linear workflow with a single phase per key event.

#### Example: `NewKeyDownEvent`

```csharp
public bool NewKeyDownEvent(Key key)
{
    if (OnKeyDown(key)) // Virtual method
    {
        return true; // Cancel if true
    }
    KeyDown?.Invoke(this, key); // Notify observers
    if (key.Handled) // Check for cancellation
    {
        return true;
    }
    bool handled = InvokeCommands(key, KeyBindingScope.HotKey | KeyBindingScope.Focused); // Default behavior
    return handled;
}
```
- **Workflow**: Linear, processing one key event.
- **Notifications**: `OnKeyDown` (virtual), `KeyDown` (event).
- **Cancellation**: `OnKeyDown` returning `true` or `key.Handled = true`.
- **Context**: `Key` provides key details and bindings.
- **Default Behavior**: `InvokeCommands` maps keys to commands (e.g., `Command.Accept`).
- **Use Case**: Allows views to customize key handling (e.g., `TextField` capturing input) or cancel default command execution.

### 3. View.Command: Command Execution

The `View.Command` APIs execute commands like `Command.Activate` and `Command.Accept`, used for state changes (e.g., `CheckBox` toggling) and action confirmation (e.g., dialog submission). It is a per-unit workflow, with one phase per command.

#### Example: `RaiseAccepting`

```csharp
protected bool? RaiseAccepting(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };
    if (OnAccepting(args) || args.Handled)
    {
        return true;
    }
    Accepting?.Invoke(this, args);
    return Accepting is null ? null : args.Handled;
}
```

- **Workflow**: Single phase for `Command.Accept`.
- **Notifications**: `OnAccepting` (virtual), `Accepting` (event).
- **Cancellation**: `OnAccepting` returning `true` or `args.Handled = true`.
- **Context**: `ICommandContext` provides `Command`, `Source`, and `Binding`.
- **Default Behavior**: Propagates to `SuperView` or default button if not handled.
- **Use Case**: Allows customization of state changes (e.g., `CheckBox` toggling) or cancellation (e.g., preventing focus in `MenuItemv2`).

#### Propagation Challenge

- `Command.Activate` is local, limiting hierarchical coordination (e.g., `MenuBarv2` popovers). A proposed `PropagatedCommands` property addresses this, as detailed in the appendix.

### 4. Application.Keyboard: Application-Level Keyboard Input

The `Application.OnKeyDown` method processes application-wide keyboard input, raising events for global key handling. It is an event-driven workflow, with a single phase per key event.

#### Example: `OnKeyDown`

```csharp
public static bool OnKeyDown(Key key)
{
    if (KeyDown is null)
    {
        return false;
    }
    KeyDown?.Invoke(null, key); // Notify observers
    return key.Handled; // Check for cancellation
}
```

- **Workflow**: Event-driven, processing one key event.
- **Notifications**: `KeyDown` (event, no virtual method).
- **Cancellation**: `key.Handled = true`.
- **Context**: `Key` provides key details.
- **Default Behavior**: None; relies on subscribers (e.g., `Top` view processing).
- **Use Case**: Allows global key bindings (e.g., `Ctrl+Q` to quit) or cancellation of default view handling.

### 5. OrientationHelper: Property Changes

The `OrientationHelper` class manages orientation changes (e.g., in `StackPanel`), raising events for property updates. It is an event-driven workflow, with a single phase per change.

#### Example: `Orientation` Setter

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
        var oldOrientation = _orientation;
        var args = new CancelEventArgs<Orientation>(_orientation, ref value);
        if (OnOrientationChanging(args))
        {
            return; // Cancel if true
        }
        OrientationChanging?.Invoke(this, args);
        if (args.Cancel)
        {
            return;
        }
        _orientation = value;
        var changedArgs = new EventArgs<Orientation>(oldOrientation, _orientation);
        OnOrientationChanged(changedArgs);
        OrientationChanged?.Invoke(this, changedArgs);
    }
}
```

- **Workflow**: Event-driven, processing one property change.
- **Notifications**: `OnOrientationChanging` (virtual), `OrientationChanging` (event), `OnOrientationChanged`, `OrientationChanged` (post-event).
- **Cancellation**: `OnOrientationChanging` returning `true` or `args.Cancel = true`.
- **Context**: `CancelEventArgs<Orientation>` provides old and new values.
- **Default Behavior**: Updates `_orientation` and notifies via `OrientationChanged`.
- **Use Case**: Allows customization of orientation changes (e.g., adjusting layout) or cancellation (e.g., rejecting invalid orientations).

