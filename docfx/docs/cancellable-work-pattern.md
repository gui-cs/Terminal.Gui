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

## Lexicon and Taxonomy

[!INCLUDE [Events Lexicon](~/includes/events-lexicon.md)]

## Implementation in Terminal.Gui

The *Cancellable Work Pattern* is implemented consistently across several key areas of Terminal.Gui’s `v2_develop` branch. Below are five primary examples, each illustrating the pattern in a different domain: rendering, keyboard input at the view level, command execution, application-level keyboard input, and property changes.

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
- **Workflow**: Single phase for text drawing within the broader `Draw` workflow.
- **Notifications**: `OnDrawingText` (virtual), `DrawingText` (event).
- **Cancellation**: `OnDrawingText` returning `true` or `dev.Cancel = true`.
- **Context**: `DrawContext` and `DrawEventArgs` provide rendering details.
- **Default Behavior**: `DrawText` renders the view’s text.
- **Use Case**: Allows customization of text rendering (e.g., custom formatting) or cancellation (e.g., skipping text for performance).

### 2. View.Keyboard: View-Level Keyboard Input

The `View.ProcessKeyDown` method processes keyboard input for a view, mapping keys to commands or handling them directly. It is a linear workflow with a single phase per key event.

#### Example: `ProcessKeyDown`
```csharp
public virtual bool ProcessKeyDown(Key key)
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

#### Example: `RaiseActivating`
The `RaiseActivating` method handles `Command.Activate`:
```csharp
protected bool? RaiseActivating(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };
    if (OnActivating(args) || args.Handled)
    {
        return true;
    }
    Activating?.Invoke(this, args);
    return Activating is null ? null : args.Handled;
}
```
- **Workflow**: Single phase for `Command.Activate`.
- **Notifications**: `OnActivating` (virtual), `Activating` (event).
- **Cancellation**: `OnActivating` returning `true` or `args.Handled = true`.
- **Context**: `ICommandContext` provides `Command`, `Source`, and `Binding`.
- **Default Behavior**: `SetFocus` for `Command.Activate` (in `SetupCommands`).
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

## Proposed Enhancement: Command Propagation

The *Cancellable Work Pattern* in `View.Command` currently supports local `Command.Activate` and propagating `Command.Accept`. To address hierarchical coordination needs (e.g., `MenuBarv2` popovers, `Dialog` closing), a `PropagatedCommands` property is proposed (Issue #4050):

- **Change**: Add `IReadOnlyList<Command> PropagatedCommands` to `View`, defaulting to `[Command.Accept]`. `Raise*` methods propagate if the command is in `SuperView?.PropagatedCommands` and `args.Handled` is `false`.
- **Example**:
  ```csharp
  public IReadOnlyList<Command> PropagatedCommands { get; set; } = new List<Command> { Command.Accept };
  protected bool? RaiseActivating(ICommandContext? ctx)
  {
      CommandEventArgs args = new() { Context = ctx };
      if (OnActivating(args) || args.Handled)
      {
          return true;
      }
      Activating?.Invoke(this, args);
      if (!args.Handled && SuperView?.PropagatedCommands.Contains(Command.Activate) == true)
      {
          return SuperView.InvokeCommand(Command.Activate, ctx);
      }
      return Activating is null ? null : args.Handled;
  }
  ```
- **Impact**: Enables `Command.Activate` propagation for `MenuBarv2` while preserving `Command.Accept` propagation, maintaining decoupling and avoiding noise from irrelevant commands.

## Challenges and Recommendations

1. **Conflation in FlagSelector**:
   - **Issue**: `CheckBox.Activating` triggers `Accepting`, conflating state change and confirmation.
   - **Recommendation**: Refactor to separate `Activating` and `Accepting`:
     ```csharp
     checkbox.Activating += (sender, args) =>
     {
         if (RaiseActivating(args.Context) is true)
         {
             args.Handled = true;
         }
     };
     ```

2. **Propagation Limitations**:
   - **Issue**: Local `Command.Activate` restricts `MenuBarv2` coordination; `Command.Accept` uses hacks (#3925).
   - **Recommendation**: Adopt `PropagatedCommands` to enable targeted propagation, as proposed.

3. **Documentation Gaps**:
   - **Issue**: The pattern’s phases and `Handled` semantics are not fully documented.
   - **Recommendation**: Document the pattern’s structure, phases, and examples across `View.Draw`, `View.Keyboard`, `View.Command`, `Application.Keyboard`, and `OrientationHelper`.

4. **Complexity in Multi-Phase Workflows**:
   - **Issue**: `View.Draw`’s multi-phase workflow can be complex for developers to customize.
   - **Recommendation**: Provide clearer phase-specific documentation and examples.

## Conclusion

The *Cancellable Work Pattern* is a foundational design in Terminal.Gui, enabling extensible, cancellable, and decoupled workflows across rendering, input handling, command execution, and property changes. Its implementation in `View.Draw`, `View.Keyboard`, `View.Command`, `Application.Keyboard`, and `OrientationHelper` supports diverse use cases, from `Menuv2`’s hierarchical menus to `CheckBox`’s state toggling. The proposed `PropagatedCommands` property enhances the pattern’s applicability in `View.Command`, addressing propagation needs while maintaining its core principles. By refining implementation flaws (e.g., `FlagSelector`) and improving documentation, Terminal.Gui can further leverage this pattern for robust, flexible UI interactions.