| Term | Meaning |
|:-----|:--------|
| **Action** | A delegate type that represents a method that can be called with specific parameters but returns no value. Used for simple callbacks in Terminal.Gui. |
| **Cancel/Cancelling/Cancelled** | Applies to scenarios where something can be cancelled. Changing the `Orientation` of a `Slider` is cancelable. |
| **Cancellation** | Mechanisms to halt a phase or workflow in the [Cancellable Work Pattern](~/docs/cancellable-work-pattern.md), such as setting `Cancel`/`Handled` properties in event arguments or returning `bool` from virtual methods. |
| **Command** | A pattern that encapsulates a request as an object, allowing for parameterization and queuing of requests. See [Command Deep Dive](~/docs/command.md). |
| **Context** | Data passed to observers for informed decision-making in the [Cancellable Work Pattern](~/docs/cancellable-work-pattern.md), such as `DrawContext` (drawing), `Key` (keyboard), `ICommandContext` (commands), or `CancelEventArgs<Orientation>` (orientation). |
| **Default Behavior** | A standard implementation for each phase in the [Cancellable Work Pattern](~/docs/cancellable-work-pattern.md), such as `DrawText` (drawing), `InvokeCommands` (keyboard and application-level), `RaiseActivating` (commands), or updating a property (`OrientationHelper`). |
| **Event** | A notification mechanism that allows objects to communicate when something of interest occurs. Terminal.Gui uses events extensively for UI interactions. |
| **Handle/Handling/Handled** | Applies to scenarios where an event can either be handled by an event listener (or override) vs not handled. Events that originate from a user action like mouse moves and key presses are examples. |
| **Invoke** | The act of calling or triggering an event, action, or method. |
| **Listen** | The act of subscribing to or registering for an event to receive notifications when it occurs. |
| **Notifications** | Events (e.g., `DrawingText`, `KeyDown`, `Activating`, `OrientationChanging`) and virtual methods (e.g., `OnDrawingText`, `OnKeyDown`, `OnActivating`, `OnOrientationChanging`) raised at each phase to notify observers in the [Cancellable Work Pattern](~/docs/cancellable-work-pattern.md). |
| **Raise** | The act of triggering an event, notifying all registered event handlers that the event has occurred. |
| **Workflow** | A sequence of phases in the [Cancellable Work Pattern](~/docs/cancellable-work-pattern.md), which may be multi-phase (e.g., rendering in `View.Draw`), linear (e.g., key processing in `View.Keyboard`), per-unit (e.g., command execution in `View.Command`), or event-driven (e.g., key handling in `Application.Keyboard`, property changes in `OrientationHelper`). | 